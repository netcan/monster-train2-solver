using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChampionAuraPreviewRetirementChecks
{
    internal static void Native(string sourcePath, string? replayPath = null)
    {
        using var document = ModelJson.ReadFixture(sourcePath);
        var fixture = document.RootElement;
        Require(fixture.GetProperty("Mismatches").GetInt32() == 3 && fixture.GetProperty("NativeWon").GetBoolean(),
            "Preserve original three raw retirement differences in the actual native win.");
        var stage = fixture.GetProperty("Stages").EnumerateArray().Single(row => row.GetProperty("Index").GetInt32() == 14);
        var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var historical = stage.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!;
        var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        var world = historical.Context!.Enchantments!;
        var actor = world.RetainedUnits.Single(actor => actor.Unit.Id == 27);
        var removed = actual.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 27);
        Require(before.RoomIndex == 0 && stage.GetProperty("Turn").GetInt32() == 1 && stage.GetProperty("Kind").GetString() == "Room" &&
            actor is { Preview: true, RoomIndex: 2, Unit: { AssetKey: "MonsterFunguy", Health: 12, MaxHealth: 12,
                Modifiers.Upgrades.Count: 4, DeathState: { IsDestroyed: false, HasStatisticsListener: true } } } &&
            removed is { Preview: true, RoomIndex: -1, Unit: { Health: 12, MaxHealth: 12, Modifiers.Upgrades.Count: 0,
                DeathState: { IsDestroyed: true, HasStatisticsListener: false, StatisticsListenerOnce: false } } } &&
            historical.Context.SpawnPoints!.Units.Single(point => point.UnitId == 27).Current?.PreviewCopyId == 126 &&
            actual.Context.SpawnPoints!.Units.Single(point => point.UnitId == 27).Current == null,
            "Missing original two-stack champion aura preview retirement witness.");
        var normalized = historical.Context.WithSpawnPoints(historical.Context.SpawnPoints.ClearDestroyedPreviewBirths(new HashSet<int> { 27 }))
            .WithEnchantments(new EnchantmentWorld(world.Rooms, world.Movement, world.EnemySlotsPerRoom,
                world.RetainedUnits.Select(item => item.Unit.Id == 27 ? PreviewBirthModel.CancelPlayer(item) : item).ToArray(),
                world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
        Require(Key(historical.WithContext(normalized)) == Key(actual), "Historical difference extends beyond copied group/preview27 destruction.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = RoomCombatModel.Resolve(before);
            Require(result.Supported && Key(result.State!) == Key(actual), "Remote champion preview retirement differs from full native room state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Preview retirement mutated its parent.");
        foreach (string control in new[] { "Floor", "Owner", "Aura", "Trigger", "Horde", "Upgrade", "Copy" })
        {
            var node = JsonSerializer.SerializeToNode(before.Context, ModelJson.Options)!;
            var child = node["Enchantments"]!["RetainedUnits"]!.AsArray().Single(item => item!["Unit"]!["Id"]!.GetValue<int>() == 27)!["Unit"]!;
            var owner = node["Enchantments"]!["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == 2)!;
            if (control == "Owner") owner["AssetKey"] = "uncalibrated-champion";
            if (control == "Aura") owner["Modifiers"]!["TroopAura"]!["Upgrade"]!["DataId"] = "uncalibrated-aura";
            if (control == "Trigger") owner["Triggers"]![1]!["Effects"]![0]!["Action"]!["Statuses"]![0]!["Stacks"] = 4;
            if (control == "Horde") child["Statuses"]!.AsArray().Single(status => status!["Id"]!.GetValue<string>() == "horde")!["Stacks"] = 3;
            if (control == "Upgrade") child["Modifiers"]!["Upgrades"]!.AsArray().RemoveAt(0);
            if (control == "Copy") node["SpawnPoints"]!["Units"]!.AsArray().Single(point => point!["UnitId"]!.GetValue<int>() == 27)!["Current"]!["PreviewCopyId"] = null;
            var context = node.Deserialize<CombatContext>(ModelJson.Options)!;
            Require(ReferenceEquals(context, PreviewBirthModel.CompleteRemoteCombatBirths(context, control == "Floor" ? 2 : 0)),
                "Unknown preview retirement signature lost guard: " + control);
        }
        RoomCombatChecks.NativeRoomOnly(sourcePath);
        TrainCombatChecks.Native(fixture);
        RollingAdviceChecks.ObservedSuccessors(fixture);
        if (replayPath != null)
        {
            CompleteNativeBattleChecks.Native(replayPath);
            using var replay = ModelJson.ReadFixture(replayPath);
            Require(fixture.GetProperty("NativeRunInput").ContentEquals(replay.RootElement.GetProperty("NativeRunInput")),
                "Independent native acceptance changed the original campaign input.");
        }
        Console.WriteLine("CHAMPION-AURA-PREVIEW-RETIREMENT PASS:74 raw room/17 train phases,43 native successors/full forecasts/RNG; copied group126 and preview27; four immutable branches/seven guards; independentNative=" + (replayPath != null));
    }
    private static string Key(RoomCombatState room) => JsonSerializer.Serialize(room.WithContext(BattleTurnChecks.ComparableContext(room.Context)), ModelJson.Options);
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
