using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SpellHarvestTowerChecks
{
    private const string Reason = "Tower trigger targeting requires complete-train unit snapshots.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 35, "Wrong native cross-floor Harvest prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var local = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
        var original = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 6);
        var changed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 6);
        Require(before.Spawn.Turn == 6 && action.CardInstanceId == 24 && action.RoomIndex == 0 && action.TargetUnitId == 20 &&
            before.Spawn.Train.Context!.FindCard(24)!.DataId == "fb0683b1-20ac-4dd2-b580-14b502e182f4" &&
            local.Units.Single(unit => unit.Id == 20).AssetKey == "ClericT2_DiscipleofRuin" &&
            local.Units.Single(unit => unit.Id == 20).Health == 1 &&
            local.Units.Single(unit => unit.Id == 8).AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth" &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 20) &&
            original.Health == 1022 && original.MaxHealth == 1250 && changed.Health == 1072 && changed.MaxHealth == 1300,
            "Missing native room0 death/Entropic Harvest and room2 Savage +50HP/maxHP.");
        var incomplete = RoomCombatModel.ApplyCardDamage(local, 20, 1, 24);
        Require(!incomplete.Supported && incomplete.UnsupportedReason == Reason,
            "Room-only damage must still refuse incomplete Tower snapshots.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("SPELL-HARVEST-TOWER PASS:35 native inputs/full state/RNG and immutable parents; room0 spell kill/Harvest heals room2 Savage +50HP/maxHP; room-only guard retained; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
