using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AscendDestructionBoundaryChecks
{
    internal static void Native(FixtureValue original, FixtureValue settled)
    {
        foreach (var fixture in new[] { original, settled })
            RandomCampaignChecks.Input(fixture, 1043013037, 0, 2, "Underlegion", "LunaCoven", 10000);
        var old = original.GetProperty("TrainPhases")[13];
        var oldBefore = old.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var oldActual = old.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        var oldResult = TrainCombatModel.Ascend(oldBefore);
        Require(original.GetProperty("RollingDecisions").GetArrayLength() == 50 &&
            old.GetProperty("Kind").GetString() == "Ascend" && old.GetProperty("Turn").GetInt32() == 6 &&
            old.GetProperty("Difference").ValueKind == FixtureKind.String && oldResult.Supported &&
            Key(oldResult.State!) != Key(oldActual) && Raw(oldBefore).SequenceEqual([7, 10, 36, 37, 51, 52, 53]) &&
            Raw(oldActual).SequenceEqual([10, 51, 52, 53]), "Preserve the unsettled-frame counterexample and its live enemy reference.");
        Require(settled.GetProperty("NativeWon").GetBoolean() && settled.GetProperty("RollingDecisions").GetArrayLength() == 48,
            "Expected the new actual-budget48-input native victory.");
        var phase = settled.GetProperty("TrainPhases")[13];
        var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var after = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        var ids = phase.GetProperty("SettledDestructionIds").EnumerateArray().Select(value => value.GetInt32()).ToHashSet();
        Require(phase.GetProperty("Kind").GetString() == "Ascend" && phase.GetProperty("Turn").GetInt32() == 6 &&
            phase.GetProperty("SettlementFrames").GetInt32() == 1 && new[] { 7, 36, 37 }.All(ids.Contains) &&
            !Raw(before).Any(ids.Contains) && Raw(before).SequenceEqual(Raw(after)) &&
            settled.GetProperty("TrainPhases").EnumerateArray().All(row => row.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "Native OnDestroy must complete before both strict component snapshots, with all train phases matching.");
        var early = settled.GetProperty("TrainPhases")[3];
        Require(early.GetProperty("Kind").GetString() == "Ascend" && early.GetProperty("Turn").GetInt32() == 1 &&
            early.GetProperty("SettlementFrames").GetInt32() == 0 && early.GetProperty("SettledDestructionIds").GetArrayLength() == 0,
            "Do not wait for the earlier BeginDestroy/InRemoveList stages before their native queue flush.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = TrainCombatModel.Ascend(before);
            Require(result.Supported && Key(result.State!) == Key(after), "Settled Ascend component differs from full native context/weak references.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Ascend changed parent.");
        Console.WriteLine("ASCEND-DESTRUCTION-BOUNDARY PASS: original50-input frame counterexample, native48-input victory, exact pending destruction/one-frame settlement, early removal no-wait control and four immutable strict components.");
    }
    private static IReadOnlyList<int> Raw(TrainCombatState state) => state.Context!.FindCard(2)!.RawPlayedRoomUnitIds!;
    private static string Key(TrainCombatState state)
    {
        var value = JsonNode.Parse(JsonSerializer.Serialize(new {
            Rooms = state.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
            Context = BattleTurnChecks.ComparableContext(state.Context) }))!;
        (value["Context"]?["Statistics"] as JsonObject)?.Remove("StoredCards");
        return value.ToJsonString();
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
