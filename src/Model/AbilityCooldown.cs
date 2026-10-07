using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitAbilityState
    {
        public string DataId { get; }
        public int Cooldown { get; }
        public int CooldownAtSpawn { get; }
        public bool FromEquipment { get; }
        public bool Resolving { get; }
        public string? PreviousDataId { get; }
        public CardCreationRule? CardCreation { get; }
        public UnitAbilityDefinition? Definition { get; }
        public UnitAbilityDefinition? PreviousDefinition { get; }
        public bool HasAbility => DataId.Length > 0;
        public UnitAbilityState(string dataId, int cooldown, int cooldownAtSpawn, bool fromEquipment = false,
            bool resolving = false, string? previousDataId = null, CardCreationRule? cardCreation = null,
            UnitAbilityDefinition? definition = null, UnitAbilityDefinition? previousDefinition = null)
        { DataId = dataId; Cooldown = cooldown; CooldownAtSpawn = cooldownAtSpawn; FromEquipment = fromEquipment;
            Resolving = resolving; PreviousDataId = previousDataId; CardCreation = cardCreation; Definition = definition; PreviousDefinition = previousDefinition; }
        internal UnitAbilityState WithCooldown(int value) => new UnitAbilityState(DataId, Math.Max(1, value), CooldownAtSpawn,
            FromEquipment, Resolving, PreviousDataId, CardCreation, Definition, PreviousDefinition);
        internal UnitAbilityState WithResolving(bool value) => new UnitAbilityState(DataId, Cooldown, CooldownAtSpawn,
            FromEquipment, value, PreviousDataId, CardCreation, Definition, PreviousDefinition);
    }

    public static class AbilityCooldownModel
    {
        internal static bool IsEffect(string type) => type == "ResetCooldown" || type == "AdjustAbilityCooldown";
        public static RoomCombatResult Apply(RoomCombatState source, int targetId, CardActionEffect effect, int sourceCardId = 0, string? triggerKind = null)
        {
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null || !IsEffect(effect.Type) || !effect.CooldownParameter.HasValue)
                return Unsupported("Missing ability cooldown target or effect parameters.");
            if (effect.Type == "ResetCooldown" && target.Status("horde") != null && triggerKind == "OnSpawn")
            {
                if (!target.IsSpawning.HasValue) return Unsupported("Horde OnSpawn cooldown requires spawning state.");
                if (!target.IsSpawning.Value) return Match(source);
            }
            UnitAbilityState ability = target.Ability ?? new UnitAbilityState("", 0, 0);
            int stacks = target.Status("cooldown")?.Stacks ?? 0;
            if (effect.Type == "ResetCooldown")
            {
                int desired = effect.CooldownParameter.Value ? ability.CooldownAtSpawn : ability.Cooldown;
                int delta = unchecked(desired - stacks);
                if (delta <= 0) return Match(source);
                CombatStatus? status = source.Context?.StatusRules.FirstOrDefault(rule => rule.Id == "cooldown");
                if (status == null) return Unsupported("Missing cooldown status definition.");
                return StatusApplicationModel.ApplyRetained(source, targetId, status.WithStacks(delta), sourceCardId,
                    overrideImmunity: true);
            }
            if (!effect.CooldownParameter.Value && ability.Cooldown <= 0 && effect.Value <= 0) return Match(source);
            int raw = effect.CooldownParameter.Value ? effect.Value : unchecked(ability.Cooldown + effect.Value);
            CombatUnit changed = Copy(target, ability.WithCooldown(raw));
            RoomCombatState state = Replace(source, changed);
            return stacks > raw ? RemoveStatus(state, targetId, "cooldown", unchecked(stacks - raw)) : Match(state);
        }

        internal static RoomCombatResult RemoveStatus(RoomCombatState source, int targetId, string id, int amount, int sourceCardId = 0)
            => StatusRemovalModel.Remove(source, targetId, id, amount, sourceCardId);
        internal static CombatUnit Copy(CombatUnit unit, UnitAbilityState? ability) => new CombatUnit(unit.Id, unit.AssetKey, unit.Team,
            unit.BaseAttack, unit.Health, unit.MaxHealth, unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath, unit.Statuses, unit.Triggers,
            unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId,
            unit.StatusRegistry, unit.EquipmentCards, unit.NextTriggerId, ability, unit.StatusDictionary, unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState);
        private static RoomCombatState Replace(RoomCombatState source, CombatUnit unit) => new RoomCombatState(source.RoomIndex,
            source.Deployment, source.Units.Select(item => item.Id == unit.Id ? unit : item).ToArray(), source.ExternalInteractions, source.Context, source.Preview);
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
