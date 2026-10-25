using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SlayEnchantmentOrderChecks
{
    private const string Reason = "Observed Ring8 retained attack value";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 31 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Changed natural Silence/Slay boundary.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit unit = before.Spawn.Train.Rooms[0].Units.Single(actor => actor.Id == 23);
        Require(before.Spawn.Turn == 5 && unit.AssetKey == "MonsterFirebrand" && unit.BaseAttack == 51 &&
            unit.Status("silenced")?.Stacks == 1 && unit.Status("ambush")?.Stacks == 1 &&
            unit.Triggers.Any(trigger => trigger.Kind == "OnKill" && !trigger.IgnoreSilence) &&
            before.Spawn.Train.Rooms[0].Units.First().AssetKey == "EnchanterT3_Silence",
            "Missing original silenced Quick attacker and dying enchanter.");
        Corrected(actual);
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel Slay/Enchantment replay differs from complete native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Slay replay mutated its parent.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason, Historical);
            NativeBranchObservationChecks.Pair(source, second, originalPredictionPresent: true);
        }
        Console.WriteLine("SLAY-ENCHANTMENT-ORDER PASS:31 native inputs/full state/RNG; dying Silence aura still blocks character Slay before its update; native sword growth preserved; eight immutable parallel branches; independentPair=" + replay.HasValue);
    }

    private static void Historical(BattleTurnState old, BattleTurnState actual)
    {
        CombatUnit oldUnit = old.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 23);
        CombatUnit native = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 23);
        Require(oldUnit.BaseAttack == 52 && native.BaseAttack == 51 && oldUnit.Modifiers!.Upgrades.Count == native.Modifiers!.Upgrades.Count + 1,
            "Missing original extra weapon Slay growth counterexample.");
        Corrected(actual);
    }

    private static void Corrected(BattleTurnState actual)
    {
        CombatUnit unit = actual.Spawn.Train.Rooms[0].Units.Single(actor => actor.Id == 23);
        Require(unit.Health == 10 && unit.BaseAttack == 51 && unit.Attack == 57 && unit.Status("silenced") == null,
            "Natural end-turn attack or completed Silence removal differs.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
