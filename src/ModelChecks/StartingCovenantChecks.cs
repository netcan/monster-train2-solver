using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StartingCovenantChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);

    private static void Evidence(FixtureValue root, int? expectedLevel = null)
    {
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Native Covenant startup battle has nonzero " + name);
        Require(root.GetProperty("BattleDistance").GetInt32() == 0 && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("CovenantEnemyStatsControl").ValueKind == FixtureKind.Null,
            "Native Covenant startup acceptance requires the original completed first battle, without post-startup activation.");
        FixtureValue startup = root.GetProperty("StartingCovenantControl");
        FixtureValue selection = startup.GetProperty("Selection");
        int level = startup.GetProperty("AscensionLevel").GetInt32();
        Require(!expectedLevel.HasValue || level == expectedLevel, "Native Covenant fixture uses a different declared cumulative rank.");
        Require(startup.GetProperty("Kind").GetString() == "NativeCovenantRunStartup" &&
            level is 1 or 2 or 3 && startup.GetProperty("StartingLevel").GetInt32() == level &&
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
        Require(calls.Length == 2 && calls[0].GetProperty("Effect").GetString() == "RelicEffectPurgeStarterDeck" &&
            calls[1].GetProperty("Effect").GetString() == "RelicEffectAddCardSetStartOfRun" &&
            calls.All(call => call.GetProperty("OwnerId").GetString() == owner.GetProperty("Id").GetString() &&
                call.GetProperty("RngBefore").GetArrayLength() == 16 && call.GetProperty("RngAfter").GetArrayLength() == 16),
            "Native Covenant startup must execute each original purge/add callback exactly once.");
        FixtureValue purge = calls[0], add = calls[1];
        Require(purge.GetProperty("Before").GetArrayLength() > purge.GetProperty("After").GetArrayLength() &&
            purge.GetProperty("After").GetArrayLength() == 1 &&
            purge.GetProperty("After").EnumerateArray().All(card => card.GetProperty("IsChampion").GetBoolean()) &&
            purge.GetProperty("RngBefore").ContentEquals(purge.GetProperty("RngAfter")) &&
            purge.GetProperty("After").ContentEquals(add.GetProperty("Before")) &&
            add.GetProperty("After").GetArrayLength() > add.GetProperty("Before").GetArrayLength() &&
            add.GetProperty("After").ContentEquals(startup.GetProperty("StartingDeck")),
            "The native startup callbacks must purge only non-champions, then rebuild the actual starting deck.");
        BattleTurnState before = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState[] captured = before.Spawn.Train.Context!.Relics!.Where(relic => relic.IsCovenant == true).ToArray();
        Require(captured.Length == level && TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(captured) &&
            captured.Select(relic => relic.DataId).SequenceEqual(startup.GetProperty("Owners").EnumerateArray().Select(value => value.GetProperty("Id").GetString())) &&
            captured[0].DataId == owner.GetProperty("Id").GetString() &&
            owner.GetProperty("EffectTypes").EnumerateArray().Select(value => value.GetString()).SequenceEqual(captured[0].EffectTypes),
            "Native startup and actual battle capture disagree on the original Covenant definition.");
        TrueFinalBossBattleChecks.CovenantWaves(root, before, startup.GetProperty("StatQueries"));
        if (level >= 2)
        {
            Require(captured[1].AssetKey == "Covenant02_BattlesNewEnemies" && captured[1].EffectTypes.Count == 0 &&
                !TrueFinalBossCovenantModel.CalibratedOwners(captured) &&
                !TrueFinalBossCovenantModel.CalibratedOuterBossOwners(before.Spawn.Train.Context, "Boss_Level8_Corrupter"),
                "Rank two must retain its original selection-only owner without admitting uncalibrated final/outer bosses.");
            CombatRelicState[] invalid = captured.Select(relic => relic.AssetKey == "Covenant02_BattlesNewEnemies" ?
                new CombatRelicState(relic.DataId, relic.AssetKey, new[] { "RelicEffectNull" }, isCovenant: true) : relic).ToArray();
            Require(!TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(invalid),
                "The selection-only Covenant must not authorize additional combat callbacks.");
        }
        if (level == 3) Require(TrueFinalBossCovenantModel.MerchantOnlyOwner(captured[2]),
            "Rank three must retain its original merchant-only owner and exact native effect signature.");
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

    internal static void Pair(FixtureValue source, FixtureValue replay, int level = 1)
    {
        Evidence(source, level); Evidence(replay, level);
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
