using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GreenRecruitPreviewIdentityChecks
{
    internal const string Reason = "Green Recruit's turn8 room2 deployment differs from native unit identity allocation.";
    internal const string EndTurnReason = "Green Recruit's turn6 EndTurn differs from native preview unit identity allocation.";
    internal static void EndTurnCounterexample(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(key => fixture.GetProperty(key).GetInt32() == 0),
            "Preserve the single original EndTurn preview identity mismatch.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 35, "Missing complete34-input prefix and failed35th decision.");
        var row = rows[^1];
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var historical = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        EndTurnWitness(historical, actual);
        Require(row.GetProperty("IsEndTurn").GetBoolean() && before.Spawn.Turn == 6 && actual.Spawn.Turn == 7,
            "Lost the real turn6-to7 boundary.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, index =>
        {
            BattleTurnState result = BattlePlannerChecks.Replay(before, [new BattleSearchStep(null, true)], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(result) == NativeBranchObservationChecks.Key(actual),
                "EndTurn preview identity repair differs from full native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn preview mutated native parent.");
        Console.WriteLine("GREEN-RECRUIT-ENDTURN-IDENTITY PASS:35th-input counterexample and four immutable full-successor branches.");
    }

    internal static void EndTurnNative(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, EndTurnReason, EndTurnWitness);
        Require(rows.Length == 35, "Missing independent35-input native observation.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, EndTurnReason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("GREEN-RECRUIT-ENDTURN-IDENTITY NATIVE PASS:35 complete inputs/forecasts/RNG; independentPair=" + replay.HasValue);
    }

    private static void EndTurnWitness(BattleTurnState historical, BattleTurnState actual)
    {
        Require(historical.Spawn.Train.Context!.NextUnitId == 30 && actual.Spawn.Train.Context!.NextUnitId == 29 &&
            historical.Spawn.Train.Context.SpawnPoints!.Units.Any(unit => unit.UnitId == 29 && unit.SpawnedInPreview) &&
            actual.Spawn.Train.Context.SpawnPoints!.Units.All(unit => unit.UnitId != 29),
            "Lost the extra unobserved preview identity29 after EndTurn.");
    }
    internal static void Counterexample(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(key => fixture.GetProperty(key).GetInt32() == 0),
            "Preserve the single original Green Recruit preview identity mismatch.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 49, "Missing complete48-input prefix and failed49th decision.");
        var row = rows[^1];
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var historical = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Witness(historical, actual);
        Require(before.Spawn.Turn == 8 && before.Spawn.Train.Context!.NextUnitId == 33 &&
            action is { CardInstanceId: 25, RoomIndex: 2, PlayerPosition: 0 } &&
            actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.SpawnerCardId == 25) is { Id: 33, AssetKey: "MonsterGenerousHost", Health: 5 },
            "Lost real Green Recruit33 deployment before its temporary fallback preview.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, index =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Green Recruit identity repair differs from full native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Green Recruit preview mutated native parent.");
        Console.WriteLine("GREEN-RECRUIT-PREVIEW-IDENTITY PASS: preserved49th-input counterexample, real33 retained, unobserved preview34 omitted, complete successor/forecast/RNG and four immutable branches.");
    }

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Witness);
        Require(rows.Length == 49, "Missing independent49-input native observation.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("GREEN-RECRUIT-PREVIEW-IDENTITY NATIVE PASS:49 complete inputs/forecasts/RNG; preserved historical mismatch; independentPair=" + replay.HasValue);
    }

    private static void Witness(BattleTurnState historical, BattleTurnState actual)
    {
        Require(historical.Spawn.Train.Context!.NextUnitId == 35 && actual.Spawn.Train.Context!.NextUnitId == 34 &&
            historical.Spawn.Train.Context.SpawnPoints!.Units.Any(unit => unit.UnitId == 34 && unit.SpawnedInPreview) &&
            actual.Spawn.Train.Context.SpawnPoints!.Units.All(unit => unit.UnitId != 34), "Lost the one extra unobserved preview identity34.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
