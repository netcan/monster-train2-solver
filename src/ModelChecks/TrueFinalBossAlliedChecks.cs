using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class TrueFinalBossAlliedChecks
{
    internal static void Native(FixtureValue root)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Single-Steward native acceptance requires all four failure counters to be zero.");
        Require(route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && root.GetProperty("Policy").GetString() == "steward-once" &&
            root.GetProperty("Actions").GetArrayLength() == 1 && root.GetProperty("Turns").GetArrayLength() == 6 &&
            !root.GetProperty("NativeWon").GetBoolean(), "Missing original single-Steward native route.");
        BattleTurnState deployment = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatUnit ally = deployment.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre);
        Require(TrueFinalBossCombatModel.SupportsAlly(ally) && ally.Health == 25 && ally.Statuses.Count == 0 &&
            deployment.Spawn.QueuedTrueFinalBoss != null, "Missing unchanged Steward before five-actor birth.");
        FixtureValue corruption = root.GetProperty("UnitPostCombats").EnumerateArray().First(row =>
            row.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit =>
                unit.GetProperty("AssetKey").GetString() == "Boss_Level9_Dominion_V2"));
        RoomCombatState changed = corruption.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        Require(changed.Context!.Enchantments!.Rooms.Single(room => room.RoomIndex == 0).Units
            .Single(unit => unit.Id == ally.Id).Status("corruption")?.Stacks == 5,
            "Dominion must corrupt the remote Steward without moving it out of floor zero.");
        BattleActionChecks.Native(root);
        foreach (FixtureValue stage in root.GetProperty("Stages").EnumerateArray().Where(row => row.GetProperty("Kind").GetString() == "Room"))
        {
            RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit? pyre = stage.GetProperty("ExternalPyre").Deserialize<CombatUnit>();
            var targets = stage.GetProperty("ExternalTargets").EnumerateArray().Select(target =>
                (target.GetProperty("RoomIndex").GetInt32(), target.GetProperty("Unit").Deserialize<CombatUnit>()!)).ToArray();
            RoomCombatResult result = RoomCombatModel.Resolve(before, externalPyre: pyre,
                externalPyreRoomIndex: stage.GetProperty("ExternalPyreRoomIndex").GetInt32(), externalTargets: targets);
            Require(result.Supported, "Steward room unsupported: " + result.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State!.Context, ModelJson.Options),
                JsonSerializer.Serialize(stage.GetProperty("Actual").GetProperty("Context").Deserialize<CombatContext>(), ModelJson.Options));
            Require(difference == null, "Steward room " + before.RoomIndex + " context differs: " + difference);
        }
        TrueFinalBossTrainChecks.Native(root);
        TrueFinalBossPreviewChecks.Native(root);
        BattleTurnChecks.Native(root);
        FixtureValue play = root.GetProperty("Actions")[0];
        BattleTurnState start = play.GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction action = play.GetProperty("Action").Deserialize<PlayCardAction>()!;
        string parent = JsonSerializer.Serialize(start, ModelJson.Options);
        Parallel.For(0, 16, _ =>
        {
            BattleActionResult played = BattleActionModel.PlayCard(start, action);
            Require(played.Supported, "Independent paid Steward play failed: " + played.Reason);
            BattleTurnChecks.RunIndependentChain(played.State!, root.GetProperty("Turns"), compareRoomEnabled: true);
            Require(JsonSerializer.Serialize(start, ModelJson.Options) == parent, "Paid full chain mutated its original decision.");
        });
        Console.WriteLine("TRUE-FINAL-BOSS-ALLIED PASS: one original Steward play, five-actor birth with ally, native train/preview phases, six EndTurns and sixteen immutable paid full chains. Other allies, outer preview damage, relentless and victory remain guarded.");
    }
}
