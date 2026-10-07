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

        // Native removal retains the zero dictionary entry and defers callbacks until queue drain.
        internal static RoomCombatResult RemoveStatus(RoomCombatState source, int targetId, string id, int amount, int sourceCardId = 0)
        {
            CombatUnit target = source.Units.First(unit => unit.Id == targetId);
            CombatStatus? status = target.RegisteredStatus(id);
            if (status == null) return Match(source);
            int count = amount == -1 ? 0 : Math.Max(0, Math.Min(status.Stackable == false ? 1 : 9999, unchecked(status.Stacks - amount)));
            if (id == "horde" && status.Stacks > 0 && count == 0)
                return Unsupported("Removing the final Horde troop requires physical-death settlement.");
            CombatUnit changed = CardSpellModel.Copy(target, target.Health, target.Statuses.Where(item => item.Id != id)
                .Concat(count > 0 ? new[] { status.WithStacks(count) } : Array.Empty<CombatStatus>()).ToArray());
            CombatContext? context = source.Context;
            if (!source.Preview && sourceCardId > 0 && count < status.Stacks && context?.Statistics != null)
                context = context.WithStatistics(context.LiveStatistics!.Increment(sourceCardId, "AnyStatusEffectStacksRemoved", status.Stacks - count,
                    requireTrackedCard: context.CardInstances?.Count == 0));
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            if (id == "horde" && count < status.Stacks)
            {
                var staged = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units, source.ExternalInteractions, context, source.Preview);
                RoomCombatResult horde = HordeStatusModel.Change(staged, target, changed, count - status.Stacks);
                if (!horde.Supported) return horde;
                changed = horde.State!.Units.First(unit => unit.Id == targetId);
                context = horde.State.Context; callbacks.AddRange(horde.PendingCallbacks);
            }
            StatusCallbackModel.Removed(source.RoomIndex, target, changed, id, callbacks);
            return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(unit => unit.Id == targetId ? changed : unit).ToArray(), source.ExternalInteractions, context, source.Preview),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: callbacks);
        }
        internal static CombatUnit Copy(CombatUnit unit, UnitAbilityState? ability) => new CombatUnit(unit.Id, unit.AssetKey, unit.Team,
            unit.BaseAttack, unit.Health, unit.MaxHealth, unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath, unit.Statuses, unit.Triggers,
            unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId,
            unit.StatusRegistry, unit.EquipmentCards, unit.NextTriggerId, ability, unit.StatusDictionary, unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning);
        private static RoomCombatState Replace(RoomCombatState source, CombatUnit unit) => new RoomCombatState(source.RoomIndex,
            source.Deployment, source.Units.Select(item => item.Id == unit.Id ? unit : item).ToArray(), source.ExternalInteractions, source.Context, source.Preview);
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
