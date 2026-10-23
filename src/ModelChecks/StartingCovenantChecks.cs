using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StartingCovenantChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);

    private static void Evidence(FixtureValue root, int? expectedLevel = null, int distance = 0)
    {
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Native Covenant startup battle has nonzero " + name);
        Require(root.GetProperty("BattleDistance").GetInt32() == distance && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("CovenantEnemyStatsControl").ValueKind == FixtureKind.Null,
            "Native Covenant startup acceptance requires the original completed first battle, without post-startup activation.");
        FixtureValue startup = root.GetProperty("StartingCovenantControl");
        FixtureValue selection = startup.GetProperty("Selection");
        int level = startup.GetProperty("AscensionLevel").GetInt32();
        Require(!expectedLevel.HasValue || level == expectedLevel, "Native Covenant fixture uses a different declared cumulative rank.");
        Require(startup.GetProperty("Kind").GetString() == "NativeCovenantRunStartup" &&
            level is >= 1 and <= 10 && startup.GetProperty("StartingLevel").GetInt32() == level &&
            selection.GetProperty("Kind").GetString() == "SelectNativeCovenantAtRunSetup" &&
            selection.GetProperty("RequestedLevel").GetInt32() == level && selection.GetProperty("LevelAfter").GetInt32() == level &&
            !selection.GetProperty("EarnedUnlockClaimed").GetBoolean() && !selection.GetProperty("StartupEffectsInvokedByProbe").GetBoolean() &&
            startup.GetProperty("Owners").ContentEquals(startup.GetProperty("ExpectedOwners")) &&
            startup.GetProperty("Owners").GetArrayLength() == level,
            "Covenant startup must use native run setup/starting conditions and native cumulative owners.");
        FixtureValue owner = startup.GetProperty("Owners")[0];
        Require(owner.GetProperty("AssetKey").GetString() == "Covenant01_StrongerEnemiesMoreCards" &&
            owner.GetProperty("Id").GetString() == "d3b53481-f26b-4344-ae8e-ec80b28d6004" && owner.GetProperty("Level").GetInt32() == 1,
            "Unexpected original rank-one Covenant owner.");
        FixtureValue[] calls = startup.GetProperty("StartupCalls").EnumerateArray().ToArray();
        Require(calls.Length == (level >= 5 ? 4 : 2) && calls[0].GetProperty("Effect").GetString() == "RelicEffectPurgeStarterDeck" &&
            calls[1].GetProperty("Effect").GetString() == "RelicEffectAddCardSetStartOfRun" &&
            calls.Take(2).All(call => call.GetProperty("OwnerId").GetString() == owner.GetProperty("Id").GetString()) &&
            calls.All(call =>
                call.GetProperty("RngBefore").GetArrayLength() == 16 && call.GetProperty("RngAfter").GetArrayLength() == 16),
            "Native Covenant startup must execute each original purge/add callback exactly once.");
        FixtureValue purge = calls[0], add = calls[1];
        Require(purge.GetProperty("Before").GetArrayLength() > purge.GetProperty("After").GetArrayLength() &&
            purge.GetProperty("After").GetArrayLength() == 1 &&
            purge.GetProperty("After").EnumerateArray().All(card => card.GetProperty("IsChampion").GetBoolean()) &&
            purge.GetProperty("RngBefore").ContentEquals(purge.GetProperty("RngAfter")) &&
            purge.GetProperty("After").ContentEquals(add.GetProperty("Before")) &&
            add.GetProperty("After").GetArrayLength() > add.GetProperty("Before").GetArrayLength() &&
            (level >= 5 || add.GetProperty("After").ContentEquals(startup.GetProperty("StartingDeck"))),
            "The native startup callbacks must purge only non-champions, then rebuild the actual starting deck.");
        BattleTurnState before = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState[] captured = before.Spawn.Train.Context!.Relics!.Where(relic => relic.IsCovenant == true).ToArray();
        Require(captured.Length == level && TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(captured) &&
            captured.Select(relic => relic.DataId).SequenceEqual(startup.GetProperty("Owners").EnumerateArray().Select(value => value.GetProperty("Id").GetString())) &&
            captured[0].DataId == owner.GetProperty("Id").GetString() &&
            startup.GetProperty("Owners").EnumerateArray().Select((value, index) =>
                value.GetProperty("EffectTypes").EnumerateArray().Select(effect => effect.GetString()).SequenceEqual(captured[index].EffectTypes)).All(match => match),
            "Native startup and actual battle capture disagree on the original Covenant definition.");
        TrueFinalBossBattleChecks.CovenantWaves(root, before, startup.GetProperty("StatQueries"));
        if (level >= 2)
        {
            Require(captured[1].AssetKey == "Covenant02_BattlesNewEnemies" && captured[1].EffectTypes.Count == 0 &&
                !TrueFinalBossCovenantModel.CalibratedOwners(captured),
                "Rank two must retain its original selection-only owner, distinct from the four-definition isolated controls.");
            CombatRelicState[] invalid = captured.Select(relic => relic.AssetKey == "Covenant02_BattlesNewEnemies" ?
                new CombatRelicState(relic.DataId, relic.AssetKey, new[] { "RelicEffectNull" }, isCovenant: true) : relic).ToArray();
            Require(!TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(invalid),
                "The selection-only Covenant must not authorize additional combat callbacks.");
        }
        if (level >= 3) Require(TrueFinalBossCovenantModel.MerchantOnlyOwner(captured[2]),
            "Rank three must retain its original merchant-only owner and exact native effect signature.");
        if (level >= 4)
        {
            Require(TrueFinalBossCovenantModel.StrongerBossOwner(captured[3]),
                "Rank four must retain the complete original 25-effect stronger-boss definition.");
            CombatRelicState[] invalid = captured.Select(relic => relic.AssetKey == "Covenant04_StrongerBosses" ?
                new CombatRelicState(relic.DataId, relic.AssetKey, relic.EffectTypes.Concat(new[] { "RelicEffectNull" }).ToArray(),
                    isCovenant: true) : relic).ToArray();
            Require(!TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(invalid),
                "The calibrated stronger-boss definition must not authorize additional callbacks.");
        }
        if (level >= 5)
        {
            Require(TrueFinalBossCovenantModel.StartupCardsOwner(captured[4]) &&
                calls.Skip(2).All(call => call.GetProperty("Effect").GetString() == "RelicEffectAddCardsStartOfRun" &&
                    call.GetProperty("OwnerId").GetString() == captured[4].DataId &&
                    call.GetProperty("After").GetArrayLength() > call.GetProperty("Before").GetArrayLength()) &&
                add.GetProperty("After").ContentEquals(calls[2].GetProperty("Before")) &&
                calls[2].GetProperty("After").ContentEquals(calls[3].GetProperty("Before")) &&
                calls[3].GetProperty("After").ContentEquals(startup.GetProperty("StartingDeck")),
                "Rank five must execute both original add/upgrade callbacks exactly once and retain their actual cards/upgrades.");
            FixtureValue[] added = startup.GetProperty("StartingDeck").EnumerateArray()
                .Skip(add.GetProperty("After").GetArrayLength()).ToArray();
            Require(added.Length == 2 && added.All(card => card.GetProperty("AssetKey").GetString() == "BlightPyreDamageInHand" &&
                card.GetProperty("DataId").GetString() == "186180e9-94e9-47d0-b69c-865b9a6d27a4" &&
                !card.GetProperty("IsChampion").GetBoolean() && card.GetProperty("Upgrades").GetArrayLength() == 0),
                "The original rank-five startup must retain both native unupgraded Vengeful Shards.");
            CombatRelicState[] invalid = captured.Select(relic => relic.AssetKey == "Covenant05_VengefulShards" ?
                new CombatRelicState(relic.DataId, relic.AssetKey, relic.EffectTypes.Concat(new[] { "RelicEffectNull" }).ToArray(),
                    isCovenant: true) : relic).ToArray();
            Require(!TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(invalid),
                "The startup-only Covenant must not authorize additional battle callbacks.");
        }
        if (level >= 6) Require(TrueFinalBossCovenantModel.DraftOnlyOwner(captured[5]),
            "Rank six must retain the exact original reward-draft-only Covenant.");
        if (level >= 7) Require(TrueFinalBossCovenantModel.WaveMessageOwner(captured[6]),
            "Rank seven must retain the exact original wave-message Covenant.");
        if (level >= 8) Require(TrueFinalBossCovenantModel.StrongerEnemyOwner(captured[7]),
            "Rank eight must retain all fourteen original enemy-stat effects.");
        if (level >= 9)
        {
            FixtureValue[] pyreCalls = startup.GetProperty("PyreStartupCalls").EnumerateArray().ToArray();
            Require(TrueFinalBossCovenantModel.StartingPyreOwner(captured[8]) && pyreCalls.Length == 1,
                "Rank nine must execute the original startup Pyre callback exactly once.");
            FixtureValue call = pyreCalls[0];
            int hp = call.GetProperty("Before").GetInt32(), amount = call.GetProperty("Amount").GetInt32();
            int damage = call.GetProperty("Percent").GetBoolean() ? (int)Math.Floor((float)hp * amount / 100f) : amount;
            Require(call.GetProperty("OwnerId").GetString() == captured[8].DataId && damage > 0 &&
                call.GetProperty("After").GetInt32() == hp - damage &&
                call.GetProperty("MaxBefore").GetInt32() == call.GetProperty("MaxAfter").GetInt32() &&
                call.GetProperty("RngBefore").GetArrayLength() == 16 &&
                call.GetProperty("RngBefore").ContentEquals(call.GetProperty("RngAfter")) &&
                startup.GetProperty("StartingPyre").GetProperty("Health").GetInt32() == hp - damage &&
                startup.GetProperty("StartingPyre").GetProperty("MaxHealth").GetInt32() == call.GetProperty("MaxAfter").GetInt32(),
                "Native startup Pyre damage must match its original parameters without changing max health or RNG.");
            if (root.GetProperty("Policy").GetString() == "no-cards")
                Require(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == hp - damage,
                    "The battle must retain native startup Pyre health.");
        }
        if (level >= 10) Require(TrueFinalBossCovenantModel.FlyingBossSelectionOwner(captured[9]),
            "Rank ten must retain the original selection-only definition without inventing combat callbacks.");
        for (int index = 5; index < captured.Length; index++)
        {
            CombatRelicState changed = captured[index];
            CombatRelicState[] invalid = captured.Select(relic => ReferenceEquals(relic, changed) ?
                new CombatRelicState(relic.DataId, relic.AssetKey, relic.EffectTypes.Concat(new[] { "RelicEffectNull" }).ToArray(),
                    isCovenant: true) : relic).ToArray();
            Require(!TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(invalid),
                "A cumulative Covenant must not authorize extra uncalibrated callbacks: " + changed.AssetKey);
        }
        Console.WriteLine($"NATIVE-COVENANT-STARTUP PASS: native rank {level} selection, exact cumulative owners, one original purge/add callback each and {startup.GetProperty("StartingDeck").GetArrayLength()} actual starting cards; original battle waves and stat-query purity match. Earned unlocks remain separate.");
    }

    internal static void Native(FixtureValue root, string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        const string prefix = "full-battle-native-covenant-startup-";
        Evidence(root, name.StartsWith(prefix, StringComparison.Ordinal) ? int.Parse(name[prefix.Length..]) : null);
        Require(root.GetProperty("Policy").GetString() == "no-cards" && root.GetProperty("Actions").GetArrayLength() == 0,
            "The native Covenant startup baseline must not play cards.");
        BattleTurnChecks.Native(root);
        TrainCombatChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path);
    }

    private static void FinalEvidence(FixtureValue root, int level)
    {
        Evidence(root, level, 8);
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Kind").GetString() == "NativeCheatJumpToTFB" &&
            !route.GetProperty("UnlockOverrideBefore").GetBoolean() && route.GetProperty("UnlockOverrideAfter").GetBoolean() &&
            route.GetProperty("DistanceBefore").GetInt32() == 0 && route.GetProperty("DistanceAfter").GetInt32() == 8 &&
            route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("CovenantControl").ValueKind == FixtureKind.Null &&
            route.GetProperty("PyreHealth").GetInt32() == root.GetProperty("StartingCovenantControl").GetProperty("StartingPyre").GetProperty("Health").GetInt32(),
            "Cumulative final-boss acceptance requires native startup and disclosed isolated unlock/route control, without post-startup activation.");
        FixtureValue[] rng = route.GetProperty("RngBefore").EnumerateArray().ToArray();
        Require(rng.Length == 16 && route.GetProperty("RngAfter").GetArrayLength() == 16 &&
            rng.Zip(route.GetProperty("RngAfter").EnumerateArray()).All(pair => pair.First.GetProperty("Id").GetString() == "NonDeterministic" ||
                pair.First.ContentEquals(pair.Second)), "The final-boss route changed gameplay RNG.");
        BattleTurnState before = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        EnemyDefinition[] definitions = before.Spawn.QueuedTrueFinalBoss!.Definitions.ToArray();
        Require(definitions.Select(value => value.Unit.AssetKey).SequenceEqual(TrueFinalBossChecks.Assets),
            "Cumulative final-boss acceptance must retain all five original definitions.");
        foreach (EnemyDefinition definition in definitions)
        {
            FixtureValue query = root.GetProperty("StartingCovenantControl").GetProperty("StatQueries").EnumerateArray()
                .Single(value => value.GetProperty("AssetKey").GetString() == definition.Unit.AssetKey);
            Require(definition.BirthStats != null && definition.Unit.BaseAttack == query.GetProperty("RawAttack").GetInt32() &&
                definition.Unit.MaxHealth == query.GetProperty("RawHealth").GetInt32() &&
                definition.BirthStats.Attack == query.GetProperty("Attack").GetInt32() &&
                definition.BirthStats.Health == query.GetProperty("Health").GetInt32() &&
                TrueFinalBossCovenantModel.MatchesOrigins(before.Spawn.Train.Context, definition.BirthStats),
                "Cumulative final-boss definitions lost their raw stats, pure native adjustments or exact owners.");
        }
    }

    internal static void FinalNative(FixtureValue root, FixtureValue birth, string path, int level)
    {
        FinalEvidence(root, level);
        Require(root.GetProperty("Policy").GetString() == "no-cards" && root.GetProperty("Actions").GetArrayLength() == 0,
            "Cumulative final-boss baseline must not play cards.");
        TrueFinalBossScheduledChecks.Native(birth, cumulativeStartup: true);
        BattleTurnChecks.Native(root);
        TrainCombatChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"NATIVE-COVENANT-FINAL-BOSS PASS rank={level}: native cumulative startup, pure five-actor birth, scheduled placement and full battle/state/RNG; isolated unlock override only.");
    }

    internal static void FinalPair(FixtureValue source, FixtureValue replay, int level)
    {
        FinalEvidence(source, level); FinalEvidence(replay, level);
        Pair(source, replay, level, 8);
    }

    internal static void CuratedFinal(FixtureValue root, string path, bool player)
    {
        string prefix = player ? "full-battle-player-native-covenant-final-boss-" : "full-battle-native-covenant-final-boss-";
        string suffix = Path.GetFileNameWithoutExtension(path)[prefix.Length..];
        if (player) suffix = suffix[..^"-search".Length];
        int level = int.Parse(suffix);
        string other = player ? prefix + suffix + "-replay.mt2f" : "true-final-boss-scheduled-native-covenant-" + suffix + ".mt2f";
        using var document = ModelJson.ReadFixture(Path.Combine(Path.GetDirectoryName(path)!, other));
        if (player) FinalPair(root, document.RootElement, level);
        else FinalNative(root, document.RootElement, path, level);
    }

    private static void OuterEvidence(FixtureValue root, string variant, int level)
    {
        Require(variant is "AllyBuffer" or "Fragile" or "Corrupter", "Uncalibrated native Covenant outer-boss variant.");
        Evidence(root, level, 7);
        string boss = "Boss_Level8_" + variant;
        FixtureValue route = root.GetProperty("OriginalOuterBossRoute");
        Require(route.GetProperty("Kind").GetString() == "SelectOriginalOuterBossScenario" &&
            route.GetProperty("Distance").GetInt32() == 7 && route.GetProperty("Boss").GetString() == boss &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("TitanTrial").ValueKind == FixtureKind.Null &&
            route.GetProperty("RngBefore").GetArrayLength() == 16 && route.GetProperty("RngBefore").ContentEquals(route.GetProperty("RngAfter")),
            "Native Covenant outer-boss acceptance requires its explicitly selected unmodified original route.");
        BattleTurnState before = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        EnemyDefinition actor = before.Spawn.QueuedOuterBoss!.Actor;
        FixtureValue query = root.GetProperty("StartingCovenantControl").GetProperty("StatQueries").EnumerateArray()
            .Single(value => value.GetProperty("AssetKey").GetString() == boss);
        Require(actor.Unit.AssetKey == boss && actor.BirthStats != null &&
            actor.Unit.BaseAttack == query.GetProperty("RawAttack").GetInt32() &&
            actor.Unit.MaxHealth == query.GetProperty("RawHealth").GetInt32() &&
            actor.BirthStats.Attack == query.GetProperty("Attack").GetInt32() &&
            actor.BirthStats.Health == query.GetProperty("Health").GetInt32() &&
            TrueFinalBossCovenantModel.MatchesOrigins(before.Spawn.Train.Context, actor.BirthStats) &&
            !TrueFinalBossCovenantModel.CalibratedOuterBossOwners(before.Spawn.Train.Context, boss + "_TitanTrial"),
            "Native cumulative boss birth must retain raw stats, exact pure native adjustments and its separate trial guard.");
    }

    internal static void OuterNative(FixtureValue root, FixtureValue birth, string path, string variant, int level)
    {
        OuterEvidence(root, variant, level);
        Require(root.GetProperty("Policy").GetString() == "no-cards" && root.GetProperty("Actions").GetArrayLength() == 0,
            "Native cumulative boss baseline must not play cards.");
        OuterBossBirthChecks.Native(birth);
        OuterBossCreationState created = birth.GetProperty("Births")[0].GetProperty("Before").Deserialize<OuterBossCreationState>()!;
        Require(created.BirthStats != null && created.CreatedUnit.AssetKey == "Boss_Level8_" + variant,
            "Native cumulative boss creation lacks its pre-Covenant actor and retained birth evidence.");
        OuterBossTurnChecks.Deployment(root, "Boss_Level8_" + variant, 2);
        BattleTurnChecks.Native(root);
        TrainCombatChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine($"NATIVE-COVENANT-OUTER-BOSS PASS rank={level} variant={variant}: original startup, raw creation, post-placement Covenant callbacks and complete native battle/state/RNG.");
    }

    internal static void OuterPair(FixtureValue source, FixtureValue replay, string variant, int level)
    {
        OuterEvidence(source, variant, level); OuterEvidence(replay, variant, level);
        Pair(source, replay, level, 7);
        if (variant == "AllyBuffer" && level == 10)
        {
            ChosenIncantAttackChecks.Native(source);
            ChosenIncantAttackChecks.Native(replay);
        }
    }

    internal static void CuratedOuter(FixtureValue root, string path, bool player = false)
    {
        string prefix = player ? "full-battle-player-native-covenant-outer-boss-" : "full-battle-native-covenant-outer-boss-";
        string suffix = Path.GetFileNameWithoutExtension(path)[prefix.Length..];
        if (player) suffix = suffix[..^"-search".Length];
        int separator = suffix.LastIndexOf('-');
        int level = int.Parse(suffix[(separator + 1)..]);
        string variant = suffix[..separator] switch
        {
            "allybuffer" => "AllyBuffer", "fragile" => "Fragile", "corrupter" => "Corrupter",
            _ => throw new InvalidOperationException("Unknown curated cumulative outer-boss variant.")
        };
        string other = player ? "full-battle-player-native-covenant-outer-boss-" + suffix + "-replay.mt2f" :
            "outer-boss-birth-native-covenant-" + suffix + ".mt2f";
        using var document = ModelJson.ReadFixture(Path.Combine(Path.GetDirectoryName(path)!, other));
        if (player) OuterPair(root, document.RootElement, variant, level);
        else OuterNative(root, document.RootElement, path, variant, level);
    }

    internal static void Pair(FixtureValue source, FixtureValue replay, int level = 1, int distance = 0)
    {
        Evidence(source, level, distance); Evidence(replay, level, distance);
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source);
        Require(source.GetProperty("StartingCovenantControl").GetProperty("StartingDeck").ContentEquals(
            replay.GetProperty("StartingCovenantControl").GetProperty("StartingDeck")),
            "Independent Covenant startup rebuilt a different original starting deck.");
        Require(source.GetProperty("PlayerAdvisor").GetProperty("InteractionsPassed").GetBoolean() &&
            source.GetProperty("RollingDecisions").EnumerateArray().Any(row => !row.GetProperty("IsEndTurn").GetBoolean()),
            "Native Covenant startup player acceptance must pass UI interactions and execute cards.");
        foreach (FixtureValue root in new[] { source, replay })
        {
            FixtureValue player = root.GetProperty("PlayerAdvisor"), overlay = player.GetProperty("Overlay");
            Require(player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("Failures").GetInt32() == 0 &&
                overlay.GetProperty("PurityFailures").GetInt32() == 0 && overlay.GetProperty("Horizon").GetInt32() == 3 &&
                overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 && overlay.GetProperty("TacticalSeed").GetBoolean(),
                "Native Covenant startup requires actual bounded player advice and pure captures.");
        }
        Console.WriteLine("PLAYER-NATIVE-COVENANT-STARTUP PASS: original startup/deck effects, actual advice/UI, every complete forecast/state/RNG and independent frozen native replay; zero native failure counters.");
    }
}
