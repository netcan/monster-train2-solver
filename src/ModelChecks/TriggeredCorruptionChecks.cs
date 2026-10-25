using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TriggeredCorruptionChecks
{
    private const string Reason = "Battle preview floor 2: CorruptionAdded unit callbacks are not modeled.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 31 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native triggered-corruption prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var champion = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 2);
        var carver = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 5);
        var changed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 5);
        Require(before.Spawn.Turn == 7 && after.Spawn.Turn == 8 &&
            champion.AssetKey == "MonsterWurmChampion1" && champion.Triggers.Any(trigger => trigger.Kind == "OnAttacking" &&
                trigger.Effects.Any(effect => effect.Action is { Type: "AdjustCorruption", Target: "Room", Value: 1, CorruptionAdd: true })) &&
            carver.AssetKey == "MonsterShardsoulCarver" && carver.Attack == 20 && changed.Attack == 20 &&
            carver.Health == 30 && changed.Health == 30 && carver.Status("armor") == null && changed.Status("armor") == null &&
            before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Corruption == 1 &&
            after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Corruption == 1,
            "Missing native next-turn preview restoration of Carver/room corruption.");
        var floor = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
        var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var fired = RoomCombatModel.ApplyQueuedCharacterTrigger(floor,
            new(2, champion, "OnAttacking"), callbacks.Add);
        Require(fired.Supported && fired.State!.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Corruption == 2 &&
            callbacks.Any(callback => callback.Kind == "CorruptionAdded" && callback.Unit.Id == 5),
            "Champion Strike must queue Inspire and commit corruption before deferred execution.");
        foreach (string control in new[] { "owner", "amount" })
        {
            var json = JsonSerializer.SerializeToNode(champion, ModelJson.Options)!;
            if (control == "owner") json["AssetKey"] = "uncalibrated-corruption-owner";
            else foreach (var trigger in json["Triggers"]!.AsArray())
                if (trigger!["Kind"]!.GetValue<string>() == "OnAttacking") trigger["Effects"]![0]!["Action"]!["Value"] = 2;
            var altered = json.Deserialize<CombatUnit>(ModelJson.Options)!;
            var room = new RoomCombatState(floor.RoomIndex, floor.Deployment,
                floor.Units.Select(unit => unit.Id == champion.Id ? altered : unit).ToArray(), floor.ExternalInteractions,
                floor.Context, floor.Preview, floor.ActiveCompanionBossIds);
            var rejected = RoomCombatModel.ApplyQueuedCharacterTrigger(room, new(2, altered, "OnAttacking"), _ => { });
            Require(!rejected.Supported && rejected.UnsupportedReason == "CorruptionAdded unit callbacks are not modeled.",
                "Unknown corruption owner or amount bypassed its native calibration guard.");
        }
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second);
            var observations = second.GetProperty("TriggeredCorruption").EnumerateArray().ToArray();
            Require(observations.Length == 8 && observations.Select((record, index) =>
                record.GetProperty("Completed").GetBoolean() && record.GetProperty("Preview").GetBoolean() &&
                record.GetProperty("UnitId").GetInt32() == 5 &&
                record.GetProperty("Turn").GetInt32() == 8 && record.GetProperty("Room").GetInt32() == 2 &&
                record.GetProperty("AssetKey").GetString() == "MonsterShardsoulCarver" &&
                record.GetProperty("AttackBefore").GetInt32() == 20 + 3 * index && record.GetProperty("AttackAfter").GetInt32() == 23 + 3 * index &&
                record.GetProperty("ArmorBefore").GetInt32() == 3 * index && record.GetProperty("ArmorAfter").GetInt32() == 3 + 3 * index &&
                record.GetProperty("CorruptionBefore").GetInt32() == 2 + index && record.GetProperty("CorruptionAfter").GetInt32() == 2 + index).All(matches => matches),
                "Missing eight passive native preview Inspires after corruption commit:20->44attack,0->24armor,corruption2..9.");
        }
        Console.WriteLine("TRIGGERED-CORRUPTION PASS:31 native inputs/full state/RNG and immutable parents; next-turn preview handles Champion Strike/Inspire and restores live Carver/corruption; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
