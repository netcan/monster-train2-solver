using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CompanionWorldRosterChecks
{
    internal static void Native(string sourcePath, string? replayPath = null)
    {
        using var sourceDocument = ModelJson.ReadFixture(sourcePath);
        FixtureValue source = sourceDocument.RootElement;
        Require(source.GetProperty("Mismatches").GetInt32() == 1 &&
            source.GetProperty("CaptureFailures").GetInt32() == 0 && source.GetProperty("Unsupported").GetInt32() == 0 &&
            source.GetProperty("Pending").GetInt32() == 0, "Preserve the one-stage historical counterexample.");
        FixtureValue[] gaps = source.GetProperty("Stages").EnumerateArray().Where(row =>
            row.GetProperty("Difference").ValueKind != FixtureKind.Null).ToArray();
        Require(gaps.Length == 1 && gaps[0].GetProperty("Index").GetInt32() == 75 &&
            gaps[0].GetProperty("Kind").GetString() == "Exchange", "Wrong native companion-death boundary.");
        FixtureValue stage = gaps[0];
        RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var externalTargets = stage.GetProperty("ExternalTargets").EnumerateArray().Select(target =>
            (target.GetProperty("RoomIndex").GetInt32(), target.GetProperty("Unit").Deserialize<CombatUnit>()!)).ToArray();
        RoomCombatState historical = stage.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!;
        RoomCombatState actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        Require(before.RoomIndex == 0 && !before.Preview && before.ActiveCompanionBossIds!.SequenceEqual([6, 8]) &&
            before.Units.Single(unit => unit.Id == 6).Health == 149 && actual.Units.All(unit => unit.Id != 6) &&
            actual.Units.Single(unit => unit.Id == 8).Health == 912 &&
            actual.Context!.Enchantments!.Rooms.All(room => room.ActiveCompanionBossIds!.SequenceEqual([8])) &&
            historical.Context!.Enchantments!.Rooms.Where(room => room.RoomIndex != 0)
                .All(room => room.ActiveCompanionBossIds!.SequenceEqual([6, 8])),
            "Missing native global roster cleanup after the local companion death.");
        Check(sourcePath, source, false);
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        // NativeRoomOnly above compares every calibrated native field. Use a
        // serial model branch for the additional raw serialization/concurrency
        // check, including model-only caches absent from the native projection.
        RoomCombatResult serial = RoomCombatModel.Exchange(before, externalTargets: externalTargets);
        Require(serial.Supported && serial.State!.Context!.Enchantments!.Rooms.All(room =>
            room.ActiveCompanionBossIds!.SequenceEqual([8])), "Serial exchange retained the removed companion.");
        string expected = JsonSerializer.Serialize(serial.State, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            RoomCombatResult result = RoomCombatModel.Exchange(before, externalTargets: externalTargets);
            Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) == expected,
                "Concurrent companion-death exchange differs from the full native state/RNG.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Companion removal mutated its shared parent.");
        if (replayPath != null)
        {
            using var replayDocument = ModelJson.ReadFixture(replayPath);
            FixtureValue replay = replayDocument.RootElement;
            Check(replayPath, replay, true);
            foreach (string collection in new[] { "Stages", "TrainPhases", "Actions", "Turns", "CardCycles" })
            {
                FixtureValue[] first = source.GetProperty(collection).EnumerateArray().ToArray();
                FixtureValue[] second = replay.GetProperty(collection).EnumerateArray().ToArray();
                Require(first.Length == second.Length && first.Length > 0, "Independent native record count changed: " + collection);
                for (int index = 0; index < first.Length; index++)
                {
                    foreach (string field in new[] { "Before", "Actual" })
                        Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)),
                            "Independent native state changed: " + collection + index + field);
                    if (collection == "Actions") Require(first[index].GetProperty("Action").ContentEquals(second[index].GetProperty("Action")),
                        "Independent native paid input changed.");
                }
            }
        }
        Console.WriteLine("COMPANION-WORLD-ROSTER PASS:98 native room stages,22 train phases,52 complete paid/EndTurn successors/RNG; " +
            "remote floors share the live companion roster; immutable parallel Exchange; " +
            (replayPath == null ? "independent replay pending." : "zero-counter independent frozen defeat, no victory claimed."));
    }

    private static void Check(string path, FixtureValue fixture, bool replay)
    {
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("Seed").GetInt32() == 1895012796 && input.GetProperty("Distance").GetInt32() == 5 &&
            input.GetProperty("AscensionLevel").GetInt32() == 0 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            !fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            fixture.GetProperty("RollingDecisions").GetArrayLength() == 52 && fixture.GetProperty("Stages").GetArrayLength() == 98 &&
            fixture.GetProperty("TrainPhases").GetArrayLength() == 22,
            "Expected the unchanged natural campaign10 Ring6 defeat.");
        if (replay) RollingAdviceChecks.Native(fixture);
        else RollingAdviceChecks.ObservedSuccessors(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(fixture);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
