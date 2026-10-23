using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

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
        Native(root);
        TrainCombatChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path);
        Console.WriteLine("TRUE-FINAL-BOSS-COVENANT-BATTLE PASS: original single-Covenant activation, exact birth stats, six complete EndTurns, native room/train phases and full state/RNG; native failure counters zero. Controlled route and loss only.");
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
