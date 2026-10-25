using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FloorTransitionPiercingChecks
{
    private const string Reason = "Native floor-transition relic damage bypasses Titanskin.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, historicalMismatch: true);
        if (replay != null)
        {
            Check(replay.Value, historicalMismatch: false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("FLOOR-TRANSITION-PIERCING PASS: native balance/runtime ordinary=false,relic=true;11 full native inputs/state/RNG; two Titanskin5 targets lose3 on ascend; ordinary damage stays blocked;" +
            (replay == null ? " independent replay pending." : " independent corrected native pair."));
    }
    private static void Check(FixtureValue fixture, bool historicalMismatch)
    {
        var calibration = fixture.GetProperty("FloorTransitionPiercing");
        var balance = calibration.GetProperty("BalanceEffects").EnumerateArray().Select(value => value.GetString()).ToArray();
        var owner = calibration.GetProperty("Owners").EnumerateArray().Single();
        Require(!calibration.GetProperty("Ordinary").GetBoolean() && owner.GetProperty("Piercing").GetBoolean() &&
            owner.GetProperty("DataId").GetString() == "269a6f43-9982-4683-ac39-1a862e481898" &&
            owner.GetProperty("AssetKey").GetString() == "DamageOnFloorTransition" &&
            owner.GetProperty("Effects").EnumerateArray().Select(value => value.GetString()).SequenceEqual(
                new[] { RelicModel.DamageEnemyOnAscend, RelicModel.DamageEnemyOnDescend }) &&
            balance.Contains(RelicModel.DamageEnemyOnAscend) && balance.Contains(RelicModel.DamageEnemyOnDescend),
            "Missing the original native balance/runtime piercing decision.");
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason,
            historicalMismatch ? HistoricalMismatch : null);
        Require(rows.Length == 11 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Incomplete native transition prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 2 && after.Spawn.Turn == 3 &&
            Actor(before, 11).Health == 250 && Actor(after, 11).Health == 247 &&
            Actor(before, 12).Health == 5 && Actor(after, 12).Health == 2 &&
            new[] { 11, 12 }.All(id => Actor(before, id).Status("titanskin")?.Stacks == 5 &&
                Actor(after, id).Status("titanskin")?.Stacks == 5 &&
                before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex == 0 &&
                after.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex == 1),
            "Missing the native Titanskin5/three piercing damage on movement0->1 boundary.");
        var room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
        var ordinary = RoomCombatModel.ApplyRelicDamage(room, 11, 3, _ => { });
        var piercing = RoomCombatModel.ApplyRelicDamage(room, 11, 3, _ => { }, piercing: true);
        Require(ordinary.Supported && piercing.Supported &&
            ordinary.State!.Units.Single(unit => unit.Id == 11).Health == 250 &&
            piercing.State!.Units.Single(unit => unit.Id == 11).Health == 247,
            "Piercing repair changed ordinary damage or failed to bypass Titanskin.");
    }
    private static void HistoricalMismatch(BattleTurnState historical, BattleTurnState actual) =>
        Require(Actor(historical, 11).Health == 250 && Actor(actual, 11).Health == 247 &&
            Actor(historical, 12).Health == 5 && Actor(actual, 12).Health == 2,
            "Original non-piercing forecast was replaced or changed.");
    private static CombatUnit Actor(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
