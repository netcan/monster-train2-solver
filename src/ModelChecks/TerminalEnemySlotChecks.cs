using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TerminalEnemySlotChecks
{
    private const string Reason = "Terminal combat leaves a dead spawn-point occupant before dissolve settlement.";
    private static string Key(BattleTurnState state, bool terminal = false) => NativeBranchObservationChecks.Key(state, terminal);
    internal static void Stages(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var fixture = document.RootElement;
        Require(fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean(), "Terminal effects did not settle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(key).GetInt32() == 0, "Incomplete terminal stage evidence: " + key);
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(fixture);
        BattleSpawnPointChecks.DecisionReferences(fixture);
        Console.WriteLine("TERMINAL-ENEMY-STAGES PASS: complete native room/train contexts, query frames and raw/canonical ownership; zero failed or skipped stages.");
    }
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            fixture.GetProperty("NativeCompleted").GetBoolean() &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4", "Missing native observation.");
        var request = fixture.GetProperty("Request");
        var prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
        var rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(request.GetProperty("Reason").GetString() == Reason && rows.Length == 37 && prefix.Length == 36,
            "Wrong terminal enemy slot boundary.");
        BattleTurnState current = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        for (int index = 0; index < rows.Length; index++)
        {
            bool boundary = index == rows.Length - 1;
            var input = boundary ? request : prefix[index];
            var row = rows[index];
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool end = input.GetProperty("IsEndTurn").GetBoolean();
            Require(Key(current) == Key(before) && row.GetProperty("Boundary").GetBoolean() == boundary &&
                row.GetProperty("IsEndTurn").GetBoolean() == end && row.GetProperty("Action").ContentEquals(input.GetProperty("Action")),
                "Native prefix/input changed at " + index);
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleTurnState predicted = BattlePlannerChecks.Replay(before,
                [new BattleSearchStep(input.GetProperty("Action").Deserialize<PlayCardAction>(), end)], out RoomOutcome outcome);
            BattleTurnState comparable = boundary ? ProjectHistoricalSlot(actual) : actual;
            Require(outcome == (boundary ? RoomOutcome.BattleWon : RoomOutcome.Exchanged) &&
                Key(predicted, boundary) == Key(comparable, boundary) &&
                Key(row.GetProperty("Expected").Deserialize<BattleTurnState>()!, boundary) == Key(comparable, boundary) &&
                row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null,
                "Full native successor/forecast/RNG differs at " + index);
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Replay mutated its parent.");
            if (boundary)
            {
                Require(end && Key(before) == Key(request.GetProperty("FailedRoot").Deserialize<BattleTurnState>()!) &&
                    before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 11) is
                        { EndsBattleOnDeath: true, IsBoss: true, Health: > 0 } &&
                    before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 12) is
                        { AssetKey: "ClericT1_OminousAcolyte", IsBoss: false, Health: 1 } &&
                    !actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 12),
                    "Boundary did not clear the remaining enemy after the original miniboss died.");
            }
            current = actual;
        }
        if (fixture.TryGetProperty("TerminalSpawnPoints", out var samples))
        {
            bool observed = false;
            foreach (var sample in samples.EnumerateArray())
            {
                int[] retiring = sample.GetProperty("RetiringEnemyIds").Deserialize<int[]>()!;
                if (!retiring.Contains(12)) continue;
                BattleSpawnPoints raw = sample.GetProperty("Raw").Deserialize<BattleSpawnPoints>()!;
                BattleSpawnPoints projected = sample.GetProperty("Canonical").Deserialize<BattleSpawnPoints>()!;
                Require(sample.GetProperty("Terminal").GetBoolean() && sample.GetProperty("Won").GetBoolean() && retiring.SequenceEqual([12]) &&
                    !sample.GetProperty("LivingUnitIds").Deserialize<int[]>()!.Contains(12) &&
                    raw.Group(0, CombatTeam.Enemy)!.Occupants[1] == 12 &&
                    projected.Group(0, CombatTeam.Enemy)!.Occupants[1] == 0 &&
                    projected.Units.Single(unit => unit.UnitId == 12) is { Current: null, LastKnown: null },
                    "Raw enemy slot and logical terminal ownership were not retained separately.");
                observed = true;
            }
            Require(observed, "Missing native raw slot/retiring enemy readback.");
        }
        var overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("PurityFailures").GetInt32() == 0 && overlay.GetProperty("Failures").GetInt32() == 0 &&
            !overlay.GetProperty("InTestScenario").GetBoolean(), "Player capture was not pure/original.");
        Console.WriteLine("TERMINAL-ENEMY-SLOT PASS:37 native inputs/full states/forecasts/RNG; historical removed enemy slot kept as raw evidence, terminal logical ownership calibrated; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        Require(source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")) &&
            replay.GetProperty("TerminalSpawnPoints").GetArrayLength() > 0, "Independent native request/readback changed.");
        var first = source.GetProperty("Rows").EnumerateArray().ToArray();
        var second = replay.GetProperty("Rows").EnumerateArray().ToArray();
        for (int index = 0; index < first.Length; index++)
        {
            bool terminal = index == first.Length - 1;
            Require(first[index].GetProperty("Action").ContentEquals(second[index].GetProperty("Action")) &&
                first[index].GetProperty("IsEndTurn").ContentEquals(second[index].GetProperty("IsEndTurn")) &&
                Key(first[index].GetProperty("Before").Deserialize<BattleTurnState>()!) ==
                    Key(second[index].GetProperty("Before").Deserialize<BattleTurnState>()!), "Independent input/root changed.");
            BattleTurnState a = first[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            BattleTurnState b = second[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string aKey = Key(terminal ? ProjectHistoricalSlot(a) : a, terminal), bKey = Key(b, terminal);
            Require(aKey == bKey, "Independent native successor/RNG changed at " + index + ": " + ModelJson.Difference(aKey, bKey));
            if (terminal) Require(a.Spawn.Train.Context!.SpawnPoints!.Group(0, CombatTeam.Enemy)!.Occupants[1] == 12 &&
                b.Spawn.Train.Context!.SpawnPoints!.Group(0, CombatTeam.Enemy)!.Occupants[1] == 0,
                "Preserve the original stale-slot failure and the repaired terminal projection.");
        }
        Console.WriteLine("TERMINAL-ENEMY-SLOT-PAIR PASS:only the proven retired enemy terminal slot changes; raw ownership remains archived.");
    }
    private static BattleTurnState ProjectHistoricalSlot(BattleTurnState state)
    {
        var points = state.Spawn.Train.Context!.SpawnPoints!;
        var owner = points.Units.Single(unit => unit.UnitId == 12);
        int slot = points.Group(0, CombatTeam.Enemy)!.Occupants[1];
        Require(slot is 0 or 12 && owner.Current == null && owner.LastKnown == null,
            "Projection is limited to the observed retired enemy slot.");
        if (slot == 0) return state;
        var json = JsonSerializer.SerializeToNode(state, ModelJson.Options)!;
        json["Spawn"]!["Train"]!["Context"]!["SpawnPoints"]!["Groups"]![0]!["Occupants"]![1] = 0;
        return json.Deserialize<BattleTurnState>(ModelJson.Options)!;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
