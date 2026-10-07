using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TriggerRepeatChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(47);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10);
        CombatTrigger Gold(bool once, bool ignore, int amount) => new("OnUnscaledSpawn", once, false, ignore, 2,
            [new("CardEffectRewardGold", amount, 0, "", 0, [], false)], false);
        var actor = new CombatUnit(1, "repeat", CombatTeam.Player, 1, 20, 20, true, false, false, [],
            [Gold(true, false, 10), Gold(false, false, 20), Gold(false, true, 30)]);
        var root = new RoomCombatState(0, false, [actor], [], context);
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        RoomCombatState Fire(RoomCombatState before, int count, bool canFire = true)
        {
            var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before,
                new(0, before.Units[0], "OnUnscaledSpawn", canFireTriggers: canFire, triggerCount: count), _ => { });
            Require(result.Supported, "Repeat phase unsupported: " + result.UnsupportedReason); return result.State!;
        }
        var first = Fire(root, 3);
        Require(first.Context!.Gold == 360 && first.Units[0].Triggers.All(trigger => trigger.HasTriggered),
            "Once flag was tested inside the repeat loop, or queue count replaced the native fire count.");
        Require(Fire(first, 2).Context!.Gold == 560, "A spent once trigger repeated in a later record.");
        foreach (int count in new[] { 0, -2, int.MaxValue })
        {
            var none = Fire(root, count);
            Require(none.Context!.Gold == 0 && none.Units[0].Triggers.All(trigger => trigger.HasTriggered) &&
                Fire(none, 1).Context!.Gold == 100, "Zero, negative or wrapped count did not retain native once marking.");
        }
        Require(Fire(root, 2, false).Context!.Gold == 120, "Explicit fire permission bypassed hidden triggers or allowed visible triggers.");
        var silent = new RoomCombatState(0, false, [CardSpellModel.Copy(actor, actor.Health, [new("silenced", 1)])], [], context);
        Require(Fire(silent, 2).Context!.Gold == 120 && !Fire(silent, 2).Units[0].Triggers[0].HasTriggered,
            "Silence consumed a visible once flag or skipped hidden repeats.");
        string expected = JsonSerializer.Serialize(first, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Fire(root, 3), ModelJson.Options) == expected, "Repeat branches differ."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Repeat phase mutated its parent.");
        Console.WriteLine("TRIGGER-REPEAT-CHECKS PASS: multiplied batches, once marking, zero/negative/overflow, silence/permissions and 32 branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("TriggerRepeatBatches", out var batches) || batches.GetArrayLength() == 0) return;
        var samples = batches.EnumerateArray().ToArray();
        var expectedGold = new Dictionary<string, int> { ["batch-first"] = 210, ["batch-once-spent"] = 100,
            ["batch-silenced"] = 120, ["batch-fire-blocked"] = 60, ["zero-first"] = 0, ["zero-after"] = 135,
            ["negative-first"] = 0, ["negative-after"] = 65 };
        Require(samples.Length == 8 && samples.Select(sample => sample.GetProperty("Label").GetString()!).ToHashSet().SetEquals(expectedGold.Keys),
            "Incomplete native repeat batch cases.");
        foreach (var sample in samples)
        {
            Verify(sample);
            var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
            string label = sample.GetProperty("Label").GetString()!;
            Require(after.Context!.Gold - before.Context!.Gold == expectedGold[label], "Native repeat case did not exercise expected behavior: " + label);
            if (label == "zero-first" || label == "negative-first")
                Require(after.Units.First(unit => unit.Id == sample.GetProperty("ActorId").GetInt32()).Triggers
                    .Where(trigger => trigger.Kind == sample.GetProperty("Kind").GetString()).All(trigger => trigger.HasTriggered),
                    "Non-positive native batch did not mark its once flags.");
        }
        var phases = fixture.GetProperty("ConditionalTriggers").EnumerateArray()
            .Where(sample => sample.GetProperty("Label").GetString() == "repeat:batch-first").ToArray();
        Require(phases.Length == 4 && phases[0].GetProperty("Kind").GetString() == "OnUnscaledSpawn" &&
            phases[0].GetProperty("TriggerCount").GetInt32() == 3 && phases.Skip(1).All(sample =>
                sample.GetProperty("Kind").GetString() == "OnArmorAdded" && sample.GetProperty("TriggerCount").GetInt32() == 1),
            "Repeated parent did not leave three separate ordered child records.");
        var phaseBefore = phases[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        var phaseAfter = phases[0].GetProperty("After").Deserialize<RoomCombatState>()!;
        Require(phaseAfter.Context!.Gold - phaseBefore.Context!.Gold == 180 && phases.Skip(1).Select(sample =>
            sample.GetProperty("After").Deserialize<RoomCombatState>()!.Context!.Gold -
                sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Context!.Gold).SequenceEqual([20, 5, 5]),
            "Child callbacks drained inside the repeated parent, or their once flag differed.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine("NATIVE-TRIGGER-REPEAT-CHECKS PASS: eight complete native batches, ordered child drain, once/zero/negative/silence/permission gates and 32 branches.");
    }

    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
        var actor = before.Units.First(unit => unit.Id == sample.GetProperty("ActorId").GetInt32());
        var result = RoomCombatModel.ApplyQueuedCharacterTrigger(before,
            new(before.RoomIndex, actor, sample.GetProperty("Kind").GetString()!, canFireTriggers: sample.GetProperty("CanFire").GetBoolean(),
                triggerCount: sample.GetProperty("TriggerCount").GetInt32()), callbacks.Add);
        Require(result.Supported, "Native repeat parent unsupported: " + result.UnsupportedReason);
        Require(RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
        {
            result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, callbacks.Add); return result.Supported;
        }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; }),
            "Native repeat child unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("After").Deserialize<RoomCombatState>(), ModelJson.Options));
        Require(difference == null, sample.GetProperty("Label").GetString() + ": " + difference);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native repeat batch mutated its root.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
