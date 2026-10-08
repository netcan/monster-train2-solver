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
                if ((int)AccessTools.Field(typeof(CardState), "cost").GetValue(card) != data.GetCost()) interactions.Add("Changed base card cost");
                var state = new CardInstanceState(cardId(card), card.GetCardDataID(),
                    Modifiers(card.GetCardStateModifiers()), Modifiers(card.GetTemporaryCardStateModifiers()),
                    card.GetLastPlayedCost(), card.GetLastForgedAmount(), card.GetCurrentScenarioPlayCount(), interactions,
                    Counters(card), DamageScalingProbe.Capture(card), StatusScalingProbe.Capture(card), UnitUpgradeScalingProbe.Capture(card), RoomCapacityProbe.Traits(card),
                    FullBattleTrace.Active?.EquippedUnitId(card) ?? 0, PlayedRoomUnits(card), RawPlayedRoomUnits(card));
                CardPlayRule rule = CardModifierModel.Resolve(BattleActionProbe.Definition(data), state);
                for (int index = 0; index < managers.GetRoomManager()!.GetNumRooms(); index++)
                    if (card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager(),
                        managers.GetRoomManager()!.GetRoom(index)) != rule.Cost) interactions.Add("Unmodeled room/trait/hand card cost");
                if (rule.SpawnUnit != null && (card.GetSize() != rule.SpawnUnit.Size ||
                    card.GetTotalAttackDamage() != rule.SpawnUnit.BaseAttack || (int)Math.Round(card.GetHealth()) != rule.SpawnUnit.MaxHealth))
                    interactions.Add("Unmodeled unit card statistics");
                if (rule.Effect == "Spell")
                {
                    var effects = card.GetEffectStates();
                    if (effects.Count != rule.Effects.Count) interactions.Add("Changed card effect sequence");
                    else for (int index = 0; index < effects.Count; index++)
                        if ((rule.Effects[index].Type == "Damage" || rule.Effects[index].Type == "Heal") &&
                            effects[index].GetParamInt() != rule.Effects[index].Value)
                            interactions.Add("Unmodeled spell damage/heal modifier");
                }
                return new CardInstanceState(state.InstanceId, state.DataId, state.Permanent, state.Temporary,
                    state.LastPlayedCost, state.LastForgedAmount, state.PlayCount,
                    interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), state.EffectCounters, state.DamageScalingTraits, state.StatusScalingTraits, state.UnitUpgradeScalingTraits, state.CapacityScalingTraits, state.EquippedUnitId, state.PlayedRoomUnitIds, state.RawPlayedRoomUnitIds);
            }).OrderBy(card => card.InstanceId).ToArray();
        }

        private static int[]? PlayedRoomUnits(CardState card)
        {
            if (!MultiSummonScenario.Prepared && !TriggeredSummonProbe.Enabled) return null;
            var cached = (IEnumerable<WeakRef<CharacterState>>)AccessTools.Field(typeof(CardState), "charactersInRoomAtTimeOfCardPlay").GetValue(card);
            return cached.Select(reference => reference.Ref).Where(unit => unit != null && card.CharacterInRoomAtTimeOfCardPlay(unit))
                .Select(unit => FullBattleTrace.Active!.UnitId(unit)).OrderBy(id => id).ToArray();
        }

        private static int[]? RawPlayedRoomUnits(CardState card)
        {
            if (!MultiSummonScenario.Prepared && !TriggeredSummonProbe.Enabled) return null;
            FullBattleTrace trace = FullBattleTrace.Active!;
            var cached = (IEnumerable<WeakRef<CharacterState>>)AccessTools.Field(typeof(CardState), "charactersInRoomAtTimeOfCardPlay").GetValue(card);
            var units = cached.Select(reference => reference.Ref).Where(unit => unit != null && !unit.SpawnedInPreviewMode).ToArray();
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

        private static CardModifiers Modifiers(CardStateModifiers modifiers)
        {
            int Field(string name) => (int)AccessTools.Field(typeof(CardStateModifiers), name).GetValue(modifiers);
            var interactions = new List<string>();
            if (modifiers.GetMergedCardStateIDs().Count > 0) interactions.Add("Merged cards");
            if (modifiers.GraftedEquipmentCardState != null || Field("graftedEquipmentCardStateId") != -1) interactions.Add("Grafted equipment");
            if (modifiers.BoxedCardStates.Count > 0 || ((ICollection)AccessTools.Field(typeof(CardStateModifiers), "boxedCardStateIds")
                .GetValue(modifiers)).Count > 0) interactions.Add("Boxed cards");
            if (modifiers.GetTemporaryTraits().Count > 0 || modifiers.GetTraitReplacements().Count > 0) interactions.Add("Temporary/replaced card traits");
            return new CardModifiers(new CardStatModifier(Field("additionalDamage"), Field("additionalMaxHP"), Field("additionalCost"),
                Field("additionalHeal"), Field("additionalSize"), Field("additionalXCost"), Field("additionalEquipmentLimit"),
                Field("additionalUpgradeSlotCount")), modifiers.GetCardUpgrades().Select(Upgrade).ToArray(),
                modifiers.GetPersistentHP(), interactions);
        }

        internal static CardUpgradeModifier Upgrade(CardUpgradeState upgrade) => Upgrade(upgrade, false);
        internal static CardUpgradeModifier Upgrade(CardUpgradeState upgrade, bool rejectFilters)
        {
            upgradeDepth++;
            try { return UpgradeCore(upgrade, rejectFilters); }
            finally { upgradeDepth--; }
        }
        private static CardUpgradeModifier UpgradeCore(CardUpgradeState upgrade, bool rejectFilters)
        {
            var interactions = new List<string>();
            if (rejectFilters && upgrade.GetFilters().Count > 0) interactions.Add("Filtered bonus-draw upgrade");
            CardUpgradeData? source = upgrade.GetSourceCardUpgradeData();
            bool refresh = source != null && !source.GetUpgradeWillBeScaledByNonMagicPowerTrait();
            if (upgrade.GetRoomAbilityUpgrade() != null) interactions.Add("Upgrade room ability");
            if (upgrade.GetTraitDataUpgrades().Count > 0 || upgrade.GetRemoveTraitUpgrades().Count > 0) interactions.Add("Upgrade traits");
            if (upgrade.GetCardTriggerUpgrades().Count > 0) interactions.Add("Upgrade card triggers");
            CombatTrigger[]? triggers = null;
            if (upgrade.GetTriggerUpgrades().Count > 0)
            {
                if (upgradeDepth > 16) interactions.Add("Recursive upgrade trigger definitions");
                else triggers = upgrade.GetTriggerUpgrades().Select(trigger => EnemySpawningProbe.TriggerDefinition(trigger, interactions)).ToArray();
            }
            if (upgrade.GetRoomModifierUpgrades().Count > 0) interactions.Add("Upgrade room modifiers");
            if (upgrade.GetFilters().Count > 0) interactions.Add("Upgrade card filters");
            if (upgrade.GetUpgradesToRemove().Count > 0) interactions.Add("Upgrade replacements");
            if (upgrade.GetStatusEffectUpgrades().Any(status => status.fromPermanentUpgrade)) interactions.Add("Separate permanent starting-status application group");
            return new CardUpgradeModifier(upgrade.GetCardUpgradeDataId(), upgrade.GetAssetName(),
                new CardStatModifier(upgrade.GetAttackDamage(), upgrade.GetAdditionalHP(), -upgrade.GetCostReduction(),
                    upgrade.GetAdditionalHeal(), upgrade.GetAdditionalSize(), upgrade.GetXCostReduction(),
                    upgrade.GetAdditionalEquipmentLimit(), upgrade.GetAdditionalUpgradeSlotCount()),
                upgrade.GetStatusEffectUpgrades().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
                upgrade.GetRemoveOnDiscard(), upgrade.IsUnique(), upgrade.GetExcludeFromClones(), upgrade.GetAdditionalUnhealedHP(),
                upgrade.GetAttackDamageBuff(), interactions, upgrade.GetRestrictSizeToRoomCapacity(), source?.GetMagicPowerTraitScalingOnly() == true,
                refresh && source!.GetBonusDamage() > 0 ? (int?)source.GetBonusDamage() : null,
                refresh && source!.GetBonusHeal() > 0 ? (int?)source.GetBonusHeal() : null, triggerUpgrades: triggers,
                abilityUpgrade: upgrade.GetUnitAbilityUpgrade() == null ? null : AbilityLifecycleProbe.Change(upgrade.GetUnitAbilityUpgrade()),
                doNotReplaceExistingAbility: upgrade.GetDoNotReplaceExistingUnitAbility());
        }
    }
}
