using System;

namespace MonsterTrain2Poju.Model
{
    public static partial class RelicResourceModel
    {
        public const string ConserveForgePoints = "RelicEffectConserveForgePoints";

        // PlayerManager.OnCombatEnd, after the terminal battle snapshot. This
        // is deliberately separate from EndTurn and its terminal return value.
        public static RelicResourceResult FinishCombatForgePoints(CombatContext source, bool preview = false)
        {
            string? error = RelicModel.Validate(source) ?? ForgePointsModel.Validate(source);
            if (error != null) return Fail(error);
            if (source.Relics == null) return Fail("Combat-end forge conservation requires captured relic ownership.");
            int current = source.QueryFrame!.ForgePoints!.Value;
            if (preview) return new RelicResourceResult(source, current);
            CombatContext context = source;
            for (int relicIndex = 0; relicIndex < (context.Relics?.Count ?? 0); relicIndex++)
            {
                CombatRelicState owner = context.Relics![relicIndex];
                for (int ruleIndex = 0; ruleIndex < (owner.ResourceEffects?.Count ?? 0); ruleIndex++)
                {
                    RelicResourceEffect rule = owner.ResourceEffects![ruleIndex];
                    if (rule.Type != ConserveForgePoints) continue;
                    var evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
                    context = evaluation.Context;
                    if (!evaluation.Passed) continue;
                    // Native manager uses the first eligible effect and notifies
                    // even when the retained value is zero. No placement gate.
                    var recorded = RelicConditionModel.Record(context, rule.Conditions);
                    if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
                    context = ReplaceConditions(recorded.Context, relicIndex, ruleIndex, recorded.Conditions);
                    int conserved = rule.ParamInt2 == 0 ? Math.Min(current, rule.Amount) :
                        rule.ParamInt2 == 1 ? unchecked(current * rule.Amount) / 100 : 0;
                    int value = Math.Max(0, conserved);
                    return new RelicResourceResult(context.WithQueryFrame(context.QueryFrame!.With(forgePoints: value)), value,
                        events: new[] { new RelicResourceEvent(owner.DataId, rule.EffectIndex, current, value, true) });
                }
            }
            return new RelicResourceResult(context.WithQueryFrame(context.QueryFrame!.With(forgePoints: 0)), 0);
        }
    }
}
