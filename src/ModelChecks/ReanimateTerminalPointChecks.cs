using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ReanimateTerminalPointChecks
{
    internal static void Native(FixtureValue fixture, string path)
    {
        Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("Mismatches").GetInt32() == 3 &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0, "Missing original Reanimate terminal component counterexample.");
        var mismatches = fixture.GetProperty("Stages").EnumerateArray().Where(stage => stage.GetProperty("Difference").ValueKind == FixtureKind.String).ToArray();
        Require(mismatches.Select(stage => stage.GetProperty("Index").GetInt32()).SequenceEqual([75, 91]), "Wrong native Reanimate death stages.");
        foreach (var stage in mismatches) {
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var point = actual.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == 25);
            Require(before.RoomIndex == 2 && !before.Preview && point.Current == null && point.LastKnown is
                { RoomIndex: 2, Team: CombatTeam.Enemy, Index: 0, PreviewCopyId: null, OuterBossAnchor: false },
                "Native final death must retain the real inner point.");
            RoomCombatResult Resolve() => stage.GetProperty("Kind").GetString() == "Room" ?
                RoomCombatModel.Resolve(before) : RoomCombatModel.Exchange(before);
            var result = Resolve();
            Require(result.Supported && Key(result.State!) == Key(actual), "Reanimate final point differs: " +
                (result.Supported ? ModelJson.Difference(Key(result.State!), Key(actual)) : result.UnsupportedReason));
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 4, _ => {
                var branch = Resolve();
                Require(branch.Supported && Key(branch.State!) == Key(actual), "Reanimate final branch differs from full native context/RNG.");
            });
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Reanimate final branch mutated its parent.");
        }
        var exchange = mismatches.Single(stage => stage.GetProperty("Kind").GetString() == "Exchange")
            .GetProperty("Before").Deserialize<RoomCombatState>()!;
        foreach (string control in new[] { "Owner", "BaseAttack", "MaxHealth", "Reviving" }) {
            var node = JsonSerializer.SerializeToNode(exchange, ModelJson.Options)!;
            var boss = node["Units"]!.AsArray().Single(unit => unit!["Id"]!.GetValue<int>() == 25)!;
            if (control == "Owner") boss["AssetKey"] = "uncalibrated-inner-boss";
            if (control == "BaseAttack") boss["BaseAttack"] = 18;
            if (control == "MaxHealth") boss["MaxHealth"] = 501;
            if (control == "Reviving") boss["Statuses"]!.AsArray().Add(JsonSerializer.SerializeToNode(
                exchange.Units.Single(unit => unit.Id == 25).StatusRegistry!.Single(status => status.Id == "undying").WithStacks(9), ModelJson.Options));
            var result = RoomCombatModel.Exchange(node.Deserialize<RoomCombatState>(ModelJson.Options)!);
            Require(result.Supported, "Final-point scope control unsupported: " + control + ": " + result.UnsupportedReason);
            var point = result.State!.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == 25);
            Require(control == "Reviving" ? result.Outcome != RoomOutcome.BattleWon && point.Current != null : point.LastKnown == null,
                "Uncalibrated terminal point changed: " + control);
        }
        RoomCombatChecks.NativeObservedRooms(path);
        TrainCombatChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
        Console.WriteLine("REANIMATE-TERMINAL-POINT PASS: original complete native room/train/EndTurn states and RNG; final inner point retained after eight Undying revivals; four immutable branches per boundary/four scope controls.");
    }
    private static string Key(RoomCombatState state) => JsonSerializer.Serialize(new { state.RoomIndex, state.Deployment,
        state.Units, Context = BattleTurnChecks.ComparableContext(state.Context!) });
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
