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
            unchecked(StatusAmount("buff") + StatusAmount("valor") + StatusAmount("mass") - StatusAmount("debuff"));
        public int Attack => UnitAttackOverrideModel.Resolve(Statuses, MaxHealth) ?? RelicLateAttackModel.Apply(this, unchecked(AttackBeforeStatuses + AttackFromStatuses +
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
        public int? LastFeederId { get; }
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
        public int? MoonPhaseMask { get; }
        // Null denotes an uncaptured history; fresh tracked births start at zero.
        public UnitDamageHistory? DamageHistory { get; }
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
            UnitBumpRules? bumpRules = null, int? lastFeederId = null, int? moonPhaseMask = null, UnitDamageHistory? damageHistory = null)
        {
            Id = id;
            DamageHistory = damageHistory;
            LastAttackerId = lastAttackerId;
            LastFeederId = lastFeederId;
            NextTriggerId = nextTriggerId;
            Ability = ability;
            AbilityRules = abilityRules;
            HordeDefinition = hordeDefinition;
            IsSpawning = isSpawning;
            SacrificeCardId = sacrificeCardId;
            DeathState = deathState;
            BumpRules = bumpRules;
            MoonPhaseMask = moonPhaseMask;
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
        internal CombatUnit WithDamageHistory(UnitDamageHistory history) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, BumpRules, LastFeederId, MoonPhaseMask, history);
        internal CombatUnit WithHealth(int health) => new CombatUnit(Id, AssetKey, Team, BaseAttack, health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, BumpRules, LastFeederId, MoonPhaseMask, damageHistory: DamageHistory);
        internal CombatUnit WithHealthAndMaxHealth(int health, int maxHealth) => new CombatUnit(Id, AssetKey, Team,
            BaseAttack, health, maxHealth, CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId,
            Size, StatusImmunities, Subtypes, Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards,
            NextTriggerId, Ability, StatusDictionary, AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId,
            DeathState, BumpRules, LastFeederId, MoonPhaseMask, damageHistory: DamageHistory);
        internal CombatUnit WithMoonPhaseMask(int mask, bool reset = false, bool initialize = false) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, BumpRules, LastFeederId,
            MoonPhaseMask.HasValue || initialize ? (reset ? 0 : (MoonPhaseMask ?? 0) & ~3) | (mask & 3) : (int?)null, damageHistory: DamageHistory);
        internal CombatUnit WithSacrifice(int cardId, bool markSacrifice = true) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, cardId, DeathState == null || !markSacrifice ? DeathState : new UnitDeathState(
                DeathState.HasFinishedDying, DeathState.IsBeingRemoved, DeathState.HasStatisticsListener,
                DeathState.PendingStatisticsCardId, DeathState.IsSacrifice.HasValue ? true : (bool?)null, DeathState.StatisticsListenerOnce,
                DeathState.IsDespawned, DeathState.IsDestroyed).WithPreviewPrimary(DeathState.PreviewPrimary), bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);

        internal CombatUnit WithDeathState(UnitDeathState state) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, state.WithLifecycle(
                state.IsDespawned ?? DeathState?.IsDespawned, state.IsDestroyed ?? DeathState?.IsDestroyed)
                    .WithPreviewPrimary(state.PreviewPrimary ?? DeathState?.PreviewPrimary), bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);
        internal CombatUnit WithTriggers(IReadOnlyList<CombatTrigger> triggers) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);
        internal CombatUnit WithTriggerAllocation(IReadOnlyList<CombatTrigger> triggers, int nextTriggerId) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, nextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);

        internal CombatUnit WithBumpRules(UnitBumpRules rules) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: rules,
            lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);

        internal CombatUnit WithoutRemovedCompanion(ISet<int> activeIds)
        {
            UnitBumpRules? rules = BumpRules;
            if (!OuterBossDefinitions.IsAngelDuoActor(AssetKey) || rules == null ||
                rules.PairedBossUnitId == 0 || activeIds.Contains(rules.PairedBossUnitId)) return this;
            return WithBumpRules(new UnitBumpRules(rules.Loops, rules.CompanionBoss, rules.OuterBoss,
                rules.OuterBossRelentlessPhase, 0, rules.CastingAnchorSlots, rules.RelentlessAnchorSlots)).WithEndsBattleOnDeath(true);
        }

        internal CombatUnit WithStatusDamage(int amount) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers!.WithStatusDamage(amount), IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);

        internal CombatUnit WithStatusRegistry(IReadOnlyList<CombatStatus> statusRegistry) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers, IsBoss, LastAttackerId, statusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);

        internal CombatUnit AfterDestruction() => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers?.AfterDestruction(), IsBoss, LastAttackerId, StatusRegistry,
            EquipmentCards == null ? null : Array.Empty<int>(), NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules,
            lastFeederId: LastFeederId.HasValue ? 0 : (int?)null, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);
        internal CombatUnit WithRawAttack(int amount) => new CombatUnit(Id, AssetKey, Team,
            Math.Max(0, unchecked(amount + Modifiers!.DamageBuff)), Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            Modifiers.WithRawAttack(amount), IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId,
            Ability, StatusDictionary, AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState,
            bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);

        internal CombatUnit WithModifiers(UnitModifiers modifiers) => new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth,
            CanAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes,
            modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary,
            AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);

        internal CombatUnit WithOuterBossPhase(bool canAttack, bool relentless) => new CombatUnit(Id, AssetKey, Team,
            BaseAttack, Health, MaxHealth, canAttack, IsPyre, EndsBattleOnDeath, Statuses, Triggers, SpawnerCardId,
            Size, StatusImmunities, Subtypes, Modifiers, IsBoss, LastAttackerId, StatusRegistry, EquipmentCards,
            NextTriggerId, Ability, StatusDictionary, AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId,
            DeathState, BumpRules == null ? null : new UnitBumpRules(BumpRules.Loops, BumpRules.CompanionBoss,
                BumpRules.OuterBoss, relentless, BumpRules.PairedBossUnitId, BumpRules.CastingAnchorSlots,
                BumpRules.RelentlessAnchorSlots), LastFeederId, MoonPhaseMask, damageHistory: DamageHistory);

        internal CombatUnit WithEndsBattleOnDeath(bool endsBattleOnDeath) => endsBattleOnDeath == EndsBattleOnDeath ? this :
            new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth, CanAttack, IsPyre, endsBattleOnDeath,
                Statuses, Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes, Modifiers, IsBoss, LastAttackerId,
                StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary, AbilityRules, HordeDefinition,
                IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);

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
                Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes, Modifiers, IsBoss, 0, StatusRegistry, EquipmentCards, NextTriggerId, Ability, StatusDictionary, AbilityRules, HordeDefinition, IsSpawning, SacrificeCardId, DeathState, bumpRules: BumpRules, lastFeederId: LastFeederId, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);
        internal CombatUnit WithoutDestroyedFeeder(ISet<int> destroyedIds) => !LastFeederId.HasValue || !destroyedIds.Contains(LastFeederId.Value)
            ? this : new CombatUnit(Id, AssetKey, Team, BaseAttack, Health, MaxHealth, CanAttack, IsPyre, EndsBattleOnDeath, Statuses,
                Triggers, SpawnerCardId, Size, StatusImmunities, Subtypes, Modifiers, IsBoss, LastAttackerId, StatusRegistry,
                EquipmentCards, NextTriggerId, Ability, StatusDictionary, AbilityRules, HordeDefinition, IsSpawning,
                SacrificeCardId, DeathState, bumpRules: BumpRules, lastFeederId: 0, moonPhaseMask: MoonPhaseMask, damageHistory: DamageHistory);
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
        public RelentlessChoiceRequest? RelentlessChoice { get; }
        public bool PreviewTruncated { get; }
        public bool ShouldSerializeRelentlessChoice() => RelentlessChoice != null;
        public bool ShouldSerializePreviewTruncated() => PreviewTruncated;
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        internal IReadOnlyList<CombatUnit> RetainedUnits { get; }
        internal IReadOnlyList<CharacterTriggerDispatch> Dispatches { get; }

        internal RoomCombatResult(RoomCombatState? state, RoomOutcome outcome, int rounds,
            List<CombatEvent> events, string? reason = null, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? pendingCallbacks = null, IReadOnlyList<CombatUnit>? retainedUnits = null, IReadOnlyList<CharacterTriggerDispatch>? dispatches = null,
            RelentlessChoiceRequest? relentlessChoice = null, bool previewTruncated = false)
        {
            Dispatches = Array.AsReadOnly((dispatches ?? Array.Empty<CharacterTriggerDispatch>()).ToArray());
            RetainedUnits = Array.AsReadOnly((retainedUnits ?? Array.Empty<CombatUnit>()).ToArray());
            State = state; Outcome = outcome; Rounds = rounds;
            RelentlessChoice = relentlessChoice; PreviewTruncated = previewTruncated;
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
            internal int? AttackDamageDealt { get; }
            internal QueuedCharacterTrigger(int roomIndex, CombatUnit unit, string kind = "OnDeath", bool returnSpawnerAfterQueue = false, bool deferUntilRemoval = false, int paramInt = 0,
                CombatUnit? overrideTarget = null, int paramInt2 = 0, string? paramString = null, CombatUnit? dyingCharacter = null, bool canFireTriggers = true,
                int triggerCount = 1, int lastSpawnedOverrideUnitId = 0, bool harvestAfterDeath = false, bool completePhysicalRemovalAfterQueue = false,
                CharacterTriggerAdmission admission = CharacterTriggerAdmission.Pending, bool removalLifecycle = false, bool preservePhysicalPoint = false,
                int? exclusiveTriggerStateId = null, bool captureAllDispatches = false, bool relicDamageRemoval = false, bool relicDamageApplied = false,
                bool relicMovementStatusApplied = false, int? attackDamageDealt = null)
            { RoomIndex = roomIndex; Unit = unit; Kind = kind; ReturnSpawnerAfterQueue = returnSpawnerAfterQueue; DeferUntilRemoval = deferUntilRemoval; ParamInt = paramInt; OverrideTarget = overrideTarget; ParamInt2 = paramInt2; ParamString = paramString; DyingCharacter = dyingCharacter; CanFireTriggers = canFireTriggers; TriggerCount = triggerCount; LastSpawnedOverrideUnitId = lastSpawnedOverrideUnitId; HarvestAfterDeath = harvestAfterDeath; CompletePhysicalRemovalAfterQueue = completePhysicalRemovalAfterQueue; Admission = admission; RemovalLifecycle = removalLifecycle; PreservePhysicalPoint = preservePhysicalPoint; ExclusiveTriggerStateId = exclusiveTriggerStateId; CaptureAllDispatches = captureAllDispatches; RelicDamageRemoval = relicDamageRemoval; RelicDamageApplied = relicDamageApplied; RelicMovementStatusApplied = relicMovementStatusApplied; AttackDamageDealt = attackDamageDealt; }
            internal QueuedCharacterTrigger WithRelicDamageRemoval() => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue,
                Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches, true, RelicDamageApplied, RelicMovementStatusApplied, AttackDamageDealt);
            internal QueuedCharacterTrigger WithRelicDamageApplied() => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue,
                Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches, RelicDamageRemoval, true, RelicMovementStatusApplied, AttackDamageDealt);
            internal QueuedCharacterTrigger WithRelicMovementStatusApplied() => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue,
                Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches,
                RelicDamageRemoval, RelicDamageApplied, true, AttackDamageDealt);
            internal QueuedCharacterTrigger InRoom(int roomIndex) => new QueuedCharacterTrigger(roomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue, Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches, RelicDamageRemoval, RelicDamageApplied, RelicMovementStatusApplied, AttackDamageDealt);
            internal QueuedCharacterTrigger WithAdmission(CharacterTriggerAdmission admission) => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue, admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches, RelicDamageRemoval, RelicDamageApplied, RelicMovementStatusApplied, AttackDamageDealt);
            internal QueuedCharacterTrigger WithCaptureAllDispatches(bool capture) => new QueuedCharacterTrigger(RoomIndex, Unit, Kind,
                ReturnSpawnerAfterQueue, DeferUntilRemoval, ParamInt, OverrideTarget, ParamInt2, ParamString, DyingCharacter,
                CanFireTriggers, TriggerCount, LastSpawnedOverrideUnitId, HarvestAfterDeath, CompletePhysicalRemovalAfterQueue,
                Admission, RemovalLifecycle, PreservePhysicalPoint, ExclusiveTriggerStateId, CaptureAllDispatches || capture, RelicDamageRemoval, RelicDamageApplied, RelicMovementStatusApplied, AttackDamageDealt);
        }
        private static readonly HashSet<string> KnownStatuses = new HashSet<string>(StringComparer.Ordinal)
        {
            "armor", "damage shield", "dazed", "stealth", "ambush", "multistrike", "burst",
            "spikes", "lifesteal", "fragile", "piercing", "immune", "immobile",
            "relentless", "sweep", "sniper", "rooted", "haste", "untouchable", "endless",
            "buff", "debuff", "regen", "poison", "decay", "corruption", "corrupt_poison", "melee weakness", "spell weakness", "silenced", "muted", "valor", "pyregel",
            "heal multiplier", "heal immunity", "cooldown", "unit_ability", "unit_ability_available", "horde", "cardless", "undying", "ephemeral", "purify", "mageblade", "titanskin", "titanite", "duality", "conduit",
            "newmoon", "fullmoon", "stygian_blessing", "scorch", "trample", "spell shield", "unstable", "hatch", "avarice", "inedible", "eatmany", "steelguard",
            "fixedattack", "armorattack", "equalizer", "inert", "spark", "captured_soul", "restricted", "unit_grafted_equipment", "mass", "zeal", "tenacious", "chained", "pain", "spite",
            StatusAttackDamageModel.Malaise, Witherbloom
        };
        internal const string Witherbloom = "witherbloom";
        internal static bool KnowsStatus(string id) => KnownStatuses.Contains(id);

        internal const string RailforgedKillAttacker = "R1_Attacker_AscendGainAtk_Ver2";

        internal static bool IsRailforgedAttackerKillBump(CombatUnit actor, CombatTrigger trigger) =>
            actor.AssetKey == RailforgedKillAttacker && actor.Team == CombatTeam.Enemy &&
            actor.BumpRules is { OuterBoss: false, CompanionBoss: false } &&
            trigger is { Kind: "OnKill", Once: false, IgnoreSilence: false, FireCount: 1,
                Conditions: null, TriggerAtThreshold: 0, Origin: { UpgradeId: "", EquipmentCardId: 0,
                    IsFromEquipment: false, OnlyIfEquipped: false }, Effects: [var bump, var armor] } &&
            bump is { Type: "CardEffectBump", Action: { Type: "Bump", Target: "Self", Value: -1,
                Range: null, AllowEnemy: true, AllowPlayer: true, UseCardTraits: false, Statuses.Count: 0 } } &&
            armor is { Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Self", Value: 0,
                Range: null, AllowEnemy: true, AllowPlayer: true, UseCardTraits: false, Statuses: [{ Id: "armor", Stacks: 10 }] } };

        private static bool IsRailforgedKillPreviewShift(CombatUnit actor, CombatTrigger trigger) =>
            actor.Team == CombatTeam.Enemy && trigger is { Kind: "OnShift", Once: false, IgnoreSilence: false,
                FireCount: 1, Conditions: null, Effects: [{ Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus",
                    Target: "Self", Value: 0, Range: null, UseCardTraits: false, Statuses: [var status] } action }] } &&
            (actor.AssetKey == RailforgedKillAttacker && action.AllowEnemy && action.AllowPlayer && status is { Id: "burst", Stacks: 1 } ||
                actor.AssetKey == "R1_Heavy_HealOnShift_Ver2" && !action.AllowEnemy && !action.AllowPlayer && status is { Id: "armor", Stacks: 20 });

        private static bool MatchesWitherbloom(CombatStatus status) =>
            status.ParamInt == 1 && status.Stackable == true && status.Hidden == false && status.DisplayCategory == "Negative" &&
            status.Propagatable == true && status.ExcludeHeroPropagation == false && status.ExcludeMonsterPropagation == true &&
            status.TriggerVfxEnemy == true && status.TriggerVfxPlayer == true &&
            !status.RemoveWhenTriggered && !status.RemoveStackAtEnd && !status.RemoveAllAtEnd && !status.RemoveAfterPostCombat &&
            !status.PreventRemovalDuringRelentless && status.SkipDuringDeployment && !status.RemoveDuringDeployment &&
            !status.RemoveWhenTriggeredAfterCardPlayed && !status.RemoveAtEndOfTurnIfTriggered && !status.MarkedForRemovalAfterCardPlayed;

        internal static RoomCombatResult ApplyWitherbloomTick(RoomCombatState state, int unitId) =>
            new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false).WitherbloomTick(unitId);

        public static RoomCombatResult Exchange(RoomCombatState state, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool updateSelectedRoom = true, IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null,
            IReadOnlyCollection<int>? pendingPhysicalDeathIds = null, IReadOnlyList<RoomPlayRule>? roomRules = null) =>
            Run(state, false, refreshPyre: refreshPyre, updateSelectedRoom: updateSelectedRoom, externalTargets: externalTargets,
                pendingPhysicalDeathIds: pendingPhysicalDeathIds, roomRules: roomRules);
        public static RoomCombatResult Resolve(RoomCombatState state, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool updateSelectedRoom = true, CombatUnit? externalPyre = null, int externalPyreRoomIndex = -1,
            IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null,
            IReadOnlyList<RelentlessDecision>? relentlessDecisions = null, UnitCopyCatalog? unitCopyCatalog = null,
            IReadOnlyList<RoomPlayRule>? roomRules = null)
        {
            int? interval = state.Context?.QueryFrame?.RelentlessNotificationInterval;
            var decisions = relentlessDecisions?.Count > 0 ? relentlessDecisions.ToArray() : null;
            if (!ValidRelentlessDecisions(interval, decisions))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    "Invalid Relentless notification interval or decisions.");
            return ResolveCore(state, refreshPyre, updateSelectedRoom, externalPyre, externalPyreRoomIndex, externalTargets,
                decisions != null ? (room, round) => decisions.FirstOrDefault(choice =>
                    choice.RoomIndex == room && choice.CompletedRounds == round) : null, unitCopyCatalog, roomRules);
        }

        internal static bool ValidRelentlessDecisions(int? interval, IReadOnlyList<RelentlessDecision>? decisions) =>
            !(interval <= 0) && decisions?.Any(choice => choice.RoomIndex < 0 || choice.CompletedRounds <= 0 ||
                !interval.HasValue || choice.CompletedRounds % interval.Value != 0) != true &&
            decisions?.GroupBy(choice => (choice.RoomIndex, choice.CompletedRounds)).Any(group => group.Count() != 1) != true;

        internal static RoomCombatResult ResolveCore(RoomCombatState state, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre,
            bool updateSelectedRoom, CombatUnit? externalPyre, int externalPyreRoomIndex,
            IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets, Func<int, int, RelentlessDecision?>? chooseRelentless,
            UnitCopyCatalog? unitCopyCatalog = null, IReadOnlyList<RoomPlayRule>? roomRules = null)
        {
            // Native room modifiers run once before the first exchange, including
            // deaths and queued callbacks; unit PreCombat is a separate phase.
            RoomCombatResult pre = RoomPreCombatModel.Apply(state, refreshPyre);
            if (!pre.Supported) return pre;
            RoomCombatResult result = pre.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated ? pre :
                Run(pre.State!, true, refreshPyre: refreshPyre, updateSelectedRoom: updateSelectedRoom,
                    externalPyre: externalPyre, externalPyreRoomIndex: externalPyreRoomIndex, externalTargets: externalTargets,
                    chooseRelentless: chooseRelentless, unitCopyCatalog: unitCopyCatalog, roomRules: roomRules);
            if (!ReferenceEquals(result, pre) && pre.Events.Count > 0)
                result = new RoomCombatResult(result.State, result.Outcome, result.Rounds, pre.Events.Concat(result.Events).ToList(),
                    result.UnsupportedReason, result.PendingCallbacks, result.RetainedUnits, result.Dispatches,
                    result.RelentlessChoice, result.PreviewTruncated);
            if (!result.Supported || result.State!.Context == null || result.PreviewTruncated) return result;
            if (!state.Preview && !ReferenceEquals(result, pre) && pre.Outcome == RoomOutcome.Exchanged && pre.RetainedUnits.Count > 0)
            {
                // A death still awaiting physical removal at combat entry
                // finishes during the first yield. Actors already removed by
                // the room effect can retain LastKnown until a later frame.
                var completed = pre.RetainedUnits.Where(unit => unit.Health <= 0 && state.Units.Any(original => original.Id == unit.Id) &&
                    pre.State!.Context?.SpawnPoints?.Units.Any(point => point.UnitId == unit.Id && point.Current != null) == true)
                    .Select(unit => unit.Id).ToHashSet();
                result = new RoomCombatResult(result.State.WithContext(ClearFinishedRemovalPoints(result.State.Context, completed)),
                    result.Outcome, result.Rounds, result.Events.ToList(), result.UnsupportedReason,
                    result.PendingCallbacks, result.RetainedUnits, result.Dispatches);
            }
            RelicFirstCardCostResult endOfTurn = RelicFirstCardCostModel.EndRoomCombat(result.State.Context);
            if (!endOfTurn.Supported)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, result.Rounds, result.Events.ToList(), endOfTurn.UnsupportedReason);
            return new RoomCombatResult(result.State.WithContext(endOfTurn.Context), result.Outcome, result.Rounds,
                result.Events.ToList(), result.UnsupportedReason, result.PendingCallbacks, result.RetainedUnits, result.Dispatches);
        }

        public static RoomCombatResult ApplyUnitPostCombat(RoomCombatState state, IReadOnlyList<int> cannotAttackOrHeal,
            IReadOnlyList<int> cannotFireTriggers, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            UnitCopyCatalog? unitCopyCatalog = null)
        {
            string? error = Validate(state, linkedHealthOnly: true);
            if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? targets = null;
            if (state.Context?.LifeLinks != null)
            {
                if (state.Context.Enchantments?.AutomaticLifecycle != true || state.Preview)
                    return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                        "True-final-boss post-combat effects require the captured non-preview whole train.");
                if (state.Context.Enchantments.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && !TrueFinalBossCombatModel.SupportsAlly(unit, state.Context)))
                    return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                        "True-final-boss post-combat with allied units requires native cross-floor callback calibration.");
                targets = state.Context.Enchantments.Rooms.Where(room => room.RoomIndex != state.RoomIndex)
                    .SelectMany(room => room.Units.Select(unit => (room.RoomIndex, unit))).ToArray();
            }
            return new Engine(state, new List<CombatEvent>(), refreshPyre: refreshPyre, externalTargets: targets, unitCopyCatalog: unitCopyCatalog)
                .UnitPostCombat(cannotAttackOrHeal, cannotFireTriggers);
        }

        public static RoomCombatResult ApplyFeeding(RoomCombatState state, CombatTeam team,
            IReadOnlyList<int>? foodIds = null, int triggerCount = 1, int selfFeederId = 0)
        {
            string? error = Validate(state);
            if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            if (state.Deployment || !state.Units.Any(unit => unit.Team == team && unit.Health > 0 && IsFood(unit)) ||
                !state.Units.Any(unit => unit.Team == team && unit.Health > 0 && unit.Status("untouchable") == null && !IsFood(unit)))
                return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            if (selfFeederId != 0 && !state.Units.Any(unit => unit.Id == selfFeederId && unit.Team == team && unit.Health > 0))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "The overridden feeder must be a captured living friendly actor.");
            return new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false).FeedOnly(team, foodIds, triggerCount, selfFeederId);
        }

        internal static bool IsFood(CombatUnit unit) => unit.Status("inedible") == null && unit.Status("silenced") == null &&
            unit.Status("muted") == null && unit.Triggers.Any(trigger => trigger.Kind == "OnEaten");

        public static RoomCombatResult ApplyUnitTurn(RoomCombatState state, int unitId,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool ignoreAttackPrevention = false, CombatUnit? externalActor = null, IReadOnlyList<int>? overrideTargets = null,
            CombatTeam? targetTeam = null)
        {
            // A single attack turn does not dispatch the deferred PostCombat
            // boss effects; health and attack statuses still run normally.
            string? error = Validate(state, linkedHealthOnly: true);
            bool localActor = state.Units.Any(unit => unit.Id == unitId);
            if (state.Units.Any(unit => unit.AssetKey == "Boss_Level9_Savagery_V2" && unit.Health == 1 &&
                unit.Status("lifelink") != null && unit.Status("pyrebane") != null) && !TrueFinalBossCombatModel.Supports(state))
                error = "Dormant Savagery attack turns require dedicated native combat-target calibration.";
            if (error != null || localActor == (externalActor != null) || externalActor != null && externalActor.Id != unitId)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Unit turn requires exactly one captured local or external actor.");
            if (externalActor != null && (externalActor.Health <= 0 || externalActor.DeathState?.IsDestroyed == true))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    "External unit turn requires a living actor.");
            if (overrideTargets != null && (overrideTargets.Distinct().Count() != overrideTargets.Count ||
                overrideTargets.Any(id => !state.Units.Any(unit => unit.Id == id))))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Unit turn override targets require captured unique room identities.");
            return new Engine(state, new List<CombatEvent>(), refreshPyre: refreshPyre, externalActor: externalActor,
                externalActorRoomIndex: externalActor == null ? -1 : state.Context?.Enchantments?.Rooms
                    .FirstOrDefault(room => room.Units.Any(unit => unit.Id == externalActor.Id))?.RoomIndex ?? -1)
                .UnitTurn(unitId, ignoreAttackPrevention, overrideTargets, targetTeam);
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

        internal static RoomCombatResult ApplyCharacterPhase(RoomCombatState state, int unitId, string kind, Action<QueuedCharacterTrigger> enqueue,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, bool allowFreshHivemind = false,
            bool allowFirstHivemindCombat = false)
        {
            string? error = Validate(state, linkedHealthOnly: kind is "PreCombat" or "EndTurnPreHandDiscard",
                allowFreshHivemind: allowFreshHivemind, allowFirstHivemindCombat: allowFirstHivemindCombat);
            if (error != null || !state.Units.Any(unit => unit.Id == unitId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Character phases require a living character actor.");
            return new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: enqueue, refreshPyre: refreshPyre).CharacterPhase(unitId, kind);
        }

        internal static RoomCombatResult ApplyQueuedCharacterTrigger(RoomCombatState state, QueuedCharacterTrigger queued, Action<QueuedCharacterTrigger> enqueue, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            CombatUnit? externalPyre = null, int externalPyreRoomIndex = -1,
            IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null,
            Func<RoomCombatState, CardActionEffect, int, BumpResult>? birthBump = null,
            Func<RoomCombatState, CardActionEffect, int, MoonPhaseTransitionResult>? moonPhase = null)
        {
            if (RelicFloorTransitionDamageModel.TryDefer(state, queued, enqueue, out RoomCombatResult floorDamage))
                return floorDamage;
            if (RelicMovementStatusModel.TryApplyBeforeMovementTrigger(state, queued, enqueue, out RoomCombatResult movementStatus))
                return movementStatus;
            RoomCombatResult triggered = new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: enqueue,
                resetPreviewTriggers: false, refreshPyre: refreshPyre, externalPyre: externalPyre,
                externalPyreRoomIndex: externalPyreRoomIndex, externalTargets: externalTargets, birthBump: birthBump, moonPhase: moonPhase)
                .TraceAllDispatches(queued.CaptureAllDispatches).QueuedTrigger(queued);
            RoomCombatResult relicStatusEffect = RelicStatusAddedEffectModel.ApplyAfterTrigger(triggered, queued);
            RoomCombatResult entered = RelicRoomEntryStatusModel.ApplyAfterEntry(relicStatusEffect, queued, enqueue);
            return RelicAfterSpawnChanceStatusModel.ApplyAfterEntry(entered, queued, enqueue);
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
            error = TriggeredSummonModel.Validate(state, trigger.Effects[effectIndex].Summon, actor, trigger.Kind);
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
            bool whileRunningQueue = false, bool doNotFullyRemove = false, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool destroyRoom = false)
            => new Engine(state, new List<CombatEvent>(), deferSpawnerExhaustion: !whileRunningQueue, enqueueCharacterTrigger: enqueue, resetPreviewTriggers: false, refreshPyre: refreshPyre)
                .SacrificeRetained(unit, sourceCardId, whileRunningQueue, doNotFullyRemove, returnSpawnerAfterQueue: !whileRunningQueue,
                    destroyRoom: destroyRoom);

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
            Func<RoomCombatState, int, RoomCombatResult>? relicBirthEffect = null,
            Func<RoomCombatState, int, string, RoomCombatResult>? relicCharacterEffect = null)
        {
            Func<RoomCombatState, int, IReadOnlyList<QueuedCharacterTrigger>, RoomCombatResult>? completeTrainBirth =
                relicBirthEffect == null ? null : (frame, addedUnitId, _) => relicBirthEffect(frame, addedUnitId);
            return ApplySpawnTriggers(state, unitId, fromCard, startingApplications, null, refreshPyre,
                moonPhase: moonPhase, relicBirthEffect: completeTrainBirth, relicCharacterEffect: relicCharacterEffect);
        }
        internal static RoomCombatResult ApplySpawnTriggers(RoomCombatState state, int unitId, bool fromCard,
            IReadOnlyList<CombatStatus>? startingApplications, Action<QueuedCharacterTrigger>? enqueue,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, SpawnStatusApplication? additionalStatus = null,
            Func<RoomCombatState, CardActionEffect, int, MoonPhaseTransitionResult>? moonPhase = null,
            Func<RoomCombatState, int, IReadOnlyList<QueuedCharacterTrigger>, RoomCombatResult>? relicBirthEffect = null,
            Func<RoomCombatState, int, string, RoomCombatResult>? relicCharacterEffect = null, bool copyHordeBeforeOrder = false)
        {
            string? error = Validate(state);
            CombatUnit? unit = state.Units.FirstOrDefault(item => item.Id == unitId);
            if (error != null || unit == null || fromCard && unit.SpawnerCardId <= 0)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Spawn triggers require a living new unit and its source card identity.");
            return new Engine(state, new List<CombatEvent>(), pendingSummonCardId: fromCard ? unit.SpawnerCardId : 0,
                enqueueCharacterTrigger: enqueue, refreshPyre: refreshPyre, moonPhase: moonPhase,
                relicBirthEffect: relicBirthEffect, relicCharacterEffect: relicCharacterEffect).Spawn(unitId, fromCard, startingApplications,
                    additionalStatus: additionalStatus, copyHordeBeforeOrder: copyHordeBeforeOrder);
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
            bool deferSpawnerExhaustion = false, int attackerUnitId = 0, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool captureAllDispatches = false) => ApplyCardDamage(state, targetId, damage, sourceCardId, deferSpawnerExhaustion, true, attackerUnitId,
                refreshPyre: refreshPyre, captureAllDispatches: captureAllDispatches);

        internal static RoomCombatResult ApplyRelicDamage(RoomCombatState state, int targetId, int damage,
            Action<QueuedCharacterTrigger> enqueueCharacterTrigger, bool piercing = false) =>
            ApplyNativeCardDamageAfterTraits(state, targetId, damage, 0, enqueueCharacterTrigger: enqueueCharacterTrigger,
                captureAllDispatches: true, piercing: piercing, relicSourced: true);

        internal static RoomCombatResult ApplyCardDamageAfterTraits(RoomCombatState state, int targetId, int damage, int sourceCardId,
            bool deferSpawnerExhaustion, int attackerUnitId = 0, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            CombatUnit? externalActor = null, int externalActorRoomIndex = -1,
            Action<QueuedCharacterTrigger>? enqueueCharacterTrigger = null) => ApplyCardDamage(state, targetId, damage, sourceCardId,
                deferSpawnerExhaustion, false, attackerUnitId, refreshPyre, externalActor, externalActorRoomIndex, enqueueCharacterTrigger);

        internal static RoomCombatResult ApplyNativeCardDamageAfterTraits(RoomCombatState state, int targetId, int damage, int sourceCardId,
            int attackerUnitId = 0,
            Func<RoomCombatState, RoomCombatResult>? finishDamageQueue = null, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            CombatUnit? externalActor = null, int externalActorRoomIndex = -1,
            Action<QueuedCharacterTrigger>? enqueueCharacterTrigger = null, bool captureAllDispatches = false,
            bool piercing = false, IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null,
            bool centerPhysicalRemovals = true, bool relicSourced = false,
            Func<RoomCombatResult, RoomCombatResult>? settleDamageCallbacks = null)
        {
            string? error = Validate(state, halfHealthLifecycle: true);
            bool localAttacker = state.Units.Any(unit => unit.Id == attackerUnitId);
            if (error != null || damage < 0 || state.Preview && state.Context?.Enchantments?.TemporaryPreviewUnitIds == null || state.Context?.SpawnPoints == null ||
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
                externalActorRoomIndex: externalActorRoomIndex, externalActorInRoom: false, externalTargets: externalTargets,
                centerDamageDeaths: centerPhysicalRemovals, settleDamageCallbacks: settleDamageCallbacks)
                .TraceAllDispatches(captureAllDispatches)
                .CardDamage(targetId, damage, sourceCardId, false, attackerUnitId, piercing, relicSourced);
        }

        private static RoomCombatResult ApplyCardDamage(RoomCombatState state, int targetId, int damage, int sourceCardId,
            bool deferSpawnerExhaustion, bool applyTraits, int attackerUnitId,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            CombatUnit? externalActor = null, int externalActorRoomIndex = -1,
            Action<QueuedCharacterTrigger>? enqueueCharacterTrigger = null, bool captureAllDispatches = false)
        {
            string? error = Validate(state, linkedHealthOnly: true, halfHealthLifecycle: true);
            if (state.Units.Any(unit => unit.Id == targetId && unit.Status("lifelink") != null))
            {
                CardInstanceState? source = sourceCardId == 0 ? null : state.Context?.FindCard(sourceCardId);
                if (attackerUnitId != 0 || sourceCardId != 0 && (source?.MaskDescriptor?.Definition.CardType != "Spell" ||
                    source.Permanent.Upgrades.Count != 0 || source.Temporary.Upgrades.Count != 0 ||
                    CardBranchMaskModel.Resolve(source).Traits.Count != 0))
                    error = "Linked boss damage requires no attacker and a captured plain spell source; modified card signatures remain guarded.";
            }
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

        internal static RoomCombatResult ApplyUnitModification(RoomCombatState state, CombatUnit changed,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, CombatContext? linkedContext = null) =>
            new Engine(state, new List<CombatEvent>(), refreshPyre: refreshPyre).ModifyUnit(changed, linkedContext);

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
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, bool accumulateScaledUpgradeRecord = true,
            bool captureAllDispatches = false)
        {
            string? error = Validate(state, halfHealthLifecycle: true);
            if (error != null || !state.Units.Any(unit => unit.Id == targetId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Missing unit upgrade target.");
            var callbacks = new List<QueuedCharacterTrigger>();
            bool deferredRemoval = remove && state.Units.Any(unit => unit.Triggers.Any(HalfHealthDeathwishModel.IsTrigger));
            RoomCombatResult result = new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false, refreshPyre: refreshPyre,
                enqueueCharacterTrigger: deferredRemoval ? callbacks.Add : (Action<QueuedCharacterTrigger>?)null)
                .TraceAllDispatches(captureAllDispatches)
                .UnitUpgrade(targetId, upgrade, lifetime, remove, roomCapacity, sourceCardId, triggerKind,
                    scaleUpgrade: scaleUpgrade, accumulateScaledUpgradeRecord: accumulateScaledUpgradeRecord);
            return new RoomCombatResult(result.State, result.Outcome, result.Rounds, result.Events.ToList(), result.UnsupportedReason,
                callbacks, result.RetainedUnits, result.Dispatches);
        }

        internal static RoomCombatResult ApplyDirectUnitUpgrade(RoomCombatState state, int targetId, CardUpgradeModifier upgrade,
            bool remove, string upgradeId, int? anonymousRemovalIndex, bool deferCallbacks = false, int equipmentSourceCardId = 0, bool captureAllDispatches = false, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, bool allowDyingTarget = false)
        {
            string? error = Validate(state, allowDyingTarget ? targetId : (int?)null, halfHealthLifecycle: true);
            if (state.Preview && state.Units.Any(unit => unit.Triggers.Any(HalfHealthDeathwishModel.IsTrigger)))
                error = "Half-health Deathwish preview upgrade APIs require separate native calibration.";
            if (deferCallbacks && state.Units.Any(unit => unit.Triggers.Any(HalfHealthDeathwishModel.IsTrigger)))
                error = "Half-health Deathwish upgrades inside an external running queue require native calibration.";
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
            bool deferAbilityCallbacks = false, bool deferCallbacks = false, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool grafted = false)
        {
            string? error = Validate(state);
            CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || target?.EquipmentCards == null || target.Modifiers == null || state.Context?.OtherPiles == null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error ?? "Missing equipment unit/pile state.");
            if (!remove && target.EquipmentCards.Contains(cardId))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Repeated raw attachment of the same equipment object is not modeled.");
            var callbacks = new List<QueuedCharacterTrigger>();
            var result = new Engine(state, new List<CombatEvent>(), enqueueCharacterTrigger: remove || deferCallbacks ? callbacks.Add : (Action<QueuedCharacterTrigger>?)null,
                resetPreviewTriggers: false, deferAbilityCallbacks: deferAbilityCallbacks, refreshPyre: refreshPyre).Equipment(targetId, cardId, definitions, remove, grafted);
            return new RoomCombatResult(result.State, result.Outcome, result.Rounds, result.Events.ToList(), result.UnsupportedReason, callbacks);
        }

        public static RoomCombatResult ApplyCardHeal(RoomCombatState state, int targetId, int amount, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null,
            bool captureAllDispatches = false)
        {
            string? error = Validate(state, linkedHealthOnly: true, halfHealthLifecycle: true);
            CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (error != null || amount < 0 || target?.Modifiers == null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                    error ?? "Card healing requires a valid amount, target and healability state.");
            // Card hover resets copied actors once; later healing retains callback flags.
            return new Engine(state, new List<CombatEvent>(),
                resetPreviewTriggers: state.Context?.Enchantments?.TemporaryPreviewUnitIds == null,
                refreshPyre: refreshPyre).TraceAllDispatches(captureAllDispatches).CardHeal(targetId, amount);
        }

        internal static RoomCombatResult ApplyMaximumHealthLifecycle(RoomCombatState state, int targetId, int amount, bool debuff,
            bool heal, bool triggerOnHeal, bool captureAllDispatches, Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre) =>
            new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false, refreshPyre: refreshPyre)
                .TraceAllDispatches(captureAllDispatches).MaximumHealthLifecycle(targetId, amount, debuff, heal, triggerOnHeal);

        internal static RoomCombatResult DrainCharacterCallbacks(RoomCombatState state, IReadOnlyList<QueuedCharacterTrigger> callbacks,
            bool captureAllDispatches = false)
        {
            string? error = Validate(state, halfHealthLifecycle: true);
            if (error != null) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            return new Engine(state, new List<CombatEvent>(), resetPreviewTriggers: false)
                .TraceAllDispatches(captureAllDispatches).DrainCallbacks(callbacks);
        }

        private static RoomCombatResult Run(RoomCombatState state, bool entireRoom,
            Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre = null, bool updateSelectedRoom = true,
            CombatUnit? externalPyre = null, int externalPyreRoomIndex = -1,
            IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null,
            IReadOnlyCollection<int>? pendingPhysicalDeathIds = null, Func<int, int, RelentlessDecision?>? chooseRelentless = null,
            UnitCopyCatalog? unitCopyCatalog = null, IReadOnlyList<RoomPlayRule>? roomRules = null)
        {
            var events = new List<CombatEvent>();
            string? error = Validate(state, allowFirstHivemindCombat: true);
            if (error != null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events, error);
            if (state.Units.Any(unit => unit.Status("lifelink") != null) && !TrueFinalBossCombatModel.Supports(state))
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events,
                    "True-final-boss room attack scheduling remains uncalibrated; linked damage/heal APIs are separate.");
            var engine = new Engine(state, events, refreshPyre: refreshPyre,
                externalPyre: externalPyre, externalPyreRoomIndex: externalPyreRoomIndex, externalTargets: externalTargets,
                chooseRelentless: chooseRelentless, unitCopyCatalog: unitCopyCatalog, roomRules: roomRules);
            return engine.Run(entireRoom, updateSelectedRoom, pendingPhysicalDeathIds);
        }

        private static CombatContext ClearFinishedRemovalPoints(CombatContext context, IReadOnlyCollection<int> completed)
        {
            if (context.SpawnPoints is not BattleSpawnPoints points || completed.Count == 0) return context;
            return context.WithSpawnPoints(points.WithState(points.Groups, points.Units.Select(point =>
                completed.Contains(point.UnitId) && point.Current == null
                    ? new UnitSpawnPointState(point.UnitId, null, null, point.OuterBoss, point.SpawnedInPreview) : point).ToArray()));
        }

        internal static string? Validate(RoomCombatState state, int? dyingTargetId = null, bool linkedHealthOnly = false,
            bool halfHealthLifecycle = true, bool allowFreshHivemind = false, bool allowFirstHivemindCombat = false,
            bool allowInfestedTerminalAscension = false)
        {
            // Real direct and battle queue scheduling share native callbacks.
            // Shared aura worlds keep their independent guard. Captured card
            // hover copies additionally calibrate lethal half-health lifecycle.
            halfHealthLifecycle &= state.Context?.Enchantments == null || state.Preview &&
                state.Context?.Enchantments?.TemporaryPreviewUnitIds != null;
            bool linkedCombat = TrueFinalBossCombatModel.Supports(state, maximumPreview: linkedHealthOnly);
            linkedHealthOnly |= linkedCombat;
            string? relicError = RelicModel.Validate(state.Context);
            if (relicError != null) return relicError;
            foreach (CombatUnit actor in state.Units)
                if (RelicLateAttackModel.Validate(actor) is string lateError) return lateError;
            if (state.Context?.Enchantments?.AutomaticLifecycle == false && state.Context.Enchantments.Rooms.SelectMany(room => room.Units).Any(unit =>
                unit.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Enchantment != null))) == true)
                return "CardEffectEnchant in the shared train requires automatic birth/movement/death lifecycle integration.";
            string? identityError = UnitIdentityModel.Validate(state.Context, state.Units);
            if (identityError != null) return identityError;
            if (state.Context != null && AbilityCardModel.Validate(state.Context) is string cacheError) return cacheError;
            bool linkedTargetOnly = linkedHealthOnly && !state.Preview && state.Context?.LifeLinks is LifeLinkContext linked &&
                !linked.CalculatingPreview && linked.SharedPreviewHpChange == 0 && LifeLinkModel.Validate(linked) == null &&
                state.Units.Count > 0 && state.Units.All(unit => unit.Status("lifelink") != null &&
                    linked.Actors.Any(actor => actor.Id == unit.Id && actor.TrueFinalBoss && actor.OuterBoss && actor.Health == unit.Health));
            bool linkedMaximumPreview = linkedHealthOnly && linkedCombat && state.Preview && state.Context?.LifeLinks?.TemporaryPreviewIds != null;
            bool observedHivemindCombat = ((state.Preview || allowFirstHivemindCombat) &&
                (HivemindTurnUpgrade.IsFirstCombat(state) || HivemindTurnUpgrade.IsSecondCombat(state) ||
                 HivemindTurnUpgrade.IsThirdCombat(state) || HivemindTurnUpgrade.IsObservedPyreCombat(state)));
            bool infestedWaveCombat = HivemindTurnUpgrade.IsInfestedOpeningCombat(state);
            bool infestedCombat = allowInfestedTerminalAscension || HivemindTurnUpgrade.IsInfestedTerminalPyre(state) ||
                HivemindTurnUpgrade.IsInfestedOrdinaryRoom(state);
            bool scaledWavePhase = allowFreshHivemind || state.Context?.QueryFrame?.Turn is 2 or 3 ||
                (!state.Preview && state.Context?.QueryFrame?.Turn == 1) ||
                (state.Context?.QueryFrame?.Turn is >= 4 and <= 7 &&
                    (state.Preview || state.Context.EnergyState?.Phase is "HeroTurn" or "MonsterTurn" or "Combat" or "EndMonsterTurn"));
            int infestedDrawTurn = allowFreshHivemind && state.Context?.QueryFrame?.Turn is 2 or 3 &&
                OuterBossDefinitions.HasLifemotherInfestedOwner(state.Context) ? state.Context.QueryFrame.Turn.Value : 0;
            bool infestedBossCombat = state.RoomIndex == 2 && state.Units.Any(unit =>
                unit.Triggers.Count >= 2 &&
                (OuterBossDefinitions.HasDormantLifemotherInfestedDeathGrant(unit, unit.Triggers[0], state.Context) ||
                 allowFirstHivemindCombat && HivemindTurnUpgrade.IsInfestedFirstBossMarkerOwner(state, unit)));
            string[] interactions = state.ExternalInteractions.Where(item =>
                !state.Units.Any(unit => HivemindTurnUpgrade.Marker(unit) == item &&
                    HivemindTurnUpgrade.IsObservedSproutCarrier(state, unit)) &&
                !((linkedTargetOnly || linkedMaximumPreview || infestedBossCombat) && item == "Outer train boss") &&
                !((allowFreshHivemind || observedHivemindCombat) && state.Units.Any(unit => HivemindTurnUpgrade.MatchesRoomMarker(item, unit))) &&
                !(infestedWaveCombat && state.Units.Any(unit => HivemindTurnUpgrade.Marker(unit) == item &&
                    HivemindTurnUpgrade.IsInfestedWaveCombatOwner(unit, state.Context))) &&
                !(infestedCombat && state.Units.Any(unit => HivemindTurnUpgrade.Marker(unit) == item &&
                    HivemindTurnUpgrade.IsInfestedCombatOwner(state, unit))) &&
                !(infestedDrawTurn > 0 && state.Units.Any(unit => HivemindTurnUpgrade.Marker(unit) == item &&
                    HivemindTurnUpgrade.IsInfestedDrawOwner(unit, infestedDrawTurn, afterRemoval: false, state.Context))) &&
                !(scaledWavePhase && state.Units.Any(unit =>
                    HivemindTurnUpgrade.Marker(unit) == item && HivemindTurnUpgrade.IsScaledRoomMarkerOwner(state, unit)))).ToArray();
            if (interactions.Length > 0) return string.Join("; ", interactions);
            foreach (CardInstanceState instance in state.Context?.CardInstances ?? Array.Empty<CardInstanceState>())
            {
                string? unsupported = CardModifierModel.UnsupportedReason(instance);
                if (unsupported != null && !RelicCardGenerationModel.CanDeferUnplayedGraftedCard(state.Context, instance, state.Units) &&
                    !CardCopyOnPlayModel.CanDeferUnplayedDrawCard(state.Context!, instance, state.Units, unsupported) &&
                    !(state.Units.All(unit => unit.SpawnerCardId != instance.InstanceId) &&
                      (CardCopyOnPlayModel.CanDeferDrawNotification(state.Context!, instance, unsupported) ||
                       DeckSpellUpgradeModel.CanDeferUnplayedTitanCurse(state.Context!, instance))))
                    return unsupported;
            }
            if (state.Units.Select(unit => unit.Id).Distinct().Count() != state.Units.Count)
                return "Unit instance IDs must be unique.";
            bool requireStatusDamage = state.Context?.Relics?.Any(relic => relic.StatusMagnitudes?.Count > 0 ||
                relic.StatusDecays?.Any(rule => rule.StatusIds.Contains("buff")) == true) == true;
            foreach (CombatUnit unit in state.Units)
            {
                bool infestedDrawOwner = infestedDrawTurn > 0 &&
                    HivemindTurnUpgrade.IsInfestedDrawOwner(unit, infestedDrawTurn, afterRemoval: true, state.Context);
                bool infestedCombatOwner = infestedCombat && HivemindTurnUpgrade.IsInfestedCombatOwner(state, unit);
                bool scaledWaveOwner = scaledWavePhase &&
                    HivemindTurnUpgrade.IsScaledFirstWaveOwner(unit, state.Context) || !state.Preview &&
                    HivemindTurnUpgrade.IsScaledFirstWaveBirth(unit, state.Context) ||
                    HivemindTurnUpgrade.IsScaledCarriedFeedOwner(unit, state.Context) ||
                    HivemindTurnUpgrade.IsScaledPreviewFeedRecipient(state, unit) ||
                    HivemindTurnUpgrade.IsScaledBossFeedOwner(state, unit, replacingCache: allowFreshHivemind);
                string? selfAttackError = UnitSelfAttackModel.Validate(state, unit);
                string? roomResourceError = UnitRoomResourceModel.Validate(unit);
                if (roomResourceError != null) return roomResourceError;
                string? roomStatusError = UnitRoomStatusModel.Validate(unit);
                if (roomStatusError != null) return roomStatusError;
                string? roomTriggerCountError = UnitRoomTriggerCountModel.Validate(unit);
                if (roomTriggerCountError != null) return roomTriggerCountError;
                if (unit.Modifiers?.SpawnCopySubtype is not (null or "SubtypesData_Snack"))
                    return "Uncalibrated unit birth copy subtype.";
                if (unit.Modifiers?.RoomTriggerCounts?.Count > 0 && state.Context?.TriggerCounts == null)
                    return "Unit room trigger counts require a captured native trigger-count cache.";
                if (selfAttackError != null) return selfAttackError;
                if (unit.RegisteredStatus("conduit") != null && !ConduitModel.Captured(state.Context))
                    return "Conduit requires captured native room/card tracker state.";
                if (unit.Status("mageblade")?.Stacks > 0 && unit.Modifiers?.MagebladeDamagePerMagicPower == null)
                    return "Missing native Mageblade cached attack coefficient.";
                if (unit.Modifiers?.MagebladeDamagePerMagicPower.HasValue == true &&
                    state.Context?.MagicPower?.Any(power => power.RoomIndex == state.RoomIndex && power.Team == unit.Team) != true)
                    return "Mageblade requires captured room magic power for this team.";
                if (requireStatusDamage && unit.Modifiers?.DamageBuffFromStatusEffects.HasValue != true)
                    return "Missing native status attack accumulator.";
                if (unit.RegisteredStatus("zeal") != null && unit.Modifiers?.DamageBuffFromStatusEffects.HasValue != true)
                    return "Zeal requires the native status attack accumulator.";
                if (unit.RegisteredStatus(StatusAttackDamageModel.Malaise) is CombatStatus malaise &&
                    StatusAttackDamageModel.ValidateMalaise(unit, malaise, state.Context) is string malaiseError)
                    return malaiseError;
                if (unit.RegisteredStatus(Witherbloom) is CombatStatus witherbloom && !MatchesWitherbloom(witherbloom))
                    return "Witherbloom requires the captured native post-combat status definition.";
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
                if (unit.Health < 0 || unit.Health == 0 && unit.Id != dyingTargetId ||
                    unit.Health > unit.MaxHealth && !RelicDeckPyreStatsModel.AllowsHealthAboveCharacterMaximum(state.Context, unit))
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
                    if (!KnownStatuses.Contains(status.Id) && !EquipmentPoolGrantModel.CosmeticStatus(status) &&
                        !(status.Id == HivemindTurnUpgrade.KillerStatusId &&
                            HivemindTurnUpgrade.IsObservedSproutCarrier(state, unit, replacingUpgrade: allowFreshHivemind)) &&
                        !((allowFreshHivemind && status.Id == HivemindTurnUpgrade.StatusId &&
                            (HivemindTurnUpgrade.IsWaitingSecondWaveOwner(unit, status) ||
                             HivemindTurnUpgrade.IsInfestedOpeningOwner(unit, status))) ||
                            ((observedHivemindCombat || infestedDrawOwner || infestedCombatOwner || scaledWaveOwner ||
                                infestedWaveCombat && HivemindTurnUpgrade.IsInfestedWaveCombatOwner(unit, state.Context) ||
                                infestedBossCombat && allowFirstHivemindCombat && HivemindTurnUpgrade.IsInfestedFirstBossMarkerOwner(state, unit)) &&
                                status.Id == HivemindTurnUpgrade.StatusId)) && !(status.Id == "pyrebane" &&
                        OuterBossDefinitions.IsAngelDuoInner(unit.AssetKey) && unit.Status("lifelink") == null) && !(linkedHealthOnly &&
                        (status.Id == "lifelink" || status.Id == "pyrebound" && unit.AssetKey == "Boss_Level9_Dominion_V2" ||
                            status.Id == "pyrebane" && unit.AssetKey == "Boss_Level9_Savagery_V2") &&
                        state.Context?.LifeLinks is LifeLinkContext world && (linkedCombat || !state.Preview && !world.CalculatingPreview) &&
                        (linkedCombat || world.SharedPreviewHpChange == 0) && LifeLinkModel.Validate(world) == null &&
                        world.Actors.Any(actor => actor.Id == unit.Id && actor.TrueFinalBoss && actor.Initialized && actor.HasLifeLink && actor.Health == unit.Health)))
                        return "Unmodeled status " + status.Id + " on " + unit.AssetKey;
                if (unit.Status("titanite") != null && unit.Status("damage shield") != null &&
                    (unit.StatusRegistry == null || unit.Statuses.Count > 66 || unit.Statuses.Count > 16 &&
                        unit.Statuses.Any(status => !KnownStatuses.Contains(status.Id) || status.Id is "heal multiplier" or "inedible")))
                    return "Titanite with Damage Shield requires captured status order and a calibrated native status list (at most 66 active entries).";
                if (!state.Deployment && unit.Status("valor") != null &&
                    state.Context?.StatusRules.FirstOrDefault(rule => rule.Id == "armor") == null)
                    return "Valor requires the armor status definition.";
                if (unit.AssetKey is TriggeredStatusModel.OriginalSpurnedWitherbloom or TriggeredStatusModel.OriginalSpurnedDuality)
                {
                    CombatTrigger[] original = unit.Triggers.Where(trigger =>
                        string.IsNullOrEmpty(trigger.Origin?.UpgradeId) && trigger.Origin?.IsFromEquipment != true).ToArray();
                    bool nativeRoster = unit.BumpRules is { OuterBoss: true, OuterBossRelentlessPhase: true }
                        ? original.Length == 0
                        : original.Length == 2 && TriggeredStatusModel.MatchesSpurnedTurnBegin(unit, original[0]) &&
                            TriggeredStatusModel.MatchesSpurnedAttack(unit, original[1]);
                    if (!nativeRoster) return "Spurned requires its original triggers for the captured boss phase.";
                }
                foreach (CombatTrigger trigger in unit.Triggers)
                {
                    if (trigger.ActivationCondition != null && !(halfHealthLifecycle && HalfHealthDeathwishModel.IsTrigger(trigger)))
                        return "Uncalibrated conditional trigger activation: " + trigger.ActivationCondition;
                    if (HalfHealthDeathwishModel.IsTrigger(trigger) && (!trigger.StateId.HasValue || !trigger.DeathwishActive.HasValue))
                        return "Half-health Deathwish requires captured source membership and persistent trigger identity.";
                    if (trigger.Conditions != null && trigger.Conditions.RequiredStatuses.Concat(trigger.Conditions.RequiredDyingStatuses).Any(string.IsNullOrWhiteSpace))
                        return "Invalid trigger status condition.";
                    if (trigger.Origin?.OnlyIfEquipped == true && unit.EquipmentCards == null)
                        return "Equipped-only triggers require equipment membership state.";
                    if (trigger.Origin != null && (trigger.Origin.EquipmentCardId < 0 ||
                        trigger.Origin.IsFromEquipment != (trigger.Origin.EquipmentCardId > 0)))
                        return "Invalid equipment trigger origin.";
                    if (trigger.Kind != "OnDeath" && trigger.Kind != "OnReanimated" && trigger.Kind != "OnBurnout" && trigger.Kind != "PostCombat" && trigger.Kind != "PostCombatHealing" && trigger.Kind != "OnEncounterComplete" && trigger.Kind != "OnHeal" &&
                        trigger.Kind != "CardMonsterPlayed" && trigger.Kind != "CardSpellPlayed" && trigger.Kind != "CardExhausted" && trigger.Kind != "OnOwnAbilityActivated" && trigger.Kind != "OnPreOwnAbilityActivated" &&
                        trigger.Kind != "OnEquipmentAdded" && trigger.Kind != "OnEquipmentAddedToAny" && trigger.Kind != "OnEquipmentRemoved" && trigger.Kind != "OnFoodSpawn" &&
                        trigger.Kind != "OnSpawn" && trigger.Kind != "OnUnscaledSpawn" && trigger.Kind != "OnSpawnNotFromCard" && trigger.Kind != "OnSpawnLate" && trigger.Kind != "AfterSpawnEnchant" &&
                        trigger.Kind != "OnTurnBegin" && trigger.Kind != "OnTeamTurnBegin" && trigger.Kind != "EndTurnPreHandDiscard" && trigger.Kind != "PreCombat" &&
                        trigger.Kind != "OnTrainRoomLoop" && trigger.Kind != "PostAscension" && trigger.Kind != "OnShift" && trigger.Kind != "OnSentry" &&
                        trigger.Kind != "PostDescension" && trigger.Kind != "PostAttemptedAscension" && trigger.Kind != "PostAttemptedDescension" &&
                        trigger.Kind != "OnHit" && trigger.Kind != "OnKill" && trigger.Kind != "OnBloodshed" && trigger.Kind != "OnPunish" && trigger.Kind != "OnAttackingBeforeDamage" && trigger.Kind != "OnAttacking" && trigger.Kind != "OnEaten" && trigger.Kind != "OnFeed" &&
                        !StatusCallbackModel.Kinds.Contains(trigger.Kind) && !DeathwishModel.CallbackKinds.Contains(trigger.Kind) &&
                        !HarvestModel.Kinds.Contains(trigger.Kind) && trigger.Kind is not ("OnRemoveHatch" or "OnHatched" or "CorruptionAdded") &&
                        trigger.Kind is not ("OnMoonLit" or "OnMoonlitLost" or "OnMoonShade" or "OnMoonshadeLost" or "OnMoonPhaseShift"))
                        return "Unmodeled trigger " + trigger.Kind;
                    if (trigger.Kind is "OnBloodshed" or "OnPunish" && state.Context?.Statistics?.MonsterDamage == null)
                        return trigger.Kind + " requires the game 2.3.1 damage lifecycle snapshot.";
                    if (trigger.Kind != "OnDeath" && trigger.Kind != "PostCombat" && trigger.SkipDuringDeployment == null)
                        return trigger.Kind + " requires deployment timing state.";
                    if (trigger.TriggerAtThreshold > 0 && trigger.Kind != "OnDeath" && trigger.Kind != "OnHit" && trigger.Kind != "OnKill" && trigger.Kind != "OnBloodshed" && trigger.Kind != "OnPunish" &&
                        trigger.Kind != "OnAttackingBeforeDamage" && trigger.Kind != "OnAttacking" && trigger.Kind != "OnSentry" && trigger.Kind != "CardSpellPlayed" && !StatusCallbackModel.Kinds.Contains(trigger.Kind))
                        return "Threshold arguments are not modeled for " + trigger.Kind;
                    if (trigger.Kind == "OnNewStatusEffectAdded" && !unit.CountUniqueVisibleStatuses().HasValue)
                        return "New-status callbacks require the complete native status dictionary and display definitions.";
                    if (trigger.FireCount < 0 && state.Context?.TriggerCounts == null) return "Invalid trigger fire count.";
                    TriggerCountModel.Query(state.Context, unit, trigger, out string? countError,
                        UnitRoomTriggerCountModel.Modifier(state.Units, unit.Team, trigger.Kind));
                    if (countError != null) return countError;
                    foreach (CombatEffect effect in trigger.Effects)
                    {
                        CardEffectTests? devotionTests = effect.Action?.Tests ?? effect.UnitUpgrade?.Tests ?? effect.Summon?.Tests ?? effect.Enchantment?.Targeting.Tests;
                        if (DevotionModel.HasRequirements(devotionTests))
                        {
                            return "Devotion-gated character triggers require separate native lifecycle calibration.";
                        }
                        if (effect.Type == "CardEffectForceRelentless")
                        {
                            if (trigger.Kind != "OnDeath" || effect.Value != 1 || effect.Action != null ||
                                unit.BumpRules?.CompanionBoss != true || !OuterBossDefinitions.IsAngelDuoActor(unit.AssetKey))
                                return "Uncalibrated forced-relentless trigger definition.";
                            continue;
                        }
                        if (effect.Type == UnitCardCopyModel.Effect)
                        {
                            string? copyError = UnitCardCopyModel.Validate(effect);
                            if (copyError != null) return copyError;
                            continue;
                        }
                        if (effect.Type == "CardEffectPermanentlyAdjustMaxPyreHealth")
                        {
                            if (trigger.Kind != "OnEncounterComplete" || effect.Action?.Type != "AdjustPyreMaxHealth" ||
                                effect.Action.Target != "Self" || effect.Action.Value != effect.Value || effect.Value <= 0 ||
                                effect.Action.Range != null || effect.Action.Statuses.Count != 0 || state.Context?.Resources == null)
                                return "Permanent Pyre health requires a positive fixed encounter-complete adjustment and captured resources.";
                            continue;
                        }
                        if (effect.Type == RelicResourceModel.PermanentPyreAttackAdjustment)
                        {
                            CardActionEffect? adjustment = effect.Action;
                            if (!unit.IsPyre || trigger.Kind != "OnKill" || state.Context?.Resources == null ||
                                adjustment?.Type != "AdjustPyreAttack" || adjustment.Target != "Self" ||
                                adjustment.Value != effect.Value || adjustment.Range != null || adjustment.Statuses.Count != 0)
                                return "Permanent Pyre attack adjustment requires a live, non-preview Pyre OnKill with captured resources.";
                            continue;
                        }
                        if (effect.Type is "CardEffectEnchant" or "CardEffectEnchantCardUpgrade")
                        {
                            if (state.Context?.Enchantments?.AutomaticLifecycle != true || effect.Enchantment == null)
                                return "CardEffectEnchant requires a captured automatic lifecycle world and effect definition.";
                            continue;
                        }
                        if (effect.Type is "CardEffectSpawnMonster" or "CardEffectSpawnHero" or TriggeredSummonModel.SpawnHeroIfAlone)
                        {
                            string? summonError = TriggeredSummonModel.Validate(state, effect.Summon, unit, trigger.Kind, effect.Type);
                            if (summonError != null) return summonError;
                            continue;
                        }
                        if (effect.Type == "CardEffectUnboxJackInTheBoxUnitCard")
                        {
                            if (state.Context?.SummonCatalog == null || state.Context.SpawnPoints == null ||
                                unit.SpawnerCardId > 0 && state.Context.FindCard(unit.SpawnerCardId)?.Permanent.BoxedCards == null)
                                return "Jack-in-the-Box requires captured boxed templates, summon definitions and physical slots.";
                            continue;
                        }
                        if (effect.Type == "CardEffectSpawnMonsterOrAddStatusEffect")
                        {
                            CardActionEffect? action = effect.Action;
                            string? filterError = action?.Filters?.Validate();
                            if (action?.Type != "SpawnOrStatus" || action.Target != "Room" || action.AllowEnemy ||
                                !action.AllowPlayer || action.Statuses.Count == 0 || action.SpawnFallbackUnit == null ||
                                action.Range != null || filterError != null)
                                return filterError ?? "Unsupported triggered spawn-or-status definition.";
                            if (action.Statuses.Any(status => !KnownStatuses.Contains(status.Id)))
                                return "Unmodeled triggered spawn-or-status status.";
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
                        if (effect.Type == "CardEffectAddTempCardUpgradeToSpellsInDeck")
                        {
                            if (state.Context?.CardInstances == null || state.Context.CardRegistry == null || state.Context.OtherPiles == null ||
                                effect.Action is not { Type: "UpgradeDeckSpells", Upgrade: not null, Lifetime: "TemporaryUntilEndOfBattle" } deckUpgrade)
                                return "Triggered deck spell upgrades require complete card ownership and an upgrade definition.";
                            if (deckUpgrade.Upgrade.ExternalInteractions.Count > 0) return string.Join("; ", deckUpgrade.Upgrade.ExternalInteractions);
                            continue;
                        }
                        if (effect.Type == "CardEffectRecursion")
                        {
                            if (state.Context?.CardRegistry == null || state.Context.CardInstances == null ||
                                effect.Action is not { Type: "Recursion", Target: "Exhaust", Value: 1, Range: null,
                                    Recursion: { SelectionMode: "RandomToHand", CardType: "Monster" or "Spell", RequiresRoomInHand: false } } recursion)
                                return "Triggered recursion requires its captured original random card-to-hand payload and source card.";
                            string? recursionError = CardRecursionModel.Validate(recursion, state.Context.OtherPiles, state.Context.Cards);
                            if (recursionError != null) return recursionError;
                            continue;
                        }
                        if (effect.Type == "CardEffectAddTempCardUpgradeToNextDrawnCard")
                        {
                            if (state.Context?.Cards.NextDrawUpgrades == null || state.Context.Cards.NextUpgradeReferenceId == null ||
                                effect.Action is not { Type: "ScheduleNextDrawUpgrade", NextDrawUpgrade: not null, RequiredGold: 0 } nextDraw)
                                return "Triggered next-draw upgrades require the captured queue, reference allocation and a zero-cost payload.";
                            if (nextDraw.NextDrawUpgrade.Upgrade.ExternalInteractions.Count > 0)
                                return string.Join("; ", nextDraw.NextDrawUpgrade.Upgrade.ExternalInteractions);
                            continue;
                        }
                        if (effect.Type == "CardEffectBump")
                        {
                            if (state.Context?.SpawnPoints == null ||
                                !HarvesterBirthModel.IsTrigger(unit, trigger) && !IsRailforgedAttackerKillBump(unit, trigger))
                                return "Triggered Bump requires the original Harvester tower birth payload and physical positions.";
                            continue;
                        }
                        if (effect.Type == "CardEffectFloorRearrange")
                        {
                            bool self = effect.Action is { Type: "FloorRearrange", Target: "Self", Value: 0 or 1, Range: null };
                            bool arkion = OuterBossDefinitions.IsArkionPush(unit.AssetKey) && trigger.Kind == "OnAttackingBeforeDamage" &&
                                effect.Action is { Type: "FloorRearrange", Target: "LastAttackedCharacter", Value: 1, Range: null,
                                    AllowEnemy: true, AllowPlayer: true, UseCardTraits: false } && effect.Action.Statuses.Count == 0;
                            if (state.Context?.SpawnPoints == null || !self && !arkion &&
                                !OuterBossDefinitions.IsUnrulySniperAdvance(unit, trigger, effect) && !HarvesterBirthModel.IsTrigger(unit, trigger))
                                return "Triggered rearrangement requires a calibrated original payload and physical positions.";
                            continue;
                        }
                        if (effect.Type == "CardEffectDrawType")
                        {
                            string? drawError = TriggeredTypedDrawModel.Validate(state.Context, effect);
                            if (drawError != null) return drawError;
                            continue;
                        }
                        if (EnergyModel.IsNativeEffect(effect.Type) || effect.Type == "CardEffectDrawAdditionalNextTurn" || effect.Type == "CardEffectAdjustRoomCapacity" || effect.Type == "CardEffectAdjustForgePoints" || effect.Type == "CardEffectGainDevotionPoints")
                        {
                            CardActionEffect? energy = effect.Action;
                            bool typeMatches = effect.Type == "CardEffectAdjustRoomCapacity" ? energy?.Type == "AdjustCapacity" :
                                effect.Type == "CardEffectDrawAdditionalNextTurn" ? energy?.Type == "DrawNextTurn" :
                                effect.Type == "CardEffectAdjustForgePoints" ? energy?.Type == ForgePointsModel.Effect :
                                effect.Type == "CardEffectGainDevotionPoints" ? energy?.Type == DevotionModel.Effect && energy.DevotionPerLevel.HasValue :
                                energy != null && EnergyModel.IsEffect(energy.Type) &&
                                    effect.Type == "CardEffect" + (energy.Type == "GainEnergyMonsterTurn" ? "GainEnergy" : energy.Type);
                            if (energy == null || !typeMatches)
                                return "Missing or mismatched triggered resource definition.";
                            string? energyError = energy.Type == "AdjustCapacity" ? RoomCapacityModel.Validate(state.Context) : energy.Type == "DrawNextTurn" ? state.Context?.Cards.BonusDraw == null ? "Missing bonus-draw state." :
                                BonusDrawModel.Validate(state.Context.Cards) : energy.Type == ForgePointsModel.Effect ? ForgePointsModel.Validate(state.Context) :
                                energy.Type == DevotionModel.Effect ? DevotionModel.Validate(state.Context) : EnergyModel.Validate(state.Context);
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
                            string? transferError = TriggeredStatusTransferModel.IsLifemotherDeath(unit, trigger.Kind, effect.Action)
                                ? null : TriggeredStatusTransferModel.Validate(effect.Action);
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
                        if (effect.Type == "CardEffectDamagePerHp")
                        {
                            CardActionEffect? retaliation = effect.Action;
                            if (retaliation?.Type != "DamagePerHp" || retaliation.Target != "Self" ||
                                !retaliation.DamagePerHpLastAttacker || retaliation.Range != null || unit.LastAttackerId == null)
                                return "Triggered damage-per-HP requires fixed self HP and a captured last attacker.";
                            continue;
                        }
                        if (effect.Type == "CardEffectDamagePerSourceAttack")
                        {
                            if (effect.Action is not { Type: "DamagePerSourceAttack", Target: "FrontInRoom", Value: 1, Range: null,
                                AllowEnemy: true, AllowPlayer: false, UseCardTraits: false } attackDamage)
                                return "Triggered source-attack damage requires its calibrated original front-enemy payload.";
                            string? filterError = attackDamage.Filters?.Validate();
                            if (filterError != null) return filterError;
                            continue;
                        }
                        if (effect.Type == "CardEffectPlayUnitTrigger")
                        {
                            CardActionEffect? replay = effect.Action;
                            if (replay is not { Type: "PlayUnitTrigger", UnitTriggerKind: "OnShift", Target: "Self", Range: null } &&
                                !OuterBossDefinitions.IsIndulgentCapacityFeed(unit, trigger) &&
                                !OuterBossDefinitions.IsIndulgentSlayFeed(unit, trigger))
                                return "Triggered unit replay requires its calibrated native self definition.";
                            continue;
                        }
                        if (effect.Type == "CardEffectAdjustCorruption")
                        {
                            CardActionEffect? corruption = effect.Action;
                            if (corruption?.Type != "AdjustCorruption" || corruption.Target is not ("Room" or "Tower"))
                                return "Triggered corruption requires its native room or tower definition.";
                            string? corruptionError = RoomCorruptionModel.Validate(state.Context, corruption);
                            if (corruptionError != null) return corruptionError;
                            continue;
                        }
                        if (effect.Type == "CardEffectAdjustAllStatusEffects")
                        {
                            CardActionEffect? adjustment = effect.Action;
                            bool roomSlay = trigger.Kind == "OnKill" && adjustment is { Target: "Room", Value: 1 or 2,
                                AllowPlayer: true, AllowEnemy: false, StatusAdjustmentFilter: 1 } && adjustment.Statuses.Count == 0 &&
                                adjustment.Tests?.StrictTargets == true;
                            if (adjustment?.Type != "AdjustAllStatusEffects" || !roomSlay && adjustment.Target != "Self" || adjustment.Range != null ||
                                adjustment.StatusAdjustmentFilter is not (0 or 1) || !roomSlay && adjustment.Tests?.StrictTargets != false ||
                                adjustment.Statuses.Any(status => !KnowsStatus(status.Id)) || adjustment.Value == int.MinValue)
                                return "Unsupported triggered all-status adjustment definition.";
                            continue;
                        }
                        if (effect.Type == "CardEffectRemoveAllStatusEffects")
                        {
                            CardActionEffect? removal = effect.Action;
                            if (removal?.Type != "RemoveAllStatusEffects" || removal.Target != "Self" ||
                                !removal.AllowPlayer || removal.AllowEnemy || removal.Range != null)
                                return "Unsupported triggered remove-all status effect definition.";
                            string? removalError = StatusRemovalModel.ValidateAll(removal);
                            if (removalError != null) return removalError;
                            continue;
                        }
                        if (TriggeredHealthModel.IsEffect(effect.Type))
                        {
                            string? healthError = TriggeredHealthModel.Validate(effect);
                            if (healthError != null) return healthError;
                            if (state.Context == null) return "Triggered maximum health requires shared battle context.";
                            continue;
                        }
                        if (effect.Type == "CardEffectHealTrain")
                        {
                            if (effect.Action?.Type != "HealTrain" || effect.Action.Target != "Pyre" ||
                                effect.Action.Range != null || effect.Action.UseCardTraits || state.Context?.Resources?.TowerHealingDisabled == null)
                                return "Triggered train healing requires fixed Pyre targeting and captured healing rules without source-trait scaling.";
                            continue;
                        }
                        if (effect.Type is "CardEffectBuffDamage" or "CardEffectDebuffDamage")
                        {
                            // Native enemy Harvest/Incant has no source card traits.
                            // Keep other target modes and parent-card scaling guarded.
                            bool duoSlay = (unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" && trigger.Kind == "OnKill" &&
                                effect.Value == 3 && effect.Action is { Target: "Tower", AllowPlayer: true, AllowEnemy: true } &&
                                effect.Action.Filters?.Subtype == "SubtypesData_EntropicAngel") ||
                                OuterBossDefinitions.CalibratedAngelDuoTrialGrowth(unit, state.Context, trigger.Kind, effect);
                            bool duoLoop = unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant1_LoopAttack" && trigger.Kind == "OnTrainRoomLoop" &&
                                effect.Value == 8 && effect.Action is { Target: "Tower", AllowPlayer: true, AllowEnemy: true } &&
                                effect.Action.Filters?.Subtype == "SubtypesData_EntropicAngel";
                            bool duoRevenge = unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack" && trigger.Kind == "OnHit" &&
                                effect.Value == 1 && effect.Action is { Target: "Tower", AllowPlayer: true, AllowEnemy: true } &&
                                effect.Action.Filters?.Subtype == "SubtypesData_EntropicAngel";
                            bool debuff = effect.Type == "CardEffectDebuffDamage";
                            if (debuff && (unit.AssetKey != "HeavyT1_BigAttackUpFront" || trigger.Kind is not ("OnDeathwishLost" or "OnVanguardLost") ||
                                effect.Action?.Target != "Self"))
                                return "Triggered attack reduction requires the original self DeathwishLost enemy signature.";
                            if (unit.Team != CombatTeam.Enemy || unit.SpawnerCardId != 0 || unit.Modifiers == null ||
                                effect.Action is not { Range: null } buff || buff.Type != (debuff ? "DebuffAttack" : "BuffAttack") ||
                                buff.Target is not ("Self" or "Room" or "Tower") || buff.Value != effect.Value || buff.UseCardTraits ||
                                buff.Target != "Self" && (!buff.AllowEnemy || buff.AllowPlayer && !duoSlay && !duoLoop && !duoRevenge))
                                return "Triggered attack buffs require a captured fixed enemy self/room/tower buff without parent-card traits.";
                            string? buffFilter = buff.Target == "Self" ? null : buff.Filters?.Validate();
                            if (buffFilter != null) return buffFilter;
                            continue;
                        }
                        if (effect.Type == OwnerDamageReactionModel.NativeEffect)
                        {
                            if (trigger.Kind != "OnPunish" || unit.DamageHistory == null ||
                                effect.Action is not { Type: "Damage", Target: "FrontInRoom", Value: 4, AllowEnemy: true, AllowPlayer: false, Range: null } reaction ||
                                effect.Value != 4 || effect.DamageStatusMultiplier != null)
                                return "Owner-damage retaliation requires captured damage history and the original fixed Punish payload.";
                            string? reactionFilter = reaction.Filters?.Validate();
                            if (reactionFilter != null) return reactionFilter;
                            continue;
                        }
                        if (effect.Type == DevotionStatusModel.NativeEffect)
                        {
                            if (effect.Action == null || trigger.Kind != "OnSpawn" || state.Context?.QueryFrame?.Devotion == null)
                                return "Devotion status triggers require OnSpawn and the captured Devotion state.";
                            string? devotionStatusError = TriggeredStatusModel.Validate(effect);
                            if (devotionStatusError != null) return devotionStatusError;
                            continue;
                        }
                        if (effect.Type == TriggeredStatusModel.OtherStatusEffect)
                        {
                            if (state.Context == null || !TriggeredStatusModel.MatchesSpurnedAttack(unit, trigger))
                                return "Status-per-other triggers require the captured ordered Spurned attack payload.";
                            continue;
                        }
                        if (effect.Type == TriggeredStatusModel.HealthPercentStatusEffect)
                        {
                            if (state.Context == null || unit.AssetKey != TriggeredStatusModel.OriginalSpurnedDuality ||
                                !TriggeredStatusModel.MatchesSpurnedTurnBegin(unit, trigger))
                                return "Health-percent status triggers require the original Spurned turn-begin payload.";
                            continue;
                        }
                        if (effect.Type == EquipmentPoolGrantModel.NativeEffect)
                        {
                            if (!EquipmentPoolGrantModel.Captured(unit, trigger, effect, state.Context))
                                return "Equipment grant requires its original ordered pool, self-spawn and standby state.";
                            continue;
                        }
                        if (effect.Action != null && effect.Type != "CardEffectHeal" && effect.Type != "CardEffectDamage" && effect.Type != "CardEffectPyreDamageIfNoPlayerUnits" && effect.Type != "CardEffectAddStatusEffect" &&
                            effect.Type is not ("CardEffectAddStatsFromSelf" or "CardEffectRemoveEquipment" or "CardEffectAdjustDragonsHoardPerStatusEffect" or "CardEffectFeederRules" or "CardEffectSynergizeStatusEffects" or "CardEffectCopyUnits") &&
                            effect.Type is not ("CardEffectMultiplyStatusEffect" or "CardEffectAddStatusEffectIfNoPlayerUnits" or FriendlyDamageStatusModel.NativeEffect or "CardEffectAddStatusEffectPerSelfAttack" or "CardEffectAddStatusEffectPerLastAttackDamage" or "CardEffectAddStatusEffectBasedOnHealth"))
                            return "Mismatched triggered action definition.";
                        if (effect.DamageStatusMultiplier != null && effect.Type is not ("CardEffectDamage" or "CardEffectPyreDamageIfNoPlayerUnits"))
                            return "Mismatched triggered damage multiplier.";
                        if (effect.StatusScaling != null && effect.Type is not ("CardEffectAddStatusEffect" or "CardEffectAddStatusEffectIfNoPlayerUnits" or
                            "CardEffectMultiplyStatusEffect" or FriendlyDamageStatusModel.NativeEffect or "CardEffectAddStatusEffectPerSelfAttack" or "CardEffectAddStatusEffectPerLastAttackDamage" or "CardEffectAddStatusEffectBasedOnHealth")) return "Mismatched triggered status scaling.";
                        if (effect.Type == "CardEffectDespawnCharacter") continue;
                        if (effect.Type == "CardEffectHeal" || effect.Type == "CardEffectDamage" || effect.Type == "CardEffectPyreDamageIfNoPlayerUnits" ||
                            effect.Type is "CardEffectAddStatusEffect" or "CardEffectMultiplyStatusEffect" or "CardEffectAddStatusEffectIfNoPlayerUnits" or FriendlyDamageStatusModel.NativeEffect or "CardEffectAddStatusEffectPerSelfAttack" or "CardEffectAddStatusEffectPerLastAttackDamage" or "CardEffectAddStatusEffectBasedOnHealth")
                        {
                            CardActionEffect? action = effect.Action;
                            if (action?.Type != (effect.Type == "CardEffectHeal" ? "Heal" : effect.Type is "CardEffectDamage" or "CardEffectPyreDamageIfNoPlayerUnits" ? "Damage" : "AddStatus")) return "Missing triggered action definition.";
                            if (effect.Type == "CardEffectPyreDamageIfNoPlayerUnits" && action.Target != "Pyre")
                                return "Conditional Pyre damage requires a Pyre target.";
                            if (action.Type == "AddStatus")
                            {
                                // The opening checks retain these death triggers for later validation at dispatch.
                                bool dormantDeath = allowFreshHivemind && !state.Preview &&
                                    (HivemindTurnUpgrade.HasDormantOpeningDeathGrant(unit, trigger) ||
                                     OuterBossDefinitions.HasDormantLifemotherInfestedDeathGrant(unit, trigger, state.Context)) ||
                                    infestedWaveCombat && HivemindTurnUpgrade.IsInfestedOpeningPreviewOwner(unit, state.Context) && trigger.Kind == "OnDeath" ||
                                    infestedBossCombat && (OuterBossDefinitions.HasDormantLifemotherInfestedDeathGrant(unit, trigger, state.Context) ||
                                        allowFirstHivemindCombat && HivemindTurnUpgrade.IsInfestedFirstBossMarkerOwner(state, unit) &&
                                        ReferenceEquals(trigger, unit.Triggers[0])) ||
                                    (infestedDrawOwner || infestedCombatOwner) && trigger.Kind is "OnDeath" or "OnAnyUnitDeathOnFloor";
                                string? statusError = dormantDeath
                                    ? null : TriggeredStatusModel.Validate(effect);
                                if (statusError != null) return statusError;
                                if (state.Context == null) return "Triggered status requires shared battle context.";
                            }
                            if (action.Type == "Heal" && state.Units.Any(target => target.Modifiers == null))
                                return "Triggered healing requires unit healability state.";
                            // A direct HP operation never runs PostCombat. Keep
                            // Dominion's original dormant all-floor corruption
                            // rule without admitting it to combat scheduling.
                            bool deferredDominionPostCombat = linkedHealthOnly && trigger.Kind == "PostCombat" &&
                                unit.AssetKey == "Boss_Level9_Dominion_V2" && unit.Status("lifelink") != null &&
                                effect.Type == "CardEffectAddStatusEffect" && action.Target == "FrontInAllRooms" &&
                                action.Range == null && action.Statuses.Count == 1 &&
                                action.Statuses[0].Id == "corruption" && action.Statuses[0].Stacks == 5;
                            if (!deferredDominionPostCombat && !new[] { "Self", "Room", "Tower", "FrontInRoom", "BackInRoom", "InFrontOfSelf", "Weakest", "RoomHealTargets", "RandomInRoom", "LastAttackedCharacter", "LastAttackerCharacter", "LastSpawnedCharacter", "LastFeederCharacter", "Pyre" }.Contains(action.Target))
                                return "Unmodeled triggered action target " + action.Target;
                            if ((action.Range != null || action.Target == "RandomInRoom") && state.Context == null)
                                return "Triggered action randomness requires shared battle context.";
                            string? healError = (action.Target is "LastAttackedCharacter" or "LastAttackerCharacter" ? null : action.Filters?.Validate()) ?? action.Range?.Validate();
                            if (healError != null) return healError;
                            continue;
                        }
                        if (effect.Type == "CardEffectAddStatsFromSelf")
                        {
                            CardActionEffect? action = effect.Action;
                            if (action?.Type != "AddStatsFromSelf") return "Missing self-stat copy action definition.";
                            if (state.Context?.CardInstances == null || unit.Modifiers == null)
                                return "Self-stat copy requires shared battle and unit modifier state.";
                            if (action.Target != "Self" && !new[] { "Room", "FrontInRoom", "BackInRoom", "Weakest", "RoomHealTargets",
                                "RandomInRoom", "LastAttackedCharacter", "LastSpawnedCharacter", "LastFeederCharacter" }.Contains(action.Target))
                                return "Unmodeled self-stat copy target " + action.Target;
                            if (action.Range != null) return "Self-stat copy range initialization is not modeled.";
                            string? filterError = action.Target == "LastAttackedCharacter" ? null : action.Filters?.Validate();
                            if (filterError != null) return filterError;
                            continue;
                        }
                        if (effect.Type == "CardEffectCopyUnits")
                        {
                            if (unit.Team != CombatTeam.Enemy || unit.AssetKey != TriggeredSummonModel.UnrulyBoss || trigger.Kind != "PostCombat" ||
                                effect.Action is not { Type: "CopyUnits", Target: "FrontInRoom", AllowEnemy: true, AllowPlayer: false,
                                    Value: 1, CopyHeroStats: true, Range: null, Filters.IgnoreBosses: true })
                                return "Unsupported triggered hero-copy configuration.";
                            string? copyFilter = effect.Action.Filters.Validate();
                            if (copyFilter != null) return copyFilter;
                            continue;
                        }
                        if (effect.Type == "CardEffectAddCardUpgradeToUnits" || effect.Type == "CardEffectAddTempCardUpgradeToUnits" ||
                            effect.Type == "CardEffectRemoveTempUpgradeFromUnit" || effect.Type == "CardEffectBuffEquipment" ||
                            effect.Type is "CardEffectAddCardUpgradeToUnitsOnMoonPhase" or "CardEffectAddCardUpgradeToUnitsPerDragonsHoard" or "CardEffectAddCardUpgradeToUnitsPerStatusEffect" or DevotionModel.NativeUnitUpgrade or MissingHealthUnitUpgradeModel.NativeEffect)
                        {
                            CardActionEffect? action = effect.UnitUpgrade;
                            if (action?.Upgrade == null || action.Type != (effect.Type == MissingHealthUnitUpgradeModel.NativeEffect ? MissingHealthUnitUpgradeModel.Effect : effect.Type == DevotionModel.NativeUnitUpgrade ? DevotionModel.UnitUpgrade : effect.Type == "CardEffectAddCardUpgradeToUnitsOnMoonPhase" ? "UnitUpgradeOnMoonPhase" :
                                effect.Type == "CardEffectAddCardUpgradeToUnitsPerDragonsHoard" ? "UnitUpgradePerHoard" :
                                effect.Type == "CardEffectAddCardUpgradeToUnitsPerStatusEffect" ? "UnitUpgradePerStatus" : effect.Type == "CardEffectBuffEquipment" ? "BuffEquipment" : effect.Type == "CardEffectRemoveTempUpgradeFromUnit" ? "RemoveUnitUpgrade" : "UnitUpgrade"))
                                return "Missing triggered unit upgrade definition.";
                            if (action.Type == MissingHealthUnitUpgradeModel.Effect && MissingHealthUnitUpgradeModel.Validate(action) is string missingHealthError)
                                return missingHealthError;
                            if (action.Type == "UnitUpgradeOnMoonPhase" && (action.Value is < 0 or > 3 || state.Context?.QueryFrame?.MoonPhase is not (1 or 2)))
                                return "Moon-dependent unit upgrades require a captured phase and a calibrated mask.";
                            if (action.Type == DevotionModel.UnitUpgrade && state.Context?.QueryFrame?.Devotion == null)
                                return "Devotion-scaled unit upgrades require captured Devotion state.";
                            if (action.Type == "UnitUpgradePerHoard" && state.Context?.QueryFrame?.DragonsHoard == null)
                                return "Hoard-dependent unit upgrades require the current Hoard amount.";
                            if (action.Type == "UnitUpgradePerStatus" && (action.Target != "Self" || action.Statuses.Count == 0))
                                return "Triggered status-scaled upgrades require one self target and captured status identities.";
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
                            if (action.Target != "Self" && !new[] { "Room", "FrontInRoom", "BackInRoom", "Weakest", "RoomHealTargets", "RandomInRoom", "LastAttackedCharacter", "LastSpawnedCharacter", "LastFeederCharacter" }.Contains(action.Target))
                                return "Unmodeled triggered upgrade target " + action.Target;
                            if (action.Range != null) return "Triggered upgrade range initialization is not modeled.";
                            string? filterError = action.Target == "LastAttackedCharacter" ? null : action.Filters?.Validate();
                            if (filterError != null) return filterError;
                            continue;
                        }
                        if (effect.Type == "CardEffectPurgeUnitOnDeath")
                        {
                            if (state.Context?.OtherPiles == null) return "Unit-card purge requires captured native piles.";
                            continue;
                        }
                        if (effect.Type == "CardEffectSynergizeStatusEffects")
                        {
                            if (effect.Action?.Type != "SynergizeStatuses" || effect.Action.Target != "Self" ||
                                effect.Action.Statuses.Count != 2 || effect.Action.Statuses.Any(status => !KnowsStatus(status.Id)) ||
                                unit.Modifiers?.SynergizeStatuses == null)
                                return "Status synergy requires captured self targets, two status definitions and the native accumulated-stack cache.";
                            continue;
                        }
                        if (effect.Type == "CardEffectFeederRules")
                        {
                            if (trigger.Kind != "OnFoodSpawn" || effect.Action?.Type != "SelfFeederRules" ||
                                effect.Action.Target != "Room" || effect.Action.Range != null ||
                                !effect.Action.AllowPlayer || effect.Action.AllowEnemy)
                                return "Triggered feeding requires the captured food-spawn target and self feeder.";
                            continue;
                        }
                        if (effect.Type == "CardEffectRemoveEquipment")
                        {
                            if (effect.Action?.Type != "RemoveEquipment" || effect.Action.Target != "Self" ||
                                state.Context?.SummonCatalog == null || unit.EquipmentCards == null)
                                return "Triggered equipment removal requires its self target, equipment catalog and bindings.";
                            continue;
                        }
                        if (effect.Type is "CardEffectAdjustDragonsHoard" or "CardEffectAdjustDragonsHoardPerStatusEffect")
                        {
                            bool perStatus = effect.Type == "CardEffectAdjustDragonsHoardPerStatusEffect";
                            if (perStatus && (effect.Action?.Type != "HoardPerStatus" || effect.Action.Target != "Self" ||
                                effect.Action.Statuses.Count != 1 || effect.Action.Statuses[0].Id != "captured_soul" || effect.Action.Statuses[0].Stacks <= 0))
                                return "Uncalibrated status-scaled Hoard award.";
                            CombatContext? context = state.Context;
                            if (context?.QueryFrame?.DragonsHoard is not int current || context.Resources?.DragonsHoardCap is not int cap ||
                                context.EnergyState?.PyreAlive == null)
                                return "Unit Dragon's Hoard effects require captured amount, cap and Pyre liveness.";
                            if (cap < 0) return "Invalid Dragon's Hoard cap.";
                            int award = perStatus ? (unit.Status("captured_soul")?.Stacks ?? 0) / effect.Action!.Statuses[0].Stacks : effect.Value;
                            bool changes = Math.Clamp(unchecked(current + award), 0, cap) != current;
                            bool scaledUpgrade = (context.CardRegistry ?? Array.Empty<CardInstanceState>())
                                .Concat(context.CardInstances ?? Array.Empty<CardInstanceState>())
                                .Any(card => card.UnitUpgradeScalingTraits?.Any(trait => trait.Query.Type == "DragonsHoardAmount") == true);
                            if (changes && scaledUpgrade)
                                return "Dragon's Hoard changes with resource-scaled unit upgrades require native stat refresh.";
                            continue;
                        }
                        if (effect.Type is not ("CardEffectRewardGold" or "CardEffectAddBattleCard" or "CardEffectAddRunCard"))
                            return "Unmodeled effect " + effect.Type + " on " + unit.AssetKey + " trigger " +
                                trigger.Kind + " in room " + state.RoomIndex;
                        if (state.Context == null) return "Unit effects require shared battle context.";
                        if (effect.Type == RelicGoldLossOnHitModel.Effect && effect.Value < 0 &&
                            !RelicGoldLossOnHitModel.OwnsTrigger(state.Context, unit, trigger))
                            return "Conditional gold costs are not modeled.";
                        if (effect.Type is "CardEffectAddBattleCard" or "CardEffectAddRunCard" &&
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
            internal bool WillDieFromEaten;
            internal bool Removed;
            internal bool InRoom = true;
            internal int? LastAttackerId;
            internal int? LastFeederId;
            private IReadOnlyList<CombatStatus>? statusRegistry;
            private StatusDictionaryState? dictionary;
            private int roomIndex;
            internal int RoomIndex => roomIndex;
            internal void MoveToRoom(int destination) { roomIndex = destination; InRoom = false; }
            private readonly Func<CombatContext?>? getContext;
            private readonly Action<string>? reject;
            private readonly Func<WorkingUnit, string, int>? roomStatusModifier;
            private readonly Func<WorkingUnit, string, int>? roomTriggerModifier;
            private readonly Func<WorkingUnit, int>? roomHealthDamage;
            internal bool Alive => Health > 0 && !Removed && !Despawned;
            internal int Attack
            {
                get
                {
                    int? replacement = UnitAttackOverrideModel.Resolve(Statuses.Values, Source.MaxHealth);
                    if (replacement.HasValue) return replacement.Value;
                    int mageblade = 0;
                    if (Source.Modifiers?.MagebladeDamagePerMagicPower is int coefficient)
                    {
                        RoomMagicPower? power = getContext?.Invoke()?.MagicPower?.FirstOrDefault(value =>
                            value.RoomIndex == roomIndex && value.Team == Source.Team);
                        mageblade = unchecked(coefficient * (power?.Value ?? 0));
                    }
                    return RelicLateAttackModel.Apply(Source, unchecked(Source.AttackBeforeStatuses +
                        (Source.Modifiers?.DamageBuffFromStatusEffects ?? unchecked(Amount("buff") + Amount("valor") + Amount("mass") - Amount("debuff"))) +
                        RelicStatusAttackModel.AdditionalAttack(Source.Modifiers?.StatusAttackModifiers, Source.Team, Statuses.Values.ToArray()) +
                        UnitSelfAttackModel.Additional(Source, getContext?.Invoke(), Statuses.Values, Health) +
                        (roomHealthDamage?.Invoke(this) ?? 0) +
                        (roomStatusModifier?.Invoke(this, "buff") ?? 0) * Count("buff") +
                        (roomStatusModifier?.Invoke(this, "valor") ?? 0) * Count("valor") +
                        mageblade + (Source.IsPyre ? getContext?.Invoke()?.Resources?.PyreModifiedBaseAttack ?? 0 : 0)));
                }
            }
            internal int Attacks => CombatUnit.AttackCount(Statuses.Values);

            internal WorkingUnit(CombatUnit source, int roomIndex, Func<CombatContext?>? getContext = null, Action<string>? reject = null,
                Func<WorkingUnit, string, int>? roomStatusModifier = null, Func<WorkingUnit, string, int>? roomTriggerModifier = null,
                Func<WorkingUnit, int>? roomHealthDamage = null)
            {
                this.roomIndex = roomIndex; this.getContext = getContext; this.reject = reject;
                this.roomStatusModifier = roomStatusModifier;
                this.roomTriggerModifier = roomTriggerModifier;
                this.roomHealthDamage = roomHealthDamage;
                Source = source; Health = source.Health; LastAttackerId = source.LastAttackerId;
                LastFeederId = source.LastFeederId;
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
                LastFeederId = changed.LastFeederId;
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
                        Source.DeathState.IsDestroyed.HasValue ? Destroyed : (bool?)null), bumpRules: Source.BumpRules, lastFeederId: LastFeederId, moonPhaseMask: Source.MoonPhaseMask, damageHistory: Source.DamageHistory);
                result = TriggerCountModel.Refresh(getContext?.Invoke(), result, out string? error,
                    kind => roomTriggerModifier?.Invoke(this, kind) ?? 0);
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
            private readonly Func<int, int, RelentlessDecision?>? chooseRelentless;
            private readonly UnitCopyCatalog? unitCopyCatalog;
            private bool previewTruncated;
            private int round;
            private CombatContext? context;
            private readonly bool deferSpawnerExhaustion;
            private readonly int pendingSummonCardId;
            private readonly Action<QueuedCharacterTrigger>? enqueueCharacterTrigger;
            private readonly bool deferAbilityCallbacks;
            private readonly bool nativeCardDamage;
            private readonly bool centerDamageDeaths;
            private bool plainSpellDamage;
            private bool triggeredCommand;
            private bool ordinaryPyreAttack;
            private int observedPyrePreviewVictimId;
            private readonly Func<RoomCombatState, RoomCombatResult>? finishDamageQueue;
            private readonly Func<RoomCombatResult, RoomCombatResult>? settleDamageCallbacks;
            private readonly Func<CombatContext, bool, RelicPyreAttackRefreshResult>? refreshPyre;
            private readonly Func<RoomCombatState, CardActionEffect, int, MoonPhaseTransitionResult>? moonPhase;
            private readonly Func<RoomCombatState, CardActionEffect, int, BumpResult>? birthBump;
            private readonly IReadOnlyList<RoomPlayRule>? roomRules;
            private readonly Func<RoomCombatState, int, IReadOnlyList<QueuedCharacterTrigger>, RoomCombatResult>? relicBirthEffect;
            private readonly Func<RoomCombatState, int, string, RoomCombatResult>? relicCharacterEffect;
            private readonly Queue<(int RoomIndex, WorkingUnit Unit, string Kind, bool CanFire, int ParamInt, WorkingUnit? OverrideTarget, int ParamInt2, string? ParamString, WorkingUnit? DyingCharacter, int TriggerCount, int LastSpawnedOverrideUnitId, int? ExclusiveTriggerStateId, int? AttackDamageDealt)> triggerQueue = new Queue<(int, WorkingUnit, string, bool, int, WorkingUnit?, int, string?, WorkingUnit?, int, int, int?, int?)>();
            private bool runningTriggerQueue;
            private bool holdPreviewTriggerQueue;
            private bool stoppedByRemotePyreDamage;
            private bool localFeedingQueue;
            private bool localUpgradeQueue;
            private bool stopAfterBossRemoval;
            private bool runningRoomCombat;
            private bool killCamActivated;
            // Older captures omitted the identity store. Retain observed source cards while
            // this operation finishes, without inventing unobserved references in its output.
            private IReadOnlyList<CardInstanceState>? legacyDetachedCards;
            private readonly HashSet<int> cannotAttackOrHeal = new HashSet<int>();
            private readonly HashSet<int> cannotFireTriggers = new HashSet<int>();
            private readonly HashSet<int> externalTurnActorIds = new HashSet<int>();
            private HashSet<int>? previewKillBumpShiftedActors;
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
                IReadOnlyList<(int RoomIndex, CombatUnit Unit)>? externalTargets = null,
                Func<RoomCombatState, int, string, RoomCombatResult>? relicCharacterEffect = null,
                Func<RoomCombatState, CardActionEffect, int, BumpResult>? birthBump = null, bool centerDamageDeaths = true,
                Func<RoomCombatResult, RoomCombatResult>? settleDamageCallbacks = null,
                Func<int, int, RelentlessDecision?>? chooseRelentless = null, UnitCopyCatalog? unitCopyCatalog = null,
                IReadOnlyList<RoomPlayRule>? roomRules = null)
            {
                this.source = source; this.events = events;
                this.chooseRelentless = chooseRelentless;
                this.unitCopyCatalog = unitCopyCatalog;
                this.deferSpawnerExhaustion = deferSpawnerExhaustion;
                this.pendingSummonCardId = pendingSummonCardId;
                this.enqueueCharacterTrigger = enqueueCharacterTrigger;
                this.deferAbilityCallbacks = deferAbilityCallbacks;
                this.nativeCardDamage = nativeCardDamage;
                this.centerDamageDeaths = centerDamageDeaths;
                this.finishDamageQueue = finishDamageQueue;
                this.settleDamageCallbacks = settleDamageCallbacks;
                this.refreshPyre = refreshPyre;
                this.moonPhase = moonPhase;
                this.birthBump = birthBump;
                this.roomRules = roomRules;
                this.relicBirthEffect = relicBirthEffect;
                this.relicCharacterEffect = relicCharacterEffect;
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
                // Card hovers already reset the selected actors when establishing
                // their temporary copies. Later victims/effects share those flags.
                if (source.Preview && resetPreviewTriggers && source.Context?.Enchantments?.TemporaryPreviewUnitIds == null)
                    foreach (WorkingUnit unit in units)
                        for (int index = 0; index < unit.Triggers.Count; index++) unit.Triggers[index] = unit.Triggers[index].ForPreview();
                context = source.Context;
                killCamActivated = context?.KillCamActivated == true;
            }

            private WorkingUnit NewUnit(CombatUnit unit, bool inRoom = true, int? roomIndex = null) =>
                new WorkingUnit(unit, roomIndex ?? source.RoomIndex, () => context, error => unsupportedReason = error,
                    RoomStatusModifier, RoomTriggerModifier, RoomHealthDamage) { InRoom = inRoom };

            private int RoomHealthDamage(WorkingUnit target)
            {
                if (target.Source.Team != CombatTeam.Player || !units.Any(unit => RoomHealthAttackModel.HasSource(unit.Source))) return 0;
                var point = context?.SpawnPoints?.Units.SingleOrDefault(unit => unit.UnitId == target.Source.Id)?.Current;
                if (point?.RoomIndex != target.RoomIndex) return 0;
                return RoomHealthAttackModel.Additional(target.Source,
                    units.Where(unit => unit.Alive && unit.InRoom && unit.RoomIndex == target.RoomIndex &&
                        unit.Source.Team == target.Source.Team && RoomHealthAttackModel.HasSource(unit.Source) && !unit.Has("untouchable") &&
                        !PyreBaneModel.ExcludedFromRoomMode(unit.Freeze(), "Room", false)).Select(unit => unit.Source), target.Health);
            }

            private int RoomTriggerModifier(WorkingUnit target, string kind) => units.Where(unit => unit.Alive && unit.InRoom &&
                unit.RoomIndex == target.RoomIndex && unit.Source.Team == target.Source.Team)
                .Sum(unit => unit.Source.Modifiers?.RoomTriggerCounts?.Where(rule => rule.Kind == kind).Sum(rule => rule.Value) ?? 0);

            private int RoomStatusModifier(WorkingUnit target, string status) => units.Where(unit => unit.Alive && unit.InRoom &&
                unit.RoomIndex == target.RoomIndex && unit.Source.Team == target.Source.Team)
                .Sum(unit => unit.Source.Modifiers?.RoomStatuses?.Where(rule => rule.StatusId == status).Sum(rule => rule.Amount) ?? 0);

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

            internal RoomCombatResult Run(bool entireRoom, bool updateSelectedRoom, IReadOnlyCollection<int>? pendingPhysicalDeathIds = null)
            {
                runningRoomCombat = true;
                ordinaryPyreAttack = source.RoomIndex == 3 && source.Units.Any(unit => unit.IsPyre);
                stopAfterBossRemoval = true;
                var pendingRemovals = context?.Enchantments?.RetainedUnits.Where(actor => !source.Preview &&
                    actor.RoomIndex == source.RoomIndex && actor.Unit.Health <= 0 &&
                    actor.Unit.DeathState is { HasFinishedDying: true, HasStatisticsListener: false } &&
                    units.All(unit => unit.Source.Id != actor.Unit.Id) &&
                    context.SpawnPoints?.Units.Any(point => point.UnitId == actor.Unit.Id && point.Current != null) == true).ToArray();
                if (pendingPhysicalDeathIds != null && (entireRoom || pendingPhysicalDeathIds.Distinct().Count() != pendingPhysicalDeathIds.Count ||
                    pendingPhysicalDeathIds.Any(id => pendingRemovals?.Count(actor => actor.Unit.Id == id) != 1)))
                {
                    unsupportedReason = "Explicit pending physical deaths require matching retained room actors.";
                    return Finish(RoomOutcome.Unsupported);
                }
                foreach (var pending in pendingRemovals ?? Array.Empty<EnchantmentRetainedUnit>())
                {
                    // CheckForDeath already completed its signals/statistics.
                    var unit = NewUnit(pending.Unit, false); units.Add(unit);
                    if (pendingPhysicalDeathIds?.Contains(unit.Source.Id) == true)
                    {
                        unit.Apply(MarkBeingRemoved(unit.Freeze()));
                        FireTriggers(unit, "OnDeath");
                        PhysicalHarvest(unit);
                        if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    }
                    SettleDeadSpawner(unit); RemovePhysicalPoint(unit);
                }
                if (pendingRemovals?.Length > 0)
                {
                    CenterAfterSummon();
                    context = ClearFinishedRemovalPoints(context!, pendingRemovals.Select(actor => actor.Unit.Id).ToArray());
                }
                if (!source.Preview && context != null && BothTeamsPresent())
                    context = PreviewBirthModel.CompleteRemoteCombatBirths(context, source.RoomIndex);
                if (updateSelectedRoom && !source.Preview && context?.QueryFrame?.SelectedRoomIndex != null &&
                    units.Any(unit => unit.Alive && unit.InRoom && unit.Source.Team == CombatTeam.Enemy) &&
                    units.Any(unit => unit.Alive && unit.InRoom && unit.Source.Team == CombatTeam.Player && !unit.Source.IsPyre))
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: source.RoomIndex));
                bool relentless = units.Any(unit => unit.InRoom && unit.Has("relentless")) && BothTeamsPresent();
                int? notificationInterval = context?.QueryFrame?.RelentlessNotificationInterval;
                bool notificationCanStop = notificationInterval.HasValue && (source.Preview || source.RoomIndex != 3);
                var seen = new HashSet<string>(StringComparer.Ordinal);
                bool repeat;
                do
                {
                    // Native yields out of the entire room coroutine in preview,
                    // before clearing status effects or completing the room.
                    if (entireRoom && round > 0 && notificationInterval.HasValue && round % notificationInterval.Value == 0)
                    {
                        if (source.Preview)
                        { previewTruncated = true; return Finish(RoomOutcome.Exchanged); }
                        if (source.RoomIndex != 3)
                        {
                            RelentlessDecision? choice = chooseRelentless?.Invoke(source.RoomIndex, round);
                            if (choice == null)
                                return new RoomCombatResult(null, RoomOutcome.Unsupported, round, events,
                                    "Relentless combat requires Continue or Abandon before the next exchange.",
                                    relentlessChoice: new RelentlessChoiceRequest(source.RoomIndex, round));
                            if (choice.RoomIndex != source.RoomIndex || choice.CompletedRounds != round)
                                return new RoomCombatResult(null, RoomOutcome.Unsupported, round, events,
                                    "Relentless decision does not match the next combat dialog.");
                            if (!choice.Continue) break;
                        }
                    }
                    if (round > 0 && entireRoom) Clear(true);
                    // Compare the same phase: decay before the next exchange can break a apparent cycle.
                    if (entireRoom && !notificationCanStop && !seen.Add(Signature()))
                        return Finish(RoomOutcome.Stalemate);
                    round++;
                    Exchange();
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    if (CombatPreviewStopped()) return Finish(RoomOutcome.Exchanged);
                    if (!entireRoom) return Finish(RoomOutcome.Exchanged);
                    if (battleWon) return Finish(RoomOutcome.BattleWon);
                    if (!source.Preview && units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                        return Finish(RoomOutcome.PlayerDefeated);
                    Feed();
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    RemoveHatch();
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    PostCombat();
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    if (CombatPreviewStopped()) return Finish(RoomOutcome.Exchanged);
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
                if (!source.Deployment && context?.QueryFrame?.Turn != 0)
                {
                    StatusScalingResult valorArmor = RelicStatusMagnitudeModel.GetValorArmorIncrease(context);
                    if (!valorArmor.Supported) { unsupportedReason = valorArmor.UnsupportedReason; return Finish(RoomOutcome.Unsupported); }
                    context = valorArmor.Context;
                    foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
                    {
                        WorkingUnit? front = units.FirstOrDefault(unit => unit.Alive && unit.InRoom && unit.Source.Team == team && !unit.Has("untouchable"));
                        if (front == null || !front.Has("valor") || front.Has("immune") || front.Source.StatusImmunities.Contains("armor")) continue;
                        int goal = Math.Min(9999, unchecked(front.Amount("valor") + front.Count("valor") * valorArmor.Stacks));
                        // Valor calls AddStatusEffect even when existing armor already meets its target.
                        // That zero addition still queues status and armor callbacks in the native game.
                        RoomCombatResult added = StatusApplicationModel.ApplyRetained(CurrentRoom(), front.Source.Id,
                            context!.StatusRules.First(rule => rule.Id == "armor").WithStacks(Math.Max(0, goal - front.Count("armor"))), 0);
                        if (!added.Supported) { unsupportedReason = added.UnsupportedReason; return Finish(RoomOutcome.Unsupported); }
                        context = added.State!.Context;
                        foreach (CombatUnit changed in added.State.Units) units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                        foreach (QueuedCharacterTrigger callback in added.PendingCallbacks) QueueCallback(callback);
                    }
                }
                if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                Clear(true);
                if (!source.Preview && context != null && !battleWon) RunRoomPostCombatEffects();
                if (context != null && !battleWon && !units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                    RunEndTurnHealthRelics();
                if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                if (context != null && !battleWon && !units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                {
                    RelicMoonPhaseHandUpgradeResult frozen = RelicFreezeHandUpgradeModel.Apply(context, source.RoomIndex,
                        source.Deployment, source.Preview);
                    if (!frozen.Supported) { unsupportedReason = frozen.UnsupportedReason; return Finish(RoomOutcome.Unsupported); }
                    context = frozen.Context;
                }
                // CombatManager removes overcharge after the room's complete
                // post-combat and end-turn relic queues, including empty rooms.
                // Individual exchanges and native previews do not clamp it.
                RoomCapacityState? capacity = context?.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == source.RoomIndex);
                if (!source.Preview && capacity?.Corruption is int currentCorruption &&
                    capacity.MaxCorruption is int maximumCorruption && currentCorruption > maximumCorruption)
                {
                    var cleanup = RoomCorruptionModel.Apply(new TrainCombatState([CurrentRoom()], [], 0, context),
                        source.RoomIndex, new CardActionEffect("AdjustCorruption", "Room", currentCorruption - maximumCorruption,
                            false, false, [], corruptionAdd: false));
                    if (!cleanup.Supported) { unsupportedReason = cleanup.UnsupportedReason; return Finish(RoomOutcome.Unsupported); }
                    context = cleanup.Context;
                }
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Cleared);
            }

            private void RunEndTurnHealthRelics()
            {
                for (int ownerIndex = 0; ownerIndex < (context?.Relics?.Count ?? 0); ownerIndex++)
                foreach (RelicResourceEffect effect in context!.Relics![ownerIndex].ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                {
                    CombatRelicState owner = context!.Relics![ownerIndex];
                    if (effect.Type == RelicOccupiedFloorDrawModel.Effect)
                    {
                        if (!RefreshOccupiedDraw("EndTurn", ownerIndex, effect.EffectIndex)) return;
                        continue;
                    }
                    if (effect.Type != RelicEndTurnHealthModel.Effect || source.Deployment && owner.DisallowedInPlacementPhase == true) continue;
                    string? error = RelicEndTurnHealthModel.Validate(owner, effect);
                    if (error != null) { unsupportedReason = error; return; }
                    var room = new RoomCombatState(source.RoomIndex, source.Deployment,
                        units.Where(unit => unit.InRoom && !unit.Removed).Select(unit => unit.Freeze()).ToArray(),
                        source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());
                    foreach (int id in RelicEndTurnHealthModel.Targets(room))
                    {
                        WorkingUnit target = units.Single(unit => unit.Source.Id == id);
                        if (target.Destroyed) continue;
                        context = context!.WithQueryFrame(context.QueryFrame?.With(selectedRoomIndex: source.RoomIndex));
                        if (target.Alive) Damage(null, target, effect.Amount, "Spell", applyTraits: false, relicSourced: true);
                        if (unsupportedReason != null) return;
                        if (!runningTriggerQueue && enqueueCharacterTrigger == null) DrainLocalTriggerQueue();
                    }
                }
            }

            private bool RefreshOccupiedDraw(string callback, int ownerIndex, int effectIndex, CombatUnit? actor = null)
            {
                var room = EnchantmentWorldModel.Sync(PhysicalRoom(), units.Where(unit => !unit.Alive || !unit.InRoom)
                    .Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits());
                var result = RelicOccupiedFloorDrawModel.RefreshRoomOne(room, ownerIndex, effectIndex, callback, actor);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                context = result.Context;
                return true;
            }

            private void RunRoomPostCombatEffects()
            {
                foreach (RoomCapacityAttachmentState attachment in context!.RoomCapacities?
                    .FirstOrDefault(room => room.RoomIndex == source.RoomIndex)?.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                {
                    RoomMoonPhaseRule? rule = attachment.MoonPhase;
                    RoomPostCombatRule? post = attachment.PostCombat;
                    if (rule == null && post == null || source.Deployment && (rule?.DisallowedInDeployment ?? post!.DisallowedInDeployment)) continue;
                    string? error = rule != null ? RoomMoonPhaseModel.Validate(rule) : RoomPostCombatModel.Validate(post!, RelicShowtimeModel.IsAttachment(attachment));
                    if (error != null) { unsupportedReason = error; return; }
                    RoomCombatResult result = CardSpellModel.Apply(CurrentRoom(), rule?.PostCombatEffects ?? post!.Effects, 0);
                    if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return; }
                    context = result.State!.Context;
                    foreach (CombatUnit changed in result.State.Units) units.Single(unit => unit.Source.Id == changed.Id).Apply(changed);
                }
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

            internal RoomCombatResult CardDamage(int targetId, int damage, int sourceCardId, bool applyTraits = true,
                int attackerUnitId = 0, bool piercing = false, bool relicSourced = false)
            {
                CardInstanceState? card = context?.LifeLinks == null ? null : context.FindCard(sourceCardId);
                plainSpellDamage = attackerUnitId == 0 && sourceCardId > 0 &&
                    card?.MaskDescriptor?.Definition.CardType == "Spell" && card.Permanent.Upgrades.Count == 0 &&
                    card.Temporary.Upgrades.Count == 0 && CardBranchMaskModel.Resolve(card).Traits.Count == 0;
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                Damage(units.FirstOrDefault(unit => unit.Source.Id == attackerUnitId), target, damage, "Spell", sourceCardId,
                    applyTraits, piercing, relicSourced);
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult ModifyUnit(CombatUnit changed, CombatContext? linkedContext = null)
            {
                if (linkedContext != null)
                {
                    context = linkedContext;
                    var shared = context.Enchantments!.Rooms.SelectMany(room => room.Units).ToDictionary(unit => unit.Id);
                    foreach (WorkingUnit unit in units)
                        if (shared.TryGetValue(unit.Source.Id, out CombatUnit? updated))
                            unit.Apply(unit.Freeze().WithHealthAndMaxHealth(updated.Health, updated.MaxHealth));
                }
                int index = units.FindIndex(unit => unit.Source.Id == changed.Id);
                WorkingUnit target = NewUnit(changed);
                units[index] = target;
                if (!target.Alive)
                {
                    target.Apply(target.Freeze().WithSacrifice(0));
                    if (!TryRevive(null, target, 0))
                    {
                        Death(null, target, 0, sacrifice: true,
                            maximumHealthSacrifice: linkedContext != null && changed.MaxHealth == 0);
                        if (!source.Preview && context?.SpawnPoints != null) CenterAfterSummon();
                    }
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

            internal RoomCombatResult Equipment(int targetId, int cardId, BattlePlayRules definitions, bool remove, bool grafted)
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
                    if (CardGraftedEquipmentModel.Present(card!) && target.Source.Triggers.Any(trigger =>
                        (trigger.Kind == "OnDeath" || trigger.Kind == "OnHatched") && trigger.Effects.Any(effect => effect.Summon != null)))
                    { unsupportedReason = "Grafted equipment child-unit standby handoff is not calibrated."; return Finish(RoomOutcome.Unsupported); }
                    if (!target.Removed)
                    {
                        if (CardGraftedEquipmentModel.Present(card!))
                            QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, target.Freeze(), "OnGraftEquipmentAdded"));
                        foreach (WorkingUnit unit in units.Where(unit => !unit.Removed && unit.InRoom).OrderBy(unit => unit.Source.Team))
                            QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, unit.Freeze(), "OnEquipmentAddedToAny"));
                    }
                    QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, target.Freeze(), "OnEquipmentAdded"));
                    if (grafted && target.Count("unit_grafted_equipment") == 0)
                    {
                        CombatStatus? status = context!.StatusRules.FirstOrDefault(value => value.Id == "unit_grafted_equipment");
                        if (status == null) { unsupportedReason = "Grafted birth requires its native marker status."; return Finish(RoomOutcome.Unsupported); }
                        RoomCombatResult applied = StatusApplicationModel.ApplyRetained(CurrentRoom(), targetId, status.WithStacks(1), 0);
                        if (!applied.Supported) return applied;
                        ImportEnchantmentRoom(applied.State!);
                        foreach (QueuedCharacterTrigger callback in applied.PendingCallbacks) QueueCallback(callback);
                    }
                    context = context!.WithCard((context.FindCard(cardId) ?? card!).WithEquippedUnit(targetId));
                    if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                }
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive) ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            private bool EquipmentUpgrades(int cardId, BattlePlayRules definitions, out CardInstanceState? card, out CardUpgradeModifier[] upgrades)
            {
                card = context?.FindCard(cardId); upgrades = Array.Empty<CardUpgradeModifier>();
                string? dataId = card?.DataId;
                EquipmentDefinition? definition = EquipmentMergeModel.Definition(context, definitions, cardId);
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
                    CardInstanceState? graft = context?.FindCard(cardId);
                    if (graft != null && !source.Preview) context = context!.WithCard(CardGraftedEquipmentModel.SetSevered(graft, true));
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
                    if (target.Count("unit_grafted_equipment") > 0 && !target.Source.EquipmentCards.Any(id =>
                        context!.FindCard(id) is CardInstanceState remaining && CardGraftedEquipmentModel.Present(remaining)))
                        RemoveStatus(target, "unit_grafted_equipment", 1);
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
                    unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId, unit.StatusRegistry, cards, unit.NextTriggerId, unit.Ability, unit.StatusDictionary, unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState, bumpRules: unit.BumpRules, lastFeederId: unit.LastFeederId, moonPhaseMask: unit.MoonPhaseMask, damageHistory: unit.DamageHistory));
            }
            private string EquipmentUpgradeKey(int cardId, BattlePlayRules definitions) =>
                EquipmentMergeModel.Definition(context, definitions, cardId)!.UpgradeId ?? EquipmentModel.UpgradeKey(cardId);

            internal RoomCombatResult CardHeal(int targetId, int amount)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                Heal(target, amount, "SpellHeal");
                return Finish(battleWon ? RoomOutcome.BattleWon : target.Source.IsPyre && !target.Alive
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult MaximumHealthLifecycle(int targetId, int amount, bool debuff, bool heal, bool triggerOnHeal)
            {
                WorkingUnit target = units.Single(unit => unit.Source.Id == targetId);
                if (!debuff && heal) Heal(target, amount, "MaxHealthHeal", fromMaxHealthChange: true, triggerOnHeal: triggerOnHeal);
                UpdateHalfHealthDeathwish(target, drain: false);
                QueuedCharacterTrigger[] pending = SnapshotLocalCallbacks();
                RoomCombatResult result = Finish(RoomOutcome.Exchanged);
                return new RoomCombatResult(result.State, result.Outcome, result.Rounds, result.Events.ToList(), result.UnsupportedReason,
                    pending, result.RetainedUnits, result.Dispatches);
            }

            private QueuedCharacterTrigger[] SnapshotLocalCallbacks() => triggerQueue.Select(queued => new QueuedCharacterTrigger(queued.RoomIndex,
                    queued.Unit.Freeze(), queued.Kind, canFireTriggers: queued.CanFire, paramInt: queued.ParamInt,
                    paramInt2: queued.ParamInt2, paramString: queued.ParamString, dyingCharacter: queued.DyingCharacter?.Freeze(),
                    overrideTarget: queued.OverrideTarget?.Freeze(), triggerCount: queued.TriggerCount,
                    lastSpawnedOverrideUnitId: queued.LastSpawnedOverrideUnitId, exclusiveTriggerStateId: queued.ExclusiveTriggerStateId,
                    admission: CharacterTriggerAdmission.Accepted, captureAllDispatches: traceAllDispatches, attackDamageDealt: queued.AttackDamageDealt)).ToArray();

            internal RoomCombatResult DrainCallbacks(IReadOnlyList<QueuedCharacterTrigger> callbacks)
            {
                foreach (QueuedCharacterTrigger callback in callbacks) QueueCallback(callback);
                DrainLocalTriggerQueue();
                return Finish(battleWon ? RoomOutcome.BattleWon : RoomOutcome.Exchanged);
            }

            internal RoomCombatResult Spawn(int unitId, bool fromCard, IReadOnlyList<CombatStatus>? startingApplications = null,
                bool prepareOnly = false, SpawnStatusApplication? additionalStatus = null, bool copyHordeBeforeOrder = false)
            {
                WorkingUnit spawned = units.Single(unit => unit.Source.Id == unitId);
                bool wasSpawning = spawned.Source.IsSpawning == true;
                // In the original mirrored Meddler birth, CloneMonsterState copies
                // the source card before the first ability card is materialized.
                // Its clone birth warms the shared cache after that copy.
                bool mirroredMeddler = spawned.Source is { AssetKey: "MonsterFaeMeddler", Modifiers.IsClone: false } &&
                    context?.FindCard(spawned.Source.SpawnerCardId)?.DataId == "d10e2dac-fa58-4509-b0ee-112b3bf78800" &&
                    RoomSpawnCopyModel.Active(CurrentRoom()) && RoomSpawnCopyModel.OriginalMirrorsOnly(CurrentRoom()) &&
                    context.SpawnPoints?.FirstEmpty(source.RoomIndex, CombatTeam.Player) >= 0;
                if (!mirroredMeddler && context?.AbilityCardCache != null && spawned.Source.Ability?.CardCreation is CardCreationRule creation)
                {
                    AbilityCardResult cached = AbilityCardModel.Get(context, creation);
                    if (!cached.Supported) { unsupportedReason = cached.UnsupportedReason; return Finish(RoomOutcome.Unsupported); }
                    context = cached.Context;
                }
                if (startingApplications != null)
                {
                    var callbacks = new List<QueuedCharacterTrigger>();
                    unsupportedReason = StatusCallbackModel.Initialize(CurrentRoom(), spawned.Freeze(), startingApplications,
                        callbacks, spawned.Apply, ImportEnchantmentRoom, additionalStatus, updateContext: changed => context = changed);
                    if (unsupportedReason != null) return Finish(RoomOutcome.Unsupported);
                    foreach (QueuedCharacterTrigger callback in callbacks) QueueCallback(callback);
                }
                if (spawned.Source.Team == CombatTeam.Player) spawned.Apply(HordeStatusModel.WithSpawning(spawned.Freeze(), true));
                if (!ApplyBirthRelics(true)) return Finish(RoomOutcome.Unsupported);
                int birthMoonMask = 0;
                if (context?.QueryFrame?.MoonPhase is int moon &&
                    (spawned.Source.MoonPhaseMask.HasValue || MoonPhaseStatusModel.IsTracking(context, units.Select(unit => unit.Freeze()))))
                {
                    birthMoonMask = RoomMoonPhaseModel.Effective(context, source.RoomIndex, moon);
                    // CharacterState.Setup registers the moon before room attachments
                    // even without tracking. Only tracked births acquire a phase
                    // status from PlayerManager.OnCharacterSpawned.
                    CombatUnit understood = spawned.Freeze();
                    if (MoonPhaseStatusModel.IsTracking(context, units.Select(unit => unit.Freeze())))
                        understood = MoonPhaseStatusModel.WithPhase(understood, moon);
                    spawned.Apply(understood.WithMoonPhaseMask(birthMoonMask));
                }
                // Native installs starting statuses and covenant effects before
                // room attachments. Initializing afterward can erase an
                // attachment's status while retaining its upgrade/stat record.
                RoomCombatState? attached = RoomAttachmentUnitModel.ApplyToNewCharacter(CurrentRoom(), unitId,
                    out string? attachmentError);
                if (attachmentError != null || attached == null)
                {
                    unsupportedReason = attachmentError ?? "Room attachment birth synchronization failed.";
                    return Finish(RoomOutcome.Unsupported);
                }
                ImportEnchantmentRoom(attached);
                if (birthMoonMask != 0)
                {
                    // The native birth updates the unit's moon understanding and
                    // queues its initial phase callback before the spawn triggers.
                    // Deferred births already carry this callback in their queue.
                    if (!prepareOnly)
                    {
                        if ((birthMoonMask & 2) != 0) FireTriggers(spawned, "OnMoonLit");
                        if ((birthMoonMask & 1) != 0) FireTriggers(spawned, "OnMoonShade");
                    }
                }
                if (copyHordeBeforeOrder)
                {
                    // The scoped cardless Funguy path already installed its
                    // initial Horde stacks. Room duplication adds its stack
                    // before the first troop-aura room-order binding; the
                    // queued OnTroopAdded callback remains a separate write.
                    RoomCombatState frame = CurrentRoom();
                    UnitCloneResult copies = RoomSpawnCopyModel.PaidBirth(new TrainCombatState(new[] { frame },
                        Array.Empty<EnemyMovement>(), 0, frame.Context), unitId, null);
                    if (!copies.Supported) { unsupportedReason = copies.UnsupportedReason; return Finish(RoomOutcome.Unsupported); }
                    ImportEnchantmentRoom(copies.State!.Rooms.Single());
                    events.AddRange(copies.Events);
                    foreach (var callback in copies.PendingCallbacks) QueueCallback(callback);
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
                        RetireRemovedUnits(result.State, damaged.State!);
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

            internal RoomCombatResult UnitTurn(int unitId, bool ignoreAttackPrevention = false,
                IReadOnlyList<int>? overrideTargets = null, CombatTeam? targetTeam = null)
            {
                runningRoomCombat = true;
                WorkingUnit actor = units.Single(unit => unit.Source.Id == unitId);
                ordinaryPyreAttack = actor.Source.IsPyre && source.RoomIndex == 3 && !externalTurnActorIds.Contains(unitId);
                triggeredCommand = actor.Source.IsPyre && externalTurnActorIds.Contains(unitId) ||
                    actor.Source.Triggers.Count == 1 && actor.Source.Status("valor") is { Stacks: 5, ParamInt: 1 } &&
                    TrueFinalBossCombatModel.SupportsAlly(actor.Source, context);
                stopAfterBossRemoval = true;
                round = 1;
                Turn(units.Single(unit => unit.Source.Id == unitId), ignoreAttackPrevention, overrideTargets, targetTeam);
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
                int retainedDuringCallback = 0;
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
                if (actor == null && (queued.Kind == "OnDeath" || queued.Kind == "OnHit" || queued.Kind == "OnKill" || queued.Kind == "OnBloodshed" || queued.Kind == "OnPunish" ||
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
                    {
                        // Native callbacks retain a mutable character reference.
                        // A prior removal can finish before a queued Sentry uses
                        // it; keep that completed state instead of the old queue
                        // snapshot when the actor has left the live room.
                        CombatUnit current = context?.Enchantments?.RetainedUnits.FirstOrDefault(unit =>
                            unit.Unit.Id == queued.OverrideTarget.Id)?.Unit ?? queued.OverrideTarget;
                        overridden = NewUnit(current, false); units.Add(overridden);
                    }
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
                        exclusiveTriggerStateId: queued.ExclusiveTriggerStateId, paramInt2: queued.ParamInt2, paramStringOverride: queued.ParamString, attackDamageDealt: queued.AttackDamageDealt);
                    if (queued.Kind == "OnDeath" && queued.ParamInt != 1 && actor.Source.DeathState != null &&
                        (queued.RemovalLifecycle || queued.Unit.Health <= 0))
                        actor.Apply(actor.Freeze().WithDeathState(new UnitDeathState(true, true, false,
                            isSacrifice: actor.Source.DeathState.IsSacrifice, statisticsListenerOnce: actor.Source.DeathState.StatisticsListenerOnce.HasValue ? false : (bool?)null)));
                    queued.Unit = actor.Freeze();
                    if (overridden != null) queued.OverrideTarget = overridden.Freeze();
                    if (dying != null)
                    {
                        queued.DyingCharacter = dying.Freeze();
                        // The transfer returns before the death queue detaches its
                        // source from the room's physical position.
                        if (actor.Triggers.Any(trigger => trigger.Kind == queued.Kind && trigger.Effects.Any(effect =>
                            TriggeredStatusTransferModel.IsLifemotherDeath(actor.Source, trigger.Kind, effect.Action))) &&
                            context?.SpawnPoints?.Units.Any(point => point.UnitId == dying.Source.Id &&
                                point.Current?.RoomIndex == source.RoomIndex) == true)
                            retainedDuringCallback = dying.Source.Id;
                    }
                }
                return Finish(battleWon ? RoomOutcome.BattleWon : units.Any(unit => unit.Source.IsPyre && !unit.Alive)
                    ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged, retainedDuringCallback);
            }

            internal RoomCombatResult SacrificeRetained(CombatUnit unit, int sourceCardId, bool whileRunningQueue, bool doNotFullyRemove,
                bool returnSpawnerAfterQueue, bool destroyRoom = false)
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
                    doNotFullyRemove: doNotFullyRemove, returnSpawnerAfterQueue: returnSpawnerAfterQueue,
                    destroyRoom: destroyRoom);
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
                    if (battleWon || stoppedByRemotePyreDamage || CombatPreviewStopped()) return;
                }
                BeginTeam(CombatTeam.Enemy);
                foreach (WorkingUnit unit in units.Where(unit => unit.InRoom && unit.Source.Team == CombatTeam.Enemy).ToArray())
                { Turn(unit); if (battleWon || stoppedByRemotePyreDamage || CombatPreviewStopped()) return; }
                BeginTeam(CombatTeam.Player);
                foreach (WorkingUnit unit in units.Where(unit => unit.InRoom && unit.Source.Team == CombatTeam.Player).ToArray())
                    if (!quick.Contains(unit)) { Turn(unit); if (battleWon || stoppedByRemotePyreDamage || CombatPreviewStopped()) return; }
            }

            private bool CombatPreviewStopped() => source.Preview && units.Any(unit => !unit.Alive &&
                (IsTerminalBossDeath(unit) || unit.Source.IsPyre));

            private void Turn(WorkingUnit actor, bool ignoreAttackPrevention = false,
                IReadOnlyList<int>? overrideTargets = null, CombatTeam? targetTeam = null)
            {
                if (!actor.Alive || !actor.InRoom) return;
                bool dazed = actor.Has("dazed") && Active(actor.Statuses["dazed"]);
                if (dazed)
                {
                    cannotAttackOrHeal.Add(actor.Source.Id); cannotFireTriggers.Add(actor.Source.Id);
                    Trigger(actor, "dazed", 1);
                    Emit("Dazed", actor, actor, 0);
                }
                // Inert checks fuel before Spark consumes one stack. The last stack still
                // permits this entire unit turn, including every Multistrike attack.
                bool inert = !dazed && actor.Has("inert") && Active(actor.Statuses["inert"]) && !actor.Has("spark");
                if (inert)
                {
                    if (!ignoreAttackPrevention) cannotAttackOrHeal.Add(actor.Source.Id);
                    Trigger(actor, "inert", 1);
                    Emit("Inert", actor, actor, 0);
                }
                bool restricted = !dazed && !inert && actor.Has("restricted") && Active(actor.Statuses["restricted"]);
                if (restricted)
                {
                    if (!ignoreAttackPrevention) cannotAttackOrHeal.Add(actor.Source.Id);
                    Trigger(actor, "restricted", 1);
                    Emit("Restricted", actor, actor, 0);
                }
                bool untouchable = actor.Has("untouchable") && Active(actor.Statuses["untouchable"]);
                if (untouchable && !ignoreAttackPrevention) cannotAttackOrHeal.Add(actor.Source.Id);
                if (actor.Has("spark") && Active(actor.Statuses["spark"])) Trigger(actor, "spark", 1);
                // Native queues this after attack/trigger prevention, before attack conditions.
                // Ignored-silence triggers can run while dazed, without restoring this turn's attack.
                FireTriggers(actor, "OnTurnBegin", canFireTriggers: !dazed);
                if (!actor.Alive || (dazed || inert || restricted || untouchable) && !ignoreAttackPrevention || !actor.Source.CanAttack || actor.Attack <= 0) return;
                int multistrikeExtraAttacks = actor.Statuses.TryGetValue("multistrike", out CombatStatus? multi)
                    ? CombatUnit.AdditionalAttacks(multi) : 0;
                int strikes = actor.Attacks;
                for (int strike = 0; strike < strikes && actor.Alive; strike++)
                {
                    bool sweep = actor.Has("sweep");
                    WorkingUnit[] targets = overrideTargets?.Select(id => units.FirstOrDefault(unit => unit.Source.Id == id))
                        .Where(unit => unit != null && unit.Alive && unit.InRoom).Cast<WorkingUnit>().ToArray() ?? Array.Empty<WorkingUnit>();
                    if (targets.Length == 0)
                    {
                        // Native prunes dead overrides before each strike, then
                        // falls back to normal targeting when none remain.
                        // AttackWithUnit always supplies the native HeroManager as
                        // target manager, including an enemy commanded to attack.
                        targets = units.Where(unit => unit.Alive && unit.InRoom &&
                            (targetTeam.HasValue ? unit.Source.Team == targetTeam : unit.Source.Team != actor.Source.Team) &&
                            !unit.Has("stealth") && !unit.Has("untouchable") && PyreBaneModel.Targetable(unit.Freeze(), inCombat: true)).ToArray();
                        if (targets.Length == 0) continue;
                        if (!sweep) targets = new[] { actor.Has("sniper") ? targets.Last() : targets.First() };
                    }
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
                            if (stoppedByRemotePyreDamage) return;
                            if (source.Preview && sweep)
                            {
                                // RunAttack removes each preview victim while the sweep
                                // queue stays held. HeroManager cancels the preview when
                                // removing its last boss, before queued attack effects run.
                                bool held = holdPreviewTriggerQueue;
                                holdPreviewTriggerQueue = true;
                                DrainLocalTriggerQueue();
                                holdPreviewTriggerQueue = held;
                                runningTriggerQueue = true;
                                if (CombatPreviewStopped()) return;
                            }
                        }
                    if (sweep) DrainLocalTriggerQueue();
                }
            }

            private void PreviewBossAttack(WorkingUnit actor, WorkingUnit target)
            {
                if (source.Preview || context?.Statistics == null || !units.Any(unit => unit.Alive && unit.InRoom &&
                    (unit.Source.IsPyre || unit.Source.IsBoss == true || unit.Source.IsBoss == null && unit.Source.EndsBattleOnDeath))) return;
                CombatUnit[] localUnits = units.Where(unit => !externalTurnActorIds.Contains(unit.Source.Id))
                    .Select(unit => unit.Freeze()).ToArray();
                var copied = new RoomCombatState(source.RoomIndex, source.Deployment, localUnits,
                    InteractionsAfterLocalCasualties(localUnits), context.SpawnPoints == null ? context :
                        context.WithSpawnPoints(context.SpawnPoints.BeginPreview()), preview: true,
                    activeCompanionBossIds: CompanionBossSnapshot());
                EnchantmentWorld? originalWorld = context.Enchantments;
                var temporaryPreviewIds = units.Where(unit => unit.InRoom && !unit.Removed &&
                    !externalTurnActorIds.Contains(unit.Source.Id)).Select(unit => unit.Source.Id).ToHashSet();
                temporaryPreviewIds.Add(actor.Source.Id);
                foreach (WorkingUnit local in units.Where(unit => unit.InRoom && !unit.Removed &&
                    !externalTurnActorIds.Contains(unit.Source.Id) && unit.Source.IsBoss == true && unit.Source.BumpRules?.CompanionBoss == true))
                {
                    int pair = local.Source.BumpRules!.PairedBossUnitId;
                    if (originalWorld?.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == pair &&
                        (unit.Health > 0 || unit.Status("undying")?.Stacks > 0)) == true)
                        temporaryPreviewIds.Add(pair);
                }
                if (originalWorld?.AutomaticLifecycle == true)
                {
                    RoomCombatState synced = EnchantmentWorldModel.Sync(PhysicalRoom(), units.Where(unit => !unit.Alive || !unit.InRoom ||
                        externalTurnActorIds.Contains(unit.Source.Id))
                        .Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits());
                    context = synced.Context!; originalWorld = context.Enchantments!;
                    EnchantmentCombatState prepared = originalWorld.WithTemporaryPreviewUnits(temporaryPreviewIds.OrderBy(id => id).ToArray()).Frame(context);
                    foreach (int id in temporaryPreviewIds)
                        prepared = EnchantmentCombatModel.PrepareForPreview(prepared, id);
                    EnchantmentWorld preparedWorld = EnchantmentWorld.From(prepared, true);
                    CombatContext testContext = context.WithEnchantments(preparedWorld);
                    if (testContext.SpawnPoints != null) testContext = testContext.WithSpawnPoints(testContext.SpawnPoints.BeginPreview());
                    localUnits = units.Where(unit => unit.InRoom && !unit.Removed &&
                        !externalTurnActorIds.Contains(unit.Source.Id))
                        .Select(unit => EnchantmentWorldModel.RestorePreviewEffects(unit.Freeze(), preparedWorld, false)).ToArray();
                    copied = new RoomCombatState(copied.RoomIndex, copied.Deployment, localUnits,
                        InteractionsAfterLocalCasualties(localUnits), testContext, true, CompanionBossSnapshot());
                    var train = new TrainCombatState(prepared.Train.Rooms.Select(room => room.RoomIndex == source.RoomIndex ? copied : room).ToArray(),
                        originalWorld.Movement, originalWorld.EnemySlotsPerRoom, testContext);
                    train = EnchantmentWorldModel.Rebase(train, preview: true, testRng: context.IsolatedBattlePreview == true ? context.BattleRng : originalWorld.TestRng);
                    copied = EnchantmentWorldModel.Room(copied, train.Context);
                }
                CombatUnit? externalActor = externalTurnActorIds.Contains(actor.Source.Id) ? actor.Freeze() : null;
                if (copied.Context?.LifeLinks is LifeLinkContext previewLinks)
                    copied = copied.WithContext(copied.Context.WithLifeLinks(new LifeLinkContext(previewLinks.Actors.Select(link =>
                        new LifeLinkActor(link.Id, link.LinkedId, link.Health, link.Health, link.Initialized, link.HasLifeLink,
                            link.TrueFinalBoss, link.OuterBoss)).ToArray(), true, 0)));
                var previewTargets = hasExternalTargetRoster ? units
                    .Where(unit => externalTurnActorIds.Contains(unit.Source.Id) && unit.Source.Id != externalActor?.Id)
                    .Select(unit => (unit.RoomIndex, unit.Freeze())).ToArray() : null;
                var preview = new Engine(copied, new List<CombatEvent>(), externalActor: externalActor,
                    externalActorRoomIndex: externalActor == null ? -1 : actor.RoomIndex, externalTargets: previewTargets,
                    roomRules: roomRules);
                // A sweep preview shares the running-queue gate: its temporary callbacks
                // stay queued rather than healing or damaging between individual targets.
                preview.runningTriggerQueue = runningTriggerQueue;
                preview.holdPreviewTriggerQueue = runningTriggerQueue;
                preview.triggeredCommand = triggeredCommand;
                preview.ordinaryPyreAttack = ordinaryPyreAttack;
                // The temporary victim can already have zero HP. Authenticate its owner in the enclosing room.
                if (ordinaryPyreAttack && actor.Source.IsPyre && source.Units.Any(unit => unit.Id == target.Source.Id &&
                    HivemindTurnUpgrade.IsScaledPyreVictim(source, unit)))
                    preview.observedPyrePreviewVictimId = target.Source.Id;
                preview.Damage(preview.units.Single(unit => unit.Source.Id == actor.Source.Id),
                    preview.units.Single(unit => unit.Source.Id == target.Source.Id), actor.Attack, "Attack");
                if (originalWorld?.AutomaticLifecycle == true) preview.DrainLocalTriggerQueue();
                // RunBossKillPreview removes its temporary dead Pyre before the
                // kill camera. The native removal batch updates auras throughout
                // the train, including effects outside the restored local room.
                if (originalWorld?.AutomaticLifecycle == true && preview.units.Any(unit => unit.Source.IsPyre && !unit.Alive))
                { preview.UpdateEnchantments(); preview.DrainLocalTriggerQueue(); }
                if (preview.unsupportedReason != null) { unsupportedReason = "Boss kill preview: " + preview.unsupportedReason; return; }
                // Native restores characters, but CardStatistics is shared: queries can refresh its
                // membership and SetAttackDamageDealt survives, including nested retaliation.
                context = context.WithStatistics(preview.context!.Statistics);
                // Native previews share relic effect objects. Bone Dog's persistent run flag is
                // restored with the save, but its OnTrigger condition bookkeeping survives.
                context = RelicPyreResurrectionModel.CarryPreviewConditions(context, preview.context);
                int firstPreviewId = context.NextUnitId ?? int.MaxValue;
                int[] unobservedPlayers;
                if (originalWorld?.AutomaticLifecycle == true)
                    preview.context = PreviewBirthModel.ForgetUnobservedBossPlayers(PhysicalRoom(), preview.context,
                        preview.units.Select(unit => unit.Freeze()).ToArray(), out unobservedPlayers);
                else unobservedPlayers = originalWorld == null ? PreviewBirthModel.UnobservedRoomPlayers(PhysicalRoom(),
                    preview.context, preview.units.Select(unit => unit.Freeze())) : Array.Empty<int>();
                EnchantmentWorld? previewWorld = preview.context.Enchantments;
                int[] enemyBirthIds = previewWorld == null ? Array.Empty<int>() : previewWorld.Rooms.SelectMany(room => room.Units)
                    .Concat(previewWorld.RetainedUnits.Select(retained => retained.Unit))
                    .Where(unit => unit.Id >= firstPreviewId && unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToArray();
                BattleSpawnPoints? restoredPoints = context.SpawnPoints?.NextPreviewCopyId.HasValue == true ?
                    BattleSpawnPoints.EndPreview(context.SpawnPoints, preview.context.SpawnPoints!, clearBirths: false,
                        clearBirthUnitIds: enemyBirthIds.Concat(unobservedPlayers).ToArray()) : null;
                if (restoredPoints != null && unobservedPlayers.Length > 0)
                    restoredPoints = restoredPoints.WithState(restoredPoints.Groups,
                        restoredPoints.Units.Where(unit => !unobservedPlayers.Contains(unit.UnitId)).ToArray());
                if (restoredPoints != null) context = context.WithSpawnPoints(restoredPoints);
                if (originalWorld?.AutomaticLifecycle == true)
                {
                    EnchantmentWorld observed = preview.context!.Enchantments!;
                    int firstPreviewCardId = context.NextCardId;
                    // The collected actors restore temporary state. New enemies keep
                    // their preview state until the next complete UI preview turns
                    // HeroManager off, but expose null primary spawn pointers now.
                    // Player births keep both preview state and copied points.
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
                    var restoredActors = temporaryPreviewIds;
                    CombatUnit Restore(CombatUnit unit, int room)
                    {
                        // The local boss-kill preview copies the combat floor only.
                        // Shift callbacks on uncopied actors in the destination change their primary state.
                        if (preview.previewKillBumpShiftedActors?.Contains(unit.Id) == true && !restoredActors.Contains(unit.Id))
                        {
                            CombatUnit? shifted = observed.Rooms[room].Units.SingleOrDefault(candidate => candidate.Id == unit.Id);
                            if (shifted != null) return shifted;
                            unsupportedReason = "Railforged kill preview removed an uncopied actor from the destination room.";
                        }
                        return EnchantmentWorldModel.RestorePreviewEffects(unit, observed, restoredActors.Contains(unit.Id), removedPreviewIds);
                    }
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
                        originalWorld.AllowUpdates, originalWorld.Updating, originalWorld.Preview,
                        context.IsolatedBattlePreview == true ? originalWorld.TestRng : observed.TestRng, true));
                    // Remote Tower targets share effect objects with this
                    // preview too, while their numeric primary state survives.
                    foreach (WorkingUnit remote in units.Where(unit => externalTurnActorIds.Contains(unit.Source.Id)))
                        remote.Apply(EnchantmentWorldModel.RestorePreviewEffects(remote.Freeze(), observed,
                            temporaryPreviewIds.Contains(remote.Source.Id), removedPreviewIds));
                    // A commanded external actor attacks this room without
                    // moving into it. Restore the aura world from physical units.
                    ImportEnchantmentRoom(EnchantmentWorldModel.Refresh(EnchantmentWorldModel.Room(PhysicalRoom(), context)));
                }
                if (preview.units.Any(unit => !unit.Alive && (preview.IsTerminalBossDeath(unit) || unit.Source.IsPyre) &&
                    units.Any(live => live.Alive && live.Source.Id == unit.Source.Id))) ClearTerminalCards();
            }

            private void Damage(WorkingUnit? actor, WorkingUnit target, int damage, string kind, int sourceCardId = 0,
                bool applyTraits = true, bool piercing = false, bool relicSourced = false)
            {
                // 2.2.1 retains self-damage attribution; 2.3.1 explicitly clears
                // it. The new damage-counter snapshot identifies that protocol.
                if (target.LastAttackerId.HasValue) target.LastAttackerId = actor != null && !actor.Source.IsPyre &&
                    (actor != target || context?.Statistics?.MonsterDamage == null) ? actor.Source.Id : 0;
                int raw = damage;
                int blocked = 0;
                int titanSkinSoaked = 0;
                int armorBlockedThisHit = 0;
                int lethalDamagePrevented = 0;
                int lifeLinkPrevented = 0;
                bool direct = kind == "Attack";
                bool attackingTrigger = direct || kind == "Trample";
                if (actor?.Has("spite") == true)
                    damage = SpiteStatusModel.Adjust(actor.Source.Team, actor.Health, actor.Source.MaxHealth, actor.Count("spite"),
                        target.Source.Team, target.Alive, damage);
                if (applyTraits)
                {
                    bool retainedLegacy = context?.CardRegistry == null && legacyDetachedCards != null;
                    CombatContext? scalingContext = retainedLegacy ? context!.WithCardRegistry(legacyDetachedCards) : context;
                    DamageScalingResult scaled = DamageScalingModel.Apply(scalingContext, actor?.Source.SpawnerCardId ?? sourceCardId, sourceCardId, damage,
                        selectedRoom: CurrentRoom(), targetStatuses: target.Statuses.Values, attackerUnitId: actor?.Source.Id ?? 0);
                    if (!scaled.Supported) { unsupportedReason = scaled.UnsupportedReason; return; }
                    context = retainedLegacy ? scaled.Context!.WithCardRegistry(null) : scaled.Context; damage = scaled.Damage;
                }
                damage = Math.Max(0, damage);
                bool cardPiercing = piercing || context?.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.AllDamageIsPiercing)) == true;
                if (damage > 0 && !cardPiercing &&
                    (target.Has("damage shield") || target.Has("spell shield") ||
                     target.Has("armor") || target.Has("titanskin") || target.Has("titanite")))
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
                // The damage coroutine applies the relic after the flat
                // increase stage and before the target's shields.
                if (actor?.Source is { Team: CombatTeam.Player, IsPyre: false } && target.Source.Team == CombatTeam.Enemy &&
                    context?.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.PreventMonsterDamage)) == true)
                    damage = 0;
                // Native resolves Spell Shield before Damage Shield. Both stages
                // check card/relic piercing without the character attacker.
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
                // Both effects occupy the same native stage with order zero.
                // Small native lists retain dictionary order; larger lists
                // sort all active statuses before filtering this stage.
                // Shield-first blocks raw damage; Titanite-first blocks less.
                bool reduceBeforeShield = target.Has("titanite") && target.Has("damage shield") &&
                    (target.RegisteredStatuses().Count(status => status.Stacks > 0) > 16 ? StatusTriggerOrderModel.Order(target.RegisteredStatuses().ToArray()) :
                        target.RegisteredStatuses()).First(status => status.Stacks > 0 &&
                        status.Id is "titanite" or "damage shield").Id == "titanite";
                if (reduceBeforeShield) damage = TitaniteModel.Adjust(damage, cardPiercing);
                if (damage > 0 && target.Has("damage shield") && !cardPiercing)
                {
                    blocked += damage;
                    Trigger(target, "damage shield", 1);
                    damage = 0;
                }
                if (!reduceBeforeShield && target.Has("titanite")) damage = TitaniteModel.Adjust(damage, cardPiercing);
                // Native OnPreAttacked runs after the shield/Titanite stage,
                // before Titanskin and armor. Odd damage rounds before weakness.
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
                // Titanskin, like Titanite and Damage Shield, tests piercing
                // without the character attacker. Unit piercing only bypasses armor.
                if (damage > 0 && target.Has("titanskin") && !cardPiercing)
                {
                    CombatStatus titanSkin = target.Statuses["titanskin"];
                    StatusScalingResult modifier = RelicStatusMagnitudeModel.GetModifier(context, "titanskin",
                        target.Source.Team == CombatTeam.Player ? 2 : 1);
                    if (!modifier.Supported) { unsupportedReason = modifier.UnsupportedReason; return; }
                    context = modifier.Context;
                    int applicableStacks = Math.Min(damage, titanSkin.Stacks);
                    int damageSoak = unchecked(unchecked(titanSkin.ParamInt + modifier.Stacks) * applicableStacks);
                    int beforeSoak = damage;
                    damage = Math.Max(0, unchecked(damage - damageSoak));
                    titanSkinSoaked = unchecked(beforeSoak - damage);
                }
                if (damage > 0 && target.Has("armor") && !cardPiercing && !unitPiercing)
                {
                    int armor = target.Amount("armor");
                    int armorStacks = target.Count("armor");
                    int armorLimit = target.Statuses.TryGetValue("steelguard", out CombatStatus? steelguard) ? steelguard.ParamInt : 0;
                    int applicable = armorLimit > 0 ? Math.Min(armorStacks, armorLimit) : armorStacks;
                    int spent = Math.Min(applicable, damage);
                    int remaining = armorLimit > 0 && armorStacks > applicable ? 0 : Math.Max(0, damage - armor);
                    int armorBlocked = damage - remaining;
                    if (armorLimit > 0) armorBlocked = Math.Min(armorBlocked, unchecked(applicable * target.Statuses["armor"].ParamInt));
                    blocked += armorBlocked;
                    armorBlockedThisHit = armorBlocked;
                    damage = remaining;
                    Trigger(target, "armor", spent);
                }
                if (damage > 0 && target.Has("fragile")) damage = target.Health + damage - 1;
                if (target.Has("lifelink"))
                {
                    if (context?.LifeLinks == null) { unsupportedReason = "Life-link damage requires the global live roster."; return; }
                    LifeLinkDamageResult linked = LifeLinkModel.Test(context.LifeLinks, target.Source.Id, damage);
                    damage = linked.Damage; lifeLinkPrevented = linked.Prevented;
                }
                if (target.Has("tenacious"))
                {
                    bool friendly = TenaciousModel.IsFriendlySource(target.Source.Team, actor?.Source.Team, sourceCardId > 0, relicSourced);
                    TenaciousDamageResult adjusted = TenaciousModel.Adjust(target.Health, damage, friendly);
                    damage = adjusted.Damage; lethalDamagePrevented = adjusted.Prevented;
                    blocked = unchecked(blocked + adjusted.Prevented);
                }
                if (target.Has("untouchable")) { damage = 0; blocked = 0; titanSkinSoaked = 0; }
                if (target.Source.IsPyre && context != null)
                {
                    RelicResourceResult modified = RelicResourceModel.ModifyPyreDamageTaken(context, damage, source.Deployment, source.Preview);
                    if (!modified.Supported) { unsupportedReason = modified.UnsupportedReason; return; }
                    context = modified.Context; damage = modified.Value;
                }
                if (target.Source.DamageHistory != null) target.Source = target.Source.WithDamageHistory(
                    target.Source.DamageHistory.WithModifiers(armorBlockedThisHit, lethalDamagePrevented));
                int hpAfterPyreResurrection = 0;
                if (target.Source.IsPyre && target.Health - damage <= 0 && context != null)
                {
                    context = RelicPyreResurrectionModel.TryApply(context, out float percentage, out string? resurrectionError);
                    if (resurrectionError != null) { unsupportedReason = resurrectionError; return; }
                    if (percentage > 0)
                    {
                        hpAfterPyreResurrection = (int)Math.Ceiling(target.Source.MaxHealth * percentage);
                        damage = 0;
                        blocked = target.Health;
                    }
                }
                int attackDamageDealt = checked(damage + blocked + (context?.Statistics?.MonsterDamage != null ? lifeLinkPrevented : 0));
                if (actor != null && context?.Statistics != null)
                    // 2.3.1 includes LifeLink prevention in attackDamageDealt;
                    // 2.2.1 stores only damageToApply plus damageBlocked.
                    context = context.WithStatistics(context.Statistics.WithLastAttackDamage(attackDamageDealt));
                ApplyDamageOverflow(actor, target, damage, kind, sourceCardId);
                if (unsupportedReason != null) return;
                // Native fixes damage and resulting HP before firing the pre-damage
                // callback. Healing, armor or attack changes there do not recalculate it.
                int previousHealth = target.Health;
                int resultingHealth = Math.Max(0, previousHealth - damage);
                if (attackingTrigger && actor != null && actor != target)
                    FireTriggers(actor, "OnAttackingBeforeDamage", overrideTarget: target, attackDamageDealt: attackDamageDealt);
                if (unsupportedReason != null) return;
                int towerHealthLoss = target.Source.IsPyre ? target.Health - resultingHealth : 0;
                if (!SetHealth(target, resultingHealth)) return;
                if (!source.Preview && towerHealthLoss > 0 && context != null)
                {
                    CombatContext awarded = RelicResourceModel.AwardGoldForPyreLoss(context, towerHealthLoss, source.Deployment);
                    if (!ReferenceEquals(awarded, context))
                    {
                        RelicPyreAttackRefreshResult refreshed = RefreshGold(awarded);
                        if (!refreshed.Supported) { unsupportedReason = refreshed.UnsupportedReason; return; }
                        context = refreshed.Context;
                    }
                }
                if (!source.Preview && target.Source.Team == CombatTeam.Player && !target.Source.IsPyre &&
                    context?.Statistics?.MonsterDamage != null)
                {
                    // Native caps HP damage at the pre-hit health and counts armor,
                    // but excludes shield/Titanskin absorption. An attacker takes
                    // precedence over a card/relic when classifying friendly sources.
                    int sustained = Math.Min(previousHealth, damage);
                    bool hit = damage > 0 || blocked > 0 || titanSkinSoaked > 0;
                    int amount = unchecked(sustained + (hit ? armorBlockedThisHit : 0));
                    bool friendly = actor != null ? actor.Source.Team == CombatTeam.Player : sourceCardId > 0 || relicSourced;
                    int friendlyAmount = hit && friendly ? unchecked(sustained + armorBlockedThisHit + lethalDamagePrevented) : 0;
                    if (amount > 0 || friendlyAmount > 0)
                    {
                        int[] owned = context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard)
                            .Concat(context.OtherPiles?.SelectMany(pile => pile.Cards) ?? Array.Empty<CardToken>())
                            .Select(card => card.InstanceId).Distinct().ToArray();
                        if (owned.Length == 0)
                        {
                            if (context.Statistics.DeckCards == null)
                            { unsupportedReason = "Monster damage statistics require permanent deck membership when all piles are empty."; return; }
                            owned = context.Statistics.DeckCards.ToArray();
                        }
                        // ScalingAddDamage only refreshes UI here; its damage getter reads
                        // the live counter. Other statistic callbacks still need calibration.
                        if (owned.Select(context.FindCard).Any(card => card?.MaskDescriptor?.SourceTraits?.Any(trait =>
                            trait.Type != "CardTraitScalingAddDamage" && trait.EntryDuration != "PreviousTurn" && (amount > 0 && trait.TrackedValue == "DamageDealtToMonsters" ||
                                friendlyAmount > 0 && trait.TrackedValue == "FriendlyDamageDealtToMonsters")) == true))
                        { unsupportedReason = "Monster damage statistic trait callbacks require native calibration."; return; }
                        context = context.WithStatistics(context.Statistics.RecordMonsterDamage(amount, friendlyAmount, owned));
                    }
                }
                if (context != null) context = ConduitModel.Alive(context, target.RoomIndex, target.Freeze());
                if (!source.Preview && target.Source.IsPyre && context?.EnergyState != null)
                    context = context.WithEnergyState(context.EnergyState.With(pyreAlive: target.Alive));
                // Tower HP changes stop the native combat coroutine immediately. A
                // remote character-effect hit or any live room-combat hit therefore
                // never reaches post-hit triggers, CheckForDeath or kill-camera
                // card clearing. Played-card resolution uses a separate queue
                // which StopCombatLoop waits for.
                if (!source.Preview && target.Source.IsPyre && !target.Alive &&
                    (!target.InRoom && runningTriggerQueue || target.InRoom && (kind == "Attack" || runningRoomCombat)) && !nativeCardDamage)
                {
                    stoppedByRemotePyreDamage = true;
                    if (context?.QueryFrame?.SelectedRoomIndex != null)
                        context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: target.RoomIndex));
                    Emit(kind, actor, target, damage);
                    return;
                }
                if (!source.Preview && !target.Alive && target.Count("undying") <= 0 && IsTerminalBossDeath(target))
                { battleWon = true; context = context?.WithBossesDead(); }
                Emit(kind, actor, target, damage);
                if (kind == "Spell" && armorBlockedThisHit > 0 && sourceCardId > 0 &&
                    context?.Relics?.Any(RelicSpellDamageMoveModel.IsOriginal) == true)
                    Emit(RelicSpellDamageMoveModel.SpellArmorDamage, actor, target, armorBlockedThisHit);
                if (attackingTrigger && actor != null && actor != target)
                    FireTriggers(actor, "OnAttacking", overrideTarget: target, attackDamageDealt: attackDamageDealt);
                // Native queues Slay before lifesteal and retaliation, for every damage type
                // with a character attacker, including damage from a character effect.
                if (actor != null && !actor.Despawned && !actor.Removed && !target.Alive) FireTriggers(actor, "OnKill", dyingCharacter: target);
                if (actor != null && !actor.Despawned && !actor.Removed && damage > 0 && actor.Count("avarice") > 0)
                {
                    bool eligible = kind is "Attack" or "Trample";
                    if (!eligible && sourceCardId > 0)
                    {
                        CardInstanceState? sourceCard = context?.FindCard(sourceCardId);
                        if (sourceCard?.MaskDescriptor == null)
                        {
                            unsupportedReason = "Avarice requires captured native damage source card metadata.";
                            return;
                        }
                        // The authored mask describes whether the card spawns an ability owner.
                        // Match the damage source to the activating unit's cached skill card.
                        eligible = sourceCard.MaskDescriptor.Definition.AuthoredUnitAbility ||
                            actor.Source.Ability is { Definition.IsUnitAbility: true } ability &&
                            context?.LastAbilityActivatorUnitId == actor.Source.Id &&
                            sourceCard.DataId == ability.DataId &&
                            context.AbilityCardCache?.Any(entry => entry.InstanceId == sourceCardId && entry.DataId == ability.DataId) == true;
                    }
                    if (eligible && !source.Preview)
                    {
                        if (context == null) { unsupportedReason = "Avarice requires captured battle gold state."; return; }
                        int reward = GoldRewardModel.Adjust(actor.Count("avarice"), roundIncrement: 1,
                            multiplier: context.Resources?.GoldRewardMultiplier ?? 1f);
                        if (reward > 0)
                        {
                            context = context.WithGold(Math.Max(0, unchecked(context.Gold + reward)));
                            RefreshPyreAfterGold();
                            if (unsupportedReason != null) return;
                            Emit("Gold", actor, actor, reward);
                        }
                    }
                }
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
                    if (stoppedByRemotePyreDamage) return;
                }
                if (target.DeathFinished) return;
                int hitType = kind == "Attack" ? 4 : kind == "Trample" ? 6 : kind == "Spikes" ? 1 : kind == "Poison" ? 2 :
                    kind == "Decay" ? 8 : kind == "CorruptPoison" ? 7 : kind == "Corruption" ? 9 : kind == "SpellOverflow" ? 10 : kind == "Chained" ? ChainedStatusModel.DamageType : 0;
                bool tookHit = damage > 0 || blocked > 0 || titanSkinSoaked > 0;
                // The 2.3.1 damage-counter snapshot also identifies its new queue
                // protocol. Bloodshed queues every friendly room actor before
                // executing any callback, including actors without that trigger.
                bool modernDamageQueue = context?.Statistics?.MonsterDamage != null;
                if (modernDamageQueue && tookHit && target.Source.Team == CombatTeam.Player)
                {
                    if (target.Source.DamageHistory != null) target.Source = target.Source.WithDamageHistory(target.Source.DamageHistory.WithTaken(damage));
                    foreach (WorkingUnit friendly in units.Where(unit => unit.InRoom && !unit.Removed &&
                        unit.RoomIndex == target.RoomIndex && unit.Source.Team == CombatTeam.Player).ToArray())
                        QueueCallback(new QueuedCharacterTrigger(friendly.RoomIndex, friendly.Freeze(), "OnBloodshed",
                            paramInt: damage, paramInt2: hitType, paramString: "", overrideTarget: target.Freeze()));
                    if (!runningTriggerQueue && (enqueueCharacterTrigger == null || localUpgradeQueue)) DrainLocalTriggerQueue();
                    if (!runningTriggerQueue && settleDamageCallbacks != null && !SettleExternalDamageCallbacks()) return;
                    if (target.DeathFinished || unsupportedReason != null) return;
                }
                // Chained runs after Bloodshed but before this victim's Revenge.
                // Armor and Tenacious count toward its input; shields and Titanskin
                // do not. Snapshot the enemy roster, then read each holder's live
                // status after the preceding holder's complete damage coroutine.
                int chainedInput = unchecked(damage + armorBlockedThisHit + lethalDamagePrevented);
                if (target.Source.Team == CombatTeam.Player && chainedInput > 0 && hitType != ChainedStatusModel.DamageType)
                {
                    WorkingUnit[] holders = units.Where(unit => unit.InRoom && !unit.Removed && unit.RoomIndex == target.RoomIndex &&
                        unit.Source.Team == CombatTeam.Enemy).ToArray();
                    bool hasChained = holders.Any(unit => ChainedStatusModel.Test(unit.Source.Team, !unit.Alive && unit.Count("undying") <= 0,
                        unit.Count("chained"), target.Source.Team, chainedInput, hitType));
                    if (hasChained && !modernDamageQueue)
                    { unsupportedReason = "Chained requires the native modern damage protocol."; return; }
                    if (hasChained && enqueueCharacterTrigger != null && !localUpgradeQueue && !runningTriggerQueue && settleDamageCallbacks == null)
                    { unsupportedReason = "Chained requires synchronous Bloodshed settlement across the external trigger queue."; return; }
                    foreach (WorkingUnit holder in holders)
                    {
                        if (holder.Removed || holder.Destroyed || !ChainedStatusModel.Test(holder.Source.Team,
                            !holder.Alive && holder.Count("undying") <= 0, holder.Count("chained"),
                            target.Source.Team, chainedInput, hitType)) continue;
                        // The native status asks the damaged friendly to complete
                        // its Undying death-card callbacks before applying damage.
                        if (!target.Alive && target.Count("undying") > 0)
                        {
                            TryRevive(holder, target, 0);
                            if (unsupportedReason != null) return;
                        }
                        if (holder.Alive) Damage(target, holder, ChainedStatusModel.Damage(chainedInput, holder.Count("chained")), "Chained");
                        if (unsupportedReason != null || target.DeathFinished || stoppedByRemotePyreDamage) return;
                    }
                }
                // CharacterState counts Titanskin soak for Revenge, separately
                // from damageBlocked used by last-attack damage statistics.
                if (tookHit)
                {
                    if (target.Source.DamageHistory != null) target.Source = target.Source.WithDamageHistory(target.Source.DamageHistory.WithTaken(damage));
                    FireTriggers(target, "OnHit", paramInt: damage, paramInt2: hitType);
                }
                // Modern CharacterState runs OnHit status effects in the damage
                // coroutine, even while the character-trigger queue is already
                // running. Horde observers therefore precede Punish/Deathwish.
                if (modernDamageQueue && tookHit && !target.DeathFinished && target.Has("horde") && !SettleHordeHealth(target)) return;
                bool friendlySource = actor != null ? actor.Source.Team == CombatTeam.Player : sourceCardId > 0 || relicSourced;
                if (modernDamageQueue && tookHit && friendlySource && !target.DeathFinished)
                    FireTriggers(target, "OnPunish", paramInt: damage, paramInt2: hitType, paramStringOverride: "");
                // A played spell finishes its hit queue while this victim already has its
                // new HP. Only previously finished deaths are eligible for removal here.
                if (nativeCardDamage && !runningTriggerQueue) DrainLocalTriggerQueue();
                if (nativeCardDamage && !runningTriggerQueue && settleDamageCallbacks != null)
                {
                    if (!SettleExternalDamageCallbacks()) return;
                }
                else if (nativeCardDamage && !runningTriggerQueue && finishDamageQueue != null)
                {
                    var frame = new RoomCombatState(source.RoomIndex, source.Deployment,
                        units.Where(unit => !unit.Removed && unit.InRoom).Select(unit => unit.Freeze()).ToArray(),
                        source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());
                    RoomCombatResult drained = finishDamageQueue(frame);
                    if (!drained.Supported) { unsupportedReason = drained.UnsupportedReason; return; }
                    MergeSummon(drained);
                }
                if (target.DeathFinished) return;
                // A lethal hit queues its half-health transition before
                // CheckDeathCardTriggers revives Undying. Those callbacks run
                // against the revived character, after the death signal.
                if (!UpdateHalfHealthDeathwish(target, drain: target.Alive)) return;
                // CharacterState.CheckDeathCardTriggers updates the dying
                // enchanter after character Slay and hit callbacks. Removing a
                // Silence aura earlier incorrectly enables the killer's Slay.
                if (!target.Alive && !target.DeathFinished && target.Count("undying") <= 0 && !UpdateEnchantments(target.Source.Id)) return;
                if (!target.Alive && !target.DeathFinished && sourceCardId > 0 &&
                    context?.FindCard(sourceCardId)?.MaskDescriptor?.KillTriggers?.Count > 0)
                {
                    // Native observes the lethal hit even when Undying restores HP.
                    TryRevive(actor, target, sourceCardId);
                    if (unsupportedReason != null) return;
                    CardInstanceState killSource = context.FindCard(sourceCardId)!;
                    context = CardKillTriggerModel.Apply(context, killSource, source.Preview, source.Deployment,
                        actor?.Source.Id ?? 0, target.Freeze(), units.Select(unit => unit.Freeze()).Concat(
                            context.Enchantments?.Rooms.SelectMany(room => room.Units) ?? Array.Empty<CombatUnit>()).ToArray(),
                        RefreshGold, (current, effect) =>
                        {
                            context = current;
                            bool applied = effect.Type == "AdjustCorruption" ? ApplyCardKillCorruption(effect) :
                                ApplyTriggeredSpawnOrStatus(target, effect, "OnKill", sourceCardId);
                            return (applied ? context : null, unsupportedReason);
                        }, out string? killError);
                    if (killError != null) { unsupportedReason = killError; return; }
                }
                if (!target.Alive && !target.DeathFinished)
                {
                    if (!TryRevive(actor, target, sourceCardId))
                    {
                        // CheckDeathCardTriggers drains its queued half-health
                        // requests before CheckForDeath dispatches death signals.
                        if (enqueueCharacterTrigger == null && !runningTriggerQueue) DrainLocalTriggerQueue();
                        if (unsupportedReason != null || target.DeathFinished || target.Alive) return;
                        // Retaliation finishes inside the outer victim's damage
                        // coroutine. Its dead attacker revives before the native
                        // removal batch updates other enchanters' target maps.
                        WorkingUnit? revivingActor = kind == "Spikes" && actor != null && !actor.Alive && actor.Count("undying") > 0 ? actor : null;
                        Death(actor, target, sourceCardId, deferRemoval: runningTriggerQueue || nativeCardDamage || revivingActor != null,
                            waitForRevival: revivingActor);
                    }
                }
                // CharacterState restores HP after hit/death callbacks, without invoking Heal.
                if (!target.DeathFinished && hpAfterPyreResurrection > 0)
                    target.Health = hpAfterPyreResurrection;
            }

            private bool SettleExternalDamageCallbacks()
            {
                var frame = new RoomCombatState(source.RoomIndex, source.Deployment,
                    units.Where(unit => !unit.Removed && unit.InRoom).Select(unit => unit.Freeze()).ToArray(),
                    source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());
                var checkpoint = new RoomCombatResult(frame, battleWon ? RoomOutcome.BattleWon : RoomOutcome.Exchanged,
                    0, new List<CombatEvent>(), retainedUnits: units.Where(unit => externalTurnActorIds.Contains(unit.Source.Id))
                        .Select(unit => unit.Freeze()).ToArray());
                RoomCombatResult settled = settleDamageCallbacks!(checkpoint);
                if (!settled.Supported) { unsupportedReason = settled.UnsupportedReason; return false; }
                MergeSummon(settled);
                RetireRemovedUnits(frame, settled.State!);
                // The global queue has already completed these physical deaths;
                // do not export another removal request when this engine returns.
                deferredDamageDeaths.RemoveAll(dead => dead.Unit.Removed);
                return true;
            }

            private void RetireRemovedUnits(RoomCombatState before, RoomCombatState after)
            {
                foreach (WorkingUnit previous in units.Where(unit => before.Units.Any(old => old.Id == unit.Source.Id) &&
                    !after.Units.Any(current => current.Id == unit.Source.Id)))
                {
                    previous.DeathFinished = true;
                    previous.Removed = true;
                    previous.InRoom = false;
                }
            }

            private bool ApplyCardKillCorruption(CardActionEffect effect)
            {
                RoomCombatState room = CurrentRoom();
                var train = new TrainCombatState(new[] { room }, Array.Empty<EnemyMovement>(), 1, context);
                // A card's Slay coroutine drains each unit callback before committing
                // the room's corruption, while the lethal victim is still retained.
                bool callbacks = room.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "CorruptionAdded"));
                if (callbacks && (runningTriggerQueue || enqueueCharacterTrigger != null))
                { unsupportedReason = "Nested card Slay corruption queues require native calibration."; return false; }
                if (context?.Relics?.Any(relic => relic.EffectTypes.Any(type => type is
                    "RelicEffectDamageOnCorruptionAdded" or "RelicEffectDrawOnCorruptionAdded" or
                    "RelicEffectAddStatusEffectOnCorruptionAdded")) == true)
                { unsupportedReason = "CorruptionAdded relic callbacks are not modeled."; return false; }
                RoomCorruptionResult changed = RoomCorruptionModel.Apply(train, room.RoomIndex, effect, runUnitCallbacks: true);
                if (!changed.Supported) { unsupportedReason = changed.UnsupportedReason; return false; }
                var before = context!.RoomCapacities!;
                foreach (var addition in changed.Additions)
                    foreach (WorkingUnit actor in units.Where(unit => unit.Alive && unit.InRoom && !unit.Removed &&
                        unit.Triggers.Any(trigger => trigger.Kind == "CorruptionAdded")).ToArray())
                        for (int stack = 0; stack < addition.Count && actor.Alive && !actor.Removed; stack++)
                        {
                            FireTriggers(actor, "CorruptionAdded");
                            if (unsupportedReason != null) return false;
                        }
                if (!before.SequenceEqual(context.RoomCapacities!))
                { unsupportedReason = "Nested corruption callbacks require a fresh native ordering calibration."; return false; }
                context = context.WithRoomCapacities(changed.Context!.RoomCapacities!);
                return true;
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
                        .WithPreviewPrimary(before.DeathState.PreviewPrimary), bumpRules: before.BumpRules, lastFeederId: before.LastFeederId, moonPhaseMask: before.MoonPhaseMask, damageHistory: before.DamageHistory));
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
                QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, revived, "OnDeath", paramInt: 1, paramString: ""));
                if (!UpdateHalfHealthDeathwish(target, drain: false)) return;
                DispatchDeathStatistics(actor, target, sourceCardId, sacrifice: false);
                if (target.Source.DeathState?.StatisticsListenerOnce == true)
                    target.Apply(target.Freeze().WithDeathState(new UnitDeathState(false, target.Source.DeathState.IsBeingRemoved,
                        false, isSacrifice: target.Source.DeathState.IsSacrifice, statisticsListenerOnce: false)));
                // ReviveFromUndyingStatus invokes the same death signal as a
                // final death, including Unstable's persistent explosion listener.
                ExplodeUnstable(target);
                if (unsupportedReason != null) return;
                Emit("Revive", actor, target, 1);
            }

            private void ExplodeUnstable(WorkingUnit target)
            {
                // Native marks food before its OnEaten queue. Its self-damage
                // can kill it there; that death signal still suppresses Unstable.
                if (target.WillDieFromEaten) return;
                int stacks = target.Count("unstable");
                if (stacks <= 0) return;
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

            private readonly List<(WorkingUnit Unit, bool Return, bool RelicDamage, WorkingUnit? WaitForRevival)> deferredDamageDeaths = new List<(WorkingUnit, bool, bool, WorkingUnit?)>();

            private void Death(WorkingUnit? actor, WorkingUnit target, int sourceCardId, bool deferRemoval = false, bool sacrifice = false,
                bool immediateHarvest = false, bool doNotFullyRemove = false, bool returnSpawnerAfterQueue = false,
                WorkingUnit? waitForRevival = null, bool maximumHealthSacrifice = false, bool destroyRoom = false)
            {
                if (target.DeathFinished) return;
                bool hivemind = target.Source.Status(HivemindTurnUpgrade.StatusId) != null;
                bool observedPyrePreviewDeath = source.Preview && sourceCardId == 0 && actor?.Source.IsPyre == true &&
                    observedPyrePreviewVictimId != 0 && observedPyrePreviewVictimId == target.Source.Id;
                bool scaledDeath = hivemind && (observedPyrePreviewDeath ||
                    destroyRoom && HivemindTurnUpgrade.IsScaledRoomDestructionVictim(source, target.Source) ||
                    HivemindTurnUpgrade.IsObservedScaledWaveDeath(source, target.Source, sourceCardId));
                if (hivemind && !scaledDeath &&
                    !HivemindTurnUpgrade.IsObservedInfestedHordeDeath(source, target.Source, sourceCardId) &&
                    !HivemindTurnUpgrade.IsObservedThirdFeedDeath(source, target.Source.Id, actor?.Source.Id) &&
                    !HivemindTurnUpgrade.IsObservedPyreHunterDeath(source, target.Source.Id, actor?.Source.IsPyre == true))
                {
                    unsupportedReason = "Hivemind owner death requires a calibrated native callback.";
                    return;
                }
                if (!source.Preview && target.Health == 0 && TrueFinalBossTerminalCombatModel.SupportsRoot(source, target.Source.Id,
                    plainSpellDamage: plainSpellDamage && actor == null && sourceCardId > 0, triggeredCommand: triggeredCommand,
                    ordinaryPyreAttack: ordinaryPyreAttack && actor?.Source.IsPyre == true, maximumHealthSacrifice: maximumHealthSacrifice))
                {
                    RemoveTrueFinalBossGroup(actor, target, sourceCardId);
                    return;
                }
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
                // Unplayed card damage can stop the native main loop before
                // CheckForDeath/ShowKillCam resumes. Ordinary and TFB unplayed
                // damage both retain the hand at AfterStopCombatLoop.
                bool stoppedBeforeKillCam = target.Source.IsPyre && nativeCardDamage && actor == null && sourceCardId > 0 &&
                    context?.EnergyState?.Phase == "HeroTurn";
                if (!source.Preview && (terminalBossDeath || target.Source.IsPyre) && !stoppedBeforeKillCam) ClearTerminalCards();
                activeCompanionBossIds.Remove(target.Source.Id);
                if (activeCompanionBossIds.Count == 1)
                {
                    int remainingId = activeCompanionBossIds.Single();
                    WorkingUnit? remaining = units.FirstOrDefault(unit => unit.Source.Id == remainingId);
                    if (remaining != null) remaining.Source = remaining.Source.WithEndsBattleOnDeath(true)
                        .WithoutRemovedCompanion(activeCompanionBossIds);
                }
                // At the captured StopCombatLoop boundary, Pyre death has no
                // ordinary unit death accounting/removal; its point stays occupied.
                if (target.Source.IsPyre) return;
                DispatchDeathStatistics(actor, target, sourceCardId, sacrifice);
                ExplodeUnstable(target);
                if (unsupportedReason != null) return;
                if (target.Source.DeathState != null)
                    target.Apply(target.Freeze().WithDeathState(new UnitDeathState(true,
                        target.Source.DeathState.IsBeingRemoved || !deferRemoval || sacrifice,
                        !nativeCardDamage && enqueueCharacterTrigger == null && target.Source.DeathState.HasStatisticsListener,
                        isSacrifice: target.Source.DeathState.IsSacrifice,
                        statisticsListenerOnce: target.Source.DeathState.StatisticsListenerOnce.HasValue ? false : (bool?)null)));
                if (!UpdateEnchantments(target.Source.Id)) return;
                if (!source.Preview && scaledDeath && !RefreshSproutAttackAfterHunterDeath()) return;
                bool wonDuringDeathTrigger = false;
                if (enqueueCharacterTrigger != null) enqueueCharacterTrigger(new QueuedCharacterTrigger(target.RoomIndex, target.Freeze(),
                    returnSpawnerAfterQueue: deferReturn || returnSpawnerAfterQueue, deferUntilRemoval: deferRemoval, harvestAfterDeath: !target.Despawned && !immediateHarvest,
                    completePhysicalRemovalAfterQueue: deferRemoval && context?.SpawnPoints != null, removalLifecycle: true,
                    preservePhysicalPoint: doNotFullyRemove, captureAllDispatches: traceAllDispatches));
                else
                {
                    if (deferRemoval) deferredDamageDeaths.Add((target, deferReturn, false, waitForRevival));
                    else
                    {
                        bool wonBeforeDeathTrigger = battleWon;
                        FireTriggers(target, "OnDeath");
                        wonDuringDeathTrigger = !wonBeforeDeathTrigger && battleWon;
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
                    // A win in this callback stops room combat before the enclosing death clears its point.
                    else if (!stopAfterBossRemoval || !wonDuringDeathTrigger) RemovePhysicalPoint(target);
                    if (!(stopAfterBossRemoval && terminalBossRemoval) && UpdateEnchantments() &&
                        !runningTriggerQueue && enqueueCharacterTrigger == null) DrainLocalTriggerQueue();
                }
            }

            private bool RefreshSproutAttackAfterHunterDeath()
            {
                RoomCombatState current = CurrentRoom();
                WorkingUnit? carrier = units.FirstOrDefault(unit => unit.Alive && unit.InRoom &&
                    HivemindTurnUpgrade.IsObservedSproutCarrier(current, unit.Freeze()));
                if (carrier == null) return true;
                if (context?.Enchantments == null)
                {
                    unsupportedReason = "Missing observed Sprout attack state for the Hunter death callback.";
                    return false;
                }
                int remaining = current.Units.Concat(context.Enchantments.Rooms.Where(room => room.RoomIndex != current.RoomIndex)
                        .SelectMany(room => room.Units)).Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)
                    .Sum(unit => unit.Status(HivemindTurnUpgrade.StatusId)?.Stacks ?? 0);
                // The original room modifier updates the temporary attack before
                // the existing physical death callback dispatches the permanent upgrade.
                CombatUnit? updated = HivemindTurnUpgrade.RefreshSproutAttack(current, carrier.Freeze(), remaining, out string? error);
                if (updated == null) { unsupportedReason = error; return false; }
                carrier.Apply(updated);
                return true;
            }

            private void RemovePhysicalPoint(WorkingUnit unit)
            {
                // Native MonsterManager's preview branch runs InnerCharacterDeath
                // and adds the unit to its ignore list; it keeps the primary point.
                if (source.Preview && (context?.LifeLinks != null || context?.Enchantments?.TemporaryPreviewUnitIds != null)) return;
                unit.Destroyed = true;
                if (context != null) context = ConduitModel.Change(context, unit.RoomIndex, unit.Freeze(), source.Preview, remove: true);
                // HeroManager no longer owns movement after physical removal,
                // including a living treasure actor that despawns. Older room
                // captures can omit lifecycle flags, so Sync cannot infer this
                // from the retained actor's positive health alone.
                if (context?.Enchantments is EnchantmentWorld world && world.Movement.Any(rule => rule.UnitId == unit.Source.Id))
                    context = context.WithEnchantments(new EnchantmentWorld(world.Rooms,
                        world.Movement.Where(rule => rule.UnitId != unit.Source.Id).ToArray(), world.EnemySlotsPerRoom,
                        world.RetainedUnits, world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview,
                        world.TestRng, world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
                if (context?.SpawnPoints == null) return;
                var removed = BattleSpawnPointModel.Apply(context.SpawnPoints, CurrentRoom(), "Remove", unit.Source.Team, unit.Source.Id);
                if (!removed.Supported) { unsupportedReason = removed.Error; return; }
                context = context.WithSpawnPoints(removed.State!);
            }

            private void RemoveTrueFinalBossGroup(WorkingUnit? attacker, WorkingUnit root, int sourceCardId)
            {
                battleWon = true;
                context = context!.WithBossesDead();
                ClearTerminalCards();
                // Original TFB spawning appends RemoveBossActionQueuedVFX after
                // the statistics once-listener. CoroutineSignal<T>.Invoke only
                // advances the final multicast delegate's returned iterator, so
                // the earlier CardStatistics coroutine never executes.
                int[] ids = context.LifeLinks!.Actors.Select(actor => actor.Id).ToArray();
                var roster = context.Enchantments!.Rooms.SelectMany(room => room.Units.Select(unit => (room.RoomIndex, Unit: unit)))
                    .Where(actor => ids.Contains(actor.Unit.Id)).OrderByDescending(actor => actor.RoomIndex).ToArray();
                foreach (var actor in roster)
                {
                    WorkingUnit? unit = units.FirstOrDefault(candidate => candidate.Source.Id == actor.Unit.Id);
                    if (unit == null)
                    {
                        int health = context.LifeLinks!.Actors.Single(link => link.Id == actor.Unit.Id).Health;
                        unit = NewUnit(actor.Unit.WithHealth(health), false, actor.RoomIndex); units.Add(unit);
                        externalTurnActorIds.Add(unit.Source.Id);
                    }
                    if (unit == root && unit.Source.DeathState != null)
                        unit.Apply(unit.Freeze().WithDeathState(new UnitDeathState(true, true, false,
                            isSacrifice: unit.Source.DeathState.IsSacrifice, statisticsListenerOnce: false)));
                    unit.Removed = true;
                    RemovePhysicalPoint(unit);
                }
                EnchantmentWorld world = context.Enchantments!;
                context = context.WithLifeLinks(null).WithEnchantments(new EnchantmentWorld(world.Rooms.Select(room =>
                    new RoomCombatState(room.RoomIndex, room.Deployment, room.Units.Where(unit => !ids.Contains(unit.Id)).ToArray(),
                        room.ExternalInteractions, null, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                    world.Movement.Where(rule => !ids.Contains(rule.UnitId)).ToArray(), world.EnemySlotsPerRoom,
                    world.RetainedUnits.Where(actor => !ids.Contains(actor.Unit.Id)).ToArray(),
                    world.EnchanterIds.Where(id => !ids.Contains(id)).ToArray(), world.AllowUpdates, world.Updating,
                    world.Preview, world.TestRng, world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
                if (context.QueryFrame?.SelectedRoomIndex != null)
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: roster.Last().RoomIndex));
                // Native removal dispatches each body's floor death observers,
                // including ordinary enemies while the encounter is ending.
                foreach (var removed in roster)
                foreach (string stage in HarvestModel.Stages(removed.Unit))
                {
                    HarvestModel.Stage(stage, out string kind, out CombatTeam team);
                    RoomCombatState floor = context.Enchantments!.Rooms.Single(room => room.RoomIndex == removed.RoomIndex);
                    CombatUnit[] observers = floor.Units.Where(unit => unit.Health > 0 && unit.Team == team &&
                        (traceAllDispatches || unit.Triggers.Any(trigger => trigger.Kind == kind))).ToArray();
                    foreach (CombatUnit observer in observers)
                        QueueCallback(new QueuedCharacterTrigger(removed.RoomIndex, observer, kind,
                            // Group cleanup removes the other bodies at their current HP.
                            dyingCharacter: units.Single(unit => unit.Source.Id == removed.Unit.Id).Freeze(),
                            triggerCount: 1, captureAllDispatches: traceAllDispatches));
                    if (enqueueCharacterTrigger == null || localUpgradeQueue) DrainLocalTriggerQueue();
                    if (unsupportedReason != null) return;
                }
                Emit("Death", attacker, root, 0);
            }

            private void DispatchDeathStatistics(WorkingUnit? actor, WorkingUnit target, int sourceCardId, bool sacrifice)
            {
                // Original outer-boss spawning appends RemoveBossActionQueuedVFX
                // after the statistics once-listener. CoroutineSignal<T> advances
                // only the last multicast iterator, masking the earlier callback.
                if (target.Source.BumpRules?.OuterBoss == true && target.Source.DeathState?.StatisticsListenerOnce == true)
                    return;
                if (!source.Preview && context?.Statistics != null && target.Source.DeathState?.HasStatisticsListener != false)
                {
                    int responsible = sourceCardId > 0 ? sourceCardId : target.Source.SpawnerCardId;
                    // An explicit damage source belongs to the death callback. For
                    // an unattributed hit, revival can clear the sacrifice flag
                    // while retaining SacrificeCard.
                    if (sourceCardId <= 0 && actor?.Source.SacrificeCardId > 0 && actor.Source.DeathState?.IsSacrifice != false)
                        responsible = actor.Source.SacrificeCardId.Value;
                    // Native has no IncrementStat call (and no cache refresh) for a null responsible card.
                    BattleStatistics statistics = responsible > 0 ? context.LiveStatistics! : context.Statistics;
                    context = context.WithStatistics(statistics.Death(target.Source.Team == CombatTeam.Player,
                        responsible, requireTrackedCard: context.CardInstances?.Count == 0));
                    // OnCharacterSacrificed calls IncrementStat even for a null
                    // attacking card. That still refreshes ownership after a
                    // burnout boss death; ordinary null-source deaths do not.
                    if (sacrifice)
                        context = context.WithStatistics(context.LiveStatistics!.Increment(sourceCardId, "LastSacrificedMonsterStats",
                            HarvestModel.Count(target.Freeze(), "OnAnyUnitDeathOnFloor"), requireTrackedCard: context.CardInstances?.Count == 0));
                }
            }

            private void SettleDeadSpawner(WorkingUnit unit)
            {
                if (source.Preview || unit.Source.Team != CombatTeam.Player) return;
                if (unit.Source.SpawnerCardId > 0 && !DetachedSpawner(unit) &&
                    context?.OtherPiles?.Any(pile => pile.Name == "Purged" && pile.Cards.Any(card => card.InstanceId == unit.Source.SpawnerCardId)) != true)
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
                if (context.OtherPiles.Any(pile => pile.Name == "Purged" && pile.Cards.Any(card => card.InstanceId == unit.Source.SpawnerCardId))) return;
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
                    context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId, context.Cards.DeckDrawDisabledThisTurn), context.BattleRng, context.Gold,
                    context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics?.ClearHandPlayHistory(),
                    context.CardInstances == null ? null : Array.Empty<CardInstanceState>(), context.CardRegistry, context.AllScenarioBossesDead,
                    context.NextAddedTemporaryUpgrades, context.OtherPiles?.Select(CardPileModel.Clear).ToArray(), context.QueryFrame,
                    context.KillCamActivated.HasValue ? true : (bool?)null, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources, context.MixState, context.ConduitRetained, context.LifeLinks);
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
                            world.EnchanterIds, world.AllowUpdates, world.Updating, world.Preview, world.TestRng, true,
                            world.TemporaryPreviewUnitIds));
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

            internal RoomCombatResult FeedOnly(CombatTeam team, IReadOnlyList<int>? foodIds = null, int triggerCount = 1, int selfFeederId = 0)
            {
                Feed(team, foodIds, triggerCount, units.FirstOrDefault(unit => unit.Source.Id == selfFeederId));
                // Forced feeding settles its selected slots; ordinary DoFeederRules leaves holes for the room phase.
                if (foodIds != null && unsupportedReason == null && context?.SpawnPoints != null) SummonPhysical("Compact", team: team);
                return Finish(RoomOutcome.Exchanged);
            }

            private void Feed()
            {
                Feed(CombatTeam.Player);
                if (unsupportedReason == null) Feed(CombatTeam.Enemy);
            }

            private void Feed(CombatTeam team, IReadOnlyList<int>? foodIds = null, int triggerCount = 1, WorkingUnit? selfFeeder = null)
            {
                if (source.Deployment) return;
                WorkingUnit[] living = units.Where(unit => unit.InRoom && unit.Alive && unit.Source.Team == team).ToArray();
                WorkingUnit[] food = foodIds == null ? living.Where(unit => IsFood(unit.Freeze())).ToArray() :
                    foodIds.Select(id => living.FirstOrDefault(unit => unit.Source.Id == id))
                        .Where(unit => unit != null && IsFood(unit.Freeze())).Cast<WorkingUnit>().ToArray();
                WorkingUnit? feeder = living.FirstOrDefault(unit => !unit.Has("untouchable") && !IsFood(unit.Freeze()));
                if (feeder != null && selfFeeder != null) feeder = selfFeeder;
                if (food.Length == 0 || feeder == null) return;
                if (context?.QueryFrame?.SelectedRoomIndex != null)
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: source.RoomIndex));
                // Native sets every relationship before dispatching any OnEaten callback.
                bool wasRunning = runningTriggerQueue;
                runningTriggerQueue = true;
                foreach (WorkingUnit unit in food)
                {
                    if (unit.Count("eatmany") <= 1) unit.WillDieFromEaten = true;
                    unit.LastFeederId = feeder.Source.Id;
                    for (int repeat = 0; repeat < triggerCount; repeat++) FireTriggers(unit, "OnEaten");
                }
                runningTriggerQueue = wasRunning;
                DrainLocalTriggerQueue();
                if (unsupportedReason != null) return;
                foreach (WorkingUnit unit in food)
                {
                    // Native snapshots lethality before OnPreEaten consumes Eatmany.
                    bool dies = unit.Count("eatmany") <= 1;
                    if (unit.Has("eatmany")) Trigger(unit, "eatmany", 1);
                    if (dies)
                    {
                        unit.Despawned = true;
                        unit.Removed = true;
                        RemovePhysicalPoint(unit);
                        if (!source.Preview && context != null)
                        {
                            if (unit.Source.Team == CombatTeam.Player && unit.Source.SpawnerCardId > 0 && !DetachedSpawner(unit))
                            {
                                CardPileState? standby = context.OtherPiles?.FirstOrDefault(pile => pile.Name == "Standby");
                                CardToken? card = standby?.Cards.FirstOrDefault(item => item.InstanceId == unit.Source.SpawnerCardId);
                                bool resolvingBirth = context.OtherPiles?.Any(pile => pile.Name == "DiscardBuffer" &&
                                    pile.Cards.Any(item => item.InstanceId == unit.Source.SpawnerCardId)) == true;
                                if ((card == null && !resolvingBirth) || context.OtherPiles!.All(pile => pile.Name != "Eaten"))
                                { unsupportedReason = "Eaten source cards require captured Standby and Eaten piles."; return; }
                                if (card != null) context = context.WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name == "Standby" ? CardPileModel.Remove(pile, card.InstanceId) :
                                    pile.Name == "Eaten" ? CardPileModel.Add(pile, card) : pile).ToArray());
                            }
                            context = EquipmentModel.ReturnAttached(context, unit.Freeze());
                        }
                    }
                    Emit("Eaten", feeder, unit, 1);
                    // A surviving Eatmany meal leaves the room roster in place.
                    if (dies && !UpdateEnchantments()) return;
                }
                runningTriggerQueue = true;
                for (int repeat = 0; repeat < unchecked(food.Length * triggerCount); repeat++) FireTriggers(feeder, "OnFeed");
                runningTriggerQueue = wasRunning;
                DrainLocalTriggerQueue();
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
                    foreach (CombatStatus status in unit.RegisteredStatuses()
                        .OrderBy(status => StatusTriggerOrderModel.TriggerOrder(status.Id)).ToArray())
                    {
                        if (!unit.Alive || !unit.Has(status.Id) || !Active(status)) continue;
                        if (status.Id is "poison" or "decay" or "corruption")
                        {
                            string? placementSuppressor = null;
                            if (source.Deployment && status.RemoveWhenTriggered && !status.RemoveDuringDeployment)
                            {
                                RelicStatusDecayResult removal = RelicStatusDecayModel.GetShouldRemove(context, true,
                                    status.Id, unit.Source.Team == CombatTeam.Player ? 2 : 1, source.Preview);
                                if (!removal.Supported) { unsupportedReason = removal.UnsupportedReason; return; }
                                context = removal.Context;
                                if (removal.ShouldRemove) continue;
                                placementSuppressor = removal.SuppressingRelicId;
                            }
                            int amount = StatusMagnitude(unit, status.Id);
                            string? suppressor = Trigger(unit, status.Id, 1);
                            Damage(null, unit, amount, status.Id == "decay" ? "Decay" : status.Id == "corruption" ? "Corruption" : "Poison",
                                relicSourced: status.Id == "poison" && (placementSuppressor != null || suppressor != null));
                        }
                        else if (status.Id == "ephemeral") TriggerEphemeral(unit);
                        else if (status.Id == Witherbloom) WitherbloomDamage(unit, status);
                        else if (status.Id == "unstable" && !unit.Has("blastresistant") &&
                            unit.Count("unstable") >= unit.Health && !unit.Has("untouchable"))
                        {
                            unit.Apply(unit.Freeze().WithSacrifice(0));
                            unit.Health = 0;
                            if (!TryRevive(null, unit, 0)) Death(null, unit, 0, sacrifice: true);
                        }
                        else if (status.Id == "stealth") Trigger(unit, "stealth", 1);
                        // A lethal boss/Pyre status tick stops the native preview
                        // before the next unit's post-combat callbacks. Those
                        // callbacks would otherwise overwrite shared card caches.
                        if (unsupportedReason != null || battleWon || CombatPreviewStopped()) return;
                    }
                }
            }

            internal RoomCombatResult WitherbloomTick(int unitId)
            {
                WorkingUnit? unit = units.FirstOrDefault(actor => actor.Source.Id == unitId);
                if (unit == null) unsupportedReason = "Missing Witherbloom tick actor.";
                else if (unit.Alive && unit.Statuses.TryGetValue(Witherbloom, out CombatStatus? status) && Active(status))
                    WitherbloomDamage(unit, status);
                return Finish(unsupportedReason != null ? RoomOutcome.Unsupported : battleWon ? RoomOutcome.BattleWon :
                    units.Any(actor => actor.Source.IsPyre && !actor.Alive) ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
            }

            private void WitherbloomDamage(WorkingUnit owner, CombatStatus status)
            {
                BattleSpawnPoints? points = context?.SpawnPoints;
                SpawnPointGroupState? players = points?.SelectedGroup(owner.RoomIndex, CombatTeam.Player);
                SpawnPointGroupState? enemies = points?.SelectedGroup(owner.RoomIndex, CombatTeam.Enemy);
                if (!MatchesWitherbloom(status) || players == null || enemies == null)
                {
                    unsupportedReason = "Witherbloom requires its native rule and captured physical room occupancies.";
                    return;
                }
                // Both fronts meet in the middle of the physical room. A dead
                // unit with Undying still occupies a neighbor slot until revival.
                var ordered = new List<WorkingUnit>();
                foreach (int id in players.Occupants.Reverse().Concat(enemies.Occupants).Where(id => id > 0))
                {
                    WorkingUnit? member = units.FirstOrDefault(unit => unit.Source.Id == id);
                    if (member == null)
                    {
                        unsupportedReason = "Witherbloom requires the actor at each occupied point.";
                        return;
                    }
                    if (!member.Destroyed && (member.Alive || member.Count("undying") > 0)) ordered.Add(member);
                }
                int index = ordered.IndexOf(owner);
                if (index < 0) return;
                var targets = new List<WorkingUnit> { owner };
                if (index > 0) targets.Add(ordered[index - 1]);
                if (index + 1 < ordered.Count) targets.Add(ordered[index + 1]);
                foreach (WorkingUnit target in targets)
                {
                    if (!owner.Alive || unsupportedReason != null || CombatPreviewStopped()) return;
                    if (!target.Alive) continue;
                    StatusScalingResult modifier = RelicStatusMagnitudeModel.GetModifier(context, Witherbloom,
                        owner.Source.Team == CombatTeam.Player ? 2 : 1);
                    if (!modifier.Supported) { unsupportedReason = modifier.UnsupportedReason; return; }
                    context = modifier.Context;
                    int amount = unchecked((status.ParamInt + modifier.Stacks) * status.Stacks);
                    Damage(null, target, amount, "Poison");
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
                QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, revived, "OnDeath", paramInt: 1, paramString: ""));
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
                        if (context.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == source.RoomIndex)?.Corruption is not int corruption)
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

            private void Heal(WorkingUnit unit, int amount, string kind, bool fromMaxHealthChange = false, bool triggerOnHeal = true)
            {
                if (!unit.Alive || unit.Source.Modifiers?.CanBeHealed == false) return;
                int modified = HealingModel.ModifiedAmount(amount, unit.Statuses.Values.ToArray(), fromMaxHealthChange);
                if (modified < 0) return;
                int old = unit.Health;
                if (!SetHealth(unit, unit.Health + Math.Min(modified, unit.Source.MaxHealth - unit.Health))) return;
                Emit(kind, unit, unit, unit.Health - old);
                // Native ApplyHeal runs these even when clipping or immunity produces zero restoration.
                if (triggerOnHeal) FireTriggers(unit, "OnHeal", paramInt: unit.Health - old);
                UpdateHalfHealthDeathwish(unit);
            }

            private bool UpdateHalfHealthDeathwish(WorkingUnit unit, bool drain = true)
            {
                if (unsupportedReason != null) return false;
                if (!unit.Triggers.Any(HalfHealthDeathwishModel.IsTrigger)) return true;
                var room = new RoomCombatState(unit.RoomIndex, source.Deployment,
                    new[] { unit.Freeze() }, Array.Empty<string>(), context, source.Preview, CompanionBossSnapshot());
                RoomCombatResult updated = HalfHealthDeathwishModel.Update(room, unit.Source.Id, context);
                if (!updated.Supported) { unsupportedReason = updated.UnsupportedReason; return false; }
                unit.Apply(updated.State!.Units.Single());
                foreach (QueuedCharacterTrigger callback in updated.PendingCallbacks) QueueCallback(callback);
                if (drain && (enqueueCharacterTrigger == null || localUpgradeQueue) && !runningTriggerQueue) DrainLocalTriggerQueue();
                return unsupportedReason == null;
            }

            private bool SetHealth(WorkingUnit target, int health)
            {
                if (context?.LifeLinks?.Actors.Any(actor => actor.Id == target.Source.Id) == true)
                {
                    CombatContext? updated = LinkedBossHealthModel.WithHealth(context, target.Source.Id, health, out string? error,
                        allowTerminal: health == 0 && TrueFinalBossTerminalCombatModel.SupportsRoot(source, target.Source.Id,
                            externalPyreAttack: units.Any(unit => unit.Source.IsPyre && externalTurnActorIds.Contains(unit.Source.Id)),
                            plainSpellDamage: plainSpellDamage, triggeredCommand: triggeredCommand,
                            ordinaryPyreAttack: ordinaryPyreAttack));
                    if (updated == null) { unsupportedReason = error; return false; }
                    context = updated;
                    foreach (WorkingUnit unit in units)
                    {
                        LifeLinkActor? shared = context.LifeLinks!.Actors.SingleOrDefault(actor => actor.Id == unit.Source.Id);
                        if (shared != null) unit.Health = shared.Health;
                    }
                }
                target.Health = health;
                return true;
            }

            private void FireTriggers(WorkingUnit unit, string kind, bool canFireTriggers = true, bool fromQueue = false, int paramInt = 0,
                WorkingUnit? overrideTarget = null, WorkingUnit? dyingCharacter = null, int triggerCount = 1, int lastSpawnedOverrideUnitId = 0,
                int? exclusiveTriggerStateId = null, int paramInt2 = 0, string? paramStringOverride = null, int? attackDamageDealt = null)
            {
                if (stoppedByRemotePyreDamage) return;
                string? paramString = paramStringOverride ?? (kind == "OnHeal" || kind == "OnHit" ? "" : null);
                // CombatManager checks purification when adding to its queue, not
                // when dispatching an already accepted callback.
                if (!fromQueue && unit.Has("purify"))
                {
                    if (context?.PurifyBlockedTriggers == null) { unsupportedReason = "Purify requires captured trigger queue restrictions."; return; }
                    if (context.PurifyBlockedTriggers.Contains(kind)) return;
                }
                if (!fromQueue && enqueueCharacterTrigger != null && !localFeedingQueue && !localUpgradeQueue)
                { enqueueCharacterTrigger(new QueuedCharacterTrigger(unit.RoomIndex, unit.Freeze(), kind, paramInt: paramInt, overrideTarget: overrideTarget?.Freeze(), paramInt2: paramInt2, paramString: paramString,
                    dyingCharacter: dyingCharacter?.Freeze(), canFireTriggers: canFireTriggers, triggerCount: triggerCount, lastSpawnedOverrideUnitId: lastSpawnedOverrideUnitId,
                    admission: CharacterTriggerAdmission.Accepted, exclusiveTriggerStateId: exclusiveTriggerStateId,
                    captureAllDispatches: traceAllDispatches, attackDamageDealt: attackDamageDealt)); return; }
                if (!fromQueue)
                {
                    triggerQueue.Enqueue((unit.RoomIndex, unit, kind, canFireTriggers, paramInt, overrideTarget, paramInt2, paramString, dyingCharacter, triggerCount, lastSpawnedOverrideUnitId, exclusiveTriggerStateId, attackDamageDealt));
                    if (!runningTriggerQueue) DrainLocalTriggerQueue();
                    return;
                }
                bool startedQueue = !runningTriggerQueue;
                runningTriggerQueue = true;
                ExecuteTriggers(unit, kind, canFireTriggers, paramInt, overrideTarget, dyingCharacter, triggerCount, lastSpawnedOverrideUnitId,
                    paramInt2: paramInt2, paramString: paramString, exclusiveTriggerStateId: exclusiveTriggerStateId, attackDamageDealt: attackDamageDealt);
                if (!startedQueue) return;
                DrainLocalTriggerQueue();
            }

            private void DrainLocalTriggerQueue()
            {
                runningTriggerQueue = true;
                while (!holdPreviewTriggerQueue && triggerQueue.Count > 0 && unsupportedReason == null && !stoppedByRemotePyreDamage)
                {
                    var queued = triggerQueue.Dequeue();
                    if (queued.RoomIndex != source.RoomIndex)
                    {
                        RoomCombatState synced = EnchantmentWorldModel.Sync(PhysicalRoom(), units.Where(unit => !unit.Alive || !unit.InRoom)
                            .Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits());
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
                            exclusiveTriggerStateId: queued.ExclusiveTriggerStateId, captureAllDispatches: traceAllDispatches, attackDamageDealt: queued.AttackDamageDealt);
                        RoomCombatResult fired = ApplyQueuedCharacterTrigger(EnchantmentWorldModel.Room(remote, context), callback, QueueCallback);
                        if (!fired.Supported) { unsupportedReason = fired.UnsupportedReason; break; }
                        events.AddRange(fired.Events); dispatches.AddRange(fired.Dispatches);
                        foreach (CombatUnit changed in fired.State!.Units)
                        {
                            WorkingUnit? borrowed = units.FirstOrDefault(unit => unit.Source.Id == changed.Id && externalTurnActorIds.Contains(changed.Id));
                            if (borrowed != null) borrowed.Apply(changed);
                        }
                        ImportEnchantmentRoom(EnchantmentWorldModel.Refresh(EnchantmentWorldModel.Room(PhysicalRoom(), fired.State!.Context)));
                        battleWon |= fired.Outcome == RoomOutcome.BattleWon;
                        continue;
                    }
                    ExecuteTriggers(queued.Unit, queued.Kind, queued.CanFire, queued.ParamInt, queued.OverrideTarget, queued.DyingCharacter,
                        queued.TriggerCount, queued.LastSpawnedOverrideUnitId, queued.ParamInt2, queued.ParamString, queued.ExclusiveTriggerStateId, queued.AttackDamageDealt);
                    if (unsupportedReason == null && queued.Kind == "OnNewStatusEffectAdded")
                    {
                        var callback = new QueuedCharacterTrigger(queued.RoomIndex, queued.Unit.Freeze(), queued.Kind,
                            paramInt: queued.ParamInt, paramInt2: queued.ParamInt2, paramString: queued.ParamString,
                            triggerCount: queued.TriggerCount, lastSpawnedOverrideUnitId: queued.LastSpawnedOverrideUnitId,
                            captureAllDispatches: traceAllDispatches);
                        // A prior strike may have killed a preview-born unit whose
                        // retained world snapshot still has its initial HP. Publish
                        // current deaths before the status callback reimports it.
                        RoomCombatState statusRoom = EnchantmentWorldModel.Sync(PhysicalRoom(),
                            units.Where(unit => !unit.Alive || !unit.InRoom).Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits());
                        RoomCombatResult relicEffect = RelicStatusAddedEffectModel.ApplyAfterTrigger(
                            new RoomCombatResult(statusRoom, RoomOutcome.Exchanged, 0, new List<CombatEvent>()), callback);
                        if (!relicEffect.Supported) { unsupportedReason = relicEffect.UnsupportedReason; break; }
                        ImportEnchantmentRoom(relicEffect.State!);
                        events.AddRange(relicEffect.Events);
                        dispatches.AddRange(relicEffect.Dispatches);
                        foreach (QueuedCharacterTrigger child in relicEffect.PendingCallbacks) QueueCallback(child);
                        battleWon |= relicEffect.Outcome == RoomOutcome.BattleWon;
                    }
                }
                if (stoppedByRemotePyreDamage) { runningTriggerQueue = holdPreviewTriggerQueue; return; }
                var removing = deferredDamageDeaths.Where(dead => dead.WaitForRevival == null || dead.WaitForRevival.Alive)
                    .OrderBy(dead => dead.Unit.Source.Team).ThenBy(dead => dead.Unit.Source.Id).ToArray();
                deferredDamageDeaths.RemoveAll(dead => dead.WaitForRevival == null || dead.WaitForRevival.Alive);
                // Native still removes temporary dead characters while its
                // trigger queue is held; their callbacks stay in the preview queue.
                runningTriggerQueue = holdPreviewTriggerQueue;
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
                    // Heal relic damage also enters this queue during room combat.
                    // The selection UI only compacts when it owns the room visuals;
                    // during combat the vacancy persists until the room has finished.
                    if ((nativeCardDamage && centerDamageDeaths || (source.Preview || !runningRoomCombat) && removing.Any(item => item.RelicDamage)) && ownedPoint &&
                        dead.Equals(removing[removing.Length - 1])) CenterAfterSummon();
                    // HeroManager removal starts GameScreen.EndCombat, which cancels
                    // the combat coroutine after this boss's death callbacks. Later
                    // members of the already-marked batch never fire OnDeath. A played
                    // card instead holds StopCombatLoop until its effects complete.
                    if (stopAfterBossRemoval && !source.Preview && battleWon) break;
                }
                if (removing.Length > 0 && !(stopAfterBossRemoval && battleWon) && UpdateEnchantments() &&
                    triggerQueue.Count > 0 && enqueueCharacterTrigger == null && !holdPreviewTriggerQueue)
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
                    SelectNotificationRoom(currentRoom.Value);
            }

            private bool SettleHordeHealth(WorkingUnit unit)
            {
                RoomCombatResult casualties = HordeStatusModel.SettleHealth(UpgradeRoom(unit), unit.Source.Id);
                if (!casualties.Supported) { unsupportedReason = casualties.UnsupportedReason; return false; }
                context = casualties.State!.Context; unit.Apply(casualties.State.Units.First(actor => actor.Id == unit.Source.Id));
                foreach (QueuedCharacterTrigger callback in casualties.PendingCallbacks) QueueCallback(callback);
                return unsupportedReason == null;
            }

            private void ExecuteTriggers(WorkingUnit unit, string kind, bool canFireTriggers, int paramInt, WorkingUnit? overrideTarget, WorkingUnit? dyingCharacter, int triggerCount, int lastSpawnedOverrideUnitId,
                int paramInt2 = 0, string? paramString = null, int? exclusiveTriggerStateId = null, int? attackDamageDealt = null)
            {
                if (kind == "OnStatusEffectChanged" && paramString == HivemindTurnUpgrade.StatusId &&
                    HivemindTurnUpgrade.NeedsFeedMarker(CurrentRoom(), unit.Freeze()))
                {
                    CardUpgradeModifier marker = HivemindTurnUpgrade.AttackUpgrade(0);
                    if (!ApplyUpgrade(unit, marker, "TemporaryUntilUnitDeath", false, null, 0, kind,
                        directApi: true, upgradeId: marker.DataId)) return;
                }
                if (kind == "OnDeath" && paramInt != 1 && !unit.Despawned && unit.Source.Modifiers?.TroopAura != null)
                {
                    RoomCombatResult cleanup = TroopAuraModel.OwnerKilled(CurrentRoom(), unit.Freeze());
                    if (!cleanup.Supported) { unsupportedReason = cleanup.UnsupportedReason; return; }
                    ImportEnchantmentRoom(cleanup.State!);
                    foreach (var callback in cleanup.PendingCallbacks) QueueCallback(callback);
                }
                if (kind == "OnDeath" && paramInt != 1 && !unit.Despawned && !unit.Alive && RelicOccupiedFloorDrawModel.Has(context))
                {
                    for (int ownerIndex = 0; ownerIndex < context!.Relics!.Count; ownerIndex++)
                    foreach (var effect in context.Relics[ownerIndex].ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                        if (effect.Type == RelicOccupiedFloorDrawModel.Effect && !RefreshOccupiedDraw("Killed", ownerIndex, effect.EffectIndex, unit.Freeze())) return;
                }
                if (traceAllDispatches || unit.Triggers.Any(trigger => trigger.Kind == kind))
                    dispatches.Add(new CharacterTriggerDispatch(unit.Source.Id, kind, dyingCharacter?.Source.Id ?? 0, triggerCount,
                        paramInt, paramInt2, paramString, overrideTarget?.Source.Id ?? 0, lastSpawnedOverrideUnitId, exclusiveTriggerStateId));
                if (kind == "OnDeath" && unit.Despawned) return;
                if (!unit.Alive && kind != "OnDeath" && (unit.Source.IsBoss == true || unit.Source.EndsBattleOnDeath)) return;
                // FireTriggers restores the hit quantity when its queued attack callback runs.
                // Retaliation may have replaced the shared statistic while that callback waited.
                if (attackDamageDealt.HasValue && context?.Statistics != null)
                    context = context.WithStatistics(context.Statistics.WithLastAttackDamage(attackDamageDealt.Value));
                if (kind is "OnSilence" or "OnSilenceLost" && (unit.Has("fullmoon") || unit.Has("newmoon")))
                {
                    // The native moon status owns these callbacks even when the
                    // character has no authored trigger. Removing Silence resets
                    // understanding to zero before rebuilding the effective mask.
                    // In particular this drops the initial None(4) birth bit.
                    CardInstanceState? spawner = context?.FindCard(unit.Source.SpawnerCardId);
                    // Moon statuses queue only the gained/lost callbacks here.
                    // ResetMoonPhaseUnderstanding also updates those callbacks
                    // and card traits, but never queues a global MoonPhaseShift.
                    if (unit.Triggers.Any(trigger => trigger.Kind is "OnMoonLit" or "OnMoonlitLost" or
                            "OnMoonShade" or "OnMoonshadeLost") ||
                        spawner?.MaskDescriptor?.BaseTraits.Concat(spawner.MaskDescriptor.TemporaryTraits)
                            .Any(trait => trait.RuntimeType.Contains("MoonPhase", StringComparison.Ordinal)) == true)
                    { unsupportedReason = "Moon silence callbacks with character or card traits require native calibration."; return; }
                    if (kind == "OnSilenceLost")
                    {
                        if (context?.QueryFrame?.MoonPhaseTrackingActive != true || context.QueryFrame.MoonPhase is not (1 or 2) ||
                            !unit.Source.MoonPhaseMask.HasValue)
                        { unsupportedReason = "Moon silence reset requires captured active phase and prior understanding."; return; }
                        unit.Apply(unit.Freeze().WithMoonPhaseMask(RoomMoonPhaseModel.Effective(context, unit.RoomIndex,
                            context.QueryFrame.MoonPhase.Value), reset: true));
                    }
                }
                if (kind == "OnHit" && context?.Statistics?.MonsterDamage == null && unit.Has("horde") && !SettleHordeHealth(unit)) return;
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
                    if (unit.InRoom && !unit.Removed && !ActionTriggerPassesTest(unit, trigger, lastSpawnedOverrideUnitId, dyingCharacter)) continue;
                    if ((kind == "CorruptionAdded" && trigger.SuppressTriggerNotification == false ||
                        kind == "OnEncounterComplete" && trigger.SuppressTriggerNotification != true) && !source.Preview &&
                        context?.QueryFrame?.SelectedRoomIndex != null)
                        context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: source.RoomIndex));
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
                    int currentCount = TriggerCountModel.Query(context, unit.Freeze(), trigger, out string? countError, RoomTriggerModifier(unit, trigger.Kind));
                    if (countError != null) { unsupportedReason = countError; return; }
                    int fireCount = unchecked(currentCount * triggerCount);
                    bool captureReplay = context?.TriggerCounts?.ReplayHistory?.CapturedKinds.Contains(kind) == true;
                    if (captureReplay && (fireCount != 1 || effects.Any(effect => !BattleTriggerReplayModel.Calibrated(effect))))
                    { unsupportedReason = "Captured replay history requires calibrated stateless single-fire trigger payloads."; return; }
                    for (int fire = 0; fire < fireCount; fire++)
                    {
                        if (!unit.Alive && kind != "OnDeath" && kind != "OnHit" && kind != "OnKill" && kind != "OnBloodshed" && kind != "OnPunish") return;
                        // CharacterState.ApplyEffects passes the host's spawner even
                        // when these trigger effects are bound to equipment.
                        int cacheCardId = unit.Source.SpawnerCardId;
                        CardInstanceState? cacheCard = context?.FindCard(cacheCardId);
                        if (cacheCard?.PlayedRoomUnitIds != null && (!source.Preview ||
                            effects.Any(effect => effect.Type != "CardEffectUnboxJackInTheBoxUnitCard")))
                            context = context!.WithCard(cacheCard.WithPlayedRoomUnits(units.Where(item => item.Alive && item.InRoom && item.Source.IsSpawning != true)
                                .Select(item => item.Source.Id).ToArray()));

                        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                        {
                            if (stoppedByRemotePyreDamage) return;
                            int live = LiveIndex();
                            if (live >= 0) retained = unit.Triggers[live];
                            effects = retained.Effects.ToArray();
                            CombatEffect effect = effects[effectIndex];
                            if (effect.Type == "CardEffectForceRelentless")
                            {
                                if (!source.Preview)
                                {
                                    StatisticQueryFrame? frame = context?.QueryFrame;
                                    ForcedRelentlessCounters? counters = frame?.ForcedRelentless;
                                    if (frame?.Turn == null || counters == null)
                                    { unsupportedReason = "Companion death requires the captured native forced-Relentless counters."; return; }
                                    if (frame.Turn == 0)
                                    { unsupportedReason = "Deployment companion death and early boss creation require separate native calibration."; return; }
                                    var before = new ForcedRelentlessProgress(frame.Turn.Value, counters.WaveCount, 0, counters.RemainingTurns);
                                    ForcedRelentlessProgress after = ForcedRelentlessModel.Apply(before);
                                    context = context!.WithQueryFrame(frame.With(turn: after.Turn,
                                        forcedRelentless: new ForcedRelentlessCounters(counters.WaveCount, after.RemainingTurns)));
                                }
                            }
                            else if (effect.Type == "CardEffectUnboxJackInTheBoxUnitCard")
                            {
                                if (source.Preview) continue;
                                if (!ApplyJackInTheBox(unit, effect, out IReadOnlyList<int>? remaining)) break;
                                if (remaining != null) { effects[effectIndex] = effect.WithBoxedPool(remaining); Store(retained.Fired(effects)); }
                            }
                            else if (effect.Enchantment != null)
                            {
                                if (!UpdateEnchantmentEffect(unit.Source.Id, live >= 0 ? live : index, effectIndex)) break;
                            }
                            else if (effect.Summon != null)
                            {
                                if (effect.Type == TriggeredSummonModel.SpawnHeroIfAlone &&
                                    units.Count(candidate => candidate.InRoom && !candidate.Removed && candidate.Source.Team == CombatTeam.Enemy) > 1)
                                { if (effect.Summon.Tests.CancelSubsequent) break; continue; }
                                if (!TriggeredSummonTest(unit, effect.Summon))
                                { if (effect.Summon.Tests.CancelSubsequent) break; continue; }
                                if (effect.Summon.EnemyHero)
                                { if (!ApplyTriggeredHero(effect.Summon)) break; continue; }
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
                                context = shifted.State!.Context;
                                // Borrowed Tower actors must receive their new phase
                                // before Finish exports them back to other floors.
                                foreach (CombatUnit actor in shifted.State.Rooms.SelectMany(room => room.Units))
                                    units.FirstOrDefault(item => item.Source.Id == actor.Id)?.Apply(actor);
                            }
                            else if (effect.Type == EquipmentPoolGrantModel.NativeEffect)
                            {
                                if (!GrantEquipment(unit, effect)) break;
                            }
                            else if (effect.Action != null)
                            {
                                if (live < 0 && effect.Action.Type == "DrawNextTurn" && retained.StateId == null)
                                { unsupportedReason = "Detached bonus-draw trigger source requires captured persistent identity."; break; }
                                if (!ApplyTriggeredAction(unit, effect, overrideTarget, retained.StateId ?? (live < 0 ? index : live), effectIndex, kind, lastSpawnedOverrideUnitId, retained.IgnoreSilence, dyingCharacter)) break;
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
                                    unit.Despawned = true; RemovePhysicalPoint(unit); Emit("Despawn", unit, unit, 0);
                                    if (!source.Preview && context?.Statistics != null && unit.Source.Team == CombatTeam.Player && !DeferSpawner(unit))
                                        context = context.WithStatistics(context.LiveStatistics!.Increment(unit.Source.SpawnerCardId, "TimesExhausted",
                                            requireTrackedCard: context.CardInstances?.Count == 0));
                                    if (!source.Preview) RouteSpawner(unit);
                                }
                            }
                            else if (effect.Type == RelicGoldLossOnHitModel.Effect)
                            {
                                if (source.Preview || effect.Value < 0 && context?.EnergyState?.PyreAlive != true) continue;
                                int reward = GoldRewardModel.Adjust(effect.Value, multiplier: context!.Resources?.GoldRewardMultiplier ?? 1f);
                                if (reward != 0)
                                {
                                    context = context!.WithGold(Math.Max(0, unchecked(context.Gold + reward)));
                                    RefreshPyreAfterGold();
                                    if (unsupportedReason != null) break;
                                }
                                Emit("Gold", unit, unit, reward);
                            }
                            else if (effect.Type == "CardEffectPurgeUnitOnDeath")
                            {
                                if (source.Preview || unit.Has("cardless")) continue;
                                CombatContext? purged = UnitCardPurgeModel.Apply(DeckChangeSource(), unit.Source.SpawnerCardId, out string? purgeError);
                                if (purged == null) { unsupportedReason = purgeError; break; }
                                ApplyDeckChange(purged);
                            }
                            else if (effect.Type == "CardEffectAdjustDragonsHoard")
                            {
                                if (source.Preview || context?.EnergyState?.PyreAlive != true) continue;
                                StatisticQueryFrame frame = context.QueryFrame!;
                                int amount = Math.Clamp(unchecked(frame.DragonsHoard!.Value + effect.Value), 0,
                                    context.Resources!.DragonsHoardCap!.Value);
                                if (amount != frame.DragonsHoard.Value)
                                    context = context.WithQueryFrame(frame.With(dragonsHoard: amount));
                            }
                            else if (effect.Type == UnitCardCopyModel.Effect && !source.Preview && context != null)
                            {
                                CardGenerationResult copied = UnitCardCopyModel.Apply(DeckChangeSource(), unit.Source.SpawnerCardId, effect);
                                if (!copied.Supported) { unsupportedReason = copied.UnsupportedReason; break; }
                                ApplyDeckChange(copied.Context!);
                                foreach (CardToken card in copied.AddedCards) Emit("AddCard", unit, unit, card.InstanceId);
                            }
                            else if (effect.Type is "CardEffectAddBattleCard" or "CardEffectAddRunCard" && !source.Preview && context != null &&
                                (effect.Type == "CardEffectAddRunCard" || !battleWon && context.AllScenarioBossesDead != true) &&
                                (effect.Generation?.RequireHandSpace != true || context.Cards.Hand.Count < context.MaxHandSize)) AddCards(unit, effect);
                        }
                        if (stoppedByRemotePyreDamage) return;
                        if (effects.Length > 0 && context?.TriggerCounts?.ReplayHistory is BattleTriggerHistory history && history.CapturedKinds.Contains(kind))
                            context = context.WithTriggerCounts(context.TriggerCounts.WithReplayHistory(history.Append(
                                new BattleTriggerBatch(unit.Source.Team, kind, unit.Source.SpawnerCardId, effects))));
                        context = context?.AfterCardEffects();
                    }
                    // The native local trigger object keeps resolving after list removal.
                    // Advancing the original index still observes list shifts and appends.
                    int remainingTriggerIndex = LiveIndex();
                    if (remainingTriggerIndex >= 0) Store(unit.Triggers[remainingTriggerIndex].Fired(unit.Triggers[remainingTriggerIndex].Effects));
                }
                if (unsupportedReason == null && !ApplyRelicUnitUpgradeTriggers(unit, kind, paramInt, paramInt2)) return;
                if (unsupportedReason == null && kind is "AfterSpawnEnchant" or "PostAscension" or "PostDescension" &&
                    context?.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == source.RoomIndex)?.Attachments?
                        .Any(attachment => attachment.EnemyEntry?.Count > 0) == true)
                {
                    RoomCombatResult entry = RoomEnemyEntryModel.Apply(CurrentRoom(), unit.Source.Id, kind);
                    if (!entry.Supported) { unsupportedReason = entry.UnsupportedReason; return; }
                    ImportEnchantmentRoom(entry.State!);
                    foreach (QueuedCharacterTrigger child in entry.PendingCallbacks) QueueCallback(child);
                }
                if (unsupportedReason == null && kind == "OnDeath" && !unit.Source.IsPyre &&
                    (unit.Alive || unit.Source.IsBoss != true && !unit.Source.EndsBattleOnDeath))
                    foreach (RoomCapacityAttachmentState attachment in context?.RoomCapacities?
                        .FirstOrDefault(room => room.RoomIndex == source.RoomIndex)?.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
                    {
                        RoomDeathEffectRule? rule = attachment.DeathEffect;
                        if (rule == null || source.Deployment && rule.DisallowedInDeployment || !RoomDeathEffectModel.Matches(rule, unit.Freeze(), context)) continue;
                        foreach (CardActionEffect effect in rule.Effects)
                            if (!ApplyTriggeredSpawnOrStatus(unit, effect, kind, fromRoomModifier: true)) return;
                    }
                if (unsupportedReason == null && kind == "OnQueuedStatusEffectToAdd" && paramString != null &&
                    units.Any(owner => owner.InRoom && owner.Alive && owner.Source.Id == paramInt2 &&
                        owner.Source.Modifiers?.RoomStatuses?.Any(rule => rule.Type == UnitRoomStatusModel.QueuedStatus && rule.StatusId == paramString) == true))
                {
                    CombatStatus? definition = unit.Freeze().RegisteredStatus(paramString);
                    if (definition == null) { unsupportedReason = "Queued room status requires its registered native definition."; return; }
                    RoomCombatResult applied = StatusApplicationModel.ApplyRetained(CurrentRoom(), unit.Source.Id,
                        definition.WithStacks(paramInt), 0, fromRoomModifier: true);
                    if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return; }
                    ImportEnchantmentRoom(applied.State!);
                    foreach (QueuedCharacterTrigger child in applied.PendingCallbacks) QueueCallback(child);
                }
                if (unsupportedReason == null && kind is "OnTroopAdded" or "OnTroopRemoved" &&
                    units.Any(owner => owner.InRoom && owner.Source.Modifiers?.TroopAura != null))
                {
                    // Harvest can follow completed death signals before their
                    // physical removal. Publish those actors before a troop aura
                    // borrows the world, or its import restores stale listeners.
                    RoomCombatState troopRoom = EnchantmentWorldModel.Sync(CurrentRoom(),
                        units.Where(actor => !actor.Alive || !actor.InRoom).Select(actor => actor.Freeze()).ToArray(), ExternalEnchantmentUnits());
                    RoomCombatResult troop = TroopAuraModel.TroopChanged(troopRoom, unit.Freeze(), kind, paramInt, paramInt2);
                    if (!troop.Supported) { unsupportedReason = troop.UnsupportedReason; return; }
                    CombatUnit? changedTroop = troop.State!.Units.Concat(troop.RetainedUnits).FirstOrDefault(actor => actor.Id == unit.Source.Id);
                    if (changedTroop != null) unit.Apply(changedTroop);
                    ImportEnchantmentRoom(troop.State!);
                    foreach (QueuedCharacterTrigger child in troop.PendingCallbacks) QueueCallback(child);
                }
                if (unsupportedReason == null && kind == "OnSentry" && overrideTarget != null && unit.Source.Modifiers?.Sentry != null)
                {
                    SentryState sentry = unit.Source.Modifiers.Sentry;
                    unit.Apply(unit.Freeze().WithModifiers(unit.Source.Modifiers.WithSentry(new SentryState(
                        sentry.Targets.Concat(new[] { overrideTarget.Source.Id }).Distinct().ToArray(), sentry.IsAttacking))));
                }
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
                CardGenerationResult result = RelicUnitTriggerCardGenerationModel.ApplyOne(
                    effect.CardGeneration?.Permanent == true ? DeckChangeSource() : context!, owner.DataId,
                    effect.EffectIndex, unit.Freeze(), kind, unit.Alive, paramInt, source.Deployment, source.Preview);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                ApplyDeckChange(result.Context!);
                return true;
            }

            private bool ApplyRelicUnitUpgradeTriggers(WorkingUnit unit, string kind, int paramInt, int paramInt2)
            {
                if (context?.Relics == null) return true;
                foreach (CombatRelicState relic in context.Relics)
                for (int effectIndex = 0; effectIndex < relic.EffectTypes.Count; effectIndex++)
                {
                    if (relic.EffectTypes[effectIndex] == RelicModel.ModifyCharacterAttackDamage && kind == "OnSpawn" &&
                        unit.Alive && RelicModel.IsSoulSaviorFirstRegionScaling(relic) &&
                        RelicModel.IsObservedSoulSaviorBossStatTarget(unit.Source))
                    {
                        CombatUnit actor = unit.Freeze();
                        UnitModifiers stats = actor.Modifiers!;
                        unit.Apply(actor.WithModifiers(stats.WithDamageBuff(checked(stats.DamageBuff + 10))).WithRawAttack(stats.AttackDamage));
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicPersistentMonsterHealthModel.Effect)
                    {
                        if (!source.Preview && (kind is "OnHit" or "OnHeal" or "OnDeath"))
                            context = RelicPersistentMonsterHealthModel.Record(context!, unit.Source, kind == "OnDeath" ? 0 : unit.Health);
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicAscensionHealModel.Effect)
                    {
                        if (kind != "PostAscension" || !unit.Alive || !RelicAscensionHealModel.Eligible(unit.Source)) continue;
                        var heal = relic.ResourceEffects!.Single(effect => effect.EffectIndex == effectIndex);
                        if (!source.Preview || heal.CanApplyInPreviewMode)
                            Heal(unit, heal.Amount, "RelicAscensionHeal");
                        if (unsupportedReason != null) return false;
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicDecayDeathModel.Effect)
                    {
                        if (!RelicDecayDeathModel.Test(unit.Freeze(), kind, paramInt2)) continue;
                        RoomCombatResult transferred = RelicDecayDeathModel.Apply(CurrentRoom(), unit.Freeze());
                        if (!transferred.Supported) { unsupportedReason = transferred.UnsupportedReason; return false; }
                        MergeSummon(transferred);
                        foreach (QueuedCharacterTrigger callback in transferred.PendingCallbacks) QueueCallback(callback);
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicUnitSpawnEffectModel.Effect)
                    {
                        RelicConditionEvaluation checkedEffect = RelicUnitSpawnEffectModel.Check(context, unit.Freeze(), kind, source.Preview, relic.DataId);
                        if (!checkedEffect.Supported) { unsupportedReason = checkedEffect.UnsupportedReason; return false; }
                        context = checkedEffect.Context;
                        if (!checkedEffect.Passed) continue;
                        if (relic.DataId == RelicUnitSpawnEffectModel.DevotionDataId)
                        {
                            // This owner's nested action targets only the spawning actor.
                            RoomCombatResult self = RelicUnitSpawnEffectModel.ApplySelf(CurrentRoom(), unit.Source.Id, kind);
                            if (!self.Supported) { unsupportedReason = self.UnsupportedReason; return false; }
                            MergeSummon(self);
                            foreach (QueuedCharacterTrigger callback in self.PendingCallbacks) QueueCallback(callback);
                            continue;
                        }
                        if (relicCharacterEffect == null)
                        { unsupportedReason = "Whelp spawn effects require a complete-train character callback."; return false; }
                        RoomCombatResult applied = relicCharacterEffect(CurrentRoom(), unit.Source.Id, kind);
                        if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return false; }
                        MergeSummon(applied);
                        foreach (QueuedCharacterTrigger callback in applied.PendingCallbacks) QueueCallback(callback);
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicSpawnSilenceModel.Effect)
                    {
                        if (!RelicSpawnSilenceModel.Eligible(unit.Freeze(), kind)) continue;
                        RoomCombatResult silenced = RelicSpawnSilenceModel.ApplyOne(CurrentRoom(), unit.Source.Id, relic);
                        if (!silenced.Supported) { unsupportedReason = silenced.UnsupportedReason; return false; }
                        MergeSummon(silenced);
                        foreach (QueuedCharacterTrigger callback in silenced.PendingCallbacks) QueueCallback(callback);
                        continue;
                    }
                    if (RelicFriendlyTriggerDamageModel.IsEffect(relic.EffectTypes[effectIndex]) ||
                        relic.EffectTypes[effectIndex] == RelicHealDamageModel.Effect)
                    {
                        bool healDamage = relic.EffectTypes[effectIndex] == RelicHealDamageModel.Effect;
                        bool piercing = !healDamage && RelicFriendlyTriggerDamageModel.IsPiercing(context, relic);
                        int damage = healDamage ? paramInt : 5;
                        RoomCombatState frame = CurrentRoom();
                        var train = new TrainCombatState([frame], [], 0, context);
                        int ownerIndex = context.Relics!.Select((owner, index) => (owner, index))
                            .Single(item => item.owner.DataId == relic.DataId).index;
                        RelicCardPlayDamageSelection selected = healDamage
                            ? RelicHealDamageModel.Select(train, unit.Freeze(), kind, ownerIndex, effectIndex)
                            : RelicFriendlyTriggerDamageModel.Select(train, unit.Freeze(), kind, source.RoomIndex, ownerIndex, effectIndex, paramInt);
                        if (!selected.Supported) { unsupportedReason = selected.UnsupportedReason; return false; }
                        foreach (int id in selected.Targets)
                        {
                            if (source.Preview)
                            {
                                // Preview damage belongs to this engine's retained
                                // units and death queue, without physical removal.
                                Damage(null, units.Single(target => target.Source.Id == id), damage, "Spell", applyTraits: false,
                                    piercing: piercing, relicSourced: true);
                                if (unsupportedReason != null) return false;
                                continue;
                            }
                            // The 2.3.1 balance list excludes this ordinary
                            // effect class, so its damage consumes armor.
                            RoomCombatResult damaged = ApplyRelicDamage(CurrentRoom(), id, damage,
                                QueueRelicDamageCallback, piercing: piercing);
                            if (!damaged.Supported) { unsupportedReason = damaged.UnsupportedReason; return false; }
                            MergeSummon(damaged);
                            foreach (QueuedCharacterTrigger callback in damaged.PendingCallbacks)
                                QueueRelicDamageCallback(callback);
                        }
                        continue;
                    }
                    var cardDrawOnHeal = relic.ResourceEffects?.SingleOrDefault(effect =>
                        effect.EffectIndex == effectIndex && RelicCardDrawOnHealModel.IsEffect(effect.Type));
                    if (cardDrawOnHeal != null)
                    {
                        if (!ApplyRelicCardDrawOnHeal(unit, kind, relic, cardDrawOnHeal)) return false;
                        continue;
                    }
                    var devotion = relic.ResourceEffects?.SingleOrDefault(effect =>
                        effect.EffectIndex == effectIndex && effect.Type == RelicDevotionOnDamageModel.Effect);
                    if (devotion != null)
                    {
                        DevotionResult result = RelicDevotionOnDamageModel.ApplyOne(context!, relic.DataId, effectIndex,
                            unit.Freeze(), kind, source.Deployment, source.Preview);
                        if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                        context = result.Context;
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
                    bool killAttack = RelicKillAttackModel.IsOwner(relic);
                    bool trialAttack = ArkionAttackTrial.IsOwner(relic);
                    bool directAttack = killAttack || trialAttack;
                    if (killAttack && kind == "OnKill" && unit.Source.Modifiers == null)
                    { unsupportedReason = "Kill attack requires captured clone/modifier state."; return false; }
                    if (killAttack && !RelicKillAttackModel.Eligible(unit.Freeze(), kind)) continue;
                    if (trialAttack && !ArkionAttackTrial.Eligible(unit.Freeze(), kind)) continue;
                    if (rule.Trigger != kind || rule.Upgrade == null || unit.Source.IsPyre && !killAttack ||
                        !(unit.Source.Team == CombatTeam.Player ? rule.SourcePlayer : rule.SourceEnemy) ||
                        !rule.SubtypeIsNone && !unit.Source.Subtypes.Contains(rule.RequiredSubtype!) ||
                        kind == "OnDeath" && unit.Alive && paramInt != 1) continue;

                    int fireCount;
                    if (directAttack) fireCount = 1;
                    else if (kind == "OnTroopAdded" || kind == "OnTroopRemoved") fireCount = paramInt2;
                    else if (context!.TriggerCounts == null) fireCount = 1;
                    else
                    {
                        var query = new CombatTrigger(kind, false, false, true, 1, Array.Empty<CombatEffect>(), false,
                            noCountModifiersAllowed: false);
                        fireCount = TriggerCountModel.Query(context, unit.Freeze(), query, out string? error, RoomTriggerModifier(unit, kind));
                        if (error != null) { unsupportedReason = error; return false; }
                    }

                    string lifetime = rule.UpgradeLifetime == RelicUnitUpgradeTriggerModel.UntilEndOfBattle
                        ? "TemporaryUntilEndOfBattle" : "TemporaryUntilUnitDeath";
                    for (int fire = 0; fire < fireCount; fire++)
                    {
                        if (!ApplyUpgrade(unit, rule.Upgrade, lifetime, rule.ShouldRemove,
                            RoomCapacityModel.Maximum(context, source.RoomIndex, unit.Source.Team),
                            unit.Source.SpawnerCardId, kind, directApi: true, upgradeId: directAttack ? "" : rule.Upgrade.DataId)) return false;
                        if (rule.UpgradeLifetime != RelicUnitUpgradeTriggerModel.UntilEndOfBattle || source.Preview ||
                            unit.Source.SpawnerCardId <= 0 || directAttack && unit.Source.Modifiers?.SpawnerMatchesDefinition != true) continue;
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
                RelicPyreAttackRefreshResult refreshed = RefreshGold(context);
                if (!refreshed.Supported) { unsupportedReason = refreshed.UnsupportedReason; return; }
                context = refreshed.Context;
            }

            private RelicPyreAttackRefreshResult RefreshGold(CombatContext next)
            {
                WorkingUnit? local = units.FirstOrDefault(unit => unit.Source.IsPyre && !unit.Destroyed);
                RelicPyreAttackRefreshResult refreshed = local != null
                    ? RelicResourceModel.RefreshPyreAttack(next, local.Freeze(), source.Deployment)
                    : refreshPyre?.Invoke(next, source.Deployment) ?? new RelicPyreAttackRefreshResult(next, null);
                if (refreshed.Supported && local != null && refreshed.Pyre != null) local.Apply(refreshed.Pyre);
                return refreshed;
            }

            private SpawnPointReference? SummonSourcePoint(WorkingUnit actor)
            {
                UnitSpawnPointState? point = context!.SpawnPoints!.Units.FirstOrDefault(unit => unit.UnitId == actor.Source.Id);
                return actor.Source.Team != CombatTeam.Player ? null : actor.Health <= 0 ? point?.LastKnown ?? point?.Current : point?.Current;
            }
            private bool TriggeredSummonTest(WorkingUnit actor, TriggeredSummonRule rule)
            {
                if (rule.EnemyHero) return context!.SpawnPoints!.FirstEmpty(source.RoomIndex, CombatTeam.Enemy) >= 0;
                RoomPlayRule room = context!.SummonCatalog!.Rooms.First(item => item.RoomIndex == source.RoomIndex);
                return SummonSourcePoint(actor) != null ? room.Enabled && !room.SummonBlocked :
                    context.SpawnPoints!.FirstEmpty(source.RoomIndex, CombatTeam.Player) >= 0;
            }
            private bool ApplyTriggeredHero(TriggeredSummonRule? rule, int copySourceId = 0)
            {
                RoomCombatState room = EnchantmentWorldModel.Sync(CurrentRoom(),
                    units.Where(unit => !unit.Alive || !unit.InRoom).Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits());
                context = room.Context;
                if (context?.Enchantments is not EnchantmentWorld world)
                { unsupportedReason = "Triggered enemy birth requires the complete native train lifecycle world."; return false; }
                EnemyDefinition? birthDefinition = null;
                if (rule != null)
                {
                    string character = rule.CharacterId;
                    if (rule.Pool.Count > 0)
                    {
                        RngDraw draw = context.BattleRng.Range(0, rule.Pool.Count);
                        context = context.WithBattleRng(draw.State); character = rule.Pool[draw.Value];
                    }
                    birthDefinition = context.SummonCatalog!.Units.Single(unit => unit.CharacterId == character).HeroDefinition;
                }
                else if (unitCopyCatalog == null)
                { unsupportedReason = "Triggered hero copy requires the captured native copy catalog."; return false; }
                room = room.WithContext(context);
                var train = new TrainCombatState(world.Rooms.Select(other => other.RoomIndex == source.RoomIndex ? room :
                    new RoomCombatState(other.RoomIndex, other.Deployment, other.Units, other.ExternalInteractions, context,
                        source.Preview, other.ActiveCompanionBossIds)).ToArray(), world.Movement, world.EnemySlotsPerRoom, context);
                UnitCloneResult born = HeroUnitBirthModel.Apply(train, copySourceId, source.RoomIndex, unitCopyCatalog, copySourceId > 0,
                    Array.Empty<QueuedCharacterTrigger>(), birthDefinition, deferCallbacks: copySourceId == 0);
                if (!born.Supported) { unsupportedReason = born.UnsupportedReason; return false; }
                TrainCombatState synchronized = EnchantmentWorldModel.Rebase(born.State!);
                RoomCombatState changed = synchronized.Rooms.Single(other => other.RoomIndex == source.RoomIndex);
                MergeSummon(new RoomCombatResult(changed, born.Outcome, 0, born.Events.ToList(), pendingCallbacks: born.PendingCallbacks));
                foreach (QueuedCharacterTrigger callback in born.PendingCallbacks) QueueCallback(callback);
                return UpdateEnchantments();
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
            private void QueueRelicDamageCallback(QueuedCharacterTrigger callback)
            {
                if (enqueueCharacterTrigger == null && callback.RemovalLifecycle)
                {
                    WorkingUnit dead = units.Single(target => target.Source.Id == callback.Unit.Id);
                    dead.Apply(callback.Unit); dead.DeathFinished = true;
                    deferredDamageDeaths.Add((dead, callback.ReturnSpawnerAfterQueue, true, null));
                }
                else QueueCallback(callback.WithRelicDamageRemoval());
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
            private bool ApplyJackInTheBox(WorkingUnit actor, CombatEffect effect, out IReadOnlyList<int>? remaining)
            {
                remaining = effect.BoxedPool;
                if (remaining?.Count == 0) return true;
                IReadOnlyList<BoxedCardState> boxed = context!.FindCard(actor.Source.SpawnerCardId)!.Permanent.BoxedCards!;
                if (remaining == null)
                {
                    ShuffleResult<int> shuffled = context.BattleRng.Shuffle(Enumerable.Range(0, boxed.Count).ToArray());
                    context = context.WithBattleRng(shuffled.State);
                    remaining = shuffled.Items.Where(index => context.SummonCatalog!.Cards.Any(card =>
                        card.Creation.DataId == boxed[index].Creation.DataId && card.SpawnCharacterId.Length > 0)).ToArray();
                }
                if (!actor.InRoom || actor.Removed) return true;
                if (context.QueryFrame?.SelectedRoomIndex != null)
                    context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: source.RoomIndex));
                if (!SummonPhysical("Compact")) return false;
                SpawnPointGroupState group = context.SpawnPoints!.SelectedGroup(source.RoomIndex, CombatTeam.Player)!;
                int at = group.Occupants.ToList().IndexOf(actor.Source.Id);
                if (at < 0 || group.Occupants.Take(group.GroupCount).Count(id => id != 0) >= group.GroupCount || remaining.Count == 0) return true;
                RngDraw draw = context.BattleRng.Range(0, remaining.Count);
                context = context.WithBattleRng(draw.State);
                BoxedCardState selected = boxed[remaining[draw.Value]];
                remaining = remaining.Where((_, index) => index != draw.Value).ToArray();
                if (selected.Permanent.BoxedCards?.Count > 0 || selected.Temporary.BoxedCards?.Count > 0 ||
                    selected.Creation.MaskDescriptor?.Definition.MainEffectNames.Contains("CardEffectReplayTrigger") == true ||
                    context.Relics?.Any(relic => relic.CardModifiers?.Count > 0 || relic.CardHealthModifiers?.Count > 0 ||
                        relic.CardAttackModifiers?.Count > 0 || relic.MagicPowerModifiers?.Count > 0 || relic.CardTypeCosts?.Count > 0) == true)
                { unsupportedReason = "Nested Jack boxes or unboxed-card relic upgrades need native calibration."; return false; }
                CardGenerationResult created = CardGenerationModel.CreateDetached(context, selected.Creation);
                if (!created.Supported) { unsupportedReason = created.UnsupportedReason; return false; }
                context = created.Context!;
                int cardId = created.AddedCards.Single().InstanceId;
                context = context.WithCard(context.FindCard(cardId)!.WithModifiers(selected.Permanent, selected.Temporary));
                SummonCardDefinition definition = context.SummonCatalog!.Cards.First(card => card.Creation.DataId == selected.Creation.DataId);
                SummonUnitDefinition unit = context.SummonCatalog.Units.First(item => item.CharacterId == definition.SpawnCharacterId);
                var birthRule = new CardPlayRule(selected.Creation.DataId, unit.Unit.AssetKey, 0, "SpawnMonster", "Standby", unit.Unit, unit.ExternalInteractions);
                var callbacks = new List<QueuedCharacterTrigger>();
                UnitBirthResult born = UnitBirthModel.SpawnQueued(CurrentRoom(), birthRule, cardId, at, true,
                    context.StatusRules.First(status => status.Id == "cardless"), callbacks.Add,
                    units.Select(item => item.Freeze()).ToArray());
                if (!born.Supported) { unsupportedReason = born.Result.UnsupportedReason; return false; }
                MergeSummon(born.Result);
                // A settled post-combat birth also centers surviving enemies after the exchange.
                if (!source.Preview && !SummonPhysical("Compact", team: CombatTeam.Enemy)) return false;
                foreach (var callback in callbacks) QueueCallback(callback);
                return true;
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
                int baseCount = Math.Max(1, rule.Count);
                int requested = baseCount;
                if (rule.CharacterId.Length > 0 && !AddTriggeredSpawnCount(ref requested)) return false;
                if (rule.AdditionalCharacterId.Length > 0)
                {
                    int additionalCount = baseCount;
                    if (!AddTriggeredSpawnCount(ref additionalCount)) return false;
                    requested = unchecked(requested + additionalCount);
                }
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

            private bool AddTriggeredSpawnCount(ref int count)
            {
                RelicResourceResult result = RelicUnitSpawnCountModel.Evaluate(context!, CombatTeam.Player, source.Preview);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                context = result.Context!;
                count = Math.Max(0, unchecked(count + result.Value));
                return true;
            }

            private RoomCombatState CurrentRoom() => new RoomCombatState(source.RoomIndex, source.Deployment,
                units.Where(unit => unit.Alive && unit.InRoom).Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions,
                context, source.Preview, CompanionBossSnapshot());

            private RoomCombatState PhysicalRoom() => new RoomCombatState(source.RoomIndex, source.Deployment,
                units.Where(unit => unit.Alive && unit.InRoom && !externalTurnActorIds.Contains(unit.Source.Id))
                    .Select(unit => unit.Freeze()).ToArray(), source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());

            private CombatUnit[] ExternalEnchantmentUnits() => units.Where(unit => externalTurnActorIds.Contains(unit.Source.Id))
                .Select(unit => unit.Freeze()).ToArray();

            private void ImportEnchantmentRoom(RoomCombatState state)
            {
                context = state.Context;
                foreach (CombatUnit changed in state.Units.Concat(context?.Enchantments?.RetainedUnits
                    .Where(actor => actor.RoomIndex == source.RoomIndex).Select(actor => actor.Unit) ?? Array.Empty<CombatUnit>()))
                {
                    WorkingUnit? current = units.FirstOrDefault(unit => unit.Source.Id == changed.Id);
                    if (current != null) current.Apply(changed);
                }
                // Aura effect objects are shared across the train. Publish their
                // updated caches into borrowed actors before Finish re-syncs them;
                // their primary numeric state must survive preview restoration.
                if (context?.Enchantments is EnchantmentWorld world)
                {
                    WorkingUnit[] shared = units.Where(unit => externalTurnActorIds.Contains(unit.Source.Id) &&
                        unit.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Enchantment != null || effect.Summon != null))).ToArray();
                    if (shared.Length > 0)
                    {
                        var ids = shared.Select(unit => unit.Source.Id).ToHashSet();
                        var observed = world.Rooms.SelectMany(room => room.Units).Concat(world.RetainedUnits.Select(actor => actor.Unit))
                            .Where(unit => ids.Contains(unit.Id)).ToDictionary(unit => unit.Id);
                        foreach (WorkingUnit remote in shared)
                            if (observed.TryGetValue(remote.Source.Id, out CombatUnit? changed))
                                remote.Apply(PreviewEffectsModel.Restore(remote.Freeze(), changed, Array.Empty<int>()));
                    }
                }
            }

            private bool UpdateEnchantments(int? onlySourceId = null)
            {
                RoomCombatState current = EnchantmentWorldModel.Sync(PhysicalRoom(), units.Where(unit => !unit.Alive || !unit.InRoom)
                    .Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits());
                RoomCombatResult result = EnchantmentWorldModel.Update(current, onlySourceId);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                ImportEnchantmentRoom(result.State!);
                foreach (QueuedCharacterTrigger callback in result.PendingCallbacks) QueueCallback(callback);
                return true;
            }

            private bool UpdateEnchantmentEffect(int unitId, int triggerIndex, int effectIndex)
            {
                RoomCombatState current = EnchantmentWorldModel.Sync(PhysicalRoom(), units.Where(unit => !unit.Alive || !unit.InRoom)
                    .Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits());
                RoomCombatResult result = EnchantmentWorldModel.UpdateEffect(current, unitId, triggerIndex, effectIndex);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                ImportEnchantmentRoom(result.State!);
                foreach (QueuedCharacterTrigger callback in result.PendingCallbacks) QueueCallback(callback);
                return true;
            }

            private RoomCombatState UpgradeRoom(WorkingUnit target)
            {
                CombatUnit[] visible = units.Where(unit => unit.Alive && unit.InRoom || unit == target)
                    .Select(unit => unit.Freeze()).ToArray();
                return new RoomCombatState(source.RoomIndex, source.Deployment, visible, InteractionsAfterLocalCasualties(visible),
                    context, source.Preview, CompanionBossSnapshot());
            }

            private IReadOnlyList<string> InteractionsAfterLocalCasualties(IReadOnlyList<CombatUnit> visible)
            {
                // A locally removed owner can be absent from a nested combat snapshot before the enclosing room settles.
                IReadOnlyList<string> interactions = source.ExternalInteractions;
                if (interactions.Any(HivemindTurnUpgrade.IsKnownMarker))
                    interactions = interactions.Where(marker =>
                        !source.Units.Any(unit => HivemindTurnUpgrade.Marker(unit) == marker &&
                            HivemindTurnUpgrade.IsScaledRoomCombatDeath(source, unit) &&
                            units.Any(working => working.Source.Id == unit.Id && !working.Alive)) ||
                        visible.Any(unit => HivemindTurnUpgrade.Marker(unit) == marker)).ToArray();
                return interactions;
            }

            private bool ApplyUpgrade(WorkingUnit target, CardUpgradeModifier upgrade, string lifetime, bool remove,
                int? roomCapacity, int sourceCardId, string? kind, bool directApi = false,
                string upgradeId = "", int? anonymousRemovalIndex = null, int equipmentSourceCardId = 0,
                bool scaleUpgrade = false, bool accumulateScaledUpgradeRecord = true)
            {
                // Removal normally leaves callbacks queued, but restoring a negative
                // healed-HP modifier runs its ApplyHeal callbacks before the next phase.
                // Keep one live queue for the entire operation before exporting it.
                bool ownQueue = enqueueCharacterTrigger != null && !localUpgradeQueue &&
                    units.Any(unit => unit.Triggers.Any(HalfHealthDeathwishModel.IsTrigger));
                if (ownQueue) localUpgradeQueue = true;
                try
                {
                    return ApplyUpgradeCore(target, upgrade, lifetime, remove, roomCapacity, sourceCardId, kind,
                        directApi, upgradeId, anonymousRemovalIndex, equipmentSourceCardId, scaleUpgrade, accumulateScaledUpgradeRecord);
                }
                finally
                {
                    if (ownQueue)
                    {
                        QueuedCharacterTrigger[] remaining = SnapshotLocalCallbacks();
                        triggerQueue.Clear(); localUpgradeQueue = false;
                        foreach (QueuedCharacterTrigger callback in remaining) enqueueCharacterTrigger!(callback);
                    }
                }
            }

            private RoomCombatResult UpgradeHealthPhase(RoomCombatState state, WorkingUnit target, int delta, bool decreaseHp, bool heal)
            {
                context = state.Context;
                target.Apply(state.Units.Single(unit => unit.Id == target.Source.Id));
                int maximum = delta < 0 ? Math.Max(0, checked(target.Source.MaxHealth + delta)) :
                    Math.Min(99999, checked(target.Source.MaxHealth + delta));
                int hp = delta < 0 ? (decreaseHp ? Math.Max(0, checked(target.Health + delta)) : Math.Min(target.Health, maximum)) : target.Health;
                if (hp <= 0)
                    return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(),
                        "Half-health Deathwish lethal upgrade phases require native sacrifice/revival calibration.");
                target.Apply(target.Freeze().WithHealthAndMaxHealth(hp, maximum));
                if (delta > 0 && heal) Heal(target, delta, "UpgradeMaxHealthHeal", fromMaxHealthChange: true, triggerOnHeal: false);
                UpdateHalfHealthDeathwish(target, drain: false);
                return new RoomCombatResult(unsupportedReason == null ? UpgradeRoom(target) : null,
                    unsupportedReason == null ? RoomOutcome.Exchanged : RoomOutcome.Unsupported, 0, new List<CombatEvent>(), unsupportedReason);
            }

            private bool ApplyUpgradeCore(WorkingUnit target, CardUpgradeModifier upgrade, string lifetime, bool remove,
                int? roomCapacity, int sourceCardId, string? kind, bool directApi,
                string upgradeId, int? anonymousRemovalIndex, int equipmentSourceCardId,
                bool scaleUpgrade, bool accumulateScaledUpgradeRecord)
            {
                RoomCombatState upgradeRoom = UpgradeRoom(target);
                RoomCombatResult result = UnitModifierModel.ApplyWithSettlement(upgradeRoom, target.Source.Id, upgrade, lifetime,
                    remove, roomCapacity, sourceCardId, kind, (state, changed, sacrifice) =>
                    {
                        context = state.Context;
                        foreach (WorkingUnit other in units.Where(unit => unit.Source.Id != target.Source.Id && unit.InRoom))
                        {
                            CombatUnit? live = state.Units.FirstOrDefault(unit => unit.Id == other.Source.Id);
                            if (live != null) other.Apply(live);
                            // OnAttacking can upgrade the attacker between lethal
                            // damage and the victim's Undying revival. A dying unit
                            // excluded from this settlement was not removed by it.
                            else if (upgradeRoom.Units.Any(unit => unit.Id == other.Source.Id))
                            { other.InRoom = false; other.Removed = true; }
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
                        if (!remove && (enqueueCharacterTrigger == null || localUpgradeQueue) && !runningTriggerQueue) DrainLocalTriggerQueue();
                        if (unsupportedReason != null)
                            return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), unsupportedReason);
                        // The native effect retains its target object through removal and
                        // terminal clearing. Its remaining source-card work must still run.
                        return new RoomCombatResult(UpgradeRoom(target), RoomOutcome.Exchanged, 0, new List<CombatEvent>());
                    }, allowDyingTarget: true, enqueueCallback: QueueCallback, directApi: directApi,
                    upgradeId: upgradeId, anonymousRemovalIndex: anonymousRemovalIndex, equipmentSourceCardId: equipmentSourceCardId,
                    deferAbilityCallbacks: !directApi || deferAbilityCallbacks || runningTriggerQueue, scaleUpgrade: scaleUpgrade,
                    accumulateScaledUpgradeRecord: accumulateScaledUpgradeRecord,
                    changeHealth: (state, delta, decreaseHp, heal) => UpgradeHealthPhase(state, target, delta, decreaseHp, heal));
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return false; }
                context = result.State!.Context;
                return true;
            }

            private CardTargets TriggerTargets(WorkingUnit actor, CardActionEffect action, bool testing, WorkingUnit? overrideTarget = null, int lastSpawnedOverrideUnitId = 0)
            {
                // This native effect selects rooms directly and never consumes character targets.
                if (action.Type == "AdjustCorruption") return new CardTargets(Array.Empty<int>());
                if (action.Target == "Pyre")
                    return new CardTargets(units.Where(unit => unit.Source.IsPyre).Take(1)
                        .Select(unit => unit.Source.Id).ToArray());
                if (action.Target is "Tower" or "FrontInAllRooms")
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
                if (action.Target == "LastAttackerCharacter")
                {
                    if (!actor.LastAttackerId.HasValue) return new CardTargets(Array.Empty<int>(), "Last-attacker targeting requires captured attacker relationships.");
                    if (actor.LastAttackerId == 0) return new CardTargets(Array.Empty<int>());
                    WorkingUnit? attacker = units.FirstOrDefault(unit => unit.Source.Id == actor.LastAttackerId);
                    if (attacker == null) return new CardTargets(Array.Empty<int>(), "Last attacker is outside the captured actor world.");
                    // Native checks only IgnoreBosses, bypassing team/health/status/subtype filters.
                    if (action.Filters?.IgnoreBosses == true)
                    {
                        if (!attacker.Source.IsBoss.HasValue) return new CardTargets(Array.Empty<int>(), "Last-attacker boss filtering requires captured boss state.");
                        if (attacker.Source.IsBoss.Value) return new CardTargets(Array.Empty<int>());
                    }
                    return new CardTargets(new[] { attacker.Source.Id });
                }
                if (action.Target == "LastFeederCharacter")
                {
                    if (!actor.LastFeederId.HasValue) return new CardTargets(Array.Empty<int>(), "Last-feeder targeting requires captured feeder relationships.");
                    if (actor.LastFeederId == 0) return new CardTargets(Array.Empty<int>());
                    WorkingUnit? feeder = units.FirstOrDefault(unit => unit.Source.Id == actor.LastFeederId);
                    return feeder == null ? new CardTargets(Array.Empty<int>(), "Last feeder is outside the captured actor world.") :
                        new CardTargets(new[] { feeder.Source.Id });
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
                if (action.Target != "Self") return CardTargetModel.Collect(CurrentRoom(), action, Array.Empty<int>(), isTesting: testing || source.Preview,
                    selfUnitId: actor.Source.Id, lastSpawnedOverrideUnitId: lastSpawnedOverrideUnitId);
                // Self bypasses teams/status/subtype/untouchable. Modern native
                // target helpers additionally honor the captured health filter.
                if (action.Filters?.MatchesSelfHealth(actor.Health, actor.Source.MaxHealth) == false)
                    return new CardTargets(Array.Empty<int>());
                if (action.Filters?.IgnoreBosses == true)
                {
                    if (!actor.Source.IsBoss.HasValue) return new CardTargets(Array.Empty<int>(), "Self boss filtering requires boss state.");
                    if (actor.Source.IsBoss.Value) return new CardTargets(Array.Empty<int>());
                }
                return new CardTargets(new[] { actor.Source.Id });
            }

            private bool ActionTriggerPassesTest(WorkingUnit actor, CombatTrigger trigger, int lastSpawnedOverrideUnitId,
                WorkingUnit? dyingCharacter = null)
            {
                if (!trigger.Effects.Any(effect => effect.UnitUpgrade != null || effect.Action != null || effect.Summon != null || effect.Enchantment != null || effect.Generation != null ||
                    effect.Type == "CardEffectUnboxJackInTheBoxUnitCard")) return true;
                bool passed = trigger.Effects.Count == 0;
                foreach (CombatEffect effect in trigger.Effects)
                {
                    if (effect.Type == "CardEffectUnboxJackInTheBoxUnitCard")
                    {
                        passed |= !source.Preview && context?.AllScenarioBossesDead != true && effect.BoxedPool?.Count != 0;
                        continue;
                    }
                    if (effect.Enchantment != null)
                    {
                        // Native Enchant auras bind during initialization. An
                        // effect with ShouldTest=false does not admit its trigger
                        // or mark HasTriggered when the ordinary queue visits it.
                        if (effect.Enchantment.Targeting.Tests?.ShouldTest != false) passed = true;
                        continue;
                    }
                    if (effect.Summon != null)
                    {
                        if (!effect.Summon.Tests.ShouldTest) continue;
                        bool summonValid = TriggeredSummonTest(actor, effect.Summon);
                        if (!summonValid && effect.Summon.Tests.FailToCast) return false;
                        passed |= summonValid;
                        continue;
                    }
                    if (effect.Type == EquipmentPoolGrantModel.NativeEffect)
                    {
                        if (source.Preview || !actor.Alive) continue;
                        if (actor.Source.EquipmentCards?.Count != 0 || actor.Source.Modifiers?.EquipmentLimit is not > 0)
                        {
                            unsupportedReason = "Equipment grant requires an original unequipped host with a free equipment slot.";
                            return false;
                        }
                        passed = true;
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
                    bool valid;
                    if (action.Type == "UpgradeDeckSpells") valid = !source.Preview && !battleWon && context?.AllScenarioBossesDead != true;
                    else if (action.Type == "Recursion") valid = true; // Native TestEffect delegates to the unconditional base for RequiresRoomInHand=false.
                    else if (action.Type == "ScheduleNextDrawUpgrade") valid = true;
                    else if (action.Type == "SpawnOrStatus") valid = SpawnOrStatusCanApply(actor, action, trigger.Kind);
                    else if (TriggeredStatusTransferModel.IsLifemotherDeath(actor.Source, trigger.Kind, action)) valid = dyingCharacter != null;
                    else
                    {
                        CardTargets targets = TriggerTargets(actor, action, testing: true, lastSpawnedOverrideUnitId: lastSpawnedOverrideUnitId);
                        if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                        if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                        valid = effect.Type == OwnerDamageReactionModel.NativeEffect
                            ? actor.Source.DamageHistory!.DamageTakenBeforeArmor > 0 && targets.UnitIds.Count > 0
                            : effect.Type is "CardEffectAddStatusEffectIfNoPlayerUnits" or "CardEffectPyreDamageIfNoPlayerUnits"
                            ? NoPlayerUnitsInActorRoom(actor)
                            : OuterBossDefinitions.IsUnrulySniperAdvance(actor.Source, trigger, effect)
                            ? units.Any(target => target.Alive && target.InRoom && !target.Destroyed &&
                                targets.UnitIds.Contains(target.Source.Id))
                            : action.Type is "AddStatus" or DevotionStatusModel.Effect ? StatusTest(actor, effect, targets) : ActionTestValid(action, targets, TestActionAmount(action));
                    }
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
                if (action.Type == DevotionModel.UnitUpgrade && context!.QueryFrame!.Devotion!.Level < 1 ||
                    action.Type != "RemoveUnitUpgrade" && tested.UnitIds.Count == 0)
                    return action.Tests?.CancelSubsequent != true;
                CardTargets targets = TriggerTargets(actor, action, testing: false, overrideTarget, lastSpawnedOverrideUnitId);
                if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                int repeats = action.Type == DevotionModel.UnitUpgrade ? Math.Max(0, context!.QueryFrame!.Devotion!.Level) :
                    action.Type == "UnitUpgradePerHoard" ? Math.Max(0, context!.QueryFrame!.DragonsHoard!.Value) * targets.UnitIds.Count :
                    action.Type == "UnitUpgradePerStatus" ? Math.Max(0, action.Statuses.Sum(status => actor.Count(status.Id))) : 1;
                for (int repetition = 0; repetition < repeats; repetition++)
                foreach (int targetId in targets.UnitIds)
                {
                    WorkingUnit? target = units.FirstOrDefault(unit => unit.Source.Id == targetId && (unit.Alive || action.Target == "Self" && unit == actor || action.Target == "LastAttackedCharacter"));
                    if (target == null) continue;
                    if (action.Type == "UnitUpgradeOnMoonPhase" && !UpgradeMoonMatches(action, target)) continue;
                    if (action.Type == "BuffEquipment")
                    {
                        if (!ApplyTriggeredEquipmentBuff(actor, target, action, kind, parentEquipmentId)) return false;
                        continue;
                    }
                    if (action.Type == MissingHealthUnitUpgradeModel.Effect)
                    {
                        int count = Math.Max(0, target.Source.MaxHealth - target.Health) / action.Value;
                        if (count == 0) continue;
                        CardUpgradeModifier scaled = MissingHealthUnitUpgradeModel.Scale(action.Upgrade!, count);
                        // This native effect calls ApplyCardUpgrade/AccumulateCardScale
                        // directly and never runs source-card unit-upgrade scaling traits.
                        if (!ApplyUpgrade(target, scaled, action.Lifetime, false, null, 0, kind, scaleUpgrade: true)) return false;
                        Emit(action.Type, actor, target, count);
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

            private bool UpgradeMoonMatches(CardActionEffect action, WorkingUnit unit) => action.Value == 0 ||
                (RoomMoonPhaseModel.Effective(context, unit.RoomIndex, context!.QueryFrame!.MoonPhase!.Value) & action.Value) != 0;

            private bool ActionTestValid(CardActionEffect action, CardTargets targets, int amount = 0) =>
                action.Type == "SelfFeederRules" ? !source.Deployment :
                action.Type == "RemoveEquipment" ? targets.UnitIds.Any(id => units.Any(unit => unit.Source.Id == id && unit.Source.EquipmentCards?.Count > 0)) :
                action.Type == "DamagePerSourceAttack" ? targets.UnitIds.Count > 0 :
                action.Type == DevotionModel.UnitUpgrade ? targets.UnitIds.Count > 0 && context?.QueryFrame?.Devotion?.Level > 0 :
                action.Type == "UnitUpgradePerHoard" ? targets.UnitIds.Count > 0 && context?.QueryFrame?.DragonsHoard > 0 :
                action.Type == "UnitUpgradeOnMoonPhase" ? targets.UnitIds.Any(id => units.Any(unit => unit.Source.Id == id && UpgradeMoonMatches(action, unit))) :
                action.Type == "HealTrain" ? context?.EnergyState?.PyreAlive == true &&
                    (!source.Preview || context.RoomCapacities?.Any(room => room.RoomIndex == source.RoomIndex && room.IsPyre) == true) :
                action.Type == "PlayUnitTrigger" ? true :
                action.Type == "AdjustCorruption" ? RoomCorruptionModel.Test(CurrentRoom(), action) :
                action.Type is "AdjustMaxHealth" or "BuffHealth" ? true :
                action.Type is "BuffAttack" or "DebuffAttack" ? targets.UnitIds.Any(id => units.Any(unit => unit.Source.Id == id && unit.Alive && unit.Source.CanAttack)) :
                action.Type == "Null" ? targets.UnitIds.Count > 0 || action.NullTargetless :
                action.Type == "TransferAllStatusesFromUnit" ? targets.UnitIds.Count > 0 && TriggeredStatusTransferModel.Validate(action) == null :
                action.Type == "RemoveStatus" ? StatusRemovalModel.Test(CurrentRoom(), action, targets.UnitIds) :
                action.Type == "AdjustAllStatusEffects" ? AllStatusAdjustmentTest(action, targets) :
                AbilityCooldownModel.IsEffect(action.Type) || AbilityLifecycleModel.IsEffect(action.Type) ? true :
                action.Type == "AdjustCapacity" ? !source.Preview && !battleWon && context!.AllScenarioBossesDead != true &&
                    RoomCapacityModel.Test(context, source.RoomIndex, units.Any(unit => unit.InRoom && unit.Source.Team == CombatTeam.Enemy), action.OnlyIfNoEnemies) :
                action.Type == ForgePointsModel.Effect ? ForgePointsModel.Test(ForgePointsPyreAlive()) :
                action.Type == DevotionModel.Effect ? !source.Preview && DevotionModel.Test(context, action) :
                action.Type is "DrawNextTurn" or "DrawType" ? !source.Preview && !battleWon && context!.AllScenarioBossesDead != true :
                EnergyModel.IsEffect(action.Type) ? !source.Preview && !battleWon && context!.AllScenarioBossesDead != true && EnergyModel.Test(context, action.Type) :
                action.Type == "Damage" ? amount >= 0 && (action.Range == null || action.Range.Max > 0) &&
                    (action.Target != "DropTargetCharacter" || targets.UnitIds.Count > 0) :
                action.Type == "RemoveUnitUpgrade" || action.Type == "Heal" && action.Target == "Room" || targets.UnitIds.Count > 0;

            private bool AllStatusAdjustmentTest(CardActionEffect action, CardTargets targets)
            {
                if (action.Tests?.StrictTargets != true) return true;
                int count = CardSpellModel.TestCount(new TrainCombatState(new[] { CurrentRoom() },
                    Array.Empty<EnemyMovement>(), 4, context), action, targets);
                if (count < 0) { unsupportedReason = "All-status adjustment requires complete propagation metadata."; return false; }
                return count > 0;
            }

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

            private CombatUnit[] SpawnOrStatusTargets(WorkingUnit actor, CardActionEffect action) => units
                .Where(unit => unit.Source.Team == CombatTeam.Player && unit.InRoom && !unit.Removed && !unit.Despawned &&
                    !unit.Source.IsPyre && unit.Source.IsBoss != true &&
                    (unit.Alive || unit.Count("endless") > 0 || unit.Count("undying") > 0) &&
                    (string.IsNullOrEmpty(action.Filters?.Subtype) || unit.Source.Subtypes.Contains(action.Filters.Subtype)))
                .Select(unit => unit.Freeze()).ToArray();

            private bool SpawnOrStatusCanApply(WorkingUnit actor, CardActionEffect action, string triggerKind)
            {
                RoomCapacityState? room = context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == source.RoomIndex);
                if (room == null || context?.SpawnPoints == null)
                { unsupportedReason = "Triggered spawn-or-status requires captured room and physical spawn-point state."; return false; }
                if (room.IsPyre) return false;
                if (SpawnOrStatusTargets(actor, action).Length > 0) return true;
                SpawnPointGroupState? points = context.SpawnPoints.SelectedGroup(source.RoomIndex, CombatTeam.Player);
                if (points == null || points.GroupCount == 0) return false;
                bool dyingMonster = triggerKind is "OnDeath" or "OnKill" && actor.Source.Team == CombatTeam.Player && !actor.Alive &&
                    actor.Source.IsPyre == false && actor.Source.IsBoss != true;
                // Native ignores capacity here and inserts at the front, moving
                // existing occupants when any physical slot is available.
                return dyingMonster || points.Occupants.Any(id => id == 0);
            }

            private bool ApplyTriggeredSpawnOrStatus(WorkingUnit actor, CardActionEffect action, string triggerKind, int sourceCardIdOverride = 0,
                bool fromRoomModifier = false)
            {
                if (!SpawnOrStatusCanApply(actor, action, triggerKind))
                    return unsupportedReason == null && action.Tests?.CancelSubsequent != true;
                if (action.Statuses.Count == 0 || action.SpawnFallbackUnit == null)
                { unsupportedReason = "Triggered spawn-or-status requires its status and fallback unit definitions."; return false; }

                CombatStatus status = action.Statuses[0];
                int statusSourceCardId = fromRoomModifier ? 0 : sourceCardIdOverride > 0 ? sourceCardIdOverride : actor.Source.SpawnerCardId;
                if (action.Statuses.Count > 1)
                {
                    RngDraw draw = context!.BattleRng.Range(0, action.Statuses.Count);
                    context = context.WithBattleRng(draw.State);
                    status = action.Statuses[draw.Value];
                }
                RelicResourceResult spawnCount = RelicUnitSpawnCountModel.Evaluate(context!, CombatTeam.Player, source.Preview);
                if (!spawnCount.Supported) { unsupportedReason = spawnCount.UnsupportedReason; return false; }
                context = spawnCount.Context!;
                status = RelicUnitSpawnCountModel.ScaleStatus(status, spawnCount.Value);
                CombatUnit[] targets = SpawnOrStatusTargets(actor, action);
                if (targets.Length > 0)
                {
                    RoomCombatResult applied = StatusApplicationModel.ApplyRetained(RetainedStatusRoom(actor,
                        new[] { targets[0].Id }), targets[0].Id, status, statusSourceCardId,
                        fromEffectType: "CardEffectSpawnMonsterOrAddStatusEffect",
                        allowStackModifiers: !action.DisallowStatusStackModifiers);
                    if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return false; }
                    context = applied.State!.Context;
                    foreach (CombatUnit changed in applied.State.Units)
                    {
                        WorkingUnit? current = units.FirstOrDefault(unit => unit.Source.Id == changed.Id);
                        current?.Apply(changed);
                    }
                    foreach (QueuedCharacterTrigger callback in applied.PendingCallbacks) QueueCallback(callback);
                    foreach (CombatEvent item in applied.Events) events.Add(new CombatEvent(round, item.Kind, item.Actor, item.Target, item.Amount));
                    battleWon |= applied.Outcome == RoomOutcome.BattleWon;
                    RoomCombatState frame = CurrentRoom();
                    if (RoomSpawnCopyModel.Active(frame))
                    {
                        UnitCloneResult copied = RoomSpawnCopyModel.ExistingHorde(new TrainCombatState(new[] { frame },
                            Array.Empty<EnemyMovement>(), 0, frame.Context), targets[0].Id);
                        if (!copied.Supported) { unsupportedReason = copied.UnsupportedReason; return false; }
                        MergeSummon(new RoomCombatResult(copied.State!.Rooms.Single(), copied.Outcome, 0, copied.Events.ToList()));
                        foreach (var callback in copied.PendingCallbacks) QueueCallback(callback);
                    }
                    return true;
                }

                var definition = new CardPlayRule("triggered-cardless-spawn:" + action.SpawnFallbackUnit.AssetKey,
                    "CardlessTriggeredSpawn", 0, "SpawnMonster", "Room", action.SpawnFallbackUnit,
                    Array.Empty<string>(), cardType: "Monster");
                var cardless = new CombatStatus("cardless", 1, triggerVfxEnemy: false, triggerVfxPlayer: false,
                    stackable: false, hidden: false, displayCategory: "Persistent",
                    propagatable: status.Propagatable.HasValue ? false : null,
                    excludeHeroPropagation: status.ExcludeHeroPropagation.HasValue ? false : null,
                    excludeMonsterPropagation: status.ExcludeMonsterPropagation.HasValue ? false : null);
                var additional = new SpawnStatusApplication(status, statusSourceCardId, !action.DisallowStatusStackModifiers,
                    "CardEffectSpawnMonsterOrAddStatusEffect");
                bool canFreeDyingActor = triggerKind is "OnDeath" or "OnKill" && actor.Source.Team == CombatTeam.Player && !actor.Alive &&
                    actor.Source.IsPyre == false && actor.Source.IsBoss != true && context!.SpawnPoints!.Units
                        .FirstOrDefault(unit => unit.UnitId == actor.Source.Id)?.Current != null;
                UnitBirthResult born = UnitBirthModel.SpawnWithStatus(CurrentRoom(), definition, 0, 0, cardless, additional,
                    // A copied point can still reference a removed/dead actor until
                    // this birth shifts and compacts it. Keep its captured lifecycle.
                    positionActors: units.Select(unit => unit.Freeze()).ToArray());
                if (!born.Supported && canFreeDyingActor)
                {
                    RemovePhysicalPoint(actor);
                    if (unsupportedReason != null) return false;
                    born = UnitBirthModel.SpawnWithStatus(CurrentRoom(), definition, 0, 0, cardless, additional);
                }
                if (!born.Supported) { unsupportedReason = born.Result.UnsupportedReason; return false; }
                context = born.Result.State!.Context;
                if (context?.SpawnPoints != null)
                {
                    var physicalUnits = units.Select(unit => unit.Freeze()).ToList();
                    var knownPhysicalIds = physicalUnits.Select(unit => unit.Id).ToHashSet();
                    physicalUnits.AddRange(born.Result.State.Units.Where(unit => knownPhysicalIds.Add(unit.Id)));
                    var physicalRoom = new RoomCombatState(source.RoomIndex, source.Deployment, physicalUnits,
                        source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());
                    BattleSpawnPointResult compacted = BattleSpawnPointModel.Apply(context.SpawnPoints,
                        physicalRoom, "Compact", CombatTeam.Enemy);
                    if (!compacted.Supported) { unsupportedReason = compacted.Error; return false; }
                    context = context.WithSpawnPoints(compacted.State!);
                }
                foreach (CombatUnit changed in born.Result.State.Units)
                {
                    WorkingUnit? current = units.FirstOrDefault(unit => unit.Source.Id == changed.Id);
                    if (current == null) units.Add(NewUnit(changed, true));
                    else current.Apply(changed);
                }
                var roomUnits = born.Result.State.Units.Select(changed => units.Single(unit => unit.Source.Id == changed.Id)).ToArray();
                var retainedUnits = units.Where(unit => !born.Result.State.Units.Any(changed => changed.Id == unit.Source.Id)).ToArray();
                units.Clear(); units.AddRange(roomUnits); units.AddRange(retainedUnits);
                foreach (QueuedCharacterTrigger callback in born.Result.PendingCallbacks) QueueCallback(callback);
                foreach (CombatEvent item in born.Result.Events) events.Add(new CombatEvent(round, item.Kind, item.Actor, item.Target, item.Amount));
                battleWon |= born.Result.Outcome == RoomOutcome.BattleWon;
                return true;
            }

            private bool ApplyRailforgedKillBump(WorkingUnit actor, CardActionEffect action)
            {
                if (roomRules == null || context?.Enchantments is not { AutomaticLifecycle: true } world || actor.RoomIndex != source.RoomIndex)
                {
                    unsupportedReason = "Railforged kill movement requires the captured destination room and its rules.";
                    return false;
                }
                RoomCombatState frame = EnchantmentWorldModel.Sync(PhysicalRoom(), units.Where(unit => !unit.Alive || !unit.InRoom)
                    .Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits());
                var train = new TrainCombatState(frame.Context!.Enchantments!.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                    room.Deployment, room.Units, room.ExternalInteractions, frame.Context, source.Preview, room.ActiveCompanionBossIds))
                    .ToArray(), world.Movement, world.EnemySlotsPerRoom, frame.Context);
                BumpResult moved = BumpModel.Apply(train, new[] { actor.Source.Id }, action.Value, roomRules, 0, null,
                    deferCharacterCallbacks: true);
                if (!moved.Supported) { unsupportedReason = moved.UnsupportedReason; return false; }
                RoomCombatState? destination = moved.State!.Rooms.SingleOrDefault(room => room.Units.Any(unit => unit.Id == actor.Source.Id));
                if (destination == null)
                { unsupportedReason = "Railforged kill movement lost its original actor."; return false; }
                actor.Apply(destination.Units.Single(unit => unit.Id == actor.Source.Id));
                if (destination.RoomIndex != source.RoomIndex)
                {
                    actor.MoveToRoom(destination.RoomIndex);
                    externalTurnActorIds.Add(actor.Source.Id);
                }
                MergeSummon(new RoomCombatResult(moved.State.Rooms[source.RoomIndex], moved.Outcome, 0, moved.Events.ToList()));
                foreach (QueuedCharacterTrigger callback in moved.PendingCallbacks)
                {
                    if (world.TemporaryPreviewUnitIds is { } copied && destination.RoomIndex != source.RoomIndex &&
                        callback.RoomIndex == destination.RoomIndex && !copied.Contains(callback.Unit.Id) && callback.Kind == "OnShift" &&
                        callback.Unit.Triggers.Where(trigger => trigger.Kind == "OnShift").ToArray() is { Length: > 0 } shifts)
                    {
                        if (shifts is not [var shift] || !IsRailforgedKillPreviewShift(callback.Unit, shift))
                        { unsupportedReason = "Railforged kill preview requires a calibrated uncopied destination Shift effect."; return false; }
                        (previewKillBumpShiftedActors ??= new HashSet<int>()).Add(callback.Unit.Id);
                    }
                    QueueCallback(callback);
                }
                return true;
            }

            private bool ApplyTriggeredAction(WorkingUnit actor, CombatEffect effect, WorkingUnit? overrideTarget, int triggerIndex, int effectIndex, string triggerKind, int lastSpawnedOverrideUnitId, bool hiddenTrigger, WorkingUnit? dyingCharacter)
            {
                CardActionEffect action = effect.Action!;
                if (TriggeredStatusTransferModel.IsLifemotherDeath(actor.Source, triggerKind, action))
                {
                    if (dyingCharacter == null) return true;
                    RoomCombatResult transfer = TriggeredStatusTransferModel.ApplyLifemotherDeath(
                        RetainedStatusRoom(actor, new[] { dyingCharacter.Source.Id }), actor.Freeze(), dyingCharacter.Freeze(), action);
                    if (!transfer.Supported) { unsupportedReason = transfer.UnsupportedReason; return false; }
                    context = transfer.State!.Context;
                    foreach (CombatUnit changed in transfer.State.Units) units.Single(unit => unit.Source.Id == changed.Id).Apply(changed);
                    foreach (QueuedCharacterTrigger callback in transfer.PendingCallbacks) QueueCallback(callback);
                    events.AddRange(transfer.Events);
                    return true;
                }
                if (action.Type == "Bump")
                {
                    if (triggerKind == "OnKill" && actor.Triggers.Any(trigger => trigger.StateId == triggerIndex &&
                        IsRailforgedAttackerKillBump(actor.Source, trigger)))
                    {
                        // The captured bottom-floor attempt keeps the actor in place,
                        // so the following native Armor effect still applies to it.
                        if (actor.RoomIndex == 0 && !actor.Has("rooted") &&
                            context?.Enchantments is { EnchanterIds.Count: 0, AllowUpdates: true, Updating: false } &&
                            !actor.Source.Triggers.Any(trigger => trigger.Kind == "PostAttemptedDescension")) return true;
                        return ApplyRailforgedKillBump(actor, action);
                    }
                    if (birthBump == null || source.Preview || source.RoomIndex != 0 || triggerKind != "OnSpawnNotFromCard")
                    { unsupportedReason = "Harvester birth movement requires the complete native spawn train and room rules."; return false; }
                    BumpResult moved = birthBump(CurrentRoom(), action, actor.Source.Id);
                    if (!moved.Supported) { unsupportedReason = moved.UnsupportedReason; return false; }
                    RoomCombatState changed = moved.State!.Rooms.Single(room => room.RoomIndex == source.RoomIndex);
                    foreach (WorkingUnit borrowed in units.Where(unit => externalTurnActorIds.Contains(unit.Source.Id)).ToArray())
                    {
                        RoomCombatState? location = moved.State.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == borrowed.Source.Id));
                        CombatUnit? current = location?.Units.First(unit => unit.Id == borrowed.Source.Id) ??
                            moved.RetainedUnits.FirstOrDefault(unit => unit.Id == borrowed.Source.Id);
                        if (current != null && location != null && location.RoomIndex != borrowed.RoomIndex)
                        {
                            units.Remove(borrowed);
                            units.Add(NewUnit(current, location.RoomIndex == source.RoomIndex, location.RoomIndex));
                            if (location.RoomIndex == source.RoomIndex) externalTurnActorIds.Remove(borrowed.Source.Id);
                        }
                        else if (current != null) borrowed.Apply(current);
                    }
                    // The original caster remains on the bottom floor. New arrivals
                    // must enter this engine before the following rearrangement.
                    foreach (WorkingUnit prior in units.Where(unit => unit.Alive && unit.InRoom &&
                        changed.Units.All(current => current.Id != unit.Source.Id)))
                    {
                        CombatUnit? retained = moved.RetainedUnits.FirstOrDefault(unit => unit.Id == prior.Source.Id);
                        if (retained != null) prior.Apply(retained);
                        prior.InRoom = false;
                    }
                    MergeSummon(new RoomCombatResult(changed, moved.Outcome, 0, moved.Events.ToList()));
                    foreach (QueuedCharacterTrigger callback in moved.PendingCallbacks) QueueCallback(callback);
                    return true;
                }
                if (action.Type == "ScheduleNextDrawUpgrade")
                {
                    if (!source.Preview && !battleWon && context?.AllScenarioBossesDead != true)
                        context = NextDrawUpgradeModel.Schedule(context!, action.NextDrawUpgrade!);
                    return true;
                }
                if (action.Type == "Recursion")
                {
                    if (source.Preview || battleWon || context?.AllScenarioBossesDead == true) return true;
                    if (actor.Source.SpawnerCardId <= 0 || context?.FindCard(actor.Source.SpawnerCardId) == null)
                    { unsupportedReason = "Triggered recursion requires the actor's captured spawner card."; return false; }
                    CardRecursionResult restored = CardRecursionModel.Apply(context!.Cards, context.OtherPiles,
                        context.BattleRng, action, null, actor.Source.SpawnerCardId, context.MaxHandSize,
                        cardRegistry: context.CardRegistry);
                    if (!restored.Supported) { unsupportedReason = restored.UnsupportedReason; return false; }
                    BattleStatistics? statistics = context.Statistics;
                    foreach (int id in restored.RestoredCardIds.Where(id => restored.Cards!.Hand.Any(card => card.InstanceId == id)))
                        statistics = statistics?.Increment(id, "TimesDrawn");
                    context = context.WithCards(restored.Cards!).WithBattleRng(restored.BattleRng!.Value).WithStatistics(statistics);
                    if (restored.OtherPiles != null) context = context.WithOtherPiles(restored.OtherPiles);
                    foreach (CardInstanceState card in restored.ChangedCards) context = context.WithCard(card);
                    foreach (int id in restored.RestoredCardIds)
                    {
                        CardInstanceState? card = context.FindCard(id);
                        if (action.Target is "Exhaust" or "Eaten" && card != null &&
                            (card.ExhaustState != null || CardRetainModel.Present(card))) context = context.WithCard(CardExhaustModel.ResetTraits(card));
                        if (restored.Cards!.Hand.Any(token => token.InstanceId == id))
                        {
                            var handCallback = RelicHandCardModel.CardAdded(context, id);
                            if (!handCallback.Supported) { unsupportedReason = handCallback.UnsupportedReason; return false; }
                            context = handCallback.Context!;
                        }
                        Emit("Recursion", actor, actor, id);
                    }
                    return true;
                }
                if (action.Type == "UpgradeDeckSpells")
                {
                    if (source.Preview || battleWon || context?.AllScenarioBossesDead == true) return true;
                    DeckSpellUpgradeResult upgraded = DeckSpellUpgradeModel.Apply(context!, action, null);
                    if (!upgraded.Supported) { unsupportedReason = upgraded.UnsupportedReason; return false; }
                    context = upgraded.Context;
                    foreach (int id in upgraded.AppliedCardIds) Emit("UpgradeDeckSpells", actor, actor, id);
                    return true;
                }
                if (action.Type == "SpawnOrStatus") return ApplyTriggeredSpawnOrStatus(actor, action, triggerKind);
                if (action.Type == "DrawType")
                {
                    if (source.Preview || battleWon || context?.AllScenarioBossesDead == true) return true;
                    string? drawError = TriggeredTypedDrawModel.Validate(context, effect);
                    if (drawError != null) { unsupportedReason = drawError; return false; }
                    // Character trigger effects have no parent/played card. Native draws
                    // use that null source rather than attributing them to the unit card.
                    TypedDrawResult drawn = TypedDrawModel.Draw(context!, action.TypedDraw!, SampleActionAmount(action), 0);
                    if (!drawn.Supported) { unsupportedReason = drawn.UnsupportedReason; return false; }
                    context = drawn.Context;
                    foreach (int id in drawn.DrawnCardIds) Emit("DrawType", actor, actor, id);
                    return true;
                }
                if (action.Type == "AdjustCorruption")
                {
                    RoomCombatState room = CurrentRoom();
                    if (!RoomCorruptionModel.Test(room, action)) return action.Tests?.CancelSubsequent != true;
                    RoomCombatState[] floors = new[] { room };
                    if (action.Target == "Tower")
                    {
                        if (context?.Enchantments == null)
                        { unsupportedReason = "Tower corruption requires complete captured rooms for callback validation."; return false; }
                        floors = context.Enchantments.Rooms.Select(other => other.RoomIndex == source.RoomIndex ? room :
                            new RoomCombatState(other.RoomIndex, other.Deployment, other.Units, other.ExternalInteractions, context, source.Preview)).ToArray();
                        if (context.RoomCapacities!.Any(capacity => !floors.Any(floor => floor.RoomIndex == capacity.RoomIndex)))
                        { unsupportedReason = "Tower corruption is missing a captured floor."; return false; }
                    }
                    var train = new TrainCombatState(floors, Array.Empty<EnemyMovement>(), 1, context);
                    bool unitCallbacks = room.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "CorruptionAdded"));
                    bool championAttack = action.Target == "Room" && action.CorruptionAdd == true && action.Value == 1 &&
                        actor.Source.AssetKey == "MonsterWurmChampion1" && triggerKind == "OnAttacking" && runningTriggerQueue;
                    // The native Strike runs in HeroTurn; previews retain the
                    // relic's separate CanApplyInPreviewMode gate. Inactive
                    // status callbacks neither choose targets nor consume RNG.
                    bool inactiveRelicStatuses = context?.Relics?.All(relic =>
                        !relic.EffectTypes.Contains(RelicModel.AddStatusOnCorruption) ||
                        RelicCorruptionStatusModel.Validate(relic) == null && relic.CorruptionStatuses!.All(rule =>
                            source.Preview ? !rule.CanApplyInPreview : rule.OnlyDuringMonsterTurn)) != false;
                    RoomCorruptionResult changed = RoomCorruptionModel.Apply(train, source.RoomIndex, action,
                        runUnitCallbacks: championAttack && inactiveRelicStatuses);
                    if (!changed.Supported) { unsupportedReason = changed.UnsupportedReason; return false; }
                    if (unitCallbacks && championAttack)
                    {
                        // QueueAndRunTrigger does not recursively drain an active native
                        // queue. Commit room corruption before the queued Inspire callbacks.
                        var capacities = context!.RoomCapacities!;
                        foreach (var addition in changed.Additions)
                            foreach (WorkingUnit target in units.Where(unit => unit.InRoom && unit.Alive && !unit.Removed &&
                                unit.Triggers.Any(trigger => trigger.Kind == "CorruptionAdded")).ToArray())
                                for (int stack = 0; stack < addition.Count; stack++) FireTriggers(target, "CorruptionAdded");
                        if (unsupportedReason != null) return false;
                        if (!capacities.SequenceEqual(context.RoomCapacities!))
                        { unsupportedReason = "Nested corruption callbacks require a fresh native ordering calibration."; return false; }
                        context = context.WithRoomCapacities(changed.Context!.RoomCapacities!);
                    }
                    else context = changed.Context;
                    return true;
                }
                if (action.Type == "AdjustPyreMaxHealth")
                {
                    if (source.Preview) return true;
                    WorkingUnit? pyre = units.SingleOrDefault(unit => unit.Source.IsPyre);
                    if (context?.Resources == null || pyre == null)
                    { unsupportedReason = "Permanent Pyre health requires the live Pyre and raw maximum health."; return false; }
                    context = context.WithResources(context.Resources.WithBasePyreMaxHealth(
                        Math.Max(1, unchecked(context.Resources.BasePyreMaxHealth + action.Value))));
                    RelicResourceResult maximum = RelicResourceModel.GetPyreMaxHealth(context);
                    if (!maximum.Supported) { unsupportedReason = maximum.UnsupportedReason; return false; }
                    // SaveManager's new maximum does not rewrite the combat
                    // character's initialized max-health field at this boundary.
                    pyre.Health = Math.Max(0, Math.Min(maximum.Value, unchecked(pyre.Health + action.Value)));
                    return true;
                }
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
                bool valid = effect.Type == OwnerDamageReactionModel.NativeEffect
                    ? actor.Source.DamageHistory!.DamageTakenBeforeArmor > 0 && tested.UnitIds.Count > 0
                    : effect.Type is "CardEffectAddStatusEffectIfNoPlayerUnits" or "CardEffectPyreDamageIfNoPlayerUnits"
                    ? NoPlayerUnitsInActorRoom(actor)
                    : action.Type is "AddStatus" or DevotionStatusModel.Effect ? StatusTest(actor, effect, tested) : ActionTestValid(action, tested, TestActionAmount(action));
                if (unsupportedReason != null) return false;
                if (!valid) return action.Tests?.CancelSubsequent != true;
                CardTargets targets = TriggerTargets(actor, action, testing: false, overrideTarget, lastSpawnedOverrideUnitId);
                if (!targets.Supported) { unsupportedReason = targets.UnsupportedReason; return false; }
                if (targets.BattleRng.HasValue) context = context!.WithBattleRng(targets.BattleRng.Value);
                if (action.Type == "CopyUnits")
                    return ApplyTriggeredHero(null, targets.UnitIds.Single());
                if (action.Type == "SynergizeStatuses")
                {
                    foreach (int id in targets.UnitIds.Reverse())
                    {
                        WorkingUnit target = units.Single(unit => unit.Source.Id == id);
                        int count = target.Source.Modifiers!.SynergizeStatuses!.FirstOrDefault(value => value.Key == action.Statuses[0].Id)?.Value ?? 0;
                        if (count > 0)
                        {
                            int synergyAmount = action.Value == 1 ? Math.Max(1, action.Statuses[1].Stacks) : count;
                            RoomCombatResult added = StatusApplicationModel.ApplyRetained(CurrentRoom(), id,
                                action.Statuses[1].WithStacks(synergyAmount), actor.Source.SpawnerCardId,
                                fromEffectType: "CardEffectSynergizeStatusEffects");
                            if (!added.Supported) { unsupportedReason = added.UnsupportedReason; return false; }
                            ImportEnchantmentRoom(added.State!);
                            foreach (QueuedCharacterTrigger child in added.PendingCallbacks) QueueCallback(child);
                        }
                        target.Apply(target.Freeze().WithModifiers(target.Source.Modifiers!.WithSynergizeStatuses(
                            target.Source.Modifiers.SynergizeStatuses!.Select(value => new StatisticCount(value.Key, 0)).ToArray())));
                    }
                    return true;
                }
                if (action.Type == "SelfFeederRules")
                {
                    // Native runs the OnEaten queue before despawning food, even
                    // when this effect is itself executing inside a character queue.
                    bool previous = localFeedingQueue;
                    localFeedingQueue = true;
                    try { if (!source.Deployment) Feed(actor.Source.Team, targets.UnitIds, selfFeeder: actor); }
                    finally { localFeedingQueue = previous; }
                    if (unsupportedReason == null && context?.SpawnPoints != null && !SummonPhysical("Compact", team: actor.Source.Team)) return false;
                    return unsupportedReason == null;
                }
                if (action.Type == "RemoveEquipment")
                {
                    var definitions = new BattlePlayRules(context!.SummonCatalog!.Rooms, context.SummonCatalog.Cards
                        .Where(card => card.Equipment != null).Select(card => new CardPlayRule(card.Creation.DataId,
                            "", 0, "Equipment", "Standby", null, Array.Empty<string>(), equipment: card.Equipment)).ToArray());
                    foreach (int id in targets.UnitIds)
                    {
                        WorkingUnit target = units.Single(unit => unit.Source.Id == id);
                        foreach (int gearId in target.Source.EquipmentCards!.Reverse().ToArray())
                            if (!RemoveEquipment(target, gearId, definitions)) return false;
                    }
                    return true;
                }
                if (action.Type == "HoardPerStatus")
                {
                    if (source.Preview || context?.EnergyState?.PyreAlive != true) return true;
                    // Native uses ParamInt only in the notification; the actual
                    // save award is the integer quotient of the collected stacks.
                    int award = targets.UnitIds.Sum(id => units.Single(unit => unit.Source.Id == id).Count(action.Statuses[0].Id)) /
                        action.Statuses[0].Stacks;
                    StatisticQueryFrame frame = context.QueryFrame!;
                    context = context.WithQueryFrame(frame.With(dragonsHoard: Math.Clamp(unchecked(frame.DragonsHoard!.Value + award),
                        0, context.Resources!.DragonsHoardCap!.Value)));
                    return true;
                }
                if (action.Type == "AddStatsFromSelf")
                {
                    int attack = UnitAttackOverrideModel.Resolve(actor.Freeze().Statuses, actor.Source.MaxHealth) ??
                        actor.Source.Modifiers?.AttackDamage ?? actor.Source.BaseAttack;
                    int health = actor.Source.MaxHealth;
                    foreach (int id in targets.UnitIds)
                    {
                        WorkingUnit? target = units.FirstOrDefault(unit => unit.Source.Id == id);
                        if (target == null) continue;
                        CardUpgradeMaskMetadata? maskMetadata = context?.CardRegistry?.Concat(context.CardInstances ?? Array.Empty<CardInstanceState>())
                            .Any(card => card.MaskDescriptor != null) == true
                            ? new CardUpgradeMaskMetadata(Array.Empty<UpgradeMaskStatus>(), false, false, false, false) : null;
                        var stats = new CardStatModifier(attack, health);
                        var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("", stats,
                            Array.Empty<UpgradeMaskStatus>(), false, false, false, false), "", false, false,
                            Array.Empty<CardTraitValue>(), Array.Empty<string>(), false, Array.Empty<string>(),
                            Array.Empty<string>(), Array.Empty<CardLifecycleTrigger>());
                        var copiedStats = new CardUpgradeModifier("", "", stats, Array.Empty<CombatStatus>(),
                            false, false, false, 0, 0, Array.Empty<string>(), maskMetadata: maskMetadata, lifecycle: lifecycle);
                        if (!ApplyUpgrade(target, copiedStats, "TemporaryUntilUnitDeath", false, null, 0, triggerKind, directApi: true)) return false;
                        if (!source.Preview && target.Source.SpawnerCardId > 0 && context?.FindCard(target.Source.SpawnerCardId) is CardInstanceState spawner)
                        {
                            context = context.WithCard(spawner.WithModifiers(spawner.Permanent,
                                UnitModifierModel.Add(spawner.Temporary, copiedStats)));
                        }
                        Emit(action.Type, actor, target, 0);
                    }
                    return true;
                }
                if (action.Type == "HealTrain")
                {
                    WorkingUnit? pyre = units.SingleOrDefault(unit => unit.Source.IsPyre && !unit.Destroyed);
                    if (pyre == null) return true;
                    RelicResourceResult maximum = RelicResourceModel.GetPyreMaxHealth(context!);
                    if (!maximum.Supported) { unsupportedReason = maximum.UnsupportedReason; return false; }
                    if (action.Value > 0 && (source.Preview || context!.Resources!.TowerHealingDisabled != true))
                        pyre.Health = Math.Min(maximum.Value, unchecked(pyre.Health + action.Value));
                    return true;
                }
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
                if (action.Type == "AdjustAllStatusEffects")
                {
                    foreach (int id in targets.UnitIds.Reverse())
                    {
                        RoomCombatResult applied = CardSpellModel.ApplyStatusAdjustment(RetainedStatusRoom(actor, targets.UnitIds),
                            action, id, actor.Source.SpawnerCardId, actor.Has("duality"));
                        if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return false; }
                        context = applied.State!.Context;
                        foreach (CombatUnit changed in applied.State.Units) units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                        foreach (QueuedCharacterTrigger callback in applied.PendingCallbacks) QueueCallback(callback);
                        foreach (CombatEvent item in applied.Events) events.Add(new CombatEvent(round, item.Kind, item.Actor, item.Target, item.Amount));
                    }
                    return true;
                }
                if (action.Type == "DamagePerHp")
                {
                    if (targets.UnitIds.Count == 0) return true;
                    WorkingUnit selected = units.First(unit => unit.Source.Id == targets.UnitIds[0]);
                    int retaliationDamage = unchecked((selected.Health + (action.DamagePerHpIncludeArmor ? selected.Count("armor") : 0)) * action.Value);
                    if (retaliationDamage <= 0 || actor.LastAttackerId == 0) return true;
                    WorkingUnit? attacker = units.FirstOrDefault(unit => unit.Source.Id == actor.LastAttackerId);
                    if (attacker == null)
                    { unsupportedReason = "Triggered damage-per-HP last attacker is outside the captured room."; return false; }
                    if (attacker.Alive && !attacker.Removed) Damage(actor, attacker, retaliationDamage, "TriggeredDamage", actor.Source.SpawnerCardId);
                    return unsupportedReason == null;
                }
                if (action.Type == "PlayUnitTrigger")
                {
                    foreach (int id in targets.UnitIds)
                    {
                        WorkingUnit target = units.First(unit => unit.Source.Id == id);
                        QueueCallback(new QueuedCharacterTrigger(target.RoomIndex, target.Freeze(), action.UnitTriggerKind!));
                    }
                    return unsupportedReason == null;
                }
                if (action.Type == "Null") return true;
                if (action.Type == "FloorRearrange")
                {
                    if (targets.UnitIds.Count == 0) return true;
                    WorkingUnit target = units.Single(unit => unit.Source.Id == targets.UnitIds[0]);
                    // The native attack trigger can retain a lethal hit target.
                    // FloorRearrange exits before moving or centering a dead actor.
                    if (!target.Alive || target.Destroyed) return true;
                    var shifted = units.Where(unit => unit.Alive && unit.InRoom && unit.Source.Team == target.Source.Team &&
                        context!.SpawnPoints!.Units.Single(point => point.UnitId == unit.Source.Id).OuterBoss != true).ToArray();
                    int from = Array.FindIndex(shifted, unit => unit == target);
                    int goal = action.Value == 0 ? 0 : shifted.Length - 1;
                    RoomCombatResult moved = CardSpellModel.ApplyRearrangement(CurrentRoom(), action, targets.UnitIds[0]);
                    if (!moved.Supported) { unsupportedReason = moved.UnsupportedReason; return false; }
                    MergeSummon(moved);
                    // Native centers both teams before returning, including dead
                    // occupants retained by this exchange but absent from CurrentRoom.
                    if (!target.Has("immobile") && context!.SpawnPoints!.Units.Single(unit => unit.UnitId == target.Source.Id).OuterBoss != true)
                    {
                        if (!CenterAfterSummon()) return false;
                        var moving = new List<WorkingUnit> { target };
                        int direction = from < goal ? 1 : -1;
                        for (int at = from; from >= 0 && at != goal; at += direction) moving.Add(shifted[at + direction]);
                        foreach (WorkingUnit entrant in moving)
                        {
                            if (entrant != target || from != goal)
                                QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, entrant.Freeze(), "OnShift"));
                            foreach (WorkingUnit guard in units.Where(unit => unit.Alive && unit.InRoom && unit.Source.Team != entrant.Source.Team))
                                QueueCallback(new QueuedCharacterTrigger(source.RoomIndex, guard.Freeze(), "OnSentry",
                                    overrideTarget: entrant.Freeze(), paramString: ""));
                        }
                    }
                    return UpdateEnchantments();
                }
                if (AbilityCooldownModel.IsEffect(action.Type) || action.Type is "RemoveStatus" or "RemoveAllStatusEffects" || AbilityLifecycleModel.IsEffect(action.Type))
                {
                    bool removeStatus = action.Type is "RemoveStatus" or "RemoveAllStatusEffects";
                    IEnumerable<int> ordered = removeStatus ? targets.UnitIds : targets.UnitIds.Reverse();
                    foreach (int id in ordered)
                    {
                        RoomCombatState retained = RetainedStatusRoom(actor, targets.UnitIds);
                        RoomCombatResult applied = AbilityLifecycleModel.IsEffect(action.Type)
                            ? AbilityLifecycleModel.Apply(retained, id, action.AbilityChange!, action.Type == "RemoveAbility", deferCallbacks: true) :
                            removeStatus ? StatusRemovalModel.Effect(retained, id, action, actor.Source.SpawnerCardId,
                                whileRunningQueue: true) :
                            AbilityCooldownModel.Apply(retained, id, action, actor.Source.SpawnerCardId, triggerKind);
                        if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return false; }
                        context = applied.State!.Context;
                        foreach (CombatUnit changed in applied.State.Units) units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                        if (removeStatus && !applied.State.Units.Any(unit => unit.Id == id))
                        {
                            var dead = applied.PendingCallbacks.LastOrDefault(item => item.Unit.Id == id && item.Unit.Health <= 0);
                            if (dead != null) units.First(unit => unit.Source.Id == id).Apply(dead.Unit);
                        }
                        foreach (QueuedCharacterTrigger callback in applied.PendingCallbacks) QueueCallback(callback);
                    }
                    return true;
                }
                if (action.Type is "AddStatus" or DevotionStatusModel.Effect or TriggeredStatusModel.OtherStatusAction)
                {
                    if (action.Type == TriggeredStatusModel.OtherStatusAction && targets.UnitIds.Any(id =>
                        units.Any(unit => unit.Source.Id == id && unit.RoomIndex != actor.RoomIndex)))
                    {
                        unsupportedReason = "Status-per-other attack target is outside the actor's room.";
                        return false;
                    }
                    EnchantmentWorld? physicalWorld = context?.Enchantments;
                    int? damageActorId = effect.Type == FriendlyDamageStatusModel.NativeEffect ? overrideTarget?.Source.Id : null;
                    var roster = damageActorId.HasValue ? targets.UnitIds.Concat(new[] { damageActorId.Value }).Distinct().ToArray() : targets.UnitIds;
                    bool observedInfestedDeathGrant = triggerKind == "OnAnyUnitDeathOnFloor" && roster.Count == 1 && roster[0] == actor.Source.Id &&
                        actor.Source.Triggers.Count >= 2 &&
                        (source.RoomIndex == 2 &&
                            OuterBossDefinitions.HasDormantLifemotherInfestedDeathGrant(actor.Source, actor.Source.Triggers[0], source.Context) ||
                         HivemindTurnUpgrade.HasObservedLaterInfestedDeathGrant(source, actor.Source, actor.Source.Triggers[0])) &&
                        ReferenceEquals(actor.Source.Triggers[0].Effects[0], effect) ||
                        triggerKind == "OnDeath" && HivemindTurnUpgrade.IsObservedInfestedHordeGrant(source, actor.Freeze(), effect);
                    RoomCombatResult applied = TriggeredStatusModel.Apply(RetainedStatusRoom(actor, roster), actor.Source.Id,
                        effect, targets.UnitIds, isFromHiddenTrigger: hiddenTrigger, damageActorId: damageActorId,
                        observedInfestedDeathGrant: observedInfestedDeathGrant);
                    if (!applied.Supported) { unsupportedReason = applied.UnsupportedReason; return false; }
                    context = applied.State!.Context;
                    foreach (CombatUnit changed in applied.State.Units) units.First(unit => unit.Source.Id == changed.Id).Apply(changed);
                    // The retained status scope includes remote targets so native
                    // status callbacks can inspect them. It is not a room move.
                    // Restore their physical membership before the next aura sync.
                    int[] remote = targets.UnitIds.Where(externalTurnActorIds.Contains).ToArray();
                    if (remote.Length > 0 && physicalWorld != null && context?.Enchantments is EnchantmentWorld world)
                    {
                        var changed = units.Where(unit => remote.Contains(unit.Source.Id)).ToDictionary(unit => unit.Source.Id, unit => unit.Freeze());
                        var rooms = world.Rooms.Select(room =>
                        {
                            RoomCombatState original = physicalWorld.Rooms.Single(floor => floor.RoomIndex == room.RoomIndex);
                            var order = original.Units.Select((unit, index) => (unit.Id, Index: index)).ToDictionary(item => item.Id, item => item.Index);
                            CombatUnit[] members = room.Units.Where(unit => !changed.ContainsKey(unit.Id)).Concat(original.Units
                                .Where(unit => changed.ContainsKey(unit.Id)).Select(unit => changed[unit.Id]))
                                .OrderBy(unit => order.TryGetValue(unit.Id, out int index) ? index : int.MaxValue).ToArray();
                            return new RoomCombatState(room.RoomIndex, room.Deployment, members, room.ExternalInteractions,
                                null, room.Preview, room.ActiveCompanionBossIds);
                        }).ToArray();
                        context = context.WithEnchantments(new EnchantmentWorld(rooms, world.Movement, world.EnemySlotsPerRoom,
                            world.RetainedUnits.Where(unit => !changed.ContainsKey(unit.Unit.Id)).ToArray(), world.EnchanterIds,
                            world.AllowUpdates, world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle, world.TemporaryPreviewUnitIds));
                    }
                    foreach (QueuedCharacterTrigger callback in applied.PendingCallbacks) QueueCallback(callback);
                    foreach (CombatEvent item in applied.Events) events.Add(new CombatEvent(round, item.Kind, item.Actor, item.Target, item.Amount));
                    return true;
                }
                int amount = action.Type == "AdjustCapacity" || (action.Type == ForgePointsModel.Effect || action.Type == DevotionModel.Effect) && source.Preview
                    ? action.Value : SampleActionAmount(action);
                if (action.Type is "BuffAttack" or "DebuffAttack")
                {
                    foreach (int id in targets.UnitIds)
                    {
                        WorkingUnit? target = units.FirstOrDefault(unit => unit.Source.Id == id && unit.Alive && unit.Source.CanAttack);
                        if (target == null) continue;
                        RoomCombatState attackRoom = target.RoomIndex == source.RoomIndex ? CurrentRoom() :
                            new RoomCombatState(target.RoomIndex, source.Deployment,
                                units.Where(unit => unit.Alive && unit.RoomIndex == target.RoomIndex).Select(unit => unit.Freeze()).ToArray(),
                                Array.Empty<string>(), context, source.Preview, CompanionBossSnapshot());
                        RoomCombatResult changed = UnitAttackModel.Apply(attackRoom, id, amount, debuff: action.Type == "DebuffAttack");
                        if (!changed.Supported) { unsupportedReason = changed.UnsupportedReason; return false; }
                        context = changed.State!.Context;
                        foreach (CombatUnit unit in changed.State.Units) units.First(current => current.Source.Id == unit.Id).Apply(unit);
                        Emit(action.Type, actor, target, amount);
                    }
                    return true;
                }
                if (action.Type is "AdjustMaxHealth" or "BuffHealth")
                {
                    string? healthError = TriggeredHealthModel.Validate(effect);
                    if (healthError != null) { unsupportedReason = healthError; return false; }
                    bool duoHealth = ((actor.Source.AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth" && amount == 30 ||
                        actor.Source.AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth" && amount == 60 ||
                        actor.Source.AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth" && amount == 50) &&
                        action.Type == "BuffHealth" && action.Target == "Tower" &&
                        action.Lifetime == "TemporaryUntilEndOfBattle" && action.Filters?.Subtype == "SubtypesData_SavageAngel") ||
                        OuterBossDefinitions.CalibratedAngelDuoTrialGrowth(actor.Source, context, triggerKind, effect);
                    if (targets.UnitIds.Any(id => units.All(unit => unit.Source.Id != id) ||
                        !duoHealth && units.Single(unit => unit.Source.Id == id).RoomIndex != source.RoomIndex))
                    { unsupportedReason = "Cross-floor triggered maximum-health mutation requires native calibration."; return false; }
                    foreach (int id in targets.UnitIds)
                    {
                        WorkingUnit? target = units.FirstOrDefault(unit => unit.Source.Id == id);
                        if (target == null) continue;
                        if (!target.Alive) { unsupportedReason = "Retained dying maximum-health targets require native calibration."; return false; }
                        if (action.Type == "AdjustMaxHealth")
                        {
                            if (target.Triggers.Any(HalfHealthDeathwishModel.IsTrigger))
                            { unsupportedReason = "Half-health Deathwish maximum-health effects require native lifecycle calibration."; return false; }
                            CombatUnit? changed = TriggeredHealthModel.Adjust(target.Freeze(), amount);
                            if (changed == null) { unsupportedReason = "Maximum-health adjustment lowers maximum below current health."; return false; }
                            target.Apply(changed);
                        }
                        else
                        {
                            if (units.Any(unit => unit.Triggers.Any(HalfHealthDeathwishModel.IsTrigger)))
                            {
                                // A nested maximum-health heal observes this engine's
                                // running queue. Starting another engine would execute
                                // gained/lost callbacks ahead of already accepted work.
                                context = UnitHealthModel.RecordSpawnerBuff(context, target.Freeze(), amount, action.Lifetime,
                                    source.Preview, out string? spawnerError);
                                if (spawnerError != null) { unsupportedReason = spawnerError; return false; }
                                if (amount > 0)
                                {
                                    target.Apply(target.Freeze().WithHealthAndMaxHealth(target.Health, Math.Min(99999, checked(target.Source.MaxHealth + amount))));
                                    Heal(target, amount, "MaxHealthHeal", fromMaxHealthChange: true, triggerOnHeal: false);
                                    if (!UpdateHalfHealthDeathwish(target, drain: false)) return false;
                                }
                                Emit(action.Type, actor, target, amount);
                                continue;
                            }
                            RoomCombatState healthRoom = target.RoomIndex == source.RoomIndex ? CurrentRoom() :
                                new RoomCombatState(target.RoomIndex, source.Deployment,
                                    units.Where(unit => unit.Alive && unit.RoomIndex == target.RoomIndex).Select(unit => unit.Freeze()).ToArray(),
                                    Array.Empty<string>(), context, source.Preview, CompanionBossSnapshot());
                            RoomCombatResult changed = UnitHealthModel.Apply(healthRoom, id, amount, lifetime: action.Lifetime);
                            if (!changed.Supported) { unsupportedReason = changed.UnsupportedReason; return false; }
                            context = changed.State!.Context;
                            foreach (CombatUnit unit in changed.State.Units) units.First(current => current.Source.Id == unit.Id).Apply(unit);
                        }
                        Emit(action.Type, actor, target, amount);
                    }
                    return true;
                }
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
                if (action.Type == DevotionModel.Effect)
                {
                    if (!source.Preview)
                    {
                        DevotionResult changed = DevotionModel.ApplyEffect(context!, new CardActionEffect(DevotionModel.Effect,
                            action.Target, amount, action.AllowEnemy, action.AllowPlayer, action.Statuses, devotionPerLevel: action.DevotionPerLevel));
                        if (!changed.Supported) { unsupportedReason = changed.UnsupportedReason; return false; }
                        context = changed.Context; Emit(action.Type, actor, actor, amount);
                    }
                    return true;
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
                if (action.Type == "DamagePerSourceAttack") amount = unchecked(amount * actor.Attack);
                if (effect.Type == OwnerDamageReactionModel.NativeEffect)
                {
                    amount = unchecked(amount * actor.Source.DamageHistory!.DamageTakenBeforeArmor);
                    if (amount <= 0) return true;
                }
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
                    // Unrelated remote actors are auxiliary combat targets, not
                    // occupants of this room. Status synchronization must not
                    // move the live Pyre into the local aura snapshot.
                    units.Where(unit => unit.Alive && !externalTurnActorIds.Contains(unit.Source.Id) ||
                        unit == actor || targets.Contains(unit.Source.Id)).Select(unit => unit.Freeze()).ToArray(),
                    source.ExternalInteractions, context, source.Preview, CompanionBossSnapshot());

            private bool StatusTest(WorkingUnit actor, CombatEffect effect, CardTargets targets)
            {
                CombatStatus? selected = null;
                if (effect.Type != "CardEffectAddStatusEffectBasedOnHealth" && source.Preview && context?.IsolatedBattlePreview == true && effect.Action!.Statuses.Count > 1)
                {
                    RngDraw choice = context.BattleRng.Range(0, effect.Action.Statuses.Count);
                    context = context.WithBattleRng(choice.State); selected = effect.Action.Statuses[choice.Value];
                }
                else if (effect.Type == TriggeredStatusModel.HealthPercentStatusEffect &&
                    context?.Enchantments is { AutomaticLifecycle: true } world)
                {
                    context = context.WithEnchantments(world.WithTestRng(world.TestRng.Range(0, 2).State));
                }
                bool result = TriggeredStatusModel.Test(RetainedStatusRoom(actor, targets.UnitIds), effect, targets.UnitIds, out string? error, selected);
                if (error != null) unsupportedReason = error;
                return result;
            }

            private bool GrantEquipment(WorkingUnit actor, CombatEffect effect)
            {
                if (source.Preview || !actor.Alive) return true;
                CombatContext sourceContext = context!;
                CardGenerationResult generated = CardGenerationModel.Apply(sourceContext, effect.Generation!, actor.Source.SpawnerCardId);
                if (!generated.Supported) { unsupportedReason = generated.UnsupportedReason; return false; }
                CardToken card = generated.AddedCards.Single();
                CardPlayRule? rule = sourceContext.SummonCatalog!.Cards
                    .SingleOrDefault(entry => entry.Creation.DataId == card.DataId)?.GeneratedEquipmentRule;
                if (rule?.Equipment == null || rule.DataId != card.DataId)
                {
                    unsupportedReason = "Selected equipment requires its captured attachment definition.";
                    return false;
                }
                context = generated.Context!;
                var rules = new BattlePlayRules(Array.Empty<RoomPlayRule>(), new[] { rule });
                RoomCombatResult equipped = ApplyEquipment(CurrentRoom(), actor.Source.Id, card.InstanceId, rules,
                    deferAbilityCallbacks: true, deferCallbacks: true);
                if (!equipped.Supported) { unsupportedReason = equipped.UnsupportedReason; return false; }
                ImportEnchantmentRoom(equipped.State!);
                context = EquipmentModel.BindTransferred(context!, card.InstanceId, actor.Source.Id);
                events.AddRange(equipped.Events);
                foreach (QueuedCharacterTrigger callback in equipped.PendingCallbacks) QueueCallback(callback);
                return true;
            }

            private void AddCards(WorkingUnit actor, CombatEffect effect)
            {
                CardGenerationRule rule = effect.Generation ?? new CardGenerationRule(effect.Destination, effect.Count,
                    effect.CardPool.Select(id => new CardCreationRule(id, CardModifiers.Empty(), null, Array.Empty<string>())).ToArray(), effect.SkipDuplicateInHand);
                CardGenerationResult result = CardGenerationModel.Apply(rule.Permanent == true ? DeckChangeSource() : context!,
                    rule, actor.Source.SpawnerCardId);
                if (!result.Supported) { unsupportedReason = result.UnsupportedReason; return; }
                ApplyDeckChange(result.Context!);
                foreach (CardToken card in result.AddedCards) Emit("AddCard", actor, actor, card.InstanceId);
            }

            private CombatContext DeckChangeSource()
            {
                if (!RelicDeckPyreStatsModel.Present(context)) return context!;
                return EnchantmentWorldModel.Sync(PhysicalRoom(), externalUnits: ExternalEnchantmentUnits()).Context!;
            }

            private void ApplyDeckChange(CombatContext next)
            {
                CombatUnit? adjustedPyre = RelicDeckPyreStatsModel.ChangedPyre(context, next);
                if (adjustedPyre != null)
                    units.FirstOrDefault(unit => unit.Source.Id == adjustedPyre.Id)?.Apply(adjustedPyre);
                context = next;
            }

            private string? Trigger(WorkingUnit unit, string id, int count)
            {
                if (unit.Statuses.TryGetValue(id, out CombatStatus? status) &&
                    (status.RemoveWhenTriggered || status.RemoveWhenTriggeredAfterCardPlayed || status.RemoveAtEndOfTurnIfTriggered) &&
                    (!source.Deployment || status.RemoveDuringDeployment))
                {
                    RelicStatusDecayResult removal = RelicStatusDecayModel.GetShouldRemove(context, true, id,
                        unit.Source.Team == CombatTeam.Player ? 2 : 1, source.Preview);
                    if (!removal.Supported) { unsupportedReason = removal.UnsupportedReason; return null; }
                    if (removal.ShouldRemove)
                    {
                        if (status.RemoveWhenTriggeredAfterCardPlayed)
                            unit.Statuses[id] = status.WithMarkedForRemovalAfterCardPlayed(true);
                        else if (status.RemoveAtEndOfTurnIfTriggered)
                            unit.Statuses[id] = status.WithRemoveAllAtEnd(true);
                        else
                        {
                            FocusRoomForStatusRemoval(id, fromTrigger: true);
                            RemoveStatus(unit, id, count);
                        }
                    }
                    return removal.SuppressingRelicId;
                }
                return null;
            }

            private void QueueCallback(QueuedCharacterTrigger callback)
            {
                callback = callback.WithCaptureAllDispatches(traceAllDispatches);
                string? error = StatusCallbackModel.Admit(context, callback, out QueuedCharacterTrigger admitted);
                if (error != null) { unsupportedReason = error; return; }
                if (admitted.Admission == CharacterTriggerAdmission.Rejected) return;
                callback = admitted;
                if (enqueueCharacterTrigger != null && !localUpgradeQueue) { enqueueCharacterTrigger(callback); return; }
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
                    callback.ExclusiveTriggerStateId, callback.AttackDamageDealt));
            }

            private void RemoveStatus(WorkingUnit unit, string id, int count)
            {
                var callbacks = new List<QueuedCharacterTrigger>();
                RoomCombatResult relicAdded = RelicStatusStackModel.OnStatusRemoved(CurrentRoom(), unit.Source.Id, id, ref count, null);
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
                    if (StatusAttackDamageModel.ChangesMaximumHealth(id))
                    {
                        RoomCombatResult membership = StatusAttackDamageModel.HealthMembership(CurrentRoom(), unit.Freeze(), status,
                            status.Stacks - (after.RegisteredStatus(id)?.Stacks ?? 0), removing: true);
                        if (!membership.Supported) { unsupportedReason = membership.UnsupportedReason; return; }
                        context = membership.State!.Context;
                        foreach (CombatUnit changedActor in membership.State.Units)
                            units.FirstOrDefault(actor => actor.Source.Id == changedActor.Id)?.Apply(changedActor);
                        callbacks.AddRange(membership.PendingCallbacks);
                    }
                    if (id == "conduit" && context != null)
                        context = ConduitModel.Change(context, unit.RoomIndex, unit.Freeze(), source.Preview,
                            refreshSelection: false, order: units.Where(actor => actor.InRoom).Select(actor => actor.Source.Id).ToArray());
                }
                StatusCallbackModel.Removed(unit.RoomIndex, before, unit.Freeze(), id, callbacks);
                foreach (QueuedCharacterTrigger callback in callbacks) QueueCallback(callback);
                // CharacterState removes control stacks before refreshing all
                // auras, including removal during the post-combat preview clear.
                if ((before.RegisteredStatus(id)?.Stacks ?? 0) > (unit.Freeze().RegisteredStatus(id)?.Stacks ?? 0) &&
                    (id == "silenced" || id == "muted" || id == "spark" && unit.Has("dormant")))
                    UpdateEnchantments();
            }

            private int StatusMagnitude(WorkingUnit unit, string id)
            {
                if (!unit.Statuses.TryGetValue(id, out CombatStatus? status)) return 0;
                StatusScalingResult modifier = RelicStatusMagnitudeModel.GetModifier(context, id, unit.Source.Team == CombatTeam.Player ? 2 : 1);
                if (!modifier.Supported) { unsupportedReason = modifier.UnsupportedReason; return 0; }
                context = modifier.Context;
                return unchecked(unchecked(status.ParamInt + modifier.Stacks + (id == "spikes" ? RoomStatusModifier(unit, id) : 0)) * status.Stacks);
            }

            private bool Active(CombatStatus status) => !source.Deployment || !status.SkipDuringDeployment;

            private void Clear(bool after)
            {
                if (source.Deployment) return;
                // RoomManager.RoomIsInRelentlessPhase deliberately returns false
                // during the player's MonsterTurn, including UI combat previews.
                bool relentless = context?.EnergyState?.Phase != "MonsterTurn" &&
                    units.Any(unit => unit.Alive && unit.InRoom && unit.Source.Team == CombatTeam.Enemy && unit.Has("relentless"));
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

            private void FocusRoomForStatusRemoval(string statusId, bool fromTrigger = false)
            {
                if ((fromTrigger || statusId != "cooldown") && !source.Preview && context?.QueryFrame?.SelectedRoomIndex != null)
                    SelectNotificationRoom(source.RoomIndex);
            }
            private void SelectNotificationRoom(int roomIndex)
            {
                int? previousRoom = context?.QueryFrame?.SelectedRoomIndex;
                if (!previousRoom.HasValue) return;
                context = context!.WithQueryFrame(context.QueryFrame!.With(selectedRoomIndex: roomIndex));
                // Status removal and completed deaths select their room and
                // refresh hand resource traits before the next card trigger.
                // The resolving card is already outside the hand.
                if (previousRoom != roomIndex)
                {
                    context = CardMoonUpgradeModel.RefreshHand(context, out string? handError);
                    if (handError != null) unsupportedReason = handError;
                }
            }

            private bool BothTeamsPresent() => units.Any(unit => unit.Source.Team == CombatTeam.Player &&
                CombatContinuationModel.Counts(unit.Alive, unit.InRoom, unit.Has("untouchable"), unit.Has("lifelink"))) &&
                units.Any(unit => unit.Source.Team == CombatTeam.Enemy &&
                    CombatContinuationModel.Counts(unit.Alive, unit.InRoom, unit.Has("untouchable"), unit.Has("lifelink")));

            private string Signature()
            {
                var text = new StringBuilder();
                foreach (WorkingUnit unit in units)
                {
                    text.Append(unit.Source.Id).Append(':').Append(unit.Health).Append(':').Append(unit.Source.BaseAttack)
                        .Append(':').Append(unit.Source.MaxHealth).Append(':').Append(unit.Source.Size).Append(':');
                    text.Append(unit.LastAttackerId).Append(':').Append(unit.Removed).Append(':');
                    if (unit.Source.MoonPhaseMask.HasValue) text.Append("|moonMask:").Append(unit.Source.MoonPhaseMask);
                    if (unit.Source.DamageHistory is UnitDamageHistory history) text.Append("|hit:").Append(history.DamageTaken).Append(':').Append(history.DamageTakenBeforeArmor).Append(':').Append(history.ArmorBlockedOnHit).Append(':').Append(history.LethalDamagePreventedOnHit);
                    if (unit.Source.Ability is UnitAbilityState ability)
                    {
                        text.Append("|ability:").Append(ability.DataId).Append(':').Append(ability.Cooldown).Append(':')
                            .Append(ability.CooldownAtSpawn).Append(':').Append(ability.FromEquipment).Append(':')
                            .Append(ability.Resolving).Append(':').Append(ability.PreviousDataId).Append(':').Append(ability.ReplacedWhileResolving);
                        foreach (UnitAbilityDefinition? definition in new[] { ability.Definition, ability.PreviousDefinition })
                            if (definition != null) text.Append(':').Append(definition.IsUnitAbility).Append(':')
                                .Append(definition.Cooldown).Append(':').Append(definition.CooldownAtSpawn);
                    }
                    if (unit.Source.AbilityRules is UnitAbilityRules abilityRules)
                    {
                        text.Append("|ability-rules:").Append(abilityRules.PreventEquipmentAbilities).Append(':').Append(abilityRules.ReplacementSkipsCooldown);
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

            private RoomCombatResult Finish(RoomOutcome outcome, int retainedDuringCallback = 0)
            {
                if (context != null) context = ConduitModel.Destroyed(context, units.Select(unit => unit.Freeze()));
                // Native attack snapshots query tracked Gold for each attached Goldtooth.
                // After ClearCards this getter replaces the owned-card cache with the deck.
                if (context != null && units.Any(unit => unit.Alive && unit.InRoom &&
                    unit.Source.Modifiers?.SelfAttackRules?.Any(rule => rule.Type == "RoomStateSelfDamagePerGoldModifier") == true))
                    context = context.WithStatistics(context.LiveStatistics);
                // The native standby predicate observes preview deaths too;
                // the outer preview transaction restores primary card piles.
                if (context != null && (!source.Preview || context.LifeLinks != null ||
                    context.Enchantments?.TemporaryPreviewUnitIds != null))
                    context = UnitStandbyModel.MarkDead(context, units.Where(unit => !unit.Alive).Select(unit => unit.Freeze()).ToArray());
                var removedIds = new HashSet<int>(units.Where(unit => !unit.Alive || unit.Removed || unit.Despawned).Select(unit => unit.Source.Id));
                if (battleWon && !source.Preview && context?.SpawnPoints is BattleSpawnPoints terminalPoints)
                {
                    // Victory can stop the death batch after the first companion.
                    // Terminal captures retire the other dead enemy's logical slot,
                    // including outside Boss anchors, without running its callbacks.
                    var retiredEnemies = units.Where(unit => !unit.Alive && unit.Source.Team == CombatTeam.Enemy)
                        .Select(unit => unit.Source.Id).ToHashSet();
                    // The original inner Reanimate is already physically
                    // removed after the final Undying life. Its room centering
                    // remembered that real point; terminal slot retirement must
                    // not erase it like an unfinished companion death batch.
                    var completedInnerBosses = units.Where(unit => !unit.Alive && unit.Destroyed &&
                        unit.Source.AssetKey == "Boss_Level5_Reanimate" && unit.Source.BaseAttack == 17 &&
                        unit.Source.MaxHealth == 500 && unit.Count("undying") == 0)
                        .Select(unit => unit.Source.Id).ToHashSet();
                    context = context.WithSpawnPoints(terminalPoints.WithState(terminalPoints.Groups.Select(group =>
                        group.Team == CombatTeam.Enemy && !group.PreviewCopyId.HasValue
                            ? new SpawnPointGroupState(group.RoomIndex, group.Team, group.InnerCount, group.GroupCount,
                                group.Occupants.Select(id => retiredEnemies.Contains(id) ? 0 : id).ToArray(), group.Outside)
                            : group).ToArray(), terminalPoints.Units.Select(point => retiredEnemies.Contains(point.UnitId)
                            ? new UnitSpawnPointState(point.UnitId, null,
                                completedInnerBosses.Contains(point.UnitId) && point.Current == null && !point.OuterBoss &&
                                !point.SpawnedInPreview && point.LastKnown is { PreviewCopyId: null, OuterBossAnchor: false }
                                    ? point.LastKnown : null, point.OuterBoss, point.SpawnedInPreview)
                            : point).ToArray()));
                }
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
                RoomCombatState physical = HivemindTurnUpgrade.PublishInfestedHordeMarkers(PhysicalRoom());
                if (HivemindTurnUpgrade.PublishesAtCombat(source))
                    physical = HivemindTurnUpgrade.PublishRoomMarkers(physical);
                if (!source.Preview && source.RoomIndex == 2)
                    physical = HivemindTurnUpgrade.PublishInfestedFirstBossMarker(physical);
                RoomCombatState state = EnchantmentWorldModel.Sync(physical, units.Where(unit => !unit.Alive || !unit.InRoom ||
                    externalTurnActorIds.Contains(unit.Source.Id)).Select(unit => unit.Freeze()).ToArray(), ExternalEnchantmentUnits(),
                    retainedDuringCallback);
                state = state.WithCompanionBosses(CompanionBossSnapshot());
                context = state.Context;
                if (!source.Preview && context?.SpawnPoints is BattleSpawnPoints points && context.Enchantments != null)
                {
                    int[] destroyedBirths = context.Enchantments.RetainedUnits.Where(actor =>
                        actor.Unit.DeathState?.IsDestroyed == true).Select(actor => actor.Unit.Id).ToArray();
                    state = state.WithContext(context.WithSpawnPoints(points.ClearDestroyedPreviewBirths(destroyedBirths)));
                    context = EnchantmentWorldModel.PruneRetiredActors(state.Context!, retainedDuringCallback);
                    state = state.WithContext(context);
                }
                return unsupportedReason != null
                ? new RoomCombatResult(null, RoomOutcome.Unsupported, round, events, unsupportedReason) : new RoomCombatResult(
                state,
                outcome, round, events, pendingCallbacks: nativeCardDamage ? deferredDamageDeaths.Select(dead =>
                    new QueuedCharacterTrigger(source.RoomIndex, dead.Unit.Freeze(), returnSpawnerAfterQueue: dead.Return,
                        deferUntilRemoval: true, harvestAfterDeath: !dead.Unit.Despawned,
                        completePhysicalRemovalAfterQueue: context?.SpawnPoints != null, removalLifecycle: true,
                        captureAllDispatches: traceAllDispatches)).ToArray() : null,
                retainedUnits: units.Where(unit => !unit.Alive || externalTurnActorIds.Contains(unit.Source.Id))
                    .Select(unit => unit.Freeze()).ToArray(), dispatches: dispatches, previewTruncated: previewTruncated);
            }
        }
    }
}
