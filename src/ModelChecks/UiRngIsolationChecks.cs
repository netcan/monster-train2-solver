using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UiRngIsolationChecks
{
    internal static void Native(string path)
    {
        using FixtureDocument document = ModelJson.ReadFixture(path);
        Require(document.RootElement.GetProperty("Enabled").GetBoolean(), "UI RNG isolation calibration was not enabled.");
        CheckRecords(document.RootElement.GetProperty("Records"));
    }
    internal static void CheckRecords(FixtureValue records)
    {
        int uiCalls = 0, uiDraws = 0;
        foreach (FixtureValue scope in records.EnumerateArray())
        {
            uiCalls++;
            UnityRng uiBefore = scope.GetProperty("BattleBefore").Deserialize<UnityRng>();
            UnityRng observed = scope.GetProperty("BattleObserved").Deserialize<UnityRng>();
            if (!uiBefore.Equals(observed)) uiDraws++;
            Require(uiBefore.Equals(scope.GetProperty("BattleAfter").Deserialize<UnityRng>()) &&
                scope.GetProperty("TestBefore").Deserialize<UnityRng>().Equals(scope.GetProperty("TestAfter").Deserialize<UnityRng>()) &&
                scope.GetProperty("BattleSeedBefore").GetInt32() == scope.GetProperty("BattleSeedAfter").GetInt32() &&
                scope.GetProperty("TestSeedBefore").GetInt32() == scope.GetProperty("TestSeedAfter").GetInt32() && !scope.GetProperty("Exception").GetBoolean() &&
                scope.GetProperty("Result").ValueKind is FixtureKind.True or FixtureKind.False,
                "UI query failed to restore complete states/seeds or preserve its result.");
        }
        Require(uiCalls > 0 && uiDraws > 0, "The UI isolation oracle has no actual Battle draws to restore.");
        Console.WriteLine($"NATIVE-UI-RNG-ISOLATION PASS: {uiCalls} original UI queries, {uiDraws} Battle-consuming queries restored, both stream states/seeds preserved.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
