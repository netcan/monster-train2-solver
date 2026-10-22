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
        private static readonly Type EntryType = AccessTools.Inner(typeof(CardEffectEnchant), "EnchantedState");
        private static readonly System.Reflection.FieldInfo DestroyedState = AccessTools.Field(typeof(CharacterState), "destroyedState");
        private static readonly HashSet<int> RetainedRemovedSources = new HashSet<int>();
        internal static int RetainedRemovedSourceCount => RetainedRemovedSources.Count;
        private static bool Bound(CardEffectEnchant effect)
        {
            var source = (CharacterState?)AccessTools.Field(typeof(CardEffectEnchant), "enchanterCharacter").GetValue(effect);
            var managers = (ICoreGameManagers?)AccessTools.Field(typeof(CardEffectEnchant), "cachedCoreGameManagers").GetValue(effect);
            return source != null && !managers.IsNullOrDestroyed() &&
                AccessTools.Field(typeof(CardEffectEnchant), "cachedState").GetValue(effect) != null;
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
                        if (state.GetCardEffect() is not CardEffectEnchant effect) continue;
                        var source = (CharacterState?)AccessTools.Field(typeof(CardEffectEnchant), "enchanterCharacter").GetValue(effect);
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
            if (data.GetEffectStateName() != "CardEffectEnchant") return null;
            var excluded = new List<SubtypeData>(); data.GetTargetCharacterExcludedSubtypes(excluded);
            var target = new CardActionEffect("AddStatus", data.GetTargetMode().ToString(), 0,
                data.GetTargetTeamType().HasFlag(Team.Type.Heroes), data.GetTargetTeamType().HasFlag(Team.Type.Monsters), Array.Empty<CombatStatus>(),
                tests: new CardEffectTests(data.GetShouldTest(), data.GetShouldFailToCastIfTestFails(),
                    data.GetShouldCancelSubsequentEffectsIfTestFails(), false, true),
                filters: new CardTargetFilters(data.GetTargetModeHealthFilter().ToString(), data.GetTargetModeStatusEffectsFilter(),
                    data.GetTargetModeStatusEffectsExcludedFilter(), data.GetTargetIgnoreBosses(),
                    data.GetTargetCharacterSubtype().IsNone ? "" : data.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()));
            return new EnchantmentRule(target, data.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray());
        }
        internal static EnchantmentRule? Capture(CardEffectState state)
        {
            EnchantmentRule? definition = Definition(state.GetSourceCardEffectData());
            if (definition == null) return null;
            var effect = (CardEffectEnchant)state.GetCardEffect();
            return new EnchantmentRule(definition.Targeting, definition.StatusPool, Snapshot(effect), Bound(effect), state.GetParentCardState() != null);
        }
        internal static EnchantmentState Snapshot(CardEffectEnchant effect)
        {
            EnchantmentTarget[] Map(string field)
            {
                var list = new List<EnchantmentTarget>();
                foreach (DictionaryEntry entry in (IDictionary)AccessTools.Field(typeof(CardEffectEnchant), field).GetValue(effect))
                    list.Add(new EnchantmentTarget(FullBattleTrace.Active!.UnitId((CharacterState)entry.Key),
                        (bool)AccessTools.Field(EntryType, "isEnchanted").GetValue(entry.Value),
                        Convert.ToInt32(AccessTools.Field(EntryType, "nextStateAction").GetValue(entry.Value))));
                return list.ToArray();
            }
            var cached = (StatusEffectStackData?)AccessTools.Field(typeof(CardEffectEnchant), "statusEffect").GetValue(effect);
            return new EnchantmentState(Map("_primaryEnchantedTargets"), Map("_previewEnchantedTargets"),
                (bool)AccessTools.Field(typeof(CardEffectEnchant), "_previewEnchantedTargetsRequireSync").GetValue(effect), cached == null ? null :
                    new EnchantmentStatus(cached.statusId, cached.count, AllGameManagers.Instance!.GetStatusEffectManager().GetStatusEffectDataById(cached.statusId)?.GetDisplayCategory().ToString()));
        }
    }
}
