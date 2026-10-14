#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('no-cards', 'steward-once', 'units-and-junk', 'units-spells-and-junk')]
    [string] $Policy = 'steward-once',
    [ValidateSet('Normal', 'Fast', 'Ultra', 'SuperUltra', 'Instant')]
    [string] $GameSpeed = 'Instant',
    [switch] $SpawnPoints,
    [switch] $PhysicalSpawnPoints,
    [switch] $Revival,
    [switch] $TriggeredSummons,
    [switch] $TriggeredSummonsFresh,
    [switch] $TriggeredSummonsDeath,
    [switch] $TriggeredSummonsEquipment,
    [switch] $TriggeredSummonsEquipmentOwned,
    [switch] $TriggeredSummonsRevival,
    [switch] $NumericUpgrades,
    [switch] $DynamicUpgrades,
    [switch] $SacrificeUpgrades,
    [switch] $SacrificeEffects,
    [switch] $SetHealth,
    [switch] $DamageByUnitsKilled,
    [switch] $TransferAttack,
    [switch] $BrainTransfer,
    [switch] $FrostChanneling,
    [switch] $TransferAttackAbility,
    [switch] $CardEffectKill,
    [switch] $CardEffectKillSubtype,
    [switch] $NullForwarding,
    [switch] $NullTriggers,
    [switch] $NullTargetless,
    [switch] $OriginalTrample,
    [switch] $OriginalSpellShield,
    [switch] $ChooseDiscard,
    [switch] $ChooseDiscardOthers,
    [switch] $ChooseDiscardOthersPolicy,
    [switch] $ChooseDiscardRefine,
    [switch] $ChooseDiscardPersistent,
    [switch] $RemoveAllStatusEffects,
    [switch] $DragonHoard,
    [switch] $TrainHeal,
    [switch] $Overheal,
    [switch] $Corruption,
    [switch] $CorruptionUnitTrigger,
    [switch] $CorruptionCarverTrigger,
    [switch] $CorruptionKeeperTrigger,
    [switch] $CorruptionRelicTrigger,
    [switch] $PermanentCorruption,
    [switch] $MoonPhaseCard,
    [switch] $MoonPhaseAdvance,
    [switch] $MoonPhaseCallbacks,
    [switch] $ConditionalNoPlayerStatus,
    [switch] $SelfAttackStatus,
    [switch] $ConditionalPyreDamage,
    [switch] $SoulSiphon,
    [switch] $StatusUpgrade,
    [switch] $CardRecursion,
    [switch] $CardRecursionDiscard,
    [switch] $CardRecursionChoose,
    [switch] $CardRecursionChooseToHand,
    [switch] $CardRecursionChooseFilter,
    [switch] $CardRecursionUpgrade,
    [switch] $CardRecursionUpgradeToHand,
    [switch] $CardRecursionUpgradeChoose,
    [switch] $CardRecursionUpgradeFilterChoose,
    [switch] $CardRecursionUpgradeReplacementChoose,
    [switch] $Mix,
    [switch] $MixAlternate,
    [switch] $MixUnstableExplosion,
    [ValidateSet('00', '01', '10', '11')]
    [string] $MixChoices,
    [switch] $HandUpgrades,
    [switch] $TargetedHandUpgrades,
    [switch] $PermanentConsumeHandUpgrades,
    [switch] $PermanentConsumeOverTemporary,
    [switch] $Healing,
    [switch] $HealingTriggers,
    [switch] $RoomSpells,
    [switch] $DamagePerHp,
    [switch] $DamagePerHpArmor,
    [switch] $DamagePerHpLastAttacker,
    [switch] $DamagePerHpAbility,
    [switch] $DamageOverflow,
    [switch] $TerminalSpells,
    [switch] $PostKillSpells,
    [switch] $RandomSpells,
    [switch] $RandomStatus,
    [switch] $CrossRoomSpells,
    [switch] $CrossRoomTargets,
    [switch] $AttackBuffs,
    [switch] $MaxHealthSpells,
    [switch] $MaxHealthLethal,
    [switch] $NumericRanges,
    [switch] $NumericRangesLethal,
    [switch] $TargetFilters,
    [switch] $Drawing,
    [switch] $HandRemoval,
    [switch] $HandRemovalLethal,
    [switch] $RandomDiscard,
    [switch] $Generation,
    [switch] $GenerationPermanentConsume,
    [switch] $GenerationLethal,
    [switch] $StatisticQueries,
    [switch] $StatisticOverflow,
    [switch] $DamageScaling,
    [switch] $DynamicStatistics,
    [switch] $StatusScaling,
    [switch] $UnitUpgradeScaling,
    [switch] $UnitTriggerUpgrades,
    [switch] $SpawnTriggers,
    [switch] $SpawnTriggersLethal,
    [switch] $UnitTurnBegin,
    [switch] $TeamTurnBegin,
    [switch] $PreHandDiscard,
    [switch] $PreHandDiscardLethal,
    [switch] $CloneUpgradeRefresh,
    [switch] $PreCombat,
    [switch] $PreCombatLethal,
    [switch] $TriggeredHealing,
    [switch] $PostCombatHealing,
    [switch] $TriggeredDamage,
    [switch] $DamageDeathQueue,
    [switch] $TerminalDeathDamage,
    [switch] $HitKill,
    [switch] $DyingUpgrades,
    [switch] $AttackTriggers,
    [switch] $TriggeredStatus,
    [switch] $StatusCallbacks,
    [switch] $StatusCallbackActions,
    [switch] $EnergyEffects,
    [switch] $EnergyEffectsLethal,
    [switch] $ForgePointsEffects,
    [switch] $XCost,
    [switch] $XCostLethal,
    [switch] $RoomCapacity,
    [switch] $RoomCapacityLethal,
    [switch] $RoomCapacityAttachment,
    [switch] $DirectUnitUpgrades,
    [switch] $Equipment,
    [switch] $EquipmentBuff,
    [switch] $EquipmentExhausted,
    [switch] $EquipmentOverflow,
    [switch] $EquipmentTriggers,
    [switch] $TriggerMutation,
    [switch] $DetachedBonusDraw,
    [switch] $ConditionalTriggers,
    [switch] $TriggerRepeats,
    [switch] $CompanionBoss,
    [switch] $AbilityEffects,
    [switch] $EquipmentAbilities,
    [switch] $HordeStats,
    [switch] $EnchantmentLifecycle,
    [switch] $EnchantmentCombat,
    [switch] $EnchantmentWorld,
    [switch] $HordeStatuses,
    [switch] $HordeMerge,
    [switch] $Bump,
    [switch] $UnitClone,
    [switch] $UnitCopy,
    [switch] $HeroCopy,
    [switch] $SpawnEnchant,
    [switch] $Purify,
    [switch] $PurifyQueues,
    [switch] $Incant,
    [switch] $IncantThresholds,
    [switch] $AbilityIncant,
    [switch] $PersistentEnchantments,
    [switch] $PersistentEnchantmentDeaths,
    [switch] $PersistentEnchantmentRevivals,
    [switch] $PersistentEnchantmentRandomPools,
    [switch] $PersistentEnchantmentUpgrades,
    [switch] $PersistentEnchantmentSummons,
    [switch] $PersistentEnchantmentSummonsFresh,
    [switch] $TraceEnchanterRemoval,
    [switch] $CharacterRemoval,
    [switch] $PreviewReferences,
    [switch] $RelicCatalog,
    [switch] $FullRuleCatalog,
    [switch] $UpgradeMasks,
    [switch] $ModifierOverflow,
    [switch] $TraitComposition,
    [switch] $CardStatusComposition,
    [switch] $OwnedCardMasks,
    [switch] $CardUpgradeLifecycle,
    [switch] $BranchCardMasks,
    [switch] $RelicCardUpgrades,
    [switch] $BattleRelicUpgrades,
    [switch] $ConditionalBattleRelicUpgrades,
    [switch] $RelicCardStatusUpgrades,
    [switch] $RelicCardPiercingUpgrades,
    [switch] $RelicCardSelfPurgeUpgrades,
    [switch] $RelicCardExhaustUpgrades,
    [switch] $RelicCardEphemeralUpgrades,
    [switch] $RelicCardHealthUpgrades,
    [switch] $RelicCardUnhealedHealthUpgrades,
    [switch] $RelicCardAbilityUpgrades,
    [switch] $RelicCardTriggerUpgrades,
    [switch] $RelicCardCastTriggerUpgrades,
    [switch] $RelicCardCastTriggerUnitUpgrades,
    [switch] $RelicCardReplacementUpgrades,
    [switch] $OriginalRelicBatch,
    [switch] $OriginalDeployableRelic,
    [switch] $OriginalStingRelicBatch,
    [switch] $OriginalUnitRelicBatch,
    [switch] $OriginalEndlessReturn,
    [switch] $OriginalTriggerRelicBatch,
    [switch] $CovenantCharacterFilter,
    [switch] $TitanSkinCombat,
    [switch] $AllUnitsMultistrikeMutator,
    [switch] $OriginalStatusRelicBatch,
    [switch] $OriginalStatusRelicBatchBurnout,
    [switch] $OriginalMagnitudeRelicBatch,
    [switch] $OriginalLifecycleRelicBatch,
    [switch] $OriginalDecayRelicBatch,
    [switch] $OriginalResourceRelicBatch,
    [switch] $PyreResourceRefresh,
    [switch] $OriginalFloorCapacityRelics,
    [switch] $Mageblade,
    [switch] $OriginalMagebladeMagnitudeRelic,
    [switch] $OriginalCardPlayMagicPower,
    [switch] $OriginalCardDrawOnCardPlay,
    [switch] $UnplayedCardTrigger,
    [switch] $UnplayedCardEffects,
    [switch] $OriginalRoomEntryStatusRelics,
    [switch] $OriginalMoonPhaseEnergyRelic,
    [switch] $OriginalCardAttackRelic,
    [switch] $OriginalUnitUpgradeTriggerRelic,
    [switch] $TypedDraw,
    [switch] $IncantRelic,
    [switch] $IncantRelics,
    [switch] $SpawnStatusRelics,
    [switch] $OriginalTunedGearRelic,
    [switch] $OriginalEggHatchRelic,
    [switch] $OriginalEggTransformation,
    [switch] $OriginalEggCard,
    [switch] $OriginalEggCardDelayed,
    [switch] $OriginalChicotRelic,
    [switch] $OriginalSpawnCombatRelics,
    [switch] $OriginalStealthWaxersRelic,
    [switch] $OriginalTeamSpawnStatusRelic,
    [switch] $SpawnStatusRelicsClones,
    [switch] $MixedCardlessRelicOrder,
    [switch] $SettleDeathDissolves,
    [switch] $SettleCardAnimations,
    [switch] $HarvestTriggers,
    [switch] $HordeRemoval,
    [switch] $HordeDeath,
    [switch] $HordeUpgrades,
    [switch] $DyingHordeUpgrades,
    [switch] $RallyTriggers,
    [switch] $LethalRally,
    [switch] $MultiSummon,
    [switch] $MultiSummonZero,
    [switch] $MultiSummonUpgrade,
    [switch] $MultiSummonUpgradeUnique,
    [switch] $MultiSummonUpgradeRestricted,
    [switch] $MultiSummonFresh,
    [switch] $MultiSummonFreshDeaths,
    [switch] $MultiSummonAdditional,
    [switch] $MultiSummonAdditionalFresh,
    [switch] $MultiSummonPool,
    [switch] $MultiSummonPoolAdditional,
    [switch] $MultiSummonPoolAdditionalFresh,
    [switch] $MultiSummonPoolSingletonAdditional,
    [switch] $MultiSummonPoolNoPrimary,
    [switch] $MultiSummonPoolMissingFresh,
    [switch] $MultiSummonPoolAdditionalMissingFresh,
    [switch] $MultiSummonPoolNoPrimaryMissingFresh,
    [switch] $MultiSummonPoolNoPrimaryMissingFreshDeaths,
    [switch] $AbilityLifecycle,
    [switch] $AbilityCooldown,
    [switch] $AbilityCache,
    [switch] $AbilityActivation,
    [switch] $AbilityActivationX,
    [switch] $AbilityActivationLethal,
    [switch] $AbilityTeleport,
    [switch] $SpawnOrStatus,
    [switch] $Sentry,
    [switch] $SentryLethal,
    [switch] $BonusDraw,
    [switch] $BonusDrawLethal,
    [switch] $BinaryCapture = $true,
    [switch] $CaptureJson,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
if ($OriginalTunedGearRelic -and ($OriginalStealthWaxersRelic -or $OriginalTeamSpawnStatusRelic -or $SpawnStatusRelicsClones -or $MixedCardlessRelicOrder)) {
    throw 'OriginalTunedGearRelic is an isolated spawn-status scenario.'
}
if ($OriginalChicotRelic -and ($OriginalTunedGearRelic -or $OriginalStealthWaxersRelic -or $OriginalTeamSpawnStatusRelic -or $SpawnStatusRelicsClones -or $MixedCardlessRelicOrder)) {
    throw 'OriginalChicotRelic is an isolated spawn-status scenario.'
}
if ($OriginalSpawnCombatRelics -and ($OriginalChicotRelic -or $OriginalTunedGearRelic -or $OriginalStealthWaxersRelic -or $OriginalTeamSpawnStatusRelic -or $SpawnStatusRelicsClones -or $MixedCardlessRelicOrder)) {
    throw 'OriginalSpawnCombatRelics is an isolated spawn-status scenario.'
}
if ($OriginalEggHatchRelic -and ($OriginalSpawnCombatRelics -or $OriginalChicotRelic -or $OriginalTunedGearRelic -or $OriginalStealthWaxersRelic -or $OriginalTeamSpawnStatusRelic -or $SpawnStatusRelicsClones -or $MixedCardlessRelicOrder)) {
    throw 'OriginalEggHatchRelic is an isolated spawn-status scenario.'
}
if ($OriginalEggTransformation -and ($OriginalEggHatchRelic -or $OriginalSpawnCombatRelics -or $OriginalChicotRelic -or $OriginalTunedGearRelic -or $OriginalStealthWaxersRelic -or $OriginalTeamSpawnStatusRelic -or $SpawnStatusRelicsClones -or $MixedCardlessRelicOrder)) {
    throw 'OriginalEggTransformation is an isolated spawn-status scenario.'
}
if (($OriginalEggCard -or $OriginalEggCardDelayed) -and
    ($OriginalEggTransformation -or $SpawnStatusRelics -or $OriginalEggHatchRelic -or ($OriginalEggCard -and $OriginalEggCardDelayed))) {
    throw 'OriginalEggCard is an isolated original-card scenario.'
}
if ($OriginalTunedGearRelic) { $SpawnStatusRelics = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalEggHatchRelic) { $SpawnStatusRelics = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalEggTransformation) { $SpawnStatusRelics = $true; $Policy = 'units-spells-and-junk'; $PhysicalSpawnPoints = $true }
if ($OriginalEggCard -or $OriginalEggCardDelayed) { $Policy = 'units-spells-and-junk'; $PhysicalSpawnPoints = $true }
if ($OriginalChicotRelic) { $SpawnStatusRelics = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSpawnCombatRelics) { $SpawnStatusRelics = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStealthWaxersRelic) { $SpawnStatusRelics = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalTeamSpawnStatusRelic) { $SpawnStatusRelics = $true; $SpawnStatusRelicsClones = $true; $Policy = 'units-spells-and-junk' }
if ($RoomCapacityAttachment) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($PermanentConsumeHandUpgrades -or $PermanentConsumeOverTemporary) { $HandUpgrades = $true; $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($GenerationPermanentConsume) { $Generation = $true; $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($TypedDraw) { $BranchCardMasks = $true; $BattleRelicUpgrades = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalUnitRelicBatch -or $OriginalEndlessReturn) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($AllUnitsMultistrikeMutator) { $BattleRelicUpgrades = $true; $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStingRelicBatch -or $OriginalDeployableRelic) { $BattleRelicUpgrades = $true; $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStatusRelicBatch -or $OriginalStatusRelicBatchBurnout) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMagnitudeRelicBatch) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDecayRelicBatch) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalResourceRelicBatch) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($PyreResourceRefresh) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFloorCapacityRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($Mageblade -or $OriginalMagebladeMagnitudeRelic) { $BranchCardMasks = $true; $BattleRelicUpgrades = $true; $Policy = 'units-and-junk' }
if ($OriginalCardPlayMagicPower) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalCardDrawOnCardPlay) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($UnplayedCardTrigger) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($UnplayedCardEffects) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalRoomEntryStatusRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMoonPhaseEnergyRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCardAttackRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalUnitUpgradeTriggerRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalTriggerRelicBatch -or $OriginalLifecycleRelicBatch) {
    $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $SettleDeathDissolves = $true; $SettleCardAnimations = $true
    $Policy = 'units-spells-and-junk'
}
if ($ConditionalBattleRelicUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardStatusUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardPiercingUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardSelfPurgeUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardExhaustUpgrades) { $BattleRelicUpgrades = $true; $Policy = 'units-spells-and-junk' }
if ($RelicCardEphemeralUpgrades) { $BattleRelicUpgrades = $true; $Policy = 'units-spells-and-junk' }
if ($RelicCardHealthUpgrades) { $BattleRelicUpgrades = $true; $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($RelicCardUnhealedHealthUpgrades) { $BattleRelicUpgrades = $true; $Policy = 'units-spells-and-junk' }
if ($RelicCardAbilityUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardTriggerUpgrades) { $BattleRelicUpgrades = $true }
if ($RelicCardCastTriggerUpgrades) { $BattleRelicUpgrades = $true; $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($RelicCardCastTriggerUnitUpgrades) { $BattleRelicUpgrades = $true; $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($RelicCardReplacementUpgrades) { $BattleRelicUpgrades = $true }
if ($MixedCardlessRelicOrder) { $SpawnStatusRelicsClones = $true }
if ($DamageOverflow) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($BattleRelicUpgrades -or $SpawnStatusRelicsClones -or $OriginalRelicBatch -or $OriginalStingRelicBatch -or $OriginalDeployableRelic) { $BranchCardMasks = $true }
if ($SpawnStatusRelicsClones) { $SpawnStatusRelics = $true; $UnitClone = $true }
if ($EnchantmentWorld) { $EnchantmentCombat = $true }
if ($PersistentEnchantmentRevivals) { $PersistentEnchantmentDeaths = $true }
if ($PersistentEnchantmentDeaths) { $PersistentEnchantments = $true }
if ($PersistentEnchantmentRandomPools) { $PersistentEnchantments = $true }
if ($PersistentEnchantmentSummonsFresh) { $PersistentEnchantmentSummons = $true }
if ($PersistentEnchantmentSummons) {
    if ($PersistentEnchantmentDeaths -or $PersistentEnchantmentUpgrades -or $TriggeredSummons -or $TriggeredSummonsFresh -or $TriggeredSummonsDeath -or $TriggeredSummonsEquipment -or $TriggeredSummonsEquipmentOwned -or $TriggeredSummonsRevival) {
        throw 'Choose aura child summons or source death/upgrades/standalone summons.'
    }
    $PersistentEnchantments = $true
}
if ($PersistentEnchantmentUpgrades) {
    if ($PersistentEnchantmentDeaths) { throw 'Choose aura child upgrades or source death/revival.' }
    $PersistentEnchantments = $true
}
if ($PersistentEnchantments) { $PhysicalSpawnPoints = $true }
if ($Revival -and $TriggeredSummonsRevival) { throw 'Choose standalone revival or triggered summon revival.' }
if ($TriggeredSummonsRevival) { $TriggeredSummonsEquipmentOwned = $true; $TriggeredSummonsDeath = $true }
if ($TriggerRepeats) { $ConditionalTriggers = $true }
if ($TriggeredSummonsEquipmentOwned) { $TriggeredSummonsEquipment = $true }
if ($TriggeredSummonsFresh -or $TriggeredSummonsDeath -or $TriggeredSummonsEquipment) { $TriggeredSummons = $true }
if ($TriggeredSummons -or $Revival -or $HordeMerge -or $Bump -or $UnitClone -or $UnitCopy -or $HeroCopy -or $SpawnEnchant) { $PhysicalSpawnPoints = $true }
if ($MultiSummonUpgradeUnique -or $MultiSummonUpgradeRestricted) { $MultiSummonUpgrade = $true }
if ($MultiSummonAdditionalFresh) { $MultiSummonAdditional = $true; $MultiSummonFresh = $true }
$missingFresh = $MultiSummonPoolMissingFresh -or $MultiSummonPoolAdditionalMissingFresh -or $MultiSummonPoolNoPrimaryMissingFresh -or $MultiSummonPoolNoPrimaryMissingFreshDeaths
if ($missingFresh) { $MultiSummonPool = $true; $MultiSummonFresh = $true }
if ($MultiSummonPoolNoPrimaryMissingFreshDeaths) { $MultiSummonFreshDeaths = $true }
if ($MultiSummonPoolAdditional -or $MultiSummonPoolAdditionalFresh -or $MultiSummonPoolSingletonAdditional -or $MultiSummonPoolNoPrimary) { $MultiSummonPool = $true }
if ($MultiSummonPoolAdditionalFresh) { $MultiSummonFresh = $true }
if ($MultiSummonFreshDeaths) { $MultiSummonFresh = $true }
if ($MultiSummonZero -or $MultiSummonUpgrade -or $MultiSummonFresh -or $MultiSummonAdditional -or $MultiSummonPool) { $MultiSummon = $true }
if ($EquipmentExhausted -or $EquipmentOverflow -or $EquipmentTriggers) { $Equipment = $true }
if ($StatusCallbackActions) { $StatusCallbacks = $true }
if ($StatusCallbacks) { $TriggeredStatus = $true }
if ($PurifyQueues) { $Purify = $true }
if ($IncantThresholds) { $Incant = $true }
if ($CaptureJson) { $BinaryCapture = $true }
$damagePerHpModes = @()
if ($DamagePerHp) { $damagePerHpModes += 'damage-per-hp' }
if ($DamagePerHpArmor) { $damagePerHpModes += 'damage-per-hp-armor' }
if ($DamagePerHpLastAttacker) { $damagePerHpModes += 'damage-per-hp-last-attacker' }
if ($damagePerHpModes.Count -gt 1) { throw 'Choose one DamagePerHp scenario.' }
if ($DamagePerHpAbility -and $damagePerHpModes.Count -gt 0) { throw 'Choose the unit-ability scenario separately from spell scenarios.' }
$cardRecursionModes = @($CardRecursion, $CardRecursionDiscard, $CardRecursionChoose, $CardRecursionChooseToHand, $CardRecursionChooseFilter, $CardRecursionUpgrade, $CardRecursionUpgradeToHand, $CardRecursionUpgradeChoose, $CardRecursionUpgradeFilterChoose, $CardRecursionUpgradeReplacementChoose) |
    Where-Object { $_ }
if ($cardRecursionModes.Count -gt 1) { throw 'Choose one CardRecursion scenario.' }
if ($SacrificeEffects) { $Policy = 'units-spells-and-junk' }
if ($SetHealth) { $Policy = 'units-spells-and-junk' }
if ($DamageByUnitsKilled) { $Policy = 'units-spells-and-junk' }
if ($TransferAttack) { $Policy = 'units-spells-and-junk' }
if ($BrainTransfer) { $Policy = 'units-spells-and-junk' }
if ($FrostChanneling) { $Policy = 'units-spells-and-junk' }
if ($TransferAttackAbility) { $Policy = 'units-spells-and-junk' }
if ($CardEffectKill -and $CardEffectKillSubtype) { throw 'Choose either -CardEffectKill or -CardEffectKillSubtype.' }
if ($CardEffectKill -or $CardEffectKillSubtype) { $Policy = 'units-spells-and-junk' }
if ($NullForwarding) { $Policy = 'units-spells-and-junk' }
if ($NullTriggers) { $Policy = 'units-spells-and-junk' }
if ($NullTargetless) { $Policy = 'units-spells-and-junk' }
if ($OriginalTrample) { $Policy = 'units-spells-and-junk' }
if ($OriginalSpellShield) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscard) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscardOthers) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscardOthersPolicy) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscardRefine) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscardPersistent) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($RemoveAllStatusEffects) { $Policy = 'units-spells-and-junk' }
if ($DragonHoard) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($TrainHeal) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($Overheal) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($Corruption) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($CorruptionUnitTrigger) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($CorruptionCarverTrigger) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($CorruptionKeeperTrigger) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($CorruptionRelicTrigger) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($PermanentCorruption) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($MoonPhaseCard) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($MoonPhaseAdvance) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($MoonPhaseCallbacks) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ConditionalNoPlayerStatus) { $Policy = 'units-spells-and-junk' }
if ($SelfAttackStatus) { $Policy = 'units-spells-and-junk' }
if ($ConditionalPyreDamage) { $Policy = 'units-spells-and-junk' }
if ($SoulSiphon) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($StatusUpgrade) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($CardRecursion -or $CardRecursionDiscard -or $CardRecursionChoose -or $CardRecursionChooseToHand -or $CardRecursionChooseFilter -or $CardRecursionUpgrade -or $CardRecursionUpgradeToHand -or $CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose) { $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true }
if ($MixAlternate -and $MixChoices) { throw 'Choose either -MixAlternate or -MixChoices.' }
if ($MixAlternate) { $MixChoices = '11' }
if ($MixUnstableExplosion -and $MixChoices -and $MixChoices -ne '01') { throw 'Unstable explosion requires Mix choices 01.' }
if ($MixUnstableExplosion) { $MixChoices = '01' }
if ($MixChoices) { $Mix = $true }
if ($Mix) { $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true }
if ($SpawnOrStatus) { $Policy = 'units-and-junk' }
if ($AbilityTeleport) { $Policy = 'units-and-junk'; $PhysicalSpawnPoints = $true }
if ($damagePerHpModes.Count -gt 0) { $Policy = 'units-spells-and-junk' }
if ($DamagePerHpAbility) { $Policy = 'units-spells-and-junk' }
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$gameRoot = Join-Path $workspace '.sandbox-game'
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$profile = Join-Path $workspace ('.probe-runs\full-battle-' + $Policy + '-' + $runId)
$originalLogRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\Shiny Shoe\MonsterTrain2'

