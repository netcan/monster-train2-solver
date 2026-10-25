using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NaturalHydraCatalogChecks
{
    internal const string Reason = "Triggered equipment removal requires its self target, equipment catalog and bindings.";
    internal const string GeneratedReason = "Missing equipment definition or card relationship.";

    internal static void GeneratedPair(FixtureValue original, FixtureValue repaired)
    {
        const string graftId = "0a928329-666c-4ce6-a1c3-413977e37e46";
        var fresh = NativeBranchObservationChecks.Replay(repaired, GeneratedReason);
        var old = original.GetProperty("Rows").EnumerateArray().ToArray();
        Require(original.GetProperty("NativeCompleted").GetBoolean() && old.Length == 31 && fresh.Length == old.Length &&
            original.GetProperty("Request").GetProperty("Prefix").ContentEquals(repaired.GetProperty("Request").GetProperty("Prefix")) &&
            original.GetProperty("Request").GetProperty("Reason").GetString() == GeneratedReason &&
            old[^1].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            old[^1].GetProperty("UnsupportedReason").GetString() == GeneratedReason,
            "Preserve the original generated-equipment refusal and all30 prefix inputs.");
        for (int index = 0; index < old.Length; index++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(old[index].GetProperty(field).ContentEquals(fresh[index].GetProperty(field)), "Changed native input " + index);
            foreach (string field in new[] { "Before", "Actual" })
            {
                var first = old[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                var second = fresh[index].GetProperty(field).Deserialize<BattleTurnState>()!;
                Require(NativeBranchObservationChecks.Key(WithoutCatalog(first)) == NativeBranchObservationChecks.Key(WithoutCatalog(second)),
                    "Changed native state/RNG beyond the equipment catalog at " + index + " " + field);
            }
        }
        var initial = fresh[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var oldInitial = old[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(initial.Spawn.Train.Context!.CardInstances.All(card => card.DataId != graftId) &&
            initial.PlayRules!.Cards.Single(rule => rule.AssetKey == "TwistedAssistant").SpawnUnit!.Modifiers!.Graft!.Card.DataId == graftId &&
            !oldInitial.Spawn.Train.Context!.SummonCatalog!.Cards.Any(card => card.Creation.DataId == graftId) &&
            initial.Spawn.Train.Context.SummonCatalog!.Cards.Any(card => card.Creation.DataId == graftId && card.Equipment != null),
            "Authored graft equipment must be available in the catalog before its first live card is created.");
        var before = fresh[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = fresh[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = fresh[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var host = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        var after = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == host.Id);
        Require(action.CardInstanceId == 28 && host.AssetKey == "MonsterHydraArmedHorror" &&
            before.Spawn.Train.Context!.FindCard(28)!.DataId == graftId && host.Status("multistrike")?.Stacks == 1 &&
            after.Status("multistrike")?.Stacks == 2 && after.Attack == host.Attack && after.Health == host.Health &&
            after.Status("trample") == null && after.EquipmentCards is { Count: 0 } &&
            actual.Spawn.Train.Context!.FindCard(28)!.EquippedUnitId == 0 &&
            actual.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == 28),
            "Generated equipment must grant one persistent Multistrike, remove equipment stats/trample/triggers and retain detached Standby ownership.");
        var absentDefinition = old[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(BattleActionModel.PlayCard(absentDefinition, action).Reason == GeneratedReason,
            "Missing generated-definition control was incorrectly accepted.");
        Console.WriteLine("NATURAL-HYDRA-GRAFT-PAIR PASS:31 unchanged native inputs/full successors/RNG; authored graft catalog captured before creation, Multistrike1->2 and complete equipment removal; original refusal/control preserved. No Ring7 victory claimed.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired)
    {
        var fresh = NativeBranchObservationChecks.Replay(repaired, Reason);
        Require(original.GetProperty("NativeCompleted").GetBoolean() && fresh.Length == 1 &&
            original.GetProperty("Rows").GetArrayLength() == 1 &&
            original.GetProperty("Request").GetProperty("Prefix").GetArrayLength() == 0 &&
            original.GetProperty("Request").GetProperty("Reason").GetString() == Reason &&
            original.GetProperty("Request").GetProperty("SourceSHA256").GetString() ==
                "9ee75d83b164a11ec9c93c2ee73d634315a609ebc99d333079bb227f2f851512",
            "Missing the original unchanged natural Ring7 zero-prefix refusal.");
        var old = original.GetProperty("Rows")[0];
        Require(old.GetProperty("Expected").ValueKind == FixtureKind.Null &&
            old.GetProperty("UnsupportedReason").GetString() == Reason &&
            old.GetProperty("Action").ContentEquals(fresh[0].GetProperty("Action")) &&
            !old.GetProperty("IsEndTurn").GetBoolean(), "Original refusal or actual input changed.");
        foreach (string field in new[] { "Before", "Actual" })
        {
            var before = old.GetProperty(field).Deserialize<BattleTurnState>()!;
            var after = fresh[0].GetProperty(field).Deserialize<BattleTurnState>()!;
            Require(before.Spawn.Train.Context!.SummonCatalog == null && after.Spawn.Train.Context!.SummonCatalog != null &&
                NativeBranchObservationChecks.Key(before) == NativeBranchObservationChecks.Key(WithoutCatalog(after)),
                "Native capture changed more than the added equipment catalog at " + field);
        }
        var root = fresh[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = fresh[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = fresh[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var unit = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == action.CardInstanceId);
        Require(action.CardInstanceId == 3 && action.RoomIndex == 0 && unit.AssetKey == "MonsterHydraArmedHorror" &&
            unit.Attack == 29 && unit.Health == 35 && unit.EquipmentCards is { Count: 0 } &&
            unit.Triggers.Single(trigger => trigger.Kind == "OnEquipmentAdded").Effects.Any(effect =>
                effect.Type == "CardEffectRemoveEquipment" && effect.Action?.Target == "Self") &&
            root.Spawn.Train.Context!.SummonCatalog!.Cards.Any(card =>
                card.Creation.DataId == "7369cb8e-f8cf-44be-8667-c699c630c063" && card.Equipment != null),
            "Missing the authored Hydra consumption trigger and natural Meat Shield equipment definition.");
        Require(BattleActionModel.PlayCard(WithoutCatalog(root), action).Reason == Reason,
            "Missing-catalog control unexpectedly accepted an unmodeled deployment.");
        foreach (var fixture in new[] { original, repaired })
        {
            var overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
            Require(overlay.GetProperty("Published").GetInt32() == 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
                overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean() &&
                overlay.GetProperty("Scenario").GetString() == "Level7BattleJunker" &&
                // This diagnostic driver retains its environment's default seed
                // label. The actual campaign is bound by the source hash/root.
                overlay.GetProperty("Seed").GetString() == "424242", "Changed natural/player projection boundary.");
        }
        Console.WriteLine("NATURAL-HYDRA-CATALOG PASS: original native refusal and independent repaired deployment/full state/RNG; only equipment catalog added, absent-catalog control retained. No Ring7 victory claimed.");
    }

    private static BattleTurnState WithoutCatalog(BattleTurnState state)
    {
        var node = JsonSerializer.SerializeToNode(state, ModelJson.Options)!;
        void Clear(JsonNode? value)
        {
            if (value is JsonObject obj)
            {
                foreach (var entry in obj.ToArray())
                    if (entry.Key == "SummonCatalog") obj[entry.Key] = null;
                    else Clear(entry.Value);
            }
            else if (value is JsonArray array) foreach (var child in array) Clear(child);
        }
        Clear(node);
        return node.Deserialize<BattleTurnState>(ModelJson.Options)!;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
