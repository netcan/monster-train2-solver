using System.Text.Json;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelentlessBattleChecks
{
    internal static void NativePair(string abandonPath, string continuePath)
    {
        Native(abandonPath); Native(continuePath);
        using var abandoned = ModelJson.ReadFixture(abandonPath);
        using var continued = ModelJson.ReadFixture(continuePath);
        Require(abandoned.RootElement.GetProperty("RelentlessBattle").GetProperty("Abandon").GetBoolean() &&
            !continued.RootElement.GetProperty("RelentlessBattle").GetProperty("Abandon").GetBoolean(), "Reversed native policies.");
        BattleTurnState Decision(FixtureValue root) => root.GetProperty("Turns").EnumerateArray().Single(turn =>
            turn.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == 1)
            .GetProperty("Before").Deserialize<BattleTurnState>()!;
        RecordedBattleChecks.Compare(Decision(abandoned.RootElement), Decision(continued.RootElement), false,
            "independent native policies must start from the same full decision");
        Console.WriteLine("RELENTLESS-BATTLE-PAIR PASS: independent native Continue/Abandon policies share an identical complete pre-choice state.");
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Incomplete native Relentless victory.");
        FixtureValue evidence = root.GetProperty("RelentlessBattle");
        bool abandon = evidence.GetProperty("Abandon").GetBoolean();
        Require(evidence.GetProperty("Prepared").GetBoolean() && root.GetProperty("ModifierScenario").GetString() ==
            "relentless-battle-" + (abandon ? "abandon" : "continue"), "Missing explicit isolated policy.");
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        var dialogs = evidence.GetProperty("Dialogs").EnumerateArray().ToArray();
        Require(actions.Length == 1 && actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() ==
            evidence.GetProperty("ControlCardId").GetInt32() && turns.Length == 7 && dialogs.Length > 0,
            "Missing deployment, dialogs or complete seven-turn battle.");
        foreach (FixtureValue turn in turns)
        {
            int number = turn.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32();
            var expected = RecordedBattleChecks.Decisions(turn) ?? Array.Empty<RelentlessDecision>();
            var observed = dialogs.Where(dialog => dialog.GetProperty("Turn").GetInt32() == number).Select(dialog => new RelentlessDecision(
                dialog.GetProperty("RoomIndex").GetInt32(), dialog.GetProperty("CompletedRounds").GetInt32(), dialog.GetProperty("Continue").GetBoolean())).ToArray();
            Require(Serialize(expected) == Serialize(observed) && expected.All(choice =>
                choice.CompletedRounds % 3 == 0 && choice.Continue != abandon), "Native input sequence differs from recorded plan.");
        }
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); BattleTurnChecks.Native(root);
        RecordedBattleChecks.Native(root);
        FixtureValue first = turns.Single(turn => turn.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == 1);
        BattleTurnState before = first.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = first.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        bool nativeEnabled = evidence.GetProperty("RoomStates").EnumerateArray().Single(state => state.GetProperty("Turn").GetInt32() == 2)
            .GetProperty("Enabled")[0].GetBoolean();
        Require(nativeEnabled != abandon && after.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Player) != abandon,
            "Missing native floor-destruction/survival witness.");
        if (abandon)
        {
            var secondAscend = root.GetProperty("TrainPhases").EnumerateArray().Single(phase =>
                phase.GetProperty("Turn").GetInt32() == 2 && phase.GetProperty("Kind").GetString() == "Ascend");
            TrainCombatState source = secondAscend.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = secondAscend.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int[] reserved = source.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray();
            Require(reserved.Length == 2 && actual.Rooms[1].Units.Count == 0 &&
                actual.Rooms.SelectMany(room => room.Units).All(unit => !reserved.Contains(unit.Id)),
                "Missing destruction of lower-floor arrivals reserved before animation.");
        }
        Search(before, after, abandon);
        Console.WriteLine($"RELENTLESS-BATTLE PASS: {(abandon ? "Abandon and reserved-arrival destruction" : "Continue and unit survival")}, {dialogs.Length} native inputs, seven complete native turns, complete room/train/decision/RNG successors and 16 immutable full branches.");
    }
    private static void Search(BattleTurnState source, BattleTurnState native, bool abandon)
    {
        string parent = Serialize(source);
        BattleEndTurnFrontier frontier = BattleEndTurnModel.Enumerate(source, 64);
        Require(!frontier.ReachedNodeLimit && !frontier.Interrupted && frontier.UnsupportedAttempts.Count == 0 &&
            frontier.Candidates.Any(end => end.Decisions.Any(choice => !choice.Continue)) &&
            frontier.Candidates.Any(end => end.Decisions.Count > 0 && end.Decisions.All(choice => choice.Continue)),
            "Finite native dialog frontier was not completely enumerated.");
        BattleEndTurnCandidate recorded = frontier.Candidates.Single(end => abandon ? end.Decisions.Count == 1 &&
            !end.Decisions[0].Continue : end.Decisions.Count > 0 && end.Decisions.All(choice => choice.Continue));
        RecordedBattleChecks.Compare(recorded.Result.State!, native, false, "frontier successor versus actual native policy");
        double Score(BattleTurnState state, RoomOutcome _) => -100000 * state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Count(unit => unit.Team == CombatTeam.Player && !unit.IsPyre) + state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Enemy).Sum(unit => unit.Health);
        var serial = BattleSearchModel.Minimize(source, Score, new(64, 1, 1));
        var parallel = BattleSearchModel.Minimize(source, Score, new(64, 1, 2));
        foreach (var result in new[] { serial, parallel })
        {
            Require(result.BestState != null && !result.HasUnsupportedAttempts && result.Steps.Count == 1 &&
                result.Steps[0].RelentlessDecisions?.Count > 0 && result.Steps[0].RelentlessDecisions!.All(choice => choice.Continue),
                "Search did not prefer the fully continued native branch.");
            var replay = BattleTurnModel.EndTurn(source, result.Steps[0].RelentlessDecisions);
            Require(replay.Supported && Serialize(replay.State) == Serialize(result.BestState), "Search lost executable dialog choices.");
        }
        Require(Serialize(serial.BestState) == Serialize(parallel.BestState), "Serial/parallel native dialog search differs.");
        var exact = ExactBattleSolver.Minimize(source, Score, new(128, TimeSpan.FromSeconds(20), 1));
        Require(exact.CertifiedHorizonOptimal && exact.Steps.Count == 1 && exact.Steps[0].RelentlessDecisions!.All(choice => choice.Continue),
            "Exact solver failed the finite dialog frontier.");
        var exactReplay = BattlePlannerChecks.Replay(source, exact.Steps, out var exactOutcome);
        Require(Serialize(exactReplay) == Serialize(exact.State) && exactOutcome == exact.Outcome,
            "Common plan replay dropped the exact solver's dialog choices.");
        if (!abandon)
        {
            RelentlessDecision[] recordedChoices = recorded.Decisions.ToArray();
            var missing = BattleTurnModel.EndTurn(source, recordedChoices.SkipLast(1).ToArray());
            Require(missing.RelentlessChoice is { } request && request.RoomIndex == recordedChoices[^1].RoomIndex &&
                request.CompletedRounds == recordedChoices[^1].CompletedRounds,
                "A genuine choice prefix must still request the next native dialog.");
            RelentlessDecision[] trailing = [.. recordedChoices,
                new(recordedChoices[^1].RoomIndex, recordedChoices[^1].CompletedRounds + 3, true)];
            Reject(trailing, "EndTurn accepted a dialog after the last native choice.");
            Reject(recordedChoices.Reverse().ToArray(), "EndTurn accepted choices in reverse native order.");

            var belief = new BattleBelief([new BattleBeliefWorld(source, 0.25), new BattleBeliefWorld(source, 0.75)],
                true, "One native state with split probability mass");
            var policy = BattleBeliefPlanner.Plan(belief, new(64, 1, TimeSpan.FromSeconds(20)), Score);
            Require(policy.CertifiedFiniteHorizonOptimal && policy.Policy.IsEndTurn &&
                Serialize(policy.Policy.RelentlessDecisions) == Serialize(recorded.Decisions) &&
                policy.ExpandedWorlds == 2 + 2 * frontier.ChoiceNodes,
                "Belief search dropped the native Continue script or miscounted dialog expansions.");
            using var capture = NativeFixtureCapture.Capture(policy.Policy);
            var decoded = capture.RootElement.Deserialize<BattleBeliefPolicy>()!;
            var beliefReplay = BattleBeliefReplay.Verify(belief, decoded, Score);
            Require(beliefReplay.FinalWorlds.Count == 2 && beliefReplay.ExpectedScore == exact.Score,
                "The archived dialog policy changed the weighted result.");
            foreach (var world in beliefReplay.FinalWorlds)
                RecordedBattleChecks.Compare(world.State, native, false, "belief successor versus actual native policy");

            var limitedBelief = BattleBeliefPlanner.Plan(belief, new(4, 1, TimeSpan.FromSeconds(20)), Score);
            Require(limitedBelief.ExpandedWorlds == 4 && limitedBelief.ReachedNodeLimit && !limitedBelief.CertifiedFiniteHorizonOptimal &&
                limitedBelief.Policy.CompletedHorizon && limitedBelief.Policy.RelentlessDecisions is { Count: 1 } limitedDecisions &&
                !limitedDecisions[0].Continue, "A bounded common Abandon script was lost or certified as optimal.");
            BattleBeliefReplay.Verify(belief, limitedBelief.Policy, Score);
            Console.WriteLine("RELENTLESS-BELIEF PASS: typed native Continue and bounded shared Abandon, binary policy, budget accounting and full native replay.");

            void Reject(IReadOnlyList<RelentlessDecision> choices, string message)
            {
                var rejected = BattleTurnModel.EndTurn(source, choices);
                Require(!rejected.Supported && rejected.RelentlessChoice == null, message);
            }
        }
        using var keys = new BattleStateKeys();
        var plan = BattlePlanner.Plan(source, keys.Key, new(maxNodes: 128, maxTurns: 1, timeLimit: TimeSpan.FromSeconds(20)), Score);
        Require(plan.CompletedHorizon && plan.UnsupportedAttempts.Count == 0 && plan.Steps.Count == 1 &&
            plan.Steps[0].RelentlessDecisions!.All(choice => choice.Continue), "Production planner lost the native continued branch.");
        var portfolio = ParallelBattlePlanner.Plan(source, new(maxNodes: 128, maxTurns: 1, timeLimit: TimeSpan.FromSeconds(20)), 2);
        Require(portfolio.CompletedHorizon && portfolio.UnsupportedAttempts.Count == 0 && portfolio.Steps.Count > 0 &&
            portfolio.Steps[0].RelentlessDecisions?.Count > 0, "Parallel portfolio dropped explicit dialog inputs.");
        var portfolioReplay = BattlePlannerChecks.Replay(source, portfolio.Steps, out var portfolioOutcome);
        Require(Serialize(portfolioReplay) == Serialize(portfolio.State) && portfolioOutcome == portfolio.Outcome,
            "Common plan replay dropped the parallel portfolio's dialog choices.");
        Require(Serialize(source) == parent, "Native dialog search mutated its source.");
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
