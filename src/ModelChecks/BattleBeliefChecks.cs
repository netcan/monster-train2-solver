using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleBeliefChecks
{
    internal static void Native(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Unaccepted native belief source.");
        var turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        var root = turns.Select(turn => turn.GetProperty("Before").Deserialize<BattleTurnState>()!)
            .First(state => state.Cards().Draw.Count == 2);
        string parent = Json(root);
        var belief = DrawOrderBelief.Create(root, 8);
        Require(belief.ExhaustiveDistribution && belief.Worlds.Count == 2 && belief.Worlds.All(world => world.Weight == 0.5),
            "Two unknown cards need two equal-weight order hypotheses.");
        using var observed = new BattleObservationKeys(); using var complete = new BattleStateKeys();
        Require(belief.Worlds.Select(world => observed.Key(world.State)).Distinct().Count() == 1 &&
            belief.Worlds.Select(world => complete.Key(world.State)).Distinct().Count() == 2, "Hidden order leaked into public observations or vanished from full state.");
        var reversed = DrawOrderBelief.WithDrawOrder(root, root.Cards().Draw.Reverse().ToArray());
        var repeated = DrawOrderBelief.Create(reversed, 8);
        Require(belief.Worlds.Select(world => complete.Key(world.State)).SequenceEqual(repeated.Worlds.Select(world => complete.Key(world.State))),
            "Belief generation depends on the captured hidden order.");
        var plan = BattleBeliefPlanner.Plan(belief, new BattleBeliefOptions(128, 2, TimeSpan.FromSeconds(30)));
        Require(plan.Policy.CompletedHorizon && plan.CertifiedFiniteHorizonOptimal && !plan.Policy.IsLeaf && plan.Policy.IsEndTurn &&
            plan.Policy.Branches.Count == 2 && plan.Policy.Branches.All(branch => Math.Abs(branch.Probability - 0.5) < 1e-12),
            "Unknown draw did not produce a complete observable posterior policy: nodes=" + plan.ExpandedWorlds +
            " unsupported=" + string.Join(";", plan.UnsupportedReasons));
        var replayed = BattleBeliefReplay.Verify(belief, plan.Policy, decisionHorizon: 2);
        Require(replayed.FinalWorlds.Count == 2 && Math.Abs(replayed.ExpectedScore - plan.Policy.ExpectedScore) < 1e-6,
            "Policy probability/value failed independent two-world replay.");
        var live = BattleTurnModel.EndTurn(root);
        var native = turns.Single(turn => turn.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == root.Spawn.Turn)
            .GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(live.Supported && BattleTurnChecks.Comparable(live.State!) == BattleTurnChecks.Comparable(native),
            "The actual-order hypothesis did not reproduce the original native transition.");
        var badBranches = plan.Policy.Branches.Select((branch, index) => new BattleBeliefBranch(branch.Observation,
            index == 0 ? 0.75 : branch.Probability, branch.Policy)).ToArray();
        var corrupted = new BattleBeliefPolicy(plan.Policy.Action, plan.Policy.IsEndTurn, false, plan.Policy.ExpectedScore,
            true, plan.Policy.AllTerminal, badBranches);
        RejectPolicy(corrupted, "Independent replay accepted a corrupted posterior probability.");
        var wrongScore = new BattleBeliefPolicy(plan.Policy.Action, plan.Policy.IsEndTurn, false,
            plan.Policy.ExpectedScore + 1, true, plan.Policy.AllTerminal, plan.Policy.Branches);
        RejectPolicy(wrongScore, "Large terminal-score scale hid a one-health expected-value error.");
        foreach (double nonfinite in new[] { double.PositiveInfinity, double.NegativeInfinity, double.NaN })
        {
            var leaf = new BattleBeliefPolicy(null, false, true, nonfinite, true, false, []);
            RejectPolicy(leaf, "Independent replay accepted a nonfinite expected score.");
            var nonfiniteBranches = plan.Policy.Branches.Select((branch, index) => new BattleBeliefBranch(branch.Observation,
                index == 0 ? nonfinite : branch.Probability, branch.Policy)).ToArray();
            var invalidPosterior = new BattleBeliefPolicy(plan.Policy.Action, plan.Policy.IsEndTurn, false,
                plan.Policy.ExpectedScore, true, plan.Policy.AllTerminal, nonfiniteBranches);
            RejectPolicy(invalidPosterior, "Independent replay accepted a nonfinite posterior probability.");
        }
        var rare = new BattleBelief([new BattleBeliefWorld(belief.Worlds[0].State, 1e-12),
            new BattleBeliefWorld(belief.Worlds[1].State, 1)], true, "RareDrawOrder");
        var rarePolicy = BattleBeliefPlanner.Plan(rare, new BattleBeliefOptions(maxDecisions: 1)).Policy;
        Require(rarePolicy.Branches.Count == 2 && rarePolicy.Branches.Any(branch => branch.Probability < 1e-9),
            "The posterior range control needs a positive probability below replay tolerance.");
        BattleBeliefReplay.Verify(rare, rarePolicy, decisionHorizon: 1);
        RejectPolicy(WithProbability(branch => branch.Probability < .5 ? 0 : branch.Probability),
            "Replay accepted a zero-probability branch for an observed world.", horizon: 1, source: rare);
        RejectPolicy(WithProbability(branch => branch.Probability > .5 ? 1 + 1e-12 : branch.Probability),
            "Replay accepted a posterior probability above one.", horizon: 1, source: rare);
        // Unequal weights across many aliases exercise summation stability at
        // the terminal win/loss scale, without adding model expansions.
        var many = new BattleBelief(Enumerable.Range(1, 4096).Select(index =>
            new BattleBeliefWorld(root, index)).ToArray(), true, "WeightedAliasNumericalControl");
        var weighted = BattleBeliefPlanner.Plan(many, new BattleBeliefOptions(maxDecisions: 0),
            (_, _) => -1e12 - 104);
        BattleBeliefReplay.Verify(many, weighted.Policy, decisionHorizon: 0, evaluate: (_, _) => -1e12 - 104);
        Require(Math.Abs(weighted.Policy.ExpectedScore - (-1e12 - 104)) < 0.01,
            "Many weighted hypotheses lost terminal health precision.");
        var sampled = DrawOrderBelief.Create(root, 1);
        var estimate = BattleBeliefPlanner.Plan(sampled, new BattleBeliefOptions(64, 1, TimeSpan.FromSeconds(15)));
        Require(!sampled.ExhaustiveDistribution && !estimate.CertifiedFiniteHorizonOptimal,
            "Sampled draw order acquired an exact distribution certificate.");
        var cancelled = BattleBeliefPlanner.Plan(belief, cancellationToken: new CancellationToken(true));
        Require(cancelled.Cancelled && cancelled.ExpandedWorlds == 0 && !cancelled.CertifiedFiniteHorizonOptimal, "Belief cancellation certified a result.");
        var expired = BattleBeliefPlanner.Plan(belief, new BattleBeliefOptions(timeLimit: TimeSpan.Zero));
        Require(expired.TimedOut && expired.Policy.IsLeaf && !expired.Policy.CompletedHorizon && !expired.Policy.AllTerminal &&
            !expired.CertifiedFiniteHorizonOptimal, "Belief time cutoff certified a result.");
        BattleBeliefReplay.Verify(belief, expired.Policy, decisionHorizon: 3);
        var falseTerminal = new BattleBeliefPolicy(null, false, true, expired.Policy.ExpectedScore, false, true, []);
        RejectPolicy(falseTerminal, "Replay accepted a false terminal claim.");
        var falseHorizon = new BattleBeliefPolicy(null, false, true, expired.Policy.ExpectedScore, true, false, []);
        RejectPolicy(falseHorizon, "Replay accepted a premature horizon claim.", horizon: 3);
        RejectPolicy(plan.Policy, "Replay accepted an action beyond the declared horizon.", horizon: 0);

        var terminalState = turns[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var terminalOutcome = turns[^1].GetProperty("ActualOutcome").Deserialize<RoomOutcome>();
        Require(terminalOutcome == RoomOutcome.BattleWon, "The terminal-policy check requires a recorded native win.");
        var terminalBelief = new BattleBelief([new BattleBeliefWorld(terminalState, 1, terminalOutcome)], true, "NativeVictory");
        var finished = BattleBeliefPlanner.Plan(terminalBelief, new BattleBeliefOptions(maxDecisions: 1));
        BattleBeliefReplay.Verify(terminalBelief, finished.Policy, decisionHorizon: 1);
        RejectPolicy(plan.Policy, "Replay attempted to execute an action after the recorded win.", horizon: 2,
            source: terminalBelief, reason: "Policy takes an action after a terminal outcome.");

        var limited = BattleBeliefPlanner.Plan(belief, new BattleBeliefOptions(1, 2));
        Require(limited.ReachedNodeLimit && limited.ExpandedWorlds == 1 && !limited.CertifiedFiniteHorizonOptimal,
            "Belief world expansion escaped the common node budget.");
        Nonanticipation(fixture.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!);
        Require(parent == Json(root), "Belief search mutated the native parent.");
        Console.WriteLine($"BELIEF-BATTLE PASS: two exact hidden draw orders, {plan.ExpandedWorlds} shared expansions, observable posterior branches, " +
            "independent weighted replay, terminal/horizon guards, sampling/cutoff guards and non-anticipating unequal-probability action choice.");

        BattleBeliefPolicy WithProbability(Func<BattleBeliefBranch, double> probability) => new(rarePolicy.Action,
            rarePolicy.IsEndTurn, false, rarePolicy.ExpectedScore, true, rarePolicy.AllTerminal,
            rarePolicy.Branches.Select(branch => new BattleBeliefBranch(branch.Observation, probability(branch), branch.Policy)).ToArray(),
            rarePolicy.RelentlessDecisions);

        void RejectPolicy(BattleBeliefPolicy policy, string message, int? horizon = null, BattleBelief? source = null,
            string? reason = null)
        {
            try
            {
                if (horizon.HasValue) BattleBeliefReplay.Verify(source ?? belief, policy, horizon.Value);
                else BattleBeliefReplay.Verify(source ?? belief, policy);
            }
            catch (InvalidOperationException error) when (reason == null || error.Message == reason) { return; }
            throw new InvalidOperationException(message);
        }
    }
    private static void Nonanticipation(BattleTurnState root)
    {
        var draw = root.Cards().Draw.ToArray(); Require(draw.Length > 1, "Need hidden draw order.");
        int hidden = draw[0].InstanceId;
        var reverse = DrawOrderBelief.WithDrawOrder(root, draw.Reverse().ToArray());
        var sampled = DrawOrderBelief.Create(root, 4);
        var shuffledSource = DrawOrderBelief.Create(reverse, 4);
        using var fingerprints = new BattleStateKeys();
        Require(!sampled.ExhaustiveDistribution && sampled.Worlds.Select(world => fingerprints.Key(world.State))
            .SequenceEqual(shuffledSource.Worlds.Select(world => fingerprints.Key(world.State))),
            "Sampling depends on the captured hidden order.");
        var ids = BattleActionModel.EnumerateActions(root).SupportedActions.Where(action => root.Cards().Hand.Any(card => card.InstanceId == action.CardInstanceId))
            .Select(action => action.CardInstanceId).Distinct().Take(2).ToArray();
        Require(ids.Length == 2, "Need two real native card choices for non-anticipation control.");
        int[] plays = ids.Select(id => root.Spawn.Train.Context!.FindCard(id)!.PlayCount).ToArray();
        double Cost(BattleTurnState state, RoomOutcome _)
        {
            int chosen = Array.FindIndex(ids, id => state.Spawn.Train.Context!.FindCard(id)!.PlayCount > plays[Array.IndexOf(ids, id)]);
            if (chosen < 0) return 10;
            bool firstWorld = state.Cards().Draw.Count > 0 && state.Cards().Draw[0].InstanceId == hidden;
            return chosen == (firstWorld ? 0 : 1) ? 0 : 10;
        }
        var worlds = new[] { new BattleBeliefWorld(root, 1), new BattleBeliefWorld(reverse, 3) };
        var joint = new BattleBelief(worlds, true, "TwoExplicitWeightedHypotheses");
        var policy = BattleBeliefPlanner.Plan(joint, new BattleBeliefOptions(16, 1, TimeSpan.FromSeconds(30)), Cost);
        Require(policy.CertifiedFiniteHorizonOptimal && policy.Policy.Action?.CardInstanceId == ids[1] &&
            Math.Abs(policy.Policy.ExpectedScore - 2.5) < 1e-12, "Policy exploited hidden order or ignored unequal prior weights.");
        BattleBeliefReplay.Verify(joint, policy.Policy, Cost);
        foreach (var world in worlds)
        {
            var oracle = BattleBeliefPlanner.Plan(new BattleBelief([world], true, "ClairvoyantControl"),
                new BattleBeliefOptions(16, 1, TimeSpan.FromSeconds(15)), Cost);
            Require(oracle.CertifiedFiniteHorizonOptimal && oracle.Policy.ExpectedScore == 0, "Missing clairvoyant zero-cost control.");
        }
    }
    private static CardCycleState Cards(this BattleTurnState state) => state.Spawn.Train.Context!.Cards;
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
