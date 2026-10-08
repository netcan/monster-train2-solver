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
        public TriggeredSummonRule(string characterId, string additionalCharacterId, IReadOnlyList<string> pool,
            int count, bool ignoreCardUpgrades, bool hasParentCard, CardUpgradeModifier? upgrade,
            CardEffectTests tests, int firstSpawnedUnitId = 0)
        { CharacterId = characterId; AdditionalCharacterId = additionalCharacterId;
            Pool = Array.AsReadOnly(pool.ToArray()); Count = count; IgnoreCardUpgrades = ignoreCardUpgrades;
            HasParentCard = hasParentCard; Upgrade = upgrade; Tests = tests; FirstSpawnedUnitId = firstSpawnedUnitId; }
        internal TriggeredSummonRule WithFirstSpawned(int id) => new TriggeredSummonRule(CharacterId,
            AdditionalCharacterId, Pool, Count, IgnoreCardUpgrades, HasParentCard, Upgrade, Tests, id);
    }
    public sealed class SummonUnitDefinition
    {
        public string CharacterId { get; }
        public CombatUnit Unit { get; }
        public CardCreationRule? Fallback { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public SummonUnitDefinition(string characterId, CombatUnit unit, CardCreationRule? fallback,
            IReadOnlyList<string> externalInteractions)
        { CharacterId = characterId; Unit = unit; Fallback = fallback;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); }
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
        internal static string? Validate(RoomCombatState state, TriggeredSummonRule? rule)
        {
            TriggeredSummonCatalog? catalog = state.Context?.SummonCatalog;
            if (rule == null || catalog == null || state.Context?.SpawnPoints == null || state.Context.NextUnitId == null)
                return "Triggered summons require captured definitions, physical positions and shared identity allocation.";
            if (catalog.Units.Select(unit => unit.CharacterId).Distinct().Count() != catalog.Units.Count ||
                catalog.Cards.Select(card => card.Creation.DataId).Distinct().Count() != catalog.Cards.Count ||
                catalog.Rooms.Select(room => room.RoomIndex).Distinct().Count() != catalog.Rooms.Count ||
                rule.FirstSpawnedUnitId < 0 || rule.FirstSpawnedUnitId >= state.Context.NextUnitId)
                return "Invalid triggered summon catalog or retained first birth.";
            foreach (string id in new[] { rule.CharacterId, rule.AdditionalCharacterId }.Concat(rule.Pool).Where(id => id.Length > 0))
            {
                SummonUnitDefinition? unit = catalog.Units.FirstOrDefault(item => item.CharacterId == id);
                if (unit == null || unit.Unit.IsPyre || unit.ExternalInteractions.Count > 0)
                    return unit == null ? "Missing triggered summon unit definition." : "Unmodeled triggered summon definition: " + string.Join("; ", unit.ExternalInteractions);
            }
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
                m.RawSize, m.EquipmentLimit, m.CanBeHealed, m.IsClone, m.Upgrades, m.HealthFromUpgrades, matches);
            return new CombatUnit(source.Id, source.AssetKey, source.Team, source.BaseAttack, source.Health, source.MaxHealth,
                source.CanAttack, source.IsPyre, source.EndsBattleOnDeath, source.Statuses, source.Triggers, source.SpawnerCardId,
                source.Size, source.StatusImmunities, source.Subtypes, modifiers, source.IsBoss, source.LastAttackerId,
                source.StatusRegistry, source.EquipmentCards, source.NextTriggerId, source.Ability, source.StatusDictionary,
                source.AbilityRules, source.HordeDefinition, source.IsSpawning, source.SacrificeCardId, source.DeathState, bumpRules: source.BumpRules);
        }
    }
}
