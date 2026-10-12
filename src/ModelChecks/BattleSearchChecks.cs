using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleSearchChecks
{
    internal static void Native(FixtureValue fixture)
    {
        FixtureValue actions = fixture.GetProperty("Actions");
        Require(actions.GetArrayLength() > 0, "The native search fixture has no recorded card actions.");
        FixtureValue first = actions[0];
        BattleTurnState root = first.GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction nativeAction = first.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(root);
        SupportedActionCandidate? nativeCandidate = frontier.SupportedCandidates.FirstOrDefault(candidate => SameAction(candidate.Action, nativeAction));
        Require(nativeCandidate != null, "The recorded native action is missing from the searchable supported frontier.");
        BattleTurnState nativeActual = first.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        bool terminal = nativeCandidate!.Result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
        Require(nativeCandidate.Result.Outcome == (RoomOutcome)first.GetProperty("ActualOutcome").GetInt32() &&
            BattleTurnChecks.Comparable(nativeCandidate.Result.State!, terminal) == BattleTurnChecks.Comparable(nativeActual, terminal),
            "The cached search successor for the recorded native action differs from the captured result.");

        int initialPyre = PyreHealth(root);
        double Score(BattleTurnState state, RoomOutcome outcome)
        {
            if (outcome == RoomOutcome.BattleWon) return -1_000_000;
            if (outcome == RoomOutcome.PlayerDefeated) return 1_000_000;
            return initialPyre - PyreHealth(state);
        }

        BattleSearchResult search = BattleSearchModel.Minimize(root, Score,
            new BattleSearchOptions(maxNodes: 128, maxTurns: 1, maxDegreeOfParallelism: 4));
        Require(search.BestState != null && search.Score.HasValue && search.Steps.Count > 0,
            "Bounded search returned no result from the native battle decision state.");
        BattleTurnState replayed = root;
        foreach (BattleSearchStep step in search.Steps)
        {
            if (step.IsEndTurn)
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(replayed);
                Require(result.Supported, result.UnsupportedReason ?? "Searched EndTurn failed during native-state replay.");
                replayed = result.State!;
            }
            else
            {
                BattleActionResult result = BattleActionModel.PlayCard(replayed, step.Action!);
                Require(result.Supported, result.Reason ?? "Searched card action failed during native-state replay.");
                replayed = result.State!;
            }
        }
        Require(BattleTurnChecks.Comparable(replayed) == BattleTurnChecks.Comparable(search.BestState!) &&
            search.DecisionStatesVisited <= 128,
            "The returned native search path did not reproduce its best state within the configured node budget.");
        string limits = string.Join(",", new[]
        {
            search.ReachedNodeLimit ? "node" : null,
            search.ReachedTurnLimit ? "turn" : null,
            search.Cancelled ? "cancelled" : null
        }.Where(value => value != null));
        if (limits.Length == 0) limits = search.HasUnsupportedAttempts ? "unsupported-frontier" : "enumerated-frontier-exhausted";
        Console.WriteLine($"NATIVE-BATTLE-SEARCH-CHECKS PASS: {frontier.SupportedCandidates.Count} root actions, " +
            $"{frontier.UnsupportedAttempts.Count} unsupported at root, {search.UnsupportedAttempts.Count} in search, " +
            $"{search.DecisionStatesVisited} states, {search.Steps.Count} replayed steps, stop={limits}, score {search.Score}.");
    }

    private static int PyreHealth(BattleTurnState state) => state.Spawn.Train.Rooms
        .SelectMany(room => room.Units).Where(unit => unit.IsPyre).Select(unit => unit.Health).DefaultIfEmpty(0).First();

    private static bool SameAction(PlayCardAction first, PlayCardAction second) =>
        first.CardInstanceId == second.CardInstanceId && first.RoomIndex == second.RoomIndex &&
        first.PlayerPosition == second.PlayerPosition && first.TargetUnitId == second.TargetUnitId &&
        first.ActivatorUnitId == second.ActivatorUnitId;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
