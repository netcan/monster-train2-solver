using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class SolverStageChecks
{
    internal static void Native(string path, bool requireVictory = true)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            (!requireVictory || root.GetProperty("NativeWon").GetBoolean()) && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Input is not a complete native battle with the required outcome for the pinned version.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(root);
        RecordedBattleChecks.Native(root);
        var (source, _, matched, _) = BattleSearchChecks.RecordedFrontiers(root, requireZeroUnsupported: true);
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        int PyreHealth(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.IsPyre).Sum(unit => unit.Health);
        double Score(BattleTurnState state, RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ? -1000000 :
            outcome == RoomOutcome.PlayerDefeated ? 1000000 : PyreHealth(source) - PyreHealth(state);
        // Complete root frontiers and all recorded successors belong to every
        // stage case. Deeper budget/choice pressure has its own focused check.
        var serial = BattleSearchModel.Minimize(source, Score, new(1, 1, 1));
        var parallel = BattleSearchModel.Minimize(source, Score, new(1, 1, 4));
        foreach (var result in new[] { serial, parallel })
        {
            Require(result.BestState != null && result.DecisionStatesVisited == 1 && !result.HasUnsupportedAttempts,
                "Stage search returned an incomplete or unsupported root frontier.");
            var replay = BattlePlannerChecks.Replay(source, result.Steps, out var outcome);
            Require(outcome == result.Outcome && BattleTurnChecks.Comparable(replay) == BattleTurnChecks.Comparable(result.BestState!),
                "Stage returned-plan replay differs from its searched complete successor.");
        }
        Require(serial.Score == parallel.Score && serial.Outcome == parallel.Outcome &&
            JsonSerializer.Serialize(serial.Steps, ModelJson.Options) == JsonSerializer.Serialize(parallel.Steps, ModelJson.Options) &&
            BattleTurnChecks.Comparable(serial.BestState!) == BattleTurnChecks.Comparable(parallel.BestState!) &&
            parent == JsonSerializer.Serialize(source, ModelJson.Options), "Stage serial/parallel result or immutable parent differs.");
        Console.WriteLine($"SOLVER-STAGE-CASE PASS plays={root.GetProperty("Actions").GetArrayLength()} " +
            $"endTurns={root.GetProperty("Turns").GetArrayLength()} decisions={matched} " +
            "nativeFourZero=true completeChainBranches=16 recordedFrontiers=complete rootSearch=complete serialParallel=equal planReplay=equal");
    }
}
