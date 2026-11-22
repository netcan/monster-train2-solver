using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonCardUpgradeChecks
{
    internal static void NativeCopiedUpgrade(FixtureValue fixture)
    {
        const string reason = "Duplicate moon upgrades require reference-identity calibration.";
        var rows = NativeBranchObservationChecks.Replay(fixture, reason,
            expectedGameModule: new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d"));
        var boundary = rows[^1];
        var before = boundary.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = boundary.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var input = before.Spawn.Train.Context!;
        var output = after.Spawn.Train.Context!;
        var source = input.FindCard(23)!;
        var copied = output.FindCard(input.NextCardId)!;
        var active = copied.MaskDescriptor!.BaseTraits.Single(trait => trait.MoonPhaseUpgradeState?.Active == true);
        Require(rows.Length == 47 && before.Spawn.Turn == 8 && after.Spawn.Turn == 8 && input.QueryFrame?.MoonPhase == 2 &&
            boundary.GetProperty("Action").Deserialize<PlayCardAction>() is { CardInstanceId: 29, ActivatorUnitId: 7 } &&
            copied.DataId == source.DataId && output.Cards.Hand.Any(token => token.InstanceId == copied.InstanceId) &&
            copied.Permanent.Upgrades is [var installed] && installed.AssetKey == "GravityAscend_Upgrade" &&
            copied.MaskDescriptor.AuthoredCastEffects.SequenceEqual(new[] { "CardEffectBump" }) &&
            active.MoonPhaseUpgradeState is { Active: true, Cached: true } state && Json(state.Upgrade) == Json(installed) &&
            Json(source) == Json(output.FindCard(source.InstanceId)),
            "The original ability must copy one installed full-Moon upgrade, activate its cache and preserve the source.");

        var descriptor = copied.MaskDescriptor;
        var coldTraits = descriptor.BaseTraits.Select(trait => ReferenceEquals(trait, active)
            ? trait.WithMoonUpgrade(new CardMoonUpgradeState(false, false, active.MoonPhaseUpgradeState!.Upgrade)) : trait).ToArray();
        var cold = copied.WithMaskDescriptor(descriptor.WithBaseTraits(coldTraits));
        Require(Json(CardMoonUpgradeModel.Update(cold, 2).Card) == Json(copied), "Adopting the installed native upgrade changed the copied card.");
        foreach (string control in new[] { "SecondEntry", "DifferentPayload", "MissingCast", "SecondCast" })
        {
            var node = JsonSerializer.SerializeToNode(cold, ModelJson.Options)!;
            var upgrades = node["Permanent"]!["Upgrades"]!.AsArray();
            var mask = node["MaskDescriptor"]!["AuthoredCastEffects"]!.AsArray();
            switch (control)
            {
                case "SecondEntry": upgrades.Add(upgrades[0]!.DeepClone()); break;
                case "DifferentPayload": upgrades[0]!["Lifecycle"]!["Triggers"]![0]!["Actions"]![0]!["Value"] = -1; break;
                case "MissingCast": mask.Clear(); break;
                case "SecondCast": mask.Add("CardEffectBump"); break;
            }
            var rejected = CardMoonUpgradeModel.Update(node.Deserialize<CardInstanceState>(ModelJson.Options)!, 2);
            Require(!rejected.Supported && rejected.UnsupportedReason == reason, "Unverified Moon upgrade passed the duplicate guard: " + control);
        }
        Console.WriteLine("MOON-COPIED-UPGRADE PASS: 47 original inputs, full successor/RNG, one permanent upgrade and one cast, four duplicate guards.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("DecisionUnchanged").GetBoolean() &&
            root.GetProperty("FrameBefore").GetInt32() == root.GetProperty("FrameAfter").GetInt32() &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Native moon calibration changed live state/frame/RNG.");
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 135 && rows.Select(row => row.GetProperty("CardDataId").GetString()).Distinct().Count() == 5 &&
            rows.Select(row => row.GetProperty("Variant").GetString()).Distinct().Count() == 3, "Incomplete original moon upgrade controls.");
        foreach (FixtureValue row in rows)
        {
            CardInstanceState source = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            CardInstanceState actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            string parent = Json(source), operation = row.GetProperty("Operation").GetString()!;
            int phase = row.GetProperty("Phase").GetInt32(); bool preview = row.GetProperty("Preview").GetBoolean();
            CardMoonUpgradeResult result = CardMoonUpgradeModel.Update(source, phase, reset: operation == "Discard", preview: preview);
            string? difference = result.Supported ? ModelJson.Difference(Json(result.Card), Json(actual)) : result.UnsupportedReason;
            Require(difference == null, source.DataId + "/" + row.GetProperty("Variant").GetString() + "/" + operation + "/" + phase + ": " + difference);
            Require(Json(source) == parent, "Moon upgrade changed its immutable parent.");
            if (preview) Require(Json(actual) == parent, "Preview resource callback must be a native no-op.");
            if (operation == "Discard") Require(actual.MaskDescriptor!.BaseTraits.Where(trait => trait.RuntimeType == CardMoonUpgradeModel.Trait)
                .All(trait => trait.MoonPhaseUpgradeState is { Active: false, Cached: true }), "Discard must reset every cached moon trait.");
            Parallel.For(0, 16, _ =>
            {
                CardMoonUpgradeResult branch = CardMoonUpgradeModel.Update(source, phase, reset: operation == "Discard", preview: preview);
                Require(branch.Supported && Json(branch.Card) == Json(result.Card) && Json(source) == parent, "Parallel moon upgrade branches differ or share mutable state.");
            });
        }
        CardInstanceState guard = rows[0].GetProperty("Before").Deserialize<CardInstanceState>()!;
        CardMaskDescriptor descriptor = guard.MaskDescriptor!;
        CardInstanceState missing = guard.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost, descriptor.BaseTraits,
            descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements));
        Require(!CardMoonUpgradeModel.Update(missing, 2).Supported, "Missing moon source metadata must remain unsupported.");
        Require(!CardMoonUpgradeModel.Update(guard, 0).Supported, "Unknown moon phase must remain unsupported.");
        CardTraitValue[] withoutState = descriptor.BaseTraits.Select(trait => new CardTraitValue(trait.RuntimeType, trait.DeclaredName,
            trait.DefinitionParamInt, trait.ParamInt, trait.Removable, trait.StackMode, trait.TemporaryReplacement,
            trait.ParameterUpgradeCastEffects, trait.DefinitionRemovable, trait.MagicPowerScale, trait.FeedingQuery, trait.HandCostTargetType)).ToArray();
        Require(!CardMoonUpgradeModel.Update(guard.WithMaskDescriptor(descriptor.WithBaseTraits(withoutState)), 2).Supported,
            "Unknown native moon upgrade cache/active state must remain unsupported.");
        CardSourceTrait sourceTrait = descriptor.SourceTraits!.Single(trait => trait.Type == CardMoonUpgradeModel.Trait);
        CardSourceTrait changed = new(sourceTrait.Type, sourceTrait.Int, sourceTrait.Int2, 1, sourceTrait.Bool, sourceTrait.Str, sourceTrait.Float,
            sourceTrait.Scaling, sourceTrait.TrackedValue, sourceTrait.EntryDuration, moonPhaseUpgrade: sourceTrait.MoonPhaseUpgrade);
        CardMaskDescriptor changedDescriptor = new(descriptor.Definition, descriptor.BaseCost, descriptor.BaseTraits, descriptor.AuthoredCastEffects,
            descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, [changed]);
        Require(!CardMoonUpgradeModel.Update(guard.WithMaskDescriptor(changedDescriptor), 2).Supported, "Changed source moon parameters must remain unsupported.");
        CheckHoardRewardIndependence(guard);
        Console.WriteLine("MOON-CARD-UPGRADE PASS: five original spells, seven upgrade definitions, 135 complete native card states, phase reversal/repeat, preview no-op, draw/discard reset, magic-power rescaling, existing-trait protection and 16 immutable branches. Whole-battle callbacks require separate integration witnesses.");
    }

    private static void CheckHoardRewardIndependence(CardInstanceState card)
    {
        const string assetKey = "ModifyDragonsHoardBattleEnd";
        const string dataId = "26c9bc93-f5a6-439d-8239-14c22cbeb02e";
        const string effect = "RelicEffectModifyDragonsHoardEndOfBattle";
        var exact = new CombatRelicState(dataId, assetKey, [effect], isCovenant: false, disallowedInPlacementPhase: false);
        UnityRng rng = UnityRng.Seed(17);
        var root = new CombatContext(new CardCycleState([new(card.InstanceId, card.DataId)], [], [], rng, 0, []),
            rng, 20, card.InstanceId + 1, 10, cardInstances: [card], queryFrame: new(moonPhase: 2, selectedRoomIndex: 0));
        CombatContext expected = CardMoonUpgradeModel.RefreshHand(root, out string? baselineError);
        CombatContext withReward = root.WithRelics([exact]);
        string parent = Json(withReward);
        CombatContext actual = CardMoonUpgradeModel.RefreshHand(withReward, out string? error);
        Require(baselineError == null && error == null && Json(actual) == Json(expected.WithRelics([exact])) && Json(withReward) == parent,
            "The original Hoard reward changed moon hand refresh or its parent.");

        var unknown = new[]
        {
            new CombatRelicState("unknown", assetKey, [effect], isCovenant: false, disallowedInPlacementPhase: false),
            new CombatRelicState(dataId, "unknown", [effect], isCovenant: false, disallowedInPlacementPhase: false),
            new CombatRelicState(dataId, assetKey, [effect, RelicModel.ModifyCardHealingPower], isCovenant: false, disallowedInPlacementPhase: false),
            new CombatRelicState(dataId, assetKey, [effect], isCovenant: false, disallowedInPlacementPhase: true)
        };
        foreach (CombatRelicState owner in unknown)
        {
            CardMoonUpgradeModel.RefreshHand(root.WithRelics([owner]), out error);
            Require(error == "Uncalibrated moon hand-refresh relic: " + owner.AssetKey + ".",
                "An uncalibrated Hoard reward signature passed moon hand refresh.");
        }
    }

    private static string Json(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
