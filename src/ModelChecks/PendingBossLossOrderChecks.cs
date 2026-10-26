using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PendingBossLossOrderChecks
{
    internal static void Run(FixtureValue early, FixtureValue late, FixtureValue prediction)
    {
        using var keys = new BattleStateKeys();
        var earlyRows = early.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var lateRows = late.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        BattleTurnState root = earlyRows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        string parent = keys.Key(root);
        foreach (var native in new[] { early, late })
        {
            Require(!native.GetProperty("NativeWon").GetBoolean(), "A defeat witness became a native victory.");
            foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                Require(native.GetProperty(counter).GetInt32() == 0, "Unaccepted native loss: " + counter);
        }
        Require(prediction.GetProperty("EvidenceKind").GetString() == "ModelPrediction",
            "The1003HP counterexample must remain explicitly model-only.");
        Require(keys.Key(lateRows[0].GetProperty("Root").Deserialize<BattleTurnState>()!) == parent &&
            keys.Key(prediction.GetProperty("Root").Deserialize<BattleTurnState>()!) == parent,
            "Loss witnesses must share their complete native deployment root/RNG.");

        BattleSearchStep[] Steps(FixtureValue fixture) => fixture.GetProperty("RollingDecisions").EnumerateArray()
            .Select(row => new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        BattleTurnState Replay(FixtureValue fixture, BattleTurnState expected)
        {
            var result = BattlePlannerChecks.Replay(root, Steps(fixture), out var outcome);
            // Native terminal UI invalidates selection/play eligibility; compare
            // with the same terminal normalization as the native replay gates.
            Require(outcome == RoomOutcome.PlayerDefeated &&
                BattleTurnChecks.Comparable(result, terminal: true) == BattleTurnChecks.Comparable(expected, terminal: true),
                "Loss forecast differs from independent complete-state/RNG replay.");
            return result;
        }
        var beforeBoss = Replay(early, earlyRows.Last().GetProperty("Actual").Deserialize<BattleTurnState>()!);
        var damagedBoss = Replay(late, lateRows.Last().GetProperty("Actual").Deserialize<BattleTurnState>()!);
        var reachedBoss = Replay(prediction, prediction.GetProperty("Final").Deserialize<BattleTurnState>()!);
        Require(beforeBoss.Spawn.Phase == 6 && beforeBoss.Spawn.Waves.Count == 7 &&
            !Bosses(beforeBoss).Any() && Bosses(damagedBoss).Single().Health == 372 &&
            Bosses(reachedBoss).Single().Health == 1003 && Bosses(reachedBoss).Single().MaxHealth == 1100,
            "Missing the unspawned boss and independently replayable372/1003HP witnesses.");
        double Cost(BattleTurnState state) => BattlePlanner.PositionCost(state, RoomOutcome.PlayerDefeated);
        Require(BattlePlanner.PositionCost(beforeBoss) < BattlePlanner.PositionCost(reachedBoss) &&
            Cost(reachedBoss) < Cost(beforeBoss) && Cost(damagedBoss) < Cost(reachedBoss),
            "Equal-loss ranking still treats the pending boss as already defeated.");
        Require(Math.Abs(Cost(beforeBoss) - BattlePlanner.PositionCost(beforeBoss) - 1e6 - 1100) < .001 &&
            Math.Abs(Cost(damagedBoss) - BattlePlanner.PositionCost(damagedBoss) - 372d / 1100 * 1e6) < .001,
            "Pending/live boss progress was omitted or counted twice.");
        foreach (var state in new[] { beforeBoss, reachedBoss, damagedBoss })
        {
            Require(BattlePlanner.Evaluate(state, RoomOutcome.PlayerDefeated) == 1e12 &&
                BattlePlanner.PositionCost(state, RoomOutcome.BattleWon) == BattlePlanner.PositionCost(state) &&
                BattlePlanner.PositionCost(state, RoomOutcome.Stalemate) == BattlePlanner.PositionCost(state),
                "Loss ordering changed the public objective or another outcome's positional tie.");
        }
        BattlePlanResult Plan(BattleTurnState state, BattleSearchStep[] steps, double score) =>
            new(state, RoomOutcome.PlayerDefeated, steps, score, steps.Count(step => step.IsEndTurn),
                0, 0, 0, TimeSpan.Zero, false, false, false, false, true,
                Array.Empty<UnsupportedActionAttempt>(), null, searchedToBattleEnd: true);
        var earlyPlan = Plan(beforeBoss, Steps(early), 1e12);
        var progress = Plan(reachedBoss, Steps(prediction), 1e12);
        var best = Plan(damagedBoss, Steps(late), 1e12);
        foreach (var plans in new[] { new[] { earlyPlan, progress }, new[] { progress, earlyPlan } })
            Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(plans), progress),
                "Worker merge favored defeat before the boss over reaching it.");
        Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(new[] { earlyPlan, best, progress }), best),
            "Worker merge discarded the strongest complete native loss.");
        var primary = Plan(beforeBoss, Steps(early), progress.Score - 1);
        Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(new[] { primary, best }), primary) &&
            keys.Key(root) == parent, "Positional loss order overrode the primary score or mutated the root.");
        Console.WriteLine("PENDING-BOSS-LOSS-ORDER PASS: native early/372HP losses and explicit model-only1003HP counterexample; " +
            "complete-state/RNG replay, pending/live boss progress, worker order independence, unchanged primary score and parent.");
    }

    private static IEnumerable<CombatUnit> Bosses(BattleTurnState state) => state.Spawn.Train.Rooms
        .SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy && unit.IsBoss == true && unit.Health > 0);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
