using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class BattleTurnChecks
{
    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("Turns", out JsonElement turns) || turns.GetArrayLength() == 0) return;
        int matched = 0, unsupported = 0;
        foreach (JsonElement turn in turns.EnumerateArray())
        {
            BattleTurnState before = turn.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            if (!result.Supported) { unsupported++; continue; }
            BattleTurnState actual = turn.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            Require(result.Outcome == (RoomOutcome)turn.GetProperty("ActualOutcome").GetInt32() &&
                Comparable(result.State!) == Comparable(actual), "Native EndTurn differs at index " + turn.GetProperty("Index"));
            matched++;
        }
        Console.WriteLine($"NATIVE-TURN-CHECKS PASS: {matched} matched, {unsupported} unsupported.");
        if (unsupported > 0) return;
        if (fixture.TryGetProperty("Policy", out JsonElement policy) &&
            policy.GetString() is "units-and-junk" or "units-spells-and-junk") return;
        BattleTurnState root = turns[0].GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
        string parent = JsonSerializer.Serialize(root);
        BattleTurnState terminal = RunIndependentChain(root, turns);
        Require(JsonSerializer.Serialize(root) == parent, "Full battle simulation mutated its root.");
        string expected = Comparable(terminal);
        Parallel.For(0, 16, _ => Require(Comparable(RunIndependentChain(root, turns)) == expected,
            "Full battle parallel branches diverged."));
        int hp = terminal.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0;
        Console.WriteLine($"NATIVE-FULL-CHAIN-CHECKS PASS: {turns.GetArrayLength()} EndTurns, final Pyre {hp}, independent root and 16 parallel branches.");
    }

    private static BattleTurnState RunIndependentChain(BattleTurnState root, JsonElement oracle)
    {
        // All following states come from the model. Native Before/Predicted states are never injected.
        BattleSimulationResult simulation = BattleSimulator.ResolveNoMoreCards(root);
        Require(simulation.Supported && simulation.Turns.Count == oracle.GetArrayLength(),
            "Independent full simulation did not reach the expected terminal turn: " + simulation.UnsupportedReason);
        for (int index = 0; index < simulation.Turns.Count; index++)
        {
            BattleTurnResult result = simulation.Turns[index];
            Require(result.Supported, "Independent full chain rejected turn " + index + ": " + result.UnsupportedReason);
            JsonElement actualTurn = oracle[index];
            BattleTurnState actual = actualTurn.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            Require(result.Outcome == (RoomOutcome)actualTurn.GetProperty("ActualOutcome").GetInt32() &&
                Comparable(result.State!) == Comparable(actual), "Independent full chain diverged at turn " + index);
            bool terminal = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(terminal == (index == oracle.GetArrayLength() - 1), "The independent chain terminated at the wrong turn.");
        }
        return simulation.State!;
    }
    internal static string Comparable(BattleTurnState state) => JsonSerializer.Serialize(new
    {
        Rooms = state.Spawn.Train.Rooms.Select(room => new { room.RoomIndex, room.Deployment, room.Units }).ToArray(),
        Movement = state.Spawn.Train.Movement.OrderBy(rule => rule.UnitId).ToArray(), state.Spawn.Train.Context,
        state.Spawn.Phase, state.Spawn.SelectedGroups, state.Spawn.Rng, state.Spawn.NextUnitId,
        state.Spawn.TreasuresRemaining, state.Spawn.Turn, state.Energy, state.ForgePoints, state.DragonsHoard,
        state.MoonPhase, state.RngStreams, state.OtherPiles, state.PlayRules, state.BattlePreviewEnabled, state.UiRngIsolated
    });
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
