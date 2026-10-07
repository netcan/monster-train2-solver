using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitModifiers
    {
        public int AttackDamage { get; }
        public int AttackDamageAdded { get; }
        public int DamageBuff { get; }
        public int RawSize { get; }
        public int EquipmentLimit { get; }
        public bool CanBeHealed { get; }
        public bool IsClone { get; }
        public bool SpawnerMatchesDefinition { get; }
        public IReadOnlyList<CardUpgradeModifier> Upgrades { get; }
        public IReadOnlyList<StatisticCount> HealthFromUpgrades { get; }
        public UnitModifiers(int attackDamage, int attackDamageAdded, int damageBuff, int rawSize, int equipmentLimit,
            bool canBeHealed, bool isClone, IReadOnlyList<CardUpgradeModifier> upgrades, IReadOnlyList<StatisticCount>? healthFromUpgrades = null,
            bool spawnerMatchesDefinition = true)
        {
            AttackDamage = attackDamage; AttackDamageAdded = attackDamageAdded; DamageBuff = damageBuff; RawSize = rawSize;
            EquipmentLimit = equipmentLimit; CanBeHealed = canBeHealed; IsClone = isClone; Upgrades = Array.AsReadOnly(upgrades.ToArray());
            HealthFromUpgrades = Array.AsReadOnly((healthFromUpgrades ?? Array.Empty<StatisticCount>()).ToArray());
            SpawnerMatchesDefinition = spawnerMatchesDefinition;
        }
    }

    public static class UnitModifierModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, int targetId, CardUpgradeModifier upgrade, string lifetime,
            bool remove = false, int? roomCapacity = null, int sourceCardId = 0, string? triggerKind = null)
            => RoomCombatModel.ApplyUnitUpgrade(source, targetId, upgrade, lifetime, remove, roomCapacity, sourceCardId, triggerKind);

        // A running room engine settles deaths on its existing unit references and trigger flags.
        // Starting another engine here would reset preview triggers and detach combat attackers.
        internal static RoomCombatResult ApplyWithSettlement(RoomCombatState source, int targetId, CardUpgradeModifier upgrade,
            string lifetime, bool remove, int? roomCapacity, int sourceCardId, string? triggerKind,
            Func<RoomCombatState, CombatUnit, RoomCombatResult> settle, bool allowDyingTarget = false)
        {
            string? error = RoomCombatModel.Validate(source, allowDyingTarget ? targetId : (int?)null);
            if (error != null) return Unsupported(error);
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target?.Modifiers == null || source.Context?.CardInstances == null)
                return Unsupported("Unit upgrades require unit and card instance modifier state.");
            if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
            if (!remove && !new[] { "TemporaryUntilEndOfBattle", "TemporaryUntilUnitDeath", "Permanent" }.Contains(lifetime))
                return Unsupported("Unmodeled unit upgrade lifetime.");
            if (remove && upgrade.DataId.Length == 0) return Unsupported("Removing an upgrade requires a definition ID.");
            if (!remove && target.Modifiers.IsClone && upgrade.ExcludeFromClones) return Match(source);
            if (!remove)
            {
                UnitUpgradeScalingResult scaled = UnitUpgradeScalingModel.Apply(source.Context, sourceCardId, upgrade, triggerKind);
                if (!scaled.Supported) return Unsupported(scaled.UnsupportedReason!);
                upgrade = scaled.Upgrade!;
                source = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units, source.ExternalInteractions, scaled.Context, source.Preview);
            }
            foreach (CombatStatus status in upgrade.Statuses)
            {
                error = RoomCombatModel.Validate(new RoomCombatState(source.RoomIndex, source.Deployment,
                    new[] { CardSpellModel.Copy(target, target.Health, target.Statuses.Where(item => item.Id != status.Id).Concat(new[] { status }).ToArray()) },
                    Array.Empty<string>(), source.Context), allowDyingTarget ? targetId : (int?)null);
                if (error != null) return Unsupported(error);
            }
            if (!remove && upgrade.RestrictSizeToRoomCapacity && upgrade.Stats.Size > 0)
            {
                if (roomCapacity == null) return Unsupported("Restricted size upgrades require a room capacity definition.");
                if (source.Units.Where(unit => unit.Team == target.Team).Sum(unit => (long)unit.Size) > roomCapacity.Value - (long)upgrade.Stats.Size)
                    return Match(source);
            }
            RoomCombatState state = source;
            int count = remove ? target.Modifiers.Upgrades.Count(item => item.DataId == upgrade.DataId) : 1;
            for (int index = 0; index < count; index++)
            {
                target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
                if (target == null) return Match(state);
                UnitModifiers modifiers = target.Modifiers!;
                if (!remove && upgrade.Unique && upgrade.DataId.Length > 0 && modifiers.Upgrades.Any(item => item.DataId == upgrade.DataId)) break;
                var upgrades = modifiers.Upgrades.ToList();
                if (remove) upgrades.RemoveAt(upgrades.FindIndex(item => item.DataId == upgrade.DataId));
                else upgrades.Add(upgrade);
                int sign = remove ? -1 : 1;
                int damage = Math.Max(0, checked(modifiers.AttackDamage + sign * upgrade.Stats.Damage));
                int added = Math.Max(0, checked(modifiers.AttackDamageAdded + sign * upgrade.Stats.Damage));
                int buff = checked(modifiers.DamageBuff + sign * upgrade.DamageBuff);
                int size = checked(modifiers.RawSize + sign * upgrade.Stats.Size);
                int equipment = checked(modifiers.EquipmentLimit + sign * upgrade.Stats.EquipmentLimit);
                if (!remove) equipment = Math.Min(4, equipment);
                IReadOnlyList<CombatTrigger> triggers = UnitHealerModel.ApplyDamageUpgrade(target.Triggers, unchecked(sign * upgrade.Stats.Damage), source.Preview);
                int health = target.Health, maxHealth = target.MaxHealth;
                ChangeHealth(sign * upgrade.Stats.Health, !(remove && upgrade.Stats.Health > 0), !remove || upgrade.Stats.Health < 0);
                // A negative HP step exits the native application if the target is dead.
                // Positive steps can finish on an already-dying target, including statuses.
                bool partial = sign * upgrade.Stats.Health < 0 && health <= 0;
                if (!partial)
                {
                    ChangeHealth(sign * upgrade.UnhealedHealth, !remove, false);
                    partial = sign * upgrade.UnhealedHealth < 0 && health <= 0;
                }
                var statuses = target.Statuses.ToDictionary(status => status.Id);
                if (!partial)
                    foreach (CombatStatus status in upgrade.Statuses)
                    {
                        statuses.TryGetValue(status.Id, out CombatStatus? existing);
                        if (!remove && (target.StatusImmunities.Contains(status.Id) || target.Status("immune") != null)) continue;
                        int stacks = remove ? Math.Max(0, (existing?.Stacks ?? 0) - Math.Max(0, status.Stacks)) :
                            Math.Min(9999, checked((existing?.Stacks ?? 0) + status.Stacks));
                        if (stacks <= 0) statuses.Remove(status.Id); else statuses[status.Id] = (existing ?? status).WithStacks(stacks);
                    }
                var nextModifiers = new UnitModifiers(damage, added, buff, size, equipment, modifiers.CanBeHealed, modifiers.IsClone, upgrades,
                    modifiers.HealthFromUpgrades, modifiers.SpawnerMatchesDefinition);
                var changed = new CombatUnit(target.Id, target.AssetKey, target.Team, Math.Max(0, checked(damage + buff)), health, maxHealth,
                    target.CanAttack, target.IsPyre, target.EndsBattleOnDeath, statuses.Values.ToArray(), triggers, target.SpawnerCardId,
                    Math.Max(1, Math.Min(6, size)), target.StatusImmunities, target.Subtypes, nextModifiers, target.IsBoss);
                RoomCombatResult applied = settle(state, changed);
                if (!applied.Supported) return applied;
                state = applied.State!;
                // Failed additions retain their partial unit changes but never write back to
                // the spawner. Removal still processes all copies and clears the source card.
                if (partial && !remove) return Match(state);

                void ChangeHealth(int delta, bool decreaseHealth, bool heal)
                {
                    if (delta < 0)
                    {
                        maxHealth = Math.Max(0, checked(maxHealth + delta));
                        health = decreaseHealth ? Math.Max(0, checked(health + delta)) : Math.Min(health, maxHealth);
                    }
                    else if (delta > 0)
                    {
                        maxHealth = Math.Min(99999, checked(maxHealth + delta));
                        if (heal) health = HealingModel.HealedHealth(health, maxHealth, delta, modifiers.CanBeHealed,
                            target.Statuses, fromMaxHealthChange: true);
                    }
                }
            }
            // The native source-card update also follows a unique unit upgrade no-op.
            target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (!source.Preview && target?.SpawnerCardId > 0 &&
                (remove || target.Modifiers!.SpawnerMatchesDefinition && lifetime != "TemporaryUntilUnitDeath"))
            {
                CardInstanceState? card = state.Context!.FindCard(target.SpawnerCardId);
                if (card == null) return Unsupported("Missing upgraded unit's spawner card.");
                CardModifiers permanent = card.Permanent, temporary = card.Temporary;
                if (remove) temporary = new CardModifiers(temporary.Offsets,
                    temporary.Upgrades.Where(item => item.DataId != upgrade.DataId).ToArray(), temporary.PersistentHealth, temporary.ExternalInteractions);
                else if (lifetime == "Permanent") permanent = Add(permanent, upgrade);
                else temporary = Add(temporary, upgrade);
                var changed = new CardInstanceState(card.InstanceId, card.DataId, permanent, temporary, card.LastPlayedCost, card.LastForgedAmount, card.PlayCount, card.ExternalInteractions, card.EffectCounters, card.DamageScalingTraits, card.StatusScalingTraits, card.UnitUpgradeScalingTraits);
                CombatContext context = state.Context.WithCard(changed);
                state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions, context, state.Preview);
            }
            return Match(state);
        }
        internal static CardModifiers Add(CardModifiers modifiers, CardUpgradeModifier upgrade) =>
            upgrade.Unique && upgrade.DataId.Length > 0 && modifiers.Upgrades.Any(item => item.DataId == upgrade.DataId) ? modifiers :
            new CardModifiers(modifiers.Offsets, modifiers.Upgrades.Concat(new[] { upgrade }).ToArray(), modifiers.PersistentHealth, modifiers.ExternalInteractions);
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
