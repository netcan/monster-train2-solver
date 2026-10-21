using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetiredUnitDestructionChecks
{
    internal static void Native(FixtureValue fixture)
    {
        int cleared = 0, living = 0;
        foreach (FixtureValue captured in fixture.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = captured.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = captured.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string original = JsonSerializer.Serialize(before);
            BattleTurnResult resolved = BattleTurnModel.EndTurn(before);
            Require(resolved.Supported, resolved.UnsupportedReason ?? "EndTurn failed.");
            foreach (CombatUnit unit in before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Modifiers?.Upgrades.Count > 0))
            {
                CombatUnit? retired = after.Spawn.Train.Context?.Enchantments?.RetainedUnits
                    .Select(actor => actor.Unit).SingleOrDefault(actor => actor.Id == unit.Id);
                if (retired?.DeathState?.IsDestroyed == true)
                {
                    Require(retired.Modifiers!.Upgrades.Count == 0 && retired.Modifiers.HealthFromUpgrades.Count == 0,
                        "Native destroyed actor retained upgrade collections.");
                    cleared++;
                }
                if (after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(actor => actor.Id == unit.Id && actor.Modifiers?.Upgrades.Count > 0)) living++;
            }
            Require(JsonSerializer.Serialize(before) == original, "Destruction mutated the source snapshot.");
        }
        Require(cleared >= 3 && living > 0, "Missing three native destruction witnesses or living upgrade control.");
        BattleTurnChecks.Native(fixture);
        Console.WriteLine($"RETIRED-UNIT-DESTRUCTION PASS: {cleared} native cleared actors, {living} living upgrade controls, exact EndTurn/context/RNG and immutable roots.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
