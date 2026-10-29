using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SurvivingPartialSelectionChecks
{
    internal static void Run(FixtureValue partialFixture, FixtureValue defeatFixture, FixtureValue nativeBattle)
    {
        Require(partialFixture.GetProperty("EvidenceKind").GetString() == "ModelRootPrediction" &&
            defeatFixture.GetProperty("EvidenceKind").GetString() == "ModelRootPrediction" &&
            partialFixture.GetProperty("SourceSHA256").GetString() == defeatFixture.GetProperty("SourceSHA256").GetString(),
            "Expected two model-only predictions from the same native battle.");
        var root = partialFixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var other = defeatFixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        Require(parent == keys.Key(other), "Predictions have different root state or RNG.");
        BattleTurnState Replay(FixtureValue fixture, out RoomOutcome outcome)
        {
            var steps = fixture.GetProperty("Plan").GetProperty("Steps").EnumerateArray()
                .Select(row => new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean())).ToArray();
            var replay = BattlePlannerChecks.Replay(root, steps, out outcome);
            Require(keys.Key(replay) == keys.Key(fixture.GetProperty("Plan").GetProperty("State").Deserialize<BattleTurnState>()!),
                "Historical prediction differs from independent state/RNG replay.");
            return replay;
        }
        Replay(partialFixture, out RoomOutcome partial);
        Replay(defeatFixture, out RoomOutcome defeat);
        Require(partial == RoomOutcome.Exchanged && defeat == RoomOutcome.PlayerDefeated &&
            partialFixture.GetProperty("Plan").GetProperty("UsedShorterHorizon").GetBoolean() &&
            defeatFixture.GetProperty("Plan").GetProperty("ReachedNodeLimit").GetBoolean(),
            "Expected a surviving partial forecast and a node-limited known defeat.");
        var options = new BattlePlannerOptions(256, 3, 8, 12, TimeSpan.FromSeconds(30),
            allowShorterHorizon: true, searchToBattleEnd: true, maxSteps: 256, stopAtBestPyreHealth: true);
        BattlePlanResult plan = ParallelBattlePlanner.Plan(root, options, 2);
        Require(plan.Outcome is RoomOutcome.Exchanged or RoomOutcome.BattleWon &&
            (plan.CompletedHorizon || plan.UsedShorterHorizon) && plan.Steps.Count > 0,
            "A known defeat displaced an available surviving complete-turn prefix.");
        if (plan.Outcome == RoomOutcome.Exchanged)
            Require(plan.UsedShorterHorizon && !plan.CompletedHorizon && plan.Steps.Last().IsEndTurn,
                "The surviving prefix must be labeled as an incomplete full-turn forecast.");
        var predicted = BattlePlannerChecks.Replay(root, plan.Steps, out RoomOutcome actual);
        Require(actual == plan.Outcome && keys.Key(predicted) == keys.Key(plan.State) && keys.Key(root) == parent,
            "Selected forecast did not independently replay or mutated its parent.");
        BattleAdviceSession.Verify(root, plan);
        var strictOptions = new BattlePlannerOptions(256, 3, 8, 12, TimeSpan.FromSeconds(30),
            allowShorterHorizon: false, searchToBattleEnd: true, maxSteps: 256);
        BattlePlanResult strict = ParallelBattlePlanner.Plan(root, strictOptions, 2);
        Require(!strict.UsedShorterHorizon && (!strict.CompletedHorizon || strict.Outcome != RoomOutcome.Exchanged),
            "Strict whole-battle search published an incomplete route as complete.");
        if (strict.CompletedHorizon) BattleAdviceSession.Verify(root, strict);
        Require(keys.Key(root) == parent, "Strict search mutated its parent.");
        FixtureValue finalDecision = nativeBattle.GetProperty("RollingDecisions").EnumerateArray().Last();
        Require(finalDecision.GetProperty("IsEndTurn").GetBoolean() &&
            finalDecision.GetProperty("Outcome").GetInt32() == (int)RoomOutcome.PlayerDefeated,
            "Missing native immediate-defeat boundary.");
        var finalRoot = finalDecision.GetProperty("Root").Deserialize<BattleTurnState>()!;
        string finalParent = keys.Key(finalRoot);
        var single = new BattlePlannerOptions(1, 3, 8, 12, TimeSpan.FromSeconds(30),
            allowShorterHorizon: true, searchToBattleEnd: true);
        BattlePlanResult unavoidable = BattlePlanner.Plan(finalRoot, keys.Key, single);
        Require(unavoidable.Outcome == RoomOutcome.PlayerDefeated && unavoidable.CompletedHorizon &&
            !unavoidable.UsedShorterHorizon && unavoidable.Steps.Count == 1 && unavoidable.Steps[0].IsEndTurn &&
            NativeBranchObservationChecks.Key(unavoidable.State, true) == NativeBranchObservationChecks.Key(finalDecision.GetProperty("Actual").Deserialize<BattleTurnState>()!, true) &&
            keys.Key(finalRoot) == finalParent, $"Immediate native defeat must not become a fabricated partial forecast: outcome={unavoidable.Outcome}, complete={unavoidable.CompletedHorizon}, partial={unavoidable.UsedShorterHorizon}, steps={unavoidable.Steps.Count}.");
        BattleAdviceSession.Verify(finalRoot, unavoidable);
        Console.WriteLine($"SURVIVING-PARTIAL PASS: same native root; historical node-limited defeat; selected={plan.Outcome}, turns={plan.TurnsCompleted}, nodes={plan.ExpandedNodes}; immediate native defeat preserved; full state/RNG replay.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
