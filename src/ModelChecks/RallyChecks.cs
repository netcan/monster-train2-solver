using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RallyChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("RallyOperations", out var records) || records.GetArrayLength() == 0) return;
        var operations = records.EnumerateArray().ToArray();
        var phases = fixture.GetProperty("RallyPhases").EnumerateArray().ToArray();
        var triggers = fixture.GetProperty("RallyTriggers").EnumerateArray().ToArray();
        Require(operations.Select(sample => Label(sample)).SequenceEqual([
            "grow-first-player-by-two", "zero-player-growth", "remove-one-player-troop",
            "first-enemy-horde", "grow-enemy-by-two"]), "Missing native Rally birth/growth/removal cases.");
        Require(phases.Length == 10 && triggers.Length == 12, "Missing native paid Rally phases or repeated Horde dispatches.");
        foreach (var sample in operations) VerifyOperation(sample, triggers);
        foreach (var sample in phases) VerifyPhase(sample);
        foreach (var sample in triggers) VerifyTrigger(sample);

        var first = triggers.Where(sample => Label(sample) == "paid-summon-0").ToArray();
        Require(first.Length == 2 && first[0].GetProperty("TriggerCount").GetInt32() == 2 &&
            first[0].GetProperty("Before").Deserialize<RoomCombatState>()!.Context!.LastSpawnedUnitId == 0 &&
            first[1].GetProperty("TriggerCount").GetInt32() == 1 &&
            first[1].GetProperty("Before").Deserialize<RoomCombatState>()!.Context!.LastSpawnedUnitId > 0,
            "First Horde birth must see an empty reference, then paid Rally must see the new unit.");
        var second = triggers.Where(sample => Label(sample) == "paid-summon-1").ToArray();
        Require(second.Select(sample => sample.GetProperty("TriggerCount").GetInt32()).SequenceEqual([2, 2, 1, 1]) &&
            second[0].GetProperty("Before").Deserialize<RoomCombatState>()!.Context!.LastSpawnedUnitId !=
                second[^1].GetProperty("Before").Deserialize<RoomCombatState>()!.Context!.LastSpawnedUnitId,
            "Second Horde birth lost prior-reference timing, both teams or paid callback ordering.");
        Require(triggers.Any(sample => sample.GetProperty("LastSpawnedOverrideUnitId").GetInt32() > 0 &&
            sample.GetProperty("LastSpawnedOverrideUnitId").GetInt32() !=
                sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Context!.LastSpawnedUnitId),
            "Native Rally did not exercise an override distinct from the last summon.");
        Require(triggers.Any(sample => sample.GetProperty("Actor").Deserialize<CombatUnit>()!.Status("silenced")?.Stacks > 0),
            "Native Rally did not exercise visible/hidden triggers under silence.");
        foreach (var sample in phases.Where(sample => Label(sample).StartsWith("paid-summon-", StringComparison.Ordinal)))
        {
            var before = sample.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var cached = sample.GetProperty("CachedUnitIds").Deserialize<int[]>()!;
            Require(!cached.Contains(before.Context!.LastSpawnedUnitId!.Value) &&
                before.Rooms.Single(room => room.RoomIndex == 1).Units.All(unit => !cached.Contains(unit.Id)),
                "Paid Rally must exclude newly summoned and other-floor actors.");
        }
        int[] gold = [115, 5, 10, 65, 130];
        for (int i = 0; i < operations.Length; i++)
        {
            var before = operations[i].GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = operations[i].GetProperty("After").Deserialize<RoomCombatState>()!;
            Require(after.Context!.Gold - before.Context!.Gold == gold[i], "Native reward/once/condition coverage changed.");
        }
        var enemyGrowth = operations[^1];
        int enemyId = enemyGrowth.GetProperty("ActorId").GetInt32();
        var enemyBefore = enemyGrowth.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Single(unit => unit.Id == enemyId);
        var enemyAfter = enemyGrowth.GetProperty("After").Deserialize<RoomCombatState>()!.Units.Single(unit => unit.Id == enemyId);
        Require(enemyAfter.Team == CombatTeam.Enemy && enemyAfter.Status("armor")!.Stacks == (enemyBefore.Status("armor")?.Stacks ?? 0) + 4,
            "Last-spawned override must bypass ordinary player-team filters and execute all four repeated upgrades.");
        Parallel.For(0, 32, _ =>
        {
            foreach (var sample in operations) VerifyOperation(sample, triggers);
            foreach (var sample in phases) VerifyPhase(sample);
            foreach (var sample in triggers) VerifyTrigger(sample);
        });
        Console.WriteLine($"NATIVE-RALLY-CHECKS PASS: {operations.Length} complete Horde operations, {phases.Length} paid team phases, {triggers.Length} exact dispatches, last-spawned timing/overrides, silence/once/conditions and 32 branches.");
    }

    private static void VerifyOperation(FixtureValue sample, FixtureValue[] triggers)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        int actorId = sample.GetProperty("ActorId").GetInt32(), amount = sample.GetProperty("Amount").GetInt32();
        string label = Label(sample), parent = Serialize(before);
        var actor = before.Units.Single(unit => unit.Id == actorId);
        var result = sample.GetProperty("Remove").GetBoolean()
            ? AbilityCooldownModel.RemoveStatus(before, actorId, "horde", amount)
            : StatusApplicationModel.ApplyRetained(before, actorId,
                (actor.RegisteredStatus("horde") ?? before.Context!.StatusRules.Single(rule => rule.Id == "horde")).WithStacks(amount),
                0, allowModification: false);
        Require(result.Supported, label + " API unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("AfterApi").Deserialize<RoomCombatState>(), label + " API");
        var callbacks = result.PendingCallbacks.ToList();
        Require(callbacks.Count == sample.GetProperty("QueueAfterApi").GetInt32(), label + " accepted queue count differs.");
        var dispatches = new List<(int Actor, int Count, int Override)>();
        Require(RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
        {
            if (queued.Kind == "CardMonsterPlayed" && queued.Unit.Triggers.Any(trigger => trigger.Kind == queued.Kind))
                dispatches.Add((queued.Unit.Id, queued.TriggerCount, queued.LastSpawnedOverrideUnitId));
            result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, callbacks.Add);
            return result.Supported;
        }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; }),
            label + " queue unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), label + " drain");
        var nativeDispatches = triggers.Where(item => Label(item) == label).Select(item =>
            (item.GetProperty("Actor").Deserialize<CombatUnit>()!.Id, item.GetProperty("TriggerCount").GetInt32(),
                item.GetProperty("LastSpawnedOverrideUnitId").GetInt32()));
        Require(dispatches.SequenceEqual(nativeDispatches), label + " Rally dispatch order/payload differs.");
        Require(sample.GetProperty("QueueAfter").GetInt32() == 0 && Serialize(before) == parent,
            label + " queue did not settle or parent changed.");
    }

    internal static void VerifyPhase(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<TrainCombatState>()!;
        string parent = Serialize(before);
        var result = CardPlayedTriggerModel.Rally(before, sample.GetProperty("Team").Deserialize<CombatTeam>(),
            sample.GetProperty("CachedUnitIds").Deserialize<int[]>()!);
        Require(result.Supported, Label(sample) + " team phase unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<TrainCombatState>(), Label(sample) + " team phase");
        Require(sample.GetProperty("Completed").GetBoolean() && Serialize(before) == parent, "Rally team phase incomplete or parent changed.");
    }

    internal static void VerifyTrigger(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actor = sample.GetProperty("Actor").Deserialize<CombatUnit>()!;
        string parent = Serialize(new { before, actor });
        var queued = new RoomCombatModel.QueuedCharacterTrigger(before.RoomIndex, actor, "CardMonsterPlayed",
            paramInt: sample.GetProperty("ParamInt").GetInt32(), canFireTriggers: sample.GetProperty("CanFire").GetBoolean(),
            triggerCount: sample.GetProperty("TriggerCount").GetInt32(),
            lastSpawnedOverrideUnitId: sample.GetProperty("LastSpawnedOverrideUnitId").GetInt32());
        var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before, queued, _ => { });
        Require(result.Supported, Label(sample) + " dispatch unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), Label(sample) + " dispatch room");
        Compare(queued.Unit, sample.GetProperty("AfterActor").Deserialize<CombatUnit>(), Label(sample) + " dispatch actor");
        Require(sample.GetProperty("Completed").GetBoolean() && Serialize(new { before, actor }) == parent,
            "Rally dispatch incomplete or parent changed.");
    }
    private static string Label(FixtureValue sample) => sample.GetProperty("Label").GetString()!;
    private static void Compare<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
