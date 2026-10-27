using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ParallelHorizonChecks
{
    internal static void Run(FixtureValue shallowFixture, FixtureValue route)
    {
        Require(shallowFixture.GetProperty("EvidenceKind").GetString() == "ModelRootPrediction" &&
            shallowFixture.GetProperty("NativeValidationRequired").GetBoolean() && route.GetProperty("EvidenceKind").GetString() == "ModelPrediction" &&
            shallowFixture.GetProperty("SourceSHA256").GetString() == route.GetProperty("SourceSHA256").GetString(),
            "Horizon controls require explicitly model-only forecasts from one native source.");
        var root = shallowFixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        Require(parent == keys.Key(route.GetProperty("Root").Deserialize<BattleTurnState>()!), "Forecasts do not share full native root/RNG.");
        BattleSearchStep Step(FixtureValue row) => new(row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean());
        var shallowSteps = shallowFixture.GetProperty("Plan").GetProperty("Steps").EnumerateArray().Select(Step).ToArray();
        var rows = route.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var routeSteps = rows.Select(Step).ToArray();
        int boundary = Array.FindLastIndex(rows, row => row.GetProperty("IsEndTurn").GetBoolean() &&
            row.GetProperty("Outcome").GetInt32() == (int)RoomOutcome.Exchanged);
        var deepSteps = routeSteps.Take(boundary + 1).ToArray();
        var shallow = BattlePlannerChecks.Replay(root, shallowSteps, out RoomOutcome shallowOutcome);
        var deep = BattlePlannerChecks.Replay(root, deepSteps, out RoomOutcome deepOutcome);
        var won = BattlePlannerChecks.Replay(root, routeSteps, out RoomOutcome wonOutcome);
        Require(shallowOutcome == RoomOutcome.Exchanged && deepOutcome == RoomOutcome.Exchanged && wonOutcome == RoomOutcome.BattleWon &&
            keys.Key(shallow) == keys.Key(shallowFixture.GetProperty("Plan").GetProperty("State").Deserialize<BattleTurnState>()!) &&
            keys.Key(deep) == keys.Key(rows[boundary].GetProperty("Expected").Deserialize<BattleTurnState>()!) &&
            NativeBranchObservationChecks.Key(won, true) == NativeBranchObservationChecks.Key(route.GetProperty("Final").Deserialize<BattleTurnState>()!, true),
            "Horizon controls differ from their independently replayed full-state forecasts.");
        BattlePlanResult Plan(BattleTurnState state, RoomOutcome outcome, IReadOnlyList<BattleSearchStep> steps) =>
            new(state, outcome, steps, BattlePlanner.Evaluate(state, outcome), steps.Count(step => step.IsEndTurn),
                0, 0, 0, TimeSpan.Zero, true, false, false, false, outcome == RoomOutcome.BattleWon || outcome == RoomOutcome.PlayerDefeated,
                Array.Empty<UnsupportedActionAttempt>(), null, usedShorterHorizon: outcome == RoomOutcome.Exchanged, searchedToBattleEnd: true);
        var early = Plan(shallow, shallowOutcome, shallowSteps);
        var later = Plan(deep, deepOutcome, deepSteps);
        var victory = Plan(won, wonOutcome, routeSteps);
        Require(early.TurnsCompleted == 5 && later.TurnsCompleted == 7 && early.Score < later.Score,
            "Missing shallower five-turn/Pyre73 forecast versus deeper seven-turn/Pyre45 forecast.");
        foreach (var candidates in new[] { new[] { early, later }, new[] { later, early } })
            Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(candidates), later),
                "Whole-battle merge favored a shallow forecast before its future Pyre damage was visible.");
        foreach (var candidates in new[] { new[] { early, later, victory }, new[] { victory, later, early }, new[] { later, victory, early } })
            Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(candidates), victory), "Verified terminal victory lost to a partial forecast.");
        Require(keys.Key(root) == parent, "Horizon merge changed its shared native parent/RNG.");
        Console.WriteLine("PARALLEL-HORIZON PASS: same native-root five/seven-turn partials, Pyre73/45, terminal win/Pyre37, order independence and immutable full-state/RNG replay; model evidence only.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
