using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class UnitAttackModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, int targetId, int amount, bool debuff = false)
        {
            string? error = RoomCombatModel.Validate(source);
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || target == null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0,
                new List<CombatEvent>(), error ?? "Missing attack-change target.");
            // Native negative/zero values do nothing; incapable units are never changed.
            if (!target.CanAttack || amount <= 0) return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            UnitModifiers? old = target.Modifiers;
            if (old == null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Attack changes require the raw unit attack state.");
            int buff = checked(old.DamageBuff + (debuff ? -amount : amount));
            var modifiers = new UnitModifiers(old.AttackDamage, old.AttackDamageAdded, buff, old.RawSize, old.EquipmentLimit,
                old.CanBeHealed, old.IsClone, old.Upgrades, old.HealthFromUpgrades, old.SpawnerMatchesDefinition);
            var changed = new CombatUnit(target.Id, target.AssetKey, target.Team, Math.Max(0, checked(old.AttackDamage + buff)),
                target.Health, target.MaxHealth, target.CanAttack, target.IsPyre, target.EndsBattleOnDeath, target.Statuses, target.Triggers,
                target.SpawnerCardId, target.Size, target.StatusImmunities, target.Subtypes, modifiers, target.IsBoss, target.LastAttackerId, target.StatusRegistry, target.EquipmentCards, target.NextTriggerId, target.Ability, target.StatusDictionary, target.AbilityRules, target.HordeDefinition, target.IsSpawning);
            return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(unit => unit.Id == targetId ? changed : unit).ToArray(), source.ExternalInteractions, source.Context, source.Preview),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        }
    }
}
