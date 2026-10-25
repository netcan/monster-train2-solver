using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptEquipmentChecks
{
    private const string Reason = "Corrupt State requires an ordinary spell or Monster with captured trait provenance.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Check(rows);
        if (replay != null)
        {
            Check(NativeBranchObservationChecks.Replay(replay.Value, Reason));
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("CORRUPT-EQUIPMENT PASS:24 native inputs/full state/RNG; starting Corrupt AmbientCharge; Corruption4->5 beyond max4; energy/attachment/immutable parents;6 signature controls;" +
            (replay == null ? " independent replay pending." : " independent native pair."));
    }
    private static void Check(FixtureValue[] rows)
    {
        Require(rows.Length == 24, "Incomplete Corrupt equipment prefix.");
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var changed = after.Spawn.Train.Context!;
        var card = context.FindCard(10)!;
        var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
        var oldRoom = context.RoomCapacities!.Single(room => room.RoomIndex == 0);
        var room = changed.RoomCapacities!.Single(room => room.RoomIndex == 0);
        Require(before.Spawn.Turn == 7 && action.CardInstanceId == 10 && action.TargetUnitId == 2 && action.RoomIndex == 0 &&
            card.DataId == "75cfe90b-47b9-406c-b891-33564aac6677" && rule.CardType == "Equipment" &&
            oldRoom.Corruption == 4 && room.Corruption == 5 && oldRoom.MaxCorruption == 4 && room.MaxCorruption == 4 &&
            before.Energy == 5 && after.Energy == 5 && card.EquippedUnitId == 0 && changed.FindCard(10)!.EquippedUnitId == 2 &&
            after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 2).EquipmentCards!.Contains(10),
            "Missing original starting-Corrupt equipment callback/over-cap corruption/attachment boundary.");
        foreach (string control in new[] { "owner", "runtime", "definition", "stack", "removable", "upgrade" })
        {
            var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var trait = json["MaskDescriptor"]!["BaseTraits"]![0]!;
            if (control == "owner") json["DataId"] = "uncalibrated-corrupt-equipment";
            else if (control == "runtime") trait["ParamInt"] = 1;
            else if (control == "definition") trait["DefinitionParamInt"] = 1;
            else if (control == "stack") trait["StackMode"] = 1;
            else if (control == "removable") trait["Removable"] = false;
            else json["Permanent"]!["Upgrades"]![0]!["DataId"] = "unobserved-corrupt-upgrade";
            var result = CardCorruptStateModel.BeforePlay(before.Spawn.Train,
                json.Deserialize<CardInstanceState>(ModelJson.Options), rule, 0, before.PlayRules);
            Require(!result.Supported && result.UnsupportedReason == Reason, "Uncalibrated equipment bypassed its guard: " + control);
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
