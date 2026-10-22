using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossLethalChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static BattleTurnState Prepare(BattleTurnState state)
    {
        TrainCombatState train = state.Spawn.Train;
        string[] assets = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2" };
        foreach (string asset in assets)
        {
            CombatUnit actor = train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == asset);
            LinkedBossHealthResult changed = LinkedBossHealthModel.Apply(train, actor.Id, "DebuffHP", actor.Health - 1);
            Require(changed.Supported, "Independent original owner HP control failed: " + changed.UnsupportedReason);
            train = changed.State!;
        }
        EnemySpawnState old = state.Spawn;
        var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng, old.NextUnitId,
            old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn,
            old.ExternalInteractions, old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds, old.OuterBoss,
            old.QueuedOuterBoss, old.QueuedTrueFinalBoss, old.TrueFinalBoss);
        return new BattleTurnState(spawn, state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints,
            state.DragonsHoard, state.MoonPhase, state.RngStreams, state.OtherPiles, state.ExternalInteractions, state.PlayRules,
            state.BattlePreviewEnabled, state.UiRngIsolated, state.CanonicalDecisionReferences, state.CanonicalPhysicalReferences,
            state.SelectedRoom, state.MoonPhaseTrackingActive);
    }
    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("Policy").GetString() == "no-cards" && root.GetProperty("BattleDistance").GetInt32() == 8 &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("Actions").GetArrayLength() == 0, "Missing original isolated TFB route and terminal capture provenance.");
        FixtureValue control = root.GetProperty("TrueFinalBossLethalControl");
        BattleTurnState before = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState controlled = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Json(Prepare(before)) == Json(controlled) && control.GetProperty("Debuffs").Deserialize<int[]>()!.SequenceEqual(new[] { 1554, 2749, 3499 }),
            "Native explicit owner health preparation changed more than the three recorded debuffs.");
        string[] owners = { "Boss_Level9_Dominion_V2", "Boss_Level9_Savagery_V2", "Boss_Level9_Entropy_Bot_V2" };
        Require(control.GetProperty("OwnerIds").Deserialize<int[]>()!.SequenceEqual(owners.Select(asset =>
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == asset).Id)) &&
            controlled.Spawn.Train.Context!.LifeLinks!.Actors.All(actor => actor.Health == 1) &&
            before.Spawn.Looping == controlled.Spawn.Looping && before.Spawn.Phase == controlled.Spawn.Phase &&
            controlled.Spawn.OuterBoss?.Phase == "Casting", "Owner IDs, five one-HP bodies or unchanged native phase/waves differ.");
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 2 && root.GetProperty("NativeWon").GetBoolean(), "Expected deployment and one normal lethal combat turn.");
        foreach (FixtureValue row in turns)
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(row.GetProperty("Before").Deserialize<BattleTurnState>()!);
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool terminal = result.Outcome == RoomOutcome.BattleWon;
            Require(result.Supported && result.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(result.State!, terminal, true) == BattleTurnChecks.Comparable(actual, terminal, true),
                "Native original lethal EndTurn " + row.GetProperty("Index").GetInt32() + " differs: " +
                (result.Supported ? ModelJson.Difference(BattleTurnChecks.Comparable(result.State!, terminal, true),
                    BattleTurnChecks.Comparable(actual, terminal, true)) : result.UnsupportedReason));
        }
        FixtureValue[] attacks = root.GetProperty("UnitTurns").EnumerateArray().ToArray();
        Require(attacks.Length == 3 && attacks[2].GetProperty("AttackedTargetIds").Deserialize<int[]>()!
            .SequenceEqual(new[] { control.GetProperty("OwnerIds")[0].GetInt32() }), "Missing the ordinary lethal Pyre attack on Dominion.");
        foreach (FixtureValue row in attacks)
        {
            RoomCombatState input = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            RoomCombatResult result = RoomCombatModel.ApplyUnitTurn(input, row.GetProperty("ActorId").GetInt32());
            Require(result.Supported && Json(result.State!) == Json(actual), "Native lethal unit turn differs: " +
                (result.Supported ? ModelJson.Difference(Json(result.State!), Json(actual)) : result.UnsupportedReason));
        }
        BattleTurnState deployment = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = Json(deployment);
        string Replay()
        {
            BattleTurnResult born = BattleTurnModel.EndTurn(deployment);
            Require(born.Supported, "Independent original birth failed.");
            BattleTurnResult won = BattleTurnModel.EndTurn(Prepare(born.State!));
            Require(won.Supported && won.Outcome == RoomOutcome.BattleWon, "Independent lethal combat failed: " + won.UnsupportedReason);
            return BattleTurnChecks.Comparable(won.State!, true, true);
        }
        string expected = BattleTurnChecks.Comparable(turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!, true, true);
        BattleTurnState terminalState = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(terminalState.Spawn.Train.Rooms.SelectMany(room => room.Units).Single().IsPyre &&
            terminalState.Spawn.Train.Rooms.SelectMany(room => room.Units).Single().Health == 77 && terminalState.Spawn.OuterBoss == null &&
            terminalState.Spawn.Train.Context!.LifeLinks == null && terminalState.Spawn.Train.Context.KillCamActivated == true &&
            terminalState.Spawn.TrueFinalBoss is { HellforgedBoss: true, WaitedOnRelentless: false, QueuedBossCount: 0 },
            "Native lethal combat must retain only Pyre77 with casting flags and complete group cleanup.");
        Require(Replay() == expected, "Independent lethal chain differs.");
        Parallel.For(0, 16, _ => Require(Replay() == expected, "Concurrent independent lethal chain differs."));
        Require(Json(deployment) == parent, "Lethal chain mutated its deployment root.");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Native lethal route must finish with all four failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-LETHAL PASS: explicit three-owner HP control; normal Pyre attack, five-body removal and full victory state/RNG; two EndTurns and 16 independent immutable chains.");
    }
}
