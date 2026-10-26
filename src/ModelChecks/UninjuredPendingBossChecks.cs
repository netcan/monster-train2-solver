using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UninjuredPendingBossChecks
{
    internal static void Run(FixtureValue early, FixtureValue late)
    {
        Require(early.GetProperty("EvidenceKind").GetString() == "ModelRootPrediction" &&
            early.GetProperty("NativeValidationRequired").GetBoolean() &&
            late.GetProperty("EvidenceKind").GetString() == "ModelPrediction" &&
            early.GetProperty("SourceSHA256").GetString() == late.GetProperty("SourceSHA256").GetString(),
            "Both loss witnesses must remain predictions from the same native archive.");
        using var keys = new BattleStateKeys();
        BattleTurnState root = early.GetProperty("Root").Deserialize<BattleTurnState>()!;
        string parent = keys.Key(root);
        Require(keys.Key(late.GetProperty("Root").Deserialize<BattleTurnState>()!) == parent,
            "Uninjured boss witnesses changed the deployment root/RNG.");
        var earlyPlan = early.GetProperty("Plan");
        BattleSearchStep[] Read(FixtureValue rows) => rows.EnumerateArray().Select(row => new BattleSearchStep(
            row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        var shortSteps = Read(earlyPlan.GetProperty("Steps"));
        var longSteps = Read(late.GetProperty("RollingDecisions"));
        var beforeSpawn = BattlePlannerChecks.Replay(root, shortSteps, out var earlyOutcome);
        var afterSpawn = BattlePlannerChecks.Replay(root, longSteps, out var lateOutcome);
        Require(earlyOutcome == RoomOutcome.PlayerDefeated && lateOutcome == earlyOutcome &&
            shortSteps.Length == 8 && longSteps.Length == 49 &&
            keys.Key(beforeSpawn) == keys.Key(earlyPlan.GetProperty("State").Deserialize<BattleTurnState>()!) &&
            keys.Key(afterSpawn) == keys.Key(late.GetProperty("Final").Deserialize<BattleTurnState>()!),
            "Saved8/49-input losses differ from independent full-state/RNG replay.");
        var pending = beforeSpawn.Spawn.Waves.Skip(beforeSpawn.Spawn.Phase).SelectMany(wave => wave.Candidates)
            .SelectMany(group => group.Units).Where(enemy => enemy.Unit.IsBoss == true).Single();
        var live = afterSpawn.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsBoss == true && unit.Health > 0);
        Require(pending.Unit.Health == 1100 && live.Health == 1100 && live.MaxHealth == 1100 &&
            !beforeSpawn.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.IsBoss == true && unit.Health > 0),
            "Missing the same uninjured1100HP boss before/after birth.");
        double Cost(BattleTurnState state) => BattlePlanner.PositionCost(state, RoomOutcome.PlayerDefeated);
        Require(Cost(beforeSpawn) - 1100 < Cost(afterSpawn) && Cost(afterSpawn) < Cost(beforeSpawn),
            "Uninjured pending boss omission no longer reproduces the old and corrected loss ordering.");
        BattlePlanResult Plan(BattleTurnState state, BattleSearchStep[] steps, double score) =>
            new(state, RoomOutcome.PlayerDefeated, steps, score, steps.Count(step => step.IsEndTurn),
                0, 0, 0, TimeSpan.Zero, false, false, false, false, true,
                Array.Empty<UnsupportedActionAttempt>(), null, searchedToBattleEnd: true);
        var idle = Plan(beforeSpawn, shortSteps, 1e12);
        var played = Plan(afterSpawn, longSteps, 1e12);
        foreach (var order in new[] { new[] { idle, played }, new[] { played, idle } })
            Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(order), played),
                "Portfolio still prefers dying before an uninjured boss is born.");
        var primary = Plan(beforeSpawn, shortSteps, played.Score - 1);
        Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(new[] { played, primary }), primary),
            "Pending material overrode the public objective.");
        foreach (var state in new[] { beforeSpawn, afterSpawn })
            Require(BattlePlanner.Evaluate(state, RoomOutcome.PlayerDefeated) == 1e12 &&
                BattlePlanner.PositionCost(state, RoomOutcome.BattleWon) == BattlePlanner.PositionCost(state) &&
                BattlePlanner.PositionCost(state, RoomOutcome.Stalemate) == BattlePlanner.PositionCost(state),
                "Pending material changed another outcome or the primary loss score.");
        Require(keys.Key(root) == parent, "Uninjured boss replay mutated its parent.");
        Console.WriteLine($"UNINJURED-PENDING-BOSS PASS:8/49-input model losses/full state/RNG; old cost={Cost(beforeSpawn)-1100:F1}<{Cost(afterSpawn):F1}, corrected={Cost(beforeSpawn):F1}; worker order/primary score/immutable root.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
