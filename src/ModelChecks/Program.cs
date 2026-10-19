using System.Collections.Concurrent;
using MonsterTrain2Poju.Model;

if (args.Length == 3 && args[0] == "--refresh-green-recruit-predictions")
{
    OriginalGreenRecruitChecks.RefreshPredictions(args[1], args[2]);
    return;
}

if (args.Length == 1 && args[0] == "--original-purge-to-exhaust-pure")
{
    OriginalPurgeToExhaustChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-purge-to-exhaust-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPurgeToExhaustChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--pyregel-on-ascend-pure")
{
    OriginalPyregelOnAscendChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--pyregel-on-ascend-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPyregelOnAscendChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 1 && args[0] == "--selection-state-pure")
{
    SelectionStateChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--combat-effects-pure")
{
    CombatEffectChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--conserve-ember-pure")
{
    ConserveEmberChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-conserve-ember-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ConserveEmberChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unstable-explosion-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    UnstableExplosionChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-nefarious-overflow-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalNefariousOverflowChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-uncontrolled-group-pure")
{
    OriginalUncontrolledGroupChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-uncontrolled-group-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalUncontrolledGroupChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-copy-first-spell-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCopyFirstSpellChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-copy-first-spell-pure")
{
    OriginalCopyFirstSpellChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-copy-first-consume-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCopyFirstConsumeChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-copy-first-consume-pure")
{
    OriginalCopyFirstConsumeChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-copy-consumed-card-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCopyConsumedCardChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-copy-consumed-card-pure")
{
    OriginalCopyConsumedCardChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-mothers-remnant3-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMothersRemnant3Checks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-mothers-remnant3-pure")
{
    OriginalMothersRemnant3Checks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--original-triboulet-pure")
{
    OriginalTribouletChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--cooldown-on-damage-pure")
{
    RelicCardDamageStatusChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--deck-turn-draw-pure")
{
    RelicDeckTurnDrawChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--card-play-damage-pure")
{
    RelicCardPlayDamageChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--consume-damage-pure")
{
    RelicConsumeDamageChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--eaten-damage-pure")
{
    RelicEatenDamageChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--more-capacity-pure")
{
    OriginalMoreCapacityChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--burnout-damage-pure")
{
    RelicBurnoutDamageChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--heal-damage-pure")
{
    RelicHealDamageChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--kill-attack-pure")
{
    OriginalKillAttackChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--random-freeze-pure")
{
    OriginalRandomFreezeChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--random-hand-upgrade-pure")
{
    OriginalRandomHandUpgradeChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--start-turn-propagation-pure")
{
    OriginalStartTurnPropagationChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--mooncycle-fae-pure")
{
    OriginalMooncycleFaeChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--random-draw-cost-pure")
{
    OriginalRandomDrawCostChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--suppress-exhaust-pure")
{
    OriginalSuppressExhaustChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--expensive-card-upgrade-pure")
{
    OriginalExpensiveCardUpgradeChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--wolos-bloodletter-pure")
{
    OriginalWolosBloodletterChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--card-type-sequence-pure")
{
    OriginalCardTypeSequenceChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--whelp-cooldown-pure")
{
    OriginalWhelpCooldownChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--spawn-retreat-pure")
{
    OriginalSpawnRetreatChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-spawn-retreat-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSpawnRetreatChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-opening-deck-spawn-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalOpeningDeckSpawnChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-perkeo-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPerkeoChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-corrupted-angels-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCorruptedAngelsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-opening-morsels-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalOpeningMorselsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--restoration-detonation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RestorationDetonationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--run-card-generation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RunCardGenerationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--armament-drive-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ArmamentDriveChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--preserve-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PreserveChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--mixalot-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MixalotChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--small-box-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    SmallBoxChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--magnetic-relay-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MagneticRelayChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--transcend-imp-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TranscendImpChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--fused-weaponry-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EquipmentMergeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-room-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRoomCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-forge-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalForgeCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-status-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStatusCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-moon-card-upgrades-only")
{
    using var moonCardBattleDocument = ModelJson.ReadFixture(args[1]);
    OriginalMoonCardUpgradesChecks.Native(moonCardBattleDocument.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--moon-card-upgrade-only")
{
    using var moonUpgradeDocument = ModelJson.ReadFixture(args[1]);
    MoonCardUpgradeChecks.Native(moonUpgradeDocument.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-discard-draw-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalDiscardDrawChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-spell-compositions-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSpellCompositionsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-positive-status-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPositiveStatusChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-scaled-moon-phase-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalScaledMoonPhaseChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-draw-upgrade-scaling-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalDrawUpgradeScalingChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-consumed-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalConsumedUpgradeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-status-compositions-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStatusCompositionsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-grafted-equipment-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalGraftedEquipmentChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-self-attack-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSelfAttackChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-healing-cleanse-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalHealingCleanseChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-on-kill-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalOnKillChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-on-kill-generation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalOnKillGenerationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-morsel-eaten-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMorselEatenChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-unit-buffs-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalUnitBuffsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-kill-copy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalKillCopyChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-kill-resources-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalKillResourcesChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-friendly-statuses-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFriendlyStatusesChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-basic-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBasicDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-resource-spells-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalResourceSpellsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-hostile-statuses-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalHostileStatusesChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--status-callback-pure")
{
    StatusCallbackChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--fuel-pure")
{
    FuelChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-fuel-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFuelChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--triggered-typed-draw-pure")
{
    TriggeredTypedDrawChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--triggered-health-pure")
{
    TriggeredHealthChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-support-units-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSupportUnitsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--attack-override-pure")
{
    AttackOverrideChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-attack-overrides-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalAttackOverridesChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--last-attacker-pure")
{
    LastAttackerTargetChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-reactive-units-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalReactiveUnitsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-base-champions-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBaseChampionsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-status-units-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStatusUnitsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-equipment-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalEquipmentCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--steelguard-pure")
{
    OriginalSupportSpellsChecks.Steelguard();
    return;
}
if (args.Length == 2 && args[0] == "--original-support-spells-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSupportSpellsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-rearrange-spells-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRearrangeSpellsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-movement-spells-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMovementSpellsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-scaled-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalScaledDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-pile-status-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPileStatusChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-scaled-buff-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalScaledBuffChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-pile-recursion-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPileRecursionChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-pile-draw-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPileDrawChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-card-draw-potion-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCardDrawPotionChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-accelerated-incubation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalAcceleratedIncubationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-tome-of-contracts-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalTomeOfContractsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-eager-conscript-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalEagerConscriptChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-copy-morsel-twice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCopyMorselTwiceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-lazarus-league-spike-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalLazarusLeagueSpikeChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-effect-applications-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalEffectApplicationsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-status-removal-scaling-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStatusRemovalScalingChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-scaled-energy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalScaledEnergyChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-specific-card-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSpecificCardDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-status-stack-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStatusStackDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-corrupt-restricted-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCorruptRestrictedChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-overflow-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalOverflowCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-permafrost-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPermafrostCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-unplayable-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalUnplayableCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-monster-hand-costs-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMonsterHandCostsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-limited-range-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalLimitedRangeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-hand-costs-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalHandCostsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-unpurgable-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalUnpurgableCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-room-status-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRoomStatusDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-corruption-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCorruptionDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-x-status-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalXStatusCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-x-banished-spike-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalXBanishedSpikeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-x-luna-spike-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalXLunaSpikeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-x-stygian-spike-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalXStygianSpikeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-x-wurmkin-spike-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalXWurmkinSpikeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-mine-collapse-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMineCollapseChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-imp-olate-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalImpOlateChecks.Native(fixture.RootElement, expectImp: true);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-imp-olate-zero-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalImpOlateChecks.Native(fixture.RootElement, expectImp: false);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-pyreshot-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPyreshotChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-retch-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRetchChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-x-damage-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalXDamageCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-strong-magic-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStrongMagicCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-consume-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalConsumeCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--inferno-room-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    InfernoRoomChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-piercing-cards-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPiercingCardsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--mushy-room-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MushyRoomChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--grow-room-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    GrowRoomChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--planetarium-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PlanetariumChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--memorial-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MemorialChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--zone-of-silence-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ZoneOfSilenceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--stewards-quarters-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    StewardsQuartersChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--engine-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EngineUpgradeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--goldmance-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    GoldmanceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-deck-spell-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalDeckSpellChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-deep-offering-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalDeepOfferingChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-molded-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMoldedChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-broken-memories-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBrokenMemoriesChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-wicked-blaze-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalWickedBlazeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-offering-token-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalOfferingTokenChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-moon-ritual-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMoonRitualChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-self-purge-energy-gain-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSelfPurgeEnergyGainChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-smelted-treasure-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSmeltedTreasureChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-pyre-gro-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPyreGroChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-deadly-variant-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalDeadlyVariantChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-drain-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalDrainChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-starter-frostbite-discard-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStarterFrostbiteDiscardChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-feeding-frenzy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFeedingFrenzyChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-titans-gratitude-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalTitansGratitudeChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-perfect-insanity-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPerfectInsanityChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-starter-reform-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStarterReformChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-rapid-raise-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRapidRaiseChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-spawning-pods-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSpawningPodsChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-minds-of-their-own-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMindsOfTheirOwnChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-metallurgy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMetallurgyChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-fertile-ground-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFertileGroundChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-cover-of-night-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCoverOfNightChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-waxen-spike-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalWaxenSpikeChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-imp-pressive-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalImpPressiveChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-tome-of-evergrowth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalTomeOfEvergrowthChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-tome-of-the-lost-path-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalTomeOfTheLostPathChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-make-it-rain-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMakeItRainChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-deathroar-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalDeathroarChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-secret-ingredient-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSecretIngredientChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-green-recruit-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalGreenRecruitChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-primordium-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPrimordiumChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-primordium-action-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPrimordiumChecks.ActionOnly(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-primordium-model-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPrimordiumChecks.ModelOnly(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-cheery-deathcap-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCheeryDeathcapChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-enthusiastic-fan-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalEnthusiasticFanChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--eclipse-chamber-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EclipseChamberChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--adaptive-mutation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AdaptiveMutationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--card-duplicate-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CardDuplicateChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--soldier-of-fortune-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    SoldierOfFortuneChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--forced-feeding-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ForcedFeedingChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-shroud-spike-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ForcedFeedingChecks.Native(fixture.RootElement, requireOriginalSource: true);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--card-backed-feeding-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CardBackedFeedingChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--decay-death-pure")
{
    OriginalDecayDeathChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-decay-death-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalDecayDeathChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--bone-dog-pure")
{
    OriginalBoneDogChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-bone-dog-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBoneDogChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--gros-michel-pure")
{
    OriginalGrosMichelChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-gros-michel-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalGrosMichelChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-whelp-cooldown-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalWhelpCooldownChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-card-type-sequence-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCardTypeSequenceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-wolos-bloodletter-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalWolosBloodletterChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-expensive-card-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalExpensiveCardUpgradeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-suppress-exhaust-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSuppressExhaustChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-random-draw-cost-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRandomDrawCostChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-mooncycle-fae-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMooncycleFaeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-start-turn-propagation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalStartTurnPropagationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-random-hand-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRandomHandUpgradeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-random-freeze-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRandomFreezeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--freeze-cost-pure")
{
    OriginalFreezeReduceCostChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-freeze-cost-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFreezeReduceCostChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--spawn-silence-pure")
{
    OriginalSpawnSilenceChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-spawn-silence-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSpawnSilenceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-kill-attack-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalKillAttackChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-heal-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicHealDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-burnout-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicBurnoutDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-more-capacity-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMoreCapacityChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-eaten-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicEatenDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-consume-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicConsumeDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-card-play-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicCardPlayDamageChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-deck-turn-draw-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicDeckTurnDrawChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-triboulet-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalTribouletChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-cooldown-on-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicCardDamageStatusChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-monster-spell-targeting-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicMonsterSpellTargetingChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-funguy-deck-pure")
{
    OriginalFunguyDeckScalingChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-funguy-deck-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFunguyDeckScalingChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-card-draw-on-heal-pure")
{
    OriginalCardDrawOnHealChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-card-draw-on-heal-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCardDrawOnHealChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-broken-rail-pure")
{
    OriginalBrokenRailChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--original-rage-on-burnout-stack-loss-pure")
{
    OriginalRageOnBurnoutStackLossChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-rage-on-burnout-stack-loss-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRageOnBurnoutStackLossChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-pyre-revenge-pure")
{
    OriginalPyreRevengeChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-pyre-revenge-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPyreRevengeChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-broken-rail-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBrokenRailChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-nefarious-overflow-pure")
{
    OriginalNefariousOverflowChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--mix-catalog-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MixCatalogChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-mix-relic-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMixRelicChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-card-type-cost-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCardTypeCostChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--relic-card-type-cost-pure")
{
    RelicCardTypeCostChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--card-type-cost-order-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicCardTypeCostOrderChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--damage-by-units-killed-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    DamageByUnitsKilledChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--room-corruption-pure")
{
    RoomCorruptionChecks.Run();
    StatisticQueryChecks.Run();
    UnitUpgradeScalingChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--moon-phase-transition-pure")
{
    MoonPhaseTransitionChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--no-player-units-status-pure")
{
    ConditionalNoPlayerUnitChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--native-turn-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleTurnChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unit-upgrade-scaling-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "unit-upgrade-scaling" ||
        !fixture.RootElement.GetProperty("NativeWon").GetBoolean() ||
        new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.Any(name => fixture.RootElement.GetProperty(name).GetInt32() != 0))
        throw new InvalidDataException("The requested fixture has no complete native unit-upgrade scaling battle.");
    UnitUpgradeScalingChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--conditional-pyre-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ConditionalPyreDamageChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 1 && args[0] == "--self-attack-status-pure")
{
    SelfAttackStatusChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--no-player-units-status-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ConditionalNoPlayerUnitChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--self-attack-status-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    SelfAttackStatusChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--moon-phase-transition-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MoonPhaseTransitionChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--moon-phase-advance-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MoonPhaseTransitionChecks.NativeAdvance(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--moon-phase-callbacks-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MoonPhaseTransitionChecks.NativeCallbacks(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--room-corruption-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomCorruptionChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--corruption-unit-trigger-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomCorruptionChecks.NativeUnitTrigger(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 2 && args[0] == "--corruption-carver-trigger-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomCorruptionChecks.NativeCarver(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 2 && args[0] == "--corruption-keeper-trigger-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomCorruptionChecks.NativeKeeper(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 2 && args[0] == "--corruption-relic-trigger-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomCorruptionChecks.NativeRelicTrigger(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 2 && args[0] == "--permanent-corruption-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomCorruptionChecks.NativePermanent(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--scorch-pure")
{
    ScorchStatusChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--scorch-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ScorchStatusChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--overheal-pure")
{
    OverhealChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--overheal-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OverhealChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--status-upgrade-pure")
{
    StatusUpgradeChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--status-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    StatusUpgradeChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--train-heal-pure")
{
    TrainHealingChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--train-heal-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrainHealingChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--battle-search-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleSearchChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--battle-frontier-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleSearchChecks.FrontierDiagnostics(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--egg-transformation-only")
{
    EggTransformationChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--original-egg-card-only")
{
    OriginalEggCardChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--original-egg-card-delayed-only")
{
    OriginalEggCardChecks.Native(args[1], delayed: true);
    return;
}

if (args.Length == 2 && args[0] == "--unplayed-card-trigger-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleTurnChecks.UnplayedCardTrigger(fixture.RootElement);
    int schema = fixture.RootElement.GetProperty("Schema").GetInt32();
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={schema} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 2 && args[0] == "--unplayed-card-effects-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleTurnChecks.UnplayedCardEffects(fixture.RootElement);
    int schema = fixture.RootElement.GetProperty("Schema").GetInt32();
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={schema} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 2 && args[0] == "--remove-all-status-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RemoveAllStatusChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 2 && args[0] == "--explain-frontier")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    var rejected = new SortedDictionary<string, int>(StringComparer.Ordinal);
    int decisions = fixture.RootElement.GetProperty("Actions").GetArrayLength();
    foreach (var entry in fixture.RootElement.GetProperty("Actions").EnumerateArray())
    {
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(before);
        foreach (UnsupportedActionAttempt attempt in frontier.UnsupportedAttempts)
        {
            PlayCardAction? action = attempt.Action;
            CardToken? card = action == null ? null : before.Spawn.Train.Context!.Cards.Hand
                .FirstOrDefault(item => item.InstanceId == action.CardInstanceId);
            CardPlayRule? rule = card == null ? null : before.PlayRules!.Cards
                .FirstOrDefault(item => item.DataId == card.DataId);
            string identity = rule?.AssetKey ?? (action?.ActivatorUnitId > 0 ? "<unit ability>" : "<frontier>");
            string key = identity + " | " + attempt.Reason;
            rejected[key] = rejected.TryGetValue(key, out int count) ? count + 1 : 1;
        }
    }
    Console.WriteLine($"ACTION-FRONTIER actionSnapshots={decisions} rejectedKinds={rejected.Count}");
    foreach (var item in rejected) Console.WriteLine($"REJECTED x{item.Value} {item.Key}");
    return;
}

if (args.Length == 1 && args[0] == "--card-cycle-pure")
{
    CardCycleChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--card-cycle-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CardCycleChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--card-spells-pure")
{
    CardSpellChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--card-recursion-pure")
{
    CardRecursionChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--card-recursion-choice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CardRecursionChecks.SearchChoices(fixture.RootElement, "ChooseToDeck");
    return;
}

if (args.Length == 2 && args[0] == "--card-recursion-hand-choice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CardRecursionChecks.SearchChoices(fixture.RootElement, "ChooseToHand");
    return;
}

if (args.Length == 2 && args[0] == "--card-recursion-filter-choice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CardRecursionChecks.SearchChoices(fixture.RootElement, "ChooseToDeck", expectedCandidateCount: 5);
    return;
}

if (args.Length == 2 && args[0] == "--card-recursion-discard-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CardRecursionChecks.SearchChoices(fixture.RootElement, "ChooseToHand", sourceTarget: "Discard");
    return;
}

if (args.Length == 2 && args[0] == "--card-recursion-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CardRecursionChecks.SearchUpgrade(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--ephemeral-card-routing-pure")
{
    CardModifierChecks.Run();
    CardCycleChecks.Run();
    RelicCardModifierChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--room-combat-only")
{
    RoomCombatChecks.NativeRoomOnly(args[1]);
    return;
}

if (args.Length == 1 && args[0] == "--attack-with-unit-pure")
{
    AttackWithUnitChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--status-per-other-effect-pure")
{
    StatusPerOtherEffectChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--titan-skin-pure")
{
    TitanSkinChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--unit-ability-pure")
{
    UnitAbilityChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--damage-per-hp-pure")
{
    DamagePerHpChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--unit-teleport-pure")
{
    UnitTeleportChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--unit-ability-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    UnitAbilityChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unit-teleport-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    UnitTeleportChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--equipment-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EquipmentChecks.Native(fixture.RootElement);
    int schema = fixture.RootElement.GetProperty("Schema").GetInt32();
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={schema} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 1 && args[0] == "--relic-floor-capacity-pure")
{
    RelicFloorCapacityChecks.Run();
    RelicResourceChecks.Run();
    RoomCapacityChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--original-floor-capacity-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalFloorCapacityRelicChecks.Native(document.RootElement, required: true, replayBattle: true);
    return;
}

if (args.Length == 2 && args[0] == "--mageblade-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    MagebladeChecks.Native(document.RootElement, required: true);
    return;
}

if (args.Length == 2 && args[0] == "--original-card-play-magic-power-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicCardPlayMagicPowerChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-two-charges-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicCardPlayCorruptionChecks.Native(document.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-two-charges-model-only")
{
    RelicCardPlayCorruptionChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-chance-for-spell-weakness-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalChanceForSpellWeaknessChecks.Native(document.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-chance-for-spell-weakness-model-only")
{
    OriginalChanceForSpellWeaknessChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-chance-to-cleanse-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalChanceToCleanseChecks.Native(document.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-chance-to-cleanse-model-only")
{
    OriginalChanceToCleanseChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-extra-feed-trigger-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalExtraFeedTriggerChecks.Native(document.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-extra-feed-trigger-model-only")
{
    OriginalExtraFeedTriggerChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-hephs-blueprints-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalHephsBlueprintsChecks.Native(document.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={document.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={document.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-hephs-blueprints-model-only")
{
    OriginalHephsBlueprintsChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-summon-with-avarice-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalSummonWithAvariceChecks.Native(document.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-summon-with-avarice-model-only")
{
    OriginalSummonWithAvariceChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-card-draw-on-card-play-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicCardDrawOnCardPlayChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-carriage-jack-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicCardPlayRoomCapacityChecks.Native(document.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-carriage-jack-model-only")
{
    RelicCardPlayRoomCapacityChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-first-room-attachment-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalFirstRoomAttachmentChecks.Native(document.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-first-room-attachment-model-only")
{
    RelicRoomAttachmentReplicationChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-start-room-attachment-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalStartRoomAttachmentChecks.Native(document.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={document.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={document.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--relic-card-draw-on-card-play-model-only")
{
    RelicCardDrawOnCardPlayChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-room-entry-status-relics-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalRoomEntryStatusRelicChecks.Native(document.RootElement, required: true);
    return;
}
if (args.Length == 2 && args[0] == "--original-moon-phase-energy-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalMoonPhaseEnergyRelicChecks.Native(document.RootElement);
    Console.WriteLine($"NATIVE-MOON-PHASE-FIXTURE bytes={new FileInfo(args[1]).Length} uniqueNodes={document.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-moon-phase-heal-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalMoonPhaseHealRelicChecks.Native(document.RootElement);
    Console.WriteLine($"NATIVE-MOON-PHASE-HEAL-FIXTURE bytes={new FileInfo(args[1]).Length} uniqueNodes={document.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-moon-phase-status-pure")
{
    OriginalMoonPhaseStatusChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-moon-phase-status-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMoonPhaseStatusChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-upgrade-count-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicUpgradeCountChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-healing-power-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalHealingPowerChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-start-combat-energy-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalStartCombatEnergyChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-start-combat-hoard-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalStartCombatHoardChecks.Native(document.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={document.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={document.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-start-combat-hoard-pure")
{
    OriginalStartCombatHoardChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-forge-conservation-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicForgeConservationChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-opening-corruption-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicOpeningCorruptionChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--campaign-relics-only")
{
    CampaignRelicChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--original-spell-play-thresholds-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalSpellPlayThresholdChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-conduit-relics-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalConduitChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-equipment-growth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalEquipmentGrowthChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--campaign-drafts-only")
{
    CampaignDraftChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--campaign-post-battle-only")
{
    CampaignPostBattleChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--campaign-battle-resources-only")
{
    CampaignBattleResourceChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--original-health-choice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalHealthChoiceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-after-draw-generation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalAfterDrawGenerationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-add-snack-on-kill-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalAddSnackOnKillChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-final-ending-artifact-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFinalEndingArtifactChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-balatro-event-yorick-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBalatroEventYorickChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-balatro-event-cavendish-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBalatroEventCavendishChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-garfield-box-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalGarfieldBoxRandomCardChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-part-factory-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPartFactoryChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-big-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBigDamageFromManyMorselsChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-big-damage-replay-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalBigDamageFromManyMorselsChecks.Replay(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-early-discard-energy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalEarlyDiscardEnergyChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-early-discard-energy-pure")
{
    OriginalEarlyDiscardEnergyChecks.Run();
    HandRemovalChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--original-extract-for-ember-and-echos-pure")
{
    OriginalExtractForEmberAndEchosChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-extract-for-ember-and-echos-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalExtractForEmberAndEchosChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-cheaper-spell-cost-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalCheaperSpellCostChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-cheaper-spell-cost-pure")
{
    OriginalCheaperSpellCostChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--original-moon-phase-hand-upgrade-pure")
{
    OriginalMoonPhaseRoomsGainMagicPowerChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-moon-phase-hand-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalMoonPhaseRoomsGainMagicPowerChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--original-propagate-on-status-effect-applied-pure")
{
    OriginalPropagateOnStatusEffectAppliedChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-propagate-on-status-effect-applied-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalPropagateOnStatusEffectAppliedChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--relic-first-card-cost-pure")
{
    RelicFirstCardCostChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--relic-pyre-card-cost-pure")
{
    RelicPayCardCostWithPyreHealthChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-pyre-health-card-cost-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicPayCardCostWithPyreHealthChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-first-card-cost-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicFirstCardCostChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 1 && args[0] == "--original-rebalance-valor-pure")
{
    OriginalRebalanceValorChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-rebalance-valor-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalRebalanceValorChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-unit-resources-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalUnitResourceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-opening-draw-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalOpeningDrawChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--campaign-purge-gold-only")
{
    CampaignPurgeGoldChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--campaign-draft-enhancer-count-only")
{
    CampaignDraftEnhancerCountChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--campaign-deck-copy-only")
{
    CampaignDeckCopyChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--original-null-relic-only")
{
    OriginalNullRelicChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--original-turn-draw-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalTurnDrawChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-unit-phase-upgrades-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalUnitPhaseUpgradeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-solo-unit-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalSoloUnitUpgradeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-front-room-health-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFrontRoomHealthUpgradeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-champion-start-turn-heal-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalChampionStartTurnHealChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-nims-oath-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalNimsOathChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-judges-scales-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalJudgesScalesChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-no-upgrade-card-cost-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalNoUpgradeCardCostChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}
if (args.Length == 2 && args[0] == "--original-combat-trigger-relics-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalCombatTriggerBatchChecks.Native(document.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--campaign-relics-pure")
{
    CampaignRelicChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--original-more-spikes-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalMoreSpikesChecks.Native(document.RootElement);
    BattleTurnChecks.Native(document.RootElement);
    BattleActionChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-stealth-slay-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalStealthSlayChecks.Native(document.RootElement);
    BattleTurnChecks.Native(document.RootElement);
    BattleActionChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-relic-count-attack-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalRelicCountAttackChecks.Native(document.RootElement);
    BattleTurnChecks.Native(document.RootElement);
    BattleActionChecks.Native(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-card-attack-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicCardAttackChecks.Native(document.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--mageblade-pure")
{
    BattleActionChecks.MagebladeStartingStatusSummon();
    StatusAttackDamageChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--relic-magic-power-pure")
{
    RelicMagicPowerChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--relic-card-play-magic-power-pure")
{
    RelicCardPlayMagicPowerChecks.Run();
    RelicCardDrawOnCardPlayChecks.Run();
    RelicCardPlayRoomCapacityChecks.Run();
    RelicRoomAttachmentReplicationChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--relic-card-attack-pure")
{
    RelicCardAttackChecks.Run();
    RelicUpgradeCountChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--relic-room-entry-status-pure")
{
    RelicRoomEntryStatusChecks.Run();
    return;
}
if (args.Length == 1 && args[0] == "--spell-weakness-pure")
{
    SpellWeaknessChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--gold-pyre-pure")
{
    GoldPyreChecks.Run();
    PyreGoldSpellChecks.Run();
    RelicResourceChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--pyre-resource-refresh-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    PyreResourceRefreshChecks.Native(document.RootElement, required: true, replayBattle: true);
    return;
}

if (args.Length == 1 && args[0] == "--relic-resources-pure")
{
    RelicResourceChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--original-resource-relics-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalResourceRelicBatchChecks.Native(document.RootElement, required: true, replayBattle: true);
    return;
}

if (args.Length == 1 && args[0] == "--relic-status-decay-pure")
{
    RelicStatusDecayChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--original-decay-relics-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalDecayRelicBatchChecks.Native(document.RootElement, required: true, replayBattle: true);
    return;
}

if (args.Length == 1 && args[0] == "--relic-lifecycle-pure")
{
    RelicCharacterTriggerChecks.Run();
    RevivalChecks.Run();
    UnitAbilityChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--relic-unit-upgrade-triggers-pure")
{
    RelicUnitUpgradeTriggerChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--relic-status-magnitudes-pure")
{
    RelicStatusMagnitudeChecks.Run();
    StatusAttackDamageChecks.Run();
    return;
}

if (args.Length == 1 && args[0] == "--relic-status-stacks-pure")
{
    RelicStatusStackChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--original-magnitude-relics-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalMagnitudeRelicBatchChecks.Native(document.RootElement, required: true, replayBattle: true);
    return;
}

if (args.Length == 2 && args[0] == "--original-lifecycle-relics-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalLifecycleRelicBatchChecks.Native(document.RootElement, required: true, replayBattle: true);
    return;
}

if (args.Length == 2 && args[0] == "--original-status-relics-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalStatusRelicBatchChecks.Native(document.RootElement, required: true);
    return;
}

if (args.Length == 3 && args[0] == "--original-trigger-relics-combined")
{
    using var deaths = ModelJson.ReadFixture(args[1]);
    using var shifts = ModelJson.ReadFixture(args[2]);
    OriginalTriggerRelicBatchChecks.Combined(deaths.RootElement, shifts.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--original-trigger-relics-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    OriginalTriggerRelicBatchChecks.Native(document.RootElement, required: true);
    return;
}

if (args.Length == 1 && args[0] == "--relic-triggers-pure")
{
    RelicCharacterTriggerChecks.Run();
    RelicSpawnStatusChecks.Run();
    TriggeredStatusTransferChecks.Run();
    RelicCardlessUpgradeChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--original-unit-relics-only")
{
    using var relicDocument = ModelJson.ReadFixture(args[1]);
    OriginalUnitRelicBatchChecks.Native(relicDocument.RootElement, required: true);
    return;
}

if (args.Length == 1 && args[0] == "--typed-draw-pure")
{
    TypedDrawChecks.Run();
    CardMagicPowerChecks.Run();
    CardExhaustChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--typed-draw-only")
{
    using var typedDocument = ModelJson.ReadFixture(args[1]);
    TypedDrawNativeChecks.Native(typedDocument.RootElement, required: true);
    return;
}

if (args.Length == 1 && args[0] == "--sacrifice-pure")
{
    SacrificeChecks.Run();
    Console.WriteLine("SACRIFICE-PURE-CHECKS PASS");
    return;
}
if (args.Length == 2 && args[0] == "--native-actions-only")
{
    using var actionDocument = ModelJson.ReadFixture(args[1]);
    BattleActionChecks.Native(actionDocument.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--choose-discard-only")
{
    using var choiceDocument = ModelJson.ReadFixture(args[1]);
    ChooseDiscardChecks.Native(choiceDocument.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-smelt-ember-only")
{
    using var smeltDocument = ModelJson.ReadFixture(args[1]);
    ChooseDiscardChecks.NativeSmeltRelic(smeltDocument.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--choose-discard-others-only")
{
    using var choiceDocument = ModelJson.ReadFixture(args[1]);
    ChooseDiscardChecks.NativeOthers(choiceDocument.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--forge-points-only")
{
    using var forgeDocument = ModelJson.ReadFixture(args[1]);
    ForgePointsChecks.Native(forgeDocument.RootElement, required: true);
    return;
}
if (args.Length == 2 && args[0] == "--relic-card-modifiers-only")
{
    using var relicDocument = ModelJson.ReadFixture(args[1]);
    RelicCardModifierChecks.Native(relicDocument.RootElement, required: true);
    return;
}
if (args.Length == 2 && args[0] == "--relic-card-upgrades-only")
{
    RelicCardUpgradeChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--branch-card-masks-only")
{
    using var branchDocument = ModelJson.ReadFixture(args[1]);
    CardBranchMaskChecks.Native(branchDocument.RootElement, required: true);
    return;
}
if (args.Length == 1 && args[0] == "--card-upgrade-lifecycle-pure")
{
    CardUpgradeLifecycleChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--card-upgrade-lifecycle-only")
{
    CardUpgradeLifecycleChecks.Native(args[1]);
    return;
}
if (args.Length == 1 && args[0] == "--owned-card-mask-pure")
{
    CardOwnedMaskChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--owned-card-mask-only")
{
    CardOwnedMaskChecks.Native(args[1]);
    return;
}
if (args.Length == 1 && args[0] == "--card-status-composition-pure")
{
    CardStatusCompositionChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--card-status-composition-only")
{
    CardStatusCompositionChecks.Native(args[1]);
    return;
}
if (args.Length == 1 && args[0] == "--trait-composition-pure")
{
    CardTraitCompositionChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--trait-composition-only")
{
    CardTraitCompositionChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--modifier-overflow-only")
{
    CardModifierOverflowChecks.Native(args[1]);
    return;
}

if (args.Length == 1 && args[0] == "--upgrade-mask-pure")
{
    CardUpgradeMaskChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--upgrade-mask-only")
{
    CardUpgradeMaskChecks.Run();
    CardUpgradeMaskChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--physical-decision-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    var physicalRoot = document.RootElement;
    if (physicalRoot.GetProperty("CaptureFailures").GetInt32() != 0 || physicalRoot.GetProperty("Pending").GetInt32() != 0 ||
        physicalRoot.GetProperty("DecisionSpawnPoints").GetArrayLength() == 0)
        throw new InvalidDataException("Physical decision mappings are incomplete.");
    BattleSpawnPointChecks.DecisionReferences(physicalRoot);
    return;
}
if (args.Length == 2 && args[0] == "--physical-plane-only")
{
    PhysicalPlaneChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--preview-birth-only")
{
    PreviewBirthChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--preview-reference-only")
{
    PreviewReferenceChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--enchantment-summon-only")
{
    EnchantmentSummonChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--character-removal-only")
{
    CharacterRemovalChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--enchantment-source-order-only")
{
    EnchantmentSourceOrderChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--enchantment-world-only")
{
    EnchantmentWorldChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--enchantment-combat-only")
{
    EnchantmentCombatChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--enchantment-lifecycle-only")
{
    EnchantmentLifecycleChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--hero-copy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "hero-copy" ||
        fixture.RootElement.GetProperty("HeroCopyOperations").GetArrayLength() == 0)
        throw new InvalidDataException("The requested fixture has no hero-copy observations.");
    HeroCopyChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unit-copy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "unit-copy" ||
        fixture.RootElement.GetProperty("UnitCopyOperations").GetArrayLength() == 0)
        throw new InvalidDataException("The requested fixture has no paid unit-copy observations.");
    UnitCopyChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unit-clone-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() is not ("unit-clone" or "spawn-status-relics-clones" or "spawn-status-relics-mixed-cardless-order" or "spawn-status-relics-team-wide") ||
        fixture.RootElement.GetProperty("UnitCloneOperations").GetArrayLength() == 0)
        throw new InvalidDataException("The requested fixture has no ordinary clone observations.");
    UnitCloneChecks.Native(fixture.RootElement);
    RelicSpawnStatusChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--bump-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "bump" ||
        fixture.RootElement.GetProperty("BumpOperations").GetArrayLength() == 0)
        throw new InvalidDataException("The requested fixture has no native Bump observations.");
    BumpChecks.Native(fixture.RootElement);
    RelicFloorTransitionDamageChecks.Native(fixture.RootElement);
    OriginalPyregelOnAscendChecks.Native(fixture.RootElement);
    BattleActionChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--ability-incant-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "ability-incant" ||
        fixture.RootElement.GetProperty("CaptureFailures").GetInt32() != 0 || fixture.RootElement.GetProperty("Pending").GetInt32() != 0)
        throw new InvalidDataException("The requested fixture has no complete native relic ability Incant observations.");
    RelicChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--relic-spawn-status-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (fixture.RootElement.GetProperty("ModifierScenario").GetString() is not ("spawn-status-relics" or "spawn-status-relics-clones" or "spawn-status-relics-mixed-cardless-order" or "spawn-status-relics-stealth-waxers" or "spawn-status-relics-team-wide" or "spawn-status-relics-tuned-gear" or "spawn-status-relics-egg-hatch" or "spawn-status-relics-boss-mute" or "spawn-status-relics-combat"))
        throw new InvalidDataException("The requested fixture has no native relic spawn status observations.");
    RelicSpawnStatusChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-first-imp-bonus-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFirstImpBonusChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--original-first-imp-bonus-diagnose")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalFirstImpBonusChecks.Diagnose(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--covenant-character-filter-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CovenantCharacterFilterChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--incant-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    var observed = fixture.RootElement;
    if (observed.GetProperty("ModifierScenario").GetString() is not ("incant" or "incant-thresholds" or "incant-relic" or "incant-relic-combined") ||
        observed.GetProperty("CaptureFailures").GetInt32() != 0 || observed.GetProperty("Pending").GetInt32() != 0)
        throw new InvalidDataException("The requested fixture has no complete native Incant observations.");
    IncantChecks.Native(observed);
    TriggerCountChecks.Native(observed);
    return;
}

const string steward = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
FixtureArchiveChecks.Run();
var input = new CombatProjectionData
{
    Scenario = "Level1BattleJunker",
    Turn = 0,
    Energy = 4,
    PyreHealth = 80,
    PyreMaxHealth = 80,
    GameplayRng = "fixed-seed",
    Hand = [new CardToken(1, steward), new CardToken(2, steward)],
    Draw = [new CardToken(3, "other")],
    DiscardBuffer = [new CardToken(4, "buffered")],
    Monsters = [new UnitToken(10, "PyreHeartStarter", 3, 0, 45, 80, 80, 0, "")],
    Rooms =
    [
        new RoomToken(0, 0, 5, 0, 0, 5, 0, 0),
        new RoomToken(3, 0, 5, 0, 0, 5, 0, 0)
    ]
};
var root = new CombatProjection(input);
input.Hand.Clear();
Check(root.Hand.Count == 2, "Capture did not isolate the caller's list.");

var errors = new ConcurrentQueue<string>();
Parallel.For(0, 32, index =>
{
    int handIndex = index % 2;
    int playedId = handIndex + 1;
    ModelStep step = SimpleUnitPlayModel.Apply(root,
        new SimpleUnitPlay(handIndex, playedId, 0, 1, 3, "TrainStewardBig", 8, 25));
    CombatProjection? branch = step.State;
    if (branch == null || branch.Energy != 3 || branch.Hand.Count != 1 ||
        branch.Hand[0].InstanceId == playedId || branch.Draw[0].InstanceId != 3 ||
        branch.DiscardBuffer.Count != 1 || branch.DiscardBuffer[0].InstanceId != 4 ||
        branch.Monsters.Count != 2 || branch.Monsters[1].InstanceId != 11 ||
        branch.Monsters[1].Health != 25 || branch.Rooms[0].MonsterCapacity != 3 ||
        branch.Rooms[0].MonsterNextSpawn != 0 || branch.GameplayRng != "fixed-seed")
    {
        errors.Enqueue("Branch " + index + " differed from the expected isolated transition.");
    }
});
Check(errors.IsEmpty, string.Join(" ", errors));
Check(root.Hand.Count == 2 && root.Energy == 4 && root.Monsters.Count == 1 &&
    root.Rooms[0].MonsterCapacity == 0, "A branch mutated its parent.");
Check(!SimpleUnitPlayModel.Apply(root,
    new SimpleUnitPlay(0, 2, 0, 1, 3, "TrainStewardBig", 8, 25)).Supported,
    "A different identical card instance was accepted.");
var lowEnergy = new CombatProjection(new CombatProjectionData
{
    Scenario = "Level1BattleJunker",
    Turn = 0,
    Energy = 0,
    Hand = [new CardToken(1, steward)],
    Rooms = [new RoomToken(0, 0, 5, 0, 0, 5, 0, 0),
        new RoomToken(3, 0, 5, 0, 0, 5, 0, 0)]
});
Check(!SimpleUnitPlayModel.Apply(lowEnergy,
    new SimpleUnitPlay(0, 1, 0, 1, 3, "TrainStewardBig", 8, 25)).Supported,
    "An unaffordable action was accepted.");
var unknownCard = new CombatProjection(new CombatProjectionData
{
    Scenario = "Level1BattleJunker",
    Turn = 0,
    Energy = 4,
    Hand = [new CardToken(5, "unknown-card")],
    Rooms = [new RoomToken(0, 0, 5, 0, 0, 5, 0, 0),
        new RoomToken(3, 0, 5, 0, 0, 5, 0, 0)]
});
Check(!SimpleUnitPlayModel.Apply(unknownCard,
    new SimpleUnitPlay(0, 5, 0, 1, 3, "TrainStewardBig", 8, 25)).Supported,
    "An unknown card was accepted as a simple unit play.");
Console.WriteLine("MODEL-CHECKS PASS: independent branches, card identity, and unsupported actions.");
CardCycleChecks.Run();
BonusDrawChecks.Run();
RoomCapacityChecks.Run();
CombatEffectChecks.Run();
StatusRegistryChecks.Run();
StatusCallbackChecks.Run();
PurifyChecks.Run();
IncantChecks.Run();
PersistentEnchantmentChecks.Run();
PreviewCopyChecks.Run();
ContextReferenceChecks.Run();
UnitUpgradeCallbackChecks.Run();
RetainedCallbackChecks.Run();
ConditionalTriggerChecks.Run();
TriggerRepeatChecks.Run();
CompanionBossChecks.Run();
SentryChecks.Run();
AbilityCooldownChecks.Run();
AbilityLifecycleChecks.Run();
AbilityEffectChecks.Run();
AbilityUpgradeChecks.Run();
HordeStatChecks.Run();
HordeStatusChecks.Run();
HordeMergeChecks.Run();
HarvestChecks.Run();
RevivalChecks.Run();
HordeRemovalChecks.Run();
HordeDeathChecks.Run();
AbilityCardChecks.Run();
StatusUpgradeChecks.Run();
OverhealChecks.Run();
RoomCorruptionChecks.Run();
MoonPhaseTransitionChecks.Run();
OriginalMoonPhaseStatusChecks.Run();
ScorchStatusChecks.Run();
UnitAbilityChecks.Run();
RelicChecks.Run();
TriggerCountChecks.Run();
RelicCardlessUpgradeChecks.Run();
RelicSpawnStatusChecks.Run();
RelicCharacterTriggerChecks.Run();
TriggeredStatusTransferChecks.Run();
RelicUnitUpgradeTriggerChecks.Run();
CovenantCharacterFilterChecks.Run();
RelicCardModifierChecks.Run();
RelicCardTypeCostChecks.Run();
ConserveEmberChecks.Run();
    RelicCardAttackChecks.Run();
RelicUpgradeCountChecks.Run();
RelicMagicPowerChecks.Run();
RelicCardPlayMagicPowerChecks.Run();
RelicCardDrawOnCardPlayChecks.Run();
RelicCardPlayRoomCapacityChecks.Run();
RelicRoomAttachmentReplicationChecks.Run();
RelicStatusStackChecks.Run();
OriginalRageOnBurnoutStackLossChecks.Run();
OriginalPyreRevengeChecks.Run();
RelicStatusMagnitudeChecks.Run();
TitanSkinChecks.Run();
DamageOverflowChecks.Run();
RelicStatusDecayChecks.Run();
RelicResourceChecks.Run();
OriginalEarlyDiscardEnergyChecks.Run();
OriginalCheaperSpellCostChecks.Run();
OriginalMoonPhaseRoomsGainMagicPowerChecks.Run();
OriginalPropagateOnStatusEffectAppliedChecks.Run();
RelicFirstCardCostChecks.Run();
RelicPayCardCostWithPyreHealthChecks.Run();
OriginalNefariousOverflowChecks.Run();
OriginalUncontrolledGroupChecks.Run();
OriginalCopyFirstSpellChecks.Run();
OriginalCopyFirstConsumeChecks.Run();
OriginalCopyConsumedCardChecks.Run();
OriginalMothersRemnant3Checks.Run();
OriginalTribouletChecks.Run();
RelicCardDamageStatusChecks.Run();
RelicDeckTurnDrawChecks.Run();
RelicCardPlayDamageChecks.Run();
RelicConsumeDamageChecks.Run();
RelicEatenDamageChecks.Run();
OriginalMoreCapacityChecks.Run();
RelicBurnoutDamageChecks.Run();
RelicHealDamageChecks.Run();
OriginalKillAttackChecks.Run();
OriginalSpawnSilenceChecks.Run();
OriginalFreezeReduceCostChecks.Run();
OriginalRandomFreezeChecks.Run();
OriginalRandomHandUpgradeChecks.Run();
OriginalStartTurnPropagationChecks.Run();
OriginalMooncycleFaeChecks.Run();
OriginalRandomDrawCostChecks.Run();
OriginalSuppressExhaustChecks.Run();
OriginalExpensiveCardUpgradeChecks.Run();
OriginalWolosBloodletterChecks.Run();
OriginalCardTypeSequenceChecks.Run();
OriginalWhelpCooldownChecks.Run();
OriginalSpawnRetreatChecks.Run();
OriginalGrosMichelChecks.Run();
OriginalBoneDogChecks.Run();
OriginalDecayDeathChecks.Run();
OriginalOpeningMorselsChecks.Run();
OriginalOpeningDeckSpawnChecks.Run();
RelicMonsterSpellTargetingChecks.Run();
RelicFloorTransitionDamageChecks.Run();
OriginalCardDrawOnHealChecks.Run();
OriginalBrokenRailChecks.Run();
RelicFloorCapacityChecks.Run();
StatusAttackDamageChecks.Run();
TypedDrawChecks.Run();
CardMagicPowerChecks.Run();
CardExhaustChecks.Run();
CardUpgradeMaskChecks.Run();
CardTraitCompositionChecks.Run();
CardStatusCompositionChecks.Run();
CardOwnedMaskChecks.Run();
CardUpgradeLifecycleChecks.Run();
BattleStatisticsChecks.Run();
StatisticOverflowChecks.Run();
StatisticZeroIncrementChecks.Run();
StatisticCacheChecks.Run();
StatisticQueryChecks.Run();
DamageScalingChecks.Run();
StatusScalingChecks.Run();
DynamicStatisticChecks.Run();
EnergyChecks.Run();
ForgePointsChecks.Run();
CardCostChecks.Run();
CardModifierChecks.Run();
TerminalSpellChecks.Run();
UnitModifierChecks.Run();
UnitUpgradeScalingChecks.Run();
UnitTriggerUpgradeChecks.Run();
SpawnTriggerChecks.Run();
UnitTurnBeginChecks.Run();
TeamTurnBeginChecks.Run();
PreHandDiscardChecks.Run();
PreCombatChecks.Run();
TriggeredHealingChecks.Run();
PostCombatHealingChecks.Run();
TriggeredDamageChecks.Run();
DamageDeathQueueChecks.Run();
TerminalDeathChecks.Run();
HitKillChecks.Run();
DyingUpgradeChecks.Run();
AttackTriggerChecks.Run();
TriggeredStatusChecks.Run();
ConditionalNoPlayerUnitChecks.Run();
SelfAttackStatusChecks.Run();
HandUpgradeChecks.Run();
HealingChecks.Run();
GoldRewardChecks.Run();
CampaignRelicChecks.Run();
HealingTriggerChecks.Run();
TriggerUpgradeChecks.Run();
EnemySpawningChecks.Run();
BattleActionChecks.Run();
SelectionStateChecks.Run();
EndlessReturnChecks.Run();
DecisionReferenceChecks.Run();
UnitIdentityChecks.Run();
CardSpellChecks.Run();
CardRecursionChecks.Run();
AttackWithUnitChecks.Run();
SacrificeChecks.Run();
RoomSpellChecks.Run();
StatusPerOtherEffectChecks.Run();
RandomSpellChecks.Run();
RandomStatusChecks.Run();
TrainSpellChecks.Run();
CrossRoomSpellChecks.Run();
UnitAttackChecks.Run();
UnitHealthChecks.Run();
NumericRangeChecks.Run();
TargetFilterChecks.Run();
DrawSpellChecks.Run();
HandRemovalChecks.Run();
RandomDiscardChecks.Run();
SharedPileChecks.Run();
CardGenerationChecks.Run();
CloneUpgradeRefreshChecks.Run();
TrainCombatChecks.Run();
RoomCombatChecks.Run(args.Where(path => !path.Contains("calibration", StringComparison.OrdinalIgnoreCase)).ToArray());
CorruptPoisonChecks.Run();
foreach (string path in args.Where(path => path.Contains("card-modifier-calibration", StringComparison.OrdinalIgnoreCase)))
    CardModifierChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("mageblade", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    MagebladeChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-card-play-magic-power", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    RelicCardPlayMagicPowerChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-two-charges-on-card-consumed", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    RelicCardPlayCorruptionChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-balatro-event-yorick", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalBalatroEventYorickChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-balatro-event-cavendish", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalBalatroEventCavendishChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-card-draw-on-card-play", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    RelicCardDrawOnCardPlayChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-carriage-jack", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    RelicCardPlayRoomCapacityChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-first-room-attachment", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalFirstRoomAttachmentChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-start-room-attachment", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalStartRoomAttachmentChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-room-entry-status-relics", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalRoomEntryStatusRelicChecks.Native(document.RootElement, required: true);
}
foreach (string path in args.Where(path => path.Contains("original-moon-phase-energy-relic", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalMoonPhaseEnergyRelicChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-moon-phase-heal-relic", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalMoonPhaseHealRelicChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("original-moon-phase-status-relic", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalMoonPhaseStatusChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("card-upgrade-mask-calibration", StringComparison.OrdinalIgnoreCase)))
    CardUpgradeMaskChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("card-modifier-overflow-calibration", StringComparison.OrdinalIgnoreCase)))
    CardModifierOverflowChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("card-trait-composition-calibration", StringComparison.OrdinalIgnoreCase)))
    CardTraitCompositionChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("card-status-composition-calibration", StringComparison.OrdinalIgnoreCase)))
    CardStatusCompositionChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("card-owned-mask-calibration", StringComparison.OrdinalIgnoreCase)))
    CardOwnedMaskChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("card-upgrade-lifecycle-calibration", StringComparison.OrdinalIgnoreCase)))
    CardUpgradeLifecycleChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("moon-card-upgrade-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var moonUpgradeDocument = ModelJson.ReadFixture(path);
    MoonCardUpgradeChecks.Native(moonUpgradeDocument.RootElement);
}
foreach (string path in args.Where(path => path.Contains("relic-card-upgrade-calibration", StringComparison.OrdinalIgnoreCase)))
    RelicCardUpgradeChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("rng-calibration", StringComparison.OrdinalIgnoreCase)))
    RngChecks.Run(path);
foreach (string path in args.Where(path => path.Contains("gold-reward-calibration", StringComparison.OrdinalIgnoreCase)))
    GoldRewardChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("campaign-relic-calibration", StringComparison.OrdinalIgnoreCase)))
    CampaignRelicChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("campaign-draft-calibration", StringComparison.OrdinalIgnoreCase)))
    CampaignDraftChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("campaign-post-battle-calibration", StringComparison.OrdinalIgnoreCase)))
    CampaignPostBattleChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("campaign-battle-resource-calibration", StringComparison.OrdinalIgnoreCase)))
    CampaignBattleResourceChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("campaign-purge-gold-calibration", StringComparison.OrdinalIgnoreCase)))
    CampaignPurgeGoldChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("campaign-deck-copy-calibration", StringComparison.OrdinalIgnoreCase)))
    CampaignDeckCopyChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("campaign-draft-enhancer-count-calibration", StringComparison.OrdinalIgnoreCase)))
    CampaignDraftEnhancerCountChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("null-relic-equivalence-calibration", StringComparison.OrdinalIgnoreCase)))
    OriginalNullRelicChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("standby-routing-calibration", StringComparison.OrdinalIgnoreCase)))
    StandbyPileChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("ui-rng-isolation-calibration", StringComparison.OrdinalIgnoreCase)))
    UiRngIsolationChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("statistic-query-calibration", StringComparison.OrdinalIgnoreCase)))
    StatisticQueryChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("statistic-overflow-calibration", StringComparison.OrdinalIgnoreCase)))
    StatisticOverflowChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("statistic-zero-increment-calibration", StringComparison.OrdinalIgnoreCase)))
    StatisticZeroIncrementChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("horde-stat-calibration", StringComparison.OrdinalIgnoreCase)))
    HordeStatChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-lifecycle-calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentLifecycleChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-combat-calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentCombatChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-world-calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentWorldChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-source-order-calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentSourceOrderChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("character-removal-calibration", StringComparison.OrdinalIgnoreCase)))
    CharacterRemovalChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("enchantment-summon-", StringComparison.OrdinalIgnoreCase) &&
    path.Contains("calibration", StringComparison.OrdinalIgnoreCase)))
    EnchantmentSummonChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("preview-reference-calibration", StringComparison.OrdinalIgnoreCase)))
    PreviewReferenceChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("preview-birth-", StringComparison.OrdinalIgnoreCase) &&
    path.Contains("calibration", StringComparison.OrdinalIgnoreCase)))
    PreviewBirthChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("physical-plane-calibration", StringComparison.OrdinalIgnoreCase)))
    PhysicalPlaneChecks.Native(path);

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
