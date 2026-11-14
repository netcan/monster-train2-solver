using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicDevotionOnDamageModel
    {
        internal const string Effect = "RelicEffectGainDevotionOnFriendlyDamage";
        internal const string Id = "63d34fff-d731-4205-b64f-a355bcb4f20b";
        internal const string Key = "DrawCardOnEarlyDiscard";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicConditionState? condition = effect.Conditions.Count == 1 ? effect.Conditions[0] : null;
            return owner.DataId == Id && owner.AssetKey == Key && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { Effect }) &&
                effect.Type == Effect && effect.EffectIndex == 0 && effect.ParamInt == 1 && effect.ParamInt2 == 10 && effect.Amount == 1 &&
                !effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null &&
                effect.UnitTrigger is { Trigger: "OnHit", Team: "Monsters", Subtype: null } rule && rule.RequiredStatuses.Count == 0 &&
                condition is { TrackTriggerCount: false, AllowMultiple: true, Comparator: 6, Value: 10 } &&
                condition.Query.Type == "FriendlyDamageDealtToMonsters" && condition.Query.Duration == "ThisBattle" && condition.Query.SubtypeIsNone
                ? null : "Missing or changed original friendly-damage Devotion definition.";
        }

        internal static DevotionResult Apply(CombatContext source, CombatUnit actor, string trigger,
            bool placement = false, bool preview = false)
        {
            CombatContext context = source; bool leveled = false;
            foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (RelicResourceEffect effect in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (effect.Type != Effect) continue;
                DevotionResult result = ApplyOne(context, owner.DataId, effect.EffectIndex, actor, trigger, placement, preview);
                if (!result.Supported) return result;
                context = result.Context!; leveled |= result.LeveledUp;
            }
            return new DevotionResult(context, leveled);
        }

        internal static DevotionResult ApplyOne(CombatContext source, string ownerId, int effectIndex,
            CombatUnit actor, string trigger, bool placement = false, bool preview = false)
        {
            CombatRelicState[] owners = source.Relics?.Where(owner => owner.DataId == ownerId).ToArray() ?? Array.Empty<CombatRelicState>();
            if (owners.Length != 1) return Fail("Devotion damage callback requires one captured owner.");
            CombatRelicState owner = owners[0];
            RelicResourceEffect[] effects = owner.ResourceEffects?.Where(effect => effect.EffectIndex == effectIndex && effect.Type == Effect).ToArray()
                ?? Array.Empty<RelicResourceEffect>();
            if (effects.Length != 1) return Fail("Missing native Devotion damage effect index.");
            RelicResourceEffect effect = effects[0];
            string? error = Validate(owner, effect) ?? DevotionModel.Validate(source);
            if (error != null) return Fail(error);
            if (source.Statistics?.MonsterDamage == null) return Fail("Devotion damage callback requires captured game 2.3.1 damage statistics.");
            if (placement && owner.DisallowedInPlacementPhase == true || preview && !effect.CanApplyInPreviewMode)
                return new DevotionResult(source);
            RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(source, effect.Conditions);
            if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
            CombatContext context = evaluation.Context;
            if (!evaluation.Passed || trigger != "OnHit" || actor.Team != CombatTeam.Player || actor.IsPyre)
                return new DevotionResult(context);

            // Native body notifies before awarding; its manager notifies again
            // after level callbacks. One hit awards once even if it crosses
            // several thresholds; both records retain the current damage value.
            DevotionResult body = Record(context, ownerId, effectIndex);
            if (!body.Supported) return body;
            DevotionResult awarded = DevotionModel.Award(body.Context!, effect.Amount);
            if (!awarded.Supported) return awarded;
            DevotionResult manager = Record(awarded.Context!, ownerId, effectIndex);
            return manager.Supported ? new DevotionResult(manager.Context, awarded.LeveledUp) : manager;
        }

        private static DevotionResult Record(CombatContext context, string ownerId, int effectIndex)
        {
            CombatRelicState owner = context.Relics!.Single(item => item.DataId == ownerId);
            RelicResourceEffect effect = owner.ResourceEffects!.Single(item => item.EffectIndex == effectIndex);
            RelicConditionRecord recorded = RelicConditionModel.Record(context, effect.Conditions);
            if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
            RelicResourceEffect[] effects = owner.ResourceEffects!.Select(item => item.EffectIndex == effectIndex
                ? item.WithConditions(recorded.Conditions) : item).ToArray();
            return new DevotionResult(recorded.Context.WithRelics(recorded.Context.Relics!.Select(item =>
                item.DataId == ownerId ? item.WithResourceEffects(effects) : item).ToArray()));
        }
        private static DevotionResult Fail(string error) => new(null, error: error);
    }
}
