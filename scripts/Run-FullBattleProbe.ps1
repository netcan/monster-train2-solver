#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('no-cards', 'steward-once', 'units-and-junk', 'units-spells-and-junk')]
    [string] $Policy = 'steward-once',
    [ValidateSet('Normal', 'Fast', 'Ultra', 'SuperUltra', 'Instant')]
    [string] $GameSpeed = 'Instant',
    [ValidateRange(1, 2)]
    [int] $NativeWorker = 1,
    [switch] $Planner,
    [switch] $PlannerTacticalSeed,
    [switch] $DotNetPlanner,
    [switch] $Advisor,
    [switch] $PlayerAdvisor,
    [switch] $RollingAdvice,
    [switch] $PreviewAttackTrace,
    [switch] $HoldoverCalibration,
    [switch] $HoldoverVariants,
    [switch] $PermanentTraits,
    [switch] $TitaniteCalibration,
    [switch] $TitaniteCombinations,
    [switch] $ConduitMovement,
    [switch] $ConduitEnemyMovement,
    [switch] $RoomMoonCallbacks,
    [switch] $RoomMoonCallbacksShade,
    [switch] $RoomMoonMovement,
    [switch] $RoomMoonEnemyMovement,
    [switch] $RoomMoonSpellUpgrades,
    [switch] $RoomMoonSpellAdvance,
    [switch] $RoomMoonSpellCostRelic,
    [switch] $RoomMoonSpellAttackRelic,
    [switch] $BossActionTargets,
    [switch] $BossActionControllers,
    [switch] $OuterBossPhases,
    [switch] $OuterBossRelentless,
    [ValidateRange(0, 100000)] [int] $OuterBossRelentlessHealth = 0,
    [ValidateRange(0, 99999)] [int] $OuterBossRelentlessPyreHealth = 0,
    [switch] $OuterBossTurns,
    [ValidateSet('', 'AllyBuffer', 'Fragile', 'Corrupter')][string] $OriginalOuterBossVariant = '',
    [switch] $OriginalOuterBossTrial,
    [switch] $TrueFinalBossBirth,
    [switch] $TrueFinalBossHealth,
    [switch] $OuterBossBirth,
    [switch] $OuterBossPyre,
    [string] $RollingInput = '',
    [string] $InputProfile = '',
    [string] $PrimaryClan = '',
    [string] $SecondaryClan = '',
    [ValidateRange(0, 20)] [int] $BattleDistance = 0,
    [switch] $ShowWindow,
    [ValidateRange(1, 2147483647)]
    [int] $Seed = 424242,
    [ValidateRange(0, 20)]
    [int] $PlannerTurn = 2,
    [ValidateRange(2, 16)]
    [int] $PlannerHorizon = 3,
    [ValidateRange(0, 2)] [int] $PlannerDeploymentRoom = 2,
    [ValidateRange(100, 30000)] [int] $PlannerTimeLimitMs = 3000,
    [string] $PlannerInput = '',
    [ValidateRange(60, 3600)]
    [int] $TimeoutSeconds = 600,
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
    [switch] $SoldierOfFortune,
    [switch] $RestorationDetonation,
    [switch] $RunCardGeneration,
    [switch] $ArmamentDrive,
    [switch] $Preserve,
    [switch] $CardDuplicate,
    [switch] $StardustInvocation,
    [switch] $AdaptiveMutation,
    [switch] $Mixalot,
    [switch] $SmallBox,
    [switch] $MagneticRelay,
    [switch] $TranscendImp,
    [switch] $FusedWeaponry,
    [switch] $EclipseChamber,
    [switch] $EclipseChamberCallbacks,
    [switch] $EclipseChamberShadeCallbacks,
    [switch] $EclipseChamberMovement,
    [switch] $EclipseChamberEnemyMovement,
    [switch] $EclipseChamberEnemyCallbacks,
    [switch] $EclipseChamberEnemyConduit,
    [switch] $EclipseChamberMixedConduit,
    [switch] $OriginalRoomCards,
    [switch] $OriginalForgeCards,
    [switch] $OriginalStatusCards,
    [switch] $OriginalDeckSpell,
    [switch] $OriginalDeepOffering,
    [switch] $OriginalMolded,
    [switch] $OriginalBrokenMemories,
    [switch] $OriginalWickedBlaze,
    [switch] $OriginalOfferingToken,
    [switch] $OriginalMoonRitual,
    [switch] $OriginalSelfPurgeEnergyGain,
    [switch] $OriginalSmeltedTreasure,
    [switch] $OriginalPyreGro,
    [switch] $OriginalDeadlyVariant,
    [switch] $OriginalDrain,
    [switch] $OriginalStarterFrostbiteDiscard,
    [switch] $OriginalFeedingFrenzy,
    [switch] $OriginalTitansGratitude,
    [switch] $OriginalPerfectInsanity,
    [switch] $OriginalStarterReform,
    [switch] $OriginalRapidRaise,
    [switch] $Goldmance,
    [switch] $EngineUpgrade,
    [switch] $StewardsQuarters,
    [switch] $ZoneOfSilence,
    [switch] $Memorial,
    [switch] $Planetarium,
    [switch] $GrowRoom,
    [switch] $MushyRoom,
    [switch] $OriginalPiercingCards,
    [switch] $InfernoRoom,
    [switch] $OriginalConsumeCards,
    [switch] $OriginalStrongMagicCards,
    [switch] $OriginalXDamageCards,
    [switch] $OriginalUnpurgableCards,
    [switch] $OriginalHandCosts,
    [switch] $OriginalLimitedRange,
    [switch] $OriginalMonsterHandCosts,
    [switch] $OriginalUnplayableCards,
    [switch] $OriginalPermafrostCards,
    [switch] $OriginalOverflowCards,
    [switch] $OriginalCorruptRestricted,
    [switch] $OriginalStatusStackDamage,
    [switch] $OriginalSpecificCardDamage,
    [switch] $OriginalScaledEnergy,
    [switch] $OriginalStatusRemovalScaling,
    [switch] $OriginalEffectApplications,
    [switch] $OriginalPileDraw,
    [switch] $OriginalCardDrawPotion,
    [switch] $OriginalAcceleratedIncubation,
    [switch] $OriginalEagerConscript,
    [switch] $OriginalSpawningPods,
    [switch] $OriginalMindsOfTheirOwn,
    [switch] $OriginalMetallurgy,
    [switch] $OriginalFertileGround,
    [switch] $OriginalCoverOfNight,
    [switch] $OriginalWaxenSpike,
    [switch] $OriginalImpPressive,
    [switch] $OriginalTomeOfEvergrowth,
    [switch] $OriginalTomeOfTheLostPath,
    [switch] $OriginalMakeItRain,
    [switch] $OriginalDeathroar,
    [switch] $OriginalGreenRecruit,
    [switch] $OriginalPrimordium,
    [switch] $OriginalCheeryDeathcap,
    [switch] $OriginalEnthusiasticFan,
    [switch] $OriginalCopyMorselTwice,
    [switch] $OriginalLazarusLeagueSpike,
    [switch] $OriginalTomeOfContracts,
    [switch] $OriginalPileRecursion,
    [switch] $OriginalScaledBuff,
    [switch] $OriginalPileStatus,
    [switch] $OriginalDrawUpgradeScaling,
    [switch] $OriginalConsumedUpgrade,
    [switch] $OriginalStatusCompositions,
    [switch] $OriginalGraftedEquipment,
    [switch] $OriginalSelfAttack,
    [switch] $OriginalHealingCleanse,
    [switch] $OriginalOnKill,
    [switch] $OriginalOnKillGeneration,
    [switch] $OriginalMorselEaten,
    [switch] $OriginalUnitBuffs,
    [switch] $OriginalKillCopy,
    [ValidateSet('', 'Greed', 'GoldenTouch', 'Bonk')][string] $OriginalKillResource = '',
    [switch] $OriginalFriendlyStatuses,
    [switch] $OriginalBasicDamage,
    [switch] $OriginalResourceSpells,
    [switch] $OriginalHostileStatuses,
    [switch] $OriginalScaledDamage,
    [switch] $OriginalMovementSpells,
    [switch] $OriginalRearrangeSpells,
    [switch] $OriginalSupportSpells,
    [string[]] $OriginalIsolatedSpellIds = @(),
    [string[]] $OriginalIsolatedSpellUpgrades = @(),
    [string] $OriginalUnitAbilityId = '',
    [switch] $OriginalEquipmentCards,
    [string[]] $OriginalIsolatedEquipmentIds = @(),
    [switch] $OriginalStatusUnits,
    [switch] $OriginalBaseChampions,
    [switch] $OriginalReactiveUnits,
    [switch] $OriginalAttackOverrides,
    [switch] $OriginalSupportUnits,
    [switch] $OriginalDivineDancer,
    [switch] $OriginalIsolatedUnitBatch,
    [switch] $OriginalIsolatedColdstrikeSiren,
    [string] $OriginalIsolatedUnitId = '',
    [string[]] $OriginalIsolatedUnitIds = @(),
    [switch] $IsolatedUnitConduit,
    [switch] $FailFastOnIsolatedUnitReject,
    [switch] $OriginalFuel,
    [switch] $OriginalScaledMoonPhase,
    [switch] $OriginalPositiveStatus,
    [switch] $OriginalSpellCompositions,
    [switch] $OriginalDiscardDraw,
    [switch] $OriginalMoonCardUpgrades,
    [switch] $OriginalMoonCardUpgradesRoomMask,
    [switch] $OriginalMoonCardUpgradesLunacy,
    [switch] $OriginalMoonCardUpgradesCostRelic,
    [switch] $OriginalMoonCardUpgradesAttackRelic,
    [switch] $OriginalRoomStatusDamage,
    [switch] $OriginalCorruptionDamage,
    [switch] $OriginalXStatusCards,
    [switch] $OriginalXBanishedSpike,
    [switch] $OriginalXLunaSpike,
    [switch] $OriginalXStygianSpike,
    [switch] $OriginalXWurmkinSpike,
    [switch] $OriginalMineCollapse,
    [switch] $OriginalImpOlate,
    [switch] $OriginalPyreshot,
    [switch] $OriginalRetch,
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
    [switch] $OriginalNullScourges,
    [switch] $OriginalTrample,
    [switch] $OriginalSpellShield,
    [switch] $ChooseDiscard,
    [switch] $ChooseDiscardOthers,
    [switch] $ChooseDiscardOthersPolicy,
    [switch] $ChooseDiscardRefine,
    [switch] $ChooseDiscardPersistent,
    [switch] $OriginalSmeltEmberRelic,
    [switch] $OriginalSmeltEmberRelicZero,
    [switch] $OriginalSmeltEmberRelicBulk,
    [switch] $RemoveAllStatusEffects,
    [switch] $DragonHoard,
    [switch] $TrainHeal,
    [switch] $TrainHealPerEmber,
    [switch] $TrainHealPerEmberX,
    [switch] $CopyOnPlay,
    [switch] $CopyOnPlayTerminal,
    [switch] $CopyOnPlayTerminalConsume,
    [switch] $CopyOnPlayTerminalPurge,
    [switch] $CopyOnPlayTerminalThreat,
    [switch] $Overheal,
    [switch] $Corruption,
    [switch] $CorruptionUnitTrigger,
    [switch] $CorruptionCarverTrigger,
    [switch] $CorruptionKeeperTrigger,
    [switch] $TowerCorruptionCallbacks,
    [switch] $CorruptionRelicTrigger,
    [switch] $PermanentCorruption,
    [switch] $MoonPhaseCard,
    [switch] $MoonPhaseAdvance,
    [switch] $MoonPhaseCallbacks,
    [switch] $ConditionalNoPlayerStatus,
    [switch] $SelfAttackStatus,
    [switch] $ConditionalPyreDamage,
    [switch] $ConditionalPyreLethal,
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
    [switch] $OriginalSecretIngredient,
    [switch] $OriginalTinkermasterMix,
    [switch] $OriginalProgressPacemakerMix,
    [switch] $OriginalCardTypeCostRelic,
    [switch] $OriginalConserveEmber,
    [ValidateSet('DiscountFirst', 'UpgradeFirst')]
    [string] $CardTypeCostUpgradeOrder,
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
    [switch] $RemovedStatusScaling,
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
    [switch] $OriginalDamageOnFloorTransition,
    [switch] $OriginalPyregelOnAscend,
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
    [switch] $CardCatalog,
    [switch] $CampaignRelicCalibration,
    [switch] $CampaignDraftCalibration,
    [switch] $CampaignPostBattleCalibration,
    [switch] $CampaignBattleResourceCalibration,
    [switch] $CampaignPurgeGoldCalibration,
    [switch] $CampaignDeckCopyCalibration,
    [switch] $CampaignDraftEnhancerCountCalibration,
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
    [switch] $OriginalTriggerRelicBatchStationary,
    [switch] $OriginalFusedPyreshard,
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
    [switch] $OriginalTwoChargesOnCardConsumed,
    [switch] $OriginalChanceForSpellWeakness,
    [switch] $OriginalChanceToCleanse,
    [switch] $OriginalExtraFeedTrigger,
    [switch] $OriginalSummonWithAvarice,
    [switch] $OriginalCardDrawOnCardPlay,
    [switch] $OriginalCarriageJack,
    [switch] $OriginalFirstRoomAttachment,
    [switch] $OriginalStartRoomAttachment,
    [switch] $OriginalStartRoomAttachmentFloor2,
    [switch] $OriginalStartRoomAttachmentFloor3,
    [switch] $UnplayedCardTrigger,
    [switch] $UnplayedCardEffects,
    [switch] $OriginalRoomEntryStatusRelics,
    [switch] $OriginalMoonPhaseEnergyRelic,
    [switch] $OriginalMoonPhaseHealRelic,
    [switch] $MoonHealCallbacks,
    [switch] $OriginalMoonPhaseStatusRelic,
    [switch] $OriginalCardAttackRelic,
    [switch] $OriginalPurgeToExhaustRelic,
    [switch] $OriginalUpgradeCountRelics,
    [switch] $OriginalHealingPowerRelic,
    [switch] $OriginalStartCombatEnergyRelic,
    [switch] $OriginalStartCombatHoardRelic,
    [switch] $OriginalForgeConservationRelic,
    [switch] $OriginalOpeningCorruptionRelics,
    [switch] $OriginalSpellPlayThresholdRelics,
    [switch] $OriginalCombatTriggerRelics,
    [switch] $OriginalConduitRelics,
    [switch] $OriginalEquipmentGrowthRelics,
    [switch] $OriginalHealthChoiceRelic,
    [switch] $OriginalAfterDrawGenerationRelics,
    [switch] $OriginalAddSnackOnKill,
    [switch] $OriginalFinalEndingArtifact,
    [switch] $OriginalBalatroEventYorick,
    [switch] $OriginalBalatroEventCavendish,
    [switch] $OriginalGarfieldBoxRandomCard,
    [switch] $OriginalPartFactory,
    [switch] $OriginalPerkeoRelic,
    [switch] $OriginalCorruptedAngelsRelic,
    [ValidateSet('cost-first', 'cost-last')]
    [string] $CorruptedAngelsCostOrder,
    [switch] $OriginalBigDamageFromManyMorsels,
    [switch] $OriginalEarlyDiscardEnergy,
    [switch] $OriginalCheaperSpellCost,
    [switch] $OriginalFirstCardCost,
    [switch] $OriginalRebalanceValor,
    [switch] $OriginalNefariousOverflow,
    [switch] $OriginalUncontrolledGroup,
    [switch] $OriginalCopyFirstSpellPlayed,
    [switch] $OriginalCopyFirstConsumePlayed,
    [switch] $OriginalCopyConsumedCard,
    [switch] $OriginalMothersRemnant3Relic,
    [switch] $OriginalTribouletRelic,
    [switch] $OriginalCooldownOnDamageRelic,
    [switch] $OriginalDeckTurnDrawRelic,
    [switch] $OriginalCardPlayDamageRelic,
    [switch] $OriginalConsumeDamageRelic,
    [switch] $OriginalEatenDamageRelic,
    [switch] $OriginalMoreCapacityRelic,
    [switch] $OriginalBurnoutDamageRelic,
    [switch] $OriginalHealDamageRelic,
    [switch] $OriginalKillAttackRelic,
    [switch] $OriginalChanceToSilenceRelic,
    [switch] $OriginalFreezeReduceCostRelic,
    [switch] $OriginalRandomFreezeRelic,
    [switch] $OriginalRandomHandUpgradeRelic,
    [switch] $OriginalStartTurnPropagationRelic,
    [switch] $OriginalMooncycleFaeRelic,
    [switch] $OriginalRandomDrawCostRelic,
    [switch] $OriginalSuppressExhaustRelic,
    [switch] $OriginalExpensiveCardUpgradeRelic,
    [switch] $OriginalWolosBloodletterRelic,
    [switch] $OriginalCardTypeSequenceRelic,
    [switch] $OriginalWhelpCooldownRelic,
    [switch] $OriginalSpawnRetreatRelic,
    [switch] $OriginalGrosMichelRelic,
    [switch] $OriginalBoneDogRelic,
    [switch] $OriginalDecayDeathRelic,
    [switch] $OriginalOpeningMorselsRelic,
    [switch] $CardBackedFeeding,
    [switch] $ForcedFeeding,
    [switch] $ShroudSpikeFeeding,
    [switch] $OriginalOpeningDeckSpawnRelic,
    [switch] $OriginalMonsterSpellTargeting,
    [switch] $OriginalFunguyDeckScaling,
    [switch] $OriginalCardDrawOnHeal,
    [switch] $OriginalBrokenRailRelic,
    [switch] $OriginalHephsBlueprints,
    [switch] $OriginalRageOnBurnoutStackLoss,
    [switch] $OriginalPyreRevenge,
    [switch] $OriginalExtractForEmberAndEchos,
    [switch] $OriginalUnitResourceRelics,
    [switch] $OriginalOpeningDrawRelics,
    [switch] $OriginalTurnDrawRelics,
    [switch] $OriginalUnitPhaseUpgradeRelics,
    [switch] $OriginalSoloUnitUpgradeRelic,
    [switch] $OriginalFrontRoomHealthUpgradeRelic,
    [switch] $OriginalChampionStartTurnHealRelic,
    [switch] $OriginalNimsOathRelic,
    [switch] $OriginalJudgesScalesRelic,
    [switch] $OriginalFirstImpBonusRelic,
    [switch] $OriginalMoonPhaseRoomsGainMagicPowerRelic,
    [switch] $OriginalPropagateOnStatusEffectAppliedRelic,
    [switch] $OriginalNoUpgradeCardCostRelic,
    [switch] $OriginalPyreHealthCardCostRelic,
    [switch] $OriginalNullRelic,
    [switch] $OriginalNullRelicBaseline,
    [switch] $OriginalRelicCountAttack,
    [switch] $OriginalMoreSpikes,
    [switch] $OriginalStealthSlay,
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
    [switch] $NullAbility,
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
if ($RoomMoonSpellAttackRelic) { $RoomMoonSpellAdvance = $true }
if ($RoomMoonSpellAttackRelic -and $RoomMoonSpellCostRelic) { throw 'Use separate isolated relic controls.' }
if ($RoomMoonSpellCostRelic) { $RoomMoonSpellAdvance = $true }
if ($RoomMoonSpellAdvance) { $RoomMoonSpellUpgrades = $true }
if ($RoomMoonSpellUpgrades) { $RoomMoonCallbacks = $true }
if ($RoomMoonEnemyMovement) { $RoomMoonMovement = $true }
if ($RoomMoonMovement) { $RoomMoonCallbacksShade = $true }
if ($RoomMoonCallbacksShade) { $RoomMoonCallbacks = $true }
if ($RoomMoonCallbacks) {
    $allowed = @('RoomMoonCallbacks', 'RoomMoonCallbacksShade', 'RoomMoonMovement', 'RoomMoonEnemyMovement', 'RoomMoonSpellUpgrades', 'RoomMoonSpellAdvance', 'RoomMoonSpellCostRelic', 'RoomMoonSpellAttackRelic', 'NativeWorker', 'GameSpeed', 'SkipBuild', 'TimeoutSeconds', 'Verbose', 'Debug', 'ErrorAction')
    if (@($PSBoundParameters.Keys | Where-Object { $_ -notin $allowed }).Count) { throw 'RoomMoonCallbacks is an isolated component calibration.' }
    if ($RoomMoonSpellUpgrades -and ($RoomMoonCallbacksShade -or $RoomMoonMovement -or $RoomMoonEnemyMovement)) { throw 'Spell upgrade controls are a separate isolated component.' }
    $Policy = 'no-cards'; $PhysicalSpawnPoints = $true; $BranchCardMasks = $true
}
if ($ConduitEnemyMovement) { $ConduitMovement = $true }
if ($ConduitMovement) {
    $allowed = @('ConduitMovement', 'ConduitEnemyMovement', 'NativeWorker', 'GameSpeed', 'SkipBuild', 'TimeoutSeconds', 'Verbose', 'Debug', 'ErrorAction')
    if (@($PSBoundParameters.Keys | Where-Object { $_ -notin $allowed }).Count) { throw 'ConduitMovement is an isolated component calibration; do not combine battle scenarios.' }
    $Policy = 'no-cards'; $PhysicalSpawnPoints = $true; $BranchCardMasks = $true
}
if ($PlayerAdvisor) {
    if ($Advisor -or $DotNetPlanner) {
        throw 'PlayerAdvisor uses the real in-process player overlay without a development advisor.'
    }
    $Planner = -not [bool]$RollingAdvice
    # Observe the same native mechanisms in the independent development trace.
    $BranchCardMasks = $true
    $PhysicalSpawnPoints = $true
}
if ($OuterBossRelentless -and -not $OuterBossPhases) { throw '-OuterBossRelentless requires -OuterBossPhases.' }
if ($OuterBossRelentlessHealth -and -not $OuterBossRelentless) { throw '-OuterBossRelentlessHealth requires -OuterBossRelentless.' }
if ($OuterBossRelentlessPyreHealth -and -not $OuterBossRelentless) { throw '-OuterBossRelentlessPyreHealth requires -OuterBossRelentless.' }
if ($OuterBossTurns -and -not $PhysicalSpawnPoints) { throw '-OuterBossTurns requires -PhysicalSpawnPoints.' }
if ($OriginalOuterBossVariant -and ($InputProfile -or $BattleDistance -ne 7 -or -not $OuterBossTurns)) {
    throw 'OriginalOuterBossVariant requires a fresh isolated Ring8 route with OuterBossTurns.'
}
if ($OriginalOuterBossTrial -and -not $OriginalOuterBossVariant) { throw 'OriginalOuterBossTrial requires an explicit original outer-boss variant.' }
if ($TrueFinalBossHealth) { $TrueFinalBossBirth = $true }
if ($TrueFinalBossBirth) {
    $allowed = @('TrueFinalBossBirth', 'TrueFinalBossHealth', 'NativeWorker', 'GameSpeed', 'SkipBuild', 'TimeoutSeconds', 'Verbose', 'Debug', 'ErrorAction')
    if (@($PSBoundParameters.Keys | Where-Object { $_ -notin $allowed }).Count) { throw 'TrueFinalBossBirth is an isolated original birth observation.' }
    $Policy = 'no-cards'; $BattleDistance = 8; $PhysicalSpawnPoints = $true; $BranchCardMasks = $true
}
if ($OuterBossBirth -and -not $OuterBossTurns) { throw '-OuterBossBirth requires -OuterBossTurns.' }
if ($OuterBossPyre -and -not $OuterBossTurns) { throw '-OuterBossPyre requires -OuterBossTurns.' }
if ($RollingInput -and -not $RollingAdvice) { throw 'RollingInput requires RollingAdvice.' }
if (($PrimaryClan -or $SecondaryClan) -and $InputProfile) { throw 'Clan selection requires a fresh native run and cannot be combined with InputProfile.' }
if ($PrimaryClan -and $SecondaryClan -and $PrimaryClan -ieq $SecondaryClan) { throw 'PrimaryClan and SecondaryClan must be different.' }
if ($RollingAdvice) {
    if ($Planner) { throw 'RollingAdvice and a frozen-window Planner run are separate test modes.' }
    $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true; $PhysicalSpawnPoints = $true
    $PlannerTurn = 0; $Advisor = -not [bool]$RollingInput -and -not [bool]$PlayerAdvisor
}
if (($OriginalStartRoomAttachment -and $OriginalStartRoomAttachmentFloor2) -or
    ($OriginalStartRoomAttachment -and $OriginalStartRoomAttachmentFloor3) -or
    ($OriginalStartRoomAttachmentFloor2 -and $OriginalStartRoomAttachmentFloor3)) {
    throw 'Select one original start-room attachment relic owner.'
}
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
if ($RestorationDetonation) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($RunCardGeneration) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ArmamentDrive) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($Preserve) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($CardDuplicate) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($StardustInvocation) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($AdaptiveMutation) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($Mixalot) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($SmallBox) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($MagneticRelay) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($TranscendImp) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($FusedWeaponry) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($EclipseChamberMixedConduit) { $EclipseChamberEnemyConduit = $true }
if ($EclipseChamberEnemyConduit) { $EclipseChamberEnemyCallbacks = $true }
if ($EclipseChamberEnemyCallbacks) { $EclipseChamberEnemyMovement = $true }
if ($EclipseChamberEnemyMovement) { $EclipseChamberMovement = $true }
if ($EclipseChamberMovement) { $EclipseChamberShadeCallbacks = $true; $PhysicalSpawnPoints = $true }
if ($EclipseChamberShadeCallbacks) { $EclipseChamberCallbacks = $true }
if ($EclipseChamberCallbacks) { $EclipseChamber = $true }
if ($EclipseChamber) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalRoomCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalForgeCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStatusCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDeckSpell) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDeepOffering) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMolded) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalBrokenMemories) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalWickedBlaze) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalOfferingToken) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMoonRitual) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSelfPurgeEnergyGain) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSmeltedTreasure) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPyreGro) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDeadlyVariant) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDrain) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStarterFrostbiteDiscard) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFeedingFrenzy) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalTitansGratitude) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPerfectInsanity) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStarterReform) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalRapidRaise) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($Goldmance) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($EngineUpgrade) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($StewardsQuarters) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ZoneOfSilence) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($Memorial) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($Planetarium) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($GrowRoom) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($MushyRoom) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPiercingCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($InfernoRoom) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalConsumeCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStrongMagicCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalXDamageCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalUnpurgableCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalHandCosts) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalLimitedRange) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMonsterHandCosts) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalUnplayableCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPermafrostCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($HoldoverCalibration -or $HoldoverVariants -or $PermanentTraits) { $BranchCardMasks = $true }
if ($OriginalOverflowCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCorruptRestricted) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStatusStackDamage) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSpecificCardDamage) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalScaledEnergy) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStatusRemovalScaling) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalEffectApplications) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPileDraw) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCardDrawPotion) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalAcceleratedIncubation) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalEagerConscript) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSpawningPods) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMindsOfTheirOwn) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMetallurgy) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFertileGround) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCoverOfNight) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalWaxenSpike) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalImpPressive) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalTomeOfEvergrowth) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalTomeOfTheLostPath) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMakeItRain) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDeathroar) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalGreenRecruit) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPrimordium) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCheeryDeathcap) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalEnthusiasticFan) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCopyMorselTwice) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalLazarusLeagueSpike) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalTomeOfContracts) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPileRecursion) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalScaledBuff) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPileStatus) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDrawUpgradeScaling) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalConsumedUpgrade) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStatusCompositions) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalGraftedEquipment) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSelfAttack) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalHealingCleanse) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalOnKill) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalOnKillGeneration) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMorselEaten) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalUnitBuffs) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalKillCopy) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalKillResource) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFriendlyStatuses) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalBasicDamage) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalResourceSpells) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalHostileStatuses) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFuel) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSupportUnits) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDivineDancer) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalIsolatedUnitBatch) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalIsolatedColdstrikeSiren) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
$isolatedUnitIds = @()
if ($OriginalIsolatedUnitId -and $OriginalIsolatedUnitIds.Count -gt 0) { throw 'Use either OriginalIsolatedUnitId or OriginalIsolatedUnitIds, not both.' }
if ($OriginalIsolatedUnitIds.Count -gt 0) { $isolatedUnitIds = @($OriginalIsolatedUnitIds | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
elseif ($OriginalIsolatedUnitId) { $isolatedUnitIds = @($OriginalIsolatedUnitId) }
if ($isolatedUnitIds.Count -gt 0) {
    foreach ($index in 0..($isolatedUnitIds.Count - 1)) {
        if ($isolatedUnitIds[$index] -notmatch '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$') { throw 'Isolated unit IDs must be card UUIDs.' }
        $isolatedUnitIds[$index] = $isolatedUnitIds[$index].ToLowerInvariant()
    }
    if (@($isolatedUnitIds | Select-Object -Unique).Count -ne $isolatedUnitIds.Count) { throw 'Isolated unit ID list contains duplicates.' }
    if ($OriginalIsolatedUnitIds.Count -gt 0) { $OriginalIsolatedUnitIds = $isolatedUnitIds }
    else { $OriginalIsolatedUnitId = $isolatedUnitIds[0] }
    $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk'
}
if ($IsolatedUnitConduit) {
    if ($isolatedUnitIds.Count -eq 0) { throw 'IsolatedUnitConduit requires original isolated unit IDs.' }
    $PhysicalSpawnPoints = $false
}
if ($OriginalAttackOverrides) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalReactiveUnits) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalBaseChampions) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStatusUnits) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalEquipmentCards) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalIsolatedEquipmentIds.Count -gt 0) {
    $OriginalIsolatedEquipmentIds = @($OriginalIsolatedEquipmentIds | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ })
    if ($OriginalIsolatedEquipmentIds.Count -eq 0 -or $OriginalIsolatedEquipmentIds.Count -gt 30 -or
        @($OriginalIsolatedEquipmentIds | Where-Object { $_ -notmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' }).Count -gt 0 -or
        @($OriginalIsolatedEquipmentIds | Select-Object -Unique).Count -ne $OriginalIsolatedEquipmentIds.Count) { throw 'Isolated equipment IDs must be one to thirty distinct native card UUIDs.' }
    $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk'
}
if ($OriginalSupportSpells) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalIsolatedSpellIds.Count -gt 0) {
    $OriginalIsolatedSpellIds = @($OriginalIsolatedSpellIds | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ })
    if ($OriginalIsolatedSpellIds.Count -eq 0 -or $OriginalIsolatedSpellIds.Count -gt 30 -or
        @($OriginalIsolatedSpellIds | Where-Object { $_ -notmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' }).Count -gt 0 -or
        @($OriginalIsolatedSpellIds | Select-Object -Unique).Count -ne $OriginalIsolatedSpellIds.Count) { throw 'Isolated spell IDs must be one to thirty distinct native card UUIDs.' }
    $BranchCardMasks = $true; $Policy = 'units-spells-and-junk'
}
if ($OriginalRearrangeSpells) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMovementSpells) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalScaledDamage) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalScaledMoonPhase) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPositiveStatus) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSpellCompositions) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDiscardDraw) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMoonCardUpgradesAttackRelic -and $OriginalMoonCardUpgradesCostRelic) { throw 'Use separate paid relic composition controls.' }
if ($OriginalMoonCardUpgradesAttackRelic -or $OriginalMoonCardUpgradesCostRelic) { $OriginalMoonCardUpgradesLunacy = $true }
if ($OriginalMoonCardUpgradesLunacy) { $OriginalMoonCardUpgradesRoomMask = $true }
if ($OriginalMoonCardUpgradesRoomMask) { $OriginalMoonCardUpgrades = $true }
if ($OriginalMoonCardUpgrades) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalRoomStatusDamage) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCorruptionDamage) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalXStatusCards) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalXBanishedSpike) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalXLunaSpike) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalXStygianSpike) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalXWurmkinSpike) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMineCollapse) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalImpOlate) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPyreshot) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalRetch) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
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
if ($OriginalTwoChargesOnCardConsumed) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalChanceForSpellWeakness) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalChanceToCleanse) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalExtraFeedTrigger) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalSummonWithAvarice) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalCheaperSpellCost) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFirstCardCost) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalRebalanceValor) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalCardDrawOnCardPlay) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCopyFirstSpellPlayed -or $OriginalCopyFirstConsumePlayed -or $OriginalCopyConsumedCard -or $OriginalMothersRemnant3Relic -or $OriginalTribouletRelic -or $OriginalCooldownOnDamageRelic -or $OriginalMonsterSpellTargeting) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFunguyDeckScaling) { $PhysicalSpawnPoints = $true; $Policy = 'no-cards' }
if ($OriginalCardDrawOnHeal) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalBrokenRailRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalHephsBlueprints) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalRageOnBurnoutStackLoss) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPyreRevenge) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalExtractForEmberAndEchos) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalCarriageJack) { $Policy = 'units-and-junk' }
if ($OriginalFirstRoomAttachment) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalStartRoomAttachment -or $OriginalStartRoomAttachmentFloor2 -or $OriginalStartRoomAttachmentFloor3) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($UnplayedCardTrigger) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($UnplayedCardEffects) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalRoomEntryStatusRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMoonPhaseEnergyRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($MoonHealCallbacks) { $OriginalMoonPhaseHealRelic = $true }
if ($RemovedStatusScaling) { $UnitUpgradeScaling = $true; $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($TowerCorruptionCallbacks) { $CorruptionKeeperTrigger = $true }
if ($NullAbility) { $AbilityActivation = $true; $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMoonPhaseHealRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMoonPhaseStatusRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCardAttackRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalBalatroEventCavendish) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalUpgradeCountRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalHealingPowerRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStartCombatEnergyRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStartCombatHoardRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalForgeConservationRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalOpeningCorruptionRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSpellPlayThresholdRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCombatTriggerRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalConduitRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalEquipmentGrowthRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalHealthChoiceRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalAfterDrawGenerationRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($CampaignDraftCalibration) { $BranchCardMasks = $true }
if ($OriginalAddSnackOnKill) {
    $BranchCardMasks = $true
    if (-not $PSBoundParameters.ContainsKey('Policy')) { $Policy = 'units-spells-and-junk' }
    if ($Policy -notin @('units-and-junk', 'units-spells-and-junk')) {
        throw 'Original AddSnackOnKill requires a model-driven card policy so recorded actions remain searchable.'
    }
}
if ($OriginalFinalEndingArtifact) {
    $BranchCardMasks = $true
    if (-not $PSBoundParameters.ContainsKey('Policy')) { $Policy = 'units-spells-and-junk' }
    if ($Policy -notin @('units-and-junk', 'units-spells-and-junk')) {
        throw 'Original FinalEndingArtifact requires a model-driven card policy so recorded actions remain searchable.'
    }
}
if ($OriginalBalatroEventYorick) {
    $BranchCardMasks = $true
    if (-not $PSBoundParameters.ContainsKey('Policy')) { $Policy = 'units-spells-and-junk' }
    if ($Policy -notin @('units-and-junk', 'units-spells-and-junk')) {
        throw 'Original BalatroEventYorick requires a model-driven card policy so recorded actions remain searchable.'
    }
}
if ($OriginalBalatroEventCavendish) {
    $BranchCardMasks = $true
    if (-not $PSBoundParameters.ContainsKey('Policy')) { $Policy = 'units-spells-and-junk' }
    if ($Policy -notin @('units-and-junk', 'units-spells-and-junk')) {
        throw 'Original BalatroEventCavendish requires a model-driven card policy so card modifiers remain searchable.'
    }
}
if ($OriginalGarfieldBoxRandomCard) {
    $BranchCardMasks = $true
    if ($Policy -eq 'steward-once') { $Policy = 'units-and-junk' }
}
if ($OriginalPartFactory) { $Policy = 'units-spells-and-junk' }
if ($OriginalPerkeoRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($CorruptedAngelsCostOrder) { $OriginalCorruptedAngelsRelic = $true }
if ($OriginalCorruptedAngelsRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalBigDamageFromManyMorsels) {
    $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $SettleDeathDissolves = $true; $SettleCardAnimations = $true
    $Policy = 'units-spells-and-junk'
}
if ($OriginalDamageOnFloorTransition) { $Bump = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPyregelOnAscend) { $Bump = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalEarlyDiscardEnergy) { $BranchCardMasks = $true; $HandRemoval = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalUnitResourceRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalOpeningDrawRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalTurnDrawRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalDeckTurnDrawRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalCardPlayDamageRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalConsumeDamageRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalEatenDamageRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalMoreCapacityRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalBurnoutDamageRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalHealDamageRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalKillAttackRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalChanceToSilenceRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalFreezeReduceCostRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalRandomFreezeRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalRandomHandUpgradeRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalStartTurnPropagationRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalMooncycleFaeRelic) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalRandomDrawCostRelic) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalSuppressExhaustRelic) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalExpensiveCardUpgradeRelic) { $BranchCardMasks = $true; $Policy = 'units-and-junk' }
if ($OriginalWolosBloodletterRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalCardTypeSequenceRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalWhelpCooldownRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalSpawnRetreatRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalGrosMichelRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalBoneDogRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalDecayDeathRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($ShroudSpikeFeeding) { $ForcedFeeding = $true }
if ($ForcedFeeding) { $CardBackedFeeding = $true }
if ($CardBackedFeeding) { $OriginalOpeningMorselsRelic = $true }
if ($OriginalOpeningMorselsRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalOpeningDeckSpawnRelic) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-and-junk' }
if ($OriginalUnitPhaseUpgradeRelics) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSoloUnitUpgradeRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFrontRoomHealthUpgradeRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalChampionStartTurnHealRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalNimsOathRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalJudgesScalesRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalFirstImpBonusRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMoonPhaseRoomsGainMagicPowerRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPropagateOnStatusEffectAppliedRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalNoUpgradeCardCostRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalPyreHealthCardCostRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalNullRelic -or $OriginalNullRelicBaseline) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalRelicCountAttack) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalMoreSpikes) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalStealthSlay) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalUnitUpgradeTriggerRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalTriggerRelicBatch -or $OriginalTriggerRelicBatchStationary -or $OriginalFusedPyreshard -or $OriginalLifecycleRelicBatch) {
    $BranchCardMasks = $true
    if (-not $OriginalTriggerRelicBatchStationary) {
        $PhysicalSpawnPoints = $true; $SettleDeathDissolves = $true; $SettleCardAnimations = $true
    }
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
if ($SoldierOfFortune) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($DamageByUnitsKilled) { $Policy = 'units-spells-and-junk' }
if ($TransferAttack) { $Policy = 'units-spells-and-junk' }
if ($BrainTransfer) { $Policy = 'units-spells-and-junk' }
if ($FrostChanneling) { $Policy = 'units-spells-and-junk' }
if ($TransferAttackAbility) { $Policy = 'units-spells-and-junk' }
if ($CardEffectKill -and $CardEffectKillSubtype) { throw 'Choose either -CardEffectKill or -CardEffectKillSubtype.' }
if ($CardEffectKill -or $CardEffectKillSubtype) { $Policy = 'units-spells-and-junk' }
if ($NullForwarding) { $Policy = 'units-spells-and-junk' }
if ($NullTriggers) { $Policy = 'units-spells-and-junk' }
if ($OriginalNullScourges) { $NullTargetless = $true; $BranchCardMasks = $true }
if ($NullTargetless) { $Policy = 'units-spells-and-junk' }
if ($OriginalTrample) { $Policy = 'units-spells-and-junk' }
if ($OriginalSpellShield) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscard) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscardOthers) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscardOthersPolicy) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscardRefine) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($ChooseDiscardPersistent) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSmeltEmberRelic) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($OriginalSmeltEmberRelicZero -or $OriginalSmeltEmberRelicBulk) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($RemoveAllStatusEffects) { $Policy = 'units-spells-and-junk' }
if ($OriginalNefariousOverflow) { $Mix = $true; $MixUnstableExplosion = $true }
if ($OriginalUncontrolledGroup -and $MixChoices -and $MixChoices -ne '01') { throw 'UncontrolledGroup requires Mix choices 01.' }
if ($OriginalUncontrolledGroup) { $Mix = $true; $MixChoices = '01' }
if ($DragonHoard) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($TrainHeal) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($TrainHealPerEmber) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($TrainHealPerEmberX) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
if ($CopyOnPlayTerminalConsume -or $CopyOnPlayTerminalPurge -or $CopyOnPlayTerminalThreat) { $CopyOnPlayTerminal = $true }
if ($CopyOnPlayTerminal) { $CopyOnPlay = $true }
if ($CopyOnPlay) { $BranchCardMasks = $true; $PhysicalSpawnPoints = $true; $Policy = 'units-spells-and-junk' }
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
if ($ConditionalPyreLethal) { $Policy = 'units-spells-and-junk'; $PhysicalSpawnPoints = $true; $BranchCardMasks = $true }
if ($SoulSiphon) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($StatusUpgrade) { $BranchCardMasks = $true; $Policy = 'units-spells-and-junk' }
if ($CardRecursion -or $CardRecursionDiscard -or $CardRecursionChoose -or $CardRecursionChooseToHand -or $CardRecursionChooseFilter -or $CardRecursionUpgrade -or $CardRecursionUpgradeToHand -or $CardRecursionUpgradeChoose -or $CardRecursionUpgradeFilterChoose -or $CardRecursionUpgradeReplacementChoose) { $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true }
if ($MixAlternate -and $MixChoices) { throw 'Choose either -MixAlternate or -MixChoices.' }
if ($OriginalSecretIngredient) {
    if ($Mix -or $MixAlternate -or $MixUnstableExplosion -or $OriginalTinkermasterMix -or $OriginalProgressPacemakerMix) {
        throw 'OriginalSecretIngredient is an isolated Mix scenario.'
    }
    if (-not $MixChoices) { $MixChoices = '00' }
}
if ($MixAlternate) { $MixChoices = '11' }
if ($MixUnstableExplosion -and $MixChoices -and $MixChoices -ne '01') { throw 'Unstable explosion requires Mix choices 01.' }
if ($MixUnstableExplosion) { $MixChoices = '01' }
if ($MixChoices) { $Mix = $true }
if ($Mix) { $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true }
if ($OriginalTinkermasterMix -or $OriginalProgressPacemakerMix) { $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true }
if ($OriginalCardTypeCostRelic -and $CardTypeCostUpgradeOrder) { throw 'Select the original cost witness or its ordering variant, not both.' }
if ($OriginalCardTypeCostRelic -or $CardTypeCostUpgradeOrder) { $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true }
if ($OriginalPurgeToExhaustRelic) { $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true }
if ($OriginalConserveEmber) { $Policy = 'units-spells-and-junk'; $BranchCardMasks = $true }
if ($SpawnOrStatus) { $Policy = 'units-and-junk' }
if ($AbilityTeleport) { $Policy = 'units-and-junk'; $PhysicalSpawnPoints = $true }
if ($damagePerHpModes.Count -gt 0) { $Policy = 'units-spells-and-junk' }
if ($DamagePerHpAbility) { $Policy = 'units-spells-and-junk' }
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$gameRoot = Join-Path $workspace $(if ($NativeWorker -eq 2) { '.sandbox-game-worker2' } else { '.sandbox-game' })
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$profile = Join-Path $workspace ('.probe-runs\full-battle-' + $Policy + '-' + $runId)
$originalLogRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\Shiny Shoe\MonsterTrain2'

