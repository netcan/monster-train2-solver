using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StewardsQuartersChecks
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
        Require(root.GetProperty("StewardsQuartersPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Stewards Quarters witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("StewardsQuartersCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 7, "Missing subtype/floor/birth controls or replacement/restoration.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int source) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == source);
        foreach (int index in new[] { 3, 4, 6 })
        {
            BattleTurnState after = After(index);
            RoomCapacityState capacity = after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
            RoomCapacityAttachmentState attachment = capacity.Attachments!.Single();
            Require(attachment.DataId == "a3366bba-51f4-4a1d-803a-dc172ac4a76d" && attachment.CapacityDelta == 1 && capacity.PlayerMaximum == 6 &&
                attachment.CharacterFilters!.Single().Subtypes.Required.SequenceEqual(new[] { "SubtypesData_TrainSteward" }) &&
                attachment.CharacterUpgrades!.Single().Stats.Damage == 20 && attachment.CharacterUpgrades.Single().Statuses.Single().Id == "multistrike",
                "Original room capacity, +20 attack, Multistrike or Steward filter changed.");
            foreach (int source in index == 3 ? new[] { ids[0] } : new[] { ids[0], ids[4] })
                Require(Unit(after, source).BaseAttack == 80 && Unit(after, source).Status("multistrike")?.Stacks == 2,
                    "Existing/later Steward did not gain the original room upgrade.");
        }
        foreach (int index in new[] { 3, 4, 5, 6 })
        {
            Require(Unit(After(index), ids[1]).BaseAttack == 60 && Unit(After(index), ids[1]).Status("multistrike")?.Stacks == 1 &&
                Unit(After(index), ids[2]).BaseAttack == 60 && Unit(After(index), ids[2]).Status("multistrike") == null,
                "Outside-floor Steward or same-floor ordinary unit gained an excluded upgrade.");
        }
        foreach (int source in new[] { ids[0], ids[4] })
            Require(Unit(After(5), source).BaseAttack == 60 && Unit(After(5), source).Status("multistrike")?.Stacks == 1,
                "Replacement failed to remove just the room contribution while preserving starting Multistrike.");
        Require(After(5).Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).PlayerMaximum == 9 &&
            Before(3).Energy == After(3).Energy && Before(6).Energy == After(6).Energy,
            "Original zero cost or capacity replacement/removal changed.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("STEWARDS-QUARTERS PASS: unchanged original room, subtype/floor controls, existing/later births, additive Multistrike, replacement/restoration and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
