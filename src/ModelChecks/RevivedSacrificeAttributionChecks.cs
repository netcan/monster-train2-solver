using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RevivedSacrificeAttributionChecks
{
    private const string Reason = "Native revived unit kill attribution differs after rolling input38";
    internal static void Pair(FixtureValue original, FixtureValue repaired)
    {
        var rows = NativeBranchObservationChecks.Replay(original, Reason, HistoricalDifference);
        NativeBranchObservationChecks.Replay(repaired, Reason);
        NativeBranchObservationChecks.Pair(original, repaired, originalPredictionPresent: true);
        Require(rows.Length == 38 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong revived-unit native boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actor = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 11);
        Require(before.Spawn.Turn == 4 && actor.AssetKey == "MonsterMeathook" && actor.Health == 1 &&
            actor.SpawnerCardId == 2 && actor.SacrificeCardId == 29 && actor.DeathState?.IsSacrifice == false,
            "Native revived Imp must retain its previous sacrifice card with IsSacrifice cleared.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == expected &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Parallel revived-unit attribution changed a native successor/RNG or parent.");
        });
        Console.WriteLine("REVIVED-SACRIFICE-ATTRIBUTION PASS: retained mistaken kill counters and independent repaired 38-input state/RNG; native IsSacrifice=false with old card29 retained, exact statistic-only historical difference and four immutable parallel branches.");
    }
    private static void HistoricalDifference(BattleTurnState predicted, BattleTurnState actual)
    {
        var a = actual.Spawn.Train.Context!.Statistics!;
        var p = predicted.Spawn.Train.Context!.Statistics!;
        var node = JsonSerializer.SerializeToNode(predicted, ModelJson.Options)!;
        node["Spawn"]!["Train"]!["Context"]!["Statistics"] = JsonSerializer.SerializeToNode(a, ModelJson.Options);
        Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) == NativeBranchObservationChecks.Key(actual),
            "Historical difference changed more than statistics.");
        static string Key(CardStatisticValue value) => value.CardId + "/" + value.Duration + "/" + value.Type;
        var pv = p.Values.ToDictionary(Key);
        var av = a.Values.ToDictionary(Key);
        var keys = pv.Keys.Concat(av.Keys).Distinct().ToArray();
        int differences = 0;
        foreach (string key in keys)
        {
            var value = pv.TryGetValue(key, out var old) ? old : av[key];
            int delta = (old?.Value ?? 0) - (av.TryGetValue(key, out var native) ? native.Value : 0);
            int extra = value.Duration is "ThisBattle" or "PreviousTurn" ?
                value.Type == "AnyHeroKilled" && a.TrackedCards.Contains(value.CardId) ? (value.CardId == 29 ? 2 : 1) :
                value.Type == "HeroesKilled" && value.CardId == 29 ? 1 : 0 : 0;
            Require(delta == extra, "Historical revived-unit statistic difference changed at " + key);
            if (delta != 0) differences++;
        }
        Require(differences == a.TrackedCards.Count * 2 + 2,
            "Expected exactly one extra kill event attributed to old sacrifice card29.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
