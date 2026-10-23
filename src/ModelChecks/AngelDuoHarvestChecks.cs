using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AngelDuoHarvestChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Kind").GetString() == "NativeDamageOriginalMinionForRemoteHarvest" &&
            fixture.GetProperty("Damage").GetInt32() == 10000 && fixture.GetProperty("NoHealthSetup").GetBoolean() &&
            fixture.GetProperty("AuthoredAssetsUnchanged").GetBoolean() && !fixture.GetProperty("NaturalVictory").GetBoolean(),
            "Missing the disclosed original-minion native-damage Harvest control.");
        var before = fixture.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = fixture.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int targetId = fixture.GetProperty("TargetId").GetInt32(), room = fixture.GetProperty("Room").GetInt32();
        CombatUnit target = before.Spawn.Train.Rooms.Single(floor => floor.RoomIndex == room).Units.Single(unit => unit.Id == targetId);
        CombatUnit outer = Unit(before, "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth");
        CombatUnit inner = Unit(before, "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack");
        CombatUnit nextOuter = Unit(actual, outer.AssetKey), nextInner = Unit(actual, inner.AssetKey);
        Require(target.Team == CombatTeam.Enemy && target.Health > 0 && target.IsBoss != true &&
            before.Spawn.Train.Rooms.Single(floor => floor.Units.Any(unit => unit.Id == outer.Id)).RoomIndex == room &&
            before.Spawn.Train.Rooms.Single(floor => floor.Units.Any(unit => unit.Id == inner.Id)).RoomIndex != room &&
            !actual.Spawn.Train.Rooms.SelectMany(floor => floor.Units).Any(unit => unit.Id == targetId && unit.Health > 0) &&
            nextInner.MaxHealth == inner.MaxHealth + 50 && nextInner.Health == inner.Health + 50 &&
            nextOuter.MaxHealth == outer.MaxHealth && nextOuter.Health == outer.Health &&
            nextInner.BaseAttack == inner.BaseAttack && nextOuter.BaseAttack == outer.BaseAttack,
            "Native remote Harvest must grant only its live Savage companion +50 current/max health after an original minion dies.");
        string immutable = Json(before), expected = Json(actual);
        Verify(); Parallel.For(0, 16, _ => Verify());
        Console.WriteLine("ANGEL-DUO-HARVEST PASS: original minion death, remote companion +50 current/max health, excluded outer/source stats, full native state/RNG and sixteen immutable damage/removal branches.");
        void Verify()
        {
            string predicted = Json(Apply(before, room, targetId));
            Require(predicted == expected, "Native Harvest differs: " + ModelJson.Difference(predicted, expected));
            Require(Json(before) == immutable, "Harvest damage branch mutated its root.");
        }
    }

    internal static void Battle(FixtureValue fixture)
    {
        AngelDuoVariantChecks.Input(fixture, "RevengeHarvest");
        FixtureValue transition = fixture.GetProperty("AngelDuoHarvestTransition");
        Native(transition);
        var control = transition.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = transition.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        var root = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        int room = transition.GetProperty("Room").GetInt32(), id = transition.GetProperty("TargetId").GetInt32();
        string immutable = Json(root);
        Chain(); Parallel.For(0, 16, _ => Chain());
        TrainCombatChecks.Native(fixture);
        Console.WriteLine($"ANGEL-DUO-HARVEST-BATTLE PASS: {turns.Length} native EndTurns, explicitly composed damage control, complete state/RNG and sixteen immutable full-battle branches.");
        void Chain()
        {
            BattleTurnState state = root;
            bool applied = false;
            for (int index = 0; index < turns.Length; index++)
            {
                if (!applied && state.Spawn.Turn == control.Spawn.Turn)
                {
                    Same(state, control, false, "before the explicit damage control");
                    state = Apply(state, room, id); applied = true;
                    Same(state, after, false, "after the explicit damage control");
                }
                BattleTurnResult result = BattleTurnModel.EndTurn(state);
                Require(result.Supported, "Controlled Harvest chain: " + result.UnsupportedReason);
                bool terminal = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
                Require(result.Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32() &&
                    terminal == (index == turns.Length - 1), "Controlled Harvest chain changed terminal outcome/boundary.");
                state = result.State!;
                Same(state, turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal, "EndTurn " + index);
            }
            Require(applied && Json(root) == immutable, "Harvest full chain skipped its control or mutated its shared root.");
        }
    }

    private static BattleTurnState Apply(BattleTurnState before, int room, int target)
    {
        var hit = CardSpellModel.ApplyNativeDamageStep(before.Spawn.Train, room, target, 10000, 0,
            Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
        Require(hit.Supported, "Harvest native damage: " + hit.UnsupportedReason);
        TrainCombatResult removed = TrainCombatModel.ApplyCharacterQueue(hit.State!, hit.PendingCallbacks.ToList());
        Require(removed.Supported, "Harvest native removal: " + removed.UnsupportedReason);
        TrainCombatState train = TrainCombatModel.ProcessRemovals(removed.State!);
        EnemySpawnState old = before.Spawn;
        var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
            old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
            old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences,
            old.PendingDestroyedUnitIds, old.OuterBoss, old.QueuedOuterBoss, old.QueuedTrueFinalBoss, old.TrueFinalBoss);
        return new BattleTurnState(spawn, before.Energy, before.EnergyPerTurn, before.DrawPerTurn, before.ForgePoints,
            before.DragonsHoard, before.MoonPhase, before.RngStreams.Select(stream => new BattleRngStream(stream.Name,
                stream.Seed, stream.Name == "Battle" ? train.Context!.BattleRng : stream.State)).ToArray(),
            before.OtherPiles, before.ExternalInteractions, before.PlayRules, before.BattlePreviewEnabled, before.UiRngIsolated,
            before.CanonicalDecisionReferences, before.CanonicalPhysicalReferences,
            train.Context!.QueryFrame?.SelectedRoomIndex ?? before.SelectedRoom, before.MoonPhaseTrackingActive);
    }

    private static CombatUnit Unit(BattleTurnState state, string asset) =>
        state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == asset && unit.Health > 0);
    private static void Same(BattleTurnState predicted, BattleTurnState actual, bool terminal, string context)
    {
        string a = BattleTurnChecks.Comparable(predicted, terminal), b = BattleTurnChecks.Comparable(actual, terminal);
        Require(a == b, "Harvest chain differs " + context + ": " + ModelJson.Difference(a, b));
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
