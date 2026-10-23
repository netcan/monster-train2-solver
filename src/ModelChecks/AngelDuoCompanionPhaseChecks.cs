using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AngelDuoCompanionPhaseChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Errors").GetArrayLength() == 0, "Native companion phase capture failed.");
        int changes = 0, rows = 0;
        foreach (FixtureValue row in fixture.GetProperty("Rows").EnumerateArray())
        {
            Require(row.GetProperty("NativeReturned").GetBoolean() &&
                row.GetProperty("Phase").GetString() == "BossActionPreCombat", "Native companion phase did not return.");
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string root = Json(before), expected = Json(actual);
            BattleTurnResult result = AngelDuoCompanionPhaseModel.Apply(before, true);
            Require(result.Supported, result.UnsupportedReason ?? "Unsupported original companion phase.");
            string? difference = ModelJson.Difference(Json(result.State), expected);
            Require(difference == null, "Native companion phase differs: " + difference);
            if (root != expected) changes++;
            Parallel.For(0, 16, _ => Require(Json(AngelDuoCompanionPhaseModel.Apply(before, true).State) == expected &&
                Json(before) == root, "Companion phase branch changed complete native state/RNG or its input."));
            rows++;
        }
        Require(rows > 0, "No native companion phase boundaries.");
        Console.WriteLine("ANGEL-DUO-COMPANION-PHASE PASS: " + rows + " complete native boundaries, " + changes +
            " positive transition(s), original movement/status/selection and sixteen immutable branches per boundary.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
