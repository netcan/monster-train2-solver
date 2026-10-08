using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SpawnEnchantChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "spawn-enchant") return;
        var records = fixture.GetProperty("SpawnEnchantments").EnumerateArray().ToArray();
        var births = fixture.GetProperty("UnitBirths").EnumerateArray().ToArray();
        Require(records.Length >= 3 && births.Length >= 3, "Native enchant or birth coverage is missing.");
        foreach (var record in records) Verify(record);
        foreach (var birth in births) VerifyBirth(birth);
        Parallel.For(0, 32, _ => { foreach (var record in records) Verify(record); foreach (var birth in births) VerifyBirth(birth); });
        bool newbornEnchant = false, oldActorEnchant = false, hordeGrowth = false;
        int childCallbacks = 0;
        foreach (var record in records)
        {
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            int id = record.GetProperty("ActorId").GetInt32();
            CombatUnit actor = before.Units.Single(unit => unit.Id == id);
            var spawning = before.Units.Where(unit => unit.IsSpawning == true).ToArray();
            Require(spawning.Length == 1, "Enchant phase did not retain exactly one in-progress player birth.");
            newbornEnchant |= actor.IsSpawning == true;
            oldActorEnchant |= actor.IsSpawning == false;
            hordeGrowth |= (actor.Status("horde")?.Stacks ?? 0) > 0 &&
                (after.Units.Single(unit => unit.Id == id).Status("horde")?.Stacks ?? 0) > actor.Status("horde")!.Stacks;
            if (actor.SpawnerCardId > 0)
                Require(after.Context!.FindCard(actor.SpawnerCardId)!.PlayedRoomUnitIds!.SequenceEqual(before.Units
                    .Where(unit => unit.Health > 0 && unit.IsSpawning != true).Select(unit => unit.Id).OrderBy(id => id)),
                    "Enchant effects included the in-progress birth in their source card's room cache.");
            childCallbacks += record.GetProperty("AfterQueue").GetArrayLength() - record.GetProperty("BeforeQueue").GetArrayLength();
        }
        Require(newbornEnchant && oldActorEnchant && hordeGrowth && childCallbacks > 0,
            "Native newborn/older enchant, Horde growth or child callback paths were not exercised.");
        var actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        var rules = actions.Select(action =>
        {
            var before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int cardId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == cardId).DataId;
            return before.PlayRules!.Cards.Single(rule => rule.DataId == dataId);
        }).Where(rule => rule.Effect == "SpawnMonster").ToArray();
        Require(rules.Any(rule => rule.Summon == null) && rules.Any(rule => rule.Summon?.Count == 2),
            "Native ordinary and repeated paid births were not both present.");
        Require(births.Any(birth =>
        {
            var definition = birth.GetProperty("Definition").Deserialize<CardPlayRule>()!;
            return definition.Summon == null && definition.SpawnUnit!.Triggers.Any(trigger => trigger.Kind == "AfterSpawnEnchant");
        }), "An ordinary paid enchant host birth was not captured.");
        foreach (var phase in fixture.GetProperty("RallyPhases").EnumerateArray()) RallyChecks.VerifyPhase(phase);
        foreach (var trigger in fixture.GetProperty("RallyTriggers").EnumerateArray()) RallyChecks.VerifyTrigger(trigger);
        Console.WriteLine($"NATIVE-SPAWN-ENCHANT-CHECKS PASS: {records.Length} native enchant phases, {births.Length} complete births, " +
            $"{childCallbacks} exact child payloads, newborn/older Horde growth, in-progress exclusion, ordinary/repeated paid sources and 32 branches.");
    }
    private static void Verify(FixtureValue record)
    {
        Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            record.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null, "Incomplete or mismatched native enchant observation.");
        var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before);
        int id = record.GetProperty("ActorId").GetInt32();
        var queued = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, before.Units.Single(unit => unit.Id == id),
            "AfterSpawnEnchant", canFireTriggers: record.GetProperty("CanFire").GetBoolean(), triggerCount: record.GetProperty("TriggerCount").GetInt32());
        var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
        RoomCombatResult result = RoomCombatModel.ApplyQueuedCharacterTrigger(before, queued, callbacks.Add);
        Require(result.Supported, "Independent enchant phase unsupported: " + result.UnsupportedReason);
        Compare(result.State, record.GetProperty("After").Deserialize<RoomCombatState>(), "Enchant actor/room state");
        Compare(record.GetProperty("BeforeQueue").Deserialize<UnitCloneCallback[]>()!.Concat(callbacks.Select(UnitCloneCallback.From)).ToArray(),
            record.GetProperty("AfterQueue").Deserialize<UnitCloneCallback[]>(), "Enchant child queue payload/order");
        Require(JsonSerializer.Serialize(before) == parent, "Enchant phase mutated its parent.");
    }
    private static void VerifyBirth(FixtureValue record)
    {
        Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Incomplete or mismatched native enchant birth.");
        var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before);
        UnitBirthResult result = UnitBirthModel.Spawn(before, record.GetProperty("Definition").Deserialize<CardPlayRule>()!,
            record.GetProperty("SpawnerCardId").GetInt32(), record.GetProperty("Position").GetInt32(), record.GetProperty("IsCardless").GetBoolean(),
            record.GetProperty("CardlessStatus").Deserialize<CombatStatus>());
        Require(result.Supported && result.UnitId == record.GetProperty("UnitId").GetInt32(), "Independent enchant birth unsupported: " + result.Result.UnsupportedReason);
        Compare(result.Result.State, record.GetProperty("After").Deserialize<RoomCombatState>(), "Complete enchant birth state");
        Require(result.Result.State!.Units.Single(unit => unit.Id == result.UnitId).IsSpawning == false && JsonSerializer.Serialize(before) == parent,
            "Completed enchant birth retained spawning or mutated its parent.");
    }
    private static void Compare<T>(T predicted, T actual, string label)
    { string? error = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(actual)); Require(error == null, label + ": " + error); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
