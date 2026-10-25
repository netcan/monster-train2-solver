using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EphemeralConsumeOrderChecks
{
    private const string Reason = "Ephemeral with later temporary Consume upgrade exhausts before its purge callback.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, Historical);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(33)!;
        Require(rows.Length == 41 && card.DataId == "f381b99b-518d-4818-b182-44d8f04110be" &&
            CardExhaustModel.EphemeralDiscardPrecedesConsume(card) &&
            !before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "CardExhausted")) &&
            actual.Spawn.Train.Context!.Statistics!.Value(33, "TimesExhausted") == 2 &&
            actual.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Count(item => item.InstanceId == 33) == 1 &&
            actual.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Count(item => item.InstanceId == 33) == 2,
            "Missing the later Consume callback's double exhaustion and native dual pile references.");
        var context = actual.Spawn.Train.Context!;
        Require(CardPileModel.ValidateMembership(context.Cards, actual.OtherPiles, context.NextCardId, context) == null &&
            CardPileModel.ValidateMembership(context.Cards, actual.OtherPiles, context.NextCardId) != null &&
            CardPileModel.ValidateMembership(context.Cards, actual.OtherPiles.Select(pile => pile.Name == "Exhausted"
                ? new CardPileState(pile.Name, pile.Cards.Concat(pile.Cards.Where(item => item.InstanceId == 33)).ToArray()) : pile).ToArray(),
                context.NextCardId, context) != null,
            "Native dual pile references must allow continuation only with matching private Consume state; extra duplicates must remain rejected.");
        Console.WriteLine("EPHEMERAL-CONSUME-ORDER PASS:41 native inputs/full successors/RNG; later temporary Consume records two exhaustions and retains exhausted/purged references; immutable parents.");
    }
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        Require(predicted.Spawn.Train.Context!.Statistics!.Value(33, "TimesExhausted") == 1 &&
            actual.Spawn.Train.Context!.Statistics!.Value(33, "TimesExhausted") == 2,
            "Wrong original Consume callback-order mismatch.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("EPHEMERAL-CONSUME-ORDER-PAIR PASS:original callback-order mismatch and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
