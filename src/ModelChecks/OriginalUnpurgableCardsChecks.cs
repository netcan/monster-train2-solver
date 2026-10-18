using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnpurgableCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 5 && source.Spawn.Turn < 1) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalUnpurgableCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Unpurgable battle witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalUnpurgableCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 7, "Missing original play/purge/unplayed controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int Pyre(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
        foreach (int index in Enumerable.Range(2, 5))
        {
            BattleTurnState before = Before(index), after = After(index);
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardSourceTrait raw = card.MaskDescriptor!.SourceTraits!.Single();
            Require(raw.Type == "CardTraitUnpurgable" && raw.Int == 0 && raw.Int2 == 0 && raw.Int3 == 0 && !raw.Bool && raw.Str == "" &&
                raw.Float == 0 && !raw.Scaling && raw.TrackedValue == "SubtypeInDeck" && raw.EntryDuration == "ThisTurn",
                "Original marker source parameters differ.");
            string dataId = index == 4 ? "17fef621-2bb7-4847-b41d-52a89795c784" : index is 3 or 6 ? "913694e9-bace-4480-8691-24f699ae583e" : "186180e9-94e9-47d0-b69c-865b9a6d27a4";
            CardPlayRule rule = before.PlayRules!.Cards.Single(value => value.DataId == dataId);
            Require(rule.CardType == "Blight" && rule.Cost == (index == 4 ? 2 : 1) && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 &&
                rule.Effects.Single().Type == "Null" && before.Energy - after.Energy == rule.Cost && Pyre(before) == Pyre(after),
                "Original marker blocked normal play, changed costs or invoked an unplayed penalty on play.");
            if (index != 2) Require(after.Spawn.Train.Context!.Cards.Discard.Any(card => card.InstanceId == ids[index]), "Played normal Blight must discard.");
        }
        Require(After(2).Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Any(card => card.InstanceId == ids[2]) &&
            !After(2).Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == ids[2]),
            "Unpurgable incorrectly blocked native battle SelfPurge or routed it to discard.");
        FixtureValue first = root.GetProperty("Turns")[0];
        BattleTurnState pre = first.GetProperty("Before").Deserialize<BattleTurnState>()!, post = first.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(pre.Spawn.Turn == 0 && Pyre(pre) == 80 && Pyre(post) == 76,
            "Unplayed Blight/Candle must deal one/three Pyre damage, excluding played controls.");
        foreach (int index in new[] { 5, 6 })
            Require(pre.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0 &&
                post.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0 &&
                post.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == ids[index]), "Unplayed cards must discard/redraw without play statistics.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-UNPURGABLE-CARDS PASS: original Blight/Candle/Titan Curse, fixed costs and discard, positive battle SelfPurge, played/unplayed Pyre controls, redraw, exact source parameters and 16 immutable chains. Campaign purge selection is outside this witness.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
