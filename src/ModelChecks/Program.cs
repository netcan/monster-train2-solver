using System.Collections.Concurrent;
using MonsterTrain2Poju.Model;

if (args.Length == 2 && args[0] == "--unstable-explosion-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    UnstableExplosionChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--mix-catalog-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MixCatalogChecks.Native(fixture.RootElement);
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
if (args.Length == 2 && args[0] == "--original-card-draw-on-card-play-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RelicCardDrawOnCardPlayChecks.Native(document.RootElement);
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
    return;
}
if (args.Length == 1 && args[0] == "--relic-card-attack-pure")
{
    RelicCardAttackChecks.Run();
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
ScorchStatusChecks.Run();
UnitAbilityChecks.Run();
RelicChecks.Run();
TriggerCountChecks.Run();
RelicCardlessUpgradeChecks.Run();
RelicSpawnStatusChecks.Run();
RelicCharacterTriggerChecks.Run();
RelicUnitUpgradeTriggerChecks.Run();
CovenantCharacterFilterChecks.Run();
RelicCardModifierChecks.Run();
    RelicCardAttackChecks.Run();
RelicMagicPowerChecks.Run();
RelicCardPlayMagicPowerChecks.Run();
RelicCardDrawOnCardPlayChecks.Run();
RelicStatusStackChecks.Run();
RelicStatusMagnitudeChecks.Run();
TitanSkinChecks.Run();
DamageOverflowChecks.Run();
RelicStatusDecayChecks.Run();
RelicResourceChecks.Run();
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
HealingTriggerChecks.Run();
TriggerUpgradeChecks.Run();
EnemySpawningChecks.Run();
BattleActionChecks.Run();
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
foreach (string path in args.Where(path => path.Contains("original-card-draw-on-card-play", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    RelicCardDrawOnCardPlayChecks.Native(document.RootElement);
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
foreach (string path in args.Where(path => path.Contains("relic-card-upgrade-calibration", StringComparison.OrdinalIgnoreCase)))
    RelicCardUpgradeChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("rng-calibration", StringComparison.OrdinalIgnoreCase)))
    RngChecks.Run(path);
foreach (string path in args.Where(path => path.Contains("gold-reward-calibration", StringComparison.OrdinalIgnoreCase)))
    GoldRewardChecks.Native(path);
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
