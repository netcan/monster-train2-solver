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
        internal static EnchantmentRule? Definition(CardEffectData data)
        {
            if (data.GetEffectStateName() != "CardEffectEnchant") return null;
            var excluded = new List<SubtypeData>(); data.GetTargetCharacterExcludedSubtypes(excluded);
            var target = new CardActionEffect("AddStatus", data.GetTargetMode().ToString(), 0,
                data.GetTargetTeamType().HasFlag(Team.Type.Heroes), data.GetTargetTeamType().HasFlag(Team.Type.Monsters), Array.Empty<CombatStatus>(),
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
            var source = (CharacterState?)AccessTools.Field(typeof(CardEffectEnchant), "enchanterCharacter").GetValue(effect);
            var managers = (ICoreGameManagers?)AccessTools.Field(typeof(CardEffectEnchant), "cachedCoreGameManagers").GetValue(effect);
            bool bound = source != null && !managers.IsNullOrDestroyed() &&
                AccessTools.Field(typeof(CardEffectEnchant), "cachedState").GetValue(effect) != null;
            return new EnchantmentRule(definition.Targeting, definition.StatusPool, Snapshot(effect), bound, state.GetParentCardState() != null);
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
