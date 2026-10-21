using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RollingAdviceChecks
{
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
    private static BattleTurnState State(FixtureValue value) => value.Deserialize<BattleTurnState>()!;
    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
    private static string Key(BattleTurnState state, bool terminal = false) => BattleTurnChecks.Comparable(state, terminal);
    private static void Accepted(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Rolling native capture has nonzero " + counter);
        Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean(), "Rolling battle did not settle.");
        Require(string.IsNullOrEmpty(fixture.GetProperty("ModifierScenario").GetString()), "Rolling encounter was modified.");
    }
    internal static void Native(FixtureValue fixture)
    {
        Accepted(fixture);
        FixtureValue[] records = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(records.Length > 1, "Missing complete rolling decision sequence.");
        BattleTurnState? previous = null;
        long generation = -1;
        int cards = 0, turns = 0;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        foreach (FixtureValue record in records)
        {
            BattleTurnState root = State(record.GetProperty("Root"));
            if (previous != null) Require(Key(previous) == Key(root), "Rolling decisions do not form one native chain.");
            else Require(root.Spawn.Turn == 0, "Rolling advice did not start at the deployment turn.");
            Require(record.GetProperty("Difference").ValueKind == FixtureKind.Null, "Rolling native mismatch.");
            bool end = record.GetProperty("IsEndTurn").GetBoolean();
            Require(end == (record.GetProperty("Action").ValueKind == FixtureKind.Null), "Invalid rolling input.");
            PlayCardAction? action = end ? null : record.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState next = BattlePlannerChecks.Replay(root, new[] { new BattleSearchStep(action, end) }, out outcome);
            Require(outcome == record.GetProperty("Outcome").Deserialize<RoomOutcome>() &&
                Key(next, Terminal(outcome)) == Key(State(record.GetProperty("Expected")), Terminal(outcome)) &&
                Key(next, Terminal(outcome)) == Key(State(record.GetProperty("Actual")), Terminal(outcome)), "Rolling native successor differs.");
            if (end) turns++; else cards++;

            FixtureValue[] plan = record.GetProperty("PlanSteps").EnumerateArray().ToArray();
            Require(plan.Length > 0 && plan[0].GetProperty("IsEndTurn").GetBoolean() == end &&
                plan[0].GetProperty("Action").ContentEquals(record.GetProperty("Action")), "Driver did not execute the latest plan's first step.");
            BattleSearchStep[] steps = plan.Select(step => new BattleSearchStep(
                step.GetProperty("Action").ValueKind == FixtureKind.Null ? null : step.GetProperty("Action").Deserialize<PlayCardAction>(),
                step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
            BattleTurnState planned = BattlePlannerChecks.Replay(root, steps, out RoomOutcome plannedOutcome);
            double score = record.GetProperty("Score").GetDouble();
            Require(plannedOutcome == record.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() &&
                Key(planned, Terminal(plannedOutcome)) == Key(State(record.GetProperty("Planned")), Terminal(plannedOutcome)) &&
                BattlePlanner.Evaluate(planned, plannedOutcome) == score, "Published continuation does not reproduce its forecast.");
            if (record.GetProperty("PolicyBaselineScore").ValueKind == FixtureKind.Number)
                Require(score <= record.GetProperty("PolicyBaselineScore").GetDouble(), "Rolling advice lost its completed policy seed.");
            if (record.GetProperty("Mode").GetString() == "rolling")
            {
                long currentGeneration = record.GetProperty("Generation").GetInt64();
                Require(currentGeneration > generation, "Advice was reused after a native action."); generation = currentGeneration;
                FixtureValue timing = record.GetProperty("Timing");
                Require(timing.GetProperty("Generation").GetInt64() == generation, "Advice timing belongs to another publication.");
                double sum = 0;
                foreach (string field in new[] { "CaptureMs", "QueueMs", "SearchMs", "VerificationMs" })
                { double duration = timing.GetProperty(field).GetDouble(); Require(double.IsFinite(duration) && duration >= 0, "Invalid advice latency."); sum += duration; }
                Require(timing.GetProperty("EndToEndMs").GetDouble() + 1 >= sum, "End-to-end latency omitted a measured stage.");
            }
            else Require(record.GetProperty("Mode").GetString() == "replay" && record.GetProperty("Nodes").GetInt32() == 0,
                "Frozen rolling replay searched again.");
            previous = State(record.GetProperty("Actual"));
        }
        Require(Terminal(outcome) && fixture.GetProperty("NativeWon").GetBoolean() == (outcome == RoomOutcome.BattleWon),
            "Rolling sequence did not cover the whole native battle.");
        Require(cards == fixture.GetProperty("Actions").GetArrayLength() && turns == fixture.GetProperty("Turns").GetArrayLength(),
            "Some native inputs bypassed rolling advice.");
        Console.WriteLine($"ROLLING-ADVICE PASS mode={records[0].GetProperty("Mode").GetString()} decisions={records.Length} cards={cards} turns={turns} outcome={outcome}");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay, FixtureValue baseline)
    {
        Native(source); Native(replay); Accepted(baseline);
        FixtureValue[] first = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        FixtureValue[] second = replay.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(first.Length == second.Length, "Fresh rolling replay has a different decision count.");
        for (int index = 0; index < first.Length; index++)
        {
            Require(first[index].GetProperty("Mode").GetString() == "rolling" && second[index].GetProperty("Mode").GetString() == "replay" &&
                first[index].GetProperty("Action").ContentEquals(second[index].GetProperty("Action")) &&
                first[index].GetProperty("IsEndTurn").ContentEquals(second[index].GetProperty("IsEndTurn")), "Fresh rolling replay changed an input.");
            bool terminal = index == first.Length - 1;
            foreach (string field in new[] { "Root", "Expected", "Actual" })
                Require(Key(State(first[index].GetProperty(field)), terminal) == Key(State(second[index].GetProperty(field)), terminal),
                    "Fresh rolling replay changed full state or RNG at " + field);
        }
        BattleAdviceChecks.Native(source);
        Require(baseline.GetProperty("Policy").GetString() == "units-spells-and-junk", "Wrong fixed-policy baseline.");
        Require(source.GetProperty("BattleDistance").GetInt32() == baseline.GetProperty("BattleDistance").GetInt32(), "Baseline encounter distance differs.");
        FixtureValue[] baselineActions = baseline.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] baselineTurns = baseline.GetProperty("Turns").EnumerateArray().ToArray();
        BattleTurnState baselineRoot = State((baselineActions.Length > 0 ? baselineActions[0] : baselineTurns[0]).GetProperty("Before"));
        Require(Key(State(first[0].GetProperty("Root"))) == Key(baselineRoot), "Fixed-policy baseline starts from a different battle state.");
        FixtureValue baselineTerminal = baselineTurns.Last().GetProperty("Actual");
        RoomOutcome baselineOutcome = baseline.GetProperty("NativeWon").GetBoolean() ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated;
        double baselineScore = BattlePlanner.Evaluate(State(baselineTerminal), baselineOutcome);
        RoomOutcome actualOutcome = first.Last().GetProperty("Outcome").Deserialize<RoomOutcome>();
        double actualScore = BattlePlanner.Evaluate(State(first.Last().GetProperty("Actual")), actualOutcome);
        Require(actualScore <= baselineScore, "Rolling advisor lost to the completed fixed policy.");
        Console.WriteLine($"ROLLING-ADVICE-PAIR PASS: all {first.Length} decisions reproduced independently; terminal score={actualScore}, completed native policy={baselineScore}.");
    }
}
