using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleJuiceSearchChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var root = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        if (rows.Length != 22 || root.Spawn.Turn != 4) throw new InvalidOperationException("Wrong natural search failure.");
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        var effect = root.PlayRules!.Cards.SelectMany(rule => rule.OnUnplayedNegativeEffects).Single(effect => effect.Type == "UpgradeDeckSpells");
        var altered = JsonSerializer.SerializeToNode(effect, ModelJson.Options)!;
        altered["Upgrade"]!["DataId"] = "unknown-curse";
        var upgraded = DeckSpellUpgradeModel.Apply(root.Spawn.Train.Context!, altered.Deserialize<CardActionEffect>(ModelJson.Options)!, root.PlayRules);
        if (upgraded.Supported || upgraded.UnsupportedReason != "Triple Juice with this card or ordered scaling traits requires native calibration.")
            throw new InvalidOperationException("Uncalibrated post-upgrade branch lost its explicit guard: " + upgraded.UnsupportedReason);
        var options = new BattlePlannerOptions(256, 3, 8, 12, TimeSpan.FromSeconds(10),
            allowShorterHorizon: true, searchToBattleEnd: true, maxSteps: 256, stopAtBestPyreHealth: true);
        var result = ParallelBattlePlanner.Plan(root, options, 2);
        if (result.Steps.Count == 0 || !result.CompletedHorizon && !result.UsedShorterHorizon)
            throw new InvalidOperationException("No complete continuation after triple Juice draw.");
        var predicted = BattlePlannerChecks.Replay(root, result.Steps.ToArray(), out RoomOutcome outcome);
        if (outcome != result.Outcome || NativeBranchObservationChecks.Key(predicted) != NativeBranchObservationChecks.Key(result.State) ||
            parent != JsonSerializer.Serialize(root, ModelJson.Options))
            throw new InvalidOperationException("Search prediction or immutable failure root changed.");
        Console.WriteLine("TRIPLE-JUICE-SEARCH PASS: natural turn4 player failure root searches a verified continuation with two workers.");
    }
}
