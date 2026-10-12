using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Preserve the native manager's order and runtime effect types. Duplicate relic
    // definitions can be separate instances; their presence is not a stack count.
    public sealed class CombatRelicState
    {
        public string DataId { get; }
        public string AssetKey { get; }
        public IReadOnlyList<string> EffectTypes { get; }
        public IReadOnlyList<RelicSpawnStatus>? SpawnStatuses { get; }
        public bool? IsCovenant { get; }
        public bool? DisallowedInPlacementPhase { get; }
        public IReadOnlyList<RelicCardModifier>? CardModifiers { get; }
        public IReadOnlyList<RelicCharacterTriggers>? TriggerAdditions { get; }
        public IReadOnlyList<RelicStatusStacks>? StatusStackAdditions { get; }
        public IReadOnlyList<RelicStatusMagnitude>? StatusMagnitudes { get; }
        public IReadOnlyList<RelicStatusDecay>? StatusDecays { get; }
        public IReadOnlyList<RelicResourceEffect>? ResourceEffects { get; }
        public IReadOnlyList<RelicMagicPowerModifier>? MagicPowerModifiers { get; }
        public CombatRelicState(string dataId, string assetKey, IReadOnlyList<string> effectTypes,
            IReadOnlyList<RelicSpawnStatus>? spawnStatuses = null, bool? isCovenant = null, bool? disallowedInPlacementPhase = null,
            IReadOnlyList<RelicCardModifier>? cardModifiers = null, IReadOnlyList<RelicCharacterTriggers>? triggerAdditions = null,
            IReadOnlyList<RelicStatusStacks>? statusStackAdditions = null, IReadOnlyList<RelicStatusMagnitude>? statusMagnitudes = null,
            IReadOnlyList<RelicStatusDecay>? statusDecays = null, IReadOnlyList<RelicResourceEffect>? resourceEffects = null,
            IReadOnlyList<RelicMagicPowerModifier>? magicPowerModifiers = null)
        { DataId = dataId; AssetKey = assetKey; EffectTypes = Array.AsReadOnly(effectTypes.ToArray());
            SpawnStatuses = spawnStatuses == null ? null : Array.AsReadOnly(spawnStatuses.ToArray());
            IsCovenant = isCovenant; DisallowedInPlacementPhase = disallowedInPlacementPhase;
            CardModifiers = cardModifiers == null ? null : Array.AsReadOnly(cardModifiers.ToArray());
            TriggerAdditions = triggerAdditions == null ? null : Array.AsReadOnly(triggerAdditions.ToArray());
            StatusStackAdditions = statusStackAdditions == null ? null : Array.AsReadOnly(statusStackAdditions.ToArray());
            StatusMagnitudes = statusMagnitudes == null ? null : Array.AsReadOnly(statusMagnitudes.ToArray());
            StatusDecays = statusDecays == null ? null : Array.AsReadOnly(statusDecays.ToArray());
            ResourceEffects = resourceEffects == null ? null : Array.AsReadOnly(resourceEffects.ToArray());
            MagicPowerModifiers = magicPowerModifiers == null ? null : Array.AsReadOnly(magicPowerModifiers.ToArray()); }
        internal CombatRelicState WithSpawnStatuses(IReadOnlyList<RelicSpawnStatus> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, rules, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers);
        internal CombatRelicState WithCardModifiers(IReadOnlyList<RelicCardModifier> modifiers) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, modifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers);
        internal CombatRelicState WithTriggerAdditions(IReadOnlyList<RelicCharacterTriggers> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, rules, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers);
        internal CombatRelicState WithStatusStackAdditions(IReadOnlyList<RelicStatusStacks> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, rules, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers);
        internal CombatRelicState WithStatusMagnitudes(IReadOnlyList<RelicStatusMagnitude> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, rules, StatusDecays, ResourceEffects, MagicPowerModifiers);
        internal CombatRelicState WithResourceEffects(IReadOnlyList<RelicResourceEffect> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, rules, MagicPowerModifiers);
        internal CombatRelicState WithMagicPowerModifiers(IReadOnlyList<RelicMagicPowerModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, rules);
    }

    internal static class RelicModel
    {
        internal const string AbilityIncant = "RelicEffectIncantTriggeredByUnitAbilities";
        internal const string ModifyTriggerCount = "RelicEffectModifyTriggerCount";
        internal const string AddStatusOnSpawn = "RelicEffectAddStatusEffectOnSpawn";
        internal const string AddTempUpgrade = "RelicEffectAddTempUpgrade";
        // This type only provides generated-card tooltip data and has no battle hook.
        internal const string AddCardsNull = "RelicEffectAddCardsNull";
        internal const string ModifyCardMagicalPower = "RelicEffectModifyCardMagicalPower";
        internal const string AddTrigger = "RelicEffectAddTrigger";
        internal const string AddStatusOnApplied = "RelicEffectAddStatusEffectOnStatusApplied";
        internal const string ModifyStatusMagnitude = "RelicEffectModifyStatusMagnitude";
        internal const string PreventStatusDecay = "RelicEffectPreventStatusEffectDecay";
        // These effects are resolved by SpawnGroupData.GetCharacters before the
        // native enemy definitions are captured for a spawn decision.
        internal const string ModifyCharacterAttackDamage = "RelicEffectModifyCharacterAttackDamage";
        internal const string ModifyCharacterMaxHealth = "RelicEffectModifyCharacterMaxHealth";
        internal const string AllDamageIsPiercing = "RelicEffectAllDamageIsPiercing";
        internal static bool AbilitiesTriggerIncant(CombatContext? context) =>
            context?.Relics?.Any(relic => relic.EffectTypes.Contains(AbilityIncant)) == true;
        internal static string? Validate(CombatContext? context) => Validate(context?.Relics) ??
            (context?.Relics?.Any(relic => relic.EffectTypes.Contains(ModifyTriggerCount) || relic.EffectTypes.Contains(AddTrigger)) == true && context.TriggerCounts == null
                ? "Missing native relic trigger count cache." : TriggerCountModel.Validate(context?.TriggerCounts)) ?? RelicResourceModel.Validate(context);
        internal static string? Validate(IReadOnlyList<CombatRelicState>? relics)
        {
            if (relics == null) return null; // Legacy captures retained their external-effect guard.
            foreach (CombatRelicState relic in relics)
            {
                if (string.IsNullOrEmpty(relic.DataId) || string.IsNullOrEmpty(relic.AssetKey))
                    return "Missing captured relic identity.";
                foreach (string effect in relic.EffectTypes)
                    if (effect != AbilityIncant && effect != ModifyTriggerCount && effect != AddStatusOnSpawn && effect != AddTempUpgrade && effect != AddCardsNull && effect != ModifyCardMagicalPower && effect != AddTrigger && effect != AddStatusOnApplied && effect != ModifyStatusMagnitude && effect != PreventStatusDecay && effect != ModifyCharacterAttackDamage && effect != ModifyCharacterMaxHealth && effect != AllDamageIsPiercing && !RelicResourceModel.IsEffect(effect)) return "Unmodeled relic effect " + effect + " on " + relic.AssetKey + ".";
                string? error = RelicCardModifierModel.Validate(relic) ?? RelicMagicPowerModel.Validate(relic) ?? RelicSpawnStatusModel.Validate(relic) ?? RelicCharacterTriggerModel.Validate(relic) ?? RelicStatusStackModel.Validate(relic) ?? RelicStatusMagnitudeModel.Validate(relic) ?? RelicStatusDecayModel.Validate(relic) ?? RelicResourceModel.Validate(relic);
                if (error != null) return error;
            }
            return null;
        }
    }
}
