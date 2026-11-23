using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ExactBattleChecks
{
    internal static void NativeMultiTurn(string workspace)
    {
        string directory = Game231FixtureCatalog.DirectoryFor(workspace);
        var registration = Game231FixtureCatalog.Load(directory)["mechanics/tenacious/full-battle.mt2f"];
        using var document = FixtureDocument.Read(registration.Verify(directory));
        FixtureValue fixture = document.RootElement;
        Require(Game231FixtureCatalog.Classify(registration, fixture) == Game231FixtureKind.SettledBattle &&
            fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            fixture.GetProperty("NativeWon").GetBoolean(), "Expected the registered 2.3.1 Tenacious victory.");

        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().TakeLast(2).ToArray();
        if (turns.Length != 2) throw new InvalidOperationException("The native Tenacious suffix requires two EndTurns.");
        BattleTurnState root = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        int firstTurn = root.Spawn.Turn;
        Require(RecordedBattleChecks.DecisionTurn(turns[1]) == firstTurn + 1 &&
            fixture.GetProperty("Actions").EnumerateArray().All(row => RecordedBattleChecks.DecisionTurn(row) < firstTurn),
            "The Tenacious suffix must contain two consecutive native EndTurns without intervening plays.");
        int[] legalPlays = [LegalPlays(root), LegalPlays(turns[1].GetProperty("Before").Deserialize<BattleTurnState>()!)];
        Require(legalPlays.All(count => count >= 2), "Both native turns must have multiple supported plays.");
        RecordedBattleChecks.ReplayOnce(fixture, firstTurn);

        string parent = Json(root);
        var options = new ExactBattleOptions(4096, TimeSpan.FromSeconds(45));
        var result = ExactBattleSolver.Minimize(root, options: options);
        Require(result.CertifiedTerminalOptimal && result.TerminalLeaves > 1 && result.Outcome == RoomOutcome.BattleWon &&
            result.Steps.Count(step => step.IsEndTurn) == 2,
            $"The two-turn Tenacious search did not exhaust its supported terminal frontier (nodes={result.ExpandedNodes}, " +
            $"terminals={result.TerminalLeaves}, unsupported={result.UnsupportedAttempts.Count}).");
        BattleTurnState replay = BattlePlannerChecks.Replay(root, result.Steps, out RoomOutcome outcome);
        Require(outcome == result.Outcome && Json(replay) == Json(result.State) && Json(root) == parent,
            "The exact two-turn plan failed independent replay or changed its root.");
        var native = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RecordedBattleChecks.Compare(replay, native, terminal: true, "Exact Tenacious terminal");
        Console.WriteLine($"EXACT-NATIVE-MULTI-TURN PASS turn={firstTurn} legalPlays={string.Join("/", legalPlays)} " +
            $"nodes={result.ExpandedNodes} duplicates={result.DuplicateStates} terminals={result.TerminalLeaves} " +
            $"steps={result.Steps.Count} score={result.Score} seconds={result.Elapsed.TotalSeconds:F3}");

        NativeParallelEndgame(directory);

        static int LegalPlays(BattleTurnState state)
        {
            var frontier = BattleActionModel.EnumerateActions(state);
            Require(!frontier.HasUnsupportedAttempts, "The recorded Tenacious decision has unsupported plays.");
            return frontier.SupportedCandidates.Count;
        }
    }

    private static void NativeParallelEndgame(string directory)
    {
        const string path = "mechanics/player-advisor-rolling/unlocked-covenant0-ring3-siphon-trial.mt2f";
        var registration = Game231FixtureCatalog.Load(directory)[path];
        using var document = FixtureDocument.Read(registration.Verify(directory));
        var root = document.RootElement.GetProperty("Turns")[7].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = Json(root);
        var options = new BattlePlannerOptions(256, beamWidth: 8, timeLimit: TimeSpan.FromSeconds(25),
            allowShorterHorizon: true, searchToBattleEnd: true, stopAtBestPyreHealth: true);
        var plan = ParallelBattlePlanner.Plan(root, options);
        var replay = BattlePlannerChecks.Replay(root, plan.Steps, out var outcome);
        int pyre = replay.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
        Require(outcome == RoomOutcome.BattleWon && plan.CompletedHorizon && (plan.SearchWorkers == 1 || pyre >= 37) &&
            plan.ExpandedNodes <= options.MaxNodes && Json(replay) == Json(plan.State) && Json(root) == parent,
            $"Parallel endgame lost a known native-root route (health={pyre}, nodes={plan.ExpandedNodes}).");
        var shortSearch = ExactBattleSolver.MinimizeCore(root, null, new ExactBattleOptions(64),
            CancellationToken.None, maxSteps: 1, maxActionsPerTurn: 1);
        Require(shortSearch.ReachedStepLimit && !shortSearch.ExhaustedEnumeratedFrontier && shortSearch.Steps.Count <= 1,
            "The terminal traversal exceeded its step limit or certified the restricted frontier.");
        Console.WriteLine($"PARALLEL-NATIVE-ENDGAME PASS turn={root.Spawn.Turn} pyre={pyre} nodes={plan.ExpandedNodes} steps={plan.Steps.Count}");
    }

    internal static void Native(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Unaccepted native source: " + counter);
        var turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        BattleTurnState? root = null; FixtureValue witness = default;
        foreach (var turn in turns.Reverse())
        {
            var state = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var frontier = BattleActionModel.EnumerateActions(state);
            Console.WriteLine($"EXACT-NATIVE-ROOT turn={state.Spawn.Turn} actions={frontier.SupportedCandidates.Count} unsupported={frontier.UnsupportedAttempts.Count}");
            if (frontier.SupportedCandidates.Count == 0 && !frontier.HasUnsupportedAttempts &&
                (RoomOutcome)turn.GetProperty("ActualOutcome").GetInt32() is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated)
            { root = state; witness = turn; break; }
        }
        Require(root != null, "Fixture needs an accepted terminal native decision without remaining legal plays.");
        string parent = Json(root);
        var result = ExactBattleSolver.Minimize(root!, options: new ExactBattleOptions(64, TimeSpan.FromSeconds(15)));
        Require(result.CertifiedTerminalOptimal && !result.CertifiedHorizonOptimal && result.ExpandedNodes == 1 &&
            result.TerminalLeaves == 1 && result.Steps.Count == 1 && result.Steps[0].IsEndTurn,
            "Exhaustive native terminal frontier did not produce a certificate.");
        var actual = witness.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(result.Outcome == (RoomOutcome)witness.GetProperty("ActualOutcome").GetInt32() &&
            BattleTurnChecks.Comparable(result.State, true) == BattleTurnChecks.Comparable(actual, true),
            "Certified terminal plan differs from native full state/RNG.");
        var replay = BattlePlannerChecks.Replay(root!, result.Steps, out var outcome);
        Require(Json(replay) == Json(result.State) && outcome == result.Outcome, "Exact plan failed independent replay.");
        var branchRoot = fixture.GetProperty("Actions").EnumerateArray().Last().GetProperty("Before").Deserialize<BattleTurnState>()!;
        var branchFrontier = BattleActionModel.EnumerateActions(branchRoot);
        Console.WriteLine($"EXACT-NATIVE-BRANCH turn={branchRoot.Spawn.Turn} actions={branchFrontier.SupportedCandidates.Count}");
        string branchParent = Json(branchRoot);
        var branched = ExactBattleSolver.Minimize(branchRoot, options: new ExactBattleOptions(4096, TimeSpan.FromSeconds(30), 1));
        Require(branchFrontier.SupportedCandidates.Count > 0 && branched.CertifiedHorizonOptimal && !branched.CertifiedTerminalOptimal &&
            branched.TerminalLeaves > 1 && branched.HorizonLeaves > 0 && branched.Outcome == RoomOutcome.BattleWon &&
            branched.Score <= BattlePlanner.Evaluate(actual, result.Outcome), "Nontrivial native decision did not exhaust its horizon frontier: nodes=" +
            branched.ExpandedNodes + " terminals=" + branched.TerminalLeaves + " unsupported=" + branched.UnsupportedAttempts.Count);
        var branchedReplay = BattlePlannerChecks.Replay(branchRoot, branched.Steps, out var branchOutcome);
        Require(Json(branchedReplay) == Json(branched.State) && branchOutcome == branched.Outcome && Json(branchRoot) == branchParent,
            "Exact choice failed independent replay or mutated its root.");
        Console.WriteLine($"EXACT-NATIVE-CERTIFICATE nodes={branched.ExpandedNodes} terminals={branched.TerminalLeaves} " +
            $"duplicates={branched.DuplicateStates} steps={branched.Steps.Count} score={branched.Score}");
        var zero = ExactBattleSolver.Minimize(root!, options: new ExactBattleOptions(timeLimit: TimeSpan.Zero));
        Require(zero.TimedOut && zero.ExpandedNodes == 0 && !zero.ExhaustedEnumeratedFrontier && !zero.CertifiedTerminalOptimal,
            "An expired budget incorrectly certified a result.");
        var cancelled = ExactBattleSolver.Minimize(root!, cancellationToken: new CancellationToken(true));
        Require(cancelled.Cancelled && cancelled.ExpandedNodes == 0 && !cancelled.CertifiedTerminalOptimal,
            "Pre-cancellation incorrectly certified a result.");
        var horizon = ExactBattleSolver.Minimize(root!, options: new ExactBattleOptions(turnHorizon: 0));
        Require(horizon.CertifiedHorizonOptimal && !horizon.CertifiedTerminalOptimal && horizon.HorizonLeaves == 1,
            "A bounded horizon was mislabeled as terminal optimality.");
        var missing = new BattleTurnState(root!.Spawn, root.Energy, root.EnergyPerTurn, root.DrawPerTurn,
            root.ForgePoints, root.DragonsHoard, root.MoonPhase, root.RngStreams, root.OtherPiles,
            root.ExternalInteractions, playRules: null);
        var unsupported = ExactBattleSolver.Minimize(missing, options: new ExactBattleOptions(8, TimeSpan.FromSeconds(5), 1));
        Require(unsupported.UnsupportedAttempts.Count > 0 && !unsupported.ExhaustedEnumeratedFrontier &&
            !unsupported.CertifiedTerminalOptimal && !unsupported.CertifiedHorizonOptimal,
            "Missing action definitions incorrectly produced a certificate.");
        // A recorded earlier decision requires additional work after one shared
        // expansion. A node-limited witness must never acquire a certificate.
        var earlier = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var limited = ExactBattleSolver.Minimize(earlier, options: new ExactBattleOptions(1, TimeSpan.FromSeconds(15)));
        Require(limited.ReachedNodeLimit && limited.ExpandedNodes == 1 && !limited.CertifiedTerminalOptimal &&
            !limited.CertifiedHorizonOptimal, "Node cutoff was ignored or certified.");
        CheckDeadline(root!, null, result.Outcome);
        CheckDeadline(earlier, 1, RoomOutcome.Exchanged);
        Require(Json(root) == parent, "Exact solver mutated its native parent.");
        Console.WriteLine("EXACT-BATTLE PASS: native terminal optimality certificate, complete state/RNG and independent replay; " +
            "node/time/cancellation/unsupported/horizon certificates distinguished, with completed leaves retained at cutoff.");

        static void CheckDeadline(BattleTurnState source, int? horizon, RoomOutcome expected)
        {
            int checks = 0;
            // These recorded EndTurns resolve directly. Expire the budget at
            // the first check after the root expansion resolves its successors.
            var expired = ExactBattleSolver.MinimizeCore(source, null, new ExactBattleOptions(64, turnHorizon: horizon),
                CancellationToken.None, () => ++checks > 1);
            Require(expired.TimedOut && !expired.Cancelled && expired.ExpandedNodes == 1 &&
                expired.Outcome == expected && expired.Steps.Count == 1 && expired.Steps[0].IsEndTurn &&
                expired.TerminalLeaves == (horizon.HasValue ? 0 : 1) && expired.HorizonLeaves == (horizon.HasValue ? 1 : 0) &&
                !expired.ExhaustedEnumeratedFrontier && !expired.CertifiedTerminalOptimal && !expired.CertifiedHorizonOptimal,
                "Exact cutoff discarded a completed native leaf or certified an interrupted frontier.");
            var replay = BattlePlannerChecks.Replay(source, expired.Steps, out var outcome);
            Require(outcome == expected && Json(replay) == Json(expired.State), "Exact cutoff result failed complete-state replay.");
        }
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
