using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitCopyChecks
{
    internal static readonly string[] Labels = ["zero-targeted", "negative-room", "single-targeted", "multiple-targeted",
        "clone-of-clone-ignores-range", "status-before-copy", "ability-before-copy", "equipped-source", "cardless-source",
        "room-multiple-targets", "full-room-two-card-allocations", "selected-last-no-allocation", "partial-room-many-targets", "horde-no-birth"];
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("UnitCopyOperations", out var values) || values.GetArrayLength() == 0) return;
        var records = values.EnumerateArray().ToArray();
        Require(records.Select(record => record.GetProperty("Label").GetString()).SequenceEqual(Labels), "Missing paid unit-copy cases.");
        var actions = fixture.GetProperty("Actions").EnumerateArray().Where(action => action.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == records.Length, "Paid-copy action inventory differs from native effect records.");
        foreach (var record in records) Verify(record);
        Parallel.For(0, 32, _ => { foreach (var record in records) Verify(record); });
        foreach (string label in new[] { "zero-targeted", "negative-room", "selected-last-no-allocation" })
        {
            var record = Find(label);
            Compare(record.GetProperty("Before").Deserialize<TrainCombatState>(), record.GetProperty("After").Deserialize<TrainCombatState>(), label + " native no-op gate");
        }
        var full = Find("full-room-two-card-allocations");
        var before = full.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var after = full.GetProperty("After").Deserialize<TrainCombatState>()!;
        Require(after.Context!.NextCardId == before.Context!.NextCardId + 2 && after.Context.NextUnitId == before.Context.NextUnitId,
            "Full-room paid copy did not allocate two detached cards without a unit.");
        var multiple = Find("multiple-targeted");
        Require(multiple.GetProperty("After").Deserialize<TrainCombatState>()!.Context!.NextUnitId ==
            multiple.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.NextUnitId + 2, "Two-copy effect did not create two actors.");
        var partial = Find("partial-room-many-targets");
        var partialBefore = partial.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var partialAfter = partial.GetProperty("After").Deserialize<TrainCombatState>()!;
        var group = partialBefore.Context!.SpawnPoints!.Group(partial.GetProperty("RoomIndex").GetInt32(), CombatTeam.Player)!;
        Require(Find("room-multiple-targets").GetProperty("Targets").GetArrayLength() >= 3 &&
            partial.GetProperty("Targets").GetArrayLength() == group.GroupCount - 1 &&
            group.Occupants.Take(group.GroupCount).Count(id => id == 0) == 1 &&
            partialAfter.Context!.NextUnitId == partialBefore.Context.NextUnitId + 1,
            "Paid copying lacks complete room snapshots and a single successful birth before full-room failures.");
        var ranged = Find("clone-of-clone-ignores-range");
        Compare(ranged.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.BattleRng,
            ranged.GetProperty("After").Deserialize<TrainCombatState>()!.Context!.BattleRng, "Copy count unexpectedly sampled integer range");
        foreach (string label in new[] { "status-before-copy", "ability-before-copy" })
            Require(Find(label).GetProperty("BeforeQueue").GetArrayLength() > 0, label + " did not exercise incoming native callbacks");
        Console.WriteLine("NATIVE-UNIT-COPY-CHECKS PASS: 14 real paid effects, complete raw states, incoming/outgoing queue payloads and intrinsic dispatch order, multi-target/continuous births, failed allocations, gear/skills/cardless/Horde cases and 32 branches.");
        FixtureValue Find(string label) => records.Single(record => record.GetProperty("Label").GetString() == label);
    }
    private static void Verify(FixtureValue record)
    {
        string label = record.GetProperty("Label").GetString()!;
        var before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        string root = JsonSerializer.Serialize(before);
        var queued = record.GetProperty("BeforeQueue").Deserialize<UnitCloneCallback[]>()!.Select(callback => From(before, callback)).ToArray();
        var result = UnitCopyModel.ApplyWithPending(before, record.GetProperty("RoomIndex").GetInt32(),
            record.GetProperty("Targets").Deserialize<int[]>()!, record.GetProperty("Count").GetInt32(),
            record.GetProperty("Catalog").Deserialize<UnitCopyCatalog>(), queued);
        Require(result.Supported, label + " unsupported: " + result.UnsupportedReason);
        Compare(result.State, record.GetProperty("After").Deserialize<TrainCombatState>(), label + " raw effect state");
        Compare(result.PendingCallbacks.Select(UnitCloneCallback.From).ToArray(), record.GetProperty("AfterQueue").Deserialize<UnitCloneCallback[]>(), label + " raw queue payload/order");
        Compare(result.Dispatched, record.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), label + " complete intrinsic dispatch payload/order");
        Require(JsonSerializer.Serialize(before) == root, label + " mutated its root");
    }
    private static RoomCombatModel.QueuedCharacterTrigger From(TrainCombatState state, UnitCloneCallback callback)
    {
        CombatUnit Actor(int id) => state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
        int room = state.Rooms.Single(room => room.Units.Any(unit => unit.Id == callback.ActorId)).RoomIndex;
        return new RoomCombatModel.QueuedCharacterTrigger(room, Actor(callback.ActorId), callback.Kind, paramInt: callback.ParamInt,
            overrideTarget: callback.OverrideTargetId == 0 ? null : Actor(callback.OverrideTargetId), paramInt2: callback.ParamInt2,
            paramString: callback.ParamString, dyingCharacter: callback.DyingId == 0 ? null : Actor(callback.DyingId),
            triggerCount: callback.TriggerCount, lastSpawnedOverrideUnitId: callback.LastSpawnedOverrideUnitId);
    }
    private static void Compare<T>(T predicted, T actual, string label)
    {
        string? error = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(actual));
        if (error != null && predicted is IEnumerable<UnitCloneCallback> callbacks)
            error += "\nModel dispatches: " + string.Join(", ", callbacks.Select(callback => callback.ActorId + ":" + callback.Kind));
        Require(error == null, label + ": " + error);
    }
    private static void Require(bool okay, string message) { if (!okay) throw new Exception(message); }
}
