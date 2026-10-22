using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class TrueFinalBossAlliedChecks
{
    internal static void Native(FixtureValue root, bool multi = false)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Allied native acceptance requires all four failure counters to be zero.");
        Require(route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && root.GetProperty("Policy").GetString() == (multi ? "units-and-junk" : "steward-once") &&
            root.GetProperty("Actions").GetArrayLength() == (multi ? 3 : 1) && root.GetProperty("Turns").GetArrayLength() == 6 &&
            !root.GetProperty("NativeWon").GetBoolean(), "Missing original allied native route.");
        BattleTurnState deployment = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatUnit[] allies = deployment.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => !unit.IsPyre).ToArray();
        CombatUnit ally = allies.First(unit => unit.AssetKey == "TrainStewardBig");
        Require(allies.Length == (multi ? 3 : 1) && allies.All(TrueFinalBossCombatModel.SupportsAlly), "Missing calibrated original allies.");
        Require(TrueFinalBossCombatModel.SupportsAlly(ally) && ally.Health == 25 && ally.Statuses.Count == 0 &&
            deployment.Spawn.QueuedTrueFinalBoss != null, "Missing unchanged Steward before five-actor birth.");
        FixtureValue corruption = root.GetProperty("UnitPostCombats").EnumerateArray().First(row =>
            row.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit =>
                unit.GetProperty("AssetKey").GetString() == "Boss_Level9_Dominion_V2"));
        RoomCombatState changed = corruption.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        Require(changed.Context!.Enchantments!.Rooms.Single(room => room.RoomIndex == 0).Units
            .Single(unit => unit.Id == ally.Id).Status("corruption")?.Stacks == 5,
            "Dominion must corrupt the remote Steward without moving it out of floor zero.");
        if (multi)
        {
            LifeLinkContext preview = root.GetProperty("TrueFinalBossPreviewRooms").EnumerateArray().First(row =>
                row.GetProperty("Turn").GetInt32() == 1 && row.GetProperty("Before").GetProperty("RoomIndex").GetInt32() == 1)
                .GetProperty("Actual").GetProperty("Context").GetProperty("LifeLinks").Deserialize<LifeLinkContext>()!;
            int[] outerHealth = preview.Actors.Where(actor => actor.OuterBoss).Select(actor => actor.Health).ToArray();
            Require(preview.CalculatingPreview && preview.SharedPreviewHpChange == 8 && outerHealth.SequenceEqual(new[] { 3500, 3492, 3500 }),
                "Native outer preview must reduce only the middle body and accumulate eight damage.");
            TrainCombatState live = root.GetProperty("TrainPhases").EnumerateArray().First(row => row.GetProperty("Kind").GetString() == "Combat" &&
                !row.GetProperty("Before").GetProperty("Rooms")[0].GetProperty("Deployment").GetBoolean()).GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(live.Context!.LifeLinks!.Actors.Where(actor => actor.OuterBoss).All(actor => actor.Health == 3492),
                "Actual outer damage must synchronize all three linked bodies.");
        }
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
        string parent = JsonSerializer.Serialize(start, ModelJson.Options);
        Parallel.For(0, 16, _ =>
        {
            BattleTurnState current = start;
            foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
            {
                Require(entry.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == 0,
                    "This independent route requires all paid plays before the first EndTurn.");
                BattleActionResult played = BattleActionModel.PlayCard(current, entry.GetProperty("Action").Deserialize<PlayCardAction>()!);
                Require(played.Supported, "Independent paid allied play failed: " + played.Reason);
                current = played.State!;
            }
            BattleTurnChecks.RunIndependentChain(current, root.GetProperty("Turns"), compareRoomEnabled: true);
            Require(JsonSerializer.Serialize(start, ModelJson.Options) == parent, "Paid full chain mutated its original decision.");
        });
        Console.WriteLine($"TRUE-FINAL-BOSS-ALLIED PASS: {(multi ? "three" : "one")} original allied plays, five-actor birth, native train/preview phases, six EndTurns and sixteen immutable paid full chains; relentless and victory remain guarded.");
    }
}
