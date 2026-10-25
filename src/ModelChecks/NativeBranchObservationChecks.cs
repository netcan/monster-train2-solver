using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NativeBranchObservationChecks
{
    internal static string Key(BattleTurnState state, bool terminal = false) => BattleTurnChecks.Comparable(state, terminal);

    internal static FixtureValue[] Replay(FixtureValue fixture, string reason,
        Action<BattleTurnState, BattleTurnState>? historicalBoundaryMismatch = null,
        RoomOutcome boundaryOutcome = RoomOutcome.Exchanged)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            fixture.GetProperty("NativeCompleted").GetBoolean() &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Missing original native observation.");
        FixtureValue request = fixture.GetProperty("Request");
        Require(request.GetProperty("Reason").GetString() == reason, "Wrong native rejection boundary.");
        FixtureValue[] prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        FixtureValue[] rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == prefix.Length + 1 && rows.Length is > 1 and <= 257, "Incomplete native prefix.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(current.Spawn.Turn == 0, "Observation must begin at deployment.");
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            bool boundary = index == prefix.Length;
            FixtureValue step = boundary ? request : prefix[index];
            bool end = step.GetProperty("IsEndTurn").GetBoolean();
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(Key(current) == Key(before) && row.GetProperty("Boundary").GetBoolean() == boundary &&
                row.GetProperty("IsEndTurn").GetBoolean() == end && row.GetProperty("Action").ContentEquals(step.GetProperty("Action")),
                "Native chain/input differs at " + index);
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            PlayCardAction? action = step.GetProperty("Action").Deserialize<PlayCardAction>();
            BattleTurnState predicted = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out RoomOutcome outcome);
            bool terminal = boundary && boundaryOutcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(outcome == (boundary ? boundaryOutcome : RoomOutcome.Exchanged) && Key(predicted, terminal) == Key(actual, terminal),
                "Native successor differs at " + index + ": " + ModelJson.Difference(Key(predicted, terminal), Key(actual, terminal)));
            FixtureValue expected = row.GetProperty("Expected");
            if (expected.ValueKind == FixtureKind.Null)
                Require(boundary && row.GetProperty("UnsupportedReason").GetString() == reason,
                    "Only the original rejected boundary may omit its prediction.");
            else
            {
                Require(row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                    row.GetProperty("Outcome").GetInt32() == (int)outcome, "Historical outcome differs at " + index);
                BattleTurnState historical = expected.Deserialize<BattleTurnState>()!;
                if (boundary && historicalBoundaryMismatch != null && Key(historical, terminal) != Key(actual, terminal))
                    historicalBoundaryMismatch(historical, actual);
                else Require(Key(historical, terminal) == Key(actual, terminal), "Historical prediction differs at " + index);
            }
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Replay mutated its parent.");
            if (boundary) Require(Key(before) == Key(request.GetProperty("FailedRoot").Deserialize<BattleTurnState>()!), "Wrong failed root.");
            current = actual;
        }
        FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean(),
            "Native player projection was not pure/original.");
        return rows;
    }

    internal static void Pair(FixtureValue source, FixtureValue replay, bool originalPredictionPresent = false)
    {
        Require(source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent process changed its request.");
        FixtureValue[] first = source.GetProperty("Rows").EnumerateArray().ToArray();
        FixtureValue[] second = replay.GetProperty("Rows").EnumerateArray().ToArray();
        Require(first.Length == second.Length, "Independent process changed its input count.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Before", "Actual" })
                Require(Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!), "Independent process changed " + field + " at " + index);
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent input changed at " + index);
        }
        Require(first[^1].GetProperty("Expected").ValueKind == (originalPredictionPresent ? FixtureKind.Object : FixtureKind.Null) &&
            second[^1].GetProperty("Expected").ValueKind == FixtureKind.Object &&
            second[^1].GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null,
            "Preserve the original rejection and the independently repaired forecast.");
        bool terminal = (RoomOutcome)second[^1].GetProperty("Outcome").GetInt32() is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
        if (originalPredictionPresent)
            Require(Key(first[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!, terminal) !=
                Key(first[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal) &&
                Key(second[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!, terminal) ==
                Key(second[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal),
                "Preserve the original mismatching forecast and the independently repaired forecast.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
