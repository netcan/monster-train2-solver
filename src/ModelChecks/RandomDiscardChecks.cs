using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RandomDiscardChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(180);
        CardMaskDefinition Definition(string id) => new(id, "Spell", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false);
        CardMaskDescriptor Descriptor(string id, params string[] traits) => new(Definition(id), 1,
            traits.Select(name => new CardTraitValue(name, name, 0, 0, true, 0)).ToArray(), []);
        CardInstanceState Instance(int id, string dataId, params string[] traits) => new(id, dataId,
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: Descriptor(dataId, traits));

        CardToken[] hand = [new(2, "held"), new(3, "held"), new(4, "held"), new(5, "held"), new(1, "source")];
        CardInstanceState[] instances =
        [
            Instance(1, "source"),
            Instance(2, "held"),
            Instance(3, "held", "CardTraitInfusion"),
            Instance(4, "held"),
            Instance(5, "held", "CardTraitPersistent")
        ];
        var context = new CombatContext(new(hand, [], [], rng, 0, []), rng, 0, 6, 10,
            statistics: BattleStatistics.Empty().TrackCards([1, 2, 3, 4, 5]), cardInstances: instances);
        BattlePlayRules rules = Rules();
        CardPileState[] piles = Piles();

        Require(RandomDiscardModel.EligibleCount(context, 1, out string? eligibilityError) == 2 && eligibilityError == null,
            "Random discard did not exclude the resolving card, Infusion and Persistent cards.");
        HandRemovalResult one = RandomDiscardModel.Apply(context, 1, 1, 2, rules, piles);
        Require(one.Supported && one.RemovedCards.Count == 1 && one.RemovedCards[0].InstanceId is 2 or 4 &&
            one.Context!.BattleRng.Equals(rng.Next()) && one.Context.Cards.Hand.Count == 4 &&
            one.Context.Cards.Discard.Select(card => card.InstanceId).SequenceEqual(one.RemovedCards.Select(card => card.InstanceId)) &&
            one.OtherPiles!.Single(pile => pile.Name == "DiscardBuffer").Cards.Select(card => card.InstanceId)
                .SequenceEqual(one.RemovedCards.Select(card => card.InstanceId)) &&
            one.Context.CardInstances!.Single(card => card.InstanceId == 1).EffectCounters == null &&
            context.Cards.Hand.SequenceEqual(hand) && context.BattleRng.Equals(rng),
            "Random discard did not preserve native RNG, selected-card routing, counters or parent isolation.");

        HandRemovalResult all = RandomDiscardModel.Apply(context, 4, 1, 3, rules, piles);
        Require(all.Supported && all.RemovedCards.Select(card => card.InstanceId).SequenceEqual([2, 4]) &&
            all.Context!.BattleRng.Equals(rng) && all.Context.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([3, 5, 1]),
            "Random discard above candidate count did not discard every eligible card without consuming RNG.");
        Require(!RandomDiscardModel.Apply(context, -1, 1, 2, rules, piles).Supported,
            "Negative random discard count was accepted.");

        var noTargets = new CombatContext(new([new(1, "source")], [], [], rng, 0, []), rng, 0, 2, 10,
            cardInstances: [instances[0]]);
        var noTargetRoom = new RoomCombatState(0, false, [], [], noTargets);
        SpellCastCheck noTargetCast = CardSpellModel.TestPlay(noTargetRoom,
            [new CardActionEffect("RandomDiscard", "Hand", 1, false, false, [])], 0, sourceCardId: 1);
        Require(noTargetCast.Supported && !noTargetCast.CanPlay,
            "The resolving card itself made a random-discard spell pass its native hand-target test.");
        Console.WriteLine("RANDOM-DISCARD-CHECKS PASS: exact battle-RNG selection, eligible trait filters, ordered discard routing and empty-target casting gate.");

        BattlePlayRules Rules() => new([new(0, 5, 5, true, false, false)],
        [
            new("source", "source", 1, "Spell", "Discard", null, [], handDiscardInteractions: [], handConsumeInteractions: []),
            new("held", "held", 0, "Null", "Discard", null, [], handDiscardInteractions: [], handConsumeInteractions: [])
        ]);
        CardPileState[] Piles() =>
        [new("Standby", [], [], []), new("Exhausted", []), new("DiscardBuffer", []), new("Purged", []), new("Eaten", [])];
    }

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue records = fixture.GetProperty("RandomDiscards");
        BattleTurnState root = fixture.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        int applications = 0, discarded = 0;
        foreach (FixtureValue record in records.EnumerateArray())
        {
            FixtureValue before = record.GetProperty("Before"), after = record.GetProperty("Actual");
            CombatContext context = before.GetProperty("Context").Deserialize<CombatContext>()!;
            CardPileState[] piles = before.GetProperty("OtherPiles").Deserialize<CardPileState[]>()!;
            int sourceCardId = record.GetProperty("CardId").GetInt32();
            int effectIndex = record.GetProperty("EffectIndex").GetInt32();
            int count = record.GetProperty("DiscardCount").GetInt32();
            HandRemovalResult predicted = RandomDiscardModel.Apply(context, count, sourceCardId, effectIndex, root.PlayRules, piles);
            Require(predicted.Supported, "Native random discard replay unsupported: " + predicted.UnsupportedReason);

            CombatContext actual = after.GetProperty("Context").Deserialize<CombatContext>()!;
            CardPileState[] actualPiles = after.GetProperty("OtherPiles").Deserialize<CardPileState[]>()!;
            string? difference = ModelJson.Difference(Comparable(predicted.Context!, predicted.OtherPiles), Comparable(actual, actualPiles));
            Require(difference == null, "Native random discard differs at effect " + effectIndex + ": " + difference);
            Require(record.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "The in-game random discard probe reported a mismatch.");
            applications++;
            discarded += predicted.RemovedCards.Count;
        }
        Require(applications > 0 && discarded > 0, "Native random discard fixture lacks a real discard application.");
        Console.WriteLine($"NATIVE-RANDOM-DISCARD-COVERAGE PASS: {applications} exact applications, {discarded} discarded cards, including hand state and battle RNG.");
    }

    private static string Comparable(CombatContext context, IReadOnlyList<CardPileState>? piles) =>
        JsonSerializer.Serialize(new { Context = context, OtherPiles = piles }, ModelJson.Options);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
