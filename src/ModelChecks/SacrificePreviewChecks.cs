using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SacrificePreviewChecks
{
    private const string Reason = "Preview restoration drops the private sacrifice cache of an unretained aura-world actor.";
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        NativeBranchObservationChecks.Replay(replay, Reason);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var unit = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 15);
        var revived = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 15);
        var world = after.Spawn.Train.Context!.Enchantments!;
        Require(rows.Length == 26 && before.Spawn.Turn == 3 && action.CardInstanceId == 20 && action.RoomIndex == 2 &&
            action.TargetUnitId == 15 && unit.Health == 9 && unit.Status("undying")?.Stacks == 1 &&
            unit.Status("unstable")?.Stacks == 4 && revived.Health == 1 && revived.Status("undying") == null &&
            revived.Status("unstable")?.Stacks == 8 && revived.SacrificeCardId == 0 && revived.DeathState?.IsSacrifice == false &&
            world.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 15).SacrificeCardId == 0,
            "Missing native sacrifice/revive/unstable preview private-reference boundary.");
        Console.WriteLine("SACRIFICE-PREVIEW PASS:26 independent paired native inputs/full state/RNG; revived assistant9->1HP/unstable4->8, private sacrifice20->0 retained after aura-world pruning; original mismatch preserved.");
    }
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        var node = JsonSerializer.SerializeToNode(predicted, ModelJson.Options)!;
        JsonNode train = node["Spawn"]!["Train"]!;
        foreach (JsonNode rooms in new[] { train["Rooms"]!, train["Context"]!["Enchantments"]!["Rooms"]! })
        {
            var unit = rooms.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == 15)!;
            Require(unit["SacrificeCardId"]!.GetValue<int>() == 20, "Original private-reference mismatch was overwritten.");
            unit["SacrificeCardId"] = 0;
        }
        Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
            NativeBranchObservationChecks.Key(actual), "Historical mismatch extends beyond the two sacrifice references.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
