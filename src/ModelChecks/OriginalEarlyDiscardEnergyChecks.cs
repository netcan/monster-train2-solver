using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalEarlyDiscardEnergyChecks
{
    internal const string DataId = "44149a09-df55-4864-b5d9-f30c4e659999";
    private const string Effect = "RelicEffectEnergyOnCardEarlyDiscard";

    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(701);
        RelicConditionState once = new(new CardStatisticQuery("TimesDiscarded", "ThisBattle"), true, 0, 1, true);
        RelicResourceEffect limited = new(0, Effect, 2, 0, 2, false, conditions: [once]);
        CombatRelicState relic = new(DataId, "EmberOnEarlyDiscard", [Effect], disallowedInPlacementPhase: false,
            resourceEffects: [limited]);
        CombatContext Context(int energy, bool placementBlocked = false) => new(new([], [], [], rng, 0, []), rng, 0, 4, 10,
            queryFrame: new StatisticQueryFrame(energy: energy, runningCombat: true),
            energyState: new BattleEnergyState(3, 0, 0, "Combat", true),
            relics: [placementBlocked ? new CombatRelicState(DataId, "EmberOnEarlyDiscard", [Effect],
                disallowedInPlacementPhase: true, resourceEffects: [limited]) : relic]);

        CombatContext source = Context(0);
        RelicEarlyDiscardEnergyResult ordinary = RelicEarlyDiscardEnergyModel.CardDiscarded(source, triggeredByCard: false);
        Require(ordinary.Supported && ordinary.Events.Count == 0 && ordinary.Context!.QueryFrame!.Energy == 0,
            "A non-card-triggered discard granted energy.");
        RelicEarlyDiscardEnergyResult first = RelicEarlyDiscardEnergyModel.CardDiscarded(source, triggeredByCard: true);
        Require(first.Supported && first.Events.Count == 1 && first.Context!.QueryFrame!.Energy == 2 &&
            first.Context.Relics!.Single().ResourceEffects!.Single().Conditions.Single().DurationTriggerCount == 1,
            "The triggered discard failed to grant energy or record its condition.");
        RelicEarlyDiscardEnergyResult limitedOut = RelicEarlyDiscardEnergyModel.CardDiscarded(first.Context, triggeredByCard: true);
        Require(limitedOut.Supported && limitedOut.Events.Count == 0 && limitedOut.Context!.QueryFrame!.Energy == 2,
            "The native trigger-count condition allowed an extra early-discard activation.");

        RelicResourceEffect unlimited = new(0, Effect, 2, 0, 2, false);
        CombatRelicState uncapped = new(DataId, "EmberOnEarlyDiscard", [Effect], disallowedInPlacementPhase: false,
            resourceEffects: [unlimited]);
        CombatContext atCap = new(new([], [], [], rng, 0, []), rng, 0, 4, 10,
            queryFrame: new StatisticQueryFrame(energy: 3, runningCombat: true),
            energyState: new BattleEnergyState(3, 0, 0, "Combat", true), relics: [uncapped]);
        RelicEarlyDiscardEnergyResult capped = RelicEarlyDiscardEnergyModel.CardDiscarded(atCap, triggeredByCard: true);
        Require(capped.Supported && capped.Events.Count == 1 && capped.Events[0].EnergyBefore == 3 &&
            capped.Events[0].EnergyAfter == 3, "A capped AddEnergy call was incorrectly suppressed.");
        RelicEarlyDiscardEnergyResult placement = RelicEarlyDiscardEnergyModel.CardDiscarded(Context(0, placementBlocked: true),
            triggeredByCard: true, placement: true);
        Require(placement.Supported && placement.Events.Count == 0 && placement.Context!.QueryFrame!.Energy == 0,
            "A relic disallowed during placement triggered on a deployment discard.");
        RelicEarlyDiscardEnergyResult preview = RelicEarlyDiscardEnergyModel.CardDiscarded(atCap, triggeredByCard: true, preview: true);
        Require(preview.Supported && preview.Events.Count == 0 && preview.Context!.QueryFrame!.Energy == 3,
            "The preview-disabled native effect was applied during preview.");
        Console.WriteLine("ORIGINAL-EARLY-DISCARD-ENERGY-CHECKS PASS: trigger gate, condition recording/count limit, energy cap, placement exclusion and preview gate.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 170 &&
            root.GetProperty("OriginalEarlyDiscardEnergyPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "hand-removal" &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "The original early-discard native battle is incomplete or mislabeled.");

        HandRemovalChecks.Native(root);
        FixtureValue[] records = root.GetProperty("HandRemovals").EnumerateArray().ToArray();
        FixtureValue[] discarded = records.Where(record => record.GetProperty("Mode").GetInt32() == 0).ToArray();
        FixtureValue[] consumed = records.Where(record => record.GetProperty("Mode").GetInt32() == 1).ToArray();
        Require(discarded.Length > 0 && consumed.Length > 0, "The native fixture lacks both discard and consume controls.");
        CombatContext before = discarded[0].GetProperty("Before").GetProperty("Context").Deserialize<CombatContext>()!;
        CombatRelicState owner = before.Relics!.Single(relic => relic.DataId == DataId);
        RelicResourceEffect rule = owner.ResourceEffects!.Single(effect => effect.Type == Effect);
        Require(owner.AssetKey == "EmberOnEarlyDiscard" && rule.Amount == rule.ParamInt && rule.Conditions.All(condition =>
            condition.Comparator is >= 0 and <= 7), "The captured original relic parameters or conditions are malformed.");

        int gained = discarded.Count(record =>
        {
            CombatContext from = record.GetProperty("Before").GetProperty("Context").Deserialize<CombatContext>()!;
            CombatContext to = record.GetProperty("Actual").GetProperty("Context").Deserialize<CombatContext>()!;
            return from.QueryFrame?.Energy is int oldEnergy && to.QueryFrame?.Energy is int newEnergy && newEnergy > oldEnergy;
        });
        int consumedEnergyChanges = consumed.Count(record =>
        {
            CombatContext from = record.GetProperty("Before").GetProperty("Context").Deserialize<CombatContext>()!;
            CombatContext to = record.GetProperty("Actual").GetProperty("Context").Deserialize<CombatContext>()!;
            return from.QueryFrame?.Energy != to.QueryFrame?.Energy;
        });
        Require(rule.Amount > 0 && gained > 0 && consumedEnergyChanges == 0,
            "Native card-triggered discard did not grant energy, or consume incorrectly invoked the discard relic.");
        Console.WriteLine($"ORIGINAL-EARLY-DISCARD-ENERGY PASS: native +{rule.Amount} per triggered card discard across {gained} gaining steps, with no energy from {consumed.Length} consume controls; cap, placement and preview gates are covered by pure checks.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
