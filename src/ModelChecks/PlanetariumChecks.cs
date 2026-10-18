using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlanetariumChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                int room = index is 1 or 4 ? 1 : 0;
                int target = index is 3 or 4 or 7 or 9 ? source.Spawn.Train.Rooms.SelectMany(item => item.Units).Single(unit => unit.SpawnerCardId == ids[room]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room &&
                    (target == 0 || play.TargetUnitId == target));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("PlanetariumPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Planetarium witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("PlanetariumCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10, "Missing Conduit contributors, damage/floor controls or replacement/restoration.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int source) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == source);
        int Power(BattleTurnState state, int room) => state.Spawn.Train.Context!.MagicPower!.Single(item => item.RoomIndex == room && item.Team == CombatTeam.Player).Value;
        foreach (int index in new[] { 2, 8 })
        {
            RoomCapacityAttachmentState attachment = After(index).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
            Require(attachment.DataId == "b895b6ef-6d0c-4474-a12c-1229fb7749c3" && attachment.AffectsMonsters && !attachment.AffectsHeroes &&
                attachment.CharacterUpgrades!.Single().Statuses.Single().Id == "conduit" && attachment.CharacterUpgrades.Single().Statuses.Single().Stacks == 5 &&
                Before(index).Energy - After(index).Energy == 1, "Original room team, Conduit five or cost changed.");
        }
        Require(Power(After(2), 0) == 5 && Power(After(5), 0) == 10 && Power(After(6), 0) == 0 && Power(After(8), 0) == 10,
            "Existing/later contributors, removal or restored magic power differ.");
        foreach (var sample in new[] { (3, 0, 10), (4, 1, 5), (7, 0, 5), (9, 0, 15) })
            Require(Unit(Before(sample.Item1), ids[sample.Item2]).Health - Unit(After(sample.Item1), ids[sample.Item2]).Health == sample.Item3,
                "Actual spell damage did not follow native Conduit floor and replacement tracking at play " + sample.Item1);
        Require(Unit(After(5), ids[5]).Status("conduit")?.Stacks == 5 && Unit(After(6), ids[0]).Status("conduit") == null &&
            Unit(After(6), ids[5]).Status("conduit") == null && Unit(After(8), ids[0]).Status("conduit")?.Stacks == 5 &&
            Unit(After(8), ids[5]).Status("conduit")?.Stacks == 5 && Unit(After(8), ids[1]).Status("conduit") == null && Power(After(8), 1) == 0,
            "Room Conduit status leaked across floors, persisted after removal or failed on later birth/restoration.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("PLANETARIUM PASS: unchanged original room, contributors 5/10/0/10, damage 10/5/5/15, floor/birth controls, replacement/restoration and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
