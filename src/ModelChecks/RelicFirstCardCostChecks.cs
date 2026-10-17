using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicFirstCardCostChecks
{
    private const string RelicId = "64d9943e-02e7-469d-b516-c483a3ad8ccf";
    private const string RelicKey = "ReduceFirstSpellCostPerDeckSize";
    private const string Effect = "RelicEffectModifyFirstCardCostPerPile";

    internal static void Run()
    {
        CardUpgradeModifier upgrade = Upgrade();
        CombatRelicState owner = Owner(upgrade, true);
        Require(RelicModel.Validate([owner]) == null, "Captured first-card cost payload was rejected.");

        CardInstanceState Card(int id, string type, bool xCost = false, int cost = 2) => new(id,
            "first-card-cost-" + id, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: new CardMaskDescriptor(new CardMaskDefinition("first-card-cost-" + id, type, "Common",
                false, false, false, [], [], [], [], null, xCost, 0, 0, false, false, false, false), cost, [], []));
        CardPlayRule Rule(CardInstanceState card, bool ability = false, string costType = "Default") => new(card.DataId,
            card.DataId, 0, "", "", null, [], costType: costType,
            cardType: card.MaskDescriptor!.Definition.CardType, isAnyAbility: ability);

        CardInstanceState[] cards = [Card(1, "Spell", cost: 3), Card(2, "Spell", cost: 4),
            Card(3, "Spell"), Card(4, "Monster"), Card(5, "Spell", xCost: true, cost: 0)];
        BattlePlayRules definitions = new([], [Rule(cards[0]), Rule(cards[1]), Rule(cards[2], ability: true),
            Rule(cards[3]), Rule(cards[4], costType: "ConsumeRemainingEnergy")]);
        var rng = UnityRng.Seed(40617);
        CardCycleState hand = new(cards.Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray(), [], [], rng, 0, []);
        BattleResourceState Resources(int visibleCount) => new(3, 0, 5, 9, 10, "pyre", 1, 100, 0, 1,
            [], [], [], visibleDeckCount: visibleCount);
        CombatContext Context(int visibleCount, IReadOnlyList<CardToken>? currentHand = null) => new(
            currentHand == null ? hand : new CardCycleState(currentHand, [], [], rng, 0, []), rng, 0, 10, 10,
            cardInstances: cards, relics: [Owner(upgrade, true)], resources: Resources(visibleCount));

        foreach (var point in new[] { (Count: 14, Reduction: 0), (Count: 15, Reduction: 1),
            (Count: 29, Reduction: 1), (Count: 30, Reduction: 2) })
        {
            RelicFirstCardCostResult result = RelicFirstCardCostModel.ApplyOnCardAdded(Context(point.Count), 2, definitions);
            CardUpgradeModifier applied = result.Context.FindCard(2)!.Temporary.Upgrades.Single(item => item.DataId == Effect);
            Require(result.Supported && applied.Stats.Cost == -point.Reduction &&
                applied.Stats.XCost == point.Reduction,
                "Deck-count cost reduction boundary differs at visible count " + point.Count + ".");
        }

        CombatContext source = Context(30);
        string before = JsonSerializer.Serialize(source);
        RelicFirstCardCostResult firstAdded = RelicFirstCardCostModel.ApplyOnCardAdded(source, 1, definitions);
        firstAdded = RelicFirstCardCostModel.ApplyOnCardAdded(firstAdded.Context, 2, definitions);
        firstAdded = RelicFirstCardCostModel.ApplyOnCardAdded(firstAdded.Context, 3, definitions);
        firstAdded = RelicFirstCardCostModel.ApplyOnCardAdded(firstAdded.Context, 4, definitions);
        firstAdded = RelicFirstCardCostModel.ApplyOnCardAdded(firstAdded.Context, 5, definitions);
        Require(firstAdded.Supported && firstAdded.Context.FindCard(1)!.Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            firstAdded.Context.FindCard(2)!.Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            !firstAdded.Context.FindCard(3)!.Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            !firstAdded.Context.FindCard(4)!.Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            firstAdded.Context.FindCard(5)!.Temporary.Upgrades.Any(item => item.DataId == Effect),
            "Card-added eligibility did not include normal/X-cost Spells and exclude Spell abilities/Monsters.");

        RelicFirstCardCostResult played = RelicFirstCardCostModel.ApplyOnCardPlayed(firstAdded.Context, 1, definitions);
        CombatRelicState afterPlayOwner = played.Context.Relics!.Single();
        Require(played.Supported && afterPlayOwner.CardTypeCosts![0].WaitingForFirstCard == false &&
            played.Context.CardInstances!.All(card => !card.Temporary.Upgrades.Any(item => item.DataId == Effect)),
            "The first eligible Spell play did not clear the upgrade from every card and disarm the owner.");

        RelicFirstCardCostResult blockedAdded = RelicFirstCardCostModel.ApplyOnCardAdded(played.Context, 2, definitions);
        Require(blockedAdded.Supported && !blockedAdded.Context.FindCard(2)!.Temporary.Upgrades.Any(item => item.DataId == Effect),
            "A card added after the first Spell was upgraded before the turn reset.");
        CardToken[] retainedHand = [new(2, cards[1].DataId), new(3, cards[2].DataId), new(4, cards[3].DataId)];
        CombatContext preNextTurn = played.Context.WithCards(new CardCycleState(retainedHand, [], [], rng, 0, []));
        RelicFirstCardCostResult ended = RelicFirstCardCostModel.EndRoomCombat(preNextTurn);
        RelicFirstCardCostResult nextTurn = RelicFirstCardCostModel.StartTurnBeforeDraw(ended.Context, definitions);
        Require(ended.Supported && nextTurn.Supported && nextTurn.Context.Relics!.Single().CardTypeCosts![0].WaitingForFirstCard == true &&
            nextTurn.Context.FindCard(2)!.Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            !nextTurn.Context.FindCard(3)!.Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            !nextTurn.Context.FindCard(4)!.Temporary.Upgrades.Any(item => item.DataId == Effect),
            "The next turn did not reapply the upgrade only to retained eligible Spells before draw.");

        RelicFirstCardCostResult nextDraw = RelicFirstCardCostModel.ApplyOnCardAdded(nextTurn.Context, 5, definitions);
        CardInstanceState discarded = nextDraw.Context.FindCard(2)!.OnDiscard(false);
        Require(nextDraw.Supported && nextDraw.Context.FindCard(5)!.Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            !discarded.Temporary.Upgrades.Any(item => item.DataId == Effect),
            "Card-added after reset or RemoveOnDiscard behavior differed.");

        CombatContext missingResources = Context(30).WithResources(null);
        RelicFirstCardCostResult missing = RelicFirstCardCostModel.ApplyOnCardAdded(missingResources, 1, definitions);
        Require(!missing.Supported && missing.UnsupportedReason!.Contains("visible deck count", StringComparison.Ordinal),
            "A missing deck-count capture silently produced a cost modifier.");
        CombatRelicState malformed = new(RelicId, RelicKey, [Effect], disallowedInPlacementPhase: false);
        Require(RelicModel.Validate([malformed]) != null,
            "An incomplete native first-card cost definition passed validation.");
        Require(JsonSerializer.Serialize(source) == before &&
            !source.FindCard(1)!.Temporary.Upgrades.Any(item => item.DataId == Effect),
            "First-card cost application mutated its source branch.");
        Console.WriteLine("RELIC-FIRST-CARD-COST-PURE PASS: deck thresholds, Spell/ability/X-cost eligibility, one-shot hand cleanup, next-turn reset, discard cleanup, and immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 205 &&
            root.GetProperty("OriginalFirstCardCostPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == OriginalName &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The isolated original first-card cost battle is incomplete or has native/model failures.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        FixtureValue[] first = actions.Where(entry => PlayedData(entry) == DataId(1)).ToArray();
        FixtureValue[] second = actions.Where(entry => PlayedData(entry) == DataId(2)).ToArray();
        FixtureValue[] retained = actions.Where(entry => PlayedData(entry) == DataId(8)).ToArray();
        Require(actions.Length == 3 && first.Length == 1 && second.Length == 1 && retained.Length == 1,
            "The isolated battle must play the first Spell, a same-turn Spell, then the retained Spell next turn.");

        BattleTurnState firstBefore = first[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState firstAfter = first[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext initial = firstBefore.Spawn.Train.Context!;
        CombatContext afterFirst = firstAfter.Spawn.Train.Context!;
        CombatRelicState owner = initial.Relics!.Single(relic => relic.DataId == RelicId);
        RelicCardTypeCost rule = owner.CardTypeCosts!.Single();
        int reduction = initial.Resources!.VisibleDeckCount / 15;
        Require(owner.AssetKey == RelicKey && owner.EffectTypes.SequenceEqual([Effect]) && rule.EffectIndex == 0 &&
            rule.CardType == "Spell" && rule.Amount == 15 && rule.PileType == "DeckPile" &&
            rule.WaitingForFirstCard == true && rule.FirstCardUpgrade != null &&
            rule.FirstCardUpgrade.DataId == Effect && rule.FirstCardUpgrade.Unique &&
            rule.FirstCardUpgrade.ExcludeFromClones && rule.FirstCardUpgrade.RemoveOnDiscard &&
            rule.FirstCardUpgrade.Stats.Cost == -reduction && rule.FirstCardUpgrade.Stats.XCost == reduction &&
            initial.Resources.VisibleDeckCount >= 15,
            "Captured relic parameters or visible-deck-scaled cost upgrade differ from native state.");

        CardInstanceState Synthetic(CombatContext context, int ordinal) => context.CardInstances!
            .Single(card => card.DataId == DataId(ordinal));
        foreach (int ordinal in new[] { 1, 2, 3, 4, 5, 8 })
        {
            CardUpgradeModifier applied = Synthetic(initial, ordinal).Temporary.Upgrades.Single(item => item.DataId == Effect);
            Require(applied.Stats.Cost == -reduction && applied.Stats.XCost == reduction,
                "OnCardAdded did not apply the captured reduction to synthetic Spell " + ordinal + ".");
        }
        Require(CardBranchMaskModel.Resolve(Synthetic(initial, 4)).CostWithoutTraits == 0 &&
            Synthetic(initial, 5).MaskDescriptor!.Definition.XCost,
            "Zero-cost and X-cost Spell controls were not captured with first-card upgrades.");
        Require(first[0].GetProperty("Difference").ValueKind == FixtureKind.Null &&
            afterFirst.Relics!.Single(relic => relic.DataId == RelicId).CardTypeCosts![0].WaitingForFirstCard == false &&
            afterFirst.CardInstances!.All(card => !card.Temporary.Upgrades.Any(item => item.DataId == Effect)),
            "First Spell play failed to clear all hand upgrades or set the one-shot flag.");

        BattleTurnState secondBefore = second[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState secondAfter = second[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(secondBefore.Spawn.Turn == 0 &&
            secondBefore.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == RelicId)
                .CardTypeCosts![0].WaitingForFirstCard == false &&
            !Synthetic(secondAfter.Spawn.Train.Context!, 8).Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            second[0].GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The one-shot effect changed state on a second same-turn Spell play.");

        FixtureValue[] turnChanges = root.GetProperty("Turns").EnumerateArray().Where(entry =>
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            return before.Spawn.Turn == 0 && after.Spawn.Turn == 1;
        }).ToArray();
        Require(turnChanges.Length == 1, "The first-card cost fixture did not capture exactly one first-to-second turn transition.");
        BattleTurnState turnBefore = turnChanges[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState turnAfter = turnChanges[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext nextTurn = turnAfter.Spawn.Train.Context!;
        Require(turnBefore.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == RelicId)
                .CardTypeCosts![0].WaitingForFirstCard == false &&
            turnBefore.Spawn.Train.Context.Cards.Hand.Any(card => card.DataId == DataId(8)) &&
            nextTurn.Cards.Hand.Any(card => card.DataId == DataId(8)) &&
            nextTurn.Relics!.Single(relic => relic.DataId == RelicId).CardTypeCosts![0].WaitingForFirstCard == true &&
            Synthetic(nextTurn, 8).Temporary.Upgrades.Single(item => item.DataId == Effect).Stats.Cost == -reduction &&
            turnChanges[0].GetProperty("Difference").ValueKind == FixtureKind.Null,
            "End-turn reset and turn-phase reapplication did not upgrade the retained Spell before its next-turn play.");

        BattleTurnState retainedBefore = retained[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState retainedAfter = retained[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext retainedContext = retainedAfter.Spawn.Train.Context!;
        Require(retainedBefore.Spawn.Turn == 1 &&
            retainedBefore.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == RelicId)
                .CardTypeCosts![0].WaitingForFirstCard == true &&
            Synthetic(retainedBefore.Spawn.Train.Context!, 8).Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            retainedContext.Relics!.Single(relic => relic.DataId == RelicId)
                .CardTypeCosts![0].WaitingForFirstCard == false &&
            Synthetic(retainedContext, 8).Temporary.Upgrades.Any(item => item.DataId == Effect) &&
            !retainedContext.Cards.Hand.Any(card => card.DataId == DataId(8)) &&
            retainedContext.OtherPiles!.Single(pile => pile.Name == "DiscardBuffer").Cards.Any(card => card.DataId == DataId(8)) &&
            retained[0].GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The persistent Spell did not consume the reset state and retain its hand-excluded upgrade in DiscardBuffer.");

        Console.WriteLine("RELIC-FIRST-CARD-COST NATIVE PASS: native OnCardAdded reduction, zero/X-cost controls, hand cleanup, room-end reset and persistent DiscardBuffer retention.");
    }

    private const string OriginalName = "original-reduce-first-spell-cost-per-deck-size";
    private const string SyntheticPrefix = "64d9943e-02e7-469d-b516-c483a3ad8ccf-";
    private static string DataId(int ordinal) => SyntheticPrefix + ordinal.ToString("D2");
    private static string PlayedData(FixtureValue entry)
    {
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        int id = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        return before.Spawn.Train.Context!.FindCard(id)?.DataId ?? "";
    }

    private static CardUpgradeModifier Upgrade() => new(Effect, Effect, new CardStatModifier(), [],
        removeOnDiscard: true, unique: true, excludeFromClones: true, unhealedHealth: 0, damageBuff: 0, []);

    private static CombatRelicState Owner(CardUpgradeModifier upgrade, bool waiting) => new(RelicId, RelicKey,
        [Effect], isCovenant: false, disallowedInPlacementPhase: false,
        cardTypeCosts: [new RelicCardTypeCost(0, "Spell", 15, false, null, 0, "DeckPile", upgrade, waiting)]);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
