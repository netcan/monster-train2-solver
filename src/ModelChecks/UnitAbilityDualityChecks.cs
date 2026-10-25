using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitAbilityDualityChecks
{
    internal const string Reason = "Native unit ability AddStatus honors caster Duality and IgnoreDualism.";

    internal static void Counterexample(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue fixture = document.RootElement;
        Require(fixture.GetProperty("NativeWon").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 && fixture.GetProperty("Pending").GetInt32() == 0,
            "Preserve the original incomplete native ability counterexample.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 31 && fixture.GetProperty("Stages").GetArrayLength() == 32 &&
            fixture.GetProperty("TrainPhases").GetArrayLength() == 8, "Wrong native ability prefix.");
        BattleTurnState before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState old = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(before.Spawn.Turn == 4 && action.ActivatorUnitId == 17 && action.CardInstanceId == 38 &&
            Unit(before, 17).AssetKey == "MonsterSneezyWhelp" && Unit(before, 17).Status("duality")?.Stacks == 1 &&
            Unit(before, 20).Status("pyregel") == null && Unit(old, 20).Status("pyregel")?.Stacks == 12 &&
            Unit(actual, 20).Status("pyregel")?.Stacks == 24 && Unit(actual, 20).Health == 295,
            "Missing the natural doubled unit-ability status application.");
        JsonNode repaired = JsonNode.Parse(Key(old))!;
        int corrected = CorrectHistoricalStatus(repaired);
        Require(corrected == 4 && JsonNode.DeepEquals(repaired, JsonNode.Parse(Key(actual))),
            "Historical discrepancy extends beyond the live/shared-world Pyregel stack counts; corrected=" + corrected +
            "; remaining=" + ModelJson.Difference(repaired.ToJsonString(), Key(actual)));
        RollingAdviceChecks.ObservedSuccessors(fixture);
        TrainCombatChecks.Native(fixture);
        VerifyBoundary(before, action, actual);
        Console.WriteLine("UNIT-ABILITY-DUALITY PASS: original 12-versus-24 counterexample preserved;31 actual native successors,8 train phases/full state/RNG; eight immutable branches; IgnoreDualism control; fresh native calibration required separately.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        var first = NativeBranchObservationChecks.Replay(source, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        Require(first.Length == 31 && second.Length == 31 &&
            source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Changed independent native ability request.");
        for (int index = 0; index < first.Length; index++)
            foreach (string field in new[] { "Before", "Actual" })
                Require(Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent native ability state differs at " + index);
        VerifyBoundary(first[^1].GetProperty("Before").Deserialize<BattleTurnState>()!,
            first[^1].GetProperty("Action").Deserialize<PlayCardAction>()!,
            first[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        Console.WriteLine("UNIT-ABILITY-DUALITY-PAIR PASS:31 natural paid/EndTurn inputs; independent complete state/forecast/RNG; doubled Pyregel and IgnoreDualism control; no terminal victory claimed.");
    }

    private static void VerifyBoundary(BattleTurnState before, PlayCardAction action, BattleTurnState actual)
    {
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            BattleTurnState result = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, false)], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged && Key(result) == Key(actual), "Concurrent ability status differs from native.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Ability status application mutated its parent.");
        var control = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        int changed = IgnoreDualism(control);
        Require(changed > 0, "No status effect available for IgnoreDualism control.");
        BattleTurnState ignored = BattlePlannerChecks.Replay(control.Deserialize<BattleTurnState>(ModelJson.Options)!,
            [new BattleSearchStep(action, false)], out RoomOutcome ignoredOutcome);
        Require(ignoredOutcome == RoomOutcome.Exchanged && Unit(ignored, 20).Status("pyregel")?.Stacks == 12 &&
            Unit(ignored, 20).Health == Unit(actual, 20).Health && ignored.Spawn.Train.Context!.BattleRng.Equals(actual.Spawn.Train.Context!.BattleRng),
            "IgnoreDualism must suppress only the ability status doubling.");
    }

    private static int IgnoreDualism(JsonNode node)
    {
        int count = 0;
        if (node is JsonObject obj)
        {
            if (obj["Type"] is JsonValue type && type.TryGetValue<string>(out string? name) && name == "AddStatus")
                { obj["IgnoreDualism"] = true; count++; }
            foreach (var property in obj.ToArray()) if (property.Value != null) count += IgnoreDualism(property.Value);
        }
        else if (node is JsonArray array) foreach (var child in array) if (child != null) count += IgnoreDualism(child);
        return count;
    }

    private static int CorrectHistoricalStatus(JsonNode node)
    {
        int count = 0;
        if (node is JsonObject obj)
        {
            if (obj["Id"] is JsonValue id && id.TryGetValue<int>(out int value) && value == 20 && obj["AssetKey"] != null)
                foreach (string collection in new[] { "Statuses", "StatusRegistry" })
                    if (obj[collection] is JsonArray statuses)
                        foreach (JsonNode? status in statuses)
                            if (status?["Id"]?.GetValue<string>() == "pyregel" && status["Stacks"]?.GetValue<int>() == 12)
                                { status["Stacks"] = 24; count++; }
            foreach (var property in obj.ToArray()) if (property.Value != null) count += CorrectHistoricalStatus(property.Value);
        }
        else if (node is JsonArray array) foreach (var child in array) if (child != null) count += CorrectHistoricalStatus(child);
        return count;
    }

    private static CombatUnit Unit(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static string Key(BattleTurnState state) => NativeBranchObservationChecks.Key(state);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
