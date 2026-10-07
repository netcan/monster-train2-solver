using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DyingHordeUpgradeChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("DyingHordeUpgradeOperations", out var records) || records.GetArrayLength() == 0) return;
        var cases = records.EnumerateArray().ToArray();
        Require(cases.Select(sample => sample.GetProperty("Label").GetString()).SequenceEqual([
            "dying-player-ordinary-hp", "dying-enemy-unhealed-hp", "dying-player-remove-hp",
            "dying-enemy-remove-unhealed", "dying-enemy-remove-attributed"]), "Missing dying Horde HP/removal cases.");
        var effects = fixture.GetProperty("DyingUpgrades").EnumerateArray().Where(sample =>
            sample.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>() is [ { Health: 0 } ]).ToArray();
        var phases = fixture.GetProperty("HarvestTriggers").EnumerateArray().ToArray();
        Require(effects.Length == 7 && phases.Length > 0, "Missing native dying upgrade effects or death/Harvest phases.");
        foreach (var effect in effects) VerifyEffect(effect);
        foreach (var sample in cases)
        {
            var actor = sample.GetProperty("AfterActor").Deserialize<CombatUnit>()!;
            Require(actor.Health == 0 && actor.Status("horde")!.Stacks == 1 &&
                actor.DeathState is { HasFinishedDying: true, IsBeingRemoved: true, HasStatisticsListener: false },
                "A dying HP step revived its Horde actor or skipped casualties.");
            HordeDeathChecks.VerifyOperation(sample);
        }
        foreach (var phase in phases) HarvestChecks.VerifyPhase(phase);
        var added = effects.Where(sample => sample.GetProperty("Effect").Deserialize<CardActionEffect>()!.Type != "RemoveUnitUpgrade").ToArray();
        Require(added.Count(sample => sample.GetProperty("Effect").Deserialize<CardActionEffect>()!.Upgrade!.Stats.Health < 0) == 1 &&
            added.Count(sample => sample.GetProperty("Effect").Deserialize<CardActionEffect>()!.Upgrade!.UnhealedHealth < 0) == 1 &&
            effects.Count(sample => sample.GetProperty("Effect").Deserialize<CardActionEffect>()!.Type == "RemoveUnitUpgrade") == 3,
            "Dying Horde fixture lacks ordinary/unhealed failed additions and all three removal steps.");
        Parallel.For(0, 32, _ =>
        {
            foreach (var effect in effects) VerifyEffect(effect);
            foreach (var sample in cases) HordeDeathChecks.VerifyOperation(sample);
            foreach (var phase in phases) HarvestChecks.VerifyPhase(phase);
        });
        Console.WriteLine($"NATIVE-DYING-HORDE-UPGRADE-CHECKS PASS: {cases.Length} complete death operations, {effects.Length} dying effect/source states, {phases.Length} death/Harvest phases, accepted queues and 32 branches.");
    }
    private static void VerifyEffect(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actor = sample.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>()!.Single();
        var effect = sample.GetProperty("Effect").Deserialize<CardActionEffect>()!;
        var after = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        var actual = sample.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!.Single();
        string parent = Serialize(new { before, actor });
        var scope = new RoomCombatState(before.RoomIndex, before.Deployment,
            before.Units.Where(unit => unit.Id != actor.Id).Append(actor).ToArray(), before.ExternalInteractions, before.Context, before.Preview);
        var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
        CombatUnit changed = actor;
        var result = UnitModifierModel.ApplyWithSettlement(scope, actor.Id, effect.Upgrade!, effect.Lifetime,
            effect.Type == "RemoveUnitUpgrade", null, sample.GetProperty("OwnerCardId").GetInt32(), sample.GetProperty("Kind").GetString(),
            (state, unit, _) =>
            {
                changed = unit;
                return new(new(state.RoomIndex, state.Deployment, state.Units.Select(item => item.Id == unit.Id ? unit : item).ToArray(),
                    state.ExternalInteractions, state.Context, state.Preview), RoomOutcome.Exchanged, 0, []);
            }, allowDyingTarget: true, enqueueCallback: callbacks.Add);
        string label = sample.GetProperty("Label").GetString()! + " effect " + sample.GetProperty("Sequence").GetInt32();
        Require(result.Supported, label + ": " + result.UnsupportedReason);
        Compare(changed, actual, label + " actor");
        Compare(result.State!.Context, after.Context, label + " context/source");
        Require(callbacks.Count == sample.GetProperty("QueueAfter").GetInt32() - sample.GetProperty("QueueBefore").GetInt32(),
            label + " accepted callback count differs.");
        Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Interactions").GetArrayLength() == 0 &&
            Serialize(new { before, actor }) == parent, label + " incomplete or parent changed.");
    }
    private static void Compare<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
