using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardStatModifier
    {
        public int Damage { get; }
        public int Health { get; }
        public int Cost { get; }
        public int Heal { get; }
        public int Size { get; }
        public int XCost { get; }
        public int EquipmentLimit { get; }
        public int UpgradeSlotCount { get; }
        public CardStatModifier(int damage = 0, int health = 0, int cost = 0, int heal = 0, int size = 0,
            int xCost = 0, int equipmentLimit = 0, int upgradeSlotCount = 0)
        { Damage = damage; Health = health; Cost = cost; Heal = heal; Size = size; XCost = xCost;
            EquipmentLimit = equipmentLimit; UpgradeSlotCount = upgradeSlotCount; }
        internal int Value(string stat) => stat == "Damage" ? Damage : stat == "Health" ? Health : stat == "Cost" ? Cost :
            stat == "Heal" ? Heal : stat == "Size" ? Size : stat == "XCost" ? XCost : stat == "EquipmentLimit" ? EquipmentLimit : UpgradeSlotCount;
    }

    public sealed class CardUpgradeModifier
    {
        public string DataId { get; }
        public string AssetKey { get; }
        public CardStatModifier Stats { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public bool RemoveOnDiscard { get; }
        public bool Unique { get; }
        public bool ExcludeFromClones { get; }
        public int UnhealedHealth { get; }
        public int DamageBuff { get; }
        public bool RestrictSizeToRoomCapacity { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public CardUpgradeModifier(string dataId, string assetKey, CardStatModifier stats, IReadOnlyList<CombatStatus> statuses,
            bool removeOnDiscard, bool unique, bool excludeFromClones, int unhealedHealth, int damageBuff,
            IReadOnlyList<string> externalInteractions, bool restrictSizeToRoomCapacity = false)
        { DataId = dataId; AssetKey = assetKey; Stats = stats; Statuses = Array.AsReadOnly(statuses.ToArray());
            RemoveOnDiscard = removeOnDiscard; Unique = unique; ExcludeFromClones = excludeFromClones;
            UnhealedHealth = unhealedHealth; DamageBuff = damageBuff; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            RestrictSizeToRoomCapacity = restrictSizeToRoomCapacity; }
    }

    public sealed class CardModifiers
    {
        public CardStatModifier Offsets { get; }
        // Order is significant: native values with magnitude >= 99 clamp immediately.
        public IReadOnlyList<CardUpgradeModifier> Upgrades { get; }
        public int PersistentHealth { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public CardModifiers(CardStatModifier offsets, IReadOnlyList<CardUpgradeModifier> upgrades, int persistentHealth,
            IReadOnlyList<string> externalInteractions)
        { Offsets = offsets; Upgrades = Array.AsReadOnly(upgrades.ToArray()); PersistentHealth = persistentHealth;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); }
        public static CardModifiers Empty() => new CardModifiers(new CardStatModifier(), Array.Empty<CardUpgradeModifier>(), 0, Array.Empty<string>());
        internal CardModifiers OnDiscard()
        {
            var upgrades = Upgrades.ToList();
            for (int index = upgrades.Count - 1; index >= 0; index--)
                if (upgrades[index].RemoveOnDiscard)
                {
                    // Native RemoveUpgrade(state) removes the first matching nonempty data ID.
                    int removed = upgrades[index].DataId.Length == 0 ? index : upgrades.FindIndex(upgrade => upgrade.DataId == upgrades[index].DataId);
                    upgrades.RemoveAt(removed);
                }
            return new CardModifiers(Offsets, upgrades, PersistentHealth, ExternalInteractions);
        }
    }

    public sealed class CardInstanceState
    {
        public int InstanceId { get; }
        public string DataId { get; }
        public CardModifiers Permanent { get; }
        public CardModifiers Temporary { get; }
        public int LastPlayedCost { get; }
        public int LastForgedAmount { get; }
        public int PlayCount { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public CardInstanceState(int instanceId, string dataId, CardModifiers permanent, CardModifiers temporary,
            int lastPlayedCost, int lastForgedAmount, int playCount, IReadOnlyList<string> externalInteractions)
        { InstanceId = instanceId; DataId = dataId; Permanent = permanent; Temporary = temporary;
            LastPlayedCost = lastPlayedCost; LastForgedAmount = lastForgedAmount; PlayCount = playCount;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); }
        public static CardInstanceState Empty(int id, string dataId) => new CardInstanceState(id, dataId,
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, Array.Empty<string>());
        public CardInstanceState OnDiscard(bool played, int cost = 0) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary.OnDiscard(), played ? cost : LastPlayedCost, LastForgedAmount,
            PlayCount + (played ? 1 : 0), ExternalInteractions);
    }

    public static class CardModifierModel
    {
        internal static string? UnsupportedReason(CardInstanceState instance)
        {
            var interactions = instance.ExternalInteractions.Concat(instance.Permanent.ExternalInteractions)
                .Concat(instance.Temporary.ExternalInteractions).Concat(instance.Permanent.Upgrades.Concat(instance.Temporary.Upgrades)
                    .SelectMany(upgrade => upgrade.ExternalInteractions)).ToList();
            if (instance.Permanent.PersistentHealth != 0 || instance.Temporary.PersistentHealth != 0) interactions.Add("Persistent card health");
            return interactions.Count == 0 ? null : string.Join("; ", interactions.Distinct());
        }
        public static int UpgradedStat(int baseValue, string stat, bool enforceFloor, params CardModifiers[] modifiers)
        {
            if (!new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.Contains(stat))
                throw new ArgumentException("Unknown card statistic.", nameof(stat));
            int floor = enforceFloor ? stat == "Health" || stat == "Size" ? 1 :
                stat == "Damage" || stat == "Cost" || stat == "Heal" ? 0 : int.MinValue : int.MinValue;
            int ceiling = stat == "Cost" ? 99 : stat == "Size" ? 6 : int.MaxValue;
            int value = Clamp(baseValue);
            foreach (CardModifiers modifier in modifiers)
            foreach (int addition in new[] { modifier.Offsets.Value(stat) }.Concat(modifier.Upgrades.Select(upgrade => upgrade.Stats.Value(stat))))
                if (addition != 0) value = Math.Abs((long)addition) >= 99 ? Clamp(checked(value + addition)) : checked(value + addition);
            return Clamp(value);
            int Clamp(int number) => Math.Max(floor, Math.Min(ceiling, number));
        }

        public static CardPlayRule Resolve(CardPlayRule rule, CardInstanceState instance)
        {
            var interactions = rule.ExternalInteractions.Concat(instance.ExternalInteractions)
                .Concat(instance.Permanent.ExternalInteractions).Concat(instance.Temporary.ExternalInteractions)
                .Concat(instance.Permanent.Upgrades.Concat(instance.Temporary.Upgrades).SelectMany(upgrade => upgrade.ExternalInteractions)).ToList();
            if (rule.DataId != instance.DataId) interactions.Add("Card instance definition differs.");
            if (instance.Permanent.PersistentHealth != 0 || instance.Temporary.PersistentHealth != 0) interactions.Add("Persistent card health");
            CardModifiers[] modifiers = { instance.Permanent, instance.Temporary };
            CombatUnit? unit = rule.SpawnUnit;
            if (unit != null)
            {
                var statuses = unit.Statuses.ToDictionary(status => status.Id);
                foreach (CombatStatus added in modifiers.SelectMany(modifier => modifier.Upgrades).SelectMany(upgrade => upgrade.Statuses))
                {
                    if (unit.StatusImmunities.Contains(added.Id)) continue;
                    statuses.TryGetValue(added.Id, out CombatStatus? existing);
                    int count = Math.Min(9999, checked((existing?.Stacks ?? 0) + added.Stacks));
                    if (count <= 0) statuses.Remove(added.Id);
                    else statuses[added.Id] = (existing ?? added).WithStacks(count);
                }
                int health = UpgradedStat(unit.MaxHealth, "Health", true, modifiers);
                UnitModifiers? original = unit.Modifiers;
                UnitModifiers? unitModifiers = original == null ? null : new UnitModifiers(
                    UpgradedStat(original.AttackDamage, "Damage", true, modifiers), original.AttackDamageAdded, original.DamageBuff,
                    UpgradedStat(original.RawSize, "Size", false, modifiers),
                    Math.Max(1, Math.Min(4, UpgradedStat(original.EquipmentLimit, "EquipmentLimit", true, modifiers))),
                    original.CanBeHealed, original.IsClone, original.Upgrades, original.HealthFromUpgrades, original.SpawnerMatchesDefinition);
                unit = new CombatUnit(unit.Id, unit.AssetKey, unit.Team, UpgradedStat(unit.BaseAttack, "Damage", true, modifiers),
                    health, health, unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath, statuses.Values.ToArray(), unit.Triggers,
                    unit.SpawnerCardId, Math.Max(1, UpgradedStat(unit.Size, "Size", false, modifiers)), unit.StatusImmunities, unit.Subtypes, unitModifiers);
            }
            CardActionEffect[] effects = rule.Effects.Select(effect => new CardActionEffect(effect.Type, effect.Target,
                effect.Type == "Damage" ? UpgradedStat(UpgradedStat(effect.Value, "Damage", true, instance.Permanent),
                    "Damage", true, instance.Temporary) : effect.Value, effect.AllowEnemy, effect.AllowPlayer, effect.Statuses, effect.Upgrade, effect.Lifetime)).ToArray();
            return new CardPlayRule(rule.DataId, rule.AssetKey, UpgradedStat(rule.Cost, "Cost", true, modifiers), rule.Effect,
                rule.Destination, unit, interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), effects);
        }
    }
}
