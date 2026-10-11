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
        public CombatRelicState(string dataId, string assetKey, IReadOnlyList<string> effectTypes,
            IReadOnlyList<RelicSpawnStatus>? spawnStatuses = null, bool? isCovenant = null, bool? disallowedInPlacementPhase = null,
            IReadOnlyList<RelicCardModifier>? cardModifiers = null, IReadOnlyList<RelicCharacterTriggers>? triggerAdditions = null,
            IReadOnlyList<RelicStatusStacks>? statusStackAdditions = null)
        { DataId = dataId; AssetKey = assetKey; EffectTypes = Array.AsReadOnly(effectTypes.ToArray());
            SpawnStatuses = spawnStatuses == null ? null : Array.AsReadOnly(spawnStatuses.ToArray());
            IsCovenant = isCovenant; DisallowedInPlacementPhase = disallowedInPlacementPhase;
            CardModifiers = cardModifiers == null ? null : Array.AsReadOnly(cardModifiers.ToArray());
            TriggerAdditions = triggerAdditions == null ? null : Array.AsReadOnly(triggerAdditions.ToArray());
            StatusStackAdditions = statusStackAdditions == null ? null : Array.AsReadOnly(statusStackAdditions.ToArray()); }
        internal CombatRelicState WithSpawnStatuses(IReadOnlyList<RelicSpawnStatus> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, rules, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions);
        internal CombatRelicState WithCardModifiers(IReadOnlyList<RelicCardModifier> modifiers) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, modifiers, TriggerAdditions, StatusStackAdditions);
        internal CombatRelicState WithTriggerAdditions(IReadOnlyList<RelicCharacterTriggers> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, rules, StatusStackAdditions);
        internal CombatRelicState WithStatusStackAdditions(IReadOnlyList<RelicStatusStacks> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, rules);
    }

    internal static class RelicModel
    {
        internal const string AbilityIncant = "RelicEffectIncantTriggeredByUnitAbilities";
        internal const string ModifyTriggerCount = "RelicEffectModifyTriggerCount";
        internal const string AddStatusOnSpawn = "RelicEffectAddStatusEffectOnSpawn";
        internal const string AddTempUpgrade = "RelicEffectAddTempUpgrade";
        internal const string AddTrigger = "RelicEffectAddTrigger";
        internal const string AddStatusOnApplied = "RelicEffectAddStatusEffectOnStatusApplied";
        internal static bool AbilitiesTriggerIncant(CombatContext? context) =>
            context?.Relics?.Any(relic => relic.EffectTypes.Contains(AbilityIncant)) == true;
        internal static string? Validate(CombatContext? context) => Validate(context?.Relics) ??
            (context?.Relics?.Any(relic => relic.EffectTypes.Contains(ModifyTriggerCount) || relic.EffectTypes.Contains(AddTrigger)) == true && context.TriggerCounts == null
                ? "Missing native relic trigger count cache." : TriggerCountModel.Validate(context?.TriggerCounts));
        internal static string? Validate(IReadOnlyList<CombatRelicState>? relics)
        {
            if (relics == null) return null; // Legacy captures retained their external-effect guard.
            foreach (CombatRelicState relic in relics)
            {
                if (string.IsNullOrEmpty(relic.DataId) || string.IsNullOrEmpty(relic.AssetKey))
                    return "Missing captured relic identity.";
                foreach (string effect in relic.EffectTypes)
                    if (effect != AbilityIncant && effect != ModifyTriggerCount && effect != AddStatusOnSpawn && effect != AddTempUpgrade && effect != AddTrigger && effect != AddStatusOnApplied) return "Unmodeled relic effect " + effect + " on " + relic.AssetKey + ".";
                string? error = RelicCardModifierModel.Validate(relic) ?? RelicSpawnStatusModel.Validate(relic) ?? RelicCharacterTriggerModel.Validate(relic) ?? RelicStatusStackModel.Validate(relic);
                if (error != null) return error;
                // Keep the prior queue boundary guarded until a mixed-order
                // native cardless-upgrade fixture observes its interleaving.
                if (relic.CardModifiers?.Any(effect => effect.ApplyToCardlessSpawns &&
                    relic.SpawnStatuses?.Any(status => status.EffectIndex < effect.EffectIndex) == true) == true)
                    return "Mixed cardless upgrade and spawn-status effect ordering is unmodeled.";
            }
            return null;
        }
    }
}
