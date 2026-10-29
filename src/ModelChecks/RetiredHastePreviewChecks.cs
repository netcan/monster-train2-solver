using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetiredHastePreviewChecks
{
    internal static void Native(FixtureValue source, FixtureValue replay, FixtureValue repaired, FixtureValue repairedReplay,
        bool combat = false)
    {
        const string reason = "Native rolling turn successor mismatch observation.";
        void Historical(BattleTurnState expected, BattleTurnState actual)
        {
            Require(expected.Spawn.Train.Context!.NextUnitId == (combat ? 22 : 21) && actual.Spawn.Train.Context!.NextUnitId == (combat ? 21 : 20),
                "Historical retired Haste identity mismatch changed.");
        }
        var rows = NativeBranchObservationChecks.Replay(source, reason, Historical);
        NativeBranchObservationChecks.Replay(replay, reason, Historical);
        NativeBranchObservationChecks.Replay(repaired, reason);
        NativeBranchObservationChecks.Replay(repairedReplay, reason);
        NativeBranchObservationChecks.Pair(source, repaired, originalPredictionPresent: true);
        NativeBranchObservationChecks.Pair(replay, repairedReplay, originalPredictionPresent: true);
        NativeBranchObservationChecks.Pair(repaired, repairedReplay, originalPredictionPresent: true, originalPredictionMatched: true);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == (combat ? 20 : 19) && before.Spawn.Train.Context!.Enchantments!.EnchanterIds.Count == 2,
            "Wrong original retired Haste boundary.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Independent retired Haste preview branch differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Preview branches mutated their parent.");
        Console.WriteLine($"RETIRED-HASTE-PREVIEW PASS: two historical and two repaired native observations, {rows.Length - 1} matched inputs, full turn successor/RNG and four immutable branches.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
