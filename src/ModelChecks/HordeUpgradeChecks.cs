using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HordeUpgradeChecks
{
    private sealed record Dispatch(int ActorId, string Kind, int DyingId, int TriggerCount);
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("HordeUpgradeOperations", out var records) || records.GetArrayLength() == 0) return;
        var cases = records.EnumerateArray().ToArray();
        Require(cases.Select(sample => sample.GetProperty("Label").GetString()).SequenceEqual([
            "health-before-growth", "remove-health-before-troops", "grow-for-two-casualties", "two-casualties-before-growth",
            "remove-negative-health-upgrade", "unhealed-maximum-add", "unhealed-maximum-remove", "attributed-health-add",
            "attributed-health-casualty", "negative-troop-add",
            "negative-troop-remove", "zero-troop-upgrade", "first-horde-resets-upgraded-stats", "raw-final-troops-upgrade",
            "unhealed-lethal-skips-status", "queued-horde-upgrade"]), "Missing Horde upgrade API/trigger boundary coverage.");
        foreach (var sample in cases) Verify(sample);
        var phases = fixture.GetProperty("HarvestTriggers").EnumerateArray().ToArray();
        foreach (var phase in phases) HarvestChecks.VerifyPhase(phase);
        var byLabel = cases.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        var dual = cases[3];
        var before = dual.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var after = dual.GetProperty("After").Deserialize<RoomCombatState>()!;
        var actor = dual.GetProperty("AfterActor").Deserialize<CombatUnit>()!;
        Require(after.Context!.Statistics!.MonstersDeadThisBattle - before.Context!.Statistics!.MonstersDeadThisBattle == 2 &&
            actor.Status("horde")!.Stacks == 3 && actor.Health == 70 && actor.MaxHealth == 70,
            "Native two HP steps did not settle two casualties before the final troop addition.");
        var first = byLabel["first-horde-resets-upgraded-stats"].GetProperty("AfterActor").Deserialize<CombatUnit>()!;
        Require(first.Modifiers!.AttackDamage == first.HordeDefinition!.Attack * 2 && first.Modifiers.AttackDamageAdded == 9 &&
            first.Health == first.HordeDefinition.Health * 2 && first.MaxHealth == first.Health,
            "First native Horde installation did not override the earlier live numeric upgrade.");
        var attributed = byLabel["attributed-health-casualty"];
        var attributedActor = attributed.GetProperty("AfterActor").Deserialize<CombatUnit>()!;
        Require(attributed.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Single(unit => unit.Id == attributedActor.Id)
            .Modifiers!.HealthFromUpgrades.Count == 1 && attributedActor.Modifiers!.HealthFromUpgrades.Count == 0 &&
            attributedActor.Status("horde")!.Stacks == 1 && attributedActor.Health == 25 && attributedActor.MaxHealth == 25,
            "Attributed maximum-health removal did not settle its own casualty step.");
        var raw = byLabel["raw-final-troops-upgrade"].GetProperty("AfterActor").Deserialize<CombatUnit>()!;
        var lethal = byLabel["unhealed-lethal-skips-status"].GetProperty("AfterActor").Deserialize<CombatUnit>()!;
        Require(raw.Health == 0 && raw.DeathState!.HasStatisticsListener && !raw.DeathState.HasFinishedDying &&
            !raw.DeathState.IsBeingRemoved && lethal.Health == 0 && lethal.Status("horde")!.Stacks == 1 &&
            lethal.DeathState!.HasFinishedDying && !lethal.DeathState.HasStatisticsListener,
            "Raw troop removal and lethal unhealed-HP sacrifice did not exercise distinct death paths.");
        var queuedUpgrade = byLabel["queued-horde-upgrade"];
        Require(queuedUpgrade.GetProperty("After").Deserialize<RoomCombatState>()!.Context!.FindCard(
            queuedUpgrade.GetProperty("AfterActor").Deserialize<CombatUnit>()!.SpawnerCardId)!.Temporary.Upgrades.Any(upgrade =>
                upgrade.DataId == queuedUpgrade.GetProperty("UpgradeId").GetString()), "Queued upgrade did not write back to its actual spawner.");
        int paidPlays = 0;
        foreach (var entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            var decision = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState? card = decision.Spawn.Train.Context!.FindCard(action.CardInstanceId);
            CardPlayRule? rule = decision.PlayRules!.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
            if (rule?.Effects.Any(effect => effect.Upgrade?.DataId == "c2f6ed7f-18ce-4070-b65f-7dd9f5170010") != true) continue;
            var settled = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(settled.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.LastPlayedCost > 0 &&
                rule.Effects.Count(effect => effect.Upgrade?.Statuses.Any(status => status.Id == "horde") == true) == 4,
                "Native upgrade chain did not execute as a real paid card play.");
            paidPlays++;
        }
        Require(paidPlays > 0, "Missing actual paid Horde upgrade/removal spells.");
        Parallel.For(0, 32, _ => { foreach (var sample in cases) Verify(sample); foreach (var phase in phases) HarvestChecks.VerifyPhase(phase); });
        Console.WriteLine($"NATIVE-HORDE-UPGRADE-CHECKS PASS: {cases.Length} complete API/trigger/retained-actor states, {phases.Length} death/Harvest phases, {paidPlays} paid upgrade chains, ordered casualties, raw removal, exact dispatches and 32 branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        string parent = Serialize(before), label = sample.GetProperty("Label").GetString()!;
        int id = sample.GetProperty("ActorId").GetInt32();
        var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
        bool triggered = sample.TryGetProperty("Kind", out var kind) && kind.GetString() == "Trigger";
        var result = triggered ? RoomCombatModel.ApplyQueuedCharacterTrigger(before,
            new(before.RoomIndex, before.Units.Single(unit => unit.Id == id), "PreCombat"), pending.Add) :
            UnitModifierModel.ApplyDirect(before, id, sample.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!,
                sample.GetProperty("Remove").GetBoolean(), sample.GetProperty("UpgradeId").GetString()!);
        Require(result.Supported, label + ": " + result.UnsupportedReason);
        var dispatches = result.Dispatches.ToList();
        var retained = result.RetainedUnits.ToDictionary(unit => unit.Id);
        var callbacks = triggered ? pending : result.PendingCallbacks.ToList();
        if (!triggered)
        {
            Compare(result.State, sample.GetProperty("AfterApi").Deserialize<RoomCombatState>(), label + " API boundary");
            Require(callbacks.Count == sample.GetProperty("QueueAfterApi").GetInt32(), label + " API queue count differs.");
        }
        Require(RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
        {
            result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, callbacks.Add);
            dispatches.AddRange(result.Dispatches);
            foreach (var unit in result.RetainedUnits) retained[unit.Id] = unit;
            return result.Supported;
        }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; }),
            label + " queue: " + result.UnsupportedReason);
        Compare(result.State, sample.GetProperty("After").Deserialize<RoomCombatState>(), label + " settled room");
        var changed = result.State!.Units.FirstOrDefault(unit => unit.Id == id) ?? retained.GetValueOrDefault(id);
        Require(changed != null, label + " lost the retained target.");
        Compare(changed, sample.GetProperty("AfterActor").Deserialize<CombatUnit>(), label + " retained target");
        var nativeDispatches = sample.GetProperty("Dispatched").EnumerateArray().Select(item => new Dispatch(
            item.GetProperty("ActorId").GetInt32(), item.GetProperty("Kind").GetString()!,
            item.GetProperty("DyingId").GetInt32(), item.GetProperty("TriggerCount").GetInt32())).ToArray();
        Compare(dispatches.Select(item => new Dispatch(item.ActorId, item.Kind, item.DyingId, item.TriggerCount)).ToArray(),
            nativeDispatches, label + " dispatch order");
        Require(Serialize(before) == parent, "Horde upgrade mutated its parent.");
    }
    private static void Compare<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
