using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelentlessOuterBossBumpChecks
{
    internal const string Reason = "Native spell-weakness Bump differs after rolling input39.";

    internal static void Counterexample(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(key => fixture.GetProperty(key).GetInt32() == 0),
            "Preserve the original outer-boss Bump mismatch.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 39, "Missing38 unchanged inputs plus the native39th Bump.");
        var row = rows[^1];
        Historical(row.GetProperty("Expected").Deserialize<BattleTurnState>()!, row.GetProperty("Actual").Deserialize<BattleTurnState>()!);
        Boundary(row.GetProperty("Root").Deserialize<BattleTurnState>()!, row.GetProperty("Action").Deserialize<PlayCardAction>()!,
            row.GetProperty("Actual").Deserialize<BattleTurnState>()!);
        Console.WriteLine("RELENTLESS-OUTER-BUMP COUNTEREXAMPLE PASS: original39th-input mismatch retained; complete corrected successor/forecast/RNG and four immutable branches.");
    }

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 39, "Incomplete natural39-input outer-boss movement observation.");
        Boundary(rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!, rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!,
            rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("RELENTLESS-OUTER-BUMP NATIVE PASS:39 complete inputs/forecasts/RNG, original floor destruction and two sacrifices; independentPair=" + replay.HasValue);
    }

    private static void Boundary(BattleTurnState before, PlayCardAction action, BattleTurnState actual)
    {
        var boss = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 9);
        Require(before.Spawn.Turn == 6 && action is { CardInstanceId: 21, RoomIndex: 0, TargetUnitId: 9 } &&
            boss.BumpRules is { OuterBoss: true, CompanionBoss: false, OuterBossRelentlessPhase: true } && boss.Status("relentless") != null &&
            before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != 9).Select(unit => unit.Id).SequenceEqual(new[] { 60, 61 }) &&
            actual.Spawn.Train.Rooms[0].Units.Count == 0 && !actual.PlayRules!.Rooms[0].Enabled &&
            actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 9).Status("spell weakness")?.Stacks == 4,
            "Lost original relentless boss upward move and cleared departure floor.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected,
                "Paid relentless outer-boss move failed its full native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Relentless outer-boss movement changed its parent.");
    }

    private static void Historical(BattleTurnState historical, BattleTurnState actual) => Require(
        historical.Spawn.Train.Rooms[0].Units.Select(unit => unit.Id).SequenceEqual(new[] { 60, 61 }) && historical.PlayRules!.Rooms[0].Enabled &&
        actual.Spawn.Train.Rooms[0].Units.Count == 0 && !actual.PlayRules!.Rooms[0].Enabled,
        "Lost original forecast's two survivors and enabled departure room.");
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
