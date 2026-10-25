using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptDiademChecks
{
    private const string Reason = "Corrupt State requires an ordinary spell or Monster with captured trait provenance.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 32, "Changed native Corrupt Diadem prefix.");
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var changed = actual.Spawn.Train.Context!;
        var card = context.FindCard(22)!;
        var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
        var oldRoom = context.RoomCapacities!.Single(room => room.RoomIndex == 1);
        var newRoom = changed.RoomCapacities!.Single(room => room.RoomIndex == 1);
        var oldHost = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 20);
        var newHost = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 20);
        Require(action.CardInstanceId == 22 && action.RoomIndex == 1 && action.TargetUnitId == 20 &&
            card.DataId == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" && rule.AssetKey == "ExilesDiadem" && rule.CardType == "Equipment" &&
            oldRoom.Corruption == 5 && newRoom.Corruption == 6 && oldRoom.MaxCorruption == 4 && newRoom.MaxCorruption == 4 &&
            before.Energy == 4 && actual.Energy == 3 && card.EquippedUnitId == 0 && changed.FindCard(22)!.EquippedUnitId == 20 &&
            oldHost.EquipmentCards!.SequenceEqual(new[] { 11 }) && newHost.EquipmentCards!.SequenceEqual(new[] { 22 }) &&
            oldHost.Size == 3 && newHost.Size == 1,
            "Missing native corruption-before-equipment/over-cap/size boundary.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Parallel Corrupt Diadem differs from native state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Corrupt Diadem mutated its parent.");
        foreach (string control in new[] { "owner", "type", "ability", "runtime", "definition", "stack", "removable", "upgrade" })
        {
            var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var trait = json["MaskDescriptor"]!["BaseTraits"]![0]!;
            var ruleJson = JsonSerializer.SerializeToNode(rule, ModelJson.Options)!;
            if (control == "owner") json["DataId"] = "uncalibrated-corrupt-equipment";
            else if (control == "type") ruleJson["CardType"] = "TrainRoomAttachment";
            else if (control == "ability") ruleJson["IsAnyAbility"] = true;
            else if (control == "runtime") trait["ParamInt"] = 1;
            else if (control == "definition") trait["DefinitionParamInt"] = 1;
            else if (control == "stack") trait["StackMode"] = 1;
            else if (control == "removable") trait["Removable"] = false;
            else json["Permanent"]!["Upgrades"]![0]!["DataId"] = "unobserved-corrupt-upgrade";
            var changedCard = json.Deserialize<CardInstanceState>(ModelJson.Options)!;
            string parent = JsonSerializer.Serialize(changedCard, ModelJson.Options);
            var rejected = CardCorruptStateModel.BeforePlay(before.Spawn.Train, changedCard,
                ruleJson.Deserialize<CardPlayRule>(ModelJson.Options)!, action.RoomIndex, before.PlayRules);
            Require(!rejected.Supported && rejected.UnsupportedReason == Reason &&
                parent == JsonSerializer.Serialize(changedCard, ModelJson.Options), "Unknown signature bypassed guard: " + control);
        }
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("CORRUPT-DIADEM PASS:32 complete native inputs/state/RNG; corruption5->6/max4 before equipment/size; eight immutable parallel branches and eight signature guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
