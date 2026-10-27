using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ParallelOpeningPortfolioChecks
{
    internal static void Lower(FixtureValue previous, FixtureValue knownRoute)
    {
        Require(previous.GetProperty("EvidenceKind").GetString() == "ModelRootPrediction" &&
            previous.GetProperty("NativeValidationRequired").GetBoolean() && knownRoute.GetProperty("EvidenceKind").GetString() == "ModelPrediction" &&
            previous.GetProperty("SourceSHA256").GetString() == knownRoute.GetProperty("SourceSHA256").GetString(),
            "Lower-floor controls must retain model-only evidence from one native deployment.");
        var root = previous.GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        Require(root.Spawn.Turn == 0 && parent == keys.Key(knownRoute.GetProperty("Root").Deserialize<BattleTurnState>()!) &&
            previous.GetProperty("Milliseconds").GetInt32() == 10000 && previous.GetProperty("RequestedMaxNodes").GetInt32() == 256 &&
            previous.GetProperty("RequestedWorkers").GetInt32() == 2 &&
            previous.GetProperty("Plan").GetProperty("Outcome").GetInt32() == (int)RoomOutcome.PlayerDefeated &&
            knownRoute.GetProperty("Outcome").GetInt32() == (int)RoomOutcome.BattleWon,
            "Missing old default-options defeat and complete lower-floor model win.");
        var oldSteps = previous.GetProperty("Plan").GetProperty("Steps").EnumerateArray().Select(step => new BattleSearchStep(
            step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        var old = BattlePlannerChecks.Replay(root, oldSteps, out RoomOutcome oldOutcome);
        Require(oldOutcome == RoomOutcome.PlayerDefeated && keys.Key(old) == keys.Key(previous.GetProperty("Plan").GetProperty("State").Deserialize<BattleTurnState>()!) &&
            old.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsBoss == true).Health == 730,
            "Changed original complete defeat/Rax730 witness.");
        var route = knownRoute.GetProperty("RollingDecisions").EnumerateArray().Select(step => new BattleSearchStep(
            step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        var winning = BattlePlannerChecks.Replay(root, route, out RoomOutcome winningOutcome);
        Require(winningOutcome == RoomOutcome.BattleWon && route.Length == 59 &&
            keys.Key(winning) == keys.Key(knownRoute.GetProperty("Final").Deserialize<BattleTurnState>()!) &&
            winning.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 46,
            "Changed lower-floor full state/RNG and Pyre46 bound.");
        var options = new BattlePlannerOptions(256, timeLimit: TimeSpan.FromSeconds(10), allowShorterHorizon: true,
            searchToBattleEnd: true, stopAtBestPyreHealth: true);
        var result = ParallelBattlePlanner.Plan(root, options);
        int pyre = result.State.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0;
        Console.WriteLine($"LOWER-OPENING result={result.Outcome} pyre={pyre} nodes={result.ExpandedNodes} elapsedMs={result.Elapsed.TotalMilliseconds:F1}");
        Require(result.Outcome == RoomOutcome.BattleWon && pyre >= 46 && result.SearchWorkers == 2 && result.ExpandedNodes <= 256,
            "Default two-worker opening lost the lower-floor model win or shared budget.");
        var replayed = BattlePlannerChecks.Replay(root, result.Steps, out RoomOutcome outcome);
        Require(outcome == result.Outcome && keys.Key(replayed) == keys.Key(result.State) && keys.Key(root) == parent,
            "Diversified opening differs from complete state/RNG or mutated root.");
        Console.WriteLine("LOWER-OPENING-PORTFOLIO PASS: same native deployment, original Rax730 model defeat to Pyre46 win under10000ms/256 shared nodes/two workers; immutable independent full-state/RNG replay; native validation required.");
    }
    internal static void Run(FixtureValue nativeRoot, FixtureValue knownRoute)
    {
        Require(nativeRoot.GetProperty("EvidenceKind").GetString() == "ModelRootPrediction" &&
            nativeRoot.GetProperty("NativeValidationRequired").GetBoolean() && knownRoute.GetProperty("EvidenceKind").GetString() == "ModelPrediction" &&
            nativeRoot.GetProperty("SourceSHA256").GetString() == knownRoute.GetProperty("SourceSHA256").GetString(),
            "Opening controls require explicitly model-only witnesses from one native capture.");
        var root = nativeRoot.GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        Require(parent == keys.Key(knownRoute.GetProperty("Root").Deserialize<BattleTurnState>()!) &&
            root.Spawn.Turn == 0 && !root.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0),
            "Opening controls must retain an empty natural deployment root/RNG.");
        int Pyre(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
        int target = Pyre(knownRoute.GetProperty("Final").Deserialize<BattleTurnState>()!);
        Require(nativeRoot.GetProperty("Milliseconds").GetInt32() == 10000 && nativeRoot.GetProperty("RequestedMaxNodes").GetInt32() == 256 &&
            nativeRoot.GetProperty("RequestedWorkers").GetInt32() == 2 && nativeRoot.GetProperty("Plan").GetProperty("Outcome").GetInt32() == (int)RoomOutcome.BattleWon &&
            Pyre(nativeRoot.GetProperty("Plan").GetProperty("State").Deserialize<BattleTurnState>()!) == 5 &&
            knownRoute.GetProperty("Outcome").GetInt32() == (int)RoomOutcome.BattleWon && target == 37,
            "Known policy target changed; this is a model quality bound, not a native victory.");
        var options = new BattlePlannerOptions(256, timeLimit: TimeSpan.FromSeconds(10), allowShorterHorizon: true,
            searchToBattleEnd: true, stopAtBestPyreHealth: true);
        var result = ParallelBattlePlanner.Plan(root, options);
        Console.WriteLine($"OPENING-PORTFOLIO result={result.Outcome} pyre={Pyre(result.State)} target={target} nodes={result.ExpandedNodes} elapsedMs={result.Elapsed.TotalMilliseconds:F1}");
        Require(result.Outcome == RoomOutcome.BattleWon && Pyre(result.State) >= target,
            "The default two-worker opening portfolio lost the known higher-Pyre policy route.");
        var replay = BattlePlannerChecks.Replay(root, result.Steps, out RoomOutcome outcome);
        Require(outcome == RoomOutcome.BattleWon && keys.Key(replay) == keys.Key(result.State) &&
            result.ExpandedNodes <= 256 && result.SearchWorkers == 2 && keys.Key(root) == parent,
            "Opening portfolio changed full state/RNG, exceeded its shared nodes or lost separate workers.");
        Console.WriteLine("PARALLEL-OPENING-PORTFOLIO PASS: actual10000ms/256-node/two-worker options retain the known Pyre37 model bound, independently replayed full state/RNG and immutable native root; native validation required.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
