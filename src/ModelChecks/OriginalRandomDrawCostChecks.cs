using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRandomDrawCostChecks
{
    private static CombatRelicState Owner(int maximum = 4) => new(RelicRandomDrawCostModel.DataId, RelicRandomDrawCostModel.AssetKey,
        [RelicRandomDrawCostModel.Effect, RelicResourceModel.Draw], isCovenant: false, disallowedInPlacementPhase: false,
        resourceEffects: [new(0, RelicRandomDrawCostModel.Effect, 3, 0, 3, false,
            randomDrawCost: new(maximum, true, 0, false, false, false, [])),
            new(1, RelicResourceModel.Draw, 2, 0, 2, true, drawValue: 2, drawMutationOrder: 0)]);
    private static CardInstanceState Card(int cost, int delta = 0, bool champion = false, bool x = false) => new(1, "draw-cost", new(new(cost: delta), [], 0, []),
        CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: new(new("draw-cost", "Spell", "Common", false, false, false,
            [], [], [], [], null, x, 0, 0, false, false, false, isChampionCard: champion), cost, [], []));
    private static CombatContext Context(CardInstanceState card, int seed, bool owner = true) => new(new([new(1, card.DataId)], [], [], UnityRng.Seed(2016), 0, []),
        UnityRng.Seed(seed), 0, 10, 10, cardInstances: [card], relics: owner ? [Owner()] : []);
    internal static void Run()
    {
        Require(RelicModel.Validate([Owner()]) == null && RelicModel.Validate([Owner(5)]) != null, "Random draw cost must validate the entire unchanged owner.");
        var rolls = new HashSet<int>(); var targetCounts = new Dictionary<int, int>();
        for (int seed = 0; seed < 200 && rolls.Count < 6; seed++)
        {
            CombatContext baseContext = Context(Card(1), seed); var random = baseContext.BattleRng.Range(0, 6);
            if (!rolls.Add(random.Value)) continue;
            int nominal = random.Value >= 4 ? random.Value - 3 : random.Value;
            targetCounts[nominal] = targetCounts.GetValueOrDefault(nominal) + 1;
            foreach (var spec in new[] { (0, 0), (1, 0), (99, 0), (3, -2), (1, 2), (3, -99), (0, 99) })
            {
                CombatContext source = Context(Card(spec.Item1, spec.Item2), seed); string parent = JsonSerializer.Serialize(source, ModelJson.Options);
                var result = RelicRandomDrawCostModel.Apply(source, 1, out bool passed);
                int prior = CardBranchMaskModel.Resolve(source.FindCard(1)!).CostWithoutTraits;
                int target = Math.Abs(spec.Item2) >= 99 ? 0 : prior < spec.Item1 ? Math.Max(0, nominal + prior - spec.Item1) : nominal;
                Require(result.Supported && passed && result.Context.BattleRng.Equals(random.State) &&
                    CardBranchMaskModel.Resolve(result.Context.FindCard(1)!).CostWithoutTraits == target, "Random roll, reduction, increase or full-reduction order differs.");
                CardInstanceState changed = result.Context.FindCard(1)!;
                Require(changed.Temporary.Upgrades.Count == (target == prior ? 0 : 1) && changed.Temporary.Upgrades.All(upgrade => upgrade.RemoveOnDiscard && upgrade.Stats.XCost == 0),
                    "No-change draws must consume RNG without adding upgrades; changed draws need removable cost-only upgrades.");
                Require(changed.Temporary.OnDiscard().Upgrades.Count == 0, "Random cost must clear on discard.");
                Parallel.For(0, 16, branch => Equal(RelicRandomDrawCostModel.Apply(source, 1, out _).Context, result.Context, "parallel draw"));
                Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Random draw mutated the parent.");
            }
        }
        Require(rolls.Count == 6 && targetCounts.OrderBy(pair => pair.Key).Select(pair => pair.Value).SequenceEqual([1, 2, 2, 1]), "Missing all six biased RNG outcomes.");
        foreach (int id in new[] { 0, 1 })
        {
            CombatContext source = Context(Card(99, champion: true), 171);
            var result = RelicRandomDrawCostModel.Apply(source, id, out bool passed);
            Require(result.Supported && !passed && ReferenceEquals(result.Context, source), "Null/champion must skip RNG and upgrades.");
        }
        CombatContext absent = Context(Card(3), 171, false);
        Require(ReferenceEquals(RelicRandomDrawCostModel.Apply(absent, 1, out _).Context, absent), "Absent owner changed the branch.");
        CombatContext placement = Context(Card(0), 171);
        Equal(RelicRandomDrawCostModel.Apply(placement, 1, out _, placement: true).Context,
            RelicRandomDrawCostModel.Apply(placement, 1, out _).Context, "deployment allowed");
        CombatContext x = Context(Card(0, x: true), 171);
        Require(RelicRandomDrawCostModel.Apply(x, 1, out bool xPassed).Supported && xPassed, "X-cost cards must receive the native numeric cost upgrade.");
        CombatContext drawSource = Context(Card(3), 171);
        var queueStats = new CardStatModifier(cost: -2);
        var queueUpgrade = new CardUpgradeModifier("", "", queueStats, [], true, false, false, 0, 0, [],
            maskMetadata: new([], false, false, false, false), lifecycle: new(new("", queueStats, [], false, false, false, false), "", false, true, [], [], false, [], [], []));
        var cycle = new CardCycleState([], [new(1, "draw-cost")], [], UnityRng.Seed(2016), 0, [], nextDrawUpgrades: [], nextUpgradeReferenceId: 1);
        drawSource = drawSource.WithCards(NextDrawUpgradeModel.Schedule(cycle, new(queueUpgrade, [])));
        var drawn = NextDrawUpgradeModel.DrawSpecific(drawSource, 1, 0, new([], [new("draw-cost", "cost-spell", 3, "Spell", "Discard", null, [])]));
        int roll = drawSource.BattleRng.Range(0, 6).Value; int biased = roll >= 4 ? roll - 3 : roll;
        Require(drawn.Supported && drawn.Context!.BattleRng.Equals(drawSource.BattleRng.Range(0, 6).State) &&
            CardBranchMaskModel.Resolve(drawn.Context.FindCard(1)!).CostWithoutTraits == Math.Max(0, biased - 2),
            "Specific-card draws must apply queued upgrades before the random cost callback.");
        Console.WriteLine("RANDOM-DRAW-COST-PURE PASS: all six biased rolls; zero/high/positive/negative/full costs, no-change RNG, X/champion/null, discard lifetime, deployment and 16 immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 233 && root.GetProperty("OriginalRandomDrawCostSchema").GetInt32() == 233 &&
            root.GetProperty("ModifierScenario").GetString() == "original-random-draw-cost" && root.GetProperty("OriginalRandomDrawCostPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalRandomDrawCostCaptureFailures", "OriginalRandomDrawCostMismatches",
                "OriginalRandomDrawCostUnsupported", "OriginalRandomDrawCostPending" }.All(name => root.GetProperty(name).GetInt32() == 0), "Native random draw-cost acceptance failed.");
        int[] carriers = root.GetProperty("OriginalRandomDrawCost").GetProperty("Carriers").Deserialize<int[]>()!;
        Require(carriers.Length == 9 && root.GetProperty("Actions").GetArrayLength() == 2, "Missing controlled cost carriers/deployments.");
        var rolls = new HashSet<int>(); var seen = new HashSet<int>(); int count = 0, controls = 0, unchanged = 0, repeated = 0;
        foreach (var row in root.GetProperty("RelicRandomDrawCostRecords").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int id = row.GetProperty("CardId").GetInt32(); bool passed = row.GetProperty("Result").GetBoolean();
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = RelicRandomDrawCostModel.Apply(before.Context!, id, out bool expected);
                Require(result.Supported && expected == passed, "Native callback return differs: " + result.UnsupportedReason);
                Equal(CardSpellModel.WithContext(before, result.Context), actual, "native callback");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            if (row.GetProperty("Control").GetBoolean()) { controls++; Require(!passed, "Null/champion control triggered."); }
            if (passed)
            {
                count++; var random = before.Context!.BattleRng.Range(0, 6); rolls.Add(random.Value);
                CardInstanceState card = before.Context.FindCard(id)!;
                int baseline = row.GetProperty("BaseCost").GetInt32(), prior = row.GetProperty("CostBefore").GetInt32();
                Require(baseline == (card.RawCostOverride ?? card.MaskDescriptor!.BaseCost) && prior == CardBranchMaskModel.Resolve(card).CostWithoutTraits &&
                    row.GetProperty("CostAfter").GetInt32() == CardBranchMaskModel.Resolve(actual.Context!.FindCard(id)!).CostWithoutTraits,
                    "Native numeric cost does not match branch-owned modifiers.");
                if (prior == row.GetProperty("CostAfter").GetInt32()) unchanged++;
                if (!seen.Add(id)) { repeated++; Require(!card.Temporary.Upgrades.Any(upgrade => upgrade.RemoveOnDiscard), "Prior draw's removable fee survived discard/redraw."); }
            }
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native replay mutated a parent.");
        }
        Require(count >= 14 && controls == 2 && rolls.Count == 6 && unchanged > 0 && repeated > 0 && carriers.All(seen.Contains),
            "Missing all rolls, unchanged/full-cost, repeated-draw cleanup or cost control witnesses.");
        bool drawConsumer = root.GetProperty("Turns").EnumerateArray().Any(row =>
        {
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            return actual.DrawPerTurn == 7 && actual.Spawn.Train.Context!.Relics!.Single(owner => owner.DataId == RelicRandomDrawCostModel.DataId)
                .ResourceEffects!.Single(rule => rule.Type == RelicResourceModel.Draw).DrawValue == 2;
        });
        Require(drawConsumer, "Original +2 draw effect has no native seven-card turn consumer.");
        Console.WriteLine($"ORIGINAL-RANDOM-DRAW-COST-NATIVE PASS: {count} draws, all six biased rolls, {unchanged} unchanged costs, {repeated} redraw cleanup witnesses, null/champion controls, +2 draw consumer and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string phase)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Random draw cost " + phase + " differs: " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
