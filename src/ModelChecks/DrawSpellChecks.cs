using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class DrawSpellChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(61);
        var cards = new CardCycleState([new(1, "draw"), new(2, "held")], [new(5, "held"), new(3, "held"), new(4, "held")],
            [new(6, "held")], rng, 3, []);
        CardActionEffect Draw(int amount, CardEffectRange? range = null, CardEffectTests? tests = null) =>
            new("Draw", "Room", amount, false, false, [], range: range, tests: tests);
        BattleTurnState Root(CardActionEffect[] effects, int limit = 10, CardCycleState? piles = null, bool isolated = false)
        {
            var context = new CombatContext(piles ?? cards, rng, 0, 7, limit,
                statistics: BattleStatistics.Empty().TrackCards([1, 2, 3, 4, 5, 6]),
                cardInstances: Enumerable.Range(1, 6).Select(id => CardInstanceState.Empty(id, id == 1 ? "draw" : "held")).ToArray());
            var train = new TrainCombatState([new(0, false, [], [], context), new(1, false,
                [new(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], context)], [], 5, context);
            var spawn = new EnemySpawnState(train, [], [], 0, false, rng, 2, [], 0, false, 0, 0, 0, []);
            var rules = new BattlePlayRules([new(0, 5, 5, true, false, false), new(1, 5, 5, true, false, true)],
                [new("draw", "draw", 1, "Spell", "Discard", null, [], effects, []), new("held", "held", 0, "Null", "Discard", null, [], upgradeInteractions: [])]);
            return new(spawn, 2, 3, 5, 0, 0, "Full", [], [new("Standby", []), new("Exhausted", [])], [], rules, uiRngIsolated: isolated);
        }
        var upgrade = new CardUpgradeModifier("draw-upgrade", "draw-upgrade", new(damage: 2), [], false, false, false, 0, 0, []);
        BattleTurnState root = Root([Draw(2), new("HandUpgrade", "Hand", 0, false, false, [], upgrade, "TemporaryUntilEndOfBattle"), Draw(1)]);
        string parent = JsonSerializer.Serialize(root);
        BattleActionResult played = BattleActionModel.PlayCard(root, new(1, 0));
        Require(played.Supported, "Drawing spell action rejected: " + played.Reason);
        CombatContext result = played.State!.Spawn.Train.Context!;
        Require(result.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([5, 3, 4, 2]) &&
            result.Cards.Discard.Select(card => card.InstanceId).SequenceEqual([6, 1]) && result.Cards.DrawModifier == 3 && played.State.Energy == 1,
            "Draw order, paid-card exclusion, final discard or pending draw modifier differs.");
        Require(result.CardInstances!.Where(card => card.InstanceId is 2 or 3 or 4).All(card => card.Temporary.Upgrades.Count == 1) &&
            result.CardInstances.Where(card => card.InstanceId is 1 or 5 or 6).All(card => card.Temporary.Upgrades.Count == 0),
            "A hand upgrade did not use live membership between two draws.");
        Require(new[] { 3, 4, 5 }.All(id => result.Statistics!.Value(id, "TimesDrawn") == 1) &&
            result.Statistics!.Value(2, "AnyCardDrawn") == 3 && result.Statistics.Value(3, "AnyCardDrawn") == 4 &&
            result.Statistics.Value(1, "TimesDrawn") == 0, "Native draw statistics or source double-count convention differs.");
        CardCycleState fullPiles = new(cards.Hand, [new(3, "held")], [], rng, 3, []);
        BattleActionResult full = BattleActionModel.PlayCard(Root([Draw(-1)], 2, fullPiles), new(1, 0));
        Require(full.Supported && full.State!.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([3, 2]),
            "A full hand failed the native pre-cast test or failed to free its paid slot.");
        Require(BattleActionModel.PlayCard(Root([Draw(0), Draw(-2)]), new(1, 0)).State!.Spawn.Train.Context!.Cards.Hand.Single().InstanceId == 2,
            "Zero or negative counts drew cards or incorrectly prevented casting.");
        CardCycleState shuffled = CardCycleModel.DrawCards(new([], [], [new(3, "held"), new(4, "held"), new(5, "held")], rng, 3, []), 10, 10).State!;
        ShuffleResult<CardToken> expectedShuffle = rng.Shuffle([new CardToken(3, "held"), new(4, "held"), new(5, "held")]);
        Require(shuffled.Hand.Select(card => card.InstanceId).SequenceEqual(expectedShuffle.Items.Select(card => card.InstanceId)) &&
            shuffled.Rng.Equals(expectedShuffle.State) && shuffled.DrawModifier == 3, "Reshuffle order or RNG differs.");
        CardCycleState ignored = CardCycleModel.DrawCards(new([], [new(2, "held"), new(1, "draw")], [new(3, "held")], rng, 3, []), 3, 10, 1).State!;
        Require(ignored.Hand.Single().InstanceId == 2 && ignored.Draw.Single().InstanceId == 1 && ignored.Discard.Single().InstanceId == 3 && ignored.Rng.Equals(rng),
            "An ignored resolving card was drawn or a nonempty deck caused a reshuffle.");
        Require(!CardCycleModel.DrawCards(new([], [], [], rng, 0, ["Draw callback"]), 1, 10).Supported,
            "Unknown draw callbacks returned a partial result.");
        CardCycleState fullIgnored = CardCycleModel.DrawCards(new([new(1, "draw")], [], [new(3, "held"), new(4, "held")], rng, 3, []), 2, 1, 1).State!;
        Require(fullIgnored.Hand.Single().InstanceId == 1 && fullIgnored.Discard.Count == 0 && fullIgnored.Draw.Count == 2 && fullIgnored.Rng.Equals(rng.Next()),
            "A full hand's reserved played slot suppressed the preceding deck selection and reshuffle.");
        Require(CardSpellModel.TestPlay(new(0, false, [], [], new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10)), [Draw(2)], 0).CanPlay,
            "An empty deck incorrectly prevented a native draw-only cast.");
        var range = new CardEffectRange(-2, 4, .75f);
        BattleTurnState rangedRoot = Root([Draw(0, range)], isolated: true);
        SpellCastCheck cast = CardSpellModel.TestPlay(rangedRoot.Spawn.Train, 0, [Draw(0, range)], 0);
        Require(cast.CanPlay && cast.BattleRngAfterTests!.Value.Equals(rng.Next()), "The casting draw test omitted its quantity draw.");
        BattleActionResult ranged = BattleActionModel.PlayCard(rangedRoot, new(1, 0));
        Require(ranged.Supported && ranged.State!.Spawn.Train.Context!.BattleRng.Equals(rng.Next().Next().Next()),
            "Ranged draw must sample in initial test, runtime test and application.");
        Require(!BattleActionModel.PlayCard(Root([Draw(0, range)]), new(1, 0)).Supported,
            "Unisolated ranged-draw UI effects were accepted.");
        CombatContext endedContext = new(cards, rng, 0, 7, 10, allScenarioBossesDead: true);
        RoomCombatResult gated = CardSpellModel.Apply(new(0, false, [], [], endedContext), [Draw(0, range)], 0);
        Require(gated.Supported && gated.State!.Context!.BattleRng.Equals(rng.Next()) && gated.State.Context.Cards.Hand.Count == 2,
            "Post-boss draw failed to sample its test or bypassed its effect gate.");
        var preview = new RoomCombatState(0, false, [], [], root.Spawn.Train.Context, preview: true);
        SpellCastCheck previewCast = CardSpellModel.TestPlay(preview, [Draw(0, range)], 0);
        RoomCombatResult previewApply = CardSpellModel.Apply(preview, [Draw(0, range)], 0);
        Require(!previewCast.CanPlay && previewCast.BattleRngAfterTests!.Value.Equals(rng.Next()) && previewApply.Supported &&
            previewApply.State!.Context!.BattleRng.Equals(rng.Next()) && previewApply.State.Context.Cards.Hand.Count == 2,
            "Preview drawing failed to sample its test or applied a preview-disabled effect.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(BattleActionModel.PlayCard(root, new(1, 0)).State) == JsonSerializer.Serialize(played.State),
            "Parallel drawing branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Drawing mutated its parent.");
        Console.WriteLine("DRAW-SPELL-CHECKS PASS: signed/max/full-hand draws, reshuffle, paid-card exclusion, live hand upgrades, statistics, three quantity phases, post-boss/preview gates and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] cycles = fixture.GetProperty("CardCycles").EnumerateArray().Where(cycle => cycle.GetProperty("Kind").GetString() == "SpellDraw").ToArray();
        int draws = 0, zero = 0, negative = 0, full = 0, reshuffles = 0, modifier = 0;
        foreach (FixtureValue cycle in cycles)
        {
            FixtureValue before = cycle.GetProperty("Before"), after = cycle.GetProperty("Actual");
            int count = cycle.GetProperty("HandSize").GetInt32();
            int gained = after.GetProperty("Hand").GetArrayLength() - before.GetProperty("Hand").GetArrayLength();
            draws += gained;
            if (count == 0 && gained == 0) zero++;
            if (count < 0 && gained == 0) negative++;
            if (before.GetProperty("Hand").GetArrayLength() == cycle.GetProperty("MaxHandSize").GetInt32() && gained == 0) full++;
            if (!before.GetProperty("Rng").ContentEquals(after.GetProperty("Rng"))) reshuffles++;
            if (before.GetProperty("DrawModifier").GetInt32() > 0 && gained > 0 &&
                before.GetProperty("DrawModifier").GetInt32() == after.GetProperty("DrawModifier").GetInt32()) modifier++;
        }
        Require(cycles.Length > 0 && draws > 0 && zero > 0 && negative > 0 && full > 0 && reshuffles > 0 && modifier > 0,
            "Native drawing oracle lacks actual draws, no-op counts, full hands, reshuffles or retained modifiers.");
        var phases = new HashSet<string>(); int samples = 0;
        foreach (FixtureValue sample in fixture.GetProperty("NumericRanges").EnumerateArray())
        {
            var range = new CardEffectRange(sample.GetProperty("Min").GetInt32(), sample.GetProperty("Max").GetInt32(), sample.GetProperty("Multiplier").GetSingle());
            RngDraw expected = range.Sample(sample.GetProperty("Before").Deserialize<UnityRng>());
            Require(expected.Value == sample.GetProperty("Value").GetInt32() && expected.State.Equals(sample.GetProperty("After").Deserialize<UnityRng>()),
                "Native draw quantity or RNG differs.");
            phases.Add(sample.GetProperty("Phase").GetString()!); samples++;
        }
        Require(phases.IsSupersetOf(["Cast", "Test", "Apply"]), "Native draw lacks quantity phases.");
        foreach (var group in fixture.GetProperty("NumericRanges").EnumerateArray().Where(sample => sample.GetProperty("ActionIndex").GetInt32() >= 0 &&
            sample.GetProperty("Phase").GetString() != "Highlight").GroupBy(sample => sample.GetProperty("ActionIndex").GetInt32()))
            Require(group.Select(sample => sample.GetProperty("Phase").GetString()).SequenceEqual(["Cast", "Test", "Apply"]),
                "Native ranged drawing did not run exactly one ordered cast/test/apply sequence.");
        UiRngIsolationChecks.CheckRecords(fixture.GetProperty("UiRngIsolation"));
        Console.WriteLine($"NATIVE-DRAW-SPELL-COVERAGE PASS: {cycles.Length} draw operations, {draws} cards, {zero} zero, {negative} negative, {full} full hands, {reshuffles} reshuffles, {modifier} retained modifiers and {samples} exact quantity samples.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
