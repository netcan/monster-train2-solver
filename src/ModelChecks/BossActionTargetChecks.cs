using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BossActionTargetChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 && fixture.GetProperty("DecisionUnchanged").GetBoolean() &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("Scenario").GetString() == "Level8FinalBossBuffer",
            "Boss targeting calibration changed the native decision.");
        Require(fixture.GetProperty("FrameBefore").GetInt32() == fixture.GetProperty("FrameAfter").GetInt32() &&
            JsonSerializer.Serialize(fixture.GetProperty("RngBefore")) == JsonSerializer.Serialize(fixture.GetProperty("RngAfter")),
            "Boss targeting calibration changed a frame or RNG stream.");
        Require(fixture.GetProperty("Errors").GetArrayLength() == 0, "Native targeting observation failed.");
        int samples = 0, forced = 0, initial = 0, singletonDraws = 0;
        foreach (FixtureValue row in fixture.GetProperty("Samples").EnumerateArray())
        {
            int[] rooms = row.GetProperty("Rooms").Deserialize<int[]>()!;
            int[]? history = row.GetProperty("VisitedRooms").Deserialize<int[]>();
            bool first = row.GetProperty("Initial").GetBoolean();
            UnityRng before = row.GetProperty("Before").Deserialize<UnityRng>();
            UnityRng after = row.GetProperty("After").Deserialize<UnityRng>();
            BossRoomSelectionResult result = BossActionTargetModel.SelectRandom(rooms, history, first, before);
            Require(result.Supported && result.Room == row.GetProperty("Room").GetInt32() && result.Rng.Equals(after),
                "Native random boss room selection or RNG differs at sample " + samples);
            if (first) { Require(before.Equals(after), "Initial native boss selection consumed RNG."); initial++; }
            if (!first && history != null && history.Where((age, room) => age >= 2 && rooms.Contains(room)).Any()) forced++;
            if (!first && rooms.Length == 1 && !before.Equals(after)) singletonDraws++;
            samples++;
        }
        Require(samples == 1792 && forced > 0 && initial == 896 && singletonDraws > 0,
            "Missing native boss selection, visitation or single-candidate RNG boundaries.");
        int actions = 0, setups = 0, skipped = 0, moved = 0;
        var bosses = new HashSet<string>();
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            BossActionTargetState before = row.GetProperty("Before").Deserialize<BossActionTargetState>()!;
            BossActionTargetResult actual = row.GetProperty("Actual").Deserialize<BossActionTargetResult>()!;
            string parent = JsonSerializer.Serialize(before);
            BossActionTargetResult result = BossActionTargetModel.Setup(before);
            Require(result.Supported && JsonSerializer.Serialize(result) == JsonSerializer.Serialize(actual),
                "Native boss action target/post-action room, visits or RNG differs at action " + actions);
            Require(JsonSerializer.Serialize(before) == parent, "Boss targeting mutated the parent branch.");
            if (before.Initial) setups++;
            else if (before.SetupAsInitialAction) skipped++;
            else if (actual.TargetRoom != actual.PostActionRoom) moved++;
            bosses.Add(row.GetProperty("Boss").GetString()!); actions++;
        }
        Require(actions >= 3 && setups > 0 && skipped > 0 && bosses.SetEquals(new[] { "Boss_Level8_AllyBuffer" }),
            "Missing actual outer-boss initial and subsequent native setup calls.");
        int controls = 0, postMoves = 0;
        var behaviors = new HashSet<string>();
        foreach (FixtureValue row in fixture.GetProperty("ControlActions").EnumerateArray())
        {
            BossActionTargetState before = row.GetProperty("Before").Deserialize<BossActionTargetState>()!;
            BossActionTargetResult actual = row.GetProperty("Actual").Deserialize<BossActionTargetResult>()!;
            string parent = JsonSerializer.Serialize(before);
            BossActionTargetResult result = BossActionTargetModel.Setup(before);
            Require(result.Supported && JsonSerializer.Serialize(result) == JsonSerializer.Serialize(actual),
                "Detached native boss target setup differs at control " + controls);
            Require(JsonSerializer.Serialize(before) == parent, "Detached boss targeting mutated the parent.");
            if (actual.TargetRoom != actual.PostActionRoom) postMoves++;
            behaviors.Add(before.Behavior); controls++;
        }
        Require(controls == 3024 && postMoves > 0 && behaviors.SetEquals(new[] {
            "RandomBossActionBehavior", "RandomOccupiedBossActionBehavior", "RoomDestroyActionBehavior" }),
            "Missing detached native boss behavior and post-action movement controls.");
        Require(!BossActionTargetModel.SelectRandom([], null, false, UnityRng.Seed(1)).Supported,
            "Empty boss room candidates were silently accepted.");
        Console.WriteLine($"BOSS-TARGET PASS: {samples} native selections ({forced} forced, {singletonDraws} singleton draws), " +
            $"{actions} actual setups ({setups} initial, {skipped} reused, {moved} post-action moves), " +
            $"{controls} detached controls ({postMoves} post-action moves), bosses={string.Join(',', bosses)}; immutable parents and exact RNG.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
