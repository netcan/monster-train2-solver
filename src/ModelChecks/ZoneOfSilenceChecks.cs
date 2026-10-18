using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ZoneOfSilenceChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 3 && source.Spawn.Turn < index - 2) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ZoneOfSilencePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Zone of Silence witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("ZoneOfSilenceCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 5 && actions.Length == 5, "Missing enemy birth or replacement/reactivation.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Mage(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.AssetKey == "MageT1_Junker");
        CombatUnit Unit(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
        bool Fired(CombatUnit unit) => unit.Triggers.Single(trigger => trigger.Kind == "PostCombat").HasTriggered;
        CombatUnit first = Mage(Before(3)), second = Mage(Before(4));
        Require(Before(3).Spawn.Turn == 1 && first.Status("silenced")?.Stacks == 1 && !Fired(first) &&
            Unit(After(3), first.Id).Status("silenced") == null, "Enemy birth did not inherit Silence or replacement failed to remove it.");
        Require(Before(4).Spawn.Turn == 2 && first.Id != second.Id && Fired(Unit(Before(4), first.Id)) &&
            second.Status("silenced") == null && Mage(After(4)).Status("silenced")?.Stacks == 1 && !Fired(Mage(After(4))),
            "Original Mage trigger did not resume after replacement, or restoration did not silence the next Mage.");
        BattleTurnState thirdTurn = root.GetProperty("Turns").EnumerateArray().Select(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!)
            .First(state => state.Spawn.Turn == 3);
        Require(!Fired(Unit(thirdTurn, second.Id)) && Unit(thirdTurn, second.Id).Status("silenced") == null,
            "Restored room failed to suppress PostCombat, or floor exit retained its Silence.");
        foreach (int index in new[] { 2, 4 })
        {
            RoomCapacityAttachmentState attachment = After(index).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
            Require(attachment.DataId == "56468ba5-86b6-49be-ba30-02baf7ffd6dd" && attachment.AffectsHeroes && !attachment.AffectsMonsters &&
                attachment.CharacterUpgrades!.Single().Statuses.Single().Id == "silenced" && Before(index).Energy - After(index).Energy == 3,
                "Original room team, Silence payload or three-energy cost changed.");
            Require(After(index).Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.Status("silenced") == null),
                "Enemy-only Silence leaked to friendly units.");
        }
        Require(Unit(After(4), first.Id).Status("silenced") == null, "Restoration affected an outside-floor Mage.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ZONE-OF-SILENCE PASS: unchanged original room, enemy birth, team/floor controls, restored/suppressed Mage trigger, floor exit and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
