using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardModifierProbe
    {
        internal sealed class CacheDecision
        {
            public int CardId { get; set; }
            public int[] Raw { get; set; } = Array.Empty<int>();
            public int[] Captured { get; set; } = Array.Empty<int>();
            public int[] Living { get; set; } = Array.Empty<int>();
        }
        internal static readonly List<CacheDecision> DecisionCaches = new List<CacheDecision>();
        [ThreadStatic] private static int upgradeDepth;
        internal static CardInstanceState[] Capture(CardManager cards, Func<CardState, int> cardId)
            => Capture(cards.GetAllCards(new List<CardState>()), cardId);

        internal static CardInstanceState[] Capture(IEnumerable<CardState> cards, Func<CardState, int> cardId)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            return cards.Select(card =>
            {
                var interactions = new List<string>();
                if (card.IsPurified) interactions.Add("Purified card");
                if (card.IsNonPlayableEnergyCostType()) interactions.Add("Nonplayable card cost type");
                if (card.GetRemoveFromStandByPileOverride(out _)) interactions.Add("Overridden standby return pile");
                CardData data = managers.GetSaveManager().GetAllGameData().FindCardData(card.GetCardDataID())!;
                if (card.IsConsumeRemainingEnergyCostType() != (data.GetCostType() == CardData.CostType.ConsumeRemainingEnergy) ||
                    card.IsNonPlayableEnergyCostType() != (data.GetCostType() == CardData.CostType.NonPlayable)) interactions.Add("Changed card cost type");
                int rawCost = (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card);
                bool captureGraft = UnitGraftProbe.Known(card);
                var state = new CardInstanceState(cardId(card), card.GetCardDataID(),
                    Modifiers(card.GetCardStateModifiers(), CardBranchMaskProbe.Enabled && (data.GetID() == JackInTheBoxProbe.CardId || data.GetEffects().Any(effect =>
                        effect.GetEffectStateName() == "CardEffectUnboxSmallBoxUnitCard")), captureGraft: captureGraft), Modifiers(card.GetTemporaryCardStateModifiers()),
                    card.GetLastPlayedCost(), card.GetLastForgedAmount(), card.GetCurrentScenarioPlayCount(), interactions,
                    Counters(card), DamageScalingProbe.Capture(card), StatusScalingProbe.Capture(card), UnitUpgradeScalingProbe.Capture(card), RoomCapacityProbe.Traits(card),
                    FullBattleTrace.Active?.EquippedUnitId(card) ?? 0, PlayedRoomUnits(card), RawPlayedRoomUnits(card),
                    forgeCountScalingTraits: ForgeCountScalingTraits(card),
                    rawCostOverride: rawCost == data.GetCost() ? null : rawCost, canBeCopied: card.CanBeCopied(),
                    trainHealScalingTraits: TrainHealScalingTraits(card), graftedEquipmentCardId: captureGraft &&
                        card.GetCardStateModifiers().GraftedEquipmentCardState is CardState graft ? cardId(graft) : (int?)null);
                CardPlayRule definition = BattleActionProbe.Definition(data);
                CardPlayRule rule = CardModifierModel.Resolve(definition, state);
                int handCost = rule.Cost;
                if (CardBranchMaskProbe.Enabled && managers.GetCardManager()!.IsCardInHand(card) &&
                    managers.GetCardManager()!.GetHand().SelectMany(owner => owner.GetTraitStates()).Any(trait => trait is CardTraitIncreaseCostOfCardsInHand))
                {
                    handCost = rawCost;
                    foreach (CardState owner in managers.GetCardManager()!.GetHand())
                        handCost = CardHandCostModel.Apply(handCost, rule.CardType,
                            owner.GetTraitStates().Select(CardTraitCompositionProbe.Value));
                    handCost = CardModifierModel.UpgradedStat(handCost, "Cost", true, state.Permanent, state.Temporary);
                }
                RoomManager roomManager = managers.GetRoomManager()!;
                for (int index = 0; index < roomManager.GetNumRooms(); index++)
                {
                    RoomState room = roomManager.GetRoom(index);
                    int healingCostDelta = UnitRoomResourceProbe.HealingCostDelta(room, rule, state);
                    int expectedCost = rule.CardType == "Spell" || healingCostDelta != 0
                        ? (int)Math.Clamp((long)handCost + healingCostDelta + (rule.CardType == "Spell" ? RoomCapacityProbe.SpellCostDelta(room) : 0), 0L, 99L)
                        : handCost;
                    if (card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager(), room) != expectedCost)
                        interactions.Add("Unmodeled room/trait/hand card cost");
                }
                // CardState.GetHealth excludes positive unhealed HP, which is added
                // only to the spawned unit's maximum health during birth.
                // CardState's displayed attack excludes an upgrade's DamageBuff;
                // that field only contributes after CharacterState applies it.
                int cardAttack = rule.SpawnUnit?.BaseAttack ?? 0;
                int cardHealth = rule.SpawnUnit?.Health ?? 0;
                if (definition.SpawnUnit != null && state.Permanent.Upgrades.Concat(state.Temporary.Upgrades).Any(upgrade => upgrade.DamageBuff != 0))
                    cardAttack = CardModifierModel.UpgradedStat(definition.SpawnUnit.BaseAttack, "Damage", true, state.Permanent, state.Temporary);
                if (captureGraft && !card.IsGraftSevered() &&
                    card.TryGetGraftedEquipment(out CardState grafted, managers.GetSaveManager()))
                {
                    EquipmentDefinition gear = BattleActionProbe.Definition(managers.GetSaveManager().GetAllGameData().FindCardData(grafted.GetCardDataID())!).Equipment!;
                    CardModifiers permanent = Modifiers(grafted.GetCardStateModifiers()), temporary = Modifiers(grafted.GetTemporaryCardStateModifiers());
                    int damage = CardModifierModel.UpgradedStat(gear.Upgrades.Sum(upgrade => upgrade.Stats.Damage), "Damage", false, permanent, temporary);
                    int health = CardModifierModel.UpgradedStat(gear.Upgrades.Sum(upgrade => upgrade.Stats.Health), "Health", false, permanent, temporary);
                    cardAttack = CardModifierModel.UpgradedStat(definition.SpawnUnit.BaseAttack + damage, "Damage", true, state.Permanent, state.Temporary);
                    cardHealth = CardModifierModel.UpgradedStat(definition.SpawnUnit.Health + health, "Health", true, state.Permanent, state.Temporary);
                }
                if (data.GetCardType() == CardType.Monster && rule.SpawnUnit != null && (card.GetSize() != rule.SpawnUnit.Size ||
                    card.GetTotalAttackDamage() != cardAttack || (int)Math.Round(card.GetHealth()) != cardHealth))
                    interactions.Add("Unmodeled unit card statistics");
                if (rule.Effect == "Spell")
                {
                    var effects = card.GetEffectStates();
                    if (effects.Count != data.GetEffects().Count) interactions.Add("Changed card effect sequence");
                    else for (int index = 0; index < effects.Count; index++)
                        if ((rule.Effects[index].Type == "Damage" || rule.Effects[index].Type == "Heal") &&
                            effects[index].GetParamInt() != rule.Effects[index].Value)
                            interactions.Add("Unmodeled spell damage/heal modifier");
                }
                return new CardInstanceState(state.InstanceId, state.DataId, state.Permanent, state.Temporary,
                    state.LastPlayedCost, state.LastForgedAmount, state.PlayCount,
                    interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), state.EffectCounters, state.DamageScalingTraits, state.StatusScalingTraits, state.UnitUpgradeScalingTraits, state.CapacityScalingTraits, state.EquippedUnitId, state.PlayedRoomUnitIds, state.RawPlayedRoomUnitIds, CardBranchMaskProbe.Capture(card), CardExhaustProbe.Capture(card, cardId), state.ForgeCountScalingTraits, state.RawCostOverride, card.CanBeCopied(), state.TrainHealScalingTraits, state.GraftedEquipmentCardId);
            }).OrderBy(card => card.InstanceId).ToArray();
        }

        private static int[]? PlayedRoomUnits(CardState card)
        {
            if (!TranscendImpScenario.Enabled && !MultiSummonScenario.Prepared && !TriggeredSummonProbe.Enabled && !HordeMergeScenario.Enabled && !BumpScenario.Enabled && !OriginalMoonCardUpgradesScenario.Enabled && !OriginalHealingCleanseScenario.Enabled && !OriginalOnKillScenario.Enabled && !OriginalOnKillGenerationScenario.Enabled &&
                !SpawnEnchantScenario.Prepared && !OriginalTribouletScenario.Prepared &&
                !(OriginalUnitRelicBatchScenario.Enabled || OriginalTriggerRelicBatchScenario.Enabled || OriginalLifecycleRelicBatchScenario.Enabled)) return null;
            var cached = (IEnumerable<WeakRef<CharacterState>>)AccessTools.Field(typeof(CardState), "charactersInRoomAtTimeOfCardPlay").GetValue(card);
            return cached.Select(reference => reference.Ref).Where(unit => unit != null && card.CharacterInRoomAtTimeOfCardPlay(unit))
                .Select(unit => FullBattleTrace.Active!.UnitId(unit)).OrderBy(id => id).ToArray();
        }

        private static int[]? RawPlayedRoomUnits(CardState card)
        {
            if (!TranscendImpScenario.Enabled && !MultiSummonScenario.Prepared && !TriggeredSummonProbe.Enabled && !HordeMergeScenario.Enabled && !BumpScenario.Enabled && !OriginalMoonCardUpgradesScenario.Enabled && !OriginalHealingCleanseScenario.Enabled && !OriginalOnKillScenario.Enabled && !OriginalOnKillGenerationScenario.Enabled &&
                !SpawnEnchantScenario.Prepared && !OriginalTribouletScenario.Prepared &&
                !(OriginalUnitRelicBatchScenario.Enabled || OriginalTriggerRelicBatchScenario.Enabled || OriginalLifecycleRelicBatchScenario.Enabled)) return null;
            FullBattleTrace trace = FullBattleTrace.Active!;
            var cached = (IEnumerable<WeakRef<CharacterState>>)AccessTools.Field(typeof(CardState), "charactersInRoomAtTimeOfCardPlay").GetValue(card);
            var units = cached.Select(reference => reference.Ref).Where(unit => unit != null).ToArray();
            int[] raw = units.Select(trace.UnitId).OrderBy(id => id).ToArray();
            if (!trace.CanonicalDecisionCapture) return raw;
            int[] living = units.Where(unit => card.CharacterInRoomAtTimeOfCardPlay(unit)).Select(trace.UnitId).OrderBy(id => id).ToArray();
            int[] liveIds = trace.KnownUnits.Where(unit => unit != null && unit.IsAlive && !unit.IsDestroyed)
                .Select(trace.UnitId).OrderBy(id => id).ToArray();
            DecisionCaches.Add(new CacheDecision { CardId = trace.CardId(card), Raw = raw, Captured = living, Living = liveIds });
            return living;
        }

        private static CardEffectCounter[]? Counters(CardState card)
        {
            CardEffectCounter[] counters = card.GetEffectStates().Select((effect, index) => new { effect, index })
                .Where(item => item.effect.GetCardEffect() is CardEffectDiscardHand).Select(item => new CardEffectCounter(
                    item.index, "CardEffectDiscardHand", ((CardEffectDiscardHand)item.effect.GetCardEffect()).GetNumCardsConsumed())).ToArray();
            return counters.Length == 0 ? null : counters;
        }

        internal static ScalingForgeCountTrait[] ForgeCountScalingTraits(CardState card) => card.GetTraitStates()
            .OfType<CardTraitScalingAddForged>().Select(trait => new ScalingForgeCountTrait(
                DamageScalingProbe.Query(trait.StatValueData, (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card),
                    card.IsConsumeRemainingEnergyCostType()), trait.GetParamInt())).ToArray();

        internal static ScalingTrainHealTrait[]? TrainHealScalingTraits(CardState card)
        {
            ScalingTrainHealTrait[] traits = card.GetTraitStates().OfType<CardTraitScalingHealTrain>()
                .Select(trait => new ScalingTrainHealTrait(DamageScalingProbe.Query(trait.StatValueData,
                    (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card),
                    card.IsConsumeRemainingEnergyCostType()), trait.GetParamInt(), trait.GetParamFloat())).ToArray();
            return traits.Length == 0 ? null : traits;
        }

        internal static CardModifiers Modifiers(CardStateModifiers modifiers, bool captureEmptyBox = false, int boxDepth = 0, bool captureGraft = false)
        {
            int Field(string name) => (int)AccessTools.Field(typeof(CardStateModifiers), name).GetValue(modifiers);
            var interactions = new List<string>();
            MergedEquipmentState? merged = null;
            if (modifiers.GetMergedCardStateIDs().Count > 0)
            {
                if (!CardBranchMaskProbe.Enabled || boxDepth > 0) interactions.Add("Unresolved or nested merged cards");
                else
                {
                    SaveManager save = AllGameManagers.Instance!.GetSaveManager();
                    merged = new MergedEquipmentState(modifiers.GetMergedCardStateIDs().Select(id =>
                    {
                        CardState source = save.GetMergedCardState(id);
                        if (source.GetCardStateModifiers().HasMergedCards()) interactions.Add("Nested merged equipment");
                        return new MergedEquipmentSource(FullBattleTrace.Active!.CardId(source), BattleActionProbe.Definition(save.GetAllGameData().FindCardData(source.GetCardDataID())!));
                    }).ToArray());
                }
            }
            if (!captureGraft && (modifiers.GraftedEquipmentCardState != null || Field("graftedEquipmentCardStateId") != -1)) interactions.Add("Grafted equipment");
            int savedBoxCount = ((ICollection)AccessTools.Field(typeof(CardStateModifiers), "boxedCardStateIds").GetValue(modifiers)).Count;
            BoxedCardState[]? contents = null;
            if (modifiers.BoxedCardStates.Count > 0 || savedBoxCount > 0 || captureEmptyBox)
            {
                if (!CardBranchMaskProbe.Enabled || boxDepth > 0 || savedBoxCount != modifiers.BoxedCardStates.Count)
                    interactions.Add("Unresolved or nested boxed cards");
                else
                {
                    AllGameData all = AllGameManagers.Instance!.GetAllGameData();
                    if (modifiers.BoxedCardStates.Any(card => all.FindCardData(card.GetCardDataID()) == null))
                        interactions.Add("Missing boxed card definition");
                    else contents = modifiers.BoxedCardStates.Select(card => new BoxedCardState(
                        CardGenerationProbe.Creation(all.FindCardData(card.GetCardDataID())!),
                        Modifiers(card.GetCardStateModifiers(), boxDepth: boxDepth + 1),
                        Modifiers(card.GetTemporaryCardStateModifiers(), boxDepth: boxDepth + 1))).ToArray();
                }
            }
            if (modifiers.GetTemporaryTraits().Any(trait => !CardBranchMaskProbe.Enabled || trait is not (CardTraitFreeze or CardTraitSelfPurge or CardTraitRetain or CardTraitGraftedEquipment)) ||
                modifiers.GetTraitReplacements().Count > 0 && !CardBranchMaskProbe.Enabled)
                interactions.Add("Temporary/replaced card traits");
            return new CardModifiers(new CardStatModifier(Field("additionalDamage"), Field("additionalMaxHP"), Field("additionalCost"),
                Field("additionalHeal"), Field("additionalSize"), Field("additionalXCost"), Field("additionalEquipmentLimit"),
                Field("additionalUpgradeSlotCount")), modifiers.GetCardUpgrades().Select(upgrade => Upgrade(upgrade, false, true)).ToArray(),
                modifiers.GetPersistentHP(), interactions, contents, merged);
        }

        internal static CardUpgradeModifier Upgrade(CardUpgradeState upgrade) => Upgrade(upgrade, false);
        internal static CardUpgradeModifier Upgrade(CardUpgradeState upgrade, bool rejectFilters, bool filtersAlreadyApplied = false,
            bool roomAttachmentUnitUpgrade = false)
        {
            upgradeDepth++;
            try { return UpgradeCore(upgrade, rejectFilters, filtersAlreadyApplied, roomAttachmentUnitUpgrade); }
            finally { upgradeDepth--; }
        }
        private static CardUpgradeModifier UpgradeCore(CardUpgradeState upgrade, bool rejectFilters, bool filtersAlreadyApplied,
            bool roomAttachmentUnitUpgrade)
        {
            var interactions = new List<string>();
            if (rejectFilters && upgrade.GetFilters().Count > 0) interactions.Add("Filtered bonus-draw upgrade");
            CardUpgradeData? source = upgrade.GetSourceCardUpgradeData();
            bool refresh = source != null && !source.GetUpgradeWillBeScaledByNonMagicPowerTrait();
            if (!roomAttachmentUnitUpgrade && upgrade.GetRoomAbilityUpgrade() != null) interactions.Add("Upgrade room ability");
            CardLifecycleUpgrade lifecycle = CardUpgradeLifecycleProbe.Definition(upgrade);
            bool discardCleanup = CardBranchMaskProbe.Enabled && upgrade.GetAssetName() is
                "CopyOnPlayTraitUpgradeNoEphemeral" or "RemoveEphemeral" or "StardustInvocation_Upgrade_NoEphemeral";
            bool supportedTraitLifecycle = (lifecycle.AddedTraits.Count > 0 || lifecycle.RemovedRuntimeTypes.Count > 0) && lifecycle.AddedTraits.All(trait =>
                trait.RuntimeType == trait.DeclaredName &&
                (trait.RuntimeType is "CardTraitIgnoreArmor" or "CardTraitSelfPurge" or "CardTraitExhaustState" or "CardTraitEphemeral" ||
                    trait.RuntimeType == CardCorruptStateModel.Trait && CardBranchMaskProbe.Enabled ||
                    trait.RuntimeType == "CardTraitDistinct" && trait.StackMode == 0 && CardBranchMaskProbe.Enabled ||
                    trait.RuntimeType == "CardTraitDrawInDeploymentPhase" ||
                    trait.RuntimeType == "CardTraitIntrinsicState" && CardBranchMaskProbe.Enabled ||
                    trait.RuntimeType == CardRetainModel.Trait && trait.StackMode == 0 && CardBranchMaskProbe.Enabled ||
                    trait.RuntimeType == "CardTraitPermafrost" && upgrade.GetCardUpgradeDataId() == RelicExpensiveCardUpgradeModel.Effect ||
                    trait.RuntimeType == "CardTraitStrongerMagicPower" && CardBranchMaskProbe.Enabled)) &&
                lifecycle.RemovedRuntimeTypes.All(type => type is "CardTraitIgnoreArmor" or "CardTraitExhaustState" ||
                    discardCleanup && type is "CardTraitEphemeral" or "CardTraitCopyOnPlay");
            if ((upgrade.GetTraitDataUpgrades().Count > 0 || upgrade.GetRemoveTraitUpgrades().Count > 0) && !supportedTraitLifecycle)
                interactions.Add("Upgrade traits");
            if (upgrade.GetCardTriggerUpgrades().Count > 0)
                interactions.AddRange(lifecycle.Triggers.SelectMany(trigger => trigger.ExternalInteractions));
            CombatTrigger[]? triggers = null;
            if (upgrade.GetTriggerUpgrades().Count > 0)
            {
                if (upgradeDepth > 16) interactions.Add("Recursive upgrade trigger definitions");
                else triggers = upgrade.GetTriggerUpgrades().Select(trigger => EnemySpawningProbe.TriggerDefinition(trigger, interactions)).ToArray();
            }
            if (!roomAttachmentUnitUpgrade && upgrade.GetRoomModifierUpgrades().Count > 0) interactions.Add("Upgrade room modifiers");
            if (!filtersAlreadyApplied && upgrade.GetFilters().Count > 0) interactions.Add("Upgrade card filters");
            if (upgrade.GetUpgradesToRemove().Count > 0 && !discardCleanup) interactions.Add("Upgrade replacements");
            bool boilerplateArmor = CardBranchMaskProbe.Enabled && upgrade.GetCardUpgradeDataId() == "29ec3d1a-190f-4aef-a9f3-3ce696c7fd09" &&
                upgrade.GetStatusEffectUpgrades().Count == 1 && upgrade.GetStatusEffectUpgrades()[0].statusId == "armor" &&
                upgrade.GetStatusEffectUpgrades()[0].fromPermanentUpgrade;
            if (!boilerplateArmor && upgrade.GetStatusEffectUpgrades().Any(status => status.fromPermanentUpgrade))
                interactions.Add("Separate permanent starting-status application group");
            CardUpgradeData? copyDefinition = AllGameManagers.Instance?.GetSaveManager()?.GetAllGameData()
                .FindCardUpgradeData(upgrade.GetCardUpgradeDataId());
            var captured = new CardUpgradeModifier(ConduitProbe.Normalize(upgrade.GetCardUpgradeDataId()), upgrade.GetAssetName(),
                new CardStatModifier(upgrade.GetAttackDamage(), upgrade.GetAdditionalHP(), -upgrade.GetCostReduction(),
                    upgrade.GetAdditionalHeal(), upgrade.GetAdditionalSize(), upgrade.GetXCostReduction(),
                    upgrade.GetAdditionalEquipmentLimit(), upgrade.GetAdditionalUpgradeSlotCount()),
                upgrade.GetStatusEffectUpgrades().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
                upgrade.GetRemoveOnDiscard(), upgrade.IsUnique(), upgrade.GetExcludeFromClones(), upgrade.GetAdditionalUnhealedHP(),
                upgrade.GetAttackDamageBuff(), interactions, upgrade.GetRestrictSizeToRoomCapacity(), source?.GetMagicPowerTraitScalingOnly() == true,
                refresh && source!.GetBonusDamage() > 0 ? (int?)source.GetBonusDamage() : null,
                refresh && source!.GetBonusHeal() > 0 ? (int?)source.GetBonusHeal() : null, triggerUpgrades: triggers,
                abilityUpgrade: upgrade.GetUnitAbilityUpgrade() == null ? null : AbilityLifecycleProbe.Change(upgrade.GetUnitAbilityUpgrade()),
                doNotReplaceExistingAbility: upgrade.GetDoNotReplaceExistingUnitAbility(), maskMetadata: CardBranchMaskProbe.Upgrade(upgrade),
                copyRemoveOnDiscard: copyDefinition == null ? null : false, copyUnique: copyDefinition?.IsUnique(),
                copyExcludeFromClones: copyDefinition?.GetExcludeFromClones(), lifecycle: lifecycle,
                sharedReferenceId: TypedDrawProbe.SharedReferenceId(upgrade));
            return TroopAuraProbe.Normalize(upgrade, captured);
        }
    }
}
