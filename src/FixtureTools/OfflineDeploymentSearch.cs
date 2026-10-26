using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static partial class OfflineBattleSearch
{
    // Explore several simultaneous placement changes without requiring each
    // single move to improve a complete loss. Never export a truncated rollout.
    internal static void DeploymentGrid(string input, string prediction, string output, int milliseconds, int maxCandidates)
    {
        Require(milliseconds is >= 100 and <= 600000 && maxCandidates is >= 1 and <= 4096,
            "Expected100..600000ms and1..4096 deployment candidates.");
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        RequireNativeInput(source.RootElement);
        Verify(prediction);
        using var incumbent = FixtureDocument.Read(prediction);
        var fixture = incumbent.RootElement;
        Require(fixture.GetProperty("EvidenceKind").GetString() == "ModelPrediction" &&
            fixture.GetProperty("SourceSHA256").GetString() == Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            "Deployment baseline must bind the exact native archive.");
        var root = DeploymentRoot(source.RootElement);
        using var keys = new BattleStateKeys();
        string immutable = keys.Key(root);
        Require(root.Spawn.Turn == 0 && keys.Key(fixture.GetProperty("Root").Deserialize<BattleTurnState>()!) == immutable,
            "Deployment baseline changed the original native root.");
        var route = fixture.GetProperty("RollingDecisions").EnumerateArray().Select(row => new Step {
            Action = row.GetProperty("Action").Deserialize<PlayCardAction>(),
            IsEndTurn = row.GetProperty("IsEndTurn").GetBoolean() }).ToArray();
        var deployment = route.TakeWhile(step => !step.IsEndTurn).ToArray();
        Require(deployment.Length is >= 1 and <= 6 && deployment.All(step => step.Action is { ActivatorUnitId: 0 } action &&
            root.PlayRules!.Cards.Single(rule => rule.DataId == root.Spawn.Train.Context!.CardInstances!
                .Single(card => card.InstanceId == action.CardInstanceId).DataId).SpawnUnit != null),
            "Expected one to six initial unit plays before the first EndTurn.");
        var bestFinal = fixture.GetProperty("Final").Deserialize<BattleTurnState>()!;
        var bestOutcome = (RoomOutcome)fixture.GetProperty("Outcome").GetInt32();
        Require(bestOutcome == RoomOutcome.PlayerDefeated, "Deployment investigation requires a complete losing baseline.");
        double best = ImprovementMerit(bestFinal, bestOutcome), initial = best;
        var bestRoute = route;
        var watch = Stopwatch.StartNew();
        int candidates = 0, completed = 0, unsupported = 0, duplicates = 0, improvements = 0;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var reasons = new HashSet<string>(StringComparer.Ordinal);
        var prefix = new List<Step>();
        bool Expired() => watch.ElapsedMilliseconds >= milliseconds;
        bool Stop() => Expired() || candidates >= maxCandidates || bestOutcome == RoomOutcome.BattleWon;
        void Rejected(string? reason)
        {
            unsupported++;
            if (reasons.Add(reason ?? "Unknown")) Console.WriteLine("DEPLOYMENT-UNSUPPORTED " + reason);
        }
        void Visit(BattleTurnState state, int index)
        {
            if (Stop()) return;
            if (index < deployment.Length)
            {
                var choices = BattleActionModel.EnumerateActions(state, deployment[index].Action!.CardInstanceId, includeAbilities: false);
                foreach (var rejected in choices.UnsupportedAttempts) Rejected(rejected.Reason);
                string parent = keys.Key(state);
                foreach (var choice in choices.SupportedCandidates.OrderByDescending(choice => choice.Action.RoomIndex)
                    .ThenBy(choice => choice.Action.PlayerPosition))
                {
                    if (Stop()) break;
                    if (choice.Result.Outcome != RoomOutcome.Exchanged) continue;
                    prefix.Add(new Step { Action = choice.Action });
                    Visit(choice.Result.State!, index + 1);
                    prefix.RemoveAt(prefix.Count - 1);
                    Require(keys.Key(state) == parent, "Deployment child changed its parent.");
                }
                return;
            }
            if (!visited.Add(keys.Key(state))) { duplicates++; return; }
            candidates++;
            var steps = prefix.ToList();
            var current = state;
            var outcome = RoomOutcome.Exchanged;
            int played = deployment.Length;
            int floor = current.Spawn.Train.Rooms.FirstOrDefault(room => room.Units.Any(unit =>
                unit.Team == CombatTeam.Player && unit.Health > 0 && unit.Modifiers?.IsChampion == true))?.RoomIndex ?? 2;
            while (!Terminal(outcome) && steps.Count < 256 && !Expired())
            {
                SupportedActionCandidate? choice = null;
                if (played < 18)
                {
                    choice = BattleTacticalPolicy.Choose(current, out var omitted, floor);
                    foreach (var rejected in omitted) Rejected(rejected.Reason);
                }
                if (choice != null)
                {
                    current = choice.Result.State!; outcome = choice.Result.Outcome;
                    steps.Add(new Step { Action = choice.Action }); played++;
                }
                else
                {
                    var ended = BattleTurnModel.EndTurn(current);
                    if (!ended.Supported) { Rejected(ended.UnsupportedReason); return; }
                    current = ended.State!; outcome = ended.Outcome;
                    steps.Add(new Step { IsEndTurn = true }); played = 0;
                }
            }
            if (!Terminal(outcome)) return;
            completed++;
            double score = ImprovementMerit(current, outcome);
            if (score >= best) return;
            best = score; bestFinal = current; bestOutcome = outcome; bestRoute = steps.ToArray(); improvements++;
            Console.WriteLine($"DEPLOYMENT-IMPROVED candidate={candidates} outcome={outcome} steps={steps.Count} merit={best:R} " +
                "placements=" + JsonSerializer.Serialize(prefix.Select(step => step.Action)));
        }
        Visit(root, 0);
        Require(keys.Key(root) == immutable, "Deployment grid changed its native parent.");
        if (best < initial)
        {
            // Freeze independently reapplies every input from deployment and
            // verifies the full terminal state, forecasts and RNG before export.
            Freeze(input, JsonSerializer.Serialize(bestRoute), output);
            using var exported = FixtureDocument.Read(output);
            Require(keys.Key(exported.RootElement.GetProperty("Final").Deserialize<BattleTurnState>()!) == keys.Key(bestFinal),
                "Independent deployment export changed its terminal state/RNG.");
        }
        Console.WriteLine($"DEPLOYMENT-GRID foundWin={bestOutcome == RoomOutcome.BattleWon} units={deployment.Length} candidates={candidates} " +
            $"completed={completed} duplicates={duplicates} unsupported={unsupported} improvements={improvements} " +
            $"timedOut={Expired()} candidateLimit={candidates >= maxCandidates} elapsedMs={watch.Elapsed.TotalMilliseconds:F1} exported={best < initial}; model prediction only.");
    }
}
