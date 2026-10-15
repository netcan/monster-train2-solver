using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicUnitResourceRule
    {
        public string Trigger { get; }
        public string Team { get; }
        public string? Subtype { get; }
        public IReadOnlyList<string> RequiredStatuses { get; }
        public RelicUnitResourceRule(string trigger, string team, string? subtype, IReadOnlyList<string> requiredStatuses)
        { Trigger = trigger; Team = team; Subtype = subtype; RequiredStatuses = Array.AsReadOnly(requiredStatuses.ToArray()); }
    }
    public static class RelicUnitResourceModel
    {
        public const string Gold = "RelicEffectGoldOnTrigger";
        public const string Ember = "RelicEffectEmberOnUnitTrigger";
        public static bool IsEffect(string type) => type == Gold || type == Ember;
        public static RelicResourceResult Apply(CombatContext source, CombatUnit unit, string trigger, bool alive,
            int paramInt = 0, bool placement = false, bool preview = false)
        {
            var context = source; var events = new List<RelicResourceEvent>();
            foreach (var owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (var effect in owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
            {
                if (!IsEffect(effect.Type)) continue;
                var result = ApplyOne(context, owner.DataId, effect.EffectIndex, unit, trigger, alive, paramInt, placement, preview);
                if (!result.Supported) return result;
                context = result.Context!; events.AddRange(result.Events);
            }
            return new RelicResourceResult(context, events.Sum(item => item.After), events: events);
        }
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            var rule = effect.UnitTrigger;
            return !owner.DisallowedInPlacementPhase.HasValue || rule == null || string.IsNullOrEmpty(rule.Trigger) ||
                rule.Team is not ("Monsters" or "Heroes" or "None") || rule.Subtype == "" ||
                effect.Amount != effect.ParamInt || effect.RequiredMutatorId != null || effect.Amount < 0 ||
                effect.Type == Gold && rule.Subtype != null || effect.Type == Ember && rule.RequiredStatuses.Count != 0 ||
                rule.RequiredStatuses.Any(string.IsNullOrEmpty)
                ? "Missing or unsupported native unit resource callback definition." : null;
        }
        public static RelicResourceResult ApplyOne(CombatContext source, string ownerId, int effectIndex,
            CombatUnit unit, string trigger, bool alive, int paramInt = 0, bool placement = false,
            bool preview = false, bool includeManagerNotification = true)
        {
            var owners = source.Relics?.Where(owner => owner.DataId == ownerId).ToArray();
            if (owners?.Length != 1) return Fail("Unit resources require one captured owner.");
            var owner = owners[0]; var effects = owner.ResourceEffects?.Where(effect => effect.EffectIndex == effectIndex && IsEffect(effect.Type)).ToArray();
            if (effects?.Length != 1) return Fail("Missing native unit resource effect index.");
            var effect = effects[0]; string? error = Validate(owner, effect);
            if (error != null) return Fail(error);
            if (placement && owner.DisallowedInPlacementPhase == true || preview && !effect.CanApplyInPreviewMode)
                return new RelicResourceResult(source, 0);
            var evaluation = RelicConditionModel.Evaluate(source, effect.Conditions);
            if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!);
            var context = evaluation.Context; var rule = effect.UnitTrigger!;
            bool team = unit.Team == CombatTeam.Player ? rule.Team == "Monsters" : rule.Team == "Heroes";
            bool subtype = rule.Subtype == null ? unit.Team == CombatTeam.Enemy || !unit.IsPyre : unit.Subtypes.Contains(rule.Subtype);
            if (!evaluation.Passed || rule.Trigger != trigger || !team || effect.Type == Ember && !subtype ||
                rule.RequiredStatuses.Any(status => !(unit.Status(status)?.Stacks > 0)) ||
                trigger == "OnDeath" && alive && (effect.Type == Ember || paramInt != 1))
                return new RelicResourceResult(context, 0);
            int amount = effect.Type == Gold ? GoldRewardModel.Adjust(effect.Amount, multiplier: context.Resources?.GoldRewardMultiplier ?? 1f) : effect.Amount;
            context = effect.Type == Gold ? context.WithGold(Math.Max(0, unchecked(context.Gold + amount))) : EnergyModel.Apply(context, "GainEnergy", amount);
            // Apply invokes NotifyRelicTriggered; RelicManager invokes it again.
            for (int i = 0; i < (includeManagerNotification ? 2 : 1); i++)
            {
                var current = context.Relics!.Single(item => item.DataId == ownerId);
                var currentEffect = current.ResourceEffects!.Single(item => item.EffectIndex == effectIndex);
                var recorded = RelicConditionModel.Record(context, currentEffect.Conditions);
                if (!recorded.Supported) return Fail(recorded.UnsupportedReason!);
                var replaced = current.WithResourceEffects(current.ResourceEffects!.Select(item => item.EffectIndex == effectIndex ? item.WithConditions(recorded.Conditions) : item).ToArray());
                context = recorded.Context.WithRelics(recorded.Context.Relics!.Select(item => item.DataId == ownerId ? replaced : item).ToArray());
            }
            return new RelicResourceResult(context, amount, events: new[] { new RelicResourceEvent(ownerId, effectIndex, 0, amount, true) });
        }
        private static RelicResourceResult Fail(string error) => new RelicResourceResult(null, 0, error);
    }
}
