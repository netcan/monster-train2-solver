using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveMoonMovementChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeCompleted").GetBoolean() &&
            root.GetProperty("InactiveMovement").GetBoolean() &&
            root.GetProperty("RngUnchanged").GetBoolean() &&
            root.GetProperty("PreviewUpdatesSuppressed").GetBoolean() &&
            root.GetProperty("Boundary").GetString() == "AfterOriginalCoroutineAndTriggerQueue",
            "Missing accepted native inactive-movement capture.");
        int ordinary = root.GetProperty("OrdinaryEnemyId").GetInt32();
        int callback = root.GetProperty("CallbackEnemyId").GetInt32();
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        string[] labels = ["full-ordinary-entry", "full-ordinary-exit", "full-callback-entry", "full-callback-exit",
            "new-ordinary-entry", "new-ordinary-exit", "new-callback-entry", "new-callback-exit"];
        int[] beforeMasks = [6, 7, 6, 7, 6, 5, 6, 5];
        int[] afterMasks = [7, 6, 7, 6, 5, 5, 5, 5];
        int[] beforeUpgrades = [0, 0, 0, 1, 0, 0, 0, 1];
        int[] afterUpgrades = [0, 0, 1, 0, 0, 0, 1, 1];
        Require(ordinary > 0 && callback > 0 && ordinary != callback && rows.Length == labels.Length,
            "Missing ordinary and callback enemies or movement rows.");
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
            int id = index % 4 < 2 ? ordinary : callback;
            CombatUnit prior = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            CombatUnit moved = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            Require(row.GetProperty("Label").GetString() == labels[index] &&
                row.GetProperty("Operation").GetString() == "Bump" && row.GetProperty("UnitId").GetInt32() == id &&
                row.GetProperty("Amount").GetInt32() == (index % 2 == 0 ? -1 : 1) &&
                before.Context?.QueryFrame is { MoonPhaseTrackingActive: false } &&
                actual.Context?.QueryFrame is { MoonPhaseTrackingActive: false } &&
                before.Context.QueryFrame.MoonPhase == (index < 4 ? 2 : 1) &&
                actual.Context.QueryFrame.MoonPhase == before.Context.QueryFrame.MoonPhase &&
                prior.Team == CombatTeam.Enemy && moved.Team == CombatTeam.Enemy &&
                prior.MoonPhaseMask == beforeMasks[index] && moved.MoonPhaseMask == afterMasks[index] &&
                (prior.Modifiers?.Upgrades.Count ?? -1) == beforeUpgrades[index] &&
                (moved.Modifiers?.Upgrades.Count ?? -1) == afterUpgrades[index] &&
                row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Actual").GetProperty("Rng")),
                "Native inactive-movement actor, phase, callback or RNG witness is missing: " + labels[index]);
            Require(index % 4 < 2 ? prior.Triggers.All(trigger => !trigger.Kind.Contains("Moon")) :
                prior.Triggers.Any(trigger => trigger.Kind == "OnMoonShade"),
                "Missing original ordinary or callback enemy definition.");
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                BumpResult result = BumpModel.Apply(before, [id], row.GetProperty("Amount").GetInt32(),
                    row.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!);
                Require(result.Supported, labels[index] + ": " + result.UnsupportedReason);
                string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                    JsonSerializer.Serialize(actual, ModelJson.Options));
                Require(difference == null, labels[index] + ": " + difference);
            }
            Replay(); Parallel.For(0, 8, _ => Replay());
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options),
                "Inactive moon movement mutated its parent.");
            if (index != 0) continue;
            void Reject(Action<JsonNode> mutate, string boundary)
            {
                JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                mutate(altered);
                BumpResult result = BumpModel.Apply(altered.Deserialize<TrainCombatState>(ModelJson.Options)!,
                    [id], -1, row.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!);
                Require(!result.Supported, "Uncalibrated inactive movement accepted: " + boundary);
            }
            Reject(state => state["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == id)!["MoonPhaseMask"] = null, "missing mask");
            Reject(state => state["Rooms"]![0]!["Preview"] = true, "preview movement");
            Reject(state => state["Context"]!["RoomCapacities"]!.AsArray().Single(room =>
                room!["RoomIndex"]!.GetValue<int>() == 0)!["Attachments"]![0]!["MoonPhase"]!["AdditionalMask"] = 2,
                "unknown room modifier");
        }
        Console.WriteLine("INACTIVE-MOON-MOVEMENT PASS: eight original enemy Bumps, Full/New, ordinary/callback masks and upgrades, complete native state/RNG and eight immutable branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
