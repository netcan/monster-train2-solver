using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HarvestListenerSnapshotChecks
{
    internal static void Native(string path, string? replayPath = null)
    {
        using var document = ModelJson.ReadFixture(path);
        var fixture = document.RootElement;
        Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("Mismatches").GetInt32() == 9,
            "Preserve the actual victory and its nine historical raw mismatches.");
        foreach (string counter in new[] { "CaptureFailures", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Incomplete native observation: " + counter);
        var affected = new Dictionary<int, int[]> {
            [26] = [8, 10], [27] = [8, 10], [42] = [15], [43] = [15], [52] = [19, 21], [53] = [19, 21]
        };
        var stages = fixture.GetProperty("Stages").EnumerateArray().ToArray();
        Require(stages.Where(stage => stage.GetProperty("Difference").ValueKind != FixtureKind.Null)
            .Select(stage => stage.GetProperty("Index").GetInt32()).SequenceEqual(affected.Keys),
            "Historical raw room mismatch boundaries changed.");
        foreach (var stage in stages.Where(stage => affected.ContainsKey(stage.GetProperty("Index").GetInt32())))
        {
            var old = stage.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Historical(old.Context!, actual.Context!, affected[stage.GetProperty("Index").GetInt32()]);
        }
        var trainDifferences = fixture.GetProperty("TrainPhases").EnumerateArray()
            .Where(phase => phase.GetProperty("Difference").ValueKind != FixtureKind.Null).ToArray();
        Require(trainDifferences.Length == 3, "Historical combat phases changed.");
        int[][] ids = [[8, 10], [15], [19, 21]];
        for (int index = 0; index < trainDifferences.Length; index++)
        {
            var phase = trainDifferences[index];
            Require(phase.GetProperty("Kind").GetString() == "Combat", "Expected raw combat observation.");
            Historical(phase.GetProperty("Predicted").GetProperty("State").Deserialize<TrainCombatState>()!.Context!,
                phase.GetProperty("Actual").Deserialize<TrainCombatState>()!.Context!, ids[index]);
        }
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(fixture);
        RollingAdviceChecks.ObservedSuccessors(fixture);
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 40 && rows.Count(row => row.GetProperty("IsEndTurn").GetBoolean()) == 9,
            "Lost the original 31 plays and nine EndTurns.");
        foreach (var row in rows)
        {
            var root = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var steps = row.GetProperty("PlanSteps").EnumerateArray().Select(step => new BattleSearchStep(
                step.GetProperty("Action").ValueKind == FixtureKind.Null ? null : step.GetProperty("Action").Deserialize<PlayCardAction>(),
                step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
            var planned = BattlePlannerChecks.Replay(root, steps, out var outcome);
            Require(outcome == row.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() &&
                NativeBranchObservationChecks.Key(planned, Terminal(outcome)) == NativeBranchObservationChecks.Key(
                    row.GetProperty("Planned").Deserialize<BattleTurnState>()!, Terminal(outcome)),
                "Historical advice forecast/state/RNG changed.");
        }
        var initial = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var final = BattlePlannerChecks.Replay(initial, rows.Select(row => new BattleSearchStep(
            row.GetProperty("Action").ValueKind == FixtureKind.Null ? null : row.GetProperty("Action").Deserialize<PlayCardAction>(),
            row.GetProperty("IsEndTurn").GetBoolean())).ToArray(), out var finalOutcome);
        Require(finalOutcome == RoomOutcome.BattleWon && NativeBranchObservationChecks.Key(final, true) ==
            NativeBranchObservationChecks.Key(rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!, true),
            "Continuous model-owned winning state/RNG changed.");
        var before = stages.Single(stage => stage.GetProperty("Index").GetInt32() == 26)
            .GetProperty("Before").Deserialize<RoomCombatState>()!;
        var expected = stages.Single(stage => stage.GetProperty("Index").GetInt32() == 26)
            .GetProperty("Actual").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var branch = RoomCombatModel.Resolve(before);
            Require(branch.Supported && ModelJson.Difference(JsonSerializer.Serialize(branch.State, ModelJson.Options),
                JsonSerializer.Serialize(expected, ModelJson.Options)) == null, "Harvest branch differs from full native room.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Harvest mutated its parent.");
        if (replayPath != null)
        {
            CompleteNativeBattleChecks.Native(replayPath);
            using var independent = ModelJson.ReadFixture(replayPath);
            var repeated = independent.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
            Require(repeated.Length == rows.Length, "Independent harvest replay changed the input count.");
            for (int index = 0; index < rows.Length; index++)
            {
                Require(rows[index].GetProperty("IsEndTurn").ContentEquals(repeated[index].GetProperty("IsEndTurn")) &&
                    rows[index].GetProperty("Action").ContentEquals(repeated[index].GetProperty("Action")),
                    "Independent harvest replay changed a native input.");
                foreach (string field in new[] { "Root", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!, index == rows.Length - 1 && field == "Actual") ==
                        NativeBranchObservationChecks.Key(repeated[index].GetProperty(field).Deserialize<BattleTurnState>()!, index == rows.Length - 1 && field == "Actual"),
                        "Independent harvest replay changed native state/forecast/RNG at input " + index);
            }
        }
        Console.WriteLine("HARVEST-LISTENER-SNAPSHOT PASS: six historical room/three train differences isolated to five enemy listeners; all78 raw stages/17 train phases/40 successors and forecasts, continuous win/RNG, four immutable branches. Historical counters remain unchanged; fresh acceptance required.");
        if (replayPath != null) Console.WriteLine("HARVEST-LISTENER-SNAPSHOT-PAIR PASS: unchanged40 native inputs/state/forecast/RNG; fresh independent raw room/train observations and four zero counters.");
    }
    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
    private static void Historical(CombatContext old, CombatContext actual, int[] ids)
    {
        var corrected = JsonSerializer.SerializeToNode(BattleTurnChecks.ComparableContext(old), ModelJson.Options)!;
        var native = JsonSerializer.SerializeToNode(BattleTurnChecks.ComparableContext(actual), ModelJson.Options)!;
        foreach (int id in ids)
        {
            var prior = old.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit;
            var current = actual.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit;
            Require(prior.DeathState is { HasFinishedDying: true, IsDestroyed: true, HasStatisticsListener: true } &&
                current.DeathState is { HasFinishedDying: true, IsDestroyed: true, HasStatisticsListener: false },
                "Lost original completed-death listener witness: " + id);
            corrected["Enchantments"]!["RetainedUnits"]!.AsArray().Single(actor => actor!["Unit"]!["Id"]!.GetValue<int>() == id)!
                ["Unit"]!["DeathState"]!["HasStatisticsListener"] = false;
        }
        Require(JsonNode.DeepEquals(corrected, native), "Historical mismatch extends beyond completed death listeners.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
