using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MirroredHordeBirthChecks
{
    internal const string Reason = "Battle preview floor 2: Room spawn copying on generated/triggered births requires its native phase integration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 11 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong mirrored Horde birth boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 2 && actual.Spawn.Turn == 3 &&
            actual.Spawn.Train.Context!.NextUnitId == 17 &&
            actual.Spawn.Train.Context.Enchantments!.RetainedUnits.Any(actor =>
                actor.Unit is { Id: 16, AssetKey: "MonsterFunguy", Health: 4 }),
            "Native preview must retain the original single Funguy birth identity after restoration.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Mirrored Horde birth differs from complete native state/forecast/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Mirrored Horde birth mutated its parent.");
        foreach (string control in new[] { "FunguyAsset", "AttackDefinition", "HealthDefinition", "Status", "HallOwner", "HallAsset", "HallSubtype" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var context = node["Spawn"]!["Train"]!["Context"]!;
            foreach (var rooms in new[] { node["Spawn"]!["Train"]!["Rooms"]!, context["Enchantments"]!["Rooms"]! })
            {
                var room = rooms.AsArray().Single(room => room!["RoomIndex"]!.GetValue<int>() == 2)!;
                var owner = room["Units"]!.AsArray().Single(unit => unit!["Id"]!.GetValue<int>() == 9)!;
                var action = owner["Triggers"]![0]!["Effects"]![0]!["Action"]!;
                if (control == "FunguyAsset") action["SpawnFallbackUnit"]!["AssetKey"] = "uncalibrated-funguy";
                if (control == "AttackDefinition") action["SpawnFallbackUnit"]!["HordeDefinition"]!["Attack"] = 4;
                if (control == "HealthDefinition") action["SpawnFallbackUnit"]!["HordeDefinition"]!["Health"] = 5;
                if (control == "Status") action["Statuses"]![0]!["Id"] = "uncalibrated-horde";
            }
            var attachment = node["Spawn"]!["Train"]!["Context"]!["RoomCapacities"]!.AsArray()
                .Single(room => room!["RoomIndex"]!.GetValue<int>() == 2)!["Attachments"]![0]!;
            if (control == "HallOwner") attachment["DataId"] = "uncalibrated-mirrors";
            if (control == "HallAsset") attachment["AssetKey"] = "uncalibrated-mirrors";
            if (control == "HallSubtype") attachment["SpawnCopySubtype"] = "SubtypesData_Horde";
            var result = BattleTurnModel.EndTurn(node.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(!result.Supported, "Unknown mirrored birth signature lost its guard: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MIRRORED-HORDE-BIRTH PASS:11 native inputs/full state/forecast/RNG; four immutable branches/seven scope controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
