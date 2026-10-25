using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnemyCommandAttackChecks
{
    private const string Reason = "Enemy-selected unit attack requires native manager-order calibration.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var first = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units;
        var second = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units;
        Require(rows.Length == 26 && action.CardInstanceId == 31 && action.ActivatorUnitId == 16 &&
            action.TargetUnitId == 13 && action.RoomIndex == 0 &&
            first.Single(unit => unit.Id == 13) is { Team: CombatTeam.Enemy, Attack: 6, Health: 120 } &&
            second.Single(unit => unit.Id == 13).Health == 114 &&
            first.Single(unit => unit.Id == 16).Health == 60 && second.Single(unit => unit.Id == 16).Health == 60 &&
            second.Single(unit => unit.Id == 16).Status("cooldown")?.Stacks == 3,
            "Missing native enemy command self-target damage and caster cooldown boundary.");
        Console.WriteLine("ENEMY-COMMAND-ATTACK PASS:26 native inputs/full successors/RNG; native target manager makes front enemy hit itself for 6; caster health and cooldown preserved; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("ENEMY-COMMAND-ATTACK-PAIR PASS:original rejection and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
