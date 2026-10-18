using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRandomHandUpgradeChecks
{
    private static CardUpgradeModifier Upgrade() => new(RelicRandomHandUpgradeModel.UpgradeId, "FreeFromBlessing", new(cost: -99), [],
        false, true, false, 0, 0, [], maskMetadata: new([], false, false, false, false));
    private static RelicRandomHandUpgrade Rule(int count = 1) => new(count, "Monster", false, Upgrade(),
        [new("ExcludeZeroAndXCost", rarities: new(requiredOperator: 1), minCost: 1, excludeXCost: true),
         new("ExcludeUnplayable", rarities: new(requiredOperator: 1), traits: new(excluded: ["CardTraitUnplayable"]))]);
    private static CombatRelicState Owner(int count = 1) => new(RelicRandomHandUpgradeModel.DataId, RelicRandomHandUpgradeModel.AssetKey,
        [RelicRandomHandUpgradeModel.Effect], isCovenant: false, disallowedInPlacementPhase: true,
        cardModifiers: [new(0, null, null, false, randomHandUpgrade: Rule(count))]);
    private static CardInstanceState Card(int id, string type = "Spell", int cost = 99, bool x = false, bool unplayable = false) => new(id,
        "random-hand-" + id, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: new(
            new("random-hand-" + id, type, "Common", false, false, false, [], [], [], [], null, x, 0, 0, false, false, false), cost,
            unplayable ? [new("CardTraitUnplayable", "CardTraitUnplayable", 0, 0, true, 0)] : [], []));
    private static CombatContext Context(CardInstanceState[] cards, bool owned = true) => new(
        new(cards.Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray(), [], [], UnityRng.Seed(2016), 0, []),
        UnityRng.Seed(1761), 0, 10, 10, cardInstances: cards, relics: owned ? [Owner()] : []);
    private static RelicRandomHandUpgrade State(CombatContext context) => context.Relics!.Single(owner => owner.DataId == RelicRandomHandUpgradeModel.DataId)
        .CardModifiers!.Single().RandomHandUpgrade!;
    internal static void Run()
    {
        CombatContext source = Context([Card(1, "Monster", 0), Card(2), Card(3), Card(4, cost: 0), Card(5, x: true), Card(6, unplayable: true)]);
        Require(RelicModel.Validate(source.Relics) == null && RelicModel.Validate([Owner(2)]) != null, "Missing strict original random-upgrade validation.");
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        var tested = RelicRandomHandUpgradeModel.Test(source, 1, out bool passed);
        Require(tested.Supported && passed && State(tested.Context).SelectedCardIds.SequenceEqual(source.BattleRng.Shuffle(new[] { 2, 3 }).Items.Take(1)) &&
            tested.Context.BattleRng.Equals(source.BattleRng.Shuffle(new[] { 2, 3 }).State) && State(tested.Context).UpgradedCardIds.Count == 0,
            "Test must filter first, shuffle the entire candidate list and cache one selection before applying.");
        var gated = RelicRandomHandUpgradeModel.Test(tested.Context, 2, out bool spellPassed);
        Require(!spellPassed && ReferenceEquals(gated.Context, tested.Context), "Wrong played type must preserve cached selection and RNG.");
        var applied = RelicRandomHandUpgradeModel.Apply(tested.Context); int selected = State(applied.Context).SelectedCardIds.Single();
        Require(applied.Supported && CardBranchMaskModel.Resolve(applied.Context.FindCard(selected)!).CostWithoutTraits == 0 &&
            State(applied.Context).UpgradedCardIds.SequenceEqual([selected]) && applied.Context.FindCard(selected)!.Temporary.Upgrades.Single().RemoveOnDiscard == false,
            "The original upgrade must make one card free and track its separate discard lifetime.");
        var second = RelicRandomHandUpgradeModel.Test(applied.Context, 1, out bool secondPassed);
        Require(second.Supported && secondPassed && second.Context.BattleRng.Equals(applied.Context.BattleRng) && State(second.Context).SelectedCardIds.Single() != selected,
            "Singleton selection must exclude the now-free card and consume no RNG.");
        var twice = RelicRandomHandUpgradeModel.Apply(second.Context);
        var empty = RelicRandomHandUpgradeModel.Test(twice.Context, 1, out bool emptyPassed);
        Require(empty.Supported && !emptyPassed && State(empty.Context).SelectedCardIds.Count == 0 && empty.Context.BattleRng.Equals(twice.Context.BattleRng),
            "An empty Test must clear cached selection without changing RNG.");
        var cleared = RelicRandomHandUpgradeModel.Discarded(empty.Context, selected);
        Require(cleared.Supported && cleared.Context.FindCard(selected)!.Temporary.Upgrades.Count == 0 &&
            !State(cleared.Context).UpgradedCardIds.Contains(selected) && CardBranchMaskModel.Resolve(cleared.Context.FindCard(selected)!).CostWithoutTraits == 99,
            "Discard must remove the original upgrade and first matching tracked entry.");
        Require(ReferenceEquals(RelicRandomHandUpgradeModel.Discarded(cleared.Context, selected).Context, cleared.Context), "Repeated discard must be inert after untracking.");
        foreach (string type in new[] { "Monster", "Equipment" })
        {
            CombatContext any = Context([Card(1, "Monster", 0), Card(2, type)]);
            Require(RelicRandomHandUpgradeModel.Test(any, 1, out bool eligible).Supported && eligible, "Target filters must preserve other card types.");
        }
        CombatContext absent = Context([Card(1, "Monster", 0)], false);
        Require(ReferenceEquals(RelicRandomHandUpgradeModel.Played(absent, 1).Context, absent), "Absent owner changed the parent.");
        foreach (var gate in new[] { (true, false), (false, true) })
            Require(ReferenceEquals(RelicCardPlayMagicPowerModel.Apply(source, 1, null, placement: gate.Item1, preview: gate.Item2).Context, source),
                "Deployment/preview must skip this original owner.");
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(RelicRandomHandUpgradeModel.Played(source, 1).Context, ModelJson.Options) ==
            JsonSerializer.Serialize(applied.Context, ModelJson.Options), "Random-upgrade branch diverged."));
        Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Random-upgrade mutated its parent.");
        Console.WriteLine("RANDOM-HAND-UPGRADE-PURE PASS: original filters, Test-stage whole-list RNG/cache, wrong-type cache preservation, singleton/empty paths, apply/discard lifetime, deployment/preview and immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 230 && root.GetProperty("OriginalRandomHandUpgradeSchema").GetInt32() == 230 &&
            root.GetProperty("ModifierScenario").GetString() == "original-random-hand-upgrade" && root.GetProperty("OriginalRandomHandUpgradePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalRandomHandUpgradeCaptureFailures",
                "OriginalRandomHandUpgradeMismatches", "OriginalRandomHandUpgradeUnsupported", "OriginalRandomHandUpgradePending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Native random hand-upgrade acceptance failed.");
        int[] carriers = root.GetProperty("OriginalRandomHandUpgrade").GetProperty("Carriers").Deserialize<int[]>()!;
        Require(carriers.Length == 5 && root.GetProperty("Actions").GetArrayLength() == 6 && root.GetProperty("Actions").EnumerateArray().All(row => row.GetProperty("ScenarioAction").GetBoolean()),
            "Missing complete controlled policy or exclusion carriers.");
        var counts = new Dictionary<string, int>(); int positive = 0, empty = 0, wrongType = 0;
        var chosen = new List<int>(); var cleared = new List<int>();
        foreach (var row in root.GetProperty("RelicRandomHandUpgradeRecords").EnumerateArray())
        {
            string phase = row.GetProperty("Phase").GetString()!; int id = row.GetProperty("CardId").GetInt32();
            counts[phase] = counts.GetValueOrDefault(phase) + 1;
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(row.GetProperty("Completed").GetBoolean() && RelicModel.Validate(before.Context!.Relics) == null, "Incomplete native effect or original definition.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                CombatContext context = before.Context!; RelicCheaperSpellUpgradeResult result;
                if (phase == "TestPlay") { result = RelicRandomHandUpgradeModel.Test(context, id, out bool passed); Require(passed == row.GetProperty("Result").GetBoolean(), "Native card-play Test differs."); }
                else if (phase == "ApplyPlay") result = RelicRandomHandUpgradeModel.Apply(context);
                else if (phase == "ApplyDiscard") result = RelicRandomHandUpgradeModel.Discarded(context, id);
                else { Require(State(context).UpgradedCardIds.Contains(id) == row.GetProperty("Result").GetBoolean(), "Native discard Test differs."); result = new(context); }
                Require(result.Supported, "Unsupported native random hand upgrade: " + result.UnsupportedReason);
                Equal(CardSpellModel.WithContext(before, result.Context), actual, phase);
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            if (phase == "TestPlay")
            {
                if (row.GetProperty("Result").GetBoolean()) { positive++; chosen.Add(State(actual.Context!).SelectedCardIds.Single()); }
                else if (before.Context!.FindCard(id)!.MaskDescriptor!.Definition.CardType == "Monster") empty++; else wrongType++;
            }
            if (phase == "ApplyPlay")
                Require(carriers.Skip(2).All(control => before.Context!.FindCard(control)!.Temporary.Upgrades.Count == actual.Context!.FindCard(control)!.Temporary.Upgrades.Count),
                    "Zero/X/unplayable control was upgraded.");
            if (phase == "ApplyDiscard")
            {
                cleared.Add(id); Require(before.Context!.FindCard(id)!.Temporary.Upgrades.Any(upgrade => upgrade.DataId == RelicRandomHandUpgradeModel.UpgradeId) &&
                    !actual.Context!.FindCard(id)!.Temporary.Upgrades.Any(upgrade => upgrade.DataId == RelicRandomHandUpgradeModel.UpgradeId) &&
                    !State(actual.Context).UpgradedCardIds.Contains(id), "Native discard did not remove its tracked discount.");
            }
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native replay mutated its parent.");
        }
        Require(positive == 2 && empty >= 1 && wrongType >= 1 && counts.GetValueOrDefault("ApplyPlay") == 2 && counts.GetValueOrDefault("ApplyDiscard") == 2 &&
            chosen.Order().SequenceEqual(carriers.Take(2).Order()) && cleared.Order().SequenceEqual(carriers.Take(2).Order()), "Missing positive/empty/wrong-type or both discard witnesses.");
        Console.WriteLine($"ORIGINAL-RANDOM-HAND-UPGRADE-NATIVE PASS: {positive} positive, {empty} empty and {wrongType} wrong-type Tests; original zero/X/unplayable filters, Test RNG/cache, two temporary upgrades, played/bulk discard cleanup and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string phase)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Random hand upgrade " + phase + " differs: " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
