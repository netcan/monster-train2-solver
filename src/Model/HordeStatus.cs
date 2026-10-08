using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class HordeStatusModel
    {
        internal static RoomCombatResult Change(RoomCombatState source, CombatUnit before, CombatUnit after, int delta,
            bool suppressSpawnCallbacks = false)
        {
            if (delta == 0) return Match(Replace(source, after));
            if (before.HordeDefinition == null || before.HordeDefinition.Health <= 0 || before.Modifiers == null)
                return Unsupported("Horde changes require authored troop stats and raw attack state.");
            int total = after.RegisteredStatus("horde")?.Stacks ?? 0;
            HordeStats changed = HordeStatModel.Change(new HordeStats(before.Modifiers.AttackDamage, before.Health, before.MaxHealth),
                before.HordeDefinition, delta, total);
            UnitModifiers old = before.Modifiers;
            var modifiers = new UnitModifiers(changed.Attack, old.AttackDamageAdded, old.DamageBuff, old.RawSize,
                old.EquipmentLimit, old.CanBeHealed, old.IsClone, old.Upgrades, old.HealthFromUpgrades, old.SpawnerMatchesDefinition);
            var actor = new CombatUnit(after.Id, after.AssetKey, after.Team, Math.Max(0, unchecked(changed.Attack + old.DamageBuff)),
                changed.Health, changed.MaxHealth, after.CanAttack, after.IsPyre, after.EndsBattleOnDeath, after.Statuses,
                after.Triggers, after.SpawnerCardId, after.Size, after.StatusImmunities, after.Subtypes, modifiers, after.IsBoss,
                after.LastAttackerId, after.StatusRegistry, after.EquipmentCards, after.NextTriggerId, after.Ability,
                after.StatusDictionary, after.AbilityRules, after.HordeDefinition, after.IsSpawning, after.SacrificeCardId, after.DeathState, bumpRules: after.BumpRules);
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            CombatContext? context = source.Context;
            if (delta > 0 && !suppressSpawnCallbacks)
            {
                if (delta != total)
                    foreach (string kind in new[] { "OnSpawn", "OnUnscaledSpawn", "OnSpawnNotFromCard" })
                        callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(source.RoomIndex, actor, kind));
                foreach (CombatUnit other in source.Units.Where(unit => unit.Id != actor.Id).OrderBy(unit => unit.Team))
                    callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(source.RoomIndex, other, "CardMonsterPlayed", triggerCount: delta,
                        paramString: delta != total ? "" : null,
                        lastSpawnedOverrideUnitId: delta != total ? actor.Id : 0));
            }
            else if (delta < 0)
            {
                int count = -delta;
                if (!source.Preview && context?.Statistics != null)
                    for (int i = 0; i < count; i++)
                    {
                        BattleStatistics statistics = actor.SpawnerCardId > 0 ? context.LiveStatistics! : context.Statistics!;
                        context = context.WithStatistics(statistics.Death(actor.Team == CombatTeam.Player, actor.SpawnerCardId,
                            requireTrackedCard: context.CardInstances?.Count == 0));
                    }
                foreach (CombatUnit other in source.Units.Where(unit => unit.Id != actor.Id).OrderBy(unit => unit.Team))
                    foreach (string kind in new[] { "OnAnyMonsterDeathOnFloor", "OnAnyUnitDeathOnFloor" })
                        callbacks.Add(new RoomCombatModel.QueuedCharacterTrigger(source.RoomIndex, other, kind, dyingCharacter: actor, triggerCount: count));
            }
            var state = Replace(source, actor);
            return new RoomCombatResult(new RoomCombatState(state.RoomIndex, state.Deployment, state.Units,
                state.ExternalInteractions, context, state.Preview), RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: callbacks);
        }

        public static RoomCombatResult SettleHealth(RoomCombatState source, int targetId)
        {
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null) return Unsupported("Missing Horde casualty target.");
            int stacks = target.Status("horde")?.Stacks ?? 0;
            if (stacks <= 1) return Match(source);
            if (target.HordeDefinition == null || target.Modifiers == null) return Unsupported("Missing Horde casualty definition.");
            HordeCasualties casualties = HordeStatModel.Casualties(new HordeStats(target.Modifiers.AttackDamage, target.Health, target.MaxHealth),
                target.HordeDefinition, stacks);
            return casualties.ShouldRemove && casualties.RemovalCount > 0
                ? AbilityCooldownModel.RemoveStatus(source, targetId, "horde", casualties.RemovalCount) : Match(source);
        }
        internal static CombatUnit WithSpawning(CombatUnit unit, bool value) => new CombatUnit(unit.Id, unit.AssetKey, unit.Team,
            unit.BaseAttack, unit.Health, unit.MaxHealth, unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath, unit.Statuses, unit.Triggers,
            unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId,
            unit.StatusRegistry, unit.EquipmentCards, unit.NextTriggerId, unit.Ability, unit.StatusDictionary, unit.AbilityRules,
            unit.HordeDefinition, unit.IsSpawning.HasValue ? value : (bool?)null, unit.SacrificeCardId, unit.DeathState, bumpRules: unit.BumpRules);
        private static RoomCombatState Replace(RoomCombatState source, CombatUnit actor) => new RoomCombatState(source.RoomIndex,
            source.Deployment, source.Units.Select(unit => unit.Id == actor.Id ? actor : unit).ToArray(), source.ExternalInteractions, source.Context, source.Preview);
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string error) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
