using System.Collections.Concurrent;
using MonsterTrain2Poju.Model;
if (args.Length is 2 or 3 && args[0] == "--combat-victory-statistics-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    CombatVictoryStatisticsChecks.Native(fixture.RootElement, replay?.RootElement);
    RoomCombatChecks.NativeRoomOnly(args[1]);
    return;
}
if (args.Length == 3 && args[0] == "--uninjured-pending-boss-only")
{
    using var early = ModelJson.ReadFixture(args[1]);
    using var late = ModelJson.ReadFixture(args[2]);
    UninjuredPendingBossChecks.Run(early.RootElement, late.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--inherent-juice-curse-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    InherentJuiceCurseChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--inherent-juice-spike-only")
{
    using var refusal = ModelJson.ReadFixture(args[1]);
    using var source = ModelJson.ReadFixture(args[2]);
    using var replay = ModelJson.ReadFixture(args[3]);
    InherentJuiceSpikeChecks.Native(refusal.RootElement, source.RootElement, replay.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--unbox-moon-relic-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    UnboxMoonRelicChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--room-upgrade-provenance-only")
{
    using var counterexample = ModelJson.ReadFixture(args[1]);
    using var repaired = ModelJson.ReadFixture(args[2]);
    RoomUpgradeProvenanceChecks.Native(counterexample.RootElement, repaired.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--double-juice-curse-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    DoubleJuiceCurseChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 3 or 5 && args[0] == "--fragile-moon-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 5 ? ModelJson.ReadFixture(args[3]) : null;
    using var replayBirth = args.Length == 5 ? ModelJson.ReadFixture(args[4]) : null;
    FragileMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay?.RootElement, replayBirth?.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--double-juice-spike-only")
{
    using var refusal = ModelJson.ReadFixture(args[1]);
    using var source = ModelJson.ReadFixture(args[2]);
    using var replay = ModelJson.ReadFixture(args[3]);
    DoubleJuiceSpikeChecks.Native(refusal.RootElement, source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--triple-juice-curse-cast-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    TripleJuiceCurseCastChecks.Native(source.RootElement, replay.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--triple-juice-curse-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TripleJuiceCurseChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--triple-juice-search-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    TripleJuiceSearchChecks.Native(source.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--triple-juice-spike-only")
{
    using var refused = ModelJson.ReadFixture(args[1]);
    using var source = ModelJson.ReadFixture(args[2]);
    using var replay = ModelJson.ReadFixture(args[3]);
    TripleJuiceSpikeChecks.Native(refused.RootElement, source.RootElement, replay.RootElement);
    return;
}
if (args.Length is 2 or 4 && args[0] == "--stasis-shatter-only")
{
    using var battle = ModelJson.ReadFixture(args[1]);
    using var source = args.Length == 4 ? ModelJson.ReadFixture(args[2]) : null;
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    StasisShatterChecks.Native(battle.RootElement, source?.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 4 && args[0] == "--untouchable-front-relic-only")
{
    using var battle = ModelJson.ReadFixture(args[1]);
    using var source = args.Length == 4 ? ModelJson.ReadFixture(args[2]) : null;
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    UntouchableFrontRelicChecks.Native(battle.RootElement, source?.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 4 && args[0] == "--untouchable-room-attachment-only")
{
    using var battle = ModelJson.ReadFixture(args[1]);
    using var source = args.Length == 4 ? ModelJson.ReadFixture(args[2]) : null;
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    UntouchableRoomAttachmentChecks.Native(battle.RootElement, source?.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 3 or 4 && args[0] == "--ability-cache-continuation-only")
{
    using var mismatch = ModelJson.ReadFixture(args[1]);
    using var display = ModelJson.ReadFixture(args[2]);
    using var native = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    AbilityCacheContinuationChecks.Native(mismatch.RootElement, display.RootElement, native?.RootElement);
    return;
}
if (args.Length is 2 or 4 && args[0] == "--moon-silence-reset-only")
{
    using var battle = ModelJson.ReadFixture(args[1]);
    using var source = args.Length == 4 ? ModelJson.ReadFixture(args[2]) : null;
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    MoonSilenceResetChecks.Native(battle.RootElement, source?.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--automatic-focus-preview-only")
{
    using var mismatch = ModelJson.ReadFixture(args[1]);
    using var prefix = ModelJson.ReadFixture(args[2]);
    AutomaticFocusPreviewChecks.Native(mismatch.RootElement, prefix.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--automatic-view-continuation-only")
{
    using var mismatch = ModelJson.ReadFixture(args[1]);
    using var battle = ModelJson.ReadFixture(args[2]);
    AutomaticViewContinuationChecks.Native(mismatch.RootElement, battle.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--prepared-selection-publication-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    PreparedSelectionPublicationChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--sentient-draw-upgrade-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) SentientDrawUpgradeChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); SentientDrawUpgradeChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length == 2 && args[0] == "--unsupported-card-ranking-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    UnsupportedCardRankingChecks.Run(source.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--effect-test-rng-counterexample-only")
{
    HandHighlightRngChecks.EffectCounterexample(args[1], args[2]);
    return;
}
if (args.Length == 3 && args[0] == "--hand-highlight-rng-counterexample-only")
{
    HandHighlightRngChecks.Counterexample(args[1], args[2]);
    return;
}
if (args.Length == 2 && args[0] == "--hand-highlight-rng-native-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    HandHighlightRngChecks.Native(source.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--hand-highlight-rng-pair-only")
{
    if (string.Equals(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Hand highlight calibration requires two distinct native archives.");
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    HandHighlightRngChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--unit-ability-duality-counterexample-only")
{
    UnitAbilityDualityChecks.Counterexample(args[1]);
    return;
}
if (args.Length == 3 && args[0] == "--unit-ability-duality-pair-only")
{
    if (string.Equals(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Unit ability calibration requires two distinct native archives.");
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    UnitAbilityDualityChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--terminal-companion-roster-only")
{
    TerminalCompanionRosterChecks.Native(args[1]);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--relic-damage-tower-roster-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        RelicDamageTowerRosterChecks.Native(source.RootElement, replay.RootElement);
    }
    else RelicDamageTowerRosterChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--last-attack-statistic-refresh-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        LastAttackStatisticRefreshChecks.Native(source.RootElement, replay.RootElement);
    }
    else LastAttackStatisticRefreshChecks.Native(source.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--juice-tome-pair-only")
{
    using var refused = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var source = ModelJson.ReadFixture(args[3]);
    JuiceTomeChecks.Pair(refused.RootElement, source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--room-status-policy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    RoomStatusPolicyChecks.Native(source.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--juice-magic-pair-only")
{
    using var refused = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var source = ModelJson.ReadFixture(args[3]);
    JuiceMagicChecks.Pair(refused.RootElement, source.RootElement, replay.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--corrupt-diadem-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        CorruptDiademChecks.Native(source.RootElement, replay.RootElement);
    }
    else CorruptDiademChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--boxed-copy-trait-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        BoxedCopyTraitChecks.Native(source.RootElement, replay.RootElement);
    }
    else BoxedCopyTraitChecks.Native(source.RootElement);
    return;
}

if (args.Length is 2 or 3 && args[0] == "--slay-enchantment-order-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) SlayEnchantmentOrderChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); SlayEnchantmentOrderChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length == 2 && args[0] == "--sentry-only")
{
    SentryChecks.Run();
    using var source = ModelJson.ReadFixture(args[1]);
    SentryChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--retained-sentry-target-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) RetainedSentryTargetChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); RetainedSentryTargetChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length == 2 && args[0] == "--card-corruption-attack-queries-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    CardCorruptionAttackUpgradeChecks.NaturalQueries(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--card-corruption-attack-upgrade-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) CardCorruptionAttackUpgradeChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); CardCorruptionAttackUpgradeChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--unbox-relic-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) UnboxRelicChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); UnboxRelicChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--companion-world-roster-only")
{
    CompanionWorldRosterChecks.Native(args[1], args.Length == 3 ? args[2] : null);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--outer-boss-pyre-attachment-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) OuterBossPyreAttachmentChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); OuterBossPyreAttachmentChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length == 3 && args[0] == "--preview-birth-stage-only")
{
    PreviewBirthStageChecks.Native(args[1], args[2]);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--recursion-draw-callbacks-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) RecursionDrawCallbacksChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); RecursionDrawCallbacksChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length == 3 && args[0] == "--preview-birth-binding-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PreviewBirthBindingChecks.Native(source.RootElement, replay.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--first-of-kin-exhaust-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) FirstOfKinExhaustChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); FirstOfKinExhaustChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--corrupt-room-attachment-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) CorruptRoomAttachmentChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); CorruptRoomAttachmentChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--pyre-ascension-queue-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) PyreAscensionQueueChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); PyreAscensionQueueChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--enchantment-preview-death-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    EnchantmentPreviewDeathChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--floor-transition-piercing-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) FloorTransitionPiercingChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); FloorTransitionPiercingChecks.Native(source.RootElement, replay.RootElement); }
    return;
}

if (args.Length is 2 or 3 && args[0] == "--corrupt-equipment-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) CorruptEquipmentChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); CorruptEquipmentChecks.Native(source.RootElement, replay.RootElement); }
    return;
}

if (args.Length == 4 && args[0] == "--juice-justcause-pair-only")
{
    using var refused = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var source = ModelJson.ReadFixture(args[3]);
    JuiceJustCauseChecks.Pair(refused.RootElement, source.RootElement, replay.RootElement);
    return;
}

if (args.Length is 2 or 3 && args[0] == "--triggered-corruption-relic-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) TriggeredCorruptionRelicChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); TriggeredCorruptionRelicChecks.Native(source.RootElement, replay.RootElement); }
    return;
}

if (args.Length == 3 && args[0] == "--post-battle-reward-combat-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PostBattleRewardCombatChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--intrinsic-infusion-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) IntrinsicInfusionChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); IntrinsicInfusionChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length == 2 && args[0] == "--corruption-scaling-policy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    CorruptionScalingPolicyChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--echoes-past-recast-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) EchoesPastRecastChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); EchoesPastRecastChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--challenge-recast-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) ChallengeRecastChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); ChallengeRecastChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--corrupt-bloodsuit-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) CorruptBloodsuitChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); CorruptBloodsuitChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--justcause-consume-copy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) JustCauseConsumeCopyChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); JustCauseConsumeCopyChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--x-room-status-policy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) XRoomStatusPolicyChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); XRoomStatusPolicyChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--fracture-recast-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) FractureRecastChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); FractureRecastChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--monster-spell-no-teammates-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) MonsterSpellNoTeammatesChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); MonsterSpellNoTeammatesChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--terminal-companion-slot-only")
{
    if (args.Length == 2) TerminalCompanionSlotChecks.Native(args[1]);
    else TerminalCompanionSlotChecks.Pair(args[1], args[2]);
    return;
}
if (args.Length == 2 && args[0] == "--terminal-companion-slot-observation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TerminalCompanionSlotChecks.Observation(fixture.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--liquid-courage-recast-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) LiquidCourageRecastChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); LiquidCourageRecastChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--zero-energy-x-policy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) ZeroEnergyXPolicyChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); ZeroEnergyXPolicyChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--unobserved-preview-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) UnobservedPreviewBirthChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); UnobservedPreviewBirthChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 3 or 4 && args[0] == "--player-egg-catalog-only")
{
    using var missing = ModelJson.ReadFixture(args[1]);
    using var complete = ModelJson.ReadFixture(args[2]);
    if (args.Length == 3) PlayerEggCatalogChecks.Native(missing.RootElement, complete.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[3]); PlayerEggCatalogChecks.Native(missing.RootElement, complete.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--spent-monster-spell-relic-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) SpentMonsterSpellRelicChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); SpentMonsterSpellRelicChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--wurmkin-spike-copy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) WurmkinSpikeCopyChecks.Copy(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); WurmkinSpikeCopyChecks.Copy(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--wurmkin-spike-copy-deployment-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) WurmkinSpikeCopyChecks.Deployment(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); WurmkinSpikeCopyChecks.Deployment(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--justcause-retain-copy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) JustCauseRetainCopyChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); JustCauseRetainCopyChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--teleport-adjacent-order-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) TeleportAdjacentOrderChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); TeleportAdjacentOrderChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length is 2 or 3 && args[0] == "--justcause-monster-spell-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) JustCauseMonsterSpellChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); JustCauseMonsterSpellChecks.Native(source.RootElement, replay.RootElement); }
    return;
}
if (args.Length == 2 && args[0] == "--retaliation-revival-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    RetaliationRevivalChecks.Original(document.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--unowned-relic-copy-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    UnownedRelicCopyChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--revived-sacrifice-attribution-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    RevivedSacrificeAttributionChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length is 2 or 3 && args[0] == "--draft-upgrade-combat-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) DraftUpgradeCombatChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); DraftUpgradeCombatChecks.Native(source.RootElement, replay.RootElement); }
    return;
}

if (args.Length == 2 && args[0] == "--friendly-room-damage-policy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    FriendlyRoomDamagePolicyChecks.Native(fixture.RootElement);
    return;
}

if (args.Length is 2 or 3 && args[0] == "--removed-ability-self-only")
{
    UnitTriggerUpgradeChecks.Run();
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 2) RemovedAbilitySelfChecks.Native(source.RootElement);
    else { using var replay = ModelJson.ReadFixture(args[2]); RemovedAbilitySelfChecks.Pair(source.RootElement, replay.RootElement); }
    return;
}

