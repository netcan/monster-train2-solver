using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class StatusAttackDamageResult
    {
        internal CombatUnit? Unit { get; }
        internal CombatContext? Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => Unit != null;
        internal StatusAttackDamageResult(CombatUnit? unit, CombatContext? context, string? error = null)
        { Unit = unit; Context = context; UnsupportedReason = error; }
    }

    internal static class StatusAttackDamageModel
    {
        // Native stores status attack separately. A later condition change does
        // not recompute old stacks, and removal can leave a nonzero accumulator.
        internal static StatusAttackDamageResult Change(CombatContext? context, CombatUnit unit, CombatStatus status, int delta, bool adding)
        {
            if (delta > 0 && status.Id == "mageblade")
            {
                if (unit.Modifiers == null)
                    return new StatusAttackDamageResult(null, context, "Mageblade requires unit modifier state.");
                StatusScalingResult queried = RelicStatusMagnitudeModel.GetModifier(context, status.Id,
                    unit.Team == CombatTeam.Player ? 2 : 1);
                if (!queried.Supported) return new StatusAttackDamageResult(null, null, queried.UnsupportedReason);
                int stacks = unit.RegisteredStatus(status.Id)?.Stacks ?? 0;
                int coefficient = unchecked(unchecked(status.ParamInt + queried.Stacks) * stacks);
                return new StatusAttackDamageResult(unit.WithModifiers(unit.Modifiers.WithMagebladeDamage(coefficient)), queried.Context);
            }
            if (delta <= 0 || status.Id != "buff" && status.Id != "debuff" && status.Id != "valor") return new StatusAttackDamageResult(unit, context);
            if (unit.Modifiers?.DamageBuffFromStatusEffects == null)
                return context?.Relics?.Any(relic => relic.StatusMagnitudes?.Count > 0) == true
                    ? new StatusAttackDamageResult(null, context, "Missing native status attack accumulator.") : new StatusAttackDamageResult(unit, context);
            if (status.Id == "valor" && unit.Health <= 0) return new StatusAttackDamageResult(unit, context);
            int modifier = 0;
            if (status.Id != "valor")
            {
                StatusScalingResult queried = RelicStatusMagnitudeModel.GetModifier(context, status.Id, unit.Team == CombatTeam.Player ? 2 : 1);
                if (!queried.Supported) return new StatusAttackDamageResult(null, null, queried.UnsupportedReason);
                context = queried.Context; modifier = queried.Stacks;
            }
            int amount = unchecked(unchecked(status.ParamInt + modifier) * delta);
            // CharacterState.BuffDamage/DebuffDamage ignore nonpositive amounts.
            if (amount <= 0) return new StatusAttackDamageResult(unit, context);
            int sign = (adding ? 1 : -1) * (status.Id == "debuff" ? -1 : 1);
            int damage = unchecked(unit.Modifiers.DamageBuffFromStatusEffects.Value + sign * amount);
            return new StatusAttackDamageResult(unit.WithStatusDamage(damage), context);
        }
    }
}
