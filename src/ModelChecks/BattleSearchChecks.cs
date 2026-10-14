using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleSearchChecks
{
    internal static void Native(FixtureValue fixture)
    {
        FixtureValue actions = fixture.GetProperty("Actions");
        Require(actions.GetArrayLength() > 0, "The native search fixture has no recorded card actions.");
        BattleTurnState? root = null;
        BattleActionEnumeration? rootFrontier = null;
        int matchedDecisions = 0, unsupportedAttempts = 0;
        for (int index = 0; index < actions.GetArrayLength(); index++)
        {
            FixtureValue entry = actions[index];
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction nativeAction = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(before);
            SupportedActionCandidate? nativeCandidate = frontier.SupportedCandidates
                .FirstOrDefault(candidate => SameAction(candidate.Action, nativeAction));
            if (nativeCandidate == null)
            {
                BattleActionResult replay = BattleActionModel.PlayCard(before, nativeAction);
                Require(false, "Recorded native action " + index + " is missing from the searchable supported frontier: card=" +
                    nativeAction.CardInstanceId + ", room=" + nativeAction.RoomIndex + ", position=" + nativeAction.PlayerPosition +
                    ", target=" + nativeAction.TargetUnitId + ", recursion=" + nativeAction.RecursionSelections.Count +
                    ", replay=" + replay.Rejection + " (" + replay.Reason + ").");
            }
            BattleTurnState nativeActual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool terminal = nativeCandidate!.Result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(nativeCandidate.Result.Outcome == (RoomOutcome)entry.GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(nativeCandidate.Result.State!, terminal) == BattleTurnChecks.Comparable(nativeActual, terminal),
                "Cached search successor for recorded native action " + index + " differs from the captured result.");
            if (index == 0)
            {
                root = before;
                rootFrontier = frontier;
            }
            matchedDecisions++;
            unsupportedAttempts += frontier.UnsupportedAttempts.Count;
        }

        BattleTurnState searchRoot = root!;
        int initialPyre = PyreHealth(searchRoot);
        double Score(BattleTurnState state, RoomOutcome outcome)
        {
            if (outcome == RoomOutcome.BattleWon) return -1_000_000;
            if (outcome == RoomOutcome.PlayerDefeated) return 1_000_000;
            return initialPyre - PyreHealth(state);
        }

        const int maxNodes = 128;
        const int maxTurns = 1;
        BattleSearchResult serialSearch = BattleSearchModel.Minimize(searchRoot, Score,
            new BattleSearchOptions(maxNodes, maxTurns, maxDegreeOfParallelism: 1));
        BattleSearchResult search = BattleSearchModel.Minimize(searchRoot, Score,
            new BattleSearchOptions(maxNodes, maxTurns, maxDegreeOfParallelism: 4));
        Require(search.BestState != null && search.Score.HasValue && search.Steps.Count > 0,
            "Bounded search returned no result from the native battle decision state.");
        string Path(BattleSearchResult result) => string.Join("|", result.Steps.Select(step => step.IsEndTurn ? "EndTurn" :
            step.Action!.CardInstanceId + ":" + step.Action.RoomIndex + ":" + step.Action.PlayerPosition + ":" +
            step.Action.TargetUnitId + ":" + step.Action.ActivatorUnitId + ":" + string.Join(";",
                step.Action.RecursionSelections.Select(selection => selection.EffectIndex + "=" +
                    string.Join(",", selection.CardInstanceIds))) + ":" + string.Join(";",
                step.Action.MixSelections.Select(selection => selection.EffectIndex + "=" +
                    selection.MixNumber + "," + selection.ChoiceIndex))));
        Require(serialSearch.BestState != null && serialSearch.Outcome == search.Outcome &&
            serialSearch.Score == search.Score &&
            serialSearch.DecisionStatesVisited <= maxNodes && search.DecisionStatesVisited <= maxNodes &&
            Path(serialSearch) == Path(search) &&
            BattleTurnChecks.Comparable(serialSearch.BestState!) == BattleTurnChecks.Comparable(search.BestState!),
            "Serial and parallel native searches returned different best paths, scores or states: " +
            $"serial score={serialSearch.Score} nodes={serialSearch.DecisionStatesVisited} path={Path(serialSearch)}; " +
            $"parallel score={search.Score} nodes={search.DecisionStatesVisited} path={Path(search)}.");
        BattleTurnState replayed = searchRoot;
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
            search.DecisionStatesVisited <= maxNodes,
            "The returned native search path did not reproduce its best state within the configured node budget.");
        string limits = string.Join(",", new[]
        {
            search.ReachedNodeLimit ? "node" : null,
            search.ReachedTurnLimit ? "turn" : null,
            search.Cancelled ? "cancelled" : null
        }.Where(value => value != null));
        if (limits.Length == 0) limits = search.HasUnsupportedAttempts ? "unsupported-frontier" : "enumerated-frontier-exhausted";
        Console.WriteLine($"NATIVE-BATTLE-SEARCH-CHECKS PASS: {matchedDecisions} recorded decisions matched, " +
            $"{rootFrontier!.SupportedCandidates.Count} root actions, {unsupportedAttempts} unsupported attempts across recorded decisions, " +
            $"{search.UnsupportedAttempts.Count} in search, serial/parallel best equal, " +
            $"{serialSearch.DecisionStatesVisited}/{search.DecisionStatesVisited} states, {search.Steps.Count} replayed steps, " +
            $"stop={limits}, score {search.Score}.");
    }

    private static int PyreHealth(BattleTurnState state) => state.Spawn.Train.Rooms
        .SelectMany(room => room.Units).Where(unit => unit.IsPyre).Select(unit => unit.Health).DefaultIfEmpty(0).First();

    private static bool SameAction(PlayCardAction first, PlayCardAction second) =>
        first.CardInstanceId == second.CardInstanceId && first.RoomIndex == second.RoomIndex &&
        first.PlayerPosition == second.PlayerPosition && first.TargetUnitId == second.TargetUnitId &&
        first.ActivatorUnitId == second.ActivatorUnitId &&
        first.RecursionSelections.Count == second.RecursionSelections.Count &&
        first.RecursionSelections.Zip(second.RecursionSelections).All(pair =>
            pair.First.EffectIndex == pair.Second.EffectIndex &&
            pair.First.CardInstanceIds.SequenceEqual(pair.Second.CardInstanceIds)) &&
        first.MixSelections.Count == second.MixSelections.Count &&
        first.MixSelections.Zip(second.MixSelections).All(pair =>
            pair.First.EffectIndex == pair.Second.EffectIndex &&
            pair.First.MixNumber == pair.Second.MixNumber &&
            pair.First.ChoiceIndex == pair.Second.ChoiceIndex);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
