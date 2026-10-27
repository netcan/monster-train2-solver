using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EphemeralEquipmentRoutingChecks
{
    internal const string Reason = "Ephemeral Resin Blades attachment differs from native standby and purge routing.";
    internal static void Counterexample(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(key => fixture.GetProperty(key).GetInt32() == 0),
            "Preserve the sole native Ephemeral equipment mismatch.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 54, "Missing53-input prefix and54th attachment boundary.");
        var row = rows[^1];
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var historical = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Historical(historical, actual); Witness(before, action, actual);
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, index =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == expected,
                "Ephemeral equipment differs from full native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Ephemeral equipment mutated its native parent.");
        foreach (string trait in new[] { "CardTraitExhaustState", "CardTraitSelfPurge" })
        {
            JsonNode node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            JsonNode context = node["Spawn"]!["Train"]!["Context"]!;
            foreach (string collection in new[] { "CardInstances", "CardRegistry" })
            foreach (JsonNode? card in context[collection]!.AsArray().Where(item => item!["InstanceId"]!.GetValue<int>() == 36))
            {
                card!["MaskDescriptor"]!["BaseTraits"]!.AsArray().Add(JsonSerializer.SerializeToNode(
                    new CardTraitValue(trait, trait, 0, 0, false, 0), ModelJson.Options));
                if (trait == "CardTraitExhaustState")
                    card["ExhaustState"]!["BaseTraits"]!.AsArray().Add(JsonSerializer.SerializeToNode(
                        new CardExhaustTraitState(card["MaskDescriptor"]!["BaseTraits"]!.AsArray().Count - 1), ModelJson.Options));
            }
            var result = BattleActionModel.PlayCard(node.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(!result.Supported && result.Reason ==
                "Ephemeral equipment with Consume or SelfPurge requires native callback-order calibration.",
                "Uncalibrated compound equipment routing was accepted: " + trait + ": " + result.Reason);
        }
        Console.WriteLine("EPHEMERAL-EQUIPMENT-ROUTING PASS:54th-input counterexample, attached upgrades retained, standby hole and double purge, four immutable branches and two compound-trait guards.");
    }

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 54, "Incomplete54-input native observation.");
        Witness(rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!,
            rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!,
            rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("EPHEMERAL-EQUIPMENT-ROUTING NATIVE PASS:54 complete inputs/forecasts/RNG; independentPair=" + replay.HasValue);
    }

    private static void Witness(BattleTurnState before, PlayCardAction action, BattleTurnState actual)
    {
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(36)!;
        CardPileState standby = actual.OtherPiles.Single(pile => pile.Name == "Standby");
        Require(before.Spawn.Turn == 8 && action is { CardInstanceId: 36, RoomIndex: 2, TargetUnitId: 8 } &&
            card.DataId == "1d2e6259-e3ef-4a6a-8894-2c12db9fd4a9" &&
            CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitEphemeral") &&
            card.Temporary.Upgrades.Single().AssetKey == "FetchCostReduction" &&
            standby.EntrySlots!.Count == 12 && standby.EntrySlots[11] == 0 && standby.FreeSlots!.SequenceEqual([11]) &&
            standby.Cards.All(token => token.InstanceId != 36) && standby.EquipmentConditions!.All(binding => binding.CardId != 36) &&
            actual.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == 36) == 2 &&
            actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 8).EquipmentCards!.Contains(36),
            "Lost fetched Ephemeral Resin Blades, live attachment, standby hole or both purge callbacks.");
    }

    private static void Historical(BattleTurnState historical, BattleTurnState actual)
    {
        Require(historical.OtherPiles.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == 36) &&
            historical.OtherPiles.Single(pile => pile.Name == "Purged").Cards.All(card => card.InstanceId != 36) &&
            actual.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Count(card => card.InstanceId == 36) == 2,
            "Lost original retained-standby versus double-purge mismatch.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
