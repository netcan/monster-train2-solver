using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitCloneChecks
{
    internal static readonly string[] Labels = ["null-source", "invalid-room", "ordinary-front", "ordinary-back-cardless",
        "wounded-buffed-excluded", "negative-damage-buff", "equipped-source", "runtime-equipment-ability", "clone-of-clone",
        "cardless-source", "full-room-card-allocation", "selected-no-adjacent", "horde-no-birth"];
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("UnitCloneOperations", out var values) || values.GetArrayLength() == 0) return;
        var records = values.EnumerateArray().ToArray();
        Require(records.Select(record => record.GetProperty("Label").GetString()).SequenceEqual(Labels), "Missing ordinary clone boundaries.");
        foreach (var record in records) Verify(record);
        Parallel.For(0, 32, _ => { foreach (var record in records) Verify(record); });
        foreach (var record in records.Where(record => record.GetProperty("UnitId").GetInt32() > 0))
        {
            var state = record.GetProperty("After").Deserialize<TrainCombatState>()!;
            var unit = state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == record.GetProperty("UnitId").GetInt32());
            Require(unit.Modifiers!.IsClone && (!record.GetProperty("Cardless").GetBoolean() || unit.Status("cardless")?.Stacks == 1),
                "Native ordinary birth lacks its clone/cardless marker.");
            if (unit.SpawnerCardId > 0)
                Require(state.Context!.CardRegistry!.Any(card => card.InstanceId == unit.SpawnerCardId) &&
                    state.Context.CardInstances!.All(card => card.InstanceId != unit.SpawnerCardId) &&
                    state.Context.FindCard(unit.SpawnerCardId)!.Permanent.Upgrades.Concat(state.Context.FindCard(unit.SpawnerCardId)!.Temporary.Upgrades)
                        .All(upgrade => !upgrade.ExcludeFromClones), "Native clone did not retain a detached source with stripped upgrades.");
        }
        var full = records.Single(record => record.GetProperty("Label").GetString() == "full-room-card-allocation");
        var fullBefore = full.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var fullAfter = full.GetProperty("AfterApi").Deserialize<TrainCombatState>()!;
        Require(full.GetProperty("UnitId").GetInt32() == 0 && fullAfter.Context!.NextCardId == fullBefore.Context!.NextCardId + 1 &&
            fullAfter.Context.NextUnitId == fullBefore.Context.NextUnitId && fullBefore.Context.SpawnPoints!.Group(2, CombatTeam.Player)!
                .Occupants.Take(fullBefore.Context.SpawnPoints.Group(2, CombatTeam.Player)!.GroupCount).All(id => id > 0),
            "Full-room clone did not exercise card allocation before birth failure.");
        foreach (var record in records.Where(record => record.GetProperty("Label").GetString() is "null-source" or "invalid-room" or "selected-no-adjacent"))
            Compare(record.GetProperty("Before").Deserialize<TrainCombatState>(), record.GetProperty("AfterApi").Deserialize<TrainCombatState>(), "Native early clone gate");
        Require(records.Single(record => record.GetProperty("Label").GetString() == "cardless-source").GetProperty("Before")
            .Deserialize<TrainCombatState>()!.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id ==
                records.Single(record => record.GetProperty("Label").GetString() == "cardless-source").GetProperty("SourceId").GetInt32()).SpawnerCardId == 0,
            "Cardless clone never exercised a missing source reference.");
        Console.WriteLine("NATIVE-UNIT-CLONE-CHECKS PASS: 13 native operations, exact copied-card/birth/equipment/stats/ability boundaries and callback payload/order, raw API and drained train states, excluded upgrades/statuses, wounded/buffed/negative actors, detached/cardless sources, failed allocations, Horde gates and 32 branches.");
    }
    private static void Verify(FixtureValue record)
    {
        string label = record.GetProperty("Label").GetString()!;
        var before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        string parent = JsonSerializer.Serialize(before);
        var result = UnitCloneModel.Apply(before, record.GetProperty("SourceId").GetInt32(), record.GetProperty("RoomIndex").GetInt32(),
            record.GetProperty("SpawnMode").GetString()!, record.GetProperty("Location").Deserialize<SpawnPointReference>(),
            record.GetProperty("Cardless").GetBoolean(), record.GetProperty("Rule").Deserialize<UnitCloneRule>());
        Require(result.Supported, label + " unsupported: " + result.UnsupportedReason);
        Compare(result.Boundaries.Select(boundary => boundary.Label).ToArray(), record.GetProperty("Boundaries").EnumerateArray()
            .Select(boundary => boundary.GetProperty("Label").GetString()).ToArray(), label + " boundary inventory");
        var expected = record.GetProperty("Boundaries").EnumerateArray().ToArray();
        for (int index = 0; index < result.Boundaries.Count; index++)
        {
            Compare(result.Boundaries[index].State, expected[index].GetProperty("State").Deserialize<TrainCombatState>(),
                label + "/" + result.Boundaries[index].Label);
            Require(result.Boundaries[index].QueueCount == expected[index].GetProperty("QueueCount").GetInt32(), label + " boundary queue differs.");
            Compare(result.Boundaries[index].Queued, expected[index].GetProperty("Queued").Deserialize<UnitCloneCallback[]>(), label + " boundary queue payloads");
        }
        Compare(result.State, record.GetProperty("AfterApi").Deserialize<TrainCombatState>(), label + " API return");
        Require(result.UnitId == record.GetProperty("UnitId").GetInt32() &&
            result.PendingCallbacks.Count == record.GetProperty("QueueAfterApi").GetInt32(), label + " API output/queue differs.");
        Compare(result.PendingCallbacks.Select(UnitCloneCallback.From).ToArray(), record.GetProperty("Queued").Deserialize<UnitCloneCallback[]>(), label + " API queue payloads");
        result = UnitCloneModel.Drain(result);
        Require(result.Supported, label + " drain unsupported: " + result.UnsupportedReason);
        Compare(result.State, record.GetProperty("After").Deserialize<TrainCombatState>(), label + " drained state");
        Compare(result.Dispatched, record.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), label + " complete dispatch payloads/order");
        Require(result.PendingCallbacks.Count == 0 && record.GetProperty("QueueAfter").GetInt32() == 0, label + " left callbacks.");
        Require(JsonSerializer.Serialize(before) == parent, label + " mutated its root.");
    }
    private static void Compare<T>(T predicted, T actual, string label)
    { string? error = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(actual)); Require(error == null, label + ": " + error); }
    private static void Require(bool okay, string message) { if (!okay) throw new Exception(message); }
}
