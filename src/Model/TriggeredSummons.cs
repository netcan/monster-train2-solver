using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // References keep recursive summon definitions finite: a unit can summon itself.
    public sealed class TriggeredSummonRule
    {
        public string CharacterId { get; }
        public string AdditionalCharacterId { get; }
        public IReadOnlyList<string> Pool { get; }
        public int Count { get; }
        public bool IgnoreCardUpgrades { get; }
        public bool HasParentCard { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public CardEffectTests Tests { get; }
        // The native effect retains its first birth even when a later application creates none.
        public int FirstSpawnedUnitId { get; }
        public bool EnemyHero { get; }
        public bool ShouldSerializeEnemyHero() => EnemyHero;
        public TriggeredSummonRule(string characterId, string additionalCharacterId, IReadOnlyList<string> pool,
            int count, bool ignoreCardUpgrades, bool hasParentCard, CardUpgradeModifier? upgrade,
            CardEffectTests tests, int firstSpawnedUnitId = 0, bool enemyHero = false)
        { CharacterId = characterId; AdditionalCharacterId = additionalCharacterId;
            Pool = Array.AsReadOnly(pool.ToArray()); Count = count; IgnoreCardUpgrades = ignoreCardUpgrades;
            HasParentCard = hasParentCard; Upgrade = upgrade; Tests = tests; FirstSpawnedUnitId = firstSpawnedUnitId; EnemyHero = enemyHero; }
        internal TriggeredSummonRule WithFirstSpawned(int id) => new TriggeredSummonRule(CharacterId,
            AdditionalCharacterId, Pool, Count, IgnoreCardUpgrades, HasParentCard, Upgrade, Tests, id, EnemyHero);
    }
    public sealed class SummonUnitDefinition
    {
        public string CharacterId { get; }
        public CombatUnit Unit { get; }
        public CardCreationRule? Fallback { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public EnemyDefinition? HeroDefinition { get; }
        public bool ShouldSerializeHeroDefinition() => HeroDefinition != null;
        public SummonUnitDefinition(string characterId, CombatUnit unit, CardCreationRule? fallback,
            IReadOnlyList<string> externalInteractions, EnemyDefinition? heroDefinition = null)
        { CharacterId = characterId; Unit = unit; Fallback = fallback;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); HeroDefinition = heroDefinition; }
    }
    public sealed class SummonCardDefinition
    {
        public CardCreationRule Creation { get; }
        public string SpawnCharacterId { get; }
        public EquipmentDefinition? Equipment { get; }
        public SummonCardDefinition(CardCreationRule creation, string spawnCharacterId, EquipmentDefinition? equipment = null)
        { Creation = creation; SpawnCharacterId = spawnCharacterId; Equipment = equipment; }
    }
    public sealed class TriggeredSummonCatalog
    {
        public IReadOnlyList<SummonUnitDefinition> Units { get; }
        public IReadOnlyList<SummonCardDefinition> Cards { get; }
        public IReadOnlyList<RoomPlayRule> Rooms { get; }
        public TriggeredSummonCatalog(IReadOnlyList<SummonUnitDefinition> units,
            IReadOnlyList<SummonCardDefinition> cards, IReadOnlyList<RoomPlayRule> rooms)
        { Units = Array.AsReadOnly(units.OrderBy(unit => unit.CharacterId, StringComparer.Ordinal).ToArray());
            Cards = Array.AsReadOnly(cards.OrderBy(card => card.Creation.DataId, StringComparer.Ordinal).ToArray());
            Rooms = Array.AsReadOnly(rooms.OrderBy(room => room.RoomIndex).ToArray()); }
    }
    internal static class TriggeredSummonModel
    {
        internal const string FleshweaverSpike = "Boss_Level3_Fleshweaver_Spike";
        internal const string FleshweaverSpikeMiniId = "eb00da55-1597-4768-85bc-2fa241af11cb";
        internal const string FleshweaverSpikeTrialMini = "Boss_Level3_MiniFleshweaver_Spike_TitanTrial";
        internal const string FleshweaverSpikeTrialOwnerId = "c4abfdc2-a89c-43cf-9e35-59f6ea3b39f7";
        internal const string FleshweaverSpikeTrialOwnerKey = "Enemy_Boss_Level3_ImmortalMass_Spike_TitanTrial";

        private static bool KnownFleshweaverSpikeTrial(RoomCombatState state, CombatUnit? actor, string? triggerKind,
            TriggeredSummonRule rule, SummonUnitDefinition mini)
        {
            if (actor is not { AssetKey: FleshweaverSpike, BumpRules.OuterBoss: true } ||
                triggerKind != "OnHit" || rule.CharacterId != FleshweaverSpikeMiniId ||
                mini.CharacterId != FleshweaverSpikeMiniId || mini.Unit.AssetKey != FleshweaverSpikeTrialMini ||
                rule.Tests is not { ShouldTest: true, FailToCast: false, CancelSubsequent: false, StrictTargets: false,
                    DevotionRequirement: 0, DevotionRequirementMax: 0 })
                return false;
            CombatRelicState[] owners = state.Context!.Relics?.Where(owner => owner.DataId == FleshweaverSpikeTrialOwnerId).ToArray() ??
                Array.Empty<CombatRelicState>();
            if (owners.Length != 1 || owners[0] is not { AssetKey: FleshweaverSpikeTrialOwnerKey, IsCovenant: false,
                    DisallowedInPlacementPhase: false, SpawnStatuses: [
                        { EffectIndex: 2, TargetPlayer: true, TargetEnemy: true, RequiredSubtype: "SubtypesData_BossAll",
                            SubtypeIsNone: false, SubtypeIsPyre: false, Conditions.Count: 0,
                            Statuses: [{ Id: "mass", Stacks: 4, ParamInt: 2 }] }] } owner ||
                !owner.EffectTypes.SequenceEqual(new[] { "RelicEffectOnSpawnHeroSubstitution", "RelicEffectNull",
                    "RelicEffectAddStatusEffectOnSpawn", "RelicEffectNull", "RelicEffectNull" }))
                return false;
            return mini.HeroDefinition is { Ascends: true, Loops: false, CompanionBoss: false, BirthStats: null,
                ExternalInteractions.Count: 0,
                Unit: { Id: 0, AssetKey: FleshweaverSpikeTrialMini, Team: CombatTeam.Enemy, BaseAttack: 3, Attack: 3,
                    Health: 9, MaxHealth: 9, CanAttack: true, IsBoss: false, EndsBattleOnDeath: false, SpawnerCardId: 0,
                    Size: 1, Subtypes.Count: 0, Ability: null, Modifiers.Upgrades.Count: 0,
                    Statuses: [{ Id: "rooted", Stacks: 1 }, { Id: "spikes", Stacks: 2, ParamInt: 1 }],
                    Triggers: [{ Kind: "OnSpawn", IgnoreSilence: true, Once: false, RemoveOnRelentlessChange: false,
                        Effects: [{ Type: "CardEffectFloorRearrange", Action: {
                            Type: "FloorRearrange", Target: "Self", Value: 0, Range: null } }] }] } };
        }

        private static bool KnownAthaneRock(SummonUnitDefinition unit) =>
            unit.HeroDefinition is { Ascends: false, Loops: false, CompanionBoss: false, BirthStats: null,
                Unit: { Id: 0, AssetKey: "SpikyRockT3_Chosen_Boss_BasicNonAttack", Team: CombatTeam.Enemy,
                    BaseAttack: 0, Attack: 0, Health: 250, MaxHealth: 250, CanAttack: false, IsBoss: false,
                    EndsBattleOnDeath: false, SpawnerCardId: 0, Size: 3, Modifiers.Upgrades.Count: 0,
                    Statuses: [{ Id: "spikes", Stacks: 7 }], Triggers: [
                        { Kind: "OnHit", Effects: [{ Type: "CardEffectBuffDamage", Value: 4,
                            Action: { Type: "BuffAttack", Target: "Tower", Value: 4, AllowEnemy: true, AllowPlayer: false,
                                Filters.Subtype: "SubtypesData_BossT3_ef0103da-a050-4ebb-af3a-b3e285db5e0c" } }] },
                        { Kind: "OnSpawn", IgnoreSilence: true, Effects: [
                            { Type: "CardEffectFloorRearrange", Action: { Type: "FloorRearrange", Target: "Self" } },
                            { Type: "CardEffectAddStatusEffect", Action: { Type: "AddStatus", Target: "Self",
                                Statuses: [{ Id: "immobile", Stacks: 1 }] } }] }] } };
        internal static string? Validate(RoomCombatState state, TriggeredSummonRule? rule,
            CombatUnit? actor = null, string? triggerKind = null)
        {
            TriggeredSummonCatalog? catalog = state.Context?.SummonCatalog;
            if (rule == null || catalog == null || state.Context?.SpawnPoints == null || state.Context.NextUnitId == null)
                return "Triggered summons require captured definitions, physical positions and shared identity allocation.";
            if (catalog.Units.Select(unit => unit.CharacterId).Distinct().Count() != catalog.Units.Count ||
                catalog.Cards.Select(card => card.Creation.DataId).Distinct().Count() != catalog.Cards.Count ||
                catalog.Rooms.Select(room => room.RoomIndex).Distinct().Count() != catalog.Rooms.Count ||
                rule.FirstSpawnedUnitId < 0 || rule.FirstSpawnedUnitId >= state.Context.NextUnitId)
                return "Invalid triggered summon catalog or retained first birth.";
            bool athane = rule.EnemyHero && rule.CharacterId.Length == 0 && rule.Pool.Count == 1 &&
                rule.Tests is { ShouldTest: true, FailToCast: false, CancelSubsequent: false } &&
                catalog.Units.Any(unit => unit.CharacterId == rule.Pool[0] && KnownAthaneRock(unit));
            foreach (string id in new[] { rule.CharacterId, rule.AdditionalCharacterId }.Concat(rule.Pool).Where(id => id.Length > 0))
            {
                SummonUnitDefinition? unit = catalog.Units.FirstOrDefault(item => item.CharacterId == id);
                if (unit == null || unit.Unit.IsPyre || unit.ExternalInteractions.Count > 0)
                    return unit == null ? "Missing triggered summon unit definition." : "Unmodeled triggered summon definition: " + string.Join("; ", unit.ExternalInteractions);
                if (rule.EnemyHero && (unit.HeroDefinition == null || unit.HeroDefinition.Unit.Team != CombatTeam.Enemy ||
                    unit.HeroDefinition.Unit.IsBoss == true || unit.HeroDefinition.Unit.EndsBattleOnDeath ||
                    !athane && unit.HeroDefinition.Unit.AssetKey is not ("Boss_Level3_MiniFleshweaver_MoreMass" or
                        "Boss_Level3_MiniFleshweaver_Corruption" or "Boss_Level3_MiniFleshweaver_Spike") &&
                    !KnownFleshweaverSpikeTrial(state, actor, triggerKind, rule, unit)))
                    return "Triggered enemy birth requires its calibrated original Fleshweaver mini definition.";
                if (rule.EnemyHero)
                {
                    string? stats = EnemySpawningModel.ValidateBirthStats(unit.HeroDefinition!, state.Context);
                    if (stats != null) return stats;
                }
            }
            if (rule.EnemyHero && (rule.Count != 1 || rule.Pool.Count != 0 && !athane || !rule.IgnoreCardUpgrades || rule.HasParentCard ||
                rule.AdditionalCharacterId.Length != 0 || rule.Upgrade != null || rule.FirstSpawnedUnitId != 0 ||
                state.Context.Enchantments?.AutomaticLifecycle != true))
                return "Triggered enemy birth requires its original single-spawn effect and complete lifecycle world.";
            if (rule.CharacterId.Length == 0 && rule.Pool.Count == 0) return "Triggered summon has no character or pool.";
            if (rule.Upgrade?.ExternalInteractions.Count > 0) return "Unmodeled triggered summon upgrade.";
            if (!catalog.Rooms.Any(room => room.RoomIndex == state.RoomIndex)) return "Missing triggered summon room gates.";
            if (state.Context.StatusRules.All(status => status.Id != "cardless")) return "Missing triggered summon cardless status.";
            return null;
        }
        internal static CombatUnit WithSourceMatch(CombatUnit source, bool matches)
        {
            UnitModifiers? m = source.Modifiers;
            var modifiers = m == null ? null : new UnitModifiers(m.AttackDamage, m.AttackDamageAdded, m.DamageBuff,
                m.RawSize, m.EquipmentLimit, m.CanBeHealed, m.IsClone, m.Upgrades, m.HealthFromUpgrades, matches,
                m.DamageBuffFromStatusEffects, m.MagebladeDamagePerMagicPower, m.StatusAttackModifiers, m.IsChampion, m.SelfAttackRules, m.RoomResources, m.RoomStatuses, m.RoomTriggerCounts, m.SpawnCopySubtype, m.Graft, m.SynergizeStatuses, m.Sentry, m.TroopAura);
            return new CombatUnit(source.Id, source.AssetKey, source.Team, source.BaseAttack, source.Health, source.MaxHealth,
                source.CanAttack, source.IsPyre, source.EndsBattleOnDeath, source.Statuses, source.Triggers, source.SpawnerCardId,
                source.Size, source.StatusImmunities, source.Subtypes, modifiers, source.IsBoss, source.LastAttackerId,
                source.StatusRegistry, source.EquipmentCards, source.NextTriggerId, source.Ability, source.StatusDictionary,
                source.AbilityRules, source.HordeDefinition, source.IsSpawning, source.SacrificeCardId, source.DeathState, bumpRules: source.BumpRules, lastFeederId: source.LastFeederId, moonPhaseMask: source.MoonPhaseMask, damageHistory: source.DamageHistory);
        }
    }
}
