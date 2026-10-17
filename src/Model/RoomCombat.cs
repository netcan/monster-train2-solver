using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonsterTrain2Poju.Model
{
    public enum CombatTeam { Enemy, Player }
    public enum RoomOutcome { Exchanged, Cleared, PlayerDefeated, BattleWon, Stalemate, Unsupported }

    public sealed class CombatStatus
    {
        public string Id { get; }
        public int Stacks { get; }
        public int ParamInt { get; }
        public bool RemoveWhenTriggered { get; }
        public bool RemoveStackAtEnd { get; }
        public bool RemoveAllAtEnd { get; }
        public bool RemoveAfterPostCombat { get; }
        public bool PreventRemovalDuringRelentless { get; }
        public bool SkipDuringDeployment { get; }
        public bool RemoveDuringDeployment { get; }
        public bool RemoveWhenTriggeredAfterCardPlayed { get; }
        public bool RemoveAtEndOfTurnIfTriggered { get; }
        public bool MarkedForRemovalAfterCardPlayed { get; }
        public bool? TriggerVfxEnemy { get; }
        public bool? TriggerVfxPlayer { get; }
        public bool? Stackable { get; }
        public bool? Hidden { get; }
        public string? DisplayCategory { get; }
        public bool? Propagatable { get; }
        public bool? ExcludeHeroPropagation { get; }
        public bool? ExcludeMonsterPropagation { get; }

        public CombatStatus(string id, int stacks, int paramInt = 0,
            bool removeWhenTriggered = false, bool removeStackAtEnd = false,
            bool removeAllAtEnd = false, bool removeAfterPostCombat = false,
            bool preventRemovalDuringRelentless = false, bool skipDuringDeployment = false,
            bool removeDuringDeployment = false, bool? triggerVfxEnemy = null, bool? triggerVfxPlayer = null, bool? stackable = null,
            bool? hidden = null, string? displayCategory = null, bool? propagatable = null,
            bool? excludeHeroPropagation = null, bool? excludeMonsterPropagation = null,
            bool removeWhenTriggeredAfterCardPlayed = false, bool removeAtEndOfTurnIfTriggered = false,
            bool markedForRemovalAfterCardPlayed = false)
        {
            Id = id;
            Stacks = stacks;
            ParamInt = paramInt;
            RemoveWhenTriggered = removeWhenTriggered;
            RemoveStackAtEnd = removeStackAtEnd;
            RemoveAllAtEnd = removeAllAtEnd;
            RemoveAfterPostCombat = removeAfterPostCombat;
            PreventRemovalDuringRelentless = preventRemovalDuringRelentless;
            SkipDuringDeployment = skipDuringDeployment;
            RemoveDuringDeployment = removeDuringDeployment;
            RemoveWhenTriggeredAfterCardPlayed = removeWhenTriggeredAfterCardPlayed;
            RemoveAtEndOfTurnIfTriggered = removeAtEndOfTurnIfTriggered;
            MarkedForRemovalAfterCardPlayed = markedForRemovalAfterCardPlayed;
            TriggerVfxEnemy = triggerVfxEnemy;
            TriggerVfxPlayer = triggerVfxPlayer;
            Stackable = stackable;
            Hidden = hidden;
            DisplayCategory = displayCategory;
            Propagatable = propagatable;
            ExcludeHeroPropagation = excludeHeroPropagation;
            ExcludeMonsterPropagation = excludeMonsterPropagation;
        }

        internal CombatStatus WithStacks(int stacks) => new CombatStatus(Id, stacks, ParamInt,
            RemoveWhenTriggered, RemoveStackAtEnd, RemoveAllAtEnd, RemoveAfterPostCombat,
            PreventRemovalDuringRelentless, SkipDuringDeployment, RemoveDuringDeployment, TriggerVfxEnemy, TriggerVfxPlayer,
            Stackable, Hidden, DisplayCategory, Propagatable, ExcludeHeroPropagation, ExcludeMonsterPropagation,
            RemoveWhenTriggeredAfterCardPlayed, RemoveAtEndOfTurnIfTriggered, MarkedForRemovalAfterCardPlayed);
        internal CombatStatus WithRemoveAllAtEnd(bool value) => new CombatStatus(Id, Stacks, ParamInt,
            RemoveWhenTriggered, RemoveStackAtEnd, value, RemoveAfterPostCombat, PreventRemovalDuringRelentless,
            SkipDuringDeployment, RemoveDuringDeployment, TriggerVfxEnemy, TriggerVfxPlayer, Stackable, Hidden,
            DisplayCategory, Propagatable, ExcludeHeroPropagation, ExcludeMonsterPropagation,
            RemoveWhenTriggeredAfterCardPlayed, RemoveAtEndOfTurnIfTriggered, MarkedForRemovalAfterCardPlayed);
        internal CombatStatus WithMarkedForRemovalAfterCardPlayed(bool value) => new CombatStatus(Id, Stacks, ParamInt,
            RemoveWhenTriggered, RemoveStackAtEnd, RemoveAllAtEnd, RemoveAfterPostCombat, PreventRemovalDuringRelentless,
            SkipDuringDeployment, RemoveDuringDeployment, TriggerVfxEnemy, TriggerVfxPlayer, Stackable, Hidden,
            DisplayCategory, Propagatable, ExcludeHeroPropagation, ExcludeMonsterPropagation,
            RemoveWhenTriggeredAfterCardPlayed, RemoveAtEndOfTurnIfTriggered, value);
    }

    public sealed class UnitDeathState
    {
        public bool HasFinishedDying { get; }
        public bool IsBeingRemoved { get; }
        public bool HasStatisticsListener { get; }
        public int? PendingStatisticsCardId { get; }
        public bool? IsSacrifice { get; }
        public bool? StatisticsListenerOnce { get; }
        // Older captures do not observe these flags. A retained positive-HP actor may be despawned.
        public bool? IsDespawned { get; }
        public bool? IsDestroyed { get; }
        // Transient immutable state needed to restore units born during a preview.
        // This is internal simulation metadata, not a native observable field.
        internal CombatUnit? PreviewPrimary { get; }
        public UnitDeathState(bool hasFinishedDying, bool isBeingRemoved, bool hasStatisticsListener, int? pendingStatisticsCardId = null,
            bool? isSacrifice = null, bool? statisticsListenerOnce = null, bool? isDespawned = null, bool? isDestroyed = null)
        { HasFinishedDying = hasFinishedDying; IsBeingRemoved = isBeingRemoved; HasStatisticsListener = hasStatisticsListener; PendingStatisticsCardId = pendingStatisticsCardId; IsSacrifice = isSacrifice; StatisticsListenerOnce = statisticsListenerOnce; IsDespawned = isDespawned; IsDestroyed = isDestroyed; }
        private UnitDeathState(UnitDeathState source, CombatUnit? previewPrimary) : this(source.HasFinishedDying,
            source.IsBeingRemoved, source.HasStatisticsListener, source.PendingStatisticsCardId, source.IsSacrifice,
            source.StatisticsListenerOnce, source.IsDespawned, source.IsDestroyed) { PreviewPrimary = previewPrimary; }
        internal UnitDeathState WithPreviewPrimary(CombatUnit? primary) => new UnitDeathState(this, primary);
        internal UnitDeathState WithLifecycle(bool? despawned, bool? destroyed) => new UnitDeathState(HasFinishedDying,
            IsBeingRemoved, HasStatisticsListener, PendingStatisticsCardId, IsSacrifice, StatisticsListenerOnce, despawned, destroyed)
                .WithPreviewPrimary(PreviewPrimary);
    }

    public sealed class CombatUnit
    {
        public int Id { get; }
        public string AssetKey { get; }
        public CombatTeam Team { get; }
        public int BaseAttack { get; }
        internal int AttackBeforeStatuses => Modifiers == null ? BaseAttack : unchecked(Modifiers.AttackDamage + Modifiers.DamageBuff);
        internal int AttackFromStatuses => Modifiers?.DamageBuffFromStatusEffects ??
            unchecked(StatusAmount("buff") + StatusAmount("valor") - StatusAmount("debuff"));
        public int Attack => Math.Max(0, unchecked(AttackBeforeStatuses + AttackFromStatuses +
            RelicStatusAttackModel.AdditionalAttack(Modifiers?.StatusAttackModifiers, Team, Statuses)));
        public int Health { get; }
        public int MaxHealth { get; }
        public bool CanAttack { get; }
        public bool IsPyre { get; }
        public bool EndsBattleOnDeath { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        // Native dictionary entries, in insertion order, including zero stacks.
        // Null marks a legacy capture that cannot prove status-presence queries.
        public IReadOnlyList<CombatStatus>? StatusRegistry { get; }
        public IReadOnlyList<CombatTrigger> Triggers { get; }
        public int SpawnerCardId { get; }
        public int Size { get; }
        public IReadOnlyList<string> StatusImmunities { get; }
        public IReadOnlyList<string> Subtypes { get; }
        public UnitModifiers? Modifiers { get; }
        public bool? IsBoss { get; }
        // Null denotes a legacy capture without relationship state; zero is known-none.
        public int? LastAttackerId { get; }
        public IReadOnlyList<int>? EquipmentCards { get; }
        public int? NextTriggerId { get; }
        public UnitAbilityState? Ability { get; }
        public StatusDictionaryState? StatusDictionary { get; }
        public UnitAbilityRules? AbilityRules { get; }
        public HordeBaseStats? HordeDefinition { get; }
        public bool? IsSpawning { get; }
        public int? SacrificeCardId { get; }
        public UnitDeathState? DeathState { get; }
        public UnitBumpRules? BumpRules { get; }
        public int Attacks => AttackCount(Statuses);

        internal static int AttackCount(IEnumerable<CombatStatus> statuses)
        {
            int attacks = 1;
            foreach (CombatStatus status in statuses)
                if (status.Id is "multistrike" or "burst")
                    attacks = unchecked(attacks + AdditionalAttacks(status));
            return Math.Max(1, attacks);
        }

        internal static int AdditionalAttacks(CombatStatus status) =>
            Math.Max(0, unchecked(status.ParamInt + status.Stacks - 2));

        public CombatUnit(int id, string assetKey, CombatTeam team, int baseAttack,
            int health, int maxHealth, bool canAttack, bool isPyre, bool endsBattleOnDeath,
            IReadOnlyList<CombatStatus> statuses, IReadOnlyList<CombatTrigger>? triggers = null, int spawnerCardId = 0, int size = 0,
            IReadOnlyList<string>? statusImmunities = null, IReadOnlyList<string>? subtypes = null, UnitModifiers? modifiers = null, bool? isBoss = null,
            int? lastAttackerId = null, IReadOnlyList<CombatStatus>? statusRegistry = null, IReadOnlyList<int>? equipmentCards = null,
            int? nextTriggerId = null, UnitAbilityState? ability = null, StatusDictionaryState? statusDictionary = null,
            UnitAbilityRules? abilityRules = null, HordeBaseStats? hordeDefinition = null, bool? isSpawning = null, int? sacrificeCardId = null, UnitDeathState? deathState = null,
            UnitBumpRules? bumpRules = null)
        {
            Id = id;
            LastAttackerId = lastAttackerId;
            NextTriggerId = nextTriggerId;
            Ability = ability;
            AbilityRules = abilityRules;
            HordeDefinition = hordeDefinition;
            IsSpawning = isSpawning;
            SacrificeCardId = sacrificeCardId;
            DeathState = deathState;
            BumpRules = bumpRules;
            EquipmentCards = equipmentCards == null ? null : Array.AsReadOnly(equipmentCards.ToArray());
            AssetKey = assetKey;
            Team = team;
            BaseAttack = baseAttack;
            Health = health;
            MaxHealth = maxHealth;
            CanAttack = canAttack;
            IsPyre = isPyre;
            EndsBattleOnDeath = endsBattleOnDeath;
            Statuses = Array.AsReadOnly(statuses.Where(item => item.Stacks > 0)
                .OrderBy(item => item.Id, StringComparer.Ordinal).ToArray());
            StatusRegistry = statusRegistry == null ? null : Array.AsReadOnly(MergeStatusRegistry(statusRegistry, statuses));
            StatusDictionary = StatusRegistry == null ? statusDictionary : statusDictionary?.Sync(StatusRegistry);
            if (StatusDictionary != null && StatusRegistry != null) StatusRegistry = Array.AsReadOnly(StatusDictionary.Order(StatusRegistry));
            Triggers = Array.AsReadOnly((triggers ?? Array.Empty<CombatTrigger>()).ToArray());
            SpawnerCardId = spawnerCardId;
            Size = size;
            StatusImmunities = Array.AsReadOnly((statusImmunities ?? Array.Empty<string>())
                .Concat(Statuses.Any(status => status.Id == "relentless") ? new[] { "rooted" } : Array.Empty<string>())
                .Concat(Statuses.Any(status => status.Id == "cardless") ? new[] { "endless" } : Array.Empty<string>())
                .Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray());
            Subtypes = Array.AsReadOnly((subtypes ?? Array.Empty<string>()).ToArray());
            Modifiers = modifiers;
            IsBoss = isBoss;
        }

        internal CombatStatus? Status(string id) => Statuses.FirstOrDefault(item => item.Id == id);
        internal CombatStatus? RegisteredStatus(string id) => StatusRegistry?.FirstOrDefault(item => item.Id == id) ?? Status(id);
        public int? CountUniqueStatusDefinitions() => StatusRegistry?.Count;
        public int? CountUniqueVisibleStatuses()
        {
            if (StatusRegistry == null || StatusRegistry.Any(status => !status.Hidden.HasValue || status.DisplayCategory == null)) return null;
            return StatusRegistry.Count(status => status.Hidden == false &&
                (status.DisplayCategory == "Positive" || status.DisplayCategory == "Negative"));
        }
        internal CombatUnit WithHealth(int health) => new CombatUnit(Id, AssetKey, Team, BaseAttack, health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, BumpRules);
        internal CombatUnit WithSacrifice(int cardId) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, cardId, DeathState == null ? null : new UnitDeathState(
                DeathState.HasFinishedDying, DeathState.IsBeingRemoved, DeathState.HasStatisticsListener,
                DeathState.PendingStatisticsCardId, DeathState.IsSacrifice.HasValue ? true : (bool?)null, DeathState.StatisticsListenerOnce,
                DeathState.IsDespawned, DeathState.IsDestroyed).WithPreviewPrimary(DeathState.PreviewPrimary), bumpRules: BumpRules);

        internal CombatUnit WithDeathState(UnitDeathState state) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, state.WithLifecycle(
                state.IsDespawned ?? DeathState?.IsDespawned, state.IsDestroyed ?? DeathState?.IsDestroyed)
                    .WithPreviewPrimary(state.PreviewPrimary ?? DeathState?.PreviewPrimary), bumpRules: BumpRules);
        internal CombatUnit WithTriggers(IReadOnlyList<CombatTrigger> triggers) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules);
        internal CombatUnit WithTriggerAllocation(IReadOnlyList<CombatTrigger> triggers, int nextTriggerId) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, nextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules);

        internal CombatUnit WithStatusDamage(int amount) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers!.WithStatusDamage(amount), IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules);

        internal CombatUnit WithStatusRegistry(IReadOnlyList<CombatStatus> statusRegistry) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, statusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules);

        internal CombatUnit WithModifiers(UnitModifiers modifiers) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules);

        internal CombatUnit WithEndsBattleOnDeath(bool endsBattleOnDeath) => endsBattleOnDeath == EndsBattleOnDeath ? this :
            new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth, CanAttack, IsPyre, endsBattleOnDeath,
                Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes, Modifiers, IsBoss, LastAttackerId,
                StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary, AbilityRules, HordeDefinition,
                IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules);

        internal static CombatStatus[] MergeStatusRegistry(IReadOnlyList<CombatStatus> registry, IReadOnlyList<CombatStatus> statuses)
        {
            var current = statuses.ToDictionary(status => status.Id, StringComparer.Ordinal);
            var result = new List<CombatStatus>();
            foreach (CombatStatus prior in registry)
            {
                result.Add(current.TryGetValue(prior.Id, out CombatStatus? changed) ? changed : prior.WithStacks(0));
                current.Remove(prior.Id);
            }
            foreach (CombatStatus status in statuses)
                if (current.Remove(status.Id)) result.Add(status);
            return result.ToArray();
        }
        internal int StatusAmount(string id) => Status(id) is CombatStatus status
            ? status.Stacks * status.ParamInt : 0;

        internal CombatUnit WithoutRemovedAttacker(ISet<int> activeIds) => !LastAttackerId.HasValue || LastAttackerId == 0 || activeIds.Contains(LastAttackerId.Value)
            ? this : new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth, CanAttack, IsPyre, EndsBattleOnDeath, Statuses,
                Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes, Modifiers, IsBoss, 0, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary, AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules);
    }

    public sealed class RoomCombatState
    {
        public int RoomIndex { get; }
        public bool Deployment { get; }
        public IReadOnlyList<CombatUnit> Units { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public CombatContext? Context { get; }
        public bool Preview { get; }
        public IReadOnlyList<int>? ActiveCompanionBossIds { get; }
        internal int? Corruption => Context?.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == RoomIndex)?.Corruption;

        // Units are ordered front to back within each team. Identity is separate from asset name.
        public RoomCombatState(int roomIndex, bool deployment, IReadOnlyList<CombatUnit> units,
            IReadOnlyList<string> externalInteractions, CombatContext? context = null, bool preview = false,
            IReadOnlyList<int>? activeCompanionBossIds = null)
        {
            RoomIndex = roomIndex;
            Deployment = deployment;
            Units = Array.AsReadOnly(units.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            Context = context;
            Preview = preview;
            ActiveCompanionBossIds = activeCompanionBossIds == null ? null : Array.AsReadOnly(activeCompanionBossIds.Distinct().ToArray());
        }

        internal RoomCombatState WithCompanionBosses(IReadOnlyList<int>? ids) => new(RoomIndex, Deployment, Units,
            ExternalInteractions, Context, Preview, ids);

        internal RoomCombatState WithUnits(IReadOnlyList<CombatUnit> units) => new(RoomIndex, Deployment, units,
            ExternalInteractions, Context, Preview, ActiveCompanionBossIds);

        internal RoomCombatState WithContext(CombatContext? context) => new(RoomIndex, Deployment, Units,
            ExternalInteractions, context, Preview, ActiveCompanionBossIds);
    }

    public sealed class CombatEvent
    {
        public int Round { get; }
        public string Kind { get; }
        public int Actor { get; }
        public int Target { get; }
        public int Amount { get; }

        internal CombatEvent(int round, string kind, int actor, int target, int amount)
        { Round = round; Kind = kind; Actor = actor; Target = target; Amount = amount; }
    }

    internal sealed class CharacterTriggerDispatch
    {
        public int ActorId { get; }
        public string Kind { get; }
        public int DyingId { get; }
        public int TriggerCount { get; }
        public int ParamInt { get; }
        public int ParamInt2 { get; }
        public string? ParamString { get; }
        public int OverrideTargetId { get; }
        public int LastSpawnedOverrideUnitId { get; }
        public int? ExclusiveTriggerStateId { get; }
        internal CharacterTriggerDispatch(int actorId, string kind, int dyingId, int triggerCount, int paramInt = 0,
            int paramInt2 = 0, string? paramString = null, int overrideTargetId = 0, int lastSpawnedOverrideUnitId = 0, int? exclusiveTriggerStateId = null)
        { ActorId = actorId; Kind = kind; DyingId = dyingId; TriggerCount = triggerCount; ParamInt = paramInt; ParamInt2 = paramInt2;
            ParamString = paramString; OverrideTargetId = overrideTargetId; LastSpawnedOverrideUnitId = lastSpawnedOverrideUnitId;
            ExclusiveTriggerStateId = exclusiveTriggerStateId; }
    }

    public sealed class RoomCombatResult
    {
        public RoomCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public int Rounds { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        internal IReadOnlyList<CombatUnit> RetainedUnits { get; }
        internal IReadOnlyList<CharacterTriggerDispatch> Dispatches { get; }

        internal RoomCombatResult(RoomCombatState? state, RoomOutcome outcome, int rounds,
            List<CombatEvent> events, string? reason = null, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? pendingCallbacks = null, IReadOnlyList<CombatUnit>? retainedUnits = null, IReadOnlyList<CharacterTriggerDispatch>? dispatches = null)
        {
            Dispatches = Array.AsReadOnly((dispatches ?? Array.Empty<CharacterTriggerDispatch>()).ToArray());
            RetainedUnits = Array.AsReadOnly((retainedUnits ?? Array.Empty<CombatUnit>()).ToArray());
            State = state; Outcome = outcome; Rounds = rounds;
            Events = Array.AsReadOnly(events.ToArray()); UnsupportedReason = reason;
            PendingCallbacks = Array.AsReadOnly((pendingCallbacks ?? Array.Empty<RoomCombatModel.QueuedCharacterTrigger>()).ToArray());
        }
    }

    public static class RoomCombatModel
    {
        private const string DeathRoomFocusPhase = "DeathRoomFocus";
        internal enum CharacterTriggerAdmission { Pending, Accepted, Rejected }
        internal sealed class QueuedCharacterTrigger
        {
            internal int RoomIndex { get; }
            internal CombatUnit Unit { get; set; }
            internal string Kind { get; }
            internal bool IsDeathRoomFocus => Kind == DeathRoomFocusPhase;
            internal int ParamInt { get; }
            internal int ParamInt2 { get; }
            internal string? ParamString { get; }
            internal CombatUnit? OverrideTarget { get; set; }
            internal CombatUnit? DyingCharacter { get; set; }
            internal bool CanFireTriggers { get; }
            internal bool ReturnSpawnerAfterQueue { get; }
            internal bool DeferUntilRemoval { get; }
            internal int TriggerCount { get; }
            internal int LastSpawnedOverrideUnitId { get; }
            internal int? ExclusiveTriggerStateId { get; }
            internal bool HarvestAfterDeath { get; }
            internal bool CompletePhysicalRemovalAfterQueue { get; }
            internal CharacterTriggerAdmission Admission { get; }
            internal bool RemovalLifecycle { get; }
            internal bool PreservePhysicalPoint { get; }
            internal bool CaptureAllDispatches { get; }
            internal bool RelicDamageRemoval { get; }
            internal bool RelicDamageApplied { get; }
            internal bool RelicMovementStatusApplied { get; }
            internal QueuedCharacterTrigger(int roomIndex, CombatUnit unit, string kind = "OnDeath", bool returnSpawnerAfterQueue = false, bool deferUntilRemoval = false, int paramInt = 0,
                CombatUnit? overrideTarget = null, int paramInt2 = 0, string? paramString = null, CombatUnit? dyingCharacter = null, bool canFireTriggers = true,
                int triggerCount = 1, int lastSpawnedOverrideUnitId = 0, bool harvestAfterDeath = false, bool completePhysicalRemovalAfterQueue = false,
                CharacterTriggerAdmission admission = CharacterTriggerAdmission.Pending, bool removalLifecycle = false, bool preservePhysicalPoint = false,
                int? exclusiveTriggerStateId = null, bool captureAllDispatches = false, bool relicDamageRemoval = false, bool relicDamageApplied = false,
                bool relicMovementStatusApplied = false)
            { RoomIndex = roomIndex; Unit = unit; Kind = kind; ReturnSpawnerAfterQueue = returnSpawnerAfterQueue; DeferUntilRemoval = deferUntilRemoval; ParamInt = paramInt; OverrideTarget = overrideTarget; ParamInt2 = paramInt2; ParamString = paramString; DyingCharacter = dyingCharacter; CanFireTriggers = canFireTriggers; TriggerCount = triggerCount; LastSpawnedOverrideUnitId = lastSpawnedOverrideUnitId; HarvestAfterDeath = harvestAfterDeath; CompletePhysicalRemovalAfterQueue = completePhysicalRemovalAfterQueue; Admission = admission; RemovalLifecycle = removalLifecycle; PreservePhysicalPoint = preservePhysicalPoint; ExclusiveTriggerStateId = exclusiveTriggerStateId; CaptureAllDispatches = captureAllDispatches; RelicDamageRemoval = relicDamageRemoval; RelicDamageApplied = relicDamageApplied; RelicMovementStatusApplied = relicMovementStatusApplied; }
            internal QueuedCharacterTrigger WithRelicDamageRemoval() => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue,
                Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches, true, RelicDamageApplied, RelicMovementStatusApplied);
            internal QueuedCharacterTrigger WithRelicDamageApplied() => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue,
                Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches, RelicDamageRemoval, true, RelicMovementStatusApplied);
            internal QueuedCharacterTrigger WithRelicMovementStatusApplied() => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue,
                Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches,
                RelicDamageRemoval, RelicDamageApplied, true);
            internal QueuedCharacterTrigger InRoom(int roomIndex) => new QueuedCharacterTrigger(roomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue, Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches, RelicDamageRemoval, RelicDamageApplied, RelicMovementStatusApplied);
            internal QueuedCharacterTrigger WithAdmission(CharacterTriggerAdmission admission) => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue, admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches, RelicDamageRemoval, RelicDamageApplied, RelicMovementStatusApplied);
            internal QueuedCharacterTrigger WithCaptureAllDispatches(bool capture) => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue,
                Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches || capture, RelicDamageRemoval, RelicDamageApplied, RelicMovementStatusApplied);
        }
        private static readonly HashSet<string> KnownStatuses = new HashSet<string>(StringComparer.Ordinal)
        {
            "armor", "damage shield", "dazed", "stealth", "ambush", "multistrike", "burst",
            "spikes", "lifesteal", "fragile", "piercing", "immune", "immobile",
            "relentless", "sweep", "sniper", "rooted", "haste", "untouchable", "endless",
            "buff", "debuff", "regen", "poison", "decay", "corrupt_poison", "melee weakness", "spell weakness", "silenced", "muted", "valor", "pyregel",
            "heal multiplier", "heal immunity", "cooldown", "unit_ability", "unit_ability_available", "horde", "cardless", "undying", "ephemeral", "purify", "mageblade", "titanskin", "duality", "conduit",
            "newmoon", "fullmoon", "stygian_blessing", "scorch", "trample", "spell shield", "unstable", "hatch"
        };
        internal static bool KnowsStatus(string id) => KnownStatuses.Contains(id);

        public static RoomCombatResult Exchange(RoomCombatState state, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool updateSelectedRoom = true, IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null) =>
            Run(state, false, refreshPyre: refreshPyre, updateSelectedRoom: updateSelectedRoom, externalTargets: externalTargets);
        public static RoomCombatResult Resolve(RoomCombatState state, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool updateSelectedRoom = true, CombatUnit? externalPyre = null, int externalPyreRoomIndex = -1,
            IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null) =>
            Run(state, true, refreshPyre: refreshPyre, updateSelectedRoom: updateSelectedRoom,
                externalPyre: externalPyre, externalPyreRoomIndex: externalPyreRoomIndex, externalTargets: externalTargets);

        public static RoomCombatResult ApplyUnitPostCombat(RoomCombatState state, IReadOnlyList<int> cannotAttackOrHeal,
            IReadOnlyList<int> cannotFireTriggers, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            string? error = Validate(state);
            if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            return new Engine(state, new List<CombatEvent>(), refreshPyre: refreshPyre).UnitPostCombat(cannotAttackOrHeal, cannotFireTriggers);
        }

        public static RoomCombatResult ApplyUnitTurn(RoomCombatState state, int unitId,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool ignoreAttackPrevention = false, CombatUnit? externalActor = null)
        {
            string? error = Validate(state);
            bool localActor = state.Units.Any(unit => unit.Id == unitId);
            if (error != null || localActor == (externalActor != null) || externalActor != null && externalActor.Id != unitId)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Unit turn requires exactly one captured local or external actor.");
            if (externalActor != null && (externalActor.Health <= 0 || externalActor.DeathState?.IsDestroyed == true))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    "External unit turn requires a living actor.");
            return new Engine(state, new List<CombatEvent>(), refreshPyre: refreshPyre, externalActor: externalActor)
                .UnitTurn(unitId, ignoreAttackPrevention);
        }

        public static RoomCombatResult ApplyTeamTurnBegin(RoomCombatState state, CombatTeam team, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            string? error = Validate(state);
            if (error != null || team != CombatTeam.Enemy && team != CombatTeam.Player)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Invalid combat team.");
            return new Engine(state, new List<CombatEvent>(), refreshPyre: refreshPyre).TeamTurnBegin(team);
        }

        public static RoomCombatResult ApplyEndTurnPreHandDiscard(RoomCombatState state, int unitId, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
            => ApplyCharacterPhase(state, unitId, "EndTurnPreHandDiscard", refreshPyre: refreshPyre);

        public static RoomCombatResult ApplyPreCombat(RoomCombatState state, int unitId, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
            => ApplyCharacterPhase(state, unitId, "PreCombat", refreshPyre: refreshPyre);

        private static RoomCombatResult ApplyCharacterPhase(RoomCombatState state, int unitId, string kind, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            var queue = new List<QueuedCharacterTrigger>();
            RoomCombatResult result = ApplyCharacterPhase(state, unitId, kind, queue.Add, refreshPyre: refreshPyre);
            if (!result.Supported) return result;
            var events = result.Events.ToList();
            RoomOutcome outcome = result.Outcome;
            bool drained = DrainCharacterQueue(queue, queued =>
            {
                result = ApplyQueuedCharacterTrigger(result.State!, queued, queue.Add, refreshPyre: refreshPyre);
                if (!result.Supported) return false;
                events.AddRange(result.Events);
                if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome;
                return true;
            }, queued =>
            {
                result = SettleQueuedSpawner(result.State!, queued.Unit, refreshPyre: refreshPyre);
                return result.Supported;
            }, () =>
            {
                result = EnchantmentWorldModel.CompleteQueuedRemovals(result.State!, queue.Add);
                return result.Supported;
            }, () => result.State?.Context, message => result = new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events, message));
            if (!drained) return result;
            return new RoomCombatResult(result.State, outcome, 0, events);
        }

        internal static bool DrainCharacterQueue(List<QueuedCharacterTrigger> queue, Func<QueuedCharacterTrigger, bool> fire,
            Func<QueuedCharacterTrigger, bool> returnSpawner, Func<bool> afterRemovalBatch,
            Func<CombatContext?> getContext, Action<string> reject)
        {
            int next = 0;
            int nextAdmission = 0;
            var pending = new List<QueuedCharacterTrigger>();
            bool AdmitPending()
            {
                // Capture admission for the whole batch before the first callback can
                // purify another actor or refresh an accepted callback's retained object.
                CombatContext? rules = getContext();
                for (int index = nextAdmission; index < queue.Count; index++)
                {
                    string? error = StatusCallbackModel.Admit(rules, queue[index], out QueuedCharacterTrigger admitted);
                    if (error != null) { reject(error); return false; }
                    if (admitted.Admission == CharacterTriggerAdmission.Rejected)
                    { queue.RemoveAt(index--); continue; }
                    queue[index] = ReferenceEquals(admitted, queue[index]) ? admitted.InRoom(admitted.RoomIndex) : admitted;
                }
                nextAdmission = queue.Count;
                return true;
            }
            bool Fire(QueuedCharacterTrigger queued)
            {
                // Callbacks retain the same native character object after lethal damage. Its
                // latest death snapshot carries health and once flags across phase engines.
                QueuedCharacterTrigger? dead = queue.LastOrDefault(item => item.Kind == "OnDeath" &&
                    item.RoomIndex == queued.RoomIndex && item.Unit.Id == queued.Unit.Id && item.Unit.Health <= 0);
                if (dead != null) queued.Unit = dead.Unit;
                if (queued.DyingCharacter != null)
                {
                    CombatUnit? latestDying = queue.LastOrDefault(item => item.Kind == "OnDeath" &&
                        item.Unit.Id == queued.DyingCharacter.Id && item.Unit.Health <= 0)?.Unit;
                    if (latestDying != null) queued.DyingCharacter = latestDying;
                }
                if (queued.OverrideTarget != null)
                {
                    CombatUnit? latestTarget = queue.LastOrDefault(item => item.Kind == "OnDeath" &&
                        item.Unit.Id == queued.OverrideTarget.Id && item.Unit.Health <= 0)?.Unit;
                    if (latestTarget != null) queued.OverrideTarget = latestTarget;
                }
                if (!fire(queued)) return false;
                if (!AdmitPending()) return false;
                foreach (QueuedCharacterTrigger item in queue.Where(item => item.DyingCharacter?.Id == queued.Unit.Id))
                    item.DyingCharacter = queued.Unit;
                foreach (QueuedCharacterTrigger item in queue.Where(item => item.OverrideTarget?.Id == queued.Unit.Id))
                    item.OverrideTarget = queued.Unit;
                if (queued.OverrideTarget != null)
                    foreach (QueuedCharacterTrigger item in queue.Where(item => item.OverrideTarget?.Id == queued.OverrideTarget.Id))
                        item.OverrideTarget = queued.OverrideTarget;
                if (queued.Unit.Health <= 0 || queued.Unit.DeathState?.IsDespawned == true || queued.Unit.DeathState?.IsDestroyed == true)
                    foreach (QueuedCharacterTrigger item in queue.Where(item => item.RoomIndex == queued.RoomIndex && item.Unit.Id == queued.Unit.Id))
                        item.Unit = queued.Unit;
                return true;
            }
            bool Drain()
            {
                if (!AdmitPending()) return false;
                while (next < queue.Count)
                {
                    QueuedCharacterTrigger queued = queue[next++];
                    if (queued.DeferUntilRemoval) pending.Add(queued);
                    else
                    {
                        if (!Fire(queued)) return false;
                    if (queued.HarvestAfterDeath && (!Drain() || !Harvest(queued))) return false;
                        if (queued.ReturnSpawnerAfterQueue && !returnSpawner(queued)) return false;
                    }
                }
                // Native snapshots all eligible deaths and marks the complete batch as being
                // removed. New deaths during a removal are handled before the current spawner returns.
                QueuedCharacterTrigger[] removing = pending.OrderBy(item => item.Unit.Team).ThenBy(item => item.Unit.Id).ToArray();
                pending.Clear();
                foreach (QueuedCharacterTrigger dead in removing)
                    dead.Unit = MarkBeingRemoved(dead.Unit);
                foreach (QueuedCharacterTrigger dead in removing)
                {
                    if (!Fire(dead) || !Drain() || !Harvest(dead)) return false;
                    if ((dead.ReturnSpawnerAfterQueue || dead.CompletePhysicalRemovalAfterQueue) && !returnSpawner(dead)) return false;
                }
                // RemoveDeadCharacters calls HandleRoomUnitOrderPossiblyChanged once
                // after the complete batch, then drains its newly queued callbacks.
                if (removing.Length > 0 && (!afterRemovalBatch() || !Drain())) return false;
                return true;
            }
            bool Harvest(QueuedCharacterTrigger dead)
            {
                if (!dead.HarvestAfterDeath) return true;
                CombatUnit counts = dead.Unit;
                foreach (string stage in HarvestModel.Stages(dead.Unit))
                {
                    HarvestModel.Stage(stage, out string kind, out _);
                    if (!Fire(new QueuedCharacterTrigger(dead.RoomIndex, dead.Unit, stage,
                        triggerCount: HarvestModel.Count(counts, kind), captureAllDispatches: dead.CaptureAllDispatches)) || !Drain()) return false;
                }
                // Deferred damage removal finishes InnerCharacterDeath after Harvest.
                // An immediate sacrifice inside a running queue already focused
                // before removing its point; its queued callbacks must not refocus.
                return !dead.DeferUntilRemoval || Fire(new QueuedCharacterTrigger(dead.RoomIndex, dead.Unit, DeathRoomFocusPhase));
            }
            return Drain();
        }

        internal static RoomCombatResult ApplyCharacterPhase(RoomCombatState state, int unitId, string kind, Action<QueuedCharacterTrigger> enqueue, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            string? error = Validate(state);
            if (error != null || !state.Units.Any(unit => unit.Id == unitId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Character phases require a living character actor.");
            return new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: enqueue, refreshPyre: refreshPyre).CharacterPhase(unitId, kind);
        }

        internal static RoomCombatResult ApplyQueuedCharacterTrigger(RoomCombatState state, QueuedCharacterTrigger queued, Action<QueuedCharacterTrigger> enqueue, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            if (RelicFloorTransitionDamageModel.TryDefer(state, queued, enqueue, out RoomCombatResult floorDamage))
                return floorDamage;
            if (RelicMovementStatusModel.TryApplyBeforeMovementTrigger(state, queued, enqueue, out RoomCombatResult movementStatus))
                return movementStatus;
            RoomCombatResult triggered = new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: enqueue,
                resetPreviewTriggers: false, refreshPyre: refreshPyre).TraceAllDispatches(queued.CaptureAllDispatches).QueuedTrigger(queued);
            return RelicRoomEntryStatusModel.ApplyAfterEntry(triggered, queued, enqueue);
        }

        // Resume a single native effect after trigger preflight/marking and source
        // caching. Its child callbacks stay queued until the caller resumes.
        internal static RoomCombatResult ApplyNativeTriggeredSummon(RoomCombatState state, CombatUnit actor,
            int triggerStateId, int effectIndex, out TriggeredSummonRule? resultingRule, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            IReadOnlyList<CombatUnit>? retainedOccupants = null)
        {
            resultingRule = null;
            CombatTrigger? trigger = actor.Triggers.FirstOrDefault(item => item.StateId == triggerStateId);
            bool present = state.Units.Any(unit => unit.Id == actor.Id);
            var scope = present ? state : new RoomCombatState(state.RoomIndex, state.Deployment,
                state.Units.Concat(new[] { actor }).ToArray(), state.ExternalInteractions, state.Context, state.Preview,
                state.ActiveCompanionBossIds);
            string? error = Validate(scope, actor.Id);
            if (error != null || state.Preview || triggerStateId < 0 || trigger == null || effectIndex < 0 || effectIndex >= trigger.Effects.Count ||
                trigger.Effects[effectIndex].Summon == null || !present && actor.Health > 0 ||
                actor.SpawnerCardId > 0 && state.Context?.FindCard(actor.SpawnerCardId)?.PlayedRoomUnitIds == null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Invalid native triggered summon effect.");
            error = TriggeredSummonModel.Validate(state, trigger.Effects[effectIndex].Summon);
            if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            retainedOccupants ??= Array.Empty<CombatUnit>();
            if (retainedOccupants.Select(unit => unit.Id).Distinct().Count() != retainedOccupants.Count ||
                retainedOccupants.Any(unit => unit.Id == actor.Id || unit.Health != 0 || state.Units.Any(live => live.Id == unit.Id) ||
                    state.Context?.SpawnPoints?.SelectedGroup(state.RoomIndex, unit.Team)?.Occupants.Contains(unit.Id) != true))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Invalid retained summon occupants.");
            foreach (CombatUnit retained in retainedOccupants)
            {
                error = Validate(new RoomCombatState(state.RoomIndex, state.Deployment, state.Units.Append(retained).ToArray(),
                    state.ExternalInteractions, state.Context, state.Preview, state.ActiveCompanionBossIds), retained.Id);
                if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            }
            var callbacks = new List<QueuedCharacterTrigger>();
            RoomCombatResult result = new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: callbacks.Add,
                resetPreviewTriggers: false, refreshPyre: refreshPyre).TriggeredSummonEffect(actor, triggerStateId, effectIndex, out resultingRule, retainedOccupants);
            if (!result.Supported) resultingRule = null;
            return new RoomCombatResult(result.State, result.Outcome, result.Rounds, result.Events.ToList(),
                result.UnsupportedReason, callbacks, result.RetainedUnits, result.Dispatches);
        }

        internal static RoomCombatResult QueueSacrifice(RoomCombatState state, CombatUnit unit, int sourceCardId, Action<QueuedCharacterTrigger> enqueue,
            bool whileRunningQueue = false, bool doNotFullyRemove = false, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
            => new Engine(state, new List<CombatEvent>(), deferSpawnerExhaustion: !whileRunningQueue, enqueueCharacterTrigger: enqueue, resetPreviewTriggers: false, refreshPyre: refreshPyre)
                .SacrificeRetained(unit, sourceCardId, whileRunningQueue, doNotFullyRemove, returnSpawnerAfterQueue: !whileRunningQueue);

        internal static RoomCombatResult SettleQueuedSpawner(RoomCombatState state, CombatUnit unit, bool preservePhysicalPoint = false, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
            => new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false, refreshPyre: refreshPyre).ReturnQueuedSpawner(unit,
                preservePhysicalPoint: preservePhysicalPoint);

        internal static RoomCombatResult SettleQueuedSpawnerAndCenter(RoomCombatState state, CombatUnit unit, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
            => new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false, refreshPyre: refreshPyre).ReturnQueuedSpawner(unit, true);

        internal static RoomCombatResult SettlePendingDeathStatistics(RoomCombatState state, CombatUnit unit, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
            => new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false, refreshPyre: refreshPyre).PendingDeathStatistics(unit);

        public static RoomCombatResult ApplySpawnTriggers(RoomCombatState state, int unitId, bool fromCard,
            IReadOnlyList<CombatStatus>? startingApplications = null, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            Func<RoomCombatState, CardActionEffect, int, MoonPhaseTransitionResult>? moonPhase = null,
            Func<RoomCombatState, int, RoomCombatResult>? relicBirthEffect = null)
        {
            Func<RoomCombatState, int, IReadOnlyList<QueuedCharacterTrigger>, RoomCombatResult>? completeTrainBirth =
                relicBirthEffect == null ? null : (frame, addedUnitId, _) => relicBirthEffect(frame, addedUnitId);
            return ApplySpawnTriggers(state, unitId, fromCard, startingApplications, null, refreshPyre,
                moonPhase: moonPhase, relicBirthEffect: completeTrainBirth);
        }
        internal static RoomCombatResult ApplySpawnTriggers(RoomCombatState state, int unitId, bool fromCard,
            IReadOnlyList<CombatStatus>? startingApplications, Action<QueuedCharacterTrigger>? enqueue,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, SpawnStatusApplication? additionalStatus = null,
            Func<RoomCombatState, CardActionEffect, int, MoonPhaseTransitionResult>? moonPhase = null,
            Func<RoomCombatState, int, IReadOnlyList<QueuedCharacterTrigger>, RoomCombatResult>? relicBirthEffect = null)
        {
            string? error = Validate(state);
            CombatUnit? unit = state.Units.FirstOrDefault(item => item.Id == unitId);
            if (error != null || unit == null || fromCard && unit.SpawnerCardId <= 0)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Spawn triggers require a living new unit and its source card identity.");
            RoomCombatState? synchronized = RoomAttachmentUnitModel.ApplyToNewCharacter(state, unitId, out error);
            if (error != null || synchronized == null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Room attachment birth synchronization failed.");
            state = synchronized;
            unit = state.Units.First(item => item.Id == unitId);
            return new Engine(state, new List<CombatEvent>(), pendingSummonCardId: fromCard ? unit.SpawnerCardId : 0,
                enqueueCharacterTrigger: enqueue, refreshPyre: refreshPyre, moonPhase: moonPhase,
                relicBirthEffect: relicBirthEffect).Spawn(unitId, fromCard, startingApplications,
                    additionalStatus: additionalStatus);
        }

        internal static RoomCombatResult PrepareSpawnTriggers(RoomCombatState state, int unitId, bool fromCard,
            IReadOnlyList<CombatStatus> startingApplications, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            SpawnStatusApplication? additionalStatus = null)
        {
            string? error = Validate(state);
            if (error != null || !state.Units.Any(unit => unit.Id == unitId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Missing birth actor.");
            var callbacks = new List<QueuedCharacterTrigger>();
            if (state.Context?.QueryFrame?.MoonPhase is int moon && (moon == 1 || moon == 2))
                callbacks.Add(new QueuedCharacterTrigger(state.RoomIndex, state.Units.Single(unit => unit.Id == unitId), moon == 1 ? "OnMoonShade" : "OnMoonLit"));
            RoomCombatResult result = new Engine(state, new List<CombatEvent>(), pendingSummonCardId: fromCard ?
                state.Units.Single(unit => unit.Id == unitId).SpawnerCardId : 0, enqueueCharacterTrigger: callbacks.Add, refreshPyre: refreshPyre)
                .Spawn(unitId, fromCard, startingApplications, prepareOnly: true, additionalStatus: additionalStatus);
            return new RoomCombatResult(result.State, result.Outcome, 0, result.Events.ToList(), result.UnsupportedReason, callbacks);
        }

        public static RoomCombatResult ApplyCardDamage(RoomCombatState state, int targetId, int damage, int sourceCardId = 0,
            bool deferSpawnerExhaustion = false, int attackerUnitId = 0, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null) => ApplyCardDamage(state, targetId, damage, sourceCardId, deferSpawnerExhaustion, true, attackerUnitId, refreshPyre: refreshPyre);

        internal static RoomCombatResult ApplyRelicDamage(RoomCombatState state, int targetId, int damage,
            Action<QueuedCharacterTrigger> enqueueCharacterTrigger) =>
            ApplyNativeCardDamageAfterTraits(state, targetId, damage, 0, enqueueCharacterTrigger: enqueueCharacterTrigger,
                captureAllDispatches: true);

        internal static RoomCombatResult ApplyCardDamageAfterTraits(RoomCombatState state, int targetId, int damage, int sourceCardId,
            bool deferSpawnerExhaustion, int attackerUnitId = 0, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            CombatUnit? externalActor = null, int externalActorRoomIndex = -1,
            Action<QueuedCharacterTrigger>? enqueueCharacterTrigger = null) => ApplyCardDamage(state, targetId, damage, sourceCardId,
                deferSpawnerExhaustion, false, attackerUnitId, refreshPyre, externalActor, externalActorRoomIndex, enqueueCharacterTrigger);

        internal static RoomCombatResult ApplyNativeCardDamageAfterTraits(RoomCombatState state, int targetId, int damage, int sourceCardId,
            int attackerUnitId = 0,
            Func<RoomCombatState, RoomCombatResult>? finishDamageQueue = null, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            CombatUnit? externalActor = null, int externalActorRoomIndex = -1,
            Action<QueuedCharacterTrigger>? enqueueCharacterTrigger = null, bool captureAllDispatches = false)
        {
            string? error = Validate(state);
            bool localAttacker = state.Units.Any(unit => unit.Id == attackerUnitId);
            if (error != null || damage < 0 || state.Preview || state.Context?.SpawnPoints == null ||
                !state.Units.Any(unit => unit.Id == targetId) ||
                attackerUnitId != 0 && !localAttacker && (externalActor == null || externalActor.Id != attackerUnitId ||
                    externalActor.Health <= 0 || externalActorRoomIndex < 0 || externalActorRoomIndex == state.RoomIndex ||
                    state.Units.Any(unit => unit.Id == externalActor.Id)) ||
                externalActor != null && (externalActor.Id != attackerUnitId || localAttacker || externalActorRoomIndex < 0 ||
                    externalActorRoomIndex == state.RoomIndex || externalActor.Health <= 0))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Invalid native spell damage phase.");
            return new Engine(state, new List<CombatEvent>(), nativeCardDamage: true,
                finishDamageQueue: finishDamageQueue, refreshPyre: refreshPyre,
                enqueueCharacterTrigger: enqueueCharacterTrigger, externalActor: externalActor,
                externalActorRoomIndex: externalActorRoomIndex, externalActorInRoom: false)
                .TraceAllDispatches(captureAllDispatches)
                .CardDamage(targetId, damage, sourceCardId, false, attackerUnitId);
        }

        private static RoomCombatResult ApplyCardDamage(RoomCombatState state, int targetId, int damage, int sourceCardId,
            bool deferSpawnerExhaustion, bool applyTraits, int attackerUnitId,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            CombatUnit? externalActor = null, int externalActorRoomIndex = -1,
            Action<QueuedCharacterTrigger>? enqueueCharacterTrigger = null, bool captureAllDispatches = false)
        {
            string? error = Validate(state);
            if (error != null || damage < 0 || !state.Units.Any(unit => unit.Id == targetId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Invalid card damage target or amount.");
            bool localAttacker = state.Units.Any(unit => unit.Id == attackerUnitId);
            if (attackerUnitId != 0 && !localAttacker && (externalActor == null || externalActor.Id != attackerUnitId ||
                externalActor.Health <= 0 || externalActorRoomIndex < 0 || externalActorRoomIndex == state.RoomIndex ||
                state.Units.Any(unit => unit.Id == externalActor.Id)))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Cross-room spell attacker requires a retained source unit.");
            if (externalActor != null && (externalActor.Id != attackerUnitId || localAttacker || externalActorRoomIndex < 0 ||
                externalActorRoomIndex == state.RoomIndex || externalActor.Health <= 0))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Invalid external spell attacker.");
            return new Engine(state, new List<CombatEvent>(), deferSpawnerExhaustion,
                enqueueCharacterTrigger: enqueueCharacterTrigger, refreshPyre: refreshPyre,
                externalActor: externalActor, externalActorRoomIndex: externalActorRoomIndex, externalActorInRoom: false)
                .TraceAllDispatches(captureAllDispatches)
                .CardDamage(targetId, damage, sourceCardId, applyTraits, attackerUnitId);
        }

        internal static RoomCombatResult ApplyUnitModification(RoomCombatState state, CombatUnit changed, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null) =>
            new Engine(state, new List<CombatEvent>(), refreshPyre: refreshPyre).ModifyUnit(changed);

        // The native revival API queues callbacks but leaves their execution to its caller.
        public static RoomCombatResult ApplyRevival(RoomCombatState state, int targetId, int sourceCardId = 0, int attackerUnitId = 0,
            CombatUnit? retainedAttacker = null, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            string? error = Validate(state, targetId);
            if (error != null || !state.Units.Any(unit => unit.Id == targetId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Invalid revival actor.");
            if (retainedAttacker != null && (retainedAttacker.Id != attackerUnitId ||
                state.Units.Any(unit => unit.Id == attackerUnitId)))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Invalid retained revival attacker.");
            if (attackerUnitId != 0 && retainedAttacker == null && !state.Units.Any(unit => unit.Id == attackerUnitId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Missing revival attacker reference.");
            var callbacks = new List<QueuedCharacterTrigger>();
            RoomCombatResult result = new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: callbacks.Add,
                resetPreviewTriggers: false, refreshPyre: refreshPyre).Revival(targetId, sourceCardId, attackerUnitId, retainedAttacker);
            return new RoomCombatResult(result.State, result.Outcome, result.Rounds, result.Events.ToList(),
                result.UnsupportedReason, callbacks, result.RetainedUnits, result.Dispatches);
        }

        internal static RoomCombatResult ApplyUnitUpgrade(RoomCombatState state, int targetId, CardUpgradeModifier upgrade,
            string lifetime, bool remove, int? roomCapacity, int sourceCardId, string? triggerKind, bool scaleUpgrade = false,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, bool accumulateScaledUpgradeRecord = true)
        {
            string? error = Validate(state);
            if (error != null || !state.Units.Any(unit => unit.Id == targetId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Missing unit upgrade target.");
            return new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false, refreshPyre: refreshPyre)
                .UnitUpgrade(targetId, upgrade, lifetime, remove, roomCapacity, sourceCardId, triggerKind,
                    scaleUpgrade: scaleUpgrade, accumulateScaledUpgradeRecord: accumulateScaledUpgradeRecord);
        }

        internal static RoomCombatResult ApplyDirectUnitUpgrade(RoomCombatState state, int targetId, CardUpgradeModifier upgrade,
            bool remove, string upgradeId, int? anonymousRemovalIndex, bool deferCallbacks = false, int equipmentSourceCardId = 0, bool captureAllDispatches = false, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            string? error = Validate(state);
            if (error != null || !state.Units.Any(unit => unit.Id == targetId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Missing direct upgrade target.");
            var callbacks = new List<QueuedCharacterTrigger>();
            // RemoveCardUpgrade queues callbacks without running them. Additions run the queue.
            RoomCombatResult result = new Engine(state, new List<CombatEvent>(),
                enqueueCharacterTrigger: remove || deferCallbacks ? callbacks.Add : (Action<QueuedCharacterTrigger>?)null, resetPreviewTriggers: false, refreshPyre: refreshPyre)
                .TraceAllDispatches(captureAllDispatches)
                .UnitUpgrade(targetId, upgrade, "TemporaryUntilUnitDeath", remove, null, 0, null,
                    directApi: true, upgradeId: upgradeId, anonymousRemovalIndex: anonymousRemovalIndex, equipmentSourceCardId: equipmentSourceCardId);
            return new RoomCombatResult(result.State, result.Outcome, result.Rounds, result.Events.ToList(), result.UnsupportedReason, callbacks, result.RetainedUnits, result.Dispatches);
        }

        public static RoomCombatResult ApplyEquipment(RoomCombatState state, int targetId, int cardId, BattlePlayRules definitions, bool remove = false,
            bool deferAbilityCallbacks = false, bool deferCallbacks = false, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            string? error = Validate(state);
            CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || target?.EquipmentCards == null || target.Modifiers == null || state.Context?.OtherPiles == null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Missing equipment unit/pile state.");
            if (!remove && target.EquipmentCards.Contains(cardId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Repeated raw attachment of the same equipment object is not modeled.");
            var callbacks = new List<QueuedCharacterTrigger>();
            var result = new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: remove || deferCallbacks ? callbacks.Add : (Action<QueuedCharacterTrigger>?)null,
                resetPreviewTriggers: false, deferAbilityCallbacks: deferAbilityCallbacks, refreshPyre: refreshPyre).Equipment(targetId, cardId, definitions, remove);
            return new RoomCombatResult(result.State, result.Outcome, result.Rounds, result.Events.ToList(), result.UnsupportedReason, callbacks);
        }

        public static RoomCombatResult ApplyCardHeal(RoomCombatState state, int targetId, int amount, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null)
        {
            string? error = Validate(state);
            CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || amount < 0 || target?.Modifiers == null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Card healing requires a valid amount, target and healability state.");
            return new Engine(state, new List<CombatEvent>(), refreshPyre: refreshPyre).CardHeal(targetId, amount);
        }

        private static RoomCombatResult Run(RoomCombatState state, bool entireRoom,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, bool updateSelectedRoom = true,
            CombatUnit? externalPyre = null, int externalPyreRoomIndex = -1,
            IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null)
        {
            var events = new List<CombatEvent>();
            string? error = Validate(state);
            if (error != null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events, error);
            var engine = new Engine(state, events, refreshPyre: refreshPyre,
                externalPyre: externalPyre, externalPyreRoomIndex: externalPyreRoomIndex, externalTargets: externalTargets);
            return engine.Run(entireRoom, updateSelectedRoom);
        }

        internal static string? Validate(RoomCombatState state, int? dyingTargetId = null)
        {
            string? relicError = RelicModel.Validate(state.Context);
            if (relicError != null) return relicError;
            if (state.Context?.Enchantments?.AutomaticLifecycle == false && state.Context.Enchantments.Rooms.SelectMany(room => room.Units).Any(unit =>
                unit.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Type == "CardEffectEnchant"))) == true)
                return "CardEffectEnchant in the shared train requires automatic birth/movement/death lifecycle integration.";
            string? identityError = UnitIdentityModel.Validate(state.Context, state.Units);
            if (identityError != null) return identityError;
            if (state.Context != null && AbilityCardModel.Validate(state.Context) is string cacheError) return cacheError;
            if (state.ExternalInteractions.Count > 0)
                return string.Join("; ", state.ExternalInteractions);
            foreach (CardInstanceState instance in state.Context?.CardInstances ?? Array.Empty<CardInstanceState>())
            {
                string? unsupported = CardModifierModel.UnsupportedReason(instance);
                if (unsupported != null && !RelicCardGenerationModel.CanDeferUnplayedGraftedCard(state.Context, instance, state.Units))
                    return unsupported;
            }
            if (state.Units.Select(unit => unit.Id).Distinct().Count() != state.Units.Count)
                return "Unit instance IDs must be unique.";
            bool requireStatusDamage = state.Context?.Relics?.Any(relic => relic.StatusMagnitudes?.Count > 0 ||
                relic.StatusDecays?.Any(rule => rule.StatusIds.Contains("buff")) == true) == true;
            foreach (CombatUnit unit in state.Units)
            {
                if (unit.RegisteredStatus("conduit") != null && !ConduitModel.Captured(state.Context))
                    return "Conduit requires captured native room/card tracker state.";
                if (unit.Status("mageblade")?.Stacks > 0 && unit.Modifiers?.MagebladeDamagePerMagicPower == null)
                    return "Missing native Mageblade cached attack coefficient.";
                if (unit.Modifiers?.MagebladeDamagePerMagicPower.HasValue == true &&
                    state.Context?.MagicPower?.Any(power => power.RoomIndex == state.RoomIndex && power.Team == unit.Team) != true)
                    return "Mageblade requires captured room magic power for this team.";
                if (requireStatusDamage && unit.Modifiers?.DamageBuffFromStatusEffects.HasValue != true)
                    return "Missing native status attack accumulator.";
                if (unit.Ability?.CardCreation != null && unit.Ability.DataId != unit.Ability.CardCreation.DataId)
                    return "Unit ability and cached card creation definitions differ.";
                if (unit.Ability?.Definition != null && (unit.Ability.DataId != unit.Ability.Definition.DataId || !unit.Ability.Definition.IsUnitAbility) ||
                    unit.Ability?.PreviousDefinition != null && unit.Ability.PreviousDataId != unit.Ability.PreviousDefinition.DataId)
                    return "Unit ability restoration definitions differ.";
                if (unit.NextTriggerId.HasValue ? unit.NextTriggerId < 0 ||
                    unit.Triggers.Any(trigger => !trigger.StateId.HasValue || trigger.StateId < 0 || trigger.StateId >= unit.NextTriggerId) ||
                    unit.Triggers.Select(trigger => trigger.StateId).Distinct().Count() != unit.Triggers.Count :
                    unit.Triggers.Any(trigger => trigger.StateId.HasValue))
                    return "Invalid persistent trigger identity allocation.";
                if (unit.Modifiers != null && unit.Modifiers.Upgrades.Any(upgrade => upgrade.ExternalInteractions.Count > 0))
                    return "Unmodeled applied unit upgrade interactions.";
                if (unit.Modifiers != null && (unit.Modifiers.HealthFromUpgrades.Any(item => item.Value < 0) ||
                    unit.Modifiers.HealthFromUpgrades.Select(item => item.Key).Distinct().Count() != unit.Modifiers.HealthFromUpgrades.Count))
                    return "Invalid attributed maximum-health ledger.";
                if (unit.Health < 0 || unit.Health == 0 && unit.Id != dyingTargetId || unit.Health > unit.MaxHealth)
                    return "Only living units with valid health can enter room combat.";
                if (unit.Status("horde") != null && (unit.HordeDefinition == null || unit.HordeDefinition.Health <= 0 || unit.Modifiers == null))
                    return "Horde requires authored troop stats and raw modifier fields.";
                if (unit.StatusDictionary?.Validate(unit.StatusRegistry) is string dictionaryError) return dictionaryError;
                if (unit.LastAttackerId < 0) return "Invalid last-attacker identity.";
                if (unit.Statuses.Select(status => status.Id).Distinct().Count() != unit.Statuses.Count)
                    return "Duplicate status IDs on unit " + unit.Id;
                if (unit.StatusRegistry != null && (unit.StatusRegistry.Select(status => status.Id).Distinct().Count() != unit.StatusRegistry.Count ||
                    unit.StatusRegistry.Any(status => status.Stacks < 0 || status.Stacks > 9999)))
                    return "Invalid retained status dictionary on unit " + unit.Id;
                foreach (CombatStatus status in unit.Statuses)
                    if (!KnownStatuses.Contains(status.Id))
                        return "Unmodeled status " + status.Id + " on " + unit.AssetKey;
                if (!state.Deployment && unit.Status("valor") != null &&
                    state.Context?.StatusRules.FirstOrDefault(rule => rule.Id == "armor") == null)
                    return "Valor requires the armor status definition.";
                foreach (CombatTrigger trigger in unit.Triggers)
                {
                    if (trigger.Conditions != null && trigger.Conditions.RequiredStatuses.Concat(trigger.Conditions.RequiredDyingStatuses).Any(string.IsNullOrWhiteSpace))
                        return "Invalid trigger status condition.";
                    if (trigger.Origin?.OnlyIfEquipped == true && unit.EquipmentCards == null)
                        return "Equipped-only triggers require equipment membership state.";
                    if (trigger.Origin != null && (trigger.Origin.EquipmentCardId < 0 ||
                        trigger.Origin.IsFromEquipment != (trigger.Origin.EquipmentCardId > 0)))
                        return "Invalid equipment trigger origin.";
                    if (trigger.Kind != "OnDeath" && trigger.Kind != "OnReanimated" && trigger.Kind != "OnBurnout" && trigger.Kind != "PostCombat" && trigger.Kind != "PostCombatHealing" && trigger.Kind != "OnHeal" &&
                        trigger.Kind != "CardMonsterPlayed" && trigger.Kind != "CardSpellPlayed" && trigger.Kind != "OnOwnAbilityActivated" && trigger.Kind != "OnPreOwnAbilityActivated" &&
                        trigger.Kind != "OnEquipmentAdded" && trigger.Kind != "OnEquipmentAddedToAny" && trigger.Kind != "OnEquipmentRemoved" &&
                        trigger.Kind != "OnSpawn" && trigger.Kind != "OnUnscaledSpawn" && trigger.Kind != "OnSpawnNotFromCard" && trigger.Kind != "AfterSpawnEnchant" &&
                        trigger.Kind != "OnTurnBegin" && trigger.Kind != "OnTeamTurnBegin" && trigger.Kind != "EndTurnPreHandDiscard" && trigger.Kind != "PreCombat" &&
                        trigger.Kind != "OnTrainRoomLoop" && trigger.Kind != "PostAscension" && trigger.Kind != "OnShift" && trigger.Kind != "OnSentry" &&
                        trigger.Kind != "PostDescension" && trigger.Kind != "PostAttemptedAscension" && trigger.Kind != "PostAttemptedDescension" &&
                        trigger.Kind != "OnHit" && trigger.Kind != "OnKill" && trigger.Kind != "OnAttackingBeforeDamage" && trigger.Kind != "OnAttacking" &&
                        !StatusCallbackModel.Kinds.Contains(trigger.Kind) && !DeathwishModel.CallbackKinds.Contains(trigger.Kind) &&
                        !HarvestModel.Kinds.Contains(trigger.Kind) && trigger.Kind is not ("OnRemoveHatch" or "OnHatched" or "CorruptionAdded") &&
                        trigger.Kind is not ("OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost" or "OnMoonPhaseShift"))
                        return "Unmodeled trigger " + trigger.Kind;
                    if (trigger.Kind != "OnDeath" && trigger.Kind != "PostCombat" && trigger.SkipDuringDeployment == null)
                        return trigger.Kind + " requires deployment timing state.";
                    if (trigger.TriggerAtThreshold > 0 && trigger.Kind != "OnDeath" && trigger.Kind != "OnHit" && trigger.Kind != "OnKill" &&
                        trigger.Kind != "OnAttackingBeforeDamage" && trigger.Kind != "OnAttacking" && trigger.Kind != "OnSentry" && trigger.Kind != "CardSpellPlayed" && !StatusCallbackModel.Kinds.Contains(trigger.Kind))
                        return "Threshold arguments are not modeled for " + trigger.Kind;
                    if (trigger.Kind == "OnNewStatusEffectAdded" && !unit.CountUniqueVisibleStatuses().HasValue)
                        return "New-status callbacks require the complete native status dictionary and display definitions.";
                    if (trigger.FireCount < 0 && state.Context?.TriggerCounts == null) return "Invalid trigger fire count.";
                    TriggerCountModel.Query(state.Context, unit, trigger, out string? countError);
                    if (countError != null) return countError;
                    foreach (CombatEffect effect in trigger.Effects)
                    {
                        if (effect.Type == RelicResourceModel.PermanentPyreAttackAdjustment)
                        {
                            CardActionEffect? adjustment = effect.Action;
                            if (!unit.IsPyre || trigger.Kind != "OnKill" || state.Context?.Resources == null ||
                                adjustment?.Type != "AdjustPyreAttack" || adjustment.Target != "Self" ||
                                adjustment.Value != effect.Value || adjustment.Range != null || adjustment.Statuses.Count != 0)
                                return "Permanent Pyre attack adjustment requires a live, non-preview Pyre OnKill with captured resources.";
                            continue;
                        }
                        if (effect.Type == "CardEffectEnchant")
                        {
                            if (state.Context?.Enchantments?.AutomaticLifecycle != true || effect.Enchantment == null)
                                return "CardEffectEnchant requires a captured automatic lifecycle world and effect definition.";
                            continue;
                        }
                        if (effect.Type == "CardEffectSpawnMonster")
                        {
                            string? summonError = TriggeredSummonModel.Validate(state, effect.Summon);
                            if (summonError != null) return summonError;
                            continue;
                        }
                        if (effect.Type is "CardEffectRemoveStatusEffectPerCorrupt" or "CardEffectSacrifice" or "CardEffectForceHatchCheck")
                        {
                            CardActionEffect? hatch = effect.Action;
                            CardTargetFilters? filter = hatch?.Filters;
                            if (hatch == null || hatch.Range != null || filter != null &&
                                (filter.Health != "Both" || filter.RequiredStatuses.Count != 0 ||
                                 filter.ExcludedStatuses.Count != 0 || filter.IgnoreBosses || filter.Subtype.Length != 0 ||
                                 filter.ExcludedSubtypes.Count != 0) ||
                                effect.Type == "CardEffectRemoveStatusEffectPerCorrupt" &&
                                (hatch.Type != "RemoveStatusPerCorrupt" || hatch.Target != "Self" || hatch.Statuses.Count != 1 ||
                                 hatch.Statuses[0].Id != "hatch" || hatch.Value != 1 || state.Context?.RoomCapacities == null) ||
                                effect.Type == "CardEffectSacrifice" &&
                                (hatch.Type != "Sacrifice" || hatch.Target != "Self" || hatch.DoNotFullyRemove != true ||
                                 hatch.Statuses.Count != 0 || hatch.Value != 0) ||
                                effect.Type == "CardEffectForceHatchCheck" &&
                                (hatch.Type != "ForceHatchCheck" || hatch.Target != "DropTargetCharacter" ||
                                 hatch.Statuses.Count != 0 || hatch.Value != 0))
                                return "Unsupported hatch trigger effect definition.";
                            continue;
                        }
                        if (effect.Type == "CardEffectSetUnitAbility" || effect.Type == "CardEffectRemoveAbility")
                        {
                            CardActionEffect? ability = effect.Action;
                            if (ability?.AbilityChange == null || effect.Type != "CardEffect" + ability.Type)
                                return "Missing lifecycle effect definition.";
                            if (!new[] { "Self", "Room", "FrontInRoom", "BackInRoom", "Weakest", "RandomInRoom", "LastAttackedCharacter", "LastSpawnedCharacter" }.Contains(ability.Target))
                                return "Unmodeled lifecycle effect target.";
                            string? filter = ability.Target == "LastAttackedCharacter" ? null : ability.Filters?.Validate();
                            if (filter != null) return filter;
                            continue;
                        }
                        if (effect.Type == "CardEffectResetCooldown" || effect.Type == "CardEffectAdjustAbilityCooldown" || effect.Type == "CardEffectRemoveStatusEffect")
                        {
                            CardActionEffect? ability = effect.Action;
                            if (ability == null || (effect.Type == "CardEffectRemoveStatusEffect" ? ability.Type != "RemoveStatus" :
                                effect.Type != "CardEffect" + ability.Type || !ability.CooldownParameter.HasValue))
                                return "Missing or mismatched ability effect definition.";
                            if (!new[] { "Self", "Room", "FrontInRoom", "BackInRoom", "Weakest", "RandomInRoom", "LastAttackedCharacter", "LastSpawnedCharacter" }.Contains(ability.Target))
                                return "Unmodeled ability effect target.";
                            if (ability.Type == "RemoveStatus")
                            {
                                string? removalError = StatusRemovalModel.Validate(ability, state);
                                if (removalError != null) return removalError;
                            }
                            if (ability.Type == "ResetCooldown" && state.Context?.StatusRules.All(rule => rule.Id != "cooldown") != false)
                                return "Missing cooldown status definition.";
                            string? filter = ability.Target == "LastAttackedCharacter" ? null : ability.Filters?.Validate();
                            if (filter != null) return filter;
                            continue;
                        }
                        if (EnergyModel.IsNativeEffect(effect.Type) || effect.Type == "CardEffectDrawAdditionalNextTurn" || effect.Type == "CardEffectAdjustRoomCapacity" || effect.Type == "CardEffectAdjustForgePoints")
                        {
                            CardActionEffect? energy = effect.Action;
                            bool typeMatches = effect.Type == "CardEffectAdjustRoomCapacity" ? energy?.Type == "AdjustCapacity" :
                                effect.Type == "CardEffectDrawAdditionalNextTurn" ? energy?.Type == "DrawNextTurn" :
                                effect.Type == "CardEffectAdjustForgePoints" ? energy?.Type == ForgePointsModel.Effect :
                                energy != null && EnergyModel.IsEffect(energy.Type) &&
                                    effect.Type == "CardEffect" + (energy.Type == "GainEnergyMonsterTurn" ? "GainEnergy" : energy.Type);
                            if (energy == null || !typeMatches)
                                return "Missing or mismatched triggered resource definition.";
                            string? energyError = energy.Type == "AdjustCapacity" ? RoomCapacityModel.Validate(state.Context) : energy.Type == "DrawNextTurn" ? state.Context?.Cards.BonusDraw == null ? "Missing bonus-draw state." :
                                BonusDrawModel.Validate(state.Context.Cards) : energy.Type == ForgePointsModel.Effect ? ForgePointsModel.Validate(state.Context) : EnergyModel.Validate(state.Context);
                            energyError = energyError ?? (energy.Type == "AdjustCapacity" ? null : energy.Range?.Validate()) ?? energy.Filters?.Validate();
                            if (energy.Upgrade?.ExternalInteractions.Count > 0) energyError = string.Join("; ", energy.Upgrade.ExternalInteractions);
                            if (energyError != null) return energyError;
                            if (!new[] { "Self", "Room", "FrontInRoom", "BackInRoom", "Weakest", "RandomInRoom", "LastAttackedCharacter", "LastSpawnedCharacter" }.Contains(energy.Target))
                                return "Unmodeled triggered energy target " + energy.Target;
                            continue;
                        }
                        if (effect.Type == "CardEffectAdvanceMoonPhase")
                        {
                            if (effect.Action?.Type != "AdvanceMoonPhase" || effect.Action.Target != "Self" ||
                                effect.Action.Range != null || effect.Action.Value != effect.Value)
                                return "Missing or mismatched triggered moon-phase definition.";
                            continue;
                        }
                        if (effect.Type == TriggeredStatusTransferModel.EffectType)
                        {
                            string? transferError = TriggeredStatusTransferModel.Validate(effect.Action);
                            if (transferError != null) return transferError;
                            continue;
                        }
                        if (effect.Type == "CardEffectNULL")
                        {
                            CardActionEffect? action = effect.Action;
                            if (action?.Type != "Null" || action.NullForwardSelf || effect.Count != 0 ||
                                action.Target is not ("Self" or "Room"))
                                return "Unmodeled triggered null-effect target forwarding.";
                            string? filter = action.Target == "Self" ? null : action.Filters?.Validate();
                            if (filter != null) return filter;
                            continue;
                        }
                        if (effect.Action != null && effect.Type != "CardEffectHeal" && effect.Type != "CardEffectDamage" && effect.Type != "CardEffectPyreDamageIfNoPlayerUnits" && effect.Type != "CardEffectAddStatusEffect" &&
                            effect.Type is not ("CardEffectAddStatusEffectIfNoPlayerUnits" or "CardEffectAddStatusEffectPerSelfAttack" or "CardEffectAddStatusEffectBasedOnHealth"))
                            return "Mismatched triggered action definition.";
                        if (effect.DamageStatusMultiplier != null && effect.Type is not ("CardEffectDamage" or "CardEffectPyreDamageIfNoPlayerUnits"))
                            return "Mismatched triggered damage multiplier.";
                        if (effect.StatusScaling != null && effect.Type is not ("CardEffectAddStatusEffect" or "CardEffectAddStatusEffectIfNoPlayerUnits" or
                            "CardEffectAddStatusEffectPerSelfAttack" or "CardEffectAddStatusEffectBasedOnHealth")) return "Mismatched triggered status scaling.";
                        if (effect.Type == "CardEffectDespawnCharacter") continue;
                        if (effect.Type == "CardEffectHeal" || effect.Type == "CardEffectDamage" || effect.Type == "CardEffectPyreDamageIfNoPlayerUnits" ||
                            effect.Type is "CardEffectAddStatusEffect" or "CardEffectAddStatusEffectIfNoPlayerUnits" or "CardEffectAddStatusEffectPerSelfAttack" or "CardEffectAddStatusEffectBasedOnHealth")
                        {
                            CardActionEffect? action = effect.Action;
                            if (action?.Type != (effect.Type == "CardEffectHeal" ? "Heal" : effect.Type is "CardEffectDamage" or "CardEffectPyreDamageIfNoPlayerUnits" ? "Damage" : "AddStatus")) return "Missing triggered action definition.";
                            if (effect.Type == "CardEffectPyreDamageIfNoPlayerUnits" && action.Target != "Pyre")
                                return "Conditional Pyre damage requires a Pyre target.";
                            if (action.Type == "AddStatus")
                            {
                                string? statusError = TriggeredStatusModel.Validate(effect);
                                if (statusError != null) return statusError;
                                if (state.Context == null) return "Triggered status requires shared battle context.";
                            }
                            if (action.Type == "Heal" && state.Units.Any(target => target.Modifiers == null))
                                return "Triggered healing requires unit healability state.";
                            if (!new[] { "Self", "Room", "Tower", "FrontInRoom", "BackInRoom", "Weakest", "RoomHealTargets", "RandomInRoom", "LastAttackedCharacter", "LastSpawnedCharacter", "Pyre" }.Contains(action.Target))
                                return "Unmodeled triggered action target " + action.Target;
                            if ((action.Range != null || action.Target == "RandomInRoom") && state.Context == null)
                                return "Triggered action randomness requires shared battle context.";
                            string? healError = (action.Target == "LastAttackedCharacter" ? null : action.Filters?.Validate()) ?? action.Range?.Validate();
                            if (healError != null) return healError;
                            continue;
                        }
                        if (effect.Type == "CardEffectAddCardUpgradeToUnits" || effect.Type == "CardEffectAddTempCardUpgradeToUnits" ||
                            effect.Type == "CardEffectRemoveTempUpgradeFromUnit" || effect.Type == "CardEffectBuffEquipment")
                        {
                            CardActionEffect? action = effect.UnitUpgrade;
                            if (action?.Upgrade == null || action.Type != (effect.Type == "CardEffectBuffEquipment" ? "BuffEquipment" : effect.Type == "CardEffectRemoveTempUpgradeFromUnit" ? "RemoveUnitUpgrade" : "UnitUpgrade"))
                                return "Missing triggered unit upgrade definition.";
                            if (state.Context?.CardInstances == null || unit.Modifiers == null)
                                return "Triggered unit upgrades require unit and card modifier state.";
                            if (effect.Type == "CardEffectBuffEquipment" && unit.EquipmentCards == null)
                                return "Triggered equipment buffs require captured equipped-card ownership.";
                            if (effect.Type == "CardEffectBuffEquipment" && action.ScaleUpgrade)
                                return "Scaled triggered equipment buffs require independent native calibration.";
                            if (action.Upgrade.ExternalInteractions.Count > 0)
                                return string.Join("; ", action.Upgrade.ExternalInteractions);
                            if (action.Upgrade.RestrictSizeToRoomCapacity && state.Context?.RoomCapacities == null)
                                return "Triggered size restrictions require room capacity state.";
                            if (action.Target != "Self" && !new[] { "Room", "FrontInRoom", "BackInRoom", "Weakest", "RoomHealTargets", "RandomInRoom", "LastAttackedCharacter", "LastSpawnedCharacter" }.Contains(action.Target))
                                return "Unmodeled triggered upgrade target " + action.Target;
                            if (action.Range != null) return "Triggered upgrade range initialization is not modeled.";
                            string? filterError = action.Target == "LastAttackedCharacter" ? null : action.Filters?.Validate();
                            if (filterError != null) return filterError;
                            continue;
                        }
                        if (effect.Type != "CardEffectRewardGold" && effect.Type != "CardEffectAddBattleCard")
                            return "Unmodeled effect " + effect.Type;
                        if (state.Context == null) return "Unit effects require shared battle context.";
                        if (effect.Type == "CardEffectRewardGold" && effect.Value < 0)
                            return "Conditional gold costs are not modeled.";
                        if (effect.Type == "CardEffectAddBattleCard" &&
                            (effect.Generation == null && effect.CardPool.Count == 0 || !new[] { "DeckPile", "DeckPileTop", "DeckPileRandom",
                                "HandPile", "DiscardPile" }.Contains(effect.Destination)))
                            return "Unmodeled generated card destination or empty pool.";
                    }
                }
            }
            return null;
        }

        private static CombatUnit MarkBeingRemoved(CombatUnit unit) => unit.DeathState == null ? unit :
            unit.WithDeathState(new UnitDeathState(unit.DeathState.HasFinishedDying, true,
                unit.DeathState.HasStatisticsListener, unit.DeathState.PendingStatisticsCardId, unit.DeathState.IsSacrifice, unit.DeathState.StatisticsListenerOnce));

        private sealed class WorkingUnit
        {
            internal CombatUnit Source;
            internal int Health;
            internal readonly Dictionary<string, CombatStatus> Statuses;
            internal readonly List<CombatTrigger> Triggers;
            internal bool Despawned;
            internal bool Destroyed;
            internal bool DeathFinished;
            internal bool Removed;
            internal bool InRoom = true;
            internal int? LastAttackerId;
            private IReadOnlyList<CombatStatus>? statusRegistry;
            private StatusDictionaryState? dictionary;
            private readonly int roomIndex;
            internal int RoomIndex => roomIndex;
            private readonly Func<CombatContext?>? getContext;
            private readonly Action<string>? reject;
            internal bool Alive => Health > 0 && !Removed && !Despawned;
            internal int Attack
            {
                get
                {
                    int mageblade = 0;
                    if (Source.Modifiers?.MagebladeDamagePerMagicPower is int coefficient)
                    {
                        RoomMagicPower? power = getContext?.Invoke()?.MagicPower?.FirstOrDefault(value =>
                            value.RoomIndex == roomIndex && value.Team == Source.Team);
                        mageblade = unchecked(coefficient * (power?.Value ?? 0));
                    }
                    return Math.Max(0, unchecked(Source.AttackBeforeStatuses +
                        (Source.Modifiers?.DamageBuffFromStatusEffects ?? unchecked(Amount("buff") + Amount("valor") - Amount("debuff"))) +
                        RelicStatusAttackModel.AdditionalAttack(Source.Modifiers?.StatusAttackModifiers, Source.Team, Statuses.Values.ToArray()) +
                        mageblade + (Source.IsPyre ? getContext?.Invoke()?.Resources?.PyreModifiedBaseAttack ?? 0 : 0)));
                }
            }
            internal int Attacks => CombatUnit.AttackCount(Statuses.Values);

            internal WorkingUnit(CombatUnit source, int roomIndex, Func<CombatContext?>? getContext = null, Action<string>? reject = null)
            {
                this.roomIndex = roomIndex; this.getContext = getContext; this.reject = reject;
                Source = source; Health = source.Health; LastAttackerId = source.LastAttackerId;
                Despawned = source.DeathState?.IsDespawned == true; Destroyed = source.DeathState?.IsDestroyed == true;
                Removed = Destroyed;
                Statuses = source.Statuses.ToDictionary(status => status.Id, StringComparer.Ordinal);
                statusRegistry = source.StatusRegistry; dictionary = source.StatusDictionary;
                Triggers = source.Triggers.ToList();
            }

            internal bool Has(string id) => Statuses.ContainsKey(id);
            internal void Apply(CombatUnit changed)
            {
                Source = changed; Health = changed.Health; LastAttackerId = changed.LastAttackerId;
                Despawned = changed.DeathState?.IsDespawned ?? Despawned; Destroyed = changed.DeathState?.IsDestroyed ?? Destroyed;
                statusRegistry = changed.StatusRegistry; dictionary = changed.StatusDictionary;
                Statuses.Clear();
                foreach (CombatStatus status in changed.Statuses) Statuses.Add(status.Id, status);
                Triggers.Clear(); Triggers.AddRange(changed.Triggers);
            }
            internal int Amount(string id) => Statuses.TryGetValue(id, out CombatStatus? status)
                ? status.ParamInt * status.Stacks : 0;
            internal int Count(string id) => Statuses.TryGetValue(id, out CombatStatus? status) ? status.Stacks : 0;
            internal void Remove(string id, int count)
            {
                SyncRegistry();
                if (!Statuses.TryGetValue(id, out CombatStatus? status)) return;
                if (status.Stacks <= count) Statuses.Remove(id);
                else Statuses[id] = status.WithStacks(status.Stacks - count);
            }
            private void SyncRegistry()
            {
                if (statusRegistry != null)
                {
                    statusRegistry = CombatUnit.MergeStatusRegistry(statusRegistry, Statuses.Values.ToArray());
                    if (dictionary != null) { dictionary = dictionary.Sync(statusRegistry); statusRegistry = dictionary.Order(statusRegistry); }
                }
            }
            internal IReadOnlyList<CombatStatus> RegisteredStatuses()
            { SyncRegistry(); return statusRegistry ?? Statuses.Values.ToArray(); }
            internal void RemoveDefinition(string id)
            { SyncRegistry(); if (statusRegistry != null) statusRegistry = statusRegistry.Where(status => status.Id != id).ToArray(); dictionary = dictionary?.Remove(id); }
            internal CombatUnit Freeze()
            {
                SyncRegistry();
                var result = new CombatUnit(Source.Id, Source.AssetKey, Source.Team,
                    Source.BaseAttack, Health, Source.MaxHealth, Source.CanAttack, Source.IsPyre,
                    Source.EndsBattleOnDeath, Statuses.Values.ToArray(), Triggers, Source.SpawnerCardId, Source.Size, Source.StatusImmunities, Source.Subtypes, Source.Modifiers, Source.IsBoss, LastAttackerId, statusRegistry, Source.EquipmentCards, Source.NextTriggerId, Source.Ability, dictionary, Source.AbilityRules, Source.HordeDefinition, Source.IsSpawning, Source.SacrificeCardId,
                    Source.DeathState?.WithLifecycle(Source.DeathState.IsDespawned.HasValue ? Despawned : (bool?)null,
                        Source.DeathState.IsDestroyed.HasValue ? Destroyed : (bool?)null), bumpRules: Source.BumpRules);
                result = TriggerCountModel.Refresh(getContext?.Invoke(), result, out string? error);
                if (error != null) reject?.Invoke(error);
                return result;
            }
        }

        private sealed class Engine
        {
            private bool traceAllDispatches;
            internal Engine TraceAllDispatches(bool enabled) { traceAllDispatches = enabled; return this; }
            private readonly RoomCombatState source;
            private readonly List<WorkingUnit> units;
            private readonly List<CombatEvent> events;
            private readonly List<CharacterTriggerDispatch> dispatches = new List<CharacterTriggerDispatch>();
            private bool battleWon;
            private int round;
            private CombatContext? context;
            private readonly bool deferSpawnerExhaustion;
            private readonly int pendingSummonCardId;
            private readonly Action<QueuedCharacterTrigger>? enqueueCharacterTrigger;
            private readonly bool deferAbilityCallbacks;
            private readonly bool nativeCardDamage;
            private readonly Func<RoomCombatState, RoomCombatResult>? finishDamageQueue;
            private readonly Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre;
            private readonly Func<RoomCombatState, CardActionEffect, int, MoonPhaseTransitionResult>? moonPhase;
            private readonly Func<RoomCombatState, int, IReadOnlyList<QueuedCharacterTrigger>, RoomCombatResult>? relicBirthEffect;
            private readonly Queue<(int RoomIndex, WorkingUnit Unit, string Kind, bool CanFire, int ParamInt, WorkingUnit? OverrideTarget, int ParamInt2, string? ParamString, WorkingUnit? DyingCharacter, int TriggerCount, int LastSpawnedOverrideUnitId, int? ExclusiveTriggerStateId)> triggerQueue = new Queue<(int, WorkingUnit, string, bool, int, WorkingUnit?, int, string?, WorkingUnit?, int, int, int?)>();
            private bool runningTriggerQueue;
            private bool stopAfterBossRemoval;
            private bool killCamActivated;
            // Older captures omitted the identity store. Retain observed source cards while
            // this operation finishes, without inventing unobserved references in its output.
            private IReadOnlyList<CardInstanceState>? legacyDetachedCards;
            private readonly HashSet<int> cannotAttackOrHeal = new HashSet<int>();
            private readonly HashSet<int> cannotFireTriggers = new HashSet<int>();
            private readonly HashSet<int> externalTurnActorIds = new HashSet<int>();
            private readonly bool hasExternalTargetRoster;
            private readonly HashSet<int> activeCompanionBossIds;
            private string? unsupportedReason;

            internal Engine(RoomCombatState source, List<CombatEvent> events, bool deferSpawnerExhaustion = false, int pendingSummonCardId = 0,
                Action<QueuedCharacterTrigger>? enqueueCharacterTrigger = null, bool resetPreviewTriggers = true, bool deferAbilityCallbacks = false,
                bool nativeCardDamage = false,
                Func<RoomCombatState, RoomCombatResult>? finishDamageQueue = null,
                Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
                CombatUnit? externalActor = null, int externalActorRoomIndex = -1, bool externalActorInRoom = true,
                Func<RoomCombatState, CardActionEffect, int, MoonPhaseTransitionResult>? moonPhase = null,
                CombatUnit? externalPyre = null, int externalPyreRoomIndex = -1,
                Func<RoomCombatState, int, IReadOnlyList<QueuedCharacterTrigger>, RoomCombatResult>? relicBirthEffect = null,
                IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null)
            {
                this.source = source; this.events = events;
                this.deferSpawnerExhaustion = deferSpawnerExhaustion;
                this.pendingSummonCardId = pendingSummonCardId;
                this.enqueueCharacterTrigger = enqueueCharacterTrigger;
                this.deferAbilityCallbacks = deferAbilityCallbacks;
                this.nativeCardDamage = nativeCardDamage;
                this.finishDamageQueue = finishDamageQueue;
                this.refreshPyre = refreshPyre;
                this.moonPhase = moonPhase;
                this.relicBirthEffect = relicBirthEffect;
                context = source.Context;
                activeCompanionBossIds = new HashSet<int>(source.ActiveCompanionBossIds ?? Array.Empty<int>());
                units = source.Units.Select(unit => NewUnit(unit)).ToList();
                if (externalActor != null)
                {
                    if (units.Any(unit => unit.Source.Id == externalActor.Id))
                        throw new ArgumentException("External actor already belongs to the combat room.", nameof(externalActor));
                    units.Add(NewUnit(externalActor, externalActorInRoom,
                        externalActorRoomIndex >= 0 ? externalActorRoomIndex : source.RoomIndex));
                    externalTurnActorIds.Add(externalActor.Id);
                }
                if (externalPyre != null)
                {
                    if (!externalPyre.IsPyre || externalPyreRoomIndex < 0 ||
                        units.Any(unit => unit.Source.Id == externalPyre.Id))
                        throw new ArgumentException("Invalid external Pyre target.", nameof(externalPyre));
                    units.Add(NewUnit(externalPyre, false, externalPyreRoomIndex));
                    externalTurnActorIds.Add(externalPyre.Id);
                }
                hasExternalTargetRoster = externalTargets != null;
                if (externalTargets != null)
                {
                    var ids = units.Select(unit => unit.Source.Id).ToHashSet();
                    foreach (var external in externalTargets)
                    {
                        if (external.RoomIndex < 0 || !ids.Add(external.Unit.Id))
                            throw new ArgumentException("Invalid or duplicated external Tower target.", nameof(externalTargets));
                        units.Add(NewUnit(external.Unit, false, external.RoomIndex));
                        externalTurnActorIds.Add(external.Unit.Id);
                    }
                }
                if (source.Preview && resetPreviewTriggers)
                    foreach (WorkingUnit unit in units)
                        for (int index = 0; index < unit.Triggers.Count; index++) unit.Triggers[index] = unit.Triggers[index].ForPreview();
                context = source.Context;
                killCamActivated = context?.KillCamActivated == true;
            }

            private WorkingUnit NewUnit(CombatUnit unit, bool inRoom = true, int? roomIndex = null) =>
                new WorkingUnit(unit, roomIndex ?? source.RoomIndex, () => context, error => unsupportedReason = error) { InRoom = inRoom };

            private bool HasLivingPairedCompanionBoss(int unitId)
            {
                foreach (int companionId in activeCompanionBossIds)
                {
                    if (companionId == unitId) continue;
                    WorkingUnit? companion = units.FirstOrDefault(unit => unit.Source.Id == companionId);
                    if (companion == null || companion.Alive || companion.Count("undying") > 0) return true;
                }
                return false;
            }

            private bool IsTerminalBossDeath(WorkingUnit unit) => !HasLivingPairedCompanionBoss(unit.Source.Id) &&
                (unit.Source.EndsBattleOnDeath || unit.Source.IsBoss == true && activeCompanionBossIds.Contains(unit.Source.Id));

            private IReadOnlyList<int>? CompanionBossSnapshot()
            {
                if (activeCompanionBossIds.Count > 0) return activeCompanionBossIds.ToArray();
                // An explicitly captured empty store differs from an omitted legacy snapshot.
                return source.ActiveCompanionBossIds == null ? null : Array.Empty<int>();
            }

            internal RoomCombatResult Run(bool entireRoom, bool updateSelectedRoom)
            {
                stopAfterBossRemoval = true;
                if (updateSelectedRoom && !source.Preview && context?.QueryFrame?.SelectedRoomIndex != null &&
                    units.Any(unit => unit.Alive && unit.InRoom && unit.Source.Team == CombatTeam.Enemy) &&
                    units.Any(unit => unit.Alive && unit.InRoom && unit.Source.Team == CombatTeam.Player && !unit.Source.IsPyre))
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: source.RoomIndex));
                bool relentless = units.Any(unit => unit.InRoom && unit.Has("relentless")) && BothTeamsPresent();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                bool repeat;
                do
                {
                    if (round > 0 && entireRoom) Clear(true);
                    // Compare the same phase: decay before the next exchange can break a apparent cycle.
                    if (entireRoom && !seen.Add(Signature()))
                        return Finish(RoomOutcome.Stalemate);
                    round++;
                    Exchange();
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    if (CombatPreviewStopped()) return Finish(RoomOutcome.Exchanged);
                    if (!entireRoom) return Finish(RoomOutcome.Exchanged);
                    if (battleWon) return Finish(RoomOutcome.BattleWon);
                    if (!source.Preview && units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                        return Finish(RoomOutcome.PlayerDefeated);
                    RemoveHatch();
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    PostCombat();
                    if (battleWon) return Finish(RoomOutcome.BattleWon);
                    if (!source.Preview && units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                        return Finish(RoomOutcome.PlayerDefeated);
                    Clear(false);
                    repeat = relentless && BothTeamsPresent();
                    if (context?.Relics?.Any(relic => relic.UnstableRebalances?.Any(rule =>
                        rule.AdditionalGrowthRate != 0) == true) == true)
                    {
                        UnstableGrowthResult growth = UnstableGrowthModel.StartTurn(new TrainCombatState(
                            [CurrentRoom()], Array.Empty<EnemyMovement>(), 0, context));
                        if (!growth.Supported) return Finish(RoomOutcome.Unsupported);
                        if (growth.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated)
                            return Finish(growth.Outcome);
                        context = growth.State!.Context;
                        foreach (CombatUnit changed in growth.State.Rooms.Single().Units)
                        {
                            WorkingUnit? current = units.FirstOrDefault(unit => unit.Source.Id == changed.Id);
                            if (current == null)
                            { unsupportedReason = "Unstable growth changed room membership between combat exchanges."; break; }
                            current.Apply(changed);
                        }
                        if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    }
                } while (repeat);
                AlmostPostRoomCombat();
                if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                if (battleWon) return Finish(RoomOutcome.BattleWon);
                if (!source.Preview && units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                    return Finish(RoomOutcome.PlayerDefeated);
                RunUnitPostCombat(updateSelectedRoom);
                if (!source.Deployment)
                    foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
                    {
                        WorkingUnit? front = units.FirstOrDefault(unit => unit.Alive && unit.InRoom && unit.Source.Team == team && !unit.Has("untouchable"));
                        if (front == null || !front.Has("valor") || front.Has("immune") || front.Source.StatusImmunities.Contains("armor")) continue;
                        int goal = Math.Min(9999, front.Amount("valor"));
                        // Valor calls AddStatusEffect even when existing armor already meets its target.
                        // That zero addition still queues status and armor callbacks in the native game.
                        RoomCombatResult added = StatusApplicationModel.ApplyRetained(CurrentRoom(), front.Source.Id,
                            context!.StatusRules.First(rule => rule.Id == "armor").WithStacks(Math.Max(0, goal - front.Count("armor"))), 0);
                        if (!added.Supported) { unsupportedReason = added.UnsupportedReason; return Finish(RoomOutcome.Unsupported); }
                        context = added.State!.Context;
                        foreach (CombatUnit changed in added.State.Units) units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                        foreach (QueuedCharacterTrigger callback in added.PendingCallbacks) QueueCallback(callback);
                    }
                if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                Clear(true);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Cleared);
            }

            internal RoomCombatResult UnitPostCombat(IReadOnlyList<int> cannotHeal, IReadOnlyList<int> cannotTrigger)
            {
                cannotAttackOrHeal.UnionWith(cannotHeal); cannotFireTriggers.UnionWith(cannotTrigger);
                RunUnitPostCombat(updateSelectedRoom: true);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            private void RunUnitPostCombat(bool updateSelectedRoom)
            {
                // Native snapshots enemies then players, preserving front-to-back order.
                // Each actor finishes its healing queue before its ordinary post-combat queue.
                bool anyUnitWillTrigger = units.Where(unit => unit.InRoom).OrderBy(unit => unit.Source.Team)
                    .SelectMany(unit => unit.Triggers.Where(trigger => trigger.Kind == "PostCombatHealing" || trigger.Kind == "PostCombat")
                        .Select(trigger => (Unit: unit, Trigger: trigger)))
                    .Any(item => (!source.Deployment || item.Trigger.SkipDuringDeployment != true) &&
                        ActionTriggerPassesTest(item.Unit, item.Trigger, 0));
                if (anyUnitWillTrigger && updateSelectedRoom && !source.Preview && context?.QueryFrame?.SelectedRoomIndex != null)
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: source.RoomIndex));
                foreach (WorkingUnit unit in units.Where(unit => unit.InRoom).OrderBy(unit => unit.Source.Team).ToArray())
                {
                    if (!unit.Alive) continue;
                    bool canFire = !cannotFireTriggers.Contains(unit.Source.Id);
                    if (!cannotAttackOrHeal.Contains(unit.Source.Id)) FireTriggers(unit, "PostCombatHealing", canFire);
                    FireTriggers(unit, "PostCombat", canFire);
                }
            }

            internal RoomCombatResult CardDamage(int targetId, int damage, int sourceCardId, bool applyTraits = true, int attackerUnitId = 0)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                Damage(units.FirstOrDefault(unit => unit.Source.Id == attackerUnitId), target, damage, "Spell", sourceCardId, applyTraits);
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult ModifyUnit(CombatUnit changed)
            {
                int index = units.FindIndex(unit => unit.Source.Id == changed.Id);
                WorkingUnit target = NewUnit(changed);
                units[index] = target;
                if (!target.Alive)
                {
                    target.Apply(target.Freeze().WithSacrifice(0));
                    if (!TryRevive(null, target, 0)) Death(null, target, 0);
                }
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult UnitUpgrade(int targetId, CardUpgradeModifier upgrade, string lifetime,
                bool remove, int? roomCapacity, int sourceCardId, string? triggerKind, bool directApi = false,
                bool scaleUpgrade = false,
                string upgradeId = "", int? anonymousRemovalIndex = null, int equipmentSourceCardId = 0,
                bool accumulateScaledUpgradeRecord = true)
            {
                ApplyUpgrade(units.Single(unit => unit.Source.Id == targetId), upgrade, lifetime, remove,
                    roomCapacity, sourceCardId, triggerKind, directApi, upgradeId, anonymousRemovalIndex,
                    equipmentSourceCardId, scaleUpgrade, accumulateScaledUpgradeRecord);
                if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult Equipment(int targetId, int cardId, BattlePlayRules definitions, bool remove)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                if (remove)
                {
                    if (cardId == 0)
                    {
                        foreach (int id in target.Source.EquipmentCards!.Reverse().ToArray()) if (!RemoveEquipment(target, id, definitions)) break;
                    }
                    else RemoveEquipment(target, cardId, definitions);
                }
                else if (!target.Source.EquipmentCards!.Contains(cardId))
                {
                    while (target.Source.EquipmentCards!.Count >= target.Source.Modifiers!.EquipmentLimit && target.Source.EquipmentCards.Count > 0)
                        if (!RemoveEquipment(target, target.Source.EquipmentCards[0], definitions)) return Finish(RoomOutcome.Unsupported);
                    if (!EquipmentUpgrades(cardId, definitions, out var card, out var upgrades)) return Finish(RoomOutcome.Unsupported);
                    SetEquipment(target, target.Source.EquipmentCards.Concat(new[] { cardId }).ToArray());
                    foreach (var upgrade in upgrades)
                        if (!ApplyUpgrade(target, upgrade, "TemporaryUntilUnitDeath", false, null, 0, null, true, EquipmentUpgradeKey(cardId, definitions),
                            equipmentSourceCardId: cardId))
                            return Finish(RoomOutcome.Unsupported);
                    if (!target.Removed)
                        foreach (WorkingUnit unit in units.Where(unit => !unit.Removed && unit.InRoom).OrderBy(unit => unit.Source.Team))
                            QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, unit.Freeze(), "OnEquipmentAddedToAny"));
                    QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, target.Freeze(), "OnEquipmentAdded"));
                    context = context!.WithCard((context.FindCard(cardId) ?? card!).WithEquippedUnit(targetId));
                    if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                }
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive) ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            private bool EquipmentUpgrades(int cardId, BattlePlayRules definitions, out CardInstanceState? card, out CardUpgradeModifier[] upgrades)
            {
                card = context?.FindCard(cardId); upgrades = Array.Empty<CardUpgradeModifier>();
                string? dataId = card?.DataId;
                EquipmentDefinition? definition = definitions.Cards.FirstOrDefault(rule => rule.DataId == dataId)?.Equipment;
                if (card?.EquippedUnitId == null || definition == null)
                { unsupportedReason = "Missing equipment definition or card relationship."; return false; }
                upgrades = definition.Upgrades.Concat(card.Permanent.Upgrades.Select((upgrade, index) => upgrade.WithEquipmentSource(cardId, index)))
                    .Concat(new[] { EquipmentModel.TemporaryAggregate(card,
                        definition.Upgrades.Any(upgrade => upgrade.Lifecycle != null)) }).ToArray();
                if (upgrades.Any(upgrade => upgrade.ExternalInteractions.Count > 0))
                { unsupportedReason = "Unmodeled equipment upgrade interactions."; return false; }
                return true;
            }

            private bool RemoveEquipment(WorkingUnit target, int cardId, BattlePlayRules definitions)
            {
                if (target.Source.EquipmentCards!.Contains(cardId))
                {
                    if (!EquipmentUpgrades(cardId, definitions, out _, out var upgrades)) return false;
                    foreach (var upgrade in upgrades)
                    {
                        int index = upgrade.DataId.Length == 0 && upgrade.EquipmentSourceCardId.HasValue
                            ? target.Source.Modifiers!.Upgrades.ToList().FindIndex(applied => applied.EquipmentSourceCardId == upgrade.EquipmentSourceCardId &&
                                applied.EquipmentSourceUpgradeIndex == upgrade.EquipmentSourceUpgradeIndex) : -1;
                        if (!ApplyUpgrade(target, upgrade, "TemporaryUntilUnitDeath", true, null, 0, null, true,
                            EquipmentUpgradeKey(cardId, definitions), index < 0 ? (int?)null : index)) return false;
                    }
                    SetEquipment(target, target.Source.EquipmentCards.Where(id => id != cardId).ToArray());
                    QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, target.Freeze(), "OnEquipmentRemoved"));
                }
                CardInstanceState? card = context?.FindCard(cardId);
                if (card != null) context = context!.WithCard(card.WithEquippedUnit(0));
                return true;
            }
            private static void SetEquipment(WorkingUnit target, IReadOnlyList<int> cards)
            {
                CombatUnit unit = target.Freeze();
                target.Apply(new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, unit.Health, unit.MaxHealth, unit.CanAttack,
                    unit.IsPyre, unit.EndsBattleOnDeath, unit.Statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities,
                    unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId, unit.StatusRegistry, cards, unit.NextTriggerId, unit.Ability, unit.StatusDictionary, unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState, bumpRules: unit.BumpRules));
            }
            private string EquipmentUpgradeKey(int cardId, BattlePlayRules definitions) => definitions.Cards
                .First(rule => rule.DataId == context!.FindCard(cardId)!.DataId).Equipment!.UpgradeId ?? EquipmentModel.UpgradeKey(cardId);

            internal RoomCombatResult CardHeal(int targetId, int amount)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                Heal(target, amount, "SpellHeal");
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult Spawn(int unitId, bool fromCard, IReadOnlyList<CombatStatus>? startingApplications = null,
                bool prepareOnly = false, SpawnStatusApplication? additionalStatus = null)
            {
                WorkingUnit spawned = units.Single(unit => unit.Source.Id == unitId);
                bool wasSpawning = spawned.Source.IsSpawning == true;
                if (context?.AbilityCardCache != null && spawned.Source.Ability?.CardCreation is CardCreationRule creation)
                {
                    AbilityCardResult cached = AbilityCardModel.Get(context, creation);
                    if (!cached.Supported) { unsupportedReason = cached.UnsupportedReason; return Finish(RoomOutcome.Unsupported); }
                    context = cached.Context;
                }
                if (startingApplications != null)
                {
                    var callbacks = new List<QueuedCharacterTrigger>();
                    unsupportedReason = StatusCallbackModel.Initialize(CurrentRoom(), spawned.Freeze(), startingApplications,
                        callbacks, spawned.Apply, ImportEnchantmentRoom, additionalStatus);
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    foreach (QueuedCharacterTrigger callback in callbacks) QueueCallback(callback);
                }
                if (spawned.Source.Team == CombatTeam.Player) spawned.Apply(HordeStatusModel.WithSpawning(spawned.Freeze(), true));
                if (!ApplyBirthRelics(true)) return Finish(RoomOutcome.Unsupported);
                if (context?.QueryFrame?.MoonPhase is int moon &&
                    MoonPhaseStatusModel.IsTracking(context, units.Select(unit => unit.Freeze())))
                {
                    spawned.Apply(MoonPhaseStatusModel.WithPhase(spawned.Freeze(), moon));
                    // The native birth updates the unit's moon understanding and
                    // queues its initial phase callback before the spawn triggers.
                    // Deferred births already carry this callback in their queue.
                    if (!prepareOnly) FireTriggers(spawned, moon == 1 ? "OnMoonShade" : "OnMoonLit");
                }
                if (!prepareOnly || spawned.Source.Team == CombatTeam.Player)
                {
                    if (!UpdateEnchantments()) return Finish(RoomOutcome.Unsupported);
                }
                if (prepareOnly)
                {
                    ImportEnchantmentRoom(EnchantmentWorldModel.Bind(CurrentRoom(), spawned.Source.Id));
                    return Finish(RoomOutcome.Exchanged);
                }
                if (!ApplyBirthRelics(false)) return Finish(RoomOutcome.Unsupported);
                RoomCombatState bound = EnchantmentWorldModel.Bind(CurrentRoom(), spawned.Source.Id);
                ImportEnchantmentRoom(bound);
                FireTriggers(spawned, "OnSpawn");
                FireTriggers(spawned, "OnUnscaledSpawn");
                if (!fromCard) FireTriggers(spawned, "OnSpawnNotFromCard");
                // Purify may reject every spawn trigger; starting-status callbacks
                // already accepted by native setup still need their normal drain.
                if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                spawned.Apply(HordeStatusModel.WithSpawning(spawned.Freeze(), wasSpawning));
                if (spawned.Alive && !spawned.Removed && context?.LastSpawnedUnitId.HasValue == true)
                    context = context.WithLastSpawned(spawned.Source.Id);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);

                bool ApplyBirthRelics(bool covenants)
                {
                    RoomCombatResult result = RelicSpawnStatusModel.CharacterAdded(CurrentRoom(), unitId,
                        fromCard ? spawned.Source.SpawnerCardId : 0, covenants);
                    if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                    var birthCallbacks = result.PendingCallbacks;
                    events.AddRange(result.Events);
                    CombatUnit? added = result.State!.Units.FirstOrDefault(unit => unit.Id == unitId);
                    if (!covenants && added?.Team == CombatTeam.Player && !added.IsPyre &&
                        result.State.Context?.Relics?.Any(relic => relic.SummonDamageRules?.Count > 0) == true)
                    {
                        if (relicBirthEffect == null)
                        { unsupportedReason = "Tower summon damage requires a complete-train birth callback."; return false; }
                        RoomCombatResult damaged = relicBirthEffect(result.State, unitId, birthCallbacks);
                        if (!damaged.Supported) { unsupportedReason = damaged.UnsupportedReason; return false; }
                        events.AddRange(damaged.Events);
                        if (damaged.Outcome != RoomOutcome.Exchanged) battleWon = damaged.Outcome == RoomOutcome.BattleWon;
                        result = new RoomCombatResult(damaged.State, damaged.Outcome, damaged.Rounds,
                            damaged.Events.ToList(), damaged.UnsupportedReason, damaged.PendingCallbacks,
                            damaged.RetainedUnits, damaged.Dispatches);
                    }
                    ImportEnchantmentRoom(result.State!);
                    foreach (QueuedCharacterTrigger callback in result.PendingCallbacks) QueueCallback(callback);
                    return true;
                }
            }

            internal RoomCombatResult UnitTurn(int unitId, bool ignoreAttackPrevention = false)
            {
                stopAfterBossRemoval = true;
                round = 1;
                Turn(units.Single(unit => unit.Source.Id == unitId), ignoreAttackPrevention);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult TeamTurnBegin(CombatTeam team)
            {
                BeginTeam(team);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult CharacterPhase(int unitId, string kind)
            {
                FireTriggers(units.Single(unit => unit.Source.Id == unitId), kind, fromQueue: true);
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult TriggeredSummonEffect(CombatUnit sourceActor, int triggerStateId, int effectIndex,
                out TriggeredSummonRule? resultingRule, IReadOnlyList<CombatUnit> retainedOccupants)
            {
                // Other deaths can still occupy physical slots after leaving the
                // live manager. Native ShiftSpawnPoints reads their actual HP.
                foreach (CombatUnit retained in retainedOccupants) units.Add(NewUnit(retained, false));
                WorkingUnit? actor = units.FirstOrDefault(unit => unit.Source.Id == sourceActor.Id);
                if (actor == null) { actor = NewUnit(sourceActor, false); units.Add(actor); }
                CombatTrigger trigger = actor.Triggers.Single(item => item.StateId == triggerStateId);
                TriggeredSummonRule rule = trigger.Effects[effectIndex].Summon!;
                resultingRule = rule;
                if (ApplyTriggeredSummon(actor, rule, out int firstId) && firstId > 0)
                {
                    resultingRule = rule.WithFirstSpawned(firstId);
                    int live = actor.Triggers.FindIndex(item => ReferenceEquals(item.Identity, trigger.Identity));
                    if (live >= 0)
                    {
                        var effects = actor.Triggers[live].Effects.ToArray();
                        effects[effectIndex] = effects[effectIndex].WithSummon(resultingRule);
                        actor.Triggers[live] = actor.Triggers[live].Fired(effects);
                    }
                }
                return Finish(battleWon ? RoomOutcome.BattleWon : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult QueuedTrigger(QueuedCharacterTrigger queued)
            {
                if (queued.Kind == DeathRoomFocusPhase)
                {
                    FocusAfterDeath(queued.Unit, queued.RoomIndex);
                    return Finish(RoomOutcome.Exchanged);
                }
                if (HarvestModel.Stage(queued.Kind, out string harvestKind, out CombatTeam harvestTeam))
                {
                    QueueHarvestGroup(queued.Unit, harvestKind, harvestTeam, queued.TriggerCount);
                    return Finish(RoomOutcome.Exchanged);
                }
                WorkingUnit? actor = units.FirstOrDefault(unit => unit.Source.Id == queued.Unit.Id);
                if (actor == null && (queued.Unit.DeathState?.IsDespawned == true || queued.Unit.DeathState?.IsDestroyed == true))
                { actor = NewUnit(queued.Unit, false); units.Add(actor); }
                if (actor == null && (queued.Kind == "OnDeath" || queued.Kind == "OnHit" || queued.Kind == "OnKill" ||
                    queued.Kind == "OnAttackingBeforeDamage" || queued.Kind == "OnAttacking" || queued.Kind == "OnSentry" || queued.Kind == "CardSpellPlayed" ||
                    StatusCallbackModel.Kinds.Contains(queued.Kind) || DeathwishModel.CallbackKinds.Contains(queued.Kind) || HarvestModel.Kinds.Contains(queued.Kind)))
                { actor = NewUnit(queued.Unit, false); units.Add(actor); }
                // A queued OnHeal on an actor killed by a later phase effect has no live effects.
                if (actor != null)
                {
                    if (queued.Kind == "OnSpawn" && actor.Source.Team == CombatTeam.Enemy && context?.Enchantments?.AutomaticLifecycle == true)
                    {
                        if (!UpdateEnchantments()) return Finish(RoomOutcome.Unsupported);
                        ImportEnchantmentRoom(EnchantmentWorldModel.Bind(CurrentRoom(), actor.Source.Id));
                    }
                    WorkingUnit? overridden = queued.OverrideTarget == null ? null : units.FirstOrDefault(unit => unit.Source.Id == queued.OverrideTarget.Id);
                    if (overridden == null && queued.OverrideTarget != null)
                    { overridden = NewUnit(queued.OverrideTarget, false); units.Add(overridden); }
                    WorkingUnit? dying = queued.DyingCharacter == null ? null : units.FirstOrDefault(unit => unit.Source.Id == queued.DyingCharacter.Id);
                    if (dying == null && queued.DyingCharacter != null)
                    { dying = NewUnit(queued.DyingCharacter, false); units.Add(dying); }
                    if (queued.Kind == "OnDeath" && actor.Source.DeathState?.PendingStatisticsCardId is int pendingSource)
                    {
                        DispatchDeathStatistics(null, actor, pendingSource, actor.Source.DeathState.IsSacrifice ?? actor.Source.SacrificeCardId.HasValue);
                        actor.Apply(actor.Freeze().WithDeathState(new UnitDeathState(actor.Source.DeathState.HasFinishedDying,
                            actor.Source.DeathState.IsBeingRemoved, actor.Source.DeathState.HasStatisticsListener,
                            isSacrifice: actor.Source.DeathState.IsSacrifice, statisticsListenerOnce: actor.Source.DeathState.StatisticsListenerOnce)));
                    }
                    // A removal entry schedules death bookkeeping, Harvest and spawner
                    // cleanup. Its native OnDeath request occurs only now, during removal.
                    bool blockedDeath = false;
                    if (queued.RemovalLifecycle && actor.Has("purify"))
                    {
                        if (context?.PurifyBlockedTriggers == null)
                        { unsupportedReason = "Purify requires captured trigger queue restrictions."; return Finish(RoomOutcome.Unsupported); }
                        blockedDeath = context.PurifyBlockedTriggers.Contains(queued.Kind);
                    }
                    if (!blockedDeath) FireTriggers(actor, queued.Kind, canFireTriggers: queued.CanFireTriggers, fromQueue: true, paramInt: queued.ParamInt,
                        overrideTarget: overridden, dyingCharacter: dying, triggerCount: queued.TriggerCount, lastSpawnedOverrideUnitId: queued.LastSpawnedOverrideUnitId,
                        exclusiveTriggerStateId: queued.ExclusiveTriggerStateId, paramInt2: queued.ParamInt2);
                    if (queued.Kind == "OnDeath" && queued.ParamInt != 1 && actor.Source.DeathState != null)
                        actor.Apply(actor.Freeze().WithDeathState(new UnitDeathState(true, true, false,
                            isSacrifice: actor.Source.DeathState.IsSacrifice, statisticsListenerOnce: actor.Source.DeathState.StatisticsListenerOnce.HasValue ? false : (bool?)null)));
                    queued.Unit = actor.Freeze();
                    if (overridden != null) queued.OverrideTarget = overridden.Freeze();
                    if (dying != null) queued.DyingCharacter = dying.Freeze();
                }
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult SacrificeRetained(CombatUnit unit, int sourceCardId, bool whileRunningQueue, bool doNotFullyRemove,
                bool returnSpawnerAfterQueue)
            {
                WorkingUnit? target = units.FirstOrDefault(item => item.Source.Id == unit.Id);
                if (target == null) { target = NewUnit(unit.WithSacrifice(sourceCardId), false); units.Add(target); }
                else target.Apply(unit.WithSacrifice(sourceCardId));
                target.Health = 0;
                if (TryRevive(null, target, sourceCardId)) return Finish(RoomOutcome.Exchanged);
                if (unit.DeathState?.IsBeingRemoved == true)
                {
                    DispatchDeathStatistics(null, target, sourceCardId, sacrifice: true);
                    target.Apply(target.Freeze().WithDeathState(new UnitDeathState(true, true, false,
                        isSacrifice: target.Source.DeathState?.IsSacrifice, statisticsListenerOnce: target.Source.DeathState?.StatisticsListenerOnce.HasValue == true ? false : (bool?)null)));
                    return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment,
                        source.Units.Select(item => item.Id == unit.Id ? target.Freeze() : item).ToArray(),
                        source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot()), RoomOutcome.Exchanged, 0, events);
                }
                // Native Sacrifice awaits CheckForDeath and its callbacks before
                // RemoveDeadCharacters clears and centers the physical positions.
                Death(null, target, sourceCardId,
                    deferRemoval: returnSpawnerAfterQueue && !doNotFullyRemove && context?.SpawnPoints != null,
                    sacrifice: true, immediateHarvest: whileRunningQueue,
                    doNotFullyRemove: doNotFullyRemove, returnSpawnerAfterQueue: returnSpawnerAfterQueue);
                if (whileRunningQueue)
                    foreach (string stage in HarvestModel.Stages(target.Freeze()))
                    {
                        HarvestModel.Stage(stage, out string kind, out CombatTeam team);
                        QueueHarvestGroup(target.Freeze(), kind, team, HarvestModel.Count(target.Freeze(), kind));
                    }
                return Finish(battleWon ? RoomOutcome.BattleWon : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult ReturnQueuedSpawner(CombatUnit unit, bool center = false, bool preservePhysicalPoint = false)
            {
                // Death queues retain their target after it leaves the live room.
                // Keep the completed removal in the shared aura world as well as
                // the physical point; otherwise a later callback sees its old flags.
                WorkingUnit? removed = units.FirstOrDefault(actor => actor.Source.Id == unit.Id);
                if (removed == null)
                { removed = NewUnit(unit, false); units.Add(removed); }
                SettleDeadSpawner(removed);
                bool ownedPoint = context?.SpawnPoints?.Units.FirstOrDefault(point => point.UnitId == unit.Id)?.Current != null;
                if (!preservePhysicalPoint) RemovePhysicalPoint(removed);
                if (center && ownedPoint && !preservePhysicalPoint) CenterAfterSummon();
                return Finish(RoomOutcome.Exchanged);
            }

            internal RoomCombatResult PendingDeathStatistics(CombatUnit unit)
            {
                var target = NewUnit(unit);
                if (unit.DeathState?.PendingStatisticsCardId is int pendingSource)
                {
                    DispatchDeathStatistics(null, target, pendingSource, unit.DeathState.IsSacrifice ?? unit.SacrificeCardId.HasValue);
                    target.Apply(target.Freeze().WithDeathState(new UnitDeathState(unit.DeathState.HasFinishedDying,
                        unit.DeathState.IsBeingRemoved, unit.DeathState.HasStatisticsListener, isSacrifice: unit.DeathState.IsSacrifice,
                        statisticsListenerOnce: unit.DeathState.StatisticsListenerOnce)));
                }
                return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment,
                    source.Units.Select(item => item.Id == unit.Id ? target.Freeze() : item).ToArray(),
                    source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot()), RoomOutcome.Exchanged, 0, events);
            }

            private void BeginTeam(CombatTeam team)
            {
                foreach (WorkingUnit actor in units.Where(unit => unit.Alive && unit.InRoom && unit.Source.Team == team).ToArray())
                    FireTriggers(actor, "OnTeamTurnBegin");
            }

            private void Exchange()
            {
                // Native clears both prevention sets for each exchange, including relentless.
                cannotAttackOrHeal.Clear(); cannotFireTriggers.Clear();
                WorkingUnit[] quick = units.Where(unit => unit.Alive && unit.InRoom &&
                    unit.Source.Team == CombatTeam.Player && unit.Has("ambush")).ToArray();
                foreach (WorkingUnit unit in quick)
                {
                    Trigger(unit, "ambush", 1);
                    Turn(unit);
                    if (CombatPreviewStopped()) return;
                }
                BeginTeam(CombatTeam.Enemy);
                foreach (WorkingUnit unit in units.Where(unit => unit.InRoom && unit.Source.Team == CombatTeam.Enemy).ToArray())
                { Turn(unit); if (CombatPreviewStopped()) return; }
                BeginTeam(CombatTeam.Player);
                foreach (WorkingUnit unit in units.Where(unit => unit.InRoom && unit.Source.Team == CombatTeam.Player).ToArray())
                    if (!quick.Contains(unit)) { Turn(unit); if (CombatPreviewStopped()) return; }
            }

            private bool CombatPreviewStopped() => source.Preview && units.Any(unit => !unit.Alive &&
                (IsTerminalBossDeath(unit) || unit.Source.IsPyre));

            private void Turn(WorkingUnit actor, bool ignoreAttackPrevention = false)
            {
                if (!actor.Alive || !actor.InRoom) return;
                bool dazed = actor.Has("dazed") && Active(actor.Statuses["dazed"]);
                if (dazed)
                {
                    cannotAttackOrHeal.Add(actor.Source.Id); cannotFireTriggers.Add(actor.Source.Id);
                    Trigger(actor, "dazed", 1);
                    Emit("Dazed", actor, actor, 0);
                }
                // Native queues this after attack/trigger prevention, before attack conditions.
                // Ignored-silence triggers can run while dazed, without restoring this turn's attack.
                FireTriggers(actor, "OnTurnBegin", canFireTriggers: !dazed);
                if (!actor.Alive || dazed && !ignoreAttackPrevention || !actor.Source.CanAttack || actor.Attack <= 0) return;
                int multistrikeExtraAttacks = actor.Statuses.TryGetValue("multistrike", out CombatStatus? multi)
                    ? CombatUnit.AdditionalAttacks(multi) : 0;
                int strikes = actor.Attacks;
                for (int strike = 0; strike < strikes && actor.Alive; strike++)
                {
                    WorkingUnit[] targets = units.Where(unit => unit.Alive && unit.InRoom && unit.Source.Team != actor.Source.Team &&
                        !unit.Has("stealth") && !unit.Has("untouchable")).ToArray();
                    if (targets.Length == 0) continue;
                    bool sweep = actor.Has("sweep");
                    if (!sweep) targets = new[] { actor.Has("sniper") ? targets.Last() : targets.First() };
                    if (strike > 0)
                    {
                        Trigger(actor, "multistrike", 1);
                        if (strike > multistrikeExtraAttacks) RemoveStatus(actor, "burst", 1);
                        if (unsupportedReason != null) return;
                    }
                    // Sweep fixes its target list before damage. Each hit has its own native boss-kill preview.
                    if (sweep) runningTriggerQueue = true;
                    foreach (WorkingUnit target in targets)
                        if (target.Alive)
                        {
                            PreviewBossAttack(actor, target);
                            if (unsupportedReason != null) return;
                            // Native uses IsDestroyed here. Lethal retaliation during sweep
                            // leaves the attacker object available until the group is removed.
                            Damage(actor, target, actor.Despawned ? 0 : actor.Attack, "Attack");
                        }
                    if (sweep) DrainLocalTriggerQueue();
                }
            }

            private void PreviewBossAttack(WorkingUnit actor, WorkingUnit target)
            {
                if (source.Preview || context?.Statistics == null || !units.Any(unit => unit.Alive && unit.InRoom &&
                    (unit.Source.IsPyre || unit.Source.IsBoss == true || unit.Source.IsBoss == null && unit.Source.EndsBattleOnDeath))) return;
                var copied = new RoomCombatState(source.RoomIndex, source.Deployment, units.Where(unit =>
                    !externalTurnActorIds.Contains(unit.Source.Id)).Select(unit => unit.Freeze()).ToArray(),
                    source.ExternalInteractions, context.SpawnPoints == null ? context :
                        context.WithSpawnPoints(context.SpawnPoints.BeginPreview()), preview: true,
                    activeCompanionBossIds: CompanionBossSnapshot());
                EnchantmentWorld? originalWorld = context.Enchantments;
                if (originalWorld?.AutomaticLifecycle == true)
                {
                    RoomCombatState synced = EnchantmentWorldModel.Sync(PhysicalRoom(), units.Where(unit => !unit.Alive || !unit.InRoom ||
                        externalTurnActorIds.Contains(unit.Source.Id))
                        .Select(unit => unit.Freeze()).ToArray());
                    context = synced.Context!; originalWorld = context.Enchantments!;
                    EnchantmentCombatState prepared = originalWorld.Frame(context);
                    foreach (WorkingUnit unit in units.Where(unit => unit.InRoom && !unit.Removed &&
                        !externalTurnActorIds.Contains(unit.Source.Id)))
                        prepared = EnchantmentCombatModel.PrepareForPreview(prepared, unit.Source.Id);
                    EnchantmentWorld preparedWorld = EnchantmentWorld.From(prepared, true);
                    CombatContext testContext = context.WithEnchantments(preparedWorld);
                    if (testContext.SpawnPoints != null) testContext = testContext.WithSpawnPoints(testContext.SpawnPoints.BeginPreview());
                    copied = new RoomCombatState(copied.RoomIndex, copied.Deployment, units.Where(unit => unit.InRoom && !unit.Removed &&
                        !externalTurnActorIds.Contains(unit.Source.Id))
                        .Select(unit => EnchantmentWorldModel.RestorePreviewEffects(unit.Freeze(), preparedWorld, false)).ToArray(),
                        copied.ExternalInteractions, testContext, true, CompanionBossSnapshot());
                    var train = new TrainCombatState(prepared.Train.Rooms.Select(room => room.RoomIndex == source.RoomIndex ? copied : room).ToArray(),
                        originalWorld.Movement, originalWorld.EnemySlotsPerRoom, testContext);
                    train = EnchantmentWorldModel.Rebase(train, preview: true, testRng: context.IsolatedBattlePreview == true ? context.BattleRng : originalWorld.TestRng);
                    copied = EnchantmentWorldModel.Room(copied, train.Context);
                }
                CombatUnit? externalActor = externalTurnActorIds.Contains(actor.Source.Id) ? actor.Freeze() : null;
                var previewTargets = hasExternalTargetRoster ? units
                    .Where(unit => externalTurnActorIds.Contains(unit.Source.Id) && unit.Source.Id != externalActor?.Id)
                    .Select(unit => (unit.RoomIndex, unit.Freeze())).ToArray() : null;
                var preview = new Engine(copied, new List<CombatEvent>(), externalActor: externalActor,
                    externalActorRoomIndex: externalActor == null ? -1 : actor.RoomIndex, externalTargets: previewTargets);
                // A sweep preview shares the running-queue gate: its temporary callbacks
                // stay queued rather than healing or damaging between individual targets.
                preview.runningTriggerQueue = runningTriggerQueue;
                preview.Damage(preview.units.Single(unit => unit.Source.Id == actor.Source.Id),
                    preview.units.Single(unit => unit.Source.Id == target.Source.Id), actor.Attack, "Attack");
                if (originalWorld?.AutomaticLifecycle == true) preview.DrainLocalTriggerQueue();
                if (preview.unsupportedReason != null) { unsupportedReason = "Boss kill preview: " + preview.unsupportedReason; return; }
                // Native restores characters, but CardStatistics is shared: queries can refresh its
                // membership and SetAttackDamageDealt survives, including nested retaliation.
                context = context.WithStatistics(preview.context!.Statistics);
                BattleSpawnPoints? restoredPoints = context.SpawnPoints?.NextPreviewCopyId.HasValue == true ?
                    BattleSpawnPoints.EndPreview(context.SpawnPoints, preview.context.SpawnPoints!, clearBirths: false) : null;
                if (restoredPoints != null) context = context.WithSpawnPoints(restoredPoints);
                if (originalWorld?.AutomaticLifecycle == true)
                {
                    EnchantmentWorld observed = preview.context!.Enchantments!;
                    int firstPreviewId = context.NextUnitId ?? int.MaxValue;
                    int firstPreviewCardId = context.NextCardId;
                    // Temporary previews restore only the characters collected before
                    // the hit. New actors keep their preview state and copied point;
                    // switching the room back to its primary list does not destroy them.
                    EnchantmentRetainedUnit[] observedActors = observed.Rooms.SelectMany(room => room.Units.Select(unit =>
                        new EnchantmentRetainedUnit(unit, room.RoomIndex, room.Preview)))
                        .Concat(observed.RetainedUnits).ToArray();
                    EnchantmentRetainedUnit[] births = observedActors.Where(unit => unit.Unit.Id >= firstPreviewId).ToArray();
                    int[] removedPreviewIds = births.Where(actor => actor.Unit.Health <= 0 || actor.Unit.DeathState?.IsDestroyed == true)
                        .Select(actor => actor.Unit.Id).ToArray();
                    var restoredIds = originalWorld.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0)
                        .Select(unit => unit.Id).Concat(originalWorld.RetainedUnits.Where(actor => actor.Unit.Health > 0 &&
                            actor.Unit.DeathState?.IsDestroyed != true).Select(actor => actor.Unit.Id))
                        .Concat(births.Where(actor => !removedPreviewIds.Contains(actor.Unit.Id)).Select(actor => actor.Unit.Id)).ToHashSet();
                    if (preview.context!.NextUnitId is int nextId) context = context.WithNextUnitId(nextId);
                    if (context.CardRegistry != null && preview.context.CardRegistry != null)
                    {
                        foreach (CardInstanceState card in context.CardRegistry)
                        {
                            CardInstanceState? tested = preview.context.FindCard(card.InstanceId);
                            IReadOnlyList<int>? raw = tested?.RawPlayedRoomUnitIds ?? tested?.PlayedRoomUnitIds;
                            if (card.PlayedRoomUnitIds == null || raw == null) continue;
                            context = context.WithCard(card.WithRoomCacheState(raw.Concat(tested!.PlayedRoomUnitIds ?? Array.Empty<int>())
                                .Distinct().Where(restoredIds.Contains).ToArray(),
                                card.RawPlayedRoomUnitIds == null ? null : raw));
                        }
                        context = context.WithCardRegistry(context.CardRegistry.Concat(
                            PreviewBirthModel.RestoreSourceCards(preview.context, firstPreviewCardId, restoredIds)).ToArray())
                            .WithNextCardId(preview.context.NextCardId);
                    }
                    if (context.LastSpawnedUnitId.HasValue) context = context.WithLastSpawned(preview.context.LastSpawnedUnitId ?? 0);
                    if (context.SpawnPoints != null && restoredPoints == null)
                        context = context.WithSpawnPoints(context.SpawnPoints.WithState(context.SpawnPoints.Groups, context.SpawnPoints.Units.Concat(
                            births.Select(actor => new UnitSpawnPointState(actor.Unit.Id, null, null, spawnedInPreview: true))).ToArray()));
                    var restoredActors = units.Where(unit => unit.InRoom && !unit.Removed).Select(unit => unit.Source.Id).ToHashSet();
                    CombatUnit Restore(CombatUnit unit, int room) => EnchantmentWorldModel.RestorePreviewEffects(unit, observed,
                        restoredActors.Contains(unit.Id), removedPreviewIds);
                    // Earlier preview births were also outside the collected restore
                    // list. Keep their complete observed state and location, including
                    // status withdrawals and callbacks from this temporary preview.
                    EnchantmentRetainedUnit Retained(EnchantmentRetainedUnit actor) => actor.Preview ? observedActors
                        .FirstOrDefault(unit => unit.Unit.Id == actor.Unit.Id) ?? actor :
                        new EnchantmentRetainedUnit(Restore(actor.Unit, actor.RoomIndex), actor.RoomIndex, actor.Preview);
                    context = context.WithEnchantments(new EnchantmentWorld(originalWorld.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                        room.Deployment, room.Units.Select(unit => Restore(unit, room.RoomIndex)).ToArray(), room.ExternalInteractions, null, room.Preview,
                        source.ActiveCompanionBossIds)).ToArray(),
                        originalWorld.Movement, originalWorld.EnemySlotsPerRoom, originalWorld.RetainedUnits.Select(Retained)
                            .Concat(births).ToArray(),
                        originalWorld.EnchanterIds.Concat(observed.EnchanterIds).Distinct().ToArray(),
                        originalWorld.AllowUpdates, originalWorld.Updating, originalWorld.Preview, originalWorld.TestRng, true));
                    ImportEnchantmentRoom(EnchantmentWorldModel.Refresh(EnchantmentWorldModel.Room(CurrentRoom(), context)));
                }
                if (preview.units.Any(unit => !unit.Alive && (preview.IsTerminalBossDeath(unit) || unit.Source.IsPyre) &&
                    units.Any(live => live.Alive && live.Source.Id == unit.Source.Id))) ClearTerminalCards();
            }

            private void Damage(WorkingUnit? actor, WorkingUnit target, int damage, string kind, int sourceCardId = 0, bool applyTraits = true)
            {
                if (target.LastAttackerId.HasValue) target.LastAttackerId = actor != null && !actor.Source.IsPyre ? actor.Source.Id : 0;
                int raw = damage;
                int blocked = 0;
                bool direct = kind == "Attack";
                bool attackingTrigger = direct || kind == "Trample";
                if (applyTraits)
                {
                    bool retainedLegacy = context?.CardRegistry == null && legacyDetachedCards != null;
                    CombatContext? scalingContext = retainedLegacy ? context!.WithCardRegistry(legacyDetachedCards) : context;
                    DamageScalingResult scaled = DamageScalingModel.Apply(scalingContext, actor?.Source.SpawnerCardId ?? sourceCardId, sourceCardId, damage);
                    if (!scaled.Supported) { unsupportedReason = scaled.UnsupportedReason; return; }
                    context = retainedLegacy ? scaled.Context!.WithCardRegistry(null) : scaled.Context; damage = scaled.Damage;
                }
                damage = Math.Max(0, damage);
                bool cardPiercing = context?.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.AllDamageIsPiercing)) == true;
                if (damage > 0 && !cardPiercing &&
                    (target.Has("damage shield") || target.Has("spell shield") ||
                     target.Has("armor") || target.Has("titanskin")))
                {
                    int piercingCardId = sourceCardId > 0 ? sourceCardId : actor?.Source.SpawnerCardId ?? 0;
                    CardInstanceState? piercingCard = piercingCardId > 0 ? context?.FindCard(piercingCardId) : null;
                    if (piercingCard?.MaskDescriptor != null)
                    {
                        string? cardError = CardModifierModel.UnsupportedReason(piercingCard);
                        if (cardError != null) { unsupportedReason = cardError; return; }
                        cardPiercing = CardBranchMaskModel.Resolve(piercingCard).Traits.Contains("CardTraitIgnoreArmor", StringComparer.Ordinal);
                    }
                }
                bool unitPiercing = sourceCardId == 0 && actor?.Has("piercing") == true;
                if (target.Has("pyregel"))
                {
                    damage = checked(damage + StatusMagnitude(target, "pyregel"));
                    Trigger(target, "pyregel", 1);
                }
                // Native applies source traits and flat damage increases before melee weakness.
                if (direct && actor != null && target.Alive && !target.Has("untouchable") &&
                    target.Has("melee weakness") && !target.Has("damage shield"))
                {
                    int beforeWeakness = damage;
                    int stacks = target.Count("melee weakness");
                    damage = checked(damage * (stacks + 1));
                    if (damage != beforeWeakness) Trigger(target, "melee weakness", stacks);
                }
                if (kind == "Spell" && sourceCardId > 0 && damage > 0 && target.Alive && target.Has("spell weakness"))
                {
                    CardInstanceState? sourceCard = context?.FindCard(sourceCardId);
                    if (sourceCard?.MaskDescriptor == null)
                    {
                        unsupportedReason = "Spell Weakness requires the native source card type.";
                        return;
                    }
                    string? cardError = CardModifierModel.UnsupportedReason(sourceCard);
                    if (cardError != null) { unsupportedReason = cardError; return; }
                    bool isSpell = sourceCard.MaskDescriptor.Definition.CardType == "Spell";
                    bool hasShield = target.Has("damage shield") || target.Has("spell shield");
                    if (isSpell && (!hasShield || cardPiercing && !target.Has("untouchable")))
                    {
                        int beforeWeakness = damage;
                        int stacks = target.Count("spell weakness");
                        damage = checked(damage * (stacks + 1));
                        if (damage != beforeWeakness) Trigger(target, "spell weakness", stacks);
                    }
                }
                // The native shield stage checks card/relic piercing without an attacker argument.
                // Unit piercing bypasses armor, but does not bypass this shield stage.
                if (damage > 0 && target.Has("damage shield") && !cardPiercing)
                {
                    blocked += damage;
                    Trigger(target, "damage shield", 1);
                    damage = 0;
                }
                if (damage > 0 && kind == "Spell" && sourceCardId > 0 && target.Has("spell shield") && !cardPiercing)
                {
                    CardInstanceState? sourceCard = context?.FindCard(sourceCardId);
                    if (sourceCard?.MaskDescriptor == null)
                    {
                        unsupportedReason = "Spell Shield requires the native source card type.";
                        return;
                    }
                    if (sourceCard.MaskDescriptor.Definition.CardType == "Spell")
                    {
                        Trigger(target, "spell shield", 1);
                        damage = 0;
                    }
                }
                if (damage > 0 && target.Has("titanskin") && !cardPiercing && !unitPiercing)
                {
                    CombatStatus titanSkin = target.Statuses["titanskin"];
                    StatusScalingResult modifier = RelicStatusMagnitudeModel.GetModifier(context, "titanskin",
                        target.Source.Team == CombatTeam.Player ? 2 : 1);
                    if (!modifier.Supported) { unsupportedReason = modifier.UnsupportedReason; return; }
                    context = modifier.Context;
                    int applicableStacks = Math.Min(damage, titanSkin.Stacks);
                    int damageSoak = unchecked(unchecked(titanSkin.ParamInt + modifier.Stacks) * applicableStacks);
                    damage = Math.Max(0, unchecked(damage - damageSoak));
                }
                if (damage > 0 && target.Has("armor") && !cardPiercing && !unitPiercing)
                {
                    int armor = target.Amount("armor");
                    int spent = Math.Min(target.Count("armor"), damage);
                    blocked += Math.Min(damage, armor);
                    damage = Math.Max(0, damage - armor);
                    Trigger(target, "armor", spent);
                }
                if (damage > 0 && target.Has("fragile")) damage = target.Health + damage - 1;
                if (target.Has("untouchable")) damage = 0;
                if (actor != null && context?.Statistics != null)
                    context = context.WithStatistics(context.Statistics.WithLastAttackDamage(checked(damage + blocked)));
                ApplyDamageOverflow(actor, target, damage, kind, sourceCardId);
                if (unsupportedReason != null) return;
                // Native fixes damage and resulting HP before firing the pre-damage
                // callback. Healing, armor or attack changes there do not recalculate it.
                int previousHealth = target.Health;
                int resultingHealth = Math.Max(0, previousHealth - damage);
                if (attackingTrigger && actor != null && actor != target)
                    FireTriggers(actor, "OnAttackingBeforeDamage", overrideTarget: target);
                if (unsupportedReason != null) return;
                target.Health = resultingHealth;
                if (context != null) context = ConduitModel.Alive(context, target.RoomIndex, target.Freeze());
                if (!source.Preview && target.Source.IsPyre && context?.EnergyState != null)
                    context = context.WithEnergyState(context.EnergyState.With(pyreAlive: target.Alive));
                if (!source.Preview && !target.Alive && IsTerminalBossDeath(target))
                { battleWon = true; context = context?.WithBossesDead(); }
                Emit(kind, actor, target, damage);
                if (attackingTrigger && actor != null && actor != target)
                    FireTriggers(actor, "OnAttacking", overrideTarget: target);
                // Native queues Slay before lifesteal and retaliation, for every damage type
                // with a character attacker, including damage from a character effect.
                if (!target.Alive && !target.DeathFinished && target.Count("undying") <= 0 && !UpdateEnchantments(target.Source.Id)) return;
                if (actor != null && !actor.Despawned && !actor.Removed && !target.Alive) FireTriggers(actor, "OnKill", dyingCharacter: target);
                // Native lifesteal heals by unmodified attack, even against armor; it happens before spikes.
                // A dying sweep attacker has not been destroyed yet: its remaining
                // attacks still consume lifesteal, while Heal itself refuses revival.
                if (direct && actor != null && !actor.Despawned && !actor.Removed && actor.Has("lifesteal") && raw > 0)
                {
                    Trigger(actor, "lifesteal", 1);
                    Heal(actor, raw, "Lifesteal");
                }
                if (attackingTrigger && actor?.Destroyed == false && actor.Count("trample") > 0 &&
                    damage > previousHealth)
                {
                    WorkingUnit? next = NextTrampleTarget(target);
                    if (next?.Alive == true)
                        Damage(actor, next, damage - previousHealth, "Trample");
                    if (unsupportedReason != null) return;
                }
                if (attackingTrigger && actor != null && actor.Alive && target.Has("spikes"))
                {
                    int retaliation = StatusMagnitude(target, "spikes");
                    Trigger(target, "spikes", 1);
                    Damage(target, actor, retaliation, "Spikes");
                }
                if (target.DeathFinished) return;
                if (damage > 0 || blocked > 0) FireTriggers(target, "OnHit", paramInt: damage);
                // A played spell finishes its hit queue while this victim already has its
                // new HP. Only previously finished deaths are eligible for removal here.
                if (nativeCardDamage && !runningTriggerQueue) DrainLocalTriggerQueue();
                if (nativeCardDamage && !runningTriggerQueue && finishDamageQueue != null)
                {
                    var frame = new RoomCombatState(source.RoomIndex, source.Deployment,
                        units.Where(unit => !unit.Removed && unit.InRoom).Select(unit => unit.Freeze()).ToArray(),
                        source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());
                    RoomCombatResult drained = finishDamageQueue(frame);
                    if (!drained.Supported) { unsupportedReason = drained.UnsupportedReason; return; }
                    MergeSummon(drained);
                }
                if (!target.Alive && !target.DeathFinished)
                {
                    if (!TryRevive(actor, target, sourceCardId))
                        Death(actor, target, sourceCardId, deferRemoval: runningTriggerQueue || nativeCardDamage);
                }
            }

            private WorkingUnit? NextTrampleTarget(WorkingUnit attacked)
            {
                CombatUnit[] ordered = BattleSpawnPointModel.Order(context?.SpawnPoints, attacked.RoomIndex,
                    units.Where(unit => unit.InRoom && !unit.Removed).Select(unit => unit.Source).ToArray());
                int index = Array.FindIndex(ordered, unit => unit.Id == attacked.Source.Id);
                if (index < 0) return null;
                foreach (CombatUnit unit in ordered.Skip(index + 1).Where(unit => unit.Team == attacked.Source.Team))
                {
                    WorkingUnit? candidate = units.FirstOrDefault(item => item.Source.Id == unit.Id);
                    if (candidate != null && !candidate.Has("stealth") && !candidate.Has("untouchable"))
                        return candidate;
                }
                return null;
            }

            private void ApplyDamageOverflow(WorkingUnit? actor, WorkingUnit target, int damage, string kind, int sourceCardId)
            {
                const string traitType = "CardTraitDamageOverflow";
                if (kind == "SpellOverflow" || damage <= target.Health || sourceCardId <= 0) return;
                CardInstanceState? card = context?.FindCard(sourceCardId);
                CardMaskDescriptor? descriptor = card?.MaskDescriptor;
                if (card == null || descriptor == null || !MayHaveDamageOverflow(card, descriptor)) return;
                string? cardError = CardModifierModel.UnsupportedReason(card);
                if (cardError != null) { unsupportedReason = cardError; return; }

                IReadOnlyList<CardTraitValue> traits = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(card).Traits)
                    .Composition.CombinedTraits ?? Array.Empty<CardTraitValue>();
                CardTraitValue? overflow = traits.FirstOrDefault(trait => trait.RuntimeType == traitType);
                if (overflow == null) return;

                CombatTeam team = overflow.ParamInt switch
                {
                    0 => target.Source.Team,
                    1 => target.Source.Team == CombatTeam.Player ? CombatTeam.Enemy : CombatTeam.Player,
                    2 => CombatTeam.Enemy,
                    3 => CombatTeam.Player,
                    _ => target.Source.Team
                };
                int overflowDamage = damage - target.Health;
                CombatUnit[] room = BattleSpawnPointModel.Order(context?.SpawnPoints, target.RoomIndex, CurrentRoom().Units);
                foreach (CombatUnit siblingState in room.Where(unit => unit.Team == team && unit.Id != target.Source.Id))
                {
                    WorkingUnit? sibling = units.FirstOrDefault(unit => unit.Source.Id == siblingState.Id);
                    if (sibling == null) continue;
                    Damage(actor, sibling, overflowDamage, "SpellOverflow", sourceCardId, applyTraits: false);
                    if (unsupportedReason != null) return;
                }
            }

            private static bool MayHaveDamageOverflow(CardInstanceState card, CardMaskDescriptor descriptor)
            {
                const string traitType = "CardTraitDamageOverflow";
                IEnumerable<CardTraitValue> possible = descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Concat(
                    card.Permanent.Upgrades.Concat(card.Temporary.Upgrades)
                        .SelectMany(upgrade => upgrade.Lifecycle?.AddedTraits ?? Array.Empty<CardTraitValue>()));
                return possible.Any(trait => trait.RuntimeType == traitType) ||
                    descriptor.PermanentReplacements.Any(replacement => replacement.NewRuntimeType == traitType);
            }

            internal RoomCombatResult Revival(int targetId, int sourceCardId, int attackerUnitId, CombatUnit? retainedAttacker)
            {
                WorkingUnit? attacker = units.FirstOrDefault(unit => unit.Source.Id == attackerUnitId);
                if (attacker == null && retainedAttacker != null) attacker = NewUnit(retainedAttacker, false);
                Revive(attacker, units.Single(unit => unit.Source.Id == targetId), sourceCardId);
                return Finish(RoomOutcome.Exchanged);
            }

            private bool TryRevive(WorkingUnit? actor, WorkingUnit target, int sourceCardId)
            {
                if (target.Count("undying") <= 0) return false;
                Revive(actor, target, sourceCardId);
                if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                return true;
            }

            private void Revive(WorkingUnit? actor, WorkingUnit target, int sourceCardId)
            {
                CombatUnit before = target.Freeze();
                if ((context?.CardRegistry ?? context?.CardInstances)?.Any(card => card.PlayedRoomUnitIds != null && card.RawPlayedRoomUnitIds == null) == true)
                { unsupportedReason = "Revival requires raw card room-cache membership."; return; }
                if (before.DeathState == null || !before.DeathState.StatisticsListenerOnce.HasValue)
                { unsupportedReason = "Revival requires the native death listener lifetime."; return; }
                target.Apply(new CombatUnit(before.Id, before.AssetKey, before.Team, before.BaseAttack, 1, Math.Max(1, before.MaxHealth),
                    before.CanAttack, before.IsPyre, before.EndsBattleOnDeath, before.Statuses, before.Triggers, before.SpawnerCardId,
                    before.Size, before.StatusImmunities, before.Subtypes, before.Modifiers, before.IsBoss, before.LastAttackerId,
                    before.StatusRegistry, before.EquipmentCards, before.NextTriggerId, before.Ability, before.StatusDictionary,
                    before.AbilityRules, before.HordeDefinition, before.IsSpawning, before.SacrificeCardId,
                    before.DeathState == null ? null : new UnitDeathState(false, before.DeathState.IsBeingRemoved,
                        before.DeathState.HasStatisticsListener, isSacrifice: before.DeathState.IsSacrifice.HasValue ? false : (bool?)null,
                        statisticsListenerOnce: before.DeathState.StatisticsListenerOnce,
                        isDespawned: before.DeathState.IsDespawned, isDestroyed: before.DeathState.IsDestroyed)
                        .WithPreviewPrimary(before.DeathState.PreviewPrimary), bumpRules: before.BumpRules));
                target.DeathFinished = false;
                if (context != null) context = ConduitModel.Alive(context, target.RoomIndex, target.Freeze());
                RemoveStatus(target, "undying", 1);
                CombatUnit revived = target.Freeze();
                // Unlike physical removal, all four harvest groups are captured before any dispatch.
                foreach (string stage in HarvestModel.Stages(revived))
                {
                    HarvestModel.Stage(stage, out string kind, out CombatTeam team);
                    QueueHarvestGroup(revived, kind, team, HarvestModel.Count(revived, kind));
                }
                QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, revived, "OnReanimated"));
                QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, revived, "OnDeath", paramInt: 1));
                DispatchDeathStatistics(actor, target, sourceCardId, sacrifice: false);
                if (target.Source.DeathState?.StatisticsListenerOnce == true)
                    target.Apply(target.Freeze().WithDeathState(new UnitDeathState(false, target.Source.DeathState.IsBeingRemoved,
                        false, isSacrifice: target.Source.DeathState.IsSacrifice, statisticsListenerOnce: false)));
                Emit("Revive", actor, target, 1);
            }

            private readonly List<(WorkingUnit Unit, bool Return)> deferredDamageDeaths = new List<(WorkingUnit, bool)>();

            private void Death(WorkingUnit? actor, WorkingUnit target, int sourceCardId, bool deferRemoval = false, bool sacrifice = false,
                bool immediateHarvest = false, bool doNotFullyRemove = false, bool returnSpawnerAfterQueue = false)
            {
                if (target.DeathFinished) return;
                if (context != null) context = ConduitModel.Alive(context, target.RoomIndex, target.Freeze());
                if (!source.Preview && target.Source.IsPyre && context?.EnergyState != null)
                    context = context.WithEnergyState(context.EnergyState.With(pyreAlive: false));
                target.DeathFinished = true;
                bool deferReturn = deferRemoval && target.Source.Team == CombatTeam.Player && target.Source.SpawnerCardId > 0 && !DeferSpawner(target);
                Emit("Death", actor, target, 0);
                // Native UpdateHp sets this gate before death triggers are fired.
                bool terminalBossRemoval = IsTerminalBossDeath(target);
                bool terminalBossDeath = !source.Preview && terminalBossRemoval;
                if (terminalBossDeath)
                { battleWon = true; context = context?.WithBossesDead(); }
                // CheckForDeath runs the terminal kill camera before dispatching death signals.
                if (!source.Preview && (terminalBossDeath || target.Source.IsPyre)) ClearTerminalCards();
                activeCompanionBossIds.Remove(target.Source.Id);
                if (activeCompanionBossIds.Count == 1)
                {
                    int remainingId = activeCompanionBossIds.Single();
                    WorkingUnit? remaining = units.FirstOrDefault(unit => unit.Source.Id == remainingId);
                    if (remaining != null) remaining.Source = remaining.Source.WithEndsBattleOnDeath(true);
                }
                // At the captured StopCombatLoop boundary, Pyre death has no
                // ordinary unit death accounting/removal; its point stays occupied.
                if (target.Source.IsPyre) return;
                DispatchDeathStatistics(actor, target, sourceCardId, sacrifice);
                if (target.Count("unstable") > 0)
                {
                    int stacks = target.Count("unstable");
                    CombatUnit unstableSource = target.Freeze();
                    CombatUnit[] roomUnits = units.Where(unit => unit.InRoom).Select(unit => unit.Freeze()).ToArray();
                    int additionalDamage = RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(
                        context, unstableSource, roomUnits);
                    int explosion = unchecked(stacks + unchecked(additionalDamage * stacks));
                    foreach (WorkingUnit enemy in units.Where(unit => unit.InRoom && unit.Source.Team == CombatTeam.Enemy &&
                        unit.Source.Id != target.Source.Id && !unit.Destroyed && !unit.Source.IsPyre &&
                        !unit.Has("blastresistant")).ToArray())
                    {
                        Damage(null, enemy, explosion, "Poison");
                        if (unsupportedReason != null) return;
                    }
                }
                if (target.Source.DeathState != null)
                    target.Apply(target.Freeze().WithDeathState(new UnitDeathState(true,
                        target.Source.DeathState.IsBeingRemoved || !deferRemoval || sacrifice,
                        !nativeCardDamage && enqueueCharacterTrigger == null && target.Source.DeathState.HasStatisticsListener,
                        isSacrifice: target.Source.DeathState.IsSacrifice,
                        statisticsListenerOnce: target.Source.DeathState.StatisticsListenerOnce.HasValue ? false : (bool?)null)));
                if (!UpdateEnchantments(target.Source.Id)) return;
                if (enqueueCharacterTrigger != null) enqueueCharacterTrigger(new QueuedCharacterTrigger(target.RoomIndex, target.Freeze(),
                    returnSpawnerAfterQueue: deferReturn || returnSpawnerAfterQueue, deferUntilRemoval: deferRemoval, harvestAfterDeath: !target.Despawned && !immediateHarvest,
                    completePhysicalRemovalAfterQueue: deferRemoval && context?.SpawnPoints != null, removalLifecycle: true,
                    preservePhysicalPoint: doNotFullyRemove, captureAllDispatches: traceAllDispatches));
                else
                {
                    if (deferRemoval) deferredDamageDeaths.Add((target, deferReturn));
                    else
                    {
                        FireTriggers(target, "OnDeath");
                        if (target.Source.DeathState != null) target.Apply(target.Freeze().WithDeathState(new UnitDeathState(true, true, false,
                            isSacrifice: target.Source.DeathState.IsSacrifice, statisticsListenerOnce: target.Source.DeathState.StatisticsListenerOnce)));
                        PhysicalHarvest(target);
                    }
                }
                // A sacrifice inside an already-running trigger queue only
                // enqueues OnDeath/Harvest. Native still focuses the room before
                // removing the point, while those callbacks remain pending.
                if (immediateHarvest || enqueueCharacterTrigger != null && !deferRemoval)
                    FocusAfterDeath(target.Freeze(), target.RoomIndex);
                if (!source.Preview && !deferReturn && !returnSpawnerAfterQueue && !DeferSpawner(target)) SettleDeadSpawner(target);
                if (!deferRemoval)
                {
                    target.Removed = true;
                    if (doNotFullyRemove) target.Destroyed = true;
                    else RemovePhysicalPoint(target);
                    if (!(stopAfterBossRemoval && terminalBossRemoval) && UpdateEnchantments() &&
                        !runningTriggerQueue && enqueueCharacterTrigger == null) DrainLocalTriggerQueue();
                }
            }

            private void RemovePhysicalPoint(WorkingUnit unit)
            {
                unit.Destroyed = true;
                if (context != null) context = ConduitModel.Change(context, unit.RoomIndex, unit.Freeze(), source.Preview, remove: true);
                if (context?.SpawnPoints == null) return;
                var removed = BattleSpawnPointModel.Apply(context.SpawnPoints, CurrentRoom(), "Remove", unit.Source.Team, unit.Source.Id);
                if (!removed.Supported) { unsupportedReason = removed.Error; return; }
                context = context.WithSpawnPoints(removed.State!);
            }

            private void DispatchDeathStatistics(WorkingUnit? actor, WorkingUnit target, int sourceCardId, bool sacrifice)
            {
                if (!source.Preview && context?.Statistics != null && target.Source.DeathState?.HasStatisticsListener != false)
                {
                    int responsible = sourceCardId > 0 ? sourceCardId : target.Source.SpawnerCardId;
                    if (actor?.Source.SacrificeCardId > 0 && context.AbilityCardCache?.Any(entry => entry.InstanceId == sourceCardId) != true)
                        responsible = actor.Source.SacrificeCardId.Value;
                    // Native has no IncrementStat call (and no cache refresh) for a null responsible card.
                    BattleStatistics statistics = responsible > 0 ? context.LiveStatistics! : context.Statistics;
                    context = context.WithStatistics(statistics.Death(target.Source.Team == CombatTeam.Player,
                        responsible, requireTrackedCard: context.CardInstances?.Count == 0));
                    if (sacrifice && sourceCardId > 0)
                        context = context.WithStatistics(context.LiveStatistics!.Increment(sourceCardId, "LastSacrificedMonsterStats",
                            HarvestModel.Count(target.Freeze(), "OnAnyUnitDeathOnFloor"), requireTrackedCard: context.CardInstances?.Count == 0));
                }
            }

            private void SettleDeadSpawner(WorkingUnit unit)
            {
                if (source.Preview || unit.Source.Team != CombatTeam.Player) return;
                if (unit.Source.SpawnerCardId > 0 && !DetachedSpawner(unit))
                {
                    bool endless = unit.Has("endless");
                    if (!endless && context?.Statistics != null)
                        context = context.WithStatistics(context.LiveStatistics!.Increment(unit.Source.SpawnerCardId, "TimesExhausted",
                            requireTrackedCard: context.CardInstances?.Count == 0));
                    RouteSpawner(unit, returnEndless: endless);
                }
                if (context != null) context = EquipmentModel.ReturnAttached(context, unit.Freeze());
            }

            private bool DeferSpawner(WorkingUnit unit) => deferSpawnerExhaustion ||
                pendingSummonCardId > 0 && unit.Source.SpawnerCardId == pendingSummonCardId;

            private bool DetachedSpawner(WorkingUnit unit) => context?.CardInstances != null &&
                !context.CardInstances.Any(card => card.InstanceId == unit.Source.SpawnerCardId) &&
                (context.KillCamActivated == true || context.CardRegistry?.Any(card => card.InstanceId == unit.Source.SpawnerCardId) == true);

            private void RouteSpawner(WorkingUnit unit, bool returnEndless = false)
            {
                if (unit.Source.Team != CombatTeam.Player || unit.Source.SpawnerCardId <= 0 || context?.OtherPiles == null) return;
                if (DetachedSpawner(unit)) return;
                CardPileState? standby = context.OtherPiles.FirstOrDefault(pile => pile.Name == "Standby");
                CardPileState? exhausted = context.OtherPiles.FirstOrDefault(pile => pile.Name == "Exhausted");
                int id = unit.Source.SpawnerCardId;
                CardToken? card = standby?.Cards.FirstOrDefault(item => item.InstanceId == id);
                if (card == null && exhausted?.Cards.Any(item => item.InstanceId == id) == true) return;
                // OnSpawn runs before its played card enters standby. Its discard callback later
                // allocates/removes that slot and applies the exhausted counter exactly once.
                if (card == null && id == pendingSummonCardId &&
                    context.OtherPiles.Any(pile => pile.Name == "DiscardBuffer" && pile.Cards.Any(item => item.InstanceId == id))) return;
                if (standby == null || exhausted == null || card == null)
                { unsupportedReason = "Missing dead unit spawner card routing."; return; }
                if (returnEndless)
                {
                    context = UnitStandbyModel.ReturnEndless(context, id);
                    return;
                }
                context = context.WithOtherPiles(context.OtherPiles.Select(pile => pile == standby ? CardPileModel.Remove(pile, id) :
                    pile == exhausted ? CardPileModel.Add(CardPileModel.Remove(pile, id), card) : pile).ToArray());
            }

            private void ClearTerminalCards()
            {
                // Native ShowKillCam clears the active card piles before StopCombat is entered.
                if (context == null || killCamActivated) return;
                killCamActivated = true;
                if (context.CardRegistry == null) legacyDetachedCards = context.CardInstances;
                context = new CombatContext(new CardCycleState(Array.Empty<CardToken>(), Array.Empty<CardToken>(),
                    Array.Empty<CardToken>(), context.Cards.Rng, context.Cards.DrawModifier,
                    context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId), context.BattleRng, context.Gold,
                    context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics,
                    context.CardInstances == null ? null : Array.Empty<CardInstanceState>(), context.CardRegistry, context.AllScenarioBossesDead,
                    context.NextAddedTemporaryUpgrades, context.OtherPiles?.Select(CardPileModel.Clear).ToArray(), context.QueryFrame,
                    context.KillCamActivated.HasValue ? true : (bool?)null, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources, context.MixState);
                // ClearCards cancels card-play previews. The original cancellation
                // removes preview-born actors without normal death signals or a
                // primary-state restoration, including temporary Boss-preview births.
                if (context.Enchantments?.AutomaticLifecycle == true && context.SpawnPoints != null)
                {
                    EnchantmentWorld world = context.Enchantments;
                    var previewIds = context.SpawnPoints.Units.Where(unit => unit.SpawnedInPreview)
                        .Select(unit => unit.UnitId).ToHashSet();
                    var cleared = world.RetainedUnits.Where(actor => actor.Preview && previewIds.Contains(actor.Unit.Id) &&
                        actor.Unit.DeathState?.IsDestroyed != true).Select(actor => actor.Unit.Id).ToHashSet();
                    if (cleared.Count > 0)
                    {
                        context = context.WithEnchantments(new EnchantmentWorld(world.Rooms, world.Movement, world.EnemySlotsPerRoom,
                            world.RetainedUnits.Select(actor => cleared.Contains(actor.Unit.Id) ? new EnchantmentRetainedUnit(
                                actor.Unit.WithDeathState((actor.Unit.DeathState ?? new UnitDeathState(false, false, false))
                                    .WithLifecycle(actor.Unit.DeathState?.IsDespawned, true)), -1, actor.Preview) : actor).ToArray(),
                            world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview, world.TestRng, true));
                        BattleSpawnPoints points = context.SpawnPoints!;
                        context = context.WithSpawnPoints(points.WithState(points.Groups.Select(group => new SpawnPointGroupState(
                            group.RoomIndex, group.Team, group.InnerCount, group.GroupCount,
                            group.Occupants.Select(id => cleared.Contains(id) ? 0 : id).ToArray(), group.Outside, group.PreviewCopyId)).ToArray(), points.Units));
                        foreach (CardInstanceState card in context.CardRegistry ?? context.CardInstances ?? Array.Empty<CardInstanceState>())
                            if (card.PlayedRoomUnitIds?.Any(cleared.Contains) == true)
                                context = context.WithCard(card.WithRoomCacheState(card.PlayedRoomUnitIds.Where(id => !cleared.Contains(id)).ToArray(), card.RawPlayedRoomUnitIds));
                    }
                }
            }

            private void RemoveHatch()
            {
                foreach (WorkingUnit unit in units.Where(unit => unit.InRoom && unit.Alive &&
                    unit.Source.Team == CombatTeam.Player && unit.Triggers.Any(trigger => trigger.Kind == "OnRemoveHatch")).ToArray())
                {
                    if (!unit.Alive) continue;
                    FireTriggers(unit, "OnRemoveHatch");
                    if (unsupportedReason != null) return;
                }
            }

            private void PostCombat()
            {
                // Enemy post-combat runs before player post-combat, each front to back.
                foreach (WorkingUnit unit in units.Where(unit => unit.InRoom).OrderBy(unit => unit.Source.Team))
                {
                    if (!unit.Alive) continue;
                    if (!source.Deployment && unit.Has("regen"))
                    {
                        int amount = StatusMagnitude(unit, "regen");
                        Trigger(unit, "regen", 1);
                        Heal(unit, amount, "Regen");
                    }
                    // Stealth shares the native post-combat poison stage and is consumed even without an opponent.
                    foreach (CombatStatus status in unit.RegisteredStatuses().OrderBy(status => status.Id == "unstable" ? 4 : 0).ToArray())
                    {
                        if (!unit.Alive || !unit.Has(status.Id) || !Active(status)) continue;
                        if (status.Id == "poison" || status.Id == "decay")
                        {
                            if (source.Deployment && status.RemoveWhenTriggered && !status.RemoveDuringDeployment)
                            {
                                RelicStatusDecayResult removal = RelicStatusDecayModel.GetShouldRemove(context, true,
                                    status.Id, unit.Source.Team == CombatTeam.Player ? 2 : 1, source.Preview);
                                if (!removal.Supported) { unsupportedReason = removal.UnsupportedReason; return; }
                                context = removal.Context;
                                if (removal.ShouldRemove) continue;
                            }
                            int amount = StatusMagnitude(unit, status.Id);
                            Trigger(unit, status.Id, 1);
                            Damage(null, unit, amount, status.Id == "decay" ? "Decay" : "Poison");
                        }
                        else if (status.Id == "unstable" && !unit.Has("blastresistant") &&
                            unit.Count("unstable") >= unit.Health && !unit.Has("untouchable"))
                        {
                            unit.Apply(unit.Freeze().WithSacrifice(0));
                            unit.Health = 0;
                            if (!TryRevive(null, unit, 0)) Death(null, unit, 0, sacrifice: true);
                        }
                        else if (status.Id == "stealth") Trigger(unit, "stealth", 1);
                    }
                    if (unit.Alive && unit.Has("ephemeral") && Active(unit.Statuses["ephemeral"]))
                        TriggerEphemeral(unit);
                    if (unsupportedReason != null) return;
                }
            }

            private void TriggerEphemeral(WorkingUnit unit)
            {
                CombatStatus ephemeral = unit.Statuses["ephemeral"];
                Trigger(unit, "ephemeral", 1);
                if (unsupportedReason != null || !unit.Alive || unit.Count("ephemeral") > 0) return;
                if (unit.Count("undying") > 0)
                {
                    ReviveFromEphemeral(unit);
                    if (unsupportedReason != null) return;
                    RoomCombatResult reapplied = StatusApplicationModel.ApplyRetained(CurrentRoom(), unit.Source.Id,
                        ephemeral.WithStacks(1), 0);
                    if (!reapplied.Supported) { unsupportedReason = reapplied.UnsupportedReason; return; }
                    context = reapplied.State!.Context;
                    foreach (CombatUnit changed in reapplied.State.Units)
                        units.First(actor => actor.Source.Id == changed.Id).Apply(changed);
                    foreach (QueuedCharacterTrigger callback in reapplied.PendingCallbacks) QueueCallback(callback);
                    DrainLocalTriggerQueue();
                    return;
                }

                FireTriggers(unit, "OnBurnout");
                if (unsupportedReason != null || !unit.Alive) return;
                unit.Apply(unit.Freeze().WithSacrifice(0));
                unit.Health = 0;
                if (!TryRevive(null, unit, 0)) Death(null, unit, 0, sacrifice: true);
            }

            private void ReviveFromEphemeral(WorkingUnit target)
            {
                target.Health = 1;
                if (context != null) context = ConduitModel.Alive(context, target.RoomIndex, target.Freeze());
                target.DeathFinished = false;
                RemoveStatus(target, "undying", 1);
                if (target.Source.DeathState is UnitDeathState death)
                    target.Apply(target.Freeze().WithDeathState(new UnitDeathState(false, death.IsBeingRemoved,
                        death.HasStatisticsListener, death.PendingStatisticsCardId, isSacrifice: false,
                        statisticsListenerOnce: death.StatisticsListenerOnce, isDespawned: death.IsDespawned,
                        isDestroyed: death.IsDestroyed)));
                CombatUnit revived = target.Freeze();
                foreach (string stage in HarvestModel.Stages(revived))
                {
                    HarvestModel.Stage(stage, out string kind, out CombatTeam team);
                    QueueHarvestGroup(revived, kind, team, HarvestModel.Count(revived, kind));
                }
                QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, revived, "OnReanimated"));
                QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, revived, "OnDeath", paramInt: 1));
            }

            private void AlmostPostRoomCombat()
            {
                // Corrupt Poison owns OnAlmostPostRoomCombat, after the final relentless exchange.
                foreach (WorkingUnit unit in units.Where(unit => unit.InRoom).OrderBy(unit => unit.Source.Team))
                {
                    if (unsupportedReason != null || !unit.Alive || !unit.Has("corrupt_poison") ||
                        !Active(unit.Statuses["corrupt_poison"])) continue;
                    if (context?.QueryFrame?.Turn is not int turn)
                    { unsupportedReason = "Corrupt Poison requires the captured battle turn."; return; }
                    if (turn != 0)
                    {
                        if (source.Corruption is not int corruption)
                        { unsupportedReason = "Corrupt Poison requires captured room corruption."; return; }
                        StatusScalingResult modifier = RelicStatusMagnitudeModel.GetModifier(context, "corrupt_poison",
                            unit.Source.Team == CombatTeam.Player ? 2 : 1);
                        if (!modifier.Supported) { unsupportedReason = modifier.UnsupportedReason; return; }
                        context = modifier.Context;
                        CombatStatus status = unit.Statuses["corrupt_poison"];
                        int amount = unchecked(unchecked(status.ParamInt + modifier.Stacks) * status.Stacks * corruption);
                        Trigger(unit, "corrupt_poison", 1);
                        Damage(null, unit, amount, "CorruptPoison");
                        if (battleWon || !source.Preview && units.Any(candidate => candidate.Source.IsPyre && !candidate.Alive)) return;
                    }
                }
            }

            private void Heal(WorkingUnit unit, int amount, string kind)
            {
                if (!unit.Alive || unit.Source.Modifiers?.CanBeHealed == false) return;
                int modified = HealingModel.ModifiedAmount(amount, unit.Statuses.Values.ToArray());
                if (modified < 0) return;
                int old = unit.Health;
                unit.Health += Math.Min(modified, unit.Source.MaxHealth - unit.Health);
                Emit(kind, unit, unit, unit.Health - old);
                // Native ApplyHeal runs these even when clipping or immunity produces zero restoration.
                FireTriggers(unit, "OnHeal", paramInt: unit.Health - old);
            }

            private void FireTriggers(WorkingUnit unit, string kind, bool canFireTriggers = true, bool fromQueue = false, int paramInt = 0,
                WorkingUnit? overrideTarget = null, WorkingUnit? dyingCharacter = null, int triggerCount = 1, int lastSpawnedOverrideUnitId = 0,
                int? exclusiveTriggerStateId = null, int paramInt2 = 0)
            {
                string? paramString = kind == "OnHeal" || kind == "OnHit" ? "" : null;
                // CombatManager checks purification when adding to its queue, not
                // when dispatching an already accepted callback.
                if (!fromQueue && unit.Has("purify"))
                {
                    if (context?.PurifyBlockedTriggers == null) { unsupportedReason = "Purify requires captured trigger queue restrictions."; return; }
                    if (context.PurifyBlockedTriggers.Contains(kind)) return;
                }
                if (!fromQueue && enqueueCharacterTrigger != null)
                { enqueueCharacterTrigger(new QueuedCharacterTrigger(unit.RoomIndex, unit.Freeze(), kind, paramInt: paramInt, overrideTarget: overrideTarget?.Freeze(), paramInt2: paramInt2, paramString: paramString,
                    dyingCharacter: dyingCharacter?.Freeze(), canFireTriggers: canFireTriggers, triggerCount: triggerCount, lastSpawnedOverrideUnitId: lastSpawnedOverrideUnitId,
                    admission: CharacterTriggerAdmission.Accepted, exclusiveTriggerStateId: exclusiveTriggerStateId,
                    captureAllDispatches: traceAllDispatches)); return; }
                if (!fromQueue)
                {
                    triggerQueue.Enqueue((unit.RoomIndex, unit, kind, canFireTriggers, paramInt, overrideTarget, paramInt2, paramString, dyingCharacter, triggerCount, lastSpawnedOverrideUnitId, exclusiveTriggerStateId));
                    if (!runningTriggerQueue) DrainLocalTriggerQueue();
                    return;
                }
                bool startedQueue = !runningTriggerQueue;
                runningTriggerQueue = true;
                ExecuteTriggers(unit, kind, canFireTriggers, paramInt, overrideTarget, dyingCharacter, triggerCount, lastSpawnedOverrideUnitId,
                    paramInt2: paramInt2, paramString: paramString, exclusiveTriggerStateId: exclusiveTriggerStateId);
                if (!startedQueue) return;
                DrainLocalTriggerQueue();
            }

            private void DrainLocalTriggerQueue()
            {
                runningTriggerQueue = true;
                while (triggerQueue.Count > 0 && unsupportedReason == null)
                {
                    var queued = triggerQueue.Dequeue();
                    if (queued.RoomIndex != source.RoomIndex)
                    {
                        RoomCombatState synced = EnchantmentWorldModel.Sync(CurrentRoom(), units.Where(unit => !unit.Alive || !unit.InRoom)
                            .Select(unit => unit.Freeze()).ToArray());
                        context = synced.Context;
                        RoomCombatState? remote = context?.Enchantments?.Rooms.FirstOrDefault(room => room.RoomIndex == queued.RoomIndex);
                        if (remote == null)
                        {
                            if (queued.Unit.Source.IsPyre && !traceAllDispatches &&
                                !queued.Unit.Triggers.Any(trigger => trigger.Kind == queued.Kind) &&
                                (queued.Kind != "OnHit" || !queued.Unit.Has("horde"))) continue;
                            unsupportedReason = "A cross-room character callback requires the captured shared world.";
                            break;
                        }
                        var callback = new QueuedCharacterTrigger(queued.RoomIndex, queued.Unit.Freeze(), queued.Kind,
                            paramInt: queued.ParamInt, overrideTarget: queued.OverrideTarget?.Freeze(), paramInt2: queued.ParamInt2,
                            paramString: queued.ParamString, dyingCharacter: queued.DyingCharacter?.Freeze(), canFireTriggers: queued.CanFire,
                            triggerCount: queued.TriggerCount, lastSpawnedOverrideUnitId: queued.LastSpawnedOverrideUnitId,
                            exclusiveTriggerStateId: queued.ExclusiveTriggerStateId, captureAllDispatches: traceAllDispatches);
                        RoomCombatResult fired = ApplyQueuedCharacterTrigger(EnchantmentWorldModel.Room(remote, context), callback, QueueCallback);
                        if (!fired.Supported) { unsupportedReason = fired.UnsupportedReason; break; }
                        events.AddRange(fired.Events); dispatches.AddRange(fired.Dispatches);
                        ImportEnchantmentRoom(EnchantmentWorldModel.Refresh(EnchantmentWorldModel.Room(CurrentRoom(), fired.State!.Context)));
                        battleWon |= fired.Outcome == RoomOutcome.BattleWon;
                        continue;
                    }
                    ExecuteTriggers(queued.Unit, queued.Kind, queued.CanFire, queued.ParamInt, queued.OverrideTarget, queued.DyingCharacter,
                        queued.TriggerCount, queued.LastSpawnedOverrideUnitId, queued.ParamInt2, queued.ParamString, queued.ExclusiveTriggerStateId);
                }
                var removing = deferredDamageDeaths.OrderBy(dead => dead.Unit.Source.Team).ThenBy(dead => dead.Unit.Source.Id).ToArray();
                deferredDamageDeaths.Clear();
                runningTriggerQueue = false;
                foreach (var dead in removing)
                    dead.Unit.Apply(MarkBeingRemoved(dead.Unit.Freeze()));
                foreach (var dead in removing)
                {
                    FireTriggers(dead.Unit, "OnDeath");
                    if ((nativeCardDamage || context?.Enchantments?.AutomaticLifecycle == true) && dead.Unit.Source.DeathState != null)
                        dead.Unit.Apply(dead.Unit.Freeze().WithDeathState(new UnitDeathState(true, true, false,
                            isSacrifice: dead.Unit.Source.DeathState.IsSacrifice, statisticsListenerOnce: dead.Unit.Source.DeathState.StatisticsListenerOnce)));
                    PhysicalHarvest(dead.Unit);
                    if (dead.Return) SettleDeadSpawner(dead.Unit);
                    bool ownedPoint = context?.SpawnPoints?.Units.FirstOrDefault(unit => unit.UnitId == dead.Unit.Source.Id)?.Current != null;
                    dead.Unit.Removed = true;
                    RemovePhysicalPoint(dead.Unit);
                    // Centering also observes the next, zero-HP victim before CheckForDeath.
                    if (nativeCardDamage && ownedPoint && dead.Equals(removing[removing.Length - 1])) CenterAfterSummon();
                    // HeroManager removal starts GameScreen.EndCombat, which cancels
                    // the combat coroutine after this boss's death callbacks. Later
                    // members of the already-marked batch never fire OnDeath. A played
                    // card instead holds StopCombatLoop until its effects complete.
                    if (stopAfterBossRemoval && !source.Preview && battleWon) break;
                }
                if (removing.Length > 0 && !(stopAfterBossRemoval && battleWon) && UpdateEnchantments() &&
                    triggerQueue.Count > 0 && enqueueCharacterTrigger == null)
                    DrainLocalTriggerQueue();
            }

            private void QueueHarvestGroup(CombatUnit dying, string kind, CombatTeam team, int count)
            {
                // Native snapshots each team's surviving actors before draining that group.
                // Inert callbacks have no effects within the validated status rules.
                foreach (WorkingUnit actor in units.Where(unit => unit.Alive && unit.InRoom && !unit.Removed &&
                    unit.Source.Team == team && (traceAllDispatches || unit.Triggers.Any(trigger => trigger.Kind == kind))).ToArray())
                    QueueCallback(new QueuedCharacterTrigger(actor.RoomIndex, actor.Freeze(), kind,
                        dyingCharacter: dying, triggerCount: count, captureAllDispatches: traceAllDispatches));
            }
            private void PhysicalHarvest(WorkingUnit dying)
            {
                if (dying.Despawned) return;
                CombatUnit counts = dying.Freeze();
                foreach (string stage in HarvestModel.Stages(dying.Freeze()))
                {
                    HarvestModel.Stage(stage, out string kind, out CombatTeam team);
                    QueueHarvestGroup(dying.Freeze(), kind, team, HarvestModel.Count(counts, kind));
                    DrainLocalTriggerQueue();
                }
                FocusAfterDeath(dying.Freeze(), dying.RoomIndex);
            }

            private void FocusAfterDeath(CombatUnit dying, int roomIndex)
            {
                if (source.Preview || context?.QueryFrame?.SelectedRoomIndex == null || dying.DeathState?.IsDespawned == true) return;
                // FocusRoom reads GetCurrentRoomIndex, unlike Harvest which can
                // use lastKnownSpawnPoint. A death summon may already have cleared
                // the dying source's current point before this final step.
                int? currentRoom = context.SpawnPoints == null ? roomIndex :
                    context.SpawnPoints.Units.FirstOrDefault(unit => unit.UnitId == dying.Id)?.Current?.RoomIndex;
                if (currentRoom >= 0)
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: currentRoom.Value));
            }

            private void ExecuteTriggers(WorkingUnit unit, string kind, bool canFireTriggers, int paramInt, WorkingUnit? overrideTarget, WorkingUnit? dyingCharacter, int triggerCount, int lastSpawnedOverrideUnitId,
                int paramInt2 = 0, string? paramString = null, int? exclusiveTriggerStateId = null)
            {
                if (traceAllDispatches || unit.Triggers.Any(trigger => trigger.Kind == kind))
                    dispatches.Add(new CharacterTriggerDispatch(unit.Source.Id, kind, dyingCharacter?.Source.Id ?? 0, triggerCount,
                        paramInt, paramInt2, paramString, overrideTarget?.Source.Id ?? 0, lastSpawnedOverrideUnitId, exclusiveTriggerStateId));
                if (kind == "OnDeath" && unit.Despawned) return;
                if (!unit.Alive && kind != "OnDeath" && (unit.Source.IsBoss == true || unit.Source.EndsBattleOnDeath)) return;
                if (kind == "OnHit" && unit.Has("horde"))
                {
                    RoomCombatResult casualties = HordeStatusModel.SettleHealth(UpgradeRoom(unit), unit.Source.Id);
                    if (!casualties.Supported) { unsupportedReason = casualties.UnsupportedReason; return; }
                    context = casualties.State!.Context; unit.Apply(casualties.State.Units.First(actor => actor.Id == unit.Source.Id));
                    foreach (QueuedCharacterTrigger callback in casualties.PendingCallbacks) QueueCallback(callback);
                }
                for (int index = 0; index < unit.Triggers.Count; index++)
                {
                    CombatTrigger trigger = unit.Triggers[index];
                    if (trigger.Kind != kind || exclusiveTriggerStateId.HasValue && trigger.StateId != exclusiveTriggerStateId || trigger.Once && trigger.HasTriggered ||
                        trigger.Origin?.OnlyIfEquipped == true && unit.Source.EquipmentCards!.Count == 0 ||
                        trigger.TriggerAtThreshold > 0 && paramInt < trigger.TriggerAtThreshold ||
                        source.Deployment && trigger.SkipDuringDeployment == true ||
                        !canFireTriggers && !trigger.IgnoreSilence ||
                        (unit.Has("silenced") || unit.Has("muted")) && !trigger.IgnoreSilence) continue;
                    if (trigger.Conditions != null && (trigger.Conditions.RequiredStatuses.Any(id => !Present(unit, id)) ||
                        dyingCharacter != null && trigger.Conditions.RequiredDyingStatuses.Any(id => !Present(dyingCharacter, id)))) continue;
                    // Removed native characters have no spawn point and skip trigger
                    // preflight, though their once flag can still be marked before abort.
                    if (unit.InRoom && !unit.Removed && !ActionTriggerPassesTest(unit, trigger, lastSpawnedOverrideUnitId)) continue;
                    var effects = trigger.Effects.ToArray();
                    // Native marks the trigger before its effects; nested death effects observe it.
                    CombatTrigger retained = trigger.Fired(effects);
                    unit.Triggers[index] = retained;
                    int LiveIndex() => unit.Triggers.FindIndex(item => ReferenceEquals(item.Identity, retained.Identity));
                    void Store(CombatTrigger changed)
                    {
                        int live = LiveIndex(); retained = changed;
                        if (live >= 0) unit.Triggers[live] = retained;
                    }
                    // Native tests and marks each trigger once per queue record, then
                    // executes its entire batch, including a trigger marked Once.
                    int currentCount = TriggerCountModel.Query(context, unit.Freeze(), trigger, out string? countError);
                    if (countError != null) { unsupportedReason = countError; return; }
                    int fireCount = unchecked(currentCount * triggerCount);
                    for (int fire = 0; fire < fireCount; fire++)
                    {
                        if (!unit.Alive && kind != "OnDeath" && kind != "OnHit" && kind != "OnKill") return;
                        // CharacterState.ApplyEffects passes the host's spawner even
                        // when these trigger effects are bound to equipment.
                        int cacheCardId = unit.Source.SpawnerCardId;
                        CardInstanceState? cacheCard = context?.FindCard(cacheCardId);
                        if (cacheCard?.PlayedRoomUnitIds != null)
                            context = context!.WithCard(cacheCard.WithPlayedRoomUnits(units.Where(item => item.Alive && item.InRoom && item.Source.IsSpawning != true)
                                .Select(item => item.Source.Id).ToArray()));

                        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                        {
                            int live = LiveIndex();
                            if (live >= 0) retained = unit.Triggers[live];
                            effects = retained.Effects.ToArray();
                            CombatEffect effect = effects[effectIndex];
                            if (effect.Enchantment != null)
                            {
                                if (!UpdateEnchantmentEffect(unit.Source.Id, live >= 0 ? live : index, effectIndex)) break;
                            }
                            else if (effect.Summon != null)
                            {
                                if (!TriggeredSummonTest(unit, effect.Summon))
                                { if (effect.Summon.Tests.CancelSubsequent) break; continue; }
                                if (!ApplyTriggeredSummon(unit, effect.Summon, out int firstId)) break;
                                if (firstId > 0)
                                { effects[effectIndex] = effect.WithSummon(effect.Summon.WithFirstSpawned(firstId)); Store(retained.Fired(effects)); }
                            }
                            else if (effect.Type == "CardEffectAdvanceMoonPhase")
                            {
                                if (moonPhase == null || effect.Action == null)
                                { unsupportedReason = "Triggered moon-phase advance requires a complete train callback."; break; }
                                MoonPhaseTransitionResult shifted = moonPhase(CurrentRoom(), effect.Action, unit.Source.SpawnerCardId);
                                if (!shifted.Supported)
                                { unsupportedReason = shifted.UnsupportedReason; break; }
                                RoomCombatState changed = shifted.State!.Rooms.Single(room => room.RoomIndex == source.RoomIndex);
                                context = changed.Context;
                                foreach (CombatUnit actor in changed.Units)
                                    units.First(item => item.Source.Id == actor.Id).Apply(actor);
                            }
                            else if (effect.Action != null)
                            {
                                if (live < 0 && effect.Action.Type == "DrawNextTurn" && retained.StateId == null)
                                { unsupportedReason = "Detached bonus-draw trigger source requires captured persistent identity."; break; }
                                if (!ApplyTriggeredAction(unit, effect, overrideTarget, retained.StateId ?? (live < 0 ? index : live), effectIndex, kind, lastSpawnedOverrideUnitId)) break;
                            }
                            else if (effect.UnitUpgrade != null)
                            {
                                if (!ApplyTriggeredUpgrade(unit, effect.UnitUpgrade, kind, overrideTarget, lastSpawnedOverrideUnitId,
                                    retained.Origin?.EquipmentCardId ?? 0)) break;
                            }
                            else if (effect.Type == "CardEffectDespawnCharacter")
                            {
                                int remaining = effect.Counter - 1;
                                effects[effectIndex] = effect.WithCounter(remaining);
                                Store(retained.Fired(effects));
                                if (remaining <= 0 && unit.Alive)
                                {
                                    unit.Despawned = true; unit.Health = 0; RemovePhysicalPoint(unit); Emit("Despawn", unit, unit, 0);
                                    if (!source.Preview && context?.Statistics != null && unit.Source.Team == CombatTeam.Player && !DeferSpawner(unit))
                                        context = context.WithStatistics(context.LiveStatistics!.Increment(unit.Source.SpawnerCardId, "TimesExhausted",
                                            requireTrackedCard: context.CardInstances?.Count == 0));
                                    if (!source.Preview) RouteSpawner(unit);
                                }
                            }
                            else if (effect.Type == "CardEffectRewardGold")
                            {
                                if (source.Preview) continue;
                                int reward = GoldRewardModel.Adjust(effect.Value, multiplier: context!.Resources?.GoldRewardMultiplier ?? 1f);
                                if (reward != 0)
                                {
                                    context = context!.WithGold(Math.Max(0, unchecked(context.Gold + reward)));
                                    RefreshPyreAfterGold();
                                    if (unsupportedReason != null) break;
                                }
                                Emit("Gold", unit, unit, reward);
                            }
                            else if (effect.Type == "CardEffectAddBattleCard" && !source.Preview && !battleWon && context != null && context.AllScenarioBossesDead != true &&
                                (effect.Generation?.RequireHandSpace != true || context.Cards.Hand.Count < context.MaxHandSize)) AddCards(unit, effect);
                        }
                        context = context?.AfterCardEffects();
                    }
                    // The native local trigger object keeps resolving after list removal.
                    // Advancing the original index still observes list shifts and appends.
                    int remainingTriggerIndex = LiveIndex();
                    if (remainingTriggerIndex >= 0) Store(unit.Triggers[remainingTriggerIndex].Fired(unit.Triggers[remainingTriggerIndex].Effects));
                }
                if (unsupportedReason == null && !ApplyRelicUnitUpgradeTriggers(unit, kind, paramInt, paramInt2)) return;
                bool Present(WorkingUnit target, string id) => target.Statuses.Any(pair =>
                    string.Equals(pair.Key, id, StringComparison.OrdinalIgnoreCase) && pair.Value.Stacks > 0);
            }

            // Native relic callbacks run after the character's own trigger effects.
            private bool ApplyRelicUnitResource(WorkingUnit unit, string kind, int paramInt, CombatRelicState owner, RelicResourceEffect effect)
            {
                var result = RelicUnitResourceModel.ApplyOne(context!, owner.DataId, effect.EffectIndex,
                    unit.Freeze(), kind, unit.Alive, paramInt, source.Deployment, source.Preview);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                context = result.Context;
                if (result.Events.Count == 0) return true;
                if (effect.Type == RelicUnitResourceModel.Gold) RefreshPyreAfterGold();
                Emit(effect.Type == RelicUnitResourceModel.Gold ? "Gold" : "GainEnergy", unit, unit, result.Value);
                if (unsupportedReason != null) return false;
                return true;
            }

            private bool ApplyRelicCardDrawOnHeal(WorkingUnit unit, string kind, CombatRelicState owner, RelicResourceEffect effect)
            {
                RelicCardDrawOnHealResult result = RelicCardDrawOnHealModel.ApplyOne(context!, owner.DataId,
                    effect.EffectIndex, unit.Freeze(), kind, source.Deployment, source.Preview);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                context = result.Context;
                return true;
            }

            private bool ApplyRelicUnitTriggerCardGeneration(WorkingUnit unit, string kind, int paramInt,
                CombatRelicState owner, RelicResourceEffect effect)
            {
                CardGenerationResult result = RelicUnitTriggerCardGenerationModel.ApplyOne(context!, owner.DataId,
                    effect.EffectIndex, unit.Freeze(), kind, unit.Alive, paramInt, source.Deployment, source.Preview);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                context = result.Context;
                return true;
            }

            private bool ApplyRelicUnitUpgradeTriggers(WorkingUnit unit, string kind, int paramInt, int paramInt2)
            {
                if (context?.Relics == null) return true;
                foreach (CombatRelicState relic in context.Relics)
                for (int effectIndex = 0; effectIndex < relic.EffectTypes.Count; effectIndex++)
                {
                    var cardDrawOnHeal = relic.ResourceEffects?.SingleOrDefault(effect =>
                        effect.EffectIndex == effectIndex && RelicCardDrawOnHealModel.IsEffect(effect.Type));
                    if (cardDrawOnHeal != null)
                    {
                        if (!ApplyRelicCardDrawOnHeal(unit, kind, relic, cardDrawOnHeal)) return false;
                        continue;
                    }
                    var unitTriggerGeneration = relic.ResourceEffects?.SingleOrDefault(effect =>
                        effect.EffectIndex == effectIndex && RelicUnitTriggerCardGenerationModel.IsEffect(effect.Type));
                    if (unitTriggerGeneration != null)
                    {
                        if (!ApplyRelicUnitTriggerCardGeneration(unit, kind, paramInt, relic, unitTriggerGeneration)) return false;
                        continue;
                    }
                    var resource = relic.ResourceEffects?.SingleOrDefault(effect => effect.EffectIndex == effectIndex && RelicUnitResourceModel.IsEffect(effect.Type));
                    if (resource != null)
                    {
                        if (!ApplyRelicUnitResource(unit, kind, paramInt, relic, resource)) return false;
                        continue;
                    }
                    var rule = relic.UnitUpgradeTriggers?.SingleOrDefault(effect => effect.EffectIndex == effectIndex);
                    if (rule == null) continue;
                    if (rule.Trigger != kind || rule.Upgrade == null || unit.Source.IsPyre ||
                        !(unit.Source.Team == CombatTeam.Player ? rule.SourcePlayer : rule.SourceEnemy) ||
                        !rule.SubtypeIsNone && !unit.Source.Subtypes.Contains(rule.RequiredSubtype!) ||
                        kind == "OnDeath" && unit.Alive && paramInt != 1) continue;

                    int fireCount;
                    if (kind == "OnTroopAdded" || kind == "OnTroopRemoved") fireCount = paramInt2;
                    else if (context!.TriggerCounts == null) fireCount = 1;
                    else
                    {
                        var query = new CombatTrigger(kind, false, false, true, 1, Array.Empty<CombatEffect>(), false,
                            noCountModifiersAllowed: false);
                        fireCount = TriggerCountModel.Query(context, unit.Freeze(), query, out string? error);
                        if (error != null) { unsupportedReason = error; return false; }
                    }

                    string lifetime = rule.UpgradeLifetime == RelicUnitUpgradeTriggerModel.UntilEndOfBattle
                        ? "TemporaryUntilEndOfBattle" : "TemporaryUntilUnitDeath";
                    for (int fire = 0; fire < fireCount; fire++)
                    {
                        if (!ApplyUpgrade(unit, rule.Upgrade, lifetime, rule.ShouldRemove,
                            RoomCapacityModel.Maximum(context, source.RoomIndex, unit.Source.Team),
                            unit.Source.SpawnerCardId, kind, directApi: true, upgradeId: rule.Upgrade.DataId)) return false;
                        if (rule.UpgradeLifetime != RelicUnitUpgradeTriggerModel.UntilEndOfBattle || source.Preview ||
                            unit.Source.SpawnerCardId <= 0) continue;
                        CardInstanceState? spawner = context?.FindCard(unit.Source.SpawnerCardId);
                        if (spawner == null) continue;
                        CardModifiers temporary = rule.ShouldRemove
                            ? new CardModifiers(spawner.Temporary.Offsets,
                                spawner.Temporary.Upgrades.Where(item => item.DataId != rule.Upgrade.DataId).ToArray(),
                                spawner.Temporary.PersistentHealth, spawner.Temporary.ExternalInteractions)
                            : UnitModifierModel.Add(spawner.Temporary, rule.Upgrade);
                        context = context!.WithCard(spawner.WithModifiers(spawner.Permanent, temporary));
                    }
                }
                return unsupportedReason == null;
            }

            private void RefreshPyreAfterGold()
            {
                if (context?.Resources == null) return;
                WorkingUnit? local = units.FirstOrDefault(unit => unit.Source.IsPyre && !unit.Destroyed);
                RelicPyreAttackRefreshResult refreshed = local != null
                    ? RelicResourceModel.RefreshPyreAttack(context, local.Freeze(), source.Deployment)
                    : refreshPyre?.Invoke(context, source.Deployment) ?? new RelicPyreAttackRefreshResult(context, null);
                if (!refreshed.Supported) { unsupportedReason = refreshed.UnsupportedReason; return; }
                context = refreshed.Context;
                if (local != null && refreshed.Pyre != null) local.Apply(refreshed.Pyre);
            }

            private SpawnPointReference? SummonSourcePoint(WorkingUnit actor)
            {
                UnitSpawnPointState? point = context!.SpawnPoints!.Units.FirstOrDefault(unit => unit.UnitId == actor.Source.Id);
                return actor.Source.Team != CombatTeam.Player ? null : actor.Health <= 0 ? point?.LastKnown ?? point?.Current : point?.Current;
            }
            private bool TriggeredSummonTest(WorkingUnit actor, TriggeredSummonRule rule)
            {
                RoomPlayRule room = context!.SummonCatalog!.Rooms.First(item => item.RoomIndex == source.RoomIndex);
                return SummonSourcePoint(actor) != null ? room.Enabled && !room.SummonBlocked :
                    context.SpawnPoints!.FirstEmpty(source.RoomIndex, CombatTeam.Player) >= 0;
            }
            private int summonPivotMoves;
            private bool SummonPhysical(string operation, int unitId = 0, int pivot = -1, CombatTeam team = CombatTeam.Player)
            {
                var scope = new RoomCombatState(source.RoomIndex, source.Deployment, units.Select(unit => unit.Freeze()).ToArray(),
                    source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());
                var changed = BattleSpawnPointModel.Apply(context!.SpawnPoints!, scope, operation, team, unitId, pivot);
                if (!changed.Supported) { unsupportedReason = changed.Error; return false; }
                context = context.WithSpawnPoints(changed.State!);
                summonPivotMoves = changed.PivotMoves;
                OrderPhysicalUnits();
                return true;
            }
            private bool CenterAfterSummon() => SummonPhysical("Compact", team: CombatTeam.Enemy) && SummonPhysical("Compact");
            private void OrderPhysicalUnits()
            {
                int[] order = BattleSpawnPointModel.Order(context!.SpawnPoints, source.RoomIndex,
                    units.Where(unit => unit.InRoom).Select(unit => unit.Freeze()).ToArray()).Select(unit => unit.Id).ToArray();
                var ordered = order.Select(id => units.First(unit => unit.Source.Id == id))
                    .Concat(units.Where(unit => !unit.InRoom)).ToArray();
                units.Clear(); units.AddRange(ordered);
            }
            private void MergeSummon(RoomCombatResult result)
            {
                context = result.State!.Context;
                foreach (CombatUnit changed in result.State.Units.Concat(result.RetainedUnits))
                {
                    WorkingUnit? actor = units.FirstOrDefault(unit => unit.Source.Id == changed.Id);
                    if (actor == null) { actor = NewUnit(changed); units.Add(actor); }
                    else actor.Apply(changed);
                }
                events.AddRange(result.Events); dispatches.AddRange(result.Dispatches);
                if (result.Outcome == RoomOutcome.BattleWon) battleWon = true;
                OrderPhysicalUnits();
            }
            private bool ApplyTriggeredSummon(WorkingUnit actor, TriggeredSummonRule rule, out int firstId)
            {
                firstId = 0;
                SpawnPointReference? from = SummonSourcePoint(actor);
                var equipment = new List<int>();
                bool transferEquipment = from != null && !rule.IgnoreCardUpgrades && actor.Source.SpawnerCardId > 0;
                int position = 0;
                if (from != null)
                {
                    SpawnPointGroupState before = context!.SpawnPoints!.SelectedGroup(source.RoomIndex, CombatTeam.Player)!;
                    var prefix = before.Occupants.Take(from.Index).Where(id => id > 0).ToHashSet();
                    if (!SummonPhysical("Compact", pivot: from.Index)) return false;
                    int moved = summonPivotMoves;
                    SpawnPointGroupState after = context.SpawnPoints!.SelectedGroup(source.RoomIndex, CombatTeam.Player)!;
                    position = Math.Max(0, Math.Min(after.InnerCount - 1, after.Occupants.Take(after.InnerCount).Count(prefix.Contains)));
                    int occupant = after.Occupants[from.Index];
                    WorkingUnit? dead = units.FirstOrDefault(unit => unit.Source.Id == occupant);
                    if (dead != null && dead.Health <= 0 && (!SummonPhysical("Remember", occupant) || !SummonPhysical("Remove", occupant))) return false;
                    if (!source.Preview && moved > 0 && !CenterAfterSummon()) return false;
                    if (transferEquipment)
                        foreach (int id in actor.Source.EquipmentCards ?? Array.Empty<int>())
                        {
                            var definition = context.SummonCatalog!.Cards.FirstOrDefault(item => item.Creation.DataId == context.FindCard(id)?.DataId);
                            if (definition?.Equipment == null)
                            { unsupportedReason = "Missing triggered summon equipment attachment definition."; return false; }
                            if (!definition.Equipment.ReturnToHand) equipment.Add(id);
                        }
                }
                int requested = Math.Max(1, rule.Count);
                if (rule.AdditionalCharacterId.Length > 0) requested = unchecked(requested + requested);
                SpawnPointGroupState group = context!.SpawnPoints!.SelectedGroup(source.RoomIndex, CombatTeam.Player)!;
                int count = Math.Min(requested, group.GroupCount - group.Occupants.Take(group.GroupCount).Count(id => id != 0));
                int bornCount = 0;
                for (int offset = 0; offset < count && position + offset < group.InnerCount; offset++)
                {
                    int cardId = rule.IgnoreCardUpgrades ? 0 : actor.Source.SpawnerCardId;
                    if (cardId > 0 && (bornCount > 0 || from != null))
                    {
                        CardInstanceState? card = context!.FindCard(cardId);
                        SummonCardDefinition? copying = context.SummonCatalog!.Cards.FirstOrDefault(item => item.Creation.DataId == card?.DataId);
                        if (copying == null) { unsupportedReason = "Missing triggered summon copy definition."; return false; }
                        CardGenerationResult clone = CardGenerationModel.CloneDetached(context, copying.Creation, cardId);
                        if (!clone.Supported) { unsupportedReason = clone.UnsupportedReason; return false; }
                        context = clone.Context!; cardId = clone.AddedCards.Single().InstanceId;
                    }
                    string selected = rule.CharacterId;
                    if (rule.Pool.Count > 0)
                    {
                        RngDraw draw = context.BattleRng.Range(0, rule.Pool.Count);
                        selected = rule.Pool[draw.Value]; context = context.WithBattleRng(draw.State);
                    }
                    if (rule.AdditionalCharacterId.Length > 0 && bornCount >= count / 2) selected = rule.AdditionalCharacterId;
                    SummonUnitDefinition? definition = context.SummonCatalog!.Units.FirstOrDefault(unit => unit.CharacterId == selected);
                    if (definition == null) continue;
                    if (cardId == 0 && definition.Fallback != null)
                    {
                        CardGenerationResult created = CardGenerationModel.CreateDetached(context, definition.Fallback);
                        if (!created.Supported) { unsupportedReason = created.UnsupportedReason; return false; }
                        context = created.Context!; cardId = created.AddedCards.Single().InstanceId;
                    }
                    CardInstanceState? sourceCard = context.FindCard(cardId);
                    SummonCardDefinition? sourceDefinition = context.SummonCatalog!.Cards.FirstOrDefault(card => card.Creation.DataId == sourceCard?.DataId);
                    bool matches = sourceCard == null || sourceDefinition?.SpawnCharacterId.Length == 0 || sourceDefinition?.SpawnCharacterId == selected;
                    CombatUnit template = TriggeredSummonModel.WithSourceMatch(definition.Unit, matches);
                    var birthRule = new CardPlayRule(sourceCard?.DataId ?? "", template.AssetKey, 0, "SpawnMonster", "Standby",
                        template, definition.ExternalInteractions);
                    int at = from != null ? position + offset : context.SpawnPoints!.FirstEmpty(source.RoomIndex, CombatTeam.Player);
                    if (at < 0) continue;
                    var callbacks = new List<QueuedCharacterTrigger>();
                    UnitBirthResult born = UnitBirthModel.SpawnQueued(CurrentRoom(), birthRule, cardId, at,
                        from != null || !rule.HasParentCard || bornCount > 0 || cardId == 0,
                        context.StatusRules.First(status => status.Id == "cardless"), callbacks.Add,
                        units.Select(unit => unit.Freeze()).ToArray(), selectedSlot: from != null);
                    if (!born.Supported) { unsupportedReason = born.Result.UnsupportedReason; return false; }
                    MergeSummon(born.Result);
                    OrderPhysicalUnits();
                    foreach (var callback in callbacks) QueueCallback(callback);
                    if (rule.Upgrade != null)
                    {
                        RoomCombatResult upgraded = SpawnUpgradeModel.ApplyQueued(CurrentRoom(), born.UnitId, cardId, rule.Upgrade);
                        if (!upgraded.Supported) { unsupportedReason = upgraded.UnsupportedReason; return false; }
                        MergeSummon(upgraded);
                        foreach (var callback in upgraded.PendingCallbacks) QueueCallback(callback);
                    }
                    foreach (int equipmentId in equipment)
                    {
                        // Managers remove a dying actor before OnDeath; the retained
                        // object still exposes its equipment, but no longer forces a copy.
                        bool attached = units.Any(unit => !unit.Removed && unit.Source.DeathState?.IsBeingRemoved != true &&
                            unit.Source.EquipmentCards?.Contains(equipmentId) == true);
                        CardGenerationResult gear = EquipmentModel.CopyForTransfer(context!, equipmentId, attached, source.Preview);
                        if (!gear.Supported) { unsupportedReason = gear.UnsupportedReason; return false; }
                        context = gear.Context!;
                        int addedId = gear.AddedCards.Single().InstanceId;
                        var definitions = new BattlePlayRules(context.SummonCatalog!.Rooms, context.SummonCatalog.Cards
                            .Where(card => card.Equipment != null).Select(card => new CardPlayRule(card.Creation.DataId,
                                "", 0, "Equipment", "Standby", null, Array.Empty<string>(), equipment: card.Equipment)).ToArray());
                        RoomCombatResult equipped = ApplyEquipment(CurrentRoom(), born.UnitId, addedId, definitions,
                            deferAbilityCallbacks: true, deferCallbacks: true);
                        if (!equipped.Supported) { unsupportedReason = equipped.UnsupportedReason; return false; }
                        MergeSummon(equipped);
                        foreach (var callback in equipped.PendingCallbacks) QueueCallback(callback);
                        if (!source.Preview) context = EquipmentModel.BindTransferred(context!, addedId, born.UnitId);
                    }
                    if (bornCount++ == 0) firstId = born.UnitId;
                }
                if (!(source.Preview ? SummonPhysical("Compact") : CenterAfterSummon())) return false;
                // Native SpawnMonster completes the birth batch with the room-order
                // handler, so the last bound child updates its aura before queued OnSpawn.
                return UpdateEnchantments();
            }

            private RoomCombatState CurrentRoom() => new RoomCombatState(source.RoomIndex, source.Deployment,
                units.Where(unit => unit.Alive && unit.InRoom).Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions,
                context, source.Preview, CompanionBossSnapshot());

            private RoomCombatState PhysicalRoom() => new RoomCombatState(source.RoomIndex, source.Deployment,
                units.Where(unit => unit.Alive && unit.InRoom && !externalTurnActorIds.Contains(unit.Source.Id))
                    .Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());

            private void ImportEnchantmentRoom(RoomCombatState state)
            {
                context = state.Context;
                foreach (CombatUnit changed in state.Units.Concat(context?.Enchantments?.RetainedUnits
                    .Where(actor => actor.RoomIndex == source.RoomIndex).Select(actor => actor.Unit) ?? Array.Empty<CombatUnit>()))
                {
                    WorkingUnit? current = units.FirstOrDefault(unit => unit.Source.Id == changed.Id);
                    if (current != null) current.Apply(changed);
                }
            }

            private bool UpdateEnchantments(int? onlySourceId = null)
            {
                RoomCombatState current = EnchantmentWorldModel.Sync(CurrentRoom(), units.Where(unit => !unit.Alive || !unit.InRoom)
                    .Select(unit => unit.Freeze()).ToArray());
                RoomCombatResult result = EnchantmentWorldModel.Update(current, onlySourceId);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                ImportEnchantmentRoom(result.State!);
                foreach (QueuedCharacterTrigger callback in result.PendingCallbacks) QueueCallback(callback);
                return true;
            }

            private bool UpdateEnchantmentEffect(int unitId, int triggerIndex, int effectIndex)
            {
                RoomCombatState current = EnchantmentWorldModel.Sync(CurrentRoom(), units.Where(unit => !unit.Alive || !unit.InRoom)
                    .Select(unit => unit.Freeze()).ToArray());
                RoomCombatResult result = EnchantmentWorldModel.UpdateEffect(current, unitId, triggerIndex, effectIndex);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                ImportEnchantmentRoom(result.State!);
                foreach (QueuedCharacterTrigger callback in result.PendingCallbacks) QueueCallback(callback);
                return true;
            }

            private RoomCombatState UpgradeRoom(WorkingUnit target) => new RoomCombatState(source.RoomIndex, source.Deployment,
                units.Where(unit => unit.Alive && unit.InRoom || unit == target).Select(unit => unit.Freeze()).ToArray(),
                source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());

            private bool ApplyUpgrade(WorkingUnit target, CardUpgradeModifier upgrade, string lifetime, bool remove,
                int? roomCapacity, int sourceCardId, string? kind, bool directApi = false,
                string upgradeId = "", int? anonymousRemovalIndex = null, int equipmentSourceCardId = 0,
                bool scaleUpgrade = false, bool accumulateScaledUpgradeRecord = true)
            {
                RoomCombatResult result = UnitModifierModel.ApplyWithSettlement(UpgradeRoom(target), target.Source.Id, upgrade, lifetime,
                    remove, roomCapacity, sourceCardId, kind, (state, changed, sacrifice) =>
                    {
                        context = state.Context;
                        foreach (WorkingUnit other in units.Where(unit => unit.Source.Id != target.Source.Id && unit.InRoom))
                        {
                            CombatUnit? live = state.Units.FirstOrDefault(unit => unit.Id == other.Source.Id);
                            if (live != null) other.Apply(live);
                            else { other.InRoom = false; other.Removed = true; }
                        }
                        bool wasAlive = target.Alive;
                        target.Apply(changed);
                        if (sacrifice && wasAlive && !target.Alive)
                        {
                            target.Apply(target.Freeze().WithSacrifice(0));
                            if (!TryRevive(null, target, 0)) Death(null, target, 0);
                        }
                        // ApplyCardUpgrade runs the native queue before the effect writes
                        // the upgrade back to its source card. A queue already running
                        // defers child callbacks until that source write has completed.
                        if (!remove && enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                        if (unsupportedReason != null)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), unsupportedReason);
                        // The native effect retains its target object through removal and
                        // terminal clearing. Its remaining source-card work must still run.
                        return new RoomCombatResult(UpgradeRoom(target), RoomOutcome.Exchanged, 0, new List<CombatEvent>());
                    }, allowDyingTarget: true, enqueueCallback: QueueCallback, directApi: directApi,
                    upgradeId: upgradeId, anonymousRemovalIndex: anonymousRemovalIndex, equipmentSourceCardId: equipmentSourceCardId,
                    deferAbilityCallbacks: !directApi || deferAbilityCallbacks || runningTriggerQueue, scaleUpgrade: scaleUpgrade,
                    accumulateScaledUpgradeRecord: accumulateScaledUpgradeRecord);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                context = result.State!.Context;
                return true;
            }

            private CardTargets TriggerTargets(WorkingUnit actor, CardActionEffect action, bool testing, WorkingUnit? overrideTarget = null, int lastSpawnedOverrideUnitId = 0)
            {
                if (action.Target == "Pyre")
                    return new CardTargets(units.Where(unit => unit.Source.IsPyre).Take(1)
                        .Select(unit => unit.Source.Id).ToArray());
                if (action.Target == "Tower")
                {
                    if (!hasExternalTargetRoster)
                        return new CardTargets(Array.Empty<int>(), "Tower trigger targeting requires complete-train unit snapshots.");
                    WorkingUnit[] visible = units.Where(unit => unit.Alive && !unit.Removed && !unit.Despawned &&
                        (unit.InRoom || externalTurnActorIds.Contains(unit.Source.Id))).ToArray();
                    int topRoom = visible.Select(unit => unit.RoomIndex).Append(source.RoomIndex).Max();
                    RoomCombatState[] floors = Enumerable.Range(0, topRoom + 1).Select(roomIndex =>
                        new RoomCombatState(roomIndex, roomIndex == source.RoomIndex && source.Deployment,
                            visible.Where(unit => unit.RoomIndex == roomIndex).Select(unit => unit.Freeze()).ToArray(),
                            Array.Empty<string>(), context, source.Preview, CompanionBossSnapshot())).ToArray();
                    var tower = new TrainCombatState(floors, Array.Empty<EnemyMovement>(), 1, context);
                    return CardTargetModel.Collect(tower, source.RoomIndex, action, Array.Empty<int>(), isTesting: testing || source.Preview,
                        ignorePyre: true);
                }
                if (action.Target == "LastAttackedCharacter")
                {
                    // The override bypasses every team/status/health/subtype/boss filter,
                    // including for a retained dying victim. Trigger preflight has no override.
                    if (overrideTarget != null) return new CardTargets(new[] { overrideTarget.Source.Id });
                    WorkingUnit[] candidates = units.Where(unit => !unit.Removed &&
                        (unit.Source.Team == CombatTeam.Enemy ? action.AllowEnemy : action.AllowPlayer)).OrderBy(unit => unit.Source.Team).ToArray();
                    if (candidates.Any(unit => !unit.LastAttackerId.HasValue))
                        return new CardTargets(Array.Empty<int>(), "Last-attacked targeting requires captured attacker relationships.");
                    return new CardTargets(candidates.Where(unit => unit.LastAttackerId == actor.Source.Id).Select(unit => unit.Source.Id).ToArray());
                }
                if (action.Target != "Self") return CardTargetModel.Collect(CurrentRoom(), action, Array.Empty<int>(), isTesting: testing || source.Preview, lastSpawnedOverrideUnitId: lastSpawnedOverrideUnitId);
                // Native Self bypasses team, health, status, subtype and untouchable filters.
                if (action.Filters?.IgnoreBosses == true)
                {
                    if (!actor.Source.IsBoss.HasValue) return new CardTargets(Array.Empty<int>(), "Self boss filtering requires boss state.");
                    if (actor.Source.IsBoss.Value) return new CardTargets(Array.Empty<int>());
                }
                return new CardTargets(new[] { actor.Source.Id });
            }

            private bool ActionTriggerPassesTest(WorkingUnit actor, CombatTrigger trigger, int lastSpawnedOverrideUnitId)
            {
                if (!trigger.Effects.Any(effect => effect.UnitUpgrade != null || effect.Action != null || effect.Summon != null)) return true;
                bool passed = trigger.Effects.Count == 0;
                foreach (CombatEffect effect in trigger.Effects)
                {
                    if (effect.Summon != null)
                    {
                        if (!effect.Summon.Tests.ShouldTest) continue;
                        bool summonValid = TriggeredSummonTest(actor, effect.Summon);
                        if (!summonValid && effect.Summon.Tests.FailToCast) return false;
                        passed |= summonValid;
                        continue;
                    }
                    CardActionEffect? action = effect.UnitUpgrade ?? effect.Action;
                    if (action == null)
                    {
                        passed |= effect.Type != "CardEffectAddBattleCard" || !source.Preview &&
                            context!.AllScenarioBossesDead != true &&
                            (effect.Generation?.RequireHandSpace != true || context.Cards.Hand.Count < context.MaxHandSize);
                        continue;
                    }
                    if (action.Tests?.ShouldTest == false) continue;
                    CardTargets targets = TriggerTargets(actor, action, testing: true, lastSpawnedOverrideUnitId: lastSpawnedOverrideUnitId);
                    if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                    if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                    bool valid = effect.Type is "CardEffectAddStatusEffectIfNoPlayerUnits" or "CardEffectPyreDamageIfNoPlayerUnits"
                        ? NoPlayerUnitsInActorRoom(actor)
                        : action.Type == "AddStatus" ? StatusTest(actor, effect, targets) : ActionTestValid(action, targets, TestActionAmount(action));
                    if (!valid && action.Tests?.FailToCast == true) return false;
                    passed |= valid;
                }
                return passed;
            }

            private bool ApplyTriggeredUpgrade(WorkingUnit actor, CardActionEffect action, string kind, WorkingUnit? overrideTarget, int lastSpawnedOverrideUnitId,
                int parentEquipmentId)
            {
                CardTargets tested = TriggerTargets(actor, action, testing: true, overrideTarget, lastSpawnedOverrideUnitId);
                if (!tested.Supported) { unsupportedReason = tested.UnsupportedReason; return false; }
                if (tested.BattleRng.HasValue) context = context!.WithBattleRng(tested.BattleRng.Value);
                if (action.Type != "RemoveUnitUpgrade" && tested.UnitIds.Count == 0)
                    return action.Tests?.CancelSubsequent != true;
                CardTargets targets = TriggerTargets(actor, action, testing: false, overrideTarget, lastSpawnedOverrideUnitId);
                if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                foreach (int targetId in targets.UnitIds)
                {
                    WorkingUnit? target = units.FirstOrDefault(unit => unit.Source.Id == targetId && (unit.Alive || action.Target == "Self" && unit == actor || action.Target == "LastAttackedCharacter"));
                    if (target == null) continue;
                    if (action.Type == "BuffEquipment")
                    {
                        if (!ApplyTriggeredEquipmentBuff(actor, target, action, kind, parentEquipmentId)) return false;
                        continue;
                    }
                    if (!ApplyUpgrade(target, action.Upgrade!, action.Lifetime, action.Type == "RemoveUnitUpgrade",
                        null, actor.Source.SpawnerCardId, kind, scaleUpgrade: action.ScaleUpgrade)) return false;
                    Emit(action.Type, actor, target, 0);
                }
                return true;
            }

            private bool ApplyTriggeredEquipmentBuff(WorkingUnit actor, WorkingUnit target, CardActionEffect action, string kind, int parentEquipmentId)
            {
                // CardEffectBuffEquipment writes the equipped card before calling
                // CharacterState.ApplyCardUpgrade, including on a retained dying host.
                // Its payload's status list is unused; only the upgrade is applied.
                foreach (int equipmentId in target.Source.EquipmentCards!)
                {
                    if (parentEquipmentId > 0 && parentEquipmentId != equipmentId) continue;
                    var card = context!.FindCard(equipmentId);
                    if (card == null) { unsupportedReason = "Missing attached card for triggered equipment buff."; return false; }
                    var upgrade = action.Upgrade!.WithSharedReference(null);
                    bool persistent = !source.Preview && context.IsolatedBattlePreview != true;
                    if (persistent)
                    {
                        var existing = action.ScaleUpgrade ? card.Permanent.Upgrades.FirstOrDefault(value => value.DataId == upgrade.DataId) : null;
                        var cardUpgrade = existing == null ? CardMagicPowerModel.Scale(upgrade, CardMagicPowerModel.Multiplier(card)) : upgrade;
                        var changed = card.WithPermanentUpgrade(cardUpgrade, action.ScaleUpgrade);
                        if (action.ScaleUpgrade) upgrade = changed.Permanent.Upgrades.First(value => value.DataId == upgrade.DataId);
                        context = context.WithCard(changed);
                    }
                    bool scale = persistent && action.ScaleUpgrade;
                    if (scale && target.Source.Modifiers!.Upgrades.Any(value => value.DataId == upgrade.DataId))
                        upgrade = action.Upgrade.WithSharedReference(null);
                    if (!ApplyUpgrade(target, upgrade, "TemporaryUntilUnitDeath", false, null, 0, kind,
                        directApi: true, scaleUpgrade: scale, accumulateScaledUpgradeRecord: false)) return false;
                    Emit("BuffEquipment", actor, target, equipmentId);
                }
                return true;
            }

            private bool ActionTestValid(CardActionEffect action, CardTargets targets, int amount = 0) =>
                action.Type == "Null" ? targets.UnitIds.Count > 0 || action.NullTargetless :
                action.Type == "TransferAllStatusesFromUnit" ? targets.UnitIds.Count > 0 && TriggeredStatusTransferModel.Validate(action) == null :
                action.Type == "RemoveStatus" ? StatusRemovalModel.Test(CurrentRoom(), action, targets.UnitIds) :
                AbilityCooldownModel.IsEffect(action.Type) || AbilityLifecycleModel.IsEffect(action.Type) ? true :
                action.Type == "AdjustCapacity" ? !source.Preview && !battleWon && context!.AllScenarioBossesDead != true &&
                    RoomCapacityModel.Test(context, source.RoomIndex, units.Any(unit => unit.InRoom && unit.Source.Team == CombatTeam.Enemy), action.OnlyIfNoEnemies) :
                action.Type == ForgePointsModel.Effect ? ForgePointsModel.Test(ForgePointsPyreAlive()) :
                action.Type == "DrawNextTurn" ? !source.Preview && !battleWon && context!.AllScenarioBossesDead != true :
                EnergyModel.IsEffect(action.Type) ? !source.Preview && !battleWon && context!.AllScenarioBossesDead != true && EnergyModel.Test(context, action.Type) :
                action.Type == "Damage" ? amount >= 0 && (action.Range == null || action.Range.Max > 0) &&
                    (action.Target != "DropTargetCharacter" || targets.UnitIds.Count > 0) :
                action.Type == "RemoveUnitUpgrade" || action.Type == "Heal" && action.Target == "Room" || targets.UnitIds.Count > 0;

            private bool NoPlayerUnitsInActorRoom(WorkingUnit actor) => actor.InRoom &&
                !units.Any(unit => unit.InRoom && unit.Alive && unit.Source.Team == CombatTeam.Player);

            private bool? ForgePointsPyreAlive()
            {
                WorkingUnit? pyre = units.FirstOrDefault(unit => unit.Source.IsPyre);
                return pyre == null ? context?.EnergyState?.PyreAlive : pyre.Health > 0;
            }

            private int SampleActionAmount(CardActionEffect action)
            {
                if (action.Range == null) return action.Value;
                RngDraw draw = action.Range.Sample(context!.BattleRng);
                context = context.WithBattleRng(draw.State); return draw.Value;
            }

            private int TestActionAmount(CardActionEffect action) => action.Type == "Damage" ? SampleActionAmount(action) : action.Value;

            private bool ApplyTriggeredAction(WorkingUnit actor, CombatEffect effect, WorkingUnit? overrideTarget, int triggerIndex, int effectIndex, string triggerKind, int lastSpawnedOverrideUnitId)
            {
                CardActionEffect action = effect.Action!;
                if (action.Type == "AdjustPyreAttack")
                {
                    if (source.Preview) return true;
                    if (!actor.Source.IsPyre || context == null)
                    { unsupportedReason = "Permanent Pyre attack adjustment requires a live Pyre."; return false; }
                    RelicResourceResult adjusted = RelicResourceModel.AdjustPyreAttack(context, action.Value);
                    if (!adjusted.Supported) { unsupportedReason = adjusted.UnsupportedReason; return false; }
                    context = adjusted.Context;
                    Emit(action.Type, actor, actor, action.Value);
                    return true;
                }
                if (action.Type == "ForceHatchCheck")
                {
                    if (actor.Alive && actor.Count("hatch") == 0)
                        QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, actor.Freeze(), "OnHatched"));
                    return unsupportedReason == null;
                }
                if (action.Type == "Sacrifice")
                {
                    RoomCombatResult sacrificed = SacrificeRetained(actor.Freeze(), actor.Source.SpawnerCardId,
                        whileRunningQueue: true, doNotFullyRemove: action.DoNotFullyRemove == true,
                        returnSpawnerAfterQueue: false);
                    if (!sacrificed.Supported) unsupportedReason = sacrificed.UnsupportedReason;
                    return sacrificed.Supported;
                }
                if (action.Type == "RemoveStatusPerCorrupt")
                {
                    RoomCapacityState? capacity = context?.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == source.RoomIndex);
                    if (capacity?.Corruption is not int corruption || capacity.PermanentCorruption is not int permanent)
                    { unsupportedReason = "Hatch removal requires captured room corruption."; return false; }
                    int removedStacks = Math.Min(actor.Count("hatch"), corruption);
                    if (removedStacks > 0)
                    {
                        RoomCombatResult removed = StatusRemovalModel.Remove(RetainedStatusRoom(actor, new[] { actor.Source.Id }),
                            actor.Source.Id, "hatch", removedStacks, actor.Source.SpawnerCardId);
                        if (!removed.Supported) { unsupportedReason = removed.UnsupportedReason; return false; }
                        context = removed.State!.Context;
                        foreach (CombatUnit changed in removed.State.Units)
                            units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                        foreach (QueuedCharacterTrigger callback in removed.PendingCallbacks) QueueCallback(callback);
                    }
                    capacity = context!.RoomCapacities!.First(room => room.RoomIndex == source.RoomIndex);
                    var changedCapacity = new RoomCapacityState(source.RoomIndex, capacity.PlayerMaximum, capacity.EnemyMaximum,
                        capacity.IsPyre, Math.Max(permanent, corruption - removedStacks), capacity.Attachments,
                        capacity.MaxCorruption, permanent, capacity.AttachmentsAddedFromCards);
                    context = context.WithRoomCapacities(context.RoomCapacities.Select(room => room.RoomIndex == source.RoomIndex
                        ? changedCapacity : room).ToArray());
                    return unsupportedReason == null;
                }
                CardTargets tested = TriggerTargets(actor, action, testing: true, overrideTarget, lastSpawnedOverrideUnitId);
                if (!tested.Supported) { unsupportedReason = tested.UnsupportedReason; return false; }
                if (tested.BattleRng.HasValue) context = context!.WithBattleRng(tested.BattleRng.Value);
                bool valid = effect.Type is "CardEffectAddStatusEffectIfNoPlayerUnits" or "CardEffectPyreDamageIfNoPlayerUnits"
                    ? NoPlayerUnitsInActorRoom(actor)
                    : action.Type == "AddStatus" ? StatusTest(actor, effect, tested) : ActionTestValid(action, tested, TestActionAmount(action));
                if (unsupportedReason != null) return false;
                if (!valid) return action.Tests?.CancelSubsequent != true;
                CardTargets targets = TriggerTargets(actor, action, testing: false, overrideTarget, lastSpawnedOverrideUnitId);
                if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                if (action.Type == "TransferAllStatusesFromUnit")
                {
                    if (!targets.UnitIds.Contains(actor.Source.Id)) return true;
                    RoomCombatResult transfer = TriggeredStatusTransferModel.Apply(RetainedStatusRoom(actor, targets.UnitIds),
                        actor.Source.Id, action, actor.Source.SpawnerCardId);
                    if (!transfer.Supported) { unsupportedReason = transfer.UnsupportedReason; return false; }
                    context = transfer.State!.Context;
                    foreach (CombatUnit changed in transfer.State.Units)
                    {
                        WorkingUnit? current = units.FirstOrDefault(unit => unit.Source.Id == changed.Id);
                        if (current != null) current.Apply(changed);
                    }
                    foreach (QueuedCharacterTrigger callback in transfer.PendingCallbacks) QueueCallback(callback);
                    foreach (CombatEvent item in transfer.Events) events.Add(new CombatEvent(round, item.Kind, item.Actor, item.Target, item.Amount));
                    return true;
                }
                if (action.Type == "Null") return true;
                if (AbilityCooldownModel.IsEffect(action.Type) || action.Type == "RemoveStatus" || AbilityLifecycleModel.IsEffect(action.Type))
                {
                    IEnumerable<int> ordered = action.Type == "RemoveStatus" ? targets.UnitIds : targets.UnitIds.Reverse();
                    foreach (int id in ordered)
                    {
                        RoomCombatState retained = RetainedStatusRoom(actor, targets.UnitIds);
                        RoomCombatResult applied = AbilityLifecycleModel.IsEffect(action.Type)
                            ? AbilityLifecycleModel.Apply(retained, id, action.AbilityChange!, action.Type == "RemoveAbility", deferCallbacks: true) :
                            action.Type == "RemoveStatus" ? StatusRemovalModel.Effect(retained, id, action, actor.Source.SpawnerCardId,
                                whileRunningQueue: true) :
                            AbilityCooldownModel.Apply(retained, id, action, actor.Source.SpawnerCardId, triggerKind);
                        if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return false; }
                        context = applied.State!.Context;
                        foreach (CombatUnit changed in applied.State.Units) units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                        if (action.Type == "RemoveStatus" && !applied.State.Units.Any(unit => unit.Id == id))
                        {
                            var dead = applied.PendingCallbacks.LastOrDefault(item => item.Unit.Id == id && item.Unit.Health <= 0);
                            if (dead != null) units.First(unit => unit.Source.Id == id).Apply(dead.Unit);
                        }
                        foreach (QueuedCharacterTrigger callback in applied.PendingCallbacks) QueueCallback(callback);
                    }
                    return true;
                }
                if (action.Type == "AddStatus")
                {
                    RoomCombatResult applied = TriggeredStatusModel.Apply(RetainedStatusRoom(actor, targets.UnitIds), actor.Source.Id, effect, targets.UnitIds);
                    if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return false; }
                    context = applied.State!.Context;
                    foreach (CombatUnit changed in applied.State.Units) units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                    foreach (QueuedCharacterTrigger callback in applied.PendingCallbacks) QueueCallback(callback);
                    foreach (CombatEvent item in applied.Events) events.Add(new CombatEvent(round, item.Kind, item.Actor, item.Target, item.Amount));
                    return true;
                }
                int amount = action.Type == "AdjustCapacity" || action.Type == ForgePointsModel.Effect && source.Preview
                    ? action.Value : SampleActionAmount(action);
                if (action.Type == "AdjustCapacity")
                {
                    RoomCapacityResult adjusted = RoomCapacityModel.Apply(context!, source.RoomIndex, action);
                    if (!adjusted.Supported) { unsupportedReason = adjusted.UnsupportedReason; return false; }
                    context = adjusted.Context; Emit(action.Type, actor, actor, adjusted.Amount); return true;
                }
                if (action.Type == "DrawNextTurn")
                {
                    context = context!.WithCards(BonusDrawModel.Schedule(context.Cards,
                        BonusDrawState.UnitKey(actor.Source.Id, triggerIndex, effectIndex), amount, action.Upgrade));
                    Emit(action.Type, actor, actor, amount); return true;
                }
                if (action.Type == ForgePointsModel.Effect)
                {
                    if (!source.Preview)
                    {
                        ForgePointsResult changed = ForgePointsModel.Apply(context, amount, actor.Source.SpawnerCardId,
                            placement: source.Deployment, preview: source.Preview);
                        if (!changed.Supported) { unsupportedReason = changed.UnsupportedReason; return false; }
                        context = changed.Context;
                        Emit(action.Type, actor, actor, amount);
                    }
                    return true;
                }
                if (EnergyModel.IsEffect(action.Type))
                {
                    context = EnergyModel.Apply(context!, action.Type, amount);
                    Emit(action.Type, actor, actor, amount);
                    return true;
                }
                // Native samples once after collection, including an empty Room target set.
                if (action.Type == "Damage" && effect.DamageStatusMultiplier != null)
                    amount = unchecked(amount * actor.Count(effect.DamageStatusMultiplier));
                foreach (int targetId in targets.UnitIds)
                {
                    WorkingUnit? target = units.FirstOrDefault(unit => unit.Source.Id == targetId);
                    if (target == null) continue;
                    if (action.Type == "Heal") Heal(target, amount, "TriggeredHeal");
                    // Effect-state parameters have no parent card, but CharacterState.ApplyEffects
                    // passes the spawner as playedCard for damage traits and death attribution.
                    else if (target.Alive && !target.Removed) Damage(actor, target, amount, "TriggeredDamage", actor.Source.SpawnerCardId);
                }
                return true;
            }

            private RoomCombatState RetainedStatusRoom(WorkingUnit actor, IReadOnlyList<int> targets) =>
                new RoomCombatState(source.RoomIndex, source.Deployment,
                    units.Where(unit => unit.Alive || unit == actor || targets.Contains(unit.Source.Id)).Select(unit => unit.Freeze()).ToArray(),
                    source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());

            private bool StatusTest(WorkingUnit actor, CombatEffect effect, CardTargets targets)
            {
                CombatStatus? selected = null;
                if (effect.Type != "CardEffectAddStatusEffectBasedOnHealth" && source.Preview && context?.IsolatedBattlePreview == true && effect.Action!.Statuses.Count > 1)
                {
                    RngDraw choice = context.BattleRng.Range(0, effect.Action.Statuses.Count);
                    context = context.WithBattleRng(choice.State); selected = effect.Action.Statuses[choice.Value];
                }
                bool result = TriggeredStatusModel.Test(RetainedStatusRoom(actor, targets.UnitIds), effect, targets.UnitIds, out string? error, selected);
                if (error != null) unsupportedReason = error;
                return result;
            }

            private void AddCards(WorkingUnit actor, CombatEffect effect)
            {
                CardGenerationRule rule = effect.Generation ?? new CardGenerationRule(effect.Destination, effect.Count,
                    effect.CardPool.Select(id => new CardCreationRule(id, CardModifiers.Empty(), null, Array.Empty<string>())).ToArray(), effect.SkipDuplicateInHand);
                CardGenerationResult result = CardGenerationModel.Apply(context!, rule, actor.Source.SpawnerCardId);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return; }
                context = result.Context;
                foreach (CardToken card in result.AddedCards) Emit("AddCard", actor, actor, card.InstanceId);
            }

            private void Trigger(WorkingUnit unit, string id, int count)
            {
                if (unit.Statuses.TryGetValue(id, out CombatStatus? status) &&
                    (status.RemoveWhenTriggered || status.RemoveWhenTriggeredAfterCardPlayed || status.RemoveAtEndOfTurnIfTriggered) &&
                    (!source.Deployment || status.RemoveDuringDeployment))
                {
                    RelicStatusDecayResult removal = RelicStatusDecayModel.GetShouldRemove(context, true, id,
                        unit.Source.Team == CombatTeam.Player ? 2 : 1, source.Preview);
                    if (!removal.Supported) { unsupportedReason = removal.UnsupportedReason; return; }
                    if (removal.ShouldRemove)
                    {
                        if (status.RemoveWhenTriggeredAfterCardPlayed)
                            unit.Statuses[id] = status.WithMarkedForRemovalAfterCardPlayed(true);
                        else if (status.RemoveAtEndOfTurnIfTriggered)
                            unit.Statuses[id] = status.WithRemoveAllAtEnd(true);
                        else
                        {
                            if (!source.Preview && context?.QueryFrame?.SelectedRoomIndex != null)
                                context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: source.RoomIndex));
                            RemoveStatus(unit, id, count);
                        }
                    }
                }
            }

            private void QueueCallback(QueuedCharacterTrigger callback)
            {
                callback = callback.WithCaptureAllDispatches(traceAllDispatches);
                string? error = StatusCallbackModel.Admit(context, callback, out QueuedCharacterTrigger admitted);
                if (error != null) { unsupportedReason = error; return; }
                if (admitted.Admission == CharacterTriggerAdmission.Rejected) return;
                callback = admitted;
                if (enqueueCharacterTrigger != null) { enqueueCharacterTrigger(callback); return; }
                bool local = callback.RoomIndex == source.RoomIndex;
                WorkingUnit? actor = local ? units.FirstOrDefault(unit => unit.Source.Id == callback.Unit.Id) : null;
                if (actor == null)
                {
                    // Aura maps retain preview-born objects outside the selected room
                    // list. Their callbacks still execute on the retained actor without
                    // restoring that actor's membership or allocating another identity.
                    CombatUnit current = local ? context?.Enchantments?.RetainedUnits
                        .FirstOrDefault(unit => unit.Unit.Id == callback.Unit.Id)?.Unit ?? callback.Unit : callback.Unit;
                    actor = NewUnit(current, false);
                    if (local) units.Add(actor);
                }
                WorkingUnit? dying = callback.DyingCharacter == null ? null : local ? units.FirstOrDefault(unit => unit.Source.Id == callback.DyingCharacter.Id) : null;
                if (dying == null && callback.DyingCharacter != null)
                { dying = NewUnit(callback.DyingCharacter, false); if (local) units.Add(dying); }
                WorkingUnit? overridden = callback.OverrideTarget == null ? null : units.FirstOrDefault(unit => unit.Source.Id == callback.OverrideTarget.Id);
                if (overridden == null && callback.OverrideTarget != null)
                { overridden = NewUnit(callback.OverrideTarget, false); if (local) units.Add(overridden); }
                triggerQueue.Enqueue((callback.RoomIndex, actor, callback.Kind, callback.CanFireTriggers, callback.ParamInt, overridden,
                    callback.ParamInt2, callback.ParamString, dying, callback.TriggerCount, callback.LastSpawnedOverrideUnitId,
                    callback.ExclusiveTriggerStateId));
            }

            private void RemoveStatus(WorkingUnit unit, string id, int count)
            {
                var callbacks = new List<QueuedCharacterTrigger>();
                RoomCombatResult relicAdded = RelicStatusStackModel.OnStatusRemoved(CurrentRoom(), unit.Source.Id, id, count, null);
                if (!relicAdded.Supported) { unsupportedReason = relicAdded.UnsupportedReason; return; }
                context = relicAdded.State!.Context;
                foreach (CombatUnit changed in relicAdded.State.Units)
                {
                    WorkingUnit? actor = units.FirstOrDefault(candidate => candidate.Source.Id == changed.Id);
                    actor?.Apply(changed);
                }
                callbacks.AddRange(relicAdded.PendingCallbacks);
                CombatUnit before = unit.Freeze();
                unit.Remove(id, count);
                CombatUnit after = unit.Freeze();
                CombatStatus? status = before.RegisteredStatus(id);
                if (status != null)
                {
                    StatusAttackDamageResult changed = StatusAttackDamageModel.Change(context, after, status,
                        status.Stacks - (after.RegisteredStatus(id)?.Stacks ?? 0), adding: false);
                    if (!changed.Supported) { unsupportedReason = changed.UnsupportedReason; return; }
                    context = changed.Context; unit.Apply(changed.Unit!);
                    if (id == "conduit" && context != null)
                        context = ConduitModel.Change(context, unit.RoomIndex, unit.Freeze(), source.Preview,
                            refreshSelection: false, order: units.Where(actor => actor.InRoom).Select(actor => actor.Source.Id).ToArray());
                }
                StatusCallbackModel.Removed(unit.RoomIndex, before, unit.Freeze(), id, callbacks);
                foreach (QueuedCharacterTrigger callback in callbacks) QueueCallback(callback);
            }

            private int StatusMagnitude(WorkingUnit unit, string id)
            {
                if (!unit.Statuses.TryGetValue(id, out CombatStatus? status)) return 0;
                StatusScalingResult modifier = RelicStatusMagnitudeModel.GetModifier(context, id, unit.Source.Team == CombatTeam.Player ? 2 : 1);
                if (!modifier.Supported) { unsupportedReason = modifier.UnsupportedReason; return 0; }
                context = modifier.Context;
                return unchecked(unchecked(status.ParamInt + modifier.Stacks) * status.Stacks);
            }

            private bool Active(CombatStatus status) => !source.Deployment || !status.SkipDuringDeployment;

            private void Clear(bool after)
            {
                if (source.Deployment) return;
                bool relentless = units.Any(unit => unit.Alive && unit.InRoom && unit.Source.Team == CombatTeam.Enemy && unit.Has("relentless"));
                foreach (WorkingUnit unit in units.Where(unit => unit.Alive && unit.InRoom))
                    foreach (CombatStatus status in unit.RegisteredStatuses().ToArray())
                        if (status.RemoveAfterPostCombat == after)
                        {
                            if (status.RemoveStackAtEnd)
                            {
                                RelicStatusDecayResult removal = RelicStatusDecayModel.GetShouldRemove(context, true, status.Id,
                                    unit.Source.Team == CombatTeam.Player ? 2 : 1, source.Preview);
                                if (!removal.Supported) { unsupportedReason = removal.UnsupportedReason; return; }
                                if (removal.ShouldRemove && !(relentless && status.PreventRemovalDuringRelentless))
                                {
                                    FocusRoomForStatusRemoval(status.Id);
                                    RemoveStatus(unit, status.Id, 1);
                                    if (unit.Count(status.Id) == 0) unit.RemoveDefinition(status.Id);
                                }
                            }
                            else if (!status.RemoveStackAtEnd && status.RemoveAllAtEnd)
                            {
                                FocusRoomForStatusRemoval(status.Id);
                                RemoveStatus(unit, status.Id, status.Stacks);
                                if (unit.Count(status.Id) == 0) unit.RemoveDefinition(status.Id);
                            }
                        }
                if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
            }

            private void FocusRoomForStatusRemoval(string statusId)
            {
                if (statusId != "cooldown" && !source.Preview && context?.QueryFrame?.SelectedRoomIndex != null)
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: source.RoomIndex));
            }

            private bool BothTeamsPresent() => units.Any(unit => unit.Alive && unit.InRoom && unit.Source.Team == CombatTeam.Player &&
                !unit.Has("untouchable")) && units.Any(unit => unit.Alive && unit.InRoom && unit.Source.Team == CombatTeam.Enemy &&
                !unit.Has("untouchable"));

            private string Signature()
            {
                var text = new StringBuilder();
                foreach (WorkingUnit unit in units)
                {
                    text.Append(unit.Source.Id).Append(':').Append(unit.Health).Append(':').Append(unit.Source.BaseAttack)
                        .Append(':').Append(unit.Source.MaxHealth).Append(':').Append(unit.Source.Size).Append(':');
                    text.Append(unit.LastAttackerId).Append(':').Append(unit.Removed).Append(':');
                    if (unit.Source.Ability is UnitAbilityState ability)
                    {
                        text.Append("|ability:").Append(ability.DataId).Append(':').Append(ability.Cooldown).Append(':')
                            .Append(ability.CooldownAtSpawn).Append(':').Append(ability.FromEquipment).Append(':')
                            .Append(ability.Resolving).Append(':').Append(ability.PreviousDataId);
                        foreach (UnitAbilityDefinition? definition in new[] { ability.Definition, ability.PreviousDefinition })
                            if (definition != null) text.Append(':').Append(definition.IsUnitAbility).Append(':')
                                .Append(definition.Cooldown).Append(':').Append(definition.CooldownAtSpawn);
                    }
                    if (unit.Source.AbilityRules is UnitAbilityRules abilityRules)
                    {
                        text.Append("|ability-rules:").Append(abilityRules.PreventEquipmentAbilities);
                        foreach (CombatStatus status in abilityRules.AuthoredStartingStatuses)
                            text.Append(':').Append(status.Id).Append(':').Append(status.Stacks);
                    }
                    if (unit.Source.StatusDictionary != null)
                    {
                        StatusDictionaryState dictionary = unit.Freeze().StatusDictionary!;
                        text.Append("|statusSlots:");
                        foreach (string? id in dictionary.Slots) text.Append(id?.Length ?? -1).Append(':').Append(id).Append(';');
                        foreach (int index in dictionary.FreeSlots) text.Append(index).Append(',');
                    }
                    if (unit.Source.Modifiers != null)
                    {
                        UnitModifiers modifiers = unit.Source.Modifiers;
                        text.Append(modifiers.AttackDamage).Append(',').Append(modifiers.AttackDamageAdded).Append(',')
                            .Append(modifiers.DamageBuff).Append(',').Append(modifiers.DamageBuffFromStatusEffects?.ToString() ?? "legacy").Append(',')
                            .Append(modifiers.RawSize).Append(',').Append(modifiers.EquipmentLimit).Append(';');
                        foreach (CardUpgradeModifier upgrade in modifiers.Upgrades)
                            text.Append(upgrade.DataId.Length).Append(':').Append(upgrade.DataId).Append(':')
                                .Append(upgrade.Stats.Damage).Append(',').Append(upgrade.Stats.Health).Append(',')
                                .Append(upgrade.Stats.Size).Append(',').Append(upgrade.UnhealedHealth).Append(',').Append(upgrade.DamageBuff)
                                .Append(':').Append(upgrade.AbilityUpgrade?.Definition?.DataId).Append(':').Append(upgrade.DoNotReplaceExistingAbility).Append(';');
                    }
                    foreach (CombatStatus status in unit.Statuses.Values.OrderBy(status => status.Id, StringComparer.Ordinal))
                        text.Append(status.Id).Append('=').Append(status.Stacks).Append(',');
                    if (unit.Source.StatusRegistry != null)
                    {
                        text.Append("|registry:");
                        foreach (CombatStatus status in unit.RegisteredStatuses())
                            text.Append(status.Id.Length).Append(':').Append(status.Id).Append(':').Append(status.Stacks).Append(':')
                                .Append(status.ParamInt).Append(':').Append(status.Hidden).Append(':').Append(status.DisplayCategory).Append(':')
                                .Append(status.RemoveWhenTriggered).Append(':').Append(status.RemoveStackAtEnd).Append(':')
                                .Append(status.RemoveAllAtEnd).Append(':').Append(status.RemoveAfterPostCombat).Append(':')
                                .Append(status.PreventRemovalDuringRelentless).Append(':').Append(status.SkipDuringDeployment).Append(':')
                                .Append(status.RemoveDuringDeployment).Append(':').Append(status.Stackable).Append(';');
                    }
                    text.Append(';');
                    foreach (CombatTrigger trigger in unit.Triggers)
                    {
                        text.Append(trigger.Kind).Append(':').Append(trigger.HasTriggered).Append(':');
                        foreach (CombatEffect effect in trigger.Effects) text.Append(effect.Value).Append(':').Append(effect.Counter).Append(',');
                    }
                }
                if (context != null)
                {
                    text.Append(context.LastAbilityActivatorUnitId).Append(':');
                    foreach (string disabled in context.PermanentlyDisabledAbilities ?? Array.Empty<string>())
                        text.Append("|disabled:").Append(disabled);
                    text.Append(context.Gold).Append(':').Append(context.NextCardId).Append(':')
                        .Append(context.BattleRng.S0).Append(',').Append(context.BattleRng.S1).Append(',')
                        .Append(context.BattleRng.S2).Append(',').Append(context.BattleRng.S3);
                    foreach (CardToken card in context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard))
                        text.Append('|').Append(card.InstanceId).Append(':').Append(card.DataId);
                    foreach (CardInstanceState card in context.CardInstances ?? Array.Empty<CardInstanceState>())
                    {
                        text.Append("|card:").Append(card.InstanceId);
                        AppendModifiers(card.Permanent); AppendModifiers(card.Temporary);
                    }
                    foreach (CardInstanceState card in context.CardRegistry ?? Array.Empty<CardInstanceState>())
                    {
                        text.Append("|reference:").Append(card.InstanceId);
                        AppendModifiers(card.Permanent); AppendModifiers(card.Temporary);
                    }
                    foreach (AbilityCardCacheEntry entry in context.AbilityCardCache ?? Array.Empty<AbilityCardCacheEntry>())
                        text.Append("|abilityCard:").Append(entry.DataId.Length).Append(':').Append(entry.DataId).Append(':').Append(entry.InstanceId);
                    if (context.Statistics != null) text.Append("|stats:").Append(context.Statistics.Signature());
                }
                return text.ToString();

                void AppendModifiers(CardModifiers modifiers)
                {
                    text.Append('[').Append(modifiers.PersistentHealth);
                    foreach (CardUpgradeModifier upgrade in modifiers.Upgrades)
                        text.Append('|').Append(upgrade.DataId.Length).Append(':').Append(upgrade.DataId).Append(':')
                            .Append(upgrade.Stats.Damage).Append(',').Append(upgrade.Stats.Health).Append(',')
                            .Append(upgrade.Stats.Size).Append(',').Append(upgrade.UnhealedHealth).Append(',').Append(upgrade.DamageBuff);
                    text.Append(']');
                }
            }

            private void Emit(string kind, WorkingUnit? actor, WorkingUnit target, int amount) =>
                events.Add(new CombatEvent(round, kind, actor?.Source.Id ?? 0, target.Source.Id, amount));

            private RoomCombatResult Finish(RoomOutcome outcome)
            {
                if (!source.Preview && context != null)
                    context = UnitStandbyModel.MarkDead(context, units.Where(unit => !unit.Alive).Select(unit => unit.Freeze()).ToArray());
                var removedIds = new HashSet<int>(units.Where(unit => !unit.Alive || unit.Removed || unit.Despawned).Select(unit => unit.Source.Id));
                var localIds = units.Where(unit => !externalTurnActorIds.Contains(unit.Source.Id))
                    .Select(unit => unit.Source.Id).ToHashSet();
                foreach (CardInstanceState card in context?.CardRegistry ?? context?.CardInstances ?? Array.Empty<CardInstanceState>())
                {
                    if (card.PlayedRoomUnitIds == null) continue;
                    if (card.RawPlayedRoomUnitIds != null)
                    {
                        var retainedPreviewIds = context?.Enchantments?.RetainedUnits.Where(actor => actor.Preview &&
                            actor.Unit.Health > 0 && actor.Unit.DeathState?.IsDestroyed != true).Select(actor => actor.Unit.Id).ToHashSet();
                        int[] visible = card.RawPlayedRoomUnitIds.Concat(card.PlayedRoomUnitIds.Where(id =>
                            retainedPreviewIds?.Contains(id) == true)).Distinct().Where(id => localIds.Contains(id) ?
                            !removedIds.Contains(id) : card.PlayedRoomUnitIds.Contains(id)).ToArray();
                        if (!visible.SequenceEqual(card.PlayedRoomUnitIds))
                            context = context!.WithCard(card.WithRoomCacheState(visible, card.RawPlayedRoomUnitIds));
                    }
                    else if (!source.Preview && card.PlayedRoomUnitIds.Any(removedIds.Contains))
                        context = context!.WithCard(card.WithPlayedRoomUnits(card.PlayedRoomUnitIds.Where(id => !removedIds.Contains(id)).ToArray()));
                }
                RoomCombatState state = EnchantmentWorldModel.Sync(PhysicalRoom(), units.Where(unit => !unit.Alive || !unit.InRoom ||
                    externalTurnActorIds.Contains(unit.Source.Id)).Select(unit => unit.Freeze()).ToArray());
                state = state.WithCompanionBosses(CompanionBossSnapshot());
                context = state.Context;
                return unsupportedReason != null
                ? new RoomCombatResult(null, RoomOutcome.Unsupported, round, events, unsupportedReason) : new RoomCombatResult(
                state,
                outcome, round, events, pendingCallbacks: nativeCardDamage ? deferredDamageDeaths.Select(dead =>
                    new QueuedCharacterTrigger(source.RoomIndex, dead.Unit.Freeze(), returnSpawnerAfterQueue: dead.Return,
                        deferUntilRemoval: true, harvestAfterDeath: !dead.Unit.Despawned,
                        completePhysicalRemovalAfterQueue: context?.SpawnPoints != null, removalLifecycle: true,
                        captureAllDispatches: traceAllDispatches)).ToArray() : null,
                retainedUnits: units.Where(unit => !unit.Alive || externalTurnActorIds.Contains(unit.Source.Id))
                    .Select(unit => unit.Freeze()).ToArray(), dispatches: dispatches);
            }
        }
    }
}
