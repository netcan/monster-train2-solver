using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRandomFreezeChecks
{
    private static CombatRelicState Owner(int count = 1) => new(RelicRandomFreezeModel.DataId, RelicRandomFreezeModel.AssetKey,
        [RelicRandomFreezeModel.Effect], isCovenant: false, disallowedInPlacementPhase: true,
        cardModifiers: [new(0, null, null, false, randomFreeze: new(count, 0, false))]);
    private static CardInstanceState Card(int id, string type = "Spell", bool frozen = false) => new(id, "freeze-random-" + id,
        CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: new(
            new("freeze-random-" + id, type, "Common", false, false, false, [], [], [], [], null, false, 0, 0, false, false, false),
            99, [], [], temporaryTraits: frozen ? [new("CardTraitFreeze", "CardTraitFreeze", 0, 0, true, 0)] : []));
    private static CombatContext Context(CardInstanceState[] cards, bool owned = true) => new(
        new(cards.Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray(), [], [], UnityRng.Seed(2016), 0, []),
        UnityRng.Seed(1761), 0, 10, 10, cardInstances: cards, relics: owned ? [Owner()] : []);
    internal static void Run()
    {
        CombatContext source = Context([Card(1, "Monster"), Card(2), Card(3, "Equipment"), Card(4, "Blight"), Card(5, "Junk"), Card(6, frozen: true)]);
        Require(RelicRandomFreezeModel.Candidates(source, out string? error)!.SequenceEqual([1, 2, 3]) && error == null,
            "Native filter must accept ordinary Monster/Spell/Equipment and exclude Blight/Junk/already frozen.");
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        foreach (var gate in new[] { (true, false), (false, true) })
        {
            var result = RelicRandomFreezeModel.Apply(source, gate.Item1, gate.Item2);
            Require(result.Supported && JsonSerializer.Serialize(result.Context, ModelJson.Options) == parent, "Deployment/preview changed random freeze.");
        }
        var applied = RelicRandomFreezeModel.Apply(source, false, false);
        Require(applied.Supported && applied.Context.BattleRng.Equals(source.BattleRng.Shuffle(new[] { 1, 2, 3 }).State) &&
            applied.Context.CardInstances!.Count(card => card.MaskDescriptor!.TemporaryTraits.Any(trait => trait.RuntimeType == "CardTraitFreeze")) == 2,
            "Random freeze did not shuffle eligible cards then add exactly one temporary trait.");
        CombatContext single = Context([Card(1), Card(2, "Junk")]);
        var singleton = RelicRandomFreezeModel.Apply(single, false, false);
        Require(singleton.Supported && singleton.Context.BattleRng.Equals(single.BattleRng) &&
            singleton.Context.FindCard(1)!.MaskDescriptor!.TemporaryTraits.Single().RuntimeType == "CardTraitFreeze",
            "A singleton shuffle must add Freeze without drawing RNG.");
        CombatContext empty = Context([Card(1, "Blight"), Card(2, "Junk"), Card(3, frozen: true)]);
        Require(JsonSerializer.Serialize(RelicRandomFreezeModel.Apply(empty, false, false).Context, ModelJson.Options) == JsonSerializer.Serialize(empty, ModelJson.Options),
            "Empty filtered candidates changed state or RNG.");
        CombatContext absent = Context([Card(1)], false);
        Require(ReferenceEquals(RelicRandomFreezeModel.Apply(absent, false, false).Context, absent) && RelicModel.Validate([Owner(2)]) != null,
            "Absent owner or changed original count was accepted incorrectly.");
        string expected = JsonSerializer.Serialize(applied.Context, ModelJson.Options);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(RelicRandomFreezeModel.Apply(source, false, false).Context, ModelJson.Options) == expected,
            "Parallel random-freeze branch diverged."));
        Require(JsonSerializer.Serialize(source, ModelJson.Options) == parent, "Random freeze mutated its parent.");
        Console.WriteLine("RANDOM-FREEZE-PURE PASS: type/frozen filtering, deployment/preview/absence gates, whole-candidate shuffle, singleton/empty RNG, strict original count and immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 229 && root.GetProperty("OriginalRandomFreezeSchema").GetInt32() == 229 &&
            root.GetProperty("ModifierScenario").GetString() == "original-random-freeze" && root.GetProperty("OriginalRandomFreezePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalRandomFreezeCaptureFailures",
                "OriginalRandomFreezeMismatches", "OriginalRandomFreezeUnsupported", "OriginalRandomFreezePending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Native random-freeze acceptance failed.");
        int[] carriers = root.GetProperty("OriginalRandomFreeze").GetProperty("Carriers").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(carriers.Length == 5 && actions.Length == 2 && actions.All(row => row.GetProperty("ScenarioAction").GetBoolean()), "Missing complete controlled deployment.");
        int positives = 0, negatives = 0;
        foreach (var row in root.GetProperty("RelicRandomFreezeTests").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            int[] candidates = RelicRandomFreezeModel.Candidates(before.Context!, out string? error)!;
            Require(error == null && candidates.SequenceEqual(row.GetProperty("Candidates").Deserialize<int[]>()!) &&
                row.GetProperty("Result").GetBoolean() == (candidates.Length > 0), "Native random-freeze Test or ordered filter differs.");
            Equal(before, row.GetProperty("Actual").Deserialize<TrainCombatState>(), "Test state/RNG");
            if (candidates.Length > 0) positives++; else negatives++;
        }
        int applications = 0; var lengths = new List<int>(); var selected = new List<int>();
        foreach (var row in root.GetProperty("RelicRandomFreezeApplications").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native random-freeze coroutine.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = RelicRandomFreezeModel.ApplySelected(before);
                Require(result.Supported, "Unsupported random-freeze application: " + result.UnsupportedReason);
                Equal(result.State, actual, "shuffle/trait/complete state");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            int[] candidates = RelicRandomFreezeModel.Candidates(before.Context!, out string? error)!; lengths.Add(candidates.Length);
            Require(error == null && actual.Context!.BattleRng.Equals(before.Context!.BattleRng.Shuffle(candidates).State), "Native shuffle RNG differs.");
            int[] newlyFrozen = candidates.Where(id => actual.Context!.FindCard(id)!.MaskDescriptor!.TemporaryTraits.Any(trait => trait.RuntimeType == "CardTraitFreeze")).ToArray();
            Require(newlyFrozen.Length == 1 && newlyFrozen[0] == before.Context!.BattleRng.Shuffle(candidates).Items[0], "Native freeze did not select the shuffled first card.");
            selected.Add(newlyFrozen[0]);
            Require(actual.Context!.FindCard(carriers[2])!.MaskDescriptor!.TemporaryTraits.Count(trait => trait.RuntimeType == "CardTraitFreeze") == 1 &&
                carriers.Skip(3).All(id => !CardBranchMaskModel.Resolve(actual.Context.FindCard(id)!).Traits.Contains("CardTraitFreeze")),
                "Already-frozen, Blight or Junk controls were changed.");
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native replay mutated its random-freeze parent."); applications++;
        }
        Require(positives == 2 && applications == 2 && negatives >= 1 && lengths.SequenceEqual([2, 1]) &&
            selected.Order().SequenceEqual(carriers.Take(2).Order()), "Missing multiple/singleton/empty candidates or complete native selection.");
        Console.WriteLine($"ORIGINAL-RANDOM-FREEZE-NATIVE PASS: {positives} positive/{negatives} empty Tests, 2->1 candidates, exact whole-list shuffle and singleton RNG, unchanged frozen/Blight/Junk controls, retained temporary traits and 16 immutable branches.");
    }
    private static void Equal<T>(T a, T b, string label)
    {
        string x = JsonSerializer.Serialize(a, ModelJson.Options), y = JsonSerializer.Serialize(b, ModelJson.Options);
        Require(x == y, "Random freeze " + label + " differs: " + ModelJson.Difference(x, y));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
