using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossSpawnChecks
{
    internal static void Native(FixtureValue root)
    {
        TrueFinalBossChecks.Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "DoTrueFinalBossSpawning" && root.GetProperty("NativeReturned").GetBoolean(),
            "Missing completed native five-actor creation boundary.");
        FixtureValue route = root.GetProperty("Route");
        TrueFinalBossChecks.Require(route.GetProperty("Kind").GetString() == "NativeCheatJumpToTFB" &&
            route.GetProperty("UnlockOverrideAfter").GetBoolean() && route.GetProperty("DistanceAfter").GetInt32() == 8 &&
            route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80,
            "Five-actor spawn must use the disclosed original native route without asset, deck or health substitution.");
        TrueFinalBossSpawnState before = root.GetProperty("Before").Deserialize<TrueFinalBossSpawnState>()!;
        TrueFinalBossSpawnState actual = root.GetProperty("Actual").Deserialize<TrueFinalBossSpawnState>()!;
        if (route.TryGetProperty("CovenantControl", out FixtureValue covenant) && covenant.ValueKind != FixtureKind.Null)
            CovenantMetadata(covenant, before);
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        TrueFinalBossSpawnResult result = TrueFinalBossSpawningModel.Apply(before);
        TrueFinalBossChecks.Require(result.Supported, "True-final-boss spawn unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
            JsonSerializer.Serialize(actual, ModelJson.Options));
        TrueFinalBossChecks.Require(difference == null, "True-final-boss spawn differs: " + difference);
        string expected = JsonSerializer.Serialize(actual, ModelJson.Options);
        Parallel.For(0, 64, _ =>
        {
            TrueFinalBossSpawnResult branch = TrueFinalBossSpawningModel.Apply(before);
            TrueFinalBossChecks.Require(branch.Supported && JsonSerializer.Serialize(branch.State, ModelJson.Options) == expected,
                "Concurrent five-actor spawn diverged from the same immutable native root.");
        });
        TrueFinalBossChecks.Require(before.QueuedBosses.SequenceEqual(new[] { 0, 2, 1 }) && actual.QueuedBosses.Count == 0 &&
            actual.OuterBossId == 6 && actual.RoomBossIds.SequenceEqual(new[] { 4, 5, 6, 0 }) &&
            before.Battle.Spawn.NextUnitId == 2 && actual.Battle.Spawn.NextUnitId == 7 &&
            !TrueFinalBossSpawningModel.Apply(actual).Supported,
            "Native queue must be cleared exactly once with fixed original creation identities and all three Entropy anchors.");
        var malformed = new TrueFinalBossSpawnState(before.Battle, before.Definitions, new[] { 0, 0, 2 }, 0,
            before.RoomBossIds, before.ExternalInteractions);
        TrueFinalBossChecks.Require(!TrueFinalBossSpawningModel.Apply(malformed).Supported &&
            !OuterBossDefinitions.Supports(TrueFinalBossChecks.Assets[2]) &&
            !TrainCombatModel.ResolveCombat(actual.Battle.Spawn.Train).Supported,
            "Malformed queues and uncalibrated full true-final-boss combat must remain guarded.");
        FixtureValue[] rngBefore = root.GetProperty("RngBefore").EnumerateArray().ToArray();
        FixtureValue[] rngAfter = root.GetProperty("RngAfter").EnumerateArray().ToArray();
        TrueFinalBossChecks.Require(rngBefore.Length == 16 && rngAfter.Length == 16, "Missing all sixteen native random streams.");
        foreach (FixtureValue stream in rngBefore)
        {
            string name = stream.GetProperty("Id").GetString()!;
            FixtureValue after = rngAfter.Single(value => value.GetProperty("Id").GetString() == name);
            if (name is not ("Battle" or "NonDeterministic" or "Chatter"))
                TrueFinalBossChecks.Require(stream.ContentEquals(after), "Birth advanced an unrelated native random stream: " + name);
        }
        TrueFinalBossChecks.Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "True-final-boss spawn mutated its input.");
        Console.WriteLine("TRUE-FINAL-BOSS-SPAWN PASS: five original instances, physical anchors/rooms, Entropy scourge birth, links and entire battle/context/RNG equality; 64 concurrent immutable branches; full combat guarded.");
    }

    internal static void CovenantMetadata(FixtureValue control, TrueFinalBossSpawnState before)
    {
        CombatRelicState owner = before.Battle.Spawn.Train.Context!.Relics!.Single(relic => relic.IsCovenant == true);
        TrueFinalBossChecks.Require(control.GetProperty("Kind").GetString() == "ControlledOriginalCovenantBeforeTFBBirth" &&
            control.GetProperty("AssetKey").GetString() == owner.AssetKey && control.GetProperty("Id").GetString() == owner.DataId &&
            !control.GetProperty("StartOfRunEffectsApplied").GetBoolean() &&
            control.GetProperty("ActiveBefore").GetArrayLength() == 0 &&
            control.GetProperty("ActiveAfter").EnumerateArray().Select(value => value.GetString()).SequenceEqual(new[] { owner.DataId }) &&
            control.GetProperty("EffectTypes").EnumerateArray().Select(value => value.GetString()).SequenceEqual(owner.EffectTypes) &&
            control.GetProperty("RngBefore").ContentEquals(control.GetProperty("RngAfter")),
            "Covenant birth must disclose a single unchanged original definition, controlled activation and unchanged RNG.");
        FixtureValue[] queries = control.GetProperty("StatQueries").EnumerateArray().ToArray();
        TrueFinalBossChecks.Require(queries.Length >= 5 && queries.Select(query => query.GetProperty("AssetKey").GetString())
            .ToHashSet().SetEquals(TrueFinalBossChecks.Assets), "Missing native stat previews for the five original bosses.");
        foreach (FixtureValue query in queries)
        {
            EnemyDefinition definition = before.Definitions.Single(item => item.Unit.AssetKey == query.GetProperty("AssetKey").GetString());
            EnemyBirthStatAdjustment stats = definition.BirthStats!;
            TrueFinalBossChecks.Require(stats != null && stats.CovenantIds.SequenceEqual(new[] { owner.DataId }) &&
                stats.Attack == query.GetProperty("Attack").GetInt32() && stats.Health == query.GetProperty("Health").GetInt32() &&
                query.GetProperty("FrameBefore").GetInt32() == query.GetProperty("FrameAfter").GetInt32() &&
                query.GetProperty("RngBefore").ContentEquals(query.GetProperty("RngAfter")),
                "Birth stats must come from a pure native preview with the exact active Covenant origin.");
        }
        TrueFinalBossChecks.Require(owner.AssetKey == "Covenant04_StrongerBosses"
                ? before.Definitions.Any(definition => definition.BirthStats!.Attack > 0 && definition.BirthStats.Health > 0)
                : before.Definitions.All(definition => definition.BirthStats!.Attack == 0 && definition.BirthStats.Health == 0),
            "The stronger-boss positive control or nonmatching Covenant controls lost their calibrated behavior.");
        TrueFinalBossSpawnState WithDefinitions(EnemyDefinition[] definitions) => new(before.Battle, definitions,
            before.QueuedBosses, before.OuterBossId, before.RoomBossIds, before.ExternalInteractions);
        EnemyDefinition[] missing = before.Definitions.Select(definition => new EnemyDefinition(definition.Unit,
            definition.Ascends, definition.Loops, definition.ExternalInteractions, definition.CompanionBoss)).ToArray();
        EnemyDefinition[] spoofed = before.Definitions.Select(definition => new EnemyDefinition(definition.Unit,
            definition.Ascends, definition.Loops, definition.ExternalInteractions, definition.CompanionBoss,
            new EnemyBirthStatAdjustment(definition.BirthStats!.Attack, definition.BirthStats.Health, new[] { "different-covenant" }))).ToArray();
        EnemyDefinition[] wrongStats = before.Definitions.Select(definition => new EnemyDefinition(definition.Unit,
            definition.Ascends, definition.Loops, definition.ExternalInteractions, definition.CompanionBoss,
            new EnemyBirthStatAdjustment(definition.BirthStats!.Attack, definition.BirthStats.Health + 1, definition.BirthStats.CovenantIds))).ToArray();
        TrueFinalBossChecks.Require(!TrueFinalBossSpawningModel.Apply(WithDefinitions(missing)).Supported &&
            !TrueFinalBossSpawningModel.Apply(WithDefinitions(spoofed)).Supported &&
            !TrueFinalBossSpawningModel.Apply(WithDefinitions(wrongStats)).Supported,
            "Missing origins, mismatched origins and uncalibrated Covenant birth stats must remain guarded.");
        Console.WriteLine($"TRUE-FINAL-BOSS-COVENANT-BIRTH PASS owner={owner.AssetKey} previews={queries.Length}: pure native stats, preserved raw bases and guarded missing/spoofed origins.");
    }
}
