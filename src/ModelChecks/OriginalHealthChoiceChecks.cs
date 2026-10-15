using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalHealthChoiceChecks
{
    private const string OwnerId = "ac208c49-610a-43e7-80cc-e9680cf84fcd";
    private const string Gear = "fc119000-0000-4000-8000-000000000401";
    private const string Wound = "fc119000-0000-4000-8000-000000000403";
    private const string Nudge = "fc119000-0000-4000-8000-000000000404";
    private const string Heal = "fc119000-0000-4000-8000-000000000405";
    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        var context = state.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(state);
        string? Data(int id) => context.FindCard(id)?.DataId;
        var room = state.Spawn.Train.Rooms[0];
        foreach (int maximum in new[] { 80, 13 })
        {
            if (room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == maximum)) continue;
            var summon = plays.FirstOrDefault(play => play.RoomIndex == 0 && play.PlayerPosition == room.Units.Count(unit => unit.Team == CombatTeam.Player) &&
                state.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == Data(play.CardInstanceId))?.SpawnUnit?.MaxHealth == maximum);
            if (summon != null) return summon;
        }
        var strong = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 80);
        if (strong?.EquipmentCards?.Count == 0)
        {
            var equip = plays.FirstOrDefault(play => Data(play.CardInstanceId) == Gear && play.TargetUnitId == strong.Id);
            if (equip != null) return equip;
        }
        var fragile = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 13);
        if (fragile != null)
        {
            bool Played(string data) => context.Statistics!.Values.Any(value => value.Type == "TimesPlayed" && value.Duration == "ThisBattle" &&
                value.Value > 0 && context.FindCard(value.CardId)?.DataId == data);
            int equipped = fragile.EquipmentCards!.Count;
            string? wanted = !Played(Wound) ? Wound : equipped == 0 ? Gear : !Played(Nudge) ? Nudge : equipped == 1 ? Gear :
                !Played(Heal) ? Heal : equipped == 2 ? Gear : null;
            var action = plays.FirstOrDefault(play => wanted != null && Data(play.CardInstanceId) == wanted && play.TargetUnitId == fragile.Id);
            if (action != null) return action;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => Data(card.InstanceId) != Gear && Data(card.InstanceId) != Wound &&
            Data(card.InstanceId) != Nudge && Data(card.InstanceId) != Heal);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-health-choice-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Original health-choice native battle failed acceptance.");
        string? definition = null;
        void Definition(CombatContext context)
        {
            var owner = context.Relics!.Single(relic => relic.DataId == OwnerId);
            var rule = owner.TriggerAdditions!.Single(); var trigger = rule.Triggers.Single(); var effect = trigger.Effects.Single();
            var action = effect.Action!;
            Require(owner.AssetKey == "HairOfTheHybrid" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual([RelicModel.AddTrigger]) && rule.SourcePlayer && !rule.SourceEnemy && rule.SubtypeIsNone &&
                !rule.SubtypeIsPyre && rule.NoCountModifiersAllowed && rule.Conditions.Count == 0 && rule.CharacterAssetKeys.Count == 0 &&
                rule.ExternalInteractions.Count == 0 && trigger.Kind == "OnEquipmentAdded" && !trigger.Once && trigger.IgnoreSilence &&
                trigger.SkipDuringDeployment == false && trigger.Origin?.OnlyIfEquipped == false &&
                effect.Type == "CardEffectAddStatusEffectBasedOnHealth" && action.Type == "AddStatus" && action.Target == "Self" &&
                action.AllowEnemy && action.AllowPlayer && action.Value == 50 && effect.StatusScaling?.HealthChoicePercent == 50 &&
                !effect.StatusScaling.HealthChoiceAbove && action.Statuses.Select(status => (status.Id, status.Stacks)).SequenceEqual([("armor", 10), ("buff", 5)]),
                "Original health-choice owner, filter or payload changed.");
            string current = JsonSerializer.Serialize(rule, ModelJson.Options);
            if (definition != null) Require(current == definition, "Original health-choice definition drifted.");
            definition = current;
        }
        int friends = 0, enemies = 0;
        foreach (var row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            var before = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            var actual = row.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            Definition(before.Context!);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            var result = RelicBirthModel.CharacterAdded(before, row.GetProperty("UnitId").GetInt32(), row.GetProperty("FromCardId").GetInt32(),
                row.GetProperty("OnlyCovenants").GetBoolean(), row.GetProperty("DeferCallbacks").GetBoolean(),
                row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
            Require(result.Supported, "Health-choice birth unsupported: " + result.UnsupportedReason);
            Equal(result.State, actual, "birth"); Equal(result.Queued, row.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>(), "queue");
            Equal(result.Dispatched, row.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), "dispatch");
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Birth changed its parent.");
            var unit = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == row.GetProperty("UnitId").GetInt32());
            if (unit.Team == CombatTeam.Enemy)
            {
                Require(!unit.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Type == "CardEffectAddStatusEffectBasedOnHealth")), "Enemy acquired friendly-only relic trigger."); enemies++;
            }
            else friends++;
        }
        var witnessed = new HashSet<(int Health, int Maximum, string Status)>(); int count = 0, ablations = 0;
        foreach (var row in root.GetProperty("TriggeredStatuses").EnumerateArray())
        {
            var effect = row.GetProperty("Effect").Deserialize<CombatEffect>()!;
            if (effect.Type != "CardEffectAddStatusEffectBasedOnHealth") continue;
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Interactions").GetArrayLength() == 0 &&
                row.GetProperty("Kind").GetString() == "OnEquipmentAdded", "Incomplete native health-choice callback.");
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!; Definition(before.Context!);
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            int actorId = row.GetProperty("ActorId").GetInt32(); var actor = before.Units.Single(unit => unit.Id == actorId);
            Require(actor.Triggers.Any(trigger => trigger.Kind == "OnEquipmentAdded" && trigger.Effects.Any(item =>
                JsonSerializer.Serialize(item, ModelJson.Options) == JsonSerializer.Serialize(effect, ModelJson.Options))), "Native status lacks original installed trigger.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            var result = TriggeredStatusModel.Apply(before, actorId, effect, row.GetProperty("Targets").Deserialize<int[]>()!, row.GetProperty("SourceCardId").GetInt32());
            Require(result.Supported, "Native health-dependent status unsupported: " + result.UnsupportedReason);
            Equal(result.State, actual, "effect");
            Require(before.Context!.BattleRng.Equals(actual.Context!.BattleRng), "Health-choice effect consumed random status/chance RNG.");
            string selected = actor.Health < (int)Math.Ceiling(actor.MaxHealth * .5f) ? "armor" : "buff";
            int amount = selected == "armor" ? 10 : 5;
            Require((actual.Units.Single(unit => unit.Id == actorId).Status(selected)?.Stacks ?? 0) - (actor.Status(selected)?.Stacks ?? 0) == amount,
                "Original health-choice positive delta missing.");
            witnessed.Add((actor.Health, actor.MaxHealth, selected)); count++;
            var controlActor = actor.WithTriggers(actor.Triggers.Where(trigger => !trigger.Effects.Any(item => item.Type == effect.Type)).ToArray());
            var controlRoot = new RoomCombatState(before.RoomIndex, before.Deployment, before.Units.Select(unit => unit.Id == actorId ? controlActor : unit).ToArray(),
                before.ExternalInteractions, before.Context.WithRelics(before.Context.Relics!.Where(relic => relic.DataId != OwnerId).ToArray()), before.Preview, before.ActiveCompanionBossIds);
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var control = RoomCombatModel.ApplyQueuedCharacterTrigger(controlRoot, new(before.RoomIndex, controlActor, "OnEquipmentAdded"), callbacks.Add);
            Require(control.Supported && (control.State!.Units.Single(unit => unit.Id == actorId).Status(selected)?.Stacks ?? 0) == (actor.Status(selected)?.Stacks ?? 0),
                "Removed owner/trigger retained its health-choice effect."); ablations++;
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Native health-choice check changed its parent.");
            foreach (bool above in new[] { false, true })
            foreach (int hp in new[] { 0, 6, 7, 13 })
            foreach (bool preview in new[] { false, true })
            {
                var unit = UnitCloneModel.Copy(actor, actor.Modifiers!, actor.Attack, hp, 13, actor.EquipmentCards!);
                var sample = new RoomCombatState(before.RoomIndex, before.Deployment, [unit], [], before.Context, preview);
                var choice = new CombatEffect(effect.Type, 50, 0, "", 0, [], false, action: effect.Action,
                    statusScaling: new(healthChoicePercent: 50, healthChoiceAbove: above));
                var applied = TriggeredStatusModel.Apply(sample, actorId, choice, [actorId]);
                string status = (above ? hp >= 7 : hp < 7) ? "armor" : "buff";
                Require(applied.Supported && (applied.State!.Units.Single().Status(status)?.Stacks ?? 0) - (unit.Status(status)?.Stacks ?? 0) == (status == "armor" ? 10 : 5) &&
                    sample.Context!.BattleRng.Equals(applied.State.Context!.BattleRng), "Odd-health/inverted/retained/preview control differs.");
            }
        }
        Require(friends > 0 && enemies > 0 && count >= 4 && ablations == count &&
            witnessed.IsSupersetOf([(7, 13, "buff"), (6, 13, "armor"), (13, 13, "buff")]), "Missing natural odd-health boundary/control witnesses.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-HEALTH-CHOICE-NATIVE PASS: {friends} friendly/{enemies} enemy births, {count} exact equipment callbacks and positive owner ablations, native 13/7/6 HP boundaries; no status/chance RNG, inverted/preview/retained controls and complete policy replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label) => Require(JsonSerializer.Serialize(predicted, ModelJson.Options) == JsonSerializer.Serialize(actual, ModelJson.Options),
        "Health choice " + label + " differs: " + ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)));
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
