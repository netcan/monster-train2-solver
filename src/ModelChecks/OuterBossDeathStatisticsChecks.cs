using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossDeathStatisticsChecks
{
    private const string Reason = "Terminal damage spells count a boss kill after native battle cleanup.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical, RoomOutcome.BattleWon);
        Require(rows.Length == 41, "Wrong native terminal prefix.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit boss = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 6);
        Require(boss.AssetKey == "Boss_Level3_Arkion_PushAttack" && boss.Health == 9 &&
            boss.BumpRules?.OuterBoss == true && boss.DeathState is { HasStatisticsListener: true, StatisticsListenerOnce: true } &&
            rows[^1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 13 &&
            before.Spawn.Train.Context!.FindCard(13)!.DataId == "fb0683b1-20ac-4dd2-b580-14b502e182f4" &&
            Pyre(before) == 37 && Pyre(actual) == 37,
            "Missing the native Pyregel spell's lethal9-HP outer-boss boundary.");
        foreach (string type in new[] { "HeroesKilled", "AnyHeroKilled" })
            foreach (string duration in new[] { "ThisBattle", "ThisTurn" })
                Require(before.Spawn.Train.Context.Statistics!.Value(13, type, duration) ==
                    actual.Spawn.Train.Context!.Statistics!.Value(13, type, duration),
                    "The masked boss death incremented its cached statistics.");
        TerminalDeathChecks.Run();
        if (replay is FixtureValue second)
        {
            var other = NativeBranchObservationChecks.Replay(second, Reason, boundaryOutcome: RoomOutcome.BattleWon);
            NativeBranchObservationChecks.Pair(source, second, originalPredictionPresent: true);
            Require(other[^1].GetProperty("TargetDeathOnceCallbacks").Deserialize<string[]>()!.SequenceEqual(new[] {
                "CardStatistics.OnCharacterDeath", "HeroManager.RemoveBossActionQueuedVFX" }),
                "Native callback order no longer masks the earlier statistics iterator.");
        }
        Console.WriteLine("OUTER-BOSS-DEATH-STATISTICS PASS:41 native inputs/full state/RNG and unchanged Pyre37; original false kill preserved; terminal attribution controls pass; independentPair=" + replay.HasValue);
    }

    private static int Pyre(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        BattleStatistics old = predicted.Spawn.Train.Context!.Statistics!, native = actual.Spawn.Train.Context!.Statistics!;
        Require(old.Value(13, "HeroesKilled", "ThisBattle") == 2 && native.Value(13, "HeroesKilled", "ThisBattle") == 1 &&
            old.Value(13, "AnyHeroKilled", "ThisBattle") == 10 && native.Value(13, "AnyHeroKilled", "ThisBattle") == 8,
            "Original false terminal kill was overwritten.");
        bool KillValue(CardStatisticValue value) => value.CardId == 13 && value.Duration is "ThisTurn" or "ThisBattle" &&
            value.Type is "HeroesKilled" or "AnyHeroKilled";
        Require(JsonSerializer.Serialize(old.Values.Where(value => !KillValue(value))) ==
            JsonSerializer.Serialize(native.Values.Where(value => !KillValue(value))), "Unrelated statistic values differ.");
        var node = JsonSerializer.SerializeToNode(predicted, ModelJson.Options)!;
        node["Spawn"]!["Train"]!["Context"]!["Statistics"]!["Values"] = JsonSerializer.SerializeToNode(native.Values, ModelJson.Options);
        Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!, terminal: true) ==
            NativeBranchObservationChecks.Key(actual, terminal: true), "Mismatch extends beyond the false terminal kill counters.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
