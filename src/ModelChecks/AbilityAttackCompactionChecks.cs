using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AbilityAttackCompactionChecks
{
    private const string Reason = "Rolling native state differs after decision 27: $.Context.SpawnPoints.Groups[2].Occupants[0]";

    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(rows.Length == 27 && before.Spawn.Turn == 4 && action.CardInstanceId == 25 &&
            action.ActivatorUnitId == 3 && action.TargetUnitId == 3 && action.RoomIndex == 1,
            "Missing the natural Steelsinger Wrath ability boundary.");
        Require(before.PlayRules!.Cards.Single(rule => rule.AssetKey == "WrathAbility").Effects.Single().Type == "AttackWithUnit" &&
            before.Spawn.Train.Context!.SpawnPoints!.Groups[2].Occupants.Take(2).SequenceEqual([13, 14]) &&
            after.Spawn.Train.Context!.SpawnPoints!.Groups[2].Occupants.Take(2).SequenceEqual([14, 0]) &&
            after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.All(unit => unit.Id != 13),
            "The commanded attack must kill enemy13 and center surviving enemy14.");
        Require(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 3).Health ==
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 3).Health,
            "This one-sided ability attack must preserve its source's health.");
        Console.WriteLine("ABILITY-ATTACK-COMPACTION PASS:27 native inputs/full successors/RNG, legal Wrath attack, native UI death centering and immutable parents; component observation.");
    }

    private static void HistoricalMismatch(BattleTurnState old, BattleTurnState actual)
    {
        Require(old.Spawn.Train.Context!.SpawnPoints!.Groups[2].Occupants.Take(2).SequenceEqual([0, 14]) &&
            actual.Spawn.Train.Context!.SpawnPoints!.Groups[2].Occupants.Take(2).SequenceEqual([14, 0]),
            "The original prediction must retain its observed vacancy.");
        JsonNode projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        JsonNode observed = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        projected["Context"]!["SpawnPoints"] = observed["Context"]!["SpawnPoints"]!.DeepClone();
        Require(JsonNode.DeepEquals(projected, observed), "Historical mismatch extends beyond physical spawn-point references.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("ABILITY-ATTACK-COMPACTION-PAIR PASS:original vacancy mismatch preserved, repaired forecast in an independent native process.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
