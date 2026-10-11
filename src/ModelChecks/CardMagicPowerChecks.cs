using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardMagicPowerChecks
{
    internal static void Run()
    {
        var descriptor = new CardMaskDescriptor(new CardMaskDefinition("strong", "Spell", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false), 1,
            [new("CardTraitStrongerMagicPower", "CardTraitStrongerMagicPower", 0, 0, true, 0)], []);
        CardUpgradeModifier Upgrade(string id, int damage, int heal = 0, int? authoredDamage = null, int? authoredHeal = null, bool unique = false)
        {
            var stats = new CardStatModifier(damage: damage, heal: heal);
            return new(id, id, stats, [], false, unique, false, 0, 0, [], cloneDamageBase: authoredDamage, cloneHealBase: authoredHeal,
                maskMetadata: new([], false, false, false, false), lifecycle: new(new(id, stats, [], false, false, false, false),
                    id, unique, false, [], [], false, [], [], [], authoredDamage, authoredHeal));
        }
        var retained = Upgrade("retained", 77, 88, 2, 3);
        var source = new CardInstanceState(1, "strong", new(new(), [retained], 0, []), CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: descriptor);
        var rng = UnityRng.Seed(612);
        var context = new CombatContext(new([new(1, "strong")], [], [], rng, 0, [], new([], [])), rng, 0, 2, 10,
            cardInstances: [source], cardRegistry: [source]);
        var rule = new CardPlayRule("strong", "strong", 1, "Null", "Discard", null, [], upgradeInteractions: [], cardType: "Spell");
        var definitions = new BattlePlayRules([], [rule]);
        var root = new RoomCombatState(0, false, [], [], context);
        string parent = JsonSerializer.Serialize(context);
        var incoming = Upgrade("incoming", 4, 6, 4, 6);
        var applied = HandUpgradeModel.Apply(root, incoming, "TemporaryUntilEndOfBattle", definitions);
        Require(applied.Supported, "Magic-power hand upgrade rejected: " + applied.UnsupportedReason);
        CardInstanceState upgraded = applied.State!.Context!.FindCard(1)!;
        Require(upgraded.Permanent.Upgrades.Single().Stats.Damage == 10 && upgraded.Permanent.Upgrades.Single().Stats.Heal == 15 &&
            upgraded.Temporary.Upgrades.Single().Stats.Damage == 20 && upgraded.Temporary.Upgrades.Single().Stats.Heal == 30,
            "Hand upgrading did not refresh all positive authored bases against the target multiplier.");
        Require(upgraded.Permanent.Upgrades.Single().Lifecycle!.Values.Stats.Damage == 10 &&
            upgraded.Temporary.Upgrades.Single().Lifecycle!.Values.Stats.Heal == 30, "Magic-power refresh left lifecycle mask values stale.");
        var negative = Upgrade("negative", -2, -3);
        var temporary = HandUpgradeModel.Apply(root, negative, "TemporaryUntilEndOfBattle", definitions).State!.Context!.FindCard(1)!;
        var permanent = HandUpgradeModel.Apply(root, negative, "Permanent", definitions).State!.Context!.FindCard(1)!;
        Require(temporary.Temporary.Upgrades.Single().Stats.Damage == -2 && permanent.Permanent.Upgrades.Last().Stats.Damage == -10 &&
            permanent.Permanent.Upgrades.Last().Stats.Heal == -15,
            "Temporary and permanent upgrades confused current-value scaling with positive-base refresh.");

        var draw = new CardCycleResult(context.Cards, upgradeApplications: [new(1, incoming)]);
        CombatContext bonus = BonusDrawModel.ApplyUpgrades(context, draw, definitions, out string? bonusError);
        Require(bonusError == null && bonus.FindCard(1)!.Temporary.Upgrades.Single().Stats.Damage == 20 &&
            bonus.FindCard(1)!.Permanent.Upgrades.Single().Stats.Damage == 10, "Future-draw helper omitted full magic-power refresh.");

        var creation = new CardCreationRule("strong", CardModifiers.Empty(), null, [], maskDescriptor: descriptor);
        var pending = Upgrade("pending", 5, 0, 5);
        var generatedContext = new CombatContext(context.Cards, rng, 0, 2, 10, cardInstances: [source], cardRegistry: [source],
            nextAddedTemporaryUpgrades: [pending]);
        var generated = CardGenerationModel.Apply(generatedContext, new("DeckPile", 1, [creation], upgrade: incoming));
        Require(generated.Supported, "Strong-magic generation rejected: " + generated.UnsupportedReason);
        CardInstanceState generatedCard = generated.Context!.FindCard(2)!;
        Require(generatedCard.Temporary.Upgrades.Select(upgrade => upgrade.Stats.Damage).SequenceEqual([20, 25]),
            "Generated optional/pending upgrades omitted destination magic-power scaling.");
        var copied = CardGenerationModel.Apply(context, new("DiscardPile", 1, [creation], copyModifiers: true,
            discardCopyUpgrades: [new("", incoming)]), 1);
        Require(copied.Supported && copied.Context!.FindCard(2)!.Permanent.Upgrades.Single().Stats.Damage == 10 &&
            copied.Context.FindCard(2)!.Temporary.Upgrades.Single().Stats.Damage == 20,
            "Generated copies failed to refresh copied and discard-conditional upgrades with their destination traits.");
        CardGenerationResult detached = CardGenerationModel.CloneDetached(context, creation, 1);
        Require(detached.Supported && detached.Context!.CardRegistry!.Single(card => card.InstanceId == 2).Permanent.Upgrades.Single().Stats.Damage == 10,
            "Detached clone refreshed to unscaled authored stats.");
        var overflow = CardMagicPowerModel.Scale(Upgrade("overflow", int.MaxValue), 5);
        Require(overflow.Stats.Damage == unchecked(int.MaxValue * 5), "Magic-power multiplication did not preserve native signed overflow.");
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(HandUpgradeModel.Apply(root, incoming,
            "TemporaryUntilEndOfBattle", definitions).State) == JsonSerializer.Serialize(applied.State), "Magic-power upgrade branches differ."));
        Require(JsonSerializer.Serialize(context) == parent, "Magic-power application mutated its source.");
        SharedUpgradeReferences();
        Console.WriteLine("CARD-MAGIC-POWER-CHECKS PASS: authored-base refresh, signed permanent/temporary semantics, lifecycle metadata, future draw, optional/pending/discard generation, clones and 16 parallel branches.");
    }

    private static void SharedUpgradeReferences()
    {
        CardUpgradeModifier Upgrade(string id, int damage, bool unique = false) => new(id, id, new(damage: damage), [],
            false, unique, false, 0, 0, [], cloneDamageBase: 2, maskMetadata: new([], false, false, false, false));
        CardInstanceState Card(int id, bool strong, params CardUpgradeModifier[] upgrades) => new(id, "card-" + id,
            CardModifiers.Empty(), new(new(), upgrades, 0, []), 0, 0, 0, [], maskDescriptor: new(new("card-" + id,
                "Spell", "Common", false, false, false, [], [], [], [], null, false, 0, 0, false, false, false), 1,
                strong ? [new("CardTraitStrongerMagicPower", "CardTraitStrongerMagicPower", 0, 0, true, 0)] : [], []));
        var instances = new[] { Card(1, true, Upgrade("basic", 7)), Card(2, true), Card(3, false) };
        var definitions = new BattlePlayRules([], instances.Select(card => new CardPlayRule(card.DataId, card.DataId,
            1, "Null", "Discard", null, [], upgradeInteractions: [], cardType: "Spell")).ToArray());
        UnityRng rng = UnityRng.Seed(1337);
        var cards = new CardCycleState([], [new(1, "card-1"), new(2, "card-2"), new(3, "card-3")], [], rng, 0, [],
            nextDrawUpgrades: [], nextUpgradeReferenceId: 1);
        var basic = new NextDrawUpgradeRule(Upgrade("basic", 2), []);
        cards = NextDrawUpgradeModel.Schedule(NextDrawUpgradeModel.Schedule(cards, basic), basic);
        Require(cards.NextUpgradeReferenceId == 3 && cards.NextDrawUpgrades!.Select(rule => rule.Upgrade.SharedReferenceId).SequenceEqual([1, 2]),
            "Repeated upgrade definitions did not receive distinct predictable identities.");
        var root = new CombatContext(cards, rng, 0, 4, 10, cardInstances: instances, cardRegistry: instances);
        CombatContext drawn = NextDrawUpgradeModel.DrawSpecific(root, 1, 0, definitions).Context!;
        Require(drawn.FindCard(1)!.Temporary.Upgrades.Select(upgrade => upgrade.SharedReferenceId).SequenceEqual([2, null, null]),
            "Remove-first duplicate semantics failed to retain the second original and detach scaled clones.");
        var handUpgrade = Upgrade("hand", 1);
        CombatContext refreshed = HandUpgradeModel.Apply(new(0, false, [], [], drawn), handUpgrade,
            "TemporaryUntilEndOfBattle", definitions).State!.Context!;
        Require(refreshed.Cards.NextDrawUpgrades!.Select(rule => rule.Upgrade.Stats.Damage).SequenceEqual([2, 10]),
            "Refreshing an installed next-draw original did not update its pending queue alias.");
        var blocker = new NextDrawUpgradeRule(Upgrade("blocker", 3, true), [new(cardType: "Equipment")]);
        var blockedCards = NextDrawUpgradeModel.Schedule(refreshed.Cards, blocker);
        blockedCards = NextDrawUpgradeModel.Schedule(blockedCards, blocker);
        Require(blockedCards.NextUpgradeReferenceId == 4 && blockedCards.NextDrawUpgrades!.Count == 3,
            "A rejected unique next-draw addition consumed a reference identity.");
        refreshed = refreshed.WithCards(blockedCards);
        refreshed = NextDrawUpgradeModel.DrawSpecific(refreshed, 2, 0, definitions).Context!;
        refreshed = NextDrawUpgradeModel.DrawSpecific(refreshed, 3, 0, definitions).Context!;
        Require(refreshed.FindCard(2)!.Temporary.Upgrades.Select(upgrade => upgrade.Stats.Damage).SequenceEqual([2, 10]),
            "Partial filter rejection did not retain the already-mutated original queue objects.");
        CardUpgradeModifier shared = refreshed.FindCard(2)!.Temporary.Upgrades[1];
        refreshed = refreshed.WithCardRegistry(refreshed.CardRegistry!.Concat(new[] { Card(4, true, shared) }).ToArray())
            .AfterCardEffects();
        Require(refreshed.Cards.NextUpgradeReferenceId == 4 && refreshed.Cards.NextDrawUpgrades!.Count == 0,
            "Effect-end clearing reset the surviving upgrade identity sequence.");
        // Serialization deliberately destroys CLR reference equality. Explicit
        // IDs must still link owned cards and detached registry references.
        refreshed = JsonSerializer.Deserialize<CombatContext>(JsonSerializer.Serialize(refreshed))!;
        string snapshot = JsonSerializer.Serialize(refreshed);
        CombatContext changed = CardMagicPowerModel.Refresh(refreshed, refreshed.FindCard(3)!, out _);
        Require(changed.FindCard(1)!.Temporary.Upgrades.Take(3).Select(upgrade => upgrade.Stats.Damage).SequenceEqual([2, 10, 10]) &&
            changed.FindCard(2)!.Temporary.Upgrades.Select(upgrade => upgrade.Stats.Damage).SequenceEqual([2, 2]) &&
            changed.FindCard(4)!.Temporary.Upgrades.Single().Stats.Damage == 2,
            "Post-clear refresh lost a registry alias or confused same-definition private clones with shared originals.");
        var creation = new CardCreationRule("card-2", CardModifiers.Empty(), null, [], maskDescriptor: refreshed.FindCard(2)!.MaskDescriptor);
        var cloneRoot = refreshed.WithNextCardId(5);
        CardGenerationResult cloned = CardGenerationModel.CloneDetached(cloneRoot, creation, 2);
        Require(cloned.Supported && cloned.Context!.FindCard(5)!.Temporary.Upgrades.All(upgrade => upgrade.SharedReferenceId == null),
            "Cloned cards retained the source's mutable upgrade identities.");
        string expected = JsonSerializer.Serialize(changed);
        Parallel.For(0, 32, branchIndex => Require(JsonSerializer.Serialize(CardMagicPowerModel.Refresh(refreshed,
            refreshed.FindCard(3)!, out _)) == expected, "Shared upgrade branches are not deterministic."));
        Require(JsonSerializer.Serialize(refreshed) == snapshot && root.Cards.NextDrawUpgrades![1].Upgrade.Stats.Damage == 2,
            "Shared upgrade refresh wrote into an immutable parent branch.");
        // Rule descriptors may be reused by the model even though native
        // creates separate upgrade objects. CLR reference equality is not a
        // captured alias and must never join these cards across owners.
        CardUpgradeModifier reusedDefinition = Upgrade("reused-definition", 7);
        var independent = new[] { Card(1, true, reusedDefinition), Card(2, false, reusedDefinition) };
        var independentContext = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 3, 10,
            cardInstances: independent, cardRegistry: independent);
        CombatContext independentChild = CardMagicPowerModel.Refresh(independentContext, independent[0], out _);
        Require(independentChild.FindCard(1)!.Temporary.Upgrades.Single().Stats.Damage == 10 &&
            independentChild.FindCard(2)!.Temporary.Upgrades.Single().Stats.Damage == 7,
            "Reusing a definition descriptor created a false cross-card native alias.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
