using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBalatroEventYorickChecks
{
    private const string OwnerId = "2bd14ebc-1589-4652-a002-54013f0eace4";
    private const string OwnerKey = "BalatroEventYorick";
    private const string Effect = "RelicEffectAddGoldOnExhaust";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 194 &&
            root.GetProperty("ModifierScenario").GetString() == "original-balatro-event-yorick" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("OriginalBalatroEventYorickPrepared").GetBoolean() &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "BalatroEventYorick native battle failed acceptance.");

        FixtureValue[] tests = root.GetProperty("RelicCardPlayGoldOnExhaustTests").EnumerateArray().ToArray();
        Require(tests.Length >= 3 && tests.All(test => test.GetProperty("CaptureError").ValueKind == FixtureKind.Null),
            "Native gold-on-exhaust Test calls are missing or failed capture.");
        foreach (FixtureValue test in tests)
        {
            int playCount = test.GetProperty("PlayCount").GetInt32();
            int modifier = test.GetProperty("PlayCountModifier").GetInt32();
            bool hasExhaust = test.GetProperty("ExhaustTraitCount").GetInt32() > 0;
            bool expected = hasExhaust && unchecked(playCount + 1) >= unchecked(1 + modifier);
            Require(test.GetProperty("Result").GetBoolean() == expected,
                "Native TestEffectOnCardPlayed differs from the exhaust-trait look-ahead rule.");
        }
        Require(tests.Single(test => test.GetProperty("Label").GetString() == "ManualNoExhaust").GetProperty("Result").GetBoolean() == false &&
            tests.Single(test => test.GetProperty("Label").GetString() == "ManualExhaust").GetProperty("Result").GetBoolean() &&
            tests.Any(test => test.GetProperty("Label").GetString() == "Natural" && test.GetProperty("Result").GetBoolean()),
            "Native positive/negative controls or the natural card-play Test callback are absent.");

        FixtureValue[] callbacks = root.GetProperty("RelicCardPlayGoldOnExhaust").EnumerateArray().ToArray();
        int carrierId = root.GetProperty("OriginalBalatroEventYorickCarrierId").GetInt32();
        FixtureValue[] carrierCallbacks = callbacks.Where(callback => callback.GetProperty("CardInstanceId").GetInt32() == carrierId).ToArray();
        Require(callbacks.Length > 0 && carrierCallbacks.Length == 1,
            "The isolated Yorick fixture must capture one carrier callback and any additional natural Consume callbacks.");
        foreach (FixtureValue callback in callbacks)
        {
            Require(callback.GetProperty("Completed").GetBoolean() &&
                callback.GetProperty("CaptureError").ValueKind == FixtureKind.Null &&
                callback.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                callback.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "A natural BalatroEventYorick callback did not complete cleanly.");
            TrainCombatState callbackBefore = callback.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState callbackActual = callback.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatContext callbackContext = callbackBefore.Context ?? throw new InvalidOperationException("Callback before-state has no context.");
            int callbackRelicIndex = callbackContext.Relics!.Select((relic, index) => (relic, index))
                .Single(item => item.relic.DataId == OwnerId).index;
            CombatRelicState callbackOwner = callbackContext.Relics[callbackRelicIndex];
            RelicResourceEffect callbackRule = callbackOwner.ResourceEffects!.Single(effect => effect.Type == Effect);
            TrainCombatResult callbackPrediction = RelicCardPlayGoldOnExhaustModel.ApplyOne(callbackBefore,
                callback.GetProperty("CardInstanceId").GetInt32(), callbackRelicIndex,
                callback.GetProperty("EffectIndex").GetInt32(), placement: false, preview: false);
            Require(callbackOwner.AssetKey == OwnerKey && callbackRule.Amount == callback.GetProperty("Amount").GetInt32() &&
                callbackRule.EffectIndex == callback.GetProperty("EffectIndex").GetInt32() && callbackPrediction.Supported,
                "Natural callback owner, effect order, or model support changed.");
            Equal(callbackPrediction.State!, callbackActual, "natural gold callback for card " + callback.GetProperty("CardInstanceId").GetInt32());
        }
        FixtureValue row = carrierCallbacks[0];
        Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("CardInstanceId").GetInt32() ==
            carrierId &&
            row.GetProperty("CaptureError").ValueKind == FixtureKind.Null &&
            row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
            row.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Natural BalatroEventYorick callback did not complete cleanly: " +
            (row.GetProperty("Difference").ValueKind == FixtureKind.String ? row.GetProperty("Difference").GetString() : "no callback difference payload"));

        TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        CombatContext context = before.Context ?? throw new InvalidOperationException("Callback before-state has no context.");
        CombatContext after = actual.Context ?? throw new InvalidOperationException("Callback after-state has no context.");
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == OwnerId);
        RelicResourceEffect rule = owner.ResourceEffects!.Single(effect => effect.Type == Effect);
        int cardId = row.GetProperty("CardInstanceId").GetInt32();
        CardInstanceState played = context.FindCard(cardId) ?? throw new InvalidOperationException("Callback card is not in the owned-card list.");
        Require(owner.AssetKey == OwnerKey && owner.IsCovenant == false && owner.EffectTypes.SequenceEqual([Effect]) &&
            rule.EffectIndex == row.GetProperty("EffectIndex").GetInt32() && rule.Amount == rule.ParamInt &&
            rule.Amount == row.GetProperty("Amount").GetInt32() && rule.Conditions.Count == 0 &&
            !rule.CanApplyInPreviewMode && rule.RequiredMutatorId == null && CardExhaustModel.HasConsume(played) &&
            CardExhaustModel.ShouldExhaustOnPlayedDiscard(played),
            "Original owner payload or native exhaust look-ahead input changed.");

        int relicIndex = context.Relics.Select((relic, index) => (relic, index))
            .Single(item => item.relic.DataId == OwnerId).index;
        TrainCombatResult predicted = RelicCardPlayGoldOnExhaustModel.ApplyOne(before, cardId,
            relicIndex, rule.EffectIndex, placement: false, preview: false);
        Require(predicted.Supported, "Supported natural callback failed in the focused model: " + predicted.UnsupportedReason);
        Equal(predicted.State!, actual, "natural gold callback");
        int adjusted = GoldRewardModel.Adjust(rule.Amount, isReward: true, roundIncrement: 1,
            multiplier: context.Resources?.GoldRewardMultiplier ?? 1f);
        Require(after.Gold == Math.Max(0, unchecked(context.Gold + adjusted)),
            "Native AdjustGold differs from adjusted reward amount or 1-gold rounding.");

        RelicCardPlayMagicPowerResult integrated = RelicCardPlayMagicPowerModel.Apply(context, cardId,
            currentTurn: 1, trainState: before, placement: false, preview: false);
        Require(integrated.Supported && integrated.TrainState != null,
            "Central card-play relic pipeline rejected Yorick or omitted the train state: " + integrated.UnsupportedReason);
        Equal(integrated.TrainState!, actual, "central card-play pipeline");

        CombatContext unchangedParent = JsonSerializer.Deserialize<CombatContext>(
            JsonSerializer.Serialize(context, ModelJson.Options), ModelJson.Options)!;
        CardInstanceState ordinary = context.CardInstances!.First(card => card.InstanceId != cardId && !CardExhaustModel.HasConsume(card));
        TrainCombatResult noExhaust = RelicCardPlayGoldOnExhaustModel.ApplyOne(before, ordinary.InstanceId,
            relicIndex, rule.EffectIndex);
        Require(noExhaust.Supported && ReferenceEquals(noExhaust.State, before) &&
            noExhaust.State!.Context!.Gold == context.Gold,
            "A non-exhausting card incorrectly gained Yorick gold.");

        TrainCombatState belowThreshold = WithExhaustInput(before, played, playCount: 0, modifier: 1);
        TrainCombatResult gated = RelicCardPlayGoldOnExhaustModel.ApplyOne(belowThreshold, cardId,
            relicIndex, rule.EffectIndex);
        Require(gated.Supported && ReferenceEquals(gated.State, belowThreshold) &&
            gated.State!.Context!.Gold == context.Gold,
            "A card below its modified exhaust play-count threshold gained gold.");
        TrainCombatState atThreshold = WithExhaustInput(before, played, playCount: 1, modifier: 1);
        TrainCombatResult threshold = RelicCardPlayGoldOnExhaustModel.ApplyOne(atThreshold, cardId,
            relicIndex, rule.EffectIndex);
        Require(threshold.Supported && threshold.State!.Context!.Gold == unchecked(context.Gold + adjusted),
            "The exact extra-play exhaust threshold failed to grant gold.");

        BattleResourceState resources = context.Resources!;
        var multipliedResources = new BattleResourceState(resources.BaseEnergy, resources.DeploymentExtraEnergy,
            resources.BaseDraw, resources.MaxEnergy, resources.MaxHandSize, resources.PyreDataId,
            resources.PyreBaseAttack, resources.BasePyreMaxHealth, resources.PyrePersistentDamage,
            resources.PyreModifiedBaseAttack, resources.UpgradeAttack, resources.UpgradeHealth,
            resources.MutatorIds, 1.73f, resources.BaseFloorCapacity, resources.DragonsHoardCap,
            resources.TowerHealingDisabled, resources.VisibleDeckCount);
        CombatContext multipliedContext = context.WithResources(multipliedResources);
        TrainCombatState multiplied = CardSpellModel.WithContext(before, multipliedContext);
        TrainCombatResult multipliedResult = RelicCardPlayGoldOnExhaustModel.ApplyOne(multiplied, cardId,
            relicIndex, rule.EffectIndex);
        int multipliedAmount = GoldRewardModel.Adjust(rule.Amount, isReward: true, roundIncrement: 1, multiplier: 1.73f);
        Require(multipliedResult.Supported && multipliedResult.State!.Context!.Gold ==
            Math.Max(0, unchecked(context.Gold + multipliedAmount)) &&
            GoldRewardModel.Adjust(rule.Amount, isReward: true, multiplier: 1.73f) != multipliedAmount,
            "Gold-on-exhaust did not preserve the live reward multiplier with roundIncrement=1.");
        TrainCombatResult preview = RelicCardPlayGoldOnExhaustModel.ApplyOne(before, cardId,
            relicIndex, rule.EffectIndex, preview: true);
        Require(preview.Supported && ReferenceEquals(preview.State, before) && context.Gold == before.Context!.Gold,
            "A preview callback ran an effect that is not enabled in native preview mode.");
        Require(Serialize(context) == Serialize(unchangedParent) && context.Gold == before.Context.Gold,
            "Yorick controls mutated the captured source branch.");

        BattleActionChecks.Native(root);
        Require(root.GetProperty("Mismatches").GetInt32() == 0,
            "BalatroEventYorick battle reported a native/model mismatch outside the callback record.");
        Console.WriteLine($"ORIGINAL-BALATRO-EVENT-YORICK-NATIVE PASS: {tests.Length} native Test calls, {callbacks.Length} natural Consume callbacks, exact gold adjustment, ordinary-card and modified-threshold gates, preview refusal, multiplier at 1-gold precision, central pipeline and full-battle policy replay.");
    }

    private static TrainCombatState WithExhaustInput(TrainCombatState source, CardInstanceState card,
        int playCount, int modifier)
    {
        CardExhaustState exhaust = card.ExhaustState!;
        var changed = new CardInstanceState(card.InstanceId, card.DataId, card.Permanent, card.Temporary,
            card.LastPlayedCost, card.LastForgedAmount, playCount, card.ExternalInteractions, card.EffectCounters,
            card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits,
            card.CapacityScalingTraits, card.EquippedUnitId, card.PlayedRoomUnitIds, card.RawPlayedRoomUnitIds,
            card.MaskDescriptor, new CardExhaustState(modifier, exhaust.BaseTraits, exhaust.UpgradeTrait),
            card.ForgeCountScalingTraits, card.RawCostOverride, card.CanBeCopied, card.TrainHealScalingTraits);
        return CardSpellModel.WithContext(source, source.Context!.WithCard(changed));
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    private static void Equal(TrainCombatState predicted, TrainCombatState actual, string name)
    {
        string left = Serialize(predicted), right = Serialize(actual);
        Require(ModelJson.Difference(left, right) == null,
            "BalatroEventYorick " + name + " differs: " + ModelJson.Difference(left, right));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
