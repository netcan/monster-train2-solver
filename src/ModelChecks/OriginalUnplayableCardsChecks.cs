using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnplayableCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids, int scrapId)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index == 2 && source.Spawn.Turn < 1) return null;
        if (index == 2) return new PlayCardAction(ids[index], 0, recursionSelections: [new CardRecursionSelection(0, [scrapId])]);
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == index);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalUnplayableCardsPrepared").GetBoolean() && root.GetProperty("OriginalUnplayableCardsQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Unplayable witness or query restoration.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalUnplayableCardsIds").Deserialize<int[]>()!, negative = root.GetProperty("OriginalUnplayableCardsNegativeIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalUnplayableCardsQueries").EnumerateArray().ToArray();
        Require(ids.Length == 3 && negative.Length == 3 && actions.Length == 3 && queries.Length == 12 &&
            queries.Select(query => query.GetProperty("DataId").GetString()).Distinct().Count() == 3, "Missing original card, floor, retention or smelt controls.");
        foreach (FixtureValue query in queries)
        {
            BattleTurnState before = query.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = query.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            CardSourceTrait[] traits = card.MaskDescriptor!.SourceTraits!.ToArray();
            Require(card.DataId == query.GetProperty("DataId").GetString() && card.Permanent.Upgrades.Count == 0 && card.Temporary.Upgrades.Count == 0 &&
                card.MaskDescriptor.TemporaryTraits.Count == 0 && JsonSerializer.Serialize(traits, ModelJson.Options) ==
                JsonSerializer.Serialize(query.GetProperty("SourceTraits").Deserialize<CardSourceTrait[]>(), ModelJson.Options), "Original rejection source metadata differs.");
            Require(traits.All(trait => trait.Int2 == 0 && trait.Int3 == 0 && !trait.Bool && trait.Str == "" && trait.Float == 0 && !trait.Scaling &&
                trait.TrackedValue == "SubtypeInDeck" && trait.EntryDuration == "ThisTurn") &&
                traits.Select(trait => trait.Type).SequenceEqual(card.DataId == "d4f0ff05-1fe1-483c-adda-928a8b05808c" ?
                    ["CardTraitPersistent", "CardTraitUnplayable", "CardTraitRefine"] : card.DataId == "f94572f9-d280-4990-a112-2c94f8fd7ede" ?
                    ["CardTraitUnplayable", "CardTraitUnpurgable"] : ["CardTraitUnplayable"]) &&
                traits.All(trait => trait.Int == (action.CardInstanceId == negative[0] ? 3 : 0)), "Original marker parameters differ.");
            Require(!query.GetProperty("CanPlay").GetBoolean() && !query.GetProperty("FromHand").GetBoolean() && !query.GetProperty("FromPlay").GetBoolean() &&
                new[] { "Error", "HandError", "PlayError" }.All(key => query.GetProperty(key).GetString() == "Unplayable") &&
                query.GetProperty("CanDiscard").GetBoolean() == (action.CardInstanceId != negative[0]), "Native rejection or discardability differs.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Rejection == ActionRejection.Illegal && result.Reason!.Contains("Unplayable"), "Unplayable must be specifically illegal, including otherwise guarded effects.");
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Rejected play mutated its parent.");
        }
        FixtureValue firstTurn = root.GetProperty("Turns")[0];
        BattleTurnState pre = firstTurn.GetProperty("Before").Deserialize<BattleTurnState>()!, post = firstTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(pre.Spawn.Turn == 0 && post.Spawn.Turn == 1 && post.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == negative[0]) &&
            post.Spawn.Train.Context.Statistics!.Value(negative[0], "TimesDiscarded", "ThisBattle") == 0, "Persistent Scrap Metal must stay in hand without discarding.");
        foreach (int id in negative.Skip(1))
            Require(post.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == id) &&
                post.Spawn.Train.Context.Statistics!.Value(id, "TimesDiscarded", "ThisBattle") == 1 &&
                post.Spawn.Train.Context.Statistics.Value(id, "TimesPlayed", "ThisBattle") == 0, "Ordinary unplayable cards must discard/redraw without being played.");
        BattleTurnState smeltBefore = actions[2].GetProperty("Before").Deserialize<BattleTurnState>()!, smeltAfter = actions[2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardInstanceState source = smeltBefore.Spawn.Train.Context!.FindCard(ids[2])!;
        CardPlayRule rule = smeltBefore.PlayRules!.Cards.Single(value => value.DataId == source.DataId);
        Require(source.DataId == "bc4cd099-93df-4771-ae72-4984fc50ab9f" && rule.Cost == 1 && rule.Effects.Single().Type == "ChooseDiscard" &&
            rule.Effects.Single().Value == 3 && smeltBefore.Spawn.Turn == 1 && smeltAfter.Energy - smeltBefore.Energy == 2 &&
            smeltAfter.ForgePoints - smeltBefore.ForgePoints == 3 && smeltAfter.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == negative[0]) &&
            !smeltAfter.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == negative[0]) &&
            smeltAfter.Spawn.Train.Context.Statistics!.Value(negative[0], "TimesPlayed", "ThisBattle") == 0 &&
            smeltAfter.Spawn.Train.Context.Statistics.Value(negative[0], "TimesExhausted", "ThisBattle") == 2,
            "Native Refine 3 must add three Forge/ember, consume Persistent Scrap Metal and preserve zero plays.");
        foreach (int id in negative.Skip(1))
        {
            BattleActionResult rejected = BattleActionModel.PlayCard(smeltAfter, new PlayCardAction(id, 0));
            Require(rejected.Rejection == ActionRejection.Illegal && rejected.Reason!.Contains("Unplayable"), "Positive Forge points must not mask native Unplayable rejection.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-UNPLAYABLE-CARDS PASS: twelve restored full-card rejection queries, original Unplayable/Unpurgable markers, Persistent retention, ordinary discard/redraw, Refine 3 smelt/ember/Forge and 16 immutable battle chains. Explicit discard and upgrade-added Refine guards remain.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
