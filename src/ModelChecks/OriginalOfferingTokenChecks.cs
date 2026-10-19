using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalOfferingTokenChecks
{
    private const string CardId = "e2d5dbd1-3930-4a41-9631-bef04cbc3fa4";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalOfferingTokenScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 255 &&
            root.GetProperty("ModifierScenario").GetString() == "original-offering-token" &&
            root.GetProperty("OriginalOfferingTokenPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Offering Token witness.");
        foreach (string key in new[] { "CaptureFailures", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalOfferingTokenCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length >= 2, "Missing Offering Token, anchor or controlled card identities.");
        if (root.GetProperty("Mismatches").GetInt32() > 0)
        {
            for (int index = 0; index < actions.Length; index++)
            {
                FixtureValue entry = actions[index];
                BattleTurnState debugBefore = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                PlayCardAction debugAction = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                BattleActionResult predicted = BattleActionModel.PlayCard(debugBefore, debugAction);
                string Cards(IEnumerable<CardToken> cards) => string.Join(",", cards.Select(card => card.InstanceId + ":" + card.DataId));
                System.Console.WriteLine("OFFERING-TOKEN-DIFF index=" + index + " id=" + debugAction.CardInstanceId +
                    " choices=" + string.Join(";", debugAction.RecursionSelections.Select(choice => choice.EffectIndex + ":" + string.Join(",", choice.CardInstanceIds))) +
                    " beforeHand=" + Cards(debugBefore.Spawn.Train.Context!.Cards.Hand) +
                    " actualHand=" + Cards(actual.Spawn.Train.Context!.Cards.Hand) +
                    " actualDraw=" + Cards(actual.Spawn.Train.Context.Cards.Draw) +
                    " actualDiscard=" + Cards(actual.Spawn.Train.Context.Cards.Discard) +
                    " predictedHand=" + (predicted.Supported ? Cards(predicted.State!.Spawn.Train.Context!.Cards.Hand) : predicted.Reason));
            }
        }
        Require(root.GetProperty("Mismatches").GetInt32() == 0, "Nonzero Mismatches");
        int anchorIndex = System.Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0]);
        int spellIndex = System.Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]);
        Require(anchorIndex >= 0 && spellIndex > anchorIndex &&
            actions.Count(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]) == 1,
            "The native policy must summon its anchor before playing exactly one Offering Token.");

        FixtureValue action = actions[spellIndex];
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        Require(rule.Cost == 0 && rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "Draw", "ChooseDiscard" }) &&
            rule.Effects[0].Value == 1 && rule.Effects[1].Target == "Hand" && rule.Effects[1].Value == 0 &&
            rule.Effects[1].ChooseDiscard?.Condition == 0 &&
            rule.Effects[1].ChooseDiscard.ExcludeEphemeralCopies && !rule.Effects[1].ChooseDiscard.GainEmberForCost &&
            played.RecursionSelections.Count == 1 && played.RecursionSelections[0].EffectIndex == 1 &&
            played.RecursionSelections[0].CardInstanceIds.SequenceEqual(new[] { ids[2] }),
            "Offering Token must draw one first, then select exactly one eligible card to discard.");
        Require(before.Spawn.Train.Context!.FindCard(ids[1])!.DataId == CardId &&
            before.Spawn.Train.Context.Cards.Draw.Any(card => card.InstanceId == ids[3] && card.DataId == StewardId) &&
            before.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == ids[2] && card.DataId == StewardId) &&
            after.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == ids[3] && card.DataId == StewardId) &&
            after.Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == ids[1]) &&
            after.Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == ids[2]) &&
            before.Energy == after.Energy,
            "Offering Token did not draw the controlled card, discard the selected Train Steward and preserve its zero-cost energy.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-OFFERING-TOKEN PASS: unchanged zero-cost play draws one card, then discards the selected Train Steward and replays the native battle.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalOfferingTokenScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
            context.Cards.Hand.Any(card => card.InstanceId == ids[2]) &&
            context.Cards.Draw.Any(card => card.InstanceId == ids[3]))
        {
            var cast = new PlayCardAction(ids[1], 0,
                recursionSelections: [new CardRecursionSelection(1, [ids[2]])]);
            if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
        }
        return null;
    }
}
