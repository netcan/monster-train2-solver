using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCheaperSpellCostChecks
{
    internal const string RelicId = "9cc85dbb-d0ae-409b-8f26-41d2202eb1e0";
    internal const string RelicKey = "ReduceSpellCostOfCheaper";
    internal const string Effect = "RelicEffectAddTempUpgradeToCardsInHandIfCheaper";
    internal const string UpgradeId = "13e8decf-3a0e-4bc5-a6e8-197d9f032087";
    internal const string UpgradeAssetKey = "FreeSpellBlessing";
    private const string SyntheticPrefix = "59f23b76-0a79-4e0b-8f34-5100000000";

    internal static void Run()
    {
        var filters = new[]
        {
            new CardUpgradeMaskRule("OnlySpells", "Spell"),
            new CardUpgradeMaskRule("ExcludeZeroAndXCost", "Spell", minCost: 1, maxCost: 99, excludeXCost: true)
        };
        var upgrade = new CardUpgradeModifier(UpgradeId, UpgradeAssetKey, new CardStatModifier(cost: -99), [],
            removeOnDiscard: false, unique: false, excludeFromClones: false, unhealedHealth: 0, damageBuff: 0, [],
            maskMetadata: new CardUpgradeMaskMetadata([], false, false, false, false));
        var relic = new CombatRelicState(RelicId, RelicKey, [RelicModel.AddTempUpgradeToHandCardsIfCheaper],
            cheaperSpellUpgradeModifiers: [new RelicCheaperSpellUpgradeModifier(0, "Spell", upgrade, filters)]);
        Require(RelicModel.Validate([relic]) == null, "A captured cost-only cheaper-spell definition was rejected.");

        CardInstanceState Card(int id, string type, int cost, bool xCost = false) =>
            new(id, "cheaper-spell-" + id, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
                maskDescriptor: new CardMaskDescriptor(new CardMaskDefinition("cheaper-spell-" + id, type, "Common",
                    false, false, false, [], [], [], [], null, xCost, 0, 0, false, false, false, false), cost, [], []));
        CardPlayRule Rule(CardInstanceState card, string costType = "Default") => new(card.DataId, card.DataId, 0,
            "", "", null, [], costType: costType, cardType: card.MaskDescriptor!.Definition.CardType, isAnyAbility: false);

        CardInstanceState[] cards =
        [Card(1, "Spell", 3), Card(2, "Spell", 1), Card(3, "Spell", 2), Card(4, "Spell", 3),
            Card(5, "Spell", 0), Card(6, "Spell", 0, xCost: true), Card(7, "Monster", 1),
            Card(8, "Spell", 2).WithModifiers(CardModifiers.Empty(), new CardModifiers(new CardStatModifier(), [upgrade], 0, []))];
        CardPlayRule[] definitions = cards.Select(card => Rule(card,
            card.InstanceId == 6 ? "ConsumeRemainingEnergy" : "Default")).ToArray();
        var rng = UnityRng.Seed(994);
        CombatContext context = new(new CardCycleState(cards.Skip(1).Take(6).Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray(),
                [], [], rng, 0, []), rng, 0, 9, 10, statistics: BattleStatistics.Empty().WithPlayedCost(1, 3),
            cardInstances: cards, relics: [relic]);
        string sourceJson = JsonSerializer.Serialize(context);
        RelicCheaperSpellUpgradeResult applied = RelicCheaperSpellUpgradeModel.ApplyOnCardPlayed(context, 1,
            new BattlePlayRules([], definitions), 0, 0);
        Require(applied.Supported, "Supported cheaper-spell card play failed: " + applied.UnsupportedReason);
        Require(applied.Context.FindCard(2)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            applied.Context.FindCard(3)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !applied.Context.FindCard(4)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !applied.Context.FindCard(5)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !applied.Context.FindCard(6)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !applied.Context.FindCard(7)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId),
            "The relic did not enforce strict lower cost, positive-cost/X filters and Spell-only matching.");
        Require(CardBranchMaskModel.Resolve(applied.Context.FindCard(2)!).CostWithoutTraits == 0 &&
            applied.Context.Relics![0].CheaperSpellUpgradeModifiers![0].TrackedCardIds.OrderBy(id => id).SequenceEqual([2, 3]),
            "The upgrade did not clamp its cost to zero or track each eligible card.");

        RelicCheaperSpellUpgradeResult discarded = RelicCheaperSpellUpgradeModel.CardDiscarded(applied.Context, 2);
        Require(discarded.Supported && !discarded.Context.FindCard(2)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            discarded.Context.Relics![0].CheaperSpellUpgradeModifiers![0].TrackedCardIds.SequenceEqual([3]) &&
            applied.Context.FindCard(2)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId),
            "Ordinary discard did not remove one upgrade and tracking occurrence without mutating its parent branch.");
        RelicCheaperSpellUpgradeResult untrackedDiscard = RelicCheaperSpellUpgradeModel.CardDiscarded(discarded.Context, 8);
        Require(untrackedDiscard.Supported && !untrackedDiscard.Context.FindCard(8)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId),
            "Discard fallback did not remove the matching upgrade when its owner was not in the tracking list.");
        RelicCheaperSpellUpgradeResult endTurn = RelicCheaperSpellUpgradeModel.EndTurnPreHandDiscard(discarded.Context);
        Require(endTurn.Supported && !endTurn.Context.FindCard(3)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            endTurn.Context.Relics![0].CheaperSpellUpgradeModifiers![0].TrackedCardIds.Count == 0 &&
            endTurn.Context.FindCard(8)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId),
            "End-turn cleanup failed to remove the tracked hand upgrade or mutated an unrelated card.");
        Require(JsonSerializer.Serialize(context) == sourceJson && context.FindCard(2)!.Temporary.Upgrades.Count == 0,
            "Applying the relic mutated its source branch.");

        CardInstanceState xPlayed = Card(9, "Spell", 0, xCost: true);
        CardInstanceState cheaper = Card(10, "Spell", 3);
        CombatContext xContext = new(new CardCycleState([new CardToken(10, cheaper.DataId)], [], [], rng, 0, []),
            rng, 0, 11, 10, statistics: BattleStatistics.Empty().WithPlayedCost(9, 4),
            cardInstances: [xPlayed, cheaper], relics: [relic]);
        BattlePlayRules xRules = new([], [Rule(xPlayed, "ConsumeRemainingEnergy"), Rule(cheaper)]);
        RelicCheaperSpellUpgradeResult xApplied = RelicCheaperSpellUpgradeModel.ApplyOnCardPlayed(xContext, 9, xRules, 0, 0);
        Require(xApplied.Supported && xApplied.Context.FindCard(10)!.Temporary.Upgrades.Any(item => item.DataId == UpgradeId),
            "An X-cost played spell was not compared using its captured unmodified paid cost.");
        RelicCheaperSpellUpgradeResult missingPaidCost = RelicCheaperSpellUpgradeModel.ApplyOnCardPlayed(
            xContext.WithStatistics(BattleStatistics.Empty()), 9, xRules, 0, 0);
        Require(!missingPaidCost.Supported && missingPaidCost.UnsupportedReason!.Contains("unmodified played cost", StringComparison.Ordinal),
            "A missing X-cost played value was silently treated as a normal cost.");

        CombatRelicState malformed = new(RelicId, RelicKey, [Effect]);
        Require(RelicModel.Validate([malformed])?.Contains("ordered native cheaper-spell", StringComparison.Ordinal) == true,
            "An uncaptured cheaper-spell payload passed relic validation.");
        Console.WriteLine("ORIGINAL-CHEAPER-SPELL-COST-CHECKS PASS: strict comparison, Spell/zero/X filters, one-shot discard and end-turn cleanup, X paid cost, and immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 171 &&
            root.GetProperty("OriginalCheaperSpellCostPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-reduce-spell-cost-cheaper" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "The original cheaper-spell battle is incomplete or has native/model failures.");

        FixtureValue[] scenarioActions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        FixtureValue[] triggerActions = scenarioActions.Where(entry =>
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int id = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            return before.Spawn.Train.Context!.FindCard(id)?.DataId == DataId(1);
        }).ToArray();
        FixtureValue[] cheapActions = scenarioActions.Where(entry =>
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int id = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            return before.Spawn.Train.Context!.FindCard(id)?.DataId == DataId(2);
        }).ToArray();
        Require(scenarioActions.Length == 2 && triggerActions.Length == 1 && cheapActions.Length == 1,
            "The isolated original fixture must play exactly the cost-3 trigger and cost-1 target.");

        BattleTurnState triggerBefore = triggerActions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState triggerAfter = triggerActions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext beforeContext = triggerBefore.Spawn.Train.Context!;
        CombatContext afterContext = triggerAfter.Spawn.Train.Context!;
        CombatRelicState owner = beforeContext.Relics!.Single(relic => relic.DataId == RelicId);
        RelicCheaperSpellUpgradeModifier rule = owner.CheaperSpellUpgradeModifiers!.Single();
        Require(owner.AssetKey == RelicKey && owner.EffectTypes.SequenceEqual([Effect]) && rule.EffectIndex == 0 &&
            rule.CardType == "Spell" && rule.Upgrade.DataId == UpgradeId && rule.Upgrade.AssetKey == UpgradeAssetKey &&
            rule.Upgrade.Stats.Cost == -99 && rule.Upgrade.Stats.Damage == 0 && rule.Upgrade.Stats.Health == 0 &&
            rule.Upgrade.Stats.Heal == 0 && rule.Upgrade.Statuses.Count == 0 && !rule.Upgrade.RemoveOnDiscard &&
            rule.Filters.Count == 2 && rule.Filters.Any(filter => filter.AssetKey == "OnlySpells" && filter.CardType == "Spell") &&
            rule.Filters.Any(filter => filter.AssetKey == "ExcludeZeroAndXCost" && filter.ExcludeXCost &&
                filter.CardType == "Invalid" && filter.MinCost == 1 && filter.MaxCost == 99),
            "The captured native relic/upgrade/filter payload differs from ReduceSpellCostOfCheaper.");

        CardInstanceState Synthetic(CombatContext context, int ordinal) => context.CardInstances!.Single(card => card.DataId == DataId(ordinal));
        int[] expectedTracked = [Synthetic(afterContext, 2).InstanceId, Synthetic(afterContext, 6).InstanceId];
        int[] actualTracked = afterContext.Relics!.Single(relic => relic.DataId == RelicId)
            .CheaperSpellUpgradeModifiers![0].TrackedCardIds.ToArray();
        Require(Synthetic(afterContext, 2).Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            Synthetic(afterContext, 6).Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !Synthetic(afterContext, 3).Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !Synthetic(afterContext, 4).Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !Synthetic(afterContext, 5).Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            expectedTracked.All(actualTracked.Contains),
            "The native card-play boundary did not upgrade only the strictly cheaper filtered spell controls.");

        BattleTurnState cheapAfter = cheapActions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext discarded = cheapAfter.Spawn.Train.Context!;
        CardInstanceState playedTarget = Synthetic(discarded, 2);
        Require(!playedTarget.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !discarded.Relics!.Single(relic => relic.DataId == RelicId).CheaperSpellUpgradeModifiers![0].TrackedCardIds.Contains(playedTarget.InstanceId),
            "Playing and naturally discarding the reduced spell did not clean its temporary upgrade.");

        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length > 0, "The native fixture did not observe an end-turn transition.");
        BattleTurnState endTurnBefore = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState endTurnAfter = turns[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardInstanceState pendingCleanup = Synthetic(endTurnBefore.Spawn.Train.Context!, 6);
        CardInstanceState cleaned = Synthetic(endTurnAfter.Spawn.Train.Context!, 6);
        Require(pendingCleanup.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            endTurnBefore.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == RelicId)
                .CheaperSpellUpgradeModifiers![0].TrackedCardIds.Contains(pendingCleanup.InstanceId) &&
            !cleaned.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) &&
            !endTurnAfter.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == RelicId)
                .CheaperSpellUpgradeModifiers![0].TrackedCardIds.Contains(cleaned.InstanceId),
            "EndTurnPreHandDiscard did not clear the still-held reduced spell before hand discard.");
        Require(triggerActions[0].GetProperty("Difference").ValueKind == FixtureKind.Null &&
            cheapActions[0].GetProperty("Difference").ValueKind == FixtureKind.Null &&
            turns[0].GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The native triggered play, discard and end-turn boundaries did not match the model.");
        Console.WriteLine("ORIGINAL-CHEAPER-SPELL-COST NATIVE PASS: original payload, strict/equal/zero/X controls, played-card discard cleanup and end-turn pre-discard cleanup.");
    }

    private static string DataId(int ordinal) => SyntheticPrefix + ordinal.ToString("D2");

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
