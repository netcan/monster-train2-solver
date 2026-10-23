using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossScheduledChecks
{
    internal static void Native(FixtureValue root, bool cumulativeStartup = false)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "ScheduledTrueFinalBossBirth" && root.GetProperty("NativeReturned").GetBoolean(),
            "Missing original scheduled true-final-boss native return.");
        BattleTurnState before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = root.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options), expected = JsonSerializer.Serialize(actual, ModelJson.Options);
        BattleTurnResult result = TrueFinalBossSpawningModel.ApplyQueued(before);
        string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), expected) : result.UnsupportedReason;
        Require(result.Supported && difference == null, "Scheduled true-final-boss birth differs: " + difference);
        Require((cumulativeStartup ? before.Spawn.QueuedTrueFinalBoss?.QueuedBosses.OrderBy(value => value).SequenceEqual(new[] { 0, 1, 2 }) :
            before.Spawn.QueuedTrueFinalBoss?.QueuedBosses.SequenceEqual(new[] { 0, 2, 1 })) == true &&
            before.Spawn.TrueFinalBoss == null && actual.Spawn.QueuedTrueFinalBoss == null &&
            actual.Spawn.OuterBoss is { UnitId: 6, Room: 2, AssetKey: "Boss_Level9_Entropy_Top_V2" } &&
            actual.Spawn.TrueFinalBoss is { HellforgedBoss: true, WaitedOnRelentless: false, QueuedBossCount: 0 } &&
            actual.Spawn.Train.Context!.LifeLinks!.Actors.Count == 5 && !TrueFinalBossSpawningModel.ApplyQueued(actual).Supported,
            "Scheduled birth must consume its queue once and preserve the original top controller and five linked actors.");
        Parallel.For(0, 64, _ =>
        {
            BattleTurnResult branch = TrueFinalBossSpawningModel.ApplyQueued(before);
            Require(branch.Supported && JsonSerializer.Serialize(branch.State, ModelJson.Options) == expected &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Scheduled birth branch changed its parent or result.");
        });
        TrueFinalBossTrainChecks.RequirePreviewGuarded(actual.Spawn.Train);
        Console.WriteLine("TRUE-FINAL-BOSS-SCHEDULED PASS: original five-actor scheduled birth, queue consumption, global controller/Hellforged flags, full battle/RNG equality and 64 immutable concurrent branches; preview remains guarded.");
    }
}
