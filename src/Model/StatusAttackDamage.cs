using System;
using System.Collections.Generic;
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
        internal static CombatUnit PrepareBirthTemplate(CombatUnit template, IReadOnlyList<CombatStatus> startingApplications)
        {
            if (template.Modifiers == null || template.Modifiers.MagebladeDamagePerMagicPower.HasValue ||
                !startingApplications.Any(status => status.Id == "mageblade" && status.Stacks > 0))
                return template;
            return template.WithModifiers(template.Modifiers.WithMagebladeDamage(0));
        }

        // Native stores status attack separately. A later condition change does
        // not recompute old stacks, and removal can leave a nonzero accumulator.
        internal static StatusAttackDamageResult Change(CombatContext? context, CombatUnit unit, CombatStatus status, int delta, bool adding)
        {
            if (status.Id == "zeal") return ZealStatusModel.Change(context, unit, status, delta, adding);
            if (status.Id == Malaise)
            {
                string? error = ValidateMalaise(unit, status, context);
                if (error != null) return new StatusAttackDamageResult(null, context, error);
                if (delta <= 0) return new StatusAttackDamageResult(unit, context);
                StatusScalingResult magnitude = RelicStatusMagnitudeModel.GetModifier(context, status.Id,
                    unit.Team == CombatTeam.Player ? 2 : 1);
                if (!magnitude.Supported) return new StatusAttackDamageResult(null, null, magnitude.UnsupportedReason);
                int malaiseAmount = unchecked((status.ParamInt + magnitude.Stacks) * delta);
                if (malaiseAmount <= 0) return new StatusAttackDamageResult(unit, magnitude.Context);
                int maximum = adding ? Math.Max(1, unit.MaxHealth - malaiseAmount) : Math.Min(99999, unit.MaxHealth + malaiseAmount);
                int attack = unchecked(unit.Modifiers!.DamageBuffFromStatusEffects!.Value + (adding ? -malaiseAmount : malaiseAmount));
                unit = unit.WithHealthAndMaxHealth(Math.Min(unit.Health, maximum), maximum).WithStatusDamage(attack);
                return new StatusAttackDamageResult(unit, magnitude.Context);
            }
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
            if (delta <= 0 || status.Id != "buff" && status.Id != "debuff" && status.Id != "valor" && status.Id != "mass") return new StatusAttackDamageResult(unit, context);
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

        internal const string Malaise = "malaise";

        internal static string? ValidateMalaise(CombatUnit unit, CombatStatus status, CombatContext? context)
        {
            if (status.ParamInt != 1 || status.Stackable != true || status.Hidden != false || status.DisplayCategory != "Negative" ||
                status.Propagatable != false || status.TriggerVfxEnemy != false || status.TriggerVfxPlayer != false ||
                status.RemoveWhenTriggered || status.RemoveStackAtEnd || status.RemoveAllAtEnd || status.RemoveAfterPostCombat ||
                status.PreventRemovalDuringRelentless || status.SkipDuringDeployment || status.RemoveDuringDeployment ||
                status.RemoveWhenTriggeredAfterCardPlayed || status.RemoveAtEndOfTurnIfTriggered || status.MarkedForRemovalAfterCardPlayed)
                return "Malaise requires the captured native status definition.";
            if (unit.Modifiers?.DamageBuffFromStatusEffects.HasValue != true)
                return "Malaise requires the native status attack accumulator.";
            if (unit.Status("lifelink") != null || context?.LifeLinks?.Actors.Any(actor => actor.Id == unit.Id) == true)
                return "Malaise on linked units requires native maximum-health lifecycle evidence.";
            return null;
        }

        internal static bool ChangesMaximumHealth(string statusId) => statusId is "zeal" or Malaise;

        internal static RoomCombatResult HealthMembership(RoomCombatState source, CombatUnit unit, CombatStatus status,
            int delta, bool removing = false)
        {
            var scope = new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(actor => actor.Id == unit.Id ? unit : actor).ToArray(),
                source.ExternalInteractions, source.Context, source.Preview, source.ActiveCompanionBossIds);
            if (delta <= 0 || unchecked(status.ParamInt * delta) <= 0)
                return new RoomCombatResult(scope, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            // Native clips HP and settles Horde casualties before updating Deathwish.
            bool reducedMaximum = status.Id == Malaise ? !removing : removing;
            RoomCombatResult casualties = reducedMaximum ? HordeStatusModel.SettleHealth(scope, unit.Id) :
                new RoomCombatResult(scope, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            if (!casualties.Supported) return casualties;
            RoomCombatResult membership = HalfHealthDeathwishModel.Update(casualties.State!, unit.Id);
            return membership.Supported ? new RoomCombatResult(membership.State, membership.Outcome, 0,
                casualties.Events.Concat(membership.Events).ToList(),
                pendingCallbacks: casualties.PendingCallbacks.Concat(membership.PendingCallbacks).ToArray()) : membership;
        }
    }
}
