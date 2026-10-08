using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HeroCopyChecks
{
    internal static readonly string[] Labels = ["zero-targeted", "negative-room", "raw-source-no-stats", "copy-live-stats", "multiple-targeted",
        "clone-of-copy-ignores-range", "status-before-copy", "ability-before-copy", "equipped-no-stats", "equipped-live-stats",
        "room-multiple-targets", "horde-selected-last", "full-room-no-allocations", "partial-room-many-targets", "mixed-mask-enemy-to-player", "mixed-mask-player"];
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("HeroCopyOperations", out var values) || values.GetArrayLength() == 0) return;
        var records = values.EnumerateArray().ToArray();
        Require(records.Select(record => record.GetProperty("Label").GetString()).SequenceEqual(Labels), "Missing hero/mixed-team paid copy cases.");
        Require(fixture.GetProperty("Actions").EnumerateArray().Count(action => action.GetProperty("ScenarioAction").GetBoolean()) == records.Length, "Hero-copy paid action inventory differs.");
        foreach (var record in records) Verify(record);
        Parallel.For(0, 32, _ => { foreach (var record in records) Verify(record); });
        foreach (string label in new[] { "zero-targeted", "negative-room", "full-room-no-allocations" })
            Compare(Before(Find(label)), After(Find(label)), label + " native no-allocation state");
        var multiple = Find("multiple-targeted");
        Require(After(multiple).Context!.NextUnitId == Before(multiple).Context!.NextUnitId + 2, "Hero copying did not create two independent actors.");
        foreach (var record in records.Where(record => record.GetProperty("HeroBirth").GetBoolean()))
        {
            var rally = record.GetProperty("AfterQueue").Deserialize<UnitCloneCallback[]>()!
                .Concat(record.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>()!).Where(callback => callback.Kind == "CardMonsterPlayed").ToArray();
            if (record.GetProperty("Label").GetString() == "horde-selected-last")
                Require(rally.Length == 3 && rally.All(callback => callback.TriggerCount == 3 && callback.LastSpawnedOverrideUnitId == 0) &&
                    rally.Select(callback => callback.ActorId).SequenceEqual(Before(record).Rooms.Single(room => room.RoomIndex == record.GetProperty("RoomIndex").GetInt32())
                        .Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id)), "Hero stats copy did not preserve Horde's separate Rally over the existing room actors.");
            else Require(rally.Length == 0, "Hero birth unexpectedly counted a final or cardless Rally.");
            foreach (CombatUnit born in NewActors(record))
                Require(born.Team == CombatTeam.Enemy && born.SpawnerCardId == 0 && born.Modifiers?.IsClone == false && born.Status("cardless") == null,
                    "Hero copying used player/cardless clone ownership or flags.");
        }
        var raw = Find("raw-source-no-stats"); var live = Find("copy-live-stats");
        Require(NewActors(raw).Single().Health == 90 && NewActors(live).Single().Health == 41 &&
            NewActors(raw).Single().BaseAttack != NewActors(live).Single().BaseAttack, "Hero copying did not exercise both raw and live stats.");
        var ability = Find("ability-before-copy");
        CombatUnit abilitySource = Before(ability).Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == ability.GetProperty("Targets")[0].GetInt32());
        CombatUnit abilityBirth = NewActors(ability).Single();
        Require(abilitySource.Ability?.DataId != abilityBirth.Ability?.DataId && abilityBirth.Ability?.CooldownAtSpawn == 2,
            "Copied hero inherited a replaced source ability instead of its natural birth skill.");
        Require(NewActors(Find("equipped-no-stats")).Single().EquipmentCards!.Count == 0 &&
            NewActors(Find("equipped-live-stats")).Single().EquipmentCards!.Count == 1, "Hero gear copy toggle was not exercised.");
        var last = Find("horde-selected-last"); var lastBefore = Before(last);
        int sourceId = last.GetProperty("Targets")[0].GetInt32();
        var point = lastBefore.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == sourceId).Current!;
        var group = lastBefore.Context.SpawnPoints.Group(point.RoomIndex, CombatTeam.Enemy)!;
        Require(point.Index == group.GroupCount - 1 && group.Occupants.Take(point.Index).Any(id => id == 0) && NewActors(last).Length == 1 &&
            NewActors(last)[0].Status("horde") != null, "Last-point Horde source did not create a new hero at an earlier free point.");
        var partial = Find("partial-room-many-targets"); var partialBefore = Before(partial);
        var partialGroup = partialBefore.Context!.SpawnPoints!.Group(partial.GetProperty("RoomIndex").GetInt32(), CombatTeam.Enemy)!;
        Require(partial.GetProperty("Targets").GetArrayLength() == partialGroup.GroupCount - 1 &&
            partialGroup.Occupants.Take(partialGroup.GroupCount).Count(id => id == 0) == 1 && NewActors(partial).Length == 1,
            "Partial hero room did not exercise one birth and later failed allocations over its original target snapshot.");
        foreach (string label in new[] { "status-before-copy", "ability-before-copy" })
            Require(Find(label).GetProperty("BeforeQueue").GetArrayLength() > 0, label + " did not preserve incoming native callbacks");
        foreach (string label in new[] { "mixed-mask-enemy-to-player", "mixed-mask-player" })
            Require(!Find(label).GetProperty("HeroBirth").GetBoolean() && NewActors(Find(label)).Single().Team == CombatTeam.Player,
                "The declared mixed-team mask did not use monster cloning.");
        var ranged = Find("clone-of-copy-ignores-range"); Compare(Before(ranged).Context!.BattleRng, After(ranged).Context!.BattleRng, "Hero copy sampled ignored range");
        Console.WriteLine("NATIVE-HERO-COPY-CHECKS PASS: 16 real paid effects, complete train states/queues/dispatch payloads, raw/live stats, natural skills, equipment, Horde/last/full/partial points, mixed-team birth selection and 32 branches.");
        FixtureValue Find(string label) => records.Single(record => record.GetProperty("Label").GetString() == label);
    }
    private static void Verify(FixtureValue record)
    {
        string label = record.GetProperty("Label").GetString()!; var before = Before(record); string parent = JsonSerializer.Serialize(before);
        var pending = record.GetProperty("BeforeQueue").Deserialize<UnitCloneCallback[]>()!.Select(callback => From(before, callback)).ToArray();
        var result = UnitCopyModel.ApplyWithPending(before, record.GetProperty("RoomIndex").GetInt32(), record.GetProperty("Targets").Deserialize<int[]>()!,
            record.GetProperty("Count").GetInt32(), record.GetProperty("Catalog").Deserialize<UnitCopyCatalog>(), pending,
            record.GetProperty("HeroBirth").GetBoolean(), record.GetProperty("CopyStats").GetBoolean());
        Require(result.Supported, label + " unsupported: " + result.UnsupportedReason);
        if (record.GetProperty("HeroBirth").GetBoolean()) Require(result.Spawned == 0, label + " unexpectedly counted its hero births for final effect Rally.");
        Compare(result.State, After(record), label + " raw state");
        Compare(result.PendingCallbacks.Select(UnitCloneCallback.From).ToArray(), record.GetProperty("AfterQueue").Deserialize<UnitCloneCallback[]>(), label + " queue payload/order");
        Compare(result.Dispatched, record.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), label + " intrinsic dispatch payload/order");
        Require(JsonSerializer.Serialize(before) == parent, label + " mutated its parent");
    }
    private static TrainCombatState Before(FixtureValue record) => record.GetProperty("Before").Deserialize<TrainCombatState>()!;
    private static TrainCombatState After(FixtureValue record) => record.GetProperty("After").Deserialize<TrainCombatState>()!;
    private static CombatUnit[] NewActors(FixtureValue record)
    { var ids = Before(record).Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet(); return After(record).Rooms.SelectMany(room => room.Units).Where(unit => !ids.Contains(unit.Id)).ToArray(); }
    private static RoomCombatModel.QueuedCharacterTrigger From(TrainCombatState state, UnitCloneCallback callback)
    {
        CombatUnit Actor(int id) => state.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
        int room = state.Rooms.Single(room => room.Units.Any(unit => unit.Id == callback.ActorId)).RoomIndex;
        return new RoomCombatModel.QueuedCharacterTrigger(room, Actor(callback.ActorId), callback.Kind, paramInt: callback.ParamInt,
            overrideTarget: callback.OverrideTargetId == 0 ? null : Actor(callback.OverrideTargetId), paramInt2: callback.ParamInt2,
            paramString: callback.ParamString, dyingCharacter: callback.DyingId == 0 ? null : Actor(callback.DyingId), triggerCount: callback.TriggerCount,
            lastSpawnedOverrideUnitId: callback.LastSpawnedOverrideUnitId);
    }
    private static void Compare<T>(T predicted, T actual, string label)
    {
        string? error = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(actual));
        Require(error == null, label + ": " + error);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
