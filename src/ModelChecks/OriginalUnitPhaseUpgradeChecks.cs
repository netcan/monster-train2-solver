using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnitPhaseUpgradeChecks
{
    private const string Death = "52c41ba0-f720-4b7e-9dd0-bd318be6acae", Moon = "e54e6cb7-f424-44be-a7d2-869c6fb7bb15";
    private const string Selene = "fc119000-0000-4000-8000-000000000511", Silent = "fc119000-0000-4000-8000-000000000512",
        Ordinary = "fc119000-0000-4000-8000-000000000513", Fragile = "fc119000-0000-4000-8000-000000000514", Sacrifice = "fc119000-0000-4000-8000-000000000515";
    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        var context = state.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(state); var room = state.Spawn.Train.Rooms[0];
        bool sacrificed = context.Statistics!.Values.Any(value => value.Type == "TimesPlayed" && value.Duration == "ThisBattle" && value.Value > 0 && context.FindCard(value.CardId)?.DataId == Sacrifice);
        foreach (string id in new[] { Selene, Silent, Ordinary, Fragile })
        {
            if (id == Fragile && sacrificed || room.Units.Any(unit => context.FindCard(unit.SpawnerCardId)?.DataId == id)) continue;
            var summon = plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == id && play.RoomIndex == 0 && play.PlayerPosition == room.Units.Count(unit => unit.Team == CombatTeam.Player));
            if (summon != null) return summon;
        }
        var victim = room.Units.FirstOrDefault(unit => context.FindCard(unit.SpawnerCardId)?.DataId == Fragile);
        if (victim != null)
        { var action = plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == Sacrifice && play.TargetUnitId == victim.Id); if (action != null) return action; }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => context.FindCard(card.InstanceId)?.DataId != Sacrifice);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-unit-phase-upgrade-relics" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0), "Phase-upgrade capture failed acceptance.");
        int deaths = 0, adds = 0, removes = 0; var phases = new HashSet<string>(); var originalOwners = new Dictionary<string, CombatRelicState>();
        foreach (var row in root.GetProperty("RelicPhaseUpgrades").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Unfinished native upgrade consumer.");
            string id = row.GetProperty("Owner").GetString()!, trigger = row.GetProperty("Trigger").GetString()!;
            var before = row.GetProperty("Before").Deserialize<CombatUnit>()!; var after = row.GetProperty("Actual").Deserialize<CombatUnit>()!;
            var context = row.GetProperty("ContextBefore").Deserialize<CombatContext>()!; var next = row.GetProperty("ContextAfter").Deserialize<CombatContext>()!;
            var owner = context.Relics!.Single(item => item.DataId == id); originalOwners[id] = owner;
            Require(owner.DisallowedInPlacementPhase == false && owner.IsCovenant == false && before.Team == CombatTeam.Player && !before.IsPyre &&
                owner.EffectTypes.All(type => type == RelicModel.AddRemoveTempUpgradeOnUnitTrigger), "Original unit-upgrade owner changed.");
            var rule = owner.UnitUpgradeTriggers!.Single(item => item.Trigger == trigger); bool remove = row.GetProperty("Remove").GetBoolean();
            Require(remove == rule.ShouldRemove && rule.SourcePlayer && !rule.SourceEnemy && rule.Upgrade != null, "Original unit-upgrade filter/remove semantics differ.");
            if (id == Death)
            {
                Require(owner.AssetKey == "EnhanceAttackOnConsume" && owner.UnitUpgradeTriggers.Count == 1 && trigger == "OnDeath" &&
                    rule.SubtypeIsNone && rule.UpgradeLifetime == 0 && !remove && rule.Upgrade!.Stats.Damage == 10 &&
                    rule.Upgrade.DataId == "1f0a08f2-ab63-442e-a089-a1063bf7e7aa", "Original death upgrade changed.");
                Require(next.FindCard(before.SpawnerCardId)!.Temporary.Upgrades.Count(upgrade => upgrade.DataId == rule.Upgrade.DataId) ==
                    context.FindCard(before.SpawnerCardId)!.Temporary.Upgrades.Count(upgrade => upgrade.DataId == rule.Upgrade.DataId) + 1, "Death upgrade was not retained on its spawner card."); deaths++;
            }
            else
            {
                Require(id == Moon && owner.AssetKey == "SeleneandSilentBuff" && owner.UnitUpgradeTriggers.Count == 4 &&
                    rule.UpgradeLifetime == 1 && rule.Upgrade!.Stats.Damage == 20 && rule.Upgrade.Unique &&
                    before.Subtypes.Contains(rule.RequiredSubtype!), "Original moon subtype/lifetime/upgrade changed.");
                Require(after.BaseAttack - before.BaseAttack == (remove ? -20 : 20), "Native phase upgrade did not change attack by exactly 20.");
                Require(JsonSerializer.Serialize(context.CardInstances, ModelJson.Options) == JsonSerializer.Serialize(next.CardInstances, ModelJson.Options), "Unit-death-lifetime moon upgrade leaked to spawner cards.");
                phases.Add(trigger); if (remove) removes++; else adds++;
            }
        }
        var tests = root.GetProperty("RelicPhaseUpgradeTests").EnumerateArray().ToArray();
        Require(tests.Any(row => row.GetProperty("Team").GetString() == "Heroes" && !row.GetProperty("Returned").GetBoolean()) &&
            tests.Any(row => row.GetProperty("Owner").GetString() == Moon && row.GetProperty("Trigger").GetString() == "OnMoonLit" &&
                row.GetProperty("Team").GetString() == "Monsters" && row.GetProperty("Subtypes").GetArrayLength() == 0 && !row.GetProperty("Returned").GetBoolean()),
            "Missing actual enemy-team/ordinary-subtype controls.");
        Require(deaths > 0 && adds > 0 && removes > 0 && phases.SetEquals(["OnMoonLit", "OnMoonlitLost", "OnMoonShade", "OnMoonshadeLost"]) && originalOwners.Count == 2,
            "Missing native death or complete phase add/remove evidence.");
        int deathAblations = 0, moonAblations = 0;
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var state = row.GetProperty("Before").Deserialize<BattleTurnState>()!; var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (state.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId != Sacrifice) continue;
            var result = BattleActionModel.PlayCard(Remove(state, Death), action);
            Require(result.Supported, "Death-owner ablation unsupported: " + result.Reason);
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int cardId = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).SpawnerCardId;
            Require(actual.Spawn.Train.Context!.FindCard(cardId)!.Temporary.Upgrades.Count > result.State!.Spawn.Train.Context!.FindCard(cardId)!.Temporary.Upgrades.Count,
                "Removing death owner retained its future permanent-for-battle upgrade."); deathAblations++;
        }
        foreach (var row in root.GetProperty("Turns").EnumerateArray())
        {
            var state = row.GetProperty("Before").Deserialize<BattleTurnState>()!; var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var result = BattleTurnModel.EndTurn(Remove(state, Moon)); Require(result.Supported, "Moon-owner ablation unsupported: " + result.UnsupportedReason);
            if (actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(other => other.Id == unit.Id && other.BaseAttack != unit.BaseAttack))) moonAblations++;
        }
        Require(deathAblations > 0 && moonAblations > 0, "Missing positive owner ablations.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-UNIT-PHASE-UPGRADES-NATIVE PASS: two unchanged owners, {deaths} death upgrades, {adds}/{removes} moon additions/removals across all four callbacks, {tests.Length} Test filters, {deathAblations}/{moonAblations} owner ablations and complete independent policy replay.");
    }
    private static BattleTurnState Remove(BattleTurnState source, string id)
    {
        var old = source.Spawn; var context = old.Train.Context!.WithRelics(old.Train.Context.Relics!.Where(owner => owner.DataId != id).ToArray()); var train = CardSpellModel.WithContext(old.Train, context);
        var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng, old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled,
            old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
        return new(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints, source.DragonsHoard, source.MoonPhase, source.RngStreams, source.OtherPiles,
            source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences, source.SelectedRoom, source.MoonPhaseTrackingActive);
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
