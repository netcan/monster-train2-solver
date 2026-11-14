using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChainedStatusChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "chained-status-calibration" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Actions").GetArrayLength() == 1 &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Incomplete native chained component carrier battle.");
        FixtureValue evidence = root.GetProperty("ChainedStatusCalibration"), definition = evidence.GetProperty("OwnerDefinition");
        Require(evidence.GetProperty("Prepared").GetBoolean() && definition.GetProperty("Id").GetString() == "d0294a77-7204-4e9e-9b22-7f38e0d59ad0" &&
            definition.GetProperty("Key").GetString() == "ChainedDamageIncrease" && !definition.GetProperty("Placement").GetBoolean() &&
            definition.GetProperty("Type").GetString() == RelicModel.AddStatusOnSpawn && definition.GetProperty("Team").GetString() == "Heroes" &&
            definition.GetProperty("Statuses").EnumerateArray().Single().GetProperty("statusId").GetString() == "chained" &&
            definition.GetProperty("Statuses").EnumerateArray().Single().GetProperty("count").GetInt32() == 1,
            "Original chained relic definition changed.");
        FixtureValue calibration = evidence.GetProperty("Calibration");
        Require(calibration.GetProperty("Boundary").GetString() == "NativeAdmissionAndInterceptedRawDamageRequests" &&
            calibration.GetProperty("Completed").GetBoolean() && calibration.GetProperty("LiveContextUnchanged").GetBoolean() &&
            calibration.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null, "Calibration scope/restoration differs.");
        TrainCombatState before = calibration.GetProperty("Before").Deserialize<TrainCombatState>()!;
        Equal(before, calibration.GetProperty("Actual").Deserialize<TrainCombatState>(), "Native complete-train restoration");
        Equal(calibration.GetProperty("RngBefore").ToObjectGraph(), calibration.GetProperty("RngAfter").ToObjectGraph(), "All native RNG restoration");
        Require(!before.Context!.Relics!.Any(relic => relic.DataId == "d0294a77-7204-4e9e-9b22-7f38e0d59ad0") &&
            !RoomCombatModel.KnowsStatus("chained"), "Admission/request calibration must not silently enable unmodeled battle damage.");
        FixtureValue[] tests = calibration.GetProperty("Tests").EnumerateArray().ToArray(),
            requests = calibration.GetProperty("Requests").EnumerateArray().ToArray();
        Require(tests.Length == 111 && requests.Length == 35 && tests.Select(row => row.GetProperty("DamageType").GetInt32()).Distinct().Order().SequenceEqual(Enumerable.Range(0, 13)) &&
            requests.Select(row => (row.GetProperty("Damage").GetInt32(), row.GetProperty("Stacks").GetInt32())).Distinct().Count() == 35,
            "Missing native damage-type, admission or signed arithmetic matrix.");
        foreach (FixtureValue row in tests) Admission(row);
        foreach (FixtureValue row in requests) Request(row);
        Parallel.For(0, 32, _ => { foreach (FixtureValue row in tests) Admission(row); foreach (FixtureValue row in requests) Request(row); });
        Require(!ChainedStatusModel.Test(null, false, 1, CombatTeam.Player, 1, 0) &&
            !ChainedStatusModel.Test(CombatTeam.Enemy, false, 1, CombatTeam.Player, 1, ChainedStatusModel.DamageType),
            "Missing-holder or no-recursion guard differs.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine("CHAINED-STATUS PASS: 111 native status-admission tests, 35 intercepted raw damage requests/magnitude results including signed overflow, complete live state/RNG restoration, 32 component branches and unchanged carrier battle with 16 full branches. Relic acquisition, real damage propagation, Bloodshed/death/revival queues remain unaccepted.");

        void Request(FixtureValue row)
        {
            int amount = row.GetProperty("Damage").GetInt32(), stacks = row.GetProperty("Stacks").GetInt32(), observed = row.GetProperty("ObservedStacks").GetInt32();
            FixtureValue request = row.GetProperty("Request");
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                request.GetProperty("Damage").GetInt32() == ChainedStatusModel.Damage(amount, observed) &&
                row.GetProperty("Magnitude").GetInt32() == ChainedStatusModel.Magnitude(stacks) &&
                request.GetProperty("DamageType").GetInt32() == ChainedStatusModel.DamageType &&
                request.GetProperty("AttackerId").GetInt32() == calibration.GetProperty("PlayerId").GetInt32() &&
                request.GetProperty("TargetId").GetInt32() == calibration.GetProperty("EnemyId").GetInt32() &&
                request.GetProperty("NullCard").GetBoolean() && request.GetProperty("NullRelic").GetBoolean(), "Independent native raw request differs.");
        }
    }
    private static void Admission(FixtureValue row)
    {
        int holder = row.GetProperty("Holder").GetInt32(), attacked = row.GetProperty("Attacked").GetInt32();
        bool dead = row.GetProperty("DeadAndUnrevivable").GetBoolean();
        Require(row.GetProperty("Completed").GetBoolean() && dead == (row.GetProperty("Health").GetInt32() <= 0 && row.GetProperty("Undying").GetInt32() == 0) &&
            ChainedStatusModel.Test(holder == 0 ? CombatTeam.Player : CombatTeam.Enemy, dead, row.GetProperty("Stacks").GetInt32(),
                attacked < 0 ? null : attacked == 0 ? CombatTeam.Player : CombatTeam.Enemy,
                row.GetProperty("Damage").GetInt32(), row.GetProperty("DamageType").GetInt32()) == row.GetProperty("NativeTest").GetBoolean(),
            "Independent native status admission differs.");
    }
    private static string Json(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? a, object? b, string label)
    { string? error = ModelJson.Difference(Json(a), Json(b)); Require(error == null, label + ": " + error); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
