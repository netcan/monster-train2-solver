using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DeferredHarvestTowerChecks
{
    private const string Reason = "Native rolling action successor mismatch observation.";

    internal static void Four(FixtureValue original, FixtureValue originalReplay, FixtureValue repaired, FixtureValue repairedReplay)
    {
        Native(original, repaired);
        Native(originalReplay, repairedReplay);
        NativeBranchObservationChecks.Pair(original, repairedReplay, originalPredictionPresent: true);
        var boundary = original.GetProperty("Rows").EnumerateArray().Last();
        var before = boundary.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = boundary.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var native = boundary.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        string parent = System.Text.Json.JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnState predicted = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, false)], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(predicted) == NativeBranchObservationChecks.Key(native),
                "Concurrent boundary replay changed its native successor.");
        });
        Require(parent == System.Text.Json.JsonSerializer.Serialize(before, ModelJson.Options), "Concurrent replay mutated its parent.");
        Console.WriteLine("DEFERRED-HARVEST-TOWER FOUR PASS: two original and two repaired independent native runs; four concurrent replays.");
    }

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, HistoricalMismatch);
        Require(rows.Length == 36, "Wrong native deferred Harvest prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var dying = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 20);
        var boss = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 5);
        var healed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 5);
        Require(before.Spawn.Turn == 7 && action.CardInstanceId == 13 && action.RoomIndex == 2 &&
            before.Spawn.Train.Context!.FindCard(13)!.DataId == "27e9ee82-fc42-4aa5-b845-90d7e31b61d9" &&
            dying.AssetKey == "ClericT2_DiscipleofRuin" && dying.Health == 4 &&
            boss.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack" &&
            boss.Health == 881 && boss.MaxHealth == 1250 && healed.Health == 931 && healed.MaxHealth == 1300 &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 20),
            "Missing native delayed death and cross-floor Savage +50 health.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason, HistoricalMismatch);
            NativeBranchObservationChecks.Pair(source, second, originalPredictionPresent: true);
        }
        Console.WriteLine("DEFERRED-HARVEST-TOWER PASS: 36 native inputs/full state/RNG and immutable parents; independentPair=" + replay.HasValue);
    }

    private static void HistoricalMismatch(BattleTurnState expected, BattleTurnState actual)
    {
        CombatUnit predicted = expected.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 5);
        CombatUnit observed = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 5);
        Require(predicted.Health == 881 && predicted.MaxHealth == 1250 && observed.Health == 931 && observed.MaxHealth == 1300,
            "Historical forecast did not preserve the cross-floor mismatch.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
