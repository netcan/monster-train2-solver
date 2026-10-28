using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleHordePhaseBoundaryChecks
{
    private const string Reason = "Train combat floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue original, FixtureValue experiment, FixtureValue phases,
        FixtureValue timeline, FixtureValue replayTimeline)
    {
        NativeBranchObservationChecks.Pair(original, experiment);
        FixtureValue request = original.GetProperty("Request");
        Require(request.GetProperty("Reason").GetString() == Reason && request.GetProperty("IsEndTurn").GetBoolean(),
            "Missing original rejected EndTurn.");
        var rows = original.GetProperty("Rows").EnumerateArray().ToArray();
        var prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        Require(prefix.Length == 20 && rows.Length == 21, "Missing native prefix.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        for (int i = 0; i < prefix.Length; i++)
        {
            var before = rows[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(before), "Native prefix changed at " + i);
            var action = prefix[i].GetProperty("Action").Deserialize<PlayCardAction>();
            var step = new BattleSearchStep(action, prefix[i].GetProperty("IsEndTurn").GetBoolean());
            current = BattlePlannerChecks.Replay(current, [step], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(actual),
                "Native prefix successor differs at " + i);
        }
        var guarded = BattleTurnModel.EndTurn(current);
        Require(!guarded.Supported && guarded.UnsupportedReason == Reason, "Unverified phase must remain guarded.");
        var boundary = experiment.GetProperty("Rows")[20].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        FixtureValue turn = phases.GetProperty("Turns")[3];
        var predicted = turn.GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
        var observed = turn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(turn.GetProperty("Predicted").GetProperty("Supported").GetBoolean() &&
            turn.GetProperty("Difference").GetString() == "Decision state, resources, card piles or gameplay RNG differ" &&
            predicted.Spawn.NextUnitId == 27 && observed.Spawn.NextUnitId == 28 && boundary.Spawn.NextUnitId == 27,
            "Missing original allocator phase mismatch.");
        Require(observed.Spawn.Train.Context!.SpawnPoints!.Units.Any(point => point.UnitId == 27 && point.SpawnedInPreview &&
                point.Current == null && point.LastKnown == null) &&
            !boundary.Spawn.Train.Context!.SpawnPoints!.Units.Any(point => point.UnitId == 27),
            "Missing transient native preview spawn point.");
        var firstFrames = CheckTimeline(original, timeline);
        var repeatedFrames = CheckTimeline(original, replayTimeline);
        for (int i = 0; i < firstFrames.Length; i++)
            Require(NativeBranchObservationChecks.Key(firstFrames[i]) == NativeBranchObservationChecks.Key(repeatedFrames[i]),
                "Independent settlement frame differs at " + i);
        Console.WriteLine("TRIPLE-HORDE-PHASE-BOUNDARY PASS: 20 native prefix inputs, two independent two-frame development snapshots and player successors; cross-collector allocator mismatch remains guarded.");
    }

    internal static BattleTurnState[] CheckTimeline(FixtureValue original, FixtureValue timeline)
    {
        Require(timeline.GetProperty("NativeCompleted").GetBoolean() &&
            original.GetProperty("Request").ContentEquals(timeline.GetProperty("Request")), "Timeline request differs.");
        var expected = original.GetProperty("Rows").EnumerateArray().ToArray();
        var observed = timeline.GetProperty("Rows").EnumerateArray().ToArray();
        Require(expected.Length == observed.Length, "Timeline input count differs.");
        for (int i = 0; i < expected.Length; i++)
        {
            foreach (string field in new[] { "Before", "Actual" })
                Require(NativeBranchObservationChecks.Key(expected[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(observed[i].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Timeline changed " + field + " at " + i);
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary", "UnsupportedReason" })
                Require(expected[i].GetProperty(field).ContentEquals(observed[i].GetProperty(field)),
                    "Timeline changed input at " + i);
        }
        var frames = timeline.GetProperty("SettlementTimeline").EnumerateArray().ToArray();
        Require(frames.Length == 2, "Expected two sampled frames.");
        var states = new BattleTurnState[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            var first = frame.GetProperty("First").Deserialize<BattleTurnState>()!;
            var second = frame.GetProperty("Second").Deserialize<BattleTurnState>()!;
            Require(frame.GetProperty("Frame").GetInt32() == frame.GetProperty("ReadyFrame").GetInt32() + i &&
                frame.GetProperty("CaptureStable").GetBoolean() &&
                NativeBranchObservationChecks.Key(first) == NativeBranchObservationChecks.Key(second) &&
                first.Spawn.NextUnitId == 28 && first.Spawn.Train.Context!.SpawnPoints!.Units.Any(point =>
                    point.UnitId == 27 && point.SpawnedInPreview && point.Current == null && point.LastKnown == null),
                "Transient preview point missing or unstable at frame " + i);
            states[i] = first;
        }
        var settled = observed[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(settled.Spawn.NextUnitId == 27 && !settled.Spawn.Train.Context!.SpawnPoints!.Units.Any(point => point.UnitId == 27),
            "Player decision unexpectedly includes the development preview point.");
        return states;
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
