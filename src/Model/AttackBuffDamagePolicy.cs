using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class AttackBuffDamagePolicy
    {
        // A positive Rage grant followed by direct damage can pay for an ally's
        // attack growth. Do not classify this narrow composition as an enemy
        // damage spell. Other mixed effects keep their existing seed ranking.
        private static bool Matches(CardPlayRule rule)
        {
            if (!rule.Effects.Concat(rule.OnCastEffects).Any(effect => effect.Type == "AddStatus" &&
                effect.Statuses.Any(status => status.Id == "buff" && status.Stacks > 0))) return false;
            var effects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
            return effects.Any(effect => effect.Type == "Damage") && effects.Any(effect => effect.Type == "AddStatus") &&
                effects.All(effect => effect.Target is "DropTargetCharacter" or "LastTargetedCharacters" && effect.AllowPlayer &&
                    (effect.Type == "Damage" && effect.Value > 0 || effect.Type == "AddStatus" && effect.Statuses.Count > 0 &&
                        effect.Statuses.All(status => status.Id == "buff" && status.Stacks > 0 && status.ParamInt > 0 && status.DisplayCategory == "Positive")));
        }

        internal static double? TargetValue(CardPlayRule rule, CombatUnit target)
        {
            if (!Matches(rule)) return null;
            if (target.Team != CombatTeam.Player || target.IsPyre || !target.CanAttack || target.Health <= 0 ||
                target.Status("fixedattack")?.Stacks > 0) return double.NegativeInfinity;
            double gain = rule.Effects.Concat(rule.OnCastEffects).Where(effect => effect.Type == "AddStatus")
                .SelectMany(effect => effect.Statuses).Sum(status => (double)status.Stacks * status.ParamInt) * target.Attacks;
            return gain * 100 + (double)target.Attack * target.Attacks;
        }

        internal static bool UsefulSuccessor(BattleTurnState source, PlayCardAction action, CardPlayRule rule, BattleTurnState result)
        {
            if (!Matches(rule)) return true;
            CombatUnit? before = source.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == action.TargetUnitId);
            CombatUnit? after = result.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == action.TargetUnitId);
            // Inspect the actual modeled result: selected-floor magic power,
            // shields and callbacks can change the damage cost or erase its gain.
            return before is { Team: CombatTeam.Player, IsPyre: false } && after is { Health: > 0 } &&
                (double)after.Attack * after.Attacks > (double)before.Attack * before.Attacks;
        }
    }
}