function Get-OriginalSignature {
    $parts = Get-ChildItem -LiteralPath $originalLogRoot -File -Force -Recurse |
        Where-Object { $_.Extension -ne '.log' } |
        Sort-Object FullName | ForEach-Object {
            $_.FullName + ':' + $_.LastWriteTimeUtc.Ticks + ':' +
                (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    return [string]::Join('|', [string[]] @($parts))
}

$workerLockPath = Join-Path $workspace ".probe-runs\native-worker-$NativeWorker.lock"
$playerCheckPlugin = $null
$playerConfigPath = $null
$playerConfigBackup = $null
$process = $null
try {
    $nativeWorkerLease = [IO.File]::Open($workerLockPath, [IO.FileMode]::OpenOrCreate,
        [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
} catch {
    throw "Native worker $NativeWorker is already reserved or its lock is unavailable: $workerLockPath"
}
try {
# A prior launcher may have been interrupted while its game process survived.
$nativeGamePath = Join-Path $gameRoot 'MonsterTrain2.exe'
$existingGame = @(Get-CimInstance Win32_Process -Filter "Name = 'MonsterTrain2.exe'" |
    Where-Object { $_.ExecutablePath -ieq $nativeGamePath })
if ($existingGame.Count -gt 0) {
    throw "Native worker $NativeWorker is already running (PID $($existingGame.ProcessId -join ', '))."
}
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src\Probe\Probe.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Probe build failed.' }
    if ($PlayerAdvisor) {
        dotnet build (Join-Path $workspace 'src/Advisor/Advisor.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Player advisor build failed.' }
    }
    if ($DotNetPlanner) {
        dotnet build (Join-Path $workspace 'src/FixtureTools/FixtureTools.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'External planner build failed.' }
    }
    if ($BinaryCapture) {
        dotnet build (Join-Path $workspace 'src\FixtureArchive\FixtureArchive.csproj') -c Release -f net8.0
        if ($LASTEXITCODE -ne 0) { throw 'Fixture inspection build failed.' }
    }
}
if ($DotNetPlanner -and -not (Test-Path -LiteralPath (Join-Path $workspace 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll'))) {
    throw 'External planner is not built; build FixtureTools or omit -SkipBuild.'
}
$pluginDir = Join-Path $gameRoot 'BepInEx\plugins'
Copy-Item -LiteralPath (Join-Path $workspace 'src\Probe\bin\Release\netstandard2.1\Probe.dll') -Destination $pluginDir -Force
foreach ($dependency in @('Model', 'FixtureArchive')) {
    # Player acceptance must use the same complete payload as packaging, even
    # if a later checker build refreshed another project's dependency outputs.
    $dependencyRoot = if ($PlayerAdvisor) { 'Advisor' } else { $dependency }
    Copy-Item -LiteralPath (Join-Path $workspace "src/$dependencyRoot/bin/Release/netstandard2.1/$dependency.dll") -Destination $pluginDir -Force
}
if ($InputProfile) {
    $checkpoint = (Resolve-Path -LiteralPath $InputProfile).Path.TrimEnd('\')
    if ((Split-Path $checkpoint -Parent) -ine (Join-Path $workspace '.probe-runs')) {
        throw 'InputProfile must be a direct child of this workspace .probe-runs directory.'
    }
    $checkpointSave = Join-Path $checkpoint 'saves/save-singlePlayer.json'
    $checkpointData = Get-Content -LiteralPath $checkpointSave -Raw | ConvertFrom-Json
    if ($checkpointData.gameSequence -ne 6 -or $checkpointData.runState.distance -ne $BattleDistance) {
        throw 'InputProfile must be a native BattleIntro checkpoint at BattleDistance.'
    }
    $null = New-Item -ItemType Directory -Path $profile
    foreach ($item in Get-ChildItem -LiteralPath $checkpoint) {
        if ($item.Name -in @('saves', 'metagameSave.json', 'preferences.json', 'preferences-local.json')) {
            Copy-Item -LiteralPath $item.FullName -Destination $profile -Recurse
        }
    }
    [pscustomobject]@{ InputProfile = $checkpoint; SHA256 = (Get-FileHash -LiteralPath $checkpointSave -Algorithm SHA256).Hash } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $profile 'native-checkpoint-input.json')
} else {
    Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $profile -Recurse
}
$preferencesPath = Join-Path $profile 'preferences.json'
if (Test-Path -LiteralPath $preferencesPath) {
    $preferences = Get-Content -LiteralPath $preferencesPath -Raw | ConvertFrom-Json
    if ($null -eq $preferences.audioSettings) { throw 'Probe profile is missing audio settings.' }
    $preferences.audioSettings.globalMuted = $true
    $preferences | ConvertTo-Json -Depth 32 -Compress | Set-Content -LiteralPath $preferencesPath -NoNewline -Encoding utf8
}
$environment = @{
    MT2_PROBE_RESUME_RUN = $(if ($InputProfile) { '1' } else { '0' })
    MT2_PROBE_PRIMARY_CLAN = $PrimaryClan
    MT2_PROBE_SECONDARY_CLAN = $SecondaryClan
    MT2_PROBE_CONDUIT_MOVEMENT = $(if ($ConduitMovement) { '1' } else { '0' })
    MT2_PROBE_CONDUIT_ENEMY_MOVEMENT = $(if ($ConduitEnemyMovement) { '1' } else { '0' })
    MT2_PROBE_ROOM_MOON_CALLBACKS = $(if ($RoomMoonCallbacks) { '1' } else { '0' })
    MT2_PROBE_ROOM_MOON_SPELL_UPGRADES = $(if ($RoomMoonSpellUpgrades) { '1' } else { '0' })
    MT2_PROBE_ROOM_MOON_SPELL_ADVANCE = $(if ($RoomMoonSpellAdvance) { '1' } else { '0' })
    MT2_PROBE_ROOM_MOON_SPELL_COST_RELIC = $(if ($RoomMoonSpellCostRelic) { '1' } else { '0' })
    MT2_PROBE_ROOM_MOON_SPELL_ATTACK_RELIC = $(if ($RoomMoonSpellAttackRelic) { '1' } else { '0' })
    MT2_PROBE_ROOM_MOON_SHADE = $(if ($RoomMoonCallbacksShade) { '1' } else { '0' })
    MT2_PROBE_ROOM_MOON_MOVEMENT = $(if ($RoomMoonMovement) { '1' } else { '0' })
    MT2_PROBE_ROOM_MOON_ENEMY = $(if ($RoomMoonEnemyMovement) { '1' } else { '0' })
    MT2_PROBE_ROLLING_ADVICE = $(if ($RollingAdvice) { '1' } else { '0' })
    MT2_PROBE_ROLLING_INPUT = $(if ($RollingInput) { (Resolve-Path -LiteralPath $RollingInput).Path } else { '' })
    MT2_PROBE_PREVIEW_ATTACK_TRACE = $(if ($PreviewAttackTrace) { '1' } else { '0' })
    MT2_PROBE_BOSS_ACTION_TARGETS = $(if ($BossActionTargets) { '1' } else { '0' })
    MT2_PROBE_BOSS_ACTION_CONTROLLERS = $(if ($BossActionControllers) { '1' } else { '0' })
    MT2_PROBE_OUTER_BOSS_PHASES = $(if ($OuterBossPhases) { '1' } else { '0' })
    MT2_PROBE_OUTER_BOSS_RELENTLESS = $(if ($OuterBossRelentless) { '1' } else { '0' })
    MT2_PROBE_OUTER_BOSS_RELENTLESS_HEALTH = [string]$OuterBossRelentlessHealth
    MT2_PROBE_OUTER_BOSS_RELENTLESS_PYRE_HEALTH = [string]$OuterBossRelentlessPyreHealth
    MT2_PROBE_OUTER_BOSS_TURNS = $(if ($OuterBossTurns) { '1' } else { '0' })
    MT2_PROBE_ORIGINAL_OUTER_BOSS = $OriginalOuterBossVariant
    MT2_PROBE_ORIGINAL_OUTER_BOSS_TRIAL = $(if ($OriginalOuterBossTrial) { '1' } else { '0' })
    MT2_PROBE_TRUE_FINAL_BOSS_BIRTH = $(if ($TrueFinalBossBirth) { '1' } else { '0' })
    MT2_PROBE_TRUE_FINAL_BOSS_HEALTH = $(if ($TrueFinalBossHealth) { '1' } else { '0' })
    MT2_PROBE_OUTER_BOSS_BIRTH = $(if ($OuterBossBirth) { '1' } else { '0' })
    MT2_PROBE_OUTER_BOSS_PYRE = $(if ($OuterBossPyre) { '1' } else { '0' })
    MT2_PROBE_BATTLE_DISTANCE = "$BattleDistance"
    MT2_PROBE_PLANNER = $(if ($Planner) { '1' } else { '0' })
    MT2_PROBE_ADVISOR = $(if ($Advisor) { '1' } else { '0' })
    MT2_PROBE_PLAYER_ADVISOR = $(if ($PlayerAdvisor) { '1' } else { '0' })
    MT2_ADVISOR_SCREENSHOT = $(if ($Advisor) { '1' } else { '0' })
    MT2_PROBE_SEED = "$Seed"
    MT2_PROBE_PLANNER_TURN = "$PlannerTurn"
    MT2_PROBE_PLANNER_HORIZON = "$PlannerHorizon"
    MT2_PROBE_PLANNER_TACTICAL_SEED = $(if ($PlannerTacticalSeed) { '1' } else { '0' })
    MT2_PROBE_DOTNET_PLANNER = $(if ($DotNetPlanner) { Join-Path $workspace 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll' } else { '' })
    MT2_PROBE_PLANNER_DEPLOYMENT_ROOM = "$PlannerDeploymentRoom"
    MT2_PROBE_PLANNER_TIME_LIMIT_MS = "$PlannerTimeLimitMs"
    MT2_PROBE_PLANNER_INPUT = $(if ($PlannerInput) { (Resolve-Path -LiteralPath $PlannerInput).Path } else { '' })
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
    MT2_PROBE_CARD_CATALOG = $(if ($CardCatalog) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_RELICS = $(if ($CampaignRelicCalibration) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_DRAFTS = $(if ($CampaignDraftCalibration) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_POST_BATTLE = $(if ($CampaignPostBattleCalibration) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_BATTLE_RESOURCES = $(if ($CampaignBattleResourceCalibration) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_PURGE_GOLD = $(if ($CampaignPurgeGoldCalibration) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_DECK_COPY = $(if ($CampaignDeckCopyCalibration) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_DRAFT_ENHANCER_COUNT = $(if ($CampaignDraftEnhancerCountCalibration) { '1' } else { '0' })
    MT2_PROBE_FULL_RULE_CATALOG = $(if ($FullRuleCatalog) { '1' } else { '0' })
    MT2_PROBE_UPGRADE_MASKS = $(if ($UpgradeMasks) { '1' } else { '0' })
    MT2_PROBE_MODIFIER_OVERFLOW = $(if ($ModifierOverflow) { '1' } else { '0' })
    MT2_PROBE_TRAIT_COMPOSITION = $(if ($TraitComposition) { '1' } else { '0' })
    MT2_PROBE_CARD_STATUS_COMPOSITION = $(if ($CardStatusComposition) { '1' } else { '0' })
    MT2_PROBE_OWNED_CARD_MASKS = $(if ($OwnedCardMasks) { '1' } else { '0' })
    MT2_PROBE_CARD_UPGRADE_LIFECYCLE = $(if ($CardUpgradeLifecycle) { '1' } else { '0' })
    MT2_PROBE_BRANCH_CARD_MASKS = $(if ($BranchCardMasks -or $CardCatalog) { '1' } else { '0' })
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
    MT2_PROBE_ISOLATED_CONDUIT = $(if ($IsolatedUnitConduit) { '1' } else { '0' })
    MT2_PROBE_BINARY_CAPTURE = $(if ($BinaryCapture) { '1' } else { '0' })
    MT2_PROBE_CAPTURE_JSON = $(if ($CaptureJson) { '1' } else { '0' })
    MT2_PROBE_NO_TIMEOUT = '1'
    MT2_PROBE_ISOLATE_PREVIEW_RNG = $(if ($TriggeredStatus -or $PersistentEnchantments -or $OriginalSupportUnits -or $OriginalDivineDancer -or $OriginalIsolatedUnitBatch -or $OriginalIsolatedColdstrikeSiren -or $isolatedUnitIds.Count -gt 0) { '1' } else { '0' })
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
if ($OriginalDamageOnFloorTransition) { $environment['MT2_PROBE_ORIGINAL_FLOOR_TRANSITION_DAMAGE'] = '1' }
if ($OriginalPyregelOnAscend) { $environment['MT2_PROBE_ORIGINAL_PYREGEL_ON_ASCEND'] = '1' }
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
if ($SoldierOfFortune) { $environment['MT2_PROBE_MODIFIERS'] = 'soldier-of-fortune' }
if ($RestorationDetonation) { $environment['MT2_PROBE_MODIFIERS'] = 'restoration-detonation' }
if ($RunCardGeneration) { $environment['MT2_PROBE_MODIFIERS'] = 'run-card-generation' }
if ($ArmamentDrive) { $environment['MT2_PROBE_MODIFIERS'] = 'armament-drive' }
if ($Preserve) { $environment['MT2_PROBE_MODIFIERS'] = 'preserve' }
if ($CardDuplicate) { $environment['MT2_PROBE_MODIFIERS'] = 'card-duplicate' }
if ($StardustInvocation) { $environment['MT2_PROBE_MODIFIERS'] = 'stardust-invocation' }
if ($AdaptiveMutation) { $environment['MT2_PROBE_MODIFIERS'] = 'adaptive-mutation' }
if ($Mixalot) { $environment['MT2_PROBE_MODIFIERS'] = 'mixalot' }
if ($SmallBox) { $environment['MT2_PROBE_MODIFIERS'] = 'small-box' }
if ($MagneticRelay) { $environment['MT2_PROBE_MODIFIERS'] = 'magnetic-relay' }
if ($TranscendImp) { $environment['MT2_PROBE_MODIFIERS'] = 'transcend-imp' }
if ($FusedWeaponry) { $environment['MT2_PROBE_MODIFIERS'] = 'fused-weaponry' }
if ($EclipseChamber) { $environment['MT2_PROBE_MODIFIERS'] = 'eclipse-chamber' }
if ($EclipseChamberCallbacks) { $environment['MT2_PROBE_ECLIPSE_CALLBACKS'] = '1' }
if ($EclipseChamberShadeCallbacks) { $environment['MT2_PROBE_ECLIPSE_SHADE_CALLBACKS'] = '1' }
if ($EclipseChamberMovement) { $environment['MT2_PROBE_ECLIPSE_MOVEMENT'] = '1' }
if ($EclipseChamberEnemyMovement) { $environment['MT2_PROBE_ECLIPSE_ENEMY_MOVEMENT'] = '1' }
if ($EclipseChamberEnemyCallbacks) { $environment['MT2_PROBE_ECLIPSE_ENEMY_CALLBACKS'] = '1' }
if ($EclipseChamberEnemyConduit) { $environment['MT2_PROBE_ECLIPSE_ENEMY_CONDUIT'] = '1' }
if ($EclipseChamberMixedConduit) { $environment['MT2_PROBE_ECLIPSE_MIXED_CONDUIT'] = '1' }
if ($OriginalRoomCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-room-cards' }
if ($OriginalForgeCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-forge-cards' }
if ($OriginalStatusCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-status-cards' }
if ($OriginalDeckSpell) { $environment['MT2_PROBE_MODIFIERS'] = 'original-deck-spell' }
if ($OriginalDeepOffering) { $environment['MT2_PROBE_MODIFIERS'] = 'original-deep-offering' }
if ($OriginalMolded) { $environment['MT2_PROBE_MODIFIERS'] = 'original-molded' }
if ($OriginalBrokenMemories) { $environment['MT2_PROBE_MODIFIERS'] = 'original-broken-memories' }
if ($OriginalWickedBlaze) { $environment['MT2_PROBE_MODIFIERS'] = 'original-wicked-blaze' }
if ($OriginalOfferingToken) { $environment['MT2_PROBE_MODIFIERS'] = 'original-offering-token' }
if ($OriginalMoonRitual) { $environment['MT2_PROBE_MODIFIERS'] = 'original-moon-ritual' }
if ($OriginalSelfPurgeEnergyGain) { $environment['MT2_PROBE_MODIFIERS'] = 'original-self-purge-energy-gain' }
if ($OriginalSmeltedTreasure) { $environment['MT2_PROBE_MODIFIERS'] = 'original-smelted-treasure' }
if ($OriginalPyreGro) { $environment['MT2_PROBE_MODIFIERS'] = 'original-pyre-gro' }
if ($OriginalDeadlyVariant) { $environment['MT2_PROBE_MODIFIERS'] = 'original-deadly-variant' }
if ($OriginalDrain) { $environment['MT2_PROBE_MODIFIERS'] = 'original-drain' }
if ($OriginalStarterFrostbiteDiscard) { $environment['MT2_PROBE_MODIFIERS'] = 'original-starter-frostbite-discard' }
if ($OriginalFeedingFrenzy) { $environment['MT2_PROBE_MODIFIERS'] = 'original-feeding-frenzy' }
if ($OriginalTitansGratitude) { $environment['MT2_PROBE_MODIFIERS'] = 'original-titans-gratitude' }
if ($OriginalPerfectInsanity) { $environment['MT2_PROBE_MODIFIERS'] = 'original-perfect-insanity' }
if ($OriginalStarterReform) { $environment['MT2_PROBE_MODIFIERS'] = 'original-starter-reform' }
if ($OriginalRapidRaise) { $environment['MT2_PROBE_MODIFIERS'] = 'original-rapid-raise' }
if ($Goldmance) { $environment['MT2_PROBE_MODIFIERS'] = 'goldmance' }
if ($EngineUpgrade) { $environment['MT2_PROBE_MODIFIERS'] = 'engine-upgrade' }
if ($StewardsQuarters) { $environment['MT2_PROBE_MODIFIERS'] = 'stewards-quarters' }
if ($ZoneOfSilence) { $environment['MT2_PROBE_MODIFIERS'] = 'zone-of-silence' }
if ($Memorial) { $environment['MT2_PROBE_MODIFIERS'] = 'memorial' }
if ($Planetarium) { $environment['MT2_PROBE_MODIFIERS'] = 'planetarium' }
if ($GrowRoom) { $environment['MT2_PROBE_MODIFIERS'] = 'grow-room' }
if ($MushyRoom) { $environment['MT2_PROBE_MODIFIERS'] = 'mushy-room' }
if ($OriginalPiercingCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-piercing-cards' }
if ($InfernoRoom) { $environment['MT2_PROBE_MODIFIERS'] = 'inferno-room' }
if ($OriginalConsumeCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-consume-cards' }
if ($OriginalStrongMagicCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-strong-magic-cards' }
if ($OriginalXDamageCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-x-damage-cards' }
if ($OriginalUnpurgableCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-unpurgable-cards' }
if ($OriginalHandCosts) { $environment['MT2_PROBE_MODIFIERS'] = 'original-hand-costs' }
if ($OriginalLimitedRange) { $environment['MT2_PROBE_MODIFIERS'] = 'original-limited-range' }
if ($OriginalMonsterHandCosts) { $environment['MT2_PROBE_MODIFIERS'] = 'original-monster-hand-costs' }
if ($OriginalUnplayableCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-unplayable-cards' }
if ($OriginalPermafrostCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-permafrost-cards' }
if ($HoldoverCalibration) { $environment['MT2_PROBE_HOLDOVER_CALIBRATION'] = '1' }
if ($HoldoverVariants) { $environment['MT2_PROBE_HOLDOVER_VARIANTS'] = '1' }
if ($PermanentTraits) { $environment['MT2_PROBE_PERMANENT_TRAITS'] = '1' }
if ($TitaniteCalibration) { $environment['MT2_PROBE_TITANITE'] = '1' }
if ($TitaniteCombinations) { $environment['MT2_PROBE_TITANITE_COMBINATIONS'] = '1' }
if ($OriginalOverflowCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-overflow-cards' }
if ($OriginalCorruptRestricted) { $environment['MT2_PROBE_MODIFIERS'] = 'original-corrupt-restricted' }
if ($OriginalStatusStackDamage) { $environment['MT2_PROBE_MODIFIERS'] = 'original-status-stack-damage' }
if ($OriginalSpecificCardDamage) { $environment['MT2_PROBE_MODIFIERS'] = 'original-specific-card-damage' }
if ($OriginalScaledEnergy) { $environment['MT2_PROBE_MODIFIERS'] = 'original-scaled-energy' }
if ($OriginalStatusRemovalScaling) { $environment['MT2_PROBE_MODIFIERS'] = 'original-status-removal-scaling' }
if ($OriginalEffectApplications) { $environment['MT2_PROBE_MODIFIERS'] = 'original-effect-applications' }
if ($OriginalPileDraw) { $environment['MT2_PROBE_MODIFIERS'] = 'original-pile-draw' }
if ($OriginalCardDrawPotion) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-draw-potion' }
if ($OriginalAcceleratedIncubation) { $environment['MT2_PROBE_MODIFIERS'] = 'original-accelerated-incubation' }
if ($OriginalEagerConscript) { $environment['MT2_PROBE_MODIFIERS'] = 'original-eager-conscript' }
if ($OriginalSpawningPods) { $environment['MT2_PROBE_MODIFIERS'] = 'original-spawning-pods' }
if ($OriginalMindsOfTheirOwn) { $environment['MT2_PROBE_MODIFIERS'] = 'original-minds-of-their-own' }
if ($OriginalMetallurgy) { $environment['MT2_PROBE_MODIFIERS'] = 'original-metallurgy' }
if ($OriginalFertileGround) { $environment['MT2_PROBE_MODIFIERS'] = 'original-fertile-ground' }
if ($OriginalCoverOfNight) { $environment['MT2_PROBE_MODIFIERS'] = 'original-cover-of-night' }
if ($OriginalWaxenSpike) { $environment['MT2_PROBE_MODIFIERS'] = 'original-waxen-spike' }
if ($OriginalImpPressive) { $environment['MT2_PROBE_MODIFIERS'] = 'original-imp-pressive' }
if ($OriginalTomeOfEvergrowth) { $environment['MT2_PROBE_MODIFIERS'] = 'original-tome-of-evergrowth' }
if ($OriginalTomeOfTheLostPath) { $environment['MT2_PROBE_MODIFIERS'] = 'original-tome-of-the-lost-path' }
if ($OriginalMakeItRain) { $environment['MT2_PROBE_MODIFIERS'] = 'original-make-it-rain' }
if ($OriginalDeathroar) { $environment['MT2_PROBE_MODIFIERS'] = 'original-deathroar' }
if ($OriginalGreenRecruit) { $environment['MT2_PROBE_MODIFIERS'] = 'original-green-recruit' }
if ($OriginalPrimordium) { $environment['MT2_PROBE_MODIFIERS'] = 'original-primordium' }
if ($OriginalCheeryDeathcap) { $environment['MT2_PROBE_MODIFIERS'] = 'original-cheery-deathcap' }
if ($OriginalEnthusiasticFan) { $environment['MT2_PROBE_MODIFIERS'] = 'original-enthusiastic-fan' }
if ($OriginalCopyMorselTwice) { $environment['MT2_PROBE_MODIFIERS'] = 'original-copy-morsel-twice' }
if ($OriginalLazarusLeagueSpike) { $environment['MT2_PROBE_MODIFIERS'] = 'original-lazarus-league-spike' }
if ($OriginalTomeOfContracts) { $environment['MT2_PROBE_MODIFIERS'] = 'original-tome-of-contracts' }
if ($OriginalPileRecursion) { $environment['MT2_PROBE_MODIFIERS'] = 'original-pile-recursion' }
if ($OriginalScaledBuff) { $environment['MT2_PROBE_MODIFIERS'] = 'original-scaled-buff' }
if ($OriginalPileStatus) { $environment['MT2_PROBE_MODIFIERS'] = 'original-pile-status' }
if ($OriginalDrawUpgradeScaling) { $environment['MT2_PROBE_MODIFIERS'] = 'original-draw-upgrade-scaling' }
if ($OriginalConsumedUpgrade) { $environment['MT2_PROBE_MODIFIERS'] = 'original-consumed-upgrade' }
if ($OriginalStatusCompositions) { $environment['MT2_PROBE_MODIFIERS'] = 'original-status-compositions' }
if ($OriginalGraftedEquipment) { $environment['MT2_PROBE_MODIFIERS'] = 'original-grafted-equipment' }
if ($OriginalSelfAttack) { $environment['MT2_PROBE_MODIFIERS'] = 'original-self-attack' }
if ($OriginalHealingCleanse) { $environment['MT2_PROBE_MODIFIERS'] = 'original-healing-cleanse' }
if ($OriginalOnKill) { $environment['MT2_PROBE_MODIFIERS'] = 'original-on-kill' }
if ($OriginalOnKillGeneration) { $environment['MT2_PROBE_MODIFIERS'] = 'original-on-kill-generation' }
if ($OriginalMorselEaten) { $environment['MT2_PROBE_MODIFIERS'] = 'original-morsel-eaten' }
if ($OriginalUnitBuffs) { $environment['MT2_PROBE_MODIFIERS'] = 'original-unit-buffs' }
if ($OriginalKillCopy) { $environment['MT2_PROBE_MODIFIERS'] = 'original-kill-copy' }
if ($OriginalFriendlyStatuses) { $environment['MT2_PROBE_MODIFIERS'] = 'original-friendly-statuses' }
if ($OriginalBasicDamage) { $environment['MT2_PROBE_MODIFIERS'] = 'original-basic-damage' }
if ($OriginalResourceSpells) { $environment['MT2_PROBE_MODIFIERS'] = 'original-resource-spells' }
if ($OriginalHostileStatuses) { $environment['MT2_PROBE_MODIFIERS'] = 'original-hostile-statuses' }
if ($OriginalFuel) { $environment['MT2_PROBE_MODIFIERS'] = 'original-fuel' }
if ($OriginalSupportUnits) { $environment['MT2_PROBE_MODIFIERS'] = 'original-support-units' }
if ($OriginalDivineDancer) { $environment['MT2_PROBE_MODIFIERS'] = 'original-divine-dancer' }
if ($OriginalIsolatedUnitBatch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-isolated-unit-batch' }
if ($OriginalIsolatedColdstrikeSiren) { $environment['MT2_PROBE_MODIFIERS'] = 'original-isolated-coldstrike-siren' }
if ($OriginalIsolatedUnitIds.Count -gt 0) { $environment['MT2_PROBE_MODIFIERS'] = 'original-isolated-unit-batch:' + ($OriginalIsolatedUnitIds -join ',') }
elseif ($OriginalIsolatedUnitId) { $environment['MT2_PROBE_MODIFIERS'] = 'original-isolated-unit:' + $OriginalIsolatedUnitId }
if ($OriginalAttackOverrides) { $environment['MT2_PROBE_MODIFIERS'] = 'original-attack-overrides' }
if ($OriginalReactiveUnits) { $environment['MT2_PROBE_MODIFIERS'] = 'original-reactive-units' }
if ($OriginalBaseChampions) { $environment['MT2_PROBE_MODIFIERS'] = 'original-base-champions' }
if ($OriginalStatusUnits) { $environment['MT2_PROBE_MODIFIERS'] = 'original-status-units' }
if ($OriginalEquipmentCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-equipment-cards' }
if ($OriginalIsolatedEquipmentIds.Count -gt 0) { $environment['MT2_PROBE_MODIFIERS'] = 'original-isolated-equipment:' + ($OriginalIsolatedEquipmentIds -join ',') }
if ($OriginalSupportSpells) { $environment['MT2_PROBE_MODIFIERS'] = 'original-support-spells' }
if ($OriginalIsolatedSpellIds.Count -gt 0) { $environment['MT2_PROBE_MODIFIERS'] = 'original-isolated-spells:' + ($OriginalIsolatedSpellIds -join ',') }
if ($OriginalIsolatedSpellUpgrades.Count -gt 0) {
    if ($OriginalIsolatedSpellIds.Count -eq 0) { throw 'Isolated spell upgrades require an isolated spell batch.' }
    $OriginalIsolatedSpellUpgrades = @($OriginalIsolatedSpellUpgrades | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Where-Object { $_ })
    if (@($OriginalIsolatedSpellUpgrades | Where-Object { $_ -notmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' }).Count -gt 0) {
        throw 'Isolated spell upgrades must be native upgrade UUIDs.'
    }
    $environment['MT2_PROBE_ISOLATED_SPELL_UPGRADES'] = $OriginalIsolatedSpellUpgrades -join ','
}
if ($OriginalUnitAbilityId) {
    if ($OriginalUnitAbilityId -notmatch '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$' -or $OriginalIsolatedSpellIds.Count -eq 0) {
        throw 'Original unit ability requires a native UUID and an isolated original spell control batch.'
    }
    $environment['MT2_PROBE_ORIGINAL_UNIT_ABILITY'] = $OriginalUnitAbilityId.ToLowerInvariant()
}
if ($OriginalRearrangeSpells) { $environment['MT2_PROBE_MODIFIERS'] = 'original-rearrange-spells' }
if ($OriginalMovementSpells) { $environment['MT2_PROBE_MODIFIERS'] = 'original-movement-spells' }
if ($OriginalScaledDamage) { $environment['MT2_PROBE_MODIFIERS'] = 'original-scaled-damage' }
if ($OriginalKillResource) {
    $environment['MT2_PROBE_MODIFIERS'] = 'original-kill-resources'
    $environment['MT2_PROBE_KILL_RESOURCE'] = $OriginalKillResource
}
if ($OriginalScaledMoonPhase) { $environment['MT2_PROBE_MODIFIERS'] = 'original-scaled-moon-phase' }
if ($OriginalPositiveStatus) { $environment['MT2_PROBE_MODIFIERS'] = 'original-positive-status' }
if ($OriginalSpellCompositions) { $environment['MT2_PROBE_MODIFIERS'] = 'original-spell-compositions' }
if ($OriginalDiscardDraw) { $environment['MT2_PROBE_MODIFIERS'] = 'original-discard-draw' }
if ($OriginalMoonCardUpgrades) { $environment['MT2_PROBE_MODIFIERS'] = 'original-moon-card-upgrades' }
if ($OriginalMoonCardUpgradesRoomMask) { $environment['MT2_PROBE_MOON_UPGRADE_ROOM_MASK'] = '1' }
if ($OriginalMoonCardUpgradesLunacy) { $environment['MT2_PROBE_MOON_UPGRADE_LUNACY'] = '1' }
if ($OriginalMoonCardUpgradesCostRelic) { $environment['MT2_PROBE_MOON_UPGRADE_COST_RELIC'] = '1' }
if ($OriginalMoonCardUpgradesAttackRelic) { $environment['MT2_PROBE_MOON_UPGRADE_ATTACK_RELIC'] = '1' }
if ($OriginalRoomStatusDamage) { $environment['MT2_PROBE_MODIFIERS'] = 'original-room-status-damage' }
if ($OriginalCorruptionDamage) { $environment['MT2_PROBE_MODIFIERS'] = 'original-corruption-damage' }
if ($OriginalXStatusCards) { $environment['MT2_PROBE_MODIFIERS'] = 'original-x-status-cards' }
if ($OriginalXBanishedSpike) { $environment['MT2_PROBE_MODIFIERS'] = 'original-x-banished-spike' }
if ($OriginalXLunaSpike) { $environment['MT2_PROBE_MODIFIERS'] = 'original-x-luna-spike' }
if ($OriginalXStygianSpike) { $environment['MT2_PROBE_MODIFIERS'] = 'original-x-stygian-spike' }
if ($OriginalXWurmkinSpike) { $environment['MT2_PROBE_MODIFIERS'] = 'original-x-wurmkin-spike' }
if ($OriginalMineCollapse) { $environment['MT2_PROBE_MODIFIERS'] = 'original-mine-collapse' }
if ($OriginalImpOlate) { $environment['MT2_PROBE_MODIFIERS'] = 'original-imp-olate' }
if ($OriginalPyreshot) { $environment['MT2_PROBE_MODIFIERS'] = 'original-pyreshot' }
if ($OriginalRetch) { $environment['MT2_PROBE_MODIFIERS'] = 'original-retch' }
if ($DamageByUnitsKilled) { $environment['MT2_PROBE_MODIFIERS'] = 'damage-by-units-killed' }
if ($TransferAttack) { $environment['MT2_PROBE_MODIFIERS'] = 'transfer-attack' }
if ($BrainTransfer) { $environment['MT2_PROBE_MODIFIERS'] = 'brain-transfer' }
if ($FrostChanneling) { $environment['MT2_PROBE_MODIFIERS'] = 'frost-channeling' }
if ($Mix) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-mix' }
if ($OriginalTinkermasterMix) { $environment['MT2_PROBE_MODIFIERS'] = 'original-tinkermaster-mix' }
if ($OriginalSecretIngredient) { $environment['MT2_PROBE_MODIFIERS'] = 'original-secret-ingredient-mix' }
if ($OriginalPartFactory) { $environment['MT2_PROBE_MODIFIERS'] = 'original-part-factory' }
if ($OriginalBigDamageFromManyMorsels) { $environment['MT2_PROBE_MODIFIERS'] = 'original-big-damage-many-morsels' }
if ($OriginalEarlyDiscardEnergy) { $environment['MT2_PROBE_MODIFIERS'] = 'hand-removal'; $environment['MT2_PROBE_ORIGINAL_EARLY_DISCARD_ENERGY'] = '1' }
if ($OriginalProgressPacemakerMix) { $environment['MT2_PROBE_MODIFIERS'] = 'original-progress-pacemaker-mix' }
if ($OriginalCardTypeCostRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-type-cost-relic' }
if ($OriginalPurgeToExhaustRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-purge-to-exhaust-relic' }
if ($OriginalConserveEmber) { $environment['MT2_PROBE_MODIFIERS'] = 'original-conserve-ember-relic' }
if ($CardTypeCostUpgradeOrder) { $environment['MT2_PROBE_MODIFIERS'] = if ($CardTypeCostUpgradeOrder -eq 'DiscountFirst') { 'card-type-cost-discount-first' } else { 'card-type-cost-upgrade-first' } }
if ($Mix) { $environment['MT2_PROBE_MIX_CHOICES'] = if ($MixChoices) { $MixChoices } else { '00' } }
if ($MixUnstableExplosion) { $environment['MT2_PROBE_MIX_UNSTABLE_EXPLOSION'] = '1' }
if ($TransferAttackAbility) { $environment['MT2_PROBE_MODIFIERS'] = 'transfer-attack-ability' }
if ($CardEffectKill -or $CardEffectKillSubtype) {
    $environment['MT2_PROBE_MODIFIERS'] = if ($CardEffectKillSubtype) { 'card-effect-kill-subtype' } else { 'card-effect-kill' }
}
if ($NullForwarding) { $environment['MT2_PROBE_MODIFIERS'] = 'null-forwarding' }
if ($NullTriggers) { $environment['MT2_PROBE_MODIFIERS'] = 'null-triggers' }
if ($NullTargetless) { $environment['MT2_PROBE_MODIFIERS'] = 'null-targetless' }
if ($OriginalNullScourges) { $environment['MT2_PROBE_MODIFIERS'] = 'original-null-scourges' }
if ($OriginalTrample) { $environment['MT2_PROBE_MODIFIERS'] = 'original-trample' }
if ($OriginalSpellShield) { $environment['MT2_PROBE_MODIFIERS'] = 'original-spell-shield' }
if ($ChooseDiscard) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard' }
if ($ChooseDiscardOthers) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard-others' }
if ($ChooseDiscardOthersPolicy) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard-others-policy' }
if ($ChooseDiscardRefine) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard-refine' }
if ($ChooseDiscardPersistent) { $environment['MT2_PROBE_MODIFIERS'] = 'card-effect-choose-discard-persistent' }
if ($OriginalSmeltEmberRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-smelt-ember-relic' }
if ($OriginalSmeltEmberRelicZero) { $environment['MT2_PROBE_MODIFIERS'] = 'original-smelt-ember-relic-zero' }
if ($OriginalSmeltEmberRelicBulk) { $environment['MT2_PROBE_MODIFIERS'] = 'original-smelt-ember-relic-bulk' }
if ($RemoveAllStatusEffects) { $environment['MT2_PROBE_MODIFIERS'] = 'remove-all-status-effects' }
if ($DragonHoard) { $environment['MT2_PROBE_MODIFIERS'] = 'dragon-hoard' }
if ($TrainHeal) { $environment['MT2_PROBE_MODIFIERS'] = 'train-heal' }
if ($TrainHealPerEmber) { $environment['MT2_PROBE_MODIFIERS'] = 'train-heal-per-ember' }
if ($TrainHealPerEmberX) { $environment['MT2_PROBE_MODIFIERS'] = 'train-heal-per-ember-x' }
if ($CopyOnPlay) { $environment['MT2_PROBE_MODIFIERS'] = 'copy-on-play' }
if ($CopyOnPlayTerminal) { $environment['MT2_PROBE_COPY_ON_PLAY_TERMINAL'] = '1' }
if ($CopyOnPlayTerminalConsume) { $environment['MT2_PROBE_COPY_ON_PLAY_TERMINAL_CONSUME'] = '1' }
if ($CopyOnPlayTerminalPurge) { $environment['MT2_PROBE_COPY_ON_PLAY_TERMINAL_PURGE'] = '1' }
if ($CopyOnPlayTerminalThreat) { $environment['MT2_PROBE_COPY_ON_PLAY_TERMINAL_THREAT'] = '1' }
if ($Overheal) { $environment['MT2_PROBE_MODIFIERS'] = 'overheal' }
if ($Corruption) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption' }
if ($CorruptionUnitTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption-unit-trigger' }
if ($CorruptionCarverTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption-carver-trigger' }
if ($CorruptionKeeperTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption-keeper-trigger' }
if ($TowerCorruptionCallbacks) { $environment['MT2_PROBE_TOWER_CORRUPTION_CALLBACKS'] = '1' }
if ($CorruptionRelicTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'corruption-relic-trigger' }
if ($PermanentCorruption) { $environment['MT2_PROBE_MODIFIERS'] = 'permanent-corruption' }
if ($MoonPhaseCard) { $environment['MT2_PROBE_MODIFIERS'] = 'moon-phase-card' }
if ($MoonPhaseAdvance) { $environment['MT2_PROBE_MODIFIERS'] = 'moon-phase-advance' }
if ($MoonPhaseCallbacks) { $environment['MT2_PROBE_MODIFIERS'] = 'moon-phase-callbacks' }
if ($ConditionalNoPlayerStatus) { $environment['MT2_PROBE_MODIFIERS'] = 'conditional-no-player-status' }
if ($SelfAttackStatus) { $environment['MT2_PROBE_MODIFIERS'] = 'self-attack-status' }
if ($ConditionalPyreDamage) { $environment['MT2_PROBE_MODIFIERS'] = 'conditional-pyre-damage' }
if ($ConditionalPyreLethal) { $environment['MT2_PROBE_MODIFIERS'] = 'conditional-pyre-damage-lethal' }
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
if ($OriginalTriggerRelicBatch -or $OriginalTriggerRelicBatchStationary -or $OriginalFusedPyreshard) {
    $environment['MT2_PROBE_MODIFIERS'] = if ($OriginalFusedPyreshard) { 'original-fused-pyreshard' } else { 'original-trigger-relic-batch' }
    $environment['MT2_PROBE_ORIGINAL_TRIGGER_RELIC_BATCH_STATIONARY'] = $(if ($OriginalTriggerRelicBatchStationary) { '1' } else { '0' })
}
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
if ($OriginalTwoChargesOnCardConsumed) { $environment['MT2_PROBE_MODIFIERS'] = 'original-two-charges-on-card-consumed' }
if ($OriginalChanceForSpellWeakness) { $environment['MT2_PROBE_MODIFIERS'] = 'original-chance-for-spell-weakness' }
if ($OriginalChanceToCleanse) { $environment['MT2_PROBE_MODIFIERS'] = 'original-chance-to-cleanse' }
if ($OriginalExtraFeedTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'original-extra-feed-trigger' }
if ($OriginalSummonWithAvarice) { $environment['MT2_PROBE_MODIFIERS'] = 'original-summon-with-avarice' }
if ($OriginalCheaperSpellCost) { $environment['MT2_PROBE_MODIFIERS'] = 'original-reduce-spell-cost-cheaper' }
if ($OriginalFirstCardCost) { $environment['MT2_PROBE_MODIFIERS'] = 'original-reduce-first-spell-cost-per-deck-size' }
if ($OriginalRebalanceValor) { $environment['MT2_PROBE_MODIFIERS'] = 'original-rebalance-valor' }
if ($OriginalNefariousOverflow) { $environment['MT2_PROBE_MODIFIERS'] = 'original-nefarious-overflow' }
if ($OriginalUncontrolledGroup) { $environment['MT2_PROBE_MODIFIERS'] = 'original-uncontrolled-group' }
if ($OriginalCopyFirstSpellPlayed) { $environment['MT2_PROBE_MODIFIERS'] = 'original-copy-first-spell-played' }
if ($OriginalCopyFirstConsumePlayed) { $environment['MT2_PROBE_MODIFIERS'] = 'original-copy-first-consume-played' }
if ($OriginalCopyConsumedCard) { $environment['MT2_PROBE_MODIFIERS'] = 'original-copy-consumed-card' }
if ($OriginalMothersRemnant3Relic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-mothers-remnant3' }
if ($OriginalTribouletRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-balatro-event-triboulet' }
if ($OriginalCooldownOnDamageRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-cooldown-on-damage-dh' }
if ($OriginalMonsterSpellTargeting) { $environment['MT2_PROBE_MODIFIERS'] = 'original-monster-spell-targeting' }
if ($OriginalFunguyDeckScaling) { $environment['MT2_PROBE_MODIFIERS'] = 'original-funguy-deck-scaling' }
if ($OriginalCardDrawOnHeal) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-draw-on-heal' }
if ($OriginalBrokenRailRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-broken-rail' }
if ($OriginalHephsBlueprints) { $environment['MT2_PROBE_MODIFIERS'] = 'original-hephs-blueprints' }
if ($OriginalRageOnBurnoutStackLoss) { $environment['MT2_PROBE_MODIFIERS'] = 'original-rage-on-burnout-stack-loss' }
if ($OriginalPyreRevenge) { $environment['MT2_PROBE_MODIFIERS'] = 'original-pyre-revenge' }
if ($OriginalExtractForEmberAndEchos) { $environment['MT2_PROBE_MODIFIERS'] = 'original-extract-for-ember-and-echos' }
if ($OriginalCardDrawOnCardPlay) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-draw-on-card-play' }
if ($OriginalCarriageJack) { $environment['MT2_PROBE_MODIFIERS'] = 'original-carriage-jack' }
if ($OriginalFirstRoomAttachment) { $environment['MT2_PROBE_MODIFIERS'] = 'original-first-room-attachment' }
if ($OriginalStartRoomAttachment) { $environment['MT2_PROBE_MODIFIERS'] = 'original-start-room-attachment' }
if ($OriginalStartRoomAttachmentFloor2) { $environment['MT2_PROBE_MODIFIERS'] = 'original-start-room-attachment-floor2' }
if ($OriginalStartRoomAttachmentFloor3) { $environment['MT2_PROBE_MODIFIERS'] = 'original-start-room-attachment-floor3' }
if ($UnplayedCardTrigger) { $environment['MT2_PROBE_MODIFIERS'] = 'unplayed-card-trigger' }
if ($UnplayedCardEffects) { $environment['MT2_PROBE_MODIFIERS'] = 'unplayed-card-effects' }
if ($OriginalRoomEntryStatusRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-room-entry-status-relics' }
if ($OriginalMoonPhaseEnergyRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-moon-phase-energy-relic' }
if ($OriginalMoonPhaseHealRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-moon-phase-heal-relic' }
if ($MoonHealCallbacks) { $environment['MT2_PROBE_MOON_HEAL_CALLBACKS'] = '1' }
if ($RemovedStatusScaling) { $environment['MT2_PROBE_REMOVAL_SCALING'] = '1' }
if ($NullAbility) { $environment['MT2_PROBE_NULL_ABILITY'] = '1' }
if ($OriginalMoonPhaseStatusRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-moon-phase-status-relic' }
if ($OriginalCardAttackRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-attack-relic' }
if ($OriginalUpgradeCountRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-upgrade-count-relics' }
if ($OriginalHealingPowerRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-healing-power-relic' }
if ($OriginalStartCombatEnergyRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-start-combat-energy-relic' }
if ($OriginalStartCombatHoardRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-start-combat-hoard-relic' }
if ($OriginalForgeConservationRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-forge-conservation-relic' }
if ($OriginalOpeningCorruptionRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-opening-corruption-relics' }
if ($OriginalSpellPlayThresholdRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-spell-play-threshold-relics' }
if ($OriginalCombatTriggerRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-combat-trigger-relics' }
if ($OriginalConduitRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-conduit-relics' }
if ($OriginalEquipmentGrowthRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-equipment-growth-relics' }
if ($OriginalHealthChoiceRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-health-choice-relic' }
if ($OriginalAfterDrawGenerationRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-after-draw-generation-relics' }
if ($OriginalAddSnackOnKill) { $environment['MT2_PROBE_MODIFIERS'] = 'original-add-snack-on-kill' }
if ($OriginalFinalEndingArtifact) { $environment['MT2_PROBE_MODIFIERS'] = 'original-final-ending-artifact' }
if ($OriginalBalatroEventYorick) { $environment['MT2_PROBE_MODIFIERS'] = 'original-balatro-event-yorick' }
if ($OriginalBalatroEventCavendish) { $environment['MT2_PROBE_MODIFIERS'] = 'original-balatro-event-cavendish' }
if ($OriginalGarfieldBoxRandomCard) { $environment['MT2_PROBE_MODIFIERS'] = 'original-garfield-box-random-card-per-turn' }
if ($OriginalPartFactory) { $environment['MT2_PROBE_MODIFIERS'] = 'original-part-factory' }
if ($OriginalBigDamageFromManyMorsels) { $environment['MT2_PROBE_MODIFIERS'] = 'original-big-damage-many-morsels' }
if ($OriginalEarlyDiscardEnergy) { $environment['MT2_PROBE_MODIFIERS'] = 'hand-removal'; $environment['MT2_PROBE_ORIGINAL_EARLY_DISCARD_ENERGY'] = '1' }
if ($OriginalUnitResourceRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-unit-resource-relics' }
if ($OriginalOpeningDrawRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-opening-draw-relics' }
if ($OriginalTurnDrawRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-turn-draw-relics' }
if ($OriginalDeckTurnDrawRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-deck-turn-draw' }
if ($OriginalCardPlayDamageRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-play-damage' }
if ($OriginalConsumeDamageRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-consume-damage' }
if ($OriginalEatenDamageRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-eaten-damage' }
if ($OriginalMoreCapacityRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-more-capacity' }
if ($OriginalBurnoutDamageRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-burnout-death-damage' }
if ($OriginalHealDamageRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-heal-to-damage' }
if ($OriginalKillAttackRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-kill-attack' }
if ($OriginalChanceToSilenceRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-chance-to-silence' }
if ($OriginalFreezeReduceCostRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-freeze-reduce-cost' }
if ($OriginalRandomFreezeRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-random-freeze' }
if ($OriginalRandomHandUpgradeRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-random-hand-upgrade' }
if ($OriginalStartTurnPropagationRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-start-turn-propagation' }
if ($OriginalMooncycleFaeRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-mooncycle-fae' }
if ($OriginalRandomDrawCostRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-random-draw-cost' }
if ($OriginalSuppressExhaustRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-suppress-exhaust' }
if ($OriginalExpensiveCardUpgradeRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-expensive-card-upgrade' }
if ($OriginalWolosBloodletterRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-wolos-bloodletter' }
if ($OriginalCardTypeSequenceRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-card-type-sequence' }
if ($OriginalWhelpCooldownRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-whelp-spawn-cooldown' }
if ($OriginalSpawnRetreatRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-spawn-retreat' }
if ($OriginalGrosMichelRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-gros-michel' }
if ($OriginalBoneDogRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-bone-dog' }
if ($OriginalDecayDeathRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-decay-death' }
if ($OriginalOpeningMorselsRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-opening-morsels' }
if ($CardBackedFeeding) { $environment['MT2_PROBE_FOOD_CARDS'] = '1' }
if ($ForcedFeeding) { $environment['MT2_PROBE_FORCED_FEEDING'] = '1' }
if ($ShroudSpikeFeeding) { $environment['MT2_PROBE_SHROUD_SPIKE'] = '1' }
if ($OriginalOpeningDeckSpawnRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-opening-deck-spawn' }
if ($OriginalPerkeoRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-perkeo' }
if ($OriginalCorruptedAngelsRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-corrupted-angels' }
if ($CorruptedAngelsCostOrder) { $environment['MT2_PROBE_ANGELS_COST_ORDER'] = $CorruptedAngelsCostOrder }
if ($OriginalUnitPhaseUpgradeRelics) { $environment['MT2_PROBE_MODIFIERS'] = 'original-unit-phase-upgrade-relics' }
if ($OriginalSoloUnitUpgradeRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-add-stats-to-solo-units' }
if ($OriginalFrontRoomHealthUpgradeRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-front-room-health-upgrade' }
if ($OriginalChampionStartTurnHealRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-champion-start-turn-heal' }
if ($OriginalNimsOathRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-nims-oath' }
if ($OriginalJudgesScalesRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-judges-scales' }
if ($OriginalFirstImpBonusRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-first-imp-bonus' }
if ($OriginalMoonPhaseRoomsGainMagicPowerRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-moon-phase-rooms-magic-power' }
if ($OriginalPropagateOnStatusEffectAppliedRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-propagate-on-status-effect-applied' }
if ($OriginalNoUpgradeCardCostRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-reduce-cost-unit-no-upgrade' }
if ($OriginalPyreHealthCardCostRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-pyre-health-card-cost' }
if ($OriginalNullRelic) { $environment['MT2_PROBE_MODIFIERS'] = 'original-null-relic' }
if ($OriginalNullRelicBaseline) { $environment['MT2_PROBE_MODIFIERS'] = 'original-null-relic-baseline' }
if ($OriginalRelicCountAttack) { $environment['MT2_PROBE_MODIFIERS'] = 'original-relic-count-attack' }
if ($OriginalMoreSpikes) { $environment['MT2_PROBE_MODIFIERS'] = 'original-more-spikes' }
if ($OriginalStealthSlay) { $environment['MT2_PROBE_MODIFIERS'] = 'original-stealth-slay' }
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
if ($FailFastOnIsolatedUnitReject -and -not $OriginalIsolatedUnitId -and $OriginalIsolatedUnitIds.Count -eq 0 -and $OriginalIsolatedEquipmentIds.Count -eq 0 -and $OriginalIsolatedSpellIds.Count -eq 0) {
    throw 'FailFastOnIsolatedUnitReject requires isolated original card IDs.'
}
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
if ($CardCatalog) {
    $cardCatalogAllowedArguments = @('Policy', 'GameSpeed', 'NativeWorker', 'TimeoutSeconds', 'CardCatalog',
        'BinaryCapture', 'CaptureJson', 'SkipBuild', 'FullRuleCatalog', 'Verbose', 'Debug', 'ErrorAction',
        'WarningAction', 'InformationAction', 'ProgressAction', 'ErrorVariable', 'WarningVariable',
        'InformationVariable', 'OutVariable', 'OutBuffer', 'PipelineVariable')
    if ($Policy -ne 'no-cards' -or @($PSBoundParameters.Keys | Where-Object { $_ -notin $cardCatalogAllowedArguments }).Count -gt 0) {
        throw 'CardCatalog requires the no-cards baseline without authored mechanism scenarios.'
    }
}
$gameArguments = @('-logFile', $unityLog)
if ($PlayerAdvisor) {
    $existingPlayer = @(Get-ChildItem -LiteralPath $pluginDir -Filter 'MonsterTrain2Poju.Advisor.dll' -File -Recurse)
    if ($existingPlayer.Count -ne 0) { throw 'Remove the existing player advisor from this isolated worker before running PlayerAdvisor.' }
    $playerCheckPlugin = Join-Path $pluginDir ('PojuPlayerCheck-' + [Guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $playerCheckPlugin
    Copy-Item -LiteralPath (Join-Path $workspace 'src/Advisor/bin/Release/netstandard2.1/MonsterTrain2Poju.Advisor.dll') -Destination $playerCheckPlugin
    $playerConfigPath = Join-Path $gameRoot 'BepInEx/config/netcan.monstertrain2.advisor.cfg'
    $playerConfigBackup = Join-Path $profile 'original-player-advisor.cfg'
    if (Test-Path -LiteralPath $playerConfigPath) { Move-Item -LiteralPath $playerConfigPath -Destination $playerConfigBackup }
    @"
[General]
Enabled = true
PauseWhenHidden = true
[Search]
Horizon = $PlannerHorizon
TimeLimitMs = $PlannerTimeLimitMs
PreferredFloor = $($PlannerDeploymentRoom + 1)
TacticalSeed = $([bool]$PlannerTacticalSeed)
"@ | Set-Content -LiteralPath $playerConfigPath -Encoding utf8
}
if ($ShowWindow) { $gameArguments += @('-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '800') }
$process = Start-Process -FilePath (Join-Path $gameRoot 'MonsterTrain2.exe') `
    -WorkingDirectory $gameRoot -ArgumentList $gameArguments `
    -Environment $environment -WindowStyle $(if ($ShowWindow) { 'Normal' } else { 'Hidden' }) -PassThru
Write-Output "FULL-BATTLE-RUN pid=$($process.Id) worker=$NativeWorker policy=$Policy profile=$profile"
. (Join-Path $PSScriptRoot 'NativeBattleProcess.ps1')
Wait-NativeBattleExit -Process $process -Timer $nativeTimer -UnityLog $unityLog -Profile $profile `
    -TimeoutSeconds $TimeoutSeconds -FailFastOnIsolatedUnitReject:$FailFastOnIsolatedUnitReject
$nativeTimer.Stop()
Write-Output "FULL-BATTLE-TIMING speed=$GameSpeed elapsedSeconds=$($nativeTimer.Elapsed.TotalSeconds.ToString('F2', [Globalization.CultureInfo]::InvariantCulture))"
if ($TrueFinalBossBirth) {
    $componentPath = Join-Path $profile $(if ($TrueFinalBossHealth) { 'true-final-boss-linked-health-calibration.mt2f' } else { 'true-final-boss-birth-calibration.mt2f' })
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $componentPath) -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'TRUE-FINAL-BOSS-BIRTH-NATIVE PASS' -Quiet) -or
        (Get-OriginalSignature) -cne $originalBefore) { throw "True-final-boss birth observation failed; inspect $unityLog" }
    [pscustomobject]@{ Profile = $profile; NativeComponentCaptured = $true; OriginalFilesUnchanged = $true;
        ModelAcceptance = 'Pending independent focused checker; full battle remains guarded'; Trace = $componentPath } | ConvertTo-Json
    return
}
if ($RoomMoonCallbacks) {
    $componentPath = Join-Path $profile $(if ($RoomMoonSpellAttackRelic) { 'room-moon-spell-attack-relic-calibration.mt2f' } elseif ($RoomMoonSpellCostRelic) { 'room-moon-spell-cost-relic-calibration.mt2f' } elseif ($RoomMoonSpellAdvance) { 'room-moon-spell-advance-calibration.mt2f' } elseif ($RoomMoonSpellUpgrades) { 'room-moon-spell-upgrade-calibration.mt2f' } elseif ($RoomMoonEnemyMovement) { 'room-moon-enemy-movement-calibration.mt2f' } elseif ($RoomMoonMovement) { 'room-moon-movement-calibration.mt2f' } else { 'room-moon-callback-calibration.mt2f' })
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $componentPath) -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ROOM-MOON-CALLBACK-NATIVE PASS' -Quiet) -or
        (Get-OriginalSignature) -cne $originalBefore) { throw "Room moon native calibration failed; inspect $unityLog" }
    Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
    $component = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($componentPath)
    try {
        $captured = $component.RootElement.ToObjectGraph()
        if (-not $captured.NativeCompleted -or -not $captured.RngUnchanged -or
            -not $captured.PreviewUpdatesSuppressed -or @($captured.Rows).Count -ne $(if ($RoomMoonSpellUpgrades) { 15 } elseif ($RoomMoonEnemyMovement) { 39 } elseif ($RoomMoonMovement) { 37 } elseif ($RoomMoonCallbacksShade) { 15 } else { 13 }) -or
            @($captured.ActorIds).Count -ne $(if ($RoomMoonSpellUpgrades) { 0 } elseif ($RoomMoonEnemyMovement) { 12 } elseif ($RoomMoonCallbacksShade) { 6 } else { 3 }) -or
            [bool]$captured.SpellUpgradeControls -ne [bool]$RoomMoonSpellUpgrades -or
            [bool]$captured.SpellAdvanceControls -ne [bool]$RoomMoonSpellAdvance -or
            [bool]$captured.CostRelicControls -ne [bool]$RoomMoonSpellCostRelic -or
            [bool]$captured.AttackRelicControls -ne [bool]$RoomMoonSpellAttackRelic -or
            [bool]$captured.ShadeCallbacks -ne [bool]$RoomMoonCallbacksShade -or
            [bool]$captured.EnemyControls -ne [bool]$RoomMoonEnemyMovement) {
            throw 'Incomplete room moon callback calibration.'
        }
    } finally { $component.Dispose() }
    [pscustomobject]@{ Profile = $profile; NativeComponentCaptured = $true; OriginalFilesUnchanged = $true; ModelAcceptance = 'Pending independent focused checker'; Trace = $componentPath } | ConvertTo-Json
    return
}
if ($ConduitMovement) {
    $componentPath = Join-Path $profile $(if ($ConduitEnemyMovement) { 'conduit-enemy-movement-calibration.mt2f' } else { 'conduit-movement-calibration.mt2f' })
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $componentPath) -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'CONDUIT-MOVEMENT-NATIVE PASS' -Quiet) -or
        (Get-OriginalSignature) -cne $originalBefore) { throw "Conduit native calibration failed; inspect $unityLog" }
    Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
    $component = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($componentPath)
    try {
        $captured = $component.RootElement.ToObjectGraph()
        if (-not $captured.NativeCompleted -or -not $captured.RngUnchanged -or @($captured.Rows).Count -ne 38 -or
            @($captured.Previews).Count -ne 3 -or [bool]$captured.EnemyCarrier -ne [bool]$ConduitEnemyMovement) { throw 'Incomplete Conduit movement calibration.' }
    } finally { $component.Dispose() }
    [pscustomobject]@{ Profile = $profile; NativeComponentCaptured = $true; Rows = 38; Previews = 3; OriginalFilesUnchanged = $true; ModelAcceptance = 'Pending independent focused checker'; Trace = $componentPath } | ConvertTo-Json
    return
}
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
if ($Planner) {
    $plannerRuns = @($trace.PlannerRuns)
    if ($plannerRuns.Count -ne 1 -or -not $plannerRuns[0].Completed -or $null -ne $plannerRuns[0].Difference -or
        @($plannerRuns[0].Steps).Count -eq 0 -or @($plannerRuns[0].Steps).Count -ne @($plannerRuns[0].Actual).Count -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'PLANNER-REPLAY-PASS' -Quiet)) {
        throw 'Native planner recommendation did not complete with every step verified.'
    }
}
if ($RollingAdvice -and (@($trace.RollingDecisions).Count -eq 0 -or
    @($trace.RollingDecisions | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference }).Count -ne 0 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ROLLING-REPLAY-PASS' -Quiet))) {
    throw 'Rolling advisor did not complete every native decision through terminal combat.'
}
if ($Advisor -and ($trace.Advisor.Captures -lt 1 -or $trace.Advisor.PurityFailures -ne 0 -or $trace.Advisor.Failures -gt 0 -or
    $trace.Advisor.Published -lt 2 -or $trace.Advisor.Invalidations -lt 1 -or $trace.Advisor.PlanningFrames -lt 2 -or
    ($null -ne $trace.Advisor.PanelVertices -and ($trace.Advisor.PanelVertices -lt 1 -or $trace.Advisor.PanelPixels -lt 1000)) -or
    -not (Test-Path -LiteralPath (Join-Path $profile 'advisor-ready.png')))) {
    throw 'Live advisor lacks pure capture, a rendered recommendation, background frames or action invalidation evidence.'
}
if ($PlayerAdvisor -and -not $PlannerInput -and -not $RollingInput) {
    $playerEvidence = $trace.PlayerAdvisor.Overlay
    if ($trace.PlayerAdvisor.Assembly -cne 'MonsterTrain2Poju.Advisor' -or -not $trace.PlayerAdvisor.InteractionsPassed -or
        $playerEvidence.Captures -lt 1 -or $playerEvidence.PurityFailures -ne 0 -or $playerEvidence.Failures -ne 0 -or
        $playerEvidence.Published -lt 4 -or $playerEvidence.Invalidations -lt 3 -or $playerEvidence.PlanningFrames -lt 2 -or
        $playerEvidence.PanelVertices -lt 1 -or $playerEvidence.PanelPixels -lt 1000 -or
        -not (Test-Path -LiteralPath (Join-Path $profile 'advisor-ready.png')) -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'PLAYER-ADVISOR-UI-PASS' -Quiet) -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'POJU-READY' -Quiet) -or $null -ne $trace.Advisor) {
        throw 'Real player advisor did not pass isolated recommendation and pointer/state acceptance.'
    }
    Write-Output "PLAYER-ADVISOR-PASS version=$($trace.PlayerAdvisor.Version) publications=$($playerEvidence.Published)"
}
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
$nullOriginalKeys = if ($OriginalNullScourges) { @('ScourgeT2', 'ScourgeT3', 'Scourge_SoulSavior') } else { @('ScourgeT1', 'SelfPurgeBlight') }
$nullOriginalScenario = if ($OriginalNullScourges) { 'original-null-scourges' } else { 'null-targetless' }
if ($NullTargetless -and ($trace.ModifierScenario -ne $nullOriginalScenario -or
    -not (Select-String -LiteralPath $unityLog -Pattern ('NULL-TARGETLESS-PREPARED originals=' + ($nullOriginalKeys -join ',')) -Quiet) -or
    @($nullOriginalKeys | Where-Object {
        -not (Select-String -LiteralPath $unityLog -Pattern "NULL-TARGETLESS-PLAY original=$_ room=0" -Quiet) -or
        -not (Select-String -LiteralPath $unityLog -Pattern "NULL-TARGETLESS-TEST original=$_ targets=0 result=True" -Quiet)
    }).Count -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne $nullOriginalKeys.Count)) {
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
if ($OriginalSmeltEmberRelic -or $OriginalSmeltEmberRelicZero -or $OriginalSmeltEmberRelicBulk) {
    $expectedScenario = if ($OriginalSmeltEmberRelicBulk) { 'original-smelt-ember-relic-bulk' } elseif ($OriginalSmeltEmberRelicZero) { 'original-smelt-ember-relic-zero' } else { 'original-smelt-ember-relic' }
    $first = @($trace.Actions)[0]
    $before = $first.Before.Spawn.Train.Context
    $after = $first.Actual.Spawn.Train.Context
    $smeltRelic = @($before.Relics | Where-Object DataId -EQ 'a034e00f-3a12-4c19-b626-4b8fd17c412a')
    $consumed = if ($OriginalSmeltEmberRelicBulk) { 4 } else { 1 }
    $cost = if ($OriginalSmeltEmberRelicBulk) { 3 } elseif ($OriginalSmeltEmberRelicZero) { 0 } else { 2 }
    if ($trace.ModifierScenario -ne $expectedScenario -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-SMELT-RELIC-ACQUIRED unchanged EmberFoundryCrucible additionalEmber=1' -Quiet) -or
        @($trace.Actions | Where-Object ScenarioAction).Count -ne 1 -or $smeltRelic.Count -ne 1 -or
        @($smeltRelic[0].ResourceEffects).Count -ne 1 -or $smeltRelic[0].ResourceEffects[0].Amount -ne 1 -or
        $after.QueryFrame.ForgePoints -ne ($before.QueryFrame.ForgePoints + $cost) -or
        $after.QueryFrame.Energy -ne ($before.QueryFrame.Energy - 1 + $cost + $consumed)) {
        throw 'Original EmberFoundryCrucible did not grant its expected per-card smelt Ember in native combat.'
    }
}
if ($OriginalTinkermasterMix -or $OriginalProgressPacemakerMix) {
    $progress = [bool] $OriginalProgressPacemakerMix
    $expectedScenario = if ($progress) { 'original-progress-pacemaker-mix' } else { 'original-tinkermaster-mix' }
    $expectedId = if ($progress) { '4446fd0c-f7c3-4a9d-96c3-bd12e4426a76' } else { 'db2193eb-a476-4cc4-9a26-2d0e35f46b74' }
    $draftLines = @(Select-String -LiteralPath $unityLog -Pattern 'MIX-DRAFT index=')
    $offerCounts = @($draftLines | ForEach-Object {
        if ($_.Line -match ' choices=(\S+) selected=') { $Matches[1].Split(',').Length } else { 0 }
    })
    $mixActions = @($trace.Actions | Where-Object { @($_.Action.MixSelections).Count -gt 0 })
    $owner = if ($mixActions.Count -gt 0) {
        @($mixActions[0].Before.Spawn.Train.Context.Relics | Where-Object DataId -EQ $expectedId)
    } else { @() }
    $effect = if ($owner.Count -eq 1) { @($owner[0].ResourceEffects)[0] } else { $null }
    if ($trace.ModifierScenario -ne $expectedScenario -or
        -not (Select-String -LiteralPath $unityLog -Pattern "ORIGINAL-MIX-RELIC-ACQUIRED unchanged $(if ($progress) { 'ProgressPacemaker' } else { 'TinkermasterTools' })" -Quiet) -or
        $mixActions.Count -ne 2 -or $owner.Count -ne 1 -or $effect -eq $null -or
        $effect.ParamInt -ne $(if ($progress) { 0 } else { 1 }) -or
        $effect.ParamInt2 -ne $(if ($progress) { 1 } else { 0 }) -or
        $effect.ParamMinInt -ne $(if ($progress) { 2 } else { 0 }) -or
        $draftLines.Count -ne $(if ($progress) { 6 } else { 4 }) -or
        @($offerCounts | Where-Object { $_ -ne $(if ($progress) { 2 } else { 3 }) }).Count -gt 0 -or
        @($mixActions | Where-Object { @($_.Action.MixSelections).Count -ne $(if ($progress) { 3 } else { 2 }) }).Count -gt 0) {
        throw 'Original Mix relic did not produce the expected native draft amounts and repetitions.'
    }
}
if ($OriginalConserveEmber) {
    $conservation = @($trace.EnergyConservationQueries)
    $carried = @($conservation | Where-Object { $_.Completed -and -not $_.Preview -and $_.RemainingPlayerEnergy -gt 0 })
    $paid = @($trace.Actions | Where-Object { $_.Before.Energy -gt $_.Actual.Energy })
    if ($trace.ModifierScenario -ne 'original-conserve-ember-relic' -or
        $carried.Count -lt 2 -or $paid.Count -eq 0 -or
        @($conservation | Where-Object { -not $_.Completed -or $_.RemovedEnergy -ne 0 -or
            $_.PlayerEnergyBefore -ne $_.PlayerEnergyAfter }).Count -ne 0 -or
        @($trace.Turns | Where-Object { $_.Before.Spawn.Turn -lt 2 }).Count -lt 2) {
        throw 'Original ConserveEmber must preserve positive energy across turns and spend retained energy in a complete battle.'
    }
    Write-Output "NATIVE-CONSERVE-EMBER PASS: $($carried.Count) positive carry queries, $($paid.Count) paid plays."
}
if ($OriginalCardTypeCostRelic -or $CardTypeCostUpgradeOrder) {
    $plays = @($trace.Actions | Select-Object -First 2)
    $expectedIds = @('21038774-c0a4-413c-ab7e-27e458228e5f', '62cc5532-5fa0-4e66-9fec-85be573af359')
    $expectedCostScenario = if ($CardTypeCostUpgradeOrder -eq 'DiscountFirst') { 'card-type-cost-discount-first' } elseif ($CardTypeCostUpgradeOrder -eq 'UpgradeFirst') { 'card-type-cost-upgrade-first' } else { 'original-card-type-cost-relic' }
    if ($trace.ModifierScenario -ne $expectedCostScenario -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARD-TYPE-COST-ACQUIRED unchanged CheaperBlightScourge Blight=-99 Junk=-99' -Quiet) -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARD-TYPE-COST-PREPARED blight=\d+ cost=0 junk=\d+ cost=0' -Quiet) -or
        $plays.Count -ne 2 -or @($trace.RelicCardModifiers).Count -lt 10) {
        throw 'Original CheaperBlightScourge native setup or manager evidence is missing.'
    }
    if ($CardTypeCostUpgradeOrder) {
        $blightResets = @($trace.RelicCardModifiers | Where-Object { $_.ResetTemporary -and $_.Before.DataId -eq $expectedIds[0] })
        $expectedMarkerCount = if ($CardTypeCostUpgradeOrder -eq 'DiscountFirst') { 1 } else { 0 }
        if ($blightResets.Count -eq 0 -or @($blightResets | Where-Object {
            @($_.Actual.Temporary.Upgrades | Where-Object DataId -EQ 'fc119000-0000-4000-8000-000000000191').Count -ne $expectedMarkerCount
        }).Count -gt 0) { throw 'Native cost/upgrade ordering did not produce the expected marker acceptance.' }
        Write-Output "NATIVE-CARD-TYPE-COST-ORDER PASS: $CardTypeCostUpgradeOrder resets=$($blightResets.Count) markerCount=$expectedMarkerCount."
    }
    for ($index = 0; $index -lt 2; $index++) {
        $play = $plays[$index]
        $card = @($play.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $play.Action.CardInstanceId)
        if (-not $play.ScenarioAction -or $card.Count -ne 1 -or $card[0].DataId -ne $expectedIds[$index] -or
            $play.Before.Energy -ne $play.Actual.Energy) {
            throw 'Original Blight/Junk cards did not play free in their expected order.'
        }
    }
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
if ($CorruptionKeeperTrigger -and -not $TowerCorruptionCallbacks) {
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
if ($ConditionalPyreLethal -and ($trace.ModifierScenario -ne 'conditional-pyre-damage-lethal' -or
    $trace.NativeWon -or @($trace.Turns).Count -ne 2 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'CONDITIONAL-PYRE-DAMAGE-PREPARED original=HeavyT3_PyreDamageIfUnblocked firstWaveGroups=\d+' -Quiet))) {
    throw 'Original conditional Pyre lethal boundary was not captured.'
}
if ($TrainHealPerEmberX -and ($trace.ModifierScenario -ne 'train-heal-per-ember-x' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'EMBER-TRAIN-HEAL-X-PREPARED original upgrade; two original Stewards, two original Vent cards with \+2 X; Pyre missing 12 HP.' -Quiet))) {
    throw 'Original X-cost Pyre healing controls were not prepared.'
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
if ($OriginalTwoChargesOnCardConsumed -and (
    $trace.Schema -ne 195 -or
    -not $trace.OriginalTwoChargesOnCardConsumedPrepared -or
    $trace.ModifierScenario -ne 'original-two-charges-on-card-consumed' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-TWO-CHARGES-ACQUIRED unchanged relic=2ChargesOnCardConsumed' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-TWO-CHARGES-PREPARED carrier=' -Quiet) -or
    $trace.OriginalTwoChargesOnCardConsumedCoverage.Error -or
    -not $trace.OriginalTwoChargesOnCardConsumedCoverage.ControlCompleted -or
    -not $trace.OriginalTwoChargesOnCardConsumedCoverage.CarrierCompleted -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2)) {
    throw 'Original 2ChargesOnCardConsumed failed its non-Exhaust control or corruption positive in the expected full-battle capture.'
}
if ($OriginalChanceForSpellWeakness -and (
    $trace.Schema -ne 196 -or
    -not $trace.OriginalChanceForSpellWeaknessPrepared -or
    $trace.ModifierScenario -ne 'original-chance-for-spell-weakness' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CHANCE-SPELL-WEAKNESS-ACQUIRED unchanged owner' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CHANCE-SPELL-WEAKNESS-PREPARED unchanged 50% owner' -Quiet) -or
    @($trace.RelicAfterSpawnChanceTests | Where-Object { $_.Trigger -eq 'AfterSpawnEnchant' -and $_.Team -eq 'Heroes' -and -not $_.IsDestroyed -and -not $_.Untouchable }).Count -lt 2 -or
    @($trace.RelicAfterSpawnChanceTests | Where-Object { $_.Trigger -eq 'AfterSpawnEnchant' -and $_.Team -eq 'Heroes' -and -not $_.IsDestroyed -and -not $_.Untouchable -and $_.Result }).Count -eq 0 -or
    @($trace.RelicAfterSpawnChanceTests | Where-Object { $_.Trigger -eq 'AfterSpawnEnchant' -and $_.Team -eq 'Heroes' -and -not $_.IsDestroyed -and -not $_.Untouchable -and -not $_.Result }).Count -eq 0 -or
    @($trace.RelicAfterSpawnChanceApplications | Where-Object { -not $_.Completed -or $_.SpellWeaknessAfter -ne ($_.SpellWeaknessBefore + 1) }).Count -gt 0)) {
    throw 'ChanceForSpellWeakness lacks its unchanged owner, natural hit/miss rolls or completed +1 status applications.'
}
if ($OriginalChanceToCleanse -and (
    $trace.Schema -ne 198 -or
    -not $trace.OriginalChanceToCleansePrepared -or
    $trace.ModifierScenario -ne 'original-chance-to-cleanse' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CHANCE-TO-CLEANSE-ACQUIRED unchanged owner' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CHANCE-TO-CLEANSE-PREPARED unchanged 50% owner' -Quiet) -or
    @($trace.RelicAfterSpawnChanceTests | Where-Object { $_.Trigger -eq 'AfterSpawnEnchant' -and $_.Team -eq 'Heroes' -and -not $_.IsDestroyed -and -not $_.Untouchable }).Count -lt 2 -or
    @($trace.RelicAfterSpawnChanceTests | Where-Object { $_.Trigger -eq 'AfterSpawnEnchant' -and $_.Team -eq 'Heroes' -and -not $_.IsDestroyed -and -not $_.Untouchable -and $_.Result }).Count -eq 0 -or
    @($trace.RelicAfterSpawnChanceTests | Where-Object { $_.Trigger -eq 'AfterSpawnEnchant' -and $_.Team -eq 'Heroes' -and -not $_.IsDestroyed -and -not $_.Untouchable -and -not $_.Result }).Count -eq 0 -or
    @($trace.RelicAfterSpawnChanceApplications | Where-Object { -not $_.Completed }).Count -gt 0)) {
    throw 'ChanceToCleanse lacks its unchanged owner, natural 50% hit/miss rolls or completed native applications.'
}
if ($OriginalExtraFeedTrigger -and (
    $trace.Schema -ne 199 -or
    -not $trace.OriginalExtraFeedTriggerPrepared -or
    $trace.ModifierScenario -ne 'original-extra-feed-trigger' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-EXTRA-FEED-TRIGGER-ACQUIRED unchanged owner' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-EXTRA-FEED-TRIGGER-PREPARED owner unchanged' -Quiet) -or
    $trace.OriginalExtraFeedTriggerEvidence.PlayerUnitId -eq 0 -or
    $trace.OriginalExtraFeedTriggerEvidence.PlayerOnFeedCount -ne 2 -or
    $trace.OriginalExtraFeedTriggerEvidence.PlayerOnDeathCount -ne 1
)) {
    throw 'ExtraFeedTrigger lacks its unchanged owner or native OnFeed/OnDeath count controls.'
}
if ($OriginalSummonWithAvarice -and (
    $trace.Schema -ne 197 -or
    -not $trace.OriginalSummonWithAvaricePrepared -or
    $trace.ModifierScenario -ne 'original-summon-with-avarice' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-SUMMON-AVARICE-ACQUIRED unchanged owner' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-SUMMON-AVARICE-PREPARED unchanged Monster-only owner' -Quiet) -or
    @($trace.RelicSpawnStatuses).Count -eq 0
)) {
    throw 'SummonWithAvarice lacks its unchanged owner, prepared spawn capture or natural character-birth witnesses.'
}
if ($OriginalCardDrawOnCardPlay -and ($trace.Schema -ne 160 -or
    $trace.ModifierScenario -ne 'original-card-draw-on-card-play' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARD-DRAW-RELICS-ACQUIRED unchanged relics=' -Quiet) -or
    @($trace.Actions).Count -eq 0)) {
    throw 'Original card-draw-on-play relic was not exercised in the expected full-battle capture.'
}
if ($OriginalCarriageJack -and (
    $trace.Schema -ne 165 -or
    -not $trace.OriginalCarriageJackPrepared -or
    $trace.ModifierScenario -ne 'original-carriage-jack' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARRIAGE-JACK-ACQUIRED unchanged relic=CarriageJack' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARRIAGE-JACK-PREPARED attachment=IncreaseRoomCapacity' -Quiet) -or
    $trace.OriginalCarriageJackCoverage.Error -or
    -not $trace.OriginalCarriageJackCoverage.NormalCompleted -or
    -not $trace.OriginalCarriageJackCoverage.AttachmentCompleted -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2)) {
    throw 'Original CarriageJack failed its ordinary-card and room-attachment controls in the expected full-battle capture.'
}
if ($OriginalFirstRoomAttachment -and (
    $trace.Schema -ne 167 -or
    -not $trace.OriginalFirstRoomAttachmentPrepared -or
    $trace.ModifierScenario -ne 'original-first-room-attachment' -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-FIRST-ROOM-ATTACHMENT-ACQUIRED unchanged relic=FirstRoomCardOnAllFloors' -Quiet) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-FIRST-ROOM-ATTACHMENT-PREPARED card=IncreaseRoomCapacity' -Quiet) -or
    $trace.OriginalFirstRoomAttachmentCoverage.Error -or
    -not $trace.OriginalFirstRoomAttachmentCoverage.NormalCompleted -or
    -not $trace.OriginalFirstRoomAttachmentCoverage.FirstAttachmentCompleted -or
    -not $trace.OriginalFirstRoomAttachmentCoverage.SecondAttachmentCompleted -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 3)) {
    throw 'Original FirstRoomCardOnAllFloors failed its negative, first-attachment and later-attachment controls in the expected full-battle capture.'
}
if ($OriginalStartRoomAttachment -or $OriginalStartRoomAttachmentFloor2 -or $OriginalStartRoomAttachmentFloor3) {
    $expectedStartRoomSchema = if ($OriginalStartRoomAttachmentFloor3) { 187 } elseif ($OriginalStartRoomAttachmentFloor2) { 186 } else { 168 }
    $expectedStartRoomModifier = if ($OriginalStartRoomAttachmentFloor3) { 'original-start-room-attachment-floor3' } elseif ($OriginalStartRoomAttachmentFloor2) { 'original-start-room-attachment-floor2' } else { 'original-start-room-attachment' }
    $expectedStartRoomRelic = if ($OriginalStartRoomAttachmentFloor3) { 'TrainRoomInstallFloor3AtStartOfCombat' } elseif ($OriginalStartRoomAttachmentFloor2) { 'TrainRoomInstallFloor2AtStartOfCombat' } else { 'TrainRoomInstallFloor1AtStartOfCombat' }
    $expectedStartRoomRelicId = if ($OriginalStartRoomAttachmentFloor3) { 'c474dd00-3725-46d6-b074-0be7f7687cdd' } elseif ($OriginalStartRoomAttachmentFloor2) { 'b037ec04-6164-4cd8-8b9d-3bc420c29351' } else { '2c97e1b3-d4bb-4133-9572-e582805c106f' }
    $expectedStartRoomIndex = if ($OriginalStartRoomAttachmentFloor3) { 2 } elseif ($OriginalStartRoomAttachmentFloor2) { 1 } else { 0 }
    $expectedStartRoomAffectsHeroes = -not $OriginalStartRoomAttachmentFloor3
    if ($trace.Schema -ne $expectedStartRoomSchema -or
        -not $trace.OriginalStartRoomAttachmentPrepared -or
        $trace.ModifierScenario -ne $expectedStartRoomModifier -or
        -not (Select-String -LiteralPath $unityLog -Pattern "ORIGINAL-START-ROOM-ATTACHMENT-ACQUIRED unchanged relic=$expectedStartRoomRelic id=$expectedStartRoomRelicId room=$expectedStartRoomIndex" -Quiet) -or
        $trace.OriginalStartRoomAttachmentCoverage.Error -or
        -not $trace.OriginalStartRoomAttachmentCoverage.Prepared -or
        $trace.OriginalStartRoomAttachmentCoverage.RelicId -ne $expectedStartRoomRelicId -or
        $trace.OriginalStartRoomAttachmentCoverage.RelicKey -ne $expectedStartRoomRelic -or
        $trace.OriginalStartRoomAttachmentCoverage.RoomIndex -ne $expectedStartRoomIndex -or
        $trace.OriginalStartRoomAttachmentCoverage.AttachmentType -ne 0 -or
        $trace.OriginalStartRoomAttachmentCoverage.AffectsHeroes -ne $expectedStartRoomAffectsHeroes -or
        -not $trace.OriginalStartRoomAttachmentCoverage.AffectsMonsters -or
        $trace.OriginalStartRoomAttachmentCoverage.AttachmentsAddedFromCards -ne 0 -or
        $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0 -or
        -not $trace.NativeWon) {
        throw 'The selected original start-of-combat attachment relic did not complete its clean full battle setup.'
    }
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
if ($OriginalMoonPhaseHealRelic) {
    $nativeMoonHealOperations = @($trace.RelicMoonPhaseHealOperations | Where-Object { $_.OwnerDataId -eq '6d977136-5aac-44fd-9843-54cf2a0bd01d' })
    if ($trace.Schema -ne 166 -or $trace.ModifierScenario -ne 'original-moon-phase-heal-relic' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-MOON-PHASE-HEAL-RELIC-PREPARED unchanged owner' -Quiet) -or
        @($trace.Actions).Count -lt 5 -or $nativeMoonHealOperations.Count -lt 3 -or
        @($nativeMoonHealOperations | Where-Object { $_.EffectIndex -ne 0 -or $_.SourceTeam -ne 'Monsters' -or
            $_.TargetMode -ne 'WeakestAllRooms' -or $_.HealthAmount -ne 10 -or $_.MoonPhaseMask -ne -1 -or $_.After -eq $null }).Count -gt 0 -or
        @($nativeMoonHealOperations | Where-Object { $_.Phase -eq 'Full' }).Count -eq 0 -or
        @($nativeMoonHealOperations | Where-Object { $_.Phase -eq 'New' }).Count -eq 0) {
        throw 'Original MooncycleRestoreFriendlyHealth lacks both phase callbacks or its unchanged payload witness.'
    }
}
if ($OriginalMoonPhaseStatusRelic) {
    $moonStatusCallbacks = @($trace.RelicMoonPhaseStatusOperations | Where-Object { $_.OwnerDataId -eq 'e013b17d-753b-47b8-a837-02af85addb4b' })
    if ($trace.Schema -ne 190 -or $trace.ModifierScenario -ne 'original-moon-phase-status-relic' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-MOON-PHASE-STATUS-RELIC-PREPARED unchanged owner' -Quiet) -or
        @($trace.Actions).Count -lt 4 -or $moonStatusCallbacks.Count -lt 2 -or
        @($moonStatusCallbacks | Where-Object { $_.EffectIndex -ne 0 -or $_.SourceTeam -ne 'Monsters' -or
            $_.TargetMode -ne 'Tower' -or $_.RequiredSubtype -ne 'SubtypesData_Silent' -or
            $_.DoNotAnimateCamera -ne $true -or $_.MoonPhaseMask -ne 1 -or
            $_.StatusIds.Count -ne 1 -or $_.StatusIds[0] -ne 'armor' -or
            $_.StatusStacks.Count -ne 1 -or $_.StatusStacks[0] -ne 10 -or $_.After -eq $null }).Count -gt 0 -or
        @($moonStatusCallbacks | Where-Object { $_.Phase -eq 'Full' }).Count -eq 0 -or
        @($moonStatusCallbacks | Where-Object { $_.Phase -eq 'New' }).Count -eq 0) {
        throw 'Original MoonPhaseAddArmorToSilent lacks both Full/New callback witnesses or its unchanged owner payload.'
    }
}
if ($OriginalUpgradeCountRelics) {
    if ($trace.ModifierScenario -ne 'original-upgrade-count-relics' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-UPGRADE-COUNT-RELICS-PREPARED 36 detached refreshes' -Quiet) -or
        @($trace.RelicCardModifiers).Count -lt 38 -or @($trace.Actions).Count -lt 2 -or
        @($trace.RelicCardModifiers | Where-Object { $_.Difference -ne $null }).Count -gt 0) {
        throw 'Original upgrade-count relics lack native manager and played-card witnesses.'
    }
}
if ($OriginalHealingPowerRelic) {
    if ($trace.ModifierScenario -ne 'original-healing-power-relic' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-HEALING-POWER-PREPARED 15 detached refreshes' -Quiet) -or
        @($trace.RelicCardModifiers).Count -lt 16 -or
        @($trace.RelicCardModifiers | Where-Object { $_.Difference -ne $null }).Count -gt 0) {
        throw 'Original healing-power relic lacks native manager observations.'
    }
}
if ($OriginalStartCombatEnergyRelic) {
    $callbacks = @($trace.RelicResourceOperations | Where-Object Kind -eq 'StartCombatEnergy')
    if ($trace.ModifierScenario -ne 'original-start-combat-energy-relic' -or $callbacks.Count -ne 1 -or
        $callbacks[0].Before.Turn -ne 1 -or $callbacks[0].Deployment -or $callbacks[0].Notifications -ne 0 -or
        $callbacks[0].After.Energy -ne [Math]::Min($callbacks[0].Before.Resources.MaxEnergy, $callbacks[0].Before.Energy + 2)) {
        throw 'Original start-combat energy relic lacks its unique turn-one callback.'
    }
}
if ($OriginalStartCombatHoardRelic) {
    $callbacks = @($trace.RelicResourceOperations | Where-Object Kind -eq 'StartCombatHoard')
    if ($trace.Schema -ne 201 -or $trace.ModifierScenario -ne 'original-start-combat-hoard-relic' -or $callbacks.Count -ne 1 -or
        -not $callbacks[0].Deployment -or $callbacks[0].Notifications -ne 0 -or
        $callbacks[0].After.Hoard -ne ($callbacks[0].Before.Hoard + 1) -or
        $callbacks[0].CardRefreshes -ne 1 -or $callbacks[0].MonsterRefreshes -ne 1) {
        throw 'Original ModifyDragonsHoardBattleStart lacks its unique deployment callback and dependent resource refreshes.'
    }
}
if ($OriginalForgeConservationRelic) {
    $forgeCallbacks = @($trace.ForgeCombatEndOperations)
    $naturalForge = @($forgeCallbacks | Where-Object Kind -eq 'natural')
    if ($trace.ModifierScenario -ne 'original-forge-conservation-relic' -or $forgeCallbacks.Count -ne 6 -or
        $naturalForge.Count -ne 1 -or $naturalForge[0].Before.QueryFrame.ForgePoints -ne 17 -or
        $naturalForge[0].After.QueryFrame.ForgePoints -ne 10 -or $naturalForge[0].Notifications -ne 1) {
        throw 'Original SteelStockpile lacks its natural battle-end 17-to-10 conservation witness.'
    }
}
if ($OriginalOpeningCorruptionRelics) {
    if ($trace.ModifierScenario -ne 'original-opening-corruption-relics' -or @($trace.OpeningCorruptionOperations).Count -ne 3 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-OPENING-CORRUPTION-PREPARED three natural deployment callbacks' -Quiet)) {
        throw 'Original opening-corruption relics lack their natural ordered deployment callbacks.'
    }
}
if ($OriginalSpellPlayThresholdRelics) {
    if ($trace.ModifierScenario -ne 'original-spell-play-threshold-relics' -or
        @($trace.Actions).Count -eq 0 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-SPELL-PLAY-THRESHOLDS-PREPARED' -Quiet)) {
        throw 'Original spell threshold relics lack the unchanged owners and consuming/non-consuming carrier setup.'
    }
}
if ($OriginalCombatTriggerRelics) {
    if ($trace.ModifierScenario -ne 'original-combat-trigger-relics' -or
        @($trace.RelicSpawnStatuses).Count -eq 0 -or @($trace.Actions).Count -eq 0 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-COMBAT-TRIGGERS-PREPARED' -Quiet)) {
        throw 'Original Shroom Decay lacks its unchanged owner, birth records and carrier setup.'
    }
}
if ($OriginalStealthSlay) {
    if ($trace.ModifierScenario -ne 'original-stealth-slay' -or
        @($trace.RelicSpawnStatuses).Count -eq 0 -or @($trace.Actions).Count -eq 0 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-STEALTH-SLAY-PREPARED' -Quiet)) {
        throw 'Original stealth-on-slay relic lacks its unchanged owner, birth records and carrier setup.'
    }
}
if ($OriginalConduitRelics) {
    if ($trace.ModifierScenario -ne 'original-conduit-relics' -or
        @($trace.RelicSpawnStatuses).Count -eq 0 -or @($trace.Actions).Count -eq 0 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CONDUIT-RELICS-PREPARED three unchanged owners' -Quiet)) {
        throw 'Original Conduit relics lack their unchanged owners, birth records and carrier setup.'
    }
}
if ($OriginalEquipmentGrowthRelics) {
    if ($trace.ModifierScenario -ne 'original-equipment-growth-relics' -or
        @($trace.RelicSpawnStatuses).Count -eq 0 -or @($trace.Actions).Count -eq 0 -or
        @($trace.OriginalEquipmentGrowthEncounterComplete).Count -ne 1 -or
        -not $trace.OriginalEquipmentGrowthEncounterComplete[0].Completed -or
        $null -eq $trace.OriginalEquipmentGrowthEncounterComplete[0].Before -or
        $null -eq $trace.OriginalEquipmentGrowthEncounterComplete[0].After -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-EQUIPMENT-GROWTH-PREPARED four unchanged owners' -Quiet)) {
        throw 'Original equipment growth relics lack unchanged owners, birth records, carrier setup or encounter-complete settlement.'
    }
}
if ($CampaignRelicCalibration) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $campaignPath = Join-Path $profile 'campaign-relic-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $campaignPath)) { throw 'Missing current campaign relic calibration.' }
    $campaignDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($campaignPath)
    try {
        $campaignRoot = $campaignDocument.RootElement
        if ($campaignRoot.GetProperty('Schema').GetInt32() -ne 1 -or
            $campaignRoot.GetProperty('Owners').GetArrayLength() -ne 6 -or
            -not $campaignRoot.GetProperty('Before').ContentEquals($campaignRoot.GetProperty('After')) -or
            $campaignRoot.GetProperty('Slots').GetArrayLength() -ne 80 -or
            $campaignRoot.GetProperty('Merchant').GetArrayLength() -ne 108 -or
            $campaignRoot.GetProperty('Trial').GetArrayLength() -ne 66 -or
            $campaignRoot.GetProperty('Hoard').GetArrayLength() -ne 6) {
            throw 'Campaign original-owner matrix or recorded state restoration is incomplete.'
        }
    } finally { $campaignDocument.Dispose() }
}
if ($CampaignDraftEnhancerCountCalibration) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $countDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'campaign-draft-enhancer-count-calibration.mt2f'))
    try {
        $countRoot = $countDocument.RootElement
        if ($countRoot.GetProperty('Schema').GetInt32() -ne 1 -or
            $countRoot.GetProperty('Consumers').GetArrayLength() -ne 5 -or
            $countRoot.GetProperty('RuleMatrix').GetArrayLength() -ne 30 -or
            -not $countRoot.GetProperty('Before').ContentEquals($countRoot.GetProperty('After'))) {
            throw 'Original draft-enhancer-count consumer/reset evidence or recorded restoration is incomplete.'
        }
    } finally { $countDocument.Dispose() }
}
if ($CampaignDeckCopyCalibration) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $copyDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'campaign-deck-copy-calibration.mt2f'))
    try {
        $copyRoot = $copyDocument.RootElement
        if ($copyRoot.GetProperty('Rows').GetArrayLength() -ne 72 -or
            -not $copyRoot.GetProperty('Before').ContentEquals($copyRoot.GetProperty('After'))) { throw 'Original permanent deck-copy matrix or recorded restoration is incomplete.' }
    } finally { $copyDocument.Dispose() }
}
if ($CampaignPurgeGoldCalibration) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $purgeDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'campaign-purge-gold-calibration.mt2f'))
    try {
        $purgeRoot = $purgeDocument.RootElement
        if ($purgeRoot.GetProperty('Rows').GetArrayLength() -ne 144 -or
            -not $purgeRoot.GetProperty('Before').ContentEquals($purgeRoot.GetProperty('After'))) {
            throw 'Original purge-gold consumer controls or recorded restoration are incomplete.'
        }
    } finally { $purgeDocument.Dispose() }
}
if ($CampaignBattleResourceCalibration) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $battleResourcePath = Join-Path $profile 'campaign-battle-resource-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $battleResourcePath)) { throw 'Missing current end-battle resource calibration.' }
    $battleResourceDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($battleResourcePath)
    try {
        $battleResourceRoot = $battleResourceDocument.RootElement
        if ($battleResourceRoot.GetProperty('Schema').GetInt32() -ne 1 -or
            $battleResourceRoot.GetProperty('Owners').GetArrayLength() -ne 2 -or
            $battleResourceRoot.GetProperty('Rows').GetArrayLength() -ne 756 -or
            -not $battleResourceRoot.GetProperty('Before').ContentEquals($battleResourceRoot.GetProperty('After'))) {
            throw 'End-battle original-owner matrix or recorded state restoration is incomplete.'
        }
    } finally { $battleResourceDocument.Dispose() }
}
if ($CampaignPostBattleCalibration) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $postBattlePath = Join-Path $profile 'campaign-post-battle-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $postBattlePath)) { throw 'Missing current post-battle reward calibration.' }
    $postBattleDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($postBattlePath)
    try {
        $postBattleRoot = $postBattleDocument.RootElement
        if ($postBattleRoot.GetProperty('Schema').GetInt32() -ne 1 -or
            $postBattleRoot.GetProperty('Owners').GetArrayLength() -ne 2 -or
            $postBattleRoot.GetProperty('Rows').GetArrayLength() -ne 108 -or
            -not $postBattleRoot.GetProperty('Before').ContentEquals($postBattleRoot.GetProperty('After'))) {
            throw 'Post-battle original-owner matrix or recorded state restoration is incomplete.'
        }
    } finally { $postBattleDocument.Dispose() }
}
if ($OriginalRelicCountAttack -and ($trace.ModifierScenario -ne 'original-relic-count-attack' -or
    @($trace.RelicCountAttackControls).Count -ne 45 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-RELIC-COUNT-PREPARED actual foreign/active starter' -Quiet))) {
    throw 'Original relic-count attack owner lacks native classification controls.'
}
if ($OriginalBigDamageFromManyMorsels -and ($trace.ModifierScenario -ne 'original-big-damage-many-morsels' -or
    @($trace.RelicSpawnStatuses | Where-Object { $null -ne $_.BeforeTrain -and $null -ne $_.AfterTrain }).Count -lt 2 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-BIG-DAMAGE-PREPARED seeded Snack count=19' -Quiet))) {
    throw 'BigDamageFromManyMorsels lacks isolated threshold controls or complete-train birth captures.'
}
if ($OriginalEarlyDiscardEnergy -and ($trace.Schema -ne 170 -or $trace.ModifierScenario -ne 'hand-removal' -or
    -not $trace.OriginalEarlyDiscardEnergyPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-EMBER-EARLY-DISCARD-PREPARED' -Quiet))) {
    throw 'EmberOnEarlyDiscard lacks the isolated card-triggered discard scenario.'
}
if ($OriginalCheaperSpellCost -and ($trace.Schema -ne 171 -or
    $trace.ModifierScenario -ne 'original-reduce-spell-cost-cheaper' -or
    -not $trace.OriginalCheaperSpellCostPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CHEAPER-SPELL-RELIC-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -lt 2)) {
    throw 'ReduceSpellCostOfCheaper lacks strict-cost, filter and cleanup actions in the isolated battle.'
}
if ($OriginalFirstCardCost -and ($trace.Schema -ne 205 -or
    $trace.ModifierScenario -ne 'original-reduce-first-spell-cost-per-deck-size' -or
    -not $trace.OriginalFirstCardCostPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-FIRST-CARD-COST-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -lt 3)) {
    throw 'ReduceFirstSpellCostPerDeckSize lacks first-play cleanup, end-turn reset and retained next-turn Spell witnesses.'
}
if ($OriginalRebalanceValor -and ($trace.Schema -ne 206 -or
    $trace.ModifierScenario -ne 'original-rebalance-valor' -or
    -not $trace.OriginalRebalanceValorPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-REBALANCE-VALOR-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -lt 1 -or
    @($trace.OriginalRebalanceValorObservations).Count -lt 1)) {
    throw 'RebalanceValor lacks the original +1 Armor-per-stack carrier witness.'
}
if ($OriginalNefariousOverflow -and ($trace.Schema -ne 172 -or
    $trace.ModifierScenario -ne 'original-nefarious-overflow' -or
    -not $trace.OriginalNefariousOverflowPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-NEFARIOUS-OVERFLOW-PREPARED' -Quiet))) {
    throw 'NefariousOverflow lacks its original relic and isolated Unstable explosion witness.'
}
if ($OriginalUncontrolledGroup) {
    $growthCallbacks = @($trace.UncontrolledGroupGrowthCallbacks | Where-Object {
        $_.Stage -eq 'OnMonsterTeamTurnBegin' -and $_.UnstableBefore -eq 5 -and $_.UnstableAfter -eq 10
    })
    if ($trace.Schema -ne 191 -or -not $trace.OriginalUncontrolledGroupPrepared -or
        $trace.ModifierScenario -ne 'original-uncontrolled-group' -or $growthCallbacks.Count -lt 1) {
        throw 'UncontrolledGroup lacks its unchanged relic and native five-stack growth callback witness.'
    }
}
if ($OriginalCopyFirstSpellPlayed -and ($trace.Schema -ne 173 -or
    $trace.ModifierScenario -ne 'original-copy-first-spell-played' -or
    -not $trace.OriginalCopyFirstSpellPlayedPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-COPY-FIRST-SPELL-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -lt 2)) {
    throw 'CopyFirstSpellPlayed lacks its first direct spell and same-turn spell witness.'
}
if ($OriginalCopyFirstConsumePlayed -and ($trace.Schema -ne 180 -or
    $trace.ModifierScenario -ne 'original-copy-first-consume-played' -or
    -not $trace.OriginalCopyFirstConsumePlayedPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-COPY-FIRST-CONSUME-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -lt 2)) {
    throw 'CopyFirstConsumePlayed lacks its consumed direct Spell and same-battle copy control.'
}
if ($OriginalCopyConsumedCard -and ($trace.Schema -ne 181 -or
    $trace.ModifierScenario -ne 'original-copy-consumed-card' -or
    -not $trace.OriginalCopyConsumedCardPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-COPY-CONSUMED-CARD-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -lt 2)) {
    throw 'CopyConsumedCard lacks its direct Consume Spell and same-battle copy control.'
}
if ($OriginalMothersRemnant3Relic -and ($trace.OriginalMothersRemnant3Schema -ne 216 -or
    $trace.ModifierScenario -ne 'original-mothers-remnant3' -or
    -not $trace.OriginalMothersRemnant3Prepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-MOTHERS-REMNANT3-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -lt 2)) {
    throw 'MothersRemnant3 lacks its unchanged owner and same-turn permanent-copy control.'
}
if ($OriginalTribouletRelic -and ($trace.Schema -ne 217 -or $trace.OriginalTribouletSchema -ne 217 -or
    $trace.ModifierScenario -ne 'original-balatro-event-triboulet' -or
    -not $trace.OriginalTribouletPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-TRIBOULET-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -lt 1)) {
    throw 'Triboulet lacks its unchanged owner, Rare deck seed, or cardless Monster-spawn witness.'
}
if ($OriginalStartTurnPropagationRelic -and ($trace.Schema -ne 231 -or $trace.OriginalStartTurnPropagationSchema -ne 231 -or
    $trace.ModifierScenario -ne 'original-start-turn-propagation' -or -not $trace.OriginalStartTurnPropagationPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalStartTurnPropagationCaptureFailures -ne 0 -or $trace.OriginalStartTurnPropagationMismatches -ne 0 -or
    $trace.OriginalStartTurnPropagationUnsupported -ne 0 -or $trace.OriginalStartTurnPropagationPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2 -or
    @($trace.RelicStartTurnPropagationApplications).Count -lt 2 -or
    @($trace.RelicStartTurnPropagationTests | Where-Object EmptyControl).Count -ne 1)) {
    throw 'Original StartOfTurnPropagate acceptance failed.'
}
if ($OriginalSuppressExhaustRelic -and ($trace.Schema -ne 234 -or $trace.OriginalSuppressExhaustSchema -ne 234 -or
    $trace.ModifierScenario -ne 'original-suppress-exhaust' -or -not $trace.OriginalSuppressExhaustPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalSuppressExhaustCaptureFailures -ne 0 -or $trace.OriginalSuppressExhaustMismatches -ne 0 -or
    $trace.OriginalSuppressExhaustUnsupported -ne 0 -or $trace.OriginalSuppressExhaustPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 8 -or @($trace.RelicSuppressExhaustRecords).Count -lt 16)) {
    throw 'Original ChanceToNotConsume acceptance failed.'
}
if ($OriginalWolosBloodletterRelic -and ($trace.Schema -ne 236 -or -not $trace.OriginalWolosBloodletterPrepared -or
    $trace.ModifierScenario -ne 'original-wolos-bloodletter' -or -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalWolosBloodletterCaptureFailures -ne 0 -or $trace.OriginalWolosBloodletterMismatches -ne 0 -or
    $trace.OriginalWolosBloodletterUnsupported -ne 0 -or $trace.OriginalWolosBloodletterPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 5 -or @($trace.RelicWolosBloodletterRecords).Count -lt 12)) {
    throw 'Original Wolos Bloodletter acceptance failed.'
}
if ($OriginalCorruptedAngelsRelic -and ($trace.Schema -ne 246 -or -not $trace.OriginalCorruptedAngelsPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalCorruptedAngelsCaptureFailures -ne 0 -or $trace.OriginalCorruptedAngelsMismatches -ne 0 -or
    $trace.OriginalCorruptedAngelsUnsupported -ne 0 -or $trace.OriginalCorruptedAngelsPending -ne 0 -or
    @($trace.RelicCorruptedAngelsRecords | Where-Object { $_.Role -eq 'natural' -and $_.Returned }).Count -eq 0 -or
    @($trace.RelicCorruptedAngelsRecords | Where-Object { $_.Depth -eq 0 -and $_.Role -ne 'natural' }).Count -ne 8 -or
    @($trace.RelicCorruptedAngelsRecords | Where-Object { $_.Role -eq 'recursive-candidate' -and $_.Depth -eq 3 }).Count -ne 1)) {
    throw 'Original Corrupted Angels positive/control evidence is incomplete or divergent.'
}
if ($OriginalPerkeoRelic -and ($trace.Schema -ne 245 -or -not $trace.OriginalPerkeoPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalPerkeoCaptureFailures -ne 0 -or $trace.OriginalPerkeoMismatches -ne 0 -or
    $trace.OriginalPerkeoUnsupported -ne 0 -or $trace.OriginalPerkeoPending -ne 0 -or
    @($trace.RelicStartBattleGeneration).Count -ne 1 -or @($trace.RelicPerkeoConversions).Count -ne 1 -or
    @($trace.RelicPerkeoUnplayed | Where-Object { @($_.UnupgradedControls).Count -gt 0 }).Count -eq 0)) {
    throw 'Original Perkeo native generation/conversion/control evidence is incomplete or divergent.'
}
if ($OriginalOpeningDeckSpawnRelic -and ($trace.Schema -ne 244 -or -not $trace.OriginalOpeningDeckSpawnPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalOpeningDeckSpawnCaptureFailures -ne 0 -or $trace.OriginalOpeningDeckSpawnMismatches -ne 0 -or
    $trace.OriginalOpeningDeckSpawnUnsupported -ne 0 -or $trace.OriginalOpeningDeckSpawnPending -ne 0 -or
    @($trace.RelicOpeningDeckSpawnApplications).Count -ne 3 -or
    @($trace.RelicOpeningDeckSpawnTests | Where-Object { $_.Isolated -and -not $_.Returned -and $_.CacheBefore.Count -gt 0 }).Count -eq 0)) {
    throw 'Original opening deck-spawn native evidence is incomplete or divergent.'
}
if ($MagneticRelay -and (-not $trace.MagneticRelayPrepared -or -not $trace.NativeWon -or @($trace.MagneticRelayRecords).Count -ne 4 -or
    @($trace.MagneticRelayRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Magnetic Relay native evidence is incomplete.'
}
if ($TranscendImp -and (-not $trace.TranscendImpPrepared -or -not $trace.NativeWon -or @($trace.TranscendImpRecords).Count -ne 3 -or
    @($trace.TranscendImpRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Transcend Imp native evidence is incomplete.'
}
if ($FusedWeaponry -and (-not $trace.EquipmentMergePrepared -or -not $trace.NativeWon -or @($trace.EquipmentMergeRecords).Count -ne 3 -or
    @($trace.EquipmentMergeRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Fused Weaponry native evidence is incomplete.'
}
if ($SmallBox -and (-not $trace.SmallBoxPrepared -or -not $trace.NativeWon -or @($trace.SmallBoxRecords).Count -ne 3 -or
    @($trace.SmallBoxRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Small Box native evidence is incomplete.'
}
if ($EclipseChamber -and (-not $trace.EclipseChamberPrepared -or -not $trace.NativeWon -or
    $(if ($RollingAdvice) { @($trace.EclipseChamberRecords | Where-Object Characters).Count -lt 1 }
      else { @($trace.EclipseChamberRecords | Where-Object Characters).Count -ne 2 }) -or
    @($trace.EclipseChamberRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Eclipse Chamber native evidence is incomplete.'
}
if ($EclipseChamberMovement -and (-not $trace.EclipseChamberMovement -or
    (-not $RollingAdvice -and @($trace.Actions).Count -ne $(if ($EclipseChamberEnemyMovement) { 13 } else { 12 })) -or
    [bool]$trace.EclipseChamberEnemyMovement -ne [bool]$EclipseChamberEnemyMovement -or
    [bool]$trace.EclipseChamberEnemyCallbacks -ne [bool]$EclipseChamberEnemyCallbacks -or
    [bool]$trace.EclipseChamberEnemyConduit -ne [bool]$EclipseChamberEnemyConduit -or
    [bool]$trace.EclipseChamberMixedConduit -ne [bool]$EclipseChamberMixedConduit)) {
    throw 'Eclipse movement capture lacks the complete paid original movement chain.'
}
if ($OriginalMonsterHandCosts -and (-not $trace.OriginalMonsterHandCostsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6 -or
    @($trace.OriginalMonsterHandCostsSamples).Count -ne 7 -or @($trace.OriginalMonsterHandCostsDeploymentQueries).Count -ne 8 -or -not $trace.OriginalMonsterHandCostsQueriesUnchanged)) {
    throw 'Original Monster hand cost fixture lacks cost/filter controls or restored deployment eligibility queries.'
}
if ($OriginalMoonCardUpgrades -and (-not $trace.OriginalMoonCardUpgradesPrepared -or -not $trace.NativeWon -or
    [bool]$trace.OriginalMoonCardUpgradesRoomMask -ne [bool]$OriginalMoonCardUpgradesRoomMask -or
    [bool]$trace.OriginalMoonCardUpgradesLunacy -ne [bool]$OriginalMoonCardUpgradesLunacy -or
    [bool]$trace.OriginalMoonCardUpgradesCostRelic -ne [bool]$OriginalMoonCardUpgradesCostRelic -or
    [bool]$trace.OriginalMoonCardUpgradesAttackRelic -ne [bool]$OriginalMoonCardUpgradesAttackRelic -or
    (-not $RollingAdvice -and @($trace.Actions).Count -ne $(if ($OriginalMoonCardUpgradesCostRelic) { 22 } elseif ($OriginalMoonCardUpgradesLunacy) { 20 } elseif ($OriginalMoonCardUpgradesRoomMask) { 16 } else { 15 })) -or
    @($trace.OriginalMoonCardUpgradesHands).Count -lt 2 -or @($trace.OriginalMoonCardUpgradesHands | Where-Object { -not $_.Completed }).Count -ne 0)) {
    throw 'Original moon card upgrades require ten original casts, complete hand callbacks and a native win.'
}
if ($OriginalDiscardDraw -and (-not $trace.OriginalDiscardDrawPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 11 -or
    @($trace.OriginalDiscardDrawCounts).Count -ne 3 -or @($trace.OriginalDiscardDrawDiscards | Where-Object WasPlayed).Count -ne 3)) {
    throw 'Original discard draw coverage incomplete.'
}
if ($OriginalSpellCompositions -and (-not $trace.OriginalSpellCompositionsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 10)) {
    throw 'Original spell compositions coverage incomplete.'
}
if ($OriginalPositiveStatus -and (-not $trace.OriginalPositiveStatusPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 10 -or
    @($trace.OriginalPositiveStatusQueries).Count -ne 4 -or -not $trace.OriginalPositiveStatusQueriesUnchanged -or
    @($trace.OriginalPositiveStatusEffects).Count -lt 5 -or @($trace.OriginalPositiveStatusEffects | Where-Object { -not $_.Completed }).Count -ne 0)) {
    throw 'Original positive status coverage incomplete.'
}
if ($OriginalScaledMoonPhase -and (-not $trace.OriginalScaledMoonPhasePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6 -or
    @($trace.OriginalScaledMoonPhaseCounts).Count -ne 3 -or @($trace.OriginalScaledMoonPhaseQueries).Count -ne 12 -or
    @($trace.OriginalScaledMoonPhaseStatusSamples).Count -ne 3 -or @($trace.OriginalScaledMoonPhasePhases).Count -ne 3 -or
    @($trace.OriginalScaledMoonPhaseQueries | Where-Object { -not $_.StateUnchanged }).Count -ne 0 -or
    @($trace.OriginalScaledMoonPhasePhases | Where-Object { -not $_.Completed }).Count -ne 0)) {
    throw 'Original Lunacy witness lacks positive/zero/large X advances, matched/unmatched status controls or complete phase boundaries.'
}
if ($OriginalDrawUpgradeScaling -and (-not $trace.OriginalDrawUpgradeScalingPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6 -or
    @($trace.OriginalDrawUpgradeScalingSamples).Count -ne 3 -or @($trace.OriginalDrawUpgradeScalingQueries).Count -ne 3 -or
    @($trace.OriginalDrawUpgradeScalingQueries | Where-Object { -not $_.StateUnchanged -or -not $_.Returned }).Count -ne 0)) {
    throw 'Original Prism Retrieval witness lacks positive/zero/large X, actual upgrade callbacks or unchanged fresh-upgrade queries.'
}
if ($OriginalConsumedUpgrade -and (-not $trace.OriginalConsumedUpgradePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6 -or
    @($trace.OriginalConsumedUpgradeSamples).Count -ne 2 -or @($trace.OriginalConsumedUpgradeQueries).Count -ne 5 -or
    @($trace.OriginalConsumedUpgradeQueries | Where-Object { -not $_.StateUnchanged -or -not $_.Returned }).Count -ne 0)) {
    throw 'Original Sacrificial Ressurection witness lacks three/zero consumed hands, fresh per-recipient upgrades or restored native controls.'
}
if ($OriginalStatusCompositions -and (-not $trace.OriginalStatusCompositionsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 10)) {
    throw 'Original status-composition witness lacks all seven original spell plays and a completed native battle.'
}
if ($OriginalGraftedEquipment -and (-not $trace.OriginalGraftedEquipmentPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 14 -or
    @($trace.OriginalGraftedEquipmentResets).Count -ne 10 -or -not $trace.OriginalGraftedEquipmentControlsUnchanged)) {
    throw 'Original grafted equipment witness lacks all five definitions, replacement/removal/death or unchanged detached reset controls.'
}
if ($OriginalSelfAttack -and (-not $trace.OriginalSelfAttackPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 11)) {
    throw 'Original Animus of Speed witness lacks four targeted ability activations or a completed native battle.'
}
if ($OriginalHealingCleanse -and (-not $trace.OriginalHealingCleansePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 19)) {
    throw 'Original healing/cleanse witness lacks its original cards and positive state controls or a completed native battle.'
}
if ($OriginalOnKill -and (-not $trace.OriginalOnKillPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 13)) {
    throw 'Original Bloody Moon witness lacks seven original plays and lethal/nonlethal/shield/Undying controls or a completed native battle.'
}
if ($OriginalOnKillGeneration -and (-not $trace.OriginalOnKillGenerationPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 14)) {
    throw 'Original Antumbra Assault witness lacks seven original plays, native pool RNG/full-hand controls or a completed native battle.'
}
if ($OriginalMorselEaten -and (-not $trace.OriginalMorselEatenPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 8 -or
    $trace.OriginalOpeningMorselsCaptureFailures -ne 0 -or $trace.OriginalOpeningMorselsMismatches -ne 0 -or
    $trace.OriginalOpeningMorselsUnsupported -ne 0 -or $trace.OriginalOpeningMorselsPending -ne 0 -or
    ($trace.NativeMorselFeeding | ForEach-Object { $_.ExpectedEaten } | Measure-Object -Sum).Sum -ne 5)) {
    throw 'Original Morsel witness lacks all five original summons and exact native feeding transitions.'
}
if ($OriginalUnitBuffs -and (-not $trace.OriginalUnitBuffsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 12)) {
    throw 'Original unit buff witness lacks eight original spells and positive upgrade/status controls.'
}
if ($OriginalKillCopy -and (-not $trace.OriginalKillCopyPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 12)) {
    throw 'Original Surgical Strike witness lacks original and copied plays with target-team and lethal controls.'
}
if ($OriginalFuel -and (-not $trace.OriginalFuelPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 13)) {
    throw 'Original fuel witness lacks unchanged Construct, forced empty/last fuel, feeding and refill controls.'
}
if ($OriginalCardDrawPotion -and ($trace.Schema -ne 247 -or $trace.ModifierScenario -ne 'original-card-draw-potion' -or
    -not $trace.OriginalCardDrawPotionPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 3)) {
    throw 'Original Card Draw Potion witness lacks its unchanged direct play and native next-turn draw.'
}
if ($OriginalAcceleratedIncubation -and ($trace.ModifierScenario -ne 'original-accelerated-incubation' -or
    -not $trace.NativeWon -or @($trace.Actions).Count -lt 3)) {
    throw 'Original Accelerated Incubation witness lacks two control unit plays and the direct Spell play.'
}
if ($OriginalEagerConscript -and ($trace.Schema -ne 249 -or $trace.ModifierScenario -ne 'original-eager-conscript' -or
    -not $trace.OriginalEagerConscriptPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -lt 3)) {
    throw 'Original Eager Conscript witness lacks its control unit and both direct Spell plays.'
}
if ($OriginalSpawningPods -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-spawning-pods' -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 3)) {
    throw 'Original Spawning Pods witness lacks its controlled Horde target and direct card play.'
}
if ($OriginalMindsOfTheirOwn -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-minds-of-their-own' -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 3)) {
    throw 'Original Minds of Their Own witness lacks its equipped unit and direct card play.'
}
if ($OriginalMetallurgy -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-metallurgy' -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 2)) {
    throw 'Original Metallurgy witness lacks its friendly armor target, native chooser and direct card play.'
}
if ($OriginalFertileGround -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-fertile-ground' -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Original Fertile Ground witness lacks its Horde setup, exhausted Spell scaling input and direct card play.'
}
if ($OriginalCoverOfNight -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-cover-of-night' -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Original Cover of Night witness lacks its two floor controls, phase setup and direct card play.'
}
if ($OriginalWaxenSpike -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-waxen-spike' -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 3)) {
    throw 'Original Waxen Spike witness lacks its target/control units and direct X-cost play.'
}
if ($OriginalImpPressive -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-imp-pressive' -or
    -not $trace.OriginalImpPressivePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 3)) {
    throw 'Original ImpPressive witness lacks its native Imp, protected floor and direct card play.'
}
if ($OriginalTomeOfEvergrowth -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-tome-of-evergrowth' -or
    -not $trace.OriginalTomeOfEvergrowthPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 3)) {
    throw 'Original Tome of Evergrowth witness lacks its two floor controls and direct ability-setting play.'
}
if ($OriginalTomeOfTheLostPath -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-tome-of-the-lost-path' -or
    -not $trace.OriginalTomeOfTheLostPathPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 3)) {
    throw 'Original Tome of the Lost Path witness lacks its two floor controls and direct ability-setting play.'
}
if ($OriginalMakeItRain -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-make-it-rain' -or
    -not $trace.OriginalMakeItRainPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 3)) {
    throw 'Original Make It Rain witness lacks its two floor controls and direct damage/reward play.'
}
if ($OriginalDeathroar -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-deathroar' -or
    -not $trace.OriginalDeathroarPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Original Deathroar witness lacks its three controlled Stewards and direct damage/status-transfer play.'
}
if ($OriginalGreenRecruit -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-green-recruit' -or
    -not $trace.OriginalGreenRecruitPrepared -or -not $trace.NativeWon)) {
    throw 'Original Green Recruit witness did not complete its native Monster deployment.'
}
if ($OriginalPrimordium -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-primordium' -or
    -not $trace.OriginalPrimordiumPrepared -or -not $trace.NativeWon)) {
    throw 'Original Primordium witness did not complete its native Champion deployment.'
}
if ($OriginalCheeryDeathcap -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-cheery-deathcap' -or
    -not $trace.OriginalCheeryDeathcapPrepared -or -not $trace.NativeWon)) {
    throw 'Original Cheery Deathcap witness did not complete its native Monster deployment.'
}
if ($OriginalEnthusiasticFan -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-enthusiastic-fan' -or
    -not $trace.OriginalEnthusiasticFanPrepared -or -not $trace.NativeWon)) {
    throw 'Original Enthusiastic Fan witness did not complete its native Monster deployment.'
}
if ($OriginalSecretIngredient -and ($trace.Schema -ne 103 -or $trace.ModifierScenario -ne 'original-secret-ingredient-mix' -or
    -not $trace.OriginalSecretIngredientPrepared -or -not $trace.NativeWon)) {
    throw 'Original Secret Ingredient witness did not prepare and complete its native Mix play.'
}
if ($OriginalCopyMorselTwice -and ($trace.Schema -ne 250 -or $trace.ModifierScenario -ne 'original-copy-morsel-twice' -or
    -not $trace.OriginalCopyMorselTwicePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -lt 3)) {
    throw 'Original Copy Morsel Twice witness lacks its control units and direct Spell play.'
}
if ($OriginalLazarusLeagueSpike -and ($trace.Schema -ne 251 -or $trace.ModifierScenario -ne 'original-lazarus-league-spike' -or
    -not $trace.OriginalLazarusLeagueSpikePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -lt 3)) {
    throw 'Original Lazarus League Spike witness lacks its control units and direct Spell play.'
}
if ($OriginalTomeOfContracts -and ($trace.Schema -ne 248 -or $trace.ModifierScenario -ne 'original-tome-of-contracts' -or
    -not $trace.OriginalTomeOfContractsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -lt 3)) {
    throw 'Original Tome of Contracts witness lacks two control unit plays and the direct Spell play.'
}
if ($OriginalSupportUnits -and (-not $trace.OriginalSupportUnitsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 36)) {
    throw 'Original support unit witness lacks twenty unchanged units and positive native death-resource callback controls.'
}
if ($OriginalDivineDancer -and ($trace.ModifierScenario -ne 'original-divine-dancer' -or -not $trace.OriginalSupportUnitsPrepared -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Isolated Divine Dancer witness lacks three anchor controls and one direct original-unit play.'
}
if ($OriginalIsolatedUnitBatch -and ($trace.ModifierScenario -ne 'original-isolated-unit-batch' -or -not $trace.OriginalSupportUnitsPrepared -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Isolated Branded Warrior witness lacks three anchor controls and one direct unit play.'
}
if ($OriginalIsolatedColdstrikeSiren -and ($trace.ModifierScenario -ne 'original-isolated-coldstrike-siren' -or -not $trace.OriginalSupportUnitsPrepared -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Isolated Coldstrike Siren witness lacks three anchor controls and one direct unit play.'
}
$isolatedEnergyActionCount = if ([int]$trace.OriginalIsolatedEnergyControlId -gt 0) { 1 } else { 0 }
if ($OriginalIsolatedUnitId -and ($trace.ModifierScenario -ne ('original-isolated-unit:' + $OriginalIsolatedUnitId) -or -not $trace.OriginalSupportUnitsPrepared -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne (4 + $isolatedEnergyActionCount))) {
    throw 'Isolated original unit witness lacks three anchor controls and one direct unit play.'
}
if ($OriginalIsolatedUnitIds.Count -gt 0 -and ($trace.ModifierScenario -ne ('original-isolated-unit-batch:' + ($OriginalIsolatedUnitIds -join ',')) -or
    -not $trace.OriginalSupportUnitsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne (3 + $OriginalIsolatedUnitIds.Count + $isolatedEnergyActionCount))) {
    throw 'Isolated original unit batch witness lacks three anchor controls and every requested direct unit play.'
}
if ($OriginalAttackOverrides -and (-not $trace.OriginalAttackOverridesPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 29)) {
    throw 'Original attack override witness lacks unchanged equipment and positive priority/removal damage controls.'
}
if ($OriginalReactiveUnits -and (-not $trace.OriginalReactiveUnitsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 24)) {
    throw 'Original reactive unit witness lacks ten unchanged units and positive OnHit/Incant controls.'
}
if ($OriginalBaseChampions -and (-not $trace.OriginalBaseChampionsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 24)) {
    throw 'Original base champion witness lacks eighteen unchanged base summons and three capacity controls.'
}
if ($OriginalStatusUnits -and (-not $trace.OriginalStatusUnitsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 21)) {
    throw 'Original status unit witness lacks eleven unchanged births and positive combat controls.'
}
if ($OriginalEquipmentCards -and (-not $trace.OriginalEquipmentCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 26)) {
    throw 'Original equipment witness lacks seventeen unchanged cards, attack triggers, host death and positive battle callbacks.'
}
$stasisAbilityActionCount = if ($OriginalIsolatedEquipmentIds.Count -eq 1 -and
    $OriginalIsolatedEquipmentIds[0] -eq '764f9b75-daf1-4deb-a6a2-f09964fb5296') { 1 } else { 0 }
if ($OriginalIsolatedEquipmentIds.Count -gt 0 -and ($trace.ModifierScenario -ne ('original-isolated-equipment:' + ($OriginalIsolatedEquipmentIds -join ',')) -or
    -not $trace.OriginalEquipmentCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne (4 + $OriginalIsolatedEquipmentIds.Count + $stasisAbilityActionCount))) {
    throw 'Isolated original equipment batch lacks three hosts, its energy control and every requested original equipment play.'
}
if ($OriginalSupportSpells -and (-not $trace.OriginalSupportSpellsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 27)) {
    throw 'Original support spell witness lacks thirteen unchanged originals and positive resource/healing controls.'
}
$returnedCorruptRecastCount = if ($OriginalIsolatedSpellIds.Count -gt 0 -and
    $OriginalIsolatedSpellIds[0] -eq 'bae01a8c-25dd-4a08-b08a-5f17a5f3f52a' -and
    $OriginalIsolatedSpellIds -contains '6a3633b6-4ecc-43b1-bb56-548f56cf1d60') { 1 } else { 0 }
$isolatedRoomAbilityCount = @($trace.Actions | Where-Object { $null -ne $_.Action.ActivatorRoomIndex -or $_.Action.ActivatorUnitId -ne 0 }).Count
if ($OriginalUnitAbilityId -and ($trace.OriginalUnitAbilityId -ne $OriginalUnitAbilityId -or
    @($trace.Actions | Where-Object { $_.Action.ActivatorUnitId -gt 0 }).Count -ne 1)) {
    throw 'Original unit skill witness requires exactly one positive native activation.'
}
if ($OriginalIsolatedSpellIds.Count -gt 0 -and ($trace.ModifierScenario -ne ('original-isolated-spells:' + ($OriginalIsolatedSpellIds -join ',')) -or
    -not $trace.OriginalSupportSpellsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne (4 + $OriginalIsolatedSpellIds.Count + $returnedCorruptRecastCount + $isolatedRoomAbilityCount))) {
    throw 'Isolated spell batch lacks three hosts, energy and every requested original spell play.'
}
if ($OriginalRearrangeSpells -and (-not $trace.OriginalRearrangeSpellsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 19)) {
    throw 'Original rearrange spell witness lacks eleven unchanged originals and positive composition controls.'
}
if ($OriginalMovementSpells -and (-not $trace.OriginalMovementSpellsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 13)) {
    throw 'Original movement spell witness lacks nine unchanged originals and tracked floor/order controls.'
}
if ($OriginalScaledDamage -and (-not $trace.OriginalScaledDamagePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 22)) {
    throw 'Original scaled damage witness lacks eleven unchanged originals and positive scaling controls.'
}
if ($OriginalHostileStatuses -and (-not $trace.OriginalHostileStatusesPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 18)) {
    throw 'Original hostile status witness lacks eleven original spells and positive status/Corruption controls.'
}
if ($OriginalResourceSpells -and (-not $trace.OriginalResourceSpellsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 13)) {
    throw 'Original resource spell witness lacks five originals and resource scaling/spending/X controls.'
}
if ($OriginalBasicDamage -and (-not $trace.OriginalBasicDamagePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 11)) {
    throw 'Original basic damage witness lacks six unchanged spells and positive damage controls.'
}
if ($OriginalFriendlyStatuses -and (-not $trace.OriginalFriendlyStatusesPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 16)) {
    throw 'Original friendly status witness lacks twelve unchanged spells and positive target controls.'
}
if ($OriginalKillResource -and (-not $trace.OriginalKillResourcesPrepared -or $trace.OriginalKillResource -ne $OriginalKillResource -or
    -not $trace.NativeWon -or @($trace.Actions).Count -ne $(if ($OriginalKillResource -eq 'Greed') { 8 } else { 13 }))) {
    throw 'Original kill resource witness lacks original spells and lethal/nonlethal resource controls.'
}
if ($OriginalPileStatus -and (-not $trace.OriginalPileStatusPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 5 -or
    @($trace.OriginalPileStatusQueries).Count -ne 24 -or -not $trace.OriginalPileStatusQueriesUnchanged -or
    @($trace.OriginalPileStatusQueries | Where-Object { -not $_.StateUnchanged }).Count -ne 0 -or @($trace.OriginalPileStatusSamples).Count -ne 2)) {
    throw 'Original Dig Deep witness lacks restored visible-deck thresholds, status controls or new/existing Horde plays.'
}
if ($OriginalScaledBuff -and (-not $trace.OriginalScaledBuffPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6 -or
    @($trace.OriginalScaledBuffSamples).Count -ne 3 -or @($trace.OriginalScaledBuffQueries).Count -ne 6 -or
    @($trace.OriginalScaledBuffQueries | Where-Object { -not $_.StateUnchanged }).Count -ne 0)) {
    throw 'Original Pyre Plating witness lacks positive/zero/large X, attack buff callbacks or unchanged override queries.'
}
if ($OriginalPileRecursion -and (-not $trace.OriginalPileRecursionPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 4 -or
    @($trace.OriginalPileRecursionQueries).Count -ne 4 -or -not $trace.OriginalPileRecursionQueriesUnchanged -or
    @($trace.OriginalPileRecursionQueries | Where-Object { -not $_.StateUnchanged }).Count -ne 0 -or @($trace.OriginalPileRecursionSamples).Count -ne 2 -or
    @($trace.OriginalPileRecursionSamples | Where-Object { -not $_.Completed }).Count -ne 0)) {
    throw 'Original Unearth Treasure witness lacks restored thresholds or completed positive/below-threshold recursion.'
}
if ($OriginalPileDraw -and (-not $trace.OriginalPileDrawPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 3 -or
    @($trace.OriginalPileDrawQueries).Count -ne 5 -or -not $trace.OriginalPileDrawQueriesUnchanged -or
    @($trace.OriginalPileDrawQueries | Where-Object { -not $_.StateUnchanged }).Count -ne 0 -or @($trace.OriginalPileDrawAdjustments).Count -ne 1)) {
    throw 'Original Mulch witness lacks restored visible-deck thresholds or actual future-draw adjustment.'
}
if ($OriginalCardDrawPotion -and (@($trace.CardCycles | Where-Object { $_.Kind -eq 'Draw' -and $_.Before.DrawModifier -eq 3 -and $_.Actual.Hand.Count -eq 8 -and $_.Actual.DrawModifier -eq 0 }).Count -ne 1)) {
    throw 'Original Card Draw Potion witness lacks one native eight-card draw and modifier reset.'
}
if ($OriginalEffectApplications -and (-not $trace.OriginalEffectApplicationsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 8 -or
    @($trace.OriginalEffectApplicationsCounts).Count -ne 3 -or @($trace.OriginalEffectApplicationsSamples).Count -ne 4 -or
    @($trace.OriginalEffectApplicationsCounts | Where-Object { -not $_.StateUnchanged -or $_.ActualCount -ne $_.DirectCount }).Count -ne 0 -or
    @($trace.OriginalEffectApplicationsSamples | Where-Object { -not $_.Completed }).Count -ne 0)) {
    throw 'Original effect applications witness lacks zero/one/three X, restored queries or completed damage applications.'
}
if ($OriginalStatusRemovalScaling -and (-not $trace.OriginalStatusRemovalScalingPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6 -or
    @($trace.OriginalStatusRemovalScalingSamples).Count -ne 3 -or @($trace.OriginalStatusRemovalScalingQueries).Count -ne 12 -or
    @($trace.OriginalStatusRemovalScalingQueries | Where-Object { -not $_.StateUnchanged }).Count -ne 0)) {
    throw 'Original status-removal scaling witness is incomplete or native queries changed shared context.'
}
if ($OriginalScaledEnergy -and (-not $trace.OriginalScaledEnergyPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 5 -or
    @($trace.OriginalScaledEnergySamples | Where-Object WasPlayed).Count -ne 2 -or
    @($trace.OriginalScaledEnergySamples | Where-Object { -not $_.WasPlayed }).Count -lt 1 -or
    @($trace.OriginalScaledEnergySamples | Where-Object { -not $_.Completed }).Count -ne 0)) {
    throw 'Original scaled energy witness is incomplete.'
}
if ($OriginalSpecificCardDamage -and (-not $trace.OriginalSpecificCardDamagePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 5 -or
    @($trace.OriginalSpecificCardDamageQueries).Count -ne 12 -or -not $trace.OriginalSpecificCardDamageQueriesUnchanged -or
    @($trace.OriginalSpecificCardDamageSamples).Count -lt 2 -or @($trace.OriginalSpecificCardDamageQueries | Where-Object { -not $_.StateUnchanged }).Count -ne 0)) {
    throw 'Original specific-card damage witness is incomplete or native queries changed shared state.'
}
if ($OriginalStatusStackDamage -and (-not $trace.OriginalStatusStackDamagePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 7 -or
    @($trace.OriginalStatusStackDamageSamples).Count -ne 3 -or @($trace.OriginalStatusStackDamageSamples | Where-Object { -not $_.StateUnchanged }).Count -ne 0)) {
    throw 'Original status-stack damage witness is incomplete or native trait changed shared state.'
}
if ($OriginalCorruptRestricted -and (-not $trace.OriginalCorruptRestrictedPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 7 -or
    @($trace.OriginalCorruptRestrictedQueries).Count -ne 12 -or @($trace.OriginalCorruptRestrictedPrePlays).Count -ne 3 -or -not $trace.OriginalCorruptRestrictedQueriesUnchanged)) {
    throw 'Original Corrupt Restricted fixture lacks restored thresholds or positive pre-effect consumption.'
}
if ($OriginalOverflowCards -and (-not $trace.OriginalOverflowCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 8)) {
    throw 'Original overflow fixture lacks nonlethal and both positive Hero-team spill controls.'
}
if ($OriginalPermafrostCards -and (-not $trace.OriginalPermafrostCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6 -or
    @($trace.OriginalPermafrostCardsDraws | Where-Object Control).Count -ne 6 -or -not $trace.OriginalPermafrostCardsQueriesUnchanged)) {
    throw 'Original Permafrost fixture lacks automatic Freeze, retention, repeated callbacks or redraw plays.'
}
if ($HoldoverCalibration -and (-not $trace.HoldoverCalibrationPrepared -or -not $trace.HoldoverControlsUnchanged -or
    @($trace.HoldoverCallbacks | Where-Object Control).Count -ne 48 -or
    @($trace.HoldoverCallbacks | Where-Object { $null -eq $_.Before -or $null -eq $_.After }).Count -ne 0 -or
    @($trace.HoldoverCallbacks | Where-Object { -not $_.Control -and $_.Kind -eq 'PreDraw' -and
        @($_.Before.MaskDescriptor.BaseTraits | Where-Object { $_.RuntimeType -eq 'CardTraitRetain' -and $_.HoldoverPending }).Count -gt 0 }).Count -eq 0)) {
    throw 'Holdover calibration lacks complete restored controls or a positive natural pre-draw callback.'
}
if ($OriginalUnplayableCards -and (-not $trace.OriginalUnplayableCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 3 -or
    @($trace.OriginalUnplayableCardsQueries).Count -ne 12 -or -not $trace.OriginalUnplayableCardsQueriesUnchanged)) {
    throw 'Original Unplayable fixture lacks restored rejection queries, retention or Refine smelt.'
}
if ($OriginalLimitedRange -and (-not $trace.OriginalLimitedRangePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 3 -or
    @($trace.OriginalLimitedRangeQueries).Count -ne 12 -or -not $trace.OriginalLimitedRangeQueriesUnchanged)) {
    throw 'Original Limited Range fixture lacks top/Pyre plays or restored native range queries.'
}
if ($OriginalHandCosts -and (-not $trace.OriginalHandCostsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 15 -or @($trace.OriginalHandCostsSamples).Count -ne 16)) {
    throw 'Original hand cost fixture did not finish its stacking, type, numeric-order and removal controls.'
}
if ($OriginalUnpurgableCards -and (-not $trace.OriginalUnpurgableCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 7)) {
    throw 'Original Unpurgable fixture did not finish its battle purge/play/unplayed controls.'
}
if ($OriginalRoomStatusDamage -and (-not $trace.OriginalRoomStatusDamagePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 14)) {
    throw 'Original room-status damage fixture did not finish its zero/positive and team/floor controls.'
}
if ($OriginalCorruptionDamage -and (-not $trace.OriginalCorruptionDamagePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 10)) {
    throw 'Original corruption-damage fixture did not finish its accumulation/fill-to-max and selected-floor controls.'
}
if ($OriginalXStatusCards -and (-not $trace.OriginalXStatusCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 10)) {
    throw 'Original X-status cards scenario lacks its positive/zero X plays or completed win.'
}
if ($OriginalXBanishedSpike -and (-not $trace.OriginalXBanishedSpikePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Original Spike of the Banished scenario lacks its positive/zero X plays or completed win.'
}
if ($OriginalXLunaSpike -and (-not $trace.OriginalXLunaSpikePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Original Spike of the Luna Coven scenario lacks its positive/zero X plays or completed win.'
}
if ($OriginalXStygianSpike -and (-not $trace.OriginalXStygianSpikePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Original Spike of the Stygian scenario lacks its positive/zero X plays or completed win.'
}
if ($OriginalXWurmkinSpike -and (-not $trace.OriginalXWurmkinSpikePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 4)) {
    throw 'Original Wurmkin Spike scenario lacks its positive/zero X plays or completed win.'
}
if ($OriginalMineCollapse -and (-not $trace.OriginalMineCollapsePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 8)) {
    throw 'Original Mine Collapse scenario lacks its lethal/refund/zero-X plays or completed win.'
}
if ($OriginalImpOlate -and (-not $trace.OriginalImpOlatePrepared -or $trace.OriginalImpOlateImpWitnessId -le 0 -or
        -not $trace.NativeWon -or @($trace.Actions).Count -ne 2)) {
    throw 'Original Imp Olate scenario lacks its native Imp witness, protector/card play or completed win.'
}
if ($OriginalPyreshot -and (-not $trace.OriginalPyreshotPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 2)) {
    throw 'Original Pyreshot scenario lacks its protector/card play or completed win.'
}
if ($OriginalRetch -and (-not $trace.OriginalRetchPrepared -or $trace.OriginalRetchEatenWitnessId -le 0 -or
        -not $trace.NativeWon -or @($trace.Actions).Count -ne 2)) {
    throw 'Original Retch scenario lacks its Eaten Monster witness, protector/card play or completed win.'
}
if ($OriginalXDamageCards -and (-not $trace.OriginalXDamageCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 9)) {
    throw 'Original X-damage cards scenario lacks its original zero/positive plays or completed win.'
}
if ($OriginalStrongMagicCards -and (-not $trace.OriginalStrongMagicCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 8)) {
    throw 'Original strong-magic cards scenario lacks its original plays or completed win.'
}
if ($OriginalConsumeCards -and (-not $trace.OriginalConsumeCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 10)) {
    throw 'Original Consume cards scenario lacks its original plays or completed win.'
}
if ($InfernoRoom -and (-not $trace.InfernoRoomPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 5 -or @($trace.RoomPreCombatRecords).Count -lt 3)) {
    throw 'Inferno Room requires original pre-combat damage pulses, replacement/restoration and five actual plays in a native win.'
}
if ($OriginalPiercingCards -and (-not $trace.OriginalPiercingCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6)) {
    throw 'Original piercing cards require the unchanged three spells, ordinary shield control and six actual plays in a native win.'
}
if ($MushyRoom -and (-not $trace.MushyRoomPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 5 -or @($trace.RoomPostCombatRecords).Count -lt 2)) {
    throw 'Mushy Room requires original enemy status pulses, replacement/restoration and five actual plays in a native win.'
}
if ($GrowRoom -and (-not $trace.GrowRoomPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 5 -or @($trace.RoomPostCombatRecords).Count -lt 2)) {
    throw 'Grow Room requires original room pulses, replacement/restoration, floor controls and five actual plays in a native win.'
}
if ($Planetarium -and (-not $trace.PlanetariumPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 10)) {
    throw 'Planetarium requires original rooms, later birth, floor/damage controls, replacement/restoration and ten actual plays in a native win.'
}
if ($Memorial -and (-not $trace.MemorialPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 8)) {
    throw 'Memorial requires native Endless death/redraw/rebirth, replacement, ordinary second death and eight actual plays in a win.'
}
if ($ZoneOfSilence -and (-not $trace.ZoneOfSilencePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 5)) {
    throw 'Zone of Silence requires enemy birth, original room replacement/reactivation and five actual actions in a native win.'
}
if ($StewardsQuarters -and (-not $trace.StewardsQuartersPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 7)) {
    throw 'Stewards Quarters requires original room, replacement/restoration and subtype/floor/birth controls in a native win.'
}
if ($EngineUpgrade -and (-not $trace.EngineUpgradePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6)) {
    throw 'Engine Upgrade requires both original rooms and both successive replacements in a native win.'
}
if ($Goldmance -and (-not $trace.GoldmancePrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 7)) {
    throw 'Goldmance requires all four units, two original ability activations and Hoard adjustment in a native win.'
}
if ($OriginalDeckSpell -and (-not $trace.OriginalDeckSpellPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6)) {
    throw 'Original Great Decay requires all six unit/spell plays and a native win.'
}
if ($OriginalDeepOffering -and (-not $trace.OriginalDeepOfferingPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalDeepOfferingCardIds).Count -ne 3)) {
    throw 'Original Deep Offering requires one matched play, a controlled discard/draw witness and a native win.'
}
if ($OriginalMolded -and (-not $trace.OriginalMoldedPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalMoldedCardIds).Count -ne 4)) {
    throw 'Original Molded requires one matched play, controlled Exhaust targets and a native win.'
}
if ($OriginalBrokenMemories -and (-not $trace.OriginalBrokenMemoriesPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalBrokenMemoriesCardIds).Count -ne 4)) {
    throw 'Original Broken Memories requires one matched play, controlled Exhaust targets and a native win.'
}
if ($OriginalWickedBlaze -and (-not $trace.OriginalWickedBlazePrepared -or -not $trace.NativeWon -or
    @($trace.OriginalWickedBlazeCardIds).Count -ne 4)) {
    throw 'Original Wicked Blaze requires one matched play, controlled Exhaust targets and a native win.'
}
if ($OriginalOfferingToken -and ($trace.Schema -ne 255 -or -not $trace.OriginalOfferingTokenPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalOfferingTokenCardIds).Count -ne 4)) {
    throw 'Original Offering Token requires one matched play, controlled draw/discard cards and a native win.'
}
if ($OriginalMoonRitual -and ($trace.Schema -ne 256 -or -not $trace.OriginalMoonRitualPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalMoonRitualCardIds).Count -ne 2)) {
    throw 'Original Moon Ritual requires one matched play, a controlled Monster target and a native win.'
}
if ($OriginalSelfPurgeEnergyGain -and ($trace.Schema -ne 257 -or -not $trace.OriginalSelfPurgeEnergyGainPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalSelfPurgeEnergyGainCardIds).Count -ne 3)) {
    throw 'Original Self-Purge Energy Gain requires one matched play, a controlled draw card and a native win.'
}
if ($OriginalSmeltedTreasure -and ($trace.Schema -ne 258 -or -not $trace.OriginalSmeltedTreasurePrepared -or -not $trace.NativeWon -or
    @($trace.OriginalSmeltedTreasureCardIds).Count -ne 2)) {
    throw 'Original Smelted Treasure requires one matched play, a controlled friendly target and a native win.'
}
if ($OriginalPyreGro -and ($trace.Schema -ne 259 -or -not $trace.OriginalPyreGroPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalPyreGroCardIds).Count -ne 2)) {
    throw 'Original PyreGro requires one matched play, scheduled draw/energy effects and a native win.'
}
if ($OriginalDeadlyVariant -and ($trace.Schema -ne 260 -or -not $trace.OriginalDeadlyVariantPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalDeadlyVariantCardIds).Count -ne 2)) {
    throw 'Original Deadly Variant requires one matched play, a controlled Debuff target and a native win.'
}
if ($OriginalDrain -and ($trace.Schema -ne 261 -or -not $trace.OriginalDrainPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalDrainCardIds).Count -ne 3)) {
    throw 'Original Drain requires one matched play, a controlled Debuff target and eligible discard card.'
}
if ($OriginalStarterFrostbiteDiscard -and ($trace.Schema -ne 262 -or -not $trace.OriginalStarterFrostbiteDiscardPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalStarterFrostbiteDiscardCardIds).Count -ne 3)) {
    throw 'Original Starter Frostbite Discard requires one matched play, an enemy-front Poison target and eligible discard card.'
}
if ($OriginalFeedingFrenzy -and ($trace.Schema -ne 263 -or -not $trace.OriginalFeedingFrenzyPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalFeedingFrenzyCardIds).Count -ne 3)) {
    throw 'Original Feeding Frenzy requires one matched play, a single enemy-room Dazed target and one discard-hand card.'
}
if ($OriginalTitansGratitude -and ($trace.Schema -ne 264 -or -not $trace.OriginalTitansGratitudePrepared -or -not $trace.NativeWon -or
    @($trace.OriginalTitansGratitudeCardIds).Count -ne 3)) {
    throw 'Original Titan''s Gratitude requires one matched play, a single enemy-front 60-damage target and one discard candidate.'
}
if ($OriginalPerfectInsanity -and ($trace.Schema -ne 265 -or -not $trace.OriginalPerfectInsanityPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalPerfectInsanityCardIds).Count -ne 2)) {
    throw 'Original Perfect Insanity requires one matched play, six selected-room corruption and a durable upgrade target.'
}
if ($OriginalStarterReform -and ($trace.Schema -ne 266 -or -not $trace.OriginalStarterReformPrepared -or -not $trace.NativeWon -or
    @($trace.OriginalStarterReformCardIds).Count -ne 3)) {
    throw 'Original Starter Reform requires one matched play and a sole controlled Exhausted Monster candidate.'
}
if ($OriginalRapidRaise -and ($trace.Schema -ne 267 -or -not $trace.OriginalRapidRaisePrepared -or -not $trace.NativeWon -or
    @($trace.OriginalRapidRaiseCardIds).Count -ne 4)) {
    throw 'Original Rapid Raise requires one matched play and exactly two controlled Exhausted Monster candidates.'
}
if ($OriginalStatusCards -and (-not $trace.OriginalStatusCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 6)) {
    throw 'Original status cards require all six unit/spell plays and a native win.'
}
if ($OriginalForgeCards -and (-not $trace.OriginalForgeCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 8)) {
    throw 'Original forge cards require all eight unit/spell plays and a native win.'
}
if ($OriginalRoomCards -and (-not $trace.OriginalRoomCardsPrepared -or -not $trace.NativeWon -or @($trace.Actions).Count -ne 10)) {
    throw 'Original room-card native evidence is incomplete.'
}
if ($Mixalot -and (-not $trace.MixalotPrepared -or -not $trace.NativeWon -or @($trace.MixalotRecords).Count -ne 4 -or
    @($trace.MixalotRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Mixalot native evidence is incomplete.'
}
if ($AdaptiveMutation -and (-not $trace.AdaptiveMutationPrepared -or -not $trace.NativeWon -or @($trace.AdaptiveMutationRecords).Count -ne 3 -or
    @($trace.AdaptiveMutationRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Adaptive Mutation native evidence is incomplete.'
}
if ($Preserve -and (-not $trace.PreservePrepared -or -not $trace.NativeWon -or @($trace.PreserveRecords).Count -ne 3 -or
    @($trace.PreserveRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Preserve native evidence is incomplete.'
}
if (($CardDuplicate -or $StardustInvocation) -and (-not $trace.CardDuplicatePrepared -or -not $trace.NativeWon -or @($trace.CardDuplicateRecords).Count -ne 2 -or
    @($trace.CardDuplicateRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Card duplication native evidence is incomplete.'
}
if ($ArmamentDrive -and (-not $trace.ArmamentDrivePrepared -or -not $trace.NativeWon -or
    @($trace.CardGenerations).Count -ne 4 -or @($trace.CardGenerations | Where-Object Difference -NE $null).Count -ne 0)) {
    throw 'Armament Drive native evidence is incomplete or mismatched.'
}
if ($RunCardGeneration -and (-not $trace.RunCardGenerationPrepared -or -not $trace.NativeWon -or
    @($trace.CardGenerations).Count -lt 8 -or @($trace.CardGenerations | Where-Object Difference -NE $null).Count -ne 0)) {
    throw 'Run-card generation native evidence is incomplete or mismatched.'
}
if ($RestorationDetonation -and (-not $trace.RestorationDetonationPrepared -or -not $trace.NativeWon -or
    @($trace.RestorationDetonationRecords).Count -ne 4 -or
    @($trace.RestorationDetonationRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Restoration Detonation requires four completed original casts.'
}
if ($SoldierOfFortune -and (-not $trace.SoldierOfFortunePrepared -or -not $trace.NativeWon -or
    -not $trace.SoldierOfFortuneEmptyTargetTest -or @($trace.SoldierOfFortuneGoldTests).Count -ne 4 -or
    @($trace.SoldierOfFortuneGoldTests | Where-Object { $_.Returned -ne ($_.Gold -ge 30) }).Count -ne 0 -or
    @($trace.SoldierOfFortuneNullRecords).Count -ne 3 -or
    @($trace.SoldierOfFortuneNullRecords | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -ne 0)) {
    throw 'Soldier of Fortune requires paid draws, insufficient-gold tests and completed empty-target no-ops.'
}
if ($ForcedFeeding -and (-not $trace.ForcedFeedingDeploymentRejected -or
    $trace.ForcedFeedingCaptureFailures -ne 0 -or $trace.ForcedFeedingMismatches -ne 0 -or
    $trace.ForcedFeedingUnsupported -ne 0 -or $trace.ForcedFeedingPending -ne 0 -or
    @($trace.ForcedFeedingRecords | Where-Object { $_.Meals -gt 0 }).Count -lt 2 -or
    @($trace.ForcedFeedingRecords | Where-Object { $_.Meals -eq 0 }).Count -lt 1)) {
    throw 'Forced feeding native evidence is incomplete or divergent.'
}
if ($ShroudSpikeFeeding -and (-not $trace.ShroudSpikeFeeding -or
    @($trace.ForcedFeedingRecords | Where-Object { $_.Meals -gt 0 -and $_.TriggerCount -eq 6 }).Count -eq 0 -or
    @($trace.ForcedFeedingRecords | Where-Object { $_.Meals -gt 0 -and $_.TriggerCount -eq 0 }).Count -eq 0)) {
    throw 'Shroud Spike needs both a paid six-trigger meal and a zero-cost consumed meal.'
}
if ($OriginalOpeningMorselsRelic -and ($trace.Schema -ne 243 -or -not $trace.OriginalOpeningMorselsPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalOpeningMorselsCaptureFailures -ne 0 -or $trace.OriginalOpeningMorselsMismatches -ne 0 -or
    $trace.OriginalOpeningMorselsUnsupported -ne 0 -or $trace.OriginalOpeningMorselsPending -ne 0 -or
    @($trace.RelicOpeningMorselApplications).Count -ne 3 -or
    @($trace.NativeMorselFeeding | Where-Object { $_.ExpectedEaten -gt 0 }).Count -eq 0)) {
    throw 'Original opening Morsels native evidence is incomplete or divergent.'
}
if ($OriginalDecayDeathRelic -and ($trace.Schema -ne 242 -or -not $trace.OriginalDecayDeathPrepared -or
    $trace.ModifierScenario -ne 'original-decay-death' -or -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalDecayDeathCaptureFailures -ne 0 -or $trace.OriginalDecayDeathMismatches -ne 0 -or
    $trace.OriginalDecayDeathUnsupported -ne 0 -or $trace.OriginalDecayDeathPending -ne 0 -or
    @($trace.RelicDecayDeathApplications).Count -lt 2 -or @($trace.Actions).Count -ne 6)) {
    throw 'Original Decay-death native evidence is incomplete or divergent.'
}
if ($OriginalBoneDogRelic -and ($trace.Schema -ne 241 -or -not $trace.OriginalBoneDogPrepared -or
    $trace.ModifierScenario -ne 'original-bone-dog' -or -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalBoneDogCaptureFailures -ne 0 -or $trace.OriginalBoneDogMismatches -ne 0 -or
    $trace.OriginalBoneDogUnsupported -ne 0 -or $trace.OriginalBoneDogPending -ne 0 -or
    @($trace.RelicBoneDogRecords).Count -lt 2 -or @($trace.Actions).Count -ne 1)) {
    throw 'Original Bone Dog native evidence is incomplete or divergent.'
}
if ($OriginalGrosMichelRelic -and ($trace.Schema -ne 240 -or -not $trace.OriginalGrosMichelPrepared -or
    $trace.ModifierScenario -ne 'original-gros-michel' -or -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalGrosMichelCaptureFailures -ne 0 -or $trace.OriginalGrosMichelMismatches -ne 0 -or
    $trace.OriginalGrosMichelUnsupported -ne 0 -or $trace.OriginalGrosMichelPending -ne 0 -or
    @($trace.RelicGrosMichelRecords).Count -lt 12 -or @($trace.Actions).Count -ne 3)) {
    throw 'Original Gros Michel native evidence is incomplete or divergent.'
}
if ($OriginalSpawnRetreatRelic -and ($trace.Schema -ne 239 -or -not $trace.OriginalSpawnRetreatPrepared -or
    $trace.ModifierScenario -ne 'original-spawn-retreat' -or -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalSpawnRetreatCaptureFailures -ne 0 -or $trace.OriginalSpawnRetreatMismatches -ne 0 -or
    $trace.OriginalSpawnRetreatUnsupported -ne 0 -or $trace.OriginalSpawnRetreatPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2 -or @($trace.RelicSpawnRetreatRecords).Count -lt 20)) {
    throw 'Original spawn retreat acceptance failed.'
}
if ($OriginalWhelpCooldownRelic -and ($trace.Schema -ne 238 -or -not $trace.OriginalWhelpCooldownPrepared -or
    $trace.ModifierScenario -ne 'original-whelp-spawn-cooldown' -or -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalWhelpCooldownCaptureFailures -ne 0 -or $trace.OriginalWhelpCooldownMismatches -ne 0 -or
    $trace.OriginalWhelpCooldownUnsupported -ne 0 -or $trace.OriginalWhelpCooldownPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 6 -or @($trace.RelicWhelpCooldownRecords).Count -lt 20)) {
    throw 'Original Whelp spawn cooldown acceptance failed.'
}
if ($OriginalCardTypeSequenceRelic -and ($trace.Schema -ne 237 -or -not $trace.OriginalCardTypeSequencePrepared -or
    $trace.ModifierScenario -ne 'original-card-type-sequence' -or -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalCardTypeSequenceCaptureFailures -ne 0 -or $trace.OriginalCardTypeSequenceMismatches -ne 0 -or
    $trace.OriginalCardTypeSequenceUnsupported -ne 0 -or $trace.OriginalCardTypeSequencePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 10 -or @($trace.RelicCardTypeSequenceRecords).Count -lt 20)) {
    throw 'Original three-card-type sequence acceptance failed.'
}
if ($OriginalExpensiveCardUpgradeRelic -and ($trace.Schema -ne 235 -or $trace.OriginalExpensiveCardUpgradeSchema -ne 235 -or
    $trace.ModifierScenario -ne 'original-expensive-card-upgrade' -or -not $trace.OriginalExpensiveCardUpgradePrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalExpensiveCardUpgradeCaptureFailures -ne 0 -or $trace.OriginalExpensiveCardUpgradeMismatches -ne 0 -or
    $trace.OriginalExpensiveCardUpgradeUnsupported -ne 0 -or $trace.OriginalExpensiveCardUpgradePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 4 -or @($trace.RelicExpensiveCardUpgradeRecords).Count -lt 15)) {
    throw 'Original expensive-card Permafrost acceptance failed.'
}
if ($OriginalRandomDrawCostRelic -and ($trace.Schema -ne 233 -or $trace.OriginalRandomDrawCostSchema -ne 233 -or
    $trace.ModifierScenario -ne 'original-random-draw-cost' -or -not $trace.OriginalRandomDrawCostPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalRandomDrawCostCaptureFailures -ne 0 -or $trace.OriginalRandomDrawCostMismatches -ne 0 -or
    $trace.OriginalRandomDrawCostUnsupported -ne 0 -or $trace.OriginalRandomDrawCostPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2 -or @($trace.RelicRandomDrawCostRecords).Count -lt 16)) {
    throw 'Original RandomCardCostOnDraw acceptance failed.'
}
if ($OriginalMooncycleFaeRelic -and ($trace.Schema -ne 232 -or $trace.OriginalMooncycleFaeSchema -ne 232 -or
    $trace.ModifierScenario -ne 'original-mooncycle-fae' -or -not $trace.OriginalMooncycleFaePrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalMooncycleFaeCaptureFailures -ne 0 -or $trace.OriginalMooncycleFaeMismatches -ne 0 -or
    $trace.OriginalMooncycleFaeUnsupported -ne 0 -or $trace.OriginalMooncycleFaePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 3 -or
    @($trace.RelicMooncycleFaeApplications).Count -lt 2 -or
    @($trace.RelicMooncycleFaeTests | Where-Object EmptyControl).Count -ne 1)) {
    throw 'Original MooncycleFaeAddMagicPower acceptance failed.'
}
if ($OriginalRandomHandUpgradeRelic -and ($trace.Schema -ne 230 -or $trace.OriginalRandomHandUpgradeSchema -ne 230 -or
    $trace.ModifierScenario -ne 'original-random-hand-upgrade' -or -not $trace.OriginalRandomHandUpgradePrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalRandomHandUpgradeCaptureFailures -ne 0 -or $trace.OriginalRandomHandUpgradeMismatches -ne 0 -or
    $trace.OriginalRandomHandUpgradeUnsupported -ne 0 -or $trace.OriginalRandomHandUpgradePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 6 -or
    @($trace.RelicRandomHandUpgradeRecords | Where-Object Phase -eq 'ApplyPlay').Count -ne 2 -or
    @($trace.RelicRandomHandUpgradeRecords | Where-Object Phase -eq 'ApplyDiscard').Count -ne 2)) {
    throw 'Original random hand-upgrade acceptance failed.'
}
if ($OriginalRandomFreezeRelic -and ($trace.Schema -ne 229 -or $trace.OriginalRandomFreezeSchema -ne 229 -or
    $trace.ModifierScenario -ne 'original-random-freeze' -or -not $trace.OriginalRandomFreezePrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalRandomFreezeCaptureFailures -ne 0 -or $trace.OriginalRandomFreezeMismatches -ne 0 -or
    $trace.OriginalRandomFreezeUnsupported -ne 0 -or $trace.OriginalRandomFreezePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2 -or
    @($trace.RelicRandomFreezeApplications).Count -ne 2 -or
    @($trace.RelicRandomFreezeTests | Where-Object { -not $_.Result }).Count -lt 1)) {
    throw 'Original random-freeze acceptance failed.'
}
if ($OriginalFreezeReduceCostRelic -and ($trace.Schema -ne 228 -or $trace.OriginalFreezeCostSchema -ne 228 -or
    $trace.ModifierScenario -ne 'original-freeze-reduce-cost' -or -not $trace.OriginalFreezeCostPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalFreezeCostCaptureFailures -ne 0 -or $trace.OriginalFreezeCostMismatches -ne 0 -or
    $trace.OriginalFreezeCostUnsupported -ne 0 -or $trace.OriginalFreezeCostPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 3 -or
    @($trace.RelicFreezeCostApplications | Where-Object Room -eq 0).Count -lt 2)) {
    throw 'Original freeze-cost acceptance failed.'
}
if ($OriginalChanceToSilenceRelic -and ($trace.Schema -ne 227 -or $trace.OriginalSpawnSilenceSchema -ne 227 -or
    $trace.ModifierScenario -ne 'original-chance-to-silence' -or -not $trace.OriginalSpawnSilencePrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalSpawnSilenceCaptureFailures -ne 0 -or $trace.OriginalSpawnSilenceMismatches -ne 0 -or
    $trace.OriginalSpawnSilenceUnsupported -ne 0 -or $trace.OriginalSpawnSilencePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2 -or
    @($trace.RelicSpawnSilenceApplications).Count -lt 2)) {
    throw 'Original spawn-silence acceptance failed.'
}
if ($OriginalKillAttackRelic -and ($trace.Schema -ne 226 -or $trace.OriginalKillAttackSchema -ne 226 -or
    $trace.ModifierScenario -ne 'original-kill-attack' -or -not $trace.OriginalKillAttackPrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalKillAttackCaptureFailures -ne 0 -or $trace.OriginalKillAttackMismatches -ne 0 -or
    $trace.OriginalKillAttackUnsupported -ne 0 -or $trace.OriginalKillAttackPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2 -or
    @($trace.RelicKillAttackApplications).Count -lt 2)) {
    throw 'Original kill-attack acceptance failed.'
}
if ($OriginalHealDamageRelic -and ($trace.Schema -ne 225 -or $trace.OriginalHealDamageSchema -ne 225 -or
    $trace.ModifierScenario -ne 'original-heal-to-damage' -or -not $trace.OriginalHealDamagePrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalHealDamageCaptureFailures -ne 0 -or $trace.OriginalHealDamageMismatches -ne 0 -or
    $trace.OriginalHealDamageUnsupported -ne 0 -or $trace.OriginalHealDamagePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 9 -or
    @($trace.RelicHealDamageApplications).Count -ne 4)) {
    throw 'Original heal-to-damage acceptance failed.'
}
if ($OriginalBurnoutDamageRelic -and ($trace.Schema -ne 224 -or $trace.OriginalBurnoutDamageSchema -ne 224 -or
    $trace.ModifierScenario -ne 'original-burnout-death-damage' -or -not $trace.OriginalBurnoutDamagePrepared -or
    -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
    $trace.OriginalBurnoutDamageCaptureFailures -ne 0 -or $trace.OriginalBurnoutDamageMismatches -ne 0 -or
    $trace.OriginalBurnoutDamageUnsupported -ne 0 -or $trace.OriginalBurnoutDamagePending -ne 0 -or
    @($trace.RelicBurnoutDamageApplications | Where-Object EffectIndex -eq 0).Count -lt 1 -or
    @($trace.RelicBurnoutDamageApplications | Where-Object EffectIndex -eq 1).Count -lt 1)) {
    throw 'Original burnout/death damage acceptance failed.'
}
if ($OriginalMoreCapacityRelic -and ($trace.Schema -ne 223 -or $trace.OriginalMoreCapacitySchema -ne 223 -or
    $trace.ModifierScenario -ne 'original-more-capacity' -or -not $trace.OriginalMoreCapacityPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-MORE-CAPACITY-PREPARED' -Quiet) -or
    $trace.OriginalMoreCapacityCaptureFailures -ne 0 -or $trace.OriginalMoreCapacityMismatches -ne 0 -or
    $trace.OriginalMoreCapacityUnsupported -ne 0 -or $trace.OriginalMoreCapacityPending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 7 -or
    @($trace.OriginalMoreCapacityRecords | Where-Object Kind -eq 'Apply').Count -ne 1 -or
    @($trace.OriginalMoreCapacityRecords | Where-Object { $_.Kind -eq 'Test' -and -not $_.Result }).Count -lt 1)) {
    throw 'MoreCapacity lacks its unchanged owner, natural deployment callback or seven middle-floor capacity witnesses.'
}
if ($OriginalEatenDamageRelic -and ($trace.Schema -ne 222 -or $trace.OriginalEatenDamageSchema -ne 222 -or
    $trace.ModifierScenario -ne 'original-eaten-damage' -or -not $trace.OriginalEatenDamagePrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-EATEN-DAMAGE-PREPARED' -Quiet) -or
    $trace.OriginalEatenDamageCaptureFailures -ne 0 -or $trace.OriginalEatenDamageMismatches -ne 0 -or
    $trace.OriginalEatenDamageUnsupported -ne 0 -or $trace.OriginalEatenDamagePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 10 -or
    @($trace.RelicEatenDamageApplications).Count -ne 5 -or
    @($trace.RelicEatenDamageTests | Where-Object { -not $_.Result }).Count -lt 4)) {
    throw 'DamageOnEaten lacks unchanged owner, weakest-enemy damage/death callbacks or trigger/team/empty-room/reset controls.'
}
if ($OriginalConsumeDamageRelic -and ($trace.Schema -ne 221 -or $trace.OriginalConsumeDamageSchema -ne 221 -or
    $trace.ModifierScenario -ne 'original-consume-damage' -or -not $trace.OriginalConsumeDamagePrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CONSUME-DAMAGE-PREPARED' -Quiet) -or
    $trace.OriginalConsumeDamageCaptureFailures -ne 0 -or $trace.OriginalConsumeDamageMismatches -ne 0 -or
    $trace.OriginalConsumeDamageUnsupported -ne 0 -or $trace.OriginalConsumeDamagePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 8 -or
    @($trace.RelicConsumeDamageApplications).Count -ne 4 -or
    @($trace.RelicConsumeDamageTests | Where-Object { -not $_.Result }).Count -lt 4)) {
    throw 'DamageOnConsume lacks unchanged owner, front-enemy damage/death callbacks or non-Consume/empty-room/reset controls.'
}
if ($OriginalCardPlayDamageRelic -and ($trace.Schema -ne 220 -or $trace.OriginalCardPlayDamageSchema -ne 220 -or
    $trace.ModifierScenario -ne 'original-card-play-damage' -or -not $trace.OriginalCardPlayDamagePrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARD-PLAY-DAMAGE-PREPARED' -Quiet) -or
    $trace.OriginalCardPlayDamageCaptureFailures -ne 0 -or $trace.OriginalCardPlayDamageMismatches -ne 0 -or
    $trace.OriginalCardPlayDamageUnsupported -ne 0 -or $trace.OriginalCardPlayDamagePending -ne 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 7 -or
    @($trace.RelicCardPlayDamageApplications).Count -ne 4 -or
    @($trace.RelicCardPlayDamageTests | Where-Object { -not $_.Result }).Count -lt 3)) {
    throw 'DamageOnCardPlayed lacks unchanged owner, random enemy damage/death callbacks, Monster/empty-room/reset controls, or matching native RNG.'
}
if ($OriginalDeckTurnDrawRelic -and ($trace.Schema -ne 219 -or $trace.OriginalDeckTurnDrawSchema -ne 219 -or
    $trace.ModifierScenario -ne 'original-deck-turn-draw' -or -not $trace.OriginalDeckTurnDrawPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-DECK-TURN-DRAW-PREPARED' -Quiet) -or
    $trace.OriginalDeckTurnDrawCaptureFailures -ne 0 -or $trace.OriginalDeckTurnDrawMismatches -ne 0 -or
    $trace.OriginalDeckTurnDrawUnsupported -ne 0 -or $trace.OriginalDeckTurnDrawPending -ne 0 -or
    @($trace.RelicDeckTurnDrawTests | Where-Object Phase -eq 'threshold-control').Count -ne 7 -or
    @($trace.RelicDeckTurnDrawApplications).Count -lt 2 -or
    @($trace.RelicDeckTurnDrawApplications | Where-Object { $_.NativeCount -ne 2 -or $_.Placement }).Count -ne 0)) {
    throw 'DrawCardPerDeckSize lacks native threshold controls, restored deck, or repeated natural +2 after-draw callbacks.'
}
if ($OriginalCooldownOnDamageRelic -and ($trace.Schema -ne 218 -or $trace.OriginalCooldownOnDamageSchema -ne 218 -or
    $trace.ModifierScenario -ne 'original-cooldown-on-damage-dh' -or
    -not $trace.OriginalCooldownOnDamagePrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-COOLDOWN-ON-DAMAGE-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 3 -or
    $trace.OriginalCooldownOnDamageCaptureFailures -ne 0 -or $trace.OriginalCooldownOnDamageMismatches -ne 0 -or
    $trace.OriginalCooldownOnDamageUnsupported -ne 0 -or $trace.OriginalCooldownOnDamagePending -ne 0 -or
    @($trace.RelicCooldownOnDamageApplications).Count -ne 1 -or
    $trace.RelicCooldownOnDamageApplications[0].CooldownBefore -ne 3 -or
    $trace.RelicCooldownOnDamageApplications[0].CooldownAfter -ne 2 -or
    @($trace.RelicCooldownOnDamageTests | Where-Object { $_.CardDataId -eq 'fc119000-0000-4000-8000-000000000553' -and $_.Result }).Count -ne 1 -or
    @($trace.RelicCooldownOnDamageTests | Where-Object { $_.CardDataId -eq 'fc119000-0000-4000-8000-000000000554' -and $_.Result }).Count -ne 0)) {
    throw 'CooldownOnDamage lacks its unchanged owner, friendly-damage callback, no-damage control, or exact 3-to-2 cooldown witness.'
}
if ($OriginalMonsterSpellTargeting -and ($trace.Schema -ne 188 -or
    $trace.ModifierScenario -ne 'original-monster-spell-targeting' -or
    -not $trace.OriginalMonsterSpellTargetingPrepared -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-MONSTER-SPELL-TARGETING-PREPARED' -Quiet) -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 1)) {
    throw 'SingleToTeamMonsterSpellTargeting lacks its direct Monster-target spell witness.'
}
if ($OriginalDamageOnFloorTransition -and ($trace.Schema -ne 182 -or
    $trace.ModifierScenario -ne 'bump' -or
    -not $trace.OriginalDamageOnFloorTransitionPrepared -or
    @($trace.BumpOperations).Count -ne 22 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-FLOOR-TRANSITION-DAMAGE-ACQUIRED' -Quiet))) {
    throw 'DamageOnFloorTransition lacks its exact owner capture or complete paid Bump witness set.'
}
if ($OriginalPyregelOnAscend -and ($trace.Schema -ne 185 -or
    $trace.ModifierScenario -ne 'bump' -or
    -not $trace.OriginalPyregelOnAscendPrepared -or
    @($trace.BumpOperations).Count -ne 22 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-PYREGEL-ON-ASCEND-ACQUIRED' -Quiet))) {
    throw 'PyregelOnAscend_DH lacks its exact owner capture or complete paid Bump witness set.'
}
if ($OriginalFunguyDeckScaling) {
    $pileStartRecords = @($trace.RelicPileStartTurn)
    if ($trace.Schema -ne 174 -or $trace.ModifierScenario -ne 'original-funguy-deck-scaling' -or
        -not $trace.OriginalFunguyDeckScalingPrepared -or $pileStartRecords.Count -lt 2 -or
        @($pileStartRecords | Where-Object {
            -not $_.Completed -or $null -ne $_.UnsupportedReason -or $null -ne $_.Difference -or
            $_.VisibleDeckCount -ne 40 -or $_.ApplicationsPerRoom -ne 2
        }).Count -gt 0 -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-FUNGUY-DECK-PREPARED deck=40 threshold=20' -Quiet)) {
        throw 'StartOfTurnAddFunguysPerDeckSize lacks the native 40-card repeated callback witness.'
    }
}
if ($OriginalCardDrawOnHeal -and ($trace.Schema -ne 175 -or
    $trace.ModifierScenario -ne 'original-card-draw-on-heal' -or
    -not $trace.OriginalCardDrawOnHealPrepared -or
    @($trace.RelicUnitResources | Where-Object Trigger -EQ 'OnHeal').Count -lt 4 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CARD-DRAW-ON-HEAL-PREPARED two same-turn allied heals, a next-turn allied heal, and an enemy-heal control' -Quiet))) {
    throw 'CardDrawOnHeal lacks repeated allied heal callbacks or its enemy-heal control.'
}
if ($OriginalBrokenRailRelic -and ($trace.Schema -ne 176 -or
    $trace.ModifierScenario -ne 'original-broken-rail' -or
    -not $trace.OriginalBrokenRailPrepared -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 1 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-BROKEN-RAIL-PREPARED points=3 native ordinary monster, 80 HP, 20 attack' -Quiet))) {
    throw 'BrokenRail lacks its prepared +2 HP per forge point native monster summon witness.'
}
if ($OriginalHephsBlueprints -and ($trace.Schema -ne 200 -or
    $trace.ModifierScenario -ne 'original-hephs-blueprints' -or
    -not $trace.OriginalHephsBlueprintsPrepared -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 2 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-HEPHS-BLUEPRINTS-PREPARED points=3 equipment-filter control monster then eligible equipment' -Quiet))) {
    throw 'HephsBlueprints lacks its Equipment-filter control and eligible three-point native equipment play.'
}
if ($OriginalRageOnBurnoutStackLoss -and ($trace.Schema -ne 177 -or
    $trace.ModifierScenario -ne 'original-rage-on-burnout-stack-loss' -or
    -not $trace.OriginalRageOnBurnoutStackLossPrepared -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 1 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-RAGE-ON-BURNOUT-STACK-LOSS-PREPARED one native Steward starts with Ephemeral' -Quiet))) {
    throw 'RageOnBurnoutStackLoss lacks its real native Ephemeral-removal action witness.'
}
if ($OriginalPyreRevenge -and ($trace.Schema -ne 178 -or
    $trace.ModifierScenario -ne 'original-pyre-revenge' -or
    -not $trace.NativeWon -or -not $trace.OriginalPyreRevengePrepared -or
    -not $trace.OriginalPyreRevengeInstallation.Completed -or
    @($trace.TriggeredStatuses | Where-Object { $_.ActorId -eq $trace.OriginalPyreRevengeInstallation.UnitId -and $_.Kind -eq 'OnHit' }).Count -eq 0 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-PYRE-REVENGE-PREPARED one unchanged OnHit trigger installed on the live Pyre' -Quiet))) {
    throw 'PyreRevengeAddPyregel lacks its live Pyre trigger installation or OnHit effect witness.'
}
$extractCallbacks = @($trace.OriginalExtractForEmberAndEchosCallbacks | Where-Object {
    $_.CardInstanceId -eq $trace.OriginalExtractForEmberAndEchosCarrierId
})
$extractActions = @($trace.Actions | Where-Object {
    $_.ScenarioAction -and $_.Action.CardInstanceId -eq $trace.OriginalExtractForEmberAndEchosCarrierId
})
if ($OriginalExtractForEmberAndEchos -and ($trace.Schema -ne 179 -or
    $trace.ModifierScenario -ne 'original-extract-for-ember-and-echos' -or
    -not $trace.NativeWon -or -not $trace.OriginalExtractForEmberAndEchosPrepared -or
    -not $trace.OriginalExtractForEmberAndEchosCarrierId -or
    $extractActions.Count -eq 0 -or $extractCallbacks.Count -lt 2 -or
    $extractCallbacks[0].Kind -ne 'Energy' -or $extractCallbacks[1].Kind -ne 'Corruption' -or
    @($trace.OriginalExtractForEmberAndEchosCallbacks | Where-Object { $_.CardInstanceId -eq $trace.OriginalExtractForEmberAndEchosCarrierId -and
        (-not $_.Completed -or $_.Amount -ne 1) }).Count -gt 0 -or
    $extractCallbacks[0].RoomIndex -ne $extractActions[0].Action.RoomIndex -or
    $extractCallbacks[1].RoomIndex -ne $extractActions[0].Action.RoomIndex -or
    @($trace.OriginalExtractForEmberAndEchosCallbacks | Where-Object { -not $_.Completed }).Count -gt 0 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-EXTRACT-EMBER-AND-ECHOS-PREPARED.*paramInt=0 paramBool=false cost=2' -Quiet))) {
    throw 'ExtractForEmberAndEchos lacks its live Corrupt Restricted card-play effect witnesses.'
}
if (($OriginalNullRelic -or $OriginalNullRelicBaseline) -and ($null -eq $trace.OriginalNullRelic -or
    $trace.OriginalNullRelic.Present -ne $(if ($OriginalNullRelicBaseline) { 0 } else { 1 }) -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-NULL-RELIC-ACQUIRED baseline=' -Quiet))) {
    throw 'Original null relic lacks native acquisition/removal evidence.'
}
if ($OriginalUnitPhaseUpgradeRelics -and ($trace.ModifierScenario -ne 'original-unit-phase-upgrade-relics' -or
    @($trace.RelicPhaseUpgrades).Count -lt 5 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-UNIT-PHASE-UPGRADES-PREPARED two unchanged' -Quiet))) {
    throw 'Original phase-upgrade owners lack actual native effects.'
}
if ($OriginalSoloUnitUpgradeRelic -and ($trace.Schema -ne 183 -or
    $trace.ModifierScenario -ne 'original-add-stats-to-solo-units' -or
    $trace.OriginalSoloUnitUpgrade.OwnerId -ne '724a1c19-192f-48c1-8099-8ae3ecb9c1e5' -or
    @($trace.RelicSoloUnitUpgrades).Count -lt 2 -or
    @($trace.RelicSoloUnitUpgradeTests | Where-Object { $_.Phase -eq 'TestEffectStartOfBattle' -and $_.Returned -eq $false }).Count -eq 0 -or
    @($trace.RelicSoloUnitUpgradeTests | Where-Object { $_.Phase -eq 'TestEffectTurnPhaseTiming' -and $_.Returned -eq $true }).Count -lt 2 -or
    @($trace.RelicSoloUnitUpgrades | Where-Object { -not $_.Completed -or $null -eq $_.Actual }).Count -gt 0 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-SOLO-UNIT-UPGRADE-PREPARED native single=' -Quiet))) {
    throw 'AddStatsToSoloUnits lacks native solo, multi-unit and empty-floor callback evidence.'
}
if ($OriginalFrontRoomHealthUpgradeRelic -and ($trace.Schema -ne 207 -or
    $trace.ModifierScenario -ne 'original-front-room-health-upgrade' -or
    $trace.OriginalFrontRoomHealthUpgrade.OwnerId -ne 'cb2c9d18-9a05-4523-acdc-6e4af73152aa' -or
    $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0 -or
    @($trace.RelicFrontRoomHealthUpgrades).Count -lt 2 -or
    @($trace.RelicFrontRoomHealthUpgradeTests | Where-Object { $_.Phase -eq 'TestEffectStartOfTurn' -and $_.Returned -eq $true }).Count -lt 1 -or
    @($trace.RelicFrontRoomHealthUpgrades | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualRooms }).Count -gt 0 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-FRONT-ROOM-HEALTH-PREPARED native \+15 max/current HP per turn' -Quiet))) {
    throw 'Front-room max-health relic lacks repeated native callback and floor-front controls.'
}
if ($OriginalChampionStartTurnHealRelic -and ($trace.OriginalChampionStartTurnHealSchema -ne 208 -or
    $trace.ModifierScenario -ne 'original-champion-start-turn-heal' -or
    $trace.OriginalChampionStartTurnHeal.OwnerId -ne 'dbd5aec6-5046-4df2-ae61-4b646b8d1702' -or
    $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0 -or
    $trace.RelicStartTurnHealCaptureFailures -ne 0 -or $trace.RelicStartTurnHealMismatches -ne 0 -or
    $trace.RelicStartTurnHealUnsupported -ne 0 -or $trace.RelicStartTurnHealPending -ne 0 -or
    @($trace.RelicStartTurnHealResets).Count -ne 1 -or
    @($trace.RelicStartTurnHealResets | Where-Object { -not $_.Completed -or $_.BeforeCounter -ne 4 -or $_.AfterCounter -ne 0 }).Count -gt 0 -or
    @($trace.RelicStartTurnHealOperations).Count -lt 1 -or
    @($trace.RelicStartTurnHealOperations | Where-Object { -not $_.Completed -or $null -eq $_.Actual }).Count -gt 0 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 6 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-CHAMPION-START-TURN-HEAL-PREPARED unchanged \+ full-health behavior' -Quiet))) {
    throw 'ChampsHealBeginningOfTurn lacks counter reset, Tower-wide Champion healing and non-Champ native controls.'
}
if ($OriginalNimsOathRelic -and ($trace.Schema -ne 210 -or
    $trace.OriginalNimsOathSchema -ne 210 -or $trace.ModifierScenario -ne 'original-nims-oath' -or
    $trace.OriginalNimsOath.OwnerId -ne '727e69a6-3a8c-4029-895c-99e9dac0bc0a' -or
    $trace.OriginalNimsOath.AssetKey -ne 'Inkbound_NimsOath' -or
    $trace.OriginalNimsOath.TriggerMetadata.RuntimeCallbackSupported -ne $false -or
    $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0 -or
    @($trace.RelicNimsOathRoomTests).Count -lt 2 -or
    @($trace.RelicNimsOathRoomTests | Where-Object Result).Count -eq 0 -or
    @($trace.RelicNimsOathRoomTests | Where-Object { -not $_.Result }).Count -eq 0 -or
    @($trace.RelicNimsOathTimingTests | Where-Object { $_.Turn -eq 0 -and -not $_.Result }).Count -ne 1 -or
    @($trace.RelicNimsOathTimingTests | Where-Object Result).Count -eq 0 -or
    @($trace.RelicNimsOathApplications).Count -lt 4 -or
    @($trace.RelicNimsOathApplications | Where-Object { -not $_.Completed -or $null -eq $_.After -or
        -not $_.ModelSupported -or $null -ne $_.Error -or $null -ne $_.Difference -or
        @($_.Ranges | Where-Object Rng -eq 'Battle').Count -ne 1 }).Count -gt 0 -or
    @($trace.RelicNimsOathApplications | Where-Object { @($_.ModelCandidateRooms).Count -ge 2 }).Count -eq 0 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-NIMS-OATH-ACQUIRED unchanged owner=727e69a6-3a8c-4029-895c-99e9dac0bc0a' -Quiet))) {
    throw 'Inkbound_NimsOath lacks TestEffects controls, exact random-room Dazed applications or the bounded OnKill metadata boundary.'
}
if ($OriginalJudgesScalesRelic -and ($trace.Schema -ne 211 -or
    $trace.OriginalJudgesScalesSchema -ne 211 -or $trace.ModifierScenario -ne 'original-judges-scales' -or
    $trace.OriginalJudgesScales.OwnerId -ne '3511a804-42be-465c-9d40-56b988c8a940' -or
    $trace.OriginalJudgesScales.EffectType -ne 'RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw' -or
    $trace.OriginalJudgesScales.DisallowedInPlacementPhase -ne $false -or
    $trace.OriginalJudgesScales.ParamInt2 -ne 2 -or
    $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0 -or
    @($trace.RelicJudgesScalesTests | Where-Object { $_.Result -and $_.Turn -eq 0 -and $_.Placement -and
        @($_.EligibleCardIds).Count -gt 0 }).Count -eq 0 -or
    @($trace.RelicJudgesScalesApplications).Count -ne 1 -or
    @($trace.RelicJudgesScalesApplications | Where-Object { -not $_.Completed -or $null -eq $_.After -or
        -not $_.Placement -or -not $_.Cancelled -or $_.SuppressedFollowupSelections -ne 1 -or
        -not $_.ModelSupported -or $null -ne $_.Error -or $null -ne $_.Difference -or $_.Choices.Count -ne 1 }).Count -gt 0 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-JUDGES-SCALES-ACQUIRED unchanged owner=3511a804-42be-465c-9d40-56b988c8a940' -Quiet))) {
    throw 'JudgesScales lacks an eligible deployment hand, native discard/cancel/replacement evidence or exact model replay.'
}
if ($OriginalNoUpgradeCardCostRelic -and ($trace.Schema -ne 212 -or
    $trace.OriginalNoUpgradeCardCostSchema -ne 212 -or
    $trace.ModifierScenario -ne 'original-reduce-cost-unit-no-upgrade' -or
    $trace.OriginalNoUpgradeCardCost.Definition.OwnerId -ne 'f03c7664-0485-4bcf-8bb9-f76321e090c3' -or
    $trace.OriginalNoUpgradeCardCost.Definition.AssetKey -ne 'ReduceCostUnitIfNoUpgrade' -or
    $trace.OriginalNoUpgradeCardCost.Definition.EffectType -ne 'RelicEffectModifyCardCostIfNoUpgrades' -or
    $trace.OriginalNoUpgradeCardCost.Definition.ParamInt -ne -1 -or
    $trace.OriginalNoUpgradeCardCost.Definition.ParamCardType -ne 'Monster' -or
    $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or
    $trace.Unsupported -ne 0 -or $trace.Pending -ne 0 -or
    @($trace.OriginalNoUpgradeCardCost.Callbacks).Count -lt 4 -or
    @($trace.OriginalNoUpgradeCardCost.Callbacks | Where-Object { -not $_.ModelSupported -or
        $null -eq $_.After -or $null -ne $_.Error -or $null -ne $_.Difference }).Count -gt 0 -or
    @($trace.OriginalNoUpgradeCardCost.Callbacks | Where-Object { $_.Kind -eq 'OnCardAdded' -and
        $_.Before.DataId -eq 'fc119000-0000-4000-8000-00000000052001' -and $_.Returned -eq $true -and $_.UpgradeAdded }).Count -eq 0 -or
    @($trace.OriginalNoUpgradeCardCost.Callbacks | Where-Object { $_.Kind -eq 'OnPermanentCardUpgradeApplied' -and
        $_.Before.DataId -eq 'fc119000-0000-4000-8000-00000000052001' -and $_.ModelSupported }).Count -eq 0 -or
    @($trace.RelicCardModifiers | Where-Object { $_.Before.DataId -in @(
        'fc119000-0000-4000-8000-00000000052001', 'fc119000-0000-4000-8000-00000000052002',
        'fc119000-0000-4000-8000-00000000052003') }).Count -ne 6 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-NO-UPGRADE-COST-PREPARED' -Quiet))) {
    throw 'ReduceCostUnitIfNoUpgrade lacks native card-modifier and callback lifecycle evidence.'
}
if ($OriginalPyreHealthCardCostRelic -and ($trace.OriginalPyreCardCostSchema -ne 209 -or
    $trace.ModifierScenario -ne 'original-pyre-health-card-cost' -or
    $trace.OriginalPyreCardCost.OwnerId -ne '7f4c8a90-2518-4a41-8c8c-0f5ca0b0a475' -or
    $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0 -or
    @($trace.OriginalPyreCardCost.Eligibility).Count -ne 3 -or
    @($trace.OriginalPyreCardCost.Results).Count -ne 3 -or
    @($trace.OriginalPyreCardCost.QueuedHits).Count -ne 1 -or
    @($trace.Actions | Where-Object ScenarioAction).Count -ne 3 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-PYRE-CARD-COST-PREPARED' -Quiet))) {
    throw 'PyreHealthForEmberCardCost lacks native affordable, health-paid, X-cost and OnHit callback evidence.'
}
if ($OriginalTurnDrawRelics -and ($trace.ModifierScenario -ne 'original-turn-draw-relics' -or
    @($trace.RelicTurnDraw).Count -lt 8 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-TURN-DRAW-PREPARED native full/partial/zero' -Quiet))) {
    throw 'Original turn draw owners lack native full-health controls and post-opening consumer.'
}
if ($OriginalOpeningDrawRelics -and ($trace.ModifierScenario -ne 'original-opening-draw-relics' -or
    @($trace.RelicOpeningDraw).Count -lt 5 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-OPENING-DRAW-PREPARED natural primary selections' -Quiet))) {
    throw 'Original opening draw owners lack natural primary and controlled fallback callbacks.'
}
if ($OriginalUnitResourceRelics -and ($trace.ModifierScenario -ne 'original-unit-resource-relics' -or
    @($trace.RelicUnitResources).Count -lt 10 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-UNIT-RESOURCES-PREPARED three unchanged owners' -Quiet))) {
    throw 'Original unit resource owners lack actual native callbacks.'
}
if ($OriginalAfterDrawGenerationRelics -and ($trace.ModifierScenario -ne 'original-after-draw-generation-relics' -or
    @($trace.RelicAfterDrawGeneration).Count -lt 8 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-AFTER-DRAW-GENERATION-PREPARED four unchanged owners' -Quiet))) {
    throw 'Original after-draw generation lacks unchanged owners or actual Test/Apply consumers.'
}
if ($OriginalAddSnackOnKill -and ($trace.Schema -ne 192 -or $trace.ModifierScenario -ne 'original-add-snack-on-kill' -or
    -not $trace.OriginalAddSnackOnKillPrepared -or @($trace.RelicUnitTriggerCardGeneration).Count -lt 1 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-ADD-SNACK-ON-KILL-PREPARED unchanged relic' -Quiet))) {
    throw 'Original AddSnackOnKill lacks an unchanged owner or native unit-death callback.'
}
if ($OriginalFinalEndingArtifact -and ($trace.Schema -ne 193 -or $trace.ModifierScenario -ne 'original-final-ending-artifact' -or
    -not $trace.OriginalFinalEndingArtifactPrepared -or @($trace.RelicChampionCardAfterDraw).Count -lt 1 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-FINAL-ENDING-ARTIFACT-PREPARED unchanged owner' -Quiet))) {
    throw 'Original FinalEndingArtifact lacks its unchanged owner or natural post-draw callback.'
}
if ($OriginalBalatroEventYorick -and ($trace.Schema -ne 194 -or $trace.ModifierScenario -ne 'original-balatro-event-yorick' -or
    -not $trace.OriginalBalatroEventYorickPrepared -or @($trace.RelicCardPlayGoldOnExhaust).Count -lt 1 -or
    @($trace.RelicCardPlayGoldOnExhaustTests).Count -lt 3 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-BALATRO-EVENT-YORICK-PREPARED unchanged owner' -Quiet))) {
    throw 'BalatroEventYorick lacks its unchanged owner, native Test controls or natural gold callback.'
}
if ($OriginalBalatroEventCavendish -and ($trace.Schema -ne 204 -or $trace.ModifierScenario -ne 'original-balatro-event-cavendish' -or
    -not $trace.OriginalBalatroEventCavendishPrepared -or @($trace.RelicCardModifiers).Count -lt 6 -or
    -not $trace.OriginalBalatroEventCavendishEndCombatRestored -or @($trace.OriginalBalatroEventCavendishEndCombat).Count -ne 2 -or
    -not $trace.OriginalBalatroEventCavendishCardlessCallbacksRestored -or @($trace.OriginalBalatroEventCavendishCardlessCallbacks).Count -ne 2 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-BALATRO-EVENT-CAVENDISH-PREPARED unchanged 3x unmodified Monster attack' -Quiet))) {
    throw 'BalatroEventCavendish lacks unchanged-owner manager, end-combat miss/hit or modifier-reset observations.'
}
if ($OriginalGarfieldBoxRandomCard -and ($trace.ModifierScenario -ne 'original-garfield-box-random-card-per-turn' -or
    $trace.Schema -ne 189 -or -not $trace.OriginalGarfieldBoxRandomCardPrepared -or
    @($trace.RelicPrecachedPlayerClanGeneration).Count -lt 2 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-GARFIELD-BOX-RANDOM-CARD-PREPARED unchanged owner' -Quiet))) {
    throw 'GarfieldBoxRandomCardPerTurn lacks unchanged owner or completed post-draw native callbacks.'
}
if ($OriginalHealthChoiceRelic -and ($trace.ModifierScenario -ne 'original-health-choice-relic' -or
    @($trace.RelicSpawnStatuses).Count -eq 0 -or @($trace.TriggeredStatuses).Count -lt 4 -or
    -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-HEALTH-CHOICE-PREPARED unchanged HairOfTheHybrid' -Quiet))) {
    throw 'Original health-choice relic lacks unchanged owner, birth or equipment callback evidence.'
}
if ($CampaignDraftCalibration) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $draftPath = Join-Path $profile 'campaign-draft-calibration.mt2f'
    if (-not (Test-Path -LiteralPath $draftPath)) { throw 'Missing current campaign draft calibration.' }
    $draftDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($draftPath)
    try {
        $draftRoot = $draftDocument.RootElement
        if ($draftRoot.GetProperty('Schema').GetInt32() -ne 1 -or
            $draftRoot.GetProperty('Owners').GetArrayLength() -ne 4 -or
            -not $draftRoot.GetProperty('Before').ContentEquals($draftRoot.GetProperty('After')) -or
            $draftRoot.GetProperty('Drafts').GetArrayLength() -ne 840 -or
            $draftRoot.GetProperty('Health').GetArrayLength() -ne 3 -or
            $draftRoot.GetProperty('StartingUpgrades').GetProperty('Pool').GetArrayLength() -eq 0 -or
            $draftRoot.GetProperty('StartingUpgrades').GetProperty('Rows').GetArrayLength() -lt 4) {
            throw 'Campaign draft original-owner matrix, UpgradedDrafts evidence or recorded state restoration is incomplete.'
        }
    } finally { $draftDocument.Dispose() }
}
if ($OriginalPurgeToExhaustRelic) {
    $purgeOwnerId = '10648aee-8577-49b6-8659-318a7f922d5a'
    $purgeBattleSpellId = 'fc119000-0000-4000-8000-0000000002a1'
    $purgeRows = @($trace.RelicCardModifiers | Where-Object {
        $ownerId = $purgeOwnerId
        @($_.Relics | Where-Object { $_.DataId -eq $ownerId }).Count -gt 0
    })
    $playedPurgeSpell = @($trace.Actions | Where-Object {
        $record = $_
        if (-not $record.ScenarioAction) { return $false }
        @($record.Before.Spawn.Train.Context.Cards.Hand | Where-Object { $_.DataId -eq $purgeBattleSpellId }).Count -gt 0
    })
    if ($trace.Schema -ne 184 -or $trace.ModifierScenario -ne 'original-purge-to-exhaust-relic' -or
        -not (Select-String -LiteralPath $unityLog -Pattern 'ORIGINAL-PURGE-TO-EXHAUST-ACQUIRED unchanged PurgeToExhaust SelfPurge=>Exhaust for spells/equipment' -Quiet) -or
        $purgeRows.Count -lt 18 -or $playedPurgeSpell.Count -ne 1) {
        throw 'PurgeToExhaust native owner, repeated manager controls or played battle spell evidence is missing.'
    }
    Write-Output "NATIVE-PURGE-TO-EXHAUST SETUP PASS: $($purgeRows.Count) manager rows; converted battle spell played."
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
if ($CardCatalog) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $cardCatalogDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'card-catalog.mt2f'))
    try {
        $cardCatalogRoot = $cardCatalogDocument.RootElement
        $cardCatalogEntries = @($cardCatalogRoot.GetProperty('Cards').EnumerateArray())
        if ($cardCatalogRoot.GetProperty('Schema').GetInt32() -ne 1 -or
            $cardCatalogRoot.GetProperty('Scope').GetString() -ne 'AllGameData' -or
            $cardCatalogRoot.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            -not $cardCatalogRoot.GetProperty('BranchCardMasksEnabled').GetBoolean() -or
            -not $cardCatalogRoot.GetProperty('DecisionUnchanged').GetBoolean() -or
            $cardCatalogRoot.GetProperty('FrameBefore').GetInt32() -ne $cardCatalogRoot.GetProperty('FrameAfter').GetInt32() -or
            -not $cardCatalogRoot.GetProperty('RngBefore').ContentEquals($cardCatalogRoot.GetProperty('RngAfter')) -or
            $cardCatalogEntries.Count -ne 668 -or
            @($cardCatalogEntries | ForEach-Object { $_.GetProperty('Id').GetString() } | Sort-Object -Unique).Count -ne 668) {
            throw 'Original card catalog is incomplete or changed native decision/RNG/frame.'
        }
        Write-Output "NATIVE-CARD-CATALOG PASS: 668 unique definitions with native logbook/ability roles and model projections; unchanged decision/RNG/frame."
    } finally { $cardCatalogDocument.Dispose() }
}
if ($RelicCatalog) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $catalogArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'relic-catalog.mt2f'))
    try {
        $catalog = $catalogArchive.RootElement
        $catalogRecords = @($catalog.GetProperty('Relics').EnumerateArray())
        if ($catalog.GetProperty('Schema').GetInt32() -ne 9 -or
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
if ($PermanentTraits) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $permanentArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'permanent-trait-calibration.mt2f'))
    try {
        $permanentData = $permanentArchive.RootElement
        if (-not $permanentData.GetProperty('ContextUnchanged').GetBoolean() -or
            $permanentData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $permanentData.GetProperty('FrameBefore').GetInt32() -ne $permanentData.GetProperty('FrameAfter').GetInt32() -or
            -not $permanentData.GetProperty('RngBefore').ContentEquals($permanentData.GetProperty('RngAfter')) -or
            $permanentData.GetProperty('Rows').GetArrayLength() -ne 176) { throw 'Incomplete or mutating permanent trait calibration.' }
        Write-Output 'NATIVE-PERMANENT-TRAITS-CAPTURE PASS: 176 native operations; unchanged decision/RNG/frame.'
    } finally { $permanentArchive.Dispose() }
}
if ($HoldoverVariants) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $holdoverArchive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'holdover-variants-calibration.mt2f'))
    try {
        $holdoverData = $holdoverArchive.RootElement
        if ($holdoverData.GetProperty('Schema').GetInt32() -ne 1 -or
            -not $holdoverData.GetProperty('ContextUnchanged').GetBoolean() -or
            $holdoverData.GetProperty('GameModuleMvid').GetString() -ne $trace.GameModuleMvid -or
            $holdoverData.GetProperty('FrameBefore').GetInt32() -ne $holdoverData.GetProperty('FrameAfter').GetInt32() -or
            -not $holdoverData.GetProperty('RngBefore').ContentEquals($holdoverData.GetProperty('RngAfter')) -or
            $holdoverData.GetProperty('Rows').GetArrayLength() -ne 100) { throw 'Incomplete or mutating Holdover variant calibration.' }
        Write-Output 'NATIVE-HOLDOVER-VARIANTS-CAPTURE PASS: 100 native operations; unchanged decision/RNG/frame.'
    } finally { $holdoverArchive.Dispose() }
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
$deepOfferingActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '37eaa51b-62cf-4cbc-971f-6d208fd07366'
})
$deepOfferingCoverage = -not $OriginalDeepOffering -or @($deepOfferingActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$moldedActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'dea99a2f-48c9-4246-ad44-01e84f46171e'
})
$moldedCoverage = -not $OriginalMolded -or @($moldedActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$brokenMemoriesActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'f954f70d-e83b-4c64-9827-89e5770a56ef'
})
$brokenMemoriesCoverage = -not $OriginalBrokenMemories -or @($brokenMemoriesActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$wickedBlazeActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'd930a4fe-1419-4c21-9ef2-fda4e25ee90b'
})
$wickedBlazeCoverage = -not $OriginalWickedBlaze -or @($wickedBlazeActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$offeringTokenActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'e2d5dbd1-3930-4a41-9631-bef04cbc3fa4'
})
$offeringTokenCoverage = -not $OriginalOfferingToken -or @($offeringTokenActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$moonRitualActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'b1fe0ba1-fcce-4764-9f4c-54b77d20b665'
})
$moonRitualCoverage = -not $OriginalMoonRitual -or @($moonRitualActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$selfPurgeEnergyGainActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'd0e1fe8a-fb2a-44e0-9d9b-a3ddd7475c34'
})
$selfPurgeEnergyGainCoverage = -not $OriginalSelfPurgeEnergyGain -or @($selfPurgeEnergyGainActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$smeltedTreasureActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'a1049085-eba0-4118-9546-ca20d680c2b8'
})
$smeltedTreasureCoverage = -not $OriginalSmeltedTreasure -or @($smeltedTreasureActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$pyreGroActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '2ba0f9eb-744e-4b3c-af4e-3ad39ae8a74f'
})
$pyreGroCoverage = -not $OriginalPyreGro -or @($pyreGroActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$deadlyVariantActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '5efb679b-6035-4d64-ab80-f29daad1fdf4'
})
$deadlyVariantCoverage = -not $OriginalDeadlyVariant -or @($deadlyVariantActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$drainActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '6fe6bf99-7e77-4191-9136-fbbecc223bdf'
})
$drainCoverage = -not $OriginalDrain -or @($drainActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$starterFrostbiteDiscardActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '6b91f902-deb4-4519-863b-0c8a7f5d871c'
})
$starterFrostbiteDiscardCoverage = -not $OriginalStarterFrostbiteDiscard -or @($starterFrostbiteDiscardActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$feedingFrenzyActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '9ed18c87-630e-4f91-a4b6-e6126e51c3b4'
})
$feedingFrenzyCoverage = -not $OriginalFeedingFrenzy -or @($feedingFrenzyActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
$titansGratitudeActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '9de23f57-7471-4c57-9b2e-b47f17e5f278'
})
$titansGratitudeCoverage = -not $OriginalTitansGratitude -or @($titansGratitudeActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
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
        @($upgradeSamples | Where-Object Origin -EQ 'Calibration').Count -eq $(if ($RemovedStatusScaling) { 42 } else { 30 }) -and
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
    $activationCoverage = $activationCoverage -and ($NullAbility -or $DamagePerHpAbility -or $selfHeals.Count -gt 0)
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
    DeepOfferingActions = $deepOfferingActions.Count
    DeepOfferingCoverage = $deepOfferingCoverage
    MoldedActions = $moldedActions.Count
    MoldedCoverage = $moldedCoverage
    BrokenMemoriesActions = $brokenMemoriesActions.Count
    BrokenMemoriesCoverage = $brokenMemoriesCoverage
    WickedBlazeActions = $wickedBlazeActions.Count
    WickedBlazeCoverage = $wickedBlazeCoverage
    OfferingTokenActions = $offeringTokenActions.Count
    OfferingTokenCoverage = $offeringTokenCoverage
    MoonRitualActions = $moonRitualActions.Count
    MoonRitualCoverage = $moonRitualCoverage
    SelfPurgeEnergyGainActions = $selfPurgeEnergyGainActions.Count
    SelfPurgeEnergyGainCoverage = $selfPurgeEnergyGainCoverage
    SmeltedTreasureActions = $smeltedTreasureActions.Count
    SmeltedTreasureCoverage = $smeltedTreasureCoverage
    PyreGroActions = $pyreGroActions.Count
    PyreGroCoverage = $pyreGroCoverage
    DeadlyVariantActions = $deadlyVariantActions.Count
    DeadlyVariantCoverage = $deadlyVariantCoverage
    DrainActions = $drainActions.Count
    DrainCoverage = $drainCoverage
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
if ($OriginalDeepOffering -and ($deepOfferingActions.Count -ne 1 -or -not $deepOfferingCoverage)) {
    throw 'Original Deep Offering did not produce exactly one matched card play.'
}
if ($OriginalMolded -and ($moldedActions.Count -ne 1 -or -not $moldedCoverage)) {
    throw 'Original Molded did not produce exactly one matched card play.'
}
if ($OriginalBrokenMemories -and ($brokenMemoriesActions.Count -ne 1 -or -not $brokenMemoriesCoverage)) {
    throw 'Original Broken Memories did not produce exactly one matched card play.'
}
if ($OriginalWickedBlaze -and ($wickedBlazeActions.Count -ne 1 -or -not $wickedBlazeCoverage)) {
    throw 'Original Wicked Blaze did not produce exactly one matched card play.'
}
if ($OriginalOfferingToken -and ($offeringTokenActions.Count -ne 1 -or -not $offeringTokenCoverage)) {
    throw 'Original Offering Token did not produce exactly one matched card play.'
}
if ($OriginalMoonRitual -and ($moonRitualActions.Count -ne 1 -or -not $moonRitualCoverage)) {
    throw 'Original Moon Ritual did not produce exactly one matched card play.'
}
if ($OriginalSelfPurgeEnergyGain -and ($selfPurgeEnergyGainActions.Count -ne 1 -or -not $selfPurgeEnergyGainCoverage)) {
    throw 'Original Self-Purge Energy Gain did not produce exactly one matched card play.'
}
if ($OriginalSmeltedTreasure -and ($smeltedTreasureActions.Count -ne 1 -or -not $smeltedTreasureCoverage)) {
    throw 'Original Smelted Treasure did not produce exactly one matched card play.'
}
if ($OriginalPyreGro -and ($pyreGroActions.Count -ne 1 -or -not $pyreGroCoverage)) {
    throw 'Original PyreGro did not produce exactly one matched card play.'
}
if ($OriginalDeadlyVariant -and ($deadlyVariantActions.Count -ne 1 -or -not $deadlyVariantCoverage)) {
    throw 'Original Deadly Variant did not produce exactly one matched card play.'
}
if ($OriginalDrain -and ($drainActions.Count -ne 1 -or -not $drainCoverage)) {
    throw 'Original Drain did not produce exactly one matched card play.'
}
if ($OriginalStarterFrostbiteDiscard -and ($starterFrostbiteDiscardActions.Count -ne 1 -or -not $starterFrostbiteDiscardCoverage)) {
    throw 'Original Starter Frostbite Discard did not produce exactly one matched card play.'
}
if ($OriginalFeedingFrenzy -and ($feedingFrenzyActions.Count -ne 1 -or -not $feedingFrenzyCoverage)) {
    throw 'Original Feeding Frenzy did not produce exactly one matched card play.'
}
if ($OriginalTitansGratitude -and ($titansGratitudeActions.Count -ne 1 -or -not $titansGratitudeCoverage)) {
    throw 'Original Titan''s Gratitude did not produce exactly one matched card play.'
}
$perfectInsanityActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '9d04efb5-8794-42a8-8282-e8ef895e6c3a'
})
$perfectInsanityCoverage = -not $OriginalPerfectInsanity -or @($perfectInsanityActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalPerfectInsanity -and ($perfectInsanityActions.Count -ne 1 -or -not $perfectInsanityCoverage)) {
    throw 'Original Perfect Insanity did not produce exactly one matched card play.'
}
$starterReformActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'fa394e19-4279-4e43-b0b3-75da045fc1eb'
})
$starterReformCoverage = -not $OriginalStarterReform -or @($starterReformActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalStarterReform -and ($starterReformActions.Count -ne 1 -or -not $starterReformCoverage)) {
    throw 'Original Starter Reform did not produce exactly one matched card play.'
}
$rapidRaiseActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '5b88c130-70b3-42cc-9903-4d245b4f946f'
})
$rapidRaiseCoverage = -not $OriginalRapidRaise -or @($rapidRaiseActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalRapidRaise -and ($rapidRaiseActions.Count -ne 1 -or -not $rapidRaiseCoverage)) {
    throw 'Original Rapid Raise did not produce exactly one matched card play.'
}
$spawningPodsActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '7c772030-474d-414c-939d-9581c6a70741'
})
$spawningPodsCoverage = -not $OriginalSpawningPods -or @($spawningPodsActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalSpawningPods -and ($spawningPodsActions.Count -ne 1 -or -not $spawningPodsCoverage)) {
    throw 'Original Spawning Pods did not produce exactly one matched card play.'
}
$mindsOfTheirOwnActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '809b4e83-cbd6-4b59-a263-d0ad76252066'
})
$mindsOfTheirOwnCoverage = -not $OriginalMindsOfTheirOwn -or @($mindsOfTheirOwnActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalMindsOfTheirOwn -and ($mindsOfTheirOwnActions.Count -ne 1 -or -not $mindsOfTheirOwnCoverage)) {
    throw 'Original Minds of Their Own did not produce exactly one matched card play.'
}
$metallurgyActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'bdfec381-a336-4b7d-b08a-c10c7ce231c0'
})
$metallurgyCoverage = -not $OriginalMetallurgy -or @($metallurgyActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalMetallurgy -and ($metallurgyActions.Count -ne 1 -or -not $metallurgyCoverage)) {
    throw 'Original Metallurgy did not produce exactly one matched card play.'
}
$fertileGroundActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '0c718572-ef91-4152-b6a9-0fdf907a9b06'
})
$fertileGroundCoverage = -not $OriginalFertileGround -or @($fertileGroundActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalFertileGround -and ($fertileGroundActions.Count -ne 1 -or -not $fertileGroundCoverage)) {
    throw 'Original Fertile Ground did not produce exactly one matched card play.'
}
$coverOfNightActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'e022d66a-f794-4ff9-95a6-b3cc53f49a27'
})
$coverOfNightCoverage = -not $OriginalCoverOfNight -or @($coverOfNightActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalCoverOfNight -and ($coverOfNightActions.Count -ne 1 -or -not $coverOfNightCoverage)) {
    throw 'Original Cover of Night did not produce exactly one matched card play.'
}
$waxenSpikeActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '8d291f9a-30a1-4a6d-8d9e-6dc2df4432d1'
})
$waxenSpikeCoverage = -not $OriginalWaxenSpike -or @($waxenSpikeActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalWaxenSpike -and ($waxenSpikeActions.Count -ne 1 -or -not $waxenSpikeCoverage)) {
    throw 'Original Waxen Spike did not produce exactly one matched X-cost card play.'
}
$impPressiveActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'adfdfdab-18cf-4f2e-8664-f33dd9251ba4'
})
$impPressiveCoverage = -not $OriginalImpPressive -or @($impPressiveActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalImpPressive -and ($impPressiveActions.Count -ne 1 -or -not $impPressiveCoverage)) {
    throw 'Original ImpPressive did not produce exactly one matched native Imp-sacrifice play.'
}
$tomeOfEvergrowthActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'e3774f6e-ae41-4563-97f8-ee0b0a475a62'
})
$tomeOfEvergrowthCoverage = -not $OriginalTomeOfEvergrowth -or @($tomeOfEvergrowthActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalTomeOfEvergrowth -and ($tomeOfEvergrowthActions.Count -ne 1 -or -not $tomeOfEvergrowthCoverage)) {
    throw 'Original Tome of Evergrowth did not produce exactly one matched ability-setting play.'
}
$tomeOfTheLostPathActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'e79f31b0-749a-4655-8af8-1fa226974edc'
})
$tomeOfTheLostPathCoverage = -not $OriginalTomeOfTheLostPath -or @($tomeOfTheLostPathActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalTomeOfTheLostPath -and ($tomeOfTheLostPathActions.Count -ne 1 -or -not $tomeOfTheLostPathCoverage)) {
    throw 'Original Tome of the Lost Path did not produce exactly one matched ability-setting play.'
}
$makeItRainActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '8a6ebb24-d5c1-430f-9332-1d3f43f47121'
})
$makeItRainCoverage = -not $OriginalMakeItRain -or @($makeItRainActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalMakeItRain -and ($makeItRainActions.Count -ne 1 -or -not $makeItRainCoverage)) {
    throw 'Original Make It Rain did not produce exactly one matched damage/reward play.'
}
$deathroarActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '73da2bc8-68a5-41b2-a639-754c2a98b6fd'
})
$deathroarCoverage = -not $OriginalDeathroar -or @($deathroarActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalDeathroar -and ($deathroarActions.Count -ne 1 -or -not $deathroarCoverage)) {
    throw 'Original Deathroar did not produce exactly one matched damage/status-transfer play.'
}
$secretIngredientActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'f4cf7469-4d84-40e9-94f6-6b743973f2ad'
})
$secretIngredientCoverage = -not $OriginalSecretIngredient -or @($secretIngredientActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -ge 1
if ($OriginalSecretIngredient -and ($secretIngredientActions.Count -lt 1 -or -not $secretIngredientCoverage)) {
    throw 'Original Secret Ingredient produced no matched original Mix play.'
}
$greenRecruitActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'c052c567-1e3c-4f36-a6cd-89b0648b9475'
})
$greenRecruitCoverage = -not $OriginalGreenRecruit -or @($greenRecruitActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalGreenRecruit -and ($greenRecruitActions.Count -ne 1 -or -not $greenRecruitCoverage)) {
    throw 'Original Green Recruit did not produce exactly one matched deployment.'
}
$primordiumActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '2e1b29f2-fbe8-47b9-8ef8-1bac4c1f1f65'
})
$primordiumCoverage = -not $OriginalPrimordium -or @($primordiumActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalPrimordium -and ($primordiumActions.Count -ne 1 -or -not $primordiumCoverage)) {
    throw 'Original Primordium did not produce exactly one matched deployment.'
}
$cheeryDeathcapActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq '97260e75-4e11-439e-8eda-cd961143fc12'
})
$cheeryDeathcapCoverage = -not $OriginalCheeryDeathcap -or @($cheeryDeathcapActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalCheeryDeathcap -and ($cheeryDeathcapActions.Count -ne 1 -or -not $cheeryDeathcapCoverage)) {
    throw 'Original Cheery Deathcap did not produce exactly one matched deployment.'
}
$enthusiasticFanActions = @($trace.Actions | Where-Object {
    $entry = $_
    $played = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $played.DataId -eq 'adaaaa9d-56a9-4a5f-a347-ee71d4767df5'
})
$enthusiasticFanCoverage = -not $OriginalEnthusiasticFan -or @($enthusiasticFanActions | Where-Object {
    -not $_.Difference -and $_.Predicted.Supported
}).Count -eq 1
if ($OriginalEnthusiasticFan -and ($enthusiasticFanActions.Count -ne 1 -or -not $enthusiasticFanCoverage)) {
    throw 'Original Enthusiastic Fan did not produce exactly one matched deployment.'
}
if (-not $frostChannelingCoverage) { throw 'Original FrostChanneling did not combine cross-team poison stacks in a matched action.' }
if (-not $transferAbilityCoverage) { throw 'Native ability-user transfer did not buff its actor and spawner card in a matched action.' }
if (-not $damageByUnitsKilledCoverage) { throw 'Original Shardtail effect did not sacrifice an Imp and damage an enemy in a matched action.' }
if (-not $cardEffectKillCoverage) { throw 'Native CardEffectKill did not kill one enemy and one player unit in a full battle action.' }
if (-not $cardEffectKillSubtypeCoverage) { throw 'Native CardEffectKill did not kill a matching subtype target in a full battle action.' }
if (-not $cardRecursionCoverage) { throw 'Native WurmkinEtchings recursion did not restore the expected cards in a matched full battle action.' }
$result | ConvertTo-Json
if ($SettleDeathDissolves) {
    if (-not $trace.DeathDissolveSettlementEnabled -or $trace.Schema -ne $(if ($OriginalBigDamageFromManyMorsels) { 169 } elseif ($OriginalFloorCapacityRelics) { 119 } elseif ($PyreResourceRefresh) { 118 } elseif ($OriginalResourceRelicBatch) { 117 } elseif ($OriginalLifecycleRelicBatch) { 115 } elseif ($TypedDraw) { 112 } elseif ($SpawnStatusRelicsClones) { 111 } elseif ($SpawnStatusRelics) { 110 } elseif ($IncantRelic) { 109 } elseif ($AbilityIncant) { 108 } elseif ($SettleCardAnimations) { 106 } else { 105 }) -or
        @($trace.DeathDissolveSettlements).Count -eq 0 -or
        @($trace.DeathDissolveSettlements | Where-Object { -not $_.Completed -or $_.PendingAfter -ne 0 -or $_.Error }).Count -ne 0 -or
        @($trace.DeathDissolveCallbacks | Where-Object { $_.Error }).Count -ne 0) {
        throw 'Native death-dissolve settlement is incomplete or failed.'
    }
}
if ($SettleCardAnimations) {
    if ($trace.Schema -ne $(if ($OriginalBigDamageFromManyMorsels) { 169 } elseif ($OriginalFloorCapacityRelics) { 119 } elseif ($PyreResourceRefresh) { 118 } elseif ($OriginalResourceRelicBatch) { 117 } elseif ($OriginalLifecycleRelicBatch) { 115 } elseif ($TypedDraw) { 112 } elseif ($SpawnStatusRelicsClones) { 111 } elseif ($SpawnStatusRelics) { 110 } elseif ($IncantRelic) { 109 } elseif ($AbilityIncant) { 108 } else { 106 }) -or -not $trace.CardAnimationSettlement.Enabled -or
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
} finally {
    try {
        if ($PlayerAdvisor -and $playerCheckPlugin -and (Test-Path -LiteralPath $playerCheckPlugin)) {
            if ($process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
            Move-Item -LiteralPath $playerCheckPlugin -Destination (Join-Path $profile 'player-plugin')
            foreach ($dependency in @('Model.dll', 'FixtureArchive.dll')) {
                Copy-Item -LiteralPath (Join-Path $pluginDir $dependency) -Destination (Join-Path $profile 'player-plugin')
            }
            if ($playerConfigPath -and (Test-Path -LiteralPath $playerConfigPath)) {
                Move-Item -LiteralPath $playerConfigPath -Destination (Join-Path $profile 'checked-player-advisor.cfg')
            }
            if ($playerConfigBackup -and (Test-Path -LiteralPath $playerConfigBackup)) { Move-Item -LiteralPath $playerConfigBackup -Destination $playerConfigPath }
        }
    } finally { $nativeWorkerLease.Dispose() }
}
