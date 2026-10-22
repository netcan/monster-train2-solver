using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossPhaseChecks
{
    internal static void Controllers(FixtureValue root)
    {
        BossActionControllerChecks.Native(root, "Boss_Level9_Entropy_Top_V2", 11, new[] { "DecideInitial", "DecideNext" });
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        TrueFinalBossChecks.Require(root.GetProperty("Definition").GetProperty("Groups").GetArrayLength() == 0 &&
            actions.Length == 11 && actions.Select(row => row.GetProperty("Boss").GetString()).Distinct().OrderBy(name => name)
                .SequenceEqual(new[] { TrueFinalBossChecks.Assets[0], TrueFinalBossChecks.Assets[4], TrueFinalBossChecks.Assets[1] }.OrderBy(name => name)) &&
            actions.All(row => row.GetProperty("Boss").GetString() == TrueFinalBossChecks.Assets[1]
                ? row.GetProperty("Before").Deserialize<BossActionControllerProgress>()!.Groups.Count == 1 &&
                    row.GetProperty("Actual").Deserialize<BossActionControllerProgress>()!.NextAction is { Group: 0, Action: 0 }
                : row.GetProperty("Before").Deserialize<BossActionControllerProgress>()!.Groups.Count == 0 &&
                    row.GetProperty("Actual").Deserialize<BossActionControllerProgress>()!.NextAction == null),
            "True-final-boss native controllers must retain the original Savagery initial action and empty Dominion/global top groups.");
        Console.WriteLine("TRUE-FINAL-BOSS-CONTROLLERS PASS: 11 original Dominion/Savagery/Entropy-top operations, Savagery initial latch, empty global top controller and 1152 detached native controls; no Level8 action substitution.");
    }
    internal static void Phases(FixtureValue root)
    {
        void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
        bool controlled = root.GetProperty("ForcedSpawnQueueEmpty").GetBoolean();
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "AfterBossActionRemoveDeadCharacters" &&
            root.GetProperty("QueueControls").GetArrayLength() == (controlled ? 2 : 0) &&
            root.GetProperty("Errors").GetArrayLength() == 0, "Missing natural original true-final-boss phase boundaries.");
        FixtureValue[] rows = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(rows.Length == (controlled ? 3 : 9), "Incomplete native true-final-boss phase sequence.");
        if (controlled)
        {
            FixtureValue queue = root.GetProperty("QueueControls")[0], pyre = root.GetProperty("QueueControls")[1];
            Require(queue.GetProperty("Kind").GetString() == "ForceSpawnQueueEmpty" && queue.GetProperty("Turn").GetInt32() == 2 &&
                queue.GetProperty("PhaseAfter").GetInt32() == queue.GetProperty("NativeWaveCount").GetInt32() &&
                pyre.GetProperty("Kind").GetString() == "SetPyreHealth" && pyre.GetProperty("HealthAfter").GetInt32() == 1 &&
                pyre.GetProperty("MaxHealthAfter").GetInt32() == 1, "Exhausted-wave proof must disclose its exact queue and short Pyre controls.");
            EnemySpawnState exhausted = rows.Last().GetProperty("Before").Deserialize<OuterBossPhaseState>()!.Battle.Spawn;
            Require(exhausted.Phase >= exhausted.Waves.Count, "Controlled phase never reached the exhausted original wave queue.");
        }
        Require(rows[0].GetProperty("Before").Deserialize<OuterBossPhaseState>()!.Battle.Spawn.Train.Context!.LifeLinks!
            .Actors.Select(actor => actor.Health).SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }),
            "The first native action phase must preserve original five-actor starting health.");
        var parents = new List<OuterBossPhaseState>(); var serialized = new List<string>();
        int pre = 0, post = 0; var savageryRooms = new HashSet<int>();
        foreach (FixtureValue row in rows)
        {
            string phase = row.GetProperty("Phase").GetString()!;
            OuterBossPhaseState before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            OuterBossPhaseState actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
            Require(row.GetProperty("NativeReturned").GetBoolean() && phase is "BossActionPreCombat" or "BossActionPostCombat",
                "Missing completed native true-final-boss pre/post action phase.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            OuterBossPhaseResult result = OuterBossPhaseModel.Apply(before, phase == "BossActionPreCombat");
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options)) : result.UnsupportedReason;
            Require(result.Supported && difference == null && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Original true-final-boss action phase differs: " + phase + ": " + difference);
            Require(before.Boss.Phase == "Casting" && actual.Boss.Phase == "Casting" && before.Boss.Controller.Groups.Count == 0 &&
                actual.TrueFinalBoss is { HellforgedBoss: true, WaitedOnRelentless: false, QueuedBossCount: 0 },
                "Native phase altered the original casting controller or Hellforged flags.");
            if (phase == "BossActionPreCombat") pre++; else post++;
            savageryRooms.Add(before.Battle.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.AssetKey == TrueFinalBossChecks.Assets[1])).RoomIndex);
            parents.Add(before); serialized.Add(parent);
        }
        Require(pre == (controlled ? 2 : 5) && post == (controlled ? 1 : 4) && savageryRooms.SetEquals(controlled ? new[] { 0, 1 } : new[] { 0, 1, 2 }),
            "Native phase coverage must include five pre/four post phases and Savagery looping over the three combat floors.");
        Parallel.For(0, 64, index =>
        {
            int selected = index % parents.Count;
            OuterBossPhaseResult result = OuterBossPhaseModel.Apply(parents[selected], rows[selected].GetProperty("Phase").GetString() == "BossActionPreCombat");
            Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) == serialized[selected] &&
                JsonSerializer.Serialize(parents[selected], ModelJson.Options) == serialized[selected], "Concurrent Hellforged casting phase changed its parent or result.");
        });
        var missing = new OuterBossPhaseState(parents[0].Battle, parents[0].Boss);
        Require(!OuterBossPhaseModel.Apply(missing, true).Supported &&
            !TrainCombatModel.ResolveCombat(parents[0].Battle.Spawn.Train).Supported,
            "Missing Hellforged flags and uncalibrated full true-final-boss combat must remain guarded.");
        Console.WriteLine($"TRUE-FINAL-BOSS-PHASES PASS: {rows.Length} original pre/post action boundaries, exhaustedQueueControl={controlled}, Hellforged casting flags, ordinary waves and full battle/controller/RNG equality; 64 immutable concurrent phases; full battle remains guarded.");
    }
}
