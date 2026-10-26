using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HandHighlightRngChecks
{
    private const string Reason = "Native final-battle preview RNG update after EndTurn.";
    internal const string EffectReason = "Native final-battle random-target card legality test RNG.";
    private static string Key(BattleTurnState state) => NativeBranchObservationChecks.Key(state);
    private static UnityRng Test(BattleTurnState state) => state.Spawn.Train.Context!.Enchantments!.TestRng;
    private static UnityRng Advance(UnityRng value, int count)
    { for (int index = 0; index < count; index++) value = value.Next(); return value; }

    internal static void Counterexample(string sourcePath, string observedPath)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        using var observed = ModelJson.ReadFixture(observedPath);
        FixtureValue fixture = source.RootElement, observation = observed.RootElement;
        Require(fixture.GetProperty("NativeWon").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(name => fixture.GetProperty(name).GetInt32() == 0),
            "Preserve the original incomplete hand-highlight counterexample.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            observation.GetProperty("NativeCompleted").GetBoolean() &&
            observation.GetProperty("Request").GetProperty("Reason").GetString() == Reason &&
            observation.GetProperty("Request").GetProperty("SourceSHA256").GetString() ==
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))).ToLowerInvariant(),
            "The passive observation must bind the exact native failing prefix.");
        var native = observation.GetProperty("Rows").EnumerateArray().ToArray();
        Require(native.Length == rows.Length, "Incomplete passive native prefix.");
        for (int index = 0; index < rows.Length; index++)
        {
            BattleTurnState before = rows[index].GetProperty("Root").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(Key(before) == Key(native[index].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                Key(actual) == Key(native[index].GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "The independent unprotected native prefix changed at " + index);
            BattleTurnState predicted = BattlePlannerChecks.Replay(before,
                [new BattleSearchStep(rows[index].GetProperty("Action").Deserialize<PlayCardAction>(),
                    rows[index].GetProperty("IsEndTurn").GetBoolean())], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged &&
                Key(predicted) == Key(rows[index].GetProperty("Expected").Deserialize<BattleTurnState>()!),
                "Original model forecast changed at " + index);
            if (index < rows.Length - 1) Require(Key(predicted) == Key(actual), "Unrelated prefix mismatch.");
            else
            {
                Require(before.Spawn.Turn == 2 && actual.Spawn.Turn == 3 &&
                    Test(predicted).Equals(Advance(Test(before), 2)) && Test(actual).Equals(Advance(Test(before), 42)),
                    "Missing the two preview draws plus forty UI target draws.");
                JsonNode repaired = JsonNode.Parse(Key(predicted))!;
                repaired["Context"]!["Enchantments"]!["TestRng"] = JsonSerializer.SerializeToNode(Test(actual));
                Require(JsonNode.DeepEquals(repaired, JsonNode.Parse(Key(actual))),
                    "The original discrepancy includes more than the UI test RNG.");
            }
        }
        var draws = observation.GetProperty("BattleTestRng").EnumerateArray().ToArray();
        var ui = draws.Where(IsHighlight).ToArray();
        Require(ui.Length == 40 && ui.All(row => !row.GetProperty("Preview").GetBoolean() &&
            row.GetProperty("Min").GetInt32() == 0 && row.GetProperty("Max").GetInt32() == 4),
            "Missing passive CardUI/HandUI random-target call stacks.");
        Console.WriteLine("HAND-HIGHLIGHT-RNG-COUNTEREXAMPLE PASS:21 independent native inputs;40 UI target draws,2 battle-preview draws; only test RNG differs; original unprotected behavior retained.");
    }

    internal static FixtureValue[] Native(FixtureValue fixture)
    {
        bool effect = fixture.GetProperty("Request").GetProperty("Reason").GetString() == EffectReason;
        var rows = NativeBranchObservationChecks.Replay(fixture, effect ? EffectReason : Reason);
        Require(rows.Length == (effect ? 25 : 21), "Changed protected native prefix.");
        FixtureValue guard = fixture.GetProperty("PlayerAdvisor").GetProperty("UiTestRngGuard");
        Require(guard.GetProperty("Stream").GetString() == "BattleTest" && guard.GetProperty("Calls").GetInt64() > 0 &&
            guard.GetProperty("RestoredDraws").GetInt64() > 0 && guard.GetProperty("Exceptions").GetInt64() == 0 &&
            guard.GetProperty("PreservesLiveBattleRng").GetBoolean(), "Player UI test-stream guard was not exercised.");
        if (effect) Require(guard.GetProperty("QueryBoundary").GetString() ==
            "CombatManager.TestEffects+GameEffectHelper.TestEffect", "Missing both effect query boundaries.");
        Require(fixture.GetProperty("BattleTestRng").EnumerateArray().Any(IsHighlight),
            "The protected process did not run original random-target highlight queries.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Test(actual).Equals(Advance(Test(before), effect ? 0 : 2)), "Queries still advance the preview stream.");
        if (effect) Require(fixture.GetProperty("BattleTestRng").EnumerateArray().Any(IsPlayValidation) &&
            fixture.GetProperty("BattleTestRng").EnumerateArray().Any(IsExecutionValidation),
            "Both original effect legality paths must still execute.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            BattleTurnState result = BattlePlannerChecks.Replay(before,
                [new BattleSearchStep(rows[^1].GetProperty("Action").Deserialize<PlayCardAction>(),
                    rows[^1].GetProperty("IsEndTurn").GetBoolean())], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && Key(result) == Key(actual),
                "Protected preview differs in a parallel model branch.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Preview replay mutated its parent.");
        Console.WriteLine($"EFFECT-QUERY-RNG-NATIVE PASS:{rows.Length} inputs/full state/forecast/RNG; original queries retained; test stream restored;8 immutable branches.");
        return rows;
    }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        var first = Native(source);
        var second = Native(replay);
        Require(source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Changed protected native request.");
        for (int index = 0; index < first.Length; index++)
            foreach (string field in new[] { "Before", "Actual" })
                Require(Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!), "Independent protected state changed.");
        Console.WriteLine($"EFFECT-QUERY-RNG-PAIR PASS:{first.Length} inputs in independent player processes; native queries retained, test stream restored; full state/forecast/live Battle RNG match;8 immutable branches; no terminal victory claimed.");
    }

    internal static void EffectCounterexample(string sourcePath, string observedPath)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        using var observed = ModelJson.ReadFixture(observedPath);
        FixtureValue fixture = source.RootElement, observation = observed.RootElement;
        Require(fixture.GetProperty("NativeWon").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(name => fixture.GetProperty(name).GetInt32() == 0),
            "Preserve the original incomplete effect legality counterexample.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var native = observation.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 25 && native.Length == 25 && !rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            observation.GetProperty("NativeCompleted").GetBoolean() &&
            observation.GetProperty("Request").GetProperty("Reason").GetString() == EffectReason &&
            observation.GetProperty("Request").GetProperty("SourceSHA256").GetString() ==
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))).ToLowerInvariant(),
            "The passive observation must bind the exact 25-input native prefix.");
        for (int index = 0; index < rows.Length; index++)
        {
            BattleTurnState before = rows[index].GetProperty("Root").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(Key(before) == Key(native[index].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                Key(actual) == Key(native[index].GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "The independently observed legality prefix changed at " + index);
            BattleTurnState predicted = BattlePlannerChecks.Replay(before,
                [new BattleSearchStep(rows[index].GetProperty("Action").Deserialize<PlayCardAction>(),
                    rows[index].GetProperty("IsEndTurn").GetBoolean())], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged &&
                Key(predicted) == Key(rows[index].GetProperty("Expected").Deserialize<BattleTurnState>()!),
                "Original effect forecast changed at " + index);
            if (index < 24) Require(Key(predicted) == Key(actual), "Unrelated native prefix mismatch.");
            else
            {
                Require(rows[index].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 27 &&
                    Test(predicted).Equals(Test(before)) && Test(actual).Equals(Advance(Test(before), 10)),
                    "Expected Icestorm and ten native legality-test draws.");
                JsonNode repaired = JsonNode.Parse(Key(predicted))!;
                repaired["Context"]!["Enchantments"]!["TestRng"] = JsonSerializer.SerializeToNode(Test(actual));
                Require(JsonNode.DeepEquals(repaired, JsonNode.Parse(Key(actual))),
                    "The original legality discrepancy includes more than test RNG.");
            }
        }
        var draws = observation.GetProperty("BattleTestRng").EnumerateArray().ToArray();
        Require(draws.Count(IsPlayValidation) == 5 && draws.Count(IsExecutionValidation) == 5,
            "Missing native pre-play and pre-effect validation call stacks.");
        Console.WriteLine("EFFECT-QUERY-RNG-COUNTEREXAMPLE PASS:25 independent native inputs;5 play validation+5 execution validation draws; only test RNG differs; original behavior retained.");
    }

    private static bool IsPlayValidation(FixtureValue row) => row.GetProperty("Stack").GetString()!
        .Contains("CardManager::PlayAnyCard", StringComparison.Ordinal);
    private static bool IsExecutionValidation(FixtureValue row) => row.GetProperty("Stack").GetString()!
        .Contains("CombatManager+<FireCardEffectInternal>", StringComparison.Ordinal);

    private static bool IsHighlight(FixtureValue row)
    {
        string stack = row.GetProperty("Stack").GetString()!;
        return stack.Contains("CardUI::IsPlayableAndAffectsState", StringComparison.Ordinal) ||
            stack.Contains("CardUI.IsPlayableAndAffectsState", StringComparison.Ordinal);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
