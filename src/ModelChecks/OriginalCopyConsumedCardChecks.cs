using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCopyConsumedCardChecks
{
    private const string SpellId = "copy-consumed-witness";
    private const string ControlSpellId = "copy-consumed-control";
    private const string WitnessSpellId = "03b20d75-b919-4f87-96e1-4d35cd556520";
    private const string Effect = RelicModel.CopyCardOnCardPlay;
    private const string ConsumeTrait = "CardTraitExhaustState";

    internal static void Run()
    {
        var condition = new RelicConditionState(new CardStatisticQuery("SubtypeInDeck", "ThisBattle"),
            true, 6, 1, false);
        var relic = new CombatRelicState(RelicCopyCardOnCardPlayModel.CopyConsumedId,
            RelicCopyCardOnCardPlayModel.CopyConsumedKey, [Effect], copyCardOnCardPlayModifiers: [
                new RelicCopyCardOnCardPlayModifier(0, 2, "Invalid", false, false, [ConsumeTrait], null, [condition])]);
        Require(RelicModel.Validate([relic]) == null, "The exact CopyConsumedCard definition was rejected.");

        CardMaskDescriptor Descriptor(string dataId, bool consumes) => new(
            new CardMaskDefinition(dataId, "Spell", "Common", false, false, false, [], [], [], [], null,
                false, 0, 0, false, false, false),
            0, consumes ? [new CardTraitValue(ConsumeTrait, ConsumeTrait, 0, 0, true, 0)] : [], []);
        CardInstanceState Card(int id, string dataId, bool consumes)
        {
            var card = new CardInstanceState(id, dataId, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
                maskDescriptor: Descriptor(dataId, consumes), canBeCopied: true);
            return consumes ? CardExhaustModel.Initialize(card, 0) : card;
        }
        var source = Card(1, SpellId, true);
        var control = Card(2, ControlSpellId, false);
        var definitions = new BattlePlayRules([], [
            new CardPlayRule(SpellId, SpellId, 0, "Spell", "Discard", null, [], cardType: "Spell", isAnyAbility: false),
            new CardPlayRule(ControlSpellId, ControlSpellId, 0, "Spell", "Discard", null, [], cardType: "Spell", isAnyAbility: false)
        ]);
        CombatContext Context(CardInstanceState[] instances) => new(
            new CardCycleState(instances.Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray(), [], [],
                UnityRng.Seed(912), 0, []), UnityRng.Seed(717), 0, 3, 5,
            cardInstances: instances, cardRegistry: instances, relics: [relic]);

        CombatContext before = Context([source, control]);
        RelicCardPlayMagicPowerResult positive = RelicCardPlayMagicPowerModel.Apply(before, source.InstanceId, definitions, currentTurn: 1);
        CardInstanceState[] copies = positive.Context.CardInstances!.Where(card => card.DataId == SpellId && card.InstanceId != source.InstanceId).ToArray();
        RelicConditionState firstCondition = positive.Context.Relics!.Single().CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        string firstResult = $"supported={positive.Supported}, applied={positive.Dispatches.SingleOrDefault()?.Applied}, copies={copies.Length}, changed={string.Join(',', positive.Dispatches.SelectMany(dispatch => dispatch.ChangedCards))}, hand={string.Join(',', positive.Context.Cards.Hand.Select(card => card.InstanceId))}, traitCopies={copies.Count(copy => copy.MaskDescriptor!.BaseTraits.Any(trait => trait.RuntimeType == ConsumeTrait))}, condition={firstCondition.Triggered}/{firstCondition.DurationTriggerCount}";
        Require(positive.Supported && positive.Dispatches.Count == 1 && positive.Dispatches[0].Applied && copies.Length == 2 &&
            positive.Dispatches[0].ChangedCards.SequenceEqual(copies.Select(card => card.InstanceId)) &&
            copies.All(copy => positive.Context.Cards.Hand.Any(card => card.InstanceId == copy.InstanceId) &&
                copy.MaskDescriptor!.BaseTraits.Any(trait => trait.RuntimeType == ConsumeTrait) &&
                copy.Permanent.Upgrades.Count == 0 && copy.Temporary.Upgrades.Count == 0) &&
            firstCondition.Triggered && firstCondition.DurationTriggerCount == 1 &&
            !before.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "The first Consume card was not copied twice with one immutable ThisBattle trigger update: " + firstResult);

        RelicCardPlayMagicPowerResult ordinary = RelicCardPlayMagicPowerModel.Apply(before, control.InstanceId, definitions, currentTurn: 1);
        Require(ordinary.Supported && ordinary.Dispatches.Count == 0 && ordinary.Context.NextCardId == before.NextCardId &&
            !ordinary.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "A card without Consume incorrectly triggered CopyConsumedCard.");

        CombatContext turnBoundary = RelicConditionModel.EndDuration(positive.Context, "ThisTurn");
        RelicCardPlayMagicPowerResult second = RelicCardPlayMagicPowerModel.Apply(turnBoundary, copies[0].InstanceId, definitions, currentTurn: 2);
        Require(second.Supported && second.Dispatches.Count == 0 && second.Context.NextCardId == positive.Context.NextCardId &&
            second.Context.CardInstances!.Count(card => card.DataId == SpellId) == 3 &&
            second.Context.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered &&
            second.Context.Relics[0].CopyCardOnCardPlayModifiers![0].Conditions[0].DurationTriggerCount == 1,
            "CopyConsumedCard did not retain its once-per-battle gate after a turn boundary.");
        Console.WriteLine("ORIGINAL-COPY-CONSUMED-CARD-CHECKS PASS: Any card type, Consume gate, two copies and once-per-battle boundary.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 181 &&
            root.GetProperty("OriginalCopyConsumedCardPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-copy-consumed-card" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0) &&
            root.GetProperty("Actions").EnumerateArray().All(entry => entry.GetProperty("Difference").ValueKind == FixtureKind.Null),
            "The original CopyConsumedCard battle is incomplete or has native/model failures.");

        FixtureValue[] scenarioActions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(scenarioActions.Length == 2, "The native witness must mark only the consumed Spell and one copied-card control.");
        FixtureValue firstEntry = scenarioActions[0];
        BattleTurnState firstBefore = firstEntry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState firstActual = firstEntry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext before = firstBefore.Spawn.Train.Context!;
        CombatContext actual = firstActual.Spawn.Train.Context!;
        int playedId = firstEntry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        CardInstanceState played = before.FindCard(playedId)!;
        CombatRelicState owner = before.Relics!.Single(relic => relic.DataId == RelicCopyCardOnCardPlayModel.CopyConsumedId);
        RelicCopyCardOnCardPlayModifier rule = owner.CopyCardOnCardPlayModifiers!.Single();
        Require(played.DataId == WitnessSpellId && played.MaskDescriptor?.Definition.CardType == "Spell" &&
            played.MaskDescriptor.BaseTraits.Any(trait => trait.RuntimeType == ConsumeTrait) && played.CanBeCopied == true &&
            firstBefore.PlayRules!.Cards.Single(card => card.DataId == WitnessSpellId).IsAnyAbility == false,
            "The native Consume Spell carrier or direct-play definition differs.");
        Require(owner.AssetKey == RelicCopyCardOnCardPlayModel.CopyConsumedKey && owner.EffectTypes.SequenceEqual([Effect]) &&
            rule.Count == 2 && rule.CardType == "Invalid" && !rule.PermanentCopy && !rule.ExcludeChampions &&
            rule.RequiredTraits.SequenceEqual([ConsumeTrait]) && rule.Upgrade == null && rule.Conditions.Count == 1 &&
            rule.Conditions[0].TrackTriggerCount && rule.Conditions[0].Query.Type == "SubtypeInDeck" &&
            rule.Conditions[0].Query.Duration == "ThisBattle" && rule.Conditions[0].Comparator == 6 &&
            rule.Conditions[0].Value == 1 && !rule.Conditions[0].AllowMultiple,
            "The native CopyConsumedCard raw payload differs.");
        Require(RelicModel.Validate([owner]) == null, "The captured CopyConsumedCard owner failed native payload validation.");

        RelicConditionState afterFirst = actual.Relics!.Single(relic => relic.DataId == owner.DataId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        CardInstanceState[] copies = actual.CardInstances!.Where(card => card.DataId == WitnessSpellId && card.InstanceId != playedId).ToArray();
        Require(afterFirst.Triggered && afterFirst.DurationTriggerCount == 1 && copies.Length == 2 &&
            copies.All(copy => actual.Cards.Hand.Any(card => card.InstanceId == copy.InstanceId) &&
                copy.Permanent.Upgrades.Count == 0 && copy.Temporary.Upgrades.Count == 0) &&
            copies.All(copy => actual.Statistics?.DeckCards?.Contains(copy.InstanceId) != true) &&
            firstEntry.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The first Consume Spell did not produce exactly two non-permanent hand copies and record its trigger.");

        FixtureValue secondEntry = scenarioActions[1];
        BattleTurnState secondBefore = secondEntry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState secondActual = secondEntry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int copiedId = secondEntry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        RelicConditionState beforeSecond = secondBefore.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == owner.DataId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        RelicConditionState afterSecond = secondActual.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == owner.DataId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        Require(copies.Any(copy => copy.InstanceId == copiedId) && secondBefore.Spawn.Turn == firstBefore.Spawn.Turn &&
            beforeSecond.Triggered && beforeSecond.DurationTriggerCount == 1 && afterSecond.Triggered &&
            afterSecond.DurationTriggerCount == 1 && secondEntry.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            secondActual.Spawn.Train.Context!.CardInstances!.Count(card => card.DataId == WitnessSpellId) == 3,
            "Playing one generated Consume copy retriggered the once-per-battle relic or diverged from native state.");
        Console.WriteLine("ORIGINAL-COPY-CONSUMED-CARD NATIVE PASS: Consume gate, two non-permanent copies and same-battle duplicate gate.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
