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

    internal static bool SameFields(FixtureValue before, FixtureValue after, params string[] exceptions)
    {
        var previous = before.EnumerateObject().Where(field => !exceptions.Contains(field.Name)).ToArray();
        var current = after.EnumerateObject().Where(field => !exceptions.Contains(field.Name)).ToArray();
        return previous.Length == current.Length && previous.Zip(current).All(pair =>
            pair.First.Name == pair.Second.Name && pair.First.Value.ContentEquals(pair.Second.Value));
    }

    internal static bool SameCards(IEnumerable<FixtureValue> values, (int, string)[] expected) =>
        values.Select(card => (card.GetProperty("InstanceId").GetInt32(), card.GetProperty("DataId").GetString()))
            .SequenceEqual(expected.Select(card => (card.Item1, (string?)card.Item2)));

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
                state = RecordedBoundary(state, turn.GetProperty("Before"), terminal: false, "before " + label);
                BattleTurnResult next = BattleTurnModel.EndTurn(state, Decisions(turn));
                Require(next.Supported, label + ": " + (next.UnsupportedReason ?? "Unsupported."));
                Require(next.Outcome == turn.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(), label + ": Outcome differs.");
                state = RecordedBoundary(next.State!, turn.GetProperty("Actual"), Terminal(next.Outcome), "after " + label);
            }
            while (actionIndex < actions.Length) Play(actionIndex++);
            completed?.Invoke(state);
            void Play(int index)
            {
                FixtureValue row = actions[index];
                string label = $"play {index + 1} in turn {state.Spawn.Turn}";
                state = RecordedBoundary(state, row.GetProperty("Before"), terminal: false, "before " + label);
                BattleActionResult next = BattleActionModel.PlayCard(state, row.GetProperty("Action").Deserialize<PlayCardAction>()!);
                Require(next.Supported, label + ": " + (next.Reason ?? "Unsupported."));
                Require(next.Outcome == row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(), label + ": Outcome differs.");
                state = RecordedBoundary(next.State!, row.GetProperty("Actual"), Terminal(next.Outcome), "after " + label);
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
    internal static BattleTurnState RecordedBoundary(BattleTurnState state, FixtureValue snapshot, bool terminal, string label)
    {
        BattleTurnState actual = snapshot.Deserialize<BattleTurnState>()!;
        string? difference = Difference(state, actual, terminal);
        if (difference == null) return state;

        // The game can focus another floor before the input or between settlement
        // and capture. Reproduce the observed focus from the modeled state.
        if (!terminal && BattleTurnModel.ReplayObservedFocus(state, actual) is BattleTurnState focused &&
            Difference(focused, actual, terminal: false) == null)
            return focused;
        if (!terminal && ReplayPreviewBetweenInputs(state, actual) is BattleTurnState refreshed &&
            Difference(refreshed, actual, terminal: false) == null)
            return refreshed;
        throw new InvalidOperationException(label + ": " + difference);
    }

    private static BattleTurnState? ReplayPreviewBetweenInputs(BattleTurnState previous, BattleTurnState observed)
    {
        if (!previous.BattlePreviewEnabled || !observed.BattlePreviewEnabled || previous.Spawn.Turn != observed.Spawn.Turn ||
            previous.PlayRules == null ||
            previous.Spawn.Train.Context?.SpawnPoints?.NextPreviewCopyId is not int priorCopy ||
            observed.Spawn.Train.Context?.SpawnPoints?.NextPreviewCopyId is not int actualCopy || actualCopy <= priorCopy)
            return null;
        if (previous.SelectedRoom != observed.SelectedRoom)
            return BattleTurnModel.SelectRoom(previous, observed.SelectedRoom).State;
        // Native UI can refresh a second time on the same floor between inputs.
        // Re-run one complete preview and compare its entire resulting decision.
        TrainCombatResult preview = BattlePreviewModel.Refresh(previous.Spawn.Train, previous.PlayRules);
        if (!preview.Supported || preview.State!.Context?.SpawnPoints?.NextPreviewCopyId != actualCopy)
            return null;
        TrainCombatState train = preview.State;
        EnemySpawnState spawn = previous.Spawn;
        var refreshed = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
            train.Context!.NextUnitId ?? spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
            spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds,
            spawn.OuterBoss, spawn.QueuedOuterBoss, spawn.QueuedTrueFinalBoss, spawn.TrueFinalBoss);
        BattleRngStream[] streams = previous.RngStreams.Select(stream => stream.Name == "Battle"
            ? new BattleRngStream(stream.Name, stream.Seed, train.Context.BattleRng)
            : stream).ToArray();
        return new BattleTurnState(refreshed, previous.Energy, previous.EnergyPerTurn, previous.DrawPerTurn, previous.ForgePoints,
            previous.DragonsHoard, previous.MoonPhase, streams, previous.OtherPiles, previous.ExternalInteractions, previous.PlayRules,
            previous.BattlePreviewEnabled, previous.UiRngIsolated, previous.CanonicalDecisionReferences, previous.CanonicalPhysicalReferences,
            previous.SelectedRoom, previous.MoonPhaseTrackingActive);
    }

    internal static void Compare(BattleTurnState predicted, BattleTurnState actual, bool terminal, string label)
    {
        string? difference = Difference(predicted, actual, terminal);
        Require(difference == null, label + ": " + difference);
    }

    private static string? Difference(BattleTurnState predicted, BattleTurnState actual, bool terminal)
    {
        string predictedState = BattleTurnChecks.Comparable(predicted, terminal);
        string actualState = BattleTurnChecks.Comparable(actual, terminal);
        return string.Equals(predictedState, actualState, StringComparison.Ordinal) ? null :
            ModelJson.Difference(predictedState, actualState);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
