using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BonusDrawChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(887);
        var upgrade = new CardUpgradeModifier("bonus", "bonus", new(damage: 1), [], false, false, false, 0, 0, []);
        CardCycleState Cards(int held = 0) => new(Enumerable.Range(20, held).Select(id => new CardToken(id, "held")).ToArray(),
            Enumerable.Range(1, 10).Select(id => new CardToken(id, "other")).ToArray(), [], rng, 0, [], new([], []));
        var root = Cards(); string parent = JsonSerializer.Serialize(root);
        var twice = BonusDrawModel.Schedule(BonusDrawModel.Schedule(root, "card:1:0", 2, upgrade), "card:1:0", 2, upgrade);
        var drawn = CardCycleModel.DrawHand(twice, 2, 10);
        Require(drawn.Supported && drawn.State!.Hand.Count == 6 && drawn.UpgradeApplications.Select(application => application.CardId).SequenceEqual([8, 8, 7, 7]) &&
            drawn.State.BonusDraw!.Counters.Count == 0 && drawn.State.BonusDraw.Listeners.Count == 0 && drawn.State.DrawModifier == 0,
            "Repeated listeners did not share one effect counter, upgrade in order or reset after draw.");
        var held = CardCycleModel.DrawHand(BonusDrawModel.Schedule(Cards(2), "unit:11:0:0", 2, upgrade), 3, 10);
        Require(held.Supported && held.UpgradeApplications.Select(application => application.CardId).SequenceEqual([9, 8]),
            "Bonus callbacks used draw position instead of the total resulting hand size.");
        var signed = BonusDrawModel.Schedule(root, "negative", -2, upgrade);
        var reduced = CardCycleModel.DrawHand(signed, 5, 10);
        Require(signed.BonusDraw!.Counters.Single().Value == -2 && signed.BonusDraw.Listeners.Count == 1 && reduced.State!.Hand.Count == 3 &&
            reduced.UpgradeApplications.Count == 0 && reduced.State.BonusDraw!.Counters.Count == 0, "Signed bonus counts or callback clearing differ.");
        var scheduled = BonusDrawModel.Schedule(root, "pending", 2, upgrade);
        foreach (int count in new[] { -3, 0, 1, 99 })
        {
            var spellDraw = CardCycleModel.DrawCards(scheduled, count, 10);
            Require(spellDraw.Supported && spellDraw.State!.DrawModifier == 2 && spellDraw.State.BonusDraw!.Listeners.Count == 0 &&
                spellDraw.State.BonusDraw.Counters.Count == 0 && spellDraw.UpgradeApplications.Count == 0,
                "An ordinary draw preserved bonus callbacks or erased the future count.");
        }
        var full = BonusDrawModel.Schedule(Cards(10), "full", 2, upgrade);
        Require(ReferenceEquals(CardCycleModel.DrawHand(full, 5, 10).State, full), "A full hand consumed pending callbacks.");
        var empty = new CardCycleState([], [], [], rng, 2, [], scheduled.BonusDraw);
        Require(ReferenceEquals(CardCycleModel.DrawHand(empty, 5, 10).State, empty) &&
            CardCycleModel.DrawCards(empty, 0, 10).State!.BonusDraw!.Listeners.Count == 0,
            "Empty-hand draw and ordinary zero draw have the wrong early-exit behavior.");
        Require(BonusDrawModel.Schedule(root, "zero", 0, upgrade).BonusDraw!.Listeners.Count == 0 &&
            CardCycleModel.DiscardHand(scheduled).State!.BonusDraw!.Listeners.Count == 1, "Zero registration or discard lost pending state.");
        var overflow = BonusDrawModel.Schedule(new([], root.Draw, [], rng, int.MaxValue, [], new([], [])), "wrap", 1, null);
        Require(overflow.DrawModifier == int.MinValue && CardCycleModel.DrawHand(overflow, 1, 10).State!.Hand.Count == 0,
            "Native draw-count arithmetic did not wrap before its zero floor.");
        Integration(rng, upgrade);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardCycleModel.DrawHand(twice, 2, 10)) == JsonSerializer.Serialize(drawn),
            "Parallel bonus-draw branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Bonus draw mutated its parent.");
        Console.WriteLine("BONUS-DRAW-CHECKS PASS: signed/ranged pending counts, shared duplicate callbacks, hand-size threshold, cap/empty exits, ordinary draw cancellation, upgrades across turns and 32 parallel branches.");
    }
    private static void Integration(UnityRng rng, CardUpgradeModifier upgrade)
    {
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("DiscardBuffer", []), new("Eaten", []), new("Purged", [])];
        var tokens = Enumerable.Range(1, 9).Select(id => new CardToken(id, id == 1 ? "schedule" : "other")).ToArray();
        var instances = tokens.Select(token => CardInstanceState.Empty(token.InstanceId, token.DataId)).ToArray();
        var context = new CombatContext(new([tokens[0]], tokens.Skip(1).ToArray(), [], rng, 0, [], new([], [])), rng, 0, 10, 10,
            statistics: new([], [], [], [], [], [], [], 0, 0, 0, 0, 0, tokens.Select(token => token.InstanceId).ToArray(), tokens.Select(token => token.InstanceId).ToArray()),
            cardInstances: instances, cardRegistry: instances, otherPiles: piles, queryFrame: new(3, true, 2, 0, 0, 1, 0), energyState: new(9, 0, 0, "MonsterTurn", true));
        var next = new CardActionEffect("DrawNextTurn", "Room", 3, false, false, [], upgrade: upgrade);
        var trigger = new CombatTrigger("PreCombat", false, false, true, 1, [new("CardEffectDrawAdditionalNextTurn", 1, 0, "", 0, [], false,
            action: new("DrawNextTurn", "Self", 1, false, true, [], upgrade: upgrade))], false);
        var unit = new CombatUnit(10, "unit", CombatTeam.Player, 1, 100, 100, false, false, false, [], [trigger]);
        var pyre = new CombatUnit(11, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState([new(0, false, [unit], [], context), new(1, false, [pyre], [], context)], [], 7, context);
        var spawn = new EnemySpawnState(train, [new([new([])]), new([new([])]), new([new([])])], [-1, -1, -1], 0, false, rng, 12, [], 0, false, 0, 0, 2, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 0, 7, true, true, true)],
            [new("schedule", "schedule", 0, "Spell", "Discard", null, [], [next], upgradeInteractions: []),
                new("other", "other", 0, "Null", "Discard", null, [], upgradeInteractions: [])]);
        var root = new BattleTurnState(spawn, 3, 3, 3, 0, 0, "New", [new("Spawning", 887, rng)], piles, [], rules);
        var played = BattleActionModel.PlayCard(root, new(1, 0));
        Require(played.Supported && played.State!.Spawn.Train.Context!.Cards.DrawModifier == 3, "Future draw spell rejected: " + played.Reason);
        var range = new CardEffectRange(-2, 4, .75f); var sampled = range.Sample(rng);
        var rangedRules = new BattlePlayRules(rules.Rooms, [new("schedule", "schedule", 0, "Spell", "Discard", null, [],
            [new("DrawNextTurn", "Room", 0, false, false, [], upgrade: upgrade, range: range)], upgradeInteractions: []), rules.Cards[1]]);
        var rangedRoot = new BattleTurnState(spawn, 3, 3, 3, 0, 0, "New", root.RngStreams, piles, [], rangedRules);
        var rangedPlay = BattleActionModel.PlayCard(rangedRoot, new(1, 0));
        Require(rangedPlay.Supported && rangedPlay.State!.Spawn.Train.Context!.Cards.DrawModifier == sampled.Value &&
            rangedPlay.State.Spawn.Train.Context.BattleRng.Equals(sampled.State), "Future draw sampled a range twice or used a stale stream.");
        var turn = BattleTurnModel.EndTurn(played.State!);
        Require(turn.Supported && turn.State!.Spawn.Train.Context!.Cards.Hand.Count == 7 &&
            turn.State.Spawn.Train.Context.CardInstances!.Sum(card => card.Temporary.Upgrades.Count) == 4 &&
            turn.State.Spawn.Train.Context.Cards.BonusDraw!.Listeners.Count == 0,
            "Spell and unit future draws did not carry their upgrades into the next turn: " + turn.UnsupportedReason);
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("BonusDrawEffects", out var records) || records.GetArrayLength() == 0) return;
        int count = 0, negative = 0, zero = 0, ranged = 0, upgraded = 0, unit = 0, duplicates = 0, cancelled = 0, capped = 0;
        foreach (var record in records.EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Sampled").GetBoolean(), "Incomplete native future-draw effect.");
            var before = record.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = record.GetProperty("After").Deserialize<CombatContext>()!;
            var effect = record.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            RngDraw? sampled = effect.Range?.Sample(before.BattleRng); int amount = sampled?.Value ?? effect.Value;
            Require(amount == record.GetProperty("Amount").GetInt32(), "Native future-draw range differs.");
            var cards = BonusDrawModel.Schedule(before.Cards, record.GetProperty("Key").GetString()!, amount, effect.Upgrade);
            var predicted = new CombatContext(cards, sampled?.State ?? before.BattleRng, before.Gold, before.NextCardId, before.MaxHandSize,
                before.StatusRules, before.Statistics, before.CardInstances, before.CardRegistry, before.AllScenarioBossesDead,
                before.NextAddedTemporaryUpgrades, before.OtherPiles, before.QueryFrame, before.KillCamActivated, before.MagicPower, before.IsolatedBattlePreview, before.EnergyState, before.RoomCapacities,
                before.AbilityCardCache, before.LastAbilityActivatorUnitId);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(after));
            Require(difference == null, "Native future-draw context differs: " + difference);
            count++; if (amount < 0) negative++; if (amount == 0) zero++; if (effect.Range != null) ranged++;
            if (effect.Upgrade != null) upgraded++; if (record.GetProperty("Key").GetString()!.StartsWith("unit:")) unit++;
            if (after.Cards.BonusDraw!.Listeners.GroupBy(listener => listener.Key).Any(group => group.Count() > 1)) duplicates++;
        }
        foreach (var cycle in fixture.GetProperty("CardCycles").EnumerateArray())
        {
            var before = cycle.GetProperty("Before").Deserialize<CardCycleState>()!;
            var after = cycle.GetProperty("Actual").Deserialize<CardCycleState>()!;
            string kind = cycle.GetProperty("Kind").GetString()!;
            int amount = cycle.GetProperty("HandSize").GetInt32(), maxHand = cycle.GetProperty("MaxHandSize").GetInt32();
            if (kind == "SpellDraw" && amount == 0 && before.BonusDraw!.Listeners.Count > 0 &&
                after.BonusDraw!.Listeners.Count == 0 && before.DrawModifier == after.DrawModifier) cancelled++;
            if (kind == "Draw" && unchecked(amount + before.DrawModifier) > maxHand - before.Hand.Count && after.Hand.Count == maxHand) capped++;
        }
        if (fixture.TryGetProperty("ModifierScenario", out var scenario) && scenario.GetString() is "bonus-draw" or "bonus-draw-lethal")
            Require(negative > 0 && zero > 0 && ranged > 0 && upgraded > 0 && unit > 0 && duplicates > 0 && cancelled > 0 && capped > 0,
                "Native future-draw mechanism coverage incomplete.");
        Console.WriteLine($"NATIVE-BONUS-DRAW-CHECKS PASS: {count} exact contexts, {negative} negative/{zero} zero, {ranged} ranges, {upgraded} upgrades, {unit} unit effects, {duplicates} duplicate-listener contexts, {cancelled} zero-draw cancellations and {capped} capped hands.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
