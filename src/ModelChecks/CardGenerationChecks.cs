using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardGenerationChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(213);
        CardUpgradeModifier Upgrade(string id, int amount, bool excluded = false) => new(id, id, new(damage: amount), [], false, false, excluded, 0, 0, []);
        CardUpgradeModifier starting = Upgrade("starting", 1), included = Upgrade("included", 2), excluded = Upgrade("excluded", 8, true),
            temporary = Upgrade("temporary", 3), pending = Upgrade("pending", 4), optional = Upgrade("optional", 5);
        var sourceCard = new CardInstanceState(1, "source", new(new(damage: 7), [starting, included, excluded], 0, []),
            new(new(damage: 6), [temporary, Upgrade("excluded", 9)], 0, []), 3, 9, 4, [], [new(0, "CardEffectDiscardHand", 8)]);
        var creation = new CardCreationRule("source", new(new(), [starting], 0, []), [new(0, "CardEffectDiscardHand", 0)], []);
        var held = new CardInstanceState(2, "held", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, []);
        var context = new CombatContext(new([new(2, "held")], [new(3, "held")], [], rng, 2, []), rng, 0, 4, 2,
            statistics: BattleStatistics.Empty().TrackCards([1, 2, 3]), cardInstances: [sourceCard, held, CardInstanceState.Empty(3, "held")],
            nextAddedTemporaryUpgrades: [pending]);
        string parent = JsonSerializer.Serialize(context);
        var cloneRule = new CardGenerationRule("DeckPileTop", 2, [creation], copyModifiers: true, upgrade: optional);
        CardGenerationResult cloned = CardGenerationModel.Apply(context, cloneRule, 1);
        Require(cloned.Supported && cloned.AddedCards.Select(card => card.InstanceId).SequenceEqual([4, 5]) &&
            cloned.Context!.Cards.Draw.Select(card => card.InstanceId).SequenceEqual([3, 4, 5]), "Generated deck-top identity/order differs.");
        CardInstanceState first = cloned.Context!.CardInstances!.Single(card => card.InstanceId == 4), second = cloned.Context.CardInstances.Single(card => card.InstanceId == 5);
        Require(first.Permanent.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(["starting", "included"]) &&
            first.Temporary.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(["optional", "temporary", "pending"]) &&
            second.Temporary.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(["optional", "temporary"]) &&
            first.Permanent.Offsets.Damage == 7 && first.Temporary.Offsets.Damage == 6 &&
            first.PlayCount == 0 && first.LastPlayedCost == 0 && first.LastForgedAmount == 0 && first.EffectCounters!.Single().Value == 0 &&
            cloned.Context.NextAddedTemporaryUpgrades!.Count == 0 && cloned.Context.Statistics!.Value(4, "TimesDrawn") == 0,
            "Clone initialization lost modifiers/exclusions, consumed pending upgrades more than once, copied history/counters or counted a draw.");
        CardGenerationResult ignoredTemp = CardGenerationModel.Apply(context, new("DiscardPile", 0, [creation], copyModifiers: true,
            ignoreTemporaryModifiers: true, upgrade: optional), 1);
        Require(ignoredTemp.Supported && ignoredTemp.Context!.Cards.Discard.Single().InstanceId == 4 &&
            ignoredTemp.Context.CardInstances!.Single(card => card.InstanceId == 4).Temporary.Upgrades.Select(upgrade => upgrade.DataId)
                .SequenceEqual(["optional", "pending"]) && ignoredTemp.Context.CardInstances.Single(card => card.InstanceId == 4).Temporary.Offsets.Damage == 0,
            "Signed generation count or temporary-copy suppression differs.");
        var conditional = CardGenerationModel.Apply(context, new("DiscardPile", 1, [creation], copyModifiers: true,
            discardCopyUpgrades: [new("included", Upgrade("matched", 2)), new("absent", Upgrade("unmatched", 2)),
                new("", Upgrade("unconditional", 2))]), 1);
        Require(conditional.Supported && conditional.Context!.CardInstances!.Single(card => card.InstanceId == 4)
            .Temporary.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(["matched", "unconditional", "temporary", "pending"]),
            "Discard generation did not apply matching/unconditional upgrades before copied and one-shot upgrades.");
        var detached = CardGenerationModel.Apply(context, new("DiscardPile", 1, [creation], copyModifiers: true,
            discardCopyUpgrades: [new("", Upgrade("unconditional", 2))]), 999);
        Require(detached.Supported && detached.Context!.CardInstances!.Single(card => card.InstanceId == 4)
            .Temporary.Upgrades.Select(upgrade => upgrade.DataId).SequenceEqual(["pending"]),
            "Generation without a source copied modifiers or applied source-dependent discard upgrades.");
        var full = new CombatContext(new([new(1, "source"), new(2, "held")], context.Cards.Draw, [], rng, 2, []),
            rng, 0, 4, 2, cardInstances: context.CardInstances, nextAddedTemporaryUpgrades: [pending]);
        CardGenerationResult refused = CardGenerationModel.Apply(full, new("HandPile", 3, [creation]), 1);
        Require(refused.Supported && refused.AddedCards.Count == 0 && refused.Context!.NextCardId == 4 &&
            refused.Context.NextAddedTemporaryUpgrades!.Count == 1 && refused.Context.BattleRng.Equals(rng.Range(0, 1).State.Range(0, 1).State.Range(0, 1).State),
            "Full-hand generation must select before refusal and preserve identity allocation/pending upgrades.");
        CardGenerationResult duplicate = CardGenerationModel.Apply(full, new("HandPile", 2, [creation], skipDuplicateInHand: true));
        Require(duplicate.Supported && duplicate.AddedCards.Count == 0 && duplicate.Context!.NextCardId == 4,
            "Duplicate generation allocated a card before skipping it.");
        CardGenerationResult empty = CardGenerationModel.Apply(context, new("DeckPileRandom", -5, []));
        Require(empty.Supported && empty.Context!.BattleRng.Equals(rng) && empty.Context.NextCardId == 4 && empty.Context.NextAddedTemporaryUpgrades!.Count == 1,
            "An empty filtered pool consumed selection/placement RNG or pending upgrades.");
        foreach (string pile in new[] { "HandPile", "DiscardPile", "DeckPile", "DeckPileTop", "DeckPileRandom" })
        {
            CardGenerationResult result = CardGenerationModel.Apply(context, new(pile, -1, [creation]));
            Require(result.Supported && result.AddedCards.Count == 1 && result.Context!.NextCardId == 5 && result.Context.Cards.DrawModifier == 2,
                "Generation rejected a supported pile or changed the pending draw modifier: " + pile);
        }
        Require(!CardGenerationModel.Apply(context, new("ExhaustedPile", 1, [creation])).Supported &&
            !CardGenerationModel.Apply(context, new("DeckPile", 1, [new("bad", CardModifiers.Empty(), null, ["Unknown setup trait"])])).Supported,
            "Unknown generation routing/setup returned a partial child.");
        CardUpgradeModifier sourceUpgrade = new("shared-upgrade", "NativeUpgrade", new(damage: 2), [new("armor", 3)],
            true, true, false, 0, 0, [], copyRemoveOnDiscard: false, copyUnique: false, copyExcludeFromClones: true);
        CardUpgradeModifier existingUpgrade = new("shared-upgrade", "NativeUpgrade", new(damage: 1), [new("armor", 1)],
            false, true, false, 0, 0, []);
        var copySource = new CardInstanceState(40, "source-card", new(new(), [sourceUpgrade], 0, []),
            CardModifiers.Empty(), 0, 0, 0, []);
        var copyContext = new CombatContext(new([], [new(40, "source-card")], [], rng, 2, []), rng, 0, 41, 2,
            cardInstances: [copySource]);
        var copyCreation = new CardCreationRule("generated-card", new(new(), [existingUpgrade], 0, []), null, []);
        CardGenerationResult copied = CardGenerationModel.Apply(copyContext,
            new("DeckPile", 1, [copyCreation], copyModifiers: true), 40);
        CardUpgradeModifier[] copiedUpgrades = copied.Context!.CardInstances!.Single(card => card.InstanceId == 41)
            .Permanent.Upgrades.ToArray();
        Require(copied.Supported && copiedUpgrades.Length == 2 && copiedUpgrades[0].Unique &&
            !copiedUpgrades[1].Unique && !copiedUpgrades[1].RemoveOnDiscard && copiedUpgrades[1].ExcludeFromClones &&
            copiedUpgrades[1].Stats.Damage == 2 && copiedUpgrades[1].Statuses.Single().Stacks == 3,
            "AddCard upgrade copy did not restore base identity flags while preserving copied values.");
        var missingInstances = new CombatContext(context.Cards, rng, 0, 4, 2);
        Require(!CardGenerationModel.Apply(missingInstances, new("DeckPile", 1, [creation])).Supported,
            "Modified generation silently lost upgrade and effect-counter state when instance ownership was absent.");
        var drawRandom = CardGenerationModel.Apply(context, new("DeckPileRandom", 1, [creation]));
        Require(drawRandom.Context!.Cards.Draw.Select(card => card.InstanceId).SequenceEqual([4, 3]) &&
            drawRandom.Context.BattleRng.Equals(rng.Range(0, 1).State.Range(0, 1).State), "Deck-random placement must exclude the current top index.");
        var generation = new CardActionEffect("Generate", "Room", 0, false, false, [], range: new(-100, 100, float.NaN), generation: new("DeckPile", 0, [creation]));
        var room = new RoomCombatState(0, false, [], [], context);
        var emptyEffect = new CardActionEffect("Generate", "Room", 0, false, false, [], generation: new("DeckPile", 1, []));
        var emptyChain = CardSpellModel.Apply(room, [emptyEffect], 0);
        Require(emptyChain.Supported && emptyChain.State!.Context!.NextAddedTemporaryUpgrades!.Count == 0 &&
            emptyChain.State.Context.NextCardId == context.NextCardId && emptyChain.State.Context.BattleRng.Equals(rng) &&
            context.NextAddedTemporaryUpgrades!.Count == 1,
            "An empty spell effect chain did not clear pending generated-card upgrades at the native chain boundary.");
        Require(CardSpellModel.Apply(room, [generation], 0).Supported &&
            !CardSpellModel.TestPlay(new(0, false, [], [], context, preview: true), [generation], 0).CanPlay &&
            CardSpellModel.Apply(new(0, false, [], [], new CombatContext(context.Cards, rng, 0, 4, 2, allScenarioBossesDead: true)), [generation], 0)
                .State!.Context!.NextCardId == 4,
            "Generation sampled its unused integer range or ignored preview/post-boss gates.");
        var required = new CardActionEffect("Generate", "Room", 0, false, false, [], generation: new("DeckPile", 1, [creation], requireHandSpace: true));
        Require(!CardSpellModel.TestPlay(new(0, false, [], [], full), [required], 0).CanPlay,
            "Require-space generation ignored the original full hand during casting tests.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardGenerationModel.Apply(context, cloneRule, 1).Context) == JsonSerializer.Serialize(cloned.Context),
            "Parallel generated card branches diverged."));
        Require(JsonSerializer.Serialize(context) == parent, "Generation mutated its source cards, pending upgrades or RNG.");
        Console.WriteLine("GENERATION-CHECKS PASS: five piles, signed counts, pool/full-hand/duplicate timing, clone offsets/upgrades/exclusions, fresh counters/history, one-shot upgrades, unused ranges, gates and 32 parallel branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        int records = 0, cards = 0, refused = 0, empty = 0, copied = 0, pending = 0, initialized = 0, discardUpgrades = 0, unitTriggers = 0;
        var piles = new HashSet<string>();
        foreach (FixtureValue record in fixture.GetProperty("CardGenerations").EnumerateArray())
        {
            CombatContext before = record.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = record.GetProperty("Actual").Deserialize<CombatContext>()!;
            CardGenerationRule rule = record.GetProperty("Rule").Deserialize<CardGenerationRule>()!;
            CardGenerationResult predicted = CardGenerationModel.Apply(before, rule, record.GetProperty("SourceCardId").GetInt32());
            Require(predicted.Supported, "Native card generation unsupported: " + predicted.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.Context), JsonSerializer.Serialize(actual));
            Require(difference == null, "Native card generation differs: " + difference);
            records++; cards += predicted.AddedCards.Count; piles.Add(rule.Destination);
            if (record.TryGetProperty("Origin", out FixtureValue origin) && origin.GetString() == "Unit") unitTriggers++;
            if (predicted.AddedCards.Count == 0 && rule.Pool.Count > 0) refused++;
            if (rule.Pool.Count == 0) empty++;
            if (rule.CopyModifiers && predicted.AddedCards.Count > 0) copied++;
            int sourceId = record.GetProperty("SourceCardId").GetInt32();
            CardInstanceState? copying = before.CardInstances?.FirstOrDefault(card => card.InstanceId == sourceId)
                ?? before.CardRegistry?.FirstOrDefault(card => card.InstanceId == sourceId);
            if (rule.CopyModifiers && rule.Destination == "DiscardPile" && copying != null && predicted.AddedCards.Count > 0)
                discardUpgrades += rule.DiscardCopyUpgrades.Count(item => item.Upgrade.ExternalInteractions.Count == 0 &&
                    (item.RequiredUpgradeId.Length == 0 || copying.Permanent.Upgrades.Concat(copying.Temporary.Upgrades)
                        .Any(upgrade => upgrade.DataId == item.RequiredUpgradeId)));
            if (before.NextAddedTemporaryUpgrades?.Count > 0 && actual.NextAddedTemporaryUpgrades?.Count == 0) pending++;
            initialized += predicted.AddedCards.Count(token => actual.CardInstances!.Single(card => card.InstanceId == token.InstanceId).EffectCounters?.Any() == true);
        }
        Require(records > 0 && cards > 0 && refused > 0 && empty > 0 && copied > 0 && pending > 0 && initialized > 0 && piles.Count == 5,
            "Native generation oracle lacks meaningful placement, refusals, empty pools, modifier copies, pending upgrades or fresh effect counters.");
        Require(fixture.GetProperty("ModifierScenario").GetString() != "generation-lethal" || discardUpgrades > 0,
            "Native lethal generation oracle lacks source-dependent discard upgrades.");
        Require(fixture.GetProperty("ModifierScenario").GetString() != "generation" || unitTriggers > 0,
            "Native generation oracle lacks modified generation from unit triggers.");
        Console.WriteLine($"NATIVE-GENERATION-COVERAGE PASS: {records} exact effects, {cards} cards, {refused} refusals, {empty} empty pools, {copied} copy operations, {pending} pending-upgrade consumptions, {initialized} initialized card-effect states, {discardUpgrades} matching discard upgrades and {unitTriggers} unit triggers; all five piles.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
