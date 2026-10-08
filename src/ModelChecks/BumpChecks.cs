using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BumpChecks
{
    private sealed record CallbackSnapshot(int ActorId, string Kind, int ParamInt, int ParamInt2, string? ParamString,
        int TriggerCount, int DyingId, int OverrideTargetId, int LastSpawnedOverrideUnitId);
    internal static readonly string[] Labels = ["player-up", "player-down", "player-partial-up-ignores-range",
        "player-pyre-blocked", "player-zero", "player-clamped-down", "enemy-up", "enemy-down", "enemy-rooted",
        "enemy-immobile-before-rooted", "enemy-loop-up-to-bottom", "enemy-room-multiple-targets",
        "enemy-to-top-before-merge", "player-cross-room-horde-merge", "enemy-cross-room-horde-merge",
        "enemy-into-pyre", "enemy-full-room-blocked", "enemy-partial-full-room"];

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("BumpOperations", out var values) || values.GetArrayLength() == 0) return;
        var records = values.EnumerateArray().ToArray();
        Require(records.Select(record => record.GetProperty("Label").GetString()).SequenceEqual(Labels), "Missing native Bump boundary cases.");
        var setup = fixture.GetProperty("Actions").EnumerateArray().Where(action => action.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(setup.Length == records.Length, "Authored paid Bump action inventory differs from the effect observations.");
        for (int index = 0; index < setup.Length; index++)
        {
            var before = setup[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = setup[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            Require(action.CardInstanceId == records[index].GetProperty("SourceCardId").GetInt32() &&
                rule.Effects.Count == 1 && rule.Effects[0].Type == "Bump" &&
                rule.Effects[0].Value == records[index].GetProperty("Amount").GetInt32(), "Authored action and native Bump effect differ.");
        }
        foreach (var record in records) Verify(record);
        Parallel.For(0, 32, _ => { foreach (var record in records) Verify(record); });
        var group = records.Single(record => record.GetProperty("Label").GetString() == "enemy-room-multiple-targets");
        Require(group.GetProperty("Targets").GetArrayLength() == 2, "Bump never exercised simultaneous targets.");
        var destinations = new Dictionary<string, int> { ["player-up"] = 1, ["player-down"] = 0,
            ["player-partial-up-ignores-range"] = 2, ["player-pyre-blocked"] = 2, ["player-zero"] = 2,
            ["player-clamped-down"] = 0, ["enemy-up"] = 1, ["enemy-down"] = 0, ["enemy-rooted"] = 0,
            ["enemy-immobile-before-rooted"] = 0, ["enemy-loop-up-to-bottom"] = 0, ["enemy-room-multiple-targets"] = 1,
            ["enemy-to-top-before-merge"] = 2, ["enemy-into-pyre"] = 3, ["enemy-full-room-blocked"] = 1,
            ["enemy-partial-full-room"] = 1 };
        foreach (var record in records.Where(record => destinations.ContainsKey(record.GetProperty("Label").GetString()!)))
        {
            string label = record.GetProperty("Label").GetString()!;
            var after = record.GetProperty("After").Deserialize<TrainCombatState>()!;
            foreach (int id in record.GetProperty("Targets").Deserialize<int[]>()!)
                Require(after.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == id).Current!.RoomIndex == destinations[label],
                    label + " did not reach the intended native boundary");
        }
        var ranged = records.Single(record => record.GetProperty("Label").GetString() == "player-partial-up-ignores-range");
        Compare(ranged.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.BattleRng,
            ranged.GetProperty("After").Deserialize<TrainCombatState>()!.Context!.BattleRng, "Native Bump unexpectedly sampled its integer range");
        var rooted = records.Single(record => record.GetProperty("Label").GetString() == "enemy-rooted");
        var immobile = records.Single(record => record.GetProperty("Label").GetString() == "enemy-immobile-before-rooted");
        Require(rooted.GetProperty("TargetsAfter").Deserialize<CombatUnit[]>()!.Single().Status("rooted")!.Stacks == 1 &&
            immobile.GetProperty("TargetsAfter").Deserialize<CombatUnit[]>()!.Single().Status("rooted")!.Stacks == 1,
            "Native rooting or immobility priority was not exercised");
        foreach (var record in records.Where(record => record.GetProperty("Label").GetString()!.Contains("full-room")))
            Require(record.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.SpawnPoints!.Group(2, CombatTeam.Enemy)!
                .Occupants.Take(record.GetProperty("Before").Deserialize<TrainCombatState>()!.Context!.SpawnPoints!.Group(2, CombatTeam.Enemy)!.GroupCount)
                .All(id => id > 0), "Native full-room case had an empty physical destination");
        foreach (var record in records.Where(record => record.GetProperty("Label").GetString()!.EndsWith("horde-merge")))
        {
            var before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = record.GetProperty("After").Deserialize<TrainCombatState>()!;
            int id = record.GetProperty("Targets")[0].GetInt32();
            var removed = record.GetProperty("TargetsAfter").Deserialize<CombatUnit[]>()!.Single();
            Require(before.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex !=
                after.Rooms.Single(room => room.Units.Any(unit => unit.Team == removed.Team && unit.Status("horde") != null)).RoomIndex &&
                removed.Health > 0 && removed.DeathState!.IsDespawned == true && removed.DeathState.IsDestroyed == true &&
                !removed.DeathState.HasFinishedDying, "Bump merge did not remove a positive-HP actor across different rooms.");
            Require(!record.GetProperty("Dispatched").EnumerateArray().Any(callback =>
                callback.GetProperty("Kind").GetString() is "OnSpawn" or "OnUnscaledSpawn" or "OnSpawnNotFromCard" or "CardMonsterPlayed" or "OnDeath"),
                "Paid Bump merge fired birth, Rally or death callbacks.");
        }
        Console.WriteLine("NATIVE-BUMP-CHECKS PASS: 18 real paid-card effect states, exact dispatch/retained targets, signed/clamped/fixed-range movement, rooting/immobility, looping, simultaneous targets, both-team cross-room Horde removal, Pyre/full/partially blocked rooms and 32 branches.");
    }

    private static void Verify(FixtureValue record)
    {
        string label = record.GetProperty("Label").GetString()!;
        var before = record.GetProperty("Before").Deserialize<TrainCombatState>()!;
        string parent = JsonSerializer.Serialize(before);
        var result = BumpModel.Apply(before, record.GetProperty("Targets").Deserialize<int[]>()!, record.GetProperty("Amount").GetInt32(),
            record.GetProperty("Rooms").Deserialize<RoomPlayRule[]>()!, record.GetProperty("SourceCardId").GetInt32());
        Require(result.Supported, label + " unsupported: " + result.UnsupportedReason);
        Compare(result.State, record.GetProperty("After").Deserialize<TrainCombatState>(), label + " effect return");
        var targets = record.GetProperty("Targets").Deserialize<int[]>()!;
        var retained = targets.Select(id => result.State!.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id)
            ?? result.RetainedUnits.FirstOrDefault(unit => unit.Id == id)).ToArray();
        Compare(retained, record.GetProperty("TargetsAfter").Deserialize<CombatUnit[]>(), label + " retained targets");
        var callbacks = result.Dispatched.Select(callback => new CallbackSnapshot(callback.Unit.Id, callback.Kind,
            callback.ParamInt, callback.ParamInt2, callback.ParamString, callback.TriggerCount,
            callback.DyingCharacter?.Id ?? 0, callback.OverrideTarget?.Id ?? 0, callback.LastSpawnedOverrideUnitId)).ToArray();
        Compare(callbacks, record.GetProperty("Dispatched").Deserialize<CallbackSnapshot[]>()!, label + " dispatch order");
        Require(result.PendingCallbacks.Count == 0 && record.GetProperty("QueueAfter").GetInt32() == 0, label + " left pending callbacks");
        Require(JsonSerializer.Serialize(before) == parent, label + " mutated its parent");
    }
    private static void Compare<T>(T predicted, T actual, string label)
    {
        string? error = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(actual));
        Require(error == null, label + ": " + error);
    }
    private static void Require(bool okay, string message) { if (!okay) throw new Exception(message); }
}
