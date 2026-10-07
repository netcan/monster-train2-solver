using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardCycleChecks
{
    internal static void Run()
    {
        var root = new CardCycleState([new CardToken(10, "held")],
            [new CardToken(1, "a"), new CardToken(2, "b")], [new CardToken(3, "c")],
            UnityRng.Seed(424242), 1, []);
        CardCycleState next = CardCycleModel.DrawHand(root, 0, 10).State!;
        Require(next.Hand.Select(card => card.InstanceId).SequenceEqual([2, 10]) &&
            next.Draw.Count == 1 && next.DrawModifier == 0 && next.Rng.Equals(root.Rng),
            "Drawing must pop the end of the deck and prepend to the hand.");
        CardCycleState discarded = CardCycleModel.DiscardHand(next).State!;
        Require(discarded.Discard.Select(card => card.InstanceId).SequenceEqual([3, 10, 2]),
            "Native hand discard order differs.");
        CardCycleState full = CardCycleModel.DrawHand(root, 5, 1).State!;
        Require(ReferenceEquals(full, root) && full.DrawModifier == 1,
            "A full hand reset its pending draw modifier.");
        CardCycleState exhausted = CardCycleModel.DrawHand(root, 10, 10).State!;
        Require(exhausted.Hand.Count == 4 && exhausted.Draw.Count == 0 && exhausted.Discard.Count == 0,
            "Drawing across a reshuffle lost or duplicated a card.");
        Require(root.Hand.Count == 1 && root.Draw.Count == 2 && root.Discard.Count == 1,
            "Card cycling mutated its parent.");
        Console.WriteLine("CARD-CYCLE-CHECKS PASS: draw order, discard order, reshuffle, hand limits, parent isolation.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("CardCycles", out FixtureValue cycles)) return;
        int matched = 0, unsupported = 0;
        foreach (FixtureValue cycle in cycles.EnumerateArray())
        {
            CardCycleState before = Read(cycle.GetProperty("Before"));
            CardCycleResult result = cycle.GetProperty("Kind").GetString() == "Draw"
                ? CardCycleModel.DrawHand(before, cycle.GetProperty("HandSize").GetInt32(),
                    cycle.GetProperty("MaxHandSize").GetInt32()) : cycle.GetProperty("Kind").GetString() == "SpellDraw"
                    ? CardCycleModel.DrawCards(before, cycle.GetProperty("HandSize").GetInt32(), cycle.GetProperty("MaxHandSize").GetInt32(),
                        cycle.GetProperty("PlayedCardId").GetInt32()) : CardCycleModel.DiscardHand(before);
            if (!result.Supported) { unsupported++; continue; }
            CardCycleState actual = Read(cycle.GetProperty("Actual"));
            Require(Comparable(result.State!) == Comparable(actual),
                "Native card cycling differs at index " + cycle.GetProperty("Index"));
            matched++;
        }
        Require(matched > 0, "No native card cycling stages were verified.");
        Console.WriteLine($"NATIVE-CARD-CYCLE-CHECKS PASS: {matched} matched, {unsupported} unsupported.");
    }

    private static string Comparable(CardCycleState state) => JsonSerializer.Serialize(new
        { state.Hand, state.Draw, state.Discard, state.Rng, state.DrawModifier, state.BonusDraw });
    private static CardCycleState Read(FixtureValue state)
    {
        // System.Text.Json needs an explicit adapter for the immutable four-word RNG struct.
        FixtureValue rng = state.GetProperty("Rng");
        return new CardCycleState(state.GetProperty("Hand").Deserialize<CardToken[]>()!,
            state.GetProperty("Draw").Deserialize<CardToken[]>()!,
            state.GetProperty("Discard").Deserialize<CardToken[]>()!,
            new UnityRng(rng.GetProperty("S0").GetUInt32(), rng.GetProperty("S1").GetUInt32(),
                rng.GetProperty("S2").GetUInt32(), rng.GetProperty("S3").GetUInt32()),
            state.GetProperty("DrawModifier").GetInt32(),
            state.GetProperty("ExternalInteractions").Deserialize<string[]>()!,
            state.TryGetProperty("BonusDraw", out var bonus) ? bonus.Deserialize<BonusDrawState>() : null);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
