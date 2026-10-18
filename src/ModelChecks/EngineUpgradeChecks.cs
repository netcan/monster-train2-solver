using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EngineUpgradeChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        for (int index = 0; index < ids.Length; index++)
            if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
            {
                if (index >= 4 && source.Spawn.Turn < index - 3) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == index % 2);
            }
        return null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("EngineUpgradePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Engine Upgrade witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("EngineUpgradeCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 6 && actions.Length == 6, "Missing two original attachments or successive replacements.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityState Capacity(BattleTurnState state, int room) => state.Spawn.Train.Context!.RoomCapacities!.Single(item => item.RoomIndex == room);
        foreach (int index in new[] { 2, 3 })
        {
            RoomCapacityState before = Capacity(Before(index), index % 2), after = Capacity(After(index), index % 2);
            RoomCapacityAttachmentState attachment = after.Attachments!.Single();
            Require(attachment.DataId == "0ec5622b-a425-489e-8857-97ba023f92d3" && attachment.CapacityDelta == -1 && attachment.EnergyPerTurnDelta == 1 &&
                after.PlayerMaximum == before.PlayerMaximum - 1 && after.EnemyMaximum == before.EnemyMaximum && Before(index).Energy == After(index).Energy,
                "Original zero-cost Engine Upgrade must reduce only friendly capacity and delay its energy reward.");
        }
        Require(Before(4).Spawn.Turn == 1 && Before(4).Energy == Before(4).EnergyPerTurn + 2 &&
            Before(5).Spawn.Turn == 2 && Before(5).Energy == Before(5).EnergyPerTurn + 1,
            "Both floors must sum their energy, then one replaced room must stop contributing on the next turn.");
        foreach (int index in new[] { 4, 5 })
        {
            int room = index % 2;
            RoomCapacityState before = Capacity(Before(index), room), after = Capacity(After(index), room);
            Require(after.PlayerMaximum == before.PlayerMaximum + 1 && after.Attachments!.Single().EnergyPerTurnDelta == 0 &&
                Before(index).Energy - After(index).Energy == 1 &&
                After(index).Spawn.Train.Rooms.Single(item => item.RoomIndex == room).Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.BaseAttack == 75),
                "Replacement must restore capacity, remove future energy and apply original Fight Club attack.");
        }
        BattleTurnState thirdTurn = root.GetProperty("Turns").EnumerateArray().Select(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!)
            .First(state => state.Spawn.Turn == 3);
        Require(thirdTurn.Energy == thirdTurn.EnergyPerTurn, "Energy persisted after both original room attachments were replaced.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ENGINE-UPGRADE PASS: unchanged original attachments, capacity/floor controls, delayed energy 5/4/3, successive replacements and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
