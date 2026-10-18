using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPiercingCardsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index == 5 && source.Spawn.Turn < 1) return null;
                int target = index is 2 or 3 or 4 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) &&
                    (target == 0 || play.TargetUnitId == target));
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalPiercingCardsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original piercing-card witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalPiercingCardsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 6 && actions.Length == 6, "Missing original cards or ordinary/outside controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int owner) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]);
        Require(Unit(Before(2), 0).Health == Unit(After(2), 0).Health && Unit(After(2), 0).Status("damage shield")?.Stacks == 2 &&
            Unit(Before(2), 0).Status("damage shield")?.Stacks == 2 && Unit(After(2), 0).Status("spell shield")?.Stacks == 1 &&
            Unit(Before(2), 0).Status("spell shield")?.Stacks == 2, "Ordinary spell damage did not consume Spell Shield before Damage Shield.");
        foreach (var sample in new[] { (3, "5a88d0e1-1c31-40f0-9e8d-8068dad832e9", 1, 16),
            (4, "f381b99b-518d-4818-b182-44d8f04110be", 2, 3), (5, "887b4b22-7f26-443f-b942-8ad620741617", 3, 120) })
        {
            BattleTurnState before = Before(sample.Item1), after = After(sample.Item1);
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == sample.Item2);
            Require(rule.Cost == sample.Item3 && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions.Count == 0 &&
                before.Energy - after.Energy == sample.Item3, "Original card cost or trait guard differs.");
            Require(before.Spawn.Train.Context!.FindCard(ids[sample.Item1])!.MaskDescriptor!.BaseTraits.Single().RuntimeType == "CardTraitIgnoreArmor",
                "Missing original piercing trait descriptor.");
            Require(Unit(before, 0).Health - Unit(after, 0).Health == sample.Item4, "Original piercing health damage differs.");
            foreach (string status in new[] { "damage shield", "spell shield", "titanskin" })
                Require(Unit(before, 0).Status(status)?.Stacks > 0 && Unit(after, 0).Status(status)?.Stacks == Unit(before, 0).Status(status)?.Stacks,
                    "Piercing consumed or failed to cover " + status);
            Require(Unit(after, 0).Status("armor")?.Stacks == Unit(before, 0).Status("armor")?.Stacks + (sample.Item1 == 4 ? 20 : 0),
                "Piercing consumed armor or Branding Rite did not add its original armor.");
            Require(Unit(before, 1).Health == Unit(after, 1).Health && Unit(after, 1).Status("armor")?.Stacks == 40,
                "Targeted/room damage leaked to the outside floor.");
        }
        Require(Before(3).PlayRules!.Cards.Single(card => card.DataId == "5a88d0e1-1c31-40f0-9e8d-8068dad832e9").Effects!.Count == 2 &&
            Before(5).Spawn.Turn == 1 && Before(5).Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Team == CombatTeam.Enemy),
            "Missing original two-hit Horn Break or enemy-present Inferno.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-PIERCING-CARDS PASS: unchanged Horn Break/Branding Rite/Inferno, ordinary shield control, armor and three shield/skin controls, outside floor and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
