using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityLifecycleProbe
    {
        internal static bool Known(string type) => type == "CardEffectSetUnitAbility" || type == "CardEffectRemoveAbility";
        internal static CardActionEffect Capture(CardEffectState state) => Describe(state.GetSourceCardEffectData(),
            state.GetParamCardData(), state.GetParamBool(), state.GetParamRelicData());
        internal static CardActionEffect Describe(CardEffectData effect) => Describe(effect, effect.GetParamCardData(),
            effect.GetParamBool(), effect.GetParamRelicData());
        private static CardActionEffect Describe(CardEffectData effect, CardData? card, bool permanent, RelicData? relic)
        {
            bool remove = effect.GetEffectStateName() == "CardEffectRemoveAbility";
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            AbilityChangeRule change = Change(remove ? null : card, permanent: remove && permanent);
            change = new AbilityChangeRule(change.Definition, false, change.Permanent, change.CommonTriggers,
                remove && relic != null && AllGameManagers.Instance!.GetRelicManager().HasRelicState(relic.GetID()));
            return new CardActionEffect(remove ? "RemoveAbility" : "SetUnitAbility", effect.GetTargetMode().ToString(), 0,
                effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters), Array.Empty<CombatStatus>(),
                tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(), effect.GetShouldCancelSubsequentEffectsIfTestFails(), false),
                filters: new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()), abilityChange: change);
        }
        internal static UnitAbilityDefinition Definition(CardData data) => new UnitAbilityDefinition(data.GetID(),
            data.IsUnitAbility(), data.GetCooldownAfterActivated(), data.GetCooldownAtSpawn(), CardGenerationProbe.Creation(data));

        internal static UnitAbilityRules Rules(CharacterData? data) => data == null ? new UnitAbilityRules(false, Array.Empty<CombatStatus>()) : new UnitAbilityRules(
            data.GetPreventAblitiesFromEquipment(), data.GetStartingStatusEffects().Select(status =>
            {
                StatusEffectData rule = StatusEffectManager.Instance.GetStatusEffectDataById(status.statusId)!;
                return new CombatStatus(status.statusId, status.count, rule.GetParamInt(), rule.GetRemoveWhenTriggered(),
                    rule.GetRemoveStackAtEndOfTurn(), rule.GetRemoveAtEndOfTurn(), rule.GetRemoveAtEndOfTurnAfterPostCombat(),
                    false, rule.GetSkipTriggerDuringDeployment(), rule.GetRemoveDuringDeployment(),
                    BattleActionProbe.TriggeredVfx(rule, -1f), BattleActionProbe.TriggeredVfx(rule, 1f), rule.IsStackable(),
                    rule.IsHidden(), rule.GetDisplayCategory().ToString());
            }).ToArray());

        internal static string[] Disabled(SaveManager save)
        {
            object data = AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            return ((List<string>)AccessTools.Field(data.GetType(), "permanentlyDisabledAbilities").GetValue(data)).ToArray();
        }

        internal static AbilityChangeRule Change(CardData? definition, bool equipment = false, bool permanent = false)
        {
            var interactions = new List<string>();
            CombatTrigger[] triggers = AllGameManagers.Instance!.GetCombatManager()!.GetUnitAbilityCommonData().GetCommonTriggers()
                .Select(trigger => EnemySpawningProbe.TriggerDefinition(trigger, interactions)).ToArray();
            if (interactions.Count != 0) throw new InvalidOperationException("Unsupported common ability definitions: " + string.Join("; ", interactions));
            return new AbilityChangeRule(definition == null ? null : Definition(definition), equipment, permanent, triggers);
        }
    }
}
