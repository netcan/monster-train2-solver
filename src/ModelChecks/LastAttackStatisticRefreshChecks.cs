using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LastAttackStatisticRefreshChecks
{
    private const string Reason = "Native enemy death card-statistics retention.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        BattleStatisticsChecks.Run();
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 35 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong statistic-refresh prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = after.Spawn.Train.Context!;
        Require(before.Spawn.Turn == 4 && after.Spawn.Turn == 5 &&
            before.Spawn.Train.Context!.FindCard(35) == null &&
            context.FindCard(35)!.DataId == "356a00a4-010b-462b-b683-2ef296d076a8" &&
            context.Cards.Hand.Any(card => card.InstanceId == 35) &&
            context.Statistics!.StoredCards!.SequenceEqual(Enumerable.Range(1, 35)) &&
            context.Statistics.TrackedCards.SequenceEqual(Enumerable.Range(1, 35)) &&
            context.Statistics.Values.All(value => value.CardId != 35),
            "Missing generated Sting's lazily materialized empty statistic entry.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(after),
                "Parallel scalar statistic-query replay differs from native.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Statistic query mutated its parent.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second, originalPredictionPresent: true);
        }
        Console.WriteLine("LAST-ATTACK-STATISTIC-REFRESH PASS:35 native inputs/full state/forecasts/RNG; generated Sting's empty stored entry; original omitted entry preserved; eight immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        var expected = predicted.Spawn.Train.Context!.Statistics!;
        var native = actual.Spawn.Train.Context!.Statistics!;
        Require(expected.StoredCards!.SequenceEqual(Enumerable.Range(1, 34)) && native.StoredCards!.SequenceEqual(Enumerable.Range(1, 35)),
            "Original statistic mismatch changed.");
        var repaired = JsonSerializer.SerializeToNode(predicted, ModelJson.Options)!;
        repaired["Spawn"]!["Train"]!["Context"]!["Statistics"]!["StoredCards"] = JsonSerializer.SerializeToNode(native.StoredCards, ModelJson.Options);
        Require(NativeBranchObservationChecks.Key(repaired.Deserialize<BattleTurnState>(ModelJson.Options)!) == NativeBranchObservationChecks.Key(actual),
            "Historical discrepancy includes another field.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
