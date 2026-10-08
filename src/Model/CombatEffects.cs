using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomMagicPower
    {
        public int RoomIndex { get; }
        public CombatTeam Team { get; }
        public int Value { get; }
        public RoomMagicPower(int roomIndex, CombatTeam team, int value)
        { RoomIndex = roomIndex; Team = team; Value = value; }
    }

    // Shared outputs of unit effects. The room and train phases carry this state forward.
    public sealed class CombatContext
    {
        public CardCycleState Cards { get; }
        public UnityRng BattleRng { get; }
        public int Gold { get; }
        public int NextCardId { get; }
        public int MaxHandSize { get; }
        public IReadOnlyList<CombatStatus> StatusRules { get; }
        public BattleStatistics? Statistics { get; }
        public IReadOnlyList<CardInstanceState>? CardInstances { get; }
        // Identity store for observed cards, including references retained after ClearCards.
        // Membership here does not make a card owned or playable.
        public IReadOnlyList<CardInstanceState>? CardRegistry { get; }
        public bool? AllScenarioBossesDead { get; }
        public IReadOnlyList<CardUpgradeModifier>? NextAddedTemporaryUpgrades { get; }
        // Shared with room/effect resolution; null identifies legacy captures without this state.
        public IReadOnlyList<CardPileState>? OtherPiles { get; }
        public StatisticQueryFrame? QueryFrame { get; }
        // Native kill-camera activation gates clearing once, including nested terminal deaths.
        // Null preserves captures made before this state was observed.
        public bool? KillCamActivated { get; }
        public IReadOnlyList<RoomMagicPower>? MagicPower { get; }
        public bool? IsolatedBattlePreview { get; }
        public BattleEnergyState? EnergyState { get; }
        public IReadOnlyList<RoomCapacityState>? RoomCapacities { get; }
        public IReadOnlyList<AbilityCardCacheEntry>? AbilityCardCache { get; }
        public int? LastAbilityActivatorUnitId { get; }
        public int? LastSpawnedUnitId { get; }
        // Shared by card/character effects; null preserves legacy captures.
        public int? NextUnitId { get; }
        public BattleSpawnPoints? SpawnPoints { get; }
        public TriggeredSummonCatalog? SummonCatalog { get; }
        public EnchantmentWorld? Enchantments { get; }
        public IReadOnlyList<string>? PermanentlyDisabledAbilities { get; }
        public CombatContext(CardCycleState cards, UnityRng battleRng, int gold, int nextCardId, int maxHandSize,
            IReadOnlyList<CombatStatus>? statusRules = null, BattleStatistics? statistics = null, IReadOnlyList<CardInstanceState>? cardInstances = null,
            IReadOnlyList<CardInstanceState>? cardRegistry = null, bool? allScenarioBossesDead = null,
            IReadOnlyList<CardUpgradeModifier>? nextAddedTemporaryUpgrades = null, IReadOnlyList<CardPileState>? otherPiles = null,
            StatisticQueryFrame? queryFrame = null, bool? killCamActivated = null, IReadOnlyList<RoomMagicPower>? magicPower = null,
            bool? isolatedBattlePreview = null, BattleEnergyState? energyState = null, IReadOnlyList<RoomCapacityState>? roomCapacities = null,
            IReadOnlyList<AbilityCardCacheEntry>? abilityCardCache = null, int? lastAbilityActivatorUnitId = null,
            IReadOnlyList<string>? permanentlyDisabledAbilities = null, int? lastSpawnedUnitId = null, int? nextUnitId = null, BattleSpawnPoints? spawnPoints = null,
            TriggeredSummonCatalog? summonCatalog = null, EnchantmentWorld? enchantments = null)
        { Cards = cards; BattleRng = battleRng; Gold = gold; NextCardId = nextCardId; MaxHandSize = maxHandSize;
            StatusRules = Array.AsReadOnly((statusRules ?? Array.Empty<CombatStatus>()).ToArray());
            Statistics = cardInstances == null ? statistics : statistics?.WithOwnedCards(cardInstances.Select(card => card.InstanceId));
            CardInstances = cardInstances == null ? null : Array.AsReadOnly(cardInstances.OrderBy(card => card.InstanceId).ToArray());
            CardRegistry = cardRegistry == null ? null : Array.AsReadOnly(cardRegistry.Concat(cardInstances ?? Array.Empty<CardInstanceState>())
                .GroupBy(card => card.InstanceId).Select(group => group.Last()).OrderBy(card => card.InstanceId).ToArray());
            AllScenarioBossesDead = allScenarioBossesDead;
            NextAddedTemporaryUpgrades = nextAddedTemporaryUpgrades == null ? null : Array.AsReadOnly(nextAddedTemporaryUpgrades.ToArray());
            OtherPiles = otherPiles == null ? null : Array.AsReadOnly(otherPiles.ToArray()); QueryFrame = queryFrame; KillCamActivated = killCamActivated;
            MagicPower = magicPower == null ? null : Array.AsReadOnly(magicPower.OrderBy(room => room.RoomIndex).ThenBy(room => room.Team).ToArray());
            IsolatedBattlePreview = isolatedBattlePreview; EnergyState = energyState;
            RoomCapacities = roomCapacities == null ? null : Array.AsReadOnly(roomCapacities.OrderBy(room => room.RoomIndex).ToArray());
            AbilityCardCache = abilityCardCache == null ? null : Array.AsReadOnly(abilityCardCache.OrderBy(entry => entry.DataId, StringComparer.Ordinal).ToArray());
            LastAbilityActivatorUnitId = lastAbilityActivatorUnitId; LastSpawnedUnitId = lastSpawnedUnitId;
            NextUnitId = nextUnitId; SpawnPoints = spawnPoints; SummonCatalog = summonCatalog;
            Enchantments = enchantments;
            PermanentlyDisabledAbilities = permanentlyDisabledAbilities == null ? null : Array.AsReadOnly(permanentlyDisabledAbilities.ToArray()); }
        internal CombatContext WithEnchantments(EnchantmentWorld? world) => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades,
            OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache,
            LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, world);
        internal CombatContext WithNextCardId(int nextCardId) => new CombatContext(Cards, BattleRng, Gold, nextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades,
            OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache,
            LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithSummonCatalog(TriggeredSummonCatalog catalog) => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades,
            OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache,
            LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, catalog, Enchantments);
        internal CombatContext WithNextUnitId(int nextUnitId) => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades,
            OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache,
            LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, nextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithSpawnPoints(BattleSpawnPoints spawnPoints) => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades,
            OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache,
            LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, spawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithLastSpawned(int? unitId) => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades,
            OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache,
            LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, unitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithAbilityActivator(int? unitId) => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades,
            OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, unitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithRoomCapacities(IReadOnlyList<RoomCapacityState> capacities) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead,
            NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, capacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithEnergyState(BattleEnergyState state) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead,
            NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, state, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithQueryFrame(StatisticQueryFrame? frame) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead,
            NextAddedTemporaryUpgrades, OtherPiles, frame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithStatistics(BattleStatistics? statistics) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithCardInstances(IReadOnlyList<CardInstanceState>? instances) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, instances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithCardRegistry(IReadOnlyList<CardInstanceState>? registry) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, registry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CardInstanceState? FindCard(int id) => CardInstances?.FirstOrDefault(card => card.InstanceId == id)
            ?? CardRegistry?.FirstOrDefault(card => card.InstanceId == id);
        internal CombatContext WithPermanentlyDisabledAbilities(IReadOnlyList<string> abilities) => new CombatContext(
            Cards, BattleRng, Gold, NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry,
            AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower,
            IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, abilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithCard(CardInstanceState changed) => new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics,
            CardInstances?.Select(card => card.InstanceId == changed.InstanceId ? changed : card).ToArray(),
            CardRegistry?.Select(card => card.InstanceId == changed.InstanceId ? changed : card).ToArray(), AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithBossesDead() => new CombatContext(Cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead.HasValue ? true : (bool?)null, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal BattleStatistics? LiveStatistics => CardInstances?.Count == 0 ? Statistics?.RefreshDeckAfterCardTerminal() : Statistics;
        internal CombatContext WithBattleRng(UnityRng rng) => new CombatContext(Cards, rng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithCards(CardCycleState cards) => new CombatContext(cards, BattleRng, Gold, NextCardId,
            MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext WithOtherPiles(IReadOnlyList<CardPileState> piles) => OtherPiles == null ? this : new CombatContext(Cards, BattleRng,
            Gold, NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, NextAddedTemporaryUpgrades, piles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
        internal CombatContext AfterCardEffects() => NextAddedTemporaryUpgrades == null || NextAddedTemporaryUpgrades.Count == 0 ? this : new CombatContext(Cards, BattleRng, Gold,
            NextCardId, MaxHandSize, StatusRules, Statistics, CardInstances, CardRegistry, AllScenarioBossesDead, Array.Empty<CardUpgradeModifier>(), OtherPiles, QueryFrame, KillCamActivated, MagicPower, IsolatedBattlePreview, EnergyState, RoomCapacities, AbilityCardCache, LastAbilityActivatorUnitId, PermanentlyDisabledAbilities, LastSpawnedUnitId, NextUnitId, SpawnPoints, SummonCatalog, Enchantments);
    }

    public sealed class CombatEffect
    {
        public string Type { get; }
        public int Value { get; }
        public int Counter { get; }
        public string Destination { get; }
        public int Count { get; }
        public IReadOnlyList<string> CardPool { get; }
        public bool SkipDuplicateInHand { get; }
        public CardGenerationRule? Generation { get; }
        public CardActionEffect? UnitUpgrade { get; }
        public CardActionEffect? Action { get; }
        // Null disables scaling; otherwise native reads stacks from the triggering unit.
        public string? DamageStatusMultiplier { get; }
        public TriggeredStatusScaling? StatusScaling { get; }
        public TriggeredSummonRule? Summon { get; }
        public EnchantmentRule? Enchantment { get; }
        public CombatEffect(string type, int value, int counter, string destination, int count,
            IReadOnlyList<string> cardPool, bool skipDuplicateInHand, CardGenerationRule? generation = null,
            CardActionEffect? unitUpgrade = null, CardActionEffect? action = null, string? damageStatusMultiplier = null,
            TriggeredStatusScaling? statusScaling = null, TriggeredSummonRule? summon = null, EnchantmentRule? enchantment = null)
        {
            Type = type; Value = value;
            // Every remaining count <= 1 despawns on the next application. Native UI previews can
            // decrement the private counter below zero; canonicalize only those equivalent states.
            Counter = type == "CardEffectDespawnCharacter" ? Math.Max(1, counter) : counter;
            // Only generated-card effects interpret this parameter as a pile destination.
            Destination = type == "CardEffectAddBattleCard" ? destination : ""; Count = count;
            CardPool = Array.AsReadOnly(cardPool.ToArray()); SkipDuplicateInHand = skipDuplicateInHand;
            Generation = generation; UnitUpgrade = unitUpgrade; Action = action; DamageStatusMultiplier = damageStatusMultiplier; StatusScaling = statusScaling; Summon = summon;
            Enchantment = enchantment;
        }
        internal CombatEffect WithCounter(int counter) => new CombatEffect(Type, Value, counter,
            Destination, Count, CardPool, SkipDuplicateInHand, Generation, UnitUpgrade, Action, DamageStatusMultiplier, StatusScaling, Summon, Enchantment);
        internal CombatEffect WithSummon(TriggeredSummonRule summon) => new CombatEffect(Type, Value, Counter,
            Destination, Count, CardPool, SkipDuplicateInHand, Generation, UnitUpgrade, Action, DamageStatusMultiplier, StatusScaling, summon, Enchantment);
        internal CombatEffect WithEnchantment(EnchantmentRule enchantment) => new CombatEffect(Type, Value, Counter,
            Destination, Count, CardPool, SkipDuplicateInHand, Generation, UnitUpgrade, Action, DamageStatusMultiplier, StatusScaling, Summon, enchantment);
        internal CombatEffect WithActionValue(int value) => new CombatEffect(Type, value, Counter, Destination, Count,
            CardPool, SkipDuplicateInHand, Generation, UnitUpgrade, Action == null ? null : new CardActionEffect(Action.Type,
                Action.Target, value, Action.AllowEnemy, Action.AllowPlayer, Action.Statuses, Action.Upgrade, Action.Lifetime,
                Action.Tests, Action.Range, Action.Filters, Action.Generation, Action.OnlyIfNoEnemies, Action.CooldownParameter, Action.AbilityChange), DamageStatusMultiplier, StatusScaling, Summon, Enchantment);
    }

    public sealed class CombatTriggerOrigin
    {
        public string UpgradeId { get; }
        public int EquipmentCardId { get; }
        public bool IsFromEquipment { get; }
        public bool OnlyIfEquipped { get; }
        public CombatTriggerOrigin(string upgradeId, int equipmentCardId, bool isFromEquipment, bool onlyIfEquipped)
        { UpgradeId = upgradeId; EquipmentCardId = equipmentCardId; IsFromEquipment = isFromEquipment; OnlyIfEquipped = onlyIfEquipped; }
    }

    public sealed class CombatTriggerConditions
    {
        public IReadOnlyList<string> RequiredStatuses { get; }
        public IReadOnlyList<string> RequiredDyingStatuses { get; }
        public CombatTriggerConditions(IReadOnlyList<string> requiredStatuses, IReadOnlyList<string> requiredDyingStatuses)
        { RequiredStatuses = Array.AsReadOnly(requiredStatuses.ToArray()); RequiredDyingStatuses = Array.AsReadOnly(requiredDyingStatuses.ToArray()); }
    }

    public sealed class CombatTrigger
    {
        public string Kind { get; }
        public bool Once { get; }
        public bool HasTriggered { get; }
        public bool IgnoreSilence { get; }
        public int FireCount { get; }
        public IReadOnlyList<CombatEffect> Effects { get; }
        public bool? SkipDuringDeployment { get; }
        public int? TriggerAtThreshold { get; }
        public CombatTriggerOrigin? Origin { get; }
        // Per-unit allocation survives list shifts and removal; null denotes a legacy capture.
        public int? StateId { get; }
        public CombatTriggerConditions? Conditions { get; }
        public bool RemoveOnRelentlessChange { get; }
        // Immutable copies retain the native trigger object's identity inside an engine.
        // This token is local to an in-memory branch and is never serialized.
        internal object Identity { get; }
        public CombatTrigger(string kind, bool once, bool hasTriggered, bool ignoreSilence,
            int fireCount, IReadOnlyList<CombatEffect> effects, bool? skipDuringDeployment = null, int? triggerAtThreshold = null,
            CombatTriggerOrigin? origin = null, int? stateId = null, CombatTriggerConditions? conditions = null,
            bool removeOnRelentlessChange = false)
        {
            Kind = kind; Once = once; HasTriggered = hasTriggered; IgnoreSilence = ignoreSilence;
            FireCount = fireCount; Effects = Array.AsReadOnly(effects.ToArray()); SkipDuringDeployment = skipDuringDeployment;
            TriggerAtThreshold = triggerAtThreshold;
            Origin = origin;
            StateId = stateId;
            Conditions = conditions;
            RemoveOnRelentlessChange = removeOnRelentlessChange;
            Identity = new object();
        }
        private CombatTrigger(CombatTrigger source, bool hasTriggered, IReadOnlyList<CombatEffect> effects, int? stateId)
        {
            Kind = source.Kind; Once = source.Once; HasTriggered = hasTriggered; IgnoreSilence = source.IgnoreSilence;
            FireCount = source.FireCount; Effects = Array.AsReadOnly(effects.ToArray()); SkipDuringDeployment = source.SkipDuringDeployment;
            TriggerAtThreshold = source.TriggerAtThreshold; Origin = source.Origin; Identity = source.Identity; StateId = stateId; Conditions = source.Conditions;
            RemoveOnRelentlessChange = source.RemoveOnRelentlessChange;
        }
        internal CombatTrigger Fired(IReadOnlyList<CombatEffect> effects) => new CombatTrigger(this, true, effects, StateId);
        internal CombatTrigger ForPreview() => new CombatTrigger(this, false, Effects, StateId);
        internal CombatTrigger WithEffects(IReadOnlyList<CombatEffect> effects) => new CombatTrigger(this, HasTriggered, effects, StateId);
        public CombatTrigger WithStateId(int? stateId) => new CombatTrigger(this, HasTriggered, Effects, stateId);
        internal CombatTrigger WithOrigin(string upgradeId, int equipmentCardId, int? stateId = null) => new CombatTrigger(Kind, Once, false,
            IgnoreSilence, FireCount, Effects, SkipDuringDeployment, TriggerAtThreshold,
            new CombatTriggerOrigin(upgradeId, equipmentCardId, equipmentCardId > 0, Origin?.OnlyIfEquipped == true), stateId, Conditions, RemoveOnRelentlessChange);
    }
}
