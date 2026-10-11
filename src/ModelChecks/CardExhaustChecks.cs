using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardExhaustChecks
{
    private const string Consume = "CardTraitExhaustState";
    internal static void Run()
    {
        var intrinsic = Card(1, [Consume]);
        BattleTurnState root = Battle(intrinsic);
        string parent = JsonSerializer.Serialize(root);
        var played = BattleActionModel.PlayCard(root, new(1, 0));
        Require(played.Supported, played.Reason ?? "Consume card failed.");
        CombatContext after = played.State!.Spawn.Train.Context!;
        Require(after.Cards.Discard.Count == 0 && after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1 &&
            after.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Count == 0 &&
            after.Statistics!.Value(1, "TimesExhausted") == 2 && after.Statistics.Value(1, "TimesDiscarded") == 1,
            "Consume must enter standby and increment exhaustion again when it returns.");
        CardInstanceState exhausted = after.FindCard(1)!;
        Require(exhausted.PlayCount == 1 && exhausted.ExhaustState!.BaseTraits.Single().IsExhausted &&
            exhausted.ExhaustState.BaseTraits.Single().ExhaustedCardId == 1, "Consume private state was omitted.");
        var reset = CardExhaustModel.ResetTraits(exhausted);
        Require(!reset.ExhaustState!.BaseTraits.Single().IsExhausted && reset.ExhaustState.BaseTraits.Single().ExhaustedCardId == 1 &&
            exhausted.ExhaustState!.BaseTraits.Single().IsExhausted, "ResetTraits must retain its private card reference and isolate the old branch.");
        var delayed = CardExhaustModel.Initialize(Card(1, [Consume]), 1);
        Require(!CardExhaustModel.ShouldExhaustOnPlayedDiscard(delayed) && CardExhaustModel.ShouldExhaustOnPlayedDiscard(delayed.OnDiscard(true)),
            "The relic threshold did not use the incremented scenario play count.");
        Require(!delayed.OnDiscard(false).ExhaustState!.BaseTraits.Single().IsExhausted && delayed.OnDiscard(false).PlayCount == 0,
            "Unplayed hand discard must not mark Consume.");
        foreach (int stackMode in new[] { 1, 2, 3 })
        {
            var permanent = HandUpgradeModel.Apply(root.Spawn.Train.Rooms[0], Upgrade(false, stackMode), "Permanent", root.PlayRules);
            Require(!permanent.Supported && permanent.UnsupportedReason!.Contains("permanent trait-installation"),
                "A permanent Consume stack mode that changes native base trait state must remain explicitly unsupported.");
        }

        CardUpgradeModifier temporary = Upgrade(true);
        var temporaryCard = Card(1, [], [temporary]);
        var temporaryPlay = BattleActionModel.PlayCard(Battle(temporaryCard), new(1, 0));
        Require(temporaryPlay.Supported, temporaryPlay.Reason ?? "Temporary Consume card failed.");
        CombatContext removed = temporaryPlay.State!.Spawn.Train.Context!;
        Require(removed.Cards.Discard.Single().InstanceId == 1 && removed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count == 0 &&
            removed.Statistics!.Value(1, "TimesExhausted") == 1 && removed.FindCard(1)!.ExhaustState!.UpgradeTrait == null,
            "RemoveOnDiscard must remove temporary Consume before its old standby closure resolves to discard.");

        CardUpgradeModifier relicConsumeUpgrade = Upgrade(false);
        CardLifecycleUpgrade relicConsumeLifecycle = relicConsumeUpgrade.Lifecycle!;
        var relicConsume = new RelicCardModifier(0,
            new RelicCardUpgradeRule(relicConsumeUpgrade.AssetKey, true, relicConsumeLifecycle, []), relicConsumeUpgrade, false);
        var relic = new CombatRelicState("consume-relic", "ConsumeRelic", [RelicModel.AddTempUpgrade], cardModifiers: [relicConsume]);
        RelicCardModifierResult relicApplied = RelicCardModifierModel.Apply(Card(1, []), [relic], resetTemporary: false);
        Require(relicApplied.Supported && relicApplied.Card!.ExhaustState!.UpgradeTrait != null,
            "A temporary relic Consume upgrade was rejected or failed to initialize its private state: " + relicApplied.UnsupportedReason);
        var relicPlayed = BattleActionModel.PlayCard(Battle(relicApplied.Card!), new(1, 0));
        Require(relicPlayed.Supported, relicPlayed.Reason ?? "Relic Consume card failed.");
        CombatContext relicAfter = relicPlayed.State!.Spawn.Train.Context!;
        Require(relicAfter.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1 &&
            relicAfter.OtherPiles.Single(pile => pile.Name == "Standby").Cards.Count == 0 &&
            relicAfter.Statistics!.Value(1, "TimesExhausted") == 2 &&
            relicAfter.FindCard(1)!.ExhaustState!.UpgradeTrait?.IsExhausted == true,
            "A temporary relic Consume upgrade did not route the played card through native exhaustion and standby closure.");
        var retainedTemporary = Card(1, [], [Upgrade(false)]).OnDiscard(true);
        Require(retainedTemporary.ExhaustState!.UpgradeTrait!.IsExhausted, "Retained temporary Consume lost callback state.");
        var refreshed = retainedTemporary.WithModifiers(retainedTemporary.Permanent,
            new(new(), retainedTemporary.Temporary.Upgrades.Concat([new CardUpgradeModifier("stat", "stat", new(damage: 2), [],
                false, false, false, 0, 0, [], maskMetadata: new([], false, false, false, false))]).ToArray(), 0, []));
        Require(!refreshed.ExhaustState!.UpgradeTrait!.IsExhausted && refreshed.ExhaustState.UpgradeTrait.ExhaustedCardId == null &&
            retainedTemporary.ExhaustState.UpgradeTrait.IsExhausted, "A dirty temporary modifier list must recreate its upgrade-contributed trait.");
        var scaled = CardMagicPowerModel.Refresh(retainedTemporary);
        Require(scaled.ExhaustState!.UpgradeTrait!.IsExhausted, "Magic-power numeric refresh must not recreate Consume traits.");

        var purging = Card(1, [Consume, "CardTraitSelfPurge"]);
        Require(!CardExhaustModel.HasConsume(purging) && !purging.OnDiscard(true).ExhaustState!.BaseTraits.Single().IsExhausted,
            "SelfPurge must suppress Consume callbacks while preserving its latent base trait.");
        var duplicated = Card(1, [Consume, Consume]).OnDiscard(true);
        Require(!duplicated.ExhaustState!.BaseTraits[0].IsExhausted && duplicated.ExhaustState.BaseTraits[1].IsExhausted,
            "Only the final base Consume trait may run.");

        var cloneContext = root.Spawn.Train.Context!.WithCard(exhausted);
        var creation = new CardCreationRule(exhausted.DataId, CardModifiers.Empty(), null, [], maskDescriptor: exhausted.MaskDescriptor);
        var clone = CardGenerationModel.CloneDetached(cloneContext, creation, 1);
        Require(clone.Supported, clone.UnsupportedReason ?? "Consume clone failed.");
        CardInstanceState cloned = clone.Context!.FindCard(2)!;
        Require(cloned.PlayCount == 0 && !cloned.ExhaustState!.BaseTraits.Single().IsExhausted &&
            cloned.ExhaustState.BaseTraits.Single().ExhaustedCardId == null && cloneContext.FindCard(1)!.ExhaustState!.BaseTraits.Single().IsExhausted,
            "A cloned Consume card inherited a private trait callback reference.");

        var summoned = BattleActionModel.PlayCard(Battle(Card(1, [Consume], type: "Monster"), true), new(1, 0));
        Require(summoned.Supported, summoned.Reason ?? "Consume unit failed.");
        CombatContext waiting = summoned.State!.Spawn.Train.Context!;
        Require(waiting.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Single().InstanceId == 1 &&
            waiting.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1 &&
            waiting.Statistics!.Value(1, "TimesExhausted") == 1 &&
            CardPileModel.ValidateMembership(waiting.Cards, waiting.OtherPiles!, waiting.NextCardId) == null,
            "Consume must preserve the existing summon condition and retain its exhausted alias.");
        waiting = waiting.WithOtherPiles(waiting.OtherPiles!.Select(pile => pile.Name == "Standby" ? new CardPileState(pile.Name,
            pile.Cards, pile.EntrySlots, pile.FreeSlots, pile.EquipmentConditions,
            pile.UnitConditions!.Select(condition => new UnitStandbyCondition(condition.CardId, condition.HostUnitId, true)).ToArray(), pile.ExhaustConditions) : pile).ToArray());
        CombatContext dead = UnitStandbyModel.ReturnReady(waiting);
        Require(dead.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Count == 0 &&
            dead.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count == 1 && dead.Statistics!.Value(1, "TimesExhausted") == 2,
            "Dead consumed unit duplicated its exhausted alias or skipped the second statistic increment.");
        Require(JsonSerializer.Serialize(root) == parent, "Consume changed its parent branch.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(BattleActionModel.PlayCard(root, new(1, 0)).State) ==
            JsonSerializer.Serialize(played.State), "Parallel Consume branches differ."));
        Console.WriteLine("CONSUME-CHECKS PASS: private trait state, delayed threshold, temporary removal, standby closure, clones, reset and branch isolation.");
    }
    private static CardInstanceState Card(int id, string[] traits, CardUpgradeModifier[]? upgrades = null, string type = "Spell")
    {
        var descriptor = new CardMaskDescriptor(new("consume", type, "Common", type == "Monster", type == "Monster", true,
            [], [], [], [], null, false, 0, 0, false, false, false), 0,
            traits.Select(trait => new CardTraitValue(trait, trait, 0, 0, true, 0)).ToArray(), []);
        return CardExhaustModel.Initialize(new(id, "consume", CardModifiers.Empty(), new(new(), upgrades ?? [], 0, []),
            0, 0, 0, [], maskDescriptor: descriptor), 0);
    }
    private static CardUpgradeModifier Upgrade(bool remove, int stackMode = 0) => new("consume-upgrade", "consume-upgrade", new(), [], remove, false, false, 0, 0, [],
        maskMetadata: new([], false, false, false, false), lifecycle: new(new("consume-upgrade", new(), [], false, false, false, false),
            "consume-upgrade", false, remove, [new(Consume, Consume, 0, 0, true, stackMode)], [], false, [], [], []));
    private static BattleTurnState Battle(CardInstanceState card, bool summon = false)
    {
        var rng = UnityRng.Seed(34);
        var piles = new CardPileState[] { new("Standby", [], [], [], [], [], []), new("Exhausted", []), new("DiscardBuffer", []), new("Purged", []) };
        var stats = BattleStatistics.Empty().TrackCards([1]);
        var context = new CombatContext(new([new(1, "consume")], [], [], rng, 0, [], nextDrawUpgrades: []), rng, 0, 2, 10,
            statistics: stats, cardInstances: [card], cardRegistry: [card], otherPiles: piles);
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 10, 100, 100, true, true, false, []);
        var unit = new CombatUnit(0, "unit", CombatTeam.Player, 5, 10, 10, true, false, false, [], size: 1);
        var train = new TrainCombatState([new(0, false, [], [], context), new(1, false, [pyre], [], context)], [], 7, context);
        var spawn = new EnemySpawnState(train, [new([new([])])], [-1], 0, false, rng, 2, [], 0, false, 2, 1, 1, []);
        var rules = new BattlePlayRules([new(0, 7, 7, true, false, false), new(1, 0, 7, true, true, true)],
            [new("consume", "consume", 0, summon ? "SpawnMonster" : "Null", summon ? "Standby" : "Discard", summon ? unit : null, [],
                handDiscardInteractions: [], handConsumeInteractions: [], upgradeInteractions: [])]);
        return new(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 34, rng)], piles, [], rules);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
