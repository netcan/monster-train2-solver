using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RecordedBattleChecks
{
    // Each child starts from the previous modeled result, never the next native snapshot.
    internal static void Native(FixtureValue root)
    {
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        var turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        BattleTurnState initial = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        if (turns.Length > 0 && turns[0].GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() < initial.Spawn.Turn)
            initial = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(initial, ModelJson.Options);
        void Replay()
        {
            BattleTurnState state = initial;
            int actionIndex = 0;
            foreach (FixtureValue turn in turns)
            {
                int number = turn.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32();
                while (actionIndex < actions.Length && actions[actionIndex].GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() <= number)
                    Play(actions[actionIndex++]);
                Compare(state, turn.GetProperty("Before").Deserialize<BattleTurnState>()!, false, "before EndTurn");
                BattleTurnResult next = BattleTurnModel.EndTurn(state);
                Require(next.Supported, next.UnsupportedReason ?? "EndTurn unsupported.");
                Require(next.Outcome == turn.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(), "EndTurn outcome differs.");
                state = next.State!;
                Compare(state, turn.GetProperty("Actual").Deserialize<BattleTurnState>()!, Terminal(next.Outcome), "after EndTurn");
            }
            while (actionIndex < actions.Length) Play(actions[actionIndex++]);
            void Play(FixtureValue row)
            {
                Compare(state, row.GetProperty("Before").Deserialize<BattleTurnState>()!, false, "before play");
                BattleActionResult next = BattleActionModel.PlayCard(state, row.GetProperty("Action").Deserialize<PlayCardAction>()!);
                Require(next.Supported, next.Reason ?? "Card action unsupported.");
                Require(next.Outcome == row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(), "Card outcome differs.");
                state = next.State!;
                Compare(state, row.GetProperty("Actual").Deserialize<BattleTurnState>()!, Terminal(next.Outcome), "after play");
            }
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(parent == JsonSerializer.Serialize(initial, ModelJson.Options), "Recorded full chain mutated its parent.");
        Console.WriteLine($"RECORDED-BATTLE-CHAIN PASS: {actions.Length} plays, {turns.Length} EndTurns, full native decision comparisons and 16 independent immutable branches.");
    }
    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
    private static void Compare(BattleTurnState predicted, BattleTurnState actual, bool terminal, string label)
    {
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(predicted, terminal), BattleTurnChecks.Comparable(actual, terminal));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
