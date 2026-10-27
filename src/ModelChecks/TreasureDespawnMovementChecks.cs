using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TreasureDespawnMovementChecks
{
    internal static void Native(string sourcePath, string? replayPath = null)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        var fixture = source.RootElement;
        Require(fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("RollingDecisions").GetArrayLength() == 28,
            "Missing original28-input raw-room-only mismatch.");
        var stage = fixture.GetProperty("Stages").EnumerateArray().Single(row => row.GetProperty("Index").GetInt32() == 20);
        var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        var historical = stage.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!;
        var treasure = before.Units.Single(unit => unit.Id == 10);
        Require(before.RoomIndex == 1 && !before.Preview && stage.GetProperty("Kind").GetString() == "Room" &&
            treasure.AssetKey == "TreasureBagLvl1Gold" && treasure.Health == 1 &&
            treasure.DeathState is { IsDespawned: null, IsDestroyed: null } &&
            historical.Context!.Enchantments!.Movement.Select(rule => rule.UnitId).SequenceEqual(new[] { 8, 10 }) &&
            actual.Context!.Enchantments!.Movement.Select(rule => rule.UnitId).SequenceEqual(new[] { 8 }) &&
            actual.Units.Select(unit => unit.Id).SequenceEqual(new[] { 3, 11 }),
            "Original living treasure removal or unaffected other-floor movement changed.");
        var normalized = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
        var movement = normalized["Context"]!["Enchantments"]!["Movement"]!.AsArray();
        movement.Remove(movement.Single(item => item!["UnitId"]!.GetValue<int>() == 10));
        Require(Key(normalized.Deserialize<RoomCombatState>(ModelJson.Options)!) == Key(actual),
            "Historical difference extends beyond the removed treasure movement rule.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = RoomCombatModel.Resolve(before);
            Require(result.Supported && result.Events.Single(item => item.Kind == "Despawn").Target == 10 &&
                Key(result.State!) == Key(actual), "Treasure despawn differs from complete native room state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Treasure removal mutated its parent.");
        // The same living actor must retain its rule while its native counter has not expired.
        var waiting = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        var waitingTreasure = waiting["Units"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == 10)!;
        var effect = waitingTreasure["Triggers"]!.AsArray().SelectMany(item => item!["Effects"]!.AsArray())
            .Single(item => item!["Type"]!.GetValue<string>() == "CardEffectDespawnCharacter")!;
        effect["Counter"] = 2;
        var retained = RoomCombatModel.Resolve(waiting.Deserialize<RoomCombatState>(ModelJson.Options)!);
        Require(retained.Supported && retained.State!.Units.Any(unit => unit.Id == 10 && unit.Health == 1) &&
            retained.State.Context!.Enchantments!.Movement.Select(rule => rule.UnitId).SequenceEqual(new[] { 8, 10 }) &&
            retained.Events.All(item => item.Kind != "Despawn"), "An unexpired living treasure lost its movement.");
        // Explicit lifecycle captures and old nullable captures must agree on physical removal.
        var captured = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        var death = captured["Units"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == 10)!["DeathState"]!;
        death["IsDespawned"] = false; death["IsDestroyed"] = false;
        var explicitLifecycle = RoomCombatModel.Resolve(captured.Deserialize<RoomCombatState>(ModelJson.Options)!);
        Require(explicitLifecycle.Supported && explicitLifecycle.State!.Context!.Enchantments!.Movement
            .Select(rule => rule.UnitId).SequenceEqual(new[] { 8 }), "Explicit lifecycle removal retained movement.");
        RoomCombatChecks.NativeRoomOnly(sourcePath);
        TrainCombatChecks.Native(fixture);
        RollingAdviceChecks.ObservedSuccessors(fixture);
        if (replayPath != null)
        {
            CompleteNativeBattleChecks.Native(replayPath);
            using var replay = ModelJson.ReadFixture(replayPath);
            Require(fixture.GetProperty("NativeRunInput").ContentEquals(replay.RootElement.GetProperty("NativeRunInput")),
                "Independent native observation changed the campaign input.");
            var removal = replay.RootElement.GetProperty("Stages").EnumerateArray().Single(row =>
                row.GetProperty("Kind").GetString() == "Room" &&
                row.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Any(unit => unit.AssetKey == "TreasureBagLvl1Gold") &&
                row.GetProperty("Predicted").GetProperty("Events").EnumerateArray().Any(item =>
                    item.GetProperty("Kind").GetString() == "Despawn"));
            var fixedRoom = removal.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var fixedPrediction = removal.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!;
            int retiredId = removal.GetProperty("Before").Deserialize<RoomCombatState>()!.Units
                .Single(unit => unit.AssetKey == "TreasureBagLvl1Gold").Id;
            Require(Key(fixedRoom) == Key(fixedPrediction) &&
                fixedRoom.Context!.Enchantments!.Movement.All(rule => rule.UnitId != retiredId),
                "Independent native capture retained the vanished treasure movement.");
        }
        Console.WriteLine("TREASURE-DESPAWN-MOVEMENT PASS: original raw-room witness/full room state/RNG, all28 native successors, four immutable branches, living-counter/explicit-lifecycle controls; independentNative=" + (replayPath != null));
    }

    private static string Key(RoomCombatState room) => JsonSerializer.Serialize(room.WithContext(
        BattleTurnChecks.ComparableContext(room.Context)), ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
