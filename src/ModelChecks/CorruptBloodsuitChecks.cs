using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptBloodsuitChecks
{
    private const string Reason = "Corrupt State requires an ordinary spell or Monster with captured trait provenance.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 19, "Incomplete Corrupt Bloodsuit prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var prior = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        var card = prior.FindCard(20)!;
        var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
        var oldHost = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 14);
        var host = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 14);
        Require(before.Spawn.Turn == 3 && action.CardInstanceId == 20 && action.RoomIndex == 0 && action.TargetUnitId == 14 &&
            card.DataId == "51eaab0c-a656-456e-8d93-7d536b31d9cf" && rule.AssetKey == "PenitentsBloodsuit" && rule.CardType == "Equipment" &&
            prior.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 0 &&
            after.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1 &&
            before.Energy == 5 && actual.Energy == 5 && prior.NextCardId == after.NextCardId &&
            card.EquippedUnitId == 0 && after.FindCard(20)!.EquippedUnitId == 14 &&
            after.FindCard(20)!.PlayCount == card.PlayCount + 1 &&
            prior.Cards.Hand.Any(token => token.InstanceId == 20) && !after.Cards.Hand.Any(token => token.InstanceId == 20) &&
            after.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Count(token => token.InstanceId == 20) == 1 &&
            oldHost.Team == CombatTeam.Enemy && oldHost.EquipmentCards!.Count == 0 && host.EquipmentCards!.SequenceEqual(new[] { 20 }) &&
            oldHost.Health == host.Health && oldHost.Attack == host.Attack && host.Status("armor")!.Stacks == 120 &&
            host.Triggers.Single().Kind == "OnShift" && host.Triggers.Single().Origin.EquipmentCardId == 20 &&
            host.Triggers.Single().Effects.Single().Action is { Type: "Damage", Target: "Self", Value: 30 },
            "Missing native corruption/zero-cost play, enemy attachment or pending shift damage.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == native &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Bloodsuit differs or mutates its parent.");
        });
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
            string immutable = JsonSerializer.Serialize(changedCard, ModelJson.Options);
            var result = CardCorruptStateModel.BeforePlay(before.Spawn.Train, changedCard,
                ruleJson.Deserialize<CardPlayRule>(ModelJson.Options)!, 0, before.PlayRules);
            Require(!result.Supported && result.UnsupportedReason == Reason &&
                immutable == JsonSerializer.Serialize(changedCard, ModelJson.Options), "Uncalibrated Bloodsuit passed: " + control);
        }
        Console.WriteLine("CORRUPT-BLOODSUIT PASS:19 native inputs/full state/RNG, corruption0->1, zero-cost enemy attachment/Standby/pending shift damage, four immutable branches and eight guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
