using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Preserve the native manager's order and runtime effect types. Duplicate relic
    // definitions can be separate instances; their presence is not a stack count.
    public sealed class CombatRelicState
    {
        public string DataId { get; }
        public string AssetKey { get; }
        public IReadOnlyList<string> EffectTypes { get; }
        public IReadOnlyList<RelicSpawnStatus>? SpawnStatuses { get; }
        public IReadOnlyList<RelicRoomEntryStatus>? RoomEntryStatuses { get; }
        public bool? IsCovenant { get; }
        public bool? DisallowedInPlacementPhase { get; }
        public IReadOnlyList<RelicCardModifier>? CardModifiers { get; }
        public IReadOnlyList<RelicCharacterTriggers>? TriggerAdditions { get; }
        public IReadOnlyList<RelicStatusStacks>? StatusStackAdditions { get; }
        public IReadOnlyList<RelicStatusMagnitude>? StatusMagnitudes { get; }
        public IReadOnlyList<RelicStatusDecay>? StatusDecays { get; }
        public IReadOnlyList<RelicResourceEffect>? ResourceEffects { get; }
        public IReadOnlyList<RelicMagicPowerModifier>? MagicPowerModifiers { get; }
        public IReadOnlyList<RelicCardPlayMagicPowerModifier>? CardPlayMagicPowerModifiers { get; }
        public IReadOnlyList<RelicUnitUpgradeTrigger>? UnitUpgradeTriggers { get; }
        public IReadOnlyList<RelicForgePointModifier>? ForgePointModifiers { get; }
        public IReadOnlyList<RelicCardHealthModifier>? CardHealthModifiers { get; }
        public IReadOnlyList<RelicCardAttackModifier>? CardAttackModifiers { get; }
        public IReadOnlyList<RelicCardDrawOnCardPlayModifier>? CardDrawOnCardPlayModifiers { get; }
        public IReadOnlyList<RelicCorruptionStatus>? CorruptionStatuses { get; }
        public IReadOnlyList<RelicCardTypeCost>? CardTypeCosts { get; }
        public IReadOnlyList<RelicStatusAttackModifier>? StatusAttackModifiers { get; }
        public IReadOnlyList<RelicCardPlayRoomCapacityModifier>? CardPlayRoomCapacityModifiers { get; }
        public IReadOnlyList<RelicMoonPhaseHealRule>? MoonPhaseHealRules { get; }
        public IReadOnlyList<RelicRoomAttachmentReplication>? RoomAttachmentReplications { get; }
        public IReadOnlyList<RelicSummonDamageRule>? SummonDamageRules { get; }
        public IReadOnlyList<RelicFloorTransitionDamageRule>? FloorTransitionDamageRules { get; }
        public IReadOnlyList<RelicCheaperSpellUpgradeModifier>? CheaperSpellUpgradeModifiers { get; }
        public IReadOnlyList<RelicUnstableRebalanceModifier>? UnstableRebalances { get; }
        public IReadOnlyList<RelicCopyCardOnCardPlayModifier>? CopyCardOnCardPlayModifiers { get; }
        public IReadOnlyList<RelicMonsterSpellTargetingModifier>? MonsterSpellTargetingModifiers { get; }
        public IReadOnlyList<RelicSoloUnitUpgrade>? SoloUnitUpgrades { get; }
        public CombatRelicState(string dataId, string assetKey, IReadOnlyList<string> effectTypes,
            IReadOnlyList<RelicSpawnStatus>? spawnStatuses = null, bool? isCovenant = null, bool? disallowedInPlacementPhase = null,
            IReadOnlyList<RelicCardModifier>? cardModifiers = null, IReadOnlyList<RelicCharacterTriggers>? triggerAdditions = null,
            IReadOnlyList<RelicStatusStacks>? statusStackAdditions = null, IReadOnlyList<RelicStatusMagnitude>? statusMagnitudes = null,
            IReadOnlyList<RelicStatusDecay>? statusDecays = null, IReadOnlyList<RelicResourceEffect>? resourceEffects = null,
            IReadOnlyList<RelicMagicPowerModifier>? magicPowerModifiers = null,
            IReadOnlyList<RelicCardPlayMagicPowerModifier>? cardPlayMagicPowerModifiers = null,
            IReadOnlyList<RelicRoomEntryStatus>? roomEntryStatuses = null,
            IReadOnlyList<RelicUnitUpgradeTrigger>? unitUpgradeTriggers = null,
            IReadOnlyList<RelicForgePointModifier>? forgePointModifiers = null,
            IReadOnlyList<RelicCardHealthModifier>? cardHealthModifiers = null,
            IReadOnlyList<RelicCardAttackModifier>? cardAttackModifiers = null,
            IReadOnlyList<RelicCardDrawOnCardPlayModifier>? cardDrawOnCardPlayModifiers = null,
            IReadOnlyList<RelicCorruptionStatus>? corruptionStatuses = null,
            IReadOnlyList<RelicCardTypeCost>? cardTypeCosts = null,
            IReadOnlyList<RelicStatusAttackModifier>? statusAttackModifiers = null,
            IReadOnlyList<RelicCardPlayRoomCapacityModifier>? cardPlayRoomCapacityModifiers = null,
            IReadOnlyList<RelicMoonPhaseHealRule>? moonPhaseHealRules = null,
            IReadOnlyList<RelicRoomAttachmentReplication>? roomAttachmentReplications = null,
            IReadOnlyList<RelicSummonDamageRule>? summonDamageRules = null,
            IReadOnlyList<RelicCheaperSpellUpgradeModifier>? cheaperSpellUpgradeModifiers = null,
            IReadOnlyList<RelicUnstableRebalanceModifier>? unstableRebalances = null,
            IReadOnlyList<RelicCopyCardOnCardPlayModifier>? copyCardOnCardPlayModifiers = null,
            IReadOnlyList<RelicFloorTransitionDamageRule>? floorTransitionDamageRules = null,
            IReadOnlyList<RelicSoloUnitUpgrade>? soloUnitUpgrades = null,
            IReadOnlyList<RelicMonsterSpellTargetingModifier>? monsterSpellTargetingModifiers = null)
        { DataId = dataId; AssetKey = assetKey; EffectTypes = Array.AsReadOnly(effectTypes.ToArray());
            SpawnStatuses = spawnStatuses == null ? null : Array.AsReadOnly(spawnStatuses.ToArray());
            RoomEntryStatuses = roomEntryStatuses == null ? null : Array.AsReadOnly(roomEntryStatuses.ToArray());
            IsCovenant = isCovenant; DisallowedInPlacementPhase = disallowedInPlacementPhase;
            CardModifiers = cardModifiers == null ? null : Array.AsReadOnly(cardModifiers.ToArray());
            TriggerAdditions = triggerAdditions == null ? null : Array.AsReadOnly(triggerAdditions.ToArray());
            StatusStackAdditions = statusStackAdditions == null ? null : Array.AsReadOnly(statusStackAdditions.ToArray());
            StatusMagnitudes = statusMagnitudes == null ? null : Array.AsReadOnly(statusMagnitudes.ToArray());
            StatusDecays = statusDecays == null ? null : Array.AsReadOnly(statusDecays.ToArray());
            ResourceEffects = resourceEffects == null ? null : Array.AsReadOnly(resourceEffects.ToArray());
            MagicPowerModifiers = magicPowerModifiers == null ? null : Array.AsReadOnly(magicPowerModifiers.ToArray());
            CardPlayMagicPowerModifiers = cardPlayMagicPowerModifiers == null ? null : Array.AsReadOnly(cardPlayMagicPowerModifiers.ToArray());
            UnitUpgradeTriggers = unitUpgradeTriggers == null ? null : Array.AsReadOnly(unitUpgradeTriggers.ToArray());
            ForgePointModifiers = forgePointModifiers == null ? null : Array.AsReadOnly(forgePointModifiers.ToArray());
            CardHealthModifiers = cardHealthModifiers == null ? null : Array.AsReadOnly(cardHealthModifiers.ToArray());
            CardAttackModifiers = cardAttackModifiers == null ? null : Array.AsReadOnly(cardAttackModifiers.ToArray());
            CardDrawOnCardPlayModifiers = cardDrawOnCardPlayModifiers == null ? null : Array.AsReadOnly(cardDrawOnCardPlayModifiers.ToArray());
            CorruptionStatuses = corruptionStatuses == null ? null : Array.AsReadOnly(corruptionStatuses.ToArray());
            CardTypeCosts = cardTypeCosts == null ? null : Array.AsReadOnly(cardTypeCosts.ToArray());
            StatusAttackModifiers = statusAttackModifiers == null ? null : Array.AsReadOnly(statusAttackModifiers.ToArray());
            CardPlayRoomCapacityModifiers = cardPlayRoomCapacityModifiers == null ? null : Array.AsReadOnly(cardPlayRoomCapacityModifiers.ToArray());
            MoonPhaseHealRules = moonPhaseHealRules == null ? null : Array.AsReadOnly(moonPhaseHealRules.ToArray());
            RoomAttachmentReplications = roomAttachmentReplications == null ? null : Array.AsReadOnly(roomAttachmentReplications.ToArray());
            SummonDamageRules = summonDamageRules == null ? null : Array.AsReadOnly(summonDamageRules.ToArray());
            CheaperSpellUpgradeModifiers = cheaperSpellUpgradeModifiers == null ? null : Array.AsReadOnly(cheaperSpellUpgradeModifiers.ToArray());
            UnstableRebalances = unstableRebalances == null ? null : Array.AsReadOnly(unstableRebalances.ToArray());
            CopyCardOnCardPlayModifiers = copyCardOnCardPlayModifiers == null ? null : Array.AsReadOnly(copyCardOnCardPlayModifiers.ToArray());
            MonsterSpellTargetingModifiers = monsterSpellTargetingModifiers == null ? null : Array.AsReadOnly(monsterSpellTargetingModifiers.ToArray());
            FloorTransitionDamageRules = floorTransitionDamageRules == null ? null : Array.AsReadOnly(floorTransitionDamageRules.ToArray());
            SoloUnitUpgrades = soloUnitUpgrades == null ? null : Array.AsReadOnly(soloUnitUpgrades.ToArray()); }
        internal CombatRelicState WithSoloUnitUpgrades(IReadOnlyList<RelicSoloUnitUpgrade> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase,
                CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects,
                MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers,
                CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts,
                StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications,
                SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers,
                floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: rules, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithSpawnStatuses(IReadOnlyList<RelicSpawnStatus> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, rules, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithRoomEntryStatuses(IReadOnlyList<RelicRoomEntryStatus> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, rules, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithCardModifiers(IReadOnlyList<RelicCardModifier> modifiers) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, modifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithTriggerAdditions(IReadOnlyList<RelicCharacterTriggers> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, rules, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithStatusStackAdditions(IReadOnlyList<RelicStatusStacks> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, rules, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithStatusMagnitudes(IReadOnlyList<RelicStatusMagnitude> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, rules, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithResourceEffects(IReadOnlyList<RelicResourceEffect> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, rules, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithMagicPowerModifiers(IReadOnlyList<RelicMagicPowerModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, rules, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, null, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithCardPlayMagicPowerModifiers(IReadOnlyList<RelicCardPlayMagicPowerModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, rules, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithUnitUpgradeTriggers(IReadOnlyList<RelicUnitUpgradeTrigger> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, rules, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithForgePointModifiers(IReadOnlyList<RelicForgePointModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, rules, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithCardHealthModifiers(IReadOnlyList<RelicCardHealthModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, rules, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithCardAttackModifiers(IReadOnlyList<RelicCardAttackModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, rules, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithCardDrawOnCardPlayModifiers(IReadOnlyList<RelicCardDrawOnCardPlayModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, rules, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithStatusAttackModifiers(IReadOnlyList<RelicStatusAttackModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, rules, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithCardPlayRoomCapacityModifiers(IReadOnlyList<RelicCardPlayRoomCapacityModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, rules, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithMoonPhaseHealRules(IReadOnlyList<RelicMoonPhaseHealRule> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, rules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithRoomAttachmentReplications(IReadOnlyList<RelicRoomAttachmentReplication> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, rules, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithSummonDamageRules(IReadOnlyList<RelicSummonDamageRule> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, rules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithCheaperSpellUpgradeModifiers(IReadOnlyList<RelicCheaperSpellUpgradeModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, rules, UnstableRebalances, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithUnstableRebalances(IReadOnlyList<RelicUnstableRebalanceModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, rules, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithCopyCardOnCardPlayModifiers(IReadOnlyList<RelicCopyCardOnCardPlayModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, rules, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: MonsterSpellTargetingModifiers);
        internal CombatRelicState WithMonsterSpellTargetingModifiers(IReadOnlyList<RelicMonsterSpellTargetingModifier> rules) =>
            new CombatRelicState(DataId, AssetKey, EffectTypes, SpawnStatuses, IsCovenant, DisallowedInPlacementPhase, CardModifiers, TriggerAdditions, StatusStackAdditions, StatusMagnitudes, StatusDecays, ResourceEffects, MagicPowerModifiers, CardPlayMagicPowerModifiers, RoomEntryStatuses, UnitUpgradeTriggers, ForgePointModifiers, CardHealthModifiers, CardAttackModifiers, CardDrawOnCardPlayModifiers, CorruptionStatuses, CardTypeCosts, StatusAttackModifiers, CardPlayRoomCapacityModifiers, MoonPhaseHealRules, RoomAttachmentReplications, SummonDamageRules, CheaperSpellUpgradeModifiers, UnstableRebalances, CopyCardOnCardPlayModifiers, floorTransitionDamageRules: FloorTransitionDamageRules, soloUnitUpgrades: SoloUnitUpgrades, monsterSpellTargetingModifiers: rules);
    }

    internal static class RelicModel
    {
        internal const string AbilityIncant = "RelicEffectIncantTriggeredByUnitAbilities";
        internal const string ModifyTriggerCount = "RelicEffectModifyTriggerCount";
        internal const string AddStatusOnSpawn = "RelicEffectAddStatusEffectOnSpawn";
        internal const string RemoveStatusOnSpawn = "RelicEffectRemoveStatusEffectOnSpawn";
        internal const string AddStatusOnTeamSpawn = "RelicEffectAddStatusEffectToTeamOnSpawn";
        internal const string AddStatusOnRoomEntry = "RelicEffectAddStatusEffectOnEnterRoomIndex";
        internal const string AddStatusOnUnitMovedUpDown = "RelicEffectAddStatusOnUnitMovedUpDown";
        internal const string AddStatusOnCorruption = "RelicEffectAddStatusEffectOnCorruptionAdded";
        internal const string AddTempUpgrade = "RelicEffectAddTempUpgrade";
        internal const string ModifyTrait = "RelicEffectModifyTrait";
        internal const string PurgeToExhaustId = "10648aee-8577-49b6-8659-318a7f922d5a";
        internal const string PurgeToExhaustKey = "PurgeToExhaust";
        internal const string ModifyCardTypeCost = "RelicEffectModifyCardTypeCost";
        internal const string ModifyCardHealth = "RelicEffectModifyCardHealth";
        internal const string ModifyCardAttack = "RelicEffectModifyCardAttack";
        internal const string ModifyAttackByStatus = "RelicEffectAdjustAttackByStatusEffect";
        internal const string AddRemoveTempUpgradeOnUnitTrigger = "RelicEffectAddRemoveTempUpgradeOnUnitTrigger";
        internal const string AddTempSoloUnitUpgradeStartOfTurn = "RelicEffectAddTempSoloUnitUpgradeStartOfTurn";
        // This type only provides generated-card tooltip data and has no battle hook.
        internal const string AddCardsNull = "RelicEffectAddCardsNull";
        internal const string Null = "RelicEffectNull";
        internal const string ModifyCardMagicalPower = "RelicEffectModifyCardMagicalPower";
        internal const string ModifyCardHealingPower = "RelicEffectModifyCardHealingPower";
        internal const string ModifyCardMagicPowerOnCardPlayed = "RelicEffectModifyCardMagicPowerOnCardPlayed";
        internal const string AddTempUpgradeToHandCardsIfCheaper = "RelicEffectAddTempUpgradeToCardsInHandIfCheaper";
        internal const string CardDrawOnCardPlay = "RelicEffectCardDrawOnCardPlay";
        internal const string CopyCardOnCardPlay = "RelicEffectCopyCardOnCardPlay";
        internal const string MonsterSpellTargeting = RelicMonsterSpellTargetingModel.Effect;
        internal const string ModifyRoomCapacityOnCardPlay = "RelicEffectModifyRoomCapacityOnCardPlay";
        internal const string FirstRoomCardOnAllFloors = "RelicEffectFirstRoomCardIsPlayedOnAllFloors";
        internal const string FirstRoomCardRelicId = "e55235f7-1d84-43f0-b8f5-037b64b8f582";
        internal const string FirstRoomCardRelicKey = "FirstRoomCardOnAllFloors";
        internal const string AddTrigger = "RelicEffectAddTrigger";
        internal const string AddStatusOnApplied = "RelicEffectAddStatusEffectOnStatusApplied";
        internal const string AddStatusOnOtherStatusRemoved = "RelicEffectAddStatusEffectOnOtherStatusRemoved";
        internal const string ModifyStatusMagnitude = "RelicEffectModifyStatusMagnitude";
        internal const string PreventStatusDecay = "RelicEffectPreventStatusEffectDecay";
        // These effects are resolved by SpawnGroupData.GetCharacters before the
        // native enemy definitions are captured for a spawn decision.
        internal const string ModifyCharacterAttackDamage = "RelicEffectModifyCharacterAttackDamage";
        internal const string ModifyCharacterMaxHealth = "RelicEffectModifyCharacterMaxHealth";
        internal const string AllDamageIsPiercing = "RelicEffectAllDamageIsPiercing";
        internal const string AdditionalForgePoints = "RelicEffectAdditionalForgePoints";
        internal const string ModifyForgeUpgrade = "RelicEffectModifyForgeUpgrade";
        internal const string HealOnMoonPhase = "RelicEffectHealOnMoonPhase";
        internal const string DamageOnMonsterSummoned = "RelicEffectDamageOnMonsterSummoned";
        internal const string DamageEnemyOnAscend = "RelicEffectDamageEnemyOnAscend";
        internal const string DamageEnemyOnDescend = "RelicEffectDamageEnemyOnDescend";
        internal static bool AbilitiesTriggerIncant(CombatContext? context) =>
            context?.Relics?.Any(relic => relic.EffectTypes.Contains(AbilityIncant)) == true;
        internal static string? Validate(CombatContext? context) => ConduitModel.Validate(context) ?? Validate(context?.Relics) ??
            RelicStartCombatAttachmentModel.Validate(context) ??
            (context?.Relics?.Any(relic => relic.EffectTypes.Contains(ModifyTriggerCount) || relic.EffectTypes.Contains(AddTrigger)) == true && context.TriggerCounts == null
                ? "Missing native relic trigger count cache." : TriggerCountModel.Validate(context?.TriggerCounts)) ?? RelicResourceModel.Validate(context);
        internal static string? Validate(IReadOnlyList<CombatRelicState>? relics)
        {
            if (relics == null) return null; // Legacy captures retained their external-effect guard.
            foreach (CombatRelicState relic in relics)
            {
                string? attackError = RelicStatusAttackModel.Validate(relic);
                if (attackError != null) return attackError;
                if (string.IsNullOrEmpty(relic.DataId) || string.IsNullOrEmpty(relic.AssetKey))
                    return "Missing captured relic identity.";
                foreach (string effect in relic.EffectTypes)
                {
                    if (effect == ModifyCardHealingPower || effect == Null) continue;
                    if (effect != AbilityIncant && effect != ModifyTriggerCount && effect != AddStatusOnSpawn && effect != RemoveStatusOnSpawn && effect != AddStatusOnTeamSpawn && effect != AddStatusOnRoomEntry && effect != AddStatusOnUnitMovedUpDown && effect != AddStatusOnCorruption && effect != AddTempUpgrade && effect != ModifyTrait && effect != AddTempUpgradeToHandCardsIfCheaper && effect != ModifyForgeUpgrade && effect != ModifyCardTypeCost && effect != ModifyCardHealth && effect != ModifyCardAttack && effect != ModifyAttackByStatus && effect != AddRemoveTempUpgradeOnUnitTrigger && effect != AddTempSoloUnitUpgradeStartOfTurn && effect != AddCardsNull && effect != ModifyCardMagicalPower && effect != ModifyCardMagicPowerOnCardPlayed && effect != CardDrawOnCardPlay && effect != CopyCardOnCardPlay && effect != MonsterSpellTargeting && effect != ModifyRoomCapacityOnCardPlay && effect != FirstRoomCardOnAllFloors && effect != RelicStartCombatAttachmentModel.Effect && effect != AddTrigger && effect != AddStatusOnApplied && effect != AddStatusOnOtherStatusRemoved && effect != ModifyStatusMagnitude && effect != PreventStatusDecay && effect != ModifyCharacterAttackDamage && effect != ModifyCharacterMaxHealth && effect != AllDamageIsPiercing && effect != AdditionalForgePoints && effect != HealOnMoonPhase && effect != DamageOnMonsterSummoned && effect != DamageEnemyOnAscend && effect != DamageEnemyOnDescend && effect != RelicUnstableRebalanceModel.Effect && !RelicResourceModel.IsEffect(effect)) return "Unmodeled relic effect " + effect + " on " + relic.AssetKey + ".";
                }
                string? error = RelicStartCombatAttachmentModel.Validate(relic) ?? RelicCardModifierModel.Validate(relic) ?? RelicCardTypeCostModel.Validate(relic) ?? RelicCardHealthModel.Validate(relic) ?? RelicCardAttackModel.Validate(relic) ?? RelicUnitUpgradeTriggerModel.Validate(relic) ?? RelicSoloUnitUpgradeModel.Validate(relic) ?? RelicMagicPowerModel.Validate(relic) ?? RelicCardPlayMagicPowerModel.Validate(relic) ?? RelicCopyCardOnCardPlayModel.Validate(relic) ?? RelicMonsterSpellTargetingModel.Validate(relic) ?? RelicCheaperSpellUpgradeModel.Validate(relic) ?? RelicUnstableRebalanceModel.Validate(relic) ?? RelicCardDrawOnCardPlayModel.Validate(relic) ?? RelicCardPlayRoomCapacityModel.Validate(relic) ?? RelicRoomAttachmentReplicationModel.Validate(relic) ?? RelicSummonDamageModel.Validate(relic) ?? RelicFloorTransitionDamageModel.Validate(relic) ?? RelicSpawnStatusModel.Validate(relic) ?? RelicRoomEntryStatusModel.Validate(relic) ?? RelicCorruptionStatusModel.Validate(relic) ?? RelicCharacterTriggerModel.Validate(relic) ?? RelicStatusStackModel.Validate(relic) ?? RelicStatusMagnitudeModel.Validate(relic) ?? RelicStatusDecayModel.Validate(relic) ?? RelicForgePointModel.Validate(relic) ?? RelicMoonPhaseHealModel.Validate(relic) ?? RelicMoonPhaseStatusModel.Validate(relic) ?? RelicResourceModel.Validate(relic);
                if (error != null) return error;
            }
            return null;
        }
    }
}
