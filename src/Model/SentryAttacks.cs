using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class SentryState
    {
        public IReadOnlyList<int> Targets { get; }
        public bool IsAttacking { get; }
        public SentryState(IReadOnlyList<int> targets, bool isAttacking = false)
        { Targets = Array.AsReadOnly(targets.ToArray()); IsAttacking = isAttacking; }
    }
    internal static class SentryAttackModel
    {
        internal static TrainCombatResult UpdateAll(TrainCombatState source)
        {
            var results = new List<RoomCombatResult>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            foreach (int id in source.Rooms.SelectMany(room => room.Units).Where(unit => unit.Modifiers?.Sentry != null).Select(unit => unit.Id).ToArray())
            {
                RoomCombatState? room = source.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == id));
                CombatUnit? actor = room?.Units.Single(unit => unit.Id == id);
                SentryState? sentry = actor?.Modifiers?.Sentry;
                if (actor == null || actor.Health <= 0 || actor.DeathState?.IsDestroyed == true || sentry!.IsAttacking || sentry.Targets.Count == 0 ||
                    room!.Units.Any(unit => unit.Modifiers?.Sentry?.IsAttacking == true)) continue;
                if (actor.Status("inert") != null || actor.Status("restricted") != null)
                    return new TrainCombatResult(null, RoomOutcome.Unsupported, results, "Sentry attack-prevention removal needs a native witness.");
                if (sentry.Targets.Any(target => !room.Units.Any(unit => unit.Id == target)))
                    return new TrainCombatResult(null, RoomOutcome.Unsupported, results, "Sentry remote or removed cached targets need a native witness.");
                source = Replace(source, id, new SentryState(sentry.Targets, true));
                TrainSpellResult attack = CardSpellModel.ApplyUnitAttackTurn(source, room.RoomIndex, id,
                    (context, placement) => RelicResourceModel.RefreshPyreAttack(source.Rooms.ToArray(), context, placement),
                    ignoreAttackPrevention: false, overrideTargets: sentry.Targets);
                if (!attack.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported, results, attack.UnsupportedReason);
                TrainCombatResult callbacks = TrainCombatModel.ApplyCharacterQueue(attack.State!, attack.PendingCallbacks.ToList());
                if (!callbacks.Supported) return callbacks;
                source = Replace(callbacks.State!, id, new SentryState(Array.Empty<int>()));
                results.Add(new RoomCombatResult(source.Rooms.Single(current => current.RoomIndex == room.RoomIndex),
                    attack.Outcome, 0, attack.Events.ToList()));
                results.AddRange(callbacks.RoomResults);
                if (attack.Outcome != RoomOutcome.Exchanged) outcome = attack.Outcome;
            }
            return new TrainCombatResult(source, outcome, results);
        }
        private static TrainCombatState Replace(TrainCombatState source, int id, SentryState state) =>
            EnchantmentWorldModel.Rebase(new TrainCombatState(source.Rooms.Select(room => room.WithUnits(room.Units.Select(unit =>
                unit.Id == id ? unit.WithModifiers(unit.Modifiers!.WithSentry(state)) : unit).ToArray())).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, source.Context));
    }
}
