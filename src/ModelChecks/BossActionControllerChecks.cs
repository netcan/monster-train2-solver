using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BossActionControllerChecks
{
    internal static void Native(FixtureValue fixture, string expectedBoss = "Boss_Level8_AllyBuffer", int minimumOperations = 10,
        IReadOnlyCollection<string>? requiredOperations = null)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 && fixture.GetProperty("DecisionUnchanged").GetBoolean() &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("Definition").GetProperty("Boss").GetString() == expectedBoss,
            "Native boss controller build, owner or state restoration differs.");
        Require(fixture.GetProperty("Errors").GetArrayLength() == 0 &&
            fixture.GetProperty("FrameBefore").GetInt32() == fixture.GetProperty("FrameAfter").GetInt32() &&
            JsonSerializer.Serialize(fixture.GetProperty("RngBefore")) == JsonSerializer.Serialize(fixture.GetProperty("RngAfter")),
            "Native controller calibration changed a frame/RNG or failed observation.");
        int natural = 0, controls = 0, frozen = 0, destroyed = 0, exhausted = 0, skipped = 0;
        var operations = new HashSet<string>();
        foreach (string group in new[] { "Actions", "Controls" })
        foreach (FixtureValue row in fixture.GetProperty(group).EnumerateArray())
        {
            BossActionControllerProgress before = row.GetProperty("Before").Deserialize<BossActionControllerProgress>()!;
            BossActionControllerProgress actual = row.GetProperty("Actual").Deserialize<BossActionControllerProgress>()!;
            BossActionReference? next = row.GetProperty("NativeNext").Deserialize<BossActionReference>();
            string operation = row.GetProperty("Operation").GetString()!;
            string parent = JsonSerializer.Serialize(before);
            BossActionControllerResult predicted = BossActionControllerModel.Apply(before, operation, row.GetProperty("DestroyRound").GetBoolean());
            Require(predicted.Supported && JsonSerializer.Serialize(predicted.State) == JsonSerializer.Serialize(actual) &&
                JsonSerializer.Serialize(predicted.State!.NextAction) == JsonSerializer.Serialize(next),
                "Native boss controller progression or action identity differs in " + group + " at " + (natural + controls));
            Require(JsonSerializer.Serialize(before) == parent, "Boss controller changed the model parent branch.");
            if (group == "Actions") { natural++; operations.Add(operation); }
            else
            {
                controls++;
                if (before.InitialAction != null && operation == "DecideNext") frozen++;
                if (actual.CurrentGroup == actual.Groups.Count) destroyed++;
                if (actual.Groups.Count > 0 && actual.GroupIndex == actual.Groups.Count - 1 && actual.Groups.Last().Repeats < 0) exhausted++;
                if (before.Groups.Any(value => value.Executable.Any(flag => !flag)) && operation == "DecideNext") skipped++;
            }
        }
        Require(natural >= minimumOperations && operations.SetEquals(requiredOperations ?? new[] { "DecideInitial", "DecideNext", "Perform" }) &&
            controls == 1152 && frozen > 0 && destroyed > 0 && exhausted > 0 && skipped > 0,
            "Missing native boss initial latch, destruction group, exhausted final group or invalid-action controls.");
        Console.WriteLine($"BOSS-CONTROLLER PASS: {natural} actual operations, {controls} detached transitions; " +
            $"initial latch={frozen}, destruction group={destroyed}, exhausted final group={exhausted}, invalid-action cases={skipped}; exact action identities/RNG and immutable parents.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