if (args.Length == 3 && args[0] == "--pyre-payment-world-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PyrePaymentWorldChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--card-type-sequence-reset-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    CardTypeSequenceResetChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--pyre-max-health-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PyreMaxHealthChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--sacrifice-preview-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    SacrificePreviewChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--sacrifice-policy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    SacrificePolicyChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--consume-kill-copy-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var observation = ModelJson.ReadFixture(args[3]);
    ConsumeKillCopyChecks.Pair(source.RootElement, replay.RootElement, observation.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--triggered-corruption-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        TriggeredCorruptionChecks.Native(source.RootElement, replay.RootElement);
    }
    else TriggeredCorruptionChecks.Native(source.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--juice-fracture-pair-only")
{
    using var refused = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var observation = ModelJson.ReadFixture(args[3]);
    JuiceFractureChecks.Pair(refused.RootElement, observation.RootElement, replay.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--juice-ritual-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var observation = ModelJson.ReadFixture(args[3]);
    JuiceRitualChecks.Pair(source.RootElement, replay.RootElement, observation.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--infusion-trigger-merge-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    InfusionTriggerMergeChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--natural-hydra-graft-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NaturalHydraCatalogChecks.GeneratedPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--natural-hydra-catalog-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NaturalHydraCatalogChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--natural-angel-pair-cache-only")
{
    NaturalAngelPairCacheChecks.Pair(args[1], args[2]);
    return;
}
if (args.Length == 2 && args[0] == "--slay-corruption-callback-native-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    SlayCorruptionCallbackChecks.Native(source.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--slay-corruption-callback-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    SlayCorruptionCallbackChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--undying-copy-effects-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    UndyingCopyOnPlayChecks.EffectsPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--undying-copy-deployment-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    UndyingCopyOnPlayChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--undying-copy-effects-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    UndyingCopyOnPlayChecks.Effects(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--exact-browse-continuation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ExactBrowseContinuationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--outer-boss-attachment-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    OuterBossAttachmentBirthChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--sweep-boss-preview-stop-native-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    SweepBossPreviewStopChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--boss-preview-test-rng-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    BossPreviewTestRngChecks.Native(native.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--boss-preview-test-rng-pair-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    BossPreviewTestRngChecks.Pair(native.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--guaranteed-status-test-rng-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    GuaranteedStatusTestRngChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--guaranteed-status-test-rng-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    GuaranteedStatusTestRngChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--random-refined-campaign-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    RandomCampaignChecks.RefinedPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 9 && args[0] == "--random-refined-campaign-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    RandomCampaignChecks.RefinedPair(source.RootElement, replay.RootElement, int.Parse(args[3]), int.Parse(args[4]),
        int.Parse(args[5]), args[6], args[7], int.Parse(args[8]));
    return;
}
if (args.Length == 2 && args[0] == "--discard-holdover-return-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    DiscardHoldoverReturnChecks.Native(native.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--discard-holdover-return-pair-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    DiscardHoldoverReturnChecks.Pair(native.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--terminal-enemy-slot-stages-only")
{
    TerminalEnemySlotChecks.Stages(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--terminal-enemy-slot-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TerminalEnemySlotChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--terminal-enemy-slot-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    TerminalEnemySlotChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--corrupt-restricted-state-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CorruptRestrictedStateChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--corrupt-restricted-state-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    CorruptRestrictedStateChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--attachment-reinstall-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AttachmentReinstallChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--attachment-reinstall-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    AttachmentReinstallChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unstable-revival-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    UnstableRevivalChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--unstable-revival-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    UnstableRevivalChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--status-callback-room-sync-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    StatusCallbackRoomSyncChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--status-callback-room-sync-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    StatusCallbackRoomSyncChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--ephemeral-consume-order-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EphemeralConsumeOrderChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--ephemeral-consume-order-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    EphemeralConsumeOrderChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--generated-discard-guard-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    GeneratedDiscardGuardChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length > 1 && args[0] == "--triggered-status-controls-only")
{
    TriggeredStatusChecks.Run();
    foreach (string path in args.Skip(1))
    {
        using var fixture = ModelJson.ReadFixture(path);
        if (fixture.RootElement.GetProperty("ModifierScenario").GetString() != "triggered-status")
            throw new InvalidOperationException("Expected a triggered-status native control.");
        TriggeredStatusChecks.Native(fixture.RootElement);
    }
    return;
}

if (args.Length == 2 && args[0] == "--guaranteed-status-preview-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    GuaranteedStatusPreviewChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--guaranteed-status-preview-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    GuaranteedStatusPreviewChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--corrupt-source-alignment-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CorruptSourceAlignmentChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--corrupt-source-alignment-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    CorruptSourceAlignmentChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--relic-attachment-replacement-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RelicAttachmentReplacementChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--relic-attachment-replacement-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    RelicAttachmentReplacementChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--enemy-command-attack-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EnemyCommandAttackChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--enemy-command-attack-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    EnemyCommandAttackChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--corrupt-before-play-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CorruptBeforePlayChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--corrupt-before-play-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    CorruptBeforePlayChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--destroyed-feeder-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    DestroyedFeederChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--destroyed-feeder-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var raw = ModelJson.ReadFixture(args[3]);
    DestroyedFeederChecks.Pair(source.RootElement, replay.RootElement, raw.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--corrupt-copyability-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CorruptCopyabilityChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--corrupt-copyability-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    CorruptCopyabilityChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--friendly-relic-piercing-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    FriendlyRelicPiercingChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--friendly-relic-piercing-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    FriendlyRelicPiercingChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--recursion-target-text-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    RecursionTargetTextChecks.Native(native.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--recursion-target-text-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    RecursionTargetTextChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 4 && args[0] == "--despawn-frame-removal-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var observation = ModelJson.ReadFixture(args[3]);
    CombatRemovalTimingChecks.DespawnFramePair(source.RootElement, replay.RootElement, observation.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--despawn-listeners-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    CombatRemovalTimingChecks.DespawnNative(native.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--despawn-listeners-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    CombatRemovalTimingChecks.DespawnPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--heal-combat-removal-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var observation = ModelJson.ReadFixture(args[3]);
    CombatRemovalTimingChecks.HealPair(source.RootElement, replay.RootElement, observation.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--overcharge-cleanup-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    OverchargeCleanupChecks.Native(native.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--overcharge-cleanup-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    OverchargeCleanupChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--heal-growth-record-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    HealGrowthRecordChecks.Native(native.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--heal-growth-record-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    HealGrowthRecordChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--ability-attack-compaction-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    AbilityAttackCompactionChecks.Native(native.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--ability-attack-compaction-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    AbilityAttackCompactionChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--dead-caster-status-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    DeadCasterStatusChecks.Native(native.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--dead-caster-status-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    DeadCasterStatusChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--authored-consume-copy-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    AuthoredConsumeCopyChecks.Native(native.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--authored-consume-copy-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    AuthoredConsumeCopyChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--feeding-spell-routing-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    FeedingSpellRoutingChecks.Native(native.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--feeding-spell-routing-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    FeedingSpellRoutingChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--merchant-permafrost-observation-only")
{
    using var before = ModelJson.ReadFixture(args[1]);
    using var source = ModelJson.ReadFixture(args[2]);
    RandomCampaignChecks.MerchantPermafrost(before.RootElement, source.RootElement);
    return;
}

if (args.Length is 2 or 3 && args[0] == "--moonbeam-copy-lifecycle-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    MoonbeamCopyChecks.Lifecycle(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--moonbeam-copy-deployment-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    MoonbeamCopyChecks.Deployment(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--boss-ember-moon-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    BossEmberMoonChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--random-campaign-curated-only")
{
    RandomCampaignChecks.Curated(args[1]);
    return;
}

if (args.Length is 7 or 8 && args[0] == "--random-campaign-source-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    RandomCampaignChecks.Source(source.RootElement, int.Parse(args[2]), int.Parse(args[3]), int.Parse(args[4]), args[5], args[6], args.Length == 8 ? int.Parse(args[7]) : 3000);
    return;
}

if (args.Length is 8 or 9 && args[0] == "--random-campaign-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    RandomCampaignChecks.Pair(source.RootElement, replay.RootElement, int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]), args[6], args[7], args.Length == 9 ? int.Parse(args[8]) : 3000);
    return;
}

if (args.Length == 1 && args[0] == "--advice-history-only")
{
    AdviceDiagnosticJournalChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--live-arkion-pyre-observed-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RollingAdviceChecks.ObservedSuccessors(fixture.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--live-arkion-pyre-native-only")
{
    using var native = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    LiveArkionPyreChecks.Native(native.RootElement, birth.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--live-arkion-pyre-only")
{
    using var live = ModelJson.ReadFixture(args[1]);
    using var native = ModelJson.ReadFixture(args[2]);
    LiveArkionPyreChecks.Run(live.RootElement, native.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--early-stop-native-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EarlyStopChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 4 && args[0] == "--early-stop-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    using var healing = ModelJson.ReadFixture(args[2]);
    using var loss = ModelJson.ReadFixture(args[3]);
    EarlyStopChecks.Run(fixture.RootElement, healing.RootElement, loss.RootElement);
    return;
}

if (args.Length >= 2 && args[0] is "--search-pruning-only" or "--search-frontier-only")
{
    SearchPruningChecks.Run(args.Skip(1).ToArray(), args[0] == "--search-pruning-only");
    return;
}

if (args.Length == 2 && args[0] == "--deployment-advice-budget-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    DeploymentAdviceBudgetChecks.Run(fixture.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--project-live-forsaken")
{
    OpeningForsakenTrialChecks.ProjectLive(args[1], args[2]);
    return;
}
if (args.Length == 2 && args[0] is "--original-opening-forsaken-only" or "--live-forsaken-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--original-opening-forsaken-only") OpeningForsakenTrialChecks.Native(fixture.RootElement);
    else OpeningForsakenTrialChecks.Live(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--live-moon-continuation-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    LiveMoonContinuationChecks.Run(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--live-moon-composition-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    LiveMoonContinuationChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 4 && args[0] == "--player-fleshweaver-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NativeCovenantCampaignChecks.FleshweaverPair(source.RootElement, replay.RootElement, args[3]);
    return;
}

if (args.Length == 3 && args[0] is "--fleshweaver-birth-only" or "--fleshweaver-baseline-only" or "--fleshweaver-growth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--fleshweaver-birth-only") FleshweaverChecks.Birth(fixture.RootElement, args[2]);
    else FleshweaverChecks.Battle(fixture.RootElement, args[1], args[2], args[0] == "--fleshweaver-growth-only");
    return;
}

if (args.Length == 3 && args[0] == "--player-arkion-push-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NativeCovenantCampaignChecks.ArkionPushPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--arkion-push-birth-only" or "--arkion-push-baseline-only" or "--arkion-push-growth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--arkion-push-birth-only") ArkionPushChecks.Birth(fixture.RootElement);
    else ArkionPushChecks.Battle(fixture.RootElement, args[1], args[0] == "--arkion-push-growth-only");
    return;
}

if (args.Length == 3 && args[0] == "--player-arkion-rage-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NativeCovenantCampaignChecks.ArkionRagePair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--arkion-rage-birth-only" or "--arkion-rage-baseline-only" or "--arkion-rage-growth-only" or
    "--arkion-rage-natural-birth-only" or "--arkion-rage-natural-baseline-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    bool natural = args[0].Contains("natural", StringComparison.Ordinal);
    if (args[0].EndsWith("birth-only", StringComparison.Ordinal)) ArkionRageChecks.Birth(fixture.RootElement, natural);
    else ArkionRageChecks.Battle(fixture.RootElement, args[1], args[0] == "--arkion-rage-growth-only", natural);
    return;
}

if (args.Length == 3 && args[0] == "--player-arkion-pyre-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NativeCovenantCampaignChecks.ArkionPyrePair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--arkion-pyre-birth-only" or "--arkion-pyre-baseline-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--arkion-pyre-birth-only") ArkionPyreChecks.Birth(fixture.RootElement);
    else ArkionPyreChecks.Battle(fixture.RootElement, args[1]);
    return;
}

if (args.Length == 4 && args[0] == "--player-cael-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NativeCovenantCampaignChecks.CaelPair(source.RootElement, replay.RootElement, args[3]);
    return;
}

if (args.Length == 3 && args[0] is "--cael-birth-only" or "--cael-baseline-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--cael-birth-only") CaelChecks.Birth(fixture.RootElement, args[2]);
    else CaelChecks.Battle(fixture.RootElement, args[1], args[2]);
    return;
}

if (args.Length == 2 && args[0] is "--angel-duo-harvest-death-only" or "--angel-duo-harvest-death-battle-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--angel-duo-harvest-death-only") AngelDuoHarvestChecks.Native(fixture.RootElement);
    else
    {
        BattleTurnChecks.Native(fixture.RootElement);
        RoomCombatChecks.NativeRoomOnly(args[1]);
    }
    return;
}

if (args.Length == 2 && args[0] is "--angel-duo-revenge-harvest-only" or "--angel-duo-revenge-only" or "--angel-duo-revenge-baseline-only" or "--angel-duo-revenge-harvest-birth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--angel-duo-revenge-harvest-birth-only") AngelDuoVariantChecks.Birth(fixture.RootElement, "RevengeHarvest");
    else AngelDuoVariantChecks.RevengeHarvest(fixture.RootElement, args[1],
        args[0] != "--angel-duo-revenge-baseline-only", args[0] != "--angel-duo-revenge-only");
    return;
}

if (args.Length == 3 && args[0] == "--ordinary-advice-budget-recovery-only")
{
    using var failure = ModelJson.ReadFixture(args[1]);
    using var source = ModelJson.ReadFixture(args[2]);
    OrdinaryAdviceBudgetChecks.NativeRecovery(failure.RootElement, source.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--ordinary-advice-budget-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OrdinaryAdviceBudgetChecks.Run(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--atomic-advice-budget-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AtomicAdviceBudgetChecks.Run(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--angel-duo-loop-resolve-birth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoVariantChecks.Birth(fixture.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--player-angel-duo-loop-resolve-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NativeCovenantCampaignChecks.AngelDuoVariantPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--player-angel-duo-revenge-harvest-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NativeCovenantCampaignChecks.AngelDuoVariantPair(source.RootElement, replay.RootElement, "RevengeHarvest");
    return;
}

if (args.Length == 2 && args[0] == "--angel-duo-loop-resolve-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoVariantChecks.Battle(fixture.RootElement, args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--angel-duo-catalog-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoCatalogChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--angel-duo-relentless-entry-only" or "--angel-duo-paired-phases-only" or "--angel-duo-paired-pyre-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--angel-duo-relentless-entry-only") AngelDuoRelentlessChecks.Entry(fixture.RootElement);
    else if (args[0] == "--angel-duo-paired-phases-only") AngelDuoRelentlessChecks.Phases(fixture.RootElement);
    else AngelDuoRelentlessChecks.Pyre(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--angel-duo-companion-phase-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoCompanionPhaseChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--angel-duo-death-battle-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleTurnChecks.Native(fixture.RootElement);
    TrainCombatChecks.Native(fixture.RootElement);
    RoomCombatChecks.NativeRoomOnly(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--angel-duo-death-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoDeathChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--angel-duo-forced-relentless-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoForcedRelentlessChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--angel-duo-incant-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoIncantChecks.Native(fixture.RootElement);
    BattleActionChecks.Native(fixture.RootElement);
    BattleTurnChecks.Native(fixture.RootElement);
    TrainCombatChecks.Native(fixture.RootElement);
    RoomCombatChecks.NativeRoomOnly(args[1]);
    return;
}
if (args.Length == 2 && args[0] is "--angel-duo-battle-only" or "--angel-duo-natural-battle-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoBirthChecks.Battle(fixture.RootElement, args[1], args[0] == "--angel-duo-natural-battle-only");
    return;
}
if (args.Length == 2 && args[0] is "--angel-duo-birth-only" or "--angel-duo-natural-birth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    AngelDuoBirthChecks.Native(fixture.RootElement, args[0] == "--angel-duo-natural-birth-only");
    return;
}

if (args.Length == 3 && args[0] == "--player-angel-duo-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NativeCovenantCampaignChecks.AngelDuoPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 5 && args[0] is "--player-native-covenant-campaign-pair-only" or "--player-native-covenant-campaign-default-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    if (args[0] == "--player-native-covenant-campaign-default-pair-only")
        NativeCovenantCampaignChecks.DefaultPair(source.RootElement, replay.RootElement, int.Parse(args[3]), int.Parse(args[4]));
    else NativeCovenantCampaignChecks.Pair(source.RootElement, replay.RootElement, int.Parse(args[3]), int.Parse(args[4]));
    return;
}

if (args.Length == 2 && args[0] == "--observed-successors-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RollingAdviceChecks.ObservedSuccessors(fixture.RootElement);
    TrainCombatChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--harvester-component-only")
{
    using var observation = ModelJson.ReadFixture(args[1]);
    using var fixture = ModelJson.ReadFixture(args[2]);
    HarvesterBirthChecks.Component(observation.RootElement, fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--harvester-birth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    HarvesterBirthChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--endless-spell-routing-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EndlessSpellRoutingChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--endless-return-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EndlessReturnChecks.Run();
    EndlessReturnChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--endless-spell-routing-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    EndlessSpellRoutingChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--player-boss-corrupter-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerOuterBossChecks.CorrupterWinningPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--player-campaign-trial-gold-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    CampaignTrialGoldBattleChecks.PlayerPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-allied-lethal-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossAlliedLethalChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-pyre-command-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossCommandedAttackChecks.Native(fixture.RootElement, pyreCommand: true);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-commanded-attack-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossCommandedAttackChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-allied-paid-spell-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossPaidSpellChecks.Native(fixture.RootElement, allied: true);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-protected-spell-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossProtectedSpellChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--true-final-boss-triggered-command-only" or "--true-final-boss-triggered-pyre-command-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossCommandedAttackChecks.Native(fixture.RootElement,
        pyreCommand: args[0] == "--true-final-boss-triggered-pyre-command-only", triggeredAlly: true);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-triggered-paid-spell-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossPaidSpellChecks.Native(fixture.RootElement, allied: true, triggeredAlly: true);
    return;
}

if (args.Length == 3 && args[0] == "--player-true-final-boss-avenger-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerTrueFinalBossChecks.Pair(source.RootElement, replay.RootElement, ordinaryAlly: true, triggeredAlly: true);
    return;
}

if (args.Length == 3 && args[0] == "--player-true-final-boss-avenger-upgraded-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerTrueFinalBossChecks.Pair(source.RootElement, replay.RootElement, ordinaryAlly: true, upgradedAlly: true, triggeredAlly: true);
    return;
}

if (args.Length == 3 && args[0] == "--player-true-final-boss-ordinary-upgraded-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerTrueFinalBossChecks.Pair(source.RootElement, replay.RootElement, ordinaryAlly: true, upgradedAlly: true);
    return;
}

if (args.Length == 3 && args[0] == "--player-true-final-boss-ordinary-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerTrueFinalBossChecks.Pair(source.RootElement, replay.RootElement, ordinaryAlly: true);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-ordinary-ally-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossOrdinaryAllyChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-ally-upgrade-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossAllyUpgradeChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-paid-spell-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossPaidSpellChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-lethal-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossLethalChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--true-final-boss-relentless-only" or "--true-final-boss-relentless-phases-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--true-final-boss-relentless-phases-only") TrueFinalBossRelentlessChecks.Phases(fixture.RootElement.GetProperty("Actions"));
    else TrueFinalBossRelentlessChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-dormant-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossDormantChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--true-final-boss-allied-only" or "--true-final-boss-multi-allied-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossAlliedChecks.Native(fixture.RootElement, args[0] == "--true-final-boss-multi-allied-only");
    return;
}

if (args.Length == 2 && args[0] == "--terminal-death-only")
{
    TerminalDeathChecks.Run();
    using var fixture = ModelJson.ReadFixture(args[1]);
    TerminalDeathChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--chosen-incant-attack-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    StartingCovenantChecks.ChosenNative(fixture.RootElement, args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--native-covenant-startup-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    StartingCovenantChecks.Native(fixture.RootElement, args[1]);
    return;
}
if (args.Length == 4 && args[0] is "--native-covenant-final-boss-only" or "--player-native-covenant-final-boss-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var other = ModelJson.ReadFixture(args[2]);
    if (args[0] == "--native-covenant-final-boss-only")
        StartingCovenantChecks.FinalNative(source.RootElement, other.RootElement, args[1], int.Parse(args[3]));
    else StartingCovenantChecks.FinalPair(source.RootElement, other.RootElement, int.Parse(args[3]));
    return;
}
if (args.Length == 5 && args[0] is "--native-covenant-outer-boss-only" or "--player-native-covenant-outer-boss-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var other = ModelJson.ReadFixture(args[2]);
    if (args[0] == "--native-covenant-outer-boss-only")
        StartingCovenantChecks.OuterNative(source.RootElement, other.RootElement, args[1], args[3], int.Parse(args[4]));
    else StartingCovenantChecks.OuterPair(source.RootElement, other.RootElement, args[3], int.Parse(args[4]));
    return;
}
if (args.Length == 3 && args[0].StartsWith("--player-native-covenant-startup-", StringComparison.Ordinal) &&
    args[0].EndsWith("pair-only", StringComparison.Ordinal))
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    int level = args[0] == "--player-native-covenant-startup-pair-only" ? 1 :
        int.Parse(args[0]["--player-native-covenant-startup-".Length..^"-pair-only".Length]);
    StartingCovenantChecks.Pair(source.RootElement, replay.RootElement, level);
    return;
}

if (args.Length == 5 && args[0] is "--covenant-outer-boss-only" or "--player-covenant-outer-boss-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var other = ModelJson.ReadFixture(args[2]);
    if (args[0] == "--covenant-outer-boss-only")
        CovenantOuterBossChecks.Native(source.RootElement, other.RootElement, args[3], bool.Parse(args[4]));
    else CovenantOuterBossChecks.Pair(source.RootElement, other.RootElement, args[3], bool.Parse(args[4]));
    return;
}

if (args.Length == 2 && args[0] == "--covenant-enemy-stats-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CovenantEnemyStatsChecks.Native(fixture.RootElement, args[1]);
    return;
}

if (args.Length == 3 && args[0] is "--player-covenant-enemy-stats-pair-only" or "--player-covenant-enemy-stats-ftue-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    CovenantEnemyStatsChecks.Pair(source.RootElement, replay.RootElement, args[0] == "--player-covenant-enemy-stats-ftue-pair-only");
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-covenant-battle-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossBattleChecks.Covenant(fixture.RootElement, args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-battle-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossBattleChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--true-final-boss-preview-only" or "--true-final-boss-preview-rooms-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossPreviewChecks.Native(fixture.RootElement, args[0] == "--true-final-boss-preview-rooms-only");
    return;
}

if (args.Length == 2 && args[0] == "--train-only")
{
    TrainCombatChecks.Run();
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrainCombatChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-train-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossTrainChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--post-combat-only")
{
    PostCombatHealingChecks.Run();
    using var fixture = ModelJson.ReadFixture(args[1]);
    PostCombatHealingChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-post-combat-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossPostCombatChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--unit-turns-only")
{
    UnitTurnBeginChecks.Run();
    using var fixture = ModelJson.ReadFixture(args[1]);
    UnitTurnBeginChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-unit-turns-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossUnitTurnChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--true-final-boss-continuation-only" or "--true-final-boss-passive-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--true-final-boss-passive-only") TrueFinalBossCombatFlowChecks.Passive(fixture.RootElement);
    else TrueFinalBossCombatFlowChecks.Continuation(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-scheduled-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossScheduledChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] is "--true-final-boss-phases-only" or "--true-final-boss-controllers-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--true-final-boss-phases-only") TrueFinalBossPhaseChecks.Phases(fixture.RootElement);
    else TrueFinalBossPhaseChecks.Controllers(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--pyrebane-targets-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PyreBaneChecks.Native(fixture.RootElement);
    return;
}

if (args.Length >= 2 && args[0] is "--true-final-boss-death-only" or "--true-final-boss-terminal-damage-only")
{
    foreach (string path in args.Skip(1))
    {
        using var fixture = ModelJson.ReadFixture(path);
        TrueFinalBossDeathChecks.Native(fixture.RootElement, args[0] == "--true-final-boss-terminal-damage-only");
    }
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossDamageChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-health-updates-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossHealthChecks.Native(fixture.RootElement);
    LinkedBossHealthChecks.Bridge(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-world-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    LinkedBossHealthChecks.Capture(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-life-link-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    LifeLinkChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-health-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    LinkedHealthChecks.Native(fixture.RootElement);
    return;
}

if (args.Length > 1 && args[0] == "--true-final-boss-covenants-matrix-only")
{
    TrueFinalBossCovenantMatrixChecks.Native(args.Skip(1));
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-spawn-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossSpawnChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--true-final-boss-birth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TrueFinalBossChecks.Birth(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--shorter-advice-budget-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ShorterAdviceBudgetChecks.Run(fixture.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--player-state-key-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    BattleStateKeyChecks.NativePair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-natural-ring7-default-win-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NaturalRing7TacticsChecks.DefaultWinningPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] is "--player-natural-ring7-clean-pair-only" or "--player-natural-ring7-default-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NaturalRing7TacticsChecks.RestartPair(source.RootElement, replay.RootElement,
        defaults: args[0] == "--player-natural-ring7-default-pair-only");
    return;
}

if (args.Length == 3 && args[0] == "--player-natural-ring7-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    NaturalRing7TacticsChecks.PlayerPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--natural-fragile-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    NaturalFragileChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--natural-ring7-tactics-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    NaturalRing7TacticsChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--tactical-ability-target-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TacticalTargetChecks.NativeAbilityBoundary(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--tactical-target-only")
{
    TacticalTargetChecks.Run();
    return;
}

if (args.Length == 2 && args[0] == "--natural-movement-capture-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    NaturalMovementCaptureChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--terminal-copy-target-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PlayerTerminalCopyChecks.TargetBoundary(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-terminal-copy-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerTerminalCopyChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] is "--terminal-purge-copy-boundary-only" or "--terminal-purge-copy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--terminal-purge-copy-boundary-only") TerminalPurgeCopyChecks.Boundary(fixture.RootElement);
    else TerminalPurgeCopyChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] is "--terminal-copy-boundary-only" or "--terminal-copy-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (args[0] == "--terminal-copy-boundary-only") TerminalCopyOnPlayChecks.Boundary(fixture.RootElement);
    else TerminalCopyOnPlayChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] is "--copy-on-play-only" or "--natural-copy-on-play-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    CopyOnPlayChecks.Native(fixture.RootElement, natural: args[0] == "--natural-copy-on-play-only");
    return;
}

if (args.Length == 3 && args[0] == "--player-event-graft-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerEventGraftChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--retired-aura-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RetiredAuraChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--ember-train-heal-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EmberTrainHealChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--ember-train-heal-x-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EmberTrainHealChecks.XCost(fixture.RootElement);
    RoomCombatChecks.NativeRoomOnly(args[1]);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

if (args.Length == 2 && args[0] == "--titanite-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TitaniteChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--titanite-combinations-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TitaniteCombinationChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--titanite-weakness-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TitaniteWeaknessChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--boss-preview-sweep-only")
{
    BossPreviewSweepChecks.Native(args[1]);
    return;
}

if (args.Length == 2 && args[0] == "--event-graft-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EventGraftChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--permanent-traits-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PermanentTraitChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--belief-battle-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleBeliefChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--fixture-archive-only")
{
    FixtureArchiveChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--tactical-selection-cache-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TacticalSelectionChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--cost-rejection-cache-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TacticalSelectionChecks.CostRejections(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--parallel-planner-merge-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    using var alternative = ModelJson.ReadFixture(args[2]);
    ParallelPlannerChecks.Merge(fixture.RootElement, alternative.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--pending-boss-loss-order-only")
{
    using var early = ModelJson.ReadFixture(args[1]);
    using var late = ModelJson.ReadFixture(args[2]);
    using var prediction = ModelJson.ReadFixture(args[3]);
    PendingBossLossOrderChecks.Run(early.RootElement, late.RootElement, prediction.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--resumed-moon-capture-only")
{
    using var counterexample = ModelJson.ReadFixture(args[1]);
    using var source = ModelJson.ReadFixture(args[2]);
    using var replay = ModelJson.ReadFixture(args[3]);
    ResumedMoonCaptureChecks.Pair(counterexample.RootElement, source.RootElement, replay.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--room-death-preview-identity-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    RoomDeathPreviewIdentityChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--moon-conduit-birth-order-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    MoonConduitBirthOrderChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--moon-null-boss-relic-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    MoonNullBossRelicChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--titan-curse-preview-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TitanCursePreviewChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 3 or 4 && args[0] == "--remote-preview-frame-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    RemotePreviewFrameChecks.Native(source.RootElement, args[2], replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--troop-dead-cache-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TroopDeadCacheBirthChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--troop-write-world-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TroopWriteWorldChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--troop-last-haste-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TroopLastHasteChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 3 or 4 && args[0] == "--troop-combat-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    TroopCombatBirthChecks.Native(source.RootElement, args[2], replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--troop-retired-source-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TroopRetiredSourceChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--troop-birth-addition-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TroopBirthAdditionChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--complete-native-battle-only")
{
    CompleteNativeBattleChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--conduit-retirement-battle-only")
{
    ConduitRetirementChecks.CompletedBattle(args[1]);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--troop-preview-addition-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TroopPreviewAdditionChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 3 or 4 && args[0] == "--conduit-retirement-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var counterexample = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    ConduitRetirementChecks.Native(source.RootElement, counterexample.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 3 or 4 && args[0] == "--generated-conduit-copy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var counterexample = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    GeneratedConduitCopyChecks.Native(source.RootElement, counterexample.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 3 or 4 && args[0] == "--retired-actor-frame-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var ownerDeath = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 4 ? ModelJson.ReadFixture(args[3]) : null;
    RetiredActorFrameChecks.Native(source.RootElement, ownerDeath.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--retained-player-preview-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    RetainedPlayerPreviewChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--troop-enchantment-noop-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    TroopEnchantmentNoOpChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length is 3 or 5 && args[0] == "--angel-duo-moon-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 5 ? ModelJson.ReadFixture(args[3]) : null;
    using var replayBirth = args.Length == 5 ? ModelJson.ReadFixture(args[4]) : null;
    AngelDuoMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay?.RootElement, replayBirth?.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--conduit-view-continuation-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    ConduitViewContinuationChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--ability-presentation-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var repaired = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    AbilityPresentationChecks.Native(source.RootElement, repaired?.RootElement);
    return;
}
if (args.Length is 3 or 5 && args[0] == "--arkion-moon-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 5 ? ModelJson.ReadFixture(args[3]) : null;
    using var replayBirth = args.Length == 5 ? ModelJson.ReadFixture(args[4]) : null;
    ArkionMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay?.RootElement, replayBirth?.RootElement);
    return;
}
if (args.Length is 3 or 5 && args[0] == "--fleshweaver-merchant-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 5 ? ModelJson.ReadFixture(args[3]) : null;
    using var replayBirth = args.Length == 5 ? ModelJson.ReadFixture(args[4]) : null;
    FleshweaverMerchantBirthChecks.Native(source.RootElement, birth.RootElement, replay?.RootElement, replayBirth?.RootElement);
    return;
}
if (args.Length is 3 or 5 && args[0] == "--cael-moon-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 5 ? ModelJson.ReadFixture(args[3]) : null;
    using var replayBirth = args.Length == 5 ? ModelJson.ReadFixture(args[4]) : null;
    CaelMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay?.RootElement, replayBirth?.RootElement);
    return;
}
if (args.Length is 3 or 5 && args[0] == "--outer-boss-moon-birth-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    using var replay = args.Length == 5 ? ModelJson.ReadFixture(args[3]) : null;
    using var replayBirth = args.Length == 5 ? ModelJson.ReadFixture(args[4]) : null;
    OuterBossMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay?.RootElement, replayBirth?.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--room-slay-status-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    RoomSlayStatusChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--terminal-coroutine-planes-only")
{
    using var counterexample = ModelJson.ReadFixture(args[1]);
    using var source = ModelJson.ReadFixture(args[2]);
    using var replay = ModelJson.ReadFixture(args[3]);
    TerminalCoroutinePlaneChecks.Pair(counterexample.RootElement, source.RootElement, replay.RootElement);
    TerminalEnemySlotChecks.Stages(args[2]);
    TerminalEnemySlotChecks.Stages(args[3]);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--room-death-retained-cleanup-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = args.Length == 3 ? ModelJson.ReadFixture(args[2]) : null;
    RoomDeathRetainedCleanupChecks.Native(source.RootElement, replay?.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--parallel-planner-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ParallelPlannerChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--parallel-planner-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    ParallelPlannerChecks.NativePair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--exact-battle-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ExactBattleChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--clone-refresh-only")
{
    CardGenerationChecks.Run();
    CloneUpgradeRefreshChecks.Run();
    using var fixture = ModelJson.ReadFixture(args[1]);
    CloneUpgradeRefreshChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--holdover-variants-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    HoldoverVariantChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--starting-duality-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    StartingDualityChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--holdover-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    HoldoverChecks.Native(fixture.RootElement);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
    return;
}

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
if (args.Length is 2 or 3 && args[0] == "--spell-harvest-tower-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        SpellHarvestTowerChecks.Native(source.RootElement, replay.RootElement);
    }
    else SpellHarvestTowerChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--spell-angel-death-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        SpellAngelDeathChecks.Native(source.RootElement, replay.RootElement);
    }
    else SpellAngelDeathChecks.Native(source.RootElement);
    return;
}
if (args.Length is 3 or 4 && args[0] == "--equipment-preview-owner-only")
{
    using var failure = ModelJson.ReadFixture(args[1]);
    using var source = ModelJson.ReadFixture(args[2]);
    if (args.Length == 4)
    {
        using var replay = ModelJson.ReadFixture(args[3]);
        EquipmentPreviewOwnerChecks.Native(failure.RootElement, source.RootElement, replay.RootElement);
    }
    else EquipmentPreviewOwnerChecks.Native(failure.RootElement, source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--pre-discard-spell-rng-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        PreDiscardSpellRngChecks.Native(source.RootElement, replay.RootElement);
    }
    else PreDiscardSpellRngChecks.Native(source.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--stronger-corruption-copy-effects-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    StrongerCorruptionCopyChecks.Effects(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--stronger-corruption-copy-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        StrongerCorruptionCopyChecks.Native(source.RootElement, replay.RootElement);
    }
    else StrongerCorruptionCopyChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--corrupt-poison-combat-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        CorruptPoisonCombatChecks.Native(source.RootElement, replay.RootElement);
    }
    else CorruptPoisonCombatChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--outer-boss-death-statistics-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        OuterBossDeathStatisticsChecks.Native(source.RootElement, replay.RootElement);
    }
    else OuterBossDeathStatisticsChecks.Native(source.RootElement);
    return;
}
if (args.Length is 2 or 3 && args[0] == "--unbox-conduit-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    if (args.Length == 3)
    {
        using var replay = ModelJson.ReadFixture(args[2]);
        UnboxConduitChecks.Native(source.RootElement, replay.RootElement);
    }
    else UnboxConduitChecks.Native(source.RootElement);
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
if (args.Length == 2 && args[0] == "--original-null-scourges-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OriginalNullScourgeChecks.Native(fixture.RootElement);
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
if (args.Length == 2 && args[0] == "--eclipse-movement-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EclipseChamberChecks.Movement(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--eclipse-callbacks-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EclipseChamberChecks.Callbacks(fixture.RootElement);
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

if (args.Length >= 2 && args[0] == "--outer-boss-pyre-turn-only")
{
    foreach (string path in args.Skip(1))
    {
        using var fixture = ModelJson.ReadFixture(path);
        OuterBossPyreChecks.Turns(fixture.RootElement);
    }
    return;
}

if (args.Length >= 2 && args[0] == "--outer-boss-pyre-only")
{
    foreach (string path in args.Skip(1))
    {
        using var fixture = ModelJson.ReadFixture(path);
        OuterBossPyreChecks.Native(fixture.RootElement);
    }
    return;
}

if (args.Length == 4 && args[0] == "--outer-boss-fragile-relentless-only")
{
    using var phases = ModelJson.ReadFixture(args[1]);
    using var pyre = ModelJson.ReadFixture(args[2]);
    using var turns = ModelJson.ReadFixture(args[3]);
    OuterBossPhaseChecks.Native(phases.RootElement, "Boss_Level8_Fragile");
    OuterBossPyreChecks.Native(pyre.RootElement, "Boss_Level8_Fragile", pyreHealthControl: true);
    OuterBossPyreChecks.Turns(turns.RootElement, "Boss_Level8_Fragile", pyreHealthControl: true);
    return;
}

if (args.Length == 3 && args[0] == "--player-boss-fragile-bounded-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerOuterBossChecks.FragileBoundedPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--player-true-final-boss-bounded-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerTrueFinalBossChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] is "--player-true-final-boss-covenant00-pair-only" or "--player-true-final-boss-covenant01-pair-only" or
    "--player-true-final-boss-covenant04-pair-only" or "--player-true-final-boss-covenant08-pair-only" or
    "--player-true-final-boss-covenants-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    string covenant = args[0] switch
    {
        "--player-true-final-boss-covenant00-pair-only" => "Covenant00_FTUE",
        "--player-true-final-boss-covenant01-pair-only" => "Covenant01_StrongerEnemiesMoreCards",
        "--player-true-final-boss-covenant08-pair-only" => "Covenant08_StrongerEnemies",
        "--player-true-final-boss-covenants-pair-only" => "Covenant00_FTUE;Covenant01_StrongerEnemiesMoreCards;Covenant04_StrongerBosses;Covenant08_StrongerEnemies",
        _ => "Covenant04_StrongerBosses"
    };
    PlayerTrueFinalBossChecks.Pair(source.RootElement, replay.RootElement, covenantAsset: covenant);
    return;
}

if (args.Length == 3 && args[0] == "--player-boss-corrupter-bounded-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerOuterBossChecks.CorrupterBoundedPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 4 && args[0] == "--player-boss-trial-bounded-pair-only")
{
    using var source = ModelJson.ReadFixture(args[2]);
    using var replay = ModelJson.ReadFixture(args[3]);
    PlayerOuterBossChecks.TrialBoundedPair(args[1], source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 6 && args[0] == "--outer-boss-trial-only")
{
    using var battle = ModelJson.ReadFixture(args[2]);
    using var birth = ModelJson.ReadFixture(args[3]);
    using var phases = ModelJson.ReadFixture(args[4]);
    using var controller = ModelJson.ReadFixture(args[5]);
    OuterBossTrialChecks.Native(args[1], battle.RootElement, birth.RootElement, phases.RootElement, controller.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--outer-boss-trial-pair-only")
{
    using var source = ModelJson.ReadFixture(args[2]);
    using var replay = ModelJson.ReadFixture(args[3]);
    OuterBossTrialChecks.Pair(args[1], source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 5 && args[0] == "--outer-boss-trial-relentless-only")
{
    using var battle = ModelJson.ReadFixture(args[2]);
    using var phases = ModelJson.ReadFixture(args[3]);
    OuterBossTrialChecks.Control(battle.RootElement, args[1]);
    OuterBossCorrupterChecks.Relentless(battle.RootElement, phases.RootElement, OuterBossTrialChecks.Boss(args[1]), int.Parse(args[4]));
    return;
}
if (args.Length == 5 && args[0] == "--outer-boss-corrupter-only")
{
    using var battle = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    using var phases = ModelJson.ReadFixture(args[3]);
    using var controller = ModelJson.ReadFixture(args[4]);
    OuterBossCorrupterChecks.Native(battle.RootElement, birth.RootElement, phases.RootElement, controller.RootElement);
    return;
}

if (args.Length == 1 && args[0] == "--outer-boss-corrupter-pure")
{
    TriggeredStatusChecks.Run();
    RelicChecks.Run();
    return;
}

if (args.Length == 3 && args[0] == "--outer-boss-corrupter-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    OuterBossCorrupterChecks.Pair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 3 && args[0] == "--outer-boss-corrupter-relentless-only")
{
    using var battle = ModelJson.ReadFixture(args[1]);
    using var phases = ModelJson.ReadFixture(args[2]);
    OuterBossCorrupterChecks.Relentless(battle.RootElement, phases.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--fragile-default-planner-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PlayerOuterBossChecks.FragileDefaultModel(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--fragile-short-budget-planner-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PlayerOuterBossChecks.FragileShortBudgetModel(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-boss-fragile-short-budget-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerOuterBossChecks.FragileShortBudgetPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-boss-fragile-default-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerOuterBossChecks.FragileDefaultPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-boss-fragile-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerOuterBossChecks.FragileWinningPair(source.RootElement, replay.RootElement);
    return;
}

if (args.Length == 5 && args[0] == "--outer-boss-fragile-only")
{
    using var battle = ModelJson.ReadFixture(args[1]);
    using var birth = ModelJson.ReadFixture(args[2]);
    using var phases = ModelJson.ReadFixture(args[3]);
    using var controller = ModelJson.ReadFixture(args[4]);
    OuterBossVariantChecks.Fragile(battle.RootElement, birth.RootElement, phases.RootElement, controller.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--outer-boss-birth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OuterBossBirthChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--outer-boss-deployment-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OuterBossTurnChecks.Deployment(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--outer-boss-turn-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OuterBossTurnChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--outer-boss-variant-phase-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    string boss = fixture.RootElement.GetProperty("Actions")[0].GetProperty("Before").GetProperty("Boss").GetProperty("AssetKey").GetString()!;
    if (!OuterBossDefinitions.Supports(boss)) throw new InvalidOperationException("Uncalibrated outer-boss phase definition.");
    OuterBossPhaseChecks.Native(fixture.RootElement, boss);
    return;
}

if (args.Length == 2 && args[0] == "--outer-boss-phase-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OuterBossPhaseChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--boss-action-controller-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BossActionControllerChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--boss-action-target-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BossActionTargetChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--retired-unit-destruction-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RetiredUnitDestructionChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--corruption-status-only")
{
    CorruptionStatusChecks.Native(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--undying-boss-preview-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    UndyingBossPreviewChecks.Native(fixture.RootElement);
    return;
}

if (args.Length == 2 && args[0] == "--intrinsic-opening-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    IntrinsicOpeningDrawChecks.Native(fixture.RootElement);
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

if (args.Length == 2 && args[0] == "--conditional-pyre-lethal-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ConditionalPyreDamageChecks.Lethal(fixture.RootElement);
    RoomCombatChecks.NativeRoomOnly(args[1]);
    Console.WriteLine($"NATIVE-FIXTURE-METADATA schema={fixture.RootElement.GetProperty("Schema").GetInt32()} bytes={new FileInfo(args[1]).Length} uniqueNodes={fixture.UniqueNodeCount}");
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

if (args.Length == 2 && args[0] == "--rolling-advice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RollingAdviceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--player-native-covenant-default-loss-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    NativeCovenantCampaignChecks.DefaultLoss(fixture.RootElement, int.Parse(args[2]), int.Parse(args[3]));
    return;
}
if (args.Length == 2 && args[0] == "--eclipse-enemy-movement-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EclipseChamberChecks.EnemyMovement(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--eclipse-enemy-callbacks-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    if (!fixture.RootElement.GetProperty("EclipseChamberEnemyCallbacks").GetBoolean())
        throw new InvalidOperationException("Missing opposing original callback carrier.");
    EclipseChamberChecks.EnemyMovement(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--eclipse-enemy-conduit-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    EclipseChamberChecks.EnemyConduit(fixture.RootElement);
    EnemySpawningChecks.StartingConduitGuard();
    return;
}
if (args.Length == 2 && args[0] == "--eclipse-mixed-conduit-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MixedConduitChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-mixed-conduit-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    MixedConduitChecks.PlayerPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--moon-covenant-startup-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MoonCovenantCompositionChecks.Startup(fixture.RootElement,
        fixture.RootElement.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!.Spawn.Train.Context!,
        int.Parse(args[2]), guards: true);
    return;
}
if (args.Length == 3 && args[0] is "--player-moon-room-spells-pair-only" or "--player-moon-room-healing-relic-pair-only" or "--player-moon-room-phase-heal-relic-pair-only" or "--player-moon-room-phase-energy-relic-pair-only" or "--player-moon-room-hand-power-relic-pair-only" or "--player-moon-room-status-relic-pair-only" or "--player-moon-room-covenant03-pair-only" or "--player-moon-room-threshold-relic-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    if (args[0] == "--player-moon-room-threshold-relic-pair-only")
    {
        MoonSpellThresholdCompositionChecks.PlayerPair(source.RootElement, replay.RootElement);
        return;
    }
    PlayerMoonRoomSpellChecks.Pair(source.RootElement, replay.RootElement,
        requireHealingRelic: args[0] == "--player-moon-room-healing-relic-pair-only",
        requirePhaseHealRelic: args[0] == "--player-moon-room-phase-heal-relic-pair-only",
        requirePhaseEnergyRelic: args[0] == "--player-moon-room-phase-energy-relic-pair-only",
        requireHandPowerRelic: args[0] == "--player-moon-room-hand-power-relic-pair-only",
        requireStatusRelic: args[0] == "--player-moon-room-status-relic-pair-only",
        requireCovenant03: args[0] == "--player-moon-room-covenant03-pair-only");
    return;
}
if (args.Length == 2 && args[0] == "--player-moon-room-turn-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PlayerMoonRoomSpellChecks.TurnBoundary(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--room-moon-spell-upgrades-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomMoonSpellUpgradeChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--moon-spell-threshold-component-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MoonSpellThresholdCompositionChecks.Component(fixture.RootElement);
    return;
}
if (args.Length == 1 && args[0] == "--retained-callbacks-only")
{
    RetainedCallbackChecks.Run();
    return;
}
if (args.Length == 2 && args[0] == "--moon-spell-threshold-card-plays-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MoonSpellThresholdCompositionChecks.CardPlays(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--moon-hand-power-callbacks-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomMoonSpellUpgradeChecks.HandPowerCallbacks(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--moon-status-callbacks-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    MoonStatusCompositionChecks.Callbacks(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--room-moon-callback-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomMoonCallbackChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] is "--status-trigger-order-only" or "--large-status-damage-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    StatusTriggerOrderChecks.Native(fixture.RootElement, damage: args[0] == "--large-status-damage-only");
    return;
}
if (args.Length == 2 && args[0] == "--unplayed-pyre-preview-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    UnplayedPyrePreviewChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--conduit-movement-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    ConduitMovementChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-room-moon-enemy-conduit-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerRoomMoonChecks.EnemyConduitPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-room-moon-enemy-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerRoomMoonChecks.EnemyCallbacksPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-room-moon-movement-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerRoomMoonChecks.MovementPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-room-moon-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerRoomMoonChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-conduit-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerConduitChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-boss-winning-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerOuterBossChecks.WinningPair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-boss-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerOuterBossChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-moon-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    PlayerMoonPhaseChecks.Pair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--rolling-replay-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    RollingAdviceChecks.FrozenPair(source.RootElement, replay.RootElement);
    BattleAdviceChecks.Native(source.RootElement, requireBoss: false);
    return;
}
if (args.Length == 4 && args[0] == "--rolling-advice-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var baseline = ModelJson.ReadFixture(args[3]);
    RollingAdviceChecks.Pair(source.RootElement, replay.RootElement, baseline.RootElement);
    return;
}
if (args.Length >= 10 && (args.Length - 1) % 3 == 0 && args[0] == "--rolling-advice-suite-only")
{
    var fixtures = args.Skip(1).Select(ModelJson.ReadFixture).ToArray();
    try { RollingAdviceChecks.Suite(fixtures.Select(fixture => fixture.RootElement).ToArray()); }
    finally { foreach (var fixture in fixtures) fixture.Dispose(); }
    return;
}
if (args.Length == 2 && args[0] == "--native-advice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleAdviceChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--ordinary-advice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OrdinaryAdviceChecks.Run(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--terminal-advice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    OrdinaryAdviceChecks.TerminalLoss(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--room-attachment-birth-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    RoomAttachmentBirthChecks.Observed(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-ordinary-advice-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    OrdinaryAdviceChecks.NativePair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--floor-browse-advice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    FloorBrowseAdviceChecks.Run(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--floor-browse-selection-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    FloorBrowseAdviceChecks.SelectionBoundary(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--live-floor-scroll-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    FloorBrowseAdviceChecks.LiveScroll(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--player-floor-browse-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    FloorBrowseAdviceChecks.NativePair(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--winning-advice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    WinningAdviceChecks.Run(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--battle-advice-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleAdviceChecks.Run(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--planner-replay-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattlePlannerChecks.NativePlan(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--planner-pair-only")
{
    using var first = ModelJson.ReadFixture(args[1]);
    using var second = ModelJson.ReadFixture(args[2]);
    BattlePlannerChecks.Pair(first.RootElement, second.RootElement);
    return;
}
if (args.Length == 4 && args[0] == "--rolling-advice-victory-pair-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    using var baseline = ModelJson.ReadFixture(args[3]);
    RollingAdviceChecks.VictoryPair(source.RootElement, replay.RootElement, baseline.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--tactical-shift-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TacticalShiftChecks.Run(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--tactical-shift-safety-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    TacticalShiftChecks.LethalRestoration(fixture.RootElement);
    return;
}
if (args.Length == 3 && args[0] == "--spell-shield-target-only")
{
    using var source = ModelJson.ReadFixture(args[1]);
    using var replay = ModelJson.ReadFixture(args[2]);
    SpellShieldTargetChecks.Native(source.RootElement, replay.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--planner-deployment-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PlannerDeploymentChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--planner-plateau-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    PlannerPlateauChecks.Native(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--deployment-forecast-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleHeuristicChecks.DeploymentForecast(fixture.RootElement);
    return;
}
if (args.Length is >= 2 and <= 4 && args[0] is "--rolling-forecast-only" or "--rolling-player-forecast-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleHeuristicChecks.RollingForecast(fixture.RootElement, args.Length >= 3 ? int.Parse(args[2]) : 256,
        args.Length == 4 ? int.Parse(args[3]) : 2, retainCurrentTurn: args[0] == "--rolling-player-forecast-only");
    return;
}
if (args.Length is 2 or 3 && args[0] == "--heuristic-forecast-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleHeuristicChecks.Forecast(fixture.RootElement, args.Length == 3 ? int.Parse(args[2]) : 2);
    return;
}
if (args.Length == 4 && args[0] == "--battle-planner-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattlePlannerChecks.Native(fixture.RootElement, int.Parse(args[2]), int.Parse(args[3]));
    return;
}
if (args.Length == 2 && args[0] == "--state-key-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattleStateKeyChecks.Run(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--natural-ring7-default-planner-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    NaturalRing7TacticsChecks.DefaultBudget(fixture.RootElement);
    return;
}
if (args.Length == 2 && args[0] is "--tactical-planner-only" or "--natural-ring7-planner-only")
{
    using var fixture = ModelJson.ReadFixture(args[1]);
    BattlePlannerChecks.TacticalSeed(fixture.RootElement, naturalRing7: args[0] == "--natural-ring7-planner-only");
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
    BattleTurnChecks.Native(fixture.RootElement);
    BattleActionChecks.Native(fixture.RootElement);
    TrainCombatChecks.Native(fixture.RootElement);
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
if (args.Length == 2 && args[0] == "--heavenfall-focus-prefix-only")
{
    using var document = ModelJson.ReadFixture(args[1]);
    HeavenfallFocusChecks.Prefix(document.RootElement);
    return;
}
if (args.Length == 2 && args[0] == "--recorded-chain-diagnose")
{
    using var chainDocument = ModelJson.ReadFixture(args[1]);
    RecordedBattleChecks.Native(chainDocument.RootElement);
    Console.WriteLine("RECORDED-CHAIN-DIAGNOSTIC: capture counters are not acceptance checks; require a fresh passing native capture before curation.");
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
TacticalTargetChecks.Run();
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
foreach (string path in args.Where(path => path.Contains("full-battle-original-conduit-relics-physical", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OriginalConduitChecks.Native(fixture.RootElement);
}
// This partial failure archive is checked as observed successors below; it
// intentionally has no accepted terminal battle or historical forecast.
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "player-endless-spell-routing-calibration.mt2f" or "player-endless-spell-routing-replay-calibration.mt2f"))
{
    using var observation = ModelJson.ReadFixture(path);
    EndlessSpellRoutingChecks.Native(observation.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "player-feeding-spell-routing-calibration.mt2f" or "player-feeding-spell-routing-replay-calibration.mt2f"))
{
    using var observation = ModelJson.ReadFixture(path);
    FeedingSpellRoutingChecks.Native(observation.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).Replace("-replay-calibration.mt2f", "-calibration.mt2f", StringComparison.Ordinal) is
    "player-authored-consume-copy-calibration.mt2f" or "player-ephemeral-copy-full-hand-calibration.mt2f" or
    "player-ephemeral-copy-hand-space-calibration.mt2f").Select(path => ModelJson.RelatedFixture(path,
        Path.GetFileName(path).Replace("-replay-calibration.mt2f", "-calibration.mt2f", StringComparison.Ordinal))).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        Path.GetFileName(path).Replace("-calibration.mt2f", "-replay-calibration.mt2f", StringComparison.Ordinal)));
    AuthoredConsumeCopyChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "player-dead-caster-status-calibration.mt2f" or
    "player-dead-caster-status-replay-calibration.mt2f").Select(path => ModelJson.RelatedFixture(path,
        "player-dead-caster-status-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-dead-caster-status-replay-calibration.mt2f"));
    DeadCasterStatusChecks.Pair(source.RootElement, replay.RootElement);
    TacticalTargetChecks.NativeAbilityBoundary(source.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "player-ability-attack-compaction-calibration.mt2f" or
    "player-ability-attack-compaction-replay-calibration.mt2f").Select(path => ModelJson.RelatedFixture(path,
        "player-ability-attack-compaction-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-ability-attack-compaction-replay-calibration.mt2f"));
    AbilityAttackCompactionChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "player-heal-growth-record-calibration.mt2f" or
    "player-heal-growth-record-replay-calibration.mt2f").Select(path => ModelJson.RelatedFixture(path,
        "player-heal-growth-record-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-heal-growth-record-replay-calibration.mt2f"));
    HealGrowthRecordChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-recursion-target-text-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-recursion-target-text-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-recursion-target-text-replay-calibration.mt2f"));
    RecursionTargetTextChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-friendly-relic-piercing-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-friendly-relic-piercing-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-friendly-relic-piercing-replay-calibration.mt2f"));
    FriendlyRelicPiercingChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-restricted-return-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-restricted-return-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-restricted-return-replay-calibration.mt2f"));
    CorruptRestrictedStateChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-restricted-holdover-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-restricted-holdover-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-restricted-holdover-replay-calibration.mt2f"));
    CorruptRestrictedStateChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-restricted-state-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-restricted-state-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-restricted-state-replay-calibration.mt2f"));
    CorruptRestrictedStateChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-terminal-enemy-slot-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-terminal-enemy-slot-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-terminal-enemy-slot-replay-calibration.mt2f"));
    TerminalEnemySlotChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-terminal-enemy-slot-calibration.mt2f"))
    TerminalEnemySlotChecks.Stages(path);
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-attachment-reinstall-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-attachment-reinstall-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-attachment-reinstall-replay-calibration.mt2f"));
    AttachmentReinstallChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-unstable-revival-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-unstable-revival-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-unstable-revival-replay-calibration.mt2f"));
    UnstableRevivalChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-status-callback-room-sync-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-status-callback-room-sync-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-status-callback-room-sync-replay-calibration.mt2f"));
    StatusCallbackRoomSyncChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-ephemeral-consume-order-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-ephemeral-consume-order-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-ephemeral-consume-order-replay-calibration.mt2f"));
    EphemeralConsumeOrderChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-generated-discard-guard-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-generated-discard-guard-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-generated-discard-guard-replay-calibration.mt2f"));
    GeneratedDiscardGuardChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-boss-preview-test-rng-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-boss-preview-test-rng-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-boss-preview-test-rng-replay-calibration.mt2f"));
    BossPreviewTestRngChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-pyre-payment-world-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-pyre-payment-world-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-pyre-payment-world-replay-calibration.mt2f"));
    PyrePaymentWorldChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-draft-upgrade-combat-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-draft-upgrade-combat-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-draft-upgrade-combat-replay-calibration.mt2f"));
    DraftUpgradeCombatChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-post-battle-reward-combat-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-post-battle-reward-combat-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-post-battle-reward-combat-replay-calibration.mt2f"));
    PostBattleRewardCombatChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-teleport-adjacent-order-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-teleport-adjacent-order-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-teleport-adjacent-order-replay-calibration.mt2f"));
    TeleportAdjacentOrderChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-justcause-monster-spell-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-justcause-monster-spell-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-justcause-monster-spell-replay-calibration.mt2f"));
    JustCauseMonsterSpellChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-liquid-courage-recast-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-liquid-courage-recast-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-liquid-courage-recast-replay-calibration.mt2f"));
    LiquidCourageRecastChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-monster-spell-no-teammates-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-monster-spell-no-teammates-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-monster-spell-no-teammates-replay-calibration.mt2f"));
    MonsterSpellNoTeammatesChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-fracture-recast-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-fracture-recast-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-fracture-recast-replay-calibration.mt2f"));
    FractureRecastChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-x-room-status-policy-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-x-room-status-policy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-x-room-status-policy-replay-calibration.mt2f"));
    XRoomStatusPolicyChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-zero-energy-x-policy-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-zero-energy-x-policy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-zero-energy-x-policy-replay-calibration.mt2f"));
    ZeroEnergyXPolicyChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-unobserved-preview-birth-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-unobserved-preview-birth-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-unobserved-preview-birth-replay-calibration.mt2f"));
    UnobservedPreviewBirthChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-room-death-preview-identity-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-room-death-preview-identity-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-room-death-preview-identity-replay-calibration.mt2f"));
    RoomDeathPreviewIdentityChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-moonbeam-copy-deployment-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-moonbeam-copy-deployment-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-moonbeam-copy-deployment-replay-calibration.mt2f"));
    MoonbeamCopyChecks.Deployment(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-moonbeam-copy-lifecycle-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-moonbeam-copy-lifecycle-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-moonbeam-copy-lifecycle-replay-calibration.mt2f"));
    MoonbeamCopyChecks.Lifecycle(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-remote-preview-frame-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-remote-preview-frame-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-remote-preview-frame-replay-calibration.mt2f"));
    RemotePreviewFrameChecks.Native(source.RootElement, ModelJson.RelatedFixture(path, "player-remote-preview-frame-counterexample-calibration.mt2f"), replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-sentient-draw-upgrade-", StringComparison.OrdinalIgnoreCase) ||
        Path.GetFileName(path).StartsWith("player-sentient-draw-upgrade2-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, Path.GetFileName(path).StartsWith("player-sentient-draw-upgrade2-", StringComparison.OrdinalIgnoreCase)
        ? "player-sentient-draw-upgrade2-calibration.mt2f" : "player-sentient-draw-upgrade-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, Path.GetFileName(path).Replace("-calibration.mt2f", "-replay-calibration.mt2f")));
    SentientDrawUpgradeChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-automatic-view-calibration.mt2f"))
{
    using var mismatch = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-prepared-selection-publication-calibration.mt2f"));
    using var battle = ModelJson.ReadFixture(path);
    AutomaticViewContinuationChecks.Native(mismatch.RootElement, battle.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-ability-presentation-mutation-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var render = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-ability-cache-display-replay-calibration.mt2f"));
    AbilityPresentationChecks.Native(source.RootElement, render.RootElement);
    using var win = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-ability-cache-winning-continuation-calibration.mt2f"));
    AbilityCacheContinuationChecks.Native(source.RootElement, render.RootElement, win.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-auto-focus-no-preview-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var prefix = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-auto-focus-no-preview-prefix-calibration.mt2f"));
    AutomaticFocusPreviewChecks.Native(source.RootElement, prefix.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-triple-juice-search-failure-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    TripleJuiceSearchChecks.Native(source.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-triple-juice-curse-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-triple-juice-curse-replay-calibration.mt2f"));
    TripleJuiceCurseChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-triple-juice-curse-cast-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-triple-juice-curse-cast-replay-calibration.mt2f"));
    TripleJuiceCurseCastChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-triple-juice-spike-refusal-calibration.mt2f"))
{
    using var refused = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-triple-juice-spike-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-triple-juice-spike-replay-calibration.mt2f"));
    TripleJuiceSpikeChecks.Native(refused.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-double-juice-spike-refusal-calibration.mt2f"))
{
    using var refusal = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-double-juice-spike-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-double-juice-spike-replay-calibration.mt2f"));
    DoubleJuiceSpikeChecks.Native(refusal.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-double-juice-curse-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-double-juice-curse-replay-calibration.mt2f"));
    DoubleJuiceCurseChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-unbox-moon-relic-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-unbox-moon-relic-replay-calibration.mt2f"));
    UnboxMoonRelicChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-inherent-juice-spike-refusal-calibration.mt2f"))
{
    using var refusal = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-inherent-juice-spike-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-inherent-juice-spike-replay-calibration.mt2f"));
    InherentJuiceSpikeChecks.Native(refusal.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-inherent-juice-curse-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-inherent-juice-curse-replay-calibration.mt2f"));
    InherentJuiceCurseChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-combat-victory-statistics-counterexample-calibration.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-combat-victory-statistics-calibration.mt2f"));
    CombatVictoryStatisticsChecks.Native(fixture.RootElement, replay.RootElement);
    RoomCombatChecks.NativeRoomOnly(path);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-room-upgrade-provenance-counterexample-calibration.mt2f"))
{
    using var counterexample = ModelJson.ReadFixture(path);
    using var repaired = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-room-upgrade-provenance-calibration.mt2f"));
    RoomUpgradeProvenanceChecks.Native(counterexample.RootElement, repaired.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-fragile-moon-birth-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var birth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "fragile-moon-creation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-fragile-moon-birth-replay-calibration.mt2f"));
    using var replayBirth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "fragile-moon-creation-replay-calibration.mt2f"));
    FragileMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay.RootElement, replayBirth.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-stasis-shatter-counterexample-calibration.mt2f"))
{
    using var battle = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-stasis-shatter-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-stasis-shatter-replay-calibration.mt2f"));
    StasisShatterChecks.Native(battle.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-untouchable-front-relic-counterexample-calibration.mt2f"))
{
    using var battle = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-untouchable-front-relic-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-untouchable-front-relic-replay-calibration.mt2f"));
    UntouchableFrontRelicChecks.Native(battle.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-untouchable-room-counterexample-calibration.mt2f"))
{
    using var battle = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-untouchable-room-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-untouchable-room-replay-calibration.mt2f"));
    UntouchableRoomAttachmentChecks.Native(battle.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-moon-silence-reset-counterexample-calibration.mt2f"))
{
    using var battle = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-moon-silence-reset-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-moon-silence-reset-replay-calibration.mt2f"));
    MoonSilenceResetChecks.Native(battle.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-cael-moon-birth-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var birth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "cael-moon-creation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-cael-moon-birth-replay-calibration.mt2f"));
    using var replayBirth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "cael-moon-creation-replay-calibration.mt2f"));
    CaelMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay.RootElement, replayBirth.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-fleshweaver-merchant-birth-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var birth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "fleshweaver-merchant-creation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-fleshweaver-merchant-birth-replay-calibration.mt2f"));
    using var replayBirth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "fleshweaver-merchant-creation-replay-calibration.mt2f"));
    FleshweaverMerchantBirthChecks.Native(source.RootElement, birth.RootElement, replay.RootElement, replayBirth.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-prepared-selection-publication-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    PreparedSelectionPublicationChecks.Native(source.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-unsupported-card-ranking-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    UnsupportedCardRankingChecks.Run(source.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-outer-boss-attachment-birth-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-outer-boss-attachment-birth-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-outer-boss-attachment-birth-replay-calibration.mt2f"));
    OuterBossAttachmentBirthChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-sweep-boss-preview-stop-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-sweep-boss-preview-stop-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-sweep-boss-preview-stop-replay-calibration.mt2f"));
    SweepBossPreviewStopChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-troop-dead-cache-birth-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-troop-dead-cache-birth-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-troop-dead-cache-birth-replay-calibration.mt2f"));
    TroopDeadCacheBirthChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-troop-write-world-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-troop-write-world-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-troop-write-world-replay-calibration.mt2f"));
    TroopWriteWorldChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-troop-last-haste-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-troop-last-haste-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-troop-last-haste-replay-calibration.mt2f"));
    TroopLastHasteChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-troop-combat-birth-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-troop-combat-birth-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-troop-combat-birth-replay-calibration.mt2f"));
    TroopCombatBirthChecks.Native(source.RootElement, ModelJson.RelatedFixture(path, "player-troop-combat-birth-counterexample-calibration.mt2f"), replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-troop-retired-source-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-troop-retired-source-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-troop-retired-source-replay-calibration.mt2f"));
    TroopRetiredSourceChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-troop-birth-addition-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-troop-birth-addition-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-troop-birth-addition-replay-calibration.mt2f"));
    TroopBirthAdditionChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-troop-preview-addition-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-troop-preview-addition-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-troop-preview-addition-replay-calibration.mt2f"));
    TroopPreviewAdditionChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-conduit-retirement-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-conduit-retirement-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var counterexample = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-conduit-retirement-counterexample-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-conduit-retirement-replay-calibration.mt2f"));
    ConduitRetirementChecks.Native(source.RootElement, counterexample.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-generated-conduit-copy-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-generated-conduit-copy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var counterexample = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-generated-conduit-copy-counterexample-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-generated-conduit-copy-replay-calibration.mt2f"));
    GeneratedConduitCopyChecks.Native(source.RootElement, counterexample.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-retired-actor-frame-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-retired-actor-frame-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var ownerDeath = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-original-isolated-squad-leader-owner-death.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-retired-actor-frame-replay-calibration.mt2f"));
    RetiredActorFrameChecks.Native(source.RootElement, ownerDeath.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-retained-player-preview-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-retained-player-preview-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-retained-player-preview-replay-calibration.mt2f"));
    RetainedPlayerPreviewChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-troop-enchantment-noop-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-troop-enchantment-noop-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-troop-enchantment-noop-replay-calibration.mt2f"));
    TroopEnchantmentNoOpChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-angel-duo-moon-birth-", StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(path).StartsWith("angel-duo-moon-creation-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-angel-duo-moon-birth-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var birth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "angel-duo-moon-creation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-angel-duo-moon-birth-replay-calibration.mt2f"));
    using var replayBirth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "angel-duo-moon-creation-replay-calibration.mt2f"));
    AngelDuoMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay.RootElement, replayBirth.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-outer-boss-moon-", StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(path).StartsWith("outer-boss-moon-creation-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-outer-boss-moon-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var birth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "outer-boss-moon-creation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-outer-boss-moon-replay-calibration.mt2f"));
    using var replayBirth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "outer-boss-moon-creation-replay-calibration.mt2f"));
    OuterBossMoonBirthChecks.Native(source.RootElement, birth.RootElement, replay.RootElement, replayBirth.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-moon-conduit-birth-order-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-moon-conduit-birth-order-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-moon-conduit-birth-order-replay-calibration.mt2f"));
    MoonConduitBirthOrderChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-moon-null-boss-relic-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-moon-null-boss-relic-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-moon-null-boss-relic-replay-calibration.mt2f"));
    MoonNullBossRelicChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-titan-curse-preview-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-titan-curse-preview-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-titan-curse-preview-replay-calibration.mt2f"));
    TitanCursePreviewChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-room-slay-status-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-room-slay-status-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-room-slay-status-replay-calibration.mt2f"));
    RoomSlayStatusChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-room-death-retained-cleanup-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-room-death-retained-cleanup-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-room-death-retained-cleanup-replay-calibration.mt2f"));
    RoomDeathRetainedCleanupChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-egg-catalog-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-egg-catalog-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var missing = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-egg-catalog-missing-observation.mt2f"));
    using var complete = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-egg-catalog-replay-calibration.mt2f"));
    PlayerEggCatalogChecks.Native(missing.RootElement, complete.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-spent-monster-spell-relic-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-spent-monster-spell-relic-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-spent-monster-spell-relic-replay-calibration.mt2f"));
    SpentMonsterSpellRelicChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "player-wurmkin-spike-copy-calibration.mt2f" or "player-wurmkin-spike-copy-replay-calibration.mt2f")
    .Select(path => ModelJson.RelatedFixture(path, "player-wurmkin-spike-copy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-wurmkin-spike-copy-replay-calibration.mt2f"));
    WurmkinSpikeCopyChecks.Copy(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-wurmkin-spike-copy-deployment-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-wurmkin-spike-copy-deployment-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-wurmkin-spike-copy-deployment-replay-calibration.mt2f"));
    WurmkinSpikeCopyChecks.Deployment(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-echoes-past-recast-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, Path.GetFileName(path).Contains("-zero-", StringComparison.OrdinalIgnoreCase)
        ? "player-echoes-past-recast-zero-calibration.mt2f" : "player-echoes-past-recast-calibration.mt2f"))
    .Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        Path.GetFileName(path).Replace("-calibration.mt2f", "-replay-calibration.mt2f")));
    EchoesPastRecastChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-challenge-recast-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-challenge-recast-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-challenge-recast-replay-calibration.mt2f"));
    ChallengeRecastChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-intrinsic-infusion-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, Path.GetFileName(path).Contains("-zero-", StringComparison.OrdinalIgnoreCase)
        ? "player-intrinsic-infusion-zero-calibration.mt2f" : "player-intrinsic-infusion-calibration.mt2f"))
    .Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        Path.GetFileName(path).Replace("-calibration.mt2f", "-replay-calibration.mt2f")));
    IntrinsicInfusionChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-justcause-consume-copy-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-justcause-consume-copy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-justcause-consume-copy-replay-calibration.mt2f"));
    JustCauseConsumeCopyChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-justcause-retain-copy-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-justcause-retain-copy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-justcause-retain-copy-replay-calibration.mt2f"));
    JustCauseRetainCopyChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-unowned-relic-copy-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-unowned-relic-copy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-unowned-relic-copy-replay-calibration.mt2f"));
    UnownedRelicCopyChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-revived-sacrifice-attribution-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-revived-sacrifice-attribution-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-revived-sacrifice-attribution-replay-calibration.mt2f"));
    RevivedSacrificeAttributionChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-retaliation-revival-calibration.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    RetaliationRevivalChecks.Original(source.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-triggered-corruption-relic-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-triggered-corruption-relic-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-triggered-corruption-relic-replay-calibration.mt2f"));
    TriggeredCorruptionRelicChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-triggered-corruption-", StringComparison.OrdinalIgnoreCase) &&
    !Path.GetFileName(path).StartsWith("player-triggered-corruption-relic-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-triggered-corruption-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-triggered-corruption-replay-calibration.mt2f"));
    TriggeredCorruptionChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-spell-harvest-tower-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-spell-harvest-tower-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-spell-harvest-tower-replay-calibration.mt2f"));
    SpellHarvestTowerChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-native-covenant-03-ring6-refined-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "full-battle-native-covenant-03-ring6-refined-search.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-native-covenant-03-ring6-refined-replay.mt2f"));
    RandomCampaignChecks.RefinedPair(source.RootElement, replay.RootElement, 424242, 3, 5, "Banished", "Pyreborne", 35);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-native-covenant-03-ring7-release110-budget10000-loss.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    RandomCampaignChecks.Source(source.RootElement, 424242, 3, 6, "Banished", "Pyreborne", 10000);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "player-effect-test-rng-counterexample.mt2f" or "player-effect-test-rng-observed.mt2f")
    .Select(path => ModelJson.RelatedFixture(path, "player-effect-test-rng-counterexample.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
    HandHighlightRngChecks.EffectCounterexample(path, ModelJson.RelatedFixture(path, "player-effect-test-rng-observed.mt2f"));
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "player-hand-highlight-test-rng-counterexample.mt2f" or "player-hand-highlight-test-rng-observed.mt2f")
    .Select(path => ModelJson.RelatedFixture(path, "player-hand-highlight-test-rng-counterexample.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
    HandHighlightRngChecks.Counterexample(path, ModelJson.RelatedFixture(path, "player-hand-highlight-test-rng-observed.mt2f"));
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "player-hand-highlight-test-rng-calibration.mt2f" or "player-hand-highlight-test-rng-replay-calibration.mt2f" or
    "player-effect-test-rng-calibration.mt2f" or "player-effect-test-rng-replay-calibration.mt2f")
    .Select(path => ModelJson.RelatedFixture(path, Path.GetFileName(path).StartsWith("player-effect-", StringComparison.Ordinal)
        ? "player-effect-test-rng-calibration.mt2f" : "player-hand-highlight-test-rng-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, Path.GetFileName(path).Replace("-calibration.mt2f", "-replay-calibration.mt2f")));
    HandHighlightRngChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-unit-ability-duality-counterexample.mt2f"))
    UnitAbilityDualityChecks.Counterexample(path);
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "player-unit-ability-duality-calibration.mt2f" or "player-unit-ability-duality-replay-calibration.mt2f")
    .Select(path => ModelJson.RelatedFixture(path, "player-unit-ability-duality-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-unit-ability-duality-replay-calibration.mt2f"));
    UnitAbilityDualityChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-terminal-companion-roster-counterexample.mt2f"))
    TerminalCompanionRosterChecks.Native(path);
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-terminal-companion-slot-counterexample.mt2f"))
    TerminalCompanionSlotChecks.Pair(path, ModelJson.RelatedFixture(path, "player-terminal-companion-slot-replay.mt2f"));
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-terminal-companion-slot-observation.mt2f"))
{
    using var observation = ModelJson.ReadFixture(path);
    TerminalCompanionSlotChecks.Observation(observation.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-relic-damage-tower-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-relic-damage-tower-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-relic-damage-tower-replay-calibration.mt2f"));
    RelicDamageTowerRosterChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-last-attack-statistic-refresh-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-last-attack-statistic-refresh-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-last-attack-statistic-refresh-replay-calibration.mt2f"));
    LastAttackStatisticRefreshChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-spell-angel-death-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-spell-angel-death-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-spell-angel-death-replay-calibration.mt2f"));
    SpellAngelDeathChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-equipment-preview-owner-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-equipment-preview-owner-failure.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var failure = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-equipment-preview-owner-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-equipment-preview-owner-replay-calibration.mt2f"));
    EquipmentPreviewOwnerChecks.Native(failure.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-stronger-corruption-copy-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-stronger-corruption-copy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-stronger-corruption-copy-replay-calibration.mt2f"));
    StrongerCorruptionCopyChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-pre-discard-spell-rng-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-pre-discard-spell-rng-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-pre-discard-spell-rng-replay-calibration.mt2f"));
    PreDiscardSpellRngChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-poison-combat-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-poison-combat-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-poison-combat-replay-calibration.mt2f"));
    CorruptPoisonCombatChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-outer-boss-death-statistics-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-outer-boss-death-statistics-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-outer-boss-death-statistics-replay-calibration.mt2f"));
    OuterBossDeathStatisticsChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-spell-shield-target-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-spell-shield-target-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-spell-shield-target-replay-calibration.mt2f"));
    SpellShieldTargetChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-boxed-copy-trait-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-boxed-copy-trait-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-boxed-copy-trait-replay-calibration.mt2f"));
    BoxedCopyTraitChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-bloodsuit-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-bloodsuit-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-bloodsuit-replay-calibration.mt2f"));
    CorruptBloodsuitChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-diadem-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-diadem-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-diadem-replay-calibration.mt2f"));
    CorruptDiademChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-slay-enchantment-order-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-slay-enchantment-order-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-slay-enchantment-order-replay-calibration.mt2f"));
    SlayEnchantmentOrderChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-retained-sentry-target-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-retained-sentry-target-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-retained-sentry-target-replay-calibration.mt2f"));
    RetainedSentryTargetChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corruption-attack-upgrade-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corruption-attack-upgrade-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corruption-attack-upgrade-replay-calibration.mt2f"));
    CardCorruptionAttackUpgradeChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-unbox-relic-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-unbox-relic-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-unbox-relic-replay-calibration.mt2f"));
    UnboxRelicChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-unbox-conduit-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-unbox-conduit-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-unbox-conduit-replay-calibration.mt2f"));
    UnboxConduitChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-removed-ability-self-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-removed-ability-self-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-removed-ability-self-replay-calibration.mt2f"));
    RemovedAbilitySelfChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-card-type-sequence-reset-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-card-type-sequence-reset-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-card-type-sequence-reset-replay-calibration.mt2f"));
    CardTypeSequenceResetChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-pyre-max-health-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-pyre-max-health-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-pyre-max-health-replay-calibration.mt2f"));
    PyreMaxHealthChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-sacrifice-preview-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-sacrifice-preview-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-sacrifice-preview-replay-calibration.mt2f"));
    SacrificePreviewChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-consume-kill-copy-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-consume-kill-copy-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-consume-kill-copy-replay-calibration.mt2f"));
    using var observation = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-consume-kill-copy-observation-calibration.mt2f"));
    ConsumeKillCopyChecks.Pair(source.RootElement, replay.RootElement, observation.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-recursion-permafrost-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-recursion-permafrost-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-recursion-permafrost-replay-calibration.mt2f"));
    RecursionDrawCallbacksChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-companion-world-roster-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-companion-world-roster-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
    CompanionWorldRosterChecks.Native(path, ModelJson.RelatedFixture(path, "player-companion-world-roster-replay-calibration.mt2f"));
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-pyre-attachment-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-pyre-attachment-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-pyre-attachment-replay-calibration.mt2f"));
    OuterBossPyreAttachmentChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-preview-birth-stage-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-preview-birth-stage-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
    PreviewBirthStageChecks.Native(path, ModelJson.RelatedFixture(path, "player-preview-birth-stage-replay-calibration.mt2f"));
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-preview-birth-binding-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-preview-birth-binding-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-preview-birth-binding-replay-calibration.mt2f"));
    PreviewBirthBindingChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string family in new[] { "player-standby-etch-paired-boss", "player-standby-etch-final-boss" })
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith(family + "-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, family + "-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, family + "-replay-calibration.mt2f"));
    FirstOfKinExhaustChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-rage-room-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-rage-room-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-rage-room-replay-calibration.mt2f"));
    CorruptRoomAttachmentChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-pyre-ascension-queue-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-pyre-ascension-queue-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-pyre-ascension-queue-replay-calibration.mt2f"));
    PyreAscensionQueueChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-deathwish-enchantment-registry-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-deathwish-enchantment-registry-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-deathwish-enchantment-registry-replay-calibration.mt2f"));
    EnchantmentPreviewDeathChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-floor-transition-piercing-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-floor-transition-piercing-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-floor-transition-piercing-replay-calibration.mt2f"));
    FloorTransitionPiercingChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-equipment-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-equipment-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-equipment-replay-calibration.mt2f"));
    CorruptEquipmentChecks.Native(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-juice-magic-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-juice-magic-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var refused = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-magic-observation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-magic-replay-calibration.mt2f"));
    JuiceMagicChecks.Pair(refused.RootElement, source.RootElement, replay.RootElement);
    RoomStatusPolicyChecks.Native(source.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-juice-tome-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-juice-tome-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var refused = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-tome-observation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-tome-replay-calibration.mt2f"));
    JuiceTomeChecks.Pair(refused.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-juice-justcause-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-juice-justcause-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var refused = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-justcause-observation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-justcause-replay-calibration.mt2f"));
    JuiceJustCauseChecks.Pair(refused.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-juice-fracture-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-juice-fracture-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var refused = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-fracture-observation-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-fracture-replay-calibration.mt2f"));
    JuiceFractureChecks.Pair(refused.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-juice-ritual-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-juice-ritual-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-ritual-replay-calibration.mt2f"));
    using var observation = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-juice-ritual-observation-calibration.mt2f"));
    JuiceRitualChecks.Pair(source.RootElement, replay.RootElement, observation.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-infusion-trigger-merge-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-infusion-trigger-merge-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-infusion-trigger-merge-replay-calibration.mt2f"));
    InfusionTriggerMergeChecks.Pair(source.RootElement, replay.RootElement);
}
// Equipment catalogs are required independently of triggered unit summoning.
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-natural-hydra-graft-equipment-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-natural-hydra-graft-equipment-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-natural-hydra-graft-equipment-replay-calibration.mt2f"));
    NaturalHydraCatalogChecks.GeneratedPair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-natural-hydra-catalog-deployment-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-natural-hydra-catalog-deployment-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-natural-hydra-catalog-deployment-replay-calibration.mt2f"));
    NaturalHydraCatalogChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string stem in new[] { "player-undying-copy", "player-undying-permafrost-copy" })
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith(stem + "-deployment-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, stem + "-deployment-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, stem + "-deployment-replay-calibration.mt2f"));
    UndyingCopyOnPlayChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string stem in new[] { "player-undying-copy-effects", "player-undying-permafrost-copy-effects", "player-undying-permafrost-copy-discard" })
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith(stem + "-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, stem + "-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, stem + "-replay-calibration.mt2f"));
    UndyingCopyOnPlayChecks.EffectsPair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-slay-corruption-callback-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-slay-corruption-callback-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-slay-corruption-callback-replay-calibration.mt2f"));
    SlayCorruptionCallbackChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-discard-holdover-return-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-discard-holdover-return-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-discard-holdover-return-replay-calibration.mt2f"));
    DiscardHoldoverReturnChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-guaranteed-status-test-rng-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-guaranteed-status-test-rng-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-guaranteed-status-test-rng-replay-calibration.mt2f"));
    GuaranteedStatusTestRngChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-guaranteed-status-preview-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-guaranteed-status-preview-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-guaranteed-status-preview-replay-calibration.mt2f"));
    GuaranteedStatusPreviewChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-source-alignment-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-source-alignment-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-source-alignment-replay-calibration.mt2f"));
    CorruptSourceAlignmentChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-relic-attachment-replacement-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-relic-attachment-replacement-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-relic-attachment-replacement-replay-calibration.mt2f"));
    RelicAttachmentReplacementChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-enemy-command-attack-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-enemy-command-attack-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-enemy-command-attack-replay-calibration.mt2f"));
    EnemyCommandAttackChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-before-play-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-before-play-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-before-play-replay-calibration.mt2f"));
    CorruptBeforePlayChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-corrupt-copyability-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-corrupt-copyability-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-corrupt-copyability-replay-calibration.mt2f"));
    CorruptCopyabilityChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-destroyed-feeder-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-destroyed-feeder-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-destroyed-feeder-replay-calibration.mt2f"));
    using var raw = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-destroyed-feeder-observation-calibration.mt2f"));
    DestroyedFeederChecks.Pair(source.RootElement, replay.RootElement, raw.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-despawn-frame-removal-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-despawn-frame-removal-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-despawn-frame-removal-replay-calibration.mt2f"));
    using var observation = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-despawn-frame-removal-observation-calibration.mt2f"));
    CombatRemovalTimingChecks.DespawnFramePair(source.RootElement, replay.RootElement, observation.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "player-despawn-listeners-calibration.mt2f" or
    "player-despawn-listeners-replay-calibration.mt2f").Select(path => ModelJson.RelatedFixture(path,
        "player-despawn-listeners-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-despawn-listeners-replay-calibration.mt2f"));
    CombatRemovalTimingChecks.DespawnPair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("player-heal-combat-removal-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "player-heal-combat-removal-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-heal-combat-removal-replay-calibration.mt2f"));
    using var observation = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-heal-combat-removal-observation-calibration.mt2f"));
    CombatRemovalTimingChecks.HealPair(source.RootElement, replay.RootElement, observation.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "player-overcharge-cleanup-calibration.mt2f" or
    "player-overcharge-cleanup-replay-calibration.mt2f").Select(path => ModelJson.RelatedFixture(path,
        "player-overcharge-cleanup-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "player-overcharge-cleanup-replay-calibration.mt2f"));
    OverchargeCleanupChecks.Pair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-harvester-birth-calibration.mt2f"))
{
    using var observation = ModelJson.ReadFixture(path);
    using var fixture = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        "full-battle-harvester-birth-calibration.mt2f"));
    HarvesterBirthChecks.Component(observation.RootElement, fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "parallel-planner-merge-model-calibration.mt2f"))
{
    using var preferred = ModelJson.ReadFixture(path);
    using var alternative = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "parallel-planner-merge-model-counterexample.mt2f"));
    ParallelPlannerChecks.Merge(preferred.RootElement, alternative.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "pending-boss-loss-order-model-calibration.mt2f"))
{
    using var early = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-random-campaign-13-ring7-release137-budget10000-loss.mt2f"));
    using var late = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-random-campaign-13-ring7-release137-refined-loss.mt2f"));
    using var prediction = ModelJson.ReadFixture(path);
    PendingBossLossOrderChecks.Run(early.RootElement, late.RootElement, prediction.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "uninjured-pending-boss-model-counterexample-calibration.mt2f"))
{
    using var early = ModelJson.ReadFixture(path);
    using var late = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "uninjured-pending-boss-model-calibration.mt2f"));
    UninjuredPendingBossChecks.Run(early.RootElement, late.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) ==
    "full-battle-random-campaign-12-ring6-release112-budget10000-loss.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    TacticalSelectionChecks.Native(fixture.RootElement);
    TacticalSelectionChecks.CostRejections(fixture.RootElement);
}
RoomCombatChecks.Run(args.Where(path => !path.Contains("calibration", StringComparison.OrdinalIgnoreCase) &&
    !path.Contains("parallel-planner-merge-model-", StringComparison.Ordinal) &&
    !path.EndsWith("player-resumed-moon-capture-counterexample.mt2f", StringComparison.Ordinal) &&
    !path.EndsWith("player-room-attachment-birth-observed.mt2f", StringComparison.OrdinalIgnoreCase)).ToArray());
foreach (string path in args.Where(path => (Path.GetFileName(path).StartsWith("full-battle-random-campaign-", StringComparison.Ordinal) ||
    Path.GetFileName(path).StartsWith("full-battle-metagame-campaign-", StringComparison.Ordinal)) &&
    (path.EndsWith("-search.mt2f", StringComparison.Ordinal) || path.EndsWith("-loss.mt2f", StringComparison.Ordinal))))
    RandomCampaignChecks.Curated(path);
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-terminal-coroutine-planes-counterexample-calibration.mt2f"))
{
    using var counterexample = ModelJson.ReadFixture(path);
    string sourcePath = ModelJson.RelatedFixture(path, "full-battle-random-campaign-14-ring2-release143-budget10000-search.mt2f");
    string replayPath = ModelJson.RelatedFixture(path, "full-battle-random-campaign-14-ring2-release143-budget10000-replay.mt2f");
    using var source = ModelJson.ReadFixture(sourcePath);
    using var replay = ModelJson.ReadFixture(replayPath);
    TerminalCoroutinePlaneChecks.Pair(counterexample.RootElement, source.RootElement, replay.RootElement);
    TerminalEnemySlotChecks.Stages(sourcePath);
    TerminalEnemySlotChecks.Stages(replayPath);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "player-resumed-moon-capture-counterexample.mt2f"))
{
    using var counterexample = ModelJson.ReadFixture(path);
    using var source = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-random-campaign-14-ring1-release138-budget10000-search.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-random-campaign-14-ring1-release138-budget10000-replay.mt2f"));
    ResumedMoonCaptureChecks.Pair(counterexample.RootElement, source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "player-random-campaign-02-ring3-upgrade-observed-prefix.mt2f" or
    "player-random-campaign-02-ring3-permafrost-copy-observed-prefix.mt2f" or
    "player-random-campaign-02-ring3-fixed-policy-native-win.mt2f" or
    "player-random-campaign-02-ring3-relentless-preview-observed.mt2f"))
    RandomCampaignChecks.Curated(path);

foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-native-covenant-campaign-03-ring6-default-loss.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    NativeCovenantCampaignChecks.DefaultLoss(fixture.RootElement, 3, 5);
}
foreach (int ring in new[] { 2, 3, 4, 5 })
foreach (string path in args.Where(path => Path.GetFileName(path) == $"full-battle-player-native-covenant-campaign-03-ring{ring}-default-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        $"full-battle-player-native-covenant-campaign-03-ring{ring}-default-replay.mt2f"));
    NativeCovenantCampaignChecks.DefaultPair(source.RootElement, replay.RootElement, 3, ring - 1);
}
CorruptPoisonChecks.Run();
foreach (string variant in new[] { "AllyBuffer", "Fragile", "Corrupter" })
{
    string stem = variant.ToLowerInvariant();
    foreach (string path in args.Where(path => Path.GetFileName(path) == $"full-battle-outer-boss-trial-{stem}-casting.mt2f"))
    {
        using var battle = ModelJson.ReadFixture(path);
        using var birth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, $"outer-boss-trial-{stem}-birth-calibration.mt2f"));
        using var phases = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, $"outer-boss-trial-{stem}-phase-calibration.mt2f"));
        using var controller = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, $"boss-action-trial-{stem}-controller-calibration.mt2f"));
        using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, $"full-battle-outer-boss-trial-{stem}-replay.mt2f"));
        using var relentless = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, $"outer-boss-trial-{stem}-relentless-turn-calibration.mt2f"));
        using var relentlessPhases = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, $"outer-boss-trial-{stem}-relentless-phase-calibration.mt2f"));
        OuterBossTrialChecks.Native(variant, battle.RootElement, birth.RootElement, phases.RootElement, controller.RootElement);
        OuterBossTrialChecks.Pair(variant, battle.RootElement, replay.RootElement);
        OuterBossCorrupterChecks.Relentless(relentless.RootElement, relentlessPhases.RootElement,
            OuterBossTrialChecks.Boss(variant), variant == "AllyBuffer" ? 7105 : variant == "Fragile" ? 7961 : 8745);
    }
}
foreach (string path in args.Where(path => Path.GetFileName(path).Contains("corrupter", StringComparison.OrdinalIgnoreCase))
    .GroupBy(path => Path.GetDirectoryName(Path.GetFullPath(path))).Select(group => group.First()))
{
    using var battle = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-outer-boss-corrupter-casting.mt2f"));
    using var birth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "outer-boss-corrupter-birth-calibration.mt2f"));
    using var phases = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "outer-boss-corrupter-phase-calibration.mt2f"));
    using var controller = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "boss-action-corrupter-controller-calibration.mt2f"));
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-outer-boss-corrupter-casting-replay.mt2f"));
    using var relentless = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "outer-boss-corrupter-relentless-turn-calibration.mt2f"));
    using var relentlessPhases = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "outer-boss-corrupter-relentless-phase-calibration.mt2f"));
    OuterBossCorrupterChecks.Native(battle.RootElement, birth.RootElement, phases.RootElement, controller.RootElement);
    OuterBossCorrupterChecks.Pair(battle.RootElement, replay.RootElement);
    OuterBossCorrupterChecks.Relentless(relentless.RootElement, relentlessPhases.RootElement);
}
foreach (string path in args.Where(path => path.Contains("full-battle-outer-boss-fragile-no-cards", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossVariantChecks.Battle(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("outer-boss-fragile-birth-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossBirthChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("outer-boss-fragile-phase-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    bool partial = !fixture.RootElement.GetProperty("ForcedSpawnQueueEmpty").GetBoolean();
    OuterBossPhaseChecks.Native(fixture.RootElement, "Boss_Level8_Fragile", earlyDefeat: partial);
}
foreach (string path in args.Where(path => path.Contains("outer-boss-fragile-relentless-phase-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossPhaseChecks.Native(fixture.RootElement, "Boss_Level8_Fragile");
}
foreach (string path in args.Where(path => path.Contains("outer-boss-fragile-pyre-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossPyreChecks.Native(fixture.RootElement, "Boss_Level8_Fragile", pyreHealthControl: true);
}
foreach (string path in args.Where(path => path.Contains("outer-boss-fragile-pyre-turn-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossPyreChecks.Turns(fixture.RootElement, "Boss_Level8_Fragile", pyreHealthControl: true);
}
foreach (string path in args.Where(path => path.Contains("boss-action-fragile-controller-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    BossActionControllerChecks.Native(fixture.RootElement, "Boss_Level8_Fragile", minimumOperations: 6);
}
foreach (string path in args.Where(path => path.Contains("outer-boss-pyre-turn-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("outer-boss-pyre-victory-turn-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossPyreChecks.Turns(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("outer-boss-pyre-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("outer-boss-pyre-victory-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossPyreChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("outer-boss-birth-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("outer-boss-instantiation-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("outer-boss-occupied-birth-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    if (Path.GetFileName(path) == "outer-boss-birth-calibration-angel-duo.mt2f")
        AngelDuoBirthChecks.Native(fixture.RootElement);
    else if (Path.GetFileName(path) == "outer-boss-birth-calibration-angel-duo-natural-covenant03.mt2f")
        AngelDuoBirthChecks.Native(fixture.RootElement, natural: true);
    else OuterBossBirthChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-native-covenant-02-angel-duo.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoBirthChecks.Battle(fixture.RootElement, path);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-angel-duo-natural-covenant03.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoBirthChecks.Battle(fixture.RootElement, path, natural: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "angel-duo-relentless-entry-calibration.mt2f" or
    "angel-duo-paired-phase-calibration.mt2f" or "angel-duo-paired-pyre-calibration.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    if (Path.GetFileName(path) == "angel-duo-relentless-entry-calibration.mt2f") AngelDuoRelentlessChecks.Entry(fixture.RootElement);
    else if (Path.GetFileName(path) == "angel-duo-paired-phase-calibration.mt2f") AngelDuoRelentlessChecks.Phases(fixture.RootElement);
    else AngelDuoRelentlessChecks.Pyre(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "angel-duo-inner-death-calibration.mt2f" or "angel-duo-outer-death-calibration.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoDeathChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "angel-duo-companion-phase-calibration.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoCompanionPhaseChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-natural-angel-pair-cache-", StringComparison.OrdinalIgnoreCase))
    .Select(path => ModelJson.RelatedFixture(path, "full-battle-natural-angel-pair-cache-calibration.mt2f")).Distinct(StringComparer.OrdinalIgnoreCase))
{
    NaturalAngelPairCacheChecks.Pair(path, ModelJson.RelatedFixture(path, "full-battle-natural-angel-pair-cache-replay-calibration.mt2f"));
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "angel-duo-forced-relentless-calibration.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoForcedRelentlessChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-angel-duo-incant.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoIncantChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-pyrebane-targets-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    PyreBaneChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-controller-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossPhaseChecks.Controllers(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-phase-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("true-final-boss-exhausted-phase-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossPhaseChecks.Phases(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-spawn-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossSpawnChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-scheduled-spawn-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossScheduledChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-continuation-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossCombatFlowChecks.Continuation(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-passive-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossCombatFlowChecks.Passive(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-unit-turn-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossUnitTurnChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-dormant", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossDormantChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-relentless-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossRelentlessChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("true-final-boss-ordinary-ally-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossOrdinaryAllyChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("true-final-boss-ally-upgrade-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossAllyUpgradeChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("true-final-boss-paid-spell-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossPaidSpellChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("true-final-boss-allied-paid-spell-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossPaidSpellChecks.Native(fixture.RootElement, allied: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("true-final-boss-triggered-paid-spell-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossPaidSpellChecks.Native(fixture.RootElement, allied: true, triggeredAlly: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path).Equals("true-final-boss-triggered-command.mt2f", StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(path).Equals("true-final-boss-triggered-pyre-command.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossCommandedAttackChecks.Native(fixture.RootElement,
        pyreCommand: Path.GetFileName(path).Contains("pyre", StringComparison.OrdinalIgnoreCase), triggeredAlly: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path).Equals("true-final-boss-pyre-command.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossCommandedAttackChecks.Native(fixture.RootElement, pyreCommand: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path).Equals("true-final-boss-commanded-attack.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossCommandedAttackChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("true-final-boss-protected-spell-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossProtectedSpellChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).Equals("true-final-boss-triggered-ordinary-pyre.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossAlliedLethalChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-lethal-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("true-final-boss-lethal-preview-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossLethalChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-multi-allied-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossAlliedChecks.Native(fixture.RootElement, multi: true);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-allied-battle-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossAlliedChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-full-battle-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossBattleChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-covenant-outer-boss-", StringComparison.OrdinalIgnoreCase)))
    CovenantOuterBossChecks.CuratedNative(path);

foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-chosen-incant-attack.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    StartingCovenantChecks.ChosenNative(fixture.RootElement, path);
}

foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-native-covenant-outer-boss-", StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(path).StartsWith("full-battle-player-native-covenant-outer-boss-", StringComparison.OrdinalIgnoreCase) &&
    Path.GetFileName(path).EndsWith("-search.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    StartingCovenantChecks.CuratedOuter(fixture.RootElement, path, Path.GetFileName(path).StartsWith("full-battle-player-", StringComparison.OrdinalIgnoreCase));
}

foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-native-covenant-final-boss-", StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(path).StartsWith("full-battle-player-native-covenant-final-boss-", StringComparison.OrdinalIgnoreCase) &&
    Path.GetFileName(path).EndsWith("-search.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    StartingCovenantChecks.CuratedFinal(fixture.RootElement, path, Path.GetFileName(path).StartsWith("full-battle-player-", StringComparison.OrdinalIgnoreCase));
}

foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-native-covenant-startup-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    StartingCovenantChecks.Native(fixture.RootElement, path);
}

foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-player-native-covenant-startup", StringComparison.Ordinal) &&
    Path.GetFileName(path).EndsWith("-search.mt2f", StringComparison.Ordinal)))
{
    using var source = ModelJson.ReadFixture(path);
    string suffix = Path.GetFileNameWithoutExtension(path)["full-battle-player-native-covenant-startup".Length..^"-search".Length];
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-native-covenant-startup" + suffix + "-replay.mt2f"));
    StartingCovenantChecks.Pair(source.RootElement, replay.RootElement, suffix.Length > 0 ? int.Parse(suffix[1..]) : 1);
}

foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-player-covenant-outer-boss-", StringComparison.OrdinalIgnoreCase) &&
    path.EndsWith("-search.mt2f", StringComparison.OrdinalIgnoreCase)))
    CovenantOuterBossChecks.CuratedPair(path);

foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-covenant-enemy-stats-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    CovenantEnemyStatsChecks.Native(fixture.RootElement, path);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-covenant-battle-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossBattleChecks.Covenant(fixture.RootElement, path);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-preview-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossPreviewChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-post-combat-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossPostCombatChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-birth-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossChecks.Birth(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-linked-health-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    LinkedHealthChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-life-link-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    LifeLinkChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-health-updates-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossHealthChecks.Native(fixture.RootElement);
    LinkedBossHealthChecks.Bridge(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-live-world-observation", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    LinkedBossHealthChecks.Capture(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("true-final-boss-damage-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("true-final-boss-all-target-damage-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("true-final-boss-spell-damage-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossDamageChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("true-final-boss-death-", StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(path).StartsWith("true-final-boss-terminal-damage-", StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(path).StartsWith("true-final-boss-terminal-spell-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    TrueFinalBossDeathChecks.Native(fixture.RootElement, Path.GetFileName(path).StartsWith("true-final-boss-terminal-", StringComparison.OrdinalIgnoreCase));
}
foreach (string path in args.Where(path => path.Contains("outer-boss-turn-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossTurnChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("full-battle-progressed-ring8-", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossTurnChecks.Deployment(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("outer-boss-phase-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("outer-boss-relentless-phase-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("outer-boss-occupied-phase-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("outer-boss-natural-relentless-phase-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OuterBossPhaseChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("boss-action-controller-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    BossActionControllerChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("boss-action-target-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    BossActionTargetChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("retired-unit-destruction-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    RetiredUnitDestructionChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("corruption-status-calibration", StringComparison.OrdinalIgnoreCase)))
    CorruptionStatusChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("undying-boss-preview-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    UndyingBossPreviewChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("intrinsic-opening-draw-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    IntrinsicOpeningDrawChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("planner-deployment-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    PlannerDeploymentChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.Contains("planner-score-plateau-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    PlannerPlateauChecks.Native(fixture.RootElement);
}
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
foreach (string path in args.Where(path => path.Contains("conduit-movement-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("conduit-enemy-movement-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    ConduitMovementChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("room-moon-callback-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("room-moon-movement-calibration", StringComparison.OrdinalIgnoreCase) ||
    path.Contains("room-moon-enemy-movement-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    RoomMoonCallbackChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-eclipse-chamber-movement.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    EclipseChamberChecks.Movement(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-eclipse-chamber-enemy-movement.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-eclipse-chamber-enemy-movement-replay.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-eclipse-chamber-enemy-callbacks.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-eclipse-chamber-enemy-callbacks-replay.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    EclipseChamberChecks.EnemyMovement(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-eclipse-chamber-enemy-conduit.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-eclipse-chamber-enemy-conduit-replay.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    EclipseChamberChecks.EnemyConduit(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-eclipse-chamber-mixed-conduit.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-eclipse-chamber-mixed-conduit-replay.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    MixedConduitChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("room-moon-spell-upgrade-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-threshold-relic-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    Path.GetFileName(path).StartsWith("room-moon-spell-covenant-", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-status-relic-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-hand-power-relic-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-phase-heal-relic-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-phase-energy-relic-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-healing-relic-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-cost-relic-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-attack-relic-calibration.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("room-moon-spell-advance-calibration.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    if (Path.GetFileName(path) == "room-moon-spell-threshold-relic-calibration.mt2f")
        MoonSpellThresholdCompositionChecks.Component(document.RootElement);
    else RoomMoonSpellUpgradeChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("player-moon-room-turn-calibration.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    PlayerMoonRoomSpellChecks.TurnBoundary(document.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-moon-room-phase-energy-relic-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        "full-battle-player-moon-room-phase-energy-relic-replay.mt2f"));
    PlayerMoonRoomSpellChecks.Pair(source.RootElement, replay.RootElement, requirePhaseEnergyRelic: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-moon-room-threshold-relic-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        "full-battle-player-moon-room-threshold-relic-replay.mt2f"));
    MoonSpellThresholdCompositionChecks.PlayerPair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-moon-room-status-relic-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        "full-battle-player-moon-room-status-relic-replay.mt2f"));
    PlayerMoonRoomSpellChecks.Pair(source.RootElement, replay.RootElement, requireStatusRelic: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-moon-room-covenant03-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        "full-battle-player-moon-room-covenant03-replay.mt2f"));
    PlayerMoonRoomSpellChecks.Pair(source.RootElement, replay.RootElement, requireCovenant03: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is "heavenfall-focus-native-prefix.mt2f" or "heavenfall-death-focus-native-prefix.mt2f"))
{
    using var document = ModelJson.ReadFixture(path);
    HeavenfallFocusChecks.Prefix(document.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-moon-room-hand-power-relic-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        "full-battle-player-moon-room-hand-power-relic-replay.mt2f"));
    PlayerMoonRoomSpellChecks.Pair(source.RootElement, replay.RootElement, requireHandPowerRelic: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-moon-room-phase-heal-relic-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        "full-battle-player-moon-room-phase-heal-relic-replay.mt2f"));
    PlayerMoonRoomSpellChecks.Pair(source.RootElement, replay.RootElement, requirePhaseHealRelic: true);
}
foreach (string path in args.Where(path => path.EndsWith("player-fragile-budget-boundary.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    ShorterAdviceBudgetChecks.Run(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("advisor-atomic-budget-failure.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    AtomicAdviceBudgetChecks.Run(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("advisor-ordinary-budget-failure.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OrdinaryAdviceBudgetChecks.Run(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-original-moon-card-upgrades-room-mask.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-threshold-relic.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-status-relic.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-covenant03.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-hand-power-relic.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-phase-heal-relic.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-phase-energy-relic.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-healing-relic.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-healing-relic-replay.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-cost-relic.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-cost-relic-replay.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-attack-relic.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-attack-relic-replay.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-lunacy.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-lunacy-replay.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-room-mask-replay.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-original-moon-card-upgrades-replay.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    OriginalMoonCardUpgradesChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("holdover-variants-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    HoldoverVariantChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("full-battle-retired-aura-card-kill", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    RetiredAuraChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-train-heal-per-ember.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    EmberTrainHealChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-train-heal-per-ember-x.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    EmberTrainHealChecks.XCost(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("player-terminal-copy-target-calibration.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    PlayerTerminalCopyChecks.TargetBoundary(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("full-battle-terminal-purge-copy", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    TerminalPurgeCopyChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("full-battle-terminal-copy-on-play", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    TerminalCopyOnPlayChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("full-battle-copy-on-play", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    CopyOnPlayChecks.Native(document.RootElement);
}
foreach (string prefix in new[] { "ordinary-continuation", "cross-turn-scroll" })
{
    string? sourcePath = args.FirstOrDefault(path => path.EndsWith($"full-battle-player-{prefix}-search.mt2f", StringComparison.OrdinalIgnoreCase));
    string? replayPath = args.FirstOrDefault(path => path.EndsWith($"full-battle-player-{prefix}-replay.mt2f", StringComparison.OrdinalIgnoreCase));
    if (sourcePath != null && replayPath != null)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        using var replay = ModelJson.ReadFixture(replayPath);
        OrdinaryAdviceChecks.Run(source.RootElement);
        OrdinaryAdviceChecks.NativePair(source.RootElement, replay.RootElement);
        FloorBrowseAdviceChecks.NativePair(source.RootElement, replay.RootElement);
    }
}
foreach (string path in args.Where(path => path.EndsWith("player-live-conduit-floor-scroll.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    FloorBrowseAdviceChecks.LiveScroll(fixture.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("player-live-moon-continuation.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    LiveMoonContinuationChecks.Run(fixture.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("player-live-arkion-pyre-unavailable.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var live = ModelJson.ReadFixture(path);
    using var native = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-live-arkion-pyre.mt2f"));
    LiveArkionPyreChecks.Run(live.RootElement, native.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-player-live-arkion-pyre.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var native = ModelJson.ReadFixture(path);
    using var birth = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "outer-boss-birth-live-arkion-pyre.mt2f"));
    LiveArkionPyreChecks.Native(native.RootElement, birth.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("player-live-arkion-pyre-observed-prefix.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    RollingAdviceChecks.ObservedSuccessors(fixture.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("player-live-empty-deployment-advice.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    DeploymentAdviceBudgetChecks.Run(fixture.RootElement);
}

foreach (string path in args.Where(path => path.EndsWith("player-live-forsaken-trial.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OpeningForsakenTrialChecks.Live(fixture.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-original-opening-forsaken-covenant10.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    OpeningForsakenTrialChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-original-moon-card-upgrades-live-relics-covenant10.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    LiveMoonContinuationChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("player-room-attachment-birth-observed.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    RoomAttachmentBirthChecks.Observed(document.RootElement);
    RollingAdviceChecks.ObservedSuccessors(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-stewards-quarters-birth-order.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    StewardsQuartersChecks.Native(document.RootElement);
}
{
    string? sourcePath = args.FirstOrDefault(path => path.EndsWith("full-battle-player-natural-ring7-ordinary-search.mt2f", StringComparison.OrdinalIgnoreCase));
    string? replayPath = args.FirstOrDefault(path => path.EndsWith("full-battle-player-natural-ring7-ordinary-replay.mt2f", StringComparison.OrdinalIgnoreCase));
    if (sourcePath != null && replayPath != null)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        using var replay = ModelJson.ReadFixture(replayPath);
        BattleStateKeyChecks.Run(source.RootElement);
        NaturalRing7TacticsChecks.RestartPair(source.RootElement, replay.RootElement, defaults: true);
        OrdinaryAdviceChecks.NativePair(source.RootElement, replay.RootElement);
    }
}
foreach (string prefix in new[] { "natural-ring7-short-budget", "state-key", "boss-fragile-short-budget" })
{
    string? sourcePath = args.FirstOrDefault(path => path.EndsWith($"full-battle-player-{prefix}-search.mt2f", StringComparison.OrdinalIgnoreCase));
    string? replayPath = args.FirstOrDefault(path => path.EndsWith($"full-battle-player-{prefix}-replay.mt2f", StringComparison.OrdinalIgnoreCase));
    if (sourcePath == null || replayPath == null) continue;
    using var source = ModelJson.ReadFixture(sourcePath);
    using var replay = ModelJson.ReadFixture(replayPath);
    if (prefix == "state-key") BattleStateKeyChecks.NativePair(source.RootElement, replay.RootElement);
    else if (prefix == "boss-fragile-short-budget")
    {
        PlayerOuterBossChecks.FragileShortBudgetPair(source.RootElement, replay.RootElement);
        string? failurePath = args.FirstOrDefault(path => path.EndsWith("advisor-ordinary-budget-failure.mt2f", StringComparison.OrdinalIgnoreCase));
        if (failurePath != null)
        {
            using var failure = ModelJson.ReadFixture(failurePath);
            OrdinaryAdviceBudgetChecks.NativeRecovery(failure.RootElement, source.RootElement);
        }
    }
    else NaturalRing7TacticsChecks.DefaultWinningPair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => path.Contains("full-battle-natural-ring7-copy-on-play", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    CopyOnPlayChecks.Native(document.RootElement, natural: true);
    NaturalMovementCaptureChecks.Native(document.RootElement);
    BattlePlannerChecks.TacticalSeed(document.RootElement, naturalRing7: true);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-natural-ring7-tactical-win.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    NaturalRing7TacticsChecks.Native(document.RootElement);
}
{
    string? sourcePath = args.FirstOrDefault(path => path.EndsWith("full-battle-player-natural-ring7-search.mt2f", StringComparison.OrdinalIgnoreCase));
    string? replayPath = args.FirstOrDefault(path => path.EndsWith("full-battle-player-natural-ring7-replay.mt2f", StringComparison.OrdinalIgnoreCase));
    if (sourcePath != null && replayPath != null)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        using var replay = ModelJson.ReadFixture(replayPath);
        NaturalRing7TacticsChecks.PlayerPair(source.RootElement, replay.RootElement);
    }
}
{
    foreach (string preset in new[] { "clean", "default" })
    {
        string? source = args.FirstOrDefault(path => path.EndsWith($"full-battle-player-natural-ring7-{preset}-search.mt2f", StringComparison.OrdinalIgnoreCase));
        string? replay = args.FirstOrDefault(path => path.EndsWith($"full-battle-player-natural-ring7-{preset}-replay.mt2f", StringComparison.OrdinalIgnoreCase));
        if (source != null && replay != null)
        {
            using var first = ModelJson.ReadFixture(source);
            using var second = ModelJson.ReadFixture(replay);
            NaturalRing7TacticsChecks.RestartPair(first.RootElement, second.RootElement, defaults: preset == "default");
        }
    }
}
{
    string? sourcePath = args.FirstOrDefault(path => path.EndsWith("full-battle-player-campaign-trial-gold-search.mt2f", StringComparison.OrdinalIgnoreCase));
    string? replayPath = args.FirstOrDefault(path => path.EndsWith("full-battle-player-campaign-trial-gold-replay.mt2f", StringComparison.OrdinalIgnoreCase));
    if (sourcePath != null && replayPath != null)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        using var replay = ModelJson.ReadFixture(replayPath);
        CampaignTrialGoldBattleChecks.PlayerPair(source.RootElement, replay.RootElement);
    }
}
{
    string? sourcePath = args.FirstOrDefault(path => path.EndsWith("full-battle-player-boss-corrupter-search.mt2f", StringComparison.OrdinalIgnoreCase));
    string? replayPath = args.FirstOrDefault(path => path.EndsWith("full-battle-player-boss-corrupter-replay.mt2f", StringComparison.OrdinalIgnoreCase));
    if (sourcePath != null && replayPath != null)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        using var replay = ModelJson.ReadFixture(replayPath);
        PlayerOuterBossChecks.CorrupterWinningPair(source.RootElement, replay.RootElement);
    }
}
foreach (var variant in new[] { (Name: "mass", Native: "Mass"), (Name: "corruption", Native: "Corruption"),
    (Name: "spikes", Native: "Spikes") })
{
    foreach (string path in args.Where(path => Path.GetFileName(path) == "outer-boss-birth-calibration-fleshweaver-" + variant.Name + ".mt2f"))
    {
        using var fixture = ModelJson.ReadFixture(path);
        FleshweaverChecks.Birth(fixture.RootElement, variant.Native);
    }
    foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-fleshweaver-" + variant.Name + "-baseline.mt2f" ||
        Path.GetFileName(path) == "full-battle-fleshweaver-" + variant.Name + "-native-spells.mt2f"))
    {
        using var fixture = ModelJson.ReadFixture(path);
        FleshweaverChecks.Battle(fixture.RootElement, path, variant.Native, Path.GetFileName(path).Contains("native-spells"));
    }
    foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-fleshweaver-" + variant.Name + "-search.mt2f"))
    {
        using var source = ModelJson.ReadFixture(path);
        using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-fleshweaver-" + variant.Name + "-replay.mt2f"));
        NativeCovenantCampaignChecks.FleshweaverPair(source.RootElement, replay.RootElement, variant.Native);
    }
}
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "full-battle-arkion-rage-baseline.mt2f" or "full-battle-arkion-rage-native-spells.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    ArkionRageChecks.Battle(fixture.RootElement, path, Path.GetFileName(path).Contains("native-spells"));
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-arkion-rage-natural-covenant03.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    ArkionRageChecks.Battle(fixture.RootElement, path, requireGrowth: false, natural: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "outer-boss-birth-calibration-arkion-rage-natural-covenant03.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    ArkionRageChecks.Birth(fixture.RootElement, natural: true);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-arkion-rage-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-arkion-rage-replay.mt2f"));
    NativeCovenantCampaignChecks.ArkionRagePair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "outer-boss-birth-calibration-arkion-rage.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    ArkionRageChecks.Birth(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "full-battle-arkion-push-baseline.mt2f" or "full-battle-arkion-push-native-spells.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    ArkionPushChecks.Battle(fixture.RootElement, path, Path.GetFileName(path).Contains("native-spells"));
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "outer-boss-birth-calibration-arkion-push.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    ArkionPushChecks.Birth(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-arkion-push-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-arkion-push-replay.mt2f"));
    NativeCovenantCampaignChecks.ArkionPushPair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-arkion-pyre.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    ArkionPyreChecks.Battle(fixture.RootElement, path);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "outer-boss-birth-calibration-arkion-pyre.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    ArkionPyreChecks.Birth(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-arkion-pyre-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-arkion-pyre-replay.mt2f"));
    NativeCovenantCampaignChecks.ArkionPyrePair(source.RootElement, replay.RootElement);
}
foreach (var variant in new[] { (Name: "atkhp", Native: "AtkHP"), (Name: "damage-shield", Native: "DamageShield"),
    (Name: "lifesteal", Native: "Lifesteal") })
{
    foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-cael-" + variant.Name + "-search.mt2f"))
    {
        using var source = ModelJson.ReadFixture(path);
        using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-cael-" + variant.Name + "-replay.mt2f"));
        NativeCovenantCampaignChecks.CaelPair(source.RootElement, replay.RootElement, variant.Native);
    }
    foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-cael-" + variant.Name + ".mt2f"))
    {
        using var fixture = ModelJson.ReadFixture(path);
        CaelChecks.Battle(fixture.RootElement, path, variant.Native);
    }
    foreach (string path in args.Where(path => Path.GetFileName(path) == "outer-boss-birth-calibration-cael-" + variant.Name + ".mt2f"))
    {
        using var fixture = ModelJson.ReadFixture(path);
        CaelChecks.Birth(fixture.RootElement, variant.Native);
    }
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-angel-duo-loop-resolve.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoVariantChecks.Battle(fixture.RootElement, path);
}
foreach (string path in args.Where(path => Path.GetFileName(path) is
    "full-battle-angel-duo-revenge-baseline.mt2f" or "full-battle-angel-duo-revenge-harvest.mt2f" or
    "full-battle-angel-duo-revenge-native-spells.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoVariantChecks.RevengeHarvest(fixture.RootElement, path,
        Path.GetFileName(path) != "full-battle-angel-duo-revenge-baseline.mt2f",
        Path.GetFileName(path) != "full-battle-angel-duo-revenge-native-spells.mt2f");
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "angel-duo-harvest-calibration.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoHarvestChecks.Native(fixture.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-angel-duo-loop-resolve-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-angel-duo-loop-resolve-replay.mt2f"));
    NativeCovenantCampaignChecks.AngelDuoVariantPair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "full-battle-player-angel-duo-revenge-harvest-search.mt2f"))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-angel-duo-revenge-harvest-replay.mt2f"));
    NativeCovenantCampaignChecks.AngelDuoVariantPair(source.RootElement, replay.RootElement, "RevengeHarvest");
}
foreach (string path in args.Where(path => Path.GetFileName(path) == "outer-boss-birth-calibration-angel-duo-revenge-harvest.mt2f"))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoVariantChecks.Birth(fixture.RootElement, "RevengeHarvest");
}
foreach (string path in args.Where(path => path.EndsWith("outer-boss-birth-calibration-angel-duo-loop-resolve.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    AngelDuoVariantChecks.Birth(fixture.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-player-advisor-capture-search.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-advisor-capture-replay.mt2f"));
    Check(source.RootElement.GetProperty("PlayerAdvisor").GetProperty("Overlay")
        .GetProperty("PublicationRootChecks").GetInt32() > 0, "Missing mandatory fresh-publication observations.");
    StartingCovenantChecks.Pair(source.RootElement, replay.RootElement, 2);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-player-angel-duo-search.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path, "full-battle-player-angel-duo-replay.mt2f"));
    NativeCovenantCampaignChecks.AngelDuoPair(source.RootElement, replay.RootElement);
}
foreach (string path in args.Where(path => Path.GetFileName(path).StartsWith("full-battle-player-native-covenant-campaign-", StringComparison.Ordinal) &&
    path.EndsWith("-search.mt2f", StringComparison.Ordinal)))
{
    var name = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(path),
        @"^full-battle-player-native-covenant-campaign-(?<rank>\d{2})-ring(?<ring>[2-8])-search\.mt2f$");
    Check(name.Success, "Invalid curated cumulative campaign fixture name.");
    using var source = ModelJson.ReadFixture(path);
    using var replay = ModelJson.ReadFixture(ModelJson.RelatedFixture(path,
        Path.GetFileName(path).Replace("-search.mt2f", "-replay.mt2f", StringComparison.Ordinal)));
    NativeCovenantCampaignChecks.Pair(source.RootElement, replay.RootElement,
        int.Parse(name.Groups["rank"].Value), int.Parse(name.Groups["ring"].Value) - 1);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-natural-fragile-no-cards.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-natural-fragile-status.mt2f", StringComparison.OrdinalIgnoreCase) ||
    path.EndsWith("full-battle-natural-fragile-titanite.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    NaturalFragileChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("full-battle-event-grafted-equipment", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    EventGraftChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.EndsWith("full-battle-conditional-pyre-lethal.mt2f", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    ConditionalPyreDamageChecks.Lethal(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("titanite-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    TitaniteChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("status-trigger-order-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var fixture = ModelJson.ReadFixture(path);
    StatusTriggerOrderChecks.Native(fixture.RootElement, damage: true);
}
foreach (string path in args.Where(path => path.Contains("titanite-combinations-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    TitaniteCombinationChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("titanite-weakness-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    TitaniteWeaknessChecks.Native(document.RootElement);
}
foreach (string path in args.Where(path => path.Contains("boss-preview-sweep-calibration", StringComparison.OrdinalIgnoreCase)))
    BossPreviewSweepChecks.Native(path);
foreach (string path in args.Where(path => path.Contains("permanent-trait-calibration", StringComparison.OrdinalIgnoreCase)))
{
    using var document = ModelJson.ReadFixture(path);
    PermanentTraitChecks.Native(document.RootElement);
}
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
foreach (string path in args.Where(path => Path.GetFileName(path).Equals("rng-calibration.mt2f", StringComparison.OrdinalIgnoreCase)))
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
