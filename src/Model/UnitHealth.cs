using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class UnitHealthModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, int targetId, int amount, bool debuff = false,
            string lifetime = "TemporaryUntilEndOfBattle", Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null) =>
            ApplyCore(source, targetId, amount, debuff, lifetime, refreshPyre, heal: true, triggerOnHeal: false, floor: 0,
                decreaseHp: true, recordSpawner: true, captureAllDispatches: false);

        // Native CharacterState APIs do not write the card-effect spawner offset.
        internal static RoomCombatResult ApplyNative(RoomCombatState source, int targetId, int amount, bool debuff = false,
            bool heal = true, bool triggerOnHeal = false, int floor = 1, bool decreaseHp = true, bool captureAllDispatches = false) =>
            ApplyCore(source, targetId, amount, debuff, "TemporaryUntilEndOfBattle", null, heal, triggerOnHeal, floor, decreaseHp, false, captureAllDispatches);

        private static RoomCombatResult ApplyCore(RoomCombatState source, int targetId, int amount, bool debuff, string lifetime,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre, bool heal, bool triggerOnHeal, int floor,
            bool decreaseHp, bool recordSpawner, bool captureAllDispatches)
        {
            string? error = RoomCombatModel.Validate(source, halfHealthLifecycle: true);
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || target == null) return Unsupported(error ?? "Missing maximum-health target.");
            if (floor < 0 || floor > 99999) return Unsupported("Invalid native maximum-health floor.");
            if (!debuff && lifetime != "" && lifetime != "TemporaryUntilEndOfBattle" && lifetime != "TemporaryUntilUnitDeath")
                return Unsupported("Unmodeled maximum-health buff lifetime.");
            if (!debuff && heal && amount > 0 && target.Modifiers == null) return Unsupported("Maximum-health healing requires healability state.");
            bool halfHealth = source.Units.Any(unit => unit.Triggers.Any(HalfHealthDeathwishModel.IsTrigger));
            CombatContext? context = source.Context;
            // Native records the raw signed amount on the spawner, even when the unit does not change.
            if (recordSpawner && !debuff)
            {
                context = RecordSpawnerBuff(context, target, amount, lifetime, source.Preview, out string? spawnerError);
                if (spawnerError != null) return Unsupported(spawnerError);
            }
            var state = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units,
                source.ExternalInteractions, context, source.Preview, source.ActiveCompanionBossIds);
            if (amount <= 0) return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            int maxHealth = debuff ? Math.Max(floor, checked(target.MaxHealth - amount)) : Math.Min(99999, checked(target.MaxHealth + amount));
            bool lifecycle = halfHealth || triggerOnHeal;
            int health = debuff ? decreaseHp ? Math.Max(0, checked(target.Health - amount)) : Math.Min(target.Health, maxHealth) :
                heal && !lifecycle ? HealingModel.HealedHealth(target.Health, maxHealth, amount, target.Modifiers!.CanBeHealed,
                    target.Statuses, fromMaxHealthChange: true) : target.Health;
            CombatUnit changed = target.WithHealthAndMaxHealth(health, maxHealth);
            // Lethal native debuffs sacrifice the unit without attributing damage to the played card.
            if (health <= 0 && halfHealth && source.Preview)
                return Unsupported("Half-health Deathwish lethal maximum-health preview changes require native sacrifice/revival calibration.");
            if (health <= 0) return RoomCombatModel.ApplyUnitModification(state, changed, refreshPyre: refreshPyre);
            var completed = new RoomCombatState(state.RoomIndex, state.Deployment,
                state.Units.Select(unit => unit.Id == targetId ? changed : unit).ToArray(),
                state.ExternalInteractions, state.Context, state.Preview, state.ActiveCompanionBossIds);
            RoomCombatResult settled = debuff ? HordeStatusModel.SettleHealth(completed, targetId) :
                new RoomCombatResult(completed, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            if (!settled.Supported || !lifecycle) return settled;
            RoomCombatResult lifecycleResult = RoomCombatModel.ApplyMaximumHealthLifecycle(settled.State!, targetId, amount, debuff, heal, triggerOnHeal,
                captureAllDispatches, refreshPyre);
            if (!lifecycleResult.Supported) return lifecycleResult;
            // DebuffMaxHP removes Horde casualties before TryUpdateDeathwish.
            // Those observer callbacks remain queued ahead of the later changes.
            return new RoomCombatResult(lifecycleResult.State, lifecycleResult.Outcome, lifecycleResult.Rounds,
                settled.Events.Concat(lifecycleResult.Events).ToList(),
                pendingCallbacks: settled.PendingCallbacks.Concat(lifecycleResult.PendingCallbacks).ToArray(),
                retainedUnits: settled.RetainedUnits.Concat(lifecycleResult.RetainedUnits).ToArray(),
                dispatches: settled.Dispatches.Concat(lifecycleResult.Dispatches).ToArray());
        }
        internal static CombatContext? RecordSpawnerBuff(CombatContext? context, CombatUnit target, int amount, string lifetime,
            bool preview, out string? error)
        {
            error = null;
            if (preview || lifetime == "TemporaryUntilUnitDeath" || target.SpawnerCardId <= 0) return context;
            CardInstanceState? card = context?.FindCard(target.SpawnerCardId);
            if (card == null) { error = "Missing maximum-health buff spawner card."; return context; }
            CardStatModifier old = card.Temporary.Offsets;
            var offsets = new CardStatModifier(old.Damage, checked(old.Health + amount), old.Cost, old.Heal, old.Size,
                old.XCost, old.EquipmentLimit, old.UpgradeSlotCount);
            var temporary = new CardModifiers(offsets, card.Temporary.Upgrades, card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
            return context!.WithCard(card.WithModifiers(card.Permanent, temporary, refreshTraits: false));
        }
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
