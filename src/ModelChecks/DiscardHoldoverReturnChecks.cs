using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DiscardHoldoverReturnChecks
{
    private const string Reason = "Returning a Holdover card from discard preserves its pending flag.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, (historical, actual) =>
        {
            var json = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
            var cards = json["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray();
            var card = cards.Single(card => (int)card!["InstanceId"]! == 30)!;
            Require((bool)card["MaskDescriptor"]!["BaseTraits"]![1]!["HoldoverPending"]! == false,
                "Original forecast must retain the wrong Holdover reset.");
            card["MaskDescriptor"]!["BaseTraits"]![1]!["HoldoverPending"] = true;
            Require(NativeBranchObservationChecks.Key(json.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                NativeBranchObservationChecks.Key(actual), "Historical difference exceeded the single pending flag.");
        });
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var oldCard = before.Spawn.Train.Context!.FindCard(30)!;
        var card = after.Spawn.Train.Context!.FindCard(30)!;
        var effect = before.PlayRules!.Cards.Single(rule => rule.DataId == before.Spawn.Train.Context.FindCard(27)!.DataId).Effects.Single();
        Require(rows.Length == 44 && !last.GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 6 && after.Spawn.Turn == 6 && action.CardInstanceId == 27 && action.RoomIndex == 2 &&
            action.RecursionSelections.Single().CardInstanceIds.SequenceEqual(new[] { 30 }) &&
            effect.Type == "Recursion" && effect.Target == "Discard" && effect.Recursion!.SelectionMode == "ChooseToHand" &&
            before.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == 30) &&
            after.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == 30) &&
            oldCard.MaskDescriptor!.BaseTraits[1] is { RuntimeType: "CardTraitRetain", HoldoverPending: true } &&
            card.MaskDescriptor!.BaseTraits[1] is { RuntimeType: "CardTraitRetain", HoldoverPending: true } &&
            card.Temporary.Upgrades.Single().AssetKey == "WurmReturnEnhanceCorrupt" &&
            before.Spawn.Train.Context.BattleRng.Equals(after.Spawn.Train.Context.BattleRng),
            "Missing native discard return preserving installed Holdover while adding Corrupt/Consume.");
        Console.WriteLine("DISCARD-HOLDOVER-RETURN PASS:44 native full successors/forecasts/RNG; discard-to-hand preserves installed pending state; return upgrade intact; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("DISCARD-HOLDOVER-RETURN-PAIR PASS:original wrong reset and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
