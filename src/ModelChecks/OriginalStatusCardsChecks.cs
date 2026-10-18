using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStatusCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        for (int index = 0; index < ids.Length; index++)
        {
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0) continue;
            int target = index >= 4 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                play.RoomIndex == (index == 2 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
        }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalStatusCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original status-card witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalStatusCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 6 && actions.Length == 6, "Missing original status spells or target/floor controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(int action, int owner) => After(action).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]);
        void Stacks(int action, int owner, int rage, int regen)
        {
            CombatUnit unit = Unit(action, owner);
            Require(unit.Status("buff")?.Stacks == rage && unit.Status("regen")?.Stacks == regen && unit.Status("armor")?.Stacks == 4,
                "Native propagation, X scaling or nonpropagatable armor control differs.");
        }
        Stacks(3, 0, 5, 4); Stacks(3, 1, 5, 4); Stacks(3, 2, 3, 2);
        Stacks(4, 0, 7, 6); Stacks(4, 1, 5, 4); Stacks(4, 2, 3, 2);
        Require(Before(4).Spawn.Train.Rooms[0].Units.First().SpawnerCardId == ids[1] &&
            After(4).Spawn.Train.Rooms[0].Units.First().SpawnerCardId == ids[0],
            "Glow Up must move its target to the front before adjusting the retained last-target collection.");
        Stacks(5, 0, 11, 10); Stacks(5, 1, 5, 4); Stacks(5, 2, 3, 2);
        Require(Before(5).Energy == 2 && After(5).Energy == 0 && new[] { ids[3], ids[5] }.All(id =>
            After(5).Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id)),
            "Spike of the Legion must consume two energy and both original Consume spells must be exhausted.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-STATUS-CARDS PASS: original Proliferate, Glow Up and Spike of the Legion; room propagation, front/last-target order, positive X scaling, armor/floor/neighbor controls and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
