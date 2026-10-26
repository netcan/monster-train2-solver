using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnownedRelicCopyChecks
{
    internal static void Pair(FixtureValue original, FixtureValue repaired)
    {
        Require(original.GetProperty("Mismatches").GetInt32() == 13 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(name => original.GetProperty(name).GetInt32() == 0) &&
            !original.GetProperty("NativeWon").GetBoolean() && !repaired.GetProperty("NativeWon").GetBoolean() &&
            original.GetProperty("NativeRunInput").ContentEquals(repaired.GetProperty("NativeRunInput")),
            "Expected the retained thirteen diagnostic mismatches and the same native defeat/checkpoint.");
        RollingAdviceChecks.ObservedSuccessors(original);
        RollingAdviceChecks.Native(repaired);
        TrainCombatChecks.Native(repaired);
        var first = original.GetProperty("RollingDecisions");
        var second = repaired.GetProperty("RollingDecisions");
        Require(first.GetArrayLength() == 30 && second.GetArrayLength() == 30 &&
            repaired.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Published").GetInt32() == 0,
            "Expected thirty unchanged inputs and a zero-search native replay.");
        for (int index = 0; index < 30; index++)
        {
            foreach (string name in new[] { "Root", "Expected", "Actual", "Planned" })
                Require(NativeBranchObservationChecks.Key(first[index].GetProperty(name).Deserialize<BattleTurnState>()!, index == 29) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(name).Deserialize<BattleTurnState>()!, index == 29),
                    "Diagnostic scope changed native player state/forecast/RNG at " + index + "/" + name);
            foreach (string name in new[] { "Action", "IsEndTurn", "PlanSteps", "PlannedOutcome", "Score", "PolicyBaselineScore", "Generation" })
                Require(first[index].GetProperty(name).ContentEquals(second[index].GetProperty(name)),
                    "Diagnostic scope changed a frozen input/forecast at " + index + "/" + name);
        }
        var clones = original.GetProperty("DetachedCardClones");
        var copies = repaired.GetProperty("UnownedRelicCopyObservations");
        Require(clones.GetArrayLength() == 13 && copies.GetArrayLength() == 13 &&
            repaired.GetProperty("DetachedCardClones").GetArrayLength() == 0,
            "Missing original copy calls or unrelated birth clones were included.");
        for (int index = 0; index < 13; index++)
        {
            var row = copies[index];
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("IndependentReference").GetBoolean() &&
                row.GetProperty("BirthClonesBefore").GetInt32() == 0 && row.GetProperty("BirthClonesAfter").GetInt32() == 0 &&
                row.GetProperty("SourceCardId").ContentEquals(clones[index].GetProperty("SourceCardId")) &&
                row.GetProperty("SourceDataId").ContentEquals(clones[index].GetProperty("Creation").GetProperty("DataId")) &&
                row.GetProperty("SourceDataId").ContentEquals(row.GetProperty("CopyDataId")),
                "Original native recast copy/order was changed or assigned a birth identity.");
        }
        Console.WriteLine("UNOWNED-RELIC-COPY PASS: thirteen original recast copies retained as native observations; thirty unchanged player inputs/full forecasts/state/RNG, zero-search independent replay and four zero failure counters. Original diagnostic refusals preserved; native defeat, not a win.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
