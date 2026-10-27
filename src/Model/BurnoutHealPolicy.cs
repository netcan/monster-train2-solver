using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class BurnoutHealPolicy
    {
        // Healing plus Burnout can keep an existing damage dealer alive for
        // several more combats. Champion identity must not hide its expiry.
        private static bool Matches(CardPlayRule rule) => rule.CardType == "Spell" &&
            rule.IsAnyAbility != true && rule.Ability == null && rule.OnCastEffects.Count == 0 &&
            rule.Effects.Count == 2 && rule.Effects[0].Type == "Heal" && rule.Effects[0].Value > 0 &&
            rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].AllowPlayer &&
            rule.Effects[1].Type == "AddStatus" && rule.Effects[1].Target == "LastTargetedCharacters" &&
            rule.Effects[1].AllowPlayer && rule.Effects[1].Statuses.Count > 0 &&
            rule.Effects[1].Statuses.All(status => status.Id == "ephemeral" && status.Stacks > 0);

        internal static double? TargetValue(CardPlayRule rule, CombatUnit target)
        {
            if (!Matches(rule)) return null;
            if (target.Team != CombatTeam.Player || target.IsPyre || target.Health <= 0) return double.NegativeInfinity;
            int ticks = target.Status("ephemeral")?.Stacks ?? 0;
            int missing = Math.Max(0, target.MaxHealth - target.Health);
            if (ticks <= 0) return missing > 0 ? missing : double.NegativeInfinity;
            // The first pending expiry is urgent even on a full-health actor;
            // additional damage output breaks ties between equally urgent units.
            return 10000d / ticks + (target.CanAttack ? (double)target.Attack * target.Attacks * 10 : 0) + missing;
        }

        internal static bool UsefulSuccessor(BattleTurnState source, PlayCardAction action, CardPlayRule rule, BattleTurnState result)
        {
            if (!Matches(rule)) return true;
            CombatUnit? before = source.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == action.TargetUnitId);
            CombatUnit? after = result.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == action.TargetUnitId);
            // Judge the actual result, including healing restrictions and status
            // callbacks. Adding a death clock to an uninjured unit is not a gain.
            return before is { Team: CombatTeam.Player, IsPyre: false } && after is { Health: > 0 } &&
                (after.Health > before.Health || before.Status("ephemeral")?.Stacks > 0 &&
                    after.Status("ephemeral")?.Stacks > before.Status("ephemeral")!.Stacks);
        }
    }
}
