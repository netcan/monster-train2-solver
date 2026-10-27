using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DiscardEquipmentTurnChecks
{
    // Preserve the initial diagnostic request, including its superseded cause hypothesis.
    internal const string Reason = "Native terminal enemy death records cardless statistics during turn combat.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, historicalBoundaryMismatch: (old, native) => {
            Require(Exhausted(old, "ThisTurn") == 1 && Exhausted(native, "ThisTurn") == 0 &&
                Exhausted(old, "PreviousTurn") == 0 && Exhausted(native, "PreviousTurn") == 1,
                "Preserve Fortify's original new-turn versus previous-turn exhaust witness.");
            JsonNode node = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
            var oldSlots = old.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").FreeSlots!;
            var nativeSlots = native.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").FreeSlots!;
            Require(oldSlots.SequenceEqual(new[] { 2, 7, 4, 6, 0 }) && nativeSlots.SequenceEqual(new[] { 7, 4, 2, 6, 0 }),
                "Preserve the native discard-before-combat standby slot release order.");
            var oldExhausted = old.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards;
            var nativeExhausted = native.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards;
            Require(oldExhausted.Select(card => card.InstanceId).SequenceEqual(new[] { 4, 18, 2, 15, 5 }) &&
                nativeExhausted.Select(card => card.InstanceId).SequenceEqual(new[] { 4, 18, 5, 2, 15 }),
                "Fortify must enter Exhausted before the two combat deaths.");
            ReplaceContext(node, context => {
                context["Statistics"] = JsonSerializer.SerializeToNode(native.Spawn.Train.Context!.Statistics, ModelJson.Options);
                context["OtherPiles"]!.AsArray().Single(pile => pile!["Name"]!.GetValue<string>() == "Standby")!
                    ["FreeSlots"] = JsonSerializer.SerializeToNode(nativeSlots, ModelJson.Options);
                context["OtherPiles"]!.AsArray().Single(pile => pile!["Name"]!.GetValue<string>() == "Exhausted")!
                    ["Cards"] = JsonSerializer.SerializeToNode(nativeExhausted, ModelJson.Options);
            });
            string normalized = NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!);
            string actualKey = NativeBranchObservationChecks.Key(native);
            Require(normalized == actualKey, "The original mismatch extends beyond exhaust statistics and standby release order: " + ModelJson.Difference(normalized, actualKey));
        });
        Require(rows.Length == 27 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong discard equipment boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = before.Spawn.Train.Context!;
        Require(before.Spawn.Turn == 4 && actual.Spawn.Turn == 5 && context.Cards.Hand.Count > 0 &&
            context.FindCard(5)?.DataId == "1dd23b59-fa5e-44bc-83ca-de119ede4f21" &&
            context.OtherPiles!.Single(pile => pile.Name == "Standby").EquipmentConditions!.Single().HostUnitId == 2 &&
            !before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 2) &&
            !actual.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == 5),
            "Native Fortify must leave its detached old host during this turn's hand discard.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Discard equipment return differs from full native state/forecasts/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Discard equipment return mutated its parent.");

        JsonNode empty = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        ReplaceContext(empty, value => {
            var hand = value["Cards"]!["Hand"]!.AsArray();
            foreach (var card in hand) value["Cards"]!["Discard"]!.AsArray().Add(card!.DeepClone());
            hand.Clear();
        });
        var noDiscard = BattleTurnModel.EndTurn(empty.Deserialize<BattleTurnState>(ModelJson.Options)!);
        Require(noDiscard.Supported && Exhausted(noDiscard.State!, "ThisTurn") == 1 && Exhausted(noDiscard.State!, "PreviousTurn") == 0,
            "Empty hands must retain the next draw's global return timing.");

        JsonNode alive = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        ReplaceContext(alive, value => value["OtherPiles"]!.AsArray().Single(pile => pile!["Name"]!.GetValue<string>() == "Standby")!
            ["EquipmentConditions"]![0]!["HostUnitId"] = 3);
        var attached = BattleTurnModel.EndTurn(alive.Deserialize<BattleTurnState>(ModelJson.Options)!);
        Require(attached.Supported && attached.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 3) &&
            attached.State.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == 5),
            "A living equipment host must retain its standby card.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("DISCARD-EQUIPMENT-TURN PASS:27 native inputs/full state/forecasts/RNG; Fortify return precedes statistics rotation; four immutable branches/empty-hand and living-host controls; independentPair=" + replay.HasValue);
    }
    private static int Exhausted(BattleTurnState state, string duration) => state.Spawn.Train.Context!.Statistics!.Values
        .Where(value => value.CardId == 5 && value.Type == "TimesExhausted" && value.Duration == duration).Sum(value => value.Value);
    private static void ReplaceContext(JsonNode node, Action<JsonNode> change)
    {
        JsonNode context = node["Spawn"]!["Train"]!["Context"]!;
        change(context);
        node["OtherPiles"] = context["OtherPiles"]!.DeepClone();
        foreach (var room in node["Spawn"]!["Train"]!["Rooms"]!.AsArray()) room!["Context"] = context.DeepClone();
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
