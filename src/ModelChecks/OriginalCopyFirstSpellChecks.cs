using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCopyFirstSpellChecks
{
    private const string RelicId = RelicCopyCardOnCardPlayModel.RelicId;
    private const string RelicKey = RelicCopyCardOnCardPlayModel.RelicKey;
    private const string Effect = RelicModel.CopyCardOnCardPlay;
    private const string UpgradeId = "80a5edf4-6dc8-41a7-ab4e-9d0e914e0848";
    private const string SpellA = "copy-spell-a";
    private const string SpellB = "copy-spell-b";
    private const string AbilitySpell = "copy-spell-ability";
    private const string WitnessSpellId = "03b20d75-b919-4f87-96e1-4d35cd556520";

    internal static void Run()
    {
        var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade(UpgradeId, new CardStatModifier(), [],
            false, false, false, false), "AddEphemeral", false, false,
            [new CardTraitValue("CardTraitEphemeral", "CardTraitEphemeral", 0, 0, true, 0)], [], false, [], [], []);
        var upgrade = new CardUpgradeModifier(UpgradeId, "AddEphemeral", new CardStatModifier(), [], false, false,
            false, 0, 0, [], maskMetadata: new CardUpgradeMaskMetadata([], false, false, false, false), lifecycle: lifecycle);
        var condition = new RelicConditionState(new CardStatisticQuery("TimesPlayed", "ThisTurn"), true, 2, 1, false);
        var relic = new CombatRelicState(RelicId, RelicKey, [Effect], copyCardOnCardPlayModifiers: [
            new RelicCopyCardOnCardPlayModifier(0, 1, "Spell", false, false, [], upgrade, [condition])]);
        Require(RelicModel.Validate([relic]) == null, "The exact original copy-first-spell payload was rejected.");

        CardInstanceState Card(int id, string dataId, bool canCopy = true) => new(id, dataId,
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: new CardMaskDescriptor(new CardMaskDefinition(dataId, "Spell", "Common", false,
                false, false, [], [], [], [], null, false, 0, 0, false, false, false), 1, [], []),
            canBeCopied: canCopy);
        CardPlayRule Play(string dataId, bool ability = false) => new(dataId, dataId, 0, "Spell", "Discard",
            null, [], cardType: "Spell", isAnyAbility: ability);
        CardInstanceState[] instances = [Card(1, SpellA), Card(2, SpellB), Card(3, AbilitySpell)];
        var definitions = new BattlePlayRules([], [Play(SpellA), Play(SpellB), Play(AbilitySpell, true)]);
        CombatContext Context(int handLimit, int handCount = 3, bool secondCopyable = true)
        {
            CardInstanceState[] contextInstances = secondCopyable ? instances :
                [instances[0], Card(2, SpellB, canCopy: false), instances[2]];
            return new CombatContext(
            new CardCycleState(contextInstances.Take(handCount).Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray(),
                [], [], UnityRng.Seed(912), 0, []), UnityRng.Seed(717), 0, 4, handLimit,
            cardInstances: contextInstances, cardRegistry: contextInstances,
            otherPiles: [new CardPileState("Purged", []), new CardPileState("DiscardBuffer", [])],
            relics: [relic]);
        }

        CombatContext copySource = Context(4);
        RelicCardPlayMagicPowerResult first = RelicCardPlayMagicPowerModel.Apply(copySource, 1, definitions, currentTurn: 1);
        Require(first.Supported, "The first direct Spell copy model failed: " + first.UnsupportedReason);
        CardInstanceState firstCopy = first.Context.CardInstances!.Single(card => card.InstanceId == first.Context.NextCardId - 1);
        Require(first.Supported && first.Context.Cards.Hand.Any(card => card.InstanceId == firstCopy.InstanceId) &&
            firstCopy.Temporary.Upgrades.Single().DataId == UpgradeId &&
            first.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered &&
            first.Context.Relics[0].CopyCardOnCardPlayModifiers![0].Conditions[0].DurationTriggerCount == 1,
            "The first direct Spell was not copied with temporary Ephemeral and a recorded once-per-turn condition.");
        Require(first.Context.BattleRng.Equals(copySource.BattleRng),
            "Deterministic copy of the played card unexpectedly consumed Battle RNG.");

        int copiesBeforeSecond = first.Context.CardInstances!.Count(card => card.DataId == SpellA &&
            card.Temporary.Upgrades.Any(item => item.DataId == UpgradeId));
        RelicCardPlayMagicPowerResult second = RelicCardPlayMagicPowerModel.Apply(first.Context, 2, definitions, currentTurn: 1);
        int copiesAfterSecond = second.Context.CardInstances!.Count(card => card.DataId == SpellA &&
            card.Temporary.Upgrades.Any(item => item.DataId == UpgradeId));
        Require(second.Supported && copiesBeforeSecond == copiesAfterSecond &&
            second.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "A second same-turn Spell generated another copy or lost the one-shot condition.");

        CombatContext reset = RelicConditionModel.EndDuration(second.Context, "ThisTurn");
        RelicCardPlayMagicPowerResult nextTurn = RelicCardPlayMagicPowerModel.Apply(reset, 2, definitions, currentTurn: 2);
        Require(nextTurn.Supported && nextTurn.Context.CardInstances!.Count(card => card.DataId == SpellB &&
                card.Temporary.Upgrades.Any(item => item.DataId == UpgradeId)) == 1 &&
            nextTurn.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "The once-per-turn copy condition did not reset for the next turn.");

        RelicCardPlayMagicPowerResult ability = RelicCardPlayMagicPowerModel.Apply(Context(4), 3, definitions, currentTurn: 1);
        Require(ability.Supported && ability.Context.CardInstances!.Count == instances.Length &&
            !ability.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "A Spell ability incorrectly triggered the direct-play copy relic.");

        RelicCardPlayMagicPowerResult uncopyable = RelicCardPlayMagicPowerModel.Apply(
            Context(4, secondCopyable: false), 2, definitions, currentTurn: 1);
        Require(uncopyable.Supported && uncopyable.Context.CardInstances!.Count == instances.Length &&
            !uncopyable.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "A Spell rejected by native CanBeCopied incorrectly triggered the relic.");

        RelicCardPlayMagicPowerResult fullHand = RelicCardPlayMagicPowerModel.Apply(Context(3, handCount: 3), 1, definitions, currentTurn: 1);
        int purgedId = fullHand.Context.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Single().InstanceId;
        Require(fullHand.Supported && fullHand.Context.Cards.Discard.Count == 0 &&
            fullHand.Context.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Count == 1 &&
            fullHand.Context.FindCard(purgedId)?.Temporary.Upgrades.Any(item => item.DataId == UpgradeId) == true,
            "A full-hand ephemeral copy was not routed through Discard and immediately Purged.");

        var missing = new CombatRelicState(RelicId, RelicKey, [Effect]);
        Require(RelicModel.Validate([missing]) != null,
            "An uncaptured copy-on-card-play payload passed validation.");
        Require(relic.WithStatusMagnitudes([]).CopyCardOnCardPlayModifiers?.Count == 1,
            "Copying another relic payload dropped the copy-on-card-play definition.");
        Console.WriteLine("ORIGINAL-COPY-FIRST-SPELL-CHECKS PASS: direct Spell, copyability and ability gates, same-turn reset, Ephemeral copy and full-hand purge.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 173 &&
            root.GetProperty("OriginalCopyFirstSpellPlayedPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-copy-first-spell-played" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0) &&
            root.GetProperty("Actions").EnumerateArray().All(entry => entry.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "The original CopyFirstSpellPlayed battle is incomplete or has native/model failures.");

        FixtureValue[] scenarioActions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(scenarioActions.Length == 2, "The native witness must mark only its direct Spell and same-turn Ephemeral copy as scenario actions.");
        FixtureValue firstEntry = scenarioActions[0];
        BattleTurnState firstBefore = firstEntry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState firstActual = firstEntry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext before = firstBefore.Spawn.Train.Context!;
        CombatContext actual = firstActual.Spawn.Train.Context!;
        int playedId = firstEntry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        string playedData = before.FindCard(playedId)?.DataId ?? "";
        CardPlayRule witnessRule = firstBefore.PlayRules!.Cards.Single(rule => rule.DataId == WitnessSpellId);
        CombatRelicState owner = before.Relics!.Single(relic => relic.DataId == RelicId);
        RelicCopyCardOnCardPlayModifier rule = owner.CopyCardOnCardPlayModifiers!.Single();
        Require(playedData == WitnessSpellId && witnessRule.CardType == "Spell" && witnessRule.Effects.Count == 0 &&
            witnessRule.IsAnyAbility == false && firstBefore.Spawn.Turn == 0 &&
            owner.AssetKey == RelicKey && owner.EffectTypes.SequenceEqual([Effect]) && rule.Count == 1 &&
            rule.CardType == "Spell" && !rule.PermanentCopy && !rule.ExcludeChampions &&
            rule.Upgrade?.DataId == UpgradeId && rule.Upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == "CardTraitEphemeral") == true &&
            rule.Conditions.Count == 1 && rule.Conditions[0].TrackTriggerCount && rule.Conditions[0].Value == 1 &&
            !rule.Conditions[0].Triggered && RelicModel.Validate([owner]) == null,
            "The native capture differs from the unchanged CopyFirstSpellPlayed payload.");

        RelicConditionState afterFirst = actual.Relics!.Single(relic => relic.DataId == RelicId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        CardInstanceState[] ephemeralCopies = actual.CardInstances!.Where(card => card.DataId == playedData &&
            card.InstanceId != playedId && card.Temporary.Upgrades.Any(upgrade => upgrade.DataId == UpgradeId)).ToArray();
        Require(afterFirst.Triggered && afterFirst.DurationTriggerCount == 1 && ephemeralCopies.Length == 1 &&
            actual.Cards.Hand.Any(card => card.InstanceId == ephemeralCopies[0].InstanceId) &&
            firstEntry.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The first direct spell did not produce exactly one ephemeral hand copy and record the trigger.");

        FixtureValue secondSpell = scenarioActions.FirstOrDefault(entry =>
        {
            BattleTurnState state = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int id = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            return id != playedId && state.Spawn.Train.Context!.FindCard(id)?.DataId == WitnessSpellId;
        });
        Require(secondSpell.ValueKind != FixtureKind.Undefined, "Missing same-turn second-spell control action.");
        BattleTurnState secondBefore = secondSpell.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState secondActual = secondSpell.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RelicConditionState beforeSecond = secondBefore.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == RelicId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        RelicConditionState afterSecond = secondActual.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == RelicId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        Require(secondBefore.Spawn.Turn == firstBefore.Spawn.Turn && beforeSecond.Triggered && afterSecond.Triggered &&
            afterSecond.DurationTriggerCount == 1 && secondSpell.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            secondActual.Spawn.Train.Context!.CardInstances!.Count(card => card.DataId == WitnessSpellId &&
                card.Temporary.Upgrades.Any(upgrade => upgrade.DataId == UpgradeId)) == 1,
            "A second same-turn spell re-triggered or diverged from the native once-per-turn relic.");
        Console.WriteLine($"ORIGINAL-COPY-FIRST-SPELL NATIVE PASS: {scenarioActions.Length} isolated card plays, same-turn gate and ephemeral first-spell copy.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
