using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardTraitRefreshState
    {
        public CardTraitCompositionState Composition { get; }
        public bool Dirty { get; }
        public int PermanentCount { get; }
        public int TemporaryCount { get; }
        public int CachedPermanentCount { get; }
        public int CachedTemporaryCount { get; }
        public CardTraitRefreshState(CardTraitCompositionState composition, bool dirty, int permanentCount,
            int temporaryCount, int cachedPermanentCount, int cachedTemporaryCount)
        { Composition = composition; Dirty = dirty; PermanentCount = permanentCount; TemporaryCount = temporaryCount;
            CachedPermanentCount = cachedPermanentCount; CachedTemporaryCount = cachedTemporaryCount; }
    }
    public static class CardTraitRefreshModel
    {
        public static CardTraitRefreshState Ensure(CardTraitRefreshState source)
        {
            if (!source.Dirty && source.PermanentCount == source.CachedPermanentCount && source.TemporaryCount == source.CachedTemporaryCount) return source;
            return new CardTraitRefreshState(CardTraitCompositionModel.Refresh(source.Composition), false,
                source.PermanentCount, source.TemporaryCount, source.PermanentCount, source.TemporaryCount);
        }
    }
    public sealed class CardMaskDefinition
    {
        public string DataId { get; }
        public string CardType { get; }
        public string Rarity { get; }
        public bool IsSpawner { get; }
        public bool HasSpawnData { get; }
        public bool CanAttack { get; }
        public IReadOnlyList<string> Subtypes { get; }
        public IReadOnlyList<UpgradeMaskStatus> StartingStatuses { get; }
        public IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> MainEffectStatuses { get; }
        public IReadOnlyList<string> MainEffectNames { get; }
        public string? LinkedClan { get; }
        public bool XCost { get; }
        public int BaseSize { get; }
        public int TargetMode { get; }
        public bool AuthoredUnitAbility { get; }
        public bool EquipmentUnitAbility { get; }
        public bool AuthoredGraft { get; }
        public CardMaskDefinition(string dataId, string cardType, string rarity, bool isSpawner, bool hasSpawnData, bool canAttack,
            IReadOnlyList<string> subtypes, IReadOnlyList<UpgradeMaskStatus> startingStatuses,
            IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> mainEffectStatuses, IReadOnlyList<string> mainEffectNames,
            string? linkedClan, bool xCost, int baseSize, int targetMode, bool authoredUnitAbility, bool equipmentUnitAbility, bool authoredGraft)
        {
            DataId = dataId; CardType = cardType; Rarity = rarity; IsSpawner = isSpawner; HasSpawnData = hasSpawnData; CanAttack = canAttack;
            Subtypes = Array.AsReadOnly(subtypes.ToArray()); StartingStatuses = Array.AsReadOnly(startingStatuses.ToArray());
            MainEffectStatuses = CardUpgradeMaskRule.CopyLists(mainEffectStatuses); MainEffectNames = Array.AsReadOnly(mainEffectNames.ToArray());
            LinkedClan = linkedClan; XCost = xCost; BaseSize = baseSize; TargetMode = targetMode;
            AuthoredUnitAbility = authoredUnitAbility; EquipmentUnitAbility = equipmentUnitAbility; AuthoredGraft = authoredGraft;
        }
    }
    public sealed class CardMaskUpgrade
    {
        public string DataId { get; }
        public CardStatModifier Stats { get; }
        public IReadOnlyList<UpgradeMaskStatus> Statuses { get; }
        public bool HasIcon { get; }
        public bool HideIcon { get; }
        public bool RegionRun { get; }
        public bool UnitAbility { get; }
        public CardMaskUpgrade(string dataId, CardStatModifier stats, IReadOnlyList<UpgradeMaskStatus> statuses,
            bool hasIcon, bool hideIcon, bool regionRun, bool unitAbility)
        { DataId = dataId; Stats = stats; Statuses = Array.AsReadOnly(statuses.ToArray());
            HasIcon = hasIcon; HideIcon = hideIcon; RegionRun = regionRun; UnitAbility = unitAbility; }
    }
    public sealed class CardMaskModifiers
    {
        public CardStatModifier Offsets { get; }
        public IReadOnlyList<CardMaskUpgrade> Upgrades { get; }
        public CardMaskModifiers(CardStatModifier offsets, IReadOnlyList<CardMaskUpgrade> upgrades)
        { Offsets = offsets; Upgrades = Array.AsReadOnly(upgrades.ToArray()); }
        internal CardModifiers Numbers() => new CardModifiers(Offsets, Upgrades.Select(upgrade =>
            new CardUpgradeModifier(upgrade.DataId, "", upgrade.Stats, Array.Empty<CombatStatus>(), false, false, false, 0, 0, Array.Empty<string>())).ToArray(), 0, Array.Empty<string>());
    }
    public sealed class CardOwnedMaskState
    {
        public CardMaskDefinition Definition { get; }
        public int BaseCost { get; }
        public CardMaskModifiers Permanent { get; }
        public CardMaskModifiers Temporary { get; }
        public CardTraitRefreshState Traits { get; }
        public IReadOnlyList<string> InstalledCastEffects { get; }
        public bool Purified { get; }
        public bool PermanentGraft { get; }
        public CardOwnedMaskState(CardMaskDefinition definition, int baseCost, CardMaskModifiers permanent, CardMaskModifiers temporary,
            CardTraitRefreshState traits, IReadOnlyList<string> installedCastEffects, bool purified, bool permanentGraft)
        { Definition = definition; BaseCost = baseCost; Permanent = permanent; Temporary = temporary; Traits = traits;
            InstalledCastEffects = Array.AsReadOnly(installedCastEffects.ToArray()); Purified = purified; PermanentGraft = permanentGraft; }
        internal CardOwnedMaskState WithTraits(CardTraitRefreshState traits) => new CardOwnedMaskState(Definition, BaseCost, Permanent,
            Temporary, traits, InstalledCastEffects, Purified, PermanentGraft);
    }
    public sealed class CardOwnedMaskResult
    {
        public CardOwnedMaskState State { get; }
        public CardUpgradeMaskCard Card { get; }
        public CardOwnedMaskResult(CardOwnedMaskState state, CardUpgradeMaskCard card) { State = state; Card = card; }
    }
    // Read a branch's values, retaining native lazy trait-refresh side effects.
    // Installing/removing upgrades and trait/trigger callbacks are caller lifecycles.
    public static class CardOwnedMaskModel
    {
        public static CardOwnedMaskResult Resolve(CardOwnedMaskState source, bool ignoreTemporaryCost = false)
        {
            var state = source.WithTraits(CardTraitRefreshModel.Ensure(source.Traits)); var definition = state.Definition;
            var upgrades = state.Permanent.Upgrades.Concat(state.Temporary.Upgrades).ToArray();
            var permanent = state.Permanent.Numbers(); var temporary = state.Temporary.Numbers();
            var modifiers = ignoreTemporaryCost ? new[] { permanent } : new[] { permanent, temporary };
            var statuses = CardStatusCompositionModel.Resolve(new CardStatusCompositionState(definition.StartingStatuses,
                state.Permanent.Upgrades.Select(upgrade => upgrade.Statuses).ToArray(), state.Temporary.Upgrades.Select(upgrade => upgrade.Statuses).ToArray(),
                state.Purified, definition.CardType == "Equipment" || (definition.IsSpawner && definition.HasSpawnData)));
            var traits = state.Traits.Composition.CombinedTraits ?? Array.Empty<CardTraitValue>();
            var effects = definition.MainEffectNames.Concat(state.InstalledCastEffects).Concat(traits.SelectMany(trait => trait.ParameterUpgradeCastEffects)).ToArray();
            var card = new CardUpgradeMaskCard(definition.DataId, definition.CardType, definition.Rarity, definition.IsSpawner, definition.CanAttack,
                definition.Subtypes, statuses, definition.MainEffectStatuses, traits.Select(trait => trait.DeclaredName!).ToArray(), effects, definition.LinkedClan,
                CardModifierModel.UpgradedStat(state.BaseCost, "Cost", true, modifiers), definition.XCost,
                definition.IsSpawner ? Math.Max(1, Math.Min(6, CardModifierModel.UpgradedStat(definition.BaseSize, "Size", false, permanent, temporary))) : 0,
                definition.TargetMode, true, definition.AuthoredUnitAbility || definition.EquipmentUnitAbility || upgrades.Any(upgrade => upgrade.UnitAbility),
                (!state.Purified && definition.AuthoredGraft) || state.PermanentGraft,
                upgrades.Count(upgrade => upgrade.HasIcon && !upgrade.HideIcon && !upgrade.RegionRun), upgrades.Select(upgrade => upgrade.DataId).ToArray());
            return new CardOwnedMaskResult(state, card);
        }
    }
}
