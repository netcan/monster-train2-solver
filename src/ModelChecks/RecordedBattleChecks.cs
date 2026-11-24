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

    internal static bool DiagnoseDecisions(FixtureValue root)
    {
        FixtureValue actions = root.GetProperty("Actions"), turns = root.GetProperty("Turns");
        Require(actions.GetArrayLength() + turns.GetArrayLength() > 0, "Recorded battle contains no actions or EndTurns.");
        bool trackPreviewPosition = TracksPreviewPosition(root);
        FixtureValue[] previewScheduling = ReadPreviewScheduling(root, trackPreviewPosition);
        var failures = new List<(string Status, string Reason, bool EndTurn, int Index, int Turn)>();
        foreach (FixtureValue action in actions.EnumerateArray()) Check(action, endTurn: false);
        foreach (FixtureValue turn in turns.EnumerateArray()) Check(turn, endTurn: true);

        foreach (var group in failures.GroupBy(failure => (failure.Status, failure.Reason))
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.Reason, StringComparer.Ordinal))
        {
            var first = group.OrderBy(failure => failure.Turn).ThenBy(failure => failure.EndTurn)
                .ThenBy(failure => failure.Index).First();
            int endTurns = group.Count(failure => failure.EndTurn);
            Console.WriteLine($"RECORDED-DECISION {group.Key.Status} plays={group.Count() - endTurns} endTurns={endTurns} " +
                $"first={(first.EndTurn ? "endTurn" : "play")}[{first.Index}] turn={first.Turn} reason={group.Key.Reason}");
        }
        int unsupported = failures.Count(failure => failure.Status == "UNSUPPORTED");
        int matched = actions.GetArrayLength() + turns.GetArrayLength() - failures.Count;
        Console.WriteLine($"RECORDED-DECISION-RESULT plays={actions.GetArrayLength()} endTurns={turns.GetArrayLength()} " +
            $"matched={matched} unsupported={unsupported} mismatched={failures.Count - unsupported}");
        return failures.Count == 0;

        void Check(FixtureValue row, bool endTurn)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState? predicted;
            RoomOutcome outcome;
            string? unsupportedReason;
            if (endTurn)
            {
                BattleTurnResult result = ResolveEndTurn(before, row, previewScheduling);
                (predicted, outcome, unsupportedReason) = (result.State, result.Outcome, result.UnsupportedReason);
            }
            else
            {
                BattleActionResult result = BattleActionModel.PlayCard(before, row.GetProperty("Action").Deserialize<PlayCardAction>()!);
                (predicted, outcome, unsupportedReason) = (result.State, result.Outcome, result.Reason);
            }

            void Report(string status, string reason) =>
                failures.Add((status, reason, endTurn, row.GetProperty("Index").GetInt32(), before.Spawn.Turn));
            if (predicted == null)
            {
                Report("UNSUPPORTED", unsupportedReason ?? "No modeled successor.");
                return;
            }
            RoomOutcome actual = row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>();
            if (outcome != actual)
            {
                Report("MISMATCH", $"Outcome {outcome} != {actual}.");
                return;
            }
            try { RecordedBoundary(predicted, row.GetProperty("Actual"), Terminal(outcome), "Successor", trackPreviewPosition); }
            catch (InvalidOperationException error) { Report("MISMATCH", error.Message); }
        }
    }

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
        bool trackPreviewPosition = TracksPreviewPosition(root);
        FixtureValue[] previewScheduling = ReadPreviewScheduling(root, trackPreviewPosition);
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
                state = RecordedBoundary(state, turn.GetProperty("Before"), terminal: false, "before " + label, trackPreviewPosition);
                BattleTurnResult next = ResolveEndTurn(state, turn, previewScheduling);
                Require(next.Supported, label + ": " + (next.UnsupportedReason ?? "Unsupported."));
                Require(next.Outcome == turn.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(), label + ": Outcome differs.");
                state = RecordedBoundary(next.State!, turn.GetProperty("Actual"), Terminal(next.Outcome), "after " + label, trackPreviewPosition);
            }
            while (actionIndex < actions.Length) Play(actionIndex++);
            completed?.Invoke(state);
            void Play(int index)
            {
                FixtureValue row = actions[index];
                string label = $"play {index + 1} in turn {state.Spawn.Turn}";
                state = RecordedBoundary(state, row.GetProperty("Before"), terminal: false, "before " + label, trackPreviewPosition);
                BattleActionResult next = BattleActionModel.PlayCard(state, row.GetProperty("Action").Deserialize<PlayCardAction>()!);
                Require(next.Supported, label + ": " + (next.Reason ?? "Unsupported."));
                Require(next.Outcome == row.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(), label + ": Outcome differs.");
                state = RecordedBoundary(next.State!, row.GetProperty("Actual"), Terminal(next.Outcome), "after " + label, trackPreviewPosition);
            }
        }
        Replay();
        if (branches > 0) Parallel.For(0, branches, _ => Replay());
        Require(parent == JsonSerializer.Serialize(initial, ModelJson.Options), "Recorded full chain mutated its parent.");
        return (actions.Length, turns.Length);
    }
    internal static int DecisionTurn(FixtureValue row) => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32();
    private static bool TracksPreviewPosition(FixtureValue root) => root.TryGetProperty("NativeRunInput", out FixtureValue run) &&
        run.ValueKind == FixtureKind.Object && run.TryGetProperty("RunType", out FixtureValue runType) && runType.GetString() == "RegionRun";
    private static FixtureValue[] ReadPreviewScheduling(FixtureValue root, bool trackPreviewPosition) =>
        trackPreviewPosition && root.TryGetProperty("PreviewScheduling", out var scheduling) && scheduling.ValueKind == FixtureKind.Array
            ? scheduling.EnumerateArray().ToArray() : [];

    private static BattleTurnResult ResolveEndTurn(BattleTurnState source, FixtureValue row, FixtureValue[] scheduling)
    {
        RelentlessDecision[]? decisions = Decisions(row);
        BattleTurnResult result = BattleTurnModel.EndTurn(source, decisions);
        if (!result.Supported || Terminal(result.Outcome) || scheduling.Length == 0 ||
            source.Spawn.Train.Context?.SpawnPoints?.NextPreviewCopyId is not int startCopy)
            return result;
        var expected = row.GetProperty("Actual").GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
        if (!expected.TryGetProperty("SpawnPoints", out var points) || points.ValueKind != FixtureKind.Object ||
            !points.TryGetProperty("NextPreviewCopyId", out var nextCopy) || nextCopy.ValueKind != FixtureKind.Number ||
            result.State!.Spawn.Train.Context?.SpawnPoints?.NextPreviewCopyId is not int modeledCopy || modeledCopy >= nextCopy.GetInt32())
            return result;

        // Hand animation can request a refresh in the last player frame before combat.
        // The original scheduling receipt identifies when it belongs before EndTurn.
        for (int index = 0; index + 2 < scheduling.Length; index++)
        {
            FixtureValue begin = scheduling[index], complete = scheduling[index + 1], close = scheduling[index + 2];
            if (!InSourceRoom(begin) || !InSourceRoom(complete) || !InSourceRoom(close) ||
                begin.GetProperty("Operation").GetString() != "Battle:Calculating" ||
                complete.GetProperty("Operation").GetString() != "Battle:On" ||
                close.GetProperty("Operation").GetString() != "Battle:Off" ||
                !begin.GetProperty("PlayerControl").GetBoolean() || !complete.GetProperty("PlayerControl").GetBoolean() ||
                close.GetProperty("PlayerControl").GetBoolean() ||
                begin.GetProperty("ActionCount").GetInt32() != complete.GetProperty("ActionCount").GetInt32() ||
                complete.GetProperty("ActionCount").GetInt32() != close.GetProperty("ActionCount").GetInt32() ||
                CopyId(begin) != startCopy || CopyId(complete) is not int afterCopy || afterCopy <= startCopy || CopyId(close) != afterCopy)
                continue;
            BattleTurnState? refreshed = BattleTurnModel.ReplayObservedPreview(source, source.SelectedRoom, afterCopy);
            if (refreshed == null) return result;
            BattleTurnResult replayed = BattleTurnModel.EndTurn(refreshed, decisions);
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            return replayed.Outcome == result.Outcome && replayed.State is BattleTurnState child && child.SelectedRoom == actual.SelectedRoom &&
                child.Spawn.Train.Context?.SpawnPoints?.NextPreviewCopyId == nextCopy.GetInt32() && Difference(child, actual, false) == null
                ? replayed : result;
        }
        return result;

        bool InSourceRoom(FixtureValue item) => item.GetProperty("Turn").GetInt32() == source.Spawn.Turn &&
            item.GetProperty("Room").GetInt32() == source.SelectedRoom;
        static int? CopyId(FixtureValue item) => item.TryGetProperty("NextPreviewCopyId", out var copy) && copy.ValueKind == FixtureKind.Number
            ? copy.GetInt32() : null;
    }
    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
    internal static RelentlessDecision[]? Decisions(FixtureValue row) => row.TryGetProperty("RelentlessDecisions", out var choices) &&
        choices.ValueKind != FixtureKind.Null ? choices.Deserialize<RelentlessDecision[]>() : null;
    internal static BattleTurnState RecordedBoundary(BattleTurnState state, FixtureValue snapshot, bool terminal, string label,
        bool trackPreviewPosition = false)
    {
        BattleTurnState actual = snapshot.Deserialize<BattleTurnState>()!;
        string? difference = Difference(state, actual, terminal);
        bool PreviewPositionMatches(BattleTurnState candidate) => terminal || !trackPreviewPosition ||
            candidate.SelectedRoom == actual.SelectedRoom &&
            candidate.Spawn.Train.Context?.SpawnPoints?.NextPreviewCopyId == actual.Spawn.Train.Context?.SpawnPoints?.NextPreviewCopyId;
        if (difference == null && PreviewPositionMatches(state)) return state;

        // The game can focus another floor before the input or between settlement
        // and capture. Reproduce the observed focus from the modeled state.
        if (!terminal && BattleTurnModel.ReplayObservedFocus(state, actual) is BattleTurnState focused &&
            PreviewPositionMatches(focused) && Difference(focused, actual, terminal: false) == null)
            return focused;
        if (!terminal && BattleTurnModel.ReplayObservedPreview(state, actual, trackPreviewPosition) is BattleTurnState refreshed &&
            PreviewPositionMatches(refreshed) && Difference(refreshed, actual, terminal: false) == null)
            return refreshed;
        throw new InvalidOperationException(label + ": " + (difference ?? "Preview room or allocator differs."));
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
