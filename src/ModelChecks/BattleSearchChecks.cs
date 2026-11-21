using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class BattleSearchChecks
{
    internal static void FrontierDiagnostics(FixtureValue fixture)
    {
        int index = 0;
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(before);
            if (frontier.HasUnsupportedAttempts)
            {
                foreach (var group in frontier.UnsupportedAttempts.GroupBy(attempt => attempt.Reason))
                    Console.WriteLine("NATIVE-FRONTIER-UNSUPPORTED decision=" + index + " count=" + group.Count() + " reason=" + group.Key +
                        " cards=" + string.Join(",", group.Select(attempt => attempt.Action == null ? "EndTurn" :
                            before.Spawn.Train.Context!.FindCard(attempt.Action.CardInstanceId)?.DataId ?? attempt.Action.CardInstanceId.ToString()).Distinct()));
                return;
            }
            index++;
        }
        Console.WriteLine("NATIVE-FRONTIER-DIAGNOSTICS PASS: " + index + " decisions, zero unsupported attempts.");
    }

    internal static void RollingFrontiers(FixtureValue fixture, int choiceBudget)
    {
        Require(choiceBudget > 0, "The EndTurn choice budget must be positive.");
        FixtureValue[] roots = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(roots.Length > 0, "The fixture has no recorded rolling decisions.");
        int actions = 0, turns = 0, unsupported = 0, limited = 0;
        for (int index = 0; index < roots.Length; index++)
        {
            FixtureValue record = roots[index];
            var before = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var cards = BattleActionModel.EnumerateActions(before);
            var ends = BattleEndTurnModel.Enumerate(before, choiceBudget);
            actions += cards.SupportedCandidates.Count;
            turns += ends.Candidates.Count;
            var rejected = cards.UnsupportedAttempts.Concat(ends.UnsupportedAttempts).ToArray();
            unsupported += rejected.Length;
            foreach (var group in rejected.GroupBy(attempt => attempt.Reason))
                Console.WriteLine($"NATIVE-ROLLING-FRONTIER decision={index} turn={before.Spawn.Turn} unsupported={group.Count()} reason={group.Key}");
            if (ends.ReachedNodeLimit)
            {
                limited++;
                Console.WriteLine($"NATIVE-ROLLING-FRONTIER decision={index} turn={before.Spawn.Turn} choiceBudget={choiceBudget} reached=true");
            }

            BattleTurnState? predicted;
            RoomOutcome? outcome;
            if (record.GetProperty("IsEndTurn").GetBoolean())
            {
                var choices = RecordedBattleChecks.Decisions(record) ?? [];
                var result = FindEndTurn(ends, choices);
                if (result == null && ends.ReachedNodeLimit) result = BattleTurnModel.EndTurn(before, choices);
                predicted = result?.State;
                outcome = result?.Outcome;
            }
            else
            {
                var input = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
                var result = cards.SupportedCandidates.FirstOrDefault(candidate => SameAction(candidate.Action, input, before))?.Result;
                predicted = result?.State;
                outcome = result?.Outcome;
            }
            Require(predicted != null && outcome == record.GetProperty("Outcome").Deserialize<RoomOutcome>(),
                $"Recorded rolling input {index} differs from the available model successor.");
            RecordedBattleChecks.Compare(predicted!, record.GetProperty("Actual").Deserialize<BattleTurnState>()!,
                outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate, $"Rolling decision {index}");
        }
        Console.WriteLine($"NATIVE-ROLLING-FRONTIER decisions={roots.Length} actions={actions} endTurns={turns} unsupported={unsupported} truncated={limited}");
        Require(unsupported == 0 && limited == 0, "Recorded rolling roots contain unsupported or truncated alternatives.");

    }

    private static BattleTurnResult? FindEndTurn(BattleEndTurnFrontier frontier, IReadOnlyList<RelentlessDecision> choices) =>
        frontier.Candidates.FirstOrDefault(candidate => candidate.Decisions.Select(Choice)
            .SequenceEqual(choices.Select(Choice)))?.Result;

    private static (int Room, int Rounds, bool Continue) Choice(RelentlessDecision choice) =>
        (choice.RoomIndex, choice.CompletedRounds, choice.Continue);

    internal static void RecordedEndTurnFrontiers(FixtureValue fixture, int choiceBudget)
    {
        foreach (FixtureValue entry in fixture.GetProperty("Turns").EnumerateArray())
        {
            var before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            string label = $"EndTurn {before.Spawn.Turn} frontier";
            var frontier = BattleEndTurnModel.Enumerate(before, choiceBudget);
            if (frontier.UnsupportedAttempts.Count > 0)
                throw new InvalidOperationException(label + " has unsupported candidates: " +
                    string.Join(" | ", frontier.UnsupportedAttempts.GroupBy(attempt => attempt.Reason)
                        .Select(group => group.Count() + "x " + group.Key)));
            Require(!frontier.ReachedNodeLimit, label + $" reached the choice budget of {choiceBudget}.");
            var native = FindEndTurn(frontier, RecordedBattleChecks.Decisions(entry) ?? []);
            Require(native?.State != null && native.Outcome == entry.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
                label + " does not contain the recorded native outcome for the recorded choices.");
            RecordedBattleChecks.Compare(native!.State!, entry.GetProperty("Actual").Deserialize<BattleTurnState>()!,
                native.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated, label);
        }
    }

    internal static (BattleTurnState Root, BattleActionEnumeration Frontier, int Matched, int Unsupported)
        RecordedFrontiers(FixtureValue fixture, bool requireZeroUnsupported = false, bool report = true)
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
            if (requireZeroUnsupported && frontier.HasUnsupportedAttempts)
                throw new InvalidOperationException("Recorded decision " + index + " has unsupported candidates: " +
                    string.Join(" | ", frontier.UnsupportedAttempts.GroupBy(attempt => attempt.Reason).Select(group =>
                        group.Count() + "x " + group.Key + " action=" + JsonSerializer.Serialize(group.First().Action, ModelJson.Options))));
            SupportedActionCandidate? nativeCandidate = frontier.SupportedCandidates
                .FirstOrDefault(candidate => SameAction(candidate.Action, nativeAction, before));
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
            string predicted = BattleTurnChecks.Comparable(nativeCandidate.Result.State!, terminal);
            string captured = BattleTurnChecks.Comparable(nativeActual, terminal);
            Require(nativeCandidate.Result.Outcome == (RoomOutcome)entry.GetProperty("ActualOutcome").GetInt32() && predicted == captured,
                "Cached search successor for recorded native action " + index + " differs from the captured result: " +
                (predicted == captured ? "outcome" : ModelJson.Difference(predicted, captured)));
            if (index == 0)
            {
                root = before;
                rootFrontier = frontier;
            }
            matchedDecisions++;
            unsupportedAttempts += frontier.UnsupportedAttempts.Count;
        }

        if (requireZeroUnsupported) Require(unsupportedAttempts == 0, "Recorded decisions contain unsupported candidates.");
        if (report)
            Console.WriteLine($"NATIVE-RECORDED-FRONTIERS PASS: {matchedDecisions} exact native successors, {unsupportedAttempts} unsupported candidates.");
        return (root!, rootFrontier!, matchedDecisions, unsupportedAttempts);
    }

    internal static void Native(FixtureValue fixture, bool requireZeroUnsupported = false)
    {
        if (fixture.TryGetProperty("Boundary", out FixtureValue boundary) && boundary.ValueKind == FixtureKind.String &&
            boundary.GetString() == "NativeOriginalAffordableNonPlayable")
        {
            NativeLargeBudget(fixture);
            return;
        }
        var (root, rootFrontier, matchedDecisions, unsupportedAttempts) = RecordedFrontiers(fixture, requireZeroUnsupported);
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
        if (requireZeroUnsupported) Require(!serialSearch.HasUnsupportedAttempts && !search.HasUnsupportedAttempts,
            "Serial or parallel search encountered unsupported candidates.");
        foreach (var group in search.UnsupportedAttempts.GroupBy(attempt => attempt.Reason).Take(5))
            Console.WriteLine("NATIVE-SEARCH-BOUNDARY count=" + group.Count() + " reason=" + group.Key);
        Require(search.BestState != null && search.Score.HasValue && search.Steps.Count > 0,
            "Bounded search returned no result from the native battle decision state.");
        string Path(BattleSearchResult result) => string.Join("|", result.Steps.Select(step => step.IsEndTurn ?
            "EndTurn:" + JsonSerializer.Serialize(step.RelentlessDecisions, ModelJson.Options) :
            step.Action!.CardInstanceId + ":" + step.Action.RoomIndex + ":" + step.Action.PlayerPosition + ":" +
            step.Action.TargetUnitId + ":" + step.Action.ActivatorUnitId + ":" + string.Join(";",
                step.Action.RecursionSelections.Select(selection => selection.EffectIndex + "=" +
                    string.Join(",", selection.CardInstanceIds))) + ":" + string.Join(";",
                step.Action.MixSelections.Select(selection => selection.EffectIndex + "=" +
                    selection.MixNumber + "," + selection.ChoiceIndex)) + ":" + string.Join(";",
                step.Action.SummonSelections.Select(selection => selection.EffectIndex + "=" + selection.CardDataId))));
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
                BattleTurnResult result = BattleTurnModel.EndTurn(replayed, step.RelentlessDecisions);
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

    private static void NativeLargeBudget(FixtureValue fixture)
    {
        BattleTurnState root = fixture.GetProperty("Affordable").GetProperty("Before").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        int initialHand = root.Spawn.Train.Context!.Cards.Hand.Count;
        double Score(BattleTurnState state, RoomOutcome _) => state.Spawn.Train.Context!.Cards.Hand.Count - initialHand;
        Require(BattleActionModel.EnumerateSupportedPlays(root).Count >= 3, "The native root no longer exercises multiple child budgets.");
        BattleSearchResult baseline = Search(1024, 1);
        Require(!baseline.ReachedNodeLimit && !baseline.HasUnsupportedAttempts && baseline.Score == -2 && baseline.Steps.Count == 2,
            "The native root must have an exhaustible two-play optimum within the control budget.");
        foreach (int parallelism in new[] { 1, 4 })
        {
            BattleSearchResult search = Search(int.MaxValue, parallelism);
            Require(!search.ReachedNodeLimit && !search.HasUnsupportedAttempts && search.Score == baseline.Score &&
                search.DecisionStatesVisited == baseline.DecisionStatesVisited && search.Steps.Count == baseline.Steps.Count,
                "Maximum node budget dropped native branches at parallelism " + parallelism +
                $" (nodes {search.DecisionStatesVisited}/{baseline.DecisionStatesVisited}, score {search.Score}/{baseline.Score}).");
            BattleTurnState replayed = BattlePlannerChecks.Replay(root, search.Steps, out RoomOutcome outcome);
            Require(outcome == search.Outcome && keys.Key(replayed) == keys.Key(search.BestState!) &&
                keys.Key(search.BestState!) == keys.Key(baseline.BestState!), "Large-budget replay did not preserve native state and RNG.");
        }
        Require(keys.Key(root) == parent, "Large-budget search changed its native root.");
        Console.WriteLine($"NATIVE-LARGE-SEARCH-BUDGET PASS: {baseline.DecisionStatesVisited} decisions, two optimal plays, exact serial, parallel and bounded native replay.");

        BattleSearchResult Search(int maxNodes, int parallelism) => BattleSearchModel.Minimize(root, Score,
            new BattleSearchOptions(maxNodes, maxTurns: 0, maxDegreeOfParallelism: parallelism));
    }

    private static int PyreHealth(BattleTurnState state) => state.Spawn.Train.Rooms
        .SelectMany(room => room.Units).Where(unit => unit.IsPyre).Select(unit => unit.Health).DefaultIfEmpty(0).First();

    private static bool SameAction(PlayCardAction first, PlayCardAction second, BattleTurnState source) =>
        first.CardInstanceId == second.CardInstanceId && first.RoomIndex == second.RoomIndex &&
        SamePosition(first, second, source) && first.TargetUnitId == second.TargetUnitId &&
        first.ActivatorUnitId == second.ActivatorUnitId && first.ActivatorRoomIndex == second.ActivatorRoomIndex &&
        first.TargetSpawnPointIndex == second.TargetSpawnPointIndex && first.Operation == second.Operation &&
        first.RecursionSelections.Count == second.RecursionSelections.Count &&
        first.RecursionSelections.Zip(second.RecursionSelections).All(pair =>
            pair.First.EffectIndex == pair.Second.EffectIndex &&
            pair.First.CardInstanceIds.SequenceEqual(pair.Second.CardInstanceIds)) &&
        first.MixSelections.Count == second.MixSelections.Count &&
        first.MixSelections.Zip(second.MixSelections).All(pair =>
            pair.First.EffectIndex == pair.Second.EffectIndex &&
            pair.First.MixNumber == pair.Second.MixNumber &&
            pair.First.ChoiceIndex == pair.Second.ChoiceIndex) &&
        first.SummonSelections.Count == second.SummonSelections.Count &&
        first.SummonSelections.Zip(second.SummonSelections).All(pair =>
            pair.First.EffectIndex == pair.Second.EffectIndex && pair.First.CardDataId == pair.Second.CardDataId);

    private static bool SamePosition(PlayCardAction first, PlayCardAction second, BattleTurnState source)
    {
        if (first.PlayerPosition == second.PlayerPosition) return true;
        string? dataId = source.Spawn.Train.Context?.FindCard(first.CardInstanceId)?.DataId;
        if (source.PlayRules?.Cards.FirstOrDefault(rule => rule.DataId == dataId)?.Effect != "SpawnMonster") return false;
        // Native recording may omit the drop point. PlayCard resolves -1 to
        // append, while search enumerates its explicit position once. The full
        // cached successor is still compared against the native actual below.
        int append = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == first.RoomIndex).Units.Count(unit => unit.Team == CombatTeam.Player);
        return (first.PlayerPosition == -1 ? append : first.PlayerPosition) ==
            (second.PlayerPosition == -1 ? append : second.PlayerPosition);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
