using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveRoomBatchChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeCompleted").GetBoolean() &&
            root.GetProperty("InactiveRoomBatch").GetBoolean() &&
            root.GetProperty("RngUnchanged").GetBoolean() &&
            root.GetProperty("PreviewUpdatesSuppressed").GetBoolean() &&
            root.GetProperty("Boundary").GetString() == "AfterOriginalCoroutineAndTriggerQueue",
            "Missing native inactive room batch.");
        int id = root.GetProperty("EnemyId").GetInt32();
        FixtureValue[] groups = root.GetProperty("Groups").EnumerateArray().ToArray();
        string[] names = ["EnchantedChamber", "Infectory", "SpellSanctum", "TrapChute", "ZoneOfSilence"];
        string[] labels = ["full-entry", "full-exit", "new-stale-entry", "new-exit", "new-stale-exit", "new-return"];
        int[] masksBefore = [6, 6, 6, 5, 6, 5], masksAfter = [6, 6, 5, 5, 5, 5];
        Require(id > 0 && groups.Length == names.Length, "Missing original room groups.");
        for (int roomIndex = 0; roomIndex < groups.Length; roomIndex++)
        {
            FixtureValue group = groups[roomIndex];
            string key = group.GetProperty("Card").GetString()!;
            string cardId = group.GetProperty("DataId").GetString()!;
            FixtureValue[] rows = group.GetProperty("Rows").EnumerateArray().ToArray();
            Require(key == names[roomIndex] && rows.Length == labels.Length,
                "Missing original room movement group: " + names[roomIndex]);
            for (int index = 0; index < rows.Length; index++)
            {
                FixtureValue row = rows[index];
                TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
                TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
                CombatUnit prior = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
                CombatUnit moved = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
                RoomCapacityAttachmentState attachment = before.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0)
                    .Attachments!.Single();
                bool entry = index is 0 or 2 or 5;
                Require(row.GetProperty("Label").GetString() == labels[index] &&
                    row.GetProperty("Operation").GetString() == "Bump" && row.GetProperty("UnitId").GetInt32() == id &&
                    row.GetProperty("Amount").GetInt32() == (entry ? -1 : 1) &&
                    before.Context.QueryFrame is { MoonPhaseTrackingActive: false } &&
                    actual.Context?.QueryFrame is { MoonPhaseTrackingActive: false } &&
                    before.Context.QueryFrame.MoonPhase == (index < 2 ? 2 : 1) &&
                    actual.Context.QueryFrame.MoonPhase == before.Context.QueryFrame.MoonPhase &&
                    attachment.AssetKey == key && attachment.DataId == cardId && attachment.AffectsHeroes &&
                    attachment.AffectsMonsters == (key != "ZoneOfSilence") &&
                    attachment.CharacterUpgrades.Count == 1 &&
                    RoomAttachmentUnitModel.OriginalBatchRoom(attachment) &&
                    prior is { AssetKey: "MageT1_Junker", Team: CombatTeam.Enemy, SpawnerCardId: 0 } &&
                    prior.Triggers.All(trigger => !trigger.Kind.Contains("Moon")) &&
                    prior.MoonPhaseMask == masksBefore[index] && moved.MoonPhaseMask == masksAfter[index] &&
                    (prior.Modifiers?.Upgrades.Count ?? -1) == (entry ? 0 : 1) &&
                    (moved.Modifiers?.Upgrades.Count ?? -1) == (entry ? 1 : 0) &&
                    row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Actual").GetProperty("Rng")),
                    "Missing original room movement witness: " + key + "/" + labels[index]);
                RoomPlayRule[] rules = row.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!;
                string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
                void Replay()
                {
                    BumpResult result = BumpModel.Apply(before, [id], entry ? -1 : 1, rules);
                    Require(result.Supported, key + "/" + labels[index] + ": " + result.UnsupportedReason);
                    string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
                        JsonSerializer.Serialize(actual, ModelJson.Options));
                    Require(difference == null, key + "/" + labels[index] + ": " + difference);
                }
                Replay(); Parallel.For(0, 4, _ => Replay());
                Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options),
                    "Inactive room batch mutated its parent: " + key);
                if (index is not (2 or 4)) continue;
                TrainCombatState roundtrip = JsonSerializer.SerializeToNode(before, ModelJson.Options)!
                    .Deserialize<TrainCombatState>(ModelJson.Options)!;
                BumpResult roundtripResult = BumpModel.Apply(roundtrip, [id], entry ? -1 : 1, rules);
                Require(roundtripResult.Supported &&
                    roundtripResult.State?.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex == (entry ? 0 : 1),
                    "JSON roundtrip lost inactive-room movement: " + key + "/" + labels[index]);
                JsonNode Unit(JsonNode state) => state["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                    .Single(unit => unit!["Id"]!.GetValue<int>() == id)!;
                JsonNode Attachment(JsonNode context) => context["RoomCapacities"]!.AsArray()
                    .Single(room => room!["RoomIndex"]!.GetValue<int>() == 0)!["Attachments"]![0]!;
                void ChangeAttachment(JsonNode state, Action<JsonNode> change)
                {
                    change(Attachment(state["Context"]!));
                    foreach (JsonNode? room in state["Rooms"]!.AsArray())
                        if (room?["Context"] != null) change(Attachment(room["Context"]!));
                }
                void Reject(Action<JsonNode> mutate, string boundary)
                {
                    JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                    mutate(changed);
                    TrainCombatState altered = changed.Deserialize<TrainCombatState>(ModelJson.Options)!;
                    BumpResult result = BumpModel.Apply(altered,
                        [id], entry ? -1 : 1, rules);
                    Require(!result.Supported, "Uncalibrated " + key + " movement accepted: " + boundary);
                }
                Reject(state => Unit(state)["MoonPhaseMask"] = null, "missing moon mask");
                Reject(state => state["Rooms"]![0]!["Preview"] = true, "preview");
                Reject(state => ChangeAttachment(state, attachment => attachment["DataId"] = "unknown"), "room identity");
                JsonNode alteredUpgrade = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                ChangeAttachment(alteredUpgrade, item => item["CharacterUpgrades"]![0]!["DataId"] = "unknown");
                Require(!RoomAttachmentUnitModel.OriginalBatchRoom(alteredUpgrade.Deserialize<TrainCombatState>(ModelJson.Options)!
                    .Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single()),
                    "Altered character upgrade retained original room allowance: " + key);
                Reject(state => Unit(state)["SpawnerCardId"] = 999, "card spawner");
                Reject(state => Unit(state)["Triggers"]![0]!["Kind"] = "OnMoonShade", "moon callback");
                JsonNode renamed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                Unit(renamed)["AssetKey"] = "AnotherOriginalEnemy";
                BumpResult generic = BumpModel.Apply(renamed.Deserialize<TrainCombatState>(ModelJson.Options)!,
                    [id], entry ? -1 : 1, rules);
                CombatUnit? renamedMoved = generic.State?.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
                Require(generic.Supported && renamedMoved?.MoonPhaseMask == 5 &&
                    renamedMoved.Modifiers?.Upgrades.Count == (entry ? 1 : 0),
                    "Original " + key + " movement still depends on enemy name: " + generic.UnsupportedReason);
            }
        }
        Console.WriteLine("INACTIVE-ROOM-BATCH PASS: five original rooms, 30 Full/New Bumps, complete context/RNG, guarded boundaries and four immutable branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
