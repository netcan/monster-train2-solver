using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRoomCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        for (int index = 0; index < ids.Length; index++)
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                    play.CardInstanceId == ids[index] && play.RoomIndex == (index is 1 or 7 ? 1 : 0));
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalRoomCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original room-card witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalRoomCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10, "Missing existing/later births, room replacement or spell/unit/floor controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int index in new[] { 2, 3, 8, 9 })
        {
            TrainCombatState train = After(index).Spawn.Train;
            Require(train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.BaseAttack == 75) &&
                train.Rooms[1].Units.Single().BaseAttack == 60, "Fight Club attack did not follow existing/born units and floor isolation.");
            RoomCapacityAttachmentState attachment = train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
            Require(attachment.DataId == "6bc6afc8-a113-4f03-b090-d4436f41d822" && attachment.SpellCostDelta == 0 &&
                attachment.CharacterUpgrades!.Single().Stats.Damage == 15 && attachment.AffectsMonsters && !attachment.AffectsHeroes,
                "Original Fight Club definition, team or replacement payload changed.");
        }
        foreach (int index in new[] { 4, 5, 6, 7 })
        {
            TrainCombatState train = After(index).Spawn.Train;
            Require(train.Rooms[0].Units.All(unit => unit.BaseAttack == 60), "Replacing Fight Club retained attack or changed later births.");
            RoomCapacityAttachmentState attachment = train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
            Require(attachment.DataId == "5af36c0f-ab98-4a49-903a-b08d93705af1" && attachment.SpellCostDelta == -1 &&
                attachment.CharacterUpgrades!.Single().Stats.Damage == 0, "Original Spell Sanctum discount or replacement changed.");
        }
        foreach (var sample in new[] { (2, 1), (4, 3), (5, 2), (6, 1), (7, 2), (8, 1), (9, 2) })
            Require(Before(sample.Item1).Energy - After(sample.Item1).Energy == sample.Item2,
                "Native room/spell/unit/floor energy cost differs at play " + sample.Item1);
        foreach (int index in new[] { 6, 7, 9 })
            Require(After(index).Spawn.Train.Context!.Gold - Before(index).Spawn.Train.Context!.Gold == 5 &&
                Before(index).Spawn.Train.Context!.Resources != null, "The paid spell must execute its independent gold effect with captured resource rules.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-ROOM-CARDS PASS: two unchanged Fight Clubs and Spell Sanctum, existing/later births, two replacements, spell/unit/floor costs, executed gold controls and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
