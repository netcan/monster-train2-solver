using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RecursionDrawCallbacksChecks
{
    private const string Reason = "Native returned spell temporary trait lifecycle.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (replay != null)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("RECURSION-DRAW-CALLBACKS PASS:46 full native inputs/state/forecasts/RNG; ReturnedSoul returns Permafrost Benediction with one temporary Freeze;" +
            (replay == null ? " independent replay pending." : " independent corrected native pair."));
    }
    private static void Check(FixtureValue fixture, bool historical)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historical ? HistoricalMismatch : null);
        Require(rows.Length == 46 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Incomplete original46-input prefix.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(before.Spawn.Turn == 9 && after.Spawn.Turn == 9 && action.CardInstanceId == 14 &&
            action.RecursionSelections.Single().CardInstanceIds.SequenceEqual([15]) &&
            Card(before).MaskDescriptor!.TemporaryTraits.Count == 0 &&
            Card(after).MaskDescriptor!.TemporaryTraits.Count(trait => trait.RuntimeType == "CardTraitFreeze") == 1 &&
            after.Spawn.Train.Context!.Cards.Hand[0].InstanceId == 15 &&
            Card(after).Temporary.Upgrades.Any(upgrade => upgrade.DataId == "2d744be5-cf16-4126-83fa-d6df9d99229c"),
            "Missing native selected return, Corrupt enhancement or immediate temporary Freeze.");
    }
    private static void HistoricalMismatch(BattleTurnState historical, BattleTurnState actual) =>
        Require(Card(historical).MaskDescriptor!.TemporaryTraits.Count == 0 &&
            Card(actual).MaskDescriptor!.TemporaryTraits.Single().RuntimeType == "CardTraitFreeze",
            "Historical missing draw trait was replaced.");
    private static CardInstanceState Card(BattleTurnState state) => state.Spawn.Train.Context!.FindCard(15)!;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
