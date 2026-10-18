using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MemorialChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        int Target() => source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[2]).Id;
        var plays = BattleActionModel.EnumerateSupportedPlays(source);
        for (int index = 0; index < 5; index++)
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) &&
                    (index != 4 || play.TargetUnitId == Target()));
        if (context.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") < 2)
            return source.Spawn.Turn < 1 ? null : plays.FirstOrDefault(play => play.CardInstanceId == ids[2] && play.RoomIndex == 1);
        if (context.Statistics.Value(ids[5], "TimesPlayed", "ThisBattle") == 0)
            return plays.FirstOrDefault(play => play.CardInstanceId == ids[5] && play.RoomIndex == 0);
        return context.Statistics.Value(ids[6], "TimesPlayed", "ThisBattle") > 0 ? null :
            plays.FirstOrDefault(play => play.CardInstanceId == ids[6] && play.RoomIndex == 1 && play.TargetUnitId == Target());
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("MemorialPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Memorial witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("MemorialCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 8, "Missing Endless death, redraw/rebirth, replacement or ordinary second death.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int source) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == source);
        RoomCapacityAttachmentState attachment = After(3).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
        Require(attachment.DataId == "98f20790-57cc-4bce-943a-2ad5955974d8" && attachment.AffectsMonsters && !attachment.AffectsHeroes &&
            attachment.CharacterUpgrades!.Single().Statuses.Single().Id == "endless" && Before(3).Energy - After(3).Energy == 1 &&
            Unit(After(3), ids[0]).Status("endless")?.Stacks == 1 && Unit(After(3), ids[2]).Status("endless")?.Stacks == 1 &&
            Unit(After(3), ids[1]).Status("endless") == null, "Original room payload, cost or floor/team status controls changed.");
        Require(After(4).Spawn.Train.Context!.Cards.Draw.First().InstanceId == ids[2] &&
            Before(5).Spawn.Turn == 1 && Before(5).Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == ids[2]) &&
            actions[5].GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId == ids[2],
            "Endless did not put the original source identity on top of draw for normal redraw/replay.");
        CombatUnit reborn = Unit(After(5), ids[2]);
        Require(reborn.Id != Unit(Before(4), ids[2]).Id && reborn.Status("endless") == null &&
            After(5).Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Any(unit => unit.Id == reborn.Id),
            "Outside-floor rebirth retained the old room's Endless or reused the dead unit identity.");
        Require(Unit(After(6), ids[0]).Status("endless") == null && Unit(After(6), ids[0]).BaseAttack == 75,
            "Replacement failed to remove Endless and apply original Fight Club attack.");
        Require(!After(7).Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == ids[2]) &&
            After(7).Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]) &&
            !After(7).Spawn.Train.Context!.Cards.Draw.Any(card => card.InstanceId == ids[2]) &&
            After(4).ForgePoints == 5 && Before(7).ForgePoints == 0 && After(7).ForgePoints == 5,
            "Ordinary second sacrifice incorrectly kept Endless routing or lost the deployment reset/original Disassemble forge effects.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("MEMORIAL PASS: unchanged original room and Disassemble, Endless death/topdeck/redraw, identity-safe outside rebirth, replacement, ordinary second death and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
