using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossCombatFlowChecks
{
    internal static void Continuation(FixtureValue root)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "OriginalTrueFinalBossCombatContinuation" &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("DetachedHealthAndStatusControls").GetBoolean() &&
            root.GetProperty("OriginalUnitsAndRooms").GetBoolean() && root.GetProperty("LiveWorldAndFrameAndRngUnchanged").GetBoolean(),
            "Missing pure native room-continuation evidence with disclosed detached controls.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 288, "Incomplete native continuation matrix.");
        int continuing = 0, lifeLinkBlocked = 0;
        var rooms = new List<RoomCombatState>();
        foreach (FixtureValue row in rows)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            bool relentless = row.GetProperty("RelentlessRoom").GetBoolean(), actual = row.GetProperty("Actual").GetBoolean();
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            bool result = CombatContinuationModel.ShouldContinue(before, relentless);
            Require(result == actual && !row.GetProperty("ShouldSpawnNextBoss").GetBoolean() &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native continuation or next-boss routing differs.");
            Require(before.RoomIndex == 3 && !before.Preview && before.Units.All(unit => unit.IsPyre || unit.AssetKey == TrueFinalBossChecks.Assets[0]),
                "Continuation query replaced the original physical Dominion/Pyre room.");
            if (actual) continuing++;
            if (!actual && relentless && before.Units.Count == 2 && before.Units.All(unit => unit.Health > 0) && before.Units.Any(unit => unit.Status("lifelink") != null) &&
                before.Units.All(unit => unit.Status("untouchable") == null)) lifeLinkBlocked++;
            rooms.Add(before);
        }
        Require(continuing == 4 && lifeLinkBlocked == 12, "Missing ordinary relentless positive controls or living life-link exclusion witnesses.");
        Parallel.For(0, 128, index => Require(CombatContinuationModel.ShouldContinue(rooms[index % rows.Length],
            rows[index % rows.Length].GetProperty("RelentlessRoom").GetBoolean()) == rows[index % rows.Length].GetProperty("Actual").GetBoolean(),
            "Concurrent continuation predicate diverged."));
        Console.WriteLine("TRUE-FINAL-BOSS-CONTINUATION PASS: 288 actual native queries, both teams' health/lifelink/untouchable and relentless controls; 128 concurrent branches; no native actor/frame/RNG changes.");
    }
    internal static void Passive(FixtureValue root)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue[] rows = root.GetProperty("PreCombats").EnumerateArray().ToArray();
        Require(rows.Length == 12, "Missing twelve original player/enemy pre-combat boundaries.");
        var inputs = new List<TrainCombatState>(); var expected = new List<string>();
        foreach (FixtureValue row in rows)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatTeam team = (CombatTeam)row.GetProperty("Team").GetInt32();
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            TrainCombatResult result = TrainCombatModel.PreCombat(before, team);
            string target = JsonSerializer.Serialize(actual, ModelJson.Options);
            string? difference = result.Supported ? ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), target) : result.UnsupportedReason;
            Require(result.Supported && difference == null && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "True-final-boss passive phase differs: " + difference);
            int turn = before.Context!.QueryFrame!.Turn!.Value;
            Require(turn == inputs.Count / 2 && team == (inputs.Count % 2 == 0 ? CombatTeam.Player : CombatTeam.Enemy) &&
                (turn == 0 ? before.Context.LifeLinks == null && before.Rooms.SelectMany(room => room.Units).All(unit => unit.IsPyre) :
                    before.Context.LifeLinks?.Actors.Count == 5 && before.Rooms.SelectMany(room => room.Units).Count(unit => unit.Status("lifelink") != null) == 5),
                "Passive phase omitted deployment/born actors or changed native team/turn order.");
            inputs.Add(before); expected.Add(target);
        }
        Parallel.For(0, 64, index =>
        {
            int selected = index % rows.Length;
            TrainCombatResult branch = TrainCombatModel.PreCombat(inputs[selected], (CombatTeam)rows[selected].GetProperty("Team").GetInt32());
            Require(branch.Supported && JsonSerializer.Serialize(branch.State, ModelJson.Options) == expected[selected], "Concurrent passive phase diverged.");
        });
        TrueFinalBossTrainChecks.RequirePreviewGuarded(inputs[2]);
        Console.WriteLine("TRUE-FINAL-BOSS-PASSIVE PASS: 12 actual pre-combat team phases, all five bodies and ordinary waves, full train/context/RNG equality; 64 immutable concurrent branches; preview remains guarded.");
    }
}
