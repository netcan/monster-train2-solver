using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptPoisonCombatChecks
{
    private const string Reason = "Room combat damage source attribution differs after native EndTurn.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 8 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native corruption boundary.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Corruption(before) == 2 && Corruption(actual) == 3 &&
            Units(before).Single(unit => unit.Id == 5).Health == 45 &&
            Units(before).Single(unit => unit.Id == 5).Status("corrupt_poison")?.Stacks == 2 &&
            Units(actual).All(unit => unit.Id != 5) &&
            Units(actual).Single(unit => unit.Id == 2).LastAttackerId == 0,
            "Attack-time corruption did not kill the enemy and clear its attacker relationship.");
        CorruptPoisonChecks.Run();
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second, originalPredictionPresent: true);
        }
        Console.WriteLine("CORRUPT-POISON-COMBAT PASS:8 native inputs/full state/RNG; attack-time corruption2->3 makes poison deal6 instead of4, removes enemy/attacker identity; original mismatch retained; poison controls pass; independentPair=" + replay.HasValue);
    }

    private static IEnumerable<CombatUnit> Units(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units);
    private static int? Corruption(BattleTurnState state) => state.Spawn.Train.Context!.RoomCapacities!
        .Single(room => room.RoomIndex == 0).Corruption;
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        Require(Corruption(predicted) == 3 && Corruption(actual) == 3 &&
            Units(predicted).Single(unit => unit.Id == 5).Health == 2 && Units(actual).All(unit => unit.Id != 5) &&
            Units(predicted).Single(unit => unit.Id == 2).LastAttackerId == 5 &&
            Units(actual).Single(unit => unit.Id == 2).LastAttackerId == 0,
            "Original stale-corruption survival and attacker relationship were overwritten.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
