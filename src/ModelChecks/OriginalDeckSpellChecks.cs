using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDeckSpellChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalDeckSpellPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original deck-spell witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalDeckSpellIds").Deserialize<int[]>()!;
        int[] controls = root.GetProperty("OriginalDeckSpellPileControlIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 6 && controls.Length == 2 && actions.Length == 6, "Missing Great Decay, hand/draw/discard or paid unit/X controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        const string upgradeId = "1984508b-2bb5-4a77-8327-3e46accaa08d";
        CombatContext updated = After(2).Spawn.Train.Context!;
        foreach (int id in controls.Concat(new[] { ids[4], ids[5] }))
        {
            CardInstanceState card = updated.FindCard(id)!;
            CardUpgradeModifier upgrade = card.Temporary.Upgrades.Single(item => item.DataId == upgradeId);
            Require(upgrade.Stats.Cost == -1 && upgrade.Stats.XCost == 1 && CardExhaustModel.HasConsume(card),
                "Great Decay did not add its original cost/X/Consume upgrade to a hand/draw/discard spell.");
        }
        foreach (int id in new[] { ids[0], ids[1], ids[3] })
            Require(!updated.FindCard(id)!.Temporary.Upgrades.Any(item => item.DataId == upgradeId), "Great Decay upgraded a non-spell unit.");
        Require(Before(2).Spawn.Train.Context!.Cards.Draw.Any(card => card.InstanceId == controls[0]) &&
            Before(2).Spawn.Train.Context!.Cards.Discard.Any(card => card.InstanceId == controls[1]), "Missing native draw/discard pile controls.");
        Require(Before(3).Energy - After(3).Energy == 2 && Before(4).Energy - After(4).Energy == 2 &&
            Before(5).Energy == 2 && After(5).Energy == 0 && Before(5).ForgePoints == 8 && After(5).ForgePoints == 20,
            "Paid unit exclusion, ordinary spell discount or X bonus changed.");
        Require(After(4).Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[4]),
            "Originally non-Consume Masterwork must actually exhaust after Great Decay.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-DECK-SPELL PASS: unchanged Great Decay, hand/draw/discard spell upgrades, unit exclusion, paid discount, granted Consume and positive X bonus; 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
