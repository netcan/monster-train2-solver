using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChampionMeatShieldPreviewChecks
{
    // The raw request was labeled before isolating the counter discrepancy.
    private const string Reason = "Native room pre-combat shared world differs after input15.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (replay.HasValue)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("CHAMPION-MEAT-SHIELD-PREVIEW PASS:15 native inputs/full state/forecast/RNG; original two-stack champion and equipped MeatShield heal; no phantom17 allocation; four immutable branches and unknown-origin/heal controls; independentPair=" + replay.HasValue);
    }

    private static void Check(FixtureValue fixture, bool historical)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historical ? Historical : null);
        Require(rows.Length == 15 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original fifteen-input EndTurn boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var hero = actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 2);
        Require(before.Spawn.Turn == 2 && actual.Spawn.Turn == 3 &&
            before.Spawn.NextUnitId == 14 && actual.Spawn.NextUnitId == 17 &&
            actual.Spawn.Train.Context!.NextUnitId == 17 && hero.AssetKey == "MonsterChampionUnderlegion1" &&
            hero.EquipmentCards.SequenceEqual([33]) && hero.Triggers.Select(trigger => trigger.Kind)
                .SequenceEqual(["OnAnyHeroDeathOnFloor", "PostCombat"]) &&
            actual.Spawn.Train.Context.FindCard(33)?.DataId == CardGraftedEquipmentModel.EventEquipmentId &&
            actual.Spawn.Train.Context.SpawnPoints!.Units.All(point => point.UnitId < 17),
            "Lost original champion, equipped heal and exact live identity allocation.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(actual),
                "Champion/MeatShield EndTurn changed complete native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated the original parent.");
        var current = fixture.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "Continuously owned native prefix changed state or RNG.");
        }
        foreach (string control in new[] { "UnknownOrigin", "DifferentHeal" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            Rewrite(node);
            var ended = BattleTurnModel.EndTurn(node.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(ended.Supported && ended.State!.Spawn.NextUnitId == 18,
                "An uncalibrated healing signature discarded its unobserved identity: " + control);
            void Rewrite(JsonNode? value)
            {
                if (value is JsonObject obj)
                {
                    if (obj["Kind"]?.GetValueKind() == JsonValueKind.String && obj["Kind"]!.GetValue<string>() == "PostCombat" &&
                        obj["Origin"] is JsonObject origin && origin["UpgradeId"]?.GetValue<string>() == CardGraftedEquipmentModel.EventEquipmentId)
                    {
                        if (control == "UnknownOrigin") origin["UpgradeId"] = "unknown";
                        else { obj["Effects"]![0]!["Value"] = 11; obj["Effects"]![0]!["Action"]!["Value"] = 11; }
                    }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (value is JsonArray array) foreach (var child in array) Rewrite(child);
            }
        }
    }

    private static void Historical(BattleTurnState old, BattleTurnState actual)
    {
        Require(old.Spawn.NextUnitId == 18 && old.Spawn.Train.Context!.NextUnitId == 18 && actual.Spawn.NextUnitId == 17,
            "Preserve the original phantom identity allocation.");
        var node = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        var points = node["Context"]!["SpawnPoints"]!["Units"]!.AsArray();
        var phantom = points.Single(point => point!["UnitId"]!.GetValue<int>() == 17)!;
        Require(phantom["SpawnedInPreview"]!.GetValue<bool>() && phantom["Current"] == null && phantom["LastKnown"] == null,
            "Historical phantom must have no physical placement.");
        points.Remove(phantom);
        node["NextUnitId"] = 17;
        node["Context"]!["NextUnitId"] = 17;
        Require(ModelJson.Difference(node.ToJsonString(), NativeBranchObservationChecks.Key(actual)) == null,
            "The historical difference includes more than one unobserved preview identity.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
