using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossPhaseChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            fixture.GetProperty("Boundary").GetString() == "AfterBossActionRemoveDeadCharacters" &&
            fixture.GetProperty("Errors").GetArrayLength() == 0, "Native outer-boss phase build or settled boundary differs.");
        int count = 0, pre = 0, post = 0, moves = 0, initial = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            OuterBossPhaseState before = row.GetProperty("Before").Deserialize<OuterBossPhaseState>()!;
            OuterBossPhaseState actual = row.GetProperty("Actual").Deserialize<OuterBossPhaseState>()!;
            string phase = row.GetProperty("Phase").GetString()!;
            Require(row.GetProperty("NativeReturned").GetBoolean() && phase is "BossActionPreCombat" or "BossActionPostCombat",
                "Missing completed native outer-boss action.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            OuterBossPhaseResult predicted = OuterBossPhaseModel.Apply(before, phase == "BossActionPreCombat");
            Require(predicted.Supported, "Outer-boss phase unsupported at " + count + ": " + predicted.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options));
            Require(difference == null, "Outer-boss phase mismatch at " + count + ": " + difference);
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Outer-boss phase changed its model parent.");
            if (phase == "BossActionPreCombat") pre++; else post++;
            if (before.Boss.Room != actual.Boss.Room) moves++;
            if (before.Boss.Controller.InitialAction != null) initial++;
            count++;
        }
        Require(count >= 11 && pre >= 6 && post >= 5 && moves >= 5 && initial == 1,
            "Missing native initial action, moving casting actions or post-combat no-op controls.");
        Console.WriteLine($"OUTER-BOSS-PHASE PASS: {count} settled native actions ({pre} pre, {post} post), {moves} moves, {initial} initial latch; complete battle/controller/RNG equality and immutable parents.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
