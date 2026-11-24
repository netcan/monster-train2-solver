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
        CovenantWaves(root, before, rows);
    }

    internal static void CovenantWaves(FixtureValue root, BattleTurnState before, FixtureValue rows)
    {
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

    internal static void SpellUpgradeSlotOwner(FixtureValue battle, FixtureValue donor)
    {
        foreach (FixtureValue archive in new[] { battle, donor })
            TrueFinalBossChecks.Require(archive.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
                archive.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
                "Spell-slot owner comparison requires original 2.3.1 archives.");
        FixtureValue firstTurn = battle.GetProperty("Turns")[0];
        var root = firstTurn.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = firstTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var donorRoot = donor.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState owner = donorRoot.Spawn.Train.Context!.Relics!.Single(RelicModel.IsOriginalSpellUpgradeSlotOwner);
        TrueFinalBossChecks.Require(root.Spawn is { Turn: 0, QueuedTrueFinalBoss: not null } &&
            root.Spawn.Train.Context!.Relics!.All(relic => relic.EffectTypes.Count == 0), "Expected the original neutral deployment.");

        BattleTurnState WithOwner(BattleTurnState state, CombatRelicState relic)
        {
            TrainCombatState train = state.Spawn.Train;
            return TrueFinalBossLethalChecks.WithTrain(state, CardSpellModel.WithContext(train,
                train.Context!.WithRelics([.. train.Context.Relics ?? [], relic])));
        }
        var source = WithOwner(root, owner);
        string immutable = JsonSerializer.Serialize(source, ModelJson.Options);
        BattleTurnResult baseline = BattleTurnModel.EndTurn(root);
        BattleTurnResult after = BattleTurnModel.EndTurn(source);
        TrueFinalBossChecks.Require(baseline.Supported && after.Supported && after.Outcome == baseline.Outcome,
            after.UnsupportedReason ?? "The original spell-slot owner changed first-turn outcome.");
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(baseline.State!), BattleTurnChecks.Comparable(actual)) ??
            ModelJson.Difference(BattleTurnChecks.Comparable(after.State!), BattleTurnChecks.Comparable(WithOwner(actual, owner)));
        TrueFinalBossChecks.Require(difference == null && after.State!.Spawn.Train.Context!.Relics!.Count(RelicModel.IsOriginalSpellUpgradeSlotOwner) == 1 &&
            JsonSerializer.Serialize(source, ModelJson.Options) == immutable,
            "Combat successor, original owner retention or immutable deployment differs: " + difference);

        var unknown = new CombatRelicState("uncalibrated-spell-slot-owner", owner.AssetKey, owner.EffectTypes,
            isCovenant: false, disallowedInPlacementPhase: false);
        var changed = new CombatRelicState(owner.DataId, owner.AssetKey, [.. owner.EffectTypes, RelicUnitTriggerCardGenerationModel.Effect],
            isCovenant: false, disallowedInPlacementPhase: false);
        TrueFinalBossChecks.Require(!BattleTurnModel.EndTurn(WithOwner(root, unknown)).Supported &&
            !BattleTurnModel.EndTurn(WithOwner(root, changed)).Supported, "A changed or additional combat effect was admitted to boss birth.");
        Console.WriteLine("TRUE-FINAL-BOSS-SPELL-SLOT-PAIR PASS: archived native deployment and exact native owner, first successor/RNG, owner retention, and unknown/active effect rejection.");
    }

    internal static void NativeSpellUpgradeSlotOwner(string relentlessPath, string terminalPath)
    {
        using var relentless = ModelJson.ReadFixture(relentlessPath);
        using var terminal = ModelJson.ReadFixture(terminalPath);
        foreach (FixtureValue archive in new[] { relentless.RootElement, terminal.RootElement })
        {
            TrueFinalBossChecks.Require(archive.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
                archive.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
                archive.GetProperty("NativeWon").GetBoolean() && archive.GetProperty("TerminalEffectsSettled").GetBoolean(),
                "Expected a settled original 2.3.1 True Final Boss win.");
            foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                TrueFinalBossChecks.Require(archive.GetProperty(counter).GetInt32() == 0, "Native spell-slot capture has nonzero " + counter);
            foreach (FixtureValue row in archive.GetProperty("Turns").EnumerateArray().Concat(archive.GetProperty("Actions").EnumerateArray()))
            {
                foreach (string boundary in new[] { "Before", "Actual" })
                {
                    var context = row.GetProperty(boundary).Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
                    TrueFinalBossChecks.Require(context.Relics!.Count(RelicModel.IsOriginalSpellUpgradeSlotOwner) == 1,
                        "The exact spell-slot relic must remain present at every decision boundary.");
                }
            }
        }
        TrueFinalBossRelentlessChecks.Native(relentless.RootElement);
        TrueFinalBossMaximumSacrificeChecks.EarlyEntropy(terminalPath);
        Console.WriteLine("TRUE-FINAL-BOSS-SPELL-SLOT-NATIVE PASS: original owner persisted through exact birth, Relentless, and paid terminal damage in two original-game battles.");
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
