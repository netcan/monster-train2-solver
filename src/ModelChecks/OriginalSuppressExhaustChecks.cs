using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSuppressExhaustChecks
{
    private static CombatRelicState Owner(bool passed = false, int chance = 50) => new(RelicSuppressExhaustModel.DataId, RelicSuppressExhaustModel.AssetKey,
        [RelicSuppressExhaustModel.Effect], isCovenant: false, disallowedInPlacementPhase: false,
        resourceEffects: [new(0, RelicSuppressExhaustModel.Effect, 50, 0, 50, false, suppressExhaust: new(chance, "Spell", passed, []))]);
    private static CardInstanceState Card(string type = "Spell", params string[] traits)
    {
        var card = new CardInstanceState(1, "suppress-card", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: new(
            new("suppress-card", type, "Common", false, false, false, [], [], [], [], null, false, 0, 0, false, false, false), 0,
            traits.Select(trait => new CardTraitValue(trait, trait, 0, 0, true, 0)).ToArray(), []));
        return CardExhaustModel.Initialize(card, 0);
    }
    private static CombatContext Context(CardInstanceState card, int seed, bool passed = false, bool owner = true) => new(
        new([new(1, card.DataId)], [], [], UnityRng.Seed(2016), 0, []), UnityRng.Seed(seed), 0, 10, 10,
        cardInstances: [card], relics: owner ? [Owner(passed)] : []);
    internal static void Run()
    {
        Require(RelicModel.Validate([Owner()]) == null && RelicModel.Validate([Owner(chance: 49)]) != null, "Original suppression definition validation failed.");
        var observed = new HashSet<int>();
        for (int seed = 0; seed < 2000 && !(observed.Contains(49) && observed.Contains(50)); seed++)
        {
            CombatContext source = Context(Card("Spell", RelicSuppressExhaustModel.Trait), seed, true);
            var random = source.BattleRng.Range(0, 100); observed.Add(random.Value);
            if (random.Value is not (49 or 50)) continue;
            string parent = JsonSerializer.Serialize(source, ModelJson.Options);
            var result = RelicSuppressExhaustModel.PreDiscard(source, 1, true, out bool hit, out string? suppression);
            Require(result.Supported && hit == (random.Value < 50) && RelicSuppressExhaustModel.Test(result.Context) == hit &&
                result.Context.BattleRng.Equals(random.State) && suppression == (hit ? RelicSuppressExhaustModel.Trait : null), "50% boundary, cache or output suppression differs.");
            Require(RelicSuppressExhaustModel.Test(result.Context) == hit && RelicSuppressExhaustModel.Test(result.Context) == hit, "Test must preserve its cached result.");
            var discarded = RelicSuppressExhaustModel.Discarded(result.Context, out string? after, suppression);
            Require(discarded.Supported && !RelicSuppressExhaustModel.Test(discarded.Context) && after == suppression && discarded.Context.BattleRng.Equals(random.State), "Conditional Apply must reset a hit without RNG.");
            var preview = RelicSuppressExhaustModel.Discarded(result.Context, out _, suppression, preview: true);
            Require(ReferenceEquals(preview.Context, result.Context), "Preview manager gate must preserve the cache.");
            CardInstanceState card = source.FindCard(1)!;
            Require(card.OnDiscard(true, suppressConsume: true).PlayCount == 1 && !card.OnDiscard(true, suppressConsume: true).ExhaustState!.BaseTraits.Single().IsExhausted &&
                card.OnDiscard(true).ExhaustState!.BaseTraits.Single().IsExhausted, "Suppression must skip Consume while retaining ordinary play-count cleanup.");
            Parallel.For(0, 16, branch => Equal(RelicSuppressExhaustModel.PreDiscard(source, 1, true, out _, out _).Context, result.Context, "parallel pre-discard"));
            Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Suppression mutated the parent.");
        }
        Require(observed.Contains(49) && observed.Contains(50), "Missing exact 49/50 random boundaries.");
        foreach (var entry in new[] { (Card("Spell", RelicSuppressExhaustModel.Trait), false), (Card("Spell"), true),
            (Card("Monster", RelicSuppressExhaustModel.Trait), true), (Card("Spell", RelicSuppressExhaustModel.Trait, "CardTraitPersistent"), true),
            (Card("Spell", RelicSuppressExhaustModel.Trait, "CardTraitInfusion"), true) })
        {
            CombatContext source = Context(entry.Item1, 77, true);
            var result = RelicSuppressExhaustModel.PreDiscard(source, 1, entry.Item2, out bool hit, out string? suppression, "existing-trait");
            Require(result.Supported && !hit && !RelicSuppressExhaustModel.Test(result.Context) && suppression == "existing-trait" && result.Context.BattleRng.Equals(source.BattleRng),
                "Excluded pre-discard must clear the cache, preserve prior suppression and consume no RNG.");
        }
        CombatContext absent = Context(Card("Spell", RelicSuppressExhaustModel.Trait), 77, owner: false);
        Require(ReferenceEquals(RelicSuppressExhaustModel.PreDiscard(absent, 1, true, out _, out _).Context, absent), "Absent owner changed state.");
        CombatContext failedTest = Context(Card("Spell", RelicSuppressExhaustModel.Trait), 77);
        var direct = RelicSuppressExhaustModel.Apply(failedTest, out string? directSuppression);
        Require(direct.Supported && directSuppression == RelicSuppressExhaustModel.Trait && !RelicSuppressExhaustModel.Test(direct.Context) &&
            direct.Context.BattleRng.Equals(failedTest.BattleRng), "The direct Apply body must suppress Consume even when the cached Test is false.");
        Require(ReferenceEquals(RelicSuppressExhaustModel.Discarded(failedTest, out _).Context, failedTest), "The manager must skip Apply when the cached Test is false.");
        Console.WriteLine("SUPPRESS-EXHAUST-PURE PASS: exact 49/50 boundary, sticky Test, unconditional body/conditional manager reset, exclusions, prior-output preservation, preview, play count/Consume routing and 16 immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 234 && root.GetProperty("OriginalSuppressExhaustSchema").GetInt32() == 234 &&
            root.GetProperty("ModifierScenario").GetString() == "original-suppress-exhaust" && root.GetProperty("OriginalSuppressExhaustPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalSuppressExhaustCaptureFailures", "OriginalSuppressExhaustMismatches",
                "OriginalSuppressExhaustUnsupported", "OriginalSuppressExhaustPending" }.All(name => root.GetProperty(name).GetInt32() == 0), "Native suppression acceptance failed.");
        int[] carriers = root.GetProperty("OriginalSuppressExhaust").GetProperty("Carriers").Deserialize<int[]>()!;
        Require(carriers.Length == 8 && root.GetProperty("Actions").GetArrayLength() == 8, "Missing suppression carriers or controlled actions.");
        int hit = 0, miss = 0, applied = 0, sticky = 0, excluded = 0; var decisions = new Dictionary<int, bool>();
        foreach (var row in root.GetProperty("RelicSuppressExhaustRecords").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            string phase = row.GetProperty("Phase").GetString()!; int id = row.GetProperty("CardId").GetInt32();
            bool played = row.GetProperty("WasPlayed").GetBoolean(), control = row.GetProperty("Control").GetBoolean(), returned = row.GetProperty("Result").GetBoolean();
            string? outputBefore = row.GetProperty("SuppressionBefore").GetString(), outputAfter = row.GetProperty("SuppressionAfter").GetString();
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                bool passed = false; string? suppression = outputBefore; RelicFirstCardCostResult result;
                if (phase == "Pre") result = RelicSuppressExhaustModel.PreDiscard(before.Context!, id, played, out passed, out suppression, suppression);
                else if (phase == "Apply") result = RelicSuppressExhaustModel.Apply(before.Context!, out suppression, suppression);
                else { passed = RelicSuppressExhaustModel.Test(before.Context!); result = new(before.Context!); }
                Require(result.Supported && (phase == "Apply" || returned == passed) && suppression == outputAfter, "Native suppression return/output differs: " + result.UnsupportedReason);
                Equal(CardSpellModel.WithContext(before, result.Context), actual, "native " + phase);
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            if (phase == "Test" && returned) sticky++;
            if (phase == "Apply") { applied++; Require(!RelicSuppressExhaustModel.Test(actual.Context!), "Native Apply did not reset its cache."); }
            if (phase == "Pre" && !control && carriers.Take(4).Contains(id)) { decisions[id] = returned; if (returned) hit++; else miss++; }
            if (phase == "Pre" && control && carriers.Skip(4).Contains(id))
            { excluded++; Require(before.Context!.BattleRng.Equals(actual.Context!.BattleRng) && !returned, "An excluded control consumed RNG or passed."); }
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native suppression replay mutated its parent.");
        }
        Require(hit > 0 && miss > 0 && decisions.Count == 4 && applied >= hit + 1 && sticky >= 2 && excluded == 4, "Missing natural hit/miss routing, sticky Tests, reset or all exclusion controls.");
        int routed = 0;
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            int id = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32(); if (!decisions.TryGetValue(id, out bool suppressed)) continue;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!; CombatContext context = actual.Spawn.Train.Context!;
            bool discarded = context.Cards.Discard.Any(card => card.InstanceId == id);
            bool exhausted = actual.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id);
            Require(discarded == suppressed && exhausted != suppressed && context.FindCard(id)!.PlayCount == 1 &&
                context.FindCard(id)!.ExhaustState!.BaseTraits.Single().IsExhausted != suppressed, "Natural Consume suppression routed to the wrong pile/private trait state.");
            routed++;
        }
        Require(routed == 4, "Missing all four natural consumer endpoints.");
        Console.WriteLine($"ORIGINAL-SUPPRESS-EXHAUST-NATIVE PASS: {hit} natural hits/{miss} misses, four exact discard/Consume endpoints, {applied} Apply resets, {sticky} cached true Tests, four excluded controls and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string phase)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options); Require(a == b, phase + ": " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
