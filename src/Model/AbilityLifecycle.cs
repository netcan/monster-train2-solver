using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitAbilityDefinition
    {
        public string DataId { get; }
        public bool IsUnitAbility { get; }
        public int Cooldown { get; }
        public int CooldownAtSpawn { get; }
        public CardCreationRule? CardCreation { get; }
        public UnitAbilityDefinition(string dataId, bool isUnitAbility, int cooldown, int cooldownAtSpawn, CardCreationRule? cardCreation)
        { DataId = dataId; IsUnitAbility = isUnitAbility; Cooldown = cooldown; CooldownAtSpawn = cooldownAtSpawn; CardCreation = cardCreation; }
    }
    public sealed class UnitAbilityRules
    {
        public bool PreventEquipmentAbilities { get; }
        public IReadOnlyList<CombatStatus> AuthoredStartingStatuses { get; }
        public UnitAbilityRules(bool preventEquipmentAbilities, IReadOnlyList<CombatStatus> authoredStartingStatuses)
        { PreventEquipmentAbilities = preventEquipmentAbilities; AuthoredStartingStatuses = Array.AsReadOnly(authoredStartingStatuses.ToArray()); }
    }
    public sealed class AbilityChangeRule
    {
        public UnitAbilityDefinition? Definition { get; }
        public bool FromEquipment { get; }
        public bool Permanent { get; }
        public bool BlockedByRelic { get; }
        public IReadOnlyList<CombatTrigger> CommonTriggers { get; }
        public AbilityChangeRule(UnitAbilityDefinition? definition, bool fromEquipment, bool permanent, IReadOnlyList<CombatTrigger> commonTriggers,
            bool blockedByRelic = false)
        { Definition = definition; FromEquipment = fromEquipment; Permanent = permanent; CommonTriggers = Array.AsReadOnly(commonTriggers.ToArray());
            BlockedByRelic = blockedByRelic; }
    }
    public static class AbilityLifecycleModel
    {
        internal const string CommonOrigin = "UnitAbilityCommonData";
        internal static bool IsEffect(string type) => type == "SetUnitAbility" || type == "RemoveAbility";
        internal static UnitAbilityState? Normalize(UnitAbilityState? state) => state == null ||
            !state.HasAbility && state.Cooldown == 0 && state.CooldownAtSpawn == 0 && !state.FromEquipment &&
            !state.Resolving && state.PreviousDataId == null ? null : state;

        public static RoomCombatResult Apply(RoomCombatState source, int unitId, AbilityChangeRule rule, bool remove,
            bool deferCallbacks = false)
        {
            if (remove && rule.BlockedByRelic) return Match(source);
            CombatUnit? actor = source.Units.FirstOrDefault(unit => unit.Id == unitId);
            if (actor == null) return Unsupported("Missing ability lifecycle target.");
            if (!remove && rule.Definition?.IsUnitAbility != true || remove && actor.Ability?.HasAbility != true) return Match(source);
            if (!remove && rule.FromEquipment)
            {
                if (actor.AbilityRules == null) return Unsupported("Equipment ability changes require captured character restrictions.");
                if (actor.AbilityRules.PreventEquipmentAbilities) return Match(source);
            }
            string? error = RoomCombatModel.Validate(source);
            if (error != null) return Unsupported(error);
            if (source.Context == null || rule.CommonTriggers.Count == 0 ||
                rule.CommonTriggers.Select(trigger => trigger.Kind).Distinct().Count() != rule.CommonTriggers.Count ||
                rule.CommonTriggers.Any(trigger => trigger.Kind == "OnDeathwishLost"))
                return Unsupported("Missing or unsupported native common ability triggers.");
            if (remove && rule.Permanent && source.Context.PermanentlyDisabledAbilities == null)
                return Unsupported("Permanent ability removal requires the captured run disable list.");
            UnitAbilityState? previous = actor.Ability;
            UnitAbilityDefinition? restored = previous?.PreviousDefinition;
            UnitAbilityDefinition? remembered = !remove && rule.FromEquipment
                ? previous?.FromEquipment == true ? restored : previous?.Definition : null;
            if (!remove && rule.FromEquipment && previous?.HasAbility == true && !previous.FromEquipment && remembered == null ||
                remove && previous?.FromEquipment == true && previous.PreviousDataId != null && restored == null)
                return Unsupported("Ability restoration requires the original definition, including its unmodified cooldowns.");
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            RoomCombatState state = source;
            var events = new List<CombatEvent>(); RoomOutcome outcome = RoomOutcome.Exchanged;
            RoomCombatResult result = Match(state);
            if (!remove && previous != null)
            {
                var history = new UnitAbilityState(previous.DataId, previous.Cooldown, previous.CooldownAtSpawn,
                    previous.FromEquipment, previous.Resolving, remembered?.DataId, previous.CardCreation, previous.Definition, remembered);
                state = Replace(state, Copy(actor, Normalize(history), actor.Triggers, actor.NextTriggerId));
            }
            if (previous?.HasAbility == true)
            {
                foreach (string id in new[] { "unit_ability", "unit_ability_available", "cooldown" })
                {
                    RoomCombatResult removed = AbilityCooldownModel.RemoveStatus(state, unitId, id, -1);
                    if (!removed.Supported) return removed;
                    state = removed.State!; callbacks.AddRange(removed.PendingCallbacks);
                }
                actor = state.Units.First(unit => unit.Id == unitId);
                // The live API yields its first common-trigger removal before continuing.
                // The main combat loop can settle status callbacks during that yield;
                // newly installed triggers must not receive those earlier callbacks.
                if (!source.Preview && !deferCallbacks)
                {
                    string first = rule.CommonTriggers[0].Kind;
                    state = Replace(state, Copy(actor, actor.Ability, actor.Triggers.Where(trigger =>
                        trigger.Origin?.UpgradeId != CommonOrigin || trigger.Kind != first).ToArray(), actor.NextTriggerId));
                    if (!Drain()) return result;
                    actor = state.Units.FirstOrDefault(unit => unit.Id == unitId);
                    if (actor == null) return Unsupported("Ability removal callback destroyed its target.");
                }
                CombatTrigger[] kept = source.Preview ? actor.Triggers.ToArray() : actor.Triggers.Where(trigger =>
                    trigger.Origin?.UpgradeId != CommonOrigin || rule.CommonTriggers.All(common => common.Kind != trigger.Kind)).ToArray();
                var cleared = new UnitAbilityState("", 0, 0, previous.FromEquipment, previous.Resolving,
                    previous.PreviousDataId, previousDefinition: previous.PreviousDefinition);
                state = Replace(state, Copy(actor, Normalize(cleared), kept, actor.NextTriggerId));
                if (remove && rule.Permanent)
                    state = Context(state, state.Context!.WithPermanentlyDisabledAbilities(
                        state.Context.PermanentlyDisabledAbilities!.Concat(new[] { previous.DataId }).ToArray()));
            }
            UnitAbilityDefinition? definition = remove ? previous?.FromEquipment == true ? restored : null : rule.Definition;
            if (definition != null)
            {
                if (!definition.IsUnitAbility || definition.CardCreation == null || definition.DataId != definition.CardCreation.DataId)
                    return Unsupported("Invalid ability creation definition.");
                actor = state.Units.First(unit => unit.Id == unitId);
                var added = new UnitAbilityState(definition.DataId, definition.Cooldown, definition.CooldownAtSpawn,
                    remove ? previous!.FromEquipment : rule.FromEquipment, previous?.Resolving ?? false,
                    remove ? previous?.PreviousDataId : remembered?.DataId, definition.CardCreation, definition, remove ? restored : remembered);
                state = Replace(state, Copy(actor, added, actor.Triggers, actor.NextTriggerId));
                int cooldown = remove ? definition.Cooldown : definition.CooldownAtSpawn;
                foreach (var application in new[] { ("unit_ability", 1, false), ("cooldown", cooldown, true) })
                {
                    CombatStatus? status = state.Context!.StatusRules.FirstOrDefault(item => item.Id == application.Item1);
                    if (status == null) return Unsupported("Missing ability status definition " + application.Item1);
                    RoomCombatResult applied = StatusApplicationModel.ApplyRetained(state, unitId, status.WithStacks(application.Item2), 0,
                        overrideImmunity: application.Item3);
                    if (!applied.Supported) return applied;
                    state = applied.State!; callbacks.AddRange(applied.PendingCallbacks);
                }
                if (cooldown == 0)
                {
                    RoomCombatResult removed = AbilityCooldownModel.RemoveStatus(state, unitId, "unit_ability_available", 1);
                    if (!removed.Supported) return removed;
                    state = removed.State!; callbacks.AddRange(removed.PendingCallbacks);
                }
                actor = state.Units.First(unit => unit.Id == unitId);
                int? next = actor.NextTriggerId;
                var triggers = actor.Triggers.Concat((source.Preview ? Array.Empty<CombatTrigger>() : rule.CommonTriggers).Select(trigger =>
                    trigger.WithOrigin(CommonOrigin, 0, next.HasValue ? next++ : null))).ToArray();
                state = Replace(state, Copy(actor, added, triggers, next));
                AbilityCardResult cached = AbilityCardModel.Get(state.Context!, definition.CardCreation);
                if (!cached.Supported) return Unsupported(cached.UnsupportedReason!);
                state = Context(state, cached.Context!);
            }
            if (remove)
            {
                actor = state.Units.First(unit => unit.Id == unitId);
                UnitAbilityState? current = actor.Ability;
                if (current != null) current = new UnitAbilityState(current.DataId, current.Cooldown, current.CooldownAtSpawn,
                    false, current.Resolving, cardCreation: current.CardCreation, definition: current.Definition);
                state = Replace(state, Copy(actor, Normalize(current), actor.Triggers, actor.NextTriggerId));
            }
            if (deferCallbacks) return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: callbacks);
            return Drain() ? new RoomCombatResult(state, outcome, 0, events) : result;

            bool Drain()
            {
                result = Match(state);
                bool drained = RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
                {
                    result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, callbacks.Add);
                    if (!result.Supported) return false;
                    events.AddRange(result.Events);
                    if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome;
                    return true;
                }, queued =>
                {
                    result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit);
                    return result.Supported;
                });
                if (drained) { state = result.State!; callbacks.Clear(); }
                return drained;
            }
        }

        internal static CombatUnit InitialAtSpawn(CombatUnit unit, CardInstanceState? card, CombatContext context, out string? error)
        {
            error = null;
            AbilityChangeRule? chosen = null;
            bool hasDefinition = unit.Ability?.HasAbility == true;
            foreach (CardUpgradeModifier upgrade in (card?.Permanent.Upgrades ?? Array.Empty<CardUpgradeModifier>())
                .Concat(card?.Temporary.Upgrades ?? Array.Empty<CardUpgradeModifier>()))
                if (upgrade.AbilityUpgrade?.Definition != null && (!upgrade.DoNotReplaceExistingAbility || !hasDefinition))
                { chosen = upgrade.AbilityUpgrade; hasDefinition = true; }
            if (chosen == null)
            {
                error = SpawnError(unit, context);
                return error == null ? SuppressAtSpawn(unit, context) : unit;
            }
            if (unit.AbilityRules == null)
            { error = "Initial upgraded abilities require captured authored starting statuses."; return unit; }
            if (chosen.Definition?.IsUnitAbility != true)
            { error = "Invalid initial ability definitions require native raw setup state."; return unit; }
            UnitAbilityDefinition? definition = chosen.Definition?.IsUnitAbility == true ? chosen.Definition : null;
            CombatStatus? marker = context.StatusRules.FirstOrDefault(status => status.Id == "unit_ability");
            if (definition != null && (marker == null || definition.CardCreation == null || chosen.CommonTriggers.Count == 0))
            { error = "Initial upgraded abilities require marker, creation and common trigger definitions."; return unit; }
            CombatStatus[] statuses = (definition == null ? Array.Empty<CombatStatus>() : new[] { marker!.WithStacks(1) })
                .Concat(unit.AbilityRules.AuthoredStartingStatuses).ToArray();
            CombatTrigger[] triggers = (definition == null ? Array.Empty<CombatTrigger>() : chosen.CommonTriggers
                .Select(trigger => trigger.WithOrigin(CommonOrigin, 0)).ToArray())
                .Concat(unit.Triggers.Where(trigger => trigger.Origin?.UpgradeId != CommonOrigin))
                .Select((trigger, index) => trigger.WithStateId(unit.NextTriggerId.HasValue ? index : (int?)null)).ToArray();
            var ability = definition == null ? null : new UnitAbilityState(definition.DataId, definition.Cooldown, definition.CooldownAtSpawn,
                cardCreation: definition.CardCreation, definition: definition);
            var initialized = new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, unit.Health, unit.MaxHealth,
                unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath, statuses, triggers, unit.SpawnerCardId, unit.Size,
                unit.StatusImmunities, unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId, statuses, unit.EquipmentCards,
                unit.NextTriggerId.HasValue ? triggers.Length : (int?)null, ability,
                unit.StatusDictionary == null ? null : new StatusDictionaryState(statuses.Select(status => (string?)status.Id).ToArray(), Array.Empty<int>()),
                unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning);
            return SuppressAtSpawn(initialized, context);
        }

        internal static CombatUnit SuppressAtSpawn(CombatUnit unit, CombatContext? context)
        {
            if (unit.Ability?.HasAbility != true || context?.PermanentlyDisabledAbilities?.Contains(unit.Ability.DataId) != true) return unit;
            if (unit.AbilityRules == null) throw new InvalidOperationException("Uncaptured disabled ability blueprint.");
            var statuses = unit.AbilityRules.AuthoredStartingStatuses;
            var triggers = unit.Triggers.Where(trigger => trigger.Origin?.UpgradeId != CommonOrigin).ToArray();
            triggers = triggers.Select((trigger, index) => trigger.WithStateId(unit.NextTriggerId.HasValue ? index : (int?)null)).ToArray();
            return new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, unit.Health, unit.MaxHealth,
                unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath, statuses, triggers, unit.SpawnerCardId, unit.Size,
                unit.StatusImmunities, unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId, statuses, unit.EquipmentCards,
                unit.NextTriggerId.HasValue ? triggers.Length : (int?)null, null,
                unit.StatusDictionary == null ? null : new StatusDictionaryState(statuses.Select(status => (string?)status.Id).ToArray(), Array.Empty<int>()),
                unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning);
        }
        internal static string? SpawnError(CombatUnit unit, CombatContext? context) =>
            unit.Ability?.HasAbility == true && context?.PermanentlyDisabledAbilities?.Contains(unit.Ability.DataId) == true &&
            unit.AbilityRules == null ? "Disabled ability spawning requires authored starting statuses." : null;
        internal static CombatUnit Copy(CombatUnit unit, UnitAbilityState? ability, IReadOnlyList<CombatTrigger> triggers, int? next) =>
            new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, unit.Health, unit.MaxHealth, unit.CanAttack,
                unit.IsPyre, unit.EndsBattleOnDeath, unit.Statuses, triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities,
                unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId, unit.StatusRegistry, unit.EquipmentCards, next,
                ability, unit.StatusDictionary, unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning);
        private static RoomCombatState Replace(RoomCombatState state, CombatUnit unit) => new RoomCombatState(state.RoomIndex,
            state.Deployment, state.Units.Select(item => item.Id == unit.Id ? unit : item).ToArray(), state.ExternalInteractions, state.Context, state.Preview);
        private static RoomCombatState Context(RoomCombatState state, CombatContext context) => new RoomCombatState(state.RoomIndex,
            state.Deployment, state.Units, state.ExternalInteractions, context, state.Preview);
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string error) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}
