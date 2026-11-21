using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RecordedBattleChecks
{
    // Each child starts from the previous modeled result, never the next native snapshot.
    internal static void Native(FixtureValue root, int firstTurn = 0, Action<BattleTurnState>? completed = null)
    {
        var (plays, turns) = Replay(root, firstTurn, completed, branches: 16);
        Console.WriteLine($"RECORDED-BATTLE-CHAIN PASS: {plays} plays, {turns} EndTurns, first turn {firstTurn}, full native decision comparisons and 16 independent immutable branches.");
    }

    internal static void ReplayOnce(FixtureValue root, int firstTurn = 0, int lastTurn = int.MaxValue) =>
        Replay(root, firstTurn, null, branches: 0, lastTurn: lastTurn);

    private static (int Plays, int EndTurns) Replay(FixtureValue root, int firstTurn, Action<BattleTurnState>? completed,
        int branches, int lastTurn = int.MaxValue)
    {
        var actions = root.GetProperty("Actions").EnumerateArray().Where(InScope).ToArray();
        var turns = root.GetProperty("Turns").EnumerateArray().Where(InScope).ToArray();
        bool InScope(FixtureValue row)
        {
            int turn = DecisionTurn(row);
            return turn >= firstTurn && turn <= lastTurn;
        }
        Require(actions.Length > 0 || turns.Length > 0, "Recorded battle contains no actions or EndTurns.");
        BattleTurnState initial = (actions.Length > 0 ? actions[0] : turns[0])
            .GetProperty("Before").Deserialize<BattleTurnState>()!;
        if (turns.Length > 0 && turns[0].GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() < initial.Spawn.Turn)
            initial = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(initial, ModelJson.Options);
        void Replay()
        {
            BattleTurnState state = initial;
            int actionIndex = 0;
            foreach (FixtureValue turn in turns)
            {
                int number = DecisionTurn(turn);
                while (actionIndex < actions.Length && DecisionTurn(actions[actionIndex]) <= number)
                    Play(actionIndex++);
                string label = $"EndTurn {number}";
                Compare(state, turn.GetProperty("Before").Deserialize<BattleTurnState>()!, false, "before " + label);
                BattleTurnResult next = BattleTurnModel.EndTurn(state, Decisions(turn));
                Require(next.Supported, label + ": " + (next.UnsupportedReason ?? "Unsupported."));
                Require(next.Outcome == turn.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(), label + ": Outcome differs.");
                state = next.State!;
                Compare(state, turn.GetProperty("Actual").Deserialize<BattleTurnState>()!, Terminal(next.Outcome), "after " + label);
            }
            while (actionIndex < actions.Length) Play(actionIndex++);
            completed?.Invoke(state);
            void Play(int index)
            {
                FixtureValue row = actions[index];
                string label = $"play {index + 1} in turn {state.Spawn.Turn}";
                Compare(state, row.GetProperty("Before").Deserialize<BattleTurnState>()!, false, "before " + label);
                BattleActionResult next = BattleActionModel.PlayCard(state, row.GetProperty("Action").Deserialize<PlayCardAction>()!);
                Require(next.Supported, label + ": " + (next.Reason ?? "Unsupported."));
                Require(next.Outcome == row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(), label + ": Outcome differs.");
                state = next.State!;
                Compare(state, row.GetProperty("Actual").Deserialize<BattleTurnState>()!, Terminal(next.Outcome), "after " + label);
            }
        }
        Replay();
        if (branches > 0) Parallel.For(0, branches, _ => Replay());
        Require(parent == JsonSerializer.Serialize(initial, ModelJson.Options), "Recorded full chain mutated its parent.");
        return (actions.Length, turns.Length);
    }
    internal static int DecisionTurn(FixtureValue row) => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32();
    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
    internal static RelentlessDecision[]? Decisions(FixtureValue row) => row.TryGetProperty("RelentlessDecisions", out var choices) &&
        choices.ValueKind != FixtureKind.Null ? choices.Deserialize<RelentlessDecision[]>() : null;
    internal static void Compare(BattleTurnState predicted, BattleTurnState actual, bool terminal, string label)
    {
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(predicted, terminal), BattleTurnChecks.Comparable(actual, terminal));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
