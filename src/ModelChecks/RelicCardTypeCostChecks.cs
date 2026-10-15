using MonsterTrain2Poju.Model;

internal static class RelicCardTypeCostChecks
{
    internal static void Run()
    {
        var definition = new CardMaskDefinition("cost-order-blight", "Blight", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false);
        var descriptor = new CardMaskDescriptor(definition, 3, [], []);
        var card = new CardInstanceState(1, definition.DataId, CardModifiers.Empty(), CardModifiers.Empty(),
            0, 0, 0, [], maskDescriptor: descriptor);
        CombatRelicState Cost(int amount) => new CombatRelicState("cost-relic", "CostRelic",
            [RelicModel.ModifyCardTypeCost], cardTypeCosts: [new RelicCardTypeCost(0, "Blight", amount, false, null, 0)]);
        CombatRelicState Upgrade(string id, CardStatModifier stats, int minCost, int maxCost,
            IReadOnlyList<CardLifecycleTrigger>? triggers = null)
        {
            var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade(id, stats, [], false, false, false, false),
                id, false, false, [], [], false, [], [], triggers ?? []);
            var modifier = new CardUpgradeModifier(id, id, stats, [], false, false, false, 0, 0, [],
                maskMetadata: new CardUpgradeMaskMetadata([], false, false, false, false), lifecycle: lifecycle);
            return new CombatRelicState(id + "-relic", id, [RelicModel.AddTempUpgrade],
                cardModifiers: [new RelicCardModifier(0, new RelicCardUpgradeRule(id, true, lifecycle,
                    [new CardUpgradeMaskRule(id + "-filter", "Blight", minCost: minCost, maxCost: maxCost)]), modifier, false)]);
        }
        RelicCardModifierResult Apply(CardInstanceState source, IReadOnlyList<CombatRelicState> relics, bool reset = true)
        {
            RelicCardModifierResult result = RelicCardModifierModel.Apply(source, relics, reset);
            Require(result.Supported, "Supported cost/filter ordering was rejected: " + result.UnsupportedReason);
            return result;
        }

        CombatRelicState freeOnly = Upgrade("free-only", new CardStatModifier(damage: 7), 0, 0);
        CombatRelicState paidOnly = Upgrade("paid-only", new CardStatModifier(damage: 9), 1, 3);
        RelicCardModifierResult discounted = Apply(card, [Cost(-99), freeOnly, paidOnly]);
        Require(discounted.Dispatches.Select(item => item.Returned).SequenceEqual([true, true, false]) &&
            discounted.Card!.Temporary.Upgrades.Select(item => item.DataId).SequenceEqual(["free-only"]) &&
            discounted.Card.Temporary.Offsets.Cost == -99 && discounted.Card.Temporary.Offsets.XCost == 99 &&
            CardBranchMaskModel.Resolve(discounted.Card).CostWithoutTraits == 0,
            "A later relic filter did not observe the discounted cost, including its zero floor.");
        RelicCardModifierResult reversed = Apply(card, [freeOnly, Cost(-99)]);
        Require(reversed.Dispatches.Select(item => item.Returned).SequenceEqual([false, true]) &&
            reversed.Card!.Temporary.Upgrades.Count == 0 && card.Temporary.Upgrades.Count == 0 &&
            card.Temporary.Offsets.Cost == 0 && CardBranchMaskModel.Resolve(card).CostWithoutTraits == 3,
            "Relic order or source branch isolation was lost while applying a cost modifier.");

        var draw = new CardLifecycleTrigger("OnCast", "cost-order-draw", ["CardEffectDraw"],
            actions: [new CardActionEffect("Draw", "Room", 1, false, false, [])]);
        CombatRelicState earlierUpgrade = Upgrade("earlier-cost-upgrade", new CardStatModifier(cost: -2), 3, 3, [draw]);
        RelicCardModifierResult interleaved = Apply(card, [earlierUpgrade, Cost(-1), freeOnly]);
        Require(interleaved.Dispatches.All(item => item.Returned) &&
            interleaved.Card!.Temporary.Upgrades.Select(item => item.DataId).SequenceEqual(["earlier-cost-upgrade", "free-only"]) &&
            interleaved.Card.MaskDescriptor!.AuthoredCastEffects.SequenceEqual(["CardEffectDraw"]) &&
            interleaved.Card.Temporary.Upgrades[0].Lifecycle!.Triggers.Single().Actions.Single().Type == "Draw" &&
            CardBranchMaskModel.Resolve(interleaved.Card).CostWithoutTraits == 0,
            "An interleaved direct cost modifier lost the preceding upgrade values or OnCast lifecycle.");
        RelicCardModifierResult cleared = Apply(interleaved.Card!, []);
        Require(cleared.Card!.Temporary.Upgrades.Count == 0 && cleared.Card.MaskDescriptor!.AuthoredCastEffects.Count == 0 &&
            cleared.Card.Temporary.Offsets.Cost == 0 && CardBranchMaskModel.Resolve(cleared.Card).CostWithoutTraits == 3,
            "Reset after an interleaved cost modifier retained temporary upgrades, offsets or installed triggers.");

        CombatRelicState oneCostOnly = Upgrade("one-cost-only", new CardStatModifier(damage: 5), 1, 1);
        RelicCardModifierResult first = Apply(card, [Cost(-2), oneCostOnly]);
        RelicCardModifierResult repeated = Apply(first.Card!, [Cost(-2), oneCostOnly], reset: false);
        Require(first.Dispatches.All(item => item.Returned) &&
            repeated.Dispatches.Select(item => item.Returned).SequenceEqual([true, false]) &&
            repeated.Card!.Temporary.Upgrades.Count == 1 && repeated.Card.Temporary.Offsets.Cost == -4 &&
            repeated.Card.Temporary.Offsets.XCost == 4,
            "A no-reset refresh filtered against old offsets or duplicated an ineligible temporary upgrade.");
        RelicCardModifierResult refreshed = Apply(repeated.Card!, [Cost(-2), oneCostOnly]);
        Require(refreshed.Dispatches.All(item => item.Returned) && refreshed.Card!.Temporary.Upgrades.Count == 1 &&
            refreshed.Card.Temporary.Offsets.Cost == -2 && refreshed.Card.Temporary.Offsets.XCost == 2 &&
            CardBranchMaskModel.Resolve(refreshed.Card).CostWithoutTraits == 1,
            "A reset refresh filtered against offsets or upgrades retained from the previous pass.");
        Console.WriteLine("RELIC-CARD-TYPE-COST-CHECKS PASS: ordered filters, branch isolation, interleaved lifecycle and reset/repeat.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
