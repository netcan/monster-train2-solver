using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMothersRemnant3Checks
{
    private const string OwnerId = RelicCopyCardOnCardPlayModel.MothersRemnant3Id;
    private const string OwnerKey = RelicCopyCardOnCardPlayModel.MothersRemnant3Key;
    private const string Effect = RelicModel.CopyCardOnCardPlay;
    private const string SpellId = "03b20d75-b919-4f87-96e1-4d35cd556520";
    private const string ChampionId = "mothers-remnant3-champion-control";

    internal static void Run()
    {
        var condition = new RelicConditionState(new CardStatisticQuery("SubtypeInDeck", "ThisTurn"),
            true, 3, 1, false);
        var owner = new CombatRelicState(OwnerId, OwnerKey, [Effect], copyCardOnCardPlayModifiers: [
            new RelicCopyCardOnCardPlayModifier(0, 1, "Invalid", true, true, [], null, [condition])]);
        Require(RelicModel.Validate([owner]) == null, "The exact MothersRemnant3 owner was rejected.");

        CardInstanceState Card(int id, string dataId, string cardType, bool? champion) => new(id, dataId,
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: new CardMaskDescriptor(new CardMaskDefinition(dataId, cardType, "Common", false,
                false, false, [], [], [], [], null, false, 0, 0, false, false, false,
                canScaleWithMagicPower: false, isChampionCard: champion), 0, [], []), canBeCopied: true);
        var source = Card(1, SpellId, "Spell", false);
        var control = Card(2, SpellId, "Spell", false);
        var champion = Card(3, ChampionId, "Monster", true);
        var unknownChampion = Card(4, "mothers-remnant3-unknown-champion", "Spell", null);
        var playRules = new BattlePlayRules([], [
            new CardPlayRule(SpellId, SpellId, 0, "Spell", "Discard", null, [], cardType: "Spell", isAnyAbility: false),
            new CardPlayRule(ChampionId, ChampionId, 0, "SpawnMonster", "Room", null, [], cardType: "Monster", isAnyAbility: false),
            new CardPlayRule(unknownChampion.DataId, unknownChampion.DataId, 0, "Spell", "Discard", null, [],
                cardType: "Spell", isAnyAbility: false)
        ]);

        CombatContext Context(int maxHand, CardInstanceState[] instances, int[] hand, int[]? draw = null,
            CombatRelicState? relic = null)
        {
            CardToken Token(int id) => new(id, instances.Single(card => card.InstanceId == id).DataId);
            return new CombatContext(new CardCycleState(hand.Select(Token).ToArray(), (draw ?? []).Select(Token).ToArray(), [],
                    UnityRng.Seed(741), 0, []), UnityRng.Seed(914), 0,
                instances.Max(card => card.InstanceId) + 1, maxHand, statistics: BattleStatistics.Empty(
                    deckCards: instances.Select(card => card.InstanceId).ToArray()),
                cardInstances: instances, cardRegistry: instances, relics: [relic ?? owner]);
        }

        CombatContext sourceState = Context(5, [source, control], [source.InstanceId, control.InstanceId]);
        RelicCardPlayMagicPowerResult positive = RelicCardPlayMagicPowerModel.Apply(sourceState,
            source.InstanceId, playRules, currentTurn: 1);
        CardInstanceState copy = positive.Context.CardInstances!.Single(card =>
            card.DataId == SpellId && card.InstanceId >= sourceState.NextCardId);
        RelicConditionState afterFirst = positive.Context.Relics!.Single().CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        Require(positive.Supported && positive.Dispatches.Single().Applied && positive.Context.Cards.Hand.Any(card =>
                card.InstanceId == copy.InstanceId) && positive.Context.Statistics!.DeckCards!.Contains(copy.InstanceId) &&
            copy.Permanent.Upgrades.Count == 0 && copy.Temporary.Upgrades.Count == 0 && afterFirst.Triggered &&
            afterFirst.DurationTriggerCount == 1 && sourceState.BattleRng.Equals(positive.Context.BattleRng),
            "The first eligible card was not permanently copied to hand with an immutable once-per-turn update.");

        RelicCardPlayMagicPowerResult sameTurn = RelicCardPlayMagicPowerModel.Apply(positive.Context,
            control.InstanceId, playRules, currentTurn: 1);
        Require(sameTurn.Supported && sameTurn.Dispatches.Count == 0 &&
            sameTurn.Context.NextCardId == positive.Context.NextCardId &&
            sameTurn.Context.Relics!.Single().CopyCardOnCardPlayModifiers!.Single().Conditions.Single().DurationTriggerCount == 1,
            "A second eligible card copied again before the turn boundary.");

        CardInstanceState fullSource = Card(10, SpellId, "Spell", false);
        CardInstanceState[] fullHandCards = [fullSource, Card(11, "full-hand-a", "Spell", false),
            Card(12, "full-hand-b", "Spell", false)];
        CombatContext fullHand = Context(2, fullHandCards, [11, 12], draw: [10]);
        RelicCardPlayMagicPowerResult deckCopy = RelicCardPlayMagicPowerModel.Apply(fullHand,
            fullSource.InstanceId, playRules, currentTurn: 1);
        CardInstanceState deckCard = deckCopy.Context.CardInstances!.Single(card => card.InstanceId >= fullHand.NextCardId);
        Require(deckCopy.Supported && deckCopy.Dispatches.Single().Applied && deckCopy.Context.Cards.Hand.Count == 2 &&
            deckCopy.Context.Cards.Draw.First().InstanceId == deckCard.InstanceId &&
            deckCopy.Context.Cards.Discard.Count == 0 && deckCopy.Context.Statistics!.DeckCards!.Contains(deckCard.InstanceId),
            "A permanent copy created with a full hand did not enter the deck draw pile.");

        CombatContext championState = Context(4, [source, champion], [champion.InstanceId, source.InstanceId]);
        RelicCardPlayMagicPowerResult championControl = RelicCardPlayMagicPowerModel.Apply(championState,
            champion.InstanceId, playRules, currentTurn: 1);
        Require(championControl.Supported && championControl.Dispatches.Count == 0 &&
            championControl.Context.NextCardId == championState.NextCardId &&
            !championControl.Context.Relics!.Single().CopyCardOnCardPlayModifiers!.Single().Conditions.Single().Triggered,
            "The Champion exclusion consumed the turn gate or copied the Champion.");
        RelicCardPlayMagicPowerResult afterChampion = RelicCardPlayMagicPowerModel.Apply(championControl.Context,
            source.InstanceId, playRules, currentTurn: 1);
        Require(afterChampion.Supported && afterChampion.Dispatches.Single().Applied,
            "A non-Champion card could not trigger after the excluded Champion control.");

        CombatContext unknownState = Context(4, [source, unknownChampion], [unknownChampion.InstanceId]);
        RelicCardPlayMagicPowerResult unknown = RelicCardPlayMagicPowerModel.Apply(unknownState,
            unknownChampion.InstanceId, playRules, currentTurn: 1);
        Require(!unknown.Supported && unknown.UnsupportedReason!.Contains("champion-card classification", StringComparison.Ordinal),
            "Missing Champion metadata was guessed instead of rejected.");

        CombatRelicState malformed = owner.WithCopyCardOnCardPlayModifiers([
            new RelicCopyCardOnCardPlayModifier(0, 1, "Invalid", true, false, [], null, [condition])]);
        Require(RelicModel.Validate([malformed]) != null &&
            !sourceState.Relics![0].CopyCardOnCardPlayModifiers![0].Conditions[0].Triggered,
            "A changed exclusion payload passed validation or mutated its parent branch.");
        Console.WriteLine("ORIGINAL-MOTHERS-REMNANT3-CHECKS PASS: permanent hand/deck copies, Champion exclusion, once-per-turn gate, metadata rejection and immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 216 &&
            root.GetProperty("OriginalMothersRemnant3Schema").GetInt32() == 216 &&
            root.GetProperty("OriginalMothersRemnant3Prepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-mothers-remnant3" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0) &&
            root.GetProperty("Actions").EnumerateArray().All(entry => entry.GetProperty("Difference").ValueKind == FixtureKind.Null),
            $"The MothersRemnant3 battle is incomplete or contains native/model differences: schema={root.GetProperty("Schema").GetInt32()} " +
                $"ownerSchema={root.GetProperty("OriginalMothersRemnant3Schema").GetInt32()} prepared={root.GetProperty("OriginalMothersRemnant3Prepared").GetBoolean()} " +
                $"scenario={root.GetProperty("ModifierScenario").GetString()} won={root.GetProperty("NativeWon").GetBoolean()} settled={root.GetProperty("TerminalEffectsSettled").GetBoolean()} " +
                "counters=" + string.Join(",", new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                    .Select(name => name + ":" + root.GetProperty(name).GetInt32())) +
                " actionDifferences=" + root.GetProperty("Actions").EnumerateArray()
                    .Count(entry => entry.GetProperty("Difference").ValueKind != FixtureKind.Null));

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 2, "The native witness must isolate its first eligible card and same-turn copy control.");
        BattleTurnState firstBefore = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState firstActual = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext before = firstBefore.Spawn.Train.Context!;
        CombatContext actual = firstActual.Spawn.Train.Context!;
        int playedId = actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        CardInstanceState played = before.FindCard(playedId)!;
        CombatRelicState owner = before.Relics!.Single(relic => relic.DataId == OwnerId);
        RelicCopyCardOnCardPlayModifier rule = owner.CopyCardOnCardPlayModifiers!.Single();
        bool firstWitnessMatches = played.DataId == SpellId && firstBefore.Spawn.Turn == 0 &&
            before.Cards.Hand.Count == before.MaxHandSize && played.CanBeCopied == true &&
            played.MaskDescriptor?.Definition.IsChampionCard == false;
        bool ownerMatches = owner.AssetKey == OwnerKey && owner.EffectTypes.SequenceEqual([Effect]) && rule.Count == 1 &&
            rule.CardType == "Invalid" && rule.PermanentCopy && rule.ExcludeChampions && rule.RequiredTraits.Count == 0 &&
            rule.Upgrade == null && rule.Conditions.Count == 1 && rule.Conditions[0].TrackTriggerCount &&
            rule.Conditions[0].Query.Type == "SubtypeInDeck" && rule.Conditions[0].Query.Duration == "ThisTurn" &&
            rule.Conditions[0].Comparator == 3 && rule.Conditions[0].Value == 1 && !rule.Conditions[0].AllowMultiple &&
            RelicModel.Validate([owner]) == null;
        Require(firstWitnessMatches && ownerMatches,
            $"The captured first direct Spell or exact MothersRemnant3 owner payload differs: " +
                $"card={played.DataId} expectedCard={SpellId} turn={firstBefore.Spawn.Turn} hand={before.Cards.Hand.Count}/{before.MaxHandSize} " +
                $"copyable={played.CanBeCopied} champion={played.MaskDescriptor?.Definition.IsChampionCard} " +
                $"ownerKey={owner.AssetKey} effectCount={owner.EffectTypes.Count} ruleCount={rule.Count} cardType={rule.CardType} " +
                $"permanent={rule.PermanentCopy} excludesChampion={rule.ExcludeChampions} traits={rule.RequiredTraits.Count} " +
                $"upgrade={rule.Upgrade != null} conditionCount={rule.Conditions.Count} " +
                $"condition={string.Join("/", rule.Conditions.Select(condition => $"{condition.TrackTriggerCount}:{condition.Query.Type}:{condition.Query.Duration}:{condition.Comparator}:{condition.Value}:{condition.AllowMultiple}"))} " +
                $"validation={RelicModel.Validate([owner])}");

        CardInstanceState copy = actual.CardInstances!.Single(card => card.InstanceId == before.NextCardId);
        RelicConditionState afterFirst = actual.Relics!.Single(relic => relic.DataId == OwnerId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        Require(copy.DataId == played.DataId && copy.Permanent.Upgrades.Count == 0 && copy.Temporary.Upgrades.Count == 0 &&
            actual.Cards.Hand.Count == actual.MaxHandSize && actual.Cards.Draw.Any(card => card.InstanceId == copy.InstanceId) &&
            actual.Statistics?.DeckCards?.Contains(copy.InstanceId) == true && afterFirst.Triggered &&
            afterFirst.DurationTriggerCount == 1,
            "The first direct card was not copied permanently to the draw pile after its draw refilled a full hand.");

        BattleTurnState secondBefore = actions[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState secondActual = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext secondContext = secondBefore.Spawn.Train.Context!;
        int controlId = actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        RelicConditionState controlCondition = secondContext.Relics!.Single(relic => relic.DataId == OwnerId)
            .CopyCardOnCardPlayModifiers!.Single().Conditions.Single();
        Require(controlId != playedId && secondBefore.Spawn.Turn == firstBefore.Spawn.Turn &&
            secondContext.FindCard(controlId)?.DataId == SpellId && controlCondition.Triggered &&
            secondActual.Spawn.Train.Context!.CardInstances!.Count(card => card.DataId == SpellId) ==
                secondContext.CardInstances!.Count(card => card.DataId == SpellId) &&
            secondActual.Spawn.Train.Context.Relics!.Single(relic => relic.DataId == OwnerId)
                .CopyCardOnCardPlayModifiers!.Single().Conditions.Single().DurationTriggerCount == 1,
            "A second same-turn eligible card was not blocked by the recorded once-per-turn gate.");
        Console.WriteLine("ORIGINAL-MOTHERS-REMNANT3-NATIVE PASS: full-hand permanent deck copy and same-turn once-only control.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
