using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UpgradeMaskContent<T>
    {
        public IReadOnlyList<T> Required { get; }
        public IReadOnlyList<T> Excluded { get; }
        public int RequiredOperator { get; }
        public int ExcludedOperator { get; }
        public UpgradeMaskContent(IReadOnlyList<T>? required = null, IReadOnlyList<T>? excluded = null,
            int requiredOperator = 0, int excludedOperator = 0)
        { Required = Array.AsReadOnly((required ?? Array.Empty<T>()).ToArray());
            Excluded = Array.AsReadOnly((excluded ?? Array.Empty<T>()).ToArray());
            RequiredOperator = requiredOperator; ExcludedOperator = excludedOperator; }
    }

    public sealed class UpgradeMaskStatus
    {
        public string Id { get; }
        public int Count { get; }
        public bool FromPermanentUpgrade { get; }
        public UpgradeMaskStatus(string id, int count, bool fromPermanentUpgrade = false)
        { Id = id; Count = count; FromPermanentUpgrade = fromPermanentUpgrade; }
    }

    public sealed class CardUpgradeMaskRule
    {
        public string AssetKey { get; }
        public string CardType { get; }
        public IReadOnlyList<string> AdditionalCardTypes { get; }
        public UpgradeMaskContent<string> Rarities { get; }
        public UpgradeMaskContent<string> Subtypes { get; }
        public UpgradeMaskContent<UpgradeMaskStatus> Statuses { get; }
        public UpgradeMaskContent<string> Traits { get; }
        public UpgradeMaskContent<string> Effects { get; }
        public UpgradeMaskContent<string> LinkedClans { get; }
        public UpgradeMaskContent<string> Upgrades { get; }
        public IReadOnlyList<IReadOnlyList<string>> AllowedPools { get; }
        public IReadOnlyList<IReadOnlyList<string>> DisallowedPools { get; }
        public IReadOnlyList<int> RequiredSizes { get; }
        public IReadOnlyList<int> ExcludedSizes { get; }
        public float MinCost { get; }
        public float MaxCost { get; }
        public int TargetMode { get; }
        public bool ExcludeNonAttackingMonsters { get; }
        public bool RequireXCost { get; }
        public bool ExcludeXCost { get; }
        public bool ExcludeIfHasUnitAbility { get; }
        public bool ExcludeIfHasGraftedEquipment { get; }
        public bool ExcludeIfHasAnyUpgrades { get; }
        public bool ExcludeIfHasNoUpgrades { get; }

        public CardUpgradeMaskRule(string assetKey = "", string cardType = "Invalid",
            IReadOnlyList<string>? additionalCardTypes = null, UpgradeMaskContent<string>? rarities = null,
            UpgradeMaskContent<string>? subtypes = null, UpgradeMaskContent<UpgradeMaskStatus>? statuses = null,
            UpgradeMaskContent<string>? traits = null, UpgradeMaskContent<string>? effects = null,
            UpgradeMaskContent<string>? linkedClans = null, UpgradeMaskContent<string>? upgrades = null,
            IReadOnlyList<IReadOnlyList<string>>? allowedPools = null, IReadOnlyList<IReadOnlyList<string>>? disallowedPools = null,
            IReadOnlyList<int>? requiredSizes = null, IReadOnlyList<int>? excludedSizes = null,
            float minCost = 0, float maxCost = 99, int targetMode = 0, bool excludeNonAttackingMonsters = false,
            bool requireXCost = false, bool excludeXCost = false, bool excludeIfHasUnitAbility = false,
            bool excludeIfHasGraftedEquipment = false, bool excludeIfHasAnyUpgrades = false, bool excludeIfHasNoUpgrades = false)
        {
            AssetKey = assetKey; CardType = cardType;
            AdditionalCardTypes = Array.AsReadOnly((additionalCardTypes ?? Array.Empty<string>()).ToArray());
            Rarities = rarities ?? new UpgradeMaskContent<string>(); Subtypes = subtypes ?? new UpgradeMaskContent<string>();
            Statuses = statuses ?? new UpgradeMaskContent<UpgradeMaskStatus>(); Traits = traits ?? new UpgradeMaskContent<string>();
            Effects = effects ?? new UpgradeMaskContent<string>(); LinkedClans = linkedClans ?? new UpgradeMaskContent<string>();
            Upgrades = upgrades ?? new UpgradeMaskContent<string>();
            AllowedPools = CopyLists(allowedPools); DisallowedPools = CopyLists(disallowedPools);
            RequiredSizes = Array.AsReadOnly((requiredSizes ?? Array.Empty<int>()).ToArray());
            ExcludedSizes = Array.AsReadOnly((excludedSizes ?? Array.Empty<int>()).ToArray());
            MinCost = minCost; MaxCost = maxCost; TargetMode = targetMode;
            ExcludeNonAttackingMonsters = excludeNonAttackingMonsters; RequireXCost = requireXCost; ExcludeXCost = excludeXCost;
            ExcludeIfHasUnitAbility = excludeIfHasUnitAbility; ExcludeIfHasGraftedEquipment = excludeIfHasGraftedEquipment;
            ExcludeIfHasAnyUpgrades = excludeIfHasAnyUpgrades; ExcludeIfHasNoUpgrades = excludeIfHasNoUpgrades;
        }
        internal static IReadOnlyList<IReadOnlyList<T>> CopyLists<T>(IReadOnlyList<IReadOnlyList<T>>? source) =>
            Array.AsReadOnly((source ?? Array.Empty<IReadOnlyList<T>>()).Select(list =>
                (IReadOnlyList<T>)Array.AsReadOnly(list.ToArray())).ToArray());
    }

    // Inputs are values/classification, never native filter decisions. The battle adapter
    // must derive changing cost/size/status/upgrade values from the current branch.
    public sealed class CardUpgradeMaskCard
    {
        public string DataId { get; }
        public string CardType { get; }
        public string Rarity { get; }
        public bool IsSpawner { get; }
        public bool CanAttack { get; }
        public IReadOnlyList<string> Subtypes { get; }
        public IReadOnlyList<UpgradeMaskStatus> SpawnStatuses { get; }
        public IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> MainEffectStatuses { get; }
        public IReadOnlyList<string> Traits { get; }
        public IReadOnlyList<string> Effects { get; }
        public string? LinkedClan { get; }
        public int CostWithoutTraits { get; }
        public bool XCost { get; }
        public int Size { get; }
        public int TargetMode { get; }
        public bool IsCardState { get; }
        public bool HasUnitAbility { get; }
        public bool HasGraftedEquipment { get; }
        public int VisibleUpgradeCount { get; }
        public IReadOnlyList<string> UpgradeIds { get; }

        public CardUpgradeMaskCard(string dataId, string cardType, string rarity, bool isSpawner, bool canAttack,
            IReadOnlyList<string> subtypes, IReadOnlyList<UpgradeMaskStatus> spawnStatuses,
            IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> mainEffectStatuses, IReadOnlyList<string> traits,
            IReadOnlyList<string> effects, string? linkedClan, int costWithoutTraits, bool xCost, int size, int targetMode,
            bool isCardState = false, bool hasUnitAbility = false, bool hasGraftedEquipment = false,
            int visibleUpgradeCount = 0, IReadOnlyList<string>? upgradeIds = null)
        {
            DataId = dataId; CardType = cardType; Rarity = rarity; IsSpawner = isSpawner; CanAttack = canAttack;
            Subtypes = Array.AsReadOnly(subtypes.ToArray()); SpawnStatuses = Array.AsReadOnly(spawnStatuses.ToArray());
            MainEffectStatuses = CardUpgradeMaskRule.CopyLists(mainEffectStatuses);
            Traits = Array.AsReadOnly(traits.ToArray()); Effects = Array.AsReadOnly(effects.ToArray()); LinkedClan = linkedClan;
            CostWithoutTraits = costWithoutTraits; XCost = xCost; Size = size; TargetMode = targetMode; IsCardState = isCardState;
            HasUnitAbility = hasUnitAbility; HasGraftedEquipment = hasGraftedEquipment; VisibleUpgradeCount = visibleUpgradeCount;
            UpgradeIds = Array.AsReadOnly((upgradeIds ?? Array.Empty<string>()).ToArray());
        }
    }

    public sealed class CardUpgradeMaskCharacter
    {
        public IReadOnlyList<string> Subtypes { get; }
        public IReadOnlyList<string> RegisteredStatusIds { get; }
        public int Size { get; }
        public CardUpgradeMaskCharacter(IReadOnlyList<string> subtypes, IReadOnlyList<string> registeredStatusIds, int size)
        { Subtypes = Array.AsReadOnly(subtypes.ToArray()); RegisteredStatusIds = Array.AsReadOnly(registeredStatusIds.ToArray()); Size = size; }
    }

    public static class CardUpgradeMaskModel
    {
        public static bool FilterCard(CardUpgradeMaskRule mask, CardUpgradeMaskCard? card, bool monstersAreAllSubtypes = false)
        {
            if (card == null) return true;
            bool allowsMonster = AllowsType(mask, "Monster");
            bool pass = AllowsType(mask, card.CardType) && Content(new[] { card.Rarity }, mask.Rarities);
            if (mask.Subtypes.Required.Count > 0 && !allowsMonster) pass = false;
            pass &= mask.AllowedPools.All(pool => pool.Count == 0 || pool.Contains(card.DataId));
            pass &= mask.DisallowedPools.All(pool => !pool.Contains(card.DataId));
            if (card.IsSpawner)
            {
                if (allowsMonster && mask.ExcludeNonAttackingMonsters) pass &= card.CanAttack;
                if (!monstersAreAllSubtypes) pass &= Content(card.Subtypes, mask.Subtypes);
                pass &= StatusContent(card.SpawnStatuses, mask.Statuses) && Sizes(card.Size, mask);
            }
            else
                foreach (var statuses in card.MainEffectStatuses) pass &= StatusContent(statuses, mask.Statuses);
            pass &= Content(card.Traits, mask.Traits);
            if (mask.RequireXCost) pass &= card.XCost;
            if (mask.ExcludeXCost) pass &= !card.XCost;
            if (!card.XCost) pass &= (float)card.CostWithoutTraits >= mask.MinCost && (float)card.CostWithoutTraits <= mask.MaxCost;
            if (card.IsCardState)
            {
                if (mask.ExcludeIfHasUnitAbility) pass &= !card.HasUnitAbility;
                if (mask.ExcludeIfHasGraftedEquipment) pass &= !card.HasGraftedEquipment;
                if (mask.ExcludeIfHasAnyUpgrades) pass &= card.VisibleUpgradeCount == 0;
                if (mask.ExcludeIfHasNoUpgrades) pass &= card.VisibleUpgradeCount > 0;
                pass &= Content(card.UpgradeIds, mask.Upgrades);
            }
            if (mask.TargetMode != 0) pass &= (mask.TargetMode & card.TargetMode) == card.TargetMode;
            pass &= Content(card.Effects, mask.Effects);
            pass &= Content(string.IsNullOrEmpty(card.LinkedClan) ? Array.Empty<string>() : new[] { card.LinkedClan! }, mask.LinkedClans);
            return pass;
        }

        public static bool FilterCharacter(CardUpgradeMaskRule mask, CardUpgradeMaskCharacter character, bool monstersAreAllSubtypes = false) =>
            (monstersAreAllSubtypes || Content(character.Subtypes, mask.Subtypes)) &&
            mask.Statuses.Required.All(status => character.RegisteredStatusIds.Contains(status.Id)) &&
            mask.Statuses.Excluded.All(status => !character.RegisteredStatusIds.Contains(status.Id)) && Sizes(character.Size, mask);

        private static bool AllowsType(CardUpgradeMaskRule mask, string type) => mask.CardType == "Invalid" ||
            mask.CardType == type || mask.AdditionalCardTypes.Any(added => added != "Invalid" && added == type);
        private static bool Sizes(int size, CardUpgradeMaskRule mask) => mask.RequiredSizes.All(required => size == required) &&
            mask.ExcludedSizes.All(excluded => size != excluded);
        private static bool Content<T>(IReadOnlyList<T> source, UpgradeMaskContent<T> mask) =>
            Content(source, mask, (item, list) => list.Contains(item));
        private static bool StatusContent(IReadOnlyList<UpgradeMaskStatus> source, UpgradeMaskContent<UpgradeMaskStatus> mask) =>
            Content(source, mask, (item, list) => list.Any(other => item.Id == other.Id && item.Count >= other.Count));
        private static bool Content<T>(IReadOnlyList<T> source, UpgradeMaskContent<T> mask, Func<T, IReadOnlyList<T>, bool> matches)
        {
            if (mask.Required.Count == 0 && mask.Excluded.Count == 0) return true;
            if (source.Count == 0) return mask.Required.Count == 0;
            bool pass = true;
            // Native exclusion OR quantifies over source entries, unlike required OR.
            if (mask.Excluded.Count > 0)
            {
                if (mask.ExcludedOperator == 0) pass &= source.All(item => !matches(item, mask.Excluded));
                else if (mask.ExcludedOperator == 1) pass &= source.Any(item => !matches(item, mask.Excluded));
            }
            if (mask.Required.Count > 0)
            {
                if (mask.RequiredOperator == 0) pass &= mask.Required.All(item => matches(item, source));
                else if (mask.RequiredOperator == 1) pass &= mask.Required.Any(item => matches(item, source));
            }
            return pass;
        }
    }
}
