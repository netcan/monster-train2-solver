using System;

namespace MonsterTrain2Poju.Model
{
    internal static class ZealStatusModel
    {
        internal static StatusAttackDamageResult Change(CombatContext? context, CombatUnit unit,
            CombatStatus status, int delta, bool adding)
        {
            if (delta <= 0) return new StatusAttackDamageResult(unit, context);
            if (unit.Modifiers?.DamageBuffFromStatusEffects == null)
                return new StatusAttackDamageResult(null, context, "Zeal requires the native status attack accumulator.");
            StatusScalingResult magnitude = RelicStatusMagnitudeModel.GetModifier(context, status.Id,
                unit.Team == CombatTeam.Player ? 2 : 1);
            if (!magnitude.Supported) return new StatusAttackDamageResult(null, context, magnitude.UnsupportedReason);
            int attack = unchecked(unchecked(status.ParamInt + magnitude.Stacks) * delta);
            if (attack > 0)
                unit = unit.WithStatusDamage(unchecked(unit.Modifiers.DamageBuffFromStatusEffects.Value + (adding ? attack : -attack)));
            // The secondary magnitude never includes relic attack modifiers.
            // Neither API heals; removal only clips HP to the remaining maximum.
            int health = unchecked(status.ParamInt * delta);
            if (health > 0)
            {
                int maximum = adding ? Math.Min(99999, unchecked(unit.MaxHealth + health)) : Math.Max(1, unchecked(unit.MaxHealth - health));
                if (maximum <= 0) return new StatusAttackDamageResult(null, context, "Zeal maximum-health overflow requires native calibration.");
                unit = unit.WithHealthAndMaxHealth(adding ? unit.Health : Math.Min(unit.Health, maximum), maximum);
            }
            return new StatusAttackDamageResult(unit, magnitude.Context);
        }
    }
}
