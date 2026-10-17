using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCopyFirstConsumeChecks
{
    private const string SpellId = "copy-consume-witness";
    private const string WitnessSpellId = "03b20d75-b919-4f87-96e1-4d35cd556520";
    private const string Effect = RelicModel.CopyCardOnCardPlay;
    private const string ConsumeTrait = "CardTraitExhaustState";

    internal static void Run()
    {
        var condition = new RelicConditionState(new CardStatisticQuery("SubtypeInDeck", "ThisBattle"),
            true, 2, 1, false);
        var relic = new CombatRelicState(RelicCopyCardOnCardPlayModel.ConsumeRelicId,
            RelicCopyCardOnCardPlayModel.ConsumeRelicKey, [Effect], copyCardOnCardPlayModifiers: [
                new RelicCopyCardOnCardPlayModifier(0, 1, "Spell", true, false, [ConsumeTrait], null, [condition])]);
        Require(RelicModel.Validate([relic]) == null, "The exact CopyFirstConsumePlayed definition was rejected.");

        CardMaskDescriptor Descriptor(string dataId, bool consumes) => new(
            new CardMaskDefinition(dataId, "Spell", "Common", false, false, false, [], [], [], [], null,
                false, 0, 0, false, false, false),
            0, consumes ? [new CardTraitValue(ConsumeTrait, ConsumeTrait, 0, 0, true, 0)] : [], []);
        CardInstanceState Card(int id, bool consumes)
        {
            var card = new CardInstanceState(id, SpellId, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
                maskDescriptor: Descriptor(SpellId, consumes), canBeCopied: true);
            return consumes ? CardExhaustModel.Initialize(card, 0) : card;
        }
        var source = Card(1, true);
        var control = Card(2, false);
        var definitions = new BattlePlayRules([], [
            new CardPlayRule(SpellId, SpellId, 0, "Spell", "Discard", null, [], cardType: "Spell", isAnyAbility: false)
        ]);
        CombatContext Context(CardInstanceState[] instances) => new(
            new CardCycleState(instances.Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray(), [], [],
                UnityRng.Seed(912), 0, []), UnityRng.Seed(717), 0, 3, 5,
            cardInstances: instances, cardRegistry: instances, relics: [relic]);

        CombatContext before = Context([source, control]);
        RelicCardPlayMagicPowerResult positive = RelicCardPlayMagicPowerModel.Apply(before, source.InstanceId, definitions, currentTurn: 1);
        CardInstanceState copy = positive.Context.CardInstances!.Single(card => card.InstanceId == positive.Context.NextCardId - 1);
        RelicConditionState firstCondition = positive.Context.Relics!.Single().CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        Require(positive.Supported && positive.Dispatches.Count == 1 && positive.Dispatches[0].Applied &&
            positive.Dispatches[0].ChangedCards.SequenceEqual([copy.InstanceId]) &&
            positive.Context.Cards.Hand.Any(card => card.InstanceId == copy.InstanceId) &&
            copy.Permanent.Upgrades.Count == 0 && copy.Temporary.Upgrades.Count == 0 &&
            copy.MaskDescriptor!.BaseTraits.Any(trait => trait.RuntimeType == ConsumeTrait) &&
            firstCondition.Triggered && firstCondition.DurationTriggerCount == 1 &&
            !before.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "The first Consume Spell was not permanently copied with its once-per-battle condition recorded immutably.");

        RelicCardPlayMagicPowerResult ordinary = RelicCardPlayMagicPowerModel.Apply(before, control.InstanceId, definitions, currentTurn: 1);
        Require(ordinary.Supported && ordinary.Dispatches.Count == 0 && ordinary.Context.NextCardId == before.NextCardId &&
            !ordinary.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "An ordinary Spell without Consume incorrectly triggered the copy relic.");

        CombatContext turnBoundary = RelicConditionModel.EndDuration(positive.Context, "ThisTurn");
        RelicCardPlayMagicPowerResult second = RelicCardPlayMagicPowerModel.Apply(turnBoundary, copy.InstanceId, definitions, currentTurn: 2);
        Require(second.Supported && second.Dispatches.Count == 0 && second.Context.NextCardId == positive.Context.NextCardId &&
            second.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "CopyFirstConsumePlayed incorrectly reset at the turn boundary or copied a second card.");

        CombatRelicState wrongTraits = relic.WithCopyCardOnCardPlayModifiers([
            new RelicCopyCardOnCardPlayModifier(0, 1, "Spell", true, false, [], null, [condition])]);
        Require(RelicModel.Validate([wrongTraits]) != null, "A CopyFirstConsumePlayed owner missing its required Consume trait passed validation.");
        Console.WriteLine("ORIGINAL-COPY-FIRST-CONSUME-CHECKS PASS: native Consume trait gate, permanent copy, once-per-battle boundary and immutable state.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 180 &&
            root.GetProperty("OriginalCopyFirstConsumePlayedPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-copy-first-consume-played" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0) &&
            root.GetProperty("Actions").EnumerateArray().All(entry => entry.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "The original CopyFirstConsumePlayed battle is incomplete or has native/model failures.");

        FixtureValue[] scenarioActions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(scenarioActions.Length == 2, "The native witness must mark only the consumed Spell and its copied-card control.");
        FixtureValue firstEntry = scenarioActions[0];
        BattleTurnState firstBefore = firstEntry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState firstActual = firstEntry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext before = firstBefore.Spawn.Train.Context!;
        CombatContext actual = firstActual.Spawn.Train.Context!;
        int playedId = firstEntry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        CardInstanceState played = before.FindCard(playedId)!;
        CombatRelicState owner = before.Relics!.Single(relic => relic.DataId == RelicCopyCardOnCardPlayModel.ConsumeRelicId);
        RelicCopyCardOnCardPlayModifier rule = owner.CopyCardOnCardPlayModifiers!.Single();
        Require(played.DataId == WitnessSpellId && played.MaskDescriptor?.Definition.CardType == "Spell" &&
            played.MaskDescriptor.BaseTraits.Any(trait => trait.RuntimeType == ConsumeTrait) && played.CanBeCopied == true &&
            firstBefore.PlayRules!.Cards.Single(card => card.DataId == WitnessSpellId).IsAnyAbility == false,
            "The native Consume Spell carrier or direct-play definition differs.");
        Require(owner.AssetKey == RelicCopyCardOnCardPlayModel.ConsumeRelicKey && owner.EffectTypes.SequenceEqual([Effect]) &&
            rule.Count == 1 && rule.CardType == "Spell" && rule.PermanentCopy && !rule.ExcludeChampions &&
            rule.RequiredTraits.SequenceEqual([ConsumeTrait]) && rule.Upgrade == null && rule.Conditions.Count == 1 &&
            rule.Conditions[0].TrackTriggerCount && rule.Conditions[0].Query.Duration == "ThisBattle" &&
            rule.Conditions[0].Value == 1 && !rule.Conditions[0].Triggered,
            "The native CopyFirstConsumePlayed raw payload differs.");
        Require(RelicModel.Validate([owner]) == null, "The captured CopyFirstConsumePlayed owner failed native payload validation.");

        RelicConditionState afterFirst = actual.Relics!.Single(relic => relic.DataId == owner.DataId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        CardInstanceState[] copies = actual.CardInstances!.Where(card => card.DataId == WitnessSpellId && card.InstanceId != playedId).ToArray();
        Require(afterFirst.Triggered && afterFirst.DurationTriggerCount == 1 && copies.Length == 1 &&
            actual.Cards.Hand.Any(card => card.InstanceId == copies[0].InstanceId) &&
            copies[0].Permanent.Upgrades.Count == 0 && copies[0].Temporary.Upgrades.Count == 0 &&
            firstEntry.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The first direct Consume Spell did not produce exactly one permanent hand copy and record the battle trigger.");

        FixtureValue secondEntry = scenarioActions[1];
        BattleTurnState secondBefore = secondEntry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState secondActual = secondEntry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int copiedId = secondEntry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        RelicConditionState beforeSecond = secondBefore.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == owner.DataId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        RelicConditionState afterSecond = secondActual.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == owner.DataId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        Require(copiedId == copies[0].InstanceId && secondBefore.Spawn.Turn == firstBefore.Spawn.Turn &&
            beforeSecond.Triggered && beforeSecond.DurationTriggerCount == 1 && afterSecond.Triggered &&
            afterSecond.DurationTriggerCount == 1 && secondEntry.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            secondActual.Spawn.Train.Context!.CardInstances!.Count(card => card.DataId == WitnessSpellId) == 2,
            "Playing the permanent Consume copy retriggered the once-per-battle relic or diverged from native state.");
        Console.WriteLine("ORIGINAL-COPY-FIRST-CONSUME NATIVE PASS: consumed direct Spell, permanent copy and same-battle duplicate gate.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