function Get-OriginalSignature {
    $parts = Get-ChildItem -LiteralPath $originalLogRoot -File -Force -Recurse |
        Sort-Object FullName | ForEach-Object {
            $_.FullName + ':' + $_.LastWriteTimeUtc.Ticks + ':' +
                (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    return [string]::Join('|', [string[]] @($parts))
}

if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src\Probe\Probe.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Probe build failed.' }
    if ($BinaryCapture) {
        dotnet build (Join-Path $workspace 'src\FixtureArchive\FixtureArchive.csproj') -c Release -f net8.0
        if ($LASTEXITCODE -ne 0) { throw 'Fixture inspection build failed.' }
    }
}
$pluginDir = Join-Path $gameRoot 'BepInEx\plugins'
Copy-Item -LiteralPath (Join-Path $workspace 'src\Probe\bin\Release\netstandard2.1\Probe.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace 'src\Model\bin\Release\netstandard2.1\Model.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace 'src\FixtureArchive\bin\Release\netstandard2.1\FixtureArchive.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $profile -Recurse
$preferencesPath = Join-Path $profile 'preferences.json'
if (Test-Path -LiteralPath $preferencesPath) {
    $preferences = Get-Content -LiteralPath $preferencesPath -Raw | ConvertFrom-Json
    if ($null -eq $preferences.audioSettings) { throw 'Probe profile is missing audio settings.' }
    $preferences.audioSettings.globalMuted = $true
    $preferences | ConvertTo-Json -Depth 32 -Compress | Set-Content -LiteralPath $preferencesPath -NoNewline -Encoding utf8
}
$environment = @{
    MT2_PROBE_DATA_DIR = $profile
    MT2_PROBE_SCENARIO = 'native-replay'
    MT2_PROBE_FULL_BATTLE = '1'
    MT2_PROBE_FULL_BATTLE_POLICY = $Policy
    MT2_PROBE_MODIFIERS = $(if ($RelicCardUnhealedHealthUpgrades) { 'relic-card-unhealed-upgrades' } elseif ($RelicCardStatusUpgrades) { 'relic-card-status-upgrades' } elseif ($ConditionalBattleRelicUpgrades) { 'conditional-relic-card-upgrades' } elseif ($Revival) { 'revival' } elseif ($TriggeredSummonsEquipment) { 'triggered-summon-equipment' + $(if ($TriggeredSummonsEquipmentOwned) { '-owned' } else { '' }) + $(if ($TriggeredSummonsDeath) { '-death' } else { '' }) + $(if ($TriggeredSummonsFresh) { '-fresh' } else { '' }) } elseif ($TriggeredSummonsDeath -and $TriggeredSummonsFresh) { 'triggered-summon-death-fresh' } elseif ($TriggeredSummonsDeath) { 'triggered-summon-death' } elseif ($TriggeredSummonsFresh) { 'triggered-summon-fresh' } elseif ($TriggeredSummons) { 'triggered-summon' } elseif ($SpawnPoints) { 'spawn-points' } elseif ($MultiSummonPoolNoPrimaryMissingFreshDeaths) { 'multi-summon-pool-no-primary-missing-fresh-deaths' } elseif ($MultiSummonPoolNoPrimaryMissingFresh) { 'multi-summon-pool-no-primary-missing-fresh' } elseif ($MultiSummonPoolAdditionalMissingFresh) { 'multi-summon-pool-additional-missing-fresh' } elseif ($MultiSummonPoolMissingFresh) { 'multi-summon-pool-missing-fresh' } elseif ($MultiSummonPoolAdditionalFresh) { 'multi-summon-pool-additional-fresh' } elseif ($MultiSummonPoolSingletonAdditional) { 'multi-summon-pool-singleton-additional' } elseif ($MultiSummonPoolNoPrimary) { 'multi-summon-pool-no-primary' } elseif ($MultiSummonPoolAdditional) { 'multi-summon-pool-additional' } elseif ($MultiSummonPool) { 'multi-summon-pool' } elseif ($MultiSummonAdditionalFresh) { 'multi-summon-additional-fresh' } elseif ($MultiSummonAdditional) { 'multi-summon-additional' } elseif ($MultiSummonFreshDeaths) { 'multi-summon-fresh-deaths' } elseif ($MultiSummonFresh) { 'multi-summon-fresh' } elseif ($MultiSummonUpgradeRestricted) { 'multi-summon-upgrade-restricted' } elseif ($MultiSummonUpgradeUnique) { 'multi-summon-upgrade-unique' } elseif ($MultiSummonUpgrade) { 'multi-summon-upgrade' } elseif ($MultiSummonZero) { 'multi-summon-zero' } elseif ($MultiSummon) { 'multi-summon' } elseif ($LethalRally) { 'rally-lethal' } elseif ($RallyTriggers) { 'rally-triggers' } elseif ($DyingHordeUpgrades) { 'dying-horde-upgrades' } elseif ($HordeUpgrades) { 'horde-upgrades' } elseif ($HordeDeath) { 'horde-death' } elseif ($HordeRemoval) { 'horde-removal' } elseif ($HarvestTriggers) { 'harvest-triggers' } elseif ($HordeStatuses) { 'horde-statuses' } elseif ($EquipmentAbilities) { 'equipment-abilities' } elseif ($AbilityEffects) { 'ability-effects' } elseif ($AbilityLifecycle) { 'ability-lifecycle' } elseif ($AbilityTeleport) { 'ability-teleport' } elseif ($AbilityActivationLethal) { 'ability-activation-lethal' } elseif ($AbilityActivationX) { 'ability-activation-x' } elseif ($AbilityActivation) { 'ability-activation' } elseif ($AbilityCache) { 'ability-cache' } elseif ($AbilityCooldown) { 'ability-cooldown' } elseif ($CompanionBoss) { 'companion-boss' } elseif ($ConditionalTriggers) { 'conditional-triggers' } elseif ($DetachedBonusDraw) { 'detached-bonus-draw' } elseif ($TriggerMutation) { 'trigger-mutation' } elseif ($EquipmentTriggers) { 'equipment-triggers' } elseif ($EquipmentOverflow) { 'equipment-overflow' } elseif ($EquipmentExhausted) { 'equipment-exhausted' } elseif ($Equipment) { 'equipment' } elseif ($DirectUnitUpgrades) { 'direct-unit-upgrades' } elseif ($RoomCapacityLethal) { 'room-capacity-lethal' } elseif ($RoomCapacity) { 'room-capacity' } elseif ($BonusDrawLethal) { 'bonus-draw-lethal' } elseif ($BonusDraw) { 'bonus-draw' } elseif ($XCostLethal) { 'x-cost-lethal' } elseif ($XCost) { 'x-cost' } elseif ($EnergyEffectsLethal) { 'energy-effects-lethal' } elseif ($EnergyEffects) { 'energy-effects' } elseif ($TriggeredStatus) { 'triggered-status' } elseif ($AttackTriggers) { 'attack-triggers' } elseif ($DyingUpgrades) { 'dying-upgrades' } elseif ($HitKill) { 'hit-kill' } elseif ($TerminalDeathDamage) { 'terminal-death-damage' } elseif ($DamageDeathQueue) { 'damage-death-queue' } elseif ($TriggeredDamage) { 'triggered-damage' } elseif ($PostCombatHealing) { 'post-combat-healing' } elseif ($TriggeredHealing) { 'triggered-healing' } elseif ($PreCombatLethal) { 'pre-combat-lethal' } elseif ($PreCombat) { 'pre-combat' } elseif ($CloneUpgradeRefresh) { 'clone-upgrade-refresh' } elseif ($PreHandDiscardLethal) { 'pre-hand-discard-lethal' } elseif ($PreHandDiscard) { 'pre-hand-discard' } elseif ($TeamTurnBegin) { 'team-turn-begin' } elseif ($UnitTurnBegin) { 'unit-turn-begin' } elseif ($SpawnTriggersLethal) { 'spawn-triggers-lethal' } elseif ($SpawnTriggers) { 'spawn-triggers' } elseif ($UnitTriggerUpgrades) { 'unit-trigger-upgrades' } elseif ($UnitUpgradeScaling) { 'unit-upgrade-scaling' } elseif ($StatusScaling) { 'status-scaling' } elseif ($DynamicStatistics) { 'dynamic-statistics' } elseif ($DamageScaling) { 'damage-scaling' } elseif ($GenerationLethal) { 'generation-lethal' } elseif ($Generation) { 'generation' } elseif ($HandRemovalLethal) { 'hand-removal-lethal' } elseif ($HandRemoval) { 'hand-removal' } elseif ($Drawing) { 'drawing' } elseif ($TargetFilters) { 'target-filters' } elseif ($NumericRangesLethal) { 'numeric-ranges-lethal' } elseif ($NumericRanges) { 'numeric-ranges' } elseif ($MaxHealthLethal) { 'max-health-lethal' } elseif ($MaxHealthSpells) { 'max-health-spells' } elseif ($AttackBuffs) { 'attack-buffs' } elseif ($CrossRoomTargets) { 'cross-room-targets' } elseif ($CrossRoomSpells) { 'cross-room-spells' } elseif ($RandomStatus) { 'random-status' } elseif ($RandomSpells) { 'random-spells' } elseif ($PostKillSpells) { 'post-kill-spells' } elseif ($TerminalSpells) { 'terminal-spells' } elseif ($RoomSpells) { 'room-spells' } elseif ($HealingTriggers) { 'healing-triggers' } elseif ($Healing) { 'healing' } elseif ($TargetedHandUpgrades) { 'targeted-hand-upgrades' } elseif ($HandUpgrades) { 'hand-upgrades' } elseif ($SacrificeUpgrades) { 'sacrifice-upgrades' } elseif ($DynamicUpgrades) { 'dynamic-upgrades' } elseif ($NumericUpgrades) { 'numeric-upgrades' } else { '' })
    MT2_PROBE_DIRECT_BRANCH = '1'
    MT2_PROBE_DEPTH = '100'
    MT2_PROBE_TARGET_TURN = '0'
    MT2_PROBE_SOURCE_PLAY_TURNS = ''
    MT2_PROBE_BRANCH_ANY_UNIT = '0'
    MT2_PROBE_FAST_REPLAY = '0'
    MT2_PROBE_GAME_SPEED = $GameSpeed
    MT2_PROBE_CHARACTER_REMOVAL = $(if ($CharacterRemoval) { '1' } else { '0' })
    MT2_PROBE_ENCHANTER_REMOVAL_TRACE = $(if ($TraceEnchanterRemoval) { '1' } else { '0' })
    MT2_PROBE_PREVIEW_REFERENCES = $(if ($PreviewReferences) { '1' } else { '0' })
    MT2_PROBE_RELIC_CATALOG = $(if ($RelicCatalog) { '1' } else { '0' })
    MT2_PROBE_FULL_RULE_CATALOG = $(if ($FullRuleCatalog) { '1' } else { '0' })
    MT2_PROBE_UPGRADE_MASKS = $(if ($UpgradeMasks) { '1' } else { '0' })
    MT2_PROBE_MODIFIER_OVERFLOW = $(if ($ModifierOverflow) { '1' } else { '0' })
    MT2_PROBE_TRAIT_COMPOSITION = $(if ($TraitComposition) { '1' } else { '0' })
    MT2_PROBE_CARD_STATUS_COMPOSITION = $(if ($CardStatusComposition) { '1' } else { '0' })
    MT2_PROBE_OWNED_CARD_MASKS = $(if ($OwnedCardMasks) { '1' } else { '0' })
    MT2_PROBE_CARD_UPGRADE_LIFECYCLE = $(if ($CardUpgradeLifecycle) { '1' } else { '0' })
    MT2_PROBE_BRANCH_CARD_MASKS = $(if ($BranchCardMasks) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_UPGRADES = $(if ($RelicCardUpgrades) { '1' } else { '0' })
    MT2_PROBE_BATTLE_RELIC_UPGRADES = $(if ($BattleRelicUpgrades) { '1' } else { '0' })
    MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES = $(if ($ConditionalBattleRelicUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_STATUS_UPGRADE = $(if ($RelicCardStatusUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_PIERCING_UPGRADE = $(if ($RelicCardPiercingUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_SELF_PURGE_UPGRADE = $(if ($RelicCardSelfPurgeUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_EXHAUST_UPGRADE = $(if ($RelicCardExhaustUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_EPHEMERAL_UPGRADE = $(if ($RelicCardEphemeralUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_HEALTH_UPGRADE = $(if ($RelicCardHealthUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_UNHEALED_UPGRADE = $(if ($RelicCardUnhealedHealthUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_ABILITY_UPGRADE = $(if ($RelicCardAbilityUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_TRIGGER_UPGRADE = $(if ($RelicCardTriggerUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_CAST_TRIGGER_UPGRADE = $(if ($RelicCardCastTriggerUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_CAST_TRIGGER_UNIT_UPGRADE = $(if ($RelicCardCastTriggerUnitUpgrades) { '1' } else { '0' })
    MT2_PROBE_RELIC_CARD_REPLACEMENT_UPGRADE = $(if ($RelicCardReplacementUpgrades) { '1' } else { '0' })
    MT2_PROBE_GENERATION = $(if ($Generation) { '1' } else { '0' })
    MT2_PROBE_MIXED_CARDLESS_RELIC_ORDER = $(if ($MixedCardlessRelicOrder) { '1' } else { '0' })
    MT2_PROBE_SETTLE_DEATH_DISSOLVES = $(if ($SettleDeathDissolves) { '1' } else { '0' })
    MT2_PROBE_SETTLE_CARD_ANIMATIONS = $(if ($SettleCardAnimations) { '1' } else { '0' })
    MT2_PROBE_STATUS_CALLBACKS = $(if ($HordeStatuses -or $StatusCallbacks) { '1' } else { '0' })
    MT2_PROBE_STATUS_CALLBACK_ACTIONS = $(if ($StatusCallbackActions) { '1' } else { '0' })
    MT2_PROBE_PHYSICAL_SPAWNPOINTS = $(if ($PhysicalSpawnPoints) { '1' } else { '0' })
    MT2_PROBE_BINARY_CAPTURE = $(if ($BinaryCapture) { '1' } else { '0' })
    MT2_PROBE_CAPTURE_JSON = $(if ($CaptureJson) { '1' } else { '0' })
    MT2_PROBE_NO_TIMEOUT = '1'
    MT2_PROBE_ISOLATE_PREVIEW_RNG = $(if ($TriggeredStatus -or $PersistentEnchantments) { '1' } else { '0' })
    MT2_PROBE_ISOLATE_UI_RNG = $(if ($CompanionBoss -or $ConditionalTriggers -or $DetachedBonusDraw -or $RoomCapacity -or $RoomCapacityLethal -or $RoomCapacityAttachment -or $BonusDraw -or $BonusDrawLethal -or $NumericRanges -or $NumericRangesLethal -or $Drawing -or $TriggeredHealing -or $PostCombatHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage -or $HitKill -or $DyingUpgrades -or $AttackTriggers -or $TriggeredStatus) { '1' } else { '0' })
    MT2_PROBE_STATISTIC_QUERIES = $(if ($StatisticQueries) { '1' } else { '0' })
    MT2_PROBE_STATISTIC_OVERFLOW = $(if ($StatisticOverflow) { '1' } else { '0' })
    MT2_PROBE_HORDE_STATS = $(if ($HordeStats) { '1' } else { '0' })
    MT2_PROBE_ENCHANTMENT_LIFECYCLE = $(if ($EnchantmentLifecycle) { '1' } else { '0' })
    MT2_PROBE_ENCHANTMENT_COMBAT = $(if ($EnchantmentCombat) { '1' } else { '0' })
    MT2_PROBE_ENCHANTMENT_WORLD = $(if ($EnchantmentWorld) { '1' } else { '0' })
    MT2_PROBE_TRIGGER_REPEATS = $(if ($TriggerRepeats) { '1' } else { '0' })
}
if ($EquipmentBuff) { $environment['MT2_PROBE_MODIFIERS'] = 'equipment-buff' }
if ($ForgePointsEffects) { $environment['MT2_PROBE_MODIFIERS'] = 'forge-points' }
if ($TriggeredSummonsRevival) {
    $environment['MT2_PROBE_MODIFIERS'] = 'triggered-summon-equipment-owned-death-revival' + $(if ($TriggeredSummonsFresh) { '-fresh' } else { '' })
}
if ($HordeMerge) { $environment['MT2_PROBE_MODIFIERS'] = 'horde-merge' }
if ($Bump) { $environment['MT2_PROBE_MODIFIERS'] = 'bump' }
if ($UnitClone) { $environment['MT2_PROBE_MODIFIERS'] = 'unit-clone' }
if ($UnitCopy) { $environment['MT2_PROBE_MODIFIERS'] = 'unit-copy' }
if ($HeroCopy) { $environment['MT2_PROBE_MODIFIERS'] = 'hero-copy' }
if ($SpawnEnchant) { $environment['MT2_PROBE_MODIFIERS'] = 'spawn-enchant' }
if ($Purify) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($PurifyQueues) { 'purify-queues' } else { 'purify' })
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}

if ($SpawnStatusRelics) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($OriginalSpawnCombatRelics) { 'spawn-status-relics-combat' } elseif ($OriginalChicotRelic) { 'spawn-status-relics-boss-mute' } elseif ($OriginalTunedGearRelic) { 'spawn-status-relics-tuned-gear' } elseif ($OriginalEggTransformation) { 'spawn-status-relics-egg-transformation' } elseif ($OriginalEggHatchRelic) { 'spawn-status-relics-egg-hatch' } elseif ($OriginalTeamSpawnStatusRelic) { 'spawn-status-relics-team-wide' } elseif ($OriginalStealthWaxersRelic) { 'spawn-status-relics-stealth-waxers' } elseif ($SpawnStatusRelicsClones) { 'spawn-status-relics-clones' } else { 'spawn-status-relics' })
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}
if ($OriginalEggCard -or $OriginalEggCardDelayed) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($OriginalEggCardDelayed) { 'original-egg-card-delayed' } else { 'original-egg-card' })
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}
if ($MixedCardlessRelicOrder) {
    $environment['MT2_PROBE_MODIFIERS'] = 'spawn-status-relics-mixed-cardless-order'
}
if ($RelicCardPiercingUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-piercing-upgrades' }
if ($RelicCardSelfPurgeUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-self-purge-upgrades' }
if ($RelicCardExhaustUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-exhaust-upgrades' }
if ($RelicCardEphemeralUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-ephemeral-upgrades' }
if ($RelicCardHealthUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-health-upgrades' }
if ($RelicCardAbilityUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-ability-upgrades' }
if ($RelicCardTriggerUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-trigger-upgrades' }
if ($RelicCardCastTriggerUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-cast-trigger-upgrades' }
if ($RelicCardCastTriggerUnitUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-cast-trigger-unit-upgrades' }
if ($RelicCardReplacementUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'relic-card-replacement-upgrades' }
if ($IncantRelics) { $IncantRelic = $true }
if ($IncantRelic) { $Incant = $true }
if ($Incant) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($IncantRelics) { 'incant-relic-combined' } elseif ($IncantRelic) { 'incant-relic' } elseif ($IncantThresholds) { 'incant-thresholds' } else { 'incant' })
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}
if ($AbilityIncant) {
    $environment['MT2_PROBE_MODIFIERS'] = 'ability-incant'
    $environment['MT2_PROBE_STATUS_CALLBACKS'] = '1'
}
if ($PersistentEnchantments) {
    $environment['MT2_PROBE_MODIFIERS'] = 'persistent-enchantment' + $(if ($PersistentEnchantmentRandomPools) { '-random' } else { '' }) +
        $(if ($PersistentEnchantmentRevivals) { '-revivals' } elseif ($PersistentEnchantmentDeaths) { '-deaths' } elseif ($PersistentEnchantmentUpgrades) { '-upgrades' } elseif ($PersistentEnchantmentSummons) { '-summons' + $(if ($PersistentEnchantmentSummonsFresh) { '-fresh' } else { '' }) } else { '' })
}
if ($Sentry -or $SentryLethal) {
    $environment['MT2_PROBE_MODIFIERS'] = $(if ($SentryLethal) { 'sentry-lethal' } else { 'sentry' })
    $environment['MT2_PROBE_ISOLATE_UI_RNG'] = '1'
}
if ($SacrificeEffects) {
    $environment['MT2_PROBE_MODIFIERS'] = 'sacrifice-effects'
}
if ($SetHealth) { $environment['MT2_PROBE_MODIFIERS'] = 'set-health' }
if ($DamageByUnitsKilled) { $environment['MT2_PROBE_MODIFIERS'] = 'damage-by-units-killed' }
if ($TransferAttack) { $environment['MT2_PROBE_MODIFIERS'] = 'transfer-attack' }
if ($BrainTransfer) { $environment['MT2_PROBE_MODIFIERS'] = 'brain-transfer' }
if ($FrostChanneling) { $environment['MT2_PROBE_MODIFIERS'] = 'frost-channeling' }
if ($Mix) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-mix' }
if ($Mix) { $environment['MT2_PROBE_MIX_CHOICES'] = if ($MixChoices) { $MixChoices } else { '00' } }
if ($MixUnstableExplosion) { $environment['MT2_PROBE_MIX_UNSTABLE_EXPLOSION'] = '1' }
if ($TransferAttackAbility) { $environment['MT2_PROBE_MODIFIERS'] = 'transfer-attack-ability' }
if ($CardEffectKill -or $CardEffectKillSubtype) {
    $environment['MT2_PROBE_MODIFIERS'] = if ($CardEffectKillSubtype) { 'card-effect-kill-subtype' } else { 'card-effect-kill' }
}
if ($NullForwarding) { $environment['MT2_PROBE_MODIFIERS'] = 'null-forwarding' }
if ($NullTriggers) { $environment['MT2_PROBE_MODIFIERS'] = 'null-triggers' }
if ($NullTargetless) { $environment['MT2_PROBE_MODIFIERS'] = 'null-targetless' }
if ($OriginalTrample) { $environment['MT2_PROBE_MODIFIERS'] = 'original-trample' }
if ($OriginalSpellShield) { $environment['MT2_PROBE_MODIFIERS'] = 'original-spell-shield' }
if ($ChooseDiscard) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard' }
if ($ChooseDiscardOthers) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard-others' }
if ($ChooseDiscardOthersPolicy) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard-others-policy' }
if ($ChooseDiscardRefine) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard-refine' }
if ($ChooseDiscardPersistent) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard-persistent' }
if ($RemoveAllStatusEffects) { $environment['MT2_PROBE_MODIFIERS'] = 'remove-all-status-effects' }
if ($DragonHoard) { $environment['MT2_PROBE_MODIFIERS'] = 'dragon-hoard' }
if ($TrainHeal) { $environment['MT2_PROBE_MODIFIERS'] = 'train-heal' }
if ($Overheal) { $environment['MT2_PROBE_MODIFIERS'] = 'overheal' }
if ($Corruption) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption' }
if ($CorruptionUnitTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption-unit-trigger' }
if ($CorruptionCarverTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption-carver-trigger' }
if ($CorruptionKeeperTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption-keeper-trigger' }
if ($CorruptionRelicTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption-relic-trigger' }
if ($PermanentCorruption) { $environment['MT2_PROBE_MODIFIERS'] = 'permanent-corruption' }
if ($MoonPhaseCard) { $environment['MT2_PROBE_MODIFIERS'] = 'moon-phase-card' }
if ($MoonPhaseAdvance) { $environment['MT2_PROBE_MODIFIERS'] = 'moon-phase-advance' }
if ($MoonPhaseCallbacks) { $environment['MT2_PROBE_MODIFIERS'] = 'moon-phase-callbacks' }
if ($ConditionalNoPlayerStatus) { $environment['MT2_PROBE_MODIFIERS'] = 'conditional-no-player-status' }
if ($SelfAttackStatus) { $environment['MT2_PROBE_MODIFIERS'] = 'self-attack-status' }
if ($ConditionalPyreDamage) { $environment['MT2_PROBE_MODIFIERS'] = 'conditional-pyre-damage' }
if ($SoulSiphon) { $environment['MT2_PROBE_MODIFIERS'] = 'soul-siphon' }
if ($StatusUpgrade) { $environment['MT2_PROBE_MODIFIERS'] = 'status-upgrade' }
if ($CardRecursion -or $CardRecursionDiscard -or $CardRecursionChoose -or $CardRecursionChooseToHand -or $CardRecursionChooseFilter -or $CardRecursionUpgrade -or $CardRecursionUpgradeToHand -or $CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose) {
    $environment['MT2_PROBE_MODIFIERS'] = if ($CardRecursionDiscard) { 'card-effect-recursion-discard-choice' } elseif ($CardRecursionUpgradeReplacementChoose) { 'card-effect-recursion-upgrade-replacement-choice' } elseif ($CardRecursionUpgradeFilterChoose) { 'card-effect-recursion-upgrade-filter-choice' } elseif ($CardRecursionUpgradeChoose) { 'card-effect-recursion-upgrade-choice' } elseif ($CardRecursionUpgradeToHand) { 'card-effect-recursion-upgrade-hand' } elseif ($CardRecursionUpgrade) { 'card-effect-recursion-upgrade' } elseif ($CardRecursionChooseFilter) { 'card-effect-recursion-filter-choice' } elseif ($CardRecursionChooseToHand) { 'card-effect-recursion-choose-to-hand' } elseif ($CardRecursionChoose) { 'card-effect-recursion-choice' } else { 'card-effect-recursion' }
}
if ($OriginalRelicBatch) {
    $environment['MT2_PROBE_MODIFIERS'] = 'original-relic-batch'
}
if ($OriginalDeployableRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-deployable-relic' }
if ($AllUnitsMultistrikeMutator) { $environment['MT2_PROBE_MODIFIERS'] = 'all-units-multistrike-mutator' }
if ($OriginalStingRelicBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-sting-relics' }
if ($OriginalUnitRelicBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-unit-relic-batch' }
if ($OriginalEndlessReturn) { $environment['MT2_PROBE_MODIFIERS'] = 'original-endless-return' }
if ($OriginalTriggerRelicBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-trigger-relic-batch' }
if ($CovenantCharacterFilter) { $environment['MT2_PROBE_MODIFIERS'] = 'covenant-character-filter' }
if ($TitanSkinCombat) { $environment['MT2_PROBE_MODIFIERS'] = 'titan-skin-combat' }
if ($OriginalStatusRelicBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-status-relic-batch' }
if ($OriginalStatusRelicBatchBurnout) { $environment['MT2_PROBE_MODIFIERS'] = 'original-status-relic-batch-burnout' }
if ($OriginalMagnitudeRelicBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-magnitude-relic-batch' }
if ($OriginalLifecycleRelicBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-lifecycle-relic-batch' }
if ($OriginalDecayRelicBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-decay-relic-batch' }
if ($OriginalResourceRelicBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-resource-relic-batch' }
if ($PyreResourceRefresh) { $environment['MT2_PROBE_MODIFIERS'] = 'pyre-resource-refresh' }
if ($OriginalFloorCapacityRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-floor-capacity-relics' }
if ($Mageblade) { $environment['MT2_PROBE_MODIFIERS'] = 'mageblade' }
if ($OriginalMagebladeMagnitudeRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-mageblade-magnitude' }
if ($OriginalCardPlayMagicPower) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-play-magic-power' }
if ($OriginalCardDrawOnCardPlay) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-draw-on-card-play' }
if ($UnplayedCardTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'unplayed-card-trigger' }
if ($UnplayedCardEffects) { $environment['MT2_PROBE_MODIFIERS'] = 'unplayed-card-effects' }
if ($OriginalRoomEntryStatusRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-room-entry-status-relics' }
if ($OriginalMoonPhaseEnergyRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-moon-phase-energy-relic' }
if ($OriginalCardAttackRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-attack-relic' }
if ($OriginalUnitUpgradeTriggerRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-unit-upgrade-trigger-relic' }
if ($TypedDraw) {
    $environment['MT2_PROBE_MODIFIERS'] = 'typed-draw'
    $environment['MT2_PROBE_ISOLATE_UI_RNG'] = '1'
}
if ($PermanentConsumeHandUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'permanent-consume-hand-upgrades' }
if ($PermanentConsumeOverTemporary) { $environment['MT2_PROBE_MODIFIERS'] = 'permanent-consume-over-temporary' }
if ($GenerationPermanentConsume) { $environment['MT2_PROBE_MODIFIERS'] = 'generation-permanent-consume' }
$originalBefore = Get-OriginalSignature
$unityLog = Join-Path $profile 'unity-scenario.log'
$nativeTimer = [System.Diagnostics.Stopwatch]::StartNew()
if ($SpawnOrStatus) {
    $environment.MT2_PROBE_MODIFIERS = 'spawn-or-status'
    $environment.MT2_PROBE_PHYSICAL_SPAWNPOINTS = '1'
}
if ($DamagePerHp) { $environment.MT2_PROBE_MODIFIERS = 'damage-per-hp' }
if ($DamagePerHpArmor) { $environment.MT2_PROBE_MODIFIERS = 'damage-per-hp-armor' }
if ($DamagePerHpLastAttacker) { $environment.MT2_PROBE_MODIFIERS = 'damage-per-hp-last-attacker' }
if ($DamagePerHpAbility) { $environment.MT2_PROBE_MODIFIERS = 'damage-per-hp-ability' }
if ($DamageOverflow) { $environment.MT2_PROBE_MODIFIERS = 'damage-overflow' }
if ($RandomDiscard) {
    $environment.MT2_PROBE_MODIFIERS = 'random-discard'
    $environment.MT2_PROBE_BRANCH_CARD_MASKS = '1'
}
if ($RoomCapacityAttachment) { $environment['MT2_PROBE_MODIFIERS'] = 'room-capacity-attachment' }
$process = Start-Process -FilePath (Join-Path $gameRoot 'MonsterTrain2.exe') `
    -WorkingDirectory $gameRoot -ArgumentList @('-logFile', $unityLog) `
    -Environment $environment -WindowStyle Hidden -PassThru
Write-Output "FULL-BATTLE-RUN pid=$($process.Id) policy=$Policy profile=$profile"
if (-not $process.WaitForExit(600000)) {
    $process.Kill()
    $process.WaitForExit()
    throw "Full battle timed out; profile: $profile"
}
$nativeTimer.Stop()
Write-Output "FULL-BATTLE-TIMING speed=$GameSpeed elapsedSeconds=$($nativeTimer.Elapsed.TotalSeconds.ToString('F2', [Globalization.CultureInfo]::InvariantCulture))"
$tracePath = Join-Path $profile $(if ($BinaryCapture) { 'full-battle.mt2f' } else { 'full-battle.json' })
if (-not (Test-Path -LiteralPath $tracePath)) { throw "Missing battle trace (native exit code $($process.ExitCode)); inspect $unityLog" }
$inspectionTimer = [System.Diagnostics.Stopwatch]::StartNew()
if ($BinaryCapture) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $archive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($tracePath)
    $trace = $archive.RootElement.ToObjectGraph()
    $archive.Dispose()
} else {
    $trace = Get-Content -LiteralPath $tracePath -Raw | ConvertFrom-Json
}
$inspectionTimer.Stop()
Write-Output "FULL-BATTLE-INSPECTION elapsedSeconds=$($inspectionTimer.Elapsed.TotalSeconds.ToString('F3', [Globalization.CultureInfo]::InvariantCulture))"
if ($TitanSkinCombat -and ($trace.ModifierScenario -ne 'titan-skin-combat' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'TITAN-SKIN-COMBAT-PREPARED .*starting titanskin stacks=2' -Quiet))) {
    throw 'Titan Skin native combat setup did not run.'
}
if ($AllUnitsMultistrikeMutator -and ($trace.ModifierScenario -ne 'all-units-multistrike-mutator' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ALL-UNITS-MULTISTRIKE-MUTATOR-PREPARED original AllUnitsMultistrike' -Quiet))) {
    throw 'AllUnitsMultistrike native mutator setup did not run.'
}
$nativePassed = [bool] (Select-String -LiteralPath $unityLog -Pattern 'DEPTH-PASS' -Quiet)
if ($damagePerHpModes.Count -gt 0 -and
    ($trace.ModifierScenario -ne $damagePerHpModes[0] -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'DAMAGE-PER-HP-PREPARED' -Quiet))) {
    throw 'DamagePerHp native setup did not run.'
}
if ($DamagePerHpAbility -and ($trace.ModifierScenario -ne 'damage-per-hp-ability' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ABILITY-ACTIVATION-PREPARED .*damagePerHp=True' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'DAMAGE-PER-HP-ABILITY-INPUT actor=' -Quiet))) {
    throw 'DamagePerHp unit-ability native setup did not run.'
}
if ($DamageOverflow -and ($trace.ModifierScenario -ne 'damage-overflow' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'DAMAGE-OVERFLOW-PREPARED friendly-team damage=20' -Quiet))) {
    throw 'Damage overflow native setup did not run.'
}
$terminalSettled = $trace.TerminalCaptureBoundary -eq 'AfterStopCombatLoop' -and $trace.TerminalEffectsSettled -eq $true
$originalUnchanged = (Get-OriginalSignature) -ceq $originalBefore
if ($EquipmentBuff -and ($trace.ModifierScenario -ne 'equipment-buff' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'EQUIPMENT-PREPARED .*buff=True' -Quiet))) {
    throw 'Equipment buff scenario setup did not run.'
}
if (($CardEffectKill -or $CardEffectKillSubtype) -and ($trace.ModifierScenario -ne $(if ($CardEffectKillSubtype) { 'card-effect-kill-subtype' } else { 'card-effect-kill' }) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CARD-EFFECT-KILL-PREPARED native KillRandomUnit' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CARD-EFFECT-KILL-SUBTYPE-NATIVE .*result=true/false' -Quiet) -or
    ($CardEffectKillSubtype -and -not (Select-String -LiteralPath $unityLog -Pattern 'CARD-EFFECT-KILL-SUBTYPE-PLAY targets=.* result=supported' -Quiet)))) {
    throw 'CardEffectKill native setup did not run.'
}
if ($NullForwarding -and ($trace.ModifierScenario -ne 'null-forwarding' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'NULL-FORWARDING-PREPARED original=Cleave' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'NULL-FORWARDING-PLAY room=' -Quiet) -or
    -not (Select-String -LiteralPath (Join-Path $profile 'logfile.log') -Pattern 'Attacker Cleave dealing [1-9][0-9]* damage' -Quiet))) {
    throw 'Original Cleave did not deal damage in the native battle.'
}
if ($NullTriggers -and ($trace.ModifierScenario -ne 'null-triggers' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'NULL-TRIGGERS-PREPARED originals=' -Quiet) -or
    @(@('LunaCovenChamp1', 'LunaCovenChamp2') | Where-Object {
        -not (Select-String -LiteralPath $unityLog -Pattern "NULL-TRIGGERS-PLAY original=$_" -Quiet)
    }).Count -ne 0 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'NULL-TRIGGER-TEST actor=MonsterChampionLunaCoven1 targets=1 result=True' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'NULL-TRIGGER-TEST actor=MonsterChampionLunaCoven2 targets=0 result=False' -Quiet))) {
    throw 'Original null-trigger target tests did not run in the native battle.'
}
if ($NullTargetless -and ($trace.ModifierScenario -ne 'null-targetless' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'NULL-TARGETLESS-PREPARED originals=ScourgeT1,SelfPurgeBlight' -Quiet) -or
    @(@('ScourgeT1', 'SelfPurgeBlight') | Where-Object {
        -not (Select-String -LiteralPath $unityLog -Pattern "NULL-TARGETLESS-PLAY original=$_ room=0" -Quiet) -or
        -not (Select-String -LiteralPath $unityLog -Pattern "NULL-TARGETLESS-TEST original=$_ targets=0 result=True" -Quiet)
    }).Count -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2)) {
    throw 'Original targetless null effects did not resolve in the native battle.'
}
if ($OriginalTrample -and ($trace.ModifierScenario -ne 'original-trample' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'TRAMPLE-PREPARED original=Basher unit=MonsterBasher stacks=1' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'TRAMPLE-PLAY original=Basher room=0' -Quiet) -or
    -not (Select-String -LiteralPath (Join-Path $profile 'logfile.log') -Pattern 'Attacker .* dealing [1-9][0-9]* damage .* with damage type Trample' -Quiet))) {
    throw 'Original Basher did not deliver Trample damage in the native battle.'
}
if ($OriginalSpellShield -and ($trace.ModifierScenario -ne 'original-spell-shield' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'SPELL-SHIELD-PREPARED original=HordeT3_PureSouls stacks=2' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'SPELL-SHIELD-PLAY target=.* stacks=2 spell=' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'SPELL-SHIELD-PLAY target=.* stacks=1 spell=' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2)) {
    throw 'Original Pure Souls did not consume both Spell Shield stacks in the native battle.'
}
if ($ChooseDiscard -and ($trace.ModifierScenario -ne 'card-effect-choose-discard' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CHOOSE-DISCARD-AUTOSELECT card=\d+ cost=2' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 1)) {
    throw 'Original StarterScrap did not consume the selected cost-2 card in the native battle.'
}
if ($ChooseDiscardOthers -and ($trace.ModifierScenario -ne 'card-effect-choose-discard-others' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CHOOSE-DISCARD-AUTOSELECT card=\d+ cost=2' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 1)) {
    throw 'Original FoundryLine did not preserve the selected cost-2 card in the native battle.'
}
if ($ChooseDiscardOthersPolicy -and ($trace.ModifierScenario -ne 'card-effect-choose-discard-others-policy' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CHOOSE-DISCARD-AUTOSELECT card=\d+ cost=2' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 1 -or $trace.Actions.Count -le 1)) {
    throw 'Original FoundryLine did not continue into the high-action native policy battle.'
}
if ($ChooseDiscardRefine -and ($trace.ModifierScenario -ne 'card-effect-choose-discard-refine' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CHOOSE-DISCARD-REFINE card=\d+ bonus=3' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CHOOSE-DISCARD-AUTOSELECT card=\d+ cost=2' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 1)) {
    throw 'Original FoundryLine did not consume the Refine-3 ScrapMetal card in the native battle.'
}
if ($ChooseDiscardPersistent -and ($trace.ModifierScenario -ne 'card-effect-choose-discard-persistent' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CHOOSE-DISCARD-REFINE card=\d+ bonus=3' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CHOOSE-DISCARD-AUTOSELECT card=\d+ cost=0' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 1)) {
    throw 'Original FoundryLine did not keep the Persistent ScrapMetal card in the native battle.'
}
if ($RemoveAllStatusEffects -and ($trace.Schema -ne 164 -or $trace.ModifierScenario -ne 'remove-all-status-effects' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'REMOVE-ALL-STATUS-SEEDED card=.* hidden=' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'REMOVE-ALL-STATUS-FULL-PLAY supported=True' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'REMOVE-ALL-STATUS-HALF-PLAY supported=True' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 2)) {
    throw 'Native remove-all status effects did not exercise both full and half removal.'
}
if ($DragonHoard -and ($trace.Schema -ne 162 -or $trace.ModifierScenario -ne 'dragon-hoard' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'DRAGON-HOARD-PREPARED native Alchemy start=2 cap=' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'DRAGON-HOARD-CANDIDATE supported=True before=2 after=4' -Quiet) -or
    @($trace.Actions).Count -eq 0)) {
    throw "Dragon's Hoard native setup did not complete a supported Alchemy play."
}
if ($TrainHeal -and ($trace.ModifierScenario -ne 'train-heal' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'TRAIN-HEAL-PREPARED PayOff card=.* hoard=2' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'TRAIN-HEAL-CANDIDATE supported=True before=\d+ after=\d+' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 1)) {
    throw 'Native PayOff train-heal action was not captured.'
}
if ($Overheal -and ($trace.ModifierScenario -ne 'overheal' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'OVERHEAL-PREPARED steward=\d+ tidal=\d+ fae=\d+' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 2)) {
    throw 'Original room and targeted overheal actions were not captured.'
}
if ($Corruption -and ($trace.ModifierScenario -ne 'corruption' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CORRUPTION-PREPARED steward=\d+ unearthed=\d+ echoes=\d+ room=0 current=\d+ max=\d+' -Quiet) -or
    @(Select-String -LiteralPath $unityLog -Pattern 'CORRUPTION-CANDIDATE spell=\d+ supported=True current=\d+->\d+ max=\d+->\d+').Count -ne 2 -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 2)) {
    throw 'Original Unearthed Remains and Echoes of the Past actions were not captured.'
}
if ($CorruptionUnitTrigger) {
    $healedActions = @($trace.Actions | Where-Object {
        $entry = $_
        $before = $entry.Before.Spawn.Train
        $after = $entry.Actual.Spawn.Train
        $card = @($before.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId)
        $oldUnit = @($before.Rooms.Units | Where-Object AssetKey -EQ 'MonsterGlugsider')
        $newUnit = @($after.Rooms.Units | Where-Object Id -EQ $oldUnit[0].Id)
        $oldRoom = @($before.Context.RoomCapacities | Where-Object RoomIndex -EQ 0)
        $newRoom = @($after.Context.RoomCapacities | Where-Object RoomIndex -EQ 0)
        $card.Count -eq 1 -and $card[0].DataId -eq '17fb5276-2311-44c9-b662-b73581d9af56' -and
            $oldUnit.Count -eq 1 -and $newUnit.Count -eq 1 -and
            $oldUnit[0].Health -gt 0 -and $oldUnit[0].Health -lt $oldUnit[0].MaxHealth -and
            $newUnit[0].Health -eq [Math]::Min($oldUnit[0].MaxHealth, $oldUnit[0].Health + 10) -and
            $oldRoom.Count -eq 1 -and $newRoom.Count -eq 1 -and
            $oldRoom[0].Corruption -eq 0 -and $newRoom[0].Corruption -eq 1 -and
            $newRoom[0].MaxCorruption -eq ($oldRoom[0].MaxCorruption + 1)
    })
    if ($trace.ModifierScenario -ne 'corruption-unit-trigger' -or $healedActions.Count -ne 1 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'CORRUPTION-UNIT-DEFINITION cost=1 hp=100 heal=10' -Quiet)) {
        throw 'Original wounded Glugsider did not heal on the Unearthed Remains corruption addition.'
    }
    Write-Output 'NATIVE-CORRUPTION-UNIT-TRIGGER PASS: wounded original Glugsider healed on corruption addition.'
}
if ($CorruptionCarverTrigger) {
    $carverActions = @($trace.Actions | Where-Object { $_.ScenarioAction })
    $upgradedActions = @($carverActions | Where-Object {
        $entry = $_
        $before = $entry.Before.Spawn.Train
        $after = $entry.Actual.Spawn.Train
        $card = @($before.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId)
        $oldUnit = @($before.Rooms.Units | Where-Object AssetKey -EQ 'MonsterShardsoulCarver')
        if ($card.Count -ne 1 -or $oldUnit.Count -ne 1) { return $false }
        $newUnit = @($after.Rooms.Units | Where-Object Id -EQ $oldUnit[0].Id)
        if ($newUnit.Count -ne 1) { return $false }
        $oldArmor = @($oldUnit[0].Statuses | Where-Object Id -EQ 'armor')
        $newArmor = @($newUnit[0].Statuses | Where-Object Id -EQ 'armor')
        $oldStacks = if ($oldArmor.Count -eq 0) { 0 } else { $oldArmor[0].Stacks }
        $added = @($newUnit[0].Modifiers.Upgrades | Where-Object { $_.DataId -notin @($oldUnit[0].Modifiers.Upgrades.DataId) })
        $card[0].DataId -eq '17fb5276-2311-44c9-b662-b73581d9af56' -and
            $newUnit[0].BaseAttack -eq ($oldUnit[0].BaseAttack + 3) -and
            $newArmor.Count -eq 1 -and $newArmor[0].Stacks -eq ($oldStacks + 3) -and
            $added.Count -eq 1 -and $added[0].Stats.Damage -eq 3
    })
    if ($trace.ModifierScenario -ne 'corruption-carver-trigger' -or
        $carverActions.Count -ne 2 -or $upgradedActions.Count -ne 1 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'CORRUPTION-CARVER-DEFINITION cost=1 attack=20 hp=30 upgrade=Plus3Attack armor=3' -Quiet)) {
        throw 'Original Shardsoul Carver did not receive its corruption-triggered attack upgrade and Armor.'
    }
    Write-Output 'NATIVE-CORRUPTION-CARVER-TRIGGER PASS: original Carver gained temporary +3 attack and +3 Armor.'
}
if ($CorruptionKeeperTrigger) {
    $keeperActions = @($trace.Actions | Where-Object { $_.ScenarioAction })
    $roomUpgrade = @($keeperActions | Where-Object {
        $entry = $_
        $before = $entry.Before.Spawn.Train
        $after = $entry.Actual.Spawn.Train
        $card = @($before.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId)
        $keeper = @($before.Rooms.Units | Where-Object AssetKey -EQ 'MonsterKeeperOfEchoes')
        $teammate = @($before.Rooms.Units | Where-Object { $_.AssetKey -like 'TrainSteward*' })
        if ($card.Count -ne 1 -or $keeper.Count -ne 1 -or $teammate.Count -ne 1) { return $false }
        $trigger = @($keeper[0].Triggers | Where-Object Kind -EQ 'CorruptionAdded')
        if ($trigger.Count -ne 1) { return $false }
        $upgradeId = $trigger[0].Effects[0].UnitUpgrade.Upgrade.DataId
        if (-not $upgradeId) { return $false }
        $oldUnits = @($keeper[0], $teammate[0])
        $newUnits = @($oldUnits | ForEach-Object {
            $id = $_.Id
            $after.Rooms.Units | Where-Object Id -EQ $id
        })
        $card[0].DataId -eq '17fb5276-2311-44c9-b662-b73581d9af56' -and
            $newUnits.Count -eq 2 -and
            @($oldUnits | Where-Object { @($_.Modifiers.Upgrades | Where-Object DataId -EQ $upgradeId).Count -ne 0 }).Count -eq 0 -and
            @($newUnits | Where-Object { @($_.Modifiers.Upgrades | Where-Object DataId -EQ $upgradeId).Count -ne 1 }).Count -eq 0
    })
    if ($trace.ModifierScenario -ne 'corruption-keeper-trigger' -or
        $keeperActions.Count -ne 2 -or $roomUpgrade.Count -ne 1 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'CORRUPTION-KEEPER-DEFINITION cost=2 attack=5 hp=20 upgrade=WurmTransfuserTrigger' -Quiet)) {
        throw 'Original Keeper of Echoes did not upgrade both same-room units on corruption addition.'
    }
    Write-Output 'NATIVE-CORRUPTION-KEEPER-TRIGGER PASS: original Keeper upgraded itself and Train Steward.'
}
if ($CorruptionRelicTrigger) {
    $relicActions = @($trace.Actions | Where-Object ScenarioAction)
    $poisonActions = @($relicActions | Where-Object {
        $entry = $_
        $before = $entry.Before.Spawn.Train
        $after = $entry.Actual.Spawn.Train
        $card = @($before.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId)
        $oldRoom = @($before.Rooms | Where-Object RoomIndex -EQ 0)
        $newRoom = @($after.Rooms | Where-Object RoomIndex -EQ 0)
        if ($card.Count -ne 1 -or $oldRoom.Count -ne 1 -or $newRoom.Count -ne 1 -or
            $card[0].DataId -ne '17fb5276-2311-44c9-b662-b73581d9af56') { return $false }
        $oldEnemies = @($oldRoom[0].Units | Where-Object Team -EQ 0)
        $newEnemies = @($newRoom[0].Units | Where-Object Team -EQ 0)
        $oldPoison = 0
        $newPoison = 0
        foreach ($unit in $oldEnemies) {
            foreach ($status in $unit.Statuses) { if ($status.Id -eq 'corrupt_poison') { $oldPoison += $status.Stacks } }
        }
        foreach ($unit in $newEnemies) {
            foreach ($status in $unit.Statuses) { if ($status.Id -eq 'corrupt_poison') { $newPoison += $status.Stacks } }
        }
        $oldEnemies.Count -ge 2 -and $newEnemies.Count -eq $oldEnemies.Count -and
            $newPoison -eq ($oldPoison + 2) -and
            @($before.Context.Relics | Where-Object DataId -EQ 'f302d4a1-13ba-4907-a26d-f0d8eead6f2e').Count -eq 1 -and
            @($before.Context.RoomCapacities | Where-Object RoomIndex -EQ 0)[0].Corruption -eq 0 -and
            @($after.Context.RoomCapacities | Where-Object RoomIndex -EQ 0)[0].Corruption -eq 1
    })
    if ($trace.ModifierScenario -ne 'corruption-relic-trigger' -or $relicActions.Count -ne 2 -or
        $poisonActions.Count -ne 1 -or -not (Select-String -LiteralPath $unityLog -Pattern 'CORRUPTION-RELIC-ACQUIRED unchanged DamageOnCorruptPlay' -Quiet)) {
        throw 'Original DamageOnCorruptPlay did not add two total corrupt_poison stacks to room enemies.'
    }
    Write-Output 'NATIVE-CORRUPTION-RELIC-TRIGGER PASS: original relic added two corrupt_poison stacks to room enemies.'
}
if ($PermanentCorruption -and ($trace.ModifierScenario -ne 'permanent-corruption' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'PERMANENT-CORRUPTION-PREPARED card=\d+ room=0 current=\d+ max=\d+ permanent=\d+' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'PERMANENT-CORRUPTION-CANDIDATE supported=True current=\d+->\d+ permanent=\d+->\d+' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 1)) {
    throw 'Original Eternal Kinstone action was not captured.'
}
if ($MoonPhaseCard -and ($trace.ModifierScenario -ne 'moon-phase-card' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'MOON-PHASE-CARD-PREPARED steward=\d+ moonrise=\d+ phase=New tracking=True' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'MOON-PHASE-CARD-CANDIDATE supported=True phase=New->Full energy=\d+->\d+ fullmoon=1' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 1)) {
    throw 'Original Moonrise phase-shift action was not captured.'
}
if ($MoonPhaseAdvance -and ($trace.ModifierScenario -ne 'moon-phase-advance' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'MOON-PHASE-ADVANCE-PREPARED steward=\d+ moonrise=\d+ fae=\d+ phase=New tracking=True' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'MOON-PHASE-ADVANCE-CANDIDATE supported=True phase=Full->New newmoon=1' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 1)) {
    throw 'Original Fae Meddler moon-phase advance was not captured.'
}
if ($MoonPhaseCallbacks -and ($trace.ModifierScenario -ne 'moon-phase-callbacks' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'MOON-PHASE-CALLBACKS-PREPARED acolyte=\d+ moonrise=\d+ fae=\d+ phase=New tracking=True' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'MOON-PHASE-CALLBACKS-CANDIDATE supported=True phase=Full->New' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 1)) {
    throw 'Original Selene Acolyte moon-phase callbacks were not captured.'
}
if ($ConditionalNoPlayerStatus -and ($trace.ModifierScenario -ne 'conditional-no-player-status' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'NO-PLAYER-STATUS-PREPARED original=R4_Heavy_ArmorIfUnblocked_Ver2 firstWaveGroups=\d+' -Quiet))) {
    throw 'Original conditional Armor enemy was not prepared.'
}
if ($SelfAttackStatus -and ($trace.ModifierScenario -ne 'self-attack-status' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'SELF-ATTACK-STATUS-PREPARED original=MonsterWeldling firstWaveGroups=\d+' -Quiet))) {
    throw 'Original Weldling status enemy was not prepared.'
}
if ($ConditionalPyreDamage -and ($trace.ModifierScenario -ne 'conditional-pyre-damage' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CONDITIONAL-PYRE-DAMAGE-PREPARED original=HeavyT3_PyreDamageIfUnblocked firstWaveGroups=\d+' -Quiet))) {
    throw 'Original conditional Pyre damage enemy was not prepared.'
}
if ($SoulSiphon -and ($trace.ModifierScenario -ne 'soul-siphon' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'SOUL-SIPHON-STATUS stage=' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'SOUL-SIPHON-CANDIDATE supported=True current=\d+->\d+ scorch=1' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 1)) {
    throw 'Original Soul Siphon action or Scorch status was not captured.'
}
if ($StatusUpgrade -and ($trace.ModifierScenario -ne 'status-upgrade' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'STATUS-UPGRADE-PREPARED originalAbility=.* blessing=2' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'STATUS-UPGRADE-ABILITY supported=True' -Quiet) -or
    @($trace.Actions | Where-Object { $_.ScenarioAction }).Count -ne 1)) {
    throw 'Original Solgard status-upgrade ability was not captured.'
}
if (($CardRecursion -or $CardRecursionDiscard -or $CardRecursionChoose -or $CardRecursionChooseToHand -or $CardRecursionChooseFilter -or $CardRecursionUpgrade -or $CardRecursionUpgradeToHand -or $CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose) -and ($trace.ModifierScenario -ne $(if ($CardRecursionDiscard) { 'card-effect-recursion-discard-choice' } elseif ($CardRecursionUpgradeReplacementChoose) { 'card-effect-recursion-upgrade-replacement-choice' } elseif ($CardRecursionUpgradeFilterChoose) { 'card-effect-recursion-upgrade-filter-choice' } elseif ($CardRecursionUpgradeChoose) { 'card-effect-recursion-upgrade-choice' } elseif ($CardRecursionUpgradeToHand) { 'card-effect-recursion-upgrade-hand' } elseif ($CardRecursionUpgrade) { 'card-effect-recursion-upgrade' } elseif ($CardRecursionChooseFilter) { 'card-effect-recursion-filter-choice' } elseif ($CardRecursionChooseToHand) { 'card-effect-recursion-choose-to-hand' } elseif ($CardRecursionChoose) { 'card-effect-recursion-choice' } else { 'card-effect-recursion' }) -or
    -not (Select-String -LiteralPath $unityLog -Pattern $(if ($CardRecursionDiscard) { 'CARD-RECURSION-PREPARED WurmkinEtchings discardSpellCopies=6 .*selection=ChooseToHand from Discard' } elseif ($CardRecursionUpgradeReplacementChoose) { 'CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=6 .*temporaryUpgrade=\+3Damage replaces=PojuRecursionPriorUpgrade' } elseif ($CardRecursionUpgradeFilterChoose) { 'CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=5 excludedCandidates=1 totalCandidates=6 .*temporaryUpgrade=\+3Damage' } elseif ($CardRecursionChooseFilter) { 'CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=5 excludedCandidates=1 totalCandidates=6' } elseif ($CardRecursionUpgradeToHand) { 'CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=6 .*temporaryUpgrade=\+3Damage requiresRoomInHand=True disallowOverflowToDrawPile=True hand=\d+/\d+ handFillers=\d+' } elseif ($CardRecursionUpgrade -or $CardRecursionUpgradeChoose) { 'CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=6 .*temporaryUpgrade=\+3Damage' } else { 'CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=6' }) -Quiet))) {
    throw 'Card recursion native setup did not run.'
}
if (($CardRecursionDiscard -or $CardRecursionChoose -or $CardRecursionChooseToHand -or $CardRecursionChooseFilter -or $CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose) -and -not (Select-String -LiteralPath $unityLog -Pattern 'CARD-RECURSION-CHOOSE-AUTOSELECT card=' -Quiet)) {
    throw 'Card recursion did not complete its native chosen-card interaction.'
}
if ($SpawnOrStatus -and ($trace.ModifierScenario -ne 'spawn-or-status' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ABILITY-ACTIVATION-PREPARED .*spawnOrStatus=True' -Quiet) -or
    @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 }).Count -eq 0)) {
    throw 'Spawn-or-status native setup or ability activation did not run.'
}
if ($Mageblade) {
    $magebladeProof = @(
        foreach ($stage in $trace.Stages) {
            if ($null -eq $stage.Actual -or $null -eq $stage.Actual.Context) { continue }
            foreach ($unit in $stage.Actual.Units) {
                $magebladeStatuses = @($unit.Statuses | Where-Object { $_.Id -eq 'mageblade' -and $_.Stacks -gt 0 })
                $power = @($stage.Actual.Context.MagicPower | Where-Object {
                    $_.RoomIndex -eq $stage.Actual.RoomIndex -and $_.Team -eq 1 -and $_.Value -gt 0
                })
                if ($magebladeStatuses.Count -gt 0 -and $unit.Modifiers.MagebladeDamagePerMagicPower -gt 0 -and $power.Count -gt 0) {
                    [pscustomobject]@{ Room = $stage.Actual.RoomIndex; Stacks = $magebladeStatuses[0].Stacks; Power = $power[0].Value; Cache = $unit.Modifiers.MagebladeDamagePerMagicPower }
                }
            }
        }
    )
    if ($trace.Schema -ne 127 -or $trace.ModifierScenario -ne 'mageblade' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'MAGEBLADE-PREPARED' -Quiet) -or $magebladeProof.Count -eq 0) {
        throw 'Mageblade native battle did not capture a cached Mageblade unit in a positive-magic-power room.'
    }
    Write-Output "MAGEBLADE-NATIVE-CAPTURE PASS: $($magebladeProof.Count) stages with stacks=$($magebladeProof[0].Stacks), roomPower=$($magebladeProof[0].Power), cachedCoefficient=$($magebladeProof[0].Cache)."
}
if ($OriginalCardPlayMagicPower -and ($trace.Schema -ne 128 -or
    $trace.ModifierScenario -ne 'original-card-play-magic-power' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARD-PLAY-MAGIC-POWER-ACQUIRED unchanged relic=' -Quiet) -or
    @($trace.Actions).Count -eq 0)) {
    throw 'Original card-play magic-power relic was not exercised in the expected full-battle capture.'
}
if ($OriginalCardDrawOnCardPlay -and ($trace.Schema -ne 160 -or
    $trace.ModifierScenario -ne 'original-card-draw-on-card-play' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARD-DRAW-RELICS-ACQUIRED unchanged relics=' -Quiet) -or
    @($trace.Actions).Count -eq 0)) {
    throw 'Original card-draw-on-play relic was not exercised in the expected full-battle capture.'
}
if ($UnplayedCardTrigger) {
    $triggerCardIds = @(
        '186180e9-94e9-47d0-b69c-865b9a6d27a4',
        '2785cc8e-d0e8-46c0-84ab-9c1d602525d4',
        '3b603b7f-300c-4022-9fc2-275448446e58',
        '913694e9-bace-4480-8691-24f699ae583e',
        'fbf74ec4-2781-4b99-b6f7-b7d75236da60'
    )
    $triggerTurns = @($trace.Turns | Where-Object {
        $handCardIds = @($_.Before.Spawn.Train.Context.Cards.Hand | ForEach-Object { $_.DataId })
        @($triggerCardIds | Where-Object { $_ -notin $handCardIds }).Count -eq 0
    })
    $matchedTriggerTurns = @($triggerTurns | Where-Object { $_.Predicted.Supported -and $null -eq $_.Difference })
    if ($trace.Schema -ne 161 -or $trace.ModifierScenario -ne 'unplayed-card-trigger' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'UNPLAYED-CARD-TRIGGER-PREPARED originals=5 totalDamage=15' -Quiet) -or
        $matchedTriggerTurns.Count -eq 0 -or $matchedTriggerTurns[0].Before.Spawn.Turn -ne 0 -or
        $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or -not $trace.NativeWon) {
        throw 'The five original Pyre-damage trigger cards did not match the modeled EndTurn in a complete battle.'
    }
}
if ($UnplayedCardEffects) {
    $effectCardIds = @(
        '17fef621-2bb7-4847-b41d-52a89795c784',
        '4d1f27af-3122-4a08-87e3-5ef5a14ced94'
    )
    $effectTurns = @($trace.Turns | Where-Object {
        $handCardIds = @($_.Before.Spawn.Train.Context.Cards.Hand | ForEach-Object { $_.DataId })
        @($effectCardIds | Where-Object { $_ -notin $handCardIds }).Count -eq 0
    })
    $matchedEffectTurns = @($effectTurns | Where-Object { $_.Predicted.Supported -and $null -eq $_.Difference })
    if ($trace.Schema -ne 163 -or $trace.ModifierScenario -ne 'unplayed-card-effects' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'UNPLAYED-CARD-EFFECTS-PREPARED originals=2 cards=TitanCurse,Scourge_SoulSavior soulSaviorCopyable=true' -Quiet) -or
        $matchedEffectTurns.Count -eq 0 -or $matchedEffectTurns[0].Before.Spawn.Turn -ne 0 -or
        $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or -not $trace.NativeWon) {
        throw 'The original TitanCurse and Scourge_SoulSavior triggers did not match modeled EndTurn in a complete battle.'
    }
}
if ($OriginalRoomEntryStatusRelics -and ($trace.Schema -ne 132 -or
    $trace.ModifierScenario -ne 'original-room-entry-status-relics' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-ROOM-ENTRY-STATUS-RELICS-ACQUIRED unchanged' -Quiet) -or
    @($trace.TrainPhases).Count -eq 0 -or @($trace.Actions).Count -eq 0)) {
    throw 'Original room-entry status relics were not exercised in the expected full-battle capture.'
}
if ($OriginalMoonPhaseEnergyRelic) {
    $moonPhaseCallbacks = @($trace.RelicResourceOperations | Where-Object { $_.Kind -eq 'MoonPhaseEnergy' })
    $fullMoonCallbacks = @($moonPhaseCallbacks | Where-Object { $_.Before.MoonPhase -eq 'Full' })
    $newMoonCallbacks = @($moonPhaseCallbacks | Where-Object { $_.Before.MoonPhase -eq 'New' })
    if ($trace.Schema -ne 146 -or $trace.ModifierScenario -ne 'original-moon-phase-energy-relic' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-MOON-PHASE-ENERGY-RELIC-PREPARED isolated native tracking enabled' -Quiet) -or
        @($trace.Turns).Count -lt 2 -or $moonPhaseCallbacks.Count -ne 6 -or
        @($moonPhaseCallbacks | Where-Object { $_.OwnerDataId -ne '70096a91-220a-4ab3-b8d0-b8fe8f3a819c' -or $_.EffectIndex -ne 0 }).Count -gt 0 -or
        $fullMoonCallbacks.Count -ne 3 -or $newMoonCallbacks.Count -ne 3 -or
        @($fullMoonCallbacks | Where-Object { $_.Notifications -ne 1 -or $_.After.Energy -ne [Math]::Min($_.Before.Resources.MaxEnergy, $_.Before.Energy + 1) }).Count -gt 0 -or
        @($newMoonCallbacks | Where-Object { $_.Notifications -ne 0 -or $_.After.Energy -ne $_.Before.Energy }).Count -gt 0) {
        throw 'Original moon-phase energy relic lacks the expected Full/New native callback witnesses.'
    }
}
if ($OriginalCardAttackRelic) {
    $attackRecords = @($trace.RelicCardModifiers | Where-Object { $_.Before.DataId -in @('fc119000-0000-4000-8000-000000000201', 'fc119000-0000-4000-8000-000000000202') })
    if ($trace.Schema -ne 147 -or $trace.ModifierScenario -ne 'original-card-attack-relic' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARD-ATTACK-RELIC-PREPARED native Whelp and Dragon' -Quiet) -or
        @($trace.Actions).Count -eq 0 -or $attackRecords.Count -lt 4 -or
        @($attackRecords | Where-Object { $_.Difference -ne $null }).Count -gt 0) {
        throw 'Original Whelp/Dragon card-attack relic was not exercised against native cards and effects.'
    }
}
if ($OriginalUnitUpgradeTriggerRelic -and ($trace.Schema -ne 133 -or
    $trace.ModifierScenario -ne 'original-unit-upgrade-trigger-relic' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-UNIT-UPGRADE-TRIGGER-RELIC-PREPARED' -Quiet) -or
    @($trace.Actions).Count -eq 0)) {
    throw 'Original unit-upgrade trigger relic was not exercised in the expected full-battle capture.'
}
if ($RelicCatalog) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $catalogArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'relic-catalog.mt2f'))
    try {
        $catalog = $catalogArchive.RootElement
        $catalogRecords = @($catalog.GetProperty('Relics').EnumerateArray())
        if ($catalog.GetProperty('Schema').GetInt32() -ne 5 -or
            $catalog.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $catalog.GetProperty('FrameBefore').GetInt32() -ne $catalog.GetProperty('FrameAfter').GetInt32() -or
            -not $catalog.GetProperty('RngBefore').ContentEquals($catalog.GetProperty('RngAfter')) -or
            $catalogRecords.Count -eq 0 -or
            @($catalogRecords | ForEach-Object { $_.GetProperty('Collection').GetString() } | Sort-Object -Unique).Count -ne 8) {
            throw 'Relic definition catalog is incomplete or changed native RNG/frame.'
        }
        Write-Output "NATIVE-RELIC-CATALOG PASS: $($catalogRecords.Count) definitions from eight original collections; unchanged gameplay/test RNG and frame."
    } finally { $catalogArchive.Dispose() }
}
if ($TraitComposition) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $traitArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-trait-composition-calibration.mt2f'))
    try {
        $traitData = $traitArchive.RootElement
        if ($traitData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $traitData.GetProperty('ContextUnchanged').GetBoolean() -or
            $traitData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $traitData.GetProperty('FrameBefore').GetInt32() -ne $traitData.GetProperty('FrameAfter').GetInt32() -or
            -not $traitData.GetProperty('RngBefore').ContentEquals($traitData.GetProperty('RngAfter')) -or
            $traitData.GetProperty('Rows').GetArrayLength() -ne 2134) { throw 'Incomplete or mutating trait composition calibration.' }
        Write-Output 'NATIVE-TRAIT-COMPOSITION-CAPTURE PASS: 2134 original/owned/controlled refresh observations; unchanged context/RNG/frame.'
    } finally { $traitArchive.Dispose() }
}
if ($CardStatusComposition) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $statusArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-status-composition-calibration.mt2f'))
    try {
        $statusData = $statusArchive.RootElement
        if ($statusData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $statusData.GetProperty('ContextUnchanged').GetBoolean() -or
            $statusData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $statusData.GetProperty('FrameBefore').GetInt32() -ne $statusData.GetProperty('FrameAfter').GetInt32() -or
            -not $statusData.GetProperty('RngBefore').ContentEquals($statusData.GetProperty('RngAfter')) -or
            $statusData.GetProperty('Rows').GetArrayLength() -ne 1039) { throw 'Incomplete or mutating card status composition calibration.' }
        Write-Output 'NATIVE-CARD-STATUS-COMPOSITION-CAPTURE PASS: 1039 original-owned/controlled status queries; unchanged context/RNG/frame.'
    } finally { $statusArchive.Dispose() }
}
if ($RelicCardUpgrades) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $relicUpgradeArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'relic-card-upgrade-calibration.mt2f'))
    try {
        $relicUpgradeData = $relicUpgradeArchive.RootElement
        if ($relicUpgradeData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $relicUpgradeData.GetProperty('ContextUnchanged').GetBoolean() -or
            $relicUpgradeData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $relicUpgradeData.GetProperty('FrameBefore').GetInt32() -ne $relicUpgradeData.GetProperty('FrameAfter').GetInt32() -or
            -not $relicUpgradeData.GetProperty('RngBefore').ContentEquals($relicUpgradeData.GetProperty('RngAfter')) -or
            $relicUpgradeData.GetProperty('Rows').GetArrayLength() -ne 432 -or
            $relicUpgradeData.GetProperty('OriginalRelicAssets').GetArrayLength() -ne 11 -or
            $relicUpgradeData.GetProperty('OwnedCardCount').GetInt32() -ne 15) { throw 'Incomplete or mutating relic card upgrade calibration.' }
        Write-Output 'NATIVE-RELIC-CARD-UPGRADE-CAPTURE PASS: 432 original-effect operations,11 original relics,15 owned cards; unchanged context/RNG/frame.'
    } finally { $relicUpgradeArchive.Dispose() }
}
if ($BattleRelicUpgrades) {
    $modifierRows = @($trace.RelicCardModifiers)
    $rejectedDispatches = if ($RelicCardStatusUpgrades -or $RelicCardUnhealedHealthUpgrades -or $RelicCardPiercingUpgrades -or $RelicCardAbilityUpgrades -or $RelicCardTriggerUpgrades -or $RelicCardCastTriggerUpgrades -or $RelicCardCastTriggerUnitUpgrades -or $RelicCardReplacementUpgrades -or $RelicCardHealthUpgrades -or $OriginalDeployableRelic) {
        @($modifierRows | ForEach-Object Dispatches | Where-Object { -not $_.Returned -and -not $_.UpgradeAdded }).Count
    } else {
        @($modifierRows | ForEach-Object Dispatches | Where-Object { $_.Returned -and -not $_.UpgradeAdded }).Count
    }
    if (-not $trace.RelicCardModifiersEnabled -or $modifierRows.Count -eq 0 -or
        @($modifierRows | Where-Object { $null -eq $_.Actual -or $_.Difference }).Count -gt 0 -or
        @($modifierRows | Where-Object { -not $_.ResetTemporary }).Count -eq 0 -or
        $rejectedDispatches -eq 0 -or
        @($modifierRows | ForEach-Object Dispatches | Where-Object UpgradeAdded).Count -eq 0) {
        throw 'Incomplete native relic manager card modifier acceptance.'
    }
    if ($RelicCardStatusUpgrades -or $RelicCardUnhealedHealthUpgrades -or $RelicCardPiercingUpgrades -or $RelicCardSelfPurgeUpgrades -or $RelicCardExhaustUpgrades -or $RelicCardEphemeralUpgrades -or $RelicCardAbilityUpgrades -or $RelicCardTriggerUpgrades -or $RelicCardCastTriggerUpgrades -or $RelicCardCastTriggerUnitUpgrades -or $RelicCardReplacementUpgrades) {
        Write-Output "NATIVE-RELIC-CARD-UPGRADE-CAPTURE PASS: $($modifierRows.Count) actual manager calls, temporary upgrade payload, reset and failed-eligibility paths."
    } else {
        Write-Output "NATIVE-RELIC-CARD-MODIFIER-CAPTURE PASS: $($modifierRows.Count) actual manager calls, original eligibility, reset/repeat/unique rejection and notifications."
    }
}
if ($ConditionalBattleRelicUpgrades -and $trace.ModifierScenario -ne 'conditional-relic-card-upgrades') {
    throw 'Conditional relic card upgrade scenario was not recorded.'
}
if ($RelicCardStatusUpgrades -and $trace.ModifierScenario -ne 'relic-card-status-upgrades') {
    throw 'Relic card status upgrade scenario was not recorded.'
}
if ($RelicCardPiercingUpgrades -and $trace.ModifierScenario -ne 'relic-card-piercing-upgrades') {
    throw 'Relic card piercing upgrade scenario was not recorded.'
}
if ($RelicCardSelfPurgeUpgrades -and $trace.ModifierScenario -ne 'relic-card-self-purge-upgrades') {
    throw 'Relic card self-purge upgrade scenario was not recorded.'
}
if ($RelicCardExhaustUpgrades -and $trace.ModifierScenario -ne 'relic-card-exhaust-upgrades') {
    throw 'Relic card exhaust upgrade scenario was not recorded.'
}
if ($RelicCardEphemeralUpgrades -and $trace.ModifierScenario -ne 'relic-card-ephemeral-upgrades') {
    throw 'Relic card Ephemeral upgrade scenario was not recorded.'
}
if ($RelicCardHealthUpgrades -and $trace.ModifierScenario -ne 'relic-card-health-upgrades') {
    throw 'Relic card health upgrade scenario was not recorded.'
}
if ($RelicCardUnhealedHealthUpgrades -and $trace.ModifierScenario -ne 'relic-card-unhealed-upgrades') {
    throw 'Relic card unhealed-health upgrade scenario was not recorded.'
}
if ($RelicCardAbilityUpgrades -and $trace.ModifierScenario -ne 'relic-card-ability-upgrades') {
    throw 'Relic card ability upgrade scenario was not recorded.'
}
if ($RelicCardTriggerUpgrades -and $trace.ModifierScenario -ne 'relic-card-trigger-upgrades') {
    throw 'Relic card trigger upgrade scenario was not recorded.'
}
if ($RelicCardCastTriggerUpgrades -and $trace.ModifierScenario -ne 'relic-card-cast-trigger-upgrades') {
    throw 'Relic card OnCast trigger upgrade scenario was not recorded.'
}
if ($RelicCardCastTriggerUpgrades) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release -- --relic-card-modifiers-only $tracePath
    if ($LASTEXITCODE -ne 0) { throw 'Native OnCast card play replay did not pass its focused model check.' }
}
if ($RelicCardCastTriggerUnitUpgrades -and $trace.ModifierScenario -ne 'relic-card-cast-trigger-unit-upgrades') {
    throw 'Relic Monster-card OnCast trigger upgrade scenario was not recorded.'
}
if ($RelicCardCastTriggerUnitUpgrades) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release -- --relic-card-modifiers-only $tracePath
    if ($LASTEXITCODE -ne 0) { throw 'Native Monster-card OnCast replay did not pass its focused model check.' }
}
if ($RelicCardReplacementUpgrades -and $trace.ModifierScenario -ne 'relic-card-replacement-upgrades') {
    throw 'Relic card replacement upgrade scenario was not recorded.'
}
if ($BranchCardMasks) {
    $branchRows = @($trace.BranchCardMasks)
    if (-not $trace.BranchCardMasksEnabled -or $branchRows.Count -eq 0 -or
        @($branchRows | Where-Object { @($_.Views).Count -ne @($_.Context.CardRegistry).Count -or
            @($_.Context.CardRegistry | Where-Object { $null -eq $_.MaskDescriptor -or $_.MaskDescriptor.Definition.DataId -ne $_.DataId }).Count -gt 0 }).Count -gt 0) {
        throw 'Branch card mask oracle or owned descriptors are incomplete.'
    }
    Write-Output "NATIVE-BRANCH-CARD-MASK-CAPTURE PASS: $($branchRows.Count) complete decision contexts with independent native views."
}
if ($OwnedCardMasks) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $ownedArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-owned-mask-calibration.mt2f'))
    try {
        $ownedData = $ownedArchive.RootElement
        if ($ownedData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $ownedData.GetProperty('ContextUnchanged').GetBoolean() -or
            $ownedData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $ownedData.GetProperty('FrameBefore').GetInt32() -ne $ownedData.GetProperty('FrameAfter').GetInt32() -or
            -not $ownedData.GetProperty('RngBefore').ContentEquals($ownedData.GetProperty('RngAfter')) -or
            $ownedData.GetProperty('Rows').GetArrayLength() -ne 416 -or
            $ownedData.GetProperty('Masks').GetArrayLength() -ne 62 -or
            -not $ownedData.GetProperty('ParameterTraitPresent').GetBoolean()) { throw 'Incomplete or mutating owned card mask calibration.' }
        Write-Output 'NATIVE-OWNED-CARD-MASK-CAPTURE PASS: 416 owned/controlled carried views and 62 original masks; unchanged context/RNG/frame.'
    } finally { $ownedArchive.Dispose() }
}
if ($CardUpgradeLifecycle) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $lifecycleArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-upgrade-lifecycle-calibration.mt2f'))
    try {
        $lifecycleData = $lifecycleArchive.RootElement
        if ($lifecycleData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $lifecycleData.GetProperty('ContextUnchanged').GetBoolean() -or
            $lifecycleData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $lifecycleData.GetProperty('FrameBefore').GetInt32() -ne $lifecycleData.GetProperty('FrameAfter').GetInt32() -or
            -not $lifecycleData.GetProperty('RngBefore').ContentEquals($lifecycleData.GetProperty('RngAfter')) -or
            $lifecycleData.GetProperty('OriginalRelicAssets').GetArrayLength() -ne 11 -or
            $lifecycleData.GetProperty('Rows').GetArrayLength() -ne 212) { throw 'Incomplete or mutating card upgrade lifecycle calibration.' }
        Write-Output 'NATIVE-CARD-UPGRADE-LIFECYCLE-CAPTURE PASS: 212 native operations across 11 original relic payloads and controlled lifecycle/magic power sequences; unchanged context/RNG/frame.'
    } finally { $lifecycleArchive.Dispose() }
}
if ($ModifierOverflow) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $overflowArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-modifier-overflow-calibration.mt2f'))
    try {
        $overflowData = $overflowArchive.RootElement
        if (-not $overflowData.GetProperty('ContextUnchanged').GetBoolean() -or
            $overflowData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $overflowData.GetProperty('FrameBefore').GetInt32() -ne $overflowData.GetProperty('FrameAfter').GetInt32() -or
            -not $overflowData.GetProperty('RngBefore').ContentEquals($overflowData.GetProperty('RngAfter')) -or
            $overflowData.GetProperty('Samples').GetArrayLength() -ne 2240) { throw 'Incomplete or mutating modifier overflow calibration.' }
        Write-Output 'NATIVE-MODIFIER-OVERFLOW-CAPTURE PASS: 2240 original numeric queries; unchanged context/RNG/frame.'
    } finally { $overflowArchive.Dispose() }
}
if ($UpgradeMasks) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $maskArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-upgrade-mask-calibration.mt2f'))
    try {
        $maskCalibration = $maskArchive.RootElement
        if ($maskCalibration.GetProperty('Schema').GetInt32() -ne 2 -or
            $maskCalibration.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            -not $maskCalibration.GetProperty('ContextUnchanged').GetBoolean() -or
            $maskCalibration.GetProperty('FrameBefore').GetInt32() -ne $maskCalibration.GetProperty('FrameAfter').GetInt32() -or
            -not $maskCalibration.GetProperty('RngBefore').ContentEquals($maskCalibration.GetProperty('RngAfter')) -or
            $maskCalibration.GetProperty('Rows').GetArrayLength() -eq 0 -or
            $maskCalibration.GetProperty('CardData').GetArrayLength() -eq 0 -or
            $maskCalibration.GetProperty('CardStates').GetArrayLength() -eq 0 -or
            $maskCalibration.GetProperty('Characters').GetArrayLength() -eq 0 -or
            $maskCalibration.GetProperty('EdgeCases').GetArrayLength() -lt 50) {
            throw 'Card upgrade mask calibration is incomplete or changed native state.'
        }
        Write-Output "NATIVE-UPGRADE-MASK-CAPTURE PASS: $($maskCalibration.GetProperty('Rows').GetArrayLength()) original masks, $($maskCalibration.GetProperty('CardData').GetArrayLength()) original card definitions, owned cards and live characters; unchanged native context/RNG/frame."
    } finally { $maskArchive.Dispose() }
}
if ($PersistentEnchantments) {
    $persistentScenario = $environment['MT2_PROBE_MODIFIERS']
    $persistentFrames = @($trace.Actions | ForEach-Object { $_.Actual.Spawn.Train.Context.Enchantments })
    $persistentRules = @($persistentFrames | ForEach-Object { $_.Rooms.Units.Triggers.Effects.Enchantment } | Where-Object { $null -ne $_ -and $_.Bound })
    if ($trace.Schema -ne $(if ($SettleCardAnimations) { 106 } elseif ($SettleDeathDissolves) { 105 } else { 104 }) -or $trace.ModifierScenario -ne $persistentScenario -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'PERSISTENT-ENCHANTMENT-PREPARED' -Quiet) -or
        $persistentFrames.Count -ne @($trace.Actions).Count -or @($trace.Turns).Count -lt 3 -or
        $persistentRules.Count -lt 2 -or @($persistentRules | Where-Object { @($_.State.PrimaryTargets).Count -gt 0 }).Count -eq 0) {
        throw 'Persistent enchantment scenario did not retain bound, nonempty aura state through the paid multi-turn battle.'
    }
    if ($PersistentEnchantmentDeaths -and @($persistentFrames | ForEach-Object { $_.RetainedUnits } | Where-Object {
        $_.Unit.Health -eq 0 -and $_.Unit.DeathState.IsDestroyed -and
        @($_.Unit.Triggers.Effects.Enchantment | Where-Object { $null -ne $_ -and -not $_.Bound -and @($_.State.PrimaryTargets).Count -gt 0 }).Count -gt 0
    }).Count -eq 0) { throw 'Persistent enchantment death scenario did not destroy and release an aura source with retained effect maps.' }
    if ($PersistentEnchantmentRevivals -and (@($trace.Revivals).Count -lt 4 -or @($trace.RevivalOperations).Count -ne 0 -or
        @($trace.Revivals | Where-Object { $_.AutomaticQueueDeferrals -ne 0 -or -not $_.Completed -or $_.Error }).Count -gt 0)) {
        throw 'Persistent aura revival must include four natural complete revivals without setup operations or queue deferrals.'
    }
    if ($PersistentEnchantmentRandomPools) {
        $randomPoolIds = @($persistentRules | ForEach-Object { $_.State.CachedStatus.Id } | Sort-Object -Unique)
        $randomPreviews = @($trace.PreviewRngIsolation | Where-Object {
            ($_.BattleBefore.Values -join ',') -ne ($_.TestObserved.Values -join ',')
        })
        if (@($persistentRules | Where-Object { @($_.StatusPool).Count -ne 3 }).Count -gt 0 -or
            $randomPoolIds.Count -ne 3 -or $randomPreviews.Count -eq 0 -or
            @($trace.PreviewRngIsolation | Where-Object { -not $_.Completed -or
                ($_.BattleBefore.Values -join ',') -ne ($_.BattleAfter.Values -join ',') -or
                ($_.TestBefore.Values -join ',') -ne ($_.TestAfter.Values -join ',') }).Count -gt 0) {
            throw 'Random persistent auras require three selected pool entries and actual isolated preview RNG draws.'
        }
    }
}
$modifierActions = @($trace.Actions | Where-Object {
    $actionEntry = $_
    $playedCard = $actionEntry.Before.Spawn.Train.Context.Cards.Hand |
        Where-Object InstanceId -EQ $actionEntry.Action.CardInstanceId
    $definition = $actionEntry.Before.PlayRules.Cards | Where-Object DataId -EQ $playedCard.DataId
    @($definition.Effects | Where-Object { $_.Type -eq 'UnitUpgrade' -or $_.Type -eq 'HandUpgrade' }).Count -gt 0
})
$equipmentBuffActions = @($trace.Actions | Where-Object {
    $actionEntry = $_
    $playedCard = $actionEntry.Before.Spawn.Train.Context.Cards.Hand |
        Where-Object InstanceId -EQ $actionEntry.Action.CardInstanceId
    $definition = $actionEntry.Before.PlayRules.Cards | Where-Object DataId -EQ $playedCard.DataId
    @($definition.Effects | Where-Object Type -EQ 'BuffEquipment').Count -gt 0
})
if ($EquipmentBuff) {
    $equipmentBuffCoverage = $equipmentBuffActions.Count -gt 0 -and @($equipmentBuffActions | Where-Object {
        $entry = $_
        $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $definition = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
        $effect = $definition.Effects | Where-Object Type -EQ 'BuffEquipment' | Select-Object -First 1
        $beforeUnit = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        $afterUnit = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        $equipmentIds = @($beforeUnit.EquipmentCards)
        $allEquipmentChanged = $equipmentIds.Count -ge 2 -and @($equipmentIds | Where-Object {
            $equipmentId = $_
            $afterCard = $entry.Actual.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $equipmentId
            @($afterCard.Permanent.Upgrades | Where-Object { $_.DataId -eq $effect.Upgrade.DataId }).Count -eq 0
        }).Count -eq 0
        $expectedAttackGain = $effect.Upgrade.Stats.Damage * $equipmentIds.Count
        $expectedMaxHealthGain = ($effect.Upgrade.Stats.Health + $effect.Upgrade.UnhealedHealth) * $equipmentIds.Count
        $unitUpgradeChanged = $afterUnit.Attack - $beforeUnit.Attack -eq $expectedAttackGain -and
            $afterUnit.MaxHealth - $beforeUnit.MaxHealth -eq $expectedMaxHealthGain
        -not $_.Difference -and $_.Predicted.Supported -and $allEquipmentChanged -and $unitUpgradeChanged
    }).Count -gt 0
    if (-not $equipmentBuffCoverage) { throw 'BuffEquipment did not upgrade its host and every attached equipment card in a matched native action.' }
}
$modifierCoverage = (-not ($DynamicUpgrades -or $SacrificeUpgrades -or $HandUpgrades -or $TargetedHandUpgrades)) -or $modifierActions.Count -gt 0
$sacrificeEffectActions = @($trace.Actions | Where-Object {
    $actionEntry = $_
    $playedCard = $actionEntry.Before.Spawn.Train.Context.Cards.Hand |
        Where-Object InstanceId -EQ $actionEntry.Action.CardInstanceId
    $definition = $actionEntry.Before.PlayRules.Cards | Where-Object DataId -EQ $playedCard.DataId
    @($definition.Effects | Where-Object Type -EQ 'Sacrifice').Count -gt 0
})
$sacrificeEffectCoverage = -not $SacrificeEffects -or @($sacrificeEffectActions | Where-Object {
    $actionEntry = $_
    $room = $actionEntry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $actionEntry.Action.RoomIndex
    $actionEntry.Action.TargetUnitId -gt 0 -and @($room.Units | Where-Object Id -EQ $actionEntry.Action.TargetUnitId).Count -eq 0
}).Count -gt 0
$setHealthActions = @($trace.Actions | Where-Object {
    $entry = $_
    $playedCard = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $playedCard.DataId -eq '4c0f6e0e-ac05-4e0c-a478-590ac06d646f'
})
$setHealthCoverage = -not $SetHealth -or @($setHealthActions | Where-Object {
    $entry = $_
    $beforeUnit = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
    $afterUnit = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
    $beforeUnit.Health -gt 1 -and $afterUnit.Health -eq 1 -and -not $entry.Difference -and $entry.Predicted.Supported
}).Count -gt 0
$transferAttackActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $definition = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $played.DataId
    @($definition.Effects | Where-Object Type -EQ 'TransferAttackFromUnit').Count -gt 0
})
$transferAttackCoverage = -not $TransferAttack -or @($transferAttackActions | Where-Object {
    $entry = $_
    $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
    $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
    $source = $beforeRoom.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
    $recipient = $beforeRoom.Units | Where-Object {
        $_.Team -eq $source.Team -and @($_.Statuses | Where-Object Id -EQ 'untouchable').Count -eq 0
    } | Select-Object -First 1
    $afterRecipient = $afterRoom.Units | Where-Object Id -EQ $recipient.Id
    $beforeSpawner = $entry.Before.Spawn.Train.Context.CardInstances |
        Where-Object InstanceId -EQ $recipient.SpawnerCardId
    $afterSpawner = $entry.Actual.Spawn.Train.Context.CardInstances |
        Where-Object InstanceId -EQ $recipient.SpawnerCardId
    $source -and $recipient -and $recipient.Id -ne $source.Id -and $source.Attack -gt 0 -and
        $afterRecipient.Attack -eq $recipient.Attack + 2 * $source.Attack -and
        @($afterSpawner.Temporary.Upgrades).Count -eq @($beforeSpawner.Temporary.Upgrades).Count + 1 -and
        -not $entry.Difference -and $entry.Predicted.Supported
}).Count -gt 0
$brainTransferActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '560b9623-f130-4848-9f9f-56cc6e8bf19e'
})
$brainTransferCoverage = -not $BrainTransfer -or @($brainTransferActions | Where-Object {
    $entry = $_
    $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ 0
    $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ 0
    $source = $beforeRoom.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
    $recipient = $beforeRoom.Units | Where-Object { $_.Team -eq 1 -and $_.Id -ne $source.Id } | Select-Object -First 1
    $afterRecipient = $afterRoom.Units | Where-Object Id -EQ $recipient.Id
    $beforeSpawner = $entry.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $recipient.SpawnerCardId
    $afterSpawner = $entry.Actual.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $recipient.SpawnerCardId
    $source.AssetKey -eq 'MonsterQueensImp' -and $recipient.AssetKey -eq 'TrainStewardBig' -and
        @($source.Statuses | Where-Object { $_.Id -eq 'buff' -and $_.Stacks -eq 3 }).Count -eq 1 -and
        @($source.Statuses | Where-Object { $_.Id -eq 'scorch' -and $_.Stacks -eq 2 }).Count -eq 1 -and
        @($afterRoom.Units | Where-Object Id -EQ $source.Id).Count -eq 0 -and
        @($afterRecipient.Statuses | Where-Object { $_.Id -eq 'buff' -and $_.Stacks -eq 3 }).Count -eq 1 -and
        @($afterRecipient.Statuses | Where-Object { $_.Id -eq 'scorch' -and $_.Stacks -eq 2 }).Count -eq 1 -and
        $afterRecipient.Attack -eq $recipient.Attack + $source.Attack -and
        @($afterSpawner.Temporary.Upgrades).Count -eq @($beforeSpawner.Temporary.Upgrades).Count + 1 -and
        -not $entry.Difference -and $entry.Predicted.Supported
}).Count -gt 0
$frostChannelingActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'a82b7b1b-31f1-4634-9cf3-197d17d61ba3'
})
$frostChannelingCoverage = -not $FrostChanneling -or @($frostChannelingActions | Where-Object {
    $entry = $_
    $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
    $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
    $enemy = $beforeRoom.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
    $friend = $beforeRoom.Units | Where-Object {
        $_.Team -eq 1 -and @($_.Statuses | Where-Object { $_.Id -eq 'poison' -and $_.Stacks -eq 8 }).Count -eq 1
    } | Select-Object -First 1
    $afterEnemy = $afterRoom.Units | Where-Object Id -EQ $enemy.Id
    $afterFriend = $afterRoom.Units | Where-Object Id -EQ $friend.Id
    $definition = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ 'a82b7b1b-31f1-4634-9cf3-197d17d61ba3'
    $enemy.Team -eq 0 -and $friend -and
        @($enemy.Statuses | Where-Object { $_.Id -eq 'poison' -and $_.Stacks -eq 8 }).Count -eq 1 -and
        @($afterEnemy.Statuses | Where-Object { $_.Id -eq 'poison' -and $_.Stacks -eq 32 }).Count -eq 1 -and
        @($afterFriend.Statuses | Where-Object { $_.Id -eq 'poison' -and $_.Stacks -gt 0 }).Count -eq 0 -and
        (@($definition.Effects | ForEach-Object Type) -join ',') -eq 'TransferAllStatuses,MultiplyStatus' -and
        -not $entry.Difference -and $entry.Predicted.Supported
}).Count -gt 0
$transferAbilityActions = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 })
$transferAbilityCoverage = -not $TransferAttackAbility -or @($transferAbilityActions | Where-Object {
    $entry = $_
    $actorId = $entry.Action.ActivatorUnitId
    $beforeActor = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $actorId
    $afterActor = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $actorId
    $beforeSpawner = $entry.Before.Spawn.Train.Context.CardInstances |
        Where-Object InstanceId -EQ $beforeActor.SpawnerCardId
    $afterSpawner = $entry.Actual.Spawn.Train.Context.CardInstances |
        Where-Object InstanceId -EQ $beforeActor.SpawnerCardId
    $beforeActor.Attack -gt 0 -and $afterActor.Attack -eq 3 * $beforeActor.Attack -and
        @($afterActor.Modifiers.Upgrades).Count -eq @($beforeActor.Modifiers.Upgrades).Count + 1 -and
        @($afterSpawner.Temporary.Upgrades).Count -eq @($beforeSpawner.Temporary.Upgrades).Count + 1 -and
        -not $entry.Difference -and $entry.Predicted.Supported
}).Count -gt 0
$damageByUnitsKilledActions = @($trace.Actions | Where-Object {
    $entry = $_
    $playedCard = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $playedCard.DataId -eq 'd9c0c510-ccb5-4a89-9f3b-99799e765540'
})
$damageByUnitsKilledCoverage = -not $DamageByUnitsKilled -or @($damageByUnitsKilledActions | Where-Object {
    $entry = $_
    $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
    $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
    $imps = @($beforeRoom.Units | Where-Object {
        $_.Team -eq 1 -and $_.Subtypes -contains 'SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c'
    })
    $enemyDamage = @($beforeRoom.Units | Where-Object {
        $enemy = $_
        $enemy.Team -eq 0 -and @($afterRoom.Units | Where-Object Id -EQ $enemy.Id |
            Where-Object { $_.Health -ge $enemy.Health }).Count -eq 0
    })
    $imps.Count -gt 0 -and @($imps | Where-Object {
        $imp = $_
        @($afterRoom.Units | Where-Object Id -EQ $imp.Id).Count -eq 0
    }).Count -eq $imps.Count -and $enemyDamage.Count -gt 0 -and
        -not $entry.Difference -and $entry.Predicted.Supported
}).Count -gt 0
$cardEffectKillActions = @($trace.Actions | Where-Object {
    $actionEntry = $_
    $playedCard = $actionEntry.Before.Spawn.Train.Context.Cards.Hand |
        Where-Object InstanceId -EQ $actionEntry.Action.CardInstanceId
    $definition = $actionEntry.Before.PlayRules.Cards | Where-Object DataId -EQ $playedCard.DataId
    @($definition.Effects | Where-Object Type -EQ 'Kill').Count -eq 2
})
$cardEffectKillCoverage = -not ($CardEffectKill -or $CardEffectKillSubtype) -or @($cardEffectKillActions | Where-Object {
    $actionEntry = $_
    $beforeRoom = $actionEntry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $actionEntry.Action.RoomIndex
    $actualRoom = $actionEntry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $actionEntry.Action.RoomIndex
    $enemyDeaths = @($beforeRoom.Units | Where-Object {
        $beforeUnit = $_
        $beforeUnit.Team -eq 0 -and -not $beforeUnit.IsPyre -and
            @($actualRoom.Units | Where-Object Id -EQ $beforeUnit.Id).Count -eq 0
    }).Count
    $playerDeaths = @($beforeRoom.Units | Where-Object {
        $beforeUnit = $_
        $beforeUnit.Team -eq 1 -and -not $beforeUnit.IsPyre -and
            @($actualRoom.Units | Where-Object Id -EQ $beforeUnit.Id).Count -eq 0
    }).Count
    $enemyDeaths -gt 0 -and $playerDeaths -gt 0
}).Count -gt 0
$cardEffectKillSubtypeCoverage = -not $CardEffectKillSubtype -or @($cardEffectKillActions | Where-Object {
    $actionEntry = $_
    $playedCard = $actionEntry.Before.Spawn.Train.Context.Cards.Hand |
        Where-Object InstanceId -EQ $actionEntry.Action.CardInstanceId
    $definition = $actionEntry.Before.PlayRules.Cards | Where-Object DataId -EQ $playedCard.DataId
    $subtypedKills = @($definition.Effects | Where-Object { $_.Type -EQ 'Kill' -and $_.TargetSubtype })
    $beforeRoom = $actionEntry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $actionEntry.Action.RoomIndex
    $actualRoom = $actionEntry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $actionEntry.Action.RoomIndex
    @($subtypedKills | Where-Object {
        $subtype = $_.TargetSubtype
        @($beforeRoom.Units | Where-Object {
            $beforeUnit = $_
            $beforeUnit.Subtypes -contains $subtype -and
                @($actualRoom.Units | Where-Object Id -EQ $beforeUnit.Id).Count -eq 0
        }).Count -gt 0
    }).Count -gt 0
}).Count -gt 0
$cardRecursionActions = @($trace.Actions | Where-Object {
    $actionEntry = $_
    $playedCard = $actionEntry.Before.Spawn.Train.Context.Cards.Hand |
        Where-Object InstanceId -EQ $actionEntry.Action.CardInstanceId
    $definition = $actionEntry.Before.PlayRules.Cards | Where-Object DataId -EQ $playedCard.DataId
    @($definition.Effects | Where-Object Type -EQ 'Recursion').Count -gt 0
})
$cardRecursionCoverage = -not ($CardRecursion -or $CardRecursionDiscard -or $CardRecursionChoose -or $CardRecursionChooseToHand -or $CardRecursionChooseFilter -or $CardRecursionUpgrade -or $CardRecursionUpgradeToHand -or $CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose)
if ($CardRecursion) {
    $cardRecursionCoverage = @($cardRecursionActions | Where-Object {
    $actionEntry = $_
    $candidateIds = @($actionEntry.Before.OtherPiles | Where-Object Name -EQ 'Exhausted' |
        ForEach-Object Cards | Where-Object DataId -EQ 'fb0683b1-20ac-4dd2-b580-14b502e182f4' |
        ForEach-Object InstanceId)
    $afterDrawIds = @($actionEntry.Actual.Spawn.Train.Context.Cards.Draw | ForEach-Object InstanceId)
    $returned = @($candidateIds | Where-Object { $afterDrawIds -contains $_ })
    -not $_.Difference -and $_.Predicted.Supported -and $returned.Count -eq 5
    }).Count -gt 0
}
if ($CardRecursionChoose) {
    $cardRecursionCoverage = @($cardRecursionActions | Where-Object {
        $actionEntry = $_
        $candidateIds = @($actionEntry.Before.OtherPiles | Where-Object Name -EQ 'Exhausted' |
            ForEach-Object Cards | Where-Object DataId -EQ 'fb0683b1-20ac-4dd2-b580-14b502e182f4' |
            ForEach-Object InstanceId)
        $selectedIds = @($actionEntry.Action.RecursionSelections | ForEach-Object CardInstanceIds | ForEach-Object { $_ })
        $afterDrawIds = @($actionEntry.Actual.Spawn.Train.Context.Cards.Draw | ForEach-Object InstanceId)
        $returned = @($candidateIds | Where-Object { $afterDrawIds -contains $_ })
        -not $_.Difference -and $_.Predicted.Supported -and $selectedIds.Count -eq 1 -and
            $candidateIds -contains $selectedIds[0] -and $returned.Count -eq 1 -and $returned[0] -eq $selectedIds[0]
    }).Count -gt 0
}
if ($CardRecursionDiscard) {
    $cardRecursionCoverage = @($cardRecursionActions | Where-Object {
        $actionEntry = $_
        $candidateIds = @($actionEntry.Before.Spawn.Train.Context.Cards.Discard |
            Where-Object DataId -EQ 'fb0683b1-20ac-4dd2-b580-14b502e182f4' | ForEach-Object InstanceId)
        $selectedIds = @($actionEntry.Action.RecursionSelections | ForEach-Object CardInstanceIds | ForEach-Object { $_ })
        $afterHandIds = @($actionEntry.Actual.Spawn.Train.Context.Cards.Hand | ForEach-Object InstanceId)
        $afterDiscardIds = @($actionEntry.Actual.Spawn.Train.Context.Cards.Discard | ForEach-Object InstanceId)
        -not $_.Difference -and $_.Predicted.Supported -and $selectedIds.Count -eq 1 -and
            $candidateIds -contains $selectedIds[0] -and $afterHandIds -contains $selectedIds[0] -and
            $afterDiscardIds -notcontains $selectedIds[0]
    }).Count -gt 0
}
if ($CardRecursionChooseToHand) {
    $cardRecursionCoverage = @($cardRecursionActions | Where-Object {
        $actionEntry = $_
        $candidateIds = @($actionEntry.Before.OtherPiles | Where-Object Name -EQ 'Exhausted' |
            ForEach-Object Cards | Where-Object DataId -EQ 'fb0683b1-20ac-4dd2-b580-14b502e182f4' |
            ForEach-Object InstanceId)
        $selectedIds = @($actionEntry.Action.RecursionSelections | ForEach-Object CardInstanceIds | ForEach-Object { $_ })
        $afterHandIds = @($actionEntry.Actual.Spawn.Train.Context.Cards.Hand | ForEach-Object InstanceId)
        $returned = @($candidateIds | Where-Object { $afterHandIds -contains $_ })
        -not $_.Difference -and $_.Predicted.Supported -and $selectedIds.Count -eq 1 -and
            $candidateIds -contains $selectedIds[0] -and $returned.Count -eq 1 -and $returned[0] -eq $selectedIds[0]
    }).Count -gt 0
}
if ($CardRecursionChooseFilter) {
    $cardRecursionCoverage = @($cardRecursionActions | Where-Object {
        $actionEntry = $_
        $candidateIds = @($actionEntry.Before.OtherPiles | Where-Object Name -EQ 'Exhausted' |
            ForEach-Object Cards | Where-Object DataId -EQ 'fb0683b1-20ac-4dd2-b580-14b502e182f4' |
            ForEach-Object InstanceId)
        $selectedIds = @($actionEntry.Action.RecursionSelections | ForEach-Object CardInstanceIds | ForEach-Object { $_ })
        $afterDrawIds = @($actionEntry.Actual.Spawn.Train.Context.Cards.Draw | ForEach-Object InstanceId)
        $returned = @($candidateIds | Where-Object { $afterDrawIds -contains $_ })
        -not $_.Difference -and $_.Predicted.Supported -and $selectedIds.Count -eq 1 -and
            $candidateIds -contains $selectedIds[0] -and $returned.Count -eq 1 -and $returned[0] -eq $selectedIds[0]
    }).Count -gt 0
}
if ($CardRecursionUpgrade -or $CardRecursionUpgradeToHand -or $CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose) {
    $cardRecursionCoverage = @($cardRecursionActions | Where-Object {
        $actionEntry = $_
        $candidateIds = @($actionEntry.Before.OtherPiles | Where-Object Name -EQ 'Exhausted' |
            ForEach-Object Cards | Where-Object DataId -EQ 'fb0683b1-20ac-4dd2-b580-14b502e182f4' |
            ForEach-Object InstanceId)
        $handIds = @($actionEntry.Actual.Spawn.Train.Context.Cards.Hand | ForEach-Object InstanceId)
        $drawIds = @($actionEntry.Actual.Spawn.Train.Context.Cards.Draw | ForEach-Object InstanceId)
        $returnedHand = @($candidateIds | Where-Object { $handIds -contains $_ })
        $returnedDraw = @($candidateIds | Where-Object { $drawIds -contains $_ })
        $returned = if ($CardRecursionUpgradeToHand) { @($returnedHand + $returnedDraw | Sort-Object -Unique) } else { $returnedDraw }
        $expectedReturned = if ($CardRecursionUpgradeToHand) { 2 } else { 1 }
        $destinationCoverage = if ($CardRecursionUpgradeToHand) {
            $actionEntry.Before.Spawn.Train.Context.Cards.Hand.Count -eq $actionEntry.Before.Spawn.Train.Context.MaxHandSize -and
                $returnedHand.Count -eq 1 -and $returnedDraw.Count -eq 1
        } else { $returnedHand.Count -eq 0 -and $returnedDraw.Count -eq 1 }
        $selectedIds = @($actionEntry.Action.RecursionSelections | ForEach-Object CardInstanceIds | ForEach-Object { $_ })
        $upgraded = @($actionEntry.Actual.Spawn.Train.Context.CardRegistry | Where-Object {
            $_.InstanceId -in $returned -and @($_.Temporary.Upgrades | Where-Object {
                $_.DataId -eq 'f2f8c03b-327d-4f3d-8cc1-f9b2a20e4ff6' -and $_.Stats.Damage -eq 3
            }).Count -eq 1
        })
        $allUpgradeIds = @($actionEntry.Actual.Spawn.Train.Context.CardRegistry | Where-Object {
            $_.Temporary.Upgrades.DataId -contains 'f2f8c03b-327d-4f3d-8cc1-f9b2a20e4ff6'
        } | ForEach-Object InstanceId)
        -not $_.Difference -and $_.Predicted.Supported -and $returned.Count -eq $expectedReturned -and $destinationCoverage -and
            (-not ($CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose) -or ($selectedIds.Count -eq 1 -and $selectedIds[0] -eq $returned[0])) -and
            $upgraded.Count -eq $expectedReturned -and $allUpgradeIds.Count -eq $expectedReturned -and
            (@($allUpgradeIds | Sort-Object) -join ',') -eq (@($returned | Sort-Object) -join ',')
    }).Count -gt 0
}
$healActions = @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $definition = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($definition.Effects | Where-Object Type -EQ 'Heal').Count -gt 0
})
$healingCoverage = -not ($Healing -or $HealingTriggers)
if ($Healing -or $HealingTriggers) {
    $healingCoverage = $healActions.Count -gt 0 -and @($healActions | Where-Object {
        $entry = $_
        $beforeUnit = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        $afterUnit = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        $null -ne $afterUnit -and $afterUnit.Health -gt $beforeUnit.Health
    }).Count -gt 0
}
$onHealCoverage = -not $HealingTriggers
if ($HealingTriggers) {
    $onHealCoverage = @($healActions | Where-Object {
        $entry = $_
        $target = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        @($target.Statuses | Where-Object Id -EQ 'heal immunity').Count -gt 0 -and
            $entry.Actual.Spawn.Train.Context.Gold - $entry.Before.Spawn.Train.Context.Gold -eq 15
    }).Count -gt 0
}
$roomSpellCoverage = -not $RoomSpells
if ($RoomSpells) {
    $multiHeal = @($healActions | Where-Object {
        $entry = $_
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        @($beforeRoom.Units | Where-Object {
            $beforeUnit = $_
            $afterUnit = $afterRoom.Units | Where-Object Id -EQ $beforeUnit.Id
            $beforeUnit.Team -eq 1 -and $null -ne $afterUnit -and $afterUnit.MaxHealth -eq $beforeUnit.MaxHealth + 4 -and
                $afterUnit.Health -eq $afterUnit.MaxHealth
        }).Count -ge 2
    }).Count -gt 0
    $multiDamage = @($healActions | Where-Object {
        $entry = $_
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        @($beforeRoom.Units | Where-Object {
            $beforeUnit = $_
            $afterUnit = $afterRoom.Units | Where-Object Id -EQ $beforeUnit.Id
            $beforeUnit.Team -eq 0 -and ($null -eq $afterUnit -or $afterUnit.Health -lt $beforeUnit.Health)
        }).Count -ge 2
    }).Count -gt 0
    $roomSpellCoverage = $multiHeal -and $multiDamage
}
$damagePerHpMode = $damagePerHpModes.Count -gt 0
$damagePerHpCoverage = -not $damagePerHpMode
if ($damagePerHpMode) {
    $damagePerHpActions = @($trace.Actions | Where-Object {
        $entry = $_
        $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
        $effect = @($rule.Effects | Where-Object Type -EQ 'DamagePerHp')
        if ($effect.Count -ne 1 -or $entry.Action.TargetUnitId -le 0) { return $false }
        $selected = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        if ($null -eq $selected) { return $false }
        $armor = @($selected.Statuses | Where-Object { $_.Id -eq 'armor' -and $_.Stacks -gt 0 }).Count -gt 0
        $lastAttacker = $selected.LastAttackerId -gt 0
        $effect[0].DamagePerHpIncludeArmor -eq [bool]$DamagePerHpArmor -and
            $effect[0].DamagePerHpLastAttacker -eq [bool]$DamagePerHpLastAttacker -and
            (-not $DamagePerHpArmor -or $armor) -and (-not $DamagePerHpLastAttacker -or $lastAttacker)
    })
    $damagePerHpCoverage = $trace.ModifierScenario -eq $damagePerHpModes[0] -and $damagePerHpActions.Count -gt 0
}
$damagePerHpAbilityCoverage = -not $DamagePerHpAbility
if ($DamagePerHpAbility) {
    $positiveAbilityHits = @($trace.Actions | Where-Object {
        $entry = $_
        $action = $entry.Action
        if ($action.ActivatorUnitId -le 0) { return $false }
        $actor = @($entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $action.ActivatorUnitId)[0]
        if ($null -eq $actor -or $actor.LastAttackerId -le 0) { return $false }
        $rule = @($entry.Before.PlayRules.Cards | Where-Object DataId -EQ $actor.Ability.DataId)[0]
        $effect = @($rule.Effects | Where-Object Type -EQ 'DamagePerHp')
        $attacker = @($entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $actor.LastAttackerId)[0]
        $afterAttacker = @($entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $actor.LastAttackerId)[0]
        $effect.Count -eq 1 -and $effect[0].DamagePerHpLastAttacker -and
            $effect[0].Target -eq 'FrontInRoom' -and $null -ne $attacker -and $attacker.Team -eq 0 -and
            ($null -eq $afterAttacker -or $afterAttacker.Health -lt $attacker.Health)
    })
    $damagePerHpAbilityCoverage = $trace.ModifierScenario -eq 'damage-per-hp-ability' -and $positiveAbilityHits.Count -gt 0
}
$damageOverflowCoverage = -not $DamageOverflow
if ($DamageOverflow) {
    $overflowActions = @($trace.Actions | Where-Object {
        $entry = $_
        if ($entry.Action.TargetUnitId -le 0) { return $false }
        $card = @($entry.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $entry.Action.CardInstanceId)[0]
        $trait = @($card.MaskDescriptor.BaseTraits | Where-Object RuntimeType -EQ 'CardTraitDamageOverflow')[0]
        if ($null -eq $trait -or $trait.ParamInt -ne 0) { return $false }
        $room = @($entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex)[0]
        $target = @($room.Units | Where-Object Id -EQ $entry.Action.TargetUnitId)[0]
        if ($null -eq $target -or $target.Health -ge 20) { return $false }
        $siblings = @($room.Units | Where-Object { $_.Team -eq $target.Team -and $_.Id -ne $target.Id })
        if ($siblings.Count -eq 0) { return $false }
        $afterRoom = @($entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex)[0]
        @($siblings | Where-Object {
            $sibling = $_
            $after = @($afterRoom.Units | Where-Object Id -EQ $sibling.Id)[0]
            $null -eq $after -or $after.Health -lt $sibling.Health
        }).Count -gt 0
    })
    $damageOverflowCoverage = $trace.ModifierScenario -eq 'damage-overflow' -and $overflowActions.Count -gt 0
}
$terminalSpellCoverage = -not ($TerminalSpells -or $PostKillSpells) -or @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -gt 0
$postKillCoverage = -not $PostKillSpells
$randomCoverage = -not $RandomSpells -or @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($rule.Effects | Where-Object Target -EQ 'RandomInRoom').Count -gt 0 -and
        (ConvertTo-Json -InputObject $entry.Before.Spawn.Train.Context.BattleRng -Compress) -cne
        (ConvertTo-Json -InputObject $entry.Actual.Spawn.Train.Context.BattleRng -Compress)
}).Count -gt 0
$randomStatusCoverage = -not $RandomStatus -or @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($rule.Effects | Where-Object { $_.Type -eq 'AddStatus' -and $_.Statuses.Count -gt 1 }).Count -gt 0 -and
        $entry.ActualOutcome -eq 3 -and @($entry.Before.Spawn.Train.Rooms.Units | Where-Object {
            @($_.Statuses | Where-Object Id -EQ 'immune').Count -gt 0 -and -not $_.IsPyre
        }).Count -gt 0
}).Count -gt 0
$crossRoomCoverage = -not ($CrossRoomSpells -or $CrossRoomTargets) -or @($trace.Actions | Where-Object {
    $entry = $_
    (-not $CrossRoomSpells -or $entry.ActualOutcome -eq 3) -and @($entry.Before.Spawn.Train.Rooms | Where-Object {
        $oldRoom = $_
        $newRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $oldRoom.RoomIndex
        $oldRoom.RoomIndex -ne $entry.Action.RoomIndex -and @($oldRoom.Units | Where-Object {
            $old = $_
            $next = $newRoom.Units | Where-Object Id -EQ $old.Id
            $old.Team -eq 1 -and -not $old.IsPyre -and $null -ne $next -and
                $next.MaxHealth -eq $old.MaxHealth + 6 -and $next.Health -eq $next.MaxHealth
        }).Count -gt 0
    }).Count -gt 0
}).Count -gt 0
$attackCoverage = -not $AttackBuffs
if ($AttackBuffs) {
    $attackActions = @($trace.Actions | Where-Object {
        $entry = $_
        $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
        $rule.Effects[0].Type -eq 'BuffAttack' -and $rule.Effects[0].Target -eq 'Tower'
    })
    $remoteAttack = $false
    $incapableAttack = $false
    $zeroAttack = $false
    foreach ($entry in $attackActions) {
        foreach ($room in $entry.Before.Spawn.Train.Rooms) {
            foreach ($old in $room.Units) {
                $next = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $old.Id
                if ($old.IsPyre -or $null -eq $next) { continue }
                if ($old.Team -eq 0 -and -not $old.CanAttack -and $next.Modifiers.DamageBuff -eq $old.Modifiers.DamageBuff) { $incapableAttack = $true }
                if ($old.CanAttack -and $old.BaseAttack -eq 0 -and $next.BaseAttack -gt 0) { $zeroAttack = $true }
                if ($room.RoomIndex -ne $entry.Action.RoomIndex -and $old.Team -eq 1 -and $old.CanAttack -and
                    ($next.Modifiers.DamageBuff - $old.Modifiers.DamageBuff) -in @(3, 4)) { $remoteAttack = $true }
            }
        }
    }
    $attackCoverage = $attackActions.Count -gt 0 -and $remoteAttack -and $incapableAttack -and $zeroAttack
}
$maxHealthCoverage = -not ($MaxHealthSpells -or $MaxHealthLethal)
if ($MaxHealthSpells -or $MaxHealthLethal) {
    $maxHealthActions = @($trace.Actions | Where-Object {
        $entry = $_
        $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
        @($rule.Effects | Where-Object Type -EQ 'BuffHealth').Count -gt 0
    })
    $maxHealthRemote = $false
    $maxHealthDeath = $false
    $maxHealthSpawner = $false
    $maxHealthNoTrigger = $false
    foreach ($entry in $maxHealthActions) {
        foreach ($room in $entry.Before.Spawn.Train.Rooms) {
            foreach ($old in $room.Units) {
                $next = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $old.Id
                if ($old.IsPyre) { continue }
                if ($old.Team -eq 0 -and $null -eq $next) { $maxHealthDeath = $true }
                if ($old.Team -ne 1 -or $null -eq $next) { continue }
                if ($room.RoomIndex -ne $entry.Action.RoomIndex -and $next.MaxHealth -gt $old.MaxHealth) { $maxHealthRemote = $true }
                $oldCard = $entry.Before.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
                $nextCard = $entry.Actual.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
                if ($nextCard.Temporary.Offsets.Health - $oldCard.Temporary.Offsets.Health -eq $(if ($MaxHealthLethal) { 4 } else { 2 })) {
                    $maxHealthSpawner = $true
                }
                $maxHealthHealingTriggers = @($next.Triggers | Where-Object Kind -EQ 'OnHeal')
                if ($maxHealthHealingTriggers.Count -gt 0 -and @($maxHealthHealingTriggers | Where-Object HasTriggered -EQ $true).Count -eq 0) { $maxHealthNoTrigger = $true }
            }
        }
    }
    $maxHealthCoverage = $maxHealthActions.Count -gt 0 -and $maxHealthRemote -and $maxHealthDeath -and $maxHealthSpawner -and $maxHealthNoTrigger -and
        (-not $MaxHealthLethal -or @($maxHealthActions | Where-Object ActualOutcome -EQ 3).Count -gt 0)
}
if ($PostKillSpells) {
    $postKillCoverage = @($trace.Actions | Where-Object {
        $entry = $_
        if ($entry.ActualOutcome -ne 3) { return $false }
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $entry.Actual.Spawn.Train.Context.AllScenarioBossesDead -eq $true -and @($beforeRoom.Units | Where-Object {
            $old = $_
            $next = $afterRoom.Units | Where-Object Id -EQ $old.Id
            $card = $entry.Actual.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
            $old.Team -eq 1 -and $null -ne $next -and $next.MaxHealth -eq $old.MaxHealth + 4 -and
                $next.Health -eq $next.MaxHealth -and
                @($card.Permanent.Upgrades | Where-Object AssetKey -EQ 'PojuPostKillPermanent').Count -gt 0
        }).Count -ge 2
    }).Count -gt 0
}
if ($HandUpgrades -or $TargetedHandUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        @($_.Actual.Spawn.Train.Context.CardInstances.Permanent.Upgrades | Where-Object AssetKey -Like 'PojuHand*').Count -gt 0
    }).Count -gt 0
} elseif ($SacrificeUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        $modifierAction = $_
        @($modifierAction.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $modifierAction.Action.TargetUnitId).Count -eq 0
    }).Count -gt 0
} elseif ($DynamicUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        @($_.Actual.Spawn.Train.Rooms.Units.Modifiers.Upgrades | Where-Object AssetKey -Like 'PojuProbe*').Count -gt 0
    }).Count -gt 0
}
if ($PermanentConsumeHandUpgrades -or $PermanentConsumeOverTemporary) {
    $modifierCoverage = $modifierCoverage -and @($trace.Actions | Where-Object {
        $actionEntry = $_
        $cardId = $actionEntry.Action.CardInstanceId
        $card = $actionEntry.Actual.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $cardId
        $exhausted = $actionEntry.Actual.OtherPiles | Where-Object Name -EQ 'Exhausted' | ForEach-Object Cards |
            Where-Object InstanceId -EQ $cardId
        $null -ne $card -and
            (($PermanentConsumeOverTemporary -and $card.ExhaustState.BaseTraits.Count -eq 0 -and
                $null -ne $card.ExhaustState.UpgradeTrait -and $card.ExhaustState.UpgradeTrait.IsExhausted -and
                @($card.Temporary.Upgrades | Where-Object AssetKey -EQ 'PojuHandTemporaryConsume').Count -gt 0) -or
             (-not $PermanentConsumeOverTemporary -and $card.ExhaustState.BaseTraits.Count -gt 0)) -and
            @($card.Permanent.Upgrades | Where-Object AssetKey -EQ 'PojuHandPermanentConsume').Count -gt 0 -and
            @($exhausted).Count -gt 0
    }).Count -gt 0
}
$numericRangeCoverage = -not ($NumericRanges -or $NumericRangesLethal) -or (@($trace.NumericRanges | Where-Object Phase -EQ 'Cast').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Test').Count -gt 0 -and @($trace.NumericRanges | Where-Object Phase -EQ 'Apply').Count -gt 0)
$targetFilterCoverage = -not $TargetFilters -or (@($trace.FilteredTargets | Where-Object Mode -EQ 'DropTargetCharacter').Count -gt 0 -and @($trace.FilteredTargets | Where-Object Mode -EQ 'Tower').Count -gt 0)
$drawCoverage = -not $Drawing -or (@($trace.CardCycles | Where-Object Kind -EQ 'SpellDraw').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Cast').Count -gt 0 -and @($trace.NumericRanges | Where-Object Phase -EQ 'Test').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Apply').Count -gt 0)
$handRemovalCoverage = -not ($HandRemoval -or $HandRemovalLethal) -or (@($trace.HandRemovals | Where-Object Mode -EQ 0).Count -gt 0 -and
    @($trace.HandRemovals | Where-Object Mode -EQ 1).Count -gt 0 -and @($trace.HandRemovals | Where-Object Difference -NE $null).Count -eq 0)
