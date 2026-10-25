using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CombatRemovalTimingChecks
{
    private const string DespawnReason = "Rolling native state differs after decision 27: $.Context.Enchantments.RetainedUnits[3].Unit.DeathState.HasStatisticsListener";
    private const string HealReason = "Native room-stage30 heal-damage removal preserves enemy physical vacancy";

    internal static void DespawnNative(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, DespawnReason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit bag = after.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 15).Unit;
        Require(rows.Length == 27 && before.Spawn.Turn == 3 && last.GetProperty("IsEndTurn").GetBoolean() &&
            bag.AssetKey == "TreasureBagLvl2Gold" && bag.Health == 3 && bag.DeathState is
            { HasFinishedDying: false, IsBeingRemoved: false, IsDespawned: true, IsDestroyed: true,
                HasStatisticsListener: false, StatisticsListenerOnce: false }, "Missing the living gold-bag vanishing boundary.");
        Console.WriteLine("DESPAWN-LISTENERS PASS:27 native inputs/full successors/RNG; living gold bag vanishes without death and clears statistics listeners.");
    }

    private static void HistoricalDespawn(BattleTurnState old, BattleTurnState actual)
    {
        JsonNode projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        JsonNode observed = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        JsonNode Death(JsonNode value) => value["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray()
            .Single(actor => actor!["Unit"]!["Id"]!.GetValue<int>() == 15)!["Unit"]!["DeathState"]!;
        JsonNode death = Death(projected), native = Death(observed);
        foreach (string field in new[] { "HasStatisticsListener", "StatisticsListenerOnce" })
        {
            Require(death[field]!.GetValue<bool>() && !native[field]!.GetValue<bool>(), "Preserve both original retained listeners.");
            death[field] = false;
        }
        Require(JsonNode.DeepEquals(projected, observed), "Historical mismatch extends beyond the gold-bag statistics listeners.");
    }

    internal static void DespawnPair(FixtureValue source, FixtureValue replay)
    {
        DespawnNative(replay);
        var decisions = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var rows = replay.GetProperty("Rows").EnumerateArray().ToArray();
        Require(decisions.Length == 27 && decisions[^1].GetProperty("Difference").GetString() == DespawnReason,
            "Preserve the original natural rolling failure.");
        ComparePrefix(decisions, rows);
        HistoricalDespawn(decisions[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!,
            decisions[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        Console.WriteLine("DESPAWN-LISTENERS-PAIR PASS:original mismatch and repaired independent native forecast preserved.");
    }

    internal static void HealPair(FixtureValue original, FixtureValue replay, FixtureValue observation)
    {
        var rows = NativeBranchObservationChecks.Replay(observation, HealReason);
        Require(rows.Length == 19 && rows[^1].GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == 3 &&
            rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing the19-input natural healing prefix.");
        var decisions = original.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        ComparePrefix(decisions, rows);
        FixtureValue Stage(FixtureValue trace) => trace.GetProperty("Stages").EnumerateArray()
            .Single(stage => stage.GetProperty("Index").GetInt32() == 30);
        FixtureValue first = Stage(original), second = Stage(replay);
        Require(first.GetProperty("Turn").GetInt32() == 3 && first.GetProperty("Kind").GetString() == "Room" &&
            first.GetProperty("Before").GetProperty("RoomIndex").GetInt32() == 0 &&
            first.GetProperty("Difference").ValueKind == FixtureKind.String &&
            second.GetProperty("Difference").ValueKind == FixtureKind.Null, "Missing the original raw room mismatch and its repaired capture.");
        foreach (string field in new[] { "Before", "Actual" })
            Require(Key(first.GetProperty(field).Deserialize<RoomCombatState>()!) == Key(second.GetProperty(field).Deserialize<RoomCombatState>()!),
                "Independent room changed " + field);
        RoomCombatState before = first.GetProperty("Before").Deserialize<RoomCombatState>()!;
        RoomCombatState actual = first.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatResult result = RoomCombatModel.Resolve(before);
        Require(result.Supported && Key(result.State!) == Key(actual), "Repaired raw healing room differs: " +
            (result.Supported ? ModelJson.Difference(Key(result.State!), Key(actual)) : result.UnsupportedReason));
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Room replay mutated its parent.");
        JsonNode historical = JsonNode.Parse(Key(first.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!))!;
        JsonNode observed = JsonNode.Parse(Key(actual))!;
        JsonNode oldSlots = historical["Context"]!["SpawnPoints"]!["Groups"]![0]!["Occupants"]!;
        JsonNode slots = observed["Context"]!["SpawnPoints"]!["Groups"]![0]!["Occupants"]!;
        Require(oldSlots[0]!.GetValue<int>() == 14 && oldSlots[1]!.GetValue<int>() == 0 &&
            slots[0]!.GetValue<int>() == 0 && slots[1]!.GetValue<int>() == 14,
            "Preserve the premature enemy14 centering counterexample.");
        historical["Context"]!["SpawnPoints"] = observed["Context"]!["SpawnPoints"]!.DeepClone();
        Require(JsonNode.DeepEquals(historical, observed), "Raw mismatch extends beyond physical spawn references: " +
            ModelJson.Difference(historical.ToJsonString(), observed.ToJsonString()));
        foreach (string count in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(replay.GetProperty(count).GetInt32() == 0, "Repaired component trace has a nonzero " + count);
        Console.WriteLine("HEAL-COMBAT-REMOVAL-PAIR PASS:19 independent native inputs/full state/RNG; raw room vacancy preserved, original compaction counterexample retained.");
    }

    internal static void DespawnFramePair(FixtureValue original, FixtureValue replay, FixtureValue observation)
    {
        var rows = NativeBranchObservationChecks.Replay(observation,
            "Native room-stage26 despawn listeners remain until frame destruction");
        Require(rows.Length == 27 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing the27-input vanishing prefix.");
        ComparePrefix(original.GetProperty("RollingDecisions").EnumerateArray().ToArray(), rows);
        FixtureValue Stage(FixtureValue trace) => trace.GetProperty("Stages").EnumerateArray()
            .Single(stage => stage.GetProperty("Index").GetInt32() == 26);
        var first = Stage(original); var second = Stage(replay);
        Require(first.GetProperty("Kind").GetString() == "Room" && first.GetProperty("Turn").GetInt32() == 3 &&
            first.GetProperty("Before").GetProperty("RoomIndex").GetInt32() == 2 &&
            first.GetProperty("Difference").ValueKind == FixtureKind.String && second.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Missing the premature room-stage26 listener cleanup counterexample.");
        foreach (string field in new[] { "Before", "Actual" })
            Require(Key(first.GetProperty(field).Deserialize<RoomCombatState>()!) == Key(second.GetProperty(field).Deserialize<RoomCombatState>()!),
                "Independent vanishing room changed " + field);
        var before = first.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = first.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = RoomCombatModel.Resolve(before);
        Require(result.Supported && Key(result.State!) == Key(actual), "Raw vanishing room differs.");
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Raw vanishing room mutated its parent.");
        JsonNode historical = JsonNode.Parse(Key(first.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!))!;
        JsonNode observed = JsonNode.Parse(Key(actual))!;
        JsonNode Death(JsonNode value) => value["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray()
            .Single(actor => actor!["Unit"]!["Id"]!.GetValue<int>() == 15)!["Unit"]!["DeathState"]!;
        JsonNode death = Death(historical), native = Death(observed);
        foreach (string field in new[] { "HasStatisticsListener", "StatisticsListenerOnce" })
        {
            Require(!death[field]!.GetValue<bool>() && native[field]!.GetValue<bool>(), "Preserve the premature room listener cleanup.");
            death[field] = true;
        }
        Require(JsonNode.DeepEquals(historical, observed), "Raw vanishing mismatch extends beyond statistics listeners.");
        var settled = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!
            .Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 15).Unit.DeathState!;
        Require(!settled.HasStatisticsListener && settled.StatisticsListenerOnce == false &&
            settled.IsDespawned == true && settled.IsDestroyed == true, "Settled destruction must clear both listeners.");
        foreach (string count in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(replay.GetProperty(count).GetInt32() == 0, "Raw vanishing component has a nonzero " + count);
        Console.WriteLine("DESPAWN-FRAME-REMOVAL-PAIR PASS:27 independent native inputs/full state/RNG; raw room listeners retained, settled frame listeners cleared; original timing counterexample preserved.");
    }

    private static string Key(RoomCombatState state) => JsonSerializer.Serialize(new
        { state.RoomIndex, state.Deployment, state.Units, Context = BattleTurnChecks.ComparableContext(state.Context), state.Preview,
            state.ActiveCompanionBossIds }, ModelJson.Options);
    private static void ComparePrefix(FixtureValue[] decisions, FixtureValue[] rows)
    {
        Require(decisions.Length >= rows.Length, "Independent prefix exceeds original inputs.");
        for (int i = 0; i < rows.Length; i++)
        {
            foreach (var pair in new[] { ("Root", "Before"), ("Actual", "Actual") })
                Require(NativeBranchObservationChecks.Key(decisions[i].GetProperty(pair.Item1).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(rows[i].GetProperty(pair.Item2).Deserialize<BattleTurnState>()!),
                    "Independent prefix changed " + pair.Item1 + " at " + i);
            Require(decisions[i].GetProperty("Action").ContentEquals(rows[i].GetProperty("Action")) &&
                decisions[i].GetProperty("IsEndTurn").ContentEquals(rows[i].GetProperty("IsEndTurn")), "Independent input changed.");
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
