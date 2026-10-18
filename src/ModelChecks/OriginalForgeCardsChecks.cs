using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalForgeCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        for (int index = 0; index < ids.Length; index++)
        {
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0) continue;
            int target = index == 5 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[2]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                play.RoomIndex == (index == 1 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
        }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalForgeCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original forge-card witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalForgeCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 8 && actions.Length == 8, "Missing original forge spell or unit controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Before(3).ForgePoints == 0 && After(3).ForgePoints == 8 && After(4).ForgePoints == 10 &&
            After(5).ForgePoints == 15 && Before(6).ForgePoints == 15 && After(6).ForgePoints == 0,
            "Masterwork/Starter Forge/Disassemble accumulation, turn retention or Anvil reset differs.");
        Require(After(4).Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Player).Status("armor")?.Stacks == 2 &&
            After(4).Spawn.Train.Rooms[1].Units.Single().Status("armor") == null,
            "Starter Forge armor did not stay on the native front friendly unit.");
        Require(!After(5).Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == ids[2]) &&
            After(5).Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == ids[0]),
            "Disassemble did not sacrifice exactly its chosen unit.");
        CombatUnit enemy = Before(6).Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
        Require(!After(6).Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == enemy.Id && unit.Health > 0) && Before(6).Spawn.Turn == 1,
            "Anvil Breaker needs a live enemy damage witness after a normal draw.");
        Require(Before(6).Energy == 3 && After(6).Energy == 1 && Before(7).Energy == 1 && After(7).Energy == 0 &&
            After(7).ForgePoints == 4 && After(7).Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[7]),
            "Original Railforged Spike X-cost scaling or Consume destination differs.");
        Require(Before(6).Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == ids[6]) &&
            Before(0).Spawn.Train.Context!.Cards.Draw.Any(card => card.InstanceId == ids[6]) &&
            Before(0).Spawn.Train.Context!.Cards.Draw.Any(card => card.InstanceId == ids[7]),
            "Anvil and Spike must enter through ordinary next-turn draw.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-FORGE-CARDS PASS: five original spells, 0/8/10/15/0/4 forge chain, front armor, selected sacrifice, enemy damage, normal draw and X-cost/Consume; 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
