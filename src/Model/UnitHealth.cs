using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class UnitHealthModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, int targetId, int amount, bool debuff = false,
            string lifetime = "TemporaryUntilEndOfBattle")
        {
            string? error = RoomCombatModel.Validate(source);
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || target == null) return Unsupported(error ?? "Missing maximum-health target.");
            if (!debuff && lifetime != "" && lifetime != "TemporaryUntilEndOfBattle" && lifetime != "TemporaryUntilUnitDeath")
                return Unsupported("Unmodeled maximum-health buff lifetime.");
            if (!debuff && amount > 0 && target.Modifiers == null) return Unsupported("Maximum-health healing requires healability state.");
            CombatContext? context = source.Context;
            // Native records the raw signed amount on the spawner, even when the unit does not change.
            if (!debuff && !source.Preview && lifetime != "TemporaryUntilUnitDeath" && target.SpawnerCardId > 0)
            {
                CardInstanceState? card = context?.FindCard(target.SpawnerCardId);
                if (card == null) return Unsupported("Missing maximum-health buff spawner card.");
                CardStatModifier old = card.Temporary.Offsets;
                var offsets = new CardStatModifier(old.Damage, checked(old.Health + amount), old.Cost, old.Heal, old.Size,
                    old.XCost, old.EquipmentLimit, old.UpgradeSlotCount);
                var temporary = new CardModifiers(offsets, card.Temporary.Upgrades, card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
                context = context!.WithCard(new CardInstanceState(card.InstanceId, card.DataId, card.Permanent, temporary,
                    card.LastPlayedCost, card.LastForgedAmount, card.PlayCount, card.ExternalInteractions, card.EffectCounters, card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits, card.CapacityScalingTraits));
            }
            var state = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units, source.ExternalInteractions, context, source.Preview);
            if (amount <= 0) return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            int maxHealth = debuff ? Math.Max(0, checked(target.MaxHealth - amount)) : Math.Min(99999, checked(target.MaxHealth + amount));
            int health = debuff ? Math.Max(0, checked(target.Health - amount)) :
                HealingModel.HealedHealth(target.Health, maxHealth, amount, target.Modifiers!.CanBeHealed, target.Statuses, fromMaxHealthChange: true);
            var changed = new CombatUnit(target.Id, target.AssetKey, target.Team, target.BaseAttack, health, maxHealth,
                target.CanAttack, target.IsPyre, target.EndsBattleOnDeath, target.Statuses, target.Triggers, target.SpawnerCardId,
                target.Size, target.StatusImmunities, target.Subtypes, target.Modifiers, target.IsBoss, target.LastAttackerId, target.StatusRegistry);
            // Lethal native debuffs sacrifice the unit without attributing damage to the played card.
            if (health <= 0) return RoomCombatModel.ApplyUnitModification(state, changed);
            return new RoomCombatResult(new RoomCombatState(state.RoomIndex, state.Deployment,
                state.Units.Select(unit => unit.Id == targetId ? changed : unit).ToArray(), state.ExternalInteractions, state.Context, state.Preview),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        }
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
