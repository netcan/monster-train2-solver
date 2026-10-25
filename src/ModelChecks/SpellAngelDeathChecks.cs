using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SpellAngelDeathChecks
{
    private const string Reason = "Tower trigger targeting requires complete-train unit snapshots.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 54, "Wrong original Angel death prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        RoomCombatState floor = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
        CombatUnit dying = floor.Units.Single(unit => unit.Id == 6);
        CombatUnit companion = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 9);
        Require(action.CardInstanceId == 28 && action.RoomIndex == 2 && action.TargetUnitId == 0 &&
            before.Spawn.Train.Context!.FindCard(28)!.DataId == "27e9ee82-fc42-4aa5-b845-90d7e31b61d9" &&
            dying.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" && dying.Health == 3 &&
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 9).Status("multistrike") == null &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 6) &&
            companion.AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth" && companion.Health == 1122 &&
            companion.Status("multistrike")?.Stacks == 1 && companion.Status("titanskin")?.Stacks == 10,
            "Missing the natural room-damage kill,companion Multistrike handoff and dead aura removal.");
        RoomCombatResult incomplete = RoomCombatModel.ApplyCardDamage(floor, 6, 20, 28);
        Require(!incomplete.Supported && incomplete.UnsupportedReason == Reason,
            "Room-only damage must still reject Tower callbacks without a complete train.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("SPELL-ANGEL-DEATH PASS:54 native inputs/full state/RNG and immutable parents; room damage kills Savage,grants companion Multistrike and removes its aura; incomplete-room guard retained; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
