using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ResumedMoonCaptureChecks
{
    internal static void Pair(FixtureValue counterexample, FixtureValue source, FixtureValue replay)
    {
        Require(counterexample.GetProperty("Unsupported").GetInt32() == 23 &&
            counterexample.GetProperty("NativeWon").GetBoolean() &&
            counterexample.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Published").GetInt32() == 25,
            "Missing the native win rejected only by the independent probe's moon capture.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Pending" })
            Require(counterexample.GetProperty(counter).GetInt32() == 0, "Changed original failure boundary.");
        var rejected = counterexample.GetProperty("Actions").EnumerateArray().ToArray();
        Require(rejected.Length == 18 && rejected.All(row => row.GetProperty("Before")
            .Deserialize<BattleTurnState>()!.ExternalInteractions.Contains("Moon phase unit/card effects")),
            "The old probe must retain its actual blanket moon rejection.");
        Require(counterexample.GetProperty("NativeRunInput").ContentEquals(source.GetProperty("NativeRunInput")),
            "Moon capture repair changed the resumed native campaign input.");
        var before = counterexample.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var after = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(before.Length == 23 && after.Length == 23, "Changed the original player route length.");
        for (int i = 0; i < before.Length; i++)
        foreach (string field in new[] { "Root", "Action", "IsEndTurn", "Expected", "Outcome", "PlanSteps", "Planned", "PlannedOutcome", "Score" })
            Require(before[i].GetProperty(field).ContentEquals(after[i].GetProperty(field)),
                "Probe synchronization changed the live player's route or forecast at " + i + "/" + field);
        foreach (var fixture in new[] { source, replay })
        {
            foreach (var row in fixture.GetProperty("Actions").EnumerateArray())
            {
                var state = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                Require(!state.ExternalInteractions.Contains("Moon phase unit/card effects") &&
                    state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0)
                        .All(unit => unit.MoonPhaseMask.HasValue), "The probe lost the real moon masks on resume.");
            }
            var final = fixture.GetProperty("RollingDecisions").EnumerateArray().Last()
                .GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(final.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 80,
                "Changed original zero-loss native result.");
        }
        RandomCampaignChecks.Pair(source, replay, 934692550, 0, 0, "Underlegion", "LunaCoven", 10000);
        Console.WriteLine("RESUMED-MOON-CAPTURE PASS: unchanged23-input live plan/forecasts/RNG, probe23unsupported->0, " +
            "captured moon masks, independent native wins/Pyre80; no clan override or model bypass.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
