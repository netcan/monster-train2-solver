using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class DeploymentAdviceBudgetChecks
{
    internal static void Run(FixtureValue fixture)
    {
        Require(fixture.GetProperty("EvidenceKind").GetString() == "LiveFirstTurnAdvice014" &&
            fixture.GetProperty("Diagnostic").GetProperty("LoadedVersion").GetString() == "0.2.14.0" &&
            fixture.GetProperty("Plan").GetProperty("Steps").EnumerateArray().All(step => step.GetProperty("IsEndTurn").GetBoolean()),
            "Missing actual published empty-tower losing advice.");
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 0 && root.Energy == 4 && root.Spawn.Train.Context!.Cards.Hand.Count == 4 &&
            root.PlayRules!.Cards.Any(rule => rule.AssetKey == "ImpQueenChampion"), "Changed actual native deployment.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        var options = new BattlePlannerOptions(256, beamWidth: 8, timeLimit: TimeSpan.FromSeconds(3),
            allowShorterHorizon: true, searchToBattleEnd: true);
        var plan = ParallelBattlePlanner.Plan(root, options, 2);
        Check(plan);
        Console.WriteLine($"DEPLOYMENT-ADVICE PASS first={Name(plan)} outcome={plan.Outcome} complete={plan.CompletedHorizon} partial={plan.UsedShorterHorizon} steps={plan.Steps.Count} turns={plan.TurnsCompleted} nodes={plan.ExpandedNodes} elapsedMs={plan.Elapsed.TotalMilliseconds:F0}");

        int scores = 0;
        var cold = BattlePlanner.Plan(root, keys.Key, new BattlePlannerOptions(128, beamWidth: 8,
            timeLimit: TimeSpan.FromMilliseconds(50), allowShorterHorizon: true, searchToBattleEnd: true), (state, outcome) =>
        {
            // A legal atomic candidate can return after the native cold/JIT
            // deadline. Retain its complete first turn, not an empty defeat.
            if (++scores == 2) Thread.Sleep(60);
            return BattlePlanner.Evaluate(state, outcome);
        });
        Check(cold);
        Require(cold.TimedOut && cold.UsedShorterHorizon && !cold.CompletedHorizon && cold.TurnsCompleted == 1,
            "A cold native budget did not retain an honest complete deployment window.");
        Require(keys.Key(root) == parent, "Deployment advice changed native root or RNG.");
        Console.WriteLine($"COLD-DEPLOYMENT PASS first={Name(cold)} partial=true fullStateRngReplay=true");

        var longer = ParallelBattlePlanner.Plan(root, new BattlePlannerOptions(256, beamWidth: 8,
            timeLimit: TimeSpan.FromSeconds(10), allowShorterHorizon: true, searchToBattleEnd: true), 2);
        Check(longer);
        Require(longer.CompletedHorizon && !longer.UsedShorterHorizon && longer.Outcome == RoomOutcome.BattleWon &&
            keys.Key(root) == parent, "The captured opening lost its verified winning route at the longer budget.");
        Console.WriteLine($"LONGER-DEPLOYMENT PASS budgetMs=10000 first={Name(longer)} outcome={longer.Outcome} steps={longer.Steps.Count} turns={longer.TurnsCompleted} nodes={longer.ExpandedNodes} elapsedMs={longer.Elapsed.TotalMilliseconds:F0} fullStateRngReplay=true");

        void Check(BattlePlanResult result)
        {
            Require(result.Steps.Count >= 2 && !result.Steps[0].IsEndTurn && result.Steps[0].Action != null,
                "A playable native opening still starts with empty EndTurn: " + JsonSerializer.Serialize(result.UnsupportedAttempts, ModelJson.Options));
            BattleAdviceSession.Verify(root, result);
            var after = BattleActionModel.PlayCard(root, result.Steps[0].Action!);
            Require(after.Supported && after.State!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0), "First action did not deploy a living ally.");
        }
        string Name(BattlePlanResult result) => root.PlayRules!.Cards.Single(rule => rule.DataId ==
            root.Spawn.Train.Context!.FindCard(result.Steps[0].Action!.CardInstanceId)!.DataId).AssetKey;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
