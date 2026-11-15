using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class EnchantmentCombatProbe
    {
        private static readonly System.Reflection.FieldInfo DestroyedState = AccessTools.Field(typeof(CharacterState), "destroyedState");
        private static readonly HashSet<int> RetainedRemovedSources = new HashSet<int>();
        internal static int RetainedRemovedSourceCount => RetainedRemovedSources.Count;
        private static bool Bound(ICardEffect effect)
        {
            var source = (CharacterState?)AccessTools.Field(effect.GetType(), "enchanterCharacter").GetValue(effect);
            var managers = (ICoreGameManagers?)AccessTools.Field(effect.GetType(), "cachedCoreGameManagers").GetValue(effect);
            return source != null && !managers.IsNullOrDestroyed() &&
                AccessTools.Field(effect.GetType(), "cachedState").GetValue(effect) != null;
        }
        internal static bool AwaitingSourceDestruction(IEnumerable<CharacterState> units)
        {
            // InRemoveList actors remain live until CombatManager.ProcessRemovals
            // at turn end. Wait only once Unity.Destroy has actually been scheduled.
            foreach (CharacterState owner in units)
            {
                if (owner == null || !owner.IsEnchanter) continue;
                foreach (CharacterTriggerState trigger in (IEnumerable<CharacterTriggerState>)
                    AccessTools.Field(typeof(CharacterState), "triggers").GetValue(owner))
                    foreach (CardEffectState state in trigger.GetEffects())
                    {
                        var effect = state.GetCardEffect();
                        if (effect.GetType().Name is not ("CardEffectEnchant" or "CardEffectEnchantCardUpgrade")) continue;
                        var source = (CharacterState?)AccessTools.Field(effect.GetType(), "enchanterCharacter").GetValue(effect);
                        if (source != null && source.IsDestroyed && Bound(effect))
                        {
                            if ((CharacterState.DestroyedState)DestroyedState.GetValue(source) == CharacterState.DestroyedState.Destroyed)
                                return true;
                            RetainedRemovedSources.Add(source.GetInstanceID());
                        }
                    }
            }
            return false;
        }
        internal static EnchantmentRule? Definition(CardEffectData data)
        {
            if (data.GetEffectStateName() is not ("CardEffectEnchant" or "CardEffectEnchantCardUpgrade")) return null;
            var excluded = new List<SubtypeData>(); data.GetTargetCharacterExcludedSubtypes(excluded);
            var target = new CardActionEffect("AddStatus", data.GetTargetMode().ToString(), 0,
                data.GetTargetTeamType().HasFlag(Team.Type.Heroes), data.GetTargetTeamType().HasFlag(Team.Type.Monsters), Array.Empty<CombatStatus>(),
                tests: new CardEffectTests(data.GetShouldTest(), data.GetShouldFailToCastIfTestFails(),
                    data.GetShouldCancelSubsequentEffectsIfTestFails(), false, true, devotionRequirement: DevotionProbe.Requirement(data), devotionRequirementMax: DevotionProbe.Requirement(data, true)),
                filters: new CardTargetFilters(data.GetTargetModeHealthFilter().ToString(), data.GetTargetModeStatusEffectsFilter(),
                    data.GetTargetModeStatusEffectsExcludedFilter(), data.GetTargetIgnoreBosses(),
                    data.GetTargetCharacterSubtype().IsNone ? "" : data.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray(), selfHealthFilter: TargetFilterProbe.HasSelfHealthFilter));
            CardUpgradeModifier? upgrade = null;
            if (data.GetEffectStateName() == "CardEffectEnchantCardUpgrade" && data.GetParamCardUpgradeData() != null)
            {
                var state = new CardUpgradeState(); state.Setup(data.GetParamCardUpgradeData());
                upgrade = CardModifierProbe.Upgrade(state, false, filtersAlreadyApplied: true);
            }
            return new EnchantmentRule(target, data.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(), upgrade: upgrade);
        }
        internal static EnchantmentRule? Capture(CardEffectState state)
        {
            EnchantmentRule? definition = Definition(state.GetSourceCardEffectData());
            if (definition == null) return null;
            var effect = state.GetCardEffect();
            return new EnchantmentRule(definition.Targeting, state.GetParamStatusEffectStackData().Select(status =>
                BattleActionProbe.Status(status.statusId, status.count)).ToArray(), Snapshot(effect), Bound(effect), state.GetParentCardState() != null, definition.Upgrade);
        }
        internal static EnchantmentState Snapshot(ICardEffect effect)
        {
            Type entryType = AccessTools.Inner(effect.GetType(), "EnchantedState");
            bool upgradeAura = effect.GetType().Name == "CardEffectEnchantCardUpgrade";
            EnchantmentTarget[] Map(string field)
            {
                var list = new List<EnchantmentTarget>();
                foreach (DictionaryEntry entry in (IDictionary)AccessTools.Field(effect.GetType(), field).GetValue(effect))
                    list.Add(new EnchantmentTarget(FullBattleTrace.Active!.UnitId((CharacterState)entry.Key),
                        (bool)AccessTools.Field(entryType, "isEnchanted").GetValue(entry.Value),
                        Convert.ToInt32(AccessTools.Field(entryType, "nextStateAction").GetValue(entry.Value)),
                        upgradeAura && AccessTools.Field(entryType, "appliedUpgrade").GetValue(entry.Value) is CardUpgradeState upgrade
                            ? CardModifierProbe.Upgrade(upgrade, false, filtersAlreadyApplied: true) : null));
                return list.ToArray();
            }
            var cached = upgradeAura ? null : (StatusEffectStackData?)AccessTools.Field(effect.GetType(), "statusEffect").GetValue(effect);
            return new EnchantmentState(Map("_primaryEnchantedTargets"), Map("_previewEnchantedTargets"),
                (bool)AccessTools.Field(effect.GetType(), "_previewEnchantedTargetsRequireSync").GetValue(effect), cached == null ? null :
                    new EnchantmentStatus(cached.statusId, cached.count, AllGameManagers.Instance!.GetStatusEffectManager().GetStatusEffectDataById(cached.statusId)?.GetDisplayCategory().ToString()));
        }
    }
}
