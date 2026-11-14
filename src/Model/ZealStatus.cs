using System;
using System.Collections.Generic;

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
            if (!adding && unit.Status("horde")?.Stacks > 0)
                return new StatusAttackDamageResult(null, context, "Zeal removal with Horde casualties requires native calibration.");
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

        // BuffMaxHPNoHeal/DebuffMaxHPNoSacrifice update Deathwish before the
        // outer status API enqueues its status-change notifications.
        internal static RoomCombatResult Membership(RoomCombatState source, CombatUnit unit, CombatStatus status, int delta)
        {
            var scope = new RoomCombatState(source.RoomIndex, source.Deployment, new[] { unit },
                Array.Empty<string>(), source.Context, source.Preview, source.ActiveCompanionBossIds);
            return status.Id == "zeal" && delta > 0 && unchecked(status.ParamInt * delta) > 0
                ? HalfHealthDeathwishModel.Update(scope, unit.Id)
                : new RoomCombatResult(scope, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        }
    }
}
