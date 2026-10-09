using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HordeStatusChecks
{
    internal static void Run()
    {
        CombatStatus horde = new("horde", 2, stackable: true, hidden: false, displayCategory: "Persistent");
        CombatStatus cooldown = new("cooldown", 0, stackable: true, hidden: true, displayCategory: "Persistent");
        var rng = UnityRng.Seed(71);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, [horde, cooldown], BattleStatistics.Empty());
        var actor = new CombatUnit(1, "horde", CombatTeam.Player, 19, 50, 50, true, false, false, [horde],
            modifiers: new(16, 9, 3, 1, 0, true, false, []), ability: new("skill", 3, 2),
            hordeDefinition: new(8, 25), isSpawning: false);
        var friend = new CombatUnit(2, "friend", CombatTeam.Player, 1, 20, 20, true, false, false, []);
        var enemy = new CombatUnit(3, "enemy", CombatTeam.Enemy, 1, 20, 20, true, false, false, [new("untouchable", 1)]);
        var root = new RoomCombatState(0, false, [actor, friend, enemy], [], context);
        string parent = Serialize(root);
        var grown = StatusApplicationModel.Apply(root, 1, horde.WithStacks(2), allowModification: false);
        Require(grown.Supported, "Horde growth unsupported: " + grown.UnsupportedReason);
        var troop = grown.State!.Units[0];
        Require(troop.Modifiers!.AttackDamage == 32 && troop.BaseAttack == 35 && troop.Modifiers.AttackDamageAdded == 9 &&
            troop.Health == 100 && troop.MaxHealth == 100 && troop.HordeDefinition!.Health == 25 && troop.IsSpawning == false,
            "Growth lost authored stats, independent attack buff bookkeeping or spawning metadata.");
        Require(grown.PendingCallbacks.Select(item => item.Kind).SequenceEqual([
            "OnSpawn", "OnUnscaledSpawn", "OnSpawnNotFromCard", "CardMonsterPlayed", "CardMonsterPlayed",
            "OnStatusEffectChanged", "OnTroopAdded"]) && grown.PendingCallbacks[3].Unit.Id == 3 &&
            grown.PendingCallbacks[4].Unit.Id == 2 && grown.PendingCallbacks.Skip(3).Take(2).All(item =>
                item.TriggerCount == 2 && item.LastSpawnedOverrideUnitId == 1),
            "Growth lost enemy-first observers, untouchable actors, repeated rally count or spawned override.");
        var zero = StatusApplicationModel.Apply(grown.State, 1, horde.WithStacks(0), allowModification: false);
        var negative = StatusApplicationModel.Apply(grown.State, 1, horde.WithStacks(-1), allowModification: false);
        Require(zero.PendingCallbacks.Count == 2 && zero.State!.Units[0].Modifiers!.AttackDamage == 32 &&
            negative.State!.Units[0].Status("horde")!.Stacks == 3 && negative.State.Units[0].Health == 100 &&
            negative.State.Units[0].Modifiers!.AttackDamage == 32 && negative.PendingCallbacks[^1].ParamInt2 == -1,
            "Zero/negative native additions must queue troop notifications without changing raw Horde stats.");
        var removed = AbilityCooldownModel.RemoveStatus(grown.State, 1, "horde", 1);
        var final = AbilityCooldownModel.RemoveStatus(root, 1, "horde", -1);
        Require(final.Supported && final.State!.Units.All(unit => unit.Id != 1) && final.PendingCallbacks.All(item => item.Kind != "OnDeath") &&
            final.State.Context!.Statistics!.MonstersDeadThisBattle == 2,
            "Final raw troop removal must not invent a physical death signal or OnDeath callback.");
        Require(removed.State!.Units[0].Health == 75 && removed.State.Units[0].Modifiers!.AttackDamage == 24 &&
            removed.State.Context!.Statistics!.MonstersDeadThisBattle == 1 && removed.PendingCallbacks.Count == 6 &&
            removed.PendingCallbacks.Take(4).Select(item => item.Kind).SequenceEqual([
                "OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor", "OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor"]) &&
            removed.PendingCallbacks.Take(4).All(item => item.DyingCharacter!.Id == 1 && item.TriggerCount == 1),
            "Troop removal lost simulated death statistics or ordered harvest payloads.");
        var wounded = new RoomCombatState(0, false, grown.State.Units.Select(unit => unit.Id == 1
            ? CardSpellModel.Copy(unit, 49, unit.Statuses) : unit).ToArray(), [], grown.State.Context);
        var casualties = HordeStatusModel.SettleHealth(wounded, 1);
        Require(casualties.State!.Units[0].Health == 49 && casualties.State.Units[0].MaxHealth == 50 &&
            casualties.State.Units[0].Status("horde")!.Stacks == 2 && casualties.State.Context!.Statistics!.MonstersDeadThisBattle == 2 &&
            casualties.PendingCallbacks.Take(4).All(item => item.TriggerCount == 2),
            "Casualty settlement changed current HP or collapsed repeated simulated deaths.");
        var preview = HordeStatusModel.SettleHealth(new(0, false, wounded.Units, [], wounded.Context, preview: true), 1);
        Require(preview.State!.Context!.Statistics!.MonstersDeadThisBattle == 0, "Preview incremented live Horde death statistics.");
        var reset = new CardActionEffect("ResetCooldown", "Self", 0, true, false, [], cooldownParameter: true);
        Require(AbilityCooldownModel.Apply(root, 1, reset, triggerKind: "OnSpawn").State!.Units[0].Status("cooldown") == null &&
            AbilityCooldownModel.Apply(new(0, false, [HordeStatusModel.WithSpawning(actor, true)], [], context), 1, reset,
                triggerKind: "OnSpawn").State!.Units[0].Status("cooldown")!.Stacks == 2 &&
            AbilityCooldownModel.Apply(root, 1, reset, triggerKind: "OnUnscaledSpawn").State!.Units[0].Status("cooldown")!.Stacks == 2,
            "Only an actual Horde OnSpawn outside initial spawning may suppress cooldown reset.");
        string expected = Serialize(grown.State);
        Parallel.For(0, 32, _ => Require(Serialize(StatusApplicationModel.Apply(root, 1, horde.WithStacks(2),
            allowModification: false).State) == expected, "Parallel Horde growth differs."));
        Require(Serialize(root) == parent, "Horde transitions mutated the parent state.");
        Console.WriteLine("HORDE-STATUS-CHECKS PASS: signed additions, raw stats, troop/rally/harvest payloads, casualties, simulated deaths, preview/cooldown gates and 32 branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("HordeStatusOperations", out var records) || records.GetArrayLength() == 0) return;
        var samples = records.EnumerateArray().ToArray();
        Require(samples.Select(sample => sample.GetProperty("Label").GetString()).SequenceEqual([
            "cooldown-ready", "troops-grow", "zero-add", "negative-add", "troops-remove", "damage-casualty", "troops-regrow", "maxhp-casualty"]),
            "Missing native Horde status boundary cases.");
        int[] counts = [2, 4, 4, 3, 2, 1, 3, 2], attacks = [16, 32, 32, 32, 24, 16, 32, 24];
        int[] hp = [50, 100, 100, 100, 75, 24, 74, 44], max = [50, 100, 100, 100, 75, 50, 100, 45], deaths = [0, 0, 0, 0, 1, 2, 2, 3];
        for (int i = 0; i < samples.Length; i++)
        {
            Require(samples[i].GetProperty("QueueAfterDrain").GetInt32() == 0 &&
                !samples[i].GetProperty("RunningBeforeDrain").GetBoolean() &&
                !samples[i].GetProperty("ActorPreviewBeforeDrain").GetBoolean() &&
                !samples[i].GetProperty("SavePreviewBeforeDrain").GetBoolean(), "Native Horde operation was not isolated from UI preview/other queues.");
            Verify(samples[i]);
            var after = samples[i].GetProperty("AfterDrain").Deserialize<RoomCombatState>()!;
            var actor = after.Units.Single(unit => unit.Id == samples[i].GetProperty("ActorId").GetInt32());
            Require(actor.Status("horde")!.Stacks == counts[i] && actor.Modifiers!.AttackDamage == attacks[i] &&
                actor.Health == hp[i] && actor.MaxHealth == max[i] && actor.Status("cooldown") == null && actor.IsSpawning == false &&
                after.Context!.Statistics!.MonstersDeadThisBattle == deaths[i], "Native Horde case did not exercise expected boundary " + i);
        }
        Require(samples[1].GetProperty("Callbacks").EnumerateArray().Any(item => item.GetProperty("Kind").GetString() == "CardMonsterPlayed" &&
                item.GetProperty("TriggerCount").GetInt32() == 2 && item.GetProperty("LastSpawnedOverrideUnitId").GetInt32() == samples[1].GetProperty("ActorId").GetInt32()) &&
            samples[4].GetProperty("Callbacks").EnumerateArray().Any(item => item.GetProperty("Kind").GetString() == "OnAnyUnitDeathOnFloor" &&
                item.GetProperty("DyingId").GetInt32() == samples[4].GetProperty("ActorId").GetInt32()), "Native observers did not receive actual rally/harvest payloads.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine("NATIVE-HORDE-STATUS-CHECKS PASS: eight exact operation/queue/drain states, signed troop notifications, damage/max-health casualties, death statistics, spawning cooldown gates and 32 branches.");
    }

    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = Serialize(before), operation = sample.GetProperty("Operation").GetString()!;
        int actor = sample.GetProperty("ActorId").GetInt32(), amount = sample.GetProperty("Amount").GetInt32();
        int source = sample.GetProperty("SourceCardId").GetInt32();
        string status = sample.GetProperty("StatusId").GetString()!;
        RoomCombatResult result = operation switch
        {
            "Add" => StatusApplicationModel.ApplyRetained(before, actor,
                (before.Units.Single(unit => unit.Id == actor).RegisteredStatus(status) ??
                    before.Context!.StatusRules.Single(rule => rule.Id == status)).WithStacks(amount), source, allowModification: false),
            "Remove" => AbilityCooldownModel.RemoveStatus(before, actor, status, amount, source),
            "Damage" => RoomCombatModel.ApplyCardDamage(before, actor, amount, source),
            "DebuffHealth" => UnitHealthModel.Apply(before, actor, amount, debuff: true),
            _ => throw new InvalidOperationException("Unknown native Horde operation.")
        };
        Require(result.Supported, "Native Horde operation unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After"), sample.GetProperty("Label").GetString() + " before drain");
        var callbacks = result.PendingCallbacks.ToList();
        if (operation != "Damage")
        {
            var payloads = callbacks.Select(item => new { ActorId = item.Unit.Id, item.Kind, item.ParamInt, item.ParamInt2,
                item.ParamString, item.TriggerCount, DyingId = item.DyingCharacter?.Id ?? 0, item.LastSpawnedOverrideUnitId }).ToArray();
            string? difference = ModelJson.Difference(Serialize(payloads), Serialize(sample.GetProperty("Callbacks").ToObjectGraph()));
            Require(difference == null, sample.GetProperty("Label").GetString() + " native queued payload differs: " + difference);
            difference = ModelJson.Difference(Serialize(payloads), Serialize(sample.GetProperty("Queued").ToObjectGraph()));
            Require(difference == null, sample.GetProperty("Label").GetString() + " native accepted queue differs: " + difference);
        }
        Require(RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
        {
            result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, callbacks.Add); return result.Supported;
        }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; },
            () => { result = EnchantmentWorldModel.CompleteQueuedRemovals(result.State!, callbacks.Add); return result.Supported; }, () => result.State?.Context, message => result = new RoomCombatResult(null, RoomOutcome.Unsupported, 0, [], message)),
            "Native Horde queue unsupported: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("AfterDrain"), sample.GetProperty("Label").GetString() + " after drain");
        var dispatched = callbacks.Select(item => new { ActorId = item.Unit.Id, item.Kind, item.ParamInt, item.ParamInt2,
            item.ParamString, item.TriggerCount, DyingId = item.DyingCharacter?.Id ?? 0, item.LastSpawnedOverrideUnitId }).ToArray();
        string? dispatchDifference = ModelJson.Difference(Serialize(dispatched), Serialize(sample.GetProperty("Dispatched").ToObjectGraph()));
        Require(dispatchDifference == null, sample.GetProperty("Label").GetString() + " native drain order differs: " + dispatchDifference);
        Require(Serialize(before) == parent, "Native Horde transition mutated its parent.");
    }
    private static void Compare(RoomCombatState? predicted, FixtureValue actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual.Deserialize<RoomCombatState>()));
        Require(difference == null, label + ": " + difference);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
