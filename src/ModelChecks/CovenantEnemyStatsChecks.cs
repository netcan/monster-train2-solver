using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CovenantEnemyStatsChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);

    private static void Evidence(FixtureValue root)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Native ordinary Covenant battle has nonzero " + counter);
        Require(root.GetProperty("BattleDistance").GetInt32() == 0 && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop",
            "Ordinary Covenant acceptance requires the completed original first battle.");
        BattleTurnState before = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState[] owners = before.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
        Require(TrueFinalBossCovenantModel.CalibratedOwners(owners) &&
            owners.Any(owner => owner.AssetKey == "Covenant01_StrongerEnemiesMoreCards") &&
            owners.All(owner => owner.AssetKey is "Covenant00_FTUE" or "Covenant01_StrongerEnemiesMoreCards"),
            "Ordinary Covenant evidence must declare exactly its calibrated original owners.");
        FixtureValue control = root.GetProperty("CovenantEnemyStatsControl");
        Require(control.GetProperty("Kind").GetString() == "ControlledOriginalEnemyStatCovenantsBeforeBattle" &&
            control.GetProperty("Distance").GetInt32() == 0 && !control.GetProperty("StartOfRunEffectsApplied").GetBoolean() &&
            control.GetProperty("ActiveBefore").GetArrayLength() == 0 &&
            control.GetProperty("ActiveAfter").EnumerateArray().Select(value => value.GetString()).SequenceEqual(owners.Select(owner => owner.DataId)) &&
            control.GetProperty("StartingDeckBefore").GetArrayLength() > 0 &&
            control.GetProperty("StartingDeckBefore").ContentEquals(control.GetProperty("StartingDeckAfter")) &&
            control.GetProperty("RngBefore").GetArrayLength() == 16 &&
            control.GetProperty("RngBefore").ContentEquals(control.GetProperty("RngAfter")),
            "Controlled Covenant activation changed deck/RNG or omitted its startup boundary.");
        FixtureValue[] definitions = control.GetProperty("Owners").EnumerateArray().ToArray();
        Require(definitions.Length == owners.Length, "Missing original Covenant definitions.");
        for (int index = 0; index < owners.Length; index++)
            Require(definitions[index].GetProperty("AssetKey").GetString() == owners[index].AssetKey &&
                definitions[index].GetProperty("Id").GetString() == owners[index].DataId &&
                definitions[index].GetProperty("EffectTypes").EnumerateArray().Select(value => value.GetString()).SequenceEqual(owners[index].EffectTypes),
                "Ordinary Covenant definition or activation order changed.");
        FixtureValue queries = control.GetProperty("StatQueries");
        Require(queries.EnumerateArray().Any(query => query.GetProperty("AssetKey").GetString() == "HeavyT1_Basic" &&
            query.GetProperty("Health").GetInt32() == 15) && queries.EnumerateArray().Any(query =>
                query.GetProperty("AssetKey").GetString() == "Boss_Level1_Basic" && query.GetProperty("Attack").GetInt32() == 1 &&
                query.GetProperty("Health").GetInt32() == 180), "Missing original rank-one heavy/boss stat controls.");
        TrueFinalBossBattleChecks.CovenantWaves(root, before, queries);
    }

    internal static void Native(FixtureValue root, string path)
    {
        Evidence(root);
        Require(root.GetProperty("Policy").GetString() == "no-cards" && root.GetProperty("Actions").GetArrayLength() == 0 &&
            root.GetProperty("Turns").GetArrayLength() == 7 && root.GetProperty("NativeWon").GetBoolean(),
            "Expected the complete seven-turn original no-cards first-battle victory.");
        BattleTurnChecks.Native(root);
        TrainCombatChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine("COVENANT-ENEMY-STATS PASS: original ordinary first battle, native wave stats, complete victory/state/RNG and sixteen independent immutable chains. Explicit activation after startup only.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay, bool ftue = false)
    {
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source);
        Require(source.GetProperty("PlayerAdvisor").GetProperty("InteractionsPassed").GetBoolean(), "Ordinary Covenant player UI interactions failed.");
        foreach (FixtureValue root in new[] { source, replay })
        {
            Evidence(root);
            BattleTurnState before = root.GetProperty("Turns")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
            CombatRelicState[] owners = before.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
            Require(owners.Length == (ftue ? 2 : 1) && owners.Any(owner => owner.AssetKey == "Covenant00_FTUE") == ftue,
                "Player pair must use its declared single-01 or 00+01 original context.");
            FixtureValue player = root.GetProperty("PlayerAdvisor");
            FixtureValue overlay = player.GetProperty("Overlay");
            Require(player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
                overlay.GetProperty("Failures").GetInt32() == 0 && overlay.GetProperty("Horizon").GetInt32() == 3 &&
                overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 && overlay.GetProperty("TacticalSeed").GetBoolean(),
                "Ordinary Covenant acceptance requires actual bounded player advice and pure captures.");
        }
        Require(source.GetProperty("RollingDecisions").EnumerateArray().Any(record => !record.GetProperty("IsEndTurn").GetBoolean()),
            "Ordinary Covenant advice must execute an actual card play.");
        Console.WriteLine("PLAYER-COVENANT-ENEMY-STATS PASS: actual ordinary-battle player advice, native card inputs, complete forecasts/state/RNG and independent frozen replay; all native failure counters zero.");
    }
}
