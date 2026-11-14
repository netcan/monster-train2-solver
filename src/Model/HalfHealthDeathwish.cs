using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class HalfHealthDeathwishModel
    {
        internal static bool IsTrigger(CombatTrigger trigger) => trigger.ActivationCondition == "HalfHealth" &&
            (trigger.Kind == "OnDeathwish" || trigger.Kind == "OnDeathwishLost");

        // Native uses Single precision and permits zero HP before CheckForDeath.
        internal static bool Eligible(CombatUnit unit) => unit.MaxHealth > 0 &&
            (float)Math.Max(0, unit.Health) / (float)unit.MaxHealth <= 0.5f &&
            (unit.Status("silenced")?.Stacks ?? 0) <= 0 && (unit.Status("muted")?.Stacks ?? 0) <= 0;

        // CharacterState.TryUpdateDeathwish is a local operation. Unlike a room
        // enchantment pass it neither filters out dying actors nor checks the
        // manager's AllowEnchantmentUpdates/reentry flags.
        internal static RoomCombatResult Update(RoomCombatState source, int unitId, CombatContext? queueContext = null)
        {
            CombatUnit? unit = source.Units.SingleOrDefault(actor => actor.Id == unitId);
            if (unit == null) return Fail("Missing half-health Deathwish actor.");
            CombatTrigger[] triggers = unit.Triggers.ToArray();
            bool eligible = Eligible(unit);
            var pending = new List<int>();
            foreach (int index in Enumerable.Range(0, triggers.Length).Where(index => IsTrigger(triggers[index])))
            {
                CombatTrigger trigger = triggers[index];
                if (!trigger.StateId.HasValue || !trigger.DeathwishActive.HasValue)
                    return Fail("Half-health Deathwish requires captured source membership and persistent trigger identity.");
                if (trigger.DeathwishActive.Value == eligible) continue;
                triggers[index] = trigger.WithDeathwishActive(eligible);
                pending.Add(trigger.StateId.Value);
            }
            if (pending.Count == 0) return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            unit = unit.WithTriggers(triggers);
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (int id in pending)
            {
                var queued = new RoomCombatModel.QueuedCharacterTrigger(source.RoomIndex, unit,
                    eligible ? "OnDeathwish" : "OnDeathwishLost", canFireTriggers: true, triggerCount: 1, exclusiveTriggerStateId: id);
                string? error = StatusCallbackModel.Admit(queueContext ?? source.Context, queued, out RoomCombatModel.QueuedCharacterTrigger admitted);
                if (error != null) return Fail(error);
                if (admitted.Admission != RoomCombatModel.CharacterTriggerAdmission.Rejected) callbacks.Add(admitted);
            }
            return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(actor => actor.Id == unitId ? unit : actor).ToArray(), source.ExternalInteractions,
                source.Context, source.Preview, source.ActiveCompanionBossIds), RoomOutcome.Exchanged, 0, new List<CombatEvent>(),
                pendingCallbacks: callbacks);
        }

        private static RoomCombatResult Fail(string error) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
