using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRetchChecks
{
    private const string CardDataId = "9d801796-2b9f-496a-96fe-99b28bb32251";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = System.Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0) return null;
        if (index == 0)
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
        if (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalRetchPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Retch witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalRetchIds").Deserialize<int[]>()!;
        int eatenWitnessId = root.GetProperty("OriginalRetchEatenWitnessId").GetInt32();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 2 && eatenWitnessId > 0 && actions.Length == 2, "Missing protector, Retch play or controlled Eaten source.");
        BattleTurnState before = actions[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = actions[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[1])!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CardActionEffect effect = rule.Effects.Single();
        CardRecursionRule recursion = effect.Recursion!;
        CardPileState eatenBefore = before.OtherPiles.Single(pile => pile.Name == "Eaten");
        CardPileState eatenAfter = after.OtherPiles.Single(pile => pile.Name == "Eaten");
        CardToken witness = eatenBefore.Cards.Single(token => token.InstanceId == eatenWitnessId);

        Require(card.DataId == CardDataId && card.MaskDescriptor!.SourceTraits!.Count == 0 && card.MaskDescriptor.BaseTraits.Count == 0 &&
            rule.AssetKey == "Retch" && rule.CardType == "Spell" && rule.CostType == "Default" &&
            rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 &&
            effect.Type == "Recursion" && effect.Target == "Eaten" && effect.Value == 99 &&
            recursion.SelectionMode == "RandomToHand" && recursion.CardType == "Monster" && recursion.Subtype.Length == 0 &&
            action.CardInstanceId == ids[1] && action.RoomIndex == 0 && before.Spawn.Turn >= 1 &&
            before.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == ids[1]) &&
            before.Spawn.Train.Context.FindCard(eatenWitnessId)?.DataId == witness.DataId &&
            after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.All(token => token.InstanceId != eatenWitnessId) &&
            after.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == eatenWitnessId) &&
            after.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == ids[1]) &&
            before.Energy - after.Energy == rule.Cost &&
            before.Spawn.Train.Context.Statistics!.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
            after.Spawn.Train.Context.Statistics!.Value(ids[1], "TimesPlayed", "ThisBattle") == 1 &&
            after.Spawn.Train.Context.FindCard(ids[1])!.LastPlayedCost == rule.Cost,
            "Original Retch identity, random Eaten-to-hand recursion or play lifecycle differs.");

        Require(eatenAfter.Cards.All(token => token.InstanceId != eatenWitnessId), "Retch left the selected Monster in Eaten.");
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-RETCH PASS: unchanged random Eaten-Monster recursion returns the captured original card to hand; Eaten was a controlled starting-pile witness.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
