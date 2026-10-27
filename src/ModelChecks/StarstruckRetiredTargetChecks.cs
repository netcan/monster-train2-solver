using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StarstruckRetiredTargetChecks
{
    internal const string Reason = "The spell target is missing.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = source.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 5 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong retired Starstruck boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
        var predicted = BattleActionModel.PlayCard(before, action);
        Require(predicted.Supported, "Retired-target cast is unsupported: " + predicted.Reason);
        var nativeStats = actual.Spawn.Train.Context!.Statistics!.Values.Select(value => JsonSerializer.Serialize(value)).ToArray();
        var predictedStats = predicted.State!.Spawn.Train.Context!.Statistics!.Values.Select(value => JsonSerializer.Serialize(value)).ToArray();
        Require(nativeStats.Order().SequenceEqual(predictedStats.Order()), "Retired-target statistics differ: nativeOnly=" +
            string.Join(";", nativeStats.Except(predictedStats)) + " predictedOnly=" + string.Join(";", predictedStats.Except(nativeStats)));
        NativeBranchObservationChecks.Replay(source, Reason);
        Require(action.CardInstanceId == 12 && action.RoomIndex == 0 && action.TargetUnitId == 7 && before.Spawn.Turn == 1 &&
            card.DataId == "753f20a2-c625-47b3-be4b-8600b673841e" &&
            before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 7).Health == 3 &&
            !actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 7) &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Status("dazed") == null) &&
            actual.Spawn.Train.Context!.Statistics!.Values.Single(value => value.CardId == 12 &&
                value.Duration == "ThisBattle" && value.Type == "AnyStatusEffectStacksAdded").Value == 3,
            "Starstruck must retire the three-health victim without stunning another actor.");
        string parent = NativeBranchObservationChecks.Key(before);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Retired-target OnCast differs from complete native state/forecast/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Retired-target cast mutated its parent.");
        var missing = new PlayCardAction(action.CardInstanceId, action.RoomIndex, targetUnitId: 9999);
        Require(BattleActionModel.PlayCard(before, missing).Rejection == ActionRejection.Illegal,
            "A missing initial target must remain illegal.");
        foreach (string control in new[] { "Owner", "Upgrade", "Status", "Trigger" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var changed = node["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
                .Single(item => item!["InstanceId"]!.GetValue<int>() == 12)!;
            var upgrade = changed["Permanent"]!["Upgrades"]!.AsArray()
                .Single(item => item!["DataId"]!.GetValue<string>() == "12793a9c-fae4-42ab-80bb-c3274e681c19")!;
            if (control == "Owner") changed["DataId"] = "uncalibrated-starstruck";
            if (control == "Upgrade") upgrade["DataId"] = "uncalibrated-daze";
            if (control == "Status") upgrade["Lifecycle"]!["Triggers"]![0]!["Actions"]![0]!["Statuses"]![0]!["Stacks"] = 2;
            if (control == "Trigger") upgrade["Lifecycle"]!["Triggers"]![0]!["Actions"]![0]!["Target"] = "Room";
            Require(!BattleActionModel.PlayCard(node.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Unknown retired-target signature lost its scope guard: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("STARSTRUCK-RETIRED-TARGET PASS:5 native inputs/full state/forecast/RNG; original moon Daze retains its killed reference and three status-stack statistics without a live target; four immutable branches/four scope controls/illegal initial target; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
