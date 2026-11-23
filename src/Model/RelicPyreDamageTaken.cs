using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static partial class RelicResourceModel
    {
        public const string ExtraPyreDamage = "RelicEffectApplyExtraPyreDamage";
        internal const string GoldOnPyreDamage = "RelicEffectAddGoldOnPyreDamage";
        internal const string FhyraOwnerId = "cc792aac-4b82-40af-bf80-3a83bc511383";
        internal const string FhyraOwnerKey = "PyreHeartFhyra_ArtifactData";

        internal static bool IsFhyraGold(CombatRelicState owner, RelicResourceEffect rule) =>
            owner.DataId == FhyraOwnerId && owner.AssetKey == FhyraOwnerKey && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase.HasValue && owner.EffectTypes is [GoldOnPyreDamage] &&
            rule.EffectIndex == 0 && rule.ParamInt == 10 && rule.Amount == rule.ParamInt &&
            rule.RequiredMutatorId == null && rule.Conditions.Count == 0 && rule.CanApplyInPreviewMode;

        internal static CombatContext AwardGoldForPyreLoss(CombatContext source, int damage, bool placement)
        {
            if (damage <= 0 || source.Relics == null) return source;
            CombatContext result = source;
            foreach (CombatRelicState owner in source.Relics)
            {
                if (owner.DataId != FhyraOwnerId || (placement && owner.DisallowedInPlacementPhase == true) ||
                    owner.ResourceEffects is not [RelicResourceEffect rule] || rule.Type != GoldOnPyreDamage) continue;
                result = result.WithGold(Math.Max(0, unchecked(result.Gold + rule.Amount)));
            }
            return result;
        }

        // DoGetDamageToTarget invokes this after defenses, before HP/resurrection.
        // GetDamageToTarget's synchronous estimate does not invoke this manager.
        public static RelicResourceResult ModifyPyreDamageTaken(CombatContext source, int damage,
            bool placement = false, bool preview = false)
        {
            if (source.Relics?.Any(owner => owner.EffectTypes.Contains(ExtraPyreDamage)) != true)
                return new RelicResourceResult(source, damage);
            string? error = RelicModel.Validate(source);
            if (error != null) return new RelicResourceResult(null, 0, error);
            CombatContext context = source;
            var events = new List<RelicResourceEvent>();
            for (int ownerIndex = 0; ownerIndex < context.Relics!.Count; ownerIndex++)
            {
                CombatRelicState owner = context.Relics[ownerIndex];
                if (placement && owner.DisallowedInPlacementPhase == true) continue;
                for (int ruleIndex = 0; ruleIndex < (owner.ResourceEffects?.Count ?? 0); ruleIndex++)
                {
                    RelicResourceEffect rule = context.Relics[ownerIndex].ResourceEffects![ruleIndex];
                    if (rule.Type != ExtraPyreDamage || preview && !rule.CanApplyInPreviewMode) continue;
                    RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!evaluation.Supported) return new RelicResourceResult(null, 0, evaluation.UnsupportedReason);
                    context = evaluation.Context;
                    if (!evaluation.Passed) continue;
                    int before = damage;
                    damage = ExtraPyreDamageStep(before, rule.Amount);
                    // The RoomState notification overload records in previews too;
                    // it skips OnTrigger only for Covenant owners.
                    bool notify = before > 0 && owner.IsCovenant != true;
                    if (notify)
                    {
                        RelicConditionRecord recorded = RelicConditionModel.Record(context, rule.Conditions);
                        if (!recorded.Supported) return new RelicResourceResult(null, 0, recorded.UnsupportedReason);
                        context = ReplaceConditions(recorded.Context, ownerIndex, ruleIndex, recorded.Conditions);
                    }
                    events.Add(new RelicResourceEvent(owner.DataId, rule.EffectIndex, before, damage, notify));
                }
            }
            return new RelicResourceResult(context, damage, events: events);
        }
        internal static int ExtraPyreDamageStep(int damage, int extra) => damage > 0 ? unchecked(damage + extra) : 0;
    }
}
