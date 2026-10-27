using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PendingPhysicalHarvestChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var stage = fixture.GetProperty("Stages")[51];
        var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        Require(stage.GetProperty("Kind").GetString() == "Exchange" && before.RoomIndex == 0 &&
            !stage.GetProperty("TriggerQueueRunning").GetBoolean() && stage.GetProperty("QueuedTriggers").GetArrayLength() == 0 &&
            stage.GetProperty("ActiveDeaths").GetArrayLength() == 0, "Unexpected native exchange entry.");
        Require(fixture.GetProperty("StageRemovals").EnumerateArray().Any(record =>
            record.GetProperty("ActiveStages").Deserialize<int[]>()!.SequenceEqual([50, 51]) &&
            record.GetProperty("EligibleUnitIds").Deserialize<int[]>()!.SequenceEqual([32]) &&
            !record.GetProperty("OverrideList").GetBoolean() && record.GetProperty("Started").GetBoolean()),
            "Missing native pending removal of unit 32.");
        Require(fixture.GetProperty("StageHarvests").EnumerateArray().Any(record =>
            record.GetProperty("ActiveStages").Deserialize<int[]>()!.SequenceEqual([50, 51]) &&
            record.GetProperty("ActorId").GetInt32() == 29 && record.GetProperty("DyingUnitId").GetInt32() == 32 &&
            record.GetProperty("AttackBefore").GetInt32() == 4 && record.GetProperty("AttackAfter").GetInt32() == 5),
            "Missing native harvest witness.");
        var old = RoomCombatModel.Exchange(before);
        Require(old.Supported && old.State!.Units.Single(unit => unit.Id == 29).Attack == 4 &&
            actual.Units.Single(unit => unit.Id == 29).Attack == 5, "Historical standalone exchange difference changed.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = RoomCombatModel.Exchange(before, pendingPhysicalDeathIds: [32]);
            Require(result.Supported && Key(result.State!) == Key(actual),
                "Pending physical harvest differs: " + (result.Supported ? ModelJson.Difference(Key(result.State!), Key(actual)) : result.UnsupportedReason));
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Exchange mutated its parent.");
        Require(!RoomCombatModel.Exchange(before, pendingPhysicalDeathIds: [999]).Supported &&
            !RoomCombatModel.Exchange(before, pendingPhysicalDeathIds: [32, 32]).Supported,
            "Unknown or duplicate pending deaths escaped validation.");
        Console.WriteLine("PENDING-PHYSICAL-HARVEST PASS: native removal/harvest witness and full exchange state/RNG; four immutable branches/two guards.");
    }
    private static string Key(RoomCombatState room) => JsonSerializer.Serialize(room.WithContext(BattleTurnChecks.ComparableContext(room.Context)), ModelJson.Options);
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
