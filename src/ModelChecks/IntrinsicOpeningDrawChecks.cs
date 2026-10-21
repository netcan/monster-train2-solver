using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class IntrinsicOpeningDrawChecks
{
    internal static void Native(FixtureValue root)
    {
        var turns = root.GetProperty("Turns");
        Require(turns.GetArrayLength() == 2, "Missing opening and ordinary turn controls.");
        var before = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = turns[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = before.Spawn.Train.Context!;
        var intrinsic = context.CardInstances!.Single(card => card.DataId == "ab3c2bfa-0ad4-4be0-a050-293f4866bd65" &&
            card.MaskDescriptor != null && CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitIntrinsicState"));
        Require(before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 &&
            context.Cards.Draw.Any(card => card.InstanceId == intrinsic.InstanceId) &&
            !context.Cards.Hand.Any(card => card.InstanceId == intrinsic.InstanceId) &&
            actual.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == intrinsic.InstanceId) &&
            actual.Spawn.Train.Context.Cards.Hand.Count == before.DrawPerTurn + 1 &&
            actual.Spawn.Train.Context.Statistics!.Value(intrinsic.InstanceId, "TimesDrawn", "ThisBattle") == 1,
            "Native Intrinsic must draw before the ordinary first hand, after deployment.");
        var next = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(next.Spawn.Turn == 2 && next.Spawn.Train.Context!.Cards.Hand.Count == before.DrawPerTurn &&
            !next.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == intrinsic.InstanceId),
            "Intrinsic must not draw again on an ordinary turn.");
        string parent = JsonSerializer.Serialize(before);
        BattleTurnChecks.Native(root); // Full native state, draw order, statistics and gameplay RNG.
        Require(JsonSerializer.Serialize(before) == parent, "Opening draw mutated the source state.");
        Console.WriteLine("INTRINSIC-OPENING-DRAW PASS: native extra opening draw and ordinary-turn control.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
