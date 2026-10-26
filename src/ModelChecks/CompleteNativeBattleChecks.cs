using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CompleteNativeBattleChecks
{
    // Shared acceptance for complete natural wins or losses. Preserve the
    // actual outcome and raw phases without adding a checker per improved loss.
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var fixture = document.RootElement;
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length is > 1 and <= 256 &&
            fixture.GetProperty("NativeWon").ValueKind is FixtureKind.True or FixtureKind.False,
            "Expected a complete native battle with an actual win/loss outcome.");
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(fixture);
        RollingAdviceChecks.Native(fixture);
        var root = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var replay = BattlePlannerChecks.Replay(root, rows.Select(row => new BattleSearchStep(
            row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean())).ToArray(), out var outcome);
        var expected = fixture.GetProperty("NativeWon").GetBoolean() ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated;
        Require(outcome == expected && NativeBranchObservationChecks.Key(replay, true) ==
            NativeBranchObservationChecks.Key(rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!, true),
            "Model-owned complete battle differs from the actual native outcome/state/RNG.");
        Console.WriteLine($"COMPLETE-NATIVE-BATTLE PASS:{rows.Length} unchanged native inputs, full raw room/train stages, " +
            $"continuous model-owned state/RNG, forecasts and four zero counters; actual outcome={outcome}.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
