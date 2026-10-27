using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SummonCapacityRejectionChecks
{
    private const string Illegal = "The room cannot accept this unit's size or another spawn slot.";
    private const string Historical = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = Recording(source, historical: true);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
        Require(before.Spawn.Turn == 3 && before.Energy == 4 && action.CardInstanceId == 14 &&
            action.RoomIndex == 2 && action.PlayerPosition == 3 && action.ActivatorUnitId == 0 &&
            room.Units.Where(unit => unit.Team == CombatTeam.Player).Sum(unit => unit.Size) == 5 &&
            before.PlayRules!.Rooms.Single(room => room.RoomIndex == 2).PlayerCapacity == 5,
            "Expected the natural turn3 full-room Spear Steward candidate.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Rejection == ActionRejection.Illegal && result.State == null && result.Reason == Illegal,
                "Full-room summon must be illegal before unsupported paid-card callbacks.");
        });
        var frontier = BattleActionModel.EnumerateActions(before, onlyCardId: 14, includeAbilities: false);
        Require(!frontier.SupportedActions.Any(candidate => candidate.RoomIndex == 2) &&
            !frontier.UnsupportedAttempts.Any(attempt => attempt.Action?.CardInstanceId == 14 && attempt.Action.RoomIndex == 2),
            "The full room must not produce a legal action or a missing-mechanism report.");
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Summon rejection or enumeration mutated its parent.");
        if (replay.HasValue)
        {
            var second = Recording(replay.Value, historical: false);
            Require(source.GetProperty("Request").ContentEquals(replay.Value.GetProperty("Request")), "Independent rejected input changed.");
            for (int index = 0; index < rows.Length; index++)
            {
                foreach (string field in new[] { "Before", "Actual" })
                    Require(Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!), "Independent rejected branch state/RNG differs.");
                foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                    Require(rows[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent rejected branch input differs.");
            }
        }
        Console.WriteLine("SUMMON-CAPACITY-REJECTION PASS:25 native full successors/RNG plus RoomFull rejection; unchanged complete native state; four immutable branches; no false frontier gap; independentPair=" + replay.HasValue);
    }

    private static FixtureValue[] Recording(FixtureValue fixture, bool historical)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("NativeCompleted").GetBoolean() && fixture.GetProperty("NativeSelectionRejection").GetString() == "RoomFull",
            "Missing complete original native selection rejection.");
        var request = fixture.GetProperty("Request");
        Require(request.GetProperty("Reason").GetString() == Historical &&
            request.GetProperty("ExpectedNativeRejection").GetString() == "RoomFull", "Wrong rejected native boundary.");
        var prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        var rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(prefix.Length == 25 && rows.Length == 26, "Missing native prefix or rejected attempt.");
        var current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(current.Spawn.Turn == 0, "Native rejection must start at original deployment.");
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var step = index == prefix.Length ? request : prefix[index];
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool boundary = index == prefix.Length, end = step.GetProperty("IsEndTurn").GetBoolean();
            Require(Key(current) == Key(before) && row.GetProperty("Boundary").GetBoolean() == boundary &&
                row.GetProperty("IsEndTurn").GetBoolean() == end && row.GetProperty("Action").ContentEquals(step.GetProperty("Action")),
                "Rejected native prefix/input differs at " + index);
            if (boundary)
                Require(!end && Key(before) == Key(request.GetProperty("FailedRoot").Deserialize<BattleTurnState>()!) &&
                    Key(actual) == Key(before) && row.GetProperty("Expected").ValueKind == FixtureKind.Null &&
                    row.GetProperty("UnsupportedReason").GetString() == (historical ? Historical : Illegal),
                    "Rejected native input must preserve its full state and its historical model classification.");
            else
            {
                var action = step.GetProperty("Action").Deserialize<PlayCardAction>();
                var predicted = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out var outcome);
                Require(outcome == RoomOutcome.Exchanged && row.GetProperty("Outcome").GetInt32() == (int)outcome &&
                    row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null && Key(predicted) == Key(actual) &&
                    Key(row.GetProperty("Expected").Deserialize<BattleTurnState>()!) == Key(actual), "Native prefix successor/RNG differs at " + index);
            }
            current = actual;
        }
        var overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean(), "Native rejection projection was not pure/original.");
        return rows;
    }
    private static string Key(BattleTurnState state) => NativeBranchObservationChecks.Key(state);
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
