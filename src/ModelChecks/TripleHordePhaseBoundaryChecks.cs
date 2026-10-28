using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleHordePhaseBoundaryChecks
{
    private const string Reason = "Train combat floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue original, FixtureValue experiment, FixtureValue phases)
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
        Console.WriteLine("TRIPLE-HORDE-PHASE-BOUNDARY PASS: 20 native prefix inputs and independent observed successors; unverified 27/28 allocator transition remains guarded.");
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
