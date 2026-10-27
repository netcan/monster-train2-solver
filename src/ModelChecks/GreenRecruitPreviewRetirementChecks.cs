using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GreenRecruitPreviewRetirementChecks
{
    internal static void Native(FixtureValue fixture, string path)
    {
        Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("Mismatches").GetInt32() == 3 &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0, "Missing original won battle with three historical component differences.");
        var stages = fixture.GetProperty("Stages").EnumerateArray().Where(stage =>
            stage.GetProperty("Difference").ValueKind == FixtureKind.String).ToArray();
        Require(stages.Select(stage => stage.GetProperty("Index").GetInt32()).SequenceEqual([46, 47]),
            "Changed historical room/one-exchange mismatch boundary.");
        var before = stages[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = stages[0].GetProperty("Actual").Deserialize<RoomCombatState>()!;
        var context = before.Context!;
        var birth = context.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 30);
        var retired = actual.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 30);
        Require(before.RoomIndex == 0 && birth.RoomIndex == 1 && birth.Preview && birth.Unit.Health == 16 &&
            birth.Unit.Status("horde")?.Stacks == 4 && birth.Unit.Modifiers!.Upgrades.Count == 0 &&
            birth.Unit.DeathState?.IsDestroyed == false && retired.RoomIndex == -1 && retired.Unit.Health == 16 &&
            retired.Unit.DeathState is { IsDestroyed: true, HasStatisticsListener: false, StatisticsListenerOnce: false } &&
            actual.Context.SpawnPoints!.Units.Single(point => point.UnitId == 30) is { Current: null, LastKnown: null },
            "Native preview retirement changed numeric state or failed to clear object ownership/listener.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var baseline = RoomCombatModel.Resolve(before);
        Require(baseline.Supported && Key(baseline.State!) == Key(actual),
            "Native preview cleanup difference: " + (baseline.Supported ? ModelJson.Difference(Key(baseline.State!), Key(actual)) : baseline.UnsupportedReason));
        Parallel.For(0, 4, _ => {
            var result = RoomCombatModel.Resolve(before);
            Require(result.Supported && Key(result.State!) == Key(actual), "Green Recruit preview cleanup differs from native room/context/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Preview cleanup mutated its parent.");
        foreach (string control in new[] { "Primary", "SameRoom", "DifferentStacks", "Upgrade", "MissingOwner", "MissingPoint" }) {
            var node = JsonSerializer.SerializeToNode(context, ModelJson.Options)!;
            var actor = node["Enchantments"]!["RetainedUnits"]!.AsArray().Single(item => item!["Unit"]!["Id"]!.GetValue<int>() == 30)!;
            if (control == "Primary") actor["Preview"] = false;
            if (control == "SameRoom") actor["RoomIndex"] = 0;
            if (control == "DifferentStacks") actor["Unit"]!["Statuses"]!.AsArray().Single(status => status!["Id"]!.GetValue<string>() == "horde")!["Stacks"] = 3;
            if (control == "Upgrade") actor["Unit"]!["Modifiers"]!["Upgrades"]!.AsArray().Add(
                JsonSerializer.SerializeToNode(context.FindCard(24)!.Permanent.Upgrades[0], ModelJson.Options));
            if (control == "MissingOwner") foreach (var owner in node["Enchantments"]!["RetainedUnits"]!.AsArray())
                if (owner!["Unit"]!["AssetKey"]!.GetValue<string>() == "MonsterGenerousHost") owner["Unit"]!["AssetKey"] = "unknown-recruit";
            if (control == "MissingPoint") node["SpawnPoints"]!["Units"]!.AsArray().Single(point => point!["UnitId"]!.GetValue<int>() == 30)!["Current"] = null;
            var changed = node.Deserialize<CombatContext>(ModelJson.Options)!;
            var kept = PreviewBirthModel.CompleteRemoteCombatBirths(changed, 0);
            Require(ReferenceEquals(kept, changed), "Uncalibrated preview retirement changed a control: " + control);
        }
        RoomCombatChecks.NativeObservedRooms(path);
        TrainCombatChecks.Native(fixture);
        BattleTurnChecks.Native(fixture);
        Console.WriteLine("GREEN-RECRUIT-PREVIEW-RETIREMENT PASS: original complete native room/train/EndTurn states and RNG; preview four-stack object retired without numeric changes; four immutable branches and six scope controls");
    }
    private static string Key(RoomCombatState state) => JsonSerializer.Serialize(new { state.RoomIndex, state.Deployment,
        state.Units, Context = BattleTurnChecks.ComparableContext(state.Context!) });
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
