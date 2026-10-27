using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomPreCombatWorldChecks
{
    internal static void NativeRoot(FixtureValue fixture)
    {
        Require(fixture.GetProperty("EvidenceKind").GetString() == "NativeDeploymentObservation",
            "Expected the unchanged native deployment capture.");
        var root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var world = root.Spawn.Train.Context!.Enchantments!;
        Require(root.Spawn.Turn == 0 && world.AutomaticLifecycle &&
            world.Rooms.Select(room => room.RoomIndex).SequenceEqual(new[] { 0, 1, 2, 3 }) &&
            fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Scenario").GetString() == "Level6BattleEaterDamageShield" &&
            fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Seed").GetString() == "1860103187",
            "Missing the shared-world, Cael and original Inferno Room combination.");
        string immutable = JsonSerializer.Serialize(root, ModelJson.Options);
        var options = new BattlePlannerOptions(256, 3, 8, 12, TimeSpan.FromSeconds(10),
            allowShorterHorizon: true, searchToBattleEnd: true, maxSteps: 256, stopAtBestPyreHealth: true);
        // Both native-player workers previously threw inside their seed routes,
        // before publishing even one input. Keep those routes and the budget.
        var plan = ParallelBattlePlanner.Plan(root, options, 2);
        Require(!plan.Cancelled && (plan.CompletedHorizon || plan.UsedShorterHorizon) && plan.Steps.Count > 0,
            "The original two-worker root still cannot publish a completed turn.");
        BattleAdviceSession.Verify(root, plan);
        var successor = BattlePlannerChecks.Replay(root, plan.Steps, out var outcome);
        Require(outcome == plan.Outcome && NativeBranchObservationChecks.Key(successor,
                outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated) ==
            NativeBranchObservationChecks.Key(plan.State, outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated),
            "Shared-world search failed independent complete-state/forecast/RNG replay.");
        Require(immutable == JsonSerializer.Serialize(root, ModelJson.Options), "Search mutated the native root.");
        Console.WriteLine($"ROOM-PRE-COMBAT-WORLD PASS: unchanged native Cael/Inferno deployment, two workers, complete forecast/state/RNG and immutable root; steps={plan.Steps.Count} outcome={outcome}; model prediction requires native replay.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
