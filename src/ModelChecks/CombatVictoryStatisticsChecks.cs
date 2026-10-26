using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CombatVictoryStatisticsChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, FixtureValue? retention = null)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 3 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0 && rows.Length == 37 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            rows[^1].GetProperty("Outcome").GetInt32() == (int)RoomOutcome.BattleWon,
            "Missing37-input native combat victory counterexample.");
        var before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var oldStats = historical.Spawn.Train.Context!.Statistics!;
        var newStats = actual.Spawn.Train.Context!.Statistics!;
        var terminalRoom = fixture.GetProperty("Stages").EnumerateArray().Single(stage =>
            stage.GetProperty("Kind").GetString() == "Room" && stage.GetProperty("Predicted").GetProperty("Outcome").GetInt32() == (int)RoomOutcome.BattleWon);
        var boss = terminalRoom.GetProperty("Before").Deserialize<RoomCombatState>()!.Units.Single(unit => unit.IsBoss == true);
        Require(boss.Status("unstable")?.Stacks == 5 && terminalRoom.GetProperty("Predicted").GetProperty("Events").EnumerateArray()
            .Any(item => item.GetProperty("Kind").GetString() == "Death" && item.GetProperty("Actor").GetInt32() == 0 &&
                item.GetProperty("Target").GetInt32() == boss.Id),
            "Missing the unstable boss's null-source sacrifice death.");
        Require(before.Spawn.Turn == 4 && historical.Spawn.Train.Context.KillCamActivated == true &&
            actual.Spawn.Train.Context.KillCamActivated == true && oldStats.TrackedCards.Count == 24 &&
            oldStats.Values.Count == 424 && newStats.TrackedCards.Count == 15 && newStats.Values.Count == 293 &&
            oldStats.TrackedCards.Except(newStats.TrackedCards).SequenceEqual(Enumerable.Range(17, 9)) &&
            newStats.TrackedCards.SequenceEqual(newStats.DeckCards!) &&
            JsonSerializer.Serialize(oldStats.RefreshDeckAfterCardTerminal(), ModelJson.Options) ==
                JsonSerializer.Serialize(newStats, ModelJson.Options),
            "The old native mismatch must be exactly nine generated infusion statistic entries.");
        using var keys = new BattleStateKeys();
        var root = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        string immutable = keys.Key(root);
        var current = root;
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var captured = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var expected = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool terminal = i == rows.Length - 1;
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(captured),
                "Native victory input root differs at" + i);
            var step = new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean());
            current = BattlePlannerChecks.Replay(current, [step], out var outcome);
            Require((int)outcome == row.GetProperty("Outcome").GetInt32() &&
                NativeBranchObservationChecks.Key(current, terminal) == NativeBranchObservationChecks.Key(expected, terminal),
                "Native victory complete successor/RNG differs at" + i);
            if (!terminal)
                Require(NativeBranchObservationChecks.Key(row.GetProperty("Expected").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(expected), "The original nonterminal forecast differs at" + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon &&
                NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(actual, true),
                "Detached victory statistics differ.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options) && keys.Key(root) == immutable,
            "Victory statistic refresh changed its parent.");
        if (replay.HasValue)
        {
            var fresh = NativeBranchObservationChecks.Replay(replay.Value,
                "Native combat victory refreshes statistics after clearing battle piles.", boundaryOutcome: RoomOutcome.BattleWon);
            Require(fresh.Length == rows.Length && fresh[^1].GetProperty("Expected").ValueKind == FixtureKind.Object,
                "Missing independent37-input repaired victory forecast.");
            for (int i = 0; i < rows.Length; i++)
            {
                bool terminal = i == rows.Length - 1;
                Require(rows[i].GetProperty("Action").ContentEquals(fresh[i].GetProperty("Action")) &&
                    rows[i].GetProperty("IsEndTurn").GetBoolean() == fresh[i].GetProperty("IsEndTurn").GetBoolean() &&
                    NativeBranchObservationChecks.Key(rows[i].GetProperty("Root").Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(fresh[i].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                    NativeBranchObservationChecks.Key(rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal) ==
                        NativeBranchObservationChecks.Key(fresh[i].GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal),
                    "Independent native victory input/full state/RNG differs at" + i);
            }
        }
        // Whole-turn equality alone can hide stale shared context in lower-level
        // room/train results. Verify every native train phase independently too.
        TrainCombatChecks.Native(fixture);
        if (retention.HasValue) Retention(retention.Value);
        Console.WriteLine("COMBAT-VICTORY-STATISTICS PASS:37 native inputs/full state/forecast/RNG and native train phases; preserved424/24 vs293/15 counterexample; exactly nine generated infusion entries removed, permanent stats retained; four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Retention(FixtureValue fixture)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 3 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0 && rows.Length == 32,
            "Missing the native ordinary victory cache-retention counterexample.");
        var stale = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Statistics!;
        var native = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Statistics!;
        Require(stale.Values.Count == 330 && native.Values.Count == 400 && stale.TrackedCards.Count == 18 && native.TrackedCards.Count == 23 &&
            JsonSerializer.Serialize(native.RefreshDeckAfterCardTerminal(), ModelJson.Options) == JsonSerializer.Serialize(stale, ModelJson.Options),
            "Ordinary victory must retain the five generated-card cache entries; a blanket refresh is incorrect.");
        var current = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        string immutable = JsonSerializer.Serialize(current, ModelJson.Options);
        var root = current;
        foreach (var row in rows)
        {
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(row.GetProperty("Root").Deserialize<BattleTurnState>()!),
                "Cache-retention native root differs.");
            current = BattlePlannerChecks.Replay(current, [new(row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require((int)outcome == row.GetProperty("Outcome").GetInt32() && NativeBranchObservationChecks.Key(current, outcome == RoomOutcome.BattleWon) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!, outcome == RoomOutcome.BattleWon),
                "Cache-retention full native successor/RNG differs.");
        }
        Require(immutable == JsonSerializer.Serialize(root, ModelJson.Options), "Cache-retention replay changed its root.");
        TrainCombatChecks.Native(fixture);
        Console.WriteLine("COMBAT-VICTORY-CACHE-RETENTION PASS:32 native inputs/full state/RNG; retained400 values/23 cards, preserving five generated infusions; ordinary victory must not force a refresh.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
