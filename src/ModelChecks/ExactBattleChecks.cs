using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ExactBattleChecks
{
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
        Require(Json(root) == parent, "Exact solver mutated its native parent.");
        Console.WriteLine("EXACT-BATTLE PASS: native terminal optimality certificate, complete state/RNG and independent replay; " +
            "node/time/cancellation/unsupported/horizon certificates distinguished.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
