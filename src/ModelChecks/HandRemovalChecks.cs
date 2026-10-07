using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class HandRemovalChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(180);
        var temporary = new CardUpgradeModifier("discard-only", "discard-only", new(damage: 2), [], true, false, false, 0, 0, []);
        CardInstanceState Instance(int id) => new(id, id == 1 ? "remove" : "held", CardModifiers.Empty(),
            new(new(), [temporary], 0, []), 3, 7, 4, [], id == 1 ? [new(0, "CardEffectDiscardHand", 9)] : null);
        var context = new CombatContext(new([new(3, "held"), new(2, "held"), new(1, "remove")], [new(4, "held")], [new(5, "held")], rng, 2, []),
            rng, 0, 9, 10, statistics: BattleStatistics.Empty().TrackCards([1, 2, 3, 4, 5, 6, 7, 8]), cardInstances: Enumerable.Range(1, 8).Select(Instance).ToArray());
        var rules = new BattlePlayRules([new(0, 5, 5, true, false, false), new(1, 5, 5, true, false, true)],
            [new("remove", "remove", 1, "Spell", "Discard", null, [], upgradeInteractions: [], handDiscardInteractions: [], handConsumeInteractions: []),
                new("held", "held", 0, "Null", "Discard", null, [], upgradeInteractions: [], handDiscardInteractions: [], handConsumeInteractions: [])]);
        CardPileState[] piles = [new("Standby", [], [], []), new("Exhausted", [new(6, "held")]), new("DiscardBuffer", []), new("Purged", []), new("Eaten", [])];
        string parent = JsonSerializer.Serialize(context), pileParent = JsonSerializer.Serialize(piles);
        HandRemovalResult discard = HandRemovalModel.Apply(context, 0, 1, 0, rules, piles);
        Require(discard.Supported && discard.Context!.Cards.Hand.Single().InstanceId == 1 &&
            discard.Context.Cards.Discard.Select(card => card.InstanceId).SequenceEqual([5, 3, 2]) &&
            discard.OtherPiles!.Single(pile => pile.Name == "DiscardBuffer").Cards.Select(card => card.InstanceId).SequenceEqual([3, 2]),
            "Spell hand discards must use the forward hand snapshot and exclude the resolving card.");
        Require(discard.Context!.CardInstances!.Where(card => card.InstanceId is 2 or 3).All(card => card.Temporary.Upgrades.Count == 0 &&
            card.PlayCount == 4 && card.LastPlayedCost == 3 && card.LastForgedAmount == 7) &&
            discard.Context.CardInstances.Single(card => card.InstanceId == 1).Temporary.Upgrades.Count == 1 &&
            discard.Context.Statistics!.Value(2, "TimesDiscarded") == 1 && discard.Context.Statistics.Value(1, "AnyDiscarded") == 2 &&
            discard.Context.Statistics.Value(1, "TimesPlayed") == 0 && discard.Context.Cards.DrawModifier == 2 && discard.Context.BattleRng.Equals(rng),
            "Unplayed discard incorrectly changed play history, upgrades, statistics, modifiers or RNG.");
        HandRemovalResult consume = HandRemovalModel.Apply(context, 1, 1, 0, rules, piles);
        Require(consume.Supported && consume.Context!.Cards.Hand.Single().InstanceId == 1 && consume.Context.Cards.Discard.Single().InstanceId == 5 &&
            consume.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId).SequenceEqual([6, 3, 2]) &&
            consume.OtherPiles.Single(pile => pile.Name == "Standby").EntrySlots!.SequenceEqual([0]) &&
            consume.OtherPiles.Single(pile => pile.Name == "Standby").FreeSlots!.SequenceEqual([0]),
            "Consumption lost its exhausted order or transient standby dictionary allocation.");
        Require(consume.Context!.CardInstances!.All(card => card.Temporary.Upgrades.Count == 1) &&
            new[] { 2, 3 }.All(id => consume.Context.Statistics!.Value(id, "TimesExhausted") == 2 && consume.Context.Statistics.Value(id, "TimesDiscarded") == 0) &&
            consume.Context.Statistics!.Value(1, "AnyExhausted") == 4 && consume.Context.Statistics.Value(2, "AnyExhausted") == 6 &&
            consume.ConsumedCount == 2 && consume.Context.CardInstances.Single(card => card.InstanceId == 1).EffectCounters!.Single().Value == 2,
            "Consumption did not preserve upgrades or distinguish double exhaustion statistics from the per-effect count.");
        CardPileState[] pendingPiles = piles.Select(pile => pile.Name == "Standby" ?
            new CardPileState("Standby", [new(7, "held")], [0, 7, 0], [0, 2]) : pile).ToArray();
        HandRemovalResult nested = HandRemovalModel.Apply(context, 1, 1, 0, rules, pendingPiles, [7], true);
        HandRemovalResult sequential = HandRemovalModel.Apply(context, 1, 1, 0, rules, pendingPiles, [7], false);
        Require(nested.Supported && sequential.Supported && nested.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards
            .Select(card => card.InstanceId).SequenceEqual([6, 3, 7, 2]) &&
            nested.OtherPiles.Single(pile => pile.Name == "Standby").FreeSlots!.SequenceEqual([0, 1, 2]) &&
            sequential.OtherPiles!.Single(pile => pile.Name == "Standby").FreeSlots!.SequenceEqual([1, 0, 2]) &&
            nested.Context!.Statistics!.Value(7, "TimesExhausted") == 1 && nested.ConsumedCount == 2,
            "Nested exhaustion callbacks must return pending deaths before removing the consuming standby entry.");
        var emptyContext = new CombatContext(new([new(1, "remove")], context.Cards.Draw, context.Cards.Discard, rng, 2, []),
            rng, 0, 9, 10, statistics: context.Statistics, cardInstances: context.CardInstances);
        HandRemovalResult emptyPending = HandRemovalModel.Apply(emptyContext, 1, 1, 0, rules, pendingPiles, [7], true);
        Require(emptyPending.Supported && emptyPending.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Single().InstanceId == 7 &&
            emptyPending.Context!.Statistics!.Value(7, "TimesExhausted") == 0, "Empty hand consumption incorrectly drained a pending death.");
        HandRemovalResult invalid = HandRemovalModel.Apply(context, 2, 1, 0, rules, piles);
        Require(invalid.Supported && invalid.RemovedCards.Count == 0 && invalid.Context!.Cards.Hand.Count == 3 &&
            invalid.Context.CardInstances!.Single(card => card.InstanceId == 1).EffectCounters!.Single().Value == 0,
            "A native unknown discard enum removed cards or failed to reset its effect counter.");
        Require(!HandRemovalModel.Apply(context, 1, 1, 0, rules).Supported && !HandRemovalModel.Apply(context, 0, 1, 0, null).Supported,
            "Missing pile/callback state returned a partial hand removal result.");
        var unsafeRules = new BattlePlayRules(rules.Rooms, rules.Cards.Select(rule => rule.DataId == "held" ?
            new CardPlayRule(rule.DataId, rule.AssetKey, 0, "Null", "Discard", null, [], handDiscardInteractions: ["Discard trigger"],
                handConsumeInteractions: ["Salvage callback"]) : rule).ToArray());
        Require(!HandRemovalModel.Apply(context, 0, 1, 0, unsafeRules, piles).Supported &&
            !HandRemovalModel.Apply(context, 1, 1, 0, unsafeRules, piles).Supported, "Unmodeled hand callbacks were ignored.");
        CardPileState[] invalidPiles = piles.Select(pile => pile.Name == "Exhausted" ? new CardPileState("Exhausted", [new(3, "held")]) : pile).ToArray();
        Require(!HandRemovalModel.Apply(context, 1, 1, 0, rules, invalidPiles).Supported &&
            CardPileModel.ValidateMembership(context.Cards, [new("DiscardBuffer", [new(2, "different-data")])], context.NextCardId) != null &&
            CardPileModel.ValidateMembership(context.Cards, [new("DiscardBuffer", [new(2, "held"), new(2, "held")])], context.NextCardId) != null,
            "Invalid primary membership or inconsistent buffer aliases were accepted.");

        CardActionEffect Remove(int mode, CardEffectRange? range = null) => new("DiscardHand", "Room", mode, false, false, [], range: range);
        var deathContext = new CombatContext(new([new(1, "remove"), new(2, "held")], context.Cards.Draw, context.Cards.Discard, rng, 2, []),
            rng, 0, 9, 10, statistics: context.Statistics, cardInstances: context.CardInstances);
        var deathRules = new BattlePlayRules(rules.Rooms, rules.Cards.Select(rule => rule.DataId == "remove" ?
            new CardPlayRule(rule.DataId, rule.AssetKey, 1, "Spell", "Discard", null, [],
                [new("Damage", "Room", 1, false, true, []), Remove(1)], [], [], []) : rule).ToArray());
        var deathTrain = new TrainCombatState([new(0, false, [new(2, "dying", CombatTeam.Player, 1, 1, 1, true, false, false, [], spawnerCardId: 7),
            new(3, "enemy", CombatTeam.Enemy, 1, 5, 5, true, false, false, [])], [], deathContext),
            new(1, false, [new(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], deathContext)], [], 5, deathContext);
        var deathRoot = new BattleTurnState(new(deathTrain, [], [], 0, false, rng, 4, [], 0, false, 0, 0, 0, []),
            2, 3, 5, 0, 0, "Full", [], pendingPiles, [], deathRules);
        BattleActionResult deathPlay = BattleActionModel.PlayCard(deathRoot, new(1, 0));
        Require(deathPlay.Supported && deathPlay.State!.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards
            .Select(card => card.InstanceId).SequenceEqual([6, 2, 7]) &&
            deathPlay.State.OtherPiles.Single(pile => pile.Name == "Standby").FreeSlots!.SequenceEqual([0, 1, 2]) &&
            deathPlay.State.Spawn.Train.Context!.Statistics!.Value(7, "TimesExhausted") == 1 &&
            deathPlay.State.Spawn.Train.Context.Statistics.Value(2, "TimesExhausted") == 2,
            "A full damage/consume action routed its last dead spawner before the consumption callback: " + deathPlay.Reason);
        var multipleRules = new BattlePlayRules([rules.Rooms[0], new(1, 5, 5, true, false, false), new(2, 5, 5, true, false, true)],
            deathRules.Cards.Select(rule => rule.DataId == "remove" ? new CardPlayRule(rule.DataId, rule.AssetKey, 1, "Spell", "Discard", null, [],
                [new("Damage", "Tower", 1, false, true, []), Remove(1)], [], [], []) : rule).ToArray());
        var multipleTrain = new TrainCombatState([deathTrain.Rooms[0],
            new(1, false, [new(4, "dying2", CombatTeam.Player, 1, 1, 1, true, false, false, [], spawnerCardId: 8)], [], deathContext),
            new(2, false, deathTrain.Rooms[1].Units, [], deathContext)], [], 5, deathContext);
        CardPileState[] multiplePiles = pendingPiles.Select(pile => pile.Name == "Standby" ?
            new CardPileState("Standby", [new(7, "held"), new(8, "held")], [7, 8, 0], [2]) : pile).ToArray();
        var multipleRoot = new BattleTurnState(new(multipleTrain, [], [], 0, false, rng, 5, [], 0, false, 0, 0, 0, []),
            2, 3, 5, 0, 0, "Full", [], multiplePiles, [], multipleRules);
        BattleActionResult multiplePlay = BattleActionModel.PlayCard(multipleRoot, new(1, 0));
        Require(multiplePlay.Supported && multiplePlay.State!.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards
            .Select(card => card.InstanceId).SequenceEqual([6, 7, 2, 8]) &&
            new[] { 7, 8 }.All(id => multiplePlay.State.Spawn.Train.Context!.Statistics!.Value(id, "TimesExhausted") == 1),
            "A later damage target lost the shared exhaustion statistic from an earlier queued death: " + multiplePlay.Reason);
        CardActionEffect[] effects = [Remove(0), new("Draw", "Room", 3, false, false, []), Remove(1), Remove(1),
            new("Draw", "Room", 1, false, false, []), Remove(2, new(-100, 100, .75f))];
        var spellRules = new BattlePlayRules(rules.Rooms, rules.Cards.Select(rule => rule.DataId == "remove" ?
            new CardPlayRule(rule.DataId, rule.AssetKey, 1, "Spell", "Discard", null, [], effects, [], [], []) : rule).ToArray());
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState([new(0, false, [], [], context), new(1, false, [pyre], [], context)], [], 5, context);
        var spawn = new EnemySpawnState(train, [], [], 0, false, rng, 2, [], 0, false, 0, 0, 0, []);
        var root = new BattleTurnState(spawn, 2, 3, 5, 0, 0, "Full", [], piles, [], spellRules);
        BattleActionResult played = BattleActionModel.PlayCard(root, new(1, 0));
        Require(played.Supported, "Full discard/consume/draw chain rejected: " + played.Reason);
        CombatContext result = played.State!.Spawn.Train.Context!;
        Require(result.Cards.Hand.Count == 1 && result.Cards.Draw.Count == 0 && result.Cards.Discard.Single().InstanceId == 1 &&
            played.State.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Count == 4 && played.State.Energy == 1 && result.BattleRng.Equals(rng) &&
            played.State.OtherPiles.Single(pile => pile.Name == "DiscardBuffer").Cards.Select(card => card.InstanceId).SequenceEqual([3, 2]),
            "Live discard reshuffling, consumption routing, ignored range or paid-card callbacks differ.");
        CardInstanceState resolved = result.CardInstances!.Single(card => card.InstanceId == 1);
        Require(resolved.EffectCounters!.Select(counter => (counter.EffectIndex, counter.Value)).SequenceEqual([(0, 0), (2, 3), (3, 0), (5, 0)]) &&
            resolved.PlayCount == 5 && resolved.LastPlayedCost == 1 && resolved.LastForgedAmount == 7,
            "Effect counters were conflated or lost during final source-card callbacks.");
        Require(CardPileModel.ValidateMembership(result.Cards, played.State.OtherPiles, result.NextCardId) == null,
            "Legitimate discard buffer aliases were treated as duplicate cards.");
        CardToken remaining = result.Cards.Hand.Single();
        BattleActionResult nextPlay = BattleActionModel.PlayCard(played.State, new(remaining.InstanceId, 0));
        Require(nextPlay.Supported && nextPlay.State!.OtherPiles.Single(pile => pile.Name == "DiscardBuffer").Cards.All(card => card.InstanceId != remaining.InstanceId),
            "A later natural play retained its discarded-hand buffer alias.");
        var ended = new RoomCombatState(0, false, [], [], new CombatContext(context.Cards, rng, 0, 9, 10, cardInstances: context.CardInstances, allScenarioBossesDead: true));
        Require(CardSpellModel.Apply(ended, [Remove(0)], 0, definitions: rules).State!.Context!.Cards.Hand.Count == 3 &&
            !CardSpellModel.TestPlay(new(0, false, [], [], context, preview: true), [Remove(0)], 0).CanPlay,
            "Post-boss or preview-disabled discard effects were applied.");
        RoomCombatResult unusedRange = CardSpellModel.Apply(new(0, false, [], [], context), [Remove(2, new(-100, 100, float.NaN))], 0);
        Require(unusedRange.Supported && unusedRange.State!.Context!.BattleRng.Equals(rng) && unusedRange.State.Context.Cards.Hand.Count == 3,
            "A completely unused native discard range was sampled or rejected.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(BattleActionModel.PlayCard(root, new(1, 0)).State) == JsonSerializer.Serialize(played.State),
            "Parallel hand removal branches diverged."));
        Require(JsonSerializer.Serialize(context) == parent && JsonSerializer.Serialize(piles) == pileParent, "Hand removal mutated its parent or pile layout.");
        Console.WriteLine("HAND-REMOVAL-CHECKS PASS: forward order, source exclusion, double exhaust counters, discard-only upgrades, transient slots, nested death returns, empty-hand queue timing, live redraw chains, latent counters, ignored modes/ranges, gates and 32 parallel branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        FixtureValue records = fixture.GetProperty("HandRemovals");
        int removals = 0, discarded = 0, consumed = 0, empty = 0, ignored = 0, kept = 0, removed = 0, allocated = 0,
            pendingReturns = 0, nestedReturns = 0;
        foreach (FixtureValue record in records.EnumerateArray())
        {
            FixtureValue before = record.GetProperty("Before"), after = record.GetProperty("Actual");
            CombatContext context = before.GetProperty("Context").Deserialize<CombatContext>()!;
            CardPileState[] piles = before.GetProperty("OtherPiles").Deserialize<CardPileState[]>()!;
            int source = record.GetProperty("CardId").GetInt32(), mode = record.GetProperty("Mode").GetInt32(), index = record.GetProperty("EffectIndex").GetInt32();
            BattleTurnState root = fixture.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
            int[] pending = before.TryGetProperty("PendingExhaustedCards", out FixtureValue pendingElement) ? pendingElement.Deserialize<int[]>()! : [];
            bool runsQueue = before.TryGetProperty("ExhaustionRunsTriggerQueue", out FixtureValue queueElement) && queueElement.GetBoolean();
            HandRemovalResult predicted = HandRemovalModel.Apply(context, mode, source, index, root.PlayRules, piles, pending, runsQueue);
            Require(predicted.Supported, "Native hand removal replay unsupported: " + predicted.UnsupportedReason);
            CombatContext actual = after.GetProperty("Context").Deserialize<CombatContext>()!;
            CardPileState[] actualPiles = after.GetProperty("OtherPiles").Deserialize<CardPileState[]>()!;
            string? difference = ModelJson.Difference(Comparable(predicted.Context!, predicted.OtherPiles), Comparable(actual, actualPiles));
            Require(difference == null && predicted.ConsumedCount == record.GetProperty("ConsumedCount").GetInt32(),
                "Native hand removal differs at effect " + index + ": " + difference);
            int returned = pending.Count(id => actualPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id));
            pendingReturns += returned;
            if (runsQueue && mode == 1 && predicted.RemovedCards.Count > 0) nestedReturns += returned;
            removals++;
            if (mode == 0) discarded += predicted.RemovedCards.Count;
            if (mode == 1) consumed += predicted.RemovedCards.Count;
            if (predicted.RemovedCards.Count == 0 && mode == 1) empty++;
            if (mode == 2) ignored++;
            if (piles.Single(pile => pile.Name == "Standby").EntrySlots!.Count < actualPiles.Single(pile => pile.Name == "Standby").EntrySlots!.Count) allocated++;
            foreach (CardToken card in predicted.RemovedCards)
            {
                CardInstanceState original = context.CardInstances!.Single(instance => instance.InstanceId == card.InstanceId);
                CardInstanceState result = actual.CardInstances!.Single(instance => instance.InstanceId == card.InstanceId);
                int beforeCount = original.Temporary.Upgrades.Count(upgrade => upgrade.RemoveOnDiscard), afterCount = result.Temporary.Upgrades.Count(upgrade => upgrade.RemoveOnDiscard);
                if (mode == 0 && beforeCount > afterCount) removed++;
                if (mode == 1 && beforeCount > 0 && afterCount == beforeCount) kept++;
                Require(mode != 1 || actual.Statistics!.Value(card.InstanceId, "TimesExhausted") - context.Statistics!.Value(card.InstanceId, "TimesExhausted") == 2,
                    "A native consumption failed to count exhaustion twice.");
            }
        }
        Require(removals > 0 && discarded > 0 && consumed > 0 && empty > 0 && ignored > 0 && removed > 0 && kept > 0 && allocated > 0,
            "Native hand removal oracle lacks discard/consume/empty/ignored operations, upgrade removal/preservation or transient allocation.");
        if (fixture.GetProperty("ModifierScenario").GetString() == "hand-removal-lethal")
            Require(pendingReturns > 0 && nestedReturns > 0, "The lethal hand-removal oracle lacks pending deaths returned inside consumption callbacks.");
        Console.WriteLine($"NATIVE-HAND-REMOVAL-COVERAGE PASS: {removals} exact effects, {discarded} discarded, {consumed} consumed, {empty} empty consumes, {ignored} ignored modes, {removed} upgrade removals, {kept} preserved upgrades, {allocated} dictionary extensions and {nestedReturns} nested death returns.");
    }
    private static string Comparable(CombatContext context, IReadOnlyList<CardPileState>? piles) => JsonSerializer.Serialize(new
        { Context = context, OtherPiles = piles });
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
