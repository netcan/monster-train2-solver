using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStatusMagnitudeDeploymentChecks
{
    internal static void Check(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalMoonStatusMagnitudeDeployment" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Unexpected original deployment capture identity.");

        var deployment = root.GetProperty("Deployment");
        var before = deployment.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = deployment.GetProperty("After").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        var endTurn = BattleTurnModel.EndTurn(before);
        Require(before.Spawn.Turn == 0 && after.Spawn.Turn == 1 && before.MoonPhase == "New" && after.MoonPhase == "Full" &&
            before.MoonPhaseTrackingActive == true && after.MoonPhaseTrackingActive == true &&
            endTurn.Outcome == RoomOutcome.Exchanged && endTurn.Supported && keys.Key(endTurn.State!) == keys.Key(after),
            "Settled deployment EndTurn differs from the original full state/RNG: " + endTurn.UnsupportedReason);

        var phases = root.GetProperty("TrainPhases").EnumerateArray().ToArray();
        Require(phases.Length == 2 && phases[0].GetProperty("Kind").GetString() == "Combat" &&
            phases[1].GetProperty("Kind").GetString() == "Ascend" && phases.All(row => row.GetProperty("Turn").GetInt32() == 0),
            "Missing original deployment combat and ascent diagnostics.");
        for (int index = 0; index < phases.Length; index++)
        {
            FixtureValue row = phases[index];
            var input = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            FixtureValue recorded = row.GetProperty("Predicted").GetProperty("State");
            FixtureValue native = row.GetProperty("Actual");
            var current = index == 0 ? TrainCombatModel.ResolveCombat(input, RecordedBattleChecks.Decisions(row)) : TrainCombatModel.Ascend(input);
            Require(current.Supported && keys.SnapshotKey(current.State) == keys.SnapshotKey(recorded.Deserialize<TrainCombatState>()),
                "The current train prediction differs from the original diagnostic prediction.");
            if (index == 0) CheckCombatDiagnostic(recorded, native);
            else Require(recorded.ContentEquals(native), "The raw ascent snapshots differ.");
        }
        Console.WriteLine("STATUS-MAGNITUDE-DEPLOYMENT PASS: exact settled EndTurn and RNG, exact raw ascent, one phase difference in the raw combat context and its four room mirrors.");
    }

    private static void CheckCombatDiagnostic(FixtureValue predicted, FixtureValue native)
    {
        CheckEqualProperties(predicted, native, "Context", "Rooms");
        CheckContext(predicted.GetProperty("Context"), native.GetProperty("Context"));
        var predictedRooms = predicted.GetProperty("Rooms");
        var nativeRooms = native.GetProperty("Rooms");
        Require(predictedRooms.GetArrayLength() == 4 && nativeRooms.GetArrayLength() == 4, "Unexpected deployment room count.");
        for (int index = 0; index < 4; index++)
        {
            CheckEqualProperties(predictedRooms[index], nativeRooms[index], "Context");
            CheckContext(predictedRooms[index].GetProperty("Context"), nativeRooms[index].GetProperty("Context"));
        }

        static void CheckContext(FixtureValue expected, FixtureValue actual)
        {
            CheckEqualProperties(expected, actual, "EnergyState");
            var energy = expected.GetProperty("EnergyState");
            var nativeEnergy = actual.GetProperty("EnergyState");
            CheckEqualProperties(energy, nativeEnergy, "Phase");
            Require(energy.GetProperty("Phase").GetString() == "Combat" &&
                nativeEnergy.GetProperty("Phase").GetString() == "MonsterTurnQueueClear", "Unexpected raw combat phase difference.");
        }
    }

    private static void CheckEqualProperties(FixtureValue expected, FixtureValue actual, params string[] exceptions)
    {
        var fields = expected.EnumerateObject().Where(field => !exceptions.Contains(field.Name)).ToArray();
        Require(fields.Length == actual.EnumerateObject().Count(field => !exceptions.Contains(field.Name)), "Additional raw combat diagnostic fields.");
        foreach (var field in fields)
            Require(actual.TryGetProperty(field.Name, out var value) && field.Value.ContentEquals(value),
                "Unexpected raw combat diagnostic difference in " + field.Name + ".");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
