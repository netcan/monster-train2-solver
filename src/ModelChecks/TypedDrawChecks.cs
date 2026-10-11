using MonsterTrain2Poju.Model;

internal static class TypedDrawChecks
{
    internal static void Run()
    {
        CardToken Token(int id) => new(id, "card-" + id);
        CardInstanceState Instance(int id, string type, string[]? traits = null, CardUpgradeModifier[]? upgrades = null) =>
            new(id, "card-" + id, CardModifiers.Empty(), new(new(), upgrades ?? [], 0, []), 0, 0, 0, [],
                maskDescriptor: new(new("card-" + id, type, "Common", type == "Monster", type == "Monster", true,
                    [], [], [], [], null, false, 1, 0, false, false, false), 1,
                    (traits ?? []).Select(trait => new CardTraitValue(trait, trait, 0, 0, true, 0)).ToArray(), []));
        CardUpgradeModifier Upgrade(string id, int damage, bool unique = false) =>
            new(id, id, new(damage: damage), [], false, unique, false, 0, 0, [], maskMetadata: new([], false, false, false, false));
        var instances = new[] { Instance(1, "Spell"), Instance(2, "Monster"), Instance(3, "Spell"),
            Instance(4, "Monster"), Instance(5, "Spell", ["CardTraitStrongerMagicPower"]), Instance(6, "Monster") };
        var definitions = new BattlePlayRules([], instances.Select(card => new CardPlayRule(card.DataId, card.DataId,
            1, "Null", "Discard", null, [], upgradeInteractions: [], cardType: card.MaskDescriptor!.Definition.CardType)).ToArray());
        var rng = UnityRng.Seed(121);
        var piles = new CardCycleState([Token(1)], [Token(2), Token(3), Token(4)], [Token(5), Token(6)], rng, 2, [],
            new([], [new("retained-bonus", Upgrade("bonus", 9))]), [], nextUpgradeReferenceId: 1);
        CombatContext Context(CardCycleState cards, int max = 10, CardInstanceState[]? cardsState = null) =>
            new(cards, rng, 100, 7, max, cardInstances: cardsState ?? instances);

        var root = Context(piles);
        var monsters = TypedDrawModel.Draw(root, new("Monster"), 3, 1, definitions);
        Require(monsters.Supported && monsters.DrawnCardIds.SequenceEqual([4, 2, 6]), "Typed draw order must exhaust the deck before the discard pile.");
        Require(monsters.Context!.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([6, 2, 4, 1]) &&
            monsters.Context.Cards.Draw.Single().InstanceId == 3 && monsters.Context.Cards.Discard.Single().InstanceId == 5,
            "Typed draw changed untargeted cards or hand insertion order.");
        Require(monsters.Context.Cards.Rng.Equals(rng) && monsters.Context.BattleRng.Equals(rng) &&
            monsters.Context.Cards.DrawModifier == 2 && monsters.Context.Cards.BonusDraw!.Listeners.Count == 1,
            "Typed draw shuffled, consumed RNG or fired the ordinary draw completion signal.");
        Require(piles.Hand.Count == 1 && piles.Draw.Count == 3 && instances.All(card => card.Temporary.Upgrades.Count == 0),
            "Typed draw mutated its parent.");

        var spells = new CardUpgradeMaskRule(cardType: "Spell");
        var noEquipment = new CardUpgradeMaskRule(cardType: "Equipment");
        var fallback = TypedDrawModel.Draw(root, new("Monster", noEquipment, spells), 2, 1, definitions);
        Require(fallback.Supported && fallback.DrawnCardIds.SequenceEqual([3, 5]), "Secondary typed-draw mask did not run after zero primary matches.");
        var primary = TypedDrawModel.Draw(root, new("Monster", spells, new(cardType: "Monster")), 4, 1, definitions);
        Require(primary.Supported && primary.DrawnCardIds.SequenceEqual([3, 5]), "A nonempty primary result incorrectly filled the shortfall from secondary.");
        var full = TypedDrawModel.Draw(Context(piles, 1), new("Monster", spells, new(cardType: "Monster")), 4, 1, definitions);
        Require(full.Supported && full.DrawnCardIds.Count == 0 && full.Context!.Cards.Draw.Count == 3,
            "Full-hand typed draws moved cards.");
        Require(TypedDrawModel.Draw(root, new("Monster"), 0, 1, definitions).Context!.Cards == piles &&
            TypedDrawModel.Draw(root, new("Monster"), -3, 1, definitions).Context!.Cards == piles,
            "Nonpositive typed draw changed card piles.");

        var upgrade = new NextDrawUpgradeRule(Upgrade("boost", 7), []);
        var scheduled = NextDrawUpgradeModel.Schedule(piles, upgrade);
        var upgraded = TypedDrawModel.Draw(Context(scheduled), new("Monster"), 3, 1, definitions);
        Require(upgraded.Supported && upgraded.DrawnCardIds.All(id =>
            upgraded.Context!.CardInstances!.Single(card => card.InstanceId == id).Temporary.Upgrades.Single().Stats.Damage == 7),
            "A next-draw queue must upgrade every matching card drawn by the effect.");
        Require(upgraded.Context!.Cards.NextDrawUpgrades!.Count == 1 &&
            NextDrawUpgradeModel.Clear(upgraded.Context.Cards).NextDrawUpgrades!.Count == 0 && scheduled.NextDrawUpgrades!.Count == 1,
            "Next-draw queue was consumed per draw or cleared mutably.");

        var first = new NextDrawUpgradeRule(Upgrade("first", 3), []);
        var rejected = new NextDrawUpgradeRule(Upgrade("rejected", 5), [noEquipment]);
        var third = new NextDrawUpgradeRule(Upgrade("third", 8), []);
        var cascade = NextDrawUpgradeModel.Schedule(NextDrawUpgradeModel.Schedule(NextDrawUpgradeModel.Schedule(piles, first), rejected), third);
        var cascadeDraw = TypedDrawModel.Draw(Context(cascade), new("Monster"), 1, 1, definitions);
        Require(cascadeDraw.Supported && cascadeDraw.Context!.CardInstances!.Single(card => card.InstanceId == 4)
            .Temporary.Upgrades.Select(item => item.DataId).SequenceEqual(["first"]),
            "A filter failure must retain earlier applied upgrades and block all later queued upgrades.");

        var unique = new NextDrawUpgradeRule(Upgrade("unique", 4, true), []);
        var uniqueQueue = NextDrawUpgradeModel.Schedule(NextDrawUpgradeModel.Schedule(piles, unique), unique);
        Require(uniqueQueue.NextDrawUpgrades!.Count == 1, "Unique pending upgrade was scheduled twice.");
        var anonymousUnique = new NextDrawUpgradeRule(Upgrade("", 4, true), []);
        var anonymousQueue = NextDrawUpgradeModel.Schedule(NextDrawUpgradeModel.Schedule(piles, anonymousUnique), anonymousUnique);
        Require(anonymousQueue.NextDrawUpgrades!.Count == 2 && anonymousQueue.NextUpgradeReferenceId == 3,
            "Anonymous unique upgrades were incorrectly deduplicated by an empty data ID.");
        var existing = instances.Select(card => card.InstanceId == 4 ? Instance(4, "Monster", upgrades: [Upgrade("unique", 1, true)]) : card).ToArray();
        var replaced = TypedDrawModel.Draw(Context(uniqueQueue, cardsState: existing), new("Monster"), 1, 1, definitions);
        Require(replaced.Supported && replaced.Context!.CardInstances!.Single(card => card.InstanceId == 4)
            .Temporary.Upgrades.Single().Stats.Damage == 4,
            "Native upgrade cleanup must replace the first existing unique upgrade even when its temporary AddUpgrade rejected.");
        var magic = TypedDrawModel.Draw(Context(scheduled), new("Spell"), 2, 1, definitions);
        Require(magic.Supported && magic.Context!.CardInstances!.Single(card => card.InstanceId == 5)
            .Temporary.Upgrades.Single().Stats.Damage == 35, "Next-draw clone did not apply the target's magic-power multiplier.");

        Require(CardCycleModel.DrawCards(scheduled, 1, 10, 1).State!.NextDrawUpgrades!.Count == 1 &&
            CardCycleModel.DiscardHand(scheduled).State!.NextDrawUpgrades!.Count == 1,
            "A card-cycle transition lost the pending upgrade queue.");
        var ordinary = NextDrawUpgradeModel.DrawCards(Context(scheduled), 3, 1, definitions);
        Require(ordinary.Supported && ordinary.DrawnCardIds.SequenceEqual([4, 3, 2]) &&
            ordinary.Context!.Cards.BonusDraw!.Listeners.Count == 0 && ordinary.Context.Cards.NextDrawUpgrades!.Count == 1 &&
            ordinary.DrawnCardIds.All(id => ordinary.Context.CardInstances!.Single(card => card.InstanceId == id).Temporary.Upgrades.Single().Stats.Damage == 7),
            "Ordinary spell draws did not apply each queued upgrade before clearing bonus listeners.");
        foreach (int count in new[] { -2, 0, 1, 3, 5, 9 })
        foreach (int max in new[] { 1, 3, 10 })
        {
            CardCycleState baseline = CardCycleModel.DrawCards(piles, count, max, 1).State!;
            var actual = NextDrawUpgradeModel.DrawCards(Context(piles, max), count, 1, definitions);
            Require(actual.Supported && actual.Context!.Cards.Hand.Select(card => card.InstanceId).SequenceEqual(baseline.Hand.Select(card => card.InstanceId)) &&
                actual.Context.Cards.Draw.Select(card => card.InstanceId).SequenceEqual(baseline.Draw.Select(card => card.InstanceId)) &&
                actual.Context.Cards.Discard.Select(card => card.InstanceId).SequenceEqual(baseline.Discard.Select(card => card.InstanceId)) &&
                actual.Context.Cards.Rng.Equals(baseline.Rng), "The contextual ordinary-draw adapter changed previously calibrated cycling or reshuffle behavior.");
        }

        var gate = new CardEffectTests(true, true, true, false, false);
        var next = new CardActionEffect("ScheduleNextDrawUpgrade", "Room", 0, false, false, [],
            tests: gate, nextDrawUpgrade: upgrade, requiredGold: -80);
        var typed = new CardActionEffect("DrawType", "DrawPile", 1, false, false, [], typedDraw: new("Monster"));
        var spell = new[] { next, typed, next, typed };
        var spellRules = new BattlePlayRules([], definitions.Cards.Select(card => card.DataId == "card-1"
            ? new CardPlayRule(card.DataId, card.AssetKey, 1, "Spell", "Discard", null, [], spell,
                upgradeInteractions: [], cardType: "Spell") : card).ToArray());
        var room = new RoomCombatState(0, false, [], [], root);
        Require(CardSpellModel.TestPlay(room, spell, 0).CanPlay, "Pre-cast payment tests did not inspect the unchanged pre-cast gold.");
        var paid = CardSpellModel.Apply(room, spell, 0, 1, definitions: spellRules);
        Require(paid.Supported && paid.State!.Context!.Gold == 20 && paid.State.Context.Cards.Hand.Count == 2 &&
            paid.State.Context.Cards.NextDrawUpgrades!.Count == 0,
            "A later failed payment did not cancel subsequent draws, or the effect-end queue was not cleared.");
        var tooDear = new CardActionEffect("ScheduleNextDrawUpgrade", "Room", 0, false, false, [],
            tests: gate, nextDrawUpgrade: upgrade, requiredGold: 101);
        Require(!CardSpellModel.TestPlay(room, [tooDear, typed], 0).CanPlay,
            "A mandatory unaffordable upgrade did not reject the card before any effect.");
        var uncapturedCards = new CardCycleState(piles.Hand, piles.Draw, piles.Discard, piles.Rng, piles.DrawModifier,
            piles.ExternalInteractions, piles.BonusDraw, []);
        var uncaptured = CardSpellModel.Apply(new(0, false, [], [], Context(uncapturedCards)), [next, typed], 0,
            definitions: definitions);
        Require(!uncaptured.Supported && uncaptured.UnsupportedReason!.Contains("reference allocation"),
            "A next-draw operation guessed missing shared-reference state from a legacy snapshot.");
        var preview = CardSpellModel.Apply(new RoomCombatState(0, false, [], [], root, preview: true), [next, typed], 0,
            definitions: definitions);
        Require(preview.Supported && preview.State!.Context!.Gold == 100 && preview.State.Context.Cards.Hand.Count == 1,
            "Preview applied a next-draw payment or typed draw.");
        Require(root.Gold == 100 && root.Cards.Hand.Count == 1 && root.Cards.NextDrawUpgrades!.Count == 0,
            "Integrated next-draw effects mutated the parent battle state.");
        var displayContext = root.WithStatistics(new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0,
            trackedCards: [1, 2, 3, 4, 5, 6], storedCards: [])).WithQueryFrame(new(activeBattle: true));
        foreach (bool targeting in new[] { false, true })
        {
            var descriptor = new CardMaskDescriptor(new("shown-targets", "Spell", "Common", false, false, false,
                [], [], [], targeting ? ["CardEffectDrawType"] : [], null, false, 0, 0, false, false, false), 0,
                [new("CardTraitShowCardTargets", "CardTraitShowCardTargets", 0, 0, true, 0)], []);
            var creation = new CardCreationRule("shown-targets", CardModifiers.Empty(), null, [], maskDescriptor: descriptor);
            CardGenerationResult generated = CardGenerationModel.Apply(displayContext, new("HandPile", 1, [creation]));
            Require(generated.Supported && generated.Context!.Statistics!.StoredCards!.Count == (targeting ? 7 : 0),
                "ShowCardTargets must refresh native deckStats when its typed-draw display queries pile counts.");
        }
        Console.WriteLine("TYPED-DRAW-CHECKS PASS: native pile order, fallback, full hand, unchanged RNG, pending upgrades, filter short circuit, unique replacement and branch isolation.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
