using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;

internal static class RelicCardDrawOnCardPlayChecks
{
    private const string RelicId = "f3f07b9b-1349-41d0-abab-bd5ec04d81e4";
    private const string RelicKey = "CardDrawOnCardPlay";

    internal static void Run()
    {
        var condition = new RelicConditionState(new CardStatisticQuery("AnyCardPlayed"), false, 6, 3, false);
        var relic = new CombatRelicState("synthetic", RelicKey, [RelicModel.CardDrawOnCardPlay],
            cardDrawOnCardPlayModifiers: [new RelicCardDrawOnCardPlayModifier(0, 1, "Invalid", [condition])]);
        Require(RelicModel.Validate([relic]) == null, "Valid card-draw-on-play relic was rejected.");

        CardInstanceState Card(int id, string type, string[]? subtypes = null) => new(id, "card-" + id, CardModifiers.Empty(), CardModifiers.Empty(),
            0, 0, 0, [], maskDescriptor: new CardMaskDescriptor(new CardMaskDefinition("card-" + id,
                type, "Common", false, false, false, subtypes ?? [], [], [], [], null, false, 0, 0, false, false, false),
                1, [], []));
        CombatRelicState[] relics = [relic];
        CardInstanceState[] instances = [Card(1, "Monster", ["SubtypesData_Snack"]), Card(2, "Spell"), Card(3, "Spell"), Card(4, "Monster")];
        BattleStatistics stats = new([new CardStatisticValue(1, "ThisTurn", "TimesPlayed", 2)], [], [], [], [], [], [],
            0, 0, 0, 0, 0, [1, 2, 3], [1, 2, 3], [1, 2, 3]);
        CombatContext Context(BattleStatistics statistics, CombatRelicState[] currentRelics) => new(
            new CardCycleState([], [new CardToken(2, "card-2"), new CardToken(3, "card-3")], [],
                UnityRng.Seed(903), 0, []), UnityRng.Seed(117), 0, 4, 2,
            statistics: statistics, cardInstances: instances, cardRegistry: instances,
            otherPiles: [new CardPileState("DiscardBuffer", [new CardToken(1, "card-1")])], relics: currentRelics);

        CombatContext belowThreshold = Context(stats, relics);
        RelicCardPlayMagicPowerResult below = RelicCardPlayMagicPowerModel.Apply(belowThreshold, 1);
        Require(below.Supported && below.Context.Cards.Hand.Count == 0 &&
            !below.Context.Relics![0].CardDrawOnCardPlayModifiers![0].Conditions[0].Triggered,
            "The relic drew before its card-play threshold.");

        BattleStatistics atThreshold = stats.Increment(1, "TimesPlayed");
        RelicCardPlayMagicPowerResult initialTurn = RelicCardPlayMagicPowerModel.Apply(
            Context(atThreshold, relics), 1, currentTurn: 0);
        Require(initialTurn.Supported && initialTurn.Context.Cards.Hand.Count == 0 &&
            !initialTurn.Context.Relics![0].CardDrawOnCardPlayModifiers![0].Conditions[0].Triggered,
            "A ThisTurn card-draw condition fired during the native deployment turn.");

        RelicCardPlayMagicPowerResult triggered = RelicCardPlayMagicPowerModel.Apply(
            Context(atThreshold, relics), 1, currentTurn: 1);
        Require(triggered.Supported && triggered.Context.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([3]) &&
            triggered.Context.Cards.Draw.Select(card => card.InstanceId).SequenceEqual([2]) &&
            triggered.Context.Statistics!.Value(3, "TimesDrawn") == 1 &&
            triggered.Context.Relics![0].CardDrawOnCardPlayModifiers![0].Conditions[0].ValueAtLastTrigger == 3,
            "The relic did not draw one card and record its triggering threshold.");

        BattleStatistics afterThreshold = atThreshold.Increment(1, "TimesPlayed");
        RelicCardPlayMagicPowerResult once = RelicCardPlayMagicPowerModel.Apply(
            triggered.Context.WithStatistics(afterThreshold), 1);
        Require(once.Supported && once.Context.Cards.Hand.Count == 1 &&
            once.Context.Relics![0].CardDrawOnCardPlayModifiers![0].Conditions[0].Triggered,
            "A once-per-turn card-draw relic fired again before its duration reset.");
        CombatContext reset = RelicConditionModel.EndDuration(once.Context, "ThisTurn");
        Require(!reset.Relics![0].CardDrawOnCardPlayModifiers![0].Conditions[0].Triggered,
            "The card-draw relic condition did not reset at the end of its duration.");

        var monsterOnly = new CombatRelicState("monster-only", "DrawOnSnackPlayed",
            [RelicModel.CardDrawOnCardPlay], cardDrawOnCardPlayModifiers: [
                new RelicCardDrawOnCardPlayModifier(0, 1, "Monster")]);
        RelicCardPlayMagicPowerResult monsterPlay = RelicCardPlayMagicPowerModel.Apply(Context(stats, [monsterOnly]), 1);
        RelicCardPlayMagicPowerResult spellPlay = RelicCardPlayMagicPowerModel.Apply(Context(stats, [monsterOnly]), 2);
        Require(monsterPlay.Supported && monsterPlay.Context.Cards.Hand.Count == 1 &&
            spellPlay.Supported && spellPlay.Context.Cards.Hand.Count == 0,
            "The relic's native played-card type filter was not respected.");

        var trackedThisTurn = new RelicConditionState(new CardStatisticQuery("TimesPlayed", "ThisTurn"), true, 6, 1, false);
        var snackOnly = new CombatRelicState("snack-only", "DrawOnSnackPlayed",
            [RelicModel.CardDrawOnCardPlay], cardDrawOnCardPlayModifiers: [
                new RelicCardDrawOnCardPlayModifier(0, 1, "Monster", [trackedThisTurn], requiredSubtype: "SubtypesData_Snack")]);
        RelicCardPlayMagicPowerResult snackPlay = RelicCardPlayMagicPowerModel.Apply(Context(stats, [snackOnly]), 1, currentTurn: 0);
        RelicCardPlayMagicPowerResult otherMonsterPlay = RelicCardPlayMagicPowerModel.Apply(Context(stats, [snackOnly]), 4);
        RelicCardPlayMagicPowerResult otherTypePlay = RelicCardPlayMagicPowerModel.Apply(Context(stats, [snackOnly]), 2);
        Require(snackPlay.Supported && snackPlay.Context.Cards.Hand.Count == 1 &&
            snackPlay.Context.Relics![0].CardDrawOnCardPlayModifiers![0].Conditions[0].Triggered &&
            otherMonsterPlay.Supported && otherMonsterPlay.Context.Cards.Hand.Count == 0 &&
            otherTypePlay.Supported && otherTypePlay.Context.Cards.Hand.Count == 0,
            "The relic's native played-card subtype or trigger-count condition was not respected.");

        var missingDefinition = new CombatRelicState("bad", "MissingCardDrawDefinition", [RelicModel.CardDrawOnCardPlay]);
        var traitFiltered = new CombatRelicState("trait", "DrawCardWhenConsumePlayed", [RelicModel.CardDrawOnCardPlay],
            cardDrawOnCardPlayModifiers: [new(0, 1, "Spell", [trackedThisTurn], requiredTraits: ["CardTraitExhaustState"])]);
        var noTrait = RelicCardPlayMagicPowerModel.Apply(Context(stats, [traitFiltered]), 2, currentTurn: 0);
        var consuming = new CardInstanceState(2, "card-2", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: new CardMaskDescriptor(instances[1].MaskDescriptor!.Definition, 1,
                [new CardTraitValue("CardTraitExhaustState", "CardTraitExhaustState", 0, 0, true, 0)], []));
        var withTrait = RelicCardPlayMagicPowerModel.Apply(Context(stats, [traitFiltered]).WithCard(consuming), 2, currentTurn: 0);
        Require(noTrait.Supported && noTrait.Context.Cards.Hand.Count == 0 && withTrait.Supported &&
            withTrait.Context.Cards.Hand.Count == 1 &&
            withTrait.Context.Relics![0].CardDrawOnCardPlayModifiers![0].RequiredTraits.SequenceEqual(["CardTraitExhaustState"]),
            "Required Consume trait was ignored or lost while recording the draw condition.");
        Require(RelicModel.Validate([missingDefinition])?.Contains("card-draw-on-card-play", StringComparison.Ordinal) == true,
            "An uncaptured native card-draw-on-play payload passed validation.");
        Console.WriteLine("RELIC-CARD-DRAW-ON-PLAY-CHECKS PASS: threshold, once-per-duration, reset, type/subtype filtering, draw statistics and explicit missing definitions.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 160 &&
            root.GetProperty("ModifierScenario").GetString() == "original-card-draw-on-card-play" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("CaptureFailures").GetInt32() == 0 &&
            root.GetProperty("Mismatches").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0,
            "Original card-draw-on-play battle is incomplete or has native/model failures.");

        int matched = 0;
        int triggered = 0;
        int draws = 0;
        int snackTriggered = 0;
        foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext beforeContext = before.Spawn.Train.Context!;
            CombatContext actualContext = actual.Spawn.Train.Context!;
            CombatRelicState relic = beforeContext.Relics!.Single(item => item.DataId == RelicId);
            Require(relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual([RelicModel.CardDrawOnCardPlay]) &&
                relic.CardDrawOnCardPlayModifiers?.Count == 1 && relic.CardDrawOnCardPlayModifiers[0].Count == 1 &&
                relic.CardDrawOnCardPlayModifiers[0].CardType == "Invalid" &&
                relic.CardDrawOnCardPlayModifiers[0].RequiredSubtype == null &&
                relic.CardDrawOnCardPlayModifiers[0].Conditions.Count == 1 &&
                relic.CardDrawOnCardPlayModifiers[0].Conditions[0].Query.Type == "AnyCardPlayed" &&
                relic.CardDrawOnCardPlayModifiers[0].Conditions[0].Comparator == 6 &&
                relic.CardDrawOnCardPlayModifiers[0].Conditions[0].Value == 3,
                "The battle did not retain the unchanged original relic effect payload.");

            const string snackRelicId = "8b00c46d-374e-4afb-aa27-526873e242d1";
            CombatRelicState snackRelic = beforeContext.Relics.Single(item => item.DataId == snackRelicId);
            Require(snackRelic.AssetKey == "DrawOnSnackPlayed" && snackRelic.EffectTypes.SequenceEqual([RelicModel.CardDrawOnCardPlay]) &&
                snackRelic.CardDrawOnCardPlayModifiers?.Count == 1 &&
                snackRelic.CardDrawOnCardPlayModifiers[0].CardType == "Monster" &&
                snackRelic.CardDrawOnCardPlayModifiers[0].RequiredSubtype == "SubtypesData_Snack" &&
                snackRelic.CardDrawOnCardPlayModifiers[0].Conditions.Count == 1 &&
                snackRelic.CardDrawOnCardPlayModifiers[0].Conditions[0].Query.Type == "TimesPlayed" &&
                snackRelic.CardDrawOnCardPlayModifiers[0].Conditions[0].Comparator == 6 &&
                snackRelic.CardDrawOnCardPlayModifiers[0].Conditions[0].Value == 1,
                "The battle did not retain the unchanged original DrawOnSnackPlayed payload.");

            RelicConditionState beforeCondition = relic.CardDrawOnCardPlayModifiers[0].Conditions[0];
            RelicConditionState afterCondition = actualContext.Relics!.Single(item => item.DataId == RelicId)
                .CardDrawOnCardPlayModifiers![0].Conditions[0];
            int beforeDrawn = beforeContext.Statistics!.Values.Where(value => value.Duration == "ThisTurn" && value.Type == "TimesDrawn")
                .Sum(value => value.Value);
            int afterDrawn = actualContext.Statistics!.Values.Where(value => value.Duration == "ThisTurn" && value.Type == "TimesDrawn")
                .Sum(value => value.Value);
            if (afterCondition.ValueAtLastTrigger != beforeCondition.ValueAtLastTrigger)
            {
                triggered++;
                draws += afterDrawn - beforeDrawn;
                Require(afterDrawn > beforeDrawn, "The native relic recorded its threshold without drawing a card.");
            }
            RelicConditionState snackBeforeCondition = snackRelic.CardDrawOnCardPlayModifiers[0].Conditions[0];
            RelicConditionState snackAfterCondition = actualContext.Relics!.Single(item => item.DataId == snackRelicId)
                .CardDrawOnCardPlayModifiers![0].Conditions[0];
            if (!snackBeforeCondition.Triggered && snackAfterCondition.Triggered)
            {
                snackTriggered++;
                Require(afterDrawn > beforeDrawn, "The original Snack relic recorded its trigger without drawing a card.");
            }
            matched++;
        }
        Require(matched > 0 && triggered > 0 && draws >= triggered,
            "The full battle did not observe the original relic draw after its threshold.");
        Require(snackTriggered > 0, "The full battle did not trigger the unchanged DrawOnSnackPlayed relic.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"NATIVE-CARD-DRAW-ON-PLAY-CHECKS PASS: {matched} actions, {triggered} threshold draws, {snackTriggered} Snack activations; unchanged owners {RelicKey} and DrawOnSnackPlayed.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