$randomDiscardCoverage = -not $RandomDiscard -or (@($trace.RandomDiscards).Count -gt 0 -and
    @($trace.RandomDiscards | Where-Object { $_.Difference -ne $null -or $null -eq $_.Actual -or $_.Predicted.RemovedCards.Count -eq 0 }).Count -eq 0)
$generationCoverage = -not ($Generation -or $GenerationLethal) -or (@($trace.CardGenerations | ForEach-Object { $_.Rule.Destination } | Sort-Object -Unique).Count -eq 5 -and
    @($trace.CardGenerations | Where-Object { $_.Predicted.AddedCards.Count -gt 0 }).Count -gt 0 -and
    @($trace.CardGenerations | Where-Object Difference -NE $null).Count -eq 0)
if ($Generation -and -not $GenerationLethal) {
    $generationCoverage = $generationCoverage -and @($trace.CardGenerations | Where-Object Origin -EQ 'Unit').Count -gt 0
}
$permanentConsumeGenerationCoverage = -not $GenerationPermanentConsume
if ($GenerationPermanentConsume) {
    $permanentConsumeGenerationCoverage = @($trace.CardGenerations | Where-Object {
        $entry = $_
        if (-not $entry.Rule.CopyModifiers) { return $false }
        $source = $entry.Before.CardInstances | Where-Object InstanceId -EQ $entry.SourceCardId
        if (@($source.Permanent.Upgrades | Where-Object AssetKey -EQ 'PojuGenerationPermanentConsume').Count -eq 0) { return $false }
        foreach ($token in $entry.Predicted.AddedCards) {
            $copy = $entry.Actual.CardInstances | Where-Object InstanceId -EQ $token.InstanceId
            if ($null -ne $copy -and @($copy.Permanent.Upgrades | Where-Object AssetKey -EQ 'PojuGenerationPermanentConsume').Count -gt 0 -and
                @($copy.ExhaustState.BaseTraits | Where-Object { -not $_.IsExhausted -and $null -eq $_.ExhaustedCardId }).Count -gt 0) { return $true }
        }
        return $false
    }).Count -gt 0
}
$scalingCoverage = -not ($DamageScaling -or $DynamicStatistics)
if ($DamageScaling -or $DynamicStatistics) {
    $samples = @($trace.DamageScaling)
    $scalingCoverage = $samples.Count -gt 0 -and @($samples | Where-Object DamageSourceCardId -GT 0).Count -gt 0 -and
        @($samples | Where-Object DamageSourceCardId -EQ 0).Count -gt 0 -and
        @($samples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0
}
if ($DynamicStatistics) {
    $queryTypes = @($samples | ForEach-Object { $_.Trait.Query.Type } | Sort-Object -Unique)
    foreach ($queryType in @('Gold', 'TurnCount', 'MoonPhase', 'ForgePoints', 'DragonsHoardAmount', 'EnergyRemainingEndOfTurn', 'PyreHeartResurrection')) {
        $scalingCoverage = $scalingCoverage -and $queryTypes.Contains($queryType)
    }
    $scalingCoverage = $scalingCoverage -and
        @($samples | Where-Object { $_.Before.Statistics.EnergyRemainingEndOfTurn -gt 0 }).Count -gt 0 -and
        @($samples | Where-Object { $_.Before.Gold -ne $_.Before.Statistics.GoldStartOfThisTurn }).Count -gt 0 -and
        @($samples | ForEach-Object { $_.Before.QueryFrame.MoonPhase } | Sort-Object -Unique).Count -eq 2
}
$statusScalingCoverage = -not $StatusScaling
if ($StatusScaling) {
    $statusSamples = @($trace.StatusScaling)
    $applications = @($trace.StatusApplications)
    $statusScalingCoverage = $statusSamples.Count -ge 60 -and $applications.Count -gt 0 -and
        @($statusSamples | ForEach-Object { $_.Trait.Filter } | Sort-Object -Unique).Count -eq 3 -and
        @($statusSamples | Where-Object ActualBonus -LT 0).Count -gt 0 -and
        @($statusSamples | Where-Object ActualBonus -GT 0).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.Filter -eq 1 -and $_.ActualBonus -gt 0 }).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.OnlyWhenSourceZero -and $_.SourceStacks -eq 0 -and $_.ActualBonus -gt 0 }).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.OnlyWhenSourceZero -and $_.SourceStacks -ne 0 -and $_.ActualBonus -eq 0 }).Count -gt 0 -and
        @($statusSamples | ForEach-Object { $_.Before.QueryFrame.MoonPhase } | Sort-Object -Unique).Count -eq 2 -and
        @($statusSamples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0 -and
        @($applications | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0 -and
        @($applications | Where-Object { @($_.After.Units.Statuses | Where-Object Stacks -EQ 9999).Count -gt 0 }).Count -gt 0
}
$unitUpgradeScalingCoverage = -not $UnitUpgradeScaling
if ($UnitUpgradeScaling) {
    $upgradeSamples = @($trace.UnitUpgradeScaling)
    $unitUpgradeScalingCoverage = $trace.UnitUpgradeScalingCalibrationContextUnchanged -and
        @($upgradeSamples | Where-Object Origin -EQ 'Calibration').Count -eq 30 -and
        @($upgradeSamples | Where-Object Origin -EQ 'Live').Count -ge 35 -and
        @($upgradeSamples | ForEach-Object { $_.Trait.Stat } | Sort-Object -Unique).Count -eq 2 -and
        @($upgradeSamples | Where-Object TriggerKind -EQ 'OnSpawn').Count -gt 0 -and
        @($upgradeSamples | Where-Object TriggerKind -EQ 'OnHeal').Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.BeforeUpgrade.MagicPowerTraitScalingOnly }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.AfterUpgrade.Stats.Damage -lt $_.BeforeUpgrade.Stats.Damage }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.AfterUpgrade.Stats.Health -gt $_.BeforeUpgrade.Stats.Health }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0
}
$unitTriggerUpgradeCoverage = -not $UnitTriggerUpgrades
if ($UnitTriggerUpgrades) {
    $triggerSamples = @($trace.UnitUpgradeScaling)
    $unitTriggerUpgradeCoverage = $triggerSamples.Count -ge 30 -and
        @($triggerSamples | Where-Object TriggerKind -EQ 'OnHeal').Count -gt 0 -and
        @($triggerSamples | Where-Object TriggerKind -EQ 'PostCombat').Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.Trait.Restriction -eq 1 }).Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.BeforeUpgrade.MagicPowerTraitScalingOnly }).Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0
}
$preHandDiscardCoverage = -not ($PreHandDiscard -or $PreHandDiscardLethal -or $CloneUpgradeRefresh)
if ($PreHandDiscard -or $PreHandDiscardLethal -or $CloneUpgradeRefresh) {
    $prePhases = @($trace.PreHandDiscards)
    $preHandDiscardCoverage = $prePhases.Count -gt 0 -and
        @($prePhases | Where-Object Team -EQ 1).Count -gt 0 -and @($prePhases | Where-Object Team -EQ 0).Count -gt 0 -and
        @($prePhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'EndTurnPreHandDiscard').Count -ge 4 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.Destination -eq 'HandPile' }).Count -gt 0
}
$preCombatCoverage = -not ($PreCombat -or $PreCombatLethal -or $TriggeredHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage)
if ($PreCombat -or $PreCombatLethal -or $TriggeredHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage) {
    $prePhases = @($trace.PreCombats)
    $preCombatCoverage = $prePhases.Count -gt 0 -and
        @($prePhases | Where-Object Team -EQ 1).Count -gt 0 -and @($prePhases | Where-Object Team -EQ 0).Count -gt 0 -and
        @($prePhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'PreCombat').Count -ge 4 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.Destination -eq 'HandPile' }).Count -gt 0
}
$triggeredHealingCoverage = -not $TriggeredHealing
if ($TriggeredHealing) {
    $heals = @($trace.TriggeredHeals)
    $triggeredHealingCoverage = $heals.Count -gt 0 -and
        @($heals | Where-Object { $_.Difference -or -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($heals | Where-Object { $_.Effect.Target -eq 'RandomInRoom' -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($heals | Where-Object { $_.Effect.Target -eq 'Room' -and $_.Targets.Count -eq 0 -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($heals | Where-Object { $_.Targets.Count -ge 2 }).Count -gt 0 -and
        @($heals.Requests | Where-Object { $_.AfterHealth -gt $_.Health }).Count -gt 0 -and
        @($heals.Requests | Where-Object Amount -LT 0).Count -gt 0 -and
        @($heals.Requests | Where-Object Amount -EQ 0).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.TriggerKind -eq 'OnHeal' -and $_.BeforeUpgrade.AssetKey -eq 'PojuOnHealDeferredMaxHealth' }).Count -gt 0
}
$postCombatHealingCoverage = -not $PostCombatHealing
if ($PostCombatHealing) {
    $postPhases = @($trace.UnitPostCombats)
    $postCombatHealingCoverage = $postPhases.Count -gt 0 -and
        @($postPhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.TriggeredHeals | Where-Object TriggerKind -EQ 'PostCombatHealing').Count -gt 0 -and
        @($postPhases | Where-Object { $_.CannotAttackOrHeal.Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'PostCombat').Count -gt 0
}
$triggeredDamageCoverage = -not $TriggeredDamage
if ($TriggeredDamage) {
    $damageRecords = @($trace.TriggeredDamage)
    $applications = @($damageRecords | Where-Object Stage -EQ 'Application')
    $triggeredDamageCoverage = $applications.Count -gt 0 -and
        @($damageRecords | Where-Object { $_.Difference -or -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($applications | Where-Object { $_.Effect.Target -eq 'RandomInRoom' -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($applications | Where-Object { $_.Targets.Count -eq 0 -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($applications | Where-Object { $_.Targets.Count -ge 2 }).Count -gt 0 -and
        @($applications | Where-Object { $null -ne $_.StatusMultiplier -and $_.MultiplierStacks -gt 0 }).Count -gt 0 -and
        @($applications | Where-Object TriggerKind -EQ 'OnDeath').Count -gt 0 -and
        @($damageRecords | Where-Object { $_.Stage -eq 'Test' -and $_.Amount -lt 0 -and -not $_.TestPassed }).Count -gt 0 -and
        @($applications.Requests | Where-Object { -not $_.AttackerPreserved -or -not $_.DefaultDamage }).Count -eq 0
}
$damageDeathQueueCoverage = -not ($DamageDeathQueue -or $TerminalDeathDamage)
if ($DamageDeathQueue -or $TerminalDeathDamage) {
    $deathDamage = @($trace.TriggeredDamage | Where-Object { $_.Stage -eq 'Application' -and $_.TriggerKind -eq 'OnDeath' })
    $damageDeathQueueCoverage = $deathDamage.Count -gt 0 -and @($trace.PreCombats).Count -ge 2 -and
        @($deathDamage.Requests | Where-Object { @($_.Before.Modifiers.Upgrades | Where-Object AssetKey -EQ 'PojuOnHealBeforeDeathArmor').Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.TriggerKind -eq 'OnHeal' -and $_.BeforeUpgrade.AssetKey -eq 'PojuOnHealBeforeDeathArmor' }).Count -gt 0
}
$cloneUpgradeRefreshCoverage = -not $CloneUpgradeRefresh
$triggeredStatusCoverage = -not $TriggeredStatus
if ($TriggeredStatus) {
    $status = @($trace.TriggeredStatuses)
    $previewRng = @($trace.PreviewRngIsolation)
    $triggeredStatusCoverage = $previewRng.Count -gt 0 -and
        @($previewRng | Where-Object { -not $_.Completed -or ($_.BattleBefore | ConvertTo-Json -Compress) -ne ($_.BattleAfter | ConvertTo-Json -Compress) -or ($_.TestBefore | ConvertTo-Json -Compress) -ne ($_.TestAfter | ConvertTo-Json -Compress) }).Count -eq 0 -and
        $status.Count -gt 10 -and
        @($status | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnits -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($status | Where-Object { $_.Targets.Count -eq 0 }).Count -gt 0 -and
        @($status | Where-Object { $_.Targets.Count -gt 1 }).Count -gt 0 -and
        @($status | Where-Object { $_.Effect.Action.Statuses.Count -gt 1 }).Count -gt 0 -and
        @($status | Where-Object { $_.Effect.StatusScaling.MissingHealth -and $_.Effect.StatusScaling.MagicPower }).Count -gt 0 -and
        @($status | Where-Object { @($_.BeforeUnits | Where-Object Health -EQ 0).Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$statusCallbackCoverage = -not $StatusCallbacks
if ($StatusCallbacks) {
    $callbackFires = @($trace.StatusCallbackFires)
    $statusCallbackCoverage = $callbackFires.Count -gt 20 -and $callbackFires.Count -eq @($trace.StatusCallbacks).Count -and
        @($callbackFires | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnit -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($callbackFires | Where-Object { $_.Kind -eq 'OnSilenceLost' -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.Kind -eq 'OnStatusEffectChanged' -and $_.ParamString -eq 'armor' -and $_.ParamInt2 -eq 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.BeforeUnit.Health -eq 0 }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.BeforeUnit.Team -eq 0 -or $_.BeforeUnit.Team -eq 'Enemy' }).Count -gt 0
    foreach ($callbackKind in 'OnStatusEffectChanged','OnArmorAdded','OnPyregelAdded','OnValiant','OnSilence','OnSilenceLost','OnNewStatusEffectAdded') {
        $statusCallbackCoverage = $statusCallbackCoverage -and @($callbackFires | Where-Object { $_.Kind -eq $callbackKind }).Count -gt 0
    }
}
$attackTriggerCoverage = -not $AttackTriggers
if ($AttackTriggers) {
    $attackFires = @($trace.AttackTriggers | Where-Object Stage -EQ 'Fire')
    $attackTriggerCoverage = $attackFires.Count -gt 10 -and
        @($attackFires | Where-Object Kind -EQ 'OnAttackingBeforeDamage').Count -gt 0 -and
        @($attackFires | Where-Object Kind -EQ 'OnAttacking').Count -gt 0 -and
        @($attackFires | Where-Object { $_.Health -eq 0 -and -not $_.DeadBoss }).Count -gt 0 -and
        @($trace.AttackTriggers | Where-Object { $_.Stage -eq 'Queue' -and $_.QueueRunning }).Count -gt 0 -and
        @($trace.AttackTriggers | Where-Object { -not $_.Completed -or $null -eq $_.GoldAfter -or $null -eq $_.AfterTriggered -or $_.OverrideTargetId -le 0 -or $_.ParamInt -ne 0 }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $_.AttackedTargetIds.Count -gt 1 }).Count -gt 0
}
$dyingUpgradeCoverage = -not $DyingUpgrades
if ($DyingUpgrades) {
    $dying = @($trace.DyingUpgrades | Where-Object { @($_.BeforeUnits | Where-Object Health -EQ 0).Count -gt 0 })
    $dyingUpgradeCoverage = $dying.Count -ge 6 -and
        @($dying | Where-Object Kind -EQ 'OnKill').Count -gt 0 -and
        @($dying | Where-Object Kind -EQ 'OnHit').Count -gt 0 -and
        @($dying | Where-Object Kind -EQ 'OnDeath').Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Type -eq 'RemoveUnitUpgrade' }).Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Type -ne 'RemoveUnitUpgrade' -and $_.Effect.Upgrade.Stats.Health -lt 0 -and
            $_.ActualUnits[0].MaxHealth -lt $_.BeforeUnits[0].MaxHealth }).Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Upgrade.UnhealedHealth -lt 0 }).Count -gt 0 -and
        @($trace.DyingUpgrades | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnits -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$hitKillCoverage = -not $HitKill
if ($HitKill) {
    $fires = @($trace.HitKills | Where-Object Stage -EQ 'Fire')
    $hitKillCoverage = $fires.Count -ge 10 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.Health -gt 0 -and $_.ParamInt -eq 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.Health -eq 0 -and -not $_.DeadBoss -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.ParamInt -ge 2 }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnKill' -and $_.DyingId -gt 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($trace.HitKills | Where-Object { $_.Stage -eq 'Queue' -and $_.QueueRunning }).Count -gt 0 -and
        @($trace.UnitTurns | Where-Object { $_.AttackedTargetIds.Count -gt 1 }).Count -gt 0 -and
        @($trace.HitKills | Where-Object { -not $_.Completed -or $null -eq $_.GoldAfter -or $null -eq $_.AfterTriggered }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$terminalDeathDamageCoverage = -not $TerminalDeathDamage
if ($TerminalDeathDamage) {
    $terminalRecords = @($trace.TerminalDeaths | Where-Object Terminal)
    $postClearDeaths = @($trace.TerminalDeaths | Where-Object { $_.Before.KillCamActivated -and $_.Team -eq 1 })
    $terminalDeathDamageCoverage = $terminalRecords.Count -gt 0 -and $postClearDeaths.Count -gt 0 -and @($trace.KillCams).Count -gt 0 -and
        @($trace.KillCams | Where-Object { -not $_.Completed -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.TerminalDeaths | Where-Object { -not $_.Completed -or -not $_.FinishedDying -or $null -eq $_.Actual -or $_.Sacrifice }).Count -eq 0 -and
        @($terminalRecords | Where-Object { -not $_.Actual.KillCamActivated -or $_.Actual.CardInstances.Count -ne 0 }).Count -eq 0 -and
        @($trace.TriggeredDamage.Requests | Where-Object { $_.Context.KillCamActivated -and $_.Context.CardInstances.Count -eq 0 -and $_.AfterHealth -eq 0 }).Count -gt 0
}
if ($CloneUpgradeRefresh) {
    $cloneUpgradeRefreshCoverage = @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.CopyModifiers }).Count -ge 2 -and
        @($trace.CardGenerations | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0
}
$teamTurnBeginCoverage = -not $TeamTurnBegin
if ($TeamTurnBegin) {
    $teamSamples = @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'OnTeamTurnBegin')
    $teamPhases = @($trace.TeamTurnBegins)
    $teamTurnBeginCoverage = $teamSamples.Count -ge 6 -and $teamPhases.Count -gt 0 -and
        @($teamPhases | Where-Object Team -EQ 0).Count -gt 0 -and @($teamPhases | Where-Object Team -EQ 1).Count -gt 0 -and
        @($teamPhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitTurns).Count -gt 0 -and @($trace.UnitTurns | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0
}
$unitTurnBeginCoverage = -not $UnitTurnBegin
if ($UnitTurnBegin) {
    $turnSamples = @($trace.UnitUpgradeScaling)
    $unitTurnBeginCoverage = $turnSamples.Count -ge 12 -and
        @($turnSamples | Where-Object TriggerKind -NE 'OnTurnBegin').Count -eq 0 -and
        @($turnSamples | Where-Object { $_.Trait.Restriction -eq 1 }).Count -gt 0 -and
        @($turnSamples | Where-Object { $_.Trait.Query.Type -eq 'TurnCount' -and $_.AfterUpgrade.Stats.Damage -gt $_.BeforeUpgrade.Stats.Damage }).Count -gt 0 -and
        @($turnSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.UnitTurns).Count -gt 0 -and @($trace.UnitTurns | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0
}
$spawnTriggerCoverage = -not ($SpawnTriggers -or $SpawnTriggersLethal)
if ($SpawnTriggers -or $SpawnTriggersLethal) {
    $spawnSamples = @($trace.UnitUpgradeScaling)
    $spawnTriggerCoverage = @($spawnSamples | Where-Object TriggerKind -EQ 'OnSpawn').Count -ge 8 -and
        @($spawnSamples | Where-Object { $_.TriggerKind -eq 'OnSpawn' -and $_.Trait.Query.Type -eq 'AnyMonsterSpawned' -and $_.AfterUpgrade.Stats.Health -gt $_.BeforeUpgrade.Stats.Health }).Count -gt 0 -and
        @($spawnSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.SourceCardId -gt 0 -and $_.Rule.Count -gt 0 }).Count -gt 0
    if (-not $SpawnTriggersLethal) {
        $spawnTriggerCoverage = $spawnTriggerCoverage -and @($spawnSamples | Where-Object TriggerKind -EQ 'OnUnscaledSpawn').Count -gt 0
    }
}
$energyCoverage = $true
if ($EnergyEffects -or $EnergyEffectsLethal) {
    $energySamples = @($trace.EnergyEffects)
    $energyCoverage = $energySamples.Count -gt 0 -and @($energySamples.Effect.Type | Sort-Object -Unique).Count -eq 5 -and
        @($energySamples | Where-Object { -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($energySamples | Where-Object Amount -EQ 0).Count -gt 0 -and @($energySamples | Where-Object Amount -LT 0).Count -gt 0
    if ($EnergyEffectsLethal) {
        $energyCoverage = $energyCoverage -and @($trace.Actions | Where-Object { $_.Actual.Spawn.Train.Context.AllScenarioBossesDead -eq $true }).Count -eq 1
    }
}
$forgePointsCoverage = -not $ForgePointsEffects
if ($ForgePointsEffects) {
    $forgePointActions = @($trace.Actions | Where-Object {
        $instance = @($_.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $_.Action.CardInstanceId)[0]
        $instance.DataId -eq 'd14a50f3-728d-43e1-87f0-ef1b013f6678' -and $_.Predicted.Supported
    })
    $forgePointsCoverage = $forgePointActions.Count -gt 0 -and
        @($forgePointActions | Where-Object { $_.Before.ForgePoints -ne $_.Actual.ForgePoints }).Count -gt 0
    if (-not $forgePointsCoverage) { throw 'Forge Points effect did not change the native battle state through a card action.' }
}
$xCostCoverage = $true
if ($XCost -or $XCostLethal) {
    $xCasts = @($trace.Actions | Where-Object {
        $cardId = $_.Action.CardInstanceId
        $instance = $_.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $cardId
        $definition = $_.Before.PlayRules.Cards | Where-Object DataId -EQ $instance.DataId
        $definition.CostType -eq 'ConsumeRemainingEnergy'
    })
    $xCostCoverage = $xCasts.Count -gt 0 -and @($xCasts | Where-Object { $_.Before.Energy -eq 0 }).Count -gt 0 -and
        @($xCasts | Where-Object { $_.Before.Energy -gt 0 }).Count -gt 0 -and
        @($trace.DamageScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.DamageScaling.Trait.Query.Type | Sort-Object -Unique).Count -eq 2
    if ($XCostLethal) { $xCostCoverage = $xCostCoverage -and @($xCasts | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$bonusDrawCoverage = $true
if ($BonusDraw -or $BonusDrawLethal) {
    $bonusSamples = @($trace.BonusDrawEffects)
    $bonusDrawCoverage = $bonusSamples.Count -gt 0 -and @($bonusSamples | Where-Object { -not $_.Completed -or -not $_.Sampled -or $null -eq $_.After }).Count -eq 0 -and
        @($bonusSamples | Where-Object Amount -LT 0).Count -gt 0 -and @($bonusSamples | Where-Object Amount -EQ 0).Count -gt 0 -and
        @($bonusSamples | Where-Object { $_.Key -like 'unit:*' }).Count -gt 0 -and @($bonusSamples | Where-Object { $null -ne $_.Effect.Range }).Count -gt 0
    if ($BonusDrawLethal) { $bonusDrawCoverage = $bonusDrawCoverage -and @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$directUpgradeCoverage = -not $DirectUnitUpgrades -or (@($trace.DirectUnitUpgrades).Count -ge 21 -and @($trace.DirectUnitUpgrades | Where-Object { $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0)
$equipmentCoverage = -not $Equipment -or (@($trace.EquipmentOperations).Count -gt 3 -and
    @($trace.EquipmentOperations | Where-Object { -not $_.Completed -or $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0 -and
    @($trace.EquipmentOperations | Where-Object { $_.Remove -and $_.CardId -eq 0 }).Count -gt 0)
$capacityCoverage = $true
if ($RoomCapacity -or $RoomCapacityLethal) {
    $capacitySamples = @($trace.CapacityEffects)
    $capacityCoverage = $capacitySamples.Count -gt 0 -and @($capacitySamples | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -eq 0 -and
        @($capacitySamples | Where-Object { $_.Effect.Value -lt 0 }).Count -gt 0 -and @($capacitySamples | Where-Object { $_.Effect.Value -eq 0 }).Count -gt 0 -and
        @($capacitySamples | Where-Object SourceCardId -EQ 0).Count -gt 0 -and @($capacitySamples | Where-Object { $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($trace.CapacityTests | Where-Object { -not $_.Actual }).Count -gt 0
    if ($RoomCapacityLethal) { $capacityCoverage = $capacityCoverage -and @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$roomCapacityAttachmentCoverage = -not $RoomCapacityAttachment
if ($RoomCapacityAttachment) {
    $attachment = $trace.RoomCapacityAttachmentCoverage
    $roomCapacityAttachmentCoverage = $trace.Schema -eq 139 -and $trace.ModifierScenario -eq 'room-capacity-attachment' -and
        $attachment.Prepared -and $attachment.Selected -and $attachment.Completed -and $null -eq $attachment.Error -and
        $attachment.AssetKey -eq 'IncreaseRoomCapacity' -and $attachment.AfterCapacity -eq ($attachment.BeforeCapacity + 4) -and
        $attachment.ActiveAttachmentCount -eq 1 -and $attachment.AttachedCardDataId -eq $attachment.CardDataId
    if (-not $roomCapacityAttachmentCoverage) { throw 'Native room-capacity attachment was not played and retained.' }
}
$companionCoverage = -not $CompanionBoss
if ($CompanionBoss) {
    $companionCoverage = @($trace.CompanionBossActions).Count -gt 0 -and @($trace.RelentlessTriggerRemovals).Count -eq 1 -and
        @($trace.RelentlessTriggerRemovals[0].Before.Triggers | Where-Object RemoveOnRelentlessChange -EQ $true).Count -eq 4 -and
        @($trace.RelentlessTriggerRemovals[0].Actual.Triggers | Where-Object RemoveOnRelentlessChange -EQ $true).Count -eq 0
    if (-not $companionCoverage) { throw 'The requested companion Boss transition/removal did not execute.' }
}
$sentryCoverage = -not ($Sentry -or $SentryLethal)
if ($Sentry -or $SentryLethal) {
    $sentryCoverage = @($trace.Sentries).Count -gt 0 -and @($trace.Sentries | Where-Object { $_.Target.IsBoss }).Count -gt 0
    if ($SentryLethal) {
        $sentryCoverage = $sentryCoverage -and @($trace.Sentries | Where-Object { $_.Target.Health -eq 0 -and $_.ActualTarget.Health -eq 0 }).Count -gt 0
    }
    if (-not $sentryCoverage) { throw 'The requested native Sentry callbacks and Boss target did not execute.' }
}
$abilityCoverage = -not $AbilityCooldown
if ($AbilityCooldown) {
    $abilitySamples = @($trace.AbilityCooldownEffects)
    $abilityCoverage = @($abilitySamples | Where-Object { $_.Effect.Type -eq 'ResetCooldown' }).Count -gt 0 -and
        @($abilitySamples | Where-Object { $_.Effect.Type -eq 'AdjustAbilityCooldown' }).Count -gt 0 -and
        @($abilitySamples | Where-Object { $_.Effect.Type -eq 'RemoveStatus' }).Count -gt 0 -and
        @($abilitySamples | Where-Object { @($_.Before.Units | Where-Object { $_.Ability.HasAbility }).Count -gt 0 }).Count -gt 0
    if (-not $abilityCoverage) { throw 'Requested native ability spawn/cooldown/availability effects did not execute.' }
}
$abilityCacheCoverage = -not $AbilityCache
if ($AbilityCache) {
    $cacheOps = @($trace.AbilityCardOperations)
    $cacheAccesses = @($trace.AbilityCardAccesses)
    $shared = @($cacheAccesses | Group-Object -Property { $_.DataId } | Where-Object {
        @($_.Group | ForEach-Object { $_.UnitId } | Sort-Object -Unique).Count -ge 2 -and
        @($_.Group | ForEach-Object { $_.CardId } | Sort-Object -Unique).Count -eq 1
    })
    $abilityCacheCoverage = @($cacheOps | Where-Object Created -EQ $true).Count -gt 0 -and
        @($cacheOps | Where-Object Created -EQ $false).Count -gt 0 -and $shared.Count -gt 0
    $abilityCacheCoverage = $abilityCacheCoverage -and
        @($cacheOps | Where-Object { $_.Created } | ForEach-Object { $_.Creation.DataId } | Sort-Object -Unique).Count -ge 2
    if (-not $abilityCacheCoverage) { throw 'Requested native cache creation, reuse and shared unit identity did not execute.' }
}
$lifecycleCoverage = -not $AbilityLifecycle
if ($AbilityLifecycle) {
    $lifecycle = @($trace.AbilityLifecycleOperations)
    $labels = @('remove-base', 'remove-missing-noop', 'null-definition-noop', 'ordinary-card-noop',
        'assign-base', 'reassign-base', 'zero-spawn', 'equipment-over-zero', 'equipment-replace-keeps-original',
        'restore-zero-with-activation-cooldown', 'equipment-again', 'direct-replace-clears-original',
        'equipment-before-permanent', 'permanent-equipment-restores-original', 'equipment-restricted-noop',
        'equipment-over-modified-duration', 'restore-raw-activation-cooldown', 'permanent-base',
        'permanent-missing-noop', 'explicit-assign-disabled', 'duplicate-permanent-base')
    $lifecycleCoverage = $lifecycle.Count -eq $labels.Count -and
        @($labels | Where-Object { $_ -notin $lifecycle.Label }).Count -eq 0 -and
        @($lifecycle | Where-Object { $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0
    $lastDisabled = @($lifecycle[-1].After.Context.PermanentlyDisabledAbilities)
    $lifecycleCoverage = $lifecycleCoverage -and $lastDisabled.Count -eq 3 -and $lastDisabled[1] -eq $lastDisabled[2]
    $laterSummons = @($trace.Actions | Where-Object { $_.Before.PlayRules.Cards.Effect -contains 'SpawnMonster' } |
        ForEach-Object { $r = $_; $r.Actual.Spawn.Train.Rooms.Units | Where-Object {
            $_.SpawnerCardId -eq $r.Action.CardInstanceId -and $_.AssetKey -like '*Steward*' -and $null -eq $_.Ability } })
    $lifecycleCoverage = $lifecycleCoverage -and $laterSummons.Count -gt 0
    if (-not $lifecycleCoverage) { throw 'Requested ability lifecycle operations, duplicate permanent disable list or naturally disabled summons did not execute.' }
}
$activationCoverage = -not ($AbilityActivation -or $AbilityActivationX -or $AbilityActivationLethal -or $DamagePerHpAbility)
if (-not $activationCoverage) {
    $activations = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 })
    $activationCoverage = $activations.Count -ge 2 -and
        @($activations | ForEach-Object { $_.Action.ActivatorUnitId } | Sort-Object -Unique).Count -ge 2 -and
        @($activations | ForEach-Object { $_.Action.CardInstanceId } | Sort-Object -Unique).Count -eq 1
    $selfHeals = @($activations | Where-Object {
        $actorId = $_.Action.ActivatorUnitId
        $priorActor = @($_.Before.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $actualActor = @($_.Actual.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $armor = @($priorActor.Statuses | Where-Object { $_.Id -eq 'armor' })[0]
        $damagedHealth = $priorActor.Health - [Math]::Max(0, 3 - [int]$armor.Stacks)
        $actualActor.Health -eq [Math]::Min($priorActor.MaxHealth, $damagedHealth + 1) -and $actualActor.Health -gt $damagedHealth -and
            @($actualActor.Triggers | Where-Object { $_.Kind -eq 'OnPreOwnAbilityActivated' -and $_.HasTriggered }).Count -gt 0 -and
            @($actualActor.Triggers | Where-Object { $_.Kind -eq 'OnOwnAbilityActivated' -and $_.HasTriggered }).Count -gt 0
    })
    $activationCoverage = $activationCoverage -and ($DamagePerHpAbility -or $selfHeals.Count -gt 0)
    if ($AbilityActivationX) {
        $activationCoverage = $activationCoverage -and
            @($activations | Where-Object { $_.Before.Energy -eq 0 }).Count -gt 0 -and
            @($activations | Where-Object { $_.Before.Energy -gt 0 -and $_.Actual.Energy -eq 0 }).Count -gt 0
    }
    if ($AbilityActivationLethal) {
        $activationCoverage = $activationCoverage -and @($activations | Where-Object { $_.ActualOutcome -eq 3 }).Count -eq 1 -and
            @($activations | Where-Object {
                @($_.Actual.Spawn.Train.Rooms.Units | Where-Object { $_.Team -eq 0 -and $_.Health -gt 0 }).Count -lt
                @($_.Before.Spawn.Train.Rooms.Units | Where-Object { $_.Team -eq 0 -and $_.Health -gt 0 }).Count
            }).Count -gt 0
    }
    if (-not $activationCoverage) { throw 'Requested native activation, shared card, own callbacks, self healing or payment/death coverage did not execute.' }
}
$teleportCoverage = -not $AbilityTeleport
$sameRoomTeleportMoves = 0
$crossRoomTeleportMoves = 0
if ($AbilityTeleport) {
    $teleportActivations = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 })
    foreach ($activation in $teleportActivations) {
        if ($activation.Action.TargetSpawnPointIndex -lt 0) { continue }
        $unitId = $activation.Action.ActivatorUnitId
        $actorRoom = @($activation.Before.Spawn.Train.Rooms | Where-Object { @($_.Units | Where-Object Id -EQ $unitId).Count -gt 0 })[0]
        $beforePoint = @($activation.Before.Spawn.Train.Context.SpawnPoints.Units | Where-Object UnitId -EQ $unitId)[0].Current
        $afterPoint = @($activation.Actual.Spawn.Train.Context.SpawnPoints.Units | Where-Object UnitId -EQ $unitId)[0].Current
        if ($null -eq $actorRoom -or $null -eq $beforePoint -or $null -eq $afterPoint) { continue }
        if ($actorRoom.RoomIndex -eq $activation.Action.RoomIndex) {
            if ($afterPoint.RoomIndex -eq $actorRoom.RoomIndex -and $afterPoint.Index -ne $beforePoint.Index) { $sameRoomTeleportMoves++ }
        } elseif ($afterPoint.RoomIndex -eq $activation.Action.RoomIndex -and $afterPoint.Index -eq $activation.Action.TargetSpawnPointIndex) {
            $crossRoomTeleportMoves++
        }
    }
    $teleportCoverage = $sameRoomTeleportMoves -gt 0 -and $crossRoomTeleportMoves -gt 0
    if (-not $teleportCoverage) { throw "Requested native unit teleport did not move a unit within and between rooms: same=$sameRoomTeleportMoves cross=$crossRoomTeleportMoves activations=$($teleportActivations.Count)." }
}
$abilityEffectsCoverage = -not $AbilityEffects
if ($AbilityEffects) {
    $effects = @($trace.AbilityEffectOperations)
    $abilityEffectsCoverage = $effects.Count -gt 0 -and @($effects | Where-Object { -not $_.Completed }).Count -eq 0 -and
        @($effects | Where-Object { $_.Effect.Type -eq 'SetUnitAbility' -and @($_.Targets).Count -ge 2 }).Count -gt 0 -and
        @($effects | Where-Object { $_.Effect.Type -eq 'SetUnitAbility' -and $_.RunningTriggerQueue }).Count -gt 0 -and
        @($effects | Where-Object { $_.Effect.Type -eq 'RemoveAbility' -and $_.HasRelicGate }).Count -gt 0
    $activations = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 })
    $replaced = @($activations | Where-Object {
        $actorId = $_.Action.ActivatorUnitId
        $prior = @($_.Before.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $actual = @($_.Actual.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $prior.Ability.DataId -eq 'c2f6ed7f-18ce-4070-b65f-7dd9f5160063' -and
            $actual.Ability.DataId -eq 'c2f6ed7f-18ce-4070-b65f-7dd9f5160071' -and $actual.Ability.Cooldown -eq 1
    })
    $removed = @($activations | Where-Object {
        $actorId = $_.Action.ActivatorUnitId
        $prior = @($_.Before.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $actual = @($_.Actual.Spawn.Train.Rooms.Units | Where-Object { $_.Id -eq $actorId })[0]
        $prior.Ability.DataId -eq 'c2f6ed7f-18ce-4070-b65f-7dd9f5160071' -and $null -eq $actual.Ability
    })
    $disabled = @($activations[-1].Actual.Spawn.Train.Context.PermanentlyDisabledAbilities)
    $abilityEffectsCoverage = $abilityEffectsCoverage -and $activations.Count -eq 4 -and $replaced.Count -eq 2 -and $removed.Count -eq 2 -and
        $disabled.Count -eq 2 -and $disabled[0] -eq 'c2f6ed7f-18ce-4070-b65f-7dd9f5160072' -and $disabled[1] -eq $disabled[0]
    if (-not $abilityEffectsCoverage) { throw 'Requested multi-target ability effects, queued replacement or cached self replacement/removal did not execute.' }
}
$equipmentAbilityCoverage = -not $EquipmentAbilities
if ($TriggerRepeats) {
    $repeatBatches = @($trace.TriggerRepeatBatches)
    $repeatLabels = @('batch-first', 'batch-once-spent', 'batch-silenced', 'batch-fire-blocked',
        'zero-first', 'zero-after', 'negative-first', 'negative-after')
    if ($repeatBatches.Count -ne 8 -or @($repeatLabels | Where-Object { $_ -notin $repeatBatches.Label }).Count -gt 0 -or
        @($repeatBatches | Where-Object { $null -eq $_.After }).Count -gt 0) {
        throw 'Requested native trigger repeat batches did not complete.'
    }
}
if ($PhysicalSpawnPoints) {
    $physical = $trace.Stages[0].Before.Context.SpawnPoints
    if ($null -eq $physical -or @($physical.Groups).Count -eq 0 -or @($trace.PhysicalCompactions).Count -eq 0 -or
        @($trace.PhysicalCompactions | Where-Object { $null -ne $_.Error -or $null -eq $_.After }).Count -gt 0) {
        throw 'Requested complete battle physical point state was not captured.'
    }
}
if ($SpawnPoints) {
    $operations = @($trace.SpawnPointOperations)
    if ($operations.Count -ne 21 -or @($operations | Where-Object {
        -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Difference -or @($_.Queries).Count -ne 3
    }).Count -gt 0) {
        throw 'Requested native physical spawn point operations did not complete.'
    }
}
if ($MultiSummon -and -not $MultiSummonFresh -and -not $MultiSummonAdditional -and -not $MultiSummonPool) {
    $births = @($trace.UnitBirths)
    $clones = @($trace.DetachedCardClones)
    if ($births.Count -lt 7 -or $clones.Count -lt 5 -or
        @($births | Where-Object { -not $_.Completed -or $null -ne $_.Difference }).Count -gt 0 -or
        @($clones | Where-Object { -not $_.Completed -or $null -ne $_.Difference }).Count -gt 0 -or
        @($births | Where-Object { $_.IsCardless -and $_.SpawnerCardId -gt 0 }).Count -lt 5) {
        throw 'Requested native repeated births, copied source references and cardless Rally did not complete.'
    }
}
if ($MultiSummonAdditional) {
    $kinds = @($trace.UnitBirths | ForEach-Object { $_.Definition.SpawnUnit.AssetKey } | Sort-Object -Unique)
    if ($kinds.Count -lt 2 -or @($trace.SpawnUpgrades).Count -lt 7) {
        throw 'Additional character births and extra upgrades were not exercised.'
    }
}
if ($MultiSummonPool) {
    if (@($trace.PoolSummonSelections).Count -lt 7 -or @($trace.PoolSummonSelections | Where-Object {
        -not $_.Completed -or $null -ne $_.Difference
    }).Count -gt 0) { throw 'Pooled character sampling and complete RNG contexts were not verified.' }
}
if ($MultiSummonFresh) {
    if (-not $missingFresh -and @($trace.FreshSpawners).Count -lt 7 -or @($trace.SpawnUpgrades).Count -lt 7 -or @($trace.GlobalStandbyChecks).Count -eq 0) {
        throw 'Fresh fallback setup, extra upgrades and global standby checks were not exercised.'
    }
    if ($missingFresh -and @($trace.UnitBirths | Where-Object { $_.SpawnerCardId -eq 0 -and $_.IsCardless }).Count -eq 0) {
        throw 'Known missing fallback sources and cardless births were not exercised.'
    }
    if ($MultiSummonFreshDeaths -and @($trace.GlobalStandbyChecks | Where-Object {
        @($_.Before.OtherPiles | ForEach-Object { $_.UnitConditions | Where-Object { $_.Ready } }).Count -gt 0
    }).Count -eq 0) { throw 'Fresh-source bound-unit deaths were not exercised.' }
}
if ($MultiSummonUpgrade) {
    $extra = @($trace.SpawnUpgrades)
    if ($extra.Count -lt 4 -or @($extra | Where-Object { -not $_.Completed -or $null -ne $_.Difference }).Count -gt 0 -or
        @($extra | Where-Object { $_.SourceAdded }).Count -eq 0) {
        throw 'Requested native extra spawn upgrades and source writes did not complete.'
    }
    if ($MultiSummonUpgradeUnique -and @($extra | Where-Object { -not $_.SourceAdded }).Count -eq 0) {
        throw 'Unique extra spawn upgrade did not exercise a rejected duplicate source write.'
    }
    if ($MultiSummonUpgradeRestricted -and @($extra | Where-Object {
        $old = @($_.Before.Units | Where-Object Id -eq $_.UnitId)
        $new = @($_.AfterDirect.Units | Where-Object Id -eq $_.UnitId)
        $_.SourceAdded -and $old.Count -eq 1 -and $new.Count -eq 1 -and $old[0].Size -eq $new[0].Size
    }).Count -eq 0) {
        throw 'Restricted extra spawn upgrade did not exercise a rejected unit change followed by a source write.'
    }
}
if ($MultiSummonZero) {
    $zeroDispatches = @($trace.RallyTriggers | Where-Object { $_.Label -eq 'multi-summon-zero-dispatch' })
    if ($zeroDispatches.Count -ne 1 -or $zeroDispatches[0].TriggerCount -ne 0 -or -not $zeroDispatches[0].Completed) {
        throw 'Requested native zero-count Rally dispatch did not execute.'
    }
}

if ($LethalRally) {
    $lethalSummons = @($trace.Actions | Where-Object { $_.ActualOutcome -eq 3 -and $_.Action.PlayerPosition -ge 0 })
    if ($lethalSummons.Count -ne 1 -or @($trace.RallyPhases | Where-Object { $_.Before.Context.AllScenarioBossesDead -eq $false -and
        $_.After.Context.AllScenarioBossesDead -eq $true }).Count -ne 1 -or @($trace.RallyTriggers | Where-Object {
        $_.Actor.EndsBattleOnDeath -and $_.AfterActor.DeathState.HasFinishedDying -and -not $_.AfterActor.DeathState.IsBeingRemoved }).Count -ne 1 -or
        $lethalSummons[0].Actual.Spawn.Train.Context.LastSpawnedUnitId -le 0) {
        throw 'Requested native lethal Rally, deferred Boss removal and terminal summon did not execute.'
    }
}
if ($RallyTriggers) {
    $rallyLabels = @('grow-first-player-by-two', 'zero-player-growth', 'remove-one-player-troop', 'first-enemy-horde', 'grow-enemy-by-two')
    if (@($trace.RallyOperations).Count -ne 5 -or @($rallyLabels | Where-Object { $_ -notin $trace.RallyOperations.Label }).Count -gt 0 -or
        @($trace.RallyPhases).Count -ne 10 -or @($trace.RallyTriggers).Count -ne 12 -or
        @($trace.RallyOperations | Where-Object { $null -eq $_.AfterApi -or $null -eq $_.After -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested native Rally and Horde birth/growth operations did not complete.'
    }
}
if ($DyingHordeUpgrades) {
    $operations = @($trace.DyingHordeUpgradeOperations)
    if ($operations.Count -ne 5 -or @($operations | Where-Object { $null -eq $_.After -or
        $null -eq $_.AfterActor -or $_.QueueAfter -ne 0 -or -not $_.HasFinishedDying -or
        -not $_.IsBeingRemoved -or -not $_.IsSacrifice }).Count -gt 0 -or @($trace.DyingUpgrades).Count -ne 7 -or
        @($trace.DyingUpgrades | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnits -or $_.Interactions.Count -gt 0 }).Count -gt 0) {
        throw 'Requested dying Horde upgrade/removal operations did not complete.'
    }
}
if ($HordeUpgrades) {
    $operations = @($trace.HordeUpgradeOperations)
    if ($operations.Count -ne 16 -or @($operations | Where-Object { $null -eq $_.AfterApi -or
        $null -eq $_.After -or $null -eq $_.AfterActor -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested ordered Horde runtime upgrade operations did not complete.'
    }
}
if ($HordeDeath) {
    $operations = @($trace.HordeDeathOperations)
    $labels = @('queued-final-player', 'dying-final-enemy', 'dying-final-player')
    if ($operations.Count -ne 3 -or @($labels | Where-Object { $_ -notin $operations.Label }).Count -gt 0 -or
        @($operations | Where-Object { $null -eq $_.After -or $null -eq $_.AfterActor -or $_.QueueAfter -ne 0 -or
            -not $_.HasFinishedDying -or -not $_.IsSacrifice -or -not $_.IsBeingRemoved }).Count -gt 0) {
        throw 'Requested queued and repeated Horde death operations did not complete.'
    }
}
if ($HordeRemoval) {
    $removals = @($trace.HordeRemovalOperations)
    if (@($trace.HordeRemovalCasts).Count -ne 1 -or $null -eq $trace.HordeRemovalCasts[0].After -or $removals.Count -ne 9 -or @($removals | Where-Object { $null -eq $_.After -or $null -eq $_.AfterDrain -or
        $null -eq $_.AfterActor -or $null -eq $_.AfterDrainActor -or $_.QueueAfterDrain -ne 0 }).Count -gt 0) {
        throw 'Requested native Horde final-removal operations did not complete.'
    }
}
if ($HarvestTriggers) {
    $operations = @($trace.HarvestOperations)
    $labels = @('enemy-horde-health-lethal', 'player-horde-health-lethal', 'enemy-horde-damage-lethal', 'player-horde-damage-lethal')
    if ($operations.Count -ne 4 -or @($labels | Where-Object { $_ -notin $operations.Label }).Count -gt 0 -or
        @($operations | Where-Object { $null -eq $_.After -or $null -eq $_.AfterActor -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested native Harvest deaths did not complete.'
    }
}
if ($HordeStatuses) {
    $hordeOperations = @($trace.HordeStatusOperations)
    $hordeLabels = @('cooldown-ready', 'troops-grow', 'zero-add', 'negative-add', 'troops-remove',
        'damage-casualty', 'troops-regrow', 'maxhp-casualty')
    if ($hordeOperations.Count -ne 8 -or @($hordeLabels | Where-Object { $_ -notin $hordeOperations.Label }).Count -gt 0 -or
        @($hordeOperations | Where-Object { $null -eq $_.After -or $null -eq $_.AfterDrain -or
            $_.QueueAfterDrain -ne 0 -or $_.RunningBeforeDrain -or $_.ActorPreviewBeforeDrain -or $_.SavePreviewBeforeDrain }).Count -gt 0) {
        throw 'Requested native Horde status operations did not complete.'
    }
}
if ($HordeMerge) {
    $mergeOperations = @($trace.HordeMergeOperations)
    $mergeLabels = @('preview-player-clone', 'preview-player-bump-merge-with-equipment',
        'preview-enemy-ordinary-merge', 'preview-enemy-self-merge',
        'player-clone-adds-one-without-birth', 'player-bump-merge-mixed-equipment',
        'enemy-clone-adds-one-without-birth', 'enemy-ordinary-merge-different-definition',
        'null-source-merge', 'null-target-merge', 'non-horde-source-merge', 'non-horde-target-merge',
        'enemy-self-merge-removes-positive-hp-recipient', 'cross-team-direct-merge',
        'immune-target-still-removes-source', 'immune-clone-no-growth', 'null-source-clone')
    if ($mergeOperations.Count -ne $mergeLabels.Count -or @($mergeLabels | Where-Object { $_ -notin $mergeOperations.Label }).Count -gt 0 -or
        @($trace.HordeMergeSelections).Count -ne 5 -or @($mergeOperations | Where-Object {
            $null -eq $_.AfterApi -or $null -eq $_.After -or $_.QueueAfter -ne 0 -or
            $_.QueueAfterApi -ne @($_.Queued).Count -or ($_.SourceId -gt 0 -and ($null -eq $_.SourceAfter -or $null -eq $_.SourceAfterDrain)) -or
            ($_.Before.Preview -and ($null -eq $_.PrimaryBeforePreview -or $null -eq $_.PrimaryAfterPreview))
        }).Count -gt 0) {
        throw 'Requested native Horde merge/clone operations did not complete.'
    }
}
if ($Bump) {
    $bumpOperations = @($trace.BumpOperations)
    $bumpLabels = @('player-up', 'player-down', 'player-partial-up-ignores-range', 'player-pyre-blocked',
        'player-zero', 'player-clamped-down', 'enemy-up', 'enemy-down', 'enemy-rooted', 'enemy-immobile-before-rooted',
        'enemy-loop-up-to-bottom', 'enemy-room-multiple-targets', 'enemy-to-top-before-merge',
        'player-cross-room-horde-merge', 'enemy-cross-room-horde-merge', 'enemy-into-pyre',
        'enemy-full-room-blocked', 'enemy-partial-full-room', 'enemy-relentless-up-destroys-room',
        'enemy-companion-relentless-up-refuses-ascension', 'enemy-outer-boss-move',
        'enemy-outer-boss-paired-relentless-move')
    if ($bumpOperations.Count -ne $bumpLabels.Count -or @($bumpLabels | Where-Object { $_ -notin $bumpOperations.Label }).Count -gt 0 -or
        @($bumpOperations | Where-Object { $null -eq $_.After -or $null -eq $_.TargetsAfter -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested native paid Bump operations did not complete.'
    }
}
if ($UnitClone) {
    $cloneLabels = @('null-source', 'invalid-room', 'ordinary-front', 'ordinary-back-cardless', 'wounded-buffed-excluded',
        'negative-damage-buff', 'equipped-source', 'runtime-equipment-ability', 'clone-of-clone', 'cardless-source',
        'full-room-card-allocation', 'selected-no-adjacent', 'horde-no-birth')
    $clones = @($trace.UnitCloneOperations)
    if ($clones.Count -ne $cloneLabels.Count -or @($cloneLabels | Where-Object { $_ -notin $clones.Label }).Count -gt 0 -or
        @($clones | Where-Object { $null -eq $_.AfterApi -or $null -eq $_.After -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Requested native ordinary clone boundaries did not complete.'
    }
}
if ($UnitCopy) {
    $copyLabels = @('zero-targeted', 'negative-room', 'single-targeted', 'multiple-targeted', 'clone-of-clone-ignores-range',
        'status-before-copy', 'ability-before-copy', 'equipped-source', 'cardless-source', 'room-multiple-targets',
        'full-room-two-card-allocations', 'selected-last-no-allocation', 'partial-room-many-targets', 'horde-no-birth')
    $copies = @($trace.UnitCopyOperations)
    if ($copies.Count -ne $copyLabels.Count -or @($copyLabels | Where-Object { $_ -notin $copies.Label }).Count -gt 0 -or
        @($copies | Where-Object { $null -eq $_.After }).Count -gt 0) {
        throw 'Requested native paid unit-copy operations did not complete.'
    }
}
if ($SpawnEnchant) {
    if (@($trace.SpawnEnchantments).Count -lt 3 -or @($trace.UnitBirths).Count -lt 3 -or
        @($trace.SpawnEnchantments | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Difference -or $null -ne $_.UnsupportedReason }).Count -gt 0) {
        throw 'Requested native spawning enchant phases did not complete.'
    }
}
if ($HeroCopy) {
    $heroCopyLabels = @('zero-targeted', 'negative-room', 'raw-source-no-stats', 'copy-live-stats', 'multiple-targeted',
        'clone-of-copy-ignores-range', 'status-before-copy', 'ability-before-copy', 'equipped-no-stats', 'equipped-live-stats',
        'room-multiple-targets', 'horde-selected-last', 'full-room-no-allocations', 'partial-room-many-targets',
        'mixed-mask-enemy-to-player', 'mixed-mask-player')
    $heroCopies = @($trace.HeroCopyOperations)
    if ($heroCopies.Count -ne $heroCopyLabels.Count -or @($heroCopyLabels | Where-Object { $_ -notin $heroCopies.Label }).Count -gt 0 -or
        @($heroCopies | Where-Object { $null -eq $_.After }).Count -gt 0) {
        throw 'Requested native hero and mixed-team copying operations did not complete.'
    }
}
$equipmentActivations = @()
if ($EquipmentAbilities) {
    $skillB = 'c2f6ed7f-18ce-4070-b65f-7dd9f5160074'
    $skillC = 'c2f6ed7f-18ce-4070-b65f-7dd9f5160075'
    $initial = @($trace.InitialAbilitySpawns)
    $initialCoverage = $initial.Count -eq 1 -and $null -ne $initial[0].Actual -and -not $initial[0].Difference -and $initial[0].Predicted.Supported
    if ($initialCoverage) {
        $sourceId = $initial[0].Action.CardInstanceId
        $source = $initial[0].Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $sourceId
        $born = $initial[0].Actual.Spawn.Train.Rooms.Units | Where-Object SpawnerCardId -EQ $sourceId
        $initialCoverage = $source.Permanent.Upgrades.AbilityUpgrade.Definition.DataId -contains $skillB -and
            $source.Temporary.Upgrades.AbilityUpgrade.Definition.DataId -contains $skillC -and
            $born.Ability.DataId -eq $skillC -and $born.Ability.Cooldown -eq 6 -and $born.Ability.CooldownAtSpawn -eq 2 -and -not $born.Ability.FromEquipment
    }
    $equipmentOps = @($trace.EquipmentOperations)
    $restoration = @($equipmentOps | Where-Object {
        $entry = $_
        $prior = $entry.Before.Units | Where-Object Id -EQ $entry.UnitId
        $actual = $entry.After.Units | Where-Object Id -EQ $entry.UnitId
        $entry.Remove -and $prior.Ability.DataId -eq $skillB -and $prior.Ability.FromEquipment -and $prior.Ability.PreviousDataId -eq $skillC -and
            $actual.Ability.DataId -eq $skillC -and $actual.Ability.Cooldown -eq 6 -and -not $actual.Ability.FromEquipment -and
            $null -eq $actual.Ability.PreviousDataId -and @($actual.Statuses | Where-Object { $_.Id -eq 'cooldown' -and $_.Stacks -eq 6 }).Count -eq 1
    })
    $equipmentCoverageBothContexts = @($equipmentOps | Where-Object DeferAbilityCallbacks -EQ $true).Count -gt 0 -and
        @($equipmentOps | Where-Object DeferAbilityCallbacks -EQ $false).Count -gt 0
    $direct = @($trace.DirectUnitUpgrades)
    $directLabels = @('ability-keep-existing', 'ability-remove-nonmatching', 'ability-direct-clears-equipment-history',
        'ability-remove-current', 'ability-add-to-empty', 'ability-remove-added', 'ability-explicit-reassign-disabled')
    $directCoverage = $direct.Count -eq $directLabels.Count -and @($directLabels | Where-Object { $_ -notin $direct.Label }).Count -eq 0 -and
        @($direct | Where-Object { $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0
    $equipmentActivations = @($trace.Actions | Where-Object {
        $entry = $_
        $entry.Action.ActivatorUnitId -gt 0 -and @($entry.Before.Spawn.Train.Rooms.Units | Where-Object {
            $_.Id -eq $entry.Action.ActivatorUnitId -and $_.Ability.FromEquipment -and $_.Ability.DataId -eq $skillB
        }).Count -eq 1
    })
    $keptBirths = @($trace.Actions | Where-Object {
        $entry = $_
        $source = $entry.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $baseRule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $source.DataId
        $baseRule.SpawnUnit.Ability.HasAbility -and @($source.Permanent.Upgrades | Where-Object {
            $_.DoNotReplaceExistingAbility -and $_.AbilityUpgrade.Definition.DataId -eq $skillB
        }).Count -gt 0 -and @($entry.Actual.Spawn.Train.Rooms.Units | Where-Object {
            $_.Id -ge $entry.Before.Spawn.NextUnitId -and $_.SpawnerCardId -eq $entry.Action.CardInstanceId -and $_.Ability.DataId -eq $baseRule.SpawnUnit.Ability.DataId
        }).Count -gt 0
    })
    $disabledBirths = @($trace.Actions | Where-Object {
        $entry = $_
        $source = $entry.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $baseRule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $source.DataId
        $source.Permanent.Upgrades.AbilityUpgrade.Definition.DataId -contains $skillC -and
            $baseRule.SpawnUnit.Ability.HasAbility -and
            $entry.Before.Spawn.Train.Context.PermanentlyDisabledAbilities -contains $skillC -and
            @($entry.Actual.Spawn.Train.Rooms.Units | Where-Object {
                $_.Id -ge $entry.Before.Spawn.NextUnitId -and $_.SpawnerCardId -eq $entry.Action.CardInstanceId -and $null -eq $_.Ability -and
                    @($_.Triggers | Where-Object { $_.Origin.UpgradeId -eq 'UnitAbilityCommonData' }).Count -eq 0
            }).Count -gt 0
    })
    $callbacks = @($trace.CharacterCallbackFires)
    $callbackKinds = @('OnPreOwnAbilityActivated', 'OnOwnAbilityActivated', 'OnUnitAbilityAvailable', 'OnUnitAbilityUnavailable')
    $callbackCoverage = @($callbackKinds | Where-Object { $_ -notin $callbacks.Kind }).Count -eq 0 -and
        @($callbacks | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnit }).Count -eq 0
    $equipmentAbilityCoverage = $initialCoverage -and $restoration.Count -gt 0 -and $equipmentCoverageBothContexts -and $directCoverage -and
        $equipmentActivations.Count -ge 2 -and $keptBirths.Count -gt 0 -and $disabledBirths.Count -gt 0 -and $callbackCoverage
    if (-not $equipmentAbilityCoverage) {
        throw "Equipment ability coverage incomplete: initial=$initialCoverage restorations=$($restoration.Count) contexts=$equipmentCoverageBothContexts direct=$directCoverage activations=$($equipmentActivations.Count) keptBirths=$($keptBirths.Count) disabledBirths=$($disabledBirths.Count) callbacks=$callbackCoverage"
    }
}
$triggeredSummonCoverage = -not $TriggeredSummons
$triggeredSummonBirths = 0
$triggeredSummonZeroBirths = 0
$triggeredSummonDyingSources = 0
if ($TriggeredSummons) {
    $summonRecords = @($trace.TriggeredSummons)
    foreach ($record in $summonRecords) {
        $born = $record.After.Context.NextUnitId - $record.Before.Context.NextUnitId
        $triggeredSummonBirths += $born
        if ($born -eq 0) { $triggeredSummonZeroBirths++ }
        if ($record.ActorBefore.Health -le 0) { $triggeredSummonDyingSources++ }
    }
    $complete = $summonRecords.Count -ge 2 -and @($summonRecords | Where-Object {
        -not $_.Completed -or $null -ne $_.Error -or -not $_.QueueRunning -or $null -eq $_.After -or $null -eq $_.RuleAfter
    }).Count -eq 0
    $deathPhases = @($trace.TriggeredSummonDamages | Where-Object { -not $_.QueueRunning -and @($_.PendingDeathsBefore).Count -gt 0 })
    $triggeredSummonCoverage = $complete -and $triggeredSummonBirths -ge 2 -and
        $(if ($TriggeredSummonsDeath) { $triggeredSummonDyingSources -ge 2 -and $deathPhases.Count -ge 2 } else { $triggeredSummonZeroBirths -ge 2 })
    if (-not $triggeredSummonCoverage) {
        throw "Triggered summon coverage incomplete: complete=$complete births=$triggeredSummonBirths zero=$triggeredSummonZeroBirths dying=$triggeredSummonDyingSources deathPhases=$($deathPhases.Count)"
    }
}
$triggeredEquipmentCoverage = -not $TriggeredSummonsEquipment
$triggeredEquipmentApplications = 0
$queuedEquipmentAttachments = 0
$ownedSummonApplications = 0
$ownedSummonBirths = 0
$ownedSummonDyingBirths = 0
$ownedSummonRevivedBirths = 0
$summonRevivalHostSamples = 0
$summonRevivalChildSamples = 0
if ($TriggeredSummonsEquipment) {
    $equipped = @($trace.TriggeredSummons | Where-Object {
        @($_.ActorBefore.EquipmentCards).Count -gt 0 -and $_.After.Context.NextUnitId -gt $_.Before.Context.NextUnitId
    })
    $triggeredEquipmentApplications = $equipped.Count
    $queuedEquipmentAttachments = @($trace.EquipmentOperations | Where-Object { -not $_.Remove -and $_.QueueRunning }).Count
    $triggeredEquipmentCoverage = $equipped.Count -gt 0 -and
        $(if ($TriggeredSummonsFresh) { $queuedEquipmentAttachments -eq 0 } else { $queuedEquipmentAttachments -ge 2 })
    if (-not $triggeredEquipmentCoverage) {
        throw "Triggered equipment coverage incomplete: applications=$triggeredEquipmentApplications queuedAttachments=$queuedEquipmentAttachments"
    }
}
if ($TriggeredSummonsEquipmentOwned) {
    $owned = @($trace.TriggeredSummons | Where-Object { $_.EquipmentCardId -gt 0 })
    $ownedSummonApplications = $owned.Count
    $ownedBirths = @($owned | Where-Object { $_.After.Context.NextUnitId -gt $_.Before.Context.NextUnitId })
    $ownedSummonBirths = $ownedBirths.Count
    $ownedSummonDyingBirths = @($ownedBirths | Where-Object { $_.ActorBefore.Health -le 0 }).Count
    $ownedSummonRevivedBirths = @($ownedBirths | Where-Object { $_.Kind -eq 'OnDeath' -and $_.ActorBefore.Health -gt 0 -and
        $_.ActorBefore.Id -in @($trace.Revivals.ActorId) }).Count
    if ($owned.Count -eq 0 -or $ownedBirths.Count -eq 0 -or @($owned | Where-Object {
        $_.SourceCardId -ne $_.ActorBefore.SpawnerCardId -or $_.SourceCardId -eq $_.EquipmentCardId -or
        $_.RuleBefore.HasParentCard -or $null -eq $_.Queued -or $_.EffectIndex -lt 0
    }).Count -gt 0 -or ($TriggeredSummonsDeath -and -not $TriggeredSummonsRevival -and $ownedSummonDyingBirths -eq 0) -or
        ($TriggeredSummonsRevival -and $ownedSummonRevivedBirths -eq 0)) {
        throw 'Equipment-owned native summon source, queue or live/death coverage incomplete.'
    }
}
if ($Revival) {
    $revivals = @($trace.Revivals)
    $operations = @($trace.RevivalOperations)
    if ($operations.Count -ne 7 -or $revivals.Count -lt 7 -or
        @($revivals | Where-Object QueueRunning).Count -lt 2 -or
        @($revivals | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Error }).Count -gt 0 -or
        @($operations | Where-Object { $null -eq $_.After -or $_.QueueAfter -ne 0 }).Count -gt 0) {
        throw 'Revival coverage requires seven native operations and both direct and queued revivals.'
    }
}
if ($TriggeredSummonsRevival) {
    $revivals = @($trace.Revivals)
    $equippedHostRevivals = @($revivals | Where-Object { @($_.AfterActor.EquipmentCards).Count -gt 0 -and
        @($_.AfterActor.Triggers | ForEach-Object { $_.Effects } | Where-Object { $null -ne $_.Summon }).Count -gt 0 })
    $childRevivals = @($revivals | Where-Object { @($_.AfterActor.Statuses | Where-Object Id -eq 'cardless').Count -gt 0 -and
        @($_.AfterActor.Triggers | ForEach-Object { $_.Effects } | Where-Object { $null -ne $_.Summon }).Count -eq 0 })
    $summonRevivalHostSamples = $equippedHostRevivals.Count
    $summonRevivalChildSamples = $childRevivals.Count
    if ($equippedHostRevivals.Count -lt 2 -or $childRevivals.Count -eq 0 -or $ownedSummonRevivedBirths -eq 0 -or
        @($revivals | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Error -or $_.AutomaticQueueDeferrals -ne 0 }).Count -gt 0) {
        throw 'Triggered summon revival requires equipped host/child revivals and live-source death births without queue deferrals.'
    }
}
$purifyCoverage = -not $Purify
if ($Purify) {
    $purifySamples = @($trace.TriggeredStatuses)
    $blockedPositive = 0; $blockedZero = 0; $blockedNegative = 0; $zeroPurifyClears = 0; $positivePurifyClears = 0
    foreach ($sample in $purifySamples) {
        $status = $sample.Effect.Action.Statuses[0]
        foreach ($unit in @($sample.BeforeUnits | Where-Object { $_.Id -in $sample.Targets })) {
            if (@($unit.Statuses | Where-Object { $_.Id -eq 'purify' -and $_.Stacks -gt 0 }).Count -gt 0) {
                if ($status.Stacks -gt 0) { $blockedPositive++ } elseif ($status.Stacks -eq 0) { $blockedZero++ } else { $blockedNegative++ }
            } elseif ($status.Id -eq 'purify' -and @($unit.Statuses | Where-Object { $_.Id -ne 'purify' -and $_.Stacks -gt 0 }).Count -gt 0) {
                if ($status.Stacks -eq 0) { $zeroPurifyClears++ } elseif ($status.Stacks -gt 0) { $positivePurifyClears++ }
            }
        }
    }
    $purifyRemovals = @($trace.AbilityCooldownEffects | Where-Object { $_.Effect.Type -eq 'RemoveStatus' -and $_.Effect.Statuses.Id -contains 'purify' })
    $purifyPaidActions = @($trace.Actions | Where-Object {
        $sample = $_
        $source = $sample.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $sample.Action.CardInstanceId
        $rule = $sample.Before.PlayRules.Cards | Where-Object DataId -EQ $source.DataId
        $rule.Effects.Statuses.Id -contains 'purify'
    })
    $purifyCoverage = $trace.ModifierScenario -in @('purify', 'purify-queues') -and $blockedPositive -gt 0 -and $blockedZero -gt 0 -and $blockedNegative -gt 0 -and
        $zeroPurifyClears -gt 0 -and $positivePurifyClears -gt 0 -and $purifyRemovals.Count -gt 0 -and $purifyPaidActions.Count -gt 0 -and
        @($purifySamples | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or @($_.Interactions).Count -gt 0 }).Count -eq 0 -and
        @($purifyRemovals | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.StatusCallbackFires).Count -gt 0 -and @($trace.StatusCallbackFires).Count -eq @($trace.StatusCallbacks).Count
    if (-not $purifyCoverage) {
        throw "Purify coverage incomplete: blocked=$blockedPositive/$blockedZero/$blockedNegative clears=$positivePurifyClears/$zeroPurifyClears removals=$($purifyRemovals.Count) paid=$($purifyPaidActions.Count)"
    }
}
$purifyQueueCoverage = -not $PurifyQueues
if ($PurifyQueues) {
    $admissions = @($trace.PurifyQueueAdmissions)
    $rejectedKinds = @($admissions | Where-Object { $_.Purified -and $_.Overload -eq 'Character' -and $_.QueueAfter -eq $_.QueueBefore } | ForEach-Object Kind | Sort-Object -Unique)
    $requiredKinds = @('OnSpawn', 'OnUnscaledSpawn', 'AfterSpawnEnchant', 'CardMonsterPlayed', 'OnSentry', 'OnDeath', 'OnAnyUnitDeathOnFloor')
    $purifyQueueCoverage = $admissions.Count -gt 0 -and @($admissions | Where-Object { -not $_.Completed -or @($_.Interactions).Count -gt 0 }).Count -eq 0 -and
        @($requiredKinds | Where-Object { $_ -notin $rejectedKinds }).Count -eq 0
    if (-not $purifyQueueCoverage) { throw "Purify queue coverage incomplete: rejected=$($rejectedKinds -join ',')" }
}
$incantCoverage = -not $Incant
if ($Incant) {
    $incantPhases = @($trace.IncantPhases)
    $incantFires = @($trace.IncantTriggers)
    $incantRequests = @($trace.PurifyQueueAdmissions | Where-Object { $_.Kind -eq 'CardSpellPlayed' })
    $incantRejected = @($incantRequests | Where-Object { $_.Purified -and $_.QueueBefore -eq $_.QueueAfter })
    $emptySpellPlays = 0; $silencedFires = 0; $visibleFires = 0
    foreach ($action in $trace.Actions) {
        $source = $action.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $action.Action.CardInstanceId
        $rule = $action.Before.PlayRules.Cards | Where-Object DataId -EQ $source.DataId
        if ($rule.CardType -eq 'Spell' -and -not $rule.IsAnyAbility -and $rule.Effect -eq 'Null') { $emptySpellPlays++ }
    }
    foreach ($fire in $incantFires) {
        if (@($fire.Actor.Statuses | Where-Object { $_.Id -eq 'silenced' -and $_.Stacks -gt 0 }).Count -gt 0) { $silencedFires++ }
        elseif (@($fire.AfterActor.Triggers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and -not $_.IgnoreSilence -and $_.HasTriggered }).Count -gt 0) { $visibleFires++ }
    }
    $incantCoverage = $trace.ModifierScenario -eq $(if ($IncantRelics) { 'incant-relic-combined' } elseif ($IncantRelic) { 'incant-relic' } elseif ($IncantThresholds) { 'incant-thresholds' } else { 'incant' }) -and $incantPhases.Count -ge 10 -and $incantFires.Count -gt 5 -and
        @($incantPhases | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Difference }).Count -eq 0 -and
        @($incantFires | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -eq $_.AfterActor -or $null -ne $_.Difference }).Count -eq 0 -and
        @($incantFires | Where-Object { $_.Actor.Team -eq 1 }).Count -gt 0 -and @($incantFires | Where-Object { $_.Actor.Team -eq 0 }).Count -gt 0 -and
        $incantRejected.Count -gt 0 -and $emptySpellPlays -gt 0 -and $silencedFires -gt 0 -and $visibleFires -gt 0
    if (-not $incantCoverage) { throw "Incant coverage incomplete: phases=$($incantPhases.Count) fires=$($incantFires.Count) rejected=$($incantRejected.Count) empty=$emptySpellPlays silenced=$silencedFires visible=$visibleFires" }
}
if ($SpawnStatusRelics) {
    $births = @($trace.RelicSpawnStatuses)
    $spawnRelicPlayers = 0
    $spawnRelicEnemies = 0
    $spawnRelicFirsts = 0
    $spawnRelicSkips = 0
    $cardlessUpgradeApplied = 0
    $cardlessCloneSkipped = 0
    $mixedOrderWitnesses = 0
    $spawnRelicTurns = [Collections.Generic.HashSet[int]]::new()
    $cardlessUpgradeId = $null
    if ($SpawnStatusRelicsClones -and $births.Count -gt 0) {
        $cardlessUpgradeRelics = @($births[0].Before.Context.Relics | Where-Object AssetKey -EQ 'PyreHeartSon')
        if ($cardlessUpgradeRelics.Count -eq 1 -and $cardlessUpgradeRelics[0].CardModifiers.Count -eq 1 -and
            $cardlessUpgradeRelics[0].CardModifiers[0].ApplyToCardlessSpawns) {
            $cardlessUpgradeId = $cardlessUpgradeRelics[0].CardModifiers[0].Upgrade.DataId
        }
    }
    foreach ($birth in $births) {
        $actor = @($birth.After.Units | Where-Object { $_.Id -eq $birth.UnitId })
        $beforeActor = @($birth.Before.Units | Where-Object { $_.Id -eq $birth.UnitId })
        if ($actor.Count -ne 1) { throw 'Native relic birth actor is missing or duplicated.' }
        if ($beforeActor.Count -ne 1) { throw 'Native relic birth before-state actor is missing or duplicated.' }
        if ($SpawnStatusRelicsClones -and $cardlessUpgradeId -and
            @($beforeActor[0].Statuses | Where-Object Id -EQ 'cardless').Count -gt 0) {
            $beforeUpgradeCount = @($beforeActor[0].Modifiers.Upgrades | Where-Object DataId -EQ $cardlessUpgradeId).Count
            $afterUpgradeCount = @($actor[0].Modifiers.Upgrades | Where-Object DataId -EQ $cardlessUpgradeId).Count
            if ($beforeActor[0].Modifiers.IsClone) {
                if ($afterUpgradeCount -eq $beforeUpgradeCount) { $cardlessCloneSkipped++ }
            } elseif ($afterUpgradeCount -gt $beforeUpgradeCount) {
                $cardlessUpgradeApplied++
                if ($MixedCardlessRelicOrder -and @($actor[0].Statuses | Where-Object Id -EQ 'armor').Count -gt 0) { $mixedOrderWitnesses++ }
            }
        }
        if ($actor[0].IsPyre -and $OriginalSpawnCombatRelics) { continue }
        if ($actor[0].Team -eq 1) {
            $spawnRelicPlayers++
            $oldShield = @($birth.Before.Context.Relics | Where-Object AssetKey -EQ 'FirstUnitGainDamageShield')[0].SpawnStatuses[0].Conditions[0]
            $newShield = @($birth.After.Context.Relics | Where-Object AssetKey -EQ 'FirstUnitGainDamageShield')[0].SpawnStatuses[0].Conditions[0]
            if ($oldShield.DurationTriggerCount -eq 0 -and $newShield.DurationTriggerCount -eq 1) {
                $spawnRelicFirsts++
                [void]$spawnRelicTurns.Add($birth.Before.Context.QueryFrame.Turn)
            } elseif ($oldShield.DurationTriggerCount -eq 1 -and $newShield.DurationTriggerCount -eq 1) { $spawnRelicSkips++ }
        } else { $spawnRelicEnemies++ }
    }
    $expectedSpawnScenario = if ($OriginalSpawnCombatRelics) { 'spawn-status-relics-combat' } elseif ($OriginalChicotRelic) { 'spawn-status-relics-boss-mute' } elseif ($OriginalTunedGearRelic) { 'spawn-status-relics-tuned-gear' } elseif ($OriginalEggTransformation) { 'spawn-status-relics-egg-transformation' } elseif ($OriginalEggHatchRelic) { 'spawn-status-relics-egg-hatch' } elseif ($OriginalTeamSpawnStatusRelic) { 'spawn-status-relics-team-wide' } elseif ($OriginalStealthWaxersRelic) { 'spawn-status-relics-stealth-waxers' } elseif ($MixedCardlessRelicOrder) { 'spawn-status-relics-mixed-cardless-order' } elseif ($SpawnStatusRelicsClones) { 'spawn-status-relics-clones' } else { 'spawn-status-relics' }
    $expectedSpawnSchema = if ($OriginalChicotRelic) { 156 } elseif ($OriginalTunedGearRelic) { 155 } elseif ($OriginalTeamSpawnStatusRelic) { 144 } elseif ($OriginalStealthWaxersRelic) { 140 } elseif ($MixedCardlessRelicOrder) { 121 } elseif ($SpawnStatusRelicsClones) { 111 } else { 110 }
    $requiredSpawnRelics = @('SpawnWithArmor', 'FirstUnitGainDamageShield', 'FrostbiteOnEnemies')
    if ($MixedCardlessRelicOrder) { $requiredSpawnRelics = @('PyreHeartSon', 'FirstUnitGainDamageShield', 'FrostbiteOnEnemies') }
    if ($OriginalStealthWaxersRelic) { $requiredSpawnRelics += 'StealthWaxers' }
    if ($OriginalChicotRelic) { $requiredSpawnRelics += 'BalatroEventChicot' }
    if ($OriginalTeamSpawnStatusRelic) { $requiredSpawnRelics += 'HymnistsApplyValorOnSummon' }
    if ($OriginalTunedGearRelic) { $requiredSpawnRelics += 'TunedGear' }
    if ($OriginalEggHatchRelic -or $OriginalEggTransformation) { $requiredSpawnRelics += 'EggHatchRemoval' }
    if ($OriginalSpawnCombatRelics) { $requiredSpawnRelics += @('DantesCandelabra', 'PressurizedBoiler') }
    $observedSpawnRelics = @($trace.Actions[0].Before.Spawn.Train.Context.Relics | Where-Object { $_.AssetKey -in $requiredSpawnRelics }).Count
    if ($trace.Schema -ne $expectedSpawnSchema -or $trace.ModifierScenario -ne $expectedSpawnScenario -or $births.Count -lt 3 -or
        $spawnRelicPlayers -lt 2 -or $spawnRelicEnemies -lt 1 -or
        (-not $OriginalEggTransformation -and ($spawnRelicFirsts -lt 1 -or $spawnRelicSkips -lt 1)) -or
        (-not $OriginalEggTransformation -and -not $SpawnStatusRelicsClones -and ($spawnRelicFirsts -lt 2 -or $spawnRelicTurns.Count -lt 2)) -or
        ($SpawnStatusRelicsClones -and (-not $cardlessUpgradeId -or $cardlessUpgradeApplied -lt 1 -or $cardlessCloneSkipped -lt 1)) -or
        @($births | Where-Object { -not $_.Completed -or -not $_.After -or $_.Difference }).Count -ne 0 -or
        $observedSpawnRelics -ne $requiredSpawnRelics.Count) {
        throw 'Native original spawn-status relic coverage is incomplete or failed.'
    }
    if ($OriginalEggHatchRelic -or $OriginalEggTransformation) {
        $egg = @($births[0].Before.Context.Relics | Where-Object { $_.AssetKey -eq 'EggHatchRemoval' -and $_.DataId -eq '8c8a2a2c-500c-4b77-90fd-0d309b94bb2c' })
        if ($egg.Count -ne 1 -or (@($egg[0].EffectTypes) -join ',') -ne 'RelicEffectRemoveStatusEffectOnSpawn,RelicEffectAddStatusEffectOnSpawn' -or
            $egg[0].SpawnStatuses.Count -ne 2 -or $egg[0].SpawnStatuses[0].Statuses[0].Id -ne 'hatch' -or
            $egg[0].SpawnStatuses[0].Statuses[0].Stacks -ne 3 -or $egg[0].SpawnStatuses[1].Statuses[0].Id -ne 'armor' -or
            $egg[0].SpawnStatuses[1].Statuses[0].Stacks -ne 30 -or
            $egg[0].SpawnStatuses[0].RequiredSubtype -ne 'SubtypesData_Egg' -or
            $egg[0].SpawnStatuses[1].RequiredSubtype -ne 'SubtypesData_Egg') {
            throw 'Original EggHatchRemoval effect order or payload is missing.'
        }
        $eggWitnesses = 0
        foreach ($birth in $births) {
            $beforeActor = @($birth.Before.Units | Where-Object Id -EQ $birth.UnitId)[0]
            $afterActor = @($birth.After.Units | Where-Object Id -EQ $birth.UnitId)[0]
            if ($beforeActor.Team -ne 1) { continue }
            $beforeHatch = @($beforeActor.Statuses | Where-Object Id -EQ 'hatch')
            $afterHatch = @($afterActor.Statuses | Where-Object Id -EQ 'hatch')
            $beforeArmor = @($beforeActor.Statuses | Where-Object Id -EQ 'armor')
            $afterArmor = @($afterActor.Statuses | Where-Object Id -EQ 'armor')
            $oldArmor = if ($beforeArmor.Count) { [int]$beforeArmor[0].Stacks } else { 0 }
            if ($beforeHatch.Count -eq 1 -and $beforeHatch[0].Stacks -eq $(if ($OriginalEggTransformation) { 5 } else { 3 }) -and
                $afterHatch.Count -eq $(if ($OriginalEggTransformation) { 1 } else { 0 }) -and
                (-not $OriginalEggTransformation -or $afterHatch[0].Stacks -eq 2) -and
                $afterArmor.Count -eq 1 -and $afterArmor[0].Stacks -eq ($oldArmor + 35)) { $eggWitnesses++ }
        }
        if ($eggWitnesses -lt 2) { throw "Original EggHatchRemoval produced only $eggWitnesses complete birth witnesses." }
        Write-Output "ORIGINAL-EGG-HATCH PASS: $eggWitnesses native births remove Hatch 3 and add Armor 30."
    }
    if ($OriginalEggTransformation) {
        $hatches = @($trace.TriggeredSummons | Where-Object Kind -EQ 'OnHatched')
        if ($trace.Actions[0].Before.BattlePreviewEnabled -or $hatches.Count -lt 2 -or
            @($hatches | Where-Object {
                -not $_.Completed -or $_.Error -or $_.RuleBefore.CharacterId -ne 'e2f7a12a-b33b-4dbe-b95b-73f39cdf3a70' -or
                $_.ActorAfter.SacrificeCardId -ne $_.SourceCardId -or
                @($_.After.Units | Where-Object AssetKey -EQ 'MonsterKinhostPupa').Count -lt 1
            }).Count -gt 0) { throw 'Native Egg transformation lacks complete sacrifice and Kinhost Pupa summons.' }
        Write-Output "NATIVE-EGG-TRANSFORMATION PASS: $($hatches.Count) original OnHatched sacrifice/summon chains."
    }
    if ($OriginalChicotRelic) {
        $chicotId = '92d66578-b148-49f8-9880-edb67f8b18de'
        $chicotRelic = @($trace.Actions[0].Before.Spawn.Train.Context.Relics | Where-Object {
            $_.AssetKey -eq 'BalatroEventChicot' -and $_.DataId -eq $chicotId
        })
        if ($chicotRelic.Count -ne 1 -or $chicotRelic[0].EffectTypes.Count -ne 1 -or
            $chicotRelic[0].EffectTypes[0] -ne 'RelicEffectAddStatusEffectOnSpawn' -or
            $chicotRelic[0].SpawnStatuses.Count -ne 1 -or $chicotRelic[0].SpawnStatuses[0].TargetPlayer -or
            -not $chicotRelic[0].SpawnStatuses[0].TargetEnemy -or
            $chicotRelic[0].SpawnStatuses[0].RequiredSubtype -ne 'SubtypesData_BossAll' -or
            $chicotRelic[0].SpawnStatuses[0].Statuses.Count -ne 1 -or
            $chicotRelic[0].SpawnStatuses[0].Statuses[0].Id -ne 'muted' -or
            $chicotRelic[0].SpawnStatuses[0].Statuses[0].Stacks -ne 3 -or
            $chicotRelic[0].SpawnStatuses[0].Conditions.Count -ne 0) {
            throw 'Original BalatroEventChicot identity, Boss filter or Mute payload is missing.'
        }
        $chicotWitnesses = 0
        foreach ($birth in $births) {
            $beforeActor = @($birth.Before.Units | Where-Object { $_.Id -eq $birth.UnitId })[0]
            $afterActor = @($birth.After.Units | Where-Object { $_.Id -eq $birth.UnitId })[0]
            $beforeMute = @($beforeActor.Statuses | Where-Object Id -EQ 'muted')
            $afterMute = @($afterActor.Statuses | Where-Object Id -EQ 'muted')
            $beforeStacks = if ($beforeMute.Count) { [int]$beforeMute[0].Stacks } else { 0 }
            $afterStacks = if ($afterMute.Count) { [int]$afterMute[0].Stacks } else { 0 }
            if ($beforeActor.Team -eq 0 -and $beforeActor.IsBoss -eq $true -and
                $beforeActor.Subtypes -contains 'SubtypesData_BossAll' -and $afterStacks -eq ($beforeStacks + 3)) {
                $chicotWitnesses++
            }
        }
        if ($chicotWitnesses -eq 0) { throw 'Original BalatroEventChicot produced no positive Boss mute birth witness.' }
        Write-Output "ORIGINAL-CHICOT-MUTE PASS: BossAll birth states gained three Mute stacks in $chicotWitnesses native witness."
    }
    if ($OriginalStealthWaxersRelic) {
        $stealthId = '22f5ff29-69be-4043-9fe1-245392ea3c95'
        $stealthRelic = @($trace.Actions[0].Before.Spawn.Train.Context.Relics | Where-Object { $_.AssetKey -eq 'StealthWaxers' -and $_.DataId -eq $stealthId })
        if ($stealthRelic.Count -ne 1 -or $stealthRelic[0].EffectTypes.Count -ne 1 -or
            $stealthRelic[0].EffectTypes[0] -ne 'RelicEffectAddStatusEffectOnSpawn' -or
            $stealthRelic[0].SpawnStatuses.Count -ne 1 -or $stealthRelic[0].SpawnStatuses[0].Statuses.Count -ne 1 -or
            $stealthRelic[0].SpawnStatuses[0].Statuses[0].Id -ne 'endless' -or
            $stealthRelic[0].SpawnStatuses[0].Statuses[0].Stacks -ne 1) {
            throw 'Original StealthWaxers identity or Endless payload is missing.'
        }
        $condition = $stealthRelic[0].SpawnStatuses[0].Conditions
        if ($condition.Count -ne 1 -or $condition[0].Query.Type -ne 'SubtypeInDeck' -or
            $condition[0].Query.Duration -ne 'ThisTurn' -or $condition[0].Comparator -ne 6 -or
            $condition[0].Value -ne 1 -or -not $condition[0].TrackTriggerCount -or $condition[0].AllowMultiple) {
            throw 'Original StealthWaxers native condition was not preserved.'
        }
        $stealthWitnesses = 0
        foreach ($birth in $births) {
            $beforeActor = @($birth.Before.Units | Where-Object { $_.Id -eq $birth.UnitId })[0]
            $afterActor = @($birth.After.Units | Where-Object { $_.Id -eq $birth.UnitId })[0]
            $beforeRule = @($birth.Before.Context.Relics | Where-Object { $_.DataId -eq $stealthId })[0].SpawnStatuses[0]
            $afterRule = @($birth.After.Context.Relics | Where-Object { $_.DataId -eq $stealthId })[0].SpawnStatuses[0]
            $beforeEndlessStatus = @($beforeActor.Statuses | Where-Object { $_.Id -eq 'endless' })
            $afterEndlessStatus = @($afterActor.Statuses | Where-Object { $_.Id -eq 'endless' })
            $beforeEndless = if ($beforeEndlessStatus.Count -gt 0) { [int]$beforeEndlessStatus[0].Stacks } else { 0 }
            $afterEndless = if ($afterEndlessStatus.Count -gt 0) { [int]$afterEndlessStatus[0].Stacks } else { 0 }
            if ($afterRule.Conditions[0].DurationTriggerCount -gt $beforeRule.Conditions[0].DurationTriggerCount -and
                $afterEndless -gt $beforeEndless) { $stealthWitnesses++ }
        }
        if ($stealthWitnesses -eq 0) { throw 'Original StealthWaxers produced no positive Endless birth witness.' }
        Write-Output "ORIGINAL-STEALTH-WAXERS PASS: original Endless-on-spawn condition changed $stealthWitnesses eligible birth states."
    }
    if ($OriginalTeamSpawnStatusRelic) {
        $teamStatusId = '55cf29f3-bc0d-4b5f-929e-07d4629141b3'
        $teamStatusRelic = @($trace.Actions[0].Before.Spawn.Train.Context.Relics | Where-Object {
            $_.AssetKey -eq 'HymnistsApplyValorOnSummon' -and $_.DataId -eq $teamStatusId
        })
        if ($teamStatusRelic.Count -ne 1 -or $teamStatusRelic[0].EffectTypes.Count -ne 1 -or
            $teamStatusRelic[0].EffectTypes[0] -ne 'RelicEffectAddStatusEffectToTeamOnSpawn' -or
            $teamStatusRelic[0].SpawnStatuses.Count -ne 1 -or -not $teamStatusRelic[0].SpawnStatuses[0].ApplyToTeam -or
            $teamStatusRelic[0].SpawnStatuses[0].Statuses.Count -ne 1 -or
            $teamStatusRelic[0].SpawnStatuses[0].Statuses[0].Id -ne 'valor' -or
            $teamStatusRelic[0].SpawnStatuses[0].Statuses[0].Stacks -ne 5 -or
            $teamStatusRelic[0].SpawnStatuses[0].RequiredSubtype -ne 'SubtypesData_Hymnist') {
            throw 'Original HymnistsApplyValorOnSummon identity or team Valor payload is missing.'
        }
        $teamWideWitnesses = 0
        foreach ($birth in $births) {
            $born = @($birth.Before.Units | Where-Object { $_.Id -eq $birth.UnitId })[0]
            if (-not $born -or $born.Team -ne 1) { continue }
            foreach ($member in @($birth.Before.Units | Where-Object {
                $_.Team -eq $born.Team -and $_.Id -ne $born.Id -and $_.Health -gt 0 -and
                $_.DeathState.IsDespawned -ne $true -and $_.DeathState.IsDestroyed -ne $true
            })) {
                $afterMember = @($birth.After.Units | Where-Object { $_.Id -eq $member.Id })[0]
                if (-not $afterMember) { continue }
                $beforeValor = @($member.Statuses | Where-Object { $_.Id -eq 'valor' })
                $afterValor = @($afterMember.Statuses | Where-Object { $_.Id -eq 'valor' })
                $oldStacks = if ($beforeValor.Count) { [int]$beforeValor[0].Stacks } else { 0 }
                $newStacks = if ($afterValor.Count) { [int]$afterValor[0].Stacks } else { 0 }
                if ($newStacks -eq ($oldStacks + 5)) { $teamWideWitnesses++ }
            }
        }
        if ($teamWideWitnesses -lt 1) { throw 'Original team-wide spawn relic has no living ally Valor witness.' }
        Write-Output "ORIGINAL-TEAM-SPAWN-STATUS PASS: $teamWideWitnesses living ally states gained five Valor from a summon."
    }
    if ($OriginalSpawnCombatRelics) {
        if (-not $births[0].Before.Units[0].IsPyre -or
            @($births[0].Before.Context.Relics | Where-Object { $_.AssetKey -in @('SpawnWithArmor', 'FirstUnitGainDamageShield', 'FrostbiteOnEnemies') }).Count -ne 0) {
            throw 'Original Pyre spawn effects were not captured during native battle setup.'
        }
        foreach ($owner in @(
            @{ Key = 'DantesCandelabra'; Id = 'a3dde0d6-0aa3-44e5-a6a4-0e993a5d9a15'; Status = 'titanskin'; Stacks = 2 },
            @{ Key = 'PressurizedBoiler'; Id = 'fda361aa-fecc-4adf-b19c-f92af04e0e19'; Status = 'burst'; Stacks = 4 }
        )) {
            $matching = @($trace.Actions[0].Before.Spawn.Train.Context.Relics | Where-Object {
                $_.AssetKey -eq $owner.Key -and $_.DataId -eq $owner.Id -and $_.EffectTypes.Count -eq 1 -and
                $_.EffectTypes[0] -eq 'RelicEffectAddStatusEffectOnSpawn' -and $_.SpawnStatuses.Count -eq 1 -and
                $_.SpawnStatuses[0].TargetPlayer -and -not $_.SpawnStatuses[0].TargetEnemy -and
                $_.SpawnStatuses[0].Statuses.Count -eq 1 -and $_.SpawnStatuses[0].Statuses[0].Id -eq $owner.Status -and
                $_.SpawnStatuses[0].Statuses[0].Stacks -eq $owner.Stacks -and $_.SpawnStatuses[0].Conditions.Count -eq 0
            })
            if ($matching.Count -ne 1) { throw "Original combat spawn relic payload missing: $($owner.Key)" }
            $witnesses = 0
            foreach ($birth in $births) {
                $beforeActor = @($birth.Before.Units | Where-Object Id -EQ $birth.UnitId)[0]
                $afterActor = @($birth.After.Units | Where-Object Id -EQ $birth.UnitId)[0]
                if (-not $beforeActor -or -not $afterActor -or $beforeActor.Team -ne 1) { continue }
                $oldStatus = @($beforeActor.Statuses | Where-Object Id -EQ $owner.Status)
                $newStatus = @($afterActor.Statuses | Where-Object Id -EQ $owner.Status)
                $oldStacks = if ($oldStatus.Count) { [int]$oldStatus[0].Stacks } else { 0 }
                $newStacks = if ($newStatus.Count) { [int]$newStatus[0].Stacks } else { 0 }
                if ($newStacks -eq ($oldStacks + $owner.Stacks)) { $witnesses++ }
            }
            if ($witnesses -eq 0) { throw "Original combat spawn relic has no positive birth witness: $($owner.Key)" }
            Write-Output "ORIGINAL-COMBAT-SPAWN PASS: $($owner.Key) applied $($owner.Stacks) $($owner.Status) on $witnesses births."
        }
    }
    if ($MixedCardlessRelicOrder) {
        $mixedRelic = @($trace.Actions[0].Before.Spawn.Train.Context.Relics | Where-Object AssetKey -EQ 'PyreHeartSon')
        $mixedOrder = if ($mixedRelic.Count -eq 1) { @($mixedRelic[0].EffectTypes) -join '|' } else { '' }
        if ($mixedOrder -ne 'RelicEffectAddStatusEffectOnSpawn|RelicEffectAddTempUpgrade' -or $mixedOrderWitnesses -lt 1) {
            throw 'Mixed cardless relic ordering lacks the native status-before-upgrade witness.'
        }
        Write-Output "NATIVE-MIXED-CARDLESS-RELIC-ORDER PASS: status then cardless upgrade matched on $mixedOrderWitnesses births."
    }
    if ($SpawnStatusRelicsClones) {
        Write-Output "NATIVE-CARDLESS-RELIC-UPGRADE PASS: upgrade applied to $cardlessUpgradeApplied non-clone cardless births and skipped on $cardlessCloneSkipped cardless clones."
    }
}
if ($OriginalEggCard -or $OriginalEggCardDelayed) {
    $first = @($trace.Actions)[0]
    $expectedCorruption = if ($OriginalEggCardDelayed) { 4 } else { 8 }
    $cardId = '057f7f77-df4b-4981-9e6d-533a6e667f39'
    $playedCard = @($first.Before.Spawn.Train.Context.Cards.Hand | Where-Object {
        $_.InstanceId -eq $first.Action.CardInstanceId -and $_.DataId -eq $cardId
    })
    $registryCard = @($first.Before.Spawn.Train.Context.CardRegistry | Where-Object {
        $_.InstanceId -eq $first.Action.CardInstanceId -and $_.DataId -eq $cardId
    })
    $room = @($first.Before.Spawn.Train.Context.RoomCapacities | Where-Object RoomIndex -EQ 2)
    $egg = @($first.Actual.Spawn.Train.Rooms[2].Units | Where-Object AssetKey -EQ 'MonsterKinhostVessel')
    $hatches = @($trace.TriggeredSummons | Where-Object {
        $_.Kind -eq 'OnHatched' -and $_.SourceCardId -eq $first.Action.CardInstanceId -and
        $_.ActorBefore.AssetKey -eq 'MonsterKinhostVessel' -and
        $_.RuleBefore.CharacterId -eq 'e2f7a12a-b33b-4dbe-b95b-73f39cdf3a70' -and
        $_.ActorAfter.SacrificeCardId -eq $_.SourceCardId -and $_.Completed -and $null -eq $_.Error -and
        @($_.After.Units | Where-Object AssetKey -EQ 'MonsterKinhostPupa').Count -gt 0
    })
    if ($trace.ModifierScenario -ne $(if ($OriginalEggCardDelayed) { 'original-egg-card-delayed' } else { 'original-egg-card' }) -or $trace.Schema -ne 103 -or
        -not $trace.NativeWon -or $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or
        $trace.Unsupported -ne 0 -or $trace.Pending -ne 0 -or
        $first.Before.BattlePreviewEnabled -or $first.Action.RoomIndex -ne 2 -or
        $playedCard.Count -ne 1 -or $registryCard.Count -ne 1 -or $room.Count -ne 1 -or
        $room[0].Corruption -ne $expectedCorruption -or $room[0].MaxCorruption -ne $expectedCorruption -or
        $room[0].PermanentCorruption -ne $(if ($OriginalEggCardDelayed) { 4 } else { 0 }) -or $egg.Count -ne 1 -or
        @($egg[0].Statuses | Where-Object { $_.Id -eq 'hatch' -and $_.Stacks -eq 8 }).Count -ne 1 -or
        @($egg[0].Triggers | Where-Object Kind -EQ 'OnRemoveHatch').Count -ne 1 -or
        @($egg[0].Triggers | Where-Object Kind -EQ 'OnHatched').Count -ne 1 -or
        $hatches.Count -lt 1) {
        throw 'Original Kinhost Vessel card play and natural hatch witness incomplete.'
    }
    Write-Output "NATIVE-ORIGINAL-EGG-CARD PASS: unchanged Kinhost Vessel played in room 2; $($hatches.Count) original Pupa hatch."
    if ($OriginalEggCardDelayed) {
        $firstCombat = @($trace.Turns | Where-Object Index -EQ 1)
        $secondCombat = @($trace.Turns | Where-Object Index -EQ 2)
        $partial = @($firstCombat[0].Actual.Spawn.Train.Rooms[2].Units | Where-Object AssetKey -EQ 'MonsterKinhostVessel')
        $pupa = @($secondCombat[0].Actual.Spawn.Train.Rooms[2].Units | Where-Object AssetKey -EQ 'MonsterKinhostPupa')
        if ($firstCombat.Count -ne 1 -or $secondCombat.Count -ne 1 -or $partial.Count -ne 1 -or
            @($partial[0].Statuses | Where-Object { $_.Id -eq 'hatch' -and $_.Stacks -eq 4 }).Count -ne 1 -or
            $firstCombat[0].Actual.Spawn.Train.Context.RoomCapacities[2].Corruption -ne 4 -or
            $pupa.Count -ne 1) {
            throw 'Delayed Kinhost Vessel lacks two-combat partial Hatch and Pupa witness.'
        }
        Write-Output 'NATIVE-ORIGINAL-EGG-DELAYED PASS: first combat leaves Hatch 4; second combat summons Pupa.'
    }
}
if ($IncantRelic) {
    $players = @($incantFires | Where-Object { $_.Actor.Team -eq 1 })
    $enemies = @($incantFires | Where-Object { $_.Actor.Team -eq 0 })
    $context = $trace.Actions[0].Before.Spawn.Train.Context
    if ($trace.Schema -ne 109 -or $players.Count -eq 0 -or $enemies.Count -eq 0 -or
        @($players.Actor.Triggers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and $_.FireCount -ne 2 }).Count -ne 0 -or
        @($enemies.Actor.Triggers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and $_.FireCount -ne 1 }).Count -ne 0 -or
        @($context.Relics | Where-Object { $_.DataId -eq '410ba540-7c4f-4dc5-a84f-b1d8af508891' -and $_.AssetKey -eq 'ExtraSpellCastTrigger' }).Count -ne 1 -or
        @($context.TriggerCounts.Modifiers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and $_.Value -eq 1 }).Count -ne 1 -or
        @($context.TriggerCounts.EnemyAllowedKinds).Count -ne 0) { throw 'Original relic trigger-count coverage incomplete.' }
    Write-Output "NATIVE-RELIC-TRIGGER-COUNT PASS: $($players.Count) player dispatches with count2 and $($enemies.Count) enemy dispatches with count1; original acquired relic and compiled cache."
    if ($IncantRelics) {
        foreach ($kind in @('OnSpawn','OnDeath')) {
            if (@($context.TriggerCounts.Modifiers | Where-Object { $_.Kind -eq $kind -and $_.Value -eq 1 }).Count -ne 1 -or
                @($players.Actor.Triggers | Where-Object { $_.Kind -eq $kind -and $_.FireCount -eq 2 }).Count -eq 0 -or
                @($enemies.Actor.Triggers | Where-Object { $_.Kind -eq $kind -and $_.FireCount -eq 1 }).Count -eq 0) { throw "Combined relic count coverage missing: $kind" }
        }
        if (@($trace.Turns | Where-Object { $_.Actual.Spawn.Train.Context.Statistics.MonstersDeadThisBattle -gt 0 }).Count -eq 0) { throw 'Combined relic battle lacks real player deaths.' }
        Write-Output 'NATIVE-RELIC-COUNTS-COMBINED PASS: original Incant/spawn/death relics, new paid births and real player deaths.'
    }
}
$abilityIncantCoverage = -not $AbilityIncant
if ($AbilityIncant) {
    $abilityPhases = @($trace.IncantPhases | Where-Object IsAnyAbility)
    $abilityActions = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 })
    $abilityIncantCoverage = $trace.ModifierScenario -eq 'ability-incant' -and $abilityPhases.Count -ge 4 -and
        @($abilityActions | ForEach-Object { $_.Action.ActivatorUnitId } | Sort-Object -Unique).Count -ge 2 -and
        @($abilityPhases | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -ne $_.Difference }).Count -eq 0 -and
        @($trace.IncantTriggers | Where-Object { -not $_.Completed -or $null -eq $_.After -or $null -eq $_.AfterActor -or $null -ne $_.Difference }).Count -eq 0 -and
        @($trace.IncantTriggers | Where-Object { $_.Actor.Team -eq 0 }).Count -gt 0 -and
        @($trace.IncantTriggers | Where-Object { $_.Actor.Team -eq 1 }).Count -gt 0
    if (-not $abilityIncantCoverage) { throw "Ability Incant coverage incomplete: phases=$($abilityPhases.Count) actions=$($abilityActions.Count)" }
}
$incantThresholdCoverage = -not $IncantThresholds
if ($IncantThresholds) {
    $positiveThresholds = 0; $zeroThresholds = 0; $negativeThresholds = 0; $spentThresholds = 0; $incorrectThresholds = 0
    foreach ($fire in $incantFires) {
        if ($fire.ParamInt -ne 0 -or $fire.TriggerCount -ne 1) { $incorrectThresholds++ }
        foreach ($trigger in $fire.AfterActor.Triggers) {
            if ($trigger.Kind -ne 'CardSpellPlayed') { continue }
            if ($trigger.TriggerAtThreshold -gt 0 -and $trigger.HasTriggered) { $incorrectThresholds++ }
            if ($trigger.TriggerAtThreshold -eq 1 -and @($trigger.Effects | Where-Object { $_.Type -eq 'CardEffectRewardGold' -and $_.Value -eq 23 }).Count -gt 0) { $positiveThresholds++ }
            if ($trigger.TriggerAtThreshold -eq 0 -and $trigger.HasTriggered -and @($trigger.Effects | Where-Object { $_.Type -eq 'CardEffectRewardGold' -and $_.Value -eq 31 }).Count -gt 0) { $zeroThresholds++ }
            if ($trigger.TriggerAtThreshold -eq -3 -and $trigger.Once -and $trigger.HasTriggered -and @($trigger.Effects | Where-Object { $_.Type -eq 'CardEffectRewardGold' -and $_.Value -eq 13 }).Count -gt 0) { $negativeThresholds++ }
        }
        if (@($fire.Actor.Triggers | Where-Object { $_.Kind -eq 'CardSpellPlayed' -and $_.TriggerAtThreshold -eq -3 -and $_.Once -and $_.HasTriggered }).Count -gt 0) { $spentThresholds++ }
    }
    $incantThresholdCoverage = $positiveThresholds -gt 0 -and $zeroThresholds -gt 0 -and $negativeThresholds -gt 0 -and $spentThresholds -gt 0 -and $incorrectThresholds -eq 0
    if (-not $incantThresholdCoverage) { throw "Incant threshold coverage incomplete: positive=$positiveThresholds zero=$zeroThresholds negative=$negativeThresholds spent=$spentThresholds incorrect=$incorrectThresholds" }
}
$result = [pscustomobject]@{
    PurifyCoverage = $purifyCoverage
    PurifyQueueCoverage = $purifyQueueCoverage
    IncantCoverage = $incantCoverage
    AbilityIncantCoverage = $abilityIncantCoverage
    IncantThresholdCoverage = $incantThresholdCoverage
    RevivalOperations = @($trace.RevivalOperations).Count
    Revivals = @($trace.Revivals).Count
    TriggeredEquipmentCoverage = $triggeredEquipmentCoverage
    TriggeredEquipmentApplications = $triggeredEquipmentApplications
    OwnedSummonApplications = $ownedSummonApplications
    OwnedSummonBirths = $ownedSummonBirths
    OwnedSummonDyingBirths = $ownedSummonDyingBirths
    OwnedSummonRevivedBirths = $ownedSummonRevivedBirths
    SummonRevivalHostSamples = $summonRevivalHostSamples
    SummonRevivalChildSamples = $summonRevivalChildSamples
    QueuedEquipmentAttachments = $queuedEquipmentAttachments
    TriggeredSummonCoverage = $triggeredSummonCoverage
    TriggeredSummonBirths = $triggeredSummonBirths
    TriggeredSummonZeroBirths = $triggeredSummonZeroBirths
    TriggeredSummonDyingSources = $triggeredSummonDyingSources
    PhysicalSpawnPoints = $PhysicalSpawnPoints.IsPresent
    PhysicalCompactions = @($trace.PhysicalCompactions).Count
    SpawnPointOperations = @($trace.SpawnPointOperations).Count
    PoolSummonSelections = @($trace.PoolSummonSelections).Count
    UnitBirths = @($trace.UnitBirths).Count
    DetachedCardClones = @($trace.DetachedCardClones).Count
    SpawnUpgrades = @($trace.SpawnUpgrades).Count
    FreshSpawners = @($trace.FreshSpawners).Count
    GlobalStandbyChecks = @($trace.GlobalStandbyChecks).Count
    RallyOperations = @($trace.RallyOperations).Count
    RallyPhases = @($trace.RallyPhases).Count
    RallyTriggers = @($trace.RallyTriggers).Count
    DyingHordeUpgradeOperations = @($trace.DyingHordeUpgradeOperations).Count
    HordeUpgradeOperations = @($trace.HordeUpgradeOperations).Count
    HordeDeathOperations = @($trace.HordeDeathOperations).Count
    HordeRemovalOperations = @($trace.HordeRemovalOperations).Count
    HarvestOperations = @($trace.HarvestOperations).Count
    HarvestTriggers = @($trace.HarvestTriggers).Count
    HordeStatusCoverage = (-not $HordeStatuses -or @($trace.HordeStatusOperations).Count -eq 8)
    HordeStatusOperations = @($trace.HordeStatusOperations).Count
    HordeMergeCoverage = (-not $HordeMerge -or @($trace.HordeMergeOperations).Count -eq 17)
    HordeMergeOperations = @($trace.HordeMergeOperations).Count
    HordeMergeSelections = @($trace.HordeMergeSelections).Count
    BumpCoverage = (-not $Bump -or @($trace.BumpOperations).Count -eq 22)
    BumpOperations = @($trace.BumpOperations).Count
    UnitCloneOperations = @($trace.UnitCloneOperations).Count
    UnitCopyOperations = @($trace.UnitCopyOperations).Count
    HeroCopyOperations = @($trace.HeroCopyOperations).Count
    SpawnEnchantments = @($trace.SpawnEnchantments).Count
    EquipmentAbilityCoverage = $equipmentAbilityCoverage
    InitialAbilitySpawns = @($trace.InitialAbilitySpawns).Count
    EquipmentAbilityActivations = $equipmentActivations.Count
    AbilityEffectsCoverage = $abilityEffectsCoverage
    AbilityEffectOperations = @($trace.AbilityEffectOperations).Count
    AbilityLifecycleCoverage = $lifecycleCoverage
    AbilityLifecycleOperations = @($trace.AbilityLifecycleOperations).Count
    AbilityActivationCoverage = $activationCoverage
    AbilityActivationSamples = @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 }).Count
    AbilityTeleportCoverage = $teleportCoverage
    AbilityTeleportSameRoomMoves = $sameRoomTeleportMoves
    AbilityTeleportCrossRoomMoves = $crossRoomTeleportMoves
    AbilityCacheCoverage = $abilityCacheCoverage
    AbilityCacheSamples = @($trace.AbilityCardOperations).Count
    AbilityCooldownCoverage = $abilityCoverage
    AbilityCooldownSamples = @($trace.AbilityCooldownEffects).Count
    SentryCoverage = $sentryCoverage
    SentrySamples = @($trace.Sentries).Count
    CompanionBossCoverage = $companionCoverage
    Policy = $Policy
    Profile = $profile
    ExitCode = $process.ExitCode
    NativePassed = $nativePassed
    NativeWon = $trace.NativeWon
    CaptureFailures = $trace.CaptureFailures
    Mismatches = $trace.Mismatches
    Unsupported = $trace.Unsupported
    Pending = $trace.Pending
    Stages = @($trace.Stages).Count
    CardCycles = @($trace.CardCycles).Count
    TrainPhases = @($trace.TrainPhases).Count
    Spawns = @($trace.Spawns).Count
    EndTurns = @($trace.Turns).Count
    Actions = @($trace.Actions).Count
    DecisionTurns = @($trace.Checkpoints).Count - 1
    OriginalFilesUnchanged = $originalUnchanged
    ModifierActions = $modifierActions.Count
    ModifierCoverage = $modifierCoverage
    SacrificeEffectActions = $sacrificeEffectActions.Count
    SacrificeEffectCoverage = $sacrificeEffectCoverage
    SetHealthActions = $setHealthActions.Count
    SetHealthCoverage = $setHealthCoverage
    TransferAttackActions = $transferAttackActions.Count
    TransferAttackCoverage = $transferAttackCoverage
    BrainTransferActions = $brainTransferActions.Count
    BrainTransferCoverage = $brainTransferCoverage
    FrostChannelingActions = $frostChannelingActions.Count
    FrostChannelingCoverage = $frostChannelingCoverage
    TransferAbilityActions = $transferAbilityActions.Count
    TransferAbilityCoverage = $transferAbilityCoverage
    DamageByUnitsKilledActions = $damageByUnitsKilledActions.Count
    DamageByUnitsKilledCoverage = $damageByUnitsKilledCoverage
    CardEffectKillActions = $cardEffectKillActions.Count
    CardEffectKillCoverage = $cardEffectKillCoverage
    CardEffectKillSubtypeCoverage = $cardEffectKillSubtypeCoverage
    CardRecursionActions = $cardRecursionActions.Count
    CardRecursionCoverage = $cardRecursionCoverage
    CardRecursionUpgradeCoverage = if ($CardRecursionUpgrade -or $CardRecursionUpgradeToHand -or $CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose) { $cardRecursionCoverage } else { $null }
    HealActions = $healActions.Count
    HealingCoverage = $healingCoverage
    OnHealCoverage = $onHealCoverage
    RoomSpellCoverage = $roomSpellCoverage
    DamagePerHpCoverage = $damagePerHpCoverage
    DamagePerHpAbilityCoverage = $damagePerHpAbilityCoverage
    DamageOverflowCoverage = $damageOverflowCoverage
    TerminalEffectsSettled = $terminalSettled
    TerminalSpellCoverage = $terminalSpellCoverage
    PostKillCoverage = $postKillCoverage
    RandomCoverage = $randomCoverage
    RandomStatusCoverage = $randomStatusCoverage
    CrossRoomCoverage = $crossRoomCoverage
    AttackCoverage = $attackCoverage
    MaxHealthCoverage = $maxHealthCoverage
    NumericRangeCoverage = $numericRangeCoverage
    TargetFilterCoverage = $targetFilterCoverage
    DrawCoverage = $drawCoverage
    HandRemovalCoverage = $handRemovalCoverage
    RandomDiscardCoverage = $randomDiscardCoverage
    RandomDiscardApplications = @($trace.RandomDiscards).Count
    GenerationCoverage = $generationCoverage
    PermanentConsumeGenerationCoverage = $permanentConsumeGenerationCoverage
    PermanentConsumeOverTemporaryCoverage = (-not $PermanentConsumeOverTemporary -or $modifierCoverage)
    DamageScalingCoverage = $scalingCoverage
    StatusScalingCoverage = $statusScalingCoverage
    UnitUpgradeScalingCoverage = $unitUpgradeScalingCoverage
    UnitTriggerUpgradeCoverage = $unitTriggerUpgradeCoverage
    SpawnTriggerCoverage = $spawnTriggerCoverage
    UnitTurnBeginCoverage = $unitTurnBeginCoverage
    TeamTurnBeginCoverage = $teamTurnBeginCoverage
    PreHandDiscardCoverage = $preHandDiscardCoverage
    CloneUpgradeRefreshCoverage = $cloneUpgradeRefreshCoverage
    PreCombatCoverage = $preCombatCoverage
    DamageDeathQueueCoverage = $damageDeathQueueCoverage
    TerminalDeathDamageCoverage = $terminalDeathDamageCoverage
    DyingUpgradeCoverage = $dyingUpgradeCoverage
    DyingUpgradeSamples = @($trace.DyingUpgrades).Count
    TriggeredStatusCoverage = $triggeredStatusCoverage
    StatusCallbackCoverage = $statusCallbackCoverage
    EnergyCoverage = $energyCoverage
    EnergySamples = @($trace.EnergyEffects).Count
    ForgePointsCoverage = $forgePointsCoverage
    XCostCoverage = $xCostCoverage
    BonusDrawCoverage = $bonusDrawCoverage
    BonusDrawSamples = @($trace.BonusDrawEffects).Count
    DirectUnitUpgradeCoverage = $directUpgradeCoverage
    EquipmentCoverage = $equipmentCoverage
    EquipmentOperations = @($trace.EquipmentOperations).Count
    CapacityCoverage = $capacityCoverage
    CapacitySamples = @($trace.CapacityEffects).Count
    CapacityTests = @($trace.CapacityTests).Count
    RoomCapacityAttachmentCoverage = $roomCapacityAttachmentCoverage
    RoomCapacityAttachment = $trace.RoomCapacityAttachmentCoverage
    TriggeredStatusSamples = @($trace.TriggeredStatuses).Count
    AttackTriggerCoverage = $attackTriggerCoverage
    AttackTriggerSamples = @($trace.AttackTriggers).Count
    HitKillCoverage = $hitKillCoverage
    HitKillSamples = @($trace.HitKills).Count
    TriggeredDamageCoverage = $triggeredDamageCoverage
    TriggeredHealingCoverage = $triggeredHealingCoverage
    TriggeredHealSamples = @($trace.TriggeredHeals).Count
    PostCombatHealingCoverage = $postCombatHealingCoverage
    UnitPostCombatPhases = @($trace.UnitPostCombats).Count
    StatisticQueryCalibration = $(if ($StatisticQueries) { Join-Path $profile 'statistic-query-calibration.json' } else { $null })
    StatisticOverflowCalibration = $(if ($StatisticOverflow) { Join-Path $profile 'statistic-overflow-calibration.json' } else { $null })
    StatisticZeroIncrementCalibration = $(if ($StatisticOverflow) { Join-Path $profile 'statistic-zero-increment-calibration.json' } else { $null })
    Trace = $tracePath
    HordeStatCalibration = $(if ($HordeStats) { Join-Path $profile 'horde-stat-calibration.mt2f' } else { $null })
    EnchantmentLifecycleCalibration = $(if ($EnchantmentLifecycle) { Join-Path $profile 'enchantment-lifecycle-calibration.mt2f' } else { $null })
    EnchantmentCombatCalibration = $(if ($EnchantmentCombat) { Join-Path $profile 'enchantment-combat-calibration.mt2f' } else { $null })
    EnchantmentWorldCalibration = $(if ($EnchantmentWorld) { Join-Path $profile 'enchantment-world-calibration.mt2f' } else { $null })
    EnchantmentSourceOrderCalibration = $(if ($EnchantmentWorld) { Join-Path $profile 'enchantment-source-order-calibration.mt2f' } else { $null })
    CharacterRemovalCalibration = $(if ($CharacterRemoval) { Join-Path $profile 'character-removal-calibration.mt2f' } else { $null })
}
if (-not $sacrificeEffectCoverage) { throw 'Native CardEffectSacrifice was not exercised through a full battle action.' }
if (-not $setHealthCoverage) { throw 'Native CardEffectSetHealth did not set a living player unit to one health in a matched action.' }
if (-not $transferAttackCoverage) { throw 'Native attack transfer did not buff a front unit and its spawner card in a matched action.' }
if (-not $brainTransferCoverage) { throw 'Original BrainTransfer did not transfer Rage, Scorch and attack in a matched action.' }
if (-not $frostChannelingCoverage) { throw 'Original FrostChanneling did not combine cross-team poison stacks in a matched action.' }
if (-not $transferAbilityCoverage) { throw 'Native ability-user transfer did not buff its actor and spawner card in a matched action.' }
if (-not $damageByUnitsKilledCoverage) { throw 'Original Shardtail effect did not sacrifice an Imp and damage an enemy in a matched action.' }
if (-not $cardEffectKillCoverage) { throw 'Native CardEffectKill did not kill one enemy and one player unit in a full battle action.' }
if (-not $cardEffectKillSubtypeCoverage) { throw 'Native CardEffectKill did not kill a matching subtype target in a full battle action.' }
if (-not $cardRecursionCoverage) { throw 'Native WurmkinEtchings recursion did not restore the expected cards in a matched full battle action.' }
$result | ConvertTo-Json
if ($SettleDeathDissolves) {
    if (-not $trace.DeathDissolveSettlementEnabled -or $trace.Schema -ne $(if ($OriginalFloorCapacityRelics) { 119 } elseif ($PyreResourceRefresh) { 118 } elseif ($OriginalResourceRelicBatch) { 117 } elseif ($OriginalLifecycleRelicBatch) { 115 } elseif ($TypedDraw) { 112 } elseif ($SpawnStatusRelicsClones) { 111 } elseif ($SpawnStatusRelics) { 110 } elseif ($IncantRelic) { 109 } elseif ($AbilityIncant) { 108 } elseif ($SettleCardAnimations) { 106 } else { 105 }) -or
        @($trace.DeathDissolveSettlements).Count -eq 0 -or
        @($trace.DeathDissolveSettlements | Where-Object { -not $_.Completed -or $_.PendingAfter -ne 0 -or $_.Error }).Count -ne 0 -or
        @($trace.DeathDissolveCallbacks | Where-Object { $_.Error }).Count -ne 0) {
        throw 'Native death-dissolve settlement is incomplete or failed.'
    }
}
if ($SettleCardAnimations) {
    if ($trace.Schema -ne $(if ($OriginalFloorCapacityRelics) { 119 } elseif ($PyreResourceRefresh) { 118 } elseif ($OriginalResourceRelicBatch) { 117 } elseif ($OriginalLifecycleRelicBatch) { 115 } elseif ($TypedDraw) { 112 } elseif ($SpawnStatusRelicsClones) { 111 } elseif ($SpawnStatusRelics) { 110 } elseif ($IncantRelic) { 109 } elseif ($AbilityIncant) { 108 } else { 106 }) -or -not $trace.CardAnimationSettlement.Enabled -or
        $trace.CardAnimationSettlement.PreviewWaits -lt 0 -or $trace.CardAnimationSettlement.DecisionWaits -lt 0 -or
        $trace.CardAnimationSettlement.ScheduledMovements -le 0 -or $trace.CardAnimationSettlement.PendingMovements -ne 0) {
        throw 'Native card-animation settlement protocol is missing or invalid.'
    }
}
if ($CharacterRemoval) {
    $removalPath = Join-Path $profile 'character-removal-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $removalPath)) { throw 'Missing native character removal calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $removalArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($removalPath)
    try { $removal = $removalArchive.RootElement.ToObjectGraph() }
    finally { $removalArchive.Dispose() }
    if ($removal.Boundary -ne 'NativeCharacterRemovalApis' -or $removal.GameplaySuppressed -or
        @($removal.Errors).Count -ne 0 -or @($removal.Samples).Count -eq 0 -or
        @($removal.Samples | Where-Object { -not $_.Completed -or $null -ne $_.Difference }).Count -ne 0) {
        throw 'Native character removal API calibration is incomplete or differs.'
    }
}
if ($EnchantmentWorld) {
    $sourceOrderPath = Join-Path $profile 'enchantment-source-order-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $sourceOrderPath)) { throw 'Missing native enchantment source-order calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $sourceOrderArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($sourceOrderPath)
    try { $sourceOrderData = $sourceOrderArchive.RootElement.ToObjectGraph() }
    finally { $sourceOrderArchive.Dispose() }
    if ($sourceOrderData.Boundary -ne 'NativeManagerListCollection' -or $sourceOrderData.GameplaySuppressed -or
        @($sourceOrderData.Samples | Where-Object { $_.Label.StartsWith('sort-stress-') }).Count -ne 32 -or
        @($sourceOrderData.Samples | Where-Object { @($_.Positions.Team | Select-Object -Unique).Count -eq 2 }).Count -eq 0) {
        throw 'Native source-order both-team/sort-threshold coverage is incomplete.'
    }
    $enchantmentPath = Join-Path $profile 'enchantment-world-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $enchantmentPath)) { throw 'Missing native automatic aura world calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $enchantmentArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($enchantmentPath)
    try { $enchantment = $enchantmentArchive.RootElement.ToObjectGraph() }
    finally { $enchantmentArchive.Dispose() }
    if (-not $enchantment.LiveContextUnchanged -or $enchantment.Mismatches -ne 0 -or
        $enchantment.Boundary -ne 'AutomaticControlStatusAndQueue' -or $enchantment.StatusMutationsSuppressed -or
        -not $enchantment.ExternalPreviewPreparationsRecorded -or @($enchantment.Samples).Count -ne 32 -or
        @($enchantment.Samples | Where-Object Completed -NE $true).Count -gt 0 -or
        @($enchantment.Samples | Where-Object { $null -ne $_.Difference }).Count -gt 0) {
        throw 'Native automatic aura world status coverage is incomplete or differs.'
    }
}
if ($EnchantmentCombat) {
    $enchantmentPath = Join-Path $profile 'enchantment-combat-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $enchantmentPath)) { throw 'Missing native real-status enchantment combat calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $enchantmentArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($enchantmentPath)
    try { $enchantment = $enchantmentArchive.RootElement.ToObjectGraph() }
    finally { $enchantmentArchive.Dispose() }
    if (-not $enchantment.LiveContextUnchanged -or $enchantment.Mismatches -ne 0 -or
        $enchantment.Boundary -ne 'RealStatusAndDrainedQueue' -or $enchantment.StatusMutationsSuppressed -or
        -not $enchantment.ExternalPreviewPreparationsRecorded -or
        @($enchantment.Samples).Count -ne 41 -or @($enchantment.Samples | Where-Object Completed -NE $true).Count -gt 0 -or
        @($enchantment.Samples | Where-Object { $null -ne $_.Difference }).Count -gt 0) {
        throw 'Native real-status enchantment combat coverage is incomplete or differs.'
    }
}
if ($EnchantmentLifecycle) {
    $enchantmentPath = Join-Path $profile 'enchantment-lifecycle-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $enchantmentPath)) { throw 'Missing native enchantment lifecycle calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $enchantmentArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($enchantmentPath)
    try { $enchantment = $enchantmentArchive.RootElement.ToObjectGraph() }
    finally { $enchantmentArchive.Dispose() }
    if (-not $enchantment.LiveContextUnchanged -or $enchantment.Mismatches -ne 0 -or
        $enchantment.Boundary -ne 'StatusApiRequests' -or -not $enchantment.StatusMutationsSuppressed -or
        @($enchantment.Samples).Count -lt 1000 -or @($enchantment.Samples | Where-Object Completed -NE $true).Count -gt 0 -or
        @($enchantment.Samples | Where-Object { $null -ne $_.Difference }).Count -gt 0) {
        throw 'Native enchantment lifecycle coverage is incomplete or differs.'
    }
}
if ($HordeStats) {
    $hordePath = Join-Path $profile 'horde-stat-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $hordePath)) { throw 'Missing native Horde numerical calibration.' }
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $hordeArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($hordePath)
    $horde = $hordeArchive.RootElement.ToObjectGraph()
    $hordeArchive.Dispose()
    if (-not $horde.LiveContextUnchanged -or $horde.Mismatches -ne 0 -or @($horde.Samples).Count -ne 560 -or @($horde.Casualties).Count -ne 175) {
        throw 'Native Horde numerical coverage is incomplete or differs.'
    }
}
if ($StatisticQueries) {
    $queryPath = Join-Path $profile 'statistic-query-calibration.json'
    if (-not (Test-Path -LiteralPath $queryPath)) { throw 'Missing native statistic-query calibration.' }
    $queryCalibration = Get-Content -LiteralPath $queryPath -Raw | ConvertFrom-Json
    if (-not $queryCalibration.LiveContextUnchanged -or @($queryCalibration.Batches).Count -ne 6 -or
        @($queryCalibration.Batches.Samples).Count -lt 500) { throw 'Native statistic-query coverage is incomplete.' }
}
if ($StatisticOverflow) {
    $overflow = Get-Content -LiteralPath (Join-Path $profile 'statistic-overflow-calibration.json') -Raw | ConvertFrom-Json
    if (-not $overflow.LiveContextUnchanged -or @($overflow.Samples).Count -ne 65) { throw 'Native statistic-overflow coverage is incomplete.' }
    $zero = Get-Content -LiteralPath (Join-Path $profile 'statistic-zero-increment-calibration.json') -Raw | ConvertFrom-Json
    if (-not $zero.LiveContextUnchanged -or @($zero.Samples).Count -ne 48 -or
        @($zero.Samples | Where-Object DetachedSource -EQ $true).Count -ne 24 -or
        @($zero.Samples | Where-Object Amount -NE 0).Count -ne 0) { throw 'Native zero-increment coverage is incomplete.' }
}
if ($null -eq $trace.NativeWon -or $process.ExitCode -ne 0 -or -not $nativePassed -or -not $originalUnchanged -or -not $modifierCoverage -or -not $healingCoverage -or -not $onHealCoverage -or -not $roomSpellCoverage -or -not $damagePerHpCoverage -or -not $damagePerHpAbilityCoverage -or -not $damageOverflowCoverage -or -not $terminalSettled -or -not $terminalSpellCoverage -or
    -not $postKillCoverage -or -not $randomCoverage -or -not $randomStatusCoverage -or -not $crossRoomCoverage -or -not $attackCoverage -or -not $maxHealthCoverage -or -not $numericRangeCoverage -or -not $targetFilterCoverage -or -not $drawCoverage -or -not $handRemovalCoverage -or -not $randomDiscardCoverage -or -not $generationCoverage -or -not $permanentConsumeGenerationCoverage -or -not $scalingCoverage -or -not $statusScalingCoverage -or -not $unitUpgradeScalingCoverage -or -not $unitTriggerUpgradeCoverage -or -not $spawnTriggerCoverage -or -not $unitTurnBeginCoverage -or -not $teamTurnBeginCoverage -or -not $preHandDiscardCoverage -or -not $cloneUpgradeRefreshCoverage -or -not $preCombatCoverage -or -not $triggeredHealingCoverage -or -not $postCombatHealingCoverage -or -not $triggeredDamageCoverage -or -not $damageDeathQueueCoverage -or -not $terminalDeathDamageCoverage -or -not $hitKillCoverage -or -not $dyingUpgradeCoverage -or -not $attackTriggerCoverage -or -not $triggeredStatusCoverage -or -not $statusCallbackCoverage -or -not $energyCoverage -or -not $forgePointsCoverage -or -not $xCostCoverage -or -not $bonusDrawCoverage -or -not $capacityCoverage -or -not $directUpgradeCoverage -or -not $equipmentCoverage -or ($PermanentConsumeOverTemporary -and -not $modifierCoverage) -or $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0) {
    throw "Full battle differential probe failed; inspect $tracePath and $unityLog"
}
