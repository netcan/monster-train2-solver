using System.Diagnostics;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DevotionChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        FixtureValue[] samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeDevotionResourceAndCardEffects" &&
            root.GetProperty("LiveContextUnchanged").GetBoolean() && root.GetProperty("EffectsDrained").GetBoolean() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null && root.GetProperty("Mismatches").GetInt32() == 0 &&
            samples.Length == 21 && samples.Select(sample => sample.GetProperty("Label").GetString()).Distinct().Count() == 21,
            "Incomplete current-version native Devotion component.");
        FixtureValue original = root.GetProperty("OriginalVenerate");
        Require(original.GetProperty("Id").GetString() == "96c9ed37-5e59-4c4f-a914-ae7040d24755" &&
            original.GetProperty("Key").GetString() == "Venerate" && original.GetProperty("Cost").GetInt32() == 3 &&
            original.GetProperty("Type").GetString() == "CardEffectGainDevotionPoints" && original.GetProperty("Int").GetInt32() == 6 &&
            original.GetProperty("PerLevel").GetInt32() == 0 && original.GetProperty("Traits").EnumerateArray()
                .Select(trait => trait.GetString()).SequenceEqual(["CardTraitExhaustState"]), "Changed original Venerate payload.");
        Equal(root.GetProperty("RngBefore").ToObjectGraph(), root.GetProperty("RngAfter").ToObjectGraph(), "Complete component RNG restoration");
        foreach (FixtureValue sample in samples) Verify(sample);
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        Expect("award-below-first", 0, 2); Expect("award-exact-first", 1, 0); Expect("award-first-remainder", 1, 1);
        Expect("award-exact-second", 2, 0); Expect("award-multiple-levels", 6, 4);
        Expect("award-existing-level", 3, 0); Expect("award-existing-remainder", 4, 10);
        Expect("award-zero", 0, 2); Expect("award-negative", 0, 2); Expect("award-signed-addition", 1, 0);
        Expect("award-preview", 1, 5); Expect("original-venerate", 1, 3); Expect("original-venerate-later", 2, 5);
        Expect("effect-per-level", 2, 8); Expect("effect-crosses-level", 2, 1); Expect("effect-multiple-levels", 6, 4);
        Expect("effect-negative-base-positive-total", 2, 3); Expect("effect-rejected-zero-total", 2, 4);
        Expect("effect-rejected-negative-total", 2, 4); Expect("effect-preview", 1, 5);
        Parallel.For(0, 32, _ => { foreach (FixtureValue sample in samples) Verify(sample); });
        GuardsAndKeys(samples[0]);
        Console.WriteLine("DEVOTION PASS: 21 native complete-train successors, arithmetic thresholds/remainders, level notification order, original Venerate effect, per-level/negative/range/preview controls, level/passive queries, RNG, state keys, missing-input guards and 32 immutable parallel branches. Devote, spawn passives and whole-battle acceptance remain separate.");

        void Expect(string label, int level, int points)
        {
            DevotionState state = byLabel[label].GetProperty("Actual").Deserialize<TrainCombatState>()!.Context!.QueryFrame!.Devotion!;
            Require(state.Level == level && state.Points == points, "Missing independent threshold/control witness: " + label);
        }
    }
    private static void Verify(FixtureValue sample)
    {
        string label = sample.GetProperty("Label").GetString()!;
        Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            sample.GetProperty("PendingAfterDrain").GetInt32() == 0, "Incomplete native successor: " + label);
        TrainCombatState before = sample.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState actual = sample.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        FixtureValue input = sample.GetProperty("Input"); bool preview = input.GetProperty("Preview").GetBoolean();
        int amount = input.GetProperty("Amount").GetInt32(); string parent = Serialize(before);
        Require(before.Context!.QueryFrame!.Devotion is { FirstThreshold: 3, ThresholdIncrease: 3, PassiveStacksPerLevel: 2 },
            "Missing original balance constants: " + label);
        TrainCombatState predicted;
        if (sample.GetProperty("Action").ValueKind == FixtureKind.Null)
        {
            DevotionResult result = DevotionModel.Award(before.Context, amount, preview);
            Require(result.Supported, label + ": " + result.UnsupportedReason);
            Require(result.LeveledUp == sample.GetProperty("NativeLeveled").GetBoolean(), "Native award return differs: " + label);
            predicted = CardSpellModel.WithContext(before, result.Context!);
        }
        else
        {
            CardActionEffect action = sample.GetProperty("Action").Deserialize<CardActionEffect>()!;
            Require(action.Type == DevotionModel.Effect && action.DevotionPerLevel.HasValue &&
                sample.GetProperty("CanApplyInPreview").GetBoolean() == false &&
                DevotionModel.Test(before.Context, action) == sample.GetProperty("NativeTest").GetBoolean(), "Native effect admission differs: " + label);
            TrainSpellResult result = CardSpellModel.Apply(before, before.Context.QueryFrame.SelectedRoomIndex!.Value, [action], 0);
            Require(result.Supported, label + ": " + result.UnsupportedReason); predicted = result.State!;
        }
        StatisticQueryResult query = StatisticQueryModel.Evaluate(predicted.Context!, new("MonolithLevel"));
        Require(query.Supported && query.Value == sample.GetProperty("NativeLevelQuery").GetInt32(), "Native level statistic differs: " + label);
        predicted = CardSpellModel.WithContext(predicted, query.Context!);
        Equal(predicted, actual, label + " complete train successor");
        DevotionState state = predicted.Context!.QueryFrame!.Devotion!;
        Require(sample.GetProperty("NativePassiveStacks").EnumerateArray().Select(value => value.GetInt32()).SequenceEqual(
            sample.GetProperty("Multipliers").EnumerateArray().Select(value => DevotionModel.PassiveStacks(state, value.GetInt32()))),
            "Native passive stack multiplier differs: " + label);
        bool leveled = state.Level > before.Context.QueryFrame.Devotion!.Level;
        Require(sample.GetProperty("Notifications").EnumerateArray().Select(value => value.GetString()).SequenceEqual(
            leveled ? ["Cards", "Monsters"] : Array.Empty<string>()), "Native level notification order differs: " + label);
        if (input.GetProperty("Range").GetBoolean())
            Require(!before.Context.BattleRng.Equals(actual.Context!.BattleRng) && state.Level == 1, "Range case lacks a real RNG draw and level crossing.");
        else Equal(HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngBefore")),
            HalfHealthLifecycleChecks.GameplayRng(sample.GetProperty("RngAfter")), label + " gameplay RNG");
        Require(Serialize(before) == parent, "A Devotion branch mutated its parent: " + label);
    }
    private static void GuardsAndKeys(FixtureValue sample)
    {
        TrainCombatState train = sample.GetProperty("Before").Deserialize<TrainCombatState>()!;
        CombatContext context = train.Context!; string parent = Serialize(context);
        var missing = context.WithQueryFrame(new StatisticQueryFrame());
        Require(!DevotionModel.Award(missing, 6).Supported && !StatisticQueryModel.Evaluate(missing, new("MonolithLevel")).Supported,
            "Missing Devotion inputs were inferred from a legacy frame.");
        var timer = Stopwatch.StartNew();
        var huge = DevotionModel.Award(context.WithQueryFrame(context.QueryFrame!.With(devotion: new(0, 0, 1, 0, 2))), int.MaxValue);
        Require(huge.Supported && huge.Context!.QueryFrame!.Devotion is { Level: int.MaxValue, Points: 0 } && timer.Elapsed.TotalSeconds < 2,
            "Large constant-threshold awards did not use a bounded arithmetic calculation.");
        using var keys = new BattleStateKeys();
        var leveled = CardSpellModel.WithContext(train, context.WithQueryFrame(context.QueryFrame!.With(devotion: new(1, 0, 3, 3, 2))));
        var remainder = CardSpellModel.WithContext(train, context.WithQueryFrame(context.QueryFrame!.With(devotion: new(0, 1, 3, 3, 2))));
        Require(keys.SnapshotKey(train) != keys.SnapshotKey(leveled) && keys.SnapshotKey(train) != keys.SnapshotKey(remainder),
            "Pruning keys merged different Devotion levels or remainders.");
        Require(Serialize(context) == parent, "Devotion guards or key checks mutated their parent.");
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? predicted, object? actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(predicted), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
