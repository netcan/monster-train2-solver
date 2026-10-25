using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptRoomAttachmentChecks
{
    private const string Reason = "Corrupt State requires an ordinary spell or Monster with captured trait provenance.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(NativeBranchObservationChecks.Replay(source, Reason));
        if (replay != null)
        {
            Check(NativeBranchObservationChecks.Replay(replay.Value, Reason));
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("CORRUPT-ROOM-ATTACHMENT PASS:5 full native inputs/state/forecasts/RNG; RageRoom starting Corrupt; corruption1->2/max4; energy1 unchanged; attachment installed;7 signature controls;" +
            (replay == null ? " independent replay pending." : " independent native pair."));
    }
    private static void Check(FixtureValue[] rows)
    {
        Require(rows.Length == 5, "Missing original Corrupt room-attachment prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(8)!;
        var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
        var oldRoom = before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 2);
        var room = after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2);
        Require(before.Spawn.Turn == 1 && after.Spawn.Turn == 1 && action.CardInstanceId == 8 && action.RoomIndex == 2 &&
            card.DataId == "93759003-bb97-44e4-9ef6-472c55bee2d9" && rule.CardType == "TrainRoomAttachment" &&
            oldRoom.Corruption == 1 && room.Corruption == 2 && oldRoom.MaxCorruption == 4 && room.MaxCorruption == 4 &&
            before.Energy == 1 && after.Energy == 1 && oldRoom.Attachments!.Count == 0 && room.Attachments!.Count == 1 &&
            room.Attachments[0].DataId == card.DataId && room.Attachments[0].AssetKey == "RageRoom" &&
            oldRoom.AttachmentsAddedFromCards == 0 && room.AttachmentsAddedFromCards == 1,
            "Missing native starting Corrupt before room-attachment installation.");
        foreach (string control in new[] { "owner", "type", "runtime", "definition", "stack", "removable", "upgrade" })
        {
            var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var trait = json["MaskDescriptor"]!["BaseTraits"]![0]!;
            var checkedRule = rule;
            if (control == "owner") json["DataId"] = "uncalibrated-corrupt-room";
            else if (control == "type")
            {
                var ruleJson = JsonSerializer.SerializeToNode(rule, ModelJson.Options)!;
                ruleJson["CardType"] = "Equipment";
                checkedRule = ruleJson.Deserialize<CardPlayRule>(ModelJson.Options)!;
            }
            else if (control == "runtime") trait["ParamInt"] = 1;
            else if (control == "definition") trait["DefinitionParamInt"] = 1;
            else if (control == "stack") trait["StackMode"] = 1;
            else if (control == "removable") trait["Removable"] = false;
            else json["Permanent"]!["Upgrades"]![0]!["DataId"] = "unobserved-corrupt-upgrade";
            var result = CardCorruptStateModel.BeforePlay(before.Spawn.Train,
                json.Deserialize<CardInstanceState>(ModelJson.Options), checkedRule, 2, before.PlayRules);
            Require(!result.Supported && result.UnsupportedReason == Reason, "Uncalibrated Corrupt attachment bypassed guard: " + control);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
