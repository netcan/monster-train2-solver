using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnitResourceChecks
{
    private const string GearId = "fc119000-0000-4000-8000-000000000461";
    private const string SacrificeId = "fc119000-0000-4000-8000-000000000462";
    private static readonly (string Id, string Key, string Type, int Amount, string Trigger, string? Subtype)[] Owners = [
        ("265d9fb6-934f-4b7d-ba12-d3a06c48b610", "GoldOnDeath", RelicUnitResourceModel.Gold, 5, "OnDeath", null),
        ("4a5b1613-568c-4bf6-8bf7-15f8bf013345", "EmberOnTombDeath", RelicUnitResourceModel.Ember, 5, "OnDeath", "SubtypesData_Tomb"),
        ("56316fc2-b41a-41ae-aba8-e83fb668e8e1", "Frenzystesia", RelicUnitResourceModel.Ember, 1, "OnEquipmentAdded", null) ];
    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        var context = state.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(state);
        string? Data(int id) => context.FindCard(id)?.DataId;
        bool sacrificed = context.Statistics!.Values.Any(value => value.Type == "TimesPlayed" && value.Duration == "ThisBattle" && value.Value > 0 && Data(value.CardId) == SacrificeId);
        var room = state.Spawn.Train.Rooms[0];
        foreach (bool strong in new[] { true, false })
        {
            if (room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == (strong ? 80 : 12)) || !strong && sacrificed) continue;
            var summon = plays.FirstOrDefault(play => play.RoomIndex == 0 && play.PlayerPosition == room.Units.Count(unit => unit.Team == CombatTeam.Player) &&
                state.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == Data(play.CardInstanceId))?.SpawnUnit?.MaxHealth == (strong ? 80 : 12));
            if (summon != null) return summon;
        }
        var target = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 80);
        if (target != null)
        {
            var gear = plays.FirstOrDefault(play => Data(play.CardInstanceId) == GearId && play.TargetUnitId == target.Id);
            if (gear != null) return gear;
        }
        var tomb = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 12);
        if (tomb != null && !sacrificed)
        {
            var sacrifice = plays.FirstOrDefault(play => Data(play.CardInstanceId) == SacrificeId && play.TargetUnitId == tomb.Id);
            if (sacrifice != null) return sacrifice;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => Data(card.InstanceId) != GearId && Data(card.InstanceId) != SacrificeId);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-unit-resource-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" && root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0), "Unit resource battle failed acceptance.");
        var positives = Owners.ToDictionary(owner => owner.Id, _ => 0); int count = 0, teamControls = 0, subtypeControls = 0, conditionControls = 0, ablations = 0;
        foreach (var row in root.GetProperty("RelicUnitResources").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Unfinished native unit resource callback.");
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!; var actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            var actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!; string kind = row.GetProperty("Trigger").GetString()!;
            int param = row.GetProperty("ParamInt").GetInt32(); bool alive = row.GetProperty("Alive").GetBoolean(), placement = row.GetProperty("Placement").GetBoolean();
            foreach (var expected in Owners)
            {
                var owner = before.Relics!.Single(item => item.DataId == expected.Id); var effect = owner.ResourceEffects!.Single(); var rule = effect.UnitTrigger!;
                Require(owner.AssetKey == expected.Key && owner.DisallowedInPlacementPhase == false && owner.IsCovenant == false &&
                    owner.EffectTypes.SequenceEqual([expected.Type]) && effect.EffectIndex == 0 && effect.Amount == expected.Amount && effect.ParamInt == expected.Amount &&
                    effect.RequiredMutatorId == null && rule.Trigger == expected.Trigger && rule.Team == "Monsters" && rule.Subtype == expected.Subtype && rule.RequiredStatuses.Count == 0,
                    "Original unit resource definition changed: " + expected.Key);
                if (expected.Key == "Frenzystesia")
                {
                    var condition = effect.Conditions.Single();
                    Require(condition.TrackTriggerCount && condition.Query.Type == "SubtypeInDeck" && condition.Query.Duration == "ThisTurn" &&
                        condition.Comparator == 1 && condition.Value == 1 && !condition.AllowMultiple, "Original first-equipment condition changed.");
                }
                else Require(effect.Conditions.Count == 0, "Unexpected original resource condition.");
            }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            var result = RelicUnitResourceModel.Apply(before, actor, kind, alive, param, placement);
            Require(result.Supported, "Unit resource consumer unsupported: " + result.UnsupportedReason);
            Equal(result.Context, actual, "actual manager callback");
            foreach (var witness in result.Events)
            {
                positives[witness.RelicId]++;
                var removed = before.WithRelics(before.Relics!.Where(owner => owner.DataId != witness.RelicId).ToArray());
                var control = RelicUnitResourceModel.Apply(removed, actor, kind, alive, param, placement);
                Require(control.Supported && (control.Context!.Gold != actual.Gold || control.Context.QueryFrame!.Energy != actual.QueryFrame!.Energy), "Positive original resource witness survived owner ablation."); ablations++;
            }
            if (actor.Team == CombatTeam.Enemy && kind == "OnDeath") { Require(result.Events.Count == 0, "Enemy death received player resource."); teamControls++; }
            if (actor.Team == CombatTeam.Player && kind == "OnDeath" && !actor.Subtypes.Contains("SubtypesData_Tomb"))
            { Require(result.Events.All(item => item.RelicId != Owners[1].Id), "Non-Tomb received Tomb energy."); subtypeControls++; }
            if (kind == "OnEquipmentAdded" && result.Events.Count == 0) conditionControls++;
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Unit resource consumer mutated parent state."); count++;
        }
        Require(count > 20 && positives.Values.All(value => value > 0) && teamControls > 0 && subtypeControls > 0 && conditionControls > 0 && ablations >= 3,
            "Missing original resource positives or team/subtype/condition controls.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-UNIT-RESOURCES-NATIVE PASS: three unchanged owners, {count} exact manager contexts, positives {string.Join('/', positives.Values)}, {teamControls} enemy deaths, {subtypeControls} non-Tomb deaths, {conditionControls} equipment-condition controls, {ablations} positive ablations; exact gold/energy/conditions/RNG and independent policy replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label) => Require(JsonSerializer.Serialize(predicted, ModelJson.Options) == JsonSerializer.Serialize(actual, ModelJson.Options),
        "Unit resources " + label + " differs: " + ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)));
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
