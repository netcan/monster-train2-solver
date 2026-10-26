using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerEggCatalogChecks
{
    private const string Reason = "Triggered summons require captured definitions, physical positions and shared identity allocation.";

    private static string WithoutCatalog(BattleTurnState state)
    {
        var json = JsonSerializer.SerializeToNode(state, ModelJson.Options)!;
        var context = json["Spawn"]!["Train"]!["Context"]!;
        context["SummonCatalog"] = null;
        var rules = context["StatusRules"]!.AsArray();
        foreach (var rule in rules.Where(rule => rule!["Id"]!.GetValue<string>() == "cardless").ToArray()) rules.Remove(rule);
        return NativeBranchObservationChecks.Key(json.Deserialize<BattleTurnState>(ModelJson.Options)!);
    }

    internal static void Native(FixtureValue missing, FixtureValue complete, FixtureValue? replay = null)
    {
        Require(missing.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            missing.GetProperty("NativeCompleted").GetBoolean(), "Missing original native catalog refusal.");
        var original = missing.GetProperty("Rows").EnumerateArray().ToArray();
        var oldRequest = missing.GetProperty("Request");
        var newRequest = complete.GetProperty("Request");
        Require(oldRequest.GetProperty("Reason").GetString() == Reason &&
            oldRequest.GetProperty("Prefix").ContentEquals(newRequest.GetProperty("Prefix")) &&
            oldRequest.GetProperty("Action").ContentEquals(newRequest.GetProperty("Action")) && original.Length == 20,
            "Catalog calibration changed its original inputs.");
        var rows = NativeBranchObservationChecks.Replay(complete, Reason);
        Require(rows.Length == 20 && original[^1].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            original[^1].GetProperty("UnsupportedReason").GetString() == Reason,
            "Lost the original refused summon.");
        foreach (string field in new[] { "Root", "FailedRoot" })
            Require(WithoutCatalog(newRequest.GetProperty(field).Deserialize<BattleTurnState>()!) ==
                NativeBranchObservationChecks.Key(oldRequest.GetProperty(field).Deserialize<BattleTurnState>()!),
                "Capture repair changed gameplay in request " + field);
        for (int index = 0; index < rows.Length; index++)
        {
            Require(original[index].GetProperty("Action").ContentEquals(rows[index].GetProperty("Action")) &&
                original[index].GetProperty("IsEndTurn").ContentEquals(rows[index].GetProperty("IsEndTurn")),
                "Catalog repair changed a native input at " + index);
            foreach (string field in new[] { "Before", "Actual" })
                Require(WithoutCatalog(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(original[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Catalog repair changed native gameplay at " + index + ":" + field);
        }
        var oldBefore = original[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var oldResult = BattleActionModel.PlayCard(oldBefore, action);
        Require(oldBefore.Spawn.Train.Context!.SummonCatalog == null && !oldResult.Supported && oldResult.Reason == Reason &&
            before.Spawn.Train.Context!.SummonCatalog is { Units.Count: 1 } && action.CardInstanceId == 9 && action.RoomIndex == 0 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 16 && unit.SpawnerCardId == 9 &&
                unit.AssetKey == "MonsterKinhostVessel" && unit.Health == 15 && unit.Status("hatch")?.Stacks == 8),
            "Missing native egg, original incomplete projection or completed authored child catalog.");
        if (replay.HasValue)
        {
            var second = NativeBranchObservationChecks.Replay(replay.Value, Reason);
            Require(complete.GetProperty("Request").ContentEquals(replay.Value.GetProperty("Request")) && second.Length == rows.Length,
                "Independent catalog process changed its request.");
            for (int index = 0; index < rows.Length; index++)
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent catalog process changed full native state/RNG at " + index);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == native && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Egg deployment changed its full successor/RNG or immutable parent.");
        });
        Console.WriteLine("PLAYER-EGG-CATALOG PASS:20 native inputs/full state/RNG, retained missing-definition refusal, gameplay unchanged by catalog/cardless capture and four immutable branches; independentPair=" + replay.HasValue + "; hatch execution requires subsequent evidence.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
