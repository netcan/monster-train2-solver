using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicMoonPhaseHealRule
    {
        public int EffectIndex { get; }
        public bool TargetPlayer { get; }
        public bool TargetEnemy { get; }
        public string TargetMode { get; }
        public string? RequiredSubtype { get; }
        public int HealthAmount { get; }
        public int MoonPhaseMask { get; }
        public int ConditionCount { get; }

        public RelicMoonPhaseHealRule(int effectIndex, bool targetPlayer, bool targetEnemy,
            string targetMode, string? requiredSubtype, int healthAmount, int moonPhaseMask, int conditionCount)
        {
            EffectIndex = effectIndex; TargetPlayer = targetPlayer; TargetEnemy = targetEnemy;
            TargetMode = targetMode; RequiredSubtype = requiredSubtype; HealthAmount = healthAmount;
            MoonPhaseMask = moonPhaseMask; ConditionCount = conditionCount;
        }
    }

    internal sealed class MoonPhaseHealResult
    {
        public TrainCombatState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal MoonPhaseHealResult(TrainCombatState? state, string? error = null)
        { State = state; UnsupportedReason = error; }
    }

    internal static class RelicMoonPhaseHealModel
    {
        internal const string Effect = "RelicEffectHealOnMoonPhase";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.MoonPhaseHealRules?.Count > 0 ? "Unexpected relic moon-phase healing rules." : null;
            if (relic.MoonPhaseHealRules == null || !indices.SequenceEqual(relic.MoonPhaseHealRules.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic moon-phase healing definitions.";
            foreach (RelicMoonPhaseHealRule rule in relic.MoonPhaseHealRules)
            {
                if ((!rule.TargetPlayer && !rule.TargetEnemy) || rule.TargetMode != "WeakestAllRooms" ||
                    rule.HealthAmount < 0 || rule.MoonPhaseMask == 0 || rule.ConditionCount != 0 ||
                    rule.EffectIndex < 0 || rule.EffectIndex >= relic.EffectTypes.Count)
                    return "Unmodeled native relic moon-phase healing rule.";
            }
            return null;
        }

        internal static string? ValidateTransition(TrainCombatState source, int phase)
        {
            if (source.Context?.Relics == null) return null;
            foreach (CombatRelicState relic in source.Context.Relics)
            {
                string? error = Validate(relic);
                if (error != null) return error;
                foreach (RelicMoonPhaseHealRule rule in relic.MoonPhaseHealRules ?? Array.Empty<RelicMoonPhaseHealRule>())
                {
                    if (!Applies(rule, phase)) continue;
                    CombatUnit? target = Weakest(source, rule);
                    if (target?.Triggers.Any(trigger => trigger.Kind == "OnHeal") == true)
                        return "Moon-phase relic healing OnHeal callbacks are not modeled.";
                }
            }
            return null;
        }

        internal static MoonPhaseHealResult Apply(TrainCombatState source, int phase)
        {
            if (source.Context?.Relics == null) return new MoonPhaseHealResult(source);
            TrainCombatState state = source;
            foreach (CombatRelicState relic in state.Context!.Relics)
            {
                string? error = Validate(relic);
                if (error != null) return Fail(error);
                foreach (RelicMoonPhaseHealRule rule in relic.MoonPhaseHealRules ?? Array.Empty<RelicMoonPhaseHealRule>())
                {
                    if (!Applies(rule, phase)) continue;
                    CombatUnit? target = Weakest(state, rule);
                    if (target == null) continue;
                    if (target.Triggers.Any(trigger => trigger.Kind == "OnHeal"))
                        return Fail("Moon-phase relic healing OnHeal callbacks are not modeled.");
                    if (target.Modifiers?.CanBeHealed == false) continue;
                    int modified = HealingModel.ModifiedAmount(rule.HealthAmount, target.Statuses);
                    if (modified < 0) continue;
                    int health = target.Health + Math.Min(modified, target.MaxHealth - target.Health);
                    if (health == target.Health) continue;
                    RoomCombatState[] rooms = state.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                        room.Units.Select(unit => unit.Id == target.Id ? unit.WithHealth(health) : unit).ToArray(),
                        room.ExternalInteractions, room.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
                    state = new TrainCombatState(rooms, state.Movement, state.EnemySlotsPerRoom, state.Context);
                }
            }
            return new MoonPhaseHealResult(state);
        }

        private static bool Applies(RelicMoonPhaseHealRule rule, int phase) =>
            rule.MoonPhaseMask == -1 || (rule.MoonPhaseMask & phase) != 0;

        private static CombatUnit? Weakest(TrainCombatState source, RelicMoonPhaseHealRule rule)
        {
            CombatUnit? weakest = null;
            foreach (CombatUnit unit in source.Rooms.OrderByDescending(room => room.RoomIndex).SelectMany(room => room.Units))
            {
                bool team = unit.Team == CombatTeam.Player ? rule.TargetPlayer : rule.TargetEnemy;
                if (!team || unit.IsPyre || unit.Health <= 0 || unit.Health >= unit.MaxHealth ||
                    unit.Status("untouchable")?.Stacks > 0 ||
                    rule.RequiredSubtype != null && !unit.Subtypes.Contains(rule.RequiredSubtype)) continue;
                if (weakest == null || unit.Health < weakest.Health) weakest = unit;
            }
            return weakest;
        }

        private static MoonPhaseHealResult Fail(string error) => new MoonPhaseHealResult(null, error);
    }
}
