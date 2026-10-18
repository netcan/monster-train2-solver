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
        CardCycleResult ephemeral = CardCycleModel.DiscardHand(new CardCycleState(
            [new CardToken(10, "held"), new CardToken(11, "ephemeral"), new CardToken(12, "held")],
            [], [new CardToken(3, "prior")], UnityRng.Seed(8), 0, []), [11]);
        Require(ephemeral.Supported && ephemeral.State!.Discard.Select(card => card.InstanceId).SequenceEqual([3, 12, 10]) &&
            ephemeral.PurgedCards.Select(card => card.InstanceId).SequenceEqual([11]),
            "Ephemeral discard routing changed hand order or retained the card in Discard.");
        CardCycleResult persistent = CardCycleModel.DiscardHand(new CardCycleState(
            [new CardToken(10, "held"), new CardToken(11, "persistent"), new CardToken(12, "ephemeral")],
            [], [new CardToken(3, "prior")], UnityRng.Seed(8), 0,
            ["CardTraitPersistent Discard callback", "CardTraitPersistent discard routing"]), [11, 12], [11]);
        Require(persistent.Supported && persistent.State!.Hand.Select(card => card.InstanceId).SequenceEqual([11]) &&
            persistent.State.Discard.Select(card => card.InstanceId).SequenceEqual([3, 10]) &&
            persistent.PurgedCards.Select(card => card.InstanceId).SequenceEqual([12]) &&
            !CardCycleModel.DiscardHand(new CardCycleState([new CardToken(11, "persistent")], [], [],
                UnityRng.Seed(8), 0, ["CardTraitPersistent discard routing"])).Supported,
            "Persistent discard did not retain the card or require its captured identity.");
        CardCycleResult infusion = CardCycleModel.DiscardHand(new CardCycleState(
            [new CardToken(10, "held"), new CardToken(11, "infusion"), new CardToken(12, "persistent")],
            [], [], UnityRng.Seed(8), 0, []), persistentCardIds: [12], infusionCardIds: [11]);
        Require(infusion.Supported && infusion.State!.Hand.Select(card => card.InstanceId).SequenceEqual([11, 12]) &&
            infusion.State.Discard.Select(card => card.InstanceId).SequenceEqual([10]),
            "Infusion discard routing did not retain the card in native hand order.");
        var playedEphemeral = new CardToken(1, "ephemeral");
        Require(CardPileModel.ValidateMembership(new CardCycleState([], [], [], UnityRng.Seed(9), 0, []),
            [new CardPileState("Purged", [playedEphemeral, playedEphemeral])], 2) == null,
            "Native's paired Ephemeral play/discard purge callbacks were rejected as duplicate pile membership.");
        CardCycleState full = CardCycleModel.DrawHand(root, 5, 1).State!;
        Require(ReferenceEquals(full, root) && full.DrawModifier == 1,
            "A full hand reset its pending draw modifier.");
        CardCycleState exhausted = CardCycleModel.DrawHand(root, 10, 10).State!;
        Require(exhausted.Hand.Count == 4 && exhausted.Draw.Count == 0 && exhausted.Discard.Count == 0,
            "Drawing across a reshuffle lost or duplicated a card.");
        Require(root.Hand.Count == 1 && root.Draw.Count == 2 && root.Discard.Count == 1,
            "Card cycling mutated its parent.");
        Console.WriteLine("CARD-CYCLE-CHECKS PASS: draw/discard order, Ephemeral purge, Persistent and Infusion retention, reshuffle, hand limits and parent isolation.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("CardCycles", out FixtureValue cycles)) return;
        int matched = 0, unsupported = 0;
        foreach (FixtureValue cycle in cycles.EnumerateArray())
        {
            CardCycleState before = Read(cycle.GetProperty("Before"));
            CombatContext? drawContext = null;
            if (cycle.TryGetProperty("StandbyContext", out var standby) && standby.ValueKind != FixtureKind.Null)
            {
                var context = standby.Deserialize<CombatContext>()!;
                Require(Comparable(context.Cards) == Comparable(before), "Pre-draw standby context differs from the actual draw input.");
                var prepared = cycle.GetProperty("Kind").GetString() == "Draw"
                    ? EquipmentModel.ReturnStandby(context, cycle.GetProperty("LivingUnitIds").Deserialize<int[]>()!).Cards : context.Cards;
                before = new CardCycleState(prepared.Hand, prepared.Draw, prepared.Discard, prepared.Rng, prepared.DrawModifier,
                    before.ExternalInteractions, prepared.BonusDraw, prepared.NextDrawUpgrades, prepared.NextUpgradeReferenceId);
                drawContext = context.WithCards(before);
            }
            CardCycleResult result = cycle.GetProperty("Kind").GetString() == "Draw"
                ? CardCycleModel.DrawHand(before, cycle.GetProperty("HandSize").GetInt32(),
                    cycle.GetProperty("MaxHandSize").GetInt32()) : cycle.GetProperty("Kind").GetString() == "SpellDraw"
                    ? CardCycleModel.DrawCards(before, cycle.GetProperty("HandSize").GetInt32(), cycle.GetProperty("MaxHandSize").GetInt32(),
                        cycle.GetProperty("PlayedCardId").GetInt32()) : CardCycleModel.DiscardHand(before,
                        cycle.TryGetProperty("EphemeralCardIds", out var ephemeralIds) ? ephemeralIds.Deserialize<int[]>() : null,
                        cycle.TryGetProperty("PersistentCardIds", out var persistentIds) ? persistentIds.Deserialize<int[]>() : null,
                        cycle.TryGetProperty("InfusionCardIds", out var infusionIds) ? infusionIds.Deserialize<int[]>() : null);
            if (drawContext != null && RelicCorruptedAngelsModel.Enabled(drawContext))
            {
                BattlePlayRules definitions = cycle.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
                TypedDrawResult draw = cycle.GetProperty("Kind").GetString() == "Draw"
                    ? RelicCorruptedAngelsModel.DrawHand(drawContext, cycle.GetProperty("HandSize").GetInt32(), definitions)
                    : NextDrawUpgradeModel.DrawCards(drawContext, cycle.GetProperty("HandSize").GetInt32(),
                        cycle.GetProperty("PlayedCardId").GetInt32(), definitions);
                result = new(draw.Context?.Cards, draw.UnsupportedReason);
            }
            if (!result.Supported) { unsupported++; continue; }
            CardCycleState actual = Read(cycle.GetProperty("Actual"));
            Require(Comparable(result.State!) == Comparable(actual),
                "Native card cycling differs at index " + cycle.GetProperty("Index"));
            if (cycle.GetProperty("Kind").GetString() == "Discard" && cycle.TryGetProperty("PurgedActual", out var purgedActual))
            {
                int[] purgedBefore = cycle.TryGetProperty("PurgedBefore", out var purged) ? purged.Deserialize<int[]>()! : Array.Empty<int>();
                Require(purgedBefore.Concat(result.PurgedCards.Select(card => card.InstanceId))
                    .SequenceEqual(purgedActual.Deserialize<int[]>()!),
                    "Native Ephemeral discard routing differs at index " + cycle.GetProperty("Index"));
            }
            matched++;
        }
        Require(matched > 0, "No native card cycling stages were verified.");
        Require(unsupported == 0, "Native card cycling verification skipped unsupported stages.");
        Console.WriteLine($"NATIVE-CARD-CYCLE-CHECKS PASS: {matched} matched, {unsupported} unsupported.");
    }

    private static string Comparable(CardCycleState state) => JsonSerializer.Serialize(new
        { state.Hand, state.Draw, state.Discard, state.Rng, state.DrawModifier, state.BonusDraw, state.NextDrawUpgrades, state.NextUpgradeReferenceId });
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
            state.TryGetProperty("BonusDraw", out var bonus) ? bonus.Deserialize<BonusDrawState>() : null,
            state.TryGetProperty("NextDrawUpgrades", out var next) ? next.Deserialize<NextDrawUpgradeRule[]>() : null,
            state.TryGetProperty("NextUpgradeReferenceId", out var reference) ? reference.Deserialize<int?>() : null);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
