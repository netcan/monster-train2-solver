using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConsumeEquipmentOrderChecks
{
    private const string Reason = "Native multi-equipment host sacrifice recycle order.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (replay != null)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("CONSUME-EQUIPMENT-ORDER PASS:26 complete native inputs/state/forecasts/RNG; Consume reuses released slot9 after two detached equipment entries; four immutable branches; independentNative=" + (replay != null));
    }
    private static void Check(FixtureValue fixture, bool historical)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historical ? Historical : null);
        Require(rows.Length == 26 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong sacrifice/Consume boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var host = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 3);
        Require(before.Spawn.Turn == 4 && actual.Spawn.Turn == 4 && action is { CardInstanceId: 16, RoomIndex: 2, TargetUnitId: 3 } &&
            host.EquipmentCards!.SequenceEqual(new[] { 12 }) &&
            Pile(before, "Standby").EquipmentConditions!.Where(condition => condition.HostUnitId == 3)
                .Select(condition => condition.CardId).SequenceEqual(new[] { 8, 19, 12 }) &&
            Pile(actual, "Standby").FreeSlots!.SequenceEqual(new[] { 9, 8, 7, 1, 4, 10 }) &&
            Pile(actual, "Exhausted").Cards.Select(card => card.InstanceId).SequenceEqual(new[] { 11, 26, 4, 2, 12, 8, 19, 16 }),
            "Missing detached equipment8/19, attached12 and consumed16 native order.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Ordered standby return changed complete native state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Consume standby return mutated its parent.");
    }
    private static void Historical(BattleTurnState old, BattleTurnState actual)
    {
        Require(Pile(old, "Standby").FreeSlots!.SequenceEqual(new[] { 8, 7, 9, 1, 4, 10 }) &&
            Pile(old, "Exhausted").Cards.Select(card => card.InstanceId).SequenceEqual(new[] { 11, 26, 4, 2, 12, 16, 8, 19 }),
            "Historical prematurely returned Consume card was replaced.");
        var node = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
        void Correct(JsonNode context)
        {
            context["OtherPiles"]!.AsArray().Single(pile => pile!["Name"]!.GetValue<string>() == "Standby")!["FreeSlots"] =
                JsonSerializer.SerializeToNode(Pile(actual, "Standby").FreeSlots, ModelJson.Options);
            context["OtherPiles"]!.AsArray().Single(pile => pile!["Name"]!.GetValue<string>() == "Exhausted")!["Cards"] =
                JsonSerializer.SerializeToNode(Pile(actual, "Exhausted").Cards, ModelJson.Options);
        }
        Correct(node["Spawn"]!["Train"]!["Context"]!);
        node["OtherPiles"] = node["Spawn"]!["Train"]!["Context"]!["OtherPiles"]!.DeepClone();
        foreach (var room in node["Spawn"]!["Train"]!["Rooms"]!.AsArray()) Correct(room!["Context"]!);
        Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) == NativeBranchObservationChecks.Key(actual),
            "Historical difference extends beyond standby free-list and exhaustion order.");
    }
    private static CardPileState Pile(BattleTurnState state, string name) => state.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == name);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
