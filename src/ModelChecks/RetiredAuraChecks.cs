using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetiredAuraChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing settled original encounter.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        var settlement = root.GetProperty("CardAnimationSettlement");
        Require(settlement.GetProperty("Enabled").GetBoolean() &&
            settlement.GetProperty("PendingMovements").GetInt32() == 0 &&
            settlement.GetProperty("RetainedRemovedSourceCount").GetInt32() > 0,
            "Missing native retained-source observation with full animation settlement enabled.");
        int retained = 0, destroyed = 0;
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var state = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            retained += Auras(state).Count(rule => rule.Bound);
        }
        foreach (var row in root.GetProperty("Turns").EnumerateArray())
        {
            var state = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            destroyed += Auras(state).Count(rule => !rule.Bound);
        }
        Require(retained > 0 && destroyed > 0, "Missing mid-turn retained binding or turn-end destruction witness.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"RETIRED-AURA PASS retainedBindings={retained} destroyedBindings={destroyed}: card kills retain native aura bindings until turn-end removal; full animation settlement and immutable native chains.");
    }
    private static IEnumerable<EnchantmentRule> Auras(BattleTurnState state) =>
        (state.Spawn.Train.Context!.Enchantments?.RetainedUnits ?? Array.Empty<EnchantmentRetainedUnit>())
            .Where(actor => actor.Unit.DeathState?.IsDestroyed == true).SelectMany(actor => actor.Unit.Triggers)
            .SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null).Select(effect => effect.Enchantment!);
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
