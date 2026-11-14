using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelentlessSearchChecks
{
    internal static void NativeComponent(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var sample = document.RootElement.GetProperty("Samples").EnumerateArray().Single(sample =>
            sample.GetProperty("Label").GetString() == "abandon-three");
        BattleTurnState source = sample.GetProperty("DecisionBefore").Deserialize<BattleTurnState>()!;
        string parent = Serialize(source);
        var frontier = BattleEndTurnModel.Enumerate(source, 6);
        Require(frontier.Candidates.Count == 3 && frontier.ChoiceNodes == 6 && frontier.ReachedNodeLimit &&
            !frontier.Interrupted && frontier.UnsupportedAttempts.Count == 0, "Incomplete dialog frontier was hidden or miscounted.");
        for (int index = 0; index < frontier.Candidates.Count; index++)
        {
            var candidate = frontier.Candidates[index];
            Require(candidate.Decisions.Count == index + 1 && candidate.Decisions.Take(index).All(choice => choice.Continue) &&
                !candidate.Decisions.Last().Continue && candidate.Decisions.Last().CompletedRounds == 3 * (index + 1),
                "Dialog frontier changed input order.");
            BattleSearchStep step = JsonSerializer.Deserialize<BattleSearchStep>(Serialize(candidate.Step), ModelJson.Options)!;
            var replay = BattleTurnModel.EndTurn(source, step.RelentlessDecisions);
            Require(replay.Supported && ModelJson.Difference(Serialize(replay.State), Serialize(candidate.Result.State)) == null,
                "Serialized EndTurn plan lost its dialog inputs.");
        }
        var empty = BattleEndTurnModel.Enumerate(source, 0);
        Require(empty.ReachedNodeLimit && empty.ChoiceNodes == 0 && empty.Candidates.Count == 0, "Zero choice budget expanded a branch.");
        var cancelled = BattleEndTurnModel.Enumerate(source, 6, () => true);
        Require(cancelled.Interrupted && !cancelled.ReachedNodeLimit && cancelled.ChoiceNodes == 0, "Dialog cancellation was hidden.");
        double Score(BattleTurnState state, RoomOutcome _) => state.Spawn.Turn > source.Spawn.Turn ? 0 : 1000000;
        BattleSearchResult serial = BattleSearchModel.Minimize(source, Score, new(16, 1, 1));
        BattleSearchResult parallel = BattleSearchModel.Minimize(source, Score, new(16, 1, 2));
        foreach (var search in new[] { serial, parallel })
            Require(search.BestState != null && search.Steps.Count == 1 && search.Steps[0].IsEndTurn &&
                search.Steps[0].RelentlessDecisions?.Count == 1 && !search.Steps[0].RelentlessDecisions![0].Continue &&
                search.ReachedNodeLimit && !search.HasUnsupportedAttempts && !search.ExhaustedEnumeratedFrontier &&
                search.DecisionStatesVisited <= 16, "Search failed to return a bounded executable dialog plan.");
        Require(Serialize(serial.BestState) == Serialize(parallel.BestState), "Serial/parallel dialog search differs.");
        using var keys = new BattleStateKeys();
        var planned = BattlePlanner.Plan(source, keys.Key, new(maxNodes: 16, maxTurns: 1, timeLimit: TimeSpan.FromSeconds(20)), Score);
        Require(planned.CompletedHorizon && planned.ReachedNodeLimit && planned.ExpandedNodes <= 16 &&
            planned.Steps.Any(step => step.RelentlessDecisions?.Count > 0) && planned.UnsupportedAttempts.Count == 0,
            "Production planner did not preserve bounded dialog choices.");
        var exact = ExactBattleSolver.Minimize(source, Score, new(16, TimeSpan.FromSeconds(20), 1));
        Require(exact.ReachedNodeLimit && !exact.CertifiedHorizonOptimal && !exact.ExhaustedEnumeratedFrontier &&
            exact.ExpandedNodes <= 16 && exact.Steps.Count == 1 && exact.Steps[0].RelentlessDecisions?.Count > 0 &&
            exact.UnsupportedAttempts.Count == 0, "Truncated infinite continuation was incorrectly certified optimal.");
        Require(Serialize(source) == parent, "Dialog search mutated the original native decision.");
        Console.WriteLine("RELENTLESS-SEARCH PASS: typed dialog frontier, six charged choice nodes/three successors, zero budget/cancellation, serialized executable plans, bounded serial/parallel search and production planner; complete native post-Abandon turns remain separate.");
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
