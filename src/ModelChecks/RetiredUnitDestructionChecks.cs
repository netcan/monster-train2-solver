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
        FixtureValue settled = fixture.GetProperty("Settled");
        Require(settled.GetProperty("DeathDissolveSettlementEnabled").GetBoolean(), "Native dissolve protocol was disabled.");
        FixtureValue[] waits = settled.GetProperty("DeathDissolveSettlements").EnumerateArray().ToArray();
        Require(waits.Any(wait => wait.GetProperty("FramesWaited").GetInt32() > 0 &&
                wait.GetProperty("PendingUnitIds").GetArrayLength() > 0) &&
            waits.All(wait => wait.GetProperty("Completed").GetBoolean() && wait.GetProperty("PendingAfter").GetInt32() == 0 &&
                wait.GetProperty("Error").ValueKind == FixtureKind.Null), "Missing completed original native dissolve waits.");
        Require(settled.GetProperty("DeathDissolveCallbacks").EnumerateArray().All(callback =>
            callback.GetProperty("CompletedFrame").ValueKind == FixtureKind.Number &&
            callback.GetProperty("Error").ValueKind == FixtureKind.Null), "Original dissolve callback did not complete.");
        var auraTurn = settled.GetProperty("Turns")[1];
        var auraBefore = auraTurn.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var auraAfter = auraTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit aura = auraBefore.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment?.Bound == true));
        CombatUnit corpse = auraAfter.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(unit => unit.Unit.Id == aura.Id).Unit;
        Require(corpse.DeathState?.IsDestroyed == true && corpse.Triggers.SelectMany(trigger => trigger.Effects)
            .Any(effect => effect.Enchantment?.Bound == false), "Native enchantment did not unbind after original destruction.");
        BattleTurnChecks.Native(settled);
        Console.WriteLine($"RETIRED-UNIT-DESTRUCTION PASS: {cleared} native cleared actors, {living} living upgrade controls, exact EndTurn/context/RNG and immutable roots.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
