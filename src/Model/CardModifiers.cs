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
        public bool MagicPowerTraitScalingOnly { get; }
        public int? CloneDamageBase { get; }
        public int? CloneHealBase { get; }
        public int? EquipmentSourceCardId { get; }
        public int? EquipmentSourceUpgradeIndex { get; }
        public IReadOnlyList<CombatTrigger>? TriggerUpgrades { get; }
        public AbilityChangeRule? AbilityUpgrade { get; }
        public bool DoNotReplaceExistingAbility { get; }
        public CardUpgradeMaskMetadata? MaskMetadata { get; }
        public CardLifecycleUpgrade? Lifecycle { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public bool? CopyRemoveOnDiscard { get; }
        public bool? CopyUnique { get; }
        public bool? CopyExcludeFromClones { get; }
        public int? SharedReferenceId { get; }
        public CardUpgradeModifier(string dataId, string assetKey, CardStatModifier stats, IReadOnlyList<CombatStatus> statuses,
            bool removeOnDiscard, bool unique, bool excludeFromClones, int unhealedHealth, int damageBuff,
            IReadOnlyList<string> externalInteractions, bool restrictSizeToRoomCapacity = false, bool magicPowerTraitScalingOnly = false,
            int? cloneDamageBase = null, int? cloneHealBase = null,
            int? equipmentSourceCardId = null, int? equipmentSourceUpgradeIndex = null,
            IReadOnlyList<CombatTrigger>? triggerUpgrades = null, AbilityChangeRule? abilityUpgrade = null,
            bool doNotReplaceExistingAbility = false, CardUpgradeMaskMetadata? maskMetadata = null,
            bool? copyRemoveOnDiscard = null, bool? copyUnique = null, bool? copyExcludeFromClones = null,
            CardLifecycleUpgrade? lifecycle = null, int? sharedReferenceId = null)
        { DataId = dataId; AssetKey = assetKey; Stats = stats; Statuses = Array.AsReadOnly(statuses.ToArray());
            RemoveOnDiscard = removeOnDiscard; Unique = unique; ExcludeFromClones = excludeFromClones;
            UnhealedHealth = unhealedHealth; DamageBuff = damageBuff; ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            RestrictSizeToRoomCapacity = restrictSizeToRoomCapacity; MagicPowerTraitScalingOnly = magicPowerTraitScalingOnly;
            CloneDamageBase = cloneDamageBase; CloneHealBase = cloneHealBase;
            EquipmentSourceCardId = equipmentSourceCardId; EquipmentSourceUpgradeIndex = equipmentSourceUpgradeIndex;
            TriggerUpgrades = triggerUpgrades == null ? null : Array.AsReadOnly(triggerUpgrades.ToArray());
            AbilityUpgrade = abilityUpgrade; DoNotReplaceExistingAbility = doNotReplaceExistingAbility; MaskMetadata = maskMetadata;
            Lifecycle = lifecycle;
            SharedReferenceId = sharedReferenceId;
            CopyRemoveOnDiscard = copyRemoveOnDiscard; CopyUnique = copyUnique; CopyExcludeFromClones = copyExcludeFromClones; }
        public CardUpgradeModifier WithEquipmentSource(int cardId, int upgradeIndex) => new CardUpgradeModifier(DataId, AssetKey,
            Stats, Statuses, RemoveOnDiscard, Unique, ExcludeFromClones, UnhealedHealth, DamageBuff, ExternalInteractions,
            RestrictSizeToRoomCapacity, MagicPowerTraitScalingOnly, CloneDamageBase, CloneHealBase, cardId, upgradeIndex, TriggerUpgrades, AbilityUpgrade, DoNotReplaceExistingAbility, MaskMetadata,
            CopyRemoveOnDiscard, CopyUnique, CopyExcludeFromClones, Lifecycle, SharedReferenceId);
        internal CardUpgradeModifier WithScaledStats(int damage, int health) => WithStats(
            new CardStatModifier(damage, health, Stats.Cost, Stats.Heal, Stats.Size, Stats.XCost, Stats.EquipmentLimit, Stats.UpgradeSlotCount));
        internal CardUpgradeModifier WithForgePoints(int forgePoints)
        {
            var stats = new CardStatModifier(unchecked(Stats.Damage * forgePoints), unchecked(Stats.Health * forgePoints),
                Stats.Cost, Stats.Heal, Stats.Size, Stats.XCost, Stats.EquipmentLimit, Stats.UpgradeSlotCount);
            var statuses = Statuses.Select(status => status.WithStacks(unchecked(status.Stacks * forgePoints))).ToArray();
            CardUpgradeMaskMetadata? maskMetadata = MaskMetadata;
            if (maskMetadata != null)
                maskMetadata = new CardUpgradeMaskMetadata(maskMetadata.Statuses.Select(status =>
                    new UpgradeMaskStatus(status.Id, unchecked(status.Count * forgePoints), status.FromPermanentUpgrade)).ToArray(),
                    maskMetadata.HasIcon, maskMetadata.HideIcon, maskMetadata.RegionRun, maskMetadata.UnitAbility);
            CardLifecycleUpgrade? lifecycle = Lifecycle;
            if (lifecycle != null)
            {
                CardMaskUpgrade values = lifecycle.Values;
                var maskStatuses = values.Statuses.Select(status => new UpgradeMaskStatus(status.Id,
                    unchecked(status.Count * forgePoints), status.FromPermanentUpgrade)).ToArray();
                lifecycle = lifecycle.WithValues(new CardMaskUpgrade(values.DataId, stats, maskStatuses,
                    values.HasIcon, values.HideIcon, values.RegionRun, values.UnitAbility));
            }
            return new CardUpgradeModifier(DataId, AssetKey, stats, statuses, RemoveOnDiscard, Unique,
                ExcludeFromClones, UnhealedHealth, DamageBuff, ExternalInteractions, RestrictSizeToRoomCapacity,
                MagicPowerTraitScalingOnly, CloneDamageBase, CloneHealBase, EquipmentSourceCardId,
                EquipmentSourceUpgradeIndex, TriggerUpgrades, AbilityUpgrade, DoNotReplaceExistingAbility,
                maskMetadata, CopyRemoveOnDiscard, CopyUnique, CopyExcludeFromClones, lifecycle, SharedReferenceId);
        }
        internal CardUpgradeModifier WithStats(CardStatModifier stats) => new CardUpgradeModifier(DataId, AssetKey,
            stats, Statuses, RemoveOnDiscard, Unique, ExcludeFromClones, UnhealedHealth, DamageBuff, ExternalInteractions,
            RestrictSizeToRoomCapacity, MagicPowerTraitScalingOnly, CloneDamageBase, CloneHealBase, EquipmentSourceCardId, EquipmentSourceUpgradeIndex, TriggerUpgrades, AbilityUpgrade, DoNotReplaceExistingAbility, MaskMetadata,
            CopyRemoveOnDiscard, CopyUnique, CopyExcludeFromClones, Lifecycle == null ? null :
                Lifecycle.WithValues(new CardMaskUpgrade(Lifecycle.Values.DataId, stats, Lifecycle.Values.Statuses,
                    Lifecycle.Values.HasIcon, Lifecycle.Values.HideIcon, Lifecycle.Values.RegionRun, Lifecycle.Values.UnitAbility)), SharedReferenceId);
        internal CardUpgradeModifier RefreshCloneMagicPower() => new CardUpgradeModifier(DataId, AssetKey,
            new CardStatModifier(CloneDamageBase ?? Stats.Damage, Stats.Health, Stats.Cost, CloneHealBase ?? Stats.Heal,
                Stats.Size, Stats.XCost, Stats.EquipmentLimit, Stats.UpgradeSlotCount), Statuses, RemoveOnDiscard, Unique,
            ExcludeFromClones, UnhealedHealth, DamageBuff, ExternalInteractions, RestrictSizeToRoomCapacity,
            MagicPowerTraitScalingOnly, CloneDamageBase, CloneHealBase, EquipmentSourceCardId, EquipmentSourceUpgradeIndex, TriggerUpgrades, AbilityUpgrade, DoNotReplaceExistingAbility, MaskMetadata,
            CopyRemoveOnDiscard, CopyUnique, CopyExcludeFromClones, Lifecycle);
        internal CardUpgradeModifier WithRuntimeIdentity(string id) => new CardUpgradeModifier(id, AssetKey,
            Stats, Statuses, RemoveOnDiscard, Unique, ExcludeFromClones, UnhealedHealth, DamageBuff, ExternalInteractions,
            RestrictSizeToRoomCapacity, MagicPowerTraitScalingOnly, CloneDamageBase, CloneHealBase, EquipmentSourceCardId,
            EquipmentSourceUpgradeIndex, TriggerUpgrades, AbilityUpgrade, DoNotReplaceExistingAbility, MaskMetadata,
            null, null, null, Lifecycle == null ? null : Lifecycle.WithValues(new CardMaskUpgrade(id,
                Lifecycle.Values.Stats, Lifecycle.Values.Statuses, Lifecycle.Values.HasIcon, Lifecycle.Values.HideIcon,
                Lifecycle.Values.RegionRun, Lifecycle.Values.UnitAbility)), SharedReferenceId);
        internal CardUpgradeModifier WithLifecycle(CardLifecycleUpgrade? lifecycle) => new CardUpgradeModifier(DataId, AssetKey,
            Stats, Statuses, RemoveOnDiscard, Unique, ExcludeFromClones, UnhealedHealth, DamageBuff, ExternalInteractions,
            RestrictSizeToRoomCapacity, MagicPowerTraitScalingOnly, CloneDamageBase, CloneHealBase, EquipmentSourceCardId,
            EquipmentSourceUpgradeIndex, TriggerUpgrades, AbilityUpgrade, DoNotReplaceExistingAbility, MaskMetadata,
            CopyRemoveOnDiscard, CopyUnique, CopyExcludeFromClones, lifecycle, SharedReferenceId);
        internal CardUpgradeModifier Accumulate(CardUpgradeModifier addition)
        {
            if (DataId.Length == 0 || DataId != addition.DataId)
                throw new InvalidOperationException("Scaled upgrades must share a nonempty definition ID.");
            var stats = new CardStatModifier(
                checked(Stats.Damage + addition.Stats.Damage), checked(Stats.Health + addition.Stats.Health),
                checked(Stats.Cost + addition.Stats.Cost), checked(Stats.Heal + addition.Stats.Heal),
                checked(Stats.Size + addition.Stats.Size), checked(Stats.XCost + addition.Stats.XCost),
                checked(Stats.EquipmentLimit + addition.Stats.EquipmentLimit),
                checked(Stats.UpgradeSlotCount + addition.Stats.UpgradeSlotCount));
            var statuses = Statuses.ToList();
            foreach (CombatStatus added in addition.Statuses)
            {
                int index = statuses.FindIndex(status => status.Id == added.Id);
                if (index < 0) statuses.Add(added);
                else statuses[index] = statuses[index].WithStacks(checked(statuses[index].Stacks + added.Stacks));
            }
            CardLifecycleUpgrade? lifecycle = Lifecycle;
            if (lifecycle != null)
            {
                var maskStatuses = lifecycle.Values.Statuses.ToList();
                foreach (UpgradeMaskStatus added in addition.Lifecycle?.Values.Statuses ?? Array.Empty<UpgradeMaskStatus>())
                {
                    int index = maskStatuses.FindIndex(status => status.Id == added.Id &&
                        status.FromPermanentUpgrade == added.FromPermanentUpgrade);
                    if (index < 0) maskStatuses.Add(added);
                    else maskStatuses[index] = new UpgradeMaskStatus(added.Id,
                        checked(maskStatuses[index].Count + added.Count), added.FromPermanentUpgrade);
                }
                var values = new CardMaskUpgrade(lifecycle.Values.DataId, stats, maskStatuses,
                    lifecycle.Values.HasIcon, lifecycle.Values.HideIcon, lifecycle.Values.RegionRun, lifecycle.Values.UnitAbility);
                lifecycle = lifecycle.WithValues(values);
            }
            return new CardUpgradeModifier(DataId, AssetKey, stats, statuses, RemoveOnDiscard, Unique, ExcludeFromClones,
                checked(UnhealedHealth + addition.UnhealedHealth), DamageBuff, ExternalInteractions, RestrictSizeToRoomCapacity,
                MagicPowerTraitScalingOnly, CloneDamageBase, CloneHealBase, EquipmentSourceCardId, EquipmentSourceUpgradeIndex,
                TriggerUpgrades, AbilityUpgrade, DoNotReplaceExistingAbility, MaskMetadata, CopyRemoveOnDiscard, CopyUnique,
                CopyExcludeFromClones, lifecycle, SharedReferenceId);
        }
        internal CardUpgradeModifier WithSharedReference(int? referenceId) => new CardUpgradeModifier(DataId, AssetKey,
            Stats, Statuses, RemoveOnDiscard, Unique, ExcludeFromClones, UnhealedHealth, DamageBuff, ExternalInteractions,
            RestrictSizeToRoomCapacity, MagicPowerTraitScalingOnly, CloneDamageBase, CloneHealBase, EquipmentSourceCardId,
            EquipmentSourceUpgradeIndex, TriggerUpgrades, AbilityUpgrade, DoNotReplaceExistingAbility, MaskMetadata,
            CopyRemoveOnDiscard, CopyUnique, CopyExcludeFromClones, Lifecycle, referenceId);
        internal CardUpgradeModifier ForAddCardCopy() => new CardUpgradeModifier(DataId, AssetKey, Stats, Statuses,
            CopyRemoveOnDiscard ?? RemoveOnDiscard, CopyUnique ?? Unique, CopyExcludeFromClones ?? ExcludeFromClones,
            UnhealedHealth, DamageBuff, ExternalInteractions, RestrictSizeToRoomCapacity, MagicPowerTraitScalingOnly,
            CloneDamageBase, CloneHealBase, EquipmentSourceCardId, EquipmentSourceUpgradeIndex, TriggerUpgrades,
            AbilityUpgrade, DoNotReplaceExistingAbility, MaskMetadata, CopyRemoveOnDiscard, CopyUnique, CopyExcludeFromClones,
            Lifecycle?.WithCopyFlags(CopyUnique ?? Unique, CopyRemoveOnDiscard ?? RemoveOnDiscard));
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
        internal CardModifiers WithPermanentUpgrade(CardUpgradeModifier upgrade, bool scale)
        {
            var upgrades = Upgrades.ToList();
            int existing = scale && upgrade.DataId.Length > 0
                ? upgrades.FindIndex(item => item.DataId == upgrade.DataId) : -1;
            if (existing >= 0) upgrades[existing] = upgrades[existing].Accumulate(upgrade);
            else upgrades.Add(upgrade);
            return new CardModifiers(Offsets, upgrades, PersistentHealth, ExternalInteractions);
        }
    }

    public sealed class CardEffectCounter
    {
        public int EffectIndex { get; }
        public string Type { get; }
        public int Value { get; }
        public CardEffectCounter(int effectIndex, string type, int value)
        { EffectIndex = effectIndex; Type = type; Value = value; }
    }

    public sealed class ScalingForgeCountTrait
    {
        public CardStatisticQuery Query { get; }
        public int ForgedCountPerStat { get; }
        public ScalingForgeCountTrait(CardStatisticQuery query, int forgedCountPerStat)
        { Query = query; ForgedCountPerStat = forgedCountPerStat; }
    }

    public sealed class ScalingTrainHealTrait
    {
        public CardStatisticQuery Query { get; }
        public int HealPerStat { get; }
        public float Multiplier { get; }
        public ScalingTrainHealTrait(CardStatisticQuery query, int healPerStat, float multiplier)
        { Query = query; HealPerStat = healPerStat; Multiplier = multiplier; }
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
        public IReadOnlyList<CardEffectCounter>? EffectCounters { get; }
        public IReadOnlyList<ScalingDamageTrait>? DamageScalingTraits { get; }
        public IReadOnlyList<ScalingStatusTrait>? StatusScalingTraits { get; }
        public IReadOnlyList<ScalingUnitUpgradeTrait>? UnitUpgradeScalingTraits { get; }
        public IReadOnlyList<ScalingCapacityTrait>? CapacityScalingTraits { get; }
        public IReadOnlyList<ScalingForgeCountTrait>? ForgeCountScalingTraits { get; }
        public IReadOnlyList<ScalingTrainHealTrait>? TrainHealScalingTraits { get; }
        public int? EquippedUnitId { get; }
        public IReadOnlyList<int>? PlayedRoomUnitIds { get; }
        public IReadOnlyList<int>? RawPlayedRoomUnitIds { get; }
        public CardMaskDescriptor? MaskDescriptor { get; }
        public CardExhaustState? ExhaustState { get; }
        public int? RawCostOverride { get; }
        public bool? CanBeCopied { get; }
        public CardInstanceState(int instanceId, string dataId, CardModifiers permanent, CardModifiers temporary,
            int lastPlayedCost, int lastForgedAmount, int playCount, IReadOnlyList<string> externalInteractions,
            IReadOnlyList<CardEffectCounter>? effectCounters = null, IReadOnlyList<ScalingDamageTrait>? damageScalingTraits = null,
            IReadOnlyList<ScalingStatusTrait>? statusScalingTraits = null, IReadOnlyList<ScalingUnitUpgradeTrait>? unitUpgradeScalingTraits = null,
            IReadOnlyList<ScalingCapacityTrait>? capacityScalingTraits = null, int? equippedUnitId = null, IReadOnlyList<int>? playedRoomUnitIds = null,
            IReadOnlyList<int>? rawPlayedRoomUnitIds = null, CardMaskDescriptor? maskDescriptor = null,
            CardExhaustState? exhaustState = null, IReadOnlyList<ScalingForgeCountTrait>? forgeCountScalingTraits = null,
            int? rawCostOverride = null, bool? canBeCopied = null,
            IReadOnlyList<ScalingTrainHealTrait>? trainHealScalingTraits = null)
        { InstanceId = instanceId; DataId = dataId; Permanent = permanent; Temporary = temporary; MaskDescriptor = maskDescriptor; ExhaustState = exhaustState;
            RawCostOverride = rawCostOverride; CanBeCopied = canBeCopied;
            LastPlayedCost = lastPlayedCost; LastForgedAmount = lastForgedAmount; PlayCount = playCount;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            EffectCounters = effectCounters == null ? null : Array.AsReadOnly(effectCounters.OrderBy(counter => counter.EffectIndex).ToArray());
            DamageScalingTraits = damageScalingTraits == null ? null : Array.AsReadOnly(damageScalingTraits.ToArray());
            StatusScalingTraits = statusScalingTraits == null ? null : Array.AsReadOnly(statusScalingTraits.ToArray());
            UnitUpgradeScalingTraits = unitUpgradeScalingTraits == null ? null : Array.AsReadOnly(unitUpgradeScalingTraits.ToArray());
            CapacityScalingTraits = capacityScalingTraits == null ? null : Array.AsReadOnly(capacityScalingTraits.ToArray());
            ForgeCountScalingTraits = forgeCountScalingTraits == null ? null : Array.AsReadOnly(forgeCountScalingTraits.ToArray());
            TrainHealScalingTraits = trainHealScalingTraits == null ? null : Array.AsReadOnly(trainHealScalingTraits.ToArray());
            EquippedUnitId = equippedUnitId;
            PlayedRoomUnitIds = playedRoomUnitIds == null ? null : Array.AsReadOnly(playedRoomUnitIds.OrderBy(id => id).ToArray());
            RawPlayedRoomUnitIds = rawPlayedRoomUnitIds == null ? null : Array.AsReadOnly(rawPlayedRoomUnitIds.OrderBy(id => id).ToArray()); }
        internal CardInstanceState WithEquippedUnit(int unitId) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary, LastPlayedCost, LastForgedAmount, PlayCount, ExternalInteractions,
            EffectCounters, DamageScalingTraits, StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, unitId, PlayedRoomUnitIds, RawPlayedRoomUnitIds, MaskDescriptor, ExhaustState, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
        internal CardInstanceState WithModifiers(CardModifiers permanent, CardModifiers temporary, bool refreshTraits = true)
        {
            CardMaskDescriptor? descriptor = MaskDescriptor;
            if (refreshTraits && !Permanent.Upgrades.SequenceEqual(permanent.Upgrades))
            {
                CardUpgradeModifier[] addedUpgrades = permanent.Upgrades.Where(upgrade => !Permanent.Upgrades.Contains(upgrade)).ToArray();
                descriptor = CardExhaustModel.InstallPermanentTraits(this, addedUpgrades) ?? descriptor;
            }
            var changed = new CardInstanceState(InstanceId, DataId,
            permanent, temporary, LastPlayedCost, LastForgedAmount, PlayCount, ExternalInteractions,
            EffectCounters, DamageScalingTraits, StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, PlayedRoomUnitIds, RawPlayedRoomUnitIds, descriptor, ExhaustState, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
            return refreshTraits && (!Permanent.Upgrades.SequenceEqual(permanent.Upgrades) || !Temporary.Upgrades.SequenceEqual(temporary.Upgrades))
                ? CardExhaustModel.RefreshUpgradeTrait(changed) : changed;
        }
        internal CardInstanceState WithLastForgedAmount(int amount) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary, LastPlayedCost, amount, PlayCount, ExternalInteractions, EffectCounters,
            DamageScalingTraits, StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId,
            PlayedRoomUnitIds, RawPlayedRoomUnitIds, MaskDescriptor, ExhaustState, ForgeCountScalingTraits,
            RawCostOverride, CanBeCopied, TrainHealScalingTraits);
        internal CardInstanceState WithPermanentUpgrade(CardUpgradeModifier upgrade, bool scale)
            => WithModifiers(Permanent.WithPermanentUpgrade(upgrade, scale), Temporary);
        internal CardInstanceState WithPlayedRoomUnits(IReadOnlyList<int> units) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary, LastPlayedCost, LastForgedAmount, PlayCount, ExternalInteractions,
            EffectCounters, DamageScalingTraits, StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, units,
            RawPlayedRoomUnitIds == null ? null : units, MaskDescriptor, ExhaustState, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
        internal CardInstanceState WithRoomCacheState(IReadOnlyList<int>? visible, IReadOnlyList<int>? raw) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary, LastPlayedCost, LastForgedAmount, PlayCount, ExternalInteractions,
            EffectCounters, DamageScalingTraits, StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, visible, raw, MaskDescriptor, ExhaustState, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
        public static CardInstanceState Empty(int id, string dataId) => new CardInstanceState(id, dataId,
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, Array.Empty<string>());
        internal CardInstanceState WithPlayedCost(int cost) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary, cost, LastForgedAmount, PlayCount, ExternalInteractions,
            EffectCounters, DamageScalingTraits, StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, PlayedRoomUnitIds, RawPlayedRoomUnitIds, MaskDescriptor, ExhaustState, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
        public CardInstanceState OnDiscard(bool played, int? cost = null, bool suppressConsume = false)
        {
            CardInstanceState callback = played && !suppressConsume ? CardExhaustModel.MarkPlayedDiscard(this) : this;
            CardModifiers temporary = Temporary.OnDiscard();
            var result = new CardInstanceState(InstanceId, DataId, Permanent, temporary, played ? cost ?? LastPlayedCost : LastPlayedCost, LastForgedAmount,
                unchecked(PlayCount + (played ? 1 : 0)), ExternalInteractions, EffectCounters, DamageScalingTraits, StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, PlayedRoomUnitIds, RawPlayedRoomUnitIds, MaskDescriptor, callback.ExhaustState, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
            return Temporary.Upgrades.SequenceEqual(temporary.Upgrades) ? result : CardExhaustModel.RefreshUpgradeTrait(result);
        }
        internal CardInstanceState WithExhaustState(CardExhaustState? state) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary, LastPlayedCost, LastForgedAmount, PlayCount, ExternalInteractions, EffectCounters, DamageScalingTraits,
            StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, PlayedRoomUnitIds, RawPlayedRoomUnitIds, MaskDescriptor, state, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
        internal CardInstanceState WithMaskDescriptor(CardMaskDescriptor? descriptor) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary, LastPlayedCost, LastForgedAmount, PlayCount, ExternalInteractions, EffectCounters, DamageScalingTraits,
            StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, PlayedRoomUnitIds, RawPlayedRoomUnitIds,
            descriptor, ExhaustState, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
        internal CardInstanceState WithCounter(int index, string type, int value) => new CardInstanceState(InstanceId, DataId,
            Permanent, Temporary, LastPlayedCost, LastForgedAmount, PlayCount, ExternalInteractions,
            (EffectCounters ?? Array.Empty<CardEffectCounter>()).Where(counter => counter.EffectIndex != index)
                .Concat(new[] { new CardEffectCounter(index, type, value) }).ToArray(), DamageScalingTraits, StatusScalingTraits, UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, PlayedRoomUnitIds, RawPlayedRoomUnitIds, MaskDescriptor, ExhaustState, ForgeCountScalingTraits, RawCostOverride, CanBeCopied, TrainHealScalingTraits);
    }

    public static class CardModifierModel
    {
        public static IEnumerable<string> UpgradeInteractions(CardUpgradeModifier upgrade) =>
            (upgrade.Lifecycle?.ReplacedAssets.Count > 0
                ? upgrade.ExternalInteractions.Where(interaction => interaction != "Upgrade replacements")
                : upgrade.ExternalInteractions)
            .Concat((upgrade.Lifecycle?.Triggers ?? Array.Empty<CardLifecycleTrigger>()).SelectMany(trigger =>
                trigger.Type != "OnCast" && !(RelicPerkeoModel.IsConversionUpgrade(upgrade) && RelicPerkeoModel.IsConversionTrigger(trigger))
                    ? new[] { "Unsupported card upgrade trigger " + trigger.Type } :
                trigger.Effects.Count != trigger.Actions.Count ? new[] { "Card upgrade trigger effects are not fully captured" } :
                trigger.ExternalInteractions));

        internal static string? UnsupportedReason(CardInstanceState instance)
        {
            string? exhaustError = CardExhaustModel.Validate(instance);
            if (exhaustError != null) return exhaustError;
            if (instance.MaskDescriptor != null && (instance.MaskDescriptor.Definition.DataId != instance.DataId ||
                instance.Permanent.Upgrades.Concat(instance.Temporary.Upgrades).Any(upgrade => upgrade.MaskMetadata == null)))
                return "Card mask descriptor identity or upgrade metadata is missing.";
            if (instance.EffectCounters != null && (instance.EffectCounters.Any(counter => counter.EffectIndex < 0 ||
                counter.Value < 0 || counter.Type != "CardEffectDiscardHand") ||
                instance.EffectCounters.Select(counter => counter.EffectIndex).Distinct().Count() != instance.EffectCounters.Count))
                return "Invalid or unmodeled card effect counters.";
            var interactions = instance.ExternalInteractions.Concat(instance.Permanent.ExternalInteractions)
                .Concat(instance.Temporary.ExternalInteractions).Concat(instance.Permanent.Upgrades.Concat(instance.Temporary.Upgrades)
                    .SelectMany(UpgradeInteractions)).ToList();
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
                // Native Mathf.Abs(int) throws for MinValue; ordinary additions
                // wrap before either the immediate large-change clamp or final clamp.
                if (addition != 0) value = Math.Abs(addition) >= 99 ? Clamp(unchecked(value + addition)) : unchecked(value + addition);
            return Clamp(value);
            int Clamp(int number) => Math.Max(floor, Math.Min(ceiling, number));
        }

        public static CardPlayRule Resolve(CardPlayRule rule, CardInstanceState instance)
        {
            var interactions = rule.ExternalInteractions.Concat(instance.ExternalInteractions)
                .Concat(instance.Permanent.ExternalInteractions).Concat(instance.Temporary.ExternalInteractions)
                .Concat(instance.Permanent.Upgrades.Concat(instance.Temporary.Upgrades).SelectMany(UpgradeInteractions)).ToList();
            string? exhaustError = CardExhaustModel.Validate(instance);
            if (exhaustError != null) interactions.Add(exhaustError);
            if (CardExhaustModel.HasConsume(instance) && instance.ExhaustState == null) interactions.Add("Consume requires captured private trait state.");
            if (rule.DataId != instance.DataId) interactions.Add("Card instance definition differs.");
            if (instance.Permanent.PersistentHealth != 0 || instance.Temporary.PersistentHealth != 0) interactions.Add("Persistent card health");
            CardModifiers[] modifiers = { instance.Permanent, instance.Temporary };
            CardUpgradeModifier[] cardUpgrades = modifiers.SelectMany(modifier => modifier.Upgrades).ToArray();
            CombatUnit? unit = rule.SpawnUnit;
            if (unit != null)
            {
                var statuses = unit.Statuses.ToDictionary(status => status.Id);
                foreach (CombatStatus added in cardUpgrades.SelectMany(upgrade => upgrade.Statuses))
                {
                    if (unit.StatusImmunities.Contains(added.Id)) continue;
                    statuses.TryGetValue(added.Id, out CombatStatus? existing);
                    int count = Math.Min(9999, checked((existing?.Stacks ?? 0) + added.Stacks));
                    statuses[added.Id] = (existing ?? unit.RegisteredStatus(added.Id) ?? added).WithStacks(Math.Max(0, count));
                }
                int health = UpgradedStat(unit.MaxHealth, "Health", true, modifiers);
                int maxHealth = health;
                foreach (CardUpgradeModifier upgrade in cardUpgrades)
                {
                    // Native card births apply positive unhealed-health bonuses; negative values are ignored.
                    if (upgrade.UnhealedHealth > 0)
                        maxHealth = Math.Min(99999, checked(maxHealth + upgrade.UnhealedHealth));
                }
                UnitModifiers? original = unit.Modifiers;
                int attackDamage = UpgradedStat(original?.AttackDamage ?? unit.BaseAttack, "Damage", true, modifiers);
                int damageBuff = original?.DamageBuff ?? 0;
                int baseAttack = UpgradedStat(unit.BaseAttack, "Damage", true, modifiers);
                foreach (CardUpgradeModifier upgrade in cardUpgrades)
                {
                    damageBuff = checked(damageBuff + upgrade.DamageBuff);
                    baseAttack = checked(baseAttack + upgrade.DamageBuff);
                }
                baseAttack = Math.Max(0, baseAttack);
                UnitModifiers? unitModifiers = original == null ? null : new UnitModifiers(
                    attackDamage, original.AttackDamageAdded, damageBuff,
                    UpgradedStat(original.RawSize, "Size", false, modifiers),
                    Math.Max(1, Math.Min(4, UpgradedStat(original.EquipmentLimit, "EquipmentLimit", true, modifiers))),
                    original.CanBeHealed, original.IsClone, original.Upgrades, original.HealthFromUpgrades, original.SpawnerMatchesDefinition,
                    original.DamageBuffFromStatusEffects, original.MagebladeDamagePerMagicPower, original.StatusAttackModifiers,
                    original.IsChampion);
                int? nextTriggerId = unit.NextTriggerId;
                var triggers = unit.Triggers.ToList();
                foreach (CombatTrigger definition in cardUpgrades.SelectMany(upgrade =>
                    upgrade.TriggerUpgrades ?? Array.Empty<CombatTrigger>()))
                {
                    triggers.Add(definition.WithOrigin("", 0, nextTriggerId));
                    if (nextTriggerId.HasValue) nextTriggerId = checked(nextTriggerId.Value + 1);
                }
                unit = new CombatUnit(unit.Id, unit.AssetKey, unit.Team, baseAttack,
                    health, maxHealth, unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath,
                    statuses.Values.Where(status => status.Stacks > 0).ToArray(), triggers,
                    unit.SpawnerCardId, Math.Max(1, UpgradedStat(unit.Size, "Size", false, modifiers)), unit.StatusImmunities, unit.Subtypes, unitModifiers, unit.IsBoss, unit.LastAttackerId, unit.StatusRegistry, unit.EquipmentCards, nextTriggerId, unit.Ability, unit.StatusDictionary, unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState, bumpRules: unit.BumpRules, lastFeederId: unit.LastFeederId);
            }
            CardActionEffect[] triggerEffects = cardUpgrades.SelectMany(upgrade => upgrade.Lifecycle?.Triggers ?? Array.Empty<CardLifecycleTrigger>())
                .Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Actions).ToArray();
            CardLifecycleTrigger[] unplayedTriggers = cardUpgrades.SelectMany(upgrade => upgrade.Lifecycle?.Triggers ?? Array.Empty<CardLifecycleTrigger>())
                .Where(trigger => trigger.Type.StartsWith("OnUnplayed", StringComparison.Ordinal)).ToArray();
            CardActionEffect[] onUnplayedNegativeEffects = rule.OnUnplayedNegativeEffects
                .Concat(unplayedTriggers.Where(trigger => trigger.Type == "OnUnplayedNegative").SelectMany(trigger => trigger.Actions))
                .Select(ResolveEffect).ToArray();
            bool unsupportedUnplayedTriggers = rule.HasUnsupportedUnplayedTriggers || unplayedTriggers.Any(trigger => !RelicPerkeoModel.IsConversionTrigger(trigger));
            CardActionEffect[] effects = rule.Effects.Select(ResolveEffect).ToArray();
            CardActionEffect[] onCastEffects = rule.OnCastEffects.Concat(triggerEffects).Select(ResolveEffect).ToArray();
            string destination = rule.Destination;
            IReadOnlyList<string> effectiveTraits = instance.MaskDescriptor == null
                ? Array.Empty<string>() : CardBranchMaskModel.Resolve(instance).Traits;
            if (destination == "Discard" && effectiveTraits.Any(trait => trait is "CardTraitSelfPurge" or "CardTraitEphemeral"))
                destination = "Purged";
            else if (destination == "Purged" &&
                instance.MaskDescriptor?.PermanentReplacements.Any(replacement =>
                    replacement.OldName == "CardTraitSelfPurge" && replacement.NewName == "CardTraitExhaustState") == true &&
                effectiveTraits.Contains("CardTraitExhaustState", StringComparer.Ordinal) &&
                !effectiveTraits.Any(trait => trait is "CardTraitSelfPurge" or "CardTraitEphemeral"))
                destination = "Discard";
            return new CardPlayRule(rule.DataId, rule.AssetKey, UpgradedStat(instance.RawCostOverride ?? rule.Cost, "Cost", true, modifiers), rule.Effect,
                destination, unit, interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), effects, rule.UpgradeInteractions,
                rule.HandDiscardInteractions, rule.HandConsumeInteractions, rule.CostType, rule.Equipment, rule.Ability, rule.Summon, rule.CardType,
                rule.IsAnyAbility, rule.CanPlayInEngineRoom, onCastEffects, onUnplayedNegativeEffects,
                rule.UnplayedNegativeOrder, unsupportedUnplayedTriggers, rule.ForgeEligible);

            CardActionEffect ResolveEffect(CardActionEffect effect) => new CardActionEffect(effect.Type, effect.Target,
                ResolveValue(effect, effect.Value), effect.AllowEnemy, effect.AllowPlayer, effect.Statuses, effect.Upgrade, effect.Lifetime, effect.Tests,
                effect.Range == null ? null : new CardEffectRange(ResolveValue(effect, effect.Range.Min),
                    ResolveValue(effect, effect.Range.Max), effect.Range.Multiplier), effect.Filters, effect.Generation, effect.OnlyIfNoEnemies,
                effect.CooldownParameter, effect.AbilityChange, effect.CopyHeroStats, effect.ScaleUpgrade,
                effect.DoNotFullyRemove, effect.TargetSubtype, effect.TypedDraw, effect.NextDrawUpgrade, effect.RequiredGold, effect.GoldCostRequired,
                effect.UseCardTraits, effect.RequireEquipment, effect.UseSelectedRoom, effect.CountStatusOnlyOnTargets,
                effect.SpawnFallbackUnit, effect.DisallowStatusStackModifiers,
                damagePerHpLastAttacker: effect.DamagePerHpLastAttacker,
                damagePerHpIncludeArmor: effect.DamagePerHpIncludeArmor,
                roomAttachment: effect.RoomAttachment, recursion: effect.Recursion,
                cardFilter: effect.CardFilter, upgradeFilters: effect.UpgradeFilters,
                removeHalfStatusEffects: effect.RemoveHalfStatusEffects, overheal: effect.Overheal,
                corruptionAdd: effect.CorruptionAdd, nullTargetless: effect.NullTargetless,
                nullForwardSelf: effect.NullForwardSelf, chooseDiscard: effect.ChooseDiscard,
                transferAttackMultiplier: effect.TransferAttackMultiplier, statusTransfer: effect.StatusTransfer,
                unitTriggerKind: effect.UnitTriggerKind, healDamageMultiplier: effect.HealDamageMultiplier);

            int ResolveValue(CardActionEffect effect, int value) => effect.Type == "Damage" || effect.Type == "Heal" || effect.Type == "HealOverheal" || effect.Type == "HealRelative"
                ? UpgradedStat(UpgradedStat(value, effect.Type == "HealOverheal" || effect.Type == "HealRelative" ? "Heal" : effect.Type, true, instance.Permanent),
                    effect.Type == "HealOverheal" || effect.Type == "HealRelative" ? "Heal" : effect.Type, true, instance.Temporary) : value;
        }
    }
}
