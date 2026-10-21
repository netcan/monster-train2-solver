using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HoldoverChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("HoldoverCalibrationPrepared").GetBoolean() &&
            root.GetProperty("HoldoverControlsUnchanged").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing isolated Holdover calibration or settled terminal state.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        FixtureValue[] rows = root.GetProperty("HoldoverCallbacks").EnumerateArray().ToArray();
        int controls = 0, returned = 0, ordinary = 0, unplayable = 0, recast = 0, reset = 0;
        foreach (FixtureValue row in rows)
        {
            CardInstanceState before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            CardInstanceState after = row.GetProperty("After").Deserialize<CardInstanceState>()!;
            string parent = Json(before);
            bool control = row.GetProperty("Control").GetBoolean();
            if (control) controls++;
            Require(CardRetainModel.Validate(before) == null, "Native Holdover state was rejected.");
            switch (row.GetProperty("Kind").GetString())
            {
                case "Discard":
                    bool played = row.GetProperty("WasPlayed").GetBoolean(), copy = row.GetProperty("Recast").GetBoolean();
                    bool triggers = row.GetProperty("TriggersOnUnplayed").GetBoolean();
                    Equal(CardRetainModel.Discarded(before, played, triggers, copy), after, "native discard callback");
                    if (control && !Pending(before) && !played)
                    {
                        bool unable = CardBranchMaskModel.Resolve(before).Traits.Contains("CardTraitUnplayable");
                        if (!copy && unable && triggers) { Require(Pending(after), "Unplayed trigger plus Unplayable must arm Holdover."); unplayable++; }
                        else if (!copy) { Require(!Pending(after), "Ordinary unplayed card unexpectedly armed Holdover."); ordinary++; }
                    }
                    if (copy) { Require(Pending(before) == Pending(after), "Recast copy changed Holdover state."); recast++; }
                    break;
                case "OpeningReset":
                case "TraitReset":
                    Equal(CardRetainModel.Reset(before), after, "native pending reset");
                    Require(!Pending(after), "Native reset left Holdover armed."); reset++;
                    break;
                case "PreDraw":
                    Require(!control, "Pre-draw evidence must come from the live unchanged battle.");
                    CombatContext context = row.GetProperty("BeforeContext").Deserialize<CombatContext>()!;
                    CombatContext actual = row.GetProperty("AfterContext").Deserialize<CombatContext>()!;
                    BattlePlayRules rules = row.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
                    string original = Json(context);
                    TypedDrawResult predicted = CardRetainModel.PreDrawCard(context, before.InstanceId, rules);
                    Require(predicted.Supported, predicted.UnsupportedReason ?? "Holdover pre-draw unsupported.");
                    Equal(predicted.Context!.FindCard(before.InstanceId), after, "native pre-draw card");
                    Equal(BattleTurnChecks.ComparableContext(predicted.Context), BattleTurnChecks.ComparableContext(actual), "native complete pre-draw context/RNG");
                    Require(original == Json(context), "Pre-draw mutated its parent context.");
                    if (Pending(before) && !context.Cards.Hand.Any(card => card.InstanceId == before.InstanceId))
                    {
                        Require(actual.Cards.Hand.Any(card => card.InstanceId == before.InstanceId) && !Pending(after) &&
                            actual.Cards.DrawModifier == context.Cards.DrawModifier - 1 && actual.Cards.Rng.Equals(context.Cards.Rng) &&
                            actual.Statistics!.Value(before.InstanceId, "TimesDrawn", "ThisBattle") ==
                                context.Statistics!.Value(before.InstanceId, "TimesDrawn", "ThisBattle") + 1,
                            "Positive Holdover return must consume a draw slot, increment TimesDrawn once and preserve draw RNG.");
                        returned++;
                    }
                    break;
                default: throw new InvalidOperationException("Unknown Holdover callback.");
            }
            Require(parent == Json(before), "Callback replay mutated its source card.");
        }
        Require(controls == 48 && ordinary >= 2 && unplayable >= 1 && recast == 16 && reset >= 16 && returned > 0,
            "Missing positive/negative unplayed, recast, reset or live return evidence.");
        foreach (var group in rows.Where(row => row.GetProperty("Control").GetBoolean()).GroupBy(row =>
            row.GetProperty("Before").GetProperty("DataId").GetString()))
            Require(group.Count() == 12, "Incomplete detached card control matrix.");
        Require(rows.Where(row => row.GetProperty("Control").GetBoolean()).Select(row =>
            row.GetProperty("Before").GetProperty("DataId").GetString()).Distinct().Count() == 4, "Expected four distinct detached controls.");
        CardInstanceState owner = rows.First(row => !row.GetProperty("Control").GetBoolean()).GetProperty("Before").Deserialize<CardInstanceState>()!;
        Require(owner.Permanent.Upgrades.Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == CardRetainModel.Trait) == true),
            "Live owner must retain the actual campaign merchant Holdover upgrade.");
        Guards(owner, rows.First(row => row.GetProperty("Kind").GetString() == "PreDraw").GetProperty("BeforeContext").Deserialize<CombatContext>()!);
        CardCycleChecks.Native(root);
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"HOLDOVER PASS: 48 isolated native controls, {returned} natural returns, complete context/RNG, immutable whole-battle replay and missing-state guards.");
    }

    private static void Guards(CardInstanceState owner, CombatContext context)
    {
        CardMaskDescriptor descriptor = owner.MaskDescriptor!;
        CardTraitValue held = descriptor.BaseTraits.Single(trait => trait.RuntimeType == CardRetainModel.Trait);
        var absent = new CardTraitValue(held.RuntimeType, held.DeclaredName, held.DefinitionParamInt, held.ParamInt,
            held.Removable, held.StackMode, held.TemporaryReplacement, held.ParameterUpgradeCastEffects,
            held.DefinitionRemovable, held.MagicPowerScale, held.FeedingQuery, held.HandCostTargetType,
            held.MoonPhaseUpgradeState, held.GraftSevered);
        CardInstanceState Missing() => owner.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
            descriptor.BaseTraits.Select(trait => trait == held ? absent : trait).ToArray(), descriptor.AuthoredCastEffects,
            descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements,
            descriptor.SourceTraits, descriptor.KillTriggers));
        Require(CardModifierModel.UnsupportedReason(Missing())?.Contains("Holdover") == true,
            "Missing private Holdover state must remain unsupported rather than defaulting to false.");
        var temporary = owner.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
            descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
            descriptor.TemporaryTraits.Concat(new[] { held }).ToArray(), descriptor.PermanentReplacements,
            descriptor.SourceTraits, descriptor.KillTriggers));
        Require(CardModifierModel.UnsupportedReason(temporary) == null,
            "Captured temporary Holdover state must be accepted.");
        var replaced = owner.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
            descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
            descriptor.TemporaryTraits, [new CardTraitReplacement(CardRetainModel.Trait, CardRetainModel.Trait, CardRetainModel.Trait)],
            descriptor.SourceTraits, descriptor.KillTriggers));
        Require(CardModifierModel.UnsupportedReason(replaced) == null,
            "Captured replacement Holdover state must be accepted.");
        CardUpgradeModifier merchant = owner.Permanent.Upgrades.First(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait =>
            trait.RuntimeType == CardRetainModel.Trait) == true);
        Require(CardExhaustModel.PermanentUpgradeError(owner, merchant) == null,
            "Default in-battle Holdover installation must be accepted.");
    }
    private static bool Pending(CardInstanceState card) => card.MaskDescriptor!.BaseTraits.Single(trait => trait.RuntimeType == CardRetainModel.Trait).HoldoverPending!.Value;
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T predicted, T actual, string label)
    { string? difference = ModelJson.Difference(Json(predicted), Json(actual)); Require(difference == null, label + ": " + difference); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
