using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class TrueFinalBossBattleChecks
{
    internal static void Covenant(FixtureValue root, string path)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            TrueFinalBossChecks.Require(root.GetProperty(counter).GetInt32() == 0, "Native Covenant battle has nonzero " + counter);
        BattleTurnState before = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        TrueFinalBossSpawnDefinition queued = before.Spawn.QueuedTrueFinalBoss!;
        TrueFinalBossChecks.Require(queued != null, "Missing original queued Covenant bosses.");
        TrueFinalBossSpawnChecks.CovenantMetadata(root.GetProperty("OriginalTrueFinalBossRoute").GetProperty("CovenantControl"),
            new TrueFinalBossSpawnState(before, queued!.Definitions, queued.QueuedBosses, 0, new int[4], queued.ExternalInteractions));
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        string[] owners = before.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).Select(owner => owner.AssetKey).ToArray();
        int turns = root.GetProperty("Turns").GetArrayLength();
        TrueFinalBossChecks.Require(route.GetProperty("Kind").GetString() == "NativeCheatJumpToTFB" &&
            route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" && route.GetProperty("UnlockOverrideAfter").GetBoolean() &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("BattleDistance").GetInt32() == 8 && root.GetProperty("Policy").GetString() == "no-cards" &&
            !root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" && root.GetProperty("Actions").GetArrayLength() == 0 &&
            turns >= 2 && (owners.Length != 1 || turns == (owners[0] == "Covenant04_StrongerBosses" ? 6 : 5)),
            "Missing unchanged original Covenant no-cards full loss.");
        CovenantWaves(root, before);
        BattleTurnChecks.Native(root);
        TrainCombatChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"TRUE-FINAL-BOSS-COVENANT-BATTLE PASS: original {string.Join('+', owners)}, exact birth stats, {turns} complete EndTurns, native room/train phases and full state/RNG; native failure counters zero. Controlled route and loss only.");
    }

    internal static void CovenantWaves(FixtureValue root, BattleTurnState before)
    {
        FixtureValue control = root.GetProperty("OriginalTrueFinalBossRoute").GetProperty("CovenantControl");
        // The earlier stronger-boss archive predates ordinary wave projections.
        if (!control.TryGetProperty("WaveStatQueries", out FixtureValue rows)) return;
        EnemyDefinition[] definitions = before.Spawn.Waves.SelectMany(wave => wave.Candidates).SelectMany(group => group.Units).ToArray();
        FixtureValue[] queries = rows.EnumerateArray().ToArray();
        // Earlier captures also queried player-card templates without applying
        // their projections. Every query must be pure; every wave must match.
        foreach (FixtureValue query in queries)
            TrueFinalBossChecks.Require(query.GetProperty("FrameBefore").GetInt32() == query.GetProperty("FrameAfter").GetInt32() &&
                query.GetProperty("RngBefore").GetArrayLength() == 16 && query.GetProperty("RngBefore").ContentEquals(query.GetProperty("RngAfter")),
                "A native Covenant stat query changed its frame or RNG.");
        TrueFinalBossChecks.Require(definitions.Length > 0 && queries.Length > 0 &&
            queries.Select(row => row.GetProperty("AssetKey").GetString()).ToHashSet().IsSupersetOf(definitions.Select(definition => definition.Unit.AssetKey)),
            "Missing pure previews for the original Covenant wave definitions.");
        foreach (EnemyDefinition definition in definitions)
        {
            FixtureValue query = queries.Single(row => row.GetProperty("AssetKey").GetString() == definition.Unit.AssetKey);
            EnemyBirthStatAdjustment stats = definition.BirthStats!;
            TrueFinalBossChecks.Require(stats != null && TrueFinalBossCovenantModel.MatchesOrigins(before.Spawn.Train.Context, stats) &&
                query.GetProperty("RawAttack").GetInt32() == definition.Unit.BaseAttack && query.GetProperty("RawHealth").GetInt32() == definition.Unit.MaxHealth &&
                query.GetProperty("Attack").GetInt32() == stats.Attack && query.GetProperty("Health").GetInt32() == stats.Health &&
                query.GetProperty("FrameBefore").GetInt32() == query.GetProperty("FrameAfter").GetInt32() &&
                query.GetProperty("RngBefore").GetArrayLength() == 16 && query.GetProperty("RngBefore").ContentEquals(query.GetProperty("RngAfter")),
                "Wave birth stats must preserve authored bases, Covenant origin, frame and all native RNG streams.");
        }
        FixtureValue waveBirth = root.GetProperty("Spawns").EnumerateArray().First(row =>
        {
            EnemySpawnState state = row.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            return state.Waves[state.Phase % state.Waves.Count].Candidates.Any(group => group.Units.Count > 0);
        });
        JsonNode invalid = JsonSerializer.SerializeToNode(waveBirth.GetProperty("Before").Deserialize<EnemySpawnState>()!, ModelJson.Options)!;
        foreach (JsonNode? wave in invalid["Waves"]!.AsArray())
            foreach (JsonNode? group in wave!["Candidates"]!.AsArray())
                foreach (JsonNode? definition in group!["Units"]!.AsArray())
                    definition!["BirthStats"]!["CovenantIds"]![0] = "different-covenant";
        EnemySpawnResult rejected = EnemySpawningModel.Spawn(invalid.Deserialize<EnemySpawnState>(ModelJson.Options)!, false);
        TrueFinalBossChecks.Require(!rejected.Supported && rejected.UnsupportedReason!.Contains("Enemy birth stats"),
            "Ordinary wave births with spoofed Covenant origins must remain guarded.");
        Console.WriteLine($"TRUE-FINAL-BOSS-COVENANT-WAVES PASS: {queries.Length} pure native stat previews, {definitions.Length} authored wave definitions and rejected spoofed origins.");
    }

    internal static void Native(FixtureValue root)
    {
        void Require(bool value, string message) => TrueFinalBossChecks.Require(value, message);
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("Policy").GetString() == "no-cards" && !root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("BattleDistance").GetInt32() == 8 && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("Turns").GetArrayLength() == 6 && root.GetProperty("Actions").GetArrayLength() == 0,
            "Missing unchanged original no-cards true-final-boss native loss.");
        FixtureValue[] damage = root.GetProperty("TrueFinalBossPyreCardDamage").EnumerateArray().ToArray();
        Require(damage.Length == 7 && damage.Take(6).All(row => row.GetProperty("NativeReturned").GetBoolean()) &&
            !damage[6].GetProperty("NativeReturned").GetBoolean() && damage[6].GetProperty("Turn").GetInt32() == 5 &&
            damage[6].GetProperty("HealthBefore").GetInt32() == 3 && damage[6].GetProperty("Damage").GetInt32() == 3,
            "Missing the native fatal unplayed damage stop before coroutine return.");
        BattleTurnState before = root.GetProperty("Turns")[5].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = root.GetProperty("Turns")[5].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int pyre = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Id;
        Require(before.Spawn.Train.Context!.Cards.Hand.Count == 7 && actual.Spawn.Train.Context!.Cards.Hand.Count == 7 &&
            actual.Spawn.Train.Context.KillCamActivated == false && !actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.IsPyre) &&
            root.GetProperty("KillCams").GetArrayLength() == 0 &&
            !root.GetProperty("TerminalDeaths").EnumerateArray().Any(row => row.GetProperty("UnitId").GetInt32() == pyre),
            "Native terminal capture must precede Pyre CheckForDeath/kill-camera card clearing.");
        BattleTurnChecks.Native(root);
        Console.WriteLine("TRUE-FINAL-BOSS-BATTLE PASS: six EndTurns and independent full loss with sixteen parallel chains; original birth/waves/preview/cards/RNG match; terminal unplayed damage preserves seven hand cards at AfterStopCombatLoop. No allied units, relentless phase or victory claim.");
    }
}
