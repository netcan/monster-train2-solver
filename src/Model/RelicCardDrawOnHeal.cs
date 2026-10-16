using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicCardDrawOnHealResult
    {
        internal CombatContext? Context { get; }
        internal IReadOnlyList<int> DrawnCardIds { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => Context != null;
        internal RelicCardDrawOnHealResult(CombatContext? context, IReadOnlyList<int>? drawnCardIds = null,
            string? unsupportedReason = null)
        { Context = context; DrawnCardIds = Array.AsReadOnly((drawnCardIds ?? Array.Empty<int>()).ToArray()); UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicCardDrawOnHealModel
    {
        internal const string Effect = "RelicEffectCardDrawOnHeal";
        internal static bool IsEffect(string type) => type == Effect;

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            RelicUnitResourceRule? rule = effect.UnitTrigger;
            return !IsEffect(effect.Type) || !owner.DisallowedInPlacementPhase.HasValue || rule == null ||
                rule.Trigger != "OnHeal" || rule.Team is not ("Monsters" or "Heroes") || rule.Subtype != null ||
                rule.RequiredStatuses.Count != 0 || effect.Amount != effect.ParamInt || effect.Amount < 0 ||
                effect.ParamInt2 != 0 || effect.RequiredMutatorId != null || effect.CanApplyInPreviewMode
                ? "Missing or unsupported native CardDrawOnHeal definition." : null;
        }

        internal static RelicCardDrawOnHealResult Apply(CombatContext source, CombatUnit unit, string trigger,
            bool placement = false, bool preview = false)
        {
            CombatContext context = source;
            var drawn = new List<int>();
            foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (RelicResourceEffect effect in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (!IsEffect(effect.Type)) continue;
                RelicCardDrawOnHealResult result = ApplyOne(context, owner.DataId, effect.EffectIndex, unit,
                    trigger, placement, preview);
                if (!result.Supported) return result;
                context = result.Context!;
                drawn.AddRange(result.DrawnCardIds);
            }
            return new RelicCardDrawOnHealResult(context, drawn);
        }

        internal static RelicCardDrawOnHealResult ApplyOne(CombatContext source, string ownerId, int effectIndex,
            CombatUnit unit, string trigger, bool placement = false, bool preview = false,
            BattlePlayRules? definitions = null)
        {
            CombatRelicState[] owners = source.Relics?.Where(owner => owner.DataId == ownerId).ToArray() ?? Array.Empty<CombatRelicState>();
            if (owners.Length != 1) return Fail("CardDrawOnHeal requires one captured owner.");
            CombatRelicState owner = owners[0];
            RelicResourceEffect[] effects = owner.ResourceEffects?.Where(effect => effect.EffectIndex == effectIndex && IsEffect(effect.Type)).ToArray()
                ?? Array.Empty<RelicResourceEffect>();
            if (effects.Length != 1) return Fail("Missing native CardDrawOnHeal effect index.");
            RelicResourceEffect effect = effects[0];
            string? error = Validate(owner, effect);
            if (error != null) return Fail(error);
            if (placement && owner.DisallowedInPlacementPhase == true || preview && !effect.CanApplyInPreviewMode)
                return new RelicCardDrawOnHealResult(source);

            RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(source, effect.Conditions);
            if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
            CombatContext context = evaluation.Context;
            RelicUnitResourceRule rule = effect.UnitTrigger!;
            bool teamMatches = unit.Team == CombatTeam.Player ? rule.Team == "Monsters" : rule.Team == "Heroes";
            if (!evaluation.Passed || rule.Trigger != trigger || !teamMatches)
                return new RelicCardDrawOnHealResult(context);

            TypedDrawResult draw = NextDrawUpgradeModel.DrawCards(context, effect.Amount, 0, definitions);
            if (!draw.Supported) return Fail(draw.UnsupportedReason!);
            context = draw.Context!;

            // The relic body and its manager each notify the same effect.
            for (int notification = 0; notification < 2; notification++)
            {
                CombatRelicState current = context.Relics!.Single(item => item.DataId == ownerId);
                RelicResourceEffect currentEffect = current.ResourceEffects!.Single(item => item.EffectIndex == effectIndex);
                RelicConditionRecord recorded = RelicConditionModel.Record(context, currentEffect.Conditions);
                if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
                RelicResourceEffect[] resources = current.ResourceEffects!.Select(item => item.EffectIndex == effectIndex
                    ? item.WithConditions(recorded.Conditions) : item).ToArray();
                CombatRelicState[] relics = recorded.Context.Relics!.Select(item => item.DataId == ownerId
                    ? item.WithResourceEffects(resources) : item).ToArray();
                context = recorded.Context.WithRelics(relics);
            }
            return new RelicCardDrawOnHealResult(context, draw.DrawnCardIds);
        }

        private static RelicCardDrawOnHealResult Fail(string error) => new(null, unsupportedReason: error);
    }
}
