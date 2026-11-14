using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveInfernoMovementChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeCompleted").GetBoolean() &&
            root.GetProperty("InactiveInfernoMovement").GetBoolean() &&
            root.GetProperty("RngUnchanged").GetBoolean() &&
            root.GetProperty("PreviewUpdatesSuppressed").GetBoolean() &&
            root.GetProperty("Boundary").GetString() == "AfterOriginalCoroutineAndTriggerQueue",
            "Missing original inactive Inferno movement capture.");
        int id = root.GetProperty("OrdinaryEnemyId").GetInt32();
        Require(id > 0 && id != root.GetProperty("CallbackEnemyId").GetInt32(),
            "Missing distinct ordinary enemy and callback control.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        string[] labels = ["full-ordinary-entry", "full-ordinary-exit", "new-ordinary-entry", "new-ordinary-exit"];
        int[] masksBefore = [6, 6, 6, 5], masksAfter = [6, 6, 5, 5];
        int[] upgradesBefore = [0, 1, 0, 1], upgradesAfter = [1, 0, 1, 0];
        Require(rows.Length == labels.Length, "Missing four native Inferno movements.");
        for (int index = 0; index < rows.Length; index++)
        {
            FixtureValue row = rows[index];
            TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
            CombatUnit prior = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            CombatUnit moved = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            RoomCapacityAttachmentState attachment = before.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0)
                .Attachments!.Single();
            Require(row.GetProperty("Label").GetString() == labels[index] &&
                row.GetProperty("Operation").GetString() == "Bump" && row.GetProperty("UnitId").GetInt32() == id &&
                row.GetProperty("Amount").GetInt32() == (index % 2 == 0 ? -1 : 1) &&
                before.Context.QueryFrame is { MoonPhaseTrackingActive: false } &&
                actual.Context?.QueryFrame is { MoonPhaseTrackingActive: false } &&
                before.Context.QueryFrame.MoonPhase == (index < 2 ? 2 : 1) &&
                actual.Context.QueryFrame.MoonPhase == before.Context.QueryFrame.MoonPhase &&
                attachment is { AssetKey: "InfernoRoom", AffectsHeroes: true, AffectsMonsters: true,
                    CharacterUpgrades.Count: 1 } &&
                prior is { Team: CombatTeam.Enemy, AssetKey: "MageT1_Junker", SpawnerCardId: 0 } &&
                prior.Triggers.All(trigger => !trigger.Kind.Contains("Moon")) &&
                prior.MoonPhaseMask == masksBefore[index] && moved.MoonPhaseMask == masksAfter[index] &&
                (prior.Modifiers?.Upgrades.Count ?? -1) == upgradesBefore[index] &&
                (moved.Modifiers?.Upgrades.Count ?? -1) == upgradesAfter[index] &&
                row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Actual").GetProperty("Rng")),
                "Missing native inactive Inferno upgrade/mask witness: " + labels[index]);
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            RoomPlayRule[] rooms = row.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!;
            void Replay()
            {
                BumpResult result = BumpModel.Apply(before, [id], index % 2 == 0 ? -1 : 1, rooms);
                Require(result.Supported, labels[index] + ": " + result.UnsupportedReason);
                string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                    JsonSerializer.Serialize(actual, ModelJson.Options));
                Require(difference == null, labels[index] + ": " + difference);
            }
            Replay(); Parallel.For(0, 8, _ => Replay());
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options),
                "Inferno movement mutated its parent.");
            if (index != 2) continue;
            void Reject(Action<JsonNode> mutate, string boundary)
            {
                JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                mutate(altered);
                BumpResult result = BumpModel.Apply(altered.Deserialize<TrainCombatState>(ModelJson.Options)!, [id], -1, rooms);
                Require(!result.Supported, "Uncalibrated Inferno movement accepted: " + boundary);
            }
            Reject(state => state["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == id)!["MoonPhaseMask"] = null, "missing mask");
            Reject(state => state["Rooms"]![0]!["Preview"] = true, "preview movement");
            Reject(state => state["Context"]!["RoomCapacities"]!.AsArray().Single(room =>
                room!["RoomIndex"]!.GetValue<int>() == 0)!["Attachments"]![0]!["DataId"] = "unknown",
                "altered attachment");
            Reject(state => state["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == id)!["SpawnerCardId"] = 999,
                "uncalibrated spawner traits");
            Reject(state => state["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == id)!["Triggers"]![0]!["Kind"] = "OnMoonShade",
                "moon callback payload");
            JsonNode renamed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            renamed["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == id)!["AssetKey"] = "AnotherOriginalEnemy";
            BumpResult generic = BumpModel.Apply(renamed.Deserialize<TrainCombatState>(ModelJson.Options)!, [id], -1, rooms);
            CombatUnit? renamedMoved = generic.State?.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
            Require(generic.Supported && renamedMoved is { MoonPhaseMask: 5, Modifiers.Upgrades.Count: 1 },
                "Inactive original Inferno still depends on an enemy asset name: " + generic.UnsupportedReason);
        }
        Console.WriteLine("INACTIVE-INFERNO-MOVEMENT PASS: four original enemy Bumps, unchanged Inferno upgrade, stale New mask, exact native context/RNG and eight immutable branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
