using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UntouchableRoomAttachmentChecks
{
    private const string Reason = "Equipment Untouchable removes the impacted room attachment upgrade.";
    internal static void Native(FixtureValue battle, FixtureValue? source = null, FixtureValue? replay = null)
    {
        var rows = battle.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 34, "Missing natural34-input counterexample.");
        var row = rows[^1];
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit Unit(BattleTurnState state) => state.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 2);
        Require(action.CardInstanceId == 9 && action.TargetUnitId == 2 && before.Spawn.Turn == 6 &&
            Unit(before).EquipmentCards!.SequenceEqual([14]) && Unit(actual).EquipmentCards!.SequenceEqual([9]) &&
            Unit(before).Status("untouchable") == null && Unit(actual).Status("untouchable")?.Stacks == 1 &&
            Unit(before).Modifiers!.Upgrades.Count == 11 && Unit(actual).Modifiers!.Upgrades.Count == 11 &&
            Unit(actual).Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "EnchantedChamber"),
            "Wrong native crystal replacement/room upgrade removal.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && BattleTurnChecks.Comparable(result.State!, false) == BattleTurnChecks.Comparable(actual, false),
                "Untouchable cleanup differs from full native state/RNG: " + (result.Reason ?? ModelJson.Difference(
                    JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options))));
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Untouchable cleanup changed parent.");
        foreach (string control in new[] { "AssetKey", "DataId", "Health", "DamageBuff" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var attachment = changed["Spawn"]!["Train"]!["Context"]!["RoomCapacities"]!.AsArray()
                .Single(room => room!["RoomIndex"]!.GetValue<int>() == 2)!["Attachments"]![0]!;
            if (control == "AssetKey") attachment["AssetKey"] = "OtherChamber";
            if (control == "DataId")
            {
                attachment["CharacterUpgrades"]![0]!["DataId"] = "other-upgrade";
                var target = changed["Spawn"]!["Train"]!["Rooms"]!.AsArray()
                    .Single(room => room!["RoomIndex"]!.GetValue<int>() == 2)!["Units"]!.AsArray()
                    .Single(unit => unit!["Id"]!.GetValue<int>() == 2)!;
                target["Modifiers"]!["Upgrades"]!.AsArray()
                    .Single(upgrade => upgrade!["DataId"]!.GetValue<string>() == "68d8696e-ec20-425e-b23d-8a0b9da0dfaf")!["DataId"] = "other-upgrade";
            }
            if (control == "Health") attachment["CharacterUpgrades"]![0]!["Stats"]!["Health"] = 10;
            if (control == "DamageBuff") attachment["CharacterUpgrades"]![0]!["DamageBuff"] = 3;
            Require(!BattleActionModel.PlayCard(changed.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Untouchable cleanup accepted uncalibrated payload: " + control);
        }
        if (source.HasValue || replay.HasValue)
        {
            Require(source.HasValue && replay.HasValue, "Independent pair missing.");
            NativeBranchObservationChecks.Replay(source!.Value, Reason, (historical, native) =>
                Require(Unit(historical).Modifiers!.Upgrades.Count == 12 && Unit(native).Modifiers!.Upgrades.Count == 11,
                    "Original extra room-upgrade counterexample disappeared."));
            NativeBranchObservationChecks.Replay(replay!.Value, Reason);
            NativeBranchObservationChecks.Pair(source.Value, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("UNTOUCHABLE-ROOM-ATTACHMENT PASS: original crystal replacement; full native state/forecast/RNG; four immutable branches; independentPair=" + source.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
