using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MonsterTrain2Poju.Capture;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    // Capture the actual whole battle while comparing the independently modeled room stages.
    // An unsupported stage is evidence of missing coverage, never counted as a pass.
    internal sealed class FullBattleTrace
    {
        private readonly ManualLogSource log;
        private readonly UnitPlayModelProbe projection;
        private readonly CardCycleProbe cardCycles;
        private readonly TrainCombatProbe trainCombat;
        private readonly EnemySpawningProbe spawning;
        private readonly BattleTurnProbe turns;
        private readonly BattleActionProbe actions;
        private readonly Dictionary<CharacterState, int> identities = new Dictionary<CharacterState, int>();
        private readonly Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>> triggerIdentities =
            new Dictionary<CharacterState, Dictionary<CharacterTriggerState, int>>();
        private readonly HashSet<string> checkedCopyCardIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<StageRecord> stages = new List<StageRecord>();
        private readonly List<object> checkpoints = new List<object>();
#if DEVELOPMENT_PROBE
        private object? nativeRunInput;
#endif
        private int nextId = 1;
        private int phaseSequence;
        private bool copyCardlessStatusRequired;
        private bool normalizeDestroyedAttacker;
        internal bool CanonicalDecisionCapture => normalizeDestroyedAttacker;
        internal T CaptureDecision<T>(Func<T> capture)
        {
            bool previous = normalizeDestroyedAttacker;
            normalizeDestroyedAttacker = true;
            try { return capture(); }
            finally { normalizeDestroyedAttacker = previous; }
        }
        internal int NextPhaseSequence() => ++phaseSequence;
        internal TrainCombatState CaptureTrain(bool includeOuterBossForBump = false) =>
            trainCombat.Capture(includeOuterBossForBump: includeOuterBossForBump);
        internal TrainCombatState CaptureEnchantmentRooms() => trainCombat.Capture(false, includeOuterBossForBump: OuterBossPhaseProbe.CaptureAnchors);
        internal void InvalidateCardRules() => actions.InvalidateRules();
        internal EnemySpawnState CaptureSpawnPhase() => spawning.CapturePhase();
        internal EnemySpawnState CaptureCanonicalSpawn() => spawning.Capture();
        internal TrainCombatState CaptureCanonicalTrain() => CaptureDecision(() => trainCombat.Capture());
        internal int[] PendingDestroyedUnitIds() => identities.Where(pair => pair.Key != null && pair.Key.IsDestroyed)
            .Select(pair => pair.Value).OrderBy(id => id).ToArray();
        internal int NextUnitId => nextId;
        internal IReadOnlyList<CardState> KnownCards => projection.KnownCards.ToArray();
        internal IReadOnlyList<CharacterState> KnownUnits => identities.Keys.ToArray();
        internal int UnitId(CharacterState unit)
        {
            if (!identities.TryGetValue(unit, out int id))
            {
                identities.Add(unit, id = nextId++);
                EnchanterRemovalProbe.MapUnit(unit.GetInstanceID(), id);
            }
            return id;
        }
        internal bool TryGetUnitId(CharacterState unit, out int id) => identities.TryGetValue(unit, out id);
        internal IReadOnlyDictionary<CharacterTriggerState, int> TriggerStates(CharacterState unit)
        {
            if (!triggerIdentities.TryGetValue(unit, out var states))
                triggerIdentities.Add(unit, states = new Dictionary<CharacterTriggerState, int>());
            foreach (CharacterTriggerState trigger in unit.GetTriggers())
                if (!states.ContainsKey(trigger)) states.Add(trigger, states.Count);
            return states;
        }
        internal int TriggerStateId(CharacterState unit, CharacterTriggerState trigger)
        {
            TriggerStates(unit);
            var states = triggerIdentities[unit];
            if (!states.TryGetValue(trigger, out int id)) states.Add(trigger, id = states.Count);
            return id;
        }
        private bool calibrated;
        private bool? nativeEnchantmentWorld;
        private bool playerEnchantmentWorld;
        private readonly HashSet<CharacterData> checkedEnchantmentDefinitions = new HashSet<CharacterData>();
        internal bool CapturesNativeEnchantments
        {
            get
            {
                if (OuterBossPhaseProbe.TurnsEnabled || playerEnchantmentWorld ||
                    (nativeEnchantmentWorld ??= EnemySpawningProbe.RequiresEnchantmentWorld())) return true;
                // A player unit can introduce an aura even when every enemy wave
                // is aura-free. Revisit newly discovered/generated definitions.
                foreach (CharacterData data in projection.KnownCards.SelectMany(card => card.GetEffects())
                    .Select(effect => effect.GetParamCharacterData()).Concat(identities.Keys.Select(unit => unit.GetSourceCharacterData()))
                    .Where(data => data != null))
                    if (checkedEnchantmentDefinitions.Add(data) && data.GetTriggers().Any(trigger =>
                        trigger.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectEnchant")))
                        return playerEnchantmentWorld = true;
                return false;
            }
        }
        internal static FullBattleTrace? Active { get; private set; }
        internal void Detach() { if (Active == this) Active = null; }
        internal bool? NativeWon { get; private set; }
        private bool? stoppingOutcome;
        internal bool? TerminalEffectsSettled { get; private set; }
        internal BattleTurnState? TerminalDecision { get; private set; }
        private int captureFailures;
        internal int CaptureFailures => RelicSpawnStatusProbe.Records.Count(record => !record.Completed || record.After == null) + SpawnEnchantScenario.Records.Count(record => !record.Completed || record.After == null) + (HeroCopyScenario.Error == null ? 0 : 1) + (UnitCopyScenario.Error == null ? 0 : 1) + (UnitCloneScenario.Error == null ? 0 : 1) + (BumpScenario.Error == null ? 0 : 1) + (HordeMergeScenario.Error == null ? 0 : 1) + (RevivalScenario.Error == null ? 0 : 1) + RevivalProbe.Records.Count(record => record.Error != null) + TriggeredSummonProbe.Damages.Count(record => record.Error != null) + TriggeredSummonProbe.Records.Count(record => record.Error != null) + BattleSpawnPointProbe.Compactions.Count(record => record.Error != null) + SpawnPointScenario.Records.Count(record => !record.Completed) + (SpawnPointScenario.Error == null ? 0 : 1) + PoolSummonProbe.Records.Count(record => !record.Completed) + FreshSpawnerProbe.Records.Count(record => !record.Completed) + FreshSpawnerProbe.Checks.Count(record => !record.Completed) + SpawnUpgradeProbe.Records.Count(record => !record.Completed) + (MultiSummonScenario.Error == null ? 0 : 1) + UnitBirthProbe.Clones.Count(record => !record.Completed) + UnitBirthProbe.Records.Count(record => !record.Completed) + (RallyScenario.Error == null ? 0 : 1) + (DyingHordeUpgradeScenario.Error == null ? 0 : 1) + (HordeUpgradeScenario.Error == null ? 0 : 1) + (HordeDeathScenario.Error == null ? 0 : 1) + (HordeRemovalScenario.Error == null ? 0 : 1) + (HarvestScenario.Error == null ? 0 : 1) + (EquipmentScenario.Error == null ? 0 : 1) + (ConditionalTriggerScenario.Error == null ? 0 : 1) + captureFailures + (TriggerMutationScenario.Error == null ? 0 : 1) + (DirectUnitUpgradeScenario.Error == null ? 0 : 1) + UncontrolledGroupGrowthProbe.Records.Count(record => record.Error != null) + DamageScalingScenario.Samples.Count(sample => sample.CaptureError != null) +
            StatusScalingScenario.Samples.Count(sample => sample.CaptureError != null) + StatusScalingScenario.Applications.Count(sample => sample.CaptureError != null) +
            UnitUpgradeScalingScenario.Samples.Count(sample => sample.CaptureError != null) + RelicPileStartTurnProbe.Records.Count(record => record.CaptureError != null) +
            RelicUnitResourceProbe.Records.Count(record => record.CaptureError != null) +
            OriginalEquipmentGrowthScenario.EncounterCompleteRecords.Count(record => record.CaptureError != null) +
            RelicNimsOathProbe.Applications.Count(record => record.Error != null || !record.Completed || record.After == null) +
            RelicJudgesScalesProbe.Applications.Count(record => record.Error != null || !record.Completed || record.After == null) +
            OriginalPropagateOnStatusEffectAppliedScenario.IncompleteApplications +
            OriginalNoUpgradeCardCostScenario.Callbacks.Count(record => record.Error != null || record.After == null);
        internal int Mismatches => RelicCardModifierProbe.Records.Count(record => record.Difference != null) + RelicSpawnStatusProbe.Records.Count(record => record.Difference != null) + IncantProbe.Mismatches + SpawnEnchantScenario.Records.Count(record => record.Difference != null) + SpawnPointScenario.Records.Count(record => record.Difference != null) + PoolSummonProbe.Records.Count(record => record.Difference != null) + FreshSpawnerProbe.Records.Count(record => record.Difference != null) + FreshSpawnerProbe.Checks.Count(record => record.Difference != null) + SpawnUpgradeProbe.Records.Count(record => record.Difference != null) + UnitBirthProbe.Clones.Count(record => record.Difference != null) + UnitBirthProbe.Records.Count(record => record.Difference != null) + RallyProbe.Phases.Count(record => record.Difference != null) + RallyProbe.Triggers.Count(record => record.Difference != null) + HarvestProbe.Records.Count(record => record.Difference != null) + EquipmentAbilityScenario.InitialSpawns.Count(record => record.Difference != null) + AbilityLifecycleScenario.Records.Count(record => record.Difference != null) + AbilityCardProbe.Records.Count(record => record.Difference != null) + SentryProbe.Mismatches + CompanionBossProbe.Mismatches + ConditionalTriggerProbe.Records.Count(record => record.Difference != null) + TriggerMutationScenario.Records.Count(record => record.Difference != null) + EquipmentProbe.Records.Count(record => record.Difference != null) + DirectUnitUpgradeScenario.Records.Count(record => record.Difference != null) + stages.Count(stage => stage.Difference != null) + cardCycles.Mismatches + trainCombat.Mismatches + spawning.Mismatches + turns.Mismatches + actions.Mismatches +
            HandRemovalScenario.Records.Count(record => record.Difference != null) + RandomDiscardScenario.Records.Count(record => record.Difference != null) + GenerationScenario.Records.Count(record => record.Difference != null) +
            DamageScalingScenario.Samples.Count(sample => sample.Difference != null) + StatusScalingScenario.Samples.Count(sample => sample.Difference != null) +
            StatusScalingScenario.Applications.Count(sample => sample.Difference != null) + UnitUpgradeScalingScenario.Samples.Count(sample => sample.Difference != null) +
            UnitTurnBeginProbe.Records.Count(record => record.Difference != null) + TeamTurnBeginProbe.Records.Count(record => record.Difference != null) + PreHandDiscardProbe.Records.Count(record => record.Difference != null) + PreCombatProbe.Records.Count(record => record.Difference != null) + TriggeredHealingProbe.Records.Count(record => record.Difference != null) + PostCombatHealingProbe.Records.Count(record => record.Difference != null) + TriggeredDamageProbe.Records.Count(record => record.Difference != null) + RelicPileStartTurnProbe.Records.Count(record => record.Difference != null) + RelicUnitResourceProbe.Records.Count(record => record.Difference != null) + UncontrolledGroupGrowthProbe.Records.Count(record => record.Difference != null) +
            RelicNimsOathProbe.Applications.Count(record => record.Difference != null) +
            RelicJudgesScalesProbe.Applications.Count(record => record.Difference != null) +
            OriginalNoUpgradeCardCostScenario.Callbacks.Count(record => record.Difference != null);
        internal int Unsupported => IncantProbe.Unsupported + SpawnEnchantScenario.Records.Count(record => record.UnsupportedReason != null) + HarvestProbe.Records.Count(record => !record.Predicted.Supported) + EquipmentAbilityScenario.InitialSpawns.Count(record => !record.Predicted.Supported) + AbilityLifecycleScenario.Records.Count(record => record.UnsupportedReason != null) + AbilityCardProbe.Records.Count(record => record.UnsupportedReason != null) + SentryProbe.Unsupported + CompanionBossProbe.Unsupported + ConditionalTriggerProbe.Records.Count(record => !record.Predicted.Supported) + TriggerMutationScenario.Records.Count(record => record.UnsupportedReason != null) + EquipmentProbe.Records.Count(record => record.UnsupportedReason != null) + DirectUnitUpgradeScenario.Records.Count(record => record.UnsupportedReason != null) + stages.Count(stage => !stage.Predicted.Supported) + cardCycles.Unsupported + trainCombat.Unsupported + spawning.Unsupported + turns.Unsupported + actions.Unsupported +
            HandRemovalScenario.Records.Count(record => !record.Predicted.Supported) + RandomDiscardScenario.Records.Count(record => !record.Predicted.Supported) + GenerationScenario.Records.Count(record => !record.Predicted.Supported) +
            UnitTurnBeginProbe.Records.Count(record => !record.Predicted.Supported) + TeamTurnBeginProbe.Records.Count(record => !record.Predicted.Supported) + PreHandDiscardProbe.Records.Count(record => !record.Predicted.Supported) + PreCombatProbe.Records.Count(record => !record.Predicted.Supported) + PostCombatHealingProbe.Records.Count(record => !record.Predicted.Supported) + RelicPileStartTurnProbe.Records.Count(record => record.UnsupportedReason != null) + RelicUnitResourceProbe.Records.Count(record => record.UnsupportedReason != null) +
            RelicNimsOathProbe.Applications.Count(record => !record.ModelSupported) +
            RelicJudgesScalesProbe.Applications.Count(record => !record.ModelSupported) +
            OriginalNoUpgradeCardCostScenario.Callbacks.Count(record => !record.ModelSupported);
        internal int Pending => RelicCardModifierProbe.Records.Count(record => record.Actual == null) + RelicSpawnStatusProbe.Records.Count(record => !record.Completed || record.After == null) + SpawnEnchantScenario.Records.Count(record => !record.Completed || record.After == null) + (HeroCopyScenario.Started && !HeroCopyScenario.Completed ? 1 : 0) + HeroCopyScenario.Records.Count(record => record.After == null) + (UnitCopyScenario.Started && !UnitCopyScenario.Completed ? 1 : 0) + UnitCopyScenario.Records.Count(record => record.After == null) + (UnitCloneScenario.Started && !UnitCloneScenario.Completed ? 1 : 0) + UnitCloneScenario.Records.Count(record => record.AfterApi == null || record.After == null) + (BumpScenario.Started && !BumpScenario.Completed ? 1 : 0) + BumpScenario.Records.Count(record => record.After == null || record.TargetsAfter == null) + (HordeMergeScenario.Started && !HordeMergeScenario.Completed ? 1 : 0) + HordeMergeScenario.Records.Count(record => record.After == null || record.AfterApi == null) + (RevivalScenario.Started && !RevivalScenario.Completed ? 1 : 0) + RevivalScenario.Operations.Count(record => record.After == null) + RevivalProbe.Records.Count(record => !record.Completed || record.After == null || record.AfterActor == null) + TriggeredSummonProbe.Damages.Count(record => !record.Completed || record.After == null || record.TargetAfter == null) + TriggeredSummonProbe.Records.Count(record => !record.Completed || record.After == null || record.ActorAfter == null || record.RuleAfter == null) + BattleSpawnPointProbe.Compactions.Count(record => record.After == null) + SpawnPointScenario.Records.Count(record => !record.Completed || record.After == null) + (SpawnPointScenario.Started && !SpawnPointScenario.Completed ? 1 : 0) + PoolSummonProbe.Records.Count(record => !record.Completed) + FreshSpawnerProbe.Records.Count(record => !record.Completed) + FreshSpawnerProbe.Checks.Count(record => !record.Completed) + SpawnUpgradeProbe.Records.Count(record => !record.Completed) + (MultiSummonScenario.ZeroStarted && !MultiSummonScenario.ZeroCompleted ? 1 : 0) + (RallyScenario.Started && !RallyScenario.Completed ? 1 : 0) + RallyScenario.Operations.Count(record => record.After == null) + RallyProbe.Phases.Count(record => !record.Completed || record.After == null) + RallyProbe.Triggers.Count(record => !record.Completed || record.After == null || record.AfterActor == null) + (DyingHordeUpgradeScenario.Started && !DyingHordeUpgradeScenario.Completed ? 1 : 0) + DyingHordeUpgradeScenario.Operations.Count(record => record.After == null || record.AfterActor == null) + (HordeUpgradeScenario.Started && !HordeUpgradeScenario.Completed ? 1 : 0) + HordeUpgradeScenario.Operations.Count(record => record.After == null || record.AfterActor == null) + (HordeDeathScenario.Started && !HordeDeathScenario.Completed ? 1 : 0) + (HordeRemovalScenario.Started && !HordeRemovalScenario.Completed ? 1 : 0) + (HarvestScenario.Started && !HarvestScenario.Completed ? 1 : 0) + HarvestProbe.Records.Count(record => !record.Completed || record.After == null || record.AfterActor == null) + EquipmentAbilityScenario.InitialSpawns.Count(record => record.Actual == null) + (EquipmentScenario.Started && !EquipmentScenario.Completed ? 1 : 0) + StatusCallbackProbe.OtherFired.Count(record => !record.Completed || record.Actual == null || record.ActualUnit == null) + AbilityEffectProbe.Records.Count(record => !record.Completed || record.After == null) + AbilityLifecycleScenario.Records.Count(record => record.After == null) + (AbilityLifecycleScenario.Started && !AbilityLifecycleScenario.Completed ? 1 : 0) + AbilityCardProbe.Records.Count(record => record.After == null) + AbilityCooldownProbe.Records.Count(record => !record.Completed || record.After == null) + IncantProbe.Pending + SentryProbe.Pending + CompanionBossProbe.Pending + ConditionalTriggerProbe.Records.Count(record => !record.Completed || record.After == null || record.AfterActor == null) + (ConditionalTriggerScenario.Started && !ConditionalTriggerScenario.Completed ? 1 : 0) + TriggerMutationScenario.Records.Count(record => record.After == null) + (TriggerMutationScenario.Started && !TriggerMutationScenario.Completed ? 1 : 0) + EquipmentProbe.Records.Count(record => !record.Completed || record.After == null) + DirectUnitUpgradeScenario.Records.Count(record => record.After == null) + (DirectUnitUpgradeScenario.Started && !DirectUnitUpgradeScenario.Completed ? 1 : 0) + RoomCapacityProbe.Records.Count(record => !record.Completed || record.After == null) + BonusDrawProbe.Records.Count(record => !record.Completed || !record.Sampled || record.After == null) + StatusCallbackProbe.Fired.Count(record => !record.Completed || record.Actual == null || record.ActualUnit == null) + PurifyQueueProbe.Records.Count(record => !record.Completed) + PreviewRngIsolation.Records.Count(record => !record.Completed) + TriggeredStatusProbe.Records.Count(record => !record.Completed || record.Actual == null || record.ActualUnits == null) + AttackTriggerProbe.Records.Count(record => !record.Completed || !record.GoldAfter.HasValue || record.AfterTriggered == null) + DyingUpgradeProbe.Records.Count(record => !record.Completed || record.Actual == null || record.ActualUnits == null) + HitKillProbe.Records.Count(record => !record.Completed || !record.GoldAfter.HasValue || record.AfterTriggered == null) + stages.Count(stage => stage.Actual == null) + cardCycles.Records.Count(record => record.Actual == null) +
            trainCombat.Records.Count(record => record.Actual == null) + spawning.Records.Count(record => record.Actual == null) +
            turns.Records.Count(record => record.Actual == null) + actions.Records.Count(record => record.Actual == null) +
            HandRemovalScenario.Records.Count(record => record.Actual == null) + RandomDiscardScenario.Records.Count(record => record.Actual == null) + GenerationScenario.Records.Count(record => record.Actual == null) +
            DamageScalingScenario.Samples.Count(sample => sample.After == null) + StatusScalingScenario.Samples.Count(sample => sample.After == null) +
            StatusScalingScenario.Applications.Count(sample => sample.After == null) + UnitUpgradeScalingScenario.Samples.Count(sample => sample.After == null) +
            UnitTurnBeginProbe.Records.Count(record => record.Actual == null) + TeamTurnBeginProbe.Records.Count(record => record.Actual == null) + PreHandDiscardProbe.Records.Count(record => record.Actual == null) + PreCombatProbe.Records.Count(record => record.Actual == null) + TriggeredHealingProbe.Records.Count(record => !record.Completed || !record.Sampled || record.Requests.Any(request => !request.AfterHealth.HasValue)) + PostCombatHealingProbe.Records.Count(record => record.Actual == null) + TriggeredDamageProbe.Records.Count(record => !record.Completed || !record.Sampled || record.Requests.Any(request => !request.AfterHealth.HasValue)) + RelicPileStartTurnProbe.Records.Count(record => !record.Completed || record.Actual == null) + RelicUnitResourceProbe.Records.Count(record => !record.Completed || record.Actual == null) + UncontrolledGroupGrowthProbe.Records.Count(record => !record.Completed) +
            OriginalEquipmentGrowthScenario.EncounterCompleteRecords.Count(record => !record.Completed || record.Before == null || record.After == null) +
            RelicNimsOathProbe.Applications.Count(record => !record.Completed || record.After == null) +
            RelicJudgesScalesProbe.Applications.Count(record => !record.Completed || record.After == null) +
            OriginalPropagateOnStatusEffectAppliedScenario.IncompleteApplications +
            OriginalNoUpgradeCardCostScenario.Callbacks.Count(record => record.After == null) +
            (OriginalStartCombatHoardScenario.Acquired &&
                (RelicResourceProbe.Operations.Count(record => record.Kind == "StartCombatHoard") != 1 ||
                    RelicResourceProbe.Operations.Any(record => record.Kind == "StartCombatHoard" && record.After == null)) ? 1 : 0);

        internal FullBattleTrace(ManualLogSource log)
        {
            if (AdvisorRuntime.PlayerSession) BattleSpawnPointProbe.ResetAdvisorSession();
            this.log = log;
            projection = new UnitPlayModelProbe(log);
            cardCycles = new CardCycleProbe(log, projection);
            trainCombat = new TrainCombatProbe(log, this);
            spawning = new EnemySpawningProbe(log, this, trainCombat);
            turns = new BattleTurnProbe(this, spawning, cardCycles, projection, log);
            actions = new BattleActionProbe(this, projection, log);
            Active = this;
        }

        internal static void InstallAdvisorPatches(Harmony harmony)
        {
            harmony.CreateClassProcessor(typeof(CardIdentityPatch)).Patch();
            harmony.CreateClassProcessor(typeof(MonsterIdentityPatch)).Patch();
            harmony.CreateClassProcessor(typeof(HeroIdentityPatch)).Patch();
        }

        internal void Checkpoint(string label)
        {
            if (label.StartsWith("decision-", StringComparison.Ordinal)) turns.Complete(RoomOutcome.Exchanged);
            AllGameManagers? managers = AllGameManagers.Instance;
            if (managers == null) return;
            SaveManager save = managers.GetSaveManager();
            CombatManager? combat = managers.GetCombatManager();
            CardManager? cards = managers.GetCardManager();
            if (save == null || combat == null || cards == null || save.PreviewMode) return;
#if DEVELOPMENT_PROBE
            if (nativeRunInput == null)
            {
                var data = (SaveData)AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
                nativeRunInput = new
                {
                    Schema = 1, Seed = data.GetSeed(), Distance = save.GetCurrentDistance(),
                    AscensionLevel = save.GetAscensionLevel(), StartingLevel = save.GetStartingConditions().AscensionLevel,
                    Scenario = save.GetScenarioData(save.GetCurrentDistance()).name,
                    WasDevCheatUsed = SaveManager.WasDevCheatUsed, BattlesWon = save.GetNumBattlesWon(),
                    Gold = save.GetGold(), PyreHealth = save.GetTowerHP(),
                    CompletedBattles = save.GetBattleRecords().Select(battle => new
                    {
                        battle.battleIndex, battle.loopIndex, Won = battle.WonBattle(),
                        battle.bossHPRemaining, battle.turnsLasted
                    }).ToArray()
                };
            }
#endif
            if (!calibrated)
            {
                CardCatalogProbe.Capture(this, managers);
                RngCalibration.Capture();
                GoldRewardCalibration.Capture(save);
                CampaignRelicCalibration.Capture(managers);
                CampaignDraftCalibration.Capture(managers);
                CampaignPostBattleCalibration.Capture(managers);
                CampaignBattleResourceCalibration.Capture(managers);
                CampaignPurgeGoldCalibration.Capture(managers);
                CampaignDeckCopyCalibration.Capture(managers);
                CampaignDraftEnhancerCountCalibration.Capture(managers);
                RuleCatalogProbe.Capture(managers, Environment.GetEnvironmentVariable("MT2_PROBE_FULL_RULE_CATALOG") == "1");
                RelicCatalogProbe.Capture(managers);
                CardUpgradeMaskCalibration.Capture(this);
                CardModifierOverflowCalibration.Capture(this);
                CardTraitCompositionCalibration.Capture(this);
                CardStatusCompositionCalibration.Capture(this);
                CardOwnedMaskCalibration.Capture(this);
                CardUpgradeLifecycleCalibration.Capture(this);
                HoldoverVariantCalibration.Capture(this);
                PermanentTraitCalibration.Capture(this);
                TitaniteCalibration.Capture(this);
                TitaniteCombinationCalibration.Capture(this);
                StatusTriggerOrderCalibration.Capture(this);
                TitaniteWeaknessCalibration.Capture(this);
                RelicCardUpgradeCalibration.Capture(this);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_STATISTIC_QUERIES") == "1") StatisticQueryCalibration.Capture(this);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_STATISTIC_OVERFLOW") == "1") StatisticOverflowCalibration.Capture(this);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_HORDE_STATS") == "1") HordeStatCalibration.Capture(this);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_ENCHANTMENT_LIFECYCLE") == "1") EnchantmentCalibration.Capture(this);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "unit-upgrade-scaling") UnitUpgradeScalingScenario.Calibrate(this);
                calibrated = true;
            }
            checkpoints.Add(new { Label = label, State = projection.Capture(managers, save, combat, cards) });
        }

        internal void BeginEndTurn() => turns.Begin();
        internal void CompleteEndTurn(RoomOutcome outcome) => turns.Complete(outcome);
        internal BattleTurnState CaptureDecision(bool includeOuterBossForBump = false,
            bool ignoreOuterBossSpawnInteraction = false) => turns.Capture(includeOuterBossForBump, ignoreOuterBossSpawnInteraction);
        internal BattlePlayRules CapturePlayRules(EnemySpawnState spawn) => actions.CaptureRules(spawn);
        internal int CardId(CardState card) => projection.CaptureCards(new List<CardState> { card })[0].InstanceId;
        internal bool TryGetCardId(CardState card, out int id) => projection.TryGetCardId(card, out id);
        internal bool KnowsNativeObject(object value) => value is CardState card ? projection.Knows(card) :
            value is CharacterState unit && identities.ContainsKey(unit);
        internal int EquippedUnitId(CardState card)
        {
            CharacterState? unit = card.EquippedCharacter;
            return unit == null || normalizeDestroyedAttacker && (unit.IsDestroyed || !unit.IsAlive) ? 0 : UnitId(unit);
        }
        private readonly Dictionary<string, string> roomAttachmentTriggerKeys = new Dictionary<string, string>();
        internal string UpgradeKey(string nativeKey)
        {
            RoomManager? rooms = AllGameManagers.Instance?.GetRoomManager();
            if (rooms != null)
                for (int index = 0; index < rooms.GetNumRooms(); index++)
                    foreach (TrainRoomAttachmentState attachment in rooms.GetRoom(index).Attachments)
                        if (attachment.CardState != null && RoomAttachmentTriggerIdentity.Known(attachment.CardState.GetCardDataID()) &&
                            !roomAttachmentTriggerKeys.ContainsKey(attachment.Guid))
                            roomAttachmentTriggerKeys.Add(attachment.Guid, RoomAttachmentTriggerIdentity.Key(
                                attachment.CardState.GetCardDataID(), index, rooms.GetRoom(index).NumRoomAttachmentsAddedFromCard));
            if (roomAttachmentTriggerKeys.TryGetValue(nativeKey, out string key)) return key;
            // CardState.GetID() is the definition ID, shared by its copies.
            return nativeKey;
        }
        internal CardUpgradeModifier CaptureAppliedUpgrade(CardUpgradeState upgrade, bool roomAttachmentUnitUpgrade = false)
        {
            CardUpgradeModifier modifier = CardModifierProbe.Upgrade(upgrade, rejectFilters: false,
                filtersAlreadyApplied: true, roomAttachmentUnitUpgrade: roomAttachmentUnitUpgrade);
            foreach (CardState card in projection.KnownCards.Where(item => item.GetCardType() == CardType.Equipment))
            {
                int index = card.GetCardStateModifiers().GetCardUpgrades().FindIndex(item => ReferenceEquals(item, upgrade));
                if (index >= 0) return modifier.WithEquipmentSource(CardId(card), index);
            }
            return modifier;
        }
        internal int? PendingActionIndex => actions.Records.LastOrDefault(record => record.Actual == null)?.Index;
        internal int? PendingTurnIndex => turns.Records.LastOrDefault(record => record.Actual == null)?.Index;
        internal void BeginCardPlay(PlayCardAction action, bool scenarioAction = false,
            bool includeOuterBossForBump = false, bool ignoreOuterBossSpawnInteraction = false) =>
            actions.Begin(action, scenarioAction, includeOuterBossForBump, ignoreOuterBossSpawnInteraction);
        internal void CompleteCardPlay()
        {
            actions.Complete();
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled) PlayerAdvisorProbe.CheckInputRecorded();
#endif
        }

        internal RoomCombatState Capture(RoomState room, bool captureContext = true, bool includeOuterBossForBump = false,
            bool observe = true)
        {
            AllGameManagers managers = AllGameManagers.Instance ?? throw new InvalidOperationException("No game managers.");
            var interactions = new List<string>();
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager() ?? throw new InvalidOperationException("No combat manager.");
            RoomManager roomManager = managers.GetRoomManager() ?? throw new InvalidOperationException("No room manager.");
            HeroManager heroes = managers.GetHeroManager() ?? throw new InvalidOperationException("No enemy manager.");
            if (room.IsRoomEnabled() && !room.GetCombatAllowed()) interactions.Add("Disabled room combat");
            string? relicError = RelicModel.Validate(RelicProbe.Capture(managers));
            if (relicError != null) interactions.Add(relicError);
            if (save.GetMutators().Count != 0 && !RelicCardModifierScenario.SupportedMutatorContext)
                interactions.Add("Mutator effects");
            if (room.Attachments.Count > 0 && !RoomCapacityProbe.SupportsAttachments(room)) interactions.Add("Room attachments");
            CharacterState? outerBoss = heroes.GetOuterTrainBossCharacter();
            bool outerBossHere = outerBoss != null && outerBoss.IsAlive && outerBoss.GetCurrentRoomIndex() == room.GetRoomIndex();
            if (outerBossHere && !includeOuterBossForBump && !OuterBossPhaseProbe.ModelsLiveBoss)
                interactions.Add("Outer train boss");
            if (heroes.HasNextBoss() && !TrueFinalBossTurnProbe.HasQueuedBoss) interactions.Add("Queued final bosses");
            var characters = new List<CharacterState>();
            room.AddCharactersToList(characters, Team.Type.Heroes);
            room.AddCharactersToList(characters, Team.Type.Monsters);
            if ((includeOuterBossForBump || OuterBossPhaseProbe.ModelsLiveBoss) && outerBossHere && !characters.Contains(outerBoss!)) characters.Add(outerBoss!);
            var units = new List<CombatUnit>();
            foreach (CharacterState character in characters)
            {
                if (!character.IsAlive || character.IsDestroyed) continue;
                units.Add(CaptureUnit(character, interactions));
            }
            var companions = new List<CharacterState>();
            for (int index = 0; index < roomManager.GetNumRooms(); index++)
            {
                // AddCharactersToList sorts its entire destination by the local
                // spawn-point index. Collect each floor separately before joining.
                var floorCompanions = new List<CharacterState>();
                roomManager.GetRoom(index).AddCharactersToList(floorCompanions, Team.Type.Heroes);
                if (outerBoss != null && outerBoss.GetCurrentRoomIndex() == index && !floorCompanions.Contains(outerBoss))
                    floorCompanions.Add(outerBoss);
                companions.AddRange(floorCompanions);
            }
            int[] activeCompanionBossIds = companions.Where(character => !character.IsDestroyed &&
                (character.IsAlive || character.HasStatusEffect("undying")) &&
                (character.IsMiniboss() || character.IsOuterTrainBoss()) && character.IsCompanionBoss())
                .Select(UnitId).Distinct().ToArray();
            var captured = new RoomCombatState(room.GetRoomIndex(), combat.IsPlacementPhase, units,
                interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), captureContext ? CaptureContext() : null,
                preview: save.PreviewMode, activeCompanionBossIds: activeCompanionBossIds);
            if (observe) ObserveRoom(captured, characters);
            return captured;
        }

        internal void ObserveRoom(RoomCombatState captured, bool includeOuterBossForBump)
        {
#if DEVELOPMENT_PROBE
            AllGameManagers managers = AllGameManagers.Instance!;
            RoomState room = managers.GetRoomManager()!.GetRoom(captured.RoomIndex);
            var characters = new List<CharacterState>();
            room.AddCharactersToList(characters, Team.Type.Heroes);
            room.AddCharactersToList(characters, Team.Type.Monsters);
            CharacterState? outerBoss = managers.GetHeroManager()!.GetOuterTrainBossCharacter();
            if ((includeOuterBossForBump || OuterBossPhaseProbe.ModelsLiveBoss) && outerBoss != null && outerBoss.IsAlive &&
                outerBoss.GetCurrentRoomIndex() == captured.RoomIndex && !characters.Contains(outerBoss)) characters.Add(outerBoss);
            ObserveRoom(captured, characters);
#endif
        }

        private void ObserveRoom(RoomCombatState captured, IReadOnlyList<CharacterState> characters)
        {
#if DEVELOPMENT_PROBE
            UnitSelfAttackProbe.Observe(captured, characters, this);
            UnitRoomStatusProbe.Observe(captured, characters, this);
            UnitRoomTriggerCountProbe.Observe(captured, characters, this);
#endif
        }

        internal CombatUnit CaptureUnit(CharacterState character, List<string>? interactions = null)
        {
            interactions ??= new List<string>();
            HeroManager heroes = AllGameManagers.Instance!.GetHeroManager()!;
            int id = UnitId(character);
            CharacterState? lastAttacker = character.GetLastAttackerCharacter();
            CombatTrigger[] triggers = CaptureTriggers(character, interactions);
            if (character.GetRoomStateModifiers().Any(modifier => modifier is not RoomStateSelfDamagePerMagicPowerModifier &&
                !(ConduitProbe.Enabled && modifier is RoomStateConduitModifier) && !UnitSelfAttackModel.Known(modifier.GetType().Name) &&
                !UnitRoomResourceModel.Known(modifier.GetType().Name) && !UnitRoomStatusModel.Known(modifier.GetType().Name) && modifier.GetType().Name != UnitRoomTriggerCountModel.Type &&
                modifier is not RoomStateSpawnUnitOnUnitSpawnModifier && modifier is not RoomStateSentryAttackModifier && modifier is not RoomStateTroopUnitUpgradeModifier))
                interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " room modifiers");
            CardState? card = character.GetSpawnerCard();
            bool unsupportedSpawnerCardTriggers = card != null && card.GetTriggers().Any(trigger =>
            {
                CardTriggerEffectData? triggerData = trigger.GetCardTriggerEffectData();
                return card.GetCardType() != CardType.Monster || triggerData == null ||
                    triggerData.GetTrigger().ToString() != "OnCast" || triggerData.GetTriggerEffects().Count > 0 ||
                    triggerData.GetCardEffects().Any(effect => !BattleActionProbe.SupportsCardActionEffect(effect));
            });
            if (card != null && (card.GetTraitStates().Any(trait =>
                    trait.GetType().Name != "CardTraitIgnoreArmor" &&
                    trait.GetType().Name != "CardTraitDrawInDeploymentPhase" &&
                    !DamageScalingProbe.Known(trait.GetType().Name) &&
                    !OriginalExtractForEmberAndEchosScenario.IsSupportedCarrierTrait(card, trait)) || unsupportedSpawnerCardTriggers))
                interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " card traits/triggers");
            var nativeStatuses = new List<CharacterState.StatusEffectStack>();
            character.GetStatusEffects(ref nativeStatuses, includeZeroStacks: true);
            var statuses = new List<CombatStatus>();
            foreach (CharacterState.StatusEffectStack status in nativeStatuses)
            {
                StatusEffectState rule = status.State;
                StatusEffectData statusData = rule.GetSourceStatusEffectData();
                statuses.Add(new CombatStatus(rule.GetStatusId(), status.Count, rule.GetParamInt(),
                    rule.GetRemoveWhenTriggered(), rule.GetRemoveStackAtEndOfTurn(), rule.GetRemoveAtEndOfTurn(),
                    rule.GetRemoveAtEndOfTurnAfterPostCombat(), rule.PreventRemovalDuringRelentlessPhase,
                    rule.GetSkipTriggerDuringDeployment(), rule.GetRemoveDuringDeployment(),
                    BattleActionProbe.TriggeredVfx(rule.GetSourceStatusEffectData(), -1f),
                    BattleActionProbe.TriggeredVfx(statusData, 1f), rule.IsStackable(), rule.IsHidden(), rule.GetDisplayCategory().ToString(),
                    statusData.IsPropagatable(), statusData.GetExcludeHeroPropagation(), statusData.GetExcludeMonsterPropagation(),
                    rule.GetRemoveWhenTriggeredAfterCardPlayed(), rule.GetRemoveAtEndOfTurnIfTriggered(),
                    rule.GetMarkForRemovalAfterCardPlayed()));
            }
            CombatTeam team = character.GetTeamType() == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player;
            CharacterState? pairedBoss = heroes.FindPairedCompanionBoss(character);
            bool endsBattle = team == CombatTeam.Enemy &&
                (character.IsMiniboss() || character.IsOuterTrainBoss()) &&
                pairedBoss == null;
            StatusRegistryCalibration.Capture(character, id, statuses);
            UnitModifiers modifiers = UnitModifierProbe.Capture(character);
            bool attackOverride = character.HasStatusEffect("fixedattack") || character.HasStatusEffect("armorattack") || character.HasStatusEffect("equalizer");
            int baseAttack = attackOverride ? Math.Max(0, unchecked(modifiers.AttackDamage + modifiers.DamageBuff)) : character.GetAttackDamageWithoutStatusEffectBuffs();
            if (!attackOverride)
                baseAttack = unchecked(baseAttack - character.GetRoomStateModifiers().Where(modifier => UnitSelfAttackModel.Known(modifier.GetType().Name))
                    .Cast<RoomStateModifierBase>().Sum(modifier => modifier.GetDynamicInt(character)));
            if (!attackOverride && modifiers.MagebladeDamagePerMagicPower is int magebladeCoefficient && character.GetCurrentRoom() is RoomState currentRoom)
            {
                AllGameManagers managers = AllGameManagers.Instance!;
                int roomMagicPower = currentRoom.GetRoomStateModifiedMagicPower(character.GetTeamType(), managers.GetCoreManagers()) +
                    managers.GetRelicManager().GetMagicPowerModification();
                baseAttack = unchecked(baseAttack - unchecked(magebladeCoefficient * roomMagicPower));
            }
            // Physical player snapshots must retain the same movement rules as
            // their deployment and future-wave definitions.
            return new CombatUnit(id, character.GetSourceCharacterData()?.GetAssetKey() ?? "",
                team, baseAttack, character.GetHP(), character.GetMaxHP(),
                character.GetCanAttack(), character.IsPyreHeart(), endsBattle, statuses, triggers,
                card == null ? 0 : projection.CaptureCards(new List<CardState> { card })[0].InstanceId, character.GetSize(),
                ((List<string>)AccessTools.Field(typeof(CharacterState), "statusEffectImmunities").GetValue(character)).ToArray(),
                character.GetSubtypes().Select(subtype => subtype.Key).ToArray(), modifiers, character.IsAnyBoss(),
                lastAttacker == null || normalizeDestroyedAttacker && (lastAttacker.IsDestroyed || !lastAttacker.IsAlive) ? 0 : UnitId(lastAttacker), statuses,
                character.GetEquipment().Select(CardId).ToArray(), TriggerStates(character).Count, AbilityCooldownProbe.Capture(character), AbilityCooldownProbe.CaptureDictionary(character), AbilityLifecycleProbe.Rules(character.GetSourceCharacterData()),
                new HordeBaseStats(character.GetSourceCharacterData().GetAttackDamage(), character.GetSourceCharacterData().GetHealth()), character.IsSpawning, character.SacrificeCard == null ? 0 : CardId(character.SacrificeCard), DeathSignalProbe.Capture(character),
                BattleSpawnPointProbe.Enabled || SentryAttackProbe.Definition(character.GetSourceCharacterData()) != null || TroopAuraProbe.Definition(character.GetSourceCharacterData()) != null || BumpScenario.Enabled || OriginalSupportSpellsScenario.CapturesBump || AbilityTeleportScenario.Enabled || OriginalMoonCardUpgradesScenario.Enabled || OriginalHealingCleanseScenario.Enabled || OriginalMovementSpellsScenario.Enabled || OriginalRearrangeSpellsScenario.Enabled || (OriginalReactiveUnitsScenario.Enabled && team == CombatTeam.Enemy) || (OriginalUnitRelicBatchScenario.Enabled || OriginalTriggerRelicBatchScenario.Enabled || OriginalLifecycleRelicBatchScenario.Enabled)
                    || OuterBossPhaseProbe.CaptureAnchors || RoomMoonCallbackCalibration.Movement || EclipseChamberScenario.Movement
                    ? new UnitBumpRules(character.GetLoopsBetweenTrainFloors(), character.IsCompanionBoss(), character.IsOuterTrainBoss(),
                        character.IsOuterTrainBoss() && character.GetBossState().GetCurrentAttackPhase() == BossState.AttackPhase.Relentless,
                        pairedBoss == null ? 0 : UnitId(pairedBoss),
                        OuterBossPhaseProbe.AnchorSlots(character, BossState.AttackPhase.Casting),
                        OuterBossPhaseProbe.AnchorSlots(character, BossState.AttackPhase.Relentless))
                    : null, lastFeederId: triggers.Any(trigger => trigger.Kind == "OnEaten") || OriginalOpeningMorselsScenario.Enabled || OriginalMorselEatenScenario.Enabled || OriginalOnKillGenerationScenario.Enabled ?
                        (character.GetLastFeederCharacter() == null ? 0 : UnitId(character.GetLastFeederCharacter())) : (int?)null,
                    moonPhaseMask: EclipseChamberScenario.Enabled || OriginalMoonCardUpgradesScenario.Enabled || MoonPhaseCaptureProbe.CaptureMasks ? (int)character.GetMoonPhaseMask() : (int?)null);
        }

        internal CombatContext CaptureContext()
        {
            CardGenerationProbe.BeginContextCapture();
            try { return CaptureContextCore(); }
            finally { CardGenerationProbe.EndContextCapture(); }
        }

        private CombatContext CaptureContextCore()
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            CardManager cards = managers.GetCardManager()!;
            CombatProjection state = projection.Capture(managers, managers.GetSaveManager(),
                managers.GetCombatManager()!, cards);
            uint[] draw = RngCalibration.Words(RandomManager.GetState(RngId.CardDraw));
            uint[] battle = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
            // Native state cannot change during this synchronous capture. Reuse
            // each card's projection between live piles and registry closure only;
            // never retain it across decisions, upgrades or cost changes.
            var capturedCards = new Dictionary<CardState, CardInstanceState>();
            CardInstanceState[] instances = CardModifierProbe.Capture(cards, CardId, capturedCards);
            if (RelicProbe.HasReplayHistory(managers)) TranscendImpScenario.CaptureHistory(managers);
            CharacterState lastSpawned = managers.GetMonsterManager()!.GetLastSpawnedCharacterThisTurn();
            int lastSpawnedId = lastSpawned == null || normalizeDestroyedAttacker && (lastSpawned.IsDestroyed || !lastSpawned.IsAlive)
                ? 0 : UnitId(lastSpawned);
            int activatorId = AccessTools.Field(typeof(CombatManager), "lastAbilityActivatorCharacter").GetValue(managers.GetCombatManager()) is CharacterState activator
                ? UnitId(activator) : 0;
            BattleStatistics statistics;
            AbilityCardCacheEntry[] abilityCache;
            CardInstanceState[] registry;
            CardPileState[] otherPiles;
            BonusDrawState bonusDraw;
            BattleSpawnPoints? spawnPoints;
            EnchantmentWorld? enchantments;
            int observedCards, observedUnits;
            // Retained preview actors can reveal detached source cards; copied
            // slots and card weak references can reveal additional actors. Close
            // these references before freezing either registry or identity counter.
            do
            {
                observedCards = projection.KnownCards.Count(); observedUnits = identities.Count;
                int decisionStart = BattleSpawnPointProbe.Decisions.Count;
                otherPiles = CaptureOtherPiles();
                RoomEchoProbe.Discover();
                enchantments = CapturesNativeEnchantments || EnchantmentBattleScenario.Prepared || OriginalSupportUnitsScenario.CapturesEnchantments || RoomDeathEffectProbe.RequiresBirthWorld(this) || RoomPreCombatProbe.RequiresWorld(this) || TroopAuraProbe.RequiresWorld(this)
                    ? EnchantmentBattleScenario.CaptureWorld(this) : null;
                abilityCache = AbilityCardProbe.Capture(this);
                bonusDraw = BonusDrawProbe.Capture(cards, this);
                spawnPoints = BattleSpawnPointProbe.Enabled ? BattleSpawnPointProbe.Capture(this) : null;
                statistics = BattleStatisticsProbe.Capture(managers.GetCardStatistics(), CardId);
                registry = CardModifierProbe.Capture(projection.KnownCards, CardId, capturedCards);
                if (observedCards != projection.KnownCards.Count() || observedUnits != identities.Count)
                    BattleSpawnPointProbe.Decisions.RemoveRange(decisionStart, BattleSpawnPointProbe.Decisions.Count - decisionStart);
            } while (observedCards != projection.KnownCards.Count() || observedUnits != identities.Count);
            var context = new CombatContext(new CardCycleState(state.Hand, state.Draw, state.Discard,
                new UnityRng(draw[0], draw[1], draw[2], draw[3]), state.DrawModifier, Array.Empty<string>(), bonusDraw,
                TypedDrawProbe.CaptureQueue(cards), TypedDrawProbe.NextUpgradeReferenceId,
                OriginalDiscardDrawScenario.Enabled ? cards.GetDrawingDeckCardsDisabledThisTurn() : (bool?)null),
                new UnityRng(battle[0], battle[1], battle[2], battle[3]), state.Gold, projection.NextCardId,
                cards.GetMaxHandSize(), new[] { "armor", "valor", "pyregel", "relentless", "cooldown", "unit_ability", "unit_ability_available", "horde", "duality" }.Concat(OriginalOpeningMorselsScenario.Enabled || TriggeredSummonProbe.Enabled || UnitCloneScenario.Enabled || UnitCopyScenario.Enabled || HeroCopyScenario.Enabled || HasCopyUnitsCard(managers) ? new[] { "cardless" } : Array.Empty<string>()).Concat(new[] { "undying", "endless" })
                    .Concat(registry.Any(card => UnitGraftProbe.Known(card.DataId) || card.MaskDescriptor?.GraftedEquipment != null) ? new[] { "unit_grafted_equipment" } : Array.Empty<string>())
                    .Select(id => BattleActionProbe.Status(id, 1)).ToArray(),
                statistics, instances, registry, managers.GetCombatManager()!.AllScenarioBossesDead,
                ((IEnumerable<CardUpgradeState>)AccessTools.Field(typeof(CardManager), "nextAddedTempCardUpgrades").GetValue(cards))
                    .Select(CardModifierProbe.Upgrade).ToArray(), otherPiles, CaptureQueryFrame(managers),
                (bool)AccessTools.Field(typeof(CombatManager), "isKillCamActivated").GetValue(managers.GetCombatManager()),
                Enumerable.Range(0, managers.GetRoomManager()!.GetNumRooms()).SelectMany(index => new[] { Team.Type.Heroes, Team.Type.Monsters }
                    .Select(team => new RoomMagicPower(index, team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player,
                        managers.GetRoomManager()!.GetRoom(index).GetRoomStateModifiedMagicPower(team, managers.GetCoreManagers()) +
                        managers.GetRelicManager().GetMagicPowerModification(), ConduitProbe.CaptureRoom(index, team, this)))).ToArray(), PreviewRngIsolation.Enabled ? true : (bool?)null,
                new BattleEnergyState(managers.GetSaveManager().GetBalanceData().GetMaxEnergy(),
                    (int)AccessTools.Field(typeof(CombatManager), "modifiedEnergyNextTurn").GetValue(managers.GetCombatManager()),
                    (int)AccessTools.Field(typeof(CombatManager), "modifiedEnergyEveryTurn").GetValue(managers.GetCombatManager()),
                    managers.GetCombatManager()!.GetCombatPhase().ToString(), managers.GetPlayerManager().GetTowerHP() > 0),
                RoomCapacityProbe.Capture(managers.GetRoomManager()!), abilityCache,
                activatorId, AbilityLifecycleProbe.Disabled(managers.GetSaveManager()),
                lastSpawnedId, NextUnitId, spawnPoints,
                TriggeredSummonProbe.Catalog(), enchantments,
                managers.GetSaveManager().GetBalanceData().GetDisallowedPurifyCharacterTriggers().Select(kind => kind.ToString()).ToArray(),
                RelicProbe.Capture(managers), RelicProbe.TriggerCounts(managers),
                resources: RelicResourceProbe.CaptureResources(managers),
                mixState: new MixRuntimeState(managers.GetCombatManager()!.LastMixCardSelected?.GetID(),
                    managers.GetCombatManager()!.LastMixUpgradeSelected?.GetID()),
                conduitRetained: ConduitProbe.CaptureRetained(this), lifeLinks: CaptureLifeLinks(managers));
            CardBranchMaskProbe.Observe(this, context);
            return context;
        }

        private LifeLinkContext? CaptureLifeLinks(AllGameManagers managers)
        {
            CharacterState[] actors = managers.GetHeroManager().GetCharactersReadOnly()
                .Where(actor => actor.IsTrueFinalBoss() && !actor.IsDestroyed).ToArray();
            if (actors.Length == 0) return null;
            // Full-battle projections contain live health. UI preview state is
            // deliberately separate: its copies drop links and change on hover.
            return new LifeLinkContext(actors.Select(actor => new LifeLinkActor(UnitId(actor),
                actor.GetLinkedCharacterState() == null ? 0 : UnitId(actor.GetLinkedCharacterState()),
                actor.GetHP(), actor.GetHP(), actor.Initialized, actor.HasStatusEffect("lifelink"),
                actor.IsTrueFinalBoss(), actor.IsOuterTrainBoss())).ToArray(), false, 0);
        }

        private bool HasCopyUnitsCard(AllGameManagers managers)
        {
            if (copyCardlessStatusRequired) return true;
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            foreach (CardState card in projection.KnownCards)
            {
                string id = card.GetCardDataID();
                if (!checkedCopyCardIds.Add(id)) continue;
                if (data.FindCardData(id)?.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectCopyUnits" ||
                    effect.GetParamCardUpgradeData()?.GetRoomModifierUpgrades().Any(RoomSpawnCopyProbe.Known) == true ||
                    effect.GetParamCharacterData()?.GetRoomModifiersData().Any(RoomSpawnCopyProbe.Known) == true) == true)
                {
                    copyCardlessStatusRequired = true;
                    break;
                }
            }
            return copyCardlessStatusRequired;
        }

        private static StatisticQueryFrame CaptureQueryFrame(AllGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager();
            PlayerManager player = managers.GetPlayerManager();
            CombatManager combat = managers.GetCombatManager()!;
            var resurrection = managers.GetRelicManager().GetRelicEffect<RelicEffectPyreHeartResurrection>();
            return new StatisticQueryFrame(player.GetEnergy(), combat.GetIsRunningCombat(), combat.GetTurnCount(),
                save.GetForgePoints(), save.GetDragonsHoardAmount(), (int)player.CurrentMoonPhase,
                resurrection != null && !resurrection.IsResurrectionAllowed(save) ? 1 : 0,
                save.GetGameSequence() == SaveData.GameSequence.InBattle, managers.GetRoomManager()!.GetSelectedRoom(),
                (bool)AccessTools.Field(typeof(PlayerManager), "isMoonPhaseTrackingActive").GetValue(player),
                (bool)AccessTools.Field(typeof(PlayerManager), "isForgeTrackingActive").GetValue(player),
                save.IsForgeToggleActive(), save.GetBalatroGrosMichelCombatRoundCount(),
                OuterBossDefinitions.IsAngelDuoScenario(save.GetCurrentScenarioData()?.name) ?
                    new ForcedRelentlessCounters((int)AccessTools.Field(typeof(CombatManager), "totalWaves").GetValue(combat),
                        (int)AccessTools.Field(typeof(CombatManager), "turnsRemainingWhenRelentlessTriggeredEarly").GetValue(combat)) : null);
        }

        internal CardPileState[] CaptureOtherPiles()
        {
            CardManager cards = AllGameManagers.Instance!.GetCardManager()!;
            var standby = (Dictionary<CardState, RemoveFromStandByCondition>)AccessTools.Field(typeof(CardManager), "pileStandBy").GetValue(cards);
            return new[] { StandbyPileProbe.Capture(standby, projection),
                new CardPileState("DiscardBuffer", projection.CaptureCards(cards.GetDiscardBufferPile())),
                new CardPileState("Exhausted", projection.CaptureCards(cards.GetExhaustedPile())),
                new CardPileState("Purged", projection.CaptureCards(cards.GetPurgedPile())),
                new CardPileState("Eaten", projection.CaptureCards(cards.GetEatenPile())) };
        }

        private static CombatTrigger[] CaptureTriggers(CharacterState unit, List<string> interactions)
        {
            object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(unit);
            var deathWishSources = (HashSet<CharacterTriggerState>)AccessTools.Field(primary.GetType(), "deathWishSources").GetValue(primary);
            return unit.GetTriggers().Select(trigger =>
            {
                if (RelicProbe.HasTriggerCounts(AllGameManagers.Instance!))
                {
                    var point = (SpawnPoint?)AccessTools.Field(primary.GetType(), "spawnPoint").GetValue(primary);
                    if (point?.GetRoomOwner() is RoomState triggerRoom &&
                        triggerRoom.GetRoomStateTriggerCountModification(trigger.GetTrigger(), unit) != UnitRoomTriggerCountProbe.Amount(triggerRoom, unit, trigger.GetTrigger()))
                        interactions.Add("Unmodeled room trigger count modification");
                }
                CharacterTriggerData data = trigger.GetTriggerData();
                if (unit.IsSacrifice && unit.SacrificeCard != null && unit.SacrificeCard.GetTraitStates().Count > 0 &&
                    trigger.GetEffectStates().Any(effect => effect.GetCardEffect() is CardEffectHeal))
                    interactions.Add("Sacrifice healing damage traits");
                var effects = trigger.GetEffectStates().Select(effect =>
                {
                    string type = effect.GetCardEffect().GetType().Name;
                    if (type == "CardEffectNULL" &&
                        (effect.GetParamBool() || effect.GetAdditionalParamInt() != 0))
                        interactions.Add("Unmodeled triggered null-effect forwarding or targeting");
                    int value = AbilityLifecycleProbe.Known(type) ? 0 : effect.GetParamInt(), counter = 0;
                    if (type == "CardEffectDespawnCharacter")
                        counter = (int)AccessTools.Field(typeof(CardEffectDespawnCharacter), "despawnCounter")
                            .GetValue(effect.GetCardEffect());
                    if (type == "CardEffectRewardGold")
                    {
                        value = (int)AccessTools.Field(typeof(CardEffectRewardGold), "unmodifiedGoldReward")
                            .GetValue(effect.GetCardEffect());
                        if (AllGameManagers.Instance!.GetSaveManager().GetAdjustedGoldAmount(value, isReward: true) != GoldRewardModel.Adjust(value))
                            interactions.Add("Modified gold reward rules");
                    }
                    var pool = new List<CardData>();
                    if (type is "CardEffectAddBattleCard" or "CardEffectAddRunCard")
                    {
                        effect.GetFilteredCardListFromPool(AllGameManagers.Instance!.GetRelicManager(), ref pool);
                    }
                    return new CombatEffect(type, value, counter, ((CardPile)(AbilityLifecycleProbe.Known(type) ? 0 : effect.GetParamInt())).ToString(),
                        effect.GetAdditionalParamInt(), pool.Select(card => card.GetID()).ToArray(), effect.GetParamBool2(),
                        type is "CardEffectAddBattleCard" or "CardEffectAddRunCard" ? CardGenerationProbe.Definition(effect) : UnitCardCopyProbe.Definition(effect.GetSourceCardEffectData(), interactions),
                        UnitTriggerUpgradeProbe.Capture(effect, interactions), UnitTriggerActionProbe.Capture(effect, interactions),
                        type is "CardEffectDamage" or "CardEffectPyreDamageIfNoPlayerUnits" && effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null, UnitTriggerActionProbe.Scaling(effect),
                        TriggeredSummonProbe.Capture(effect), EnchantmentCombatProbe.Capture(effect), JackInTheBoxProbe.Capture(effect, unit));
                }).ToArray();
                int[] boundEquipment = trigger.GetEffectStates().Select(effect => effect.GetParentEquipment() == null ? 0 : Active!.CardId(effect.GetParentEquipment()!)).Distinct().ToArray();
                int equipmentId = boundEquipment.Length == 1 ? boundEquipment[0] : 0;
                if (boundEquipment.Length > 1 || trigger.IsFromEquipment != (equipmentId > 0)) interactions.Add("Inconsistent equipment trigger bindings");
                return new CombatTrigger(trigger.GetTrigger().ToString(), data.GetTriggerOnce(),
                    trigger.GetHasTriggeredOnce(unit.PreviewMode), trigger.GetHideVisualAndIgnoreSilence(),
                    unit.GetTriggerFireCount(trigger.GetTrigger(), trigger), effects,
                    AllGameManagers.Instance!.GetSaveManager().GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers().Contains(trigger.GetTrigger()),
                    data.GetTriggerAtThreshold(), new CombatTriggerOrigin(Active!.UpgradeKey(trigger.triggerId), equipmentId,
                        trigger.IsFromEquipment, data.GetOnlyTriggerIfEquipped()), Active!.TriggerStateId(unit, trigger), EnemySpawningProbe.TriggerConditions(data), data.GetRemoveOnRelentlessChange(),
                    RelicProbe.HasTriggerCounts(AllGameManagers.Instance!) ? trigger.isNoCountModifiersAllowed : (bool?)null,
                    trigger.GetTrigger() == CharacterTriggerData.Trigger.OnDeathwish || trigger.GetTrigger() == CharacterTriggerData.Trigger.OnDeathwishLost
                        ? deathWishSources.Contains(trigger) : (bool?)null,
                    trigger.GetTrigger() == CharacterTriggerData.Trigger.CorruptionAdded
                        ? data.IsSuppressTriggerNotification() || trigger.isSuppressTriggerNotificationOverride : (bool?)null);
            }).ToArray();
        }

        private IEnumerator Wrap(IEnumerator native, RoomState room, string kind)
        {
            StageRecord? record = null;
            try
            {
                AllGameManagers? managers = AllGameManagers.Instance;
                if (managers != null && !managers.GetSaveManager().PreviewMode && NativeWon == null)
                {
                    RoomCombatState before = Capture(room);
                    RoomManager roomManager = managers.GetRoomManager()!;
                    int pyreRoomIndex = roomManager.GetNumRooms() - 1;
                    bool externalPyreTarget = kind != "Exchange" && room.GetRoomIndex() != pyreRoomIndex &&
                        before.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Effects.Any(effect =>
                            effect.Type == "CardEffectPyreDamageIfNoPlayerUnits")));
                    CombatUnit? pyre = externalPyreTarget ? Capture(roomManager.GetRoom(pyreRoomIndex),
                        captureContext: false).Units.FirstOrDefault(unit => unit.IsPyre) : null;
                    bool requiresTowerRoster = before.Units.SelectMany(unit => unit.Triggers)
                        .SelectMany(trigger => trigger.Effects)
                        .Any(effect => (effect.Action ?? effect.UnitUpgrade)?.Target is "Tower" or "FrontInAllRooms");
                    TrainCombatState? beforeTrain = requiresTowerRoster ? CaptureTrain() : null;
                    (int RoomIndex, CombatUnit Unit)[] externalTargets = beforeTrain?.Rooms
                        .Where(item => item.RoomIndex != room.GetRoomIndex())
                        .SelectMany(item => item.Units.Select(unit => (RoomIndex: item.RoomIndex, Unit: unit)))
                        .Where(target => target.Unit.Id != (pyre?.Id ?? 0)).ToArray() ?? Array.Empty<(int, CombatUnit)>();
                    record = new StageRecord
                    {
                        Index = stages.Count,
                        Turn = managers.GetCombatManager()!.GetTurnCount(),
                        Kind = kind,
                        Room = room,
                        Before = before,
                        ExternalTargets = externalTargets.Select(target => new TowerTargetSnapshot
                            { RoomIndex = target.RoomIndex, Unit = target.Unit }).ToArray(),
                        ExternalPyre = pyre,
                        ExternalPyreRoomIndex = pyre == null ? -1 : pyreRoomIndex,
                        Predicted = kind == "Exchange" ? RoomCombatModel.Exchange(before, externalTargets: externalTargets) :
                            RoomCombatModel.Resolve(before, externalPyre: pyre,
                                externalPyreRoomIndex: pyre == null ? -1 : pyreRoomIndex, externalTargets: externalTargets)
                    };
                    stages.Add(record);
                }
            }
            catch (Exception error) { CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) Complete(record);
            }
        }

        private void Complete(StageRecord stage)
        {
            if (stage.Actual != null) return;
            try
            {
                stage.Actual = Capture(stage.Room);
                if (stage.ExternalPyre != null)
                    stage.ActualExternalPyre = Capture(AllGameManagers.Instance!.GetRoomManager()!
                        .GetRoom(stage.ExternalPyreRoomIndex), captureContext: false).Units
                        .FirstOrDefault(unit => unit.IsPyre) ?? CaptureRetained(stage.ExternalPyre.Id);
                if (stage.ExternalTargets.Length > 0)
                {
                    TrainCombatState actualTrain = CaptureTrain();
                    stage.ActualExternalTargets = stage.ExternalTargets.Select(target => new TowerTargetSnapshot
                    {
                        RoomIndex = target.RoomIndex,
                        Unit = actualTrain.Rooms.Single(item => item.RoomIndex == target.RoomIndex).Units
                            .FirstOrDefault(unit => unit.Id == target.Unit.Id) ?? CaptureRetained(target.Unit.Id)
                    }).ToArray();
                }
                if (stage.Predicted.Supported)
                {
                    JToken expected = JToken.FromObject(stage.Predicted.State!.Units);
                    JToken actual = JToken.FromObject(stage.Actual.Units);
                    stage.Difference = JToken.DeepEquals(expected, actual) ? null : "Unit states differ";
                    if (stage.ExternalPyre != null && !JToken.DeepEquals(JToken.FromObject(stage.Predicted.RetainedUnits
                        .Single(unit => unit.Id == stage.ExternalPyre.Id)), JToken.FromObject(stage.ActualExternalPyre!)))
                        stage.Difference = "External Pyre state differs";
                    foreach (TowerTargetSnapshot target in stage.ExternalTargets)
                    {
                        CombatUnit expectedUnit = stage.Predicted.RetainedUnits.Single(unit => unit.Id == target.Unit.Id);
                        CombatUnit actualUnit = stage.ActualExternalTargets!.Single(unit => unit.Unit.Id == target.Unit.Id).Unit;
                        if (!JToken.DeepEquals(JToken.FromObject(expectedUnit), JToken.FromObject(actualUnit)))
                        { stage.Difference = "External Tower target state differs for unit " + target.Unit.Id; break; }
                    }
                    if (!JToken.DeepEquals(JToken.FromObject(BattleTurnProbe.ComparableContext(stage.Predicted.State.Context)!),
                        JToken.FromObject(BattleTurnProbe.ComparableContext(stage.Actual.Context)!))) stage.Difference = "Card piles, RNG or gold differ";
                    log.LogInfo("BATTLE-MODEL-" + (stage.Difference == null ? "MATCH" : "MISMATCH") +
                        " index=" + stage.Index + " kind=" + stage.Kind + " turn=" + stage.Turn +
                        " room=" + stage.Before.RoomIndex + " rounds=" + stage.Predicted.Rounds);
                }
                else log.LogInfo("BATTLE-MODEL-UNSUPPORTED index=" + stage.Index + " reason=" +
                    stage.Predicted.UnsupportedReason);
            }
            catch (Exception error) { CaptureFailure(error); }
        }

        internal void CaptureFailure(Exception error)
        {
            captureFailures++;
            log.LogError("BATTLE-CAPTURE-FAIL " + error);
        }

        private CombatUnit CaptureRetained(int id)
        {
            CharacterState actor = identities.Single(pair => pair.Value == id).Key;
            using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                return CaptureUnit(actor);
        }

        private void Stop(bool won)
        {
            if (NativeWon != null) return;
            TerminalEffectsSettled = !(bool)AccessTools.Field(typeof(CombatManager), "cardEffectResolving")
                .GetValue(AllGameManagers.Instance!.GetCombatManager());
            if (TerminalEffectsSettled != true)
            {
                CaptureFailure(new InvalidOperationException("Terminal capture preceded completion of the resolving card effect."));
                return;
            }
            NativeWon = won;
            foreach (StageRecord stage in stages.Where(stage => stage.Actual == null).ToArray()) Complete(stage);
            trainCombat.CompletePending();
            spawning.CompletePending();
            actions.Complete();
            turns.Complete(won ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated);
            Checkpoint("terminal");
#if DEVELOPMENT_PROBE
            if (RollingAdviceScenario.Enabled && !PlayerAdvisorProbe.Enabled) TerminalDecision = CaptureDecision();
#endif
            OuterBossPyreProbe.CompleteTerminal();
#if DEVELOPMENT_PROBE
            OuterBossPhaseProbe.CompleteTerminal(won);
#endif
            if (won && OriginalBalatroEventCavendishScenario.Prepared)
            {
                try { OriginalBalatroEventCavendishScenario.CaptureCardlessCallbacks(AllGameManagers.Instance!, log); }
                catch (Exception error) { CaptureFailure(error); }
            }
            log.LogInfo("BATTLE-TERMINAL won=" + won + " pyre=" + AllGameManagers.Instance!.GetSaveManager().GetTowerHP());
#if DEVELOPMENT_PROBE
            PlayerAdvisorProbe.CaptureTerminal();
#endif
            // Tick exports once, after this native coroutine and its pending
            // observation wrappers have returned. Exporting here would duplicate
            // the entire capture and could still include an unfinished wrapper.
        }

        internal string Write()
        {
            BossActionTargetProbe.Write(this, log);
            BossActionControllerProbe.Write(this, log);
            OuterBossPhaseProbe.Write(log);
            OuterBossBirthProbe.Write(log);
#if DEVELOPMENT_PROBE
            AngelDuoForcedRelentlessProbe.Write(log);
            AngelDuoCompanionPhaseProbe.Write(log);
#endif
            OuterBossPyreProbe.Write(log);
            CharacterRemovalProbe.Write();
            PhysicalPlaneProbe.Write();
            EnchantmentSourceOrderProbe.Write();
            bool binary = Environment.GetEnvironmentVariable("MT2_PROBE_BINARY_CAPTURE") == "1";
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, binary ? "full-battle.mt2f" : "full-battle.json");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            string temporary = path + ".tmp";
            var snapshot = new
            {
                Schema = OriginalRapidRaiseScenario.Prepared ? 267 : OriginalStarterReformScenario.Prepared ? 266 : OriginalPerfectInsanityScenario.Prepared ? 265 : OriginalTitansGratitudeScenario.Prepared ? 264 : OriginalFeedingFrenzyScenario.Prepared ? 263 : OriginalStarterFrostbiteDiscardScenario.Prepared ? 262 : OriginalDrainScenario.Prepared ? 261 : OriginalDeadlyVariantScenario.Prepared ? 260 : OriginalPyreGroScenario.Prepared ? 259 : OriginalSmeltedTreasureScenario.Prepared ? 258 : OriginalSelfPurgeEnergyGainScenario.Prepared ? 257 : OriginalMoonRitualScenario.Prepared ? 256 : OriginalOfferingTokenScenario.Prepared ? 255 : OriginalWickedBlazeScenario.Prepared ? 254 : OriginalBrokenMemoriesScenario.Prepared ? 253 : OriginalMoldedScenario.Prepared ? 252 : OriginalLazarusLeagueSpikeScenario.Prepared ? 251 : OriginalCopyMorselTwiceScenario.Prepared ? 250 : OriginalEagerConscriptScenario.Prepared ? 249 : OriginalTomeOfContractsScenario.Prepared ? 248 : OriginalCardDrawPotionScenario.Prepared ? 247 : OriginalCorruptedAngelsScenario.Enabled ? 246 : OriginalPerkeoScenario.Enabled ? 245 : OriginalOpeningDeckSpawnScenario.Enabled ? 244 : OriginalOpeningMorselsScenario.Enabled ? 243 : OriginalDecayDeathScenario.Enabled ? 242 : OriginalBoneDogScenario.Enabled ? 241 : OriginalGrosMichelScenario.Enabled ? 240 : OriginalSpawnRetreatScenario.Prepared ? 239 : OriginalWhelpCooldownScenario.Prepared ? 238 : OriginalCardTypeSequenceScenario.Prepared ? 237 : OriginalWolosBloodletterScenario.Prepared ? 236 : OriginalExpensiveCardUpgradeScenario.Prepared ? 235 : OriginalSuppressExhaustScenario.Prepared ? 234 : OriginalRandomDrawCostScenario.Prepared ? 233 : OriginalMooncycleFaeScenario.Prepared ? 232 : OriginalStartTurnPropagationScenario.Prepared ? 231 : OriginalRandomHandUpgradeScenario.Prepared ? 230 : OriginalRandomFreezeScenario.Prepared ? 229 : OriginalFreezeReduceCostScenario.Prepared ? 228 : OriginalSpawnSilenceScenario.Prepared ? 227 : OriginalKillAttackScenario.Prepared ? 226 : OriginalHealDamageScenario.Prepared ? 225 : OriginalBurnoutDamageScenario.Prepared ? 224 : OriginalMoreCapacityScenario.Prepared ? 223 : OriginalEatenDamageScenario.Prepared ? 222 : OriginalConsumeDamageScenario.Prepared ? 221 : OriginalCardPlayDamageScenario.Prepared ? 220 : OriginalDeckTurnDrawScenario.Prepared ? 219 : OriginalCooldownOnDamageScenario.Prepared ? 218 : OriginalTribouletScenario.Prepared ? 217 : MixScenario.MothersRemnant3Mode && MixScenario.Prepared ? 216 : OriginalPropagateOnStatusEffectAppliedScenario.Prepared ? 215 : OriginalMoonPhaseRoomsGainMagicPowerScenario.Prepared ? 214 : OriginalFirstImpBonusScenario.Prepared ? 213 : OriginalNoUpgradeCardCostScenario.Prepared ? 212 : OriginalJudgesScalesScenario.Prepared ? 211 : OriginalNimsOathScenario.Prepared ? 210 : OriginalFrontRoomHealthUpgradeScenario.Prepared ? 207 : OriginalRebalanceValorScenario.Prepared ? 206 : OriginalFirstCardCostScenario.Prepared ? 205 : OriginalBalatroEventCavendishScenario.Prepared ? 204 : OriginalStartCombatHoardScenario.Prepared ? 201 : OriginalHephsBlueprintsScenario.Prepared ? 200 : OriginalExtraFeedTriggerScenario.Prepared ? 199 : OriginalChanceToCleanseScenario.Prepared ? 198 : OriginalSummonWithAvariceScenario.Prepared ? 197 : OriginalChanceForSpellWeaknessScenario.Prepared ? 196 : OriginalTwoChargesOnCardConsumedScenario.Prepared ? 195 : OriginalBalatroEventYorickScenario.Prepared ? 194 : OriginalFinalEndingArtifactScenario.Prepared ? 193 : OriginalAddSnackOnKillScenario.Prepared ? 192 : MixScenario.UncontrolledGroupMode && MixScenario.Prepared ? 191 : OriginalMoonPhaseStatusRelicScenario.Prepared ? 190 : OriginalGarfieldBoxRandomCardScenario.Prepared ? 189 : MixScenario.MonsterSpellTargetingMode && MixScenario.Prepared ? 188 : OriginalStartRoomAttachmentScenario.Floor3 && OriginalStartRoomAttachmentScenario.Prepared ? 187 : OriginalStartRoomAttachmentScenario.Floor2 && OriginalStartRoomAttachmentScenario.Prepared ? 186 : OriginalPurgeToExhaustScenario.Prepared ? 184 : BumpScenario.PyregelRelicAcquired ? 185 : OriginalSoloUnitUpgradeScenario.Prepared ? 183 : BumpScenario.DamageRelicAcquired ? 182 : MixScenario.CopyConsumedCardMode && MixScenario.Prepared ? 181 : MixScenario.CopyFirstConsumeMode && MixScenario.Prepared ? 180 : OriginalExtractForEmberAndEchosScenario.Prepared ? 179 : OriginalPyreRevengeScenario.Prepared ? 178 : OriginalRageOnBurnoutStackLossScenario.Prepared ? 177 : OriginalBrokenRailScenario.Prepared ? 176 : OriginalCardDrawOnHealScenario.Prepared ? 175 : OriginalFunguyDeckScalingScenario.Prepared ? 174 : MixScenario.CopyFirstSpellMode && MixScenario.Prepared ? 173 : MixScenario.NefariousOverflowMode && MixScenario.Prepared ? 172 : OriginalCheaperSpellCostScenario.Prepared ? 171 : OriginalEarlyDiscardEnergyScenario.Prepared ? 170 : OriginalBigDamageScenario.Prepared ? 169 : OriginalStartRoomAttachmentScenario.Prepared ? 168 : OriginalFirstRoomAttachmentScenario.Prepared ? 167 : OriginalMoonPhaseHealRelicScenario.Prepared ? 166 : OriginalCarriageJackScenario.Prepared ? 165 : CardEffectRemoveAllStatusScenario.Prepared ? 164 : DragonHoardScenario.Prepared ? 162 : UnplayedCardTriggerScenario.Prepared ? 161 : UnplayedCardEffectsScenario.Prepared ? 163 : OriginalCardDrawOnCardPlayScenario.Acquired ? 160 : RelicCardModifierScenario.CastTriggerUnitUpgrade ? 158 : RelicCardModifierScenario.CastTriggerUpgrade ? 157 : CardEffectRecursionScenario.DiscardMode ? 154 : CardEffectRecursionScenario.UpgradeReplacementChooseMode ? 153 : CardEffectRecursionScenario.UpgradeFilterChooseMode ? 152 : CardEffectRecursionScenario.UpgradeChooseMode ? 151 : CardEffectRecursionScenario.UpgradeMode ? 149 : CardEffectRecursionScenario.FilterMode ? 148 : CardEffectRecursionScenario.UpgradeToHandMode ? 150 : OriginalCardAttackRelicScenario.Prepared ? 147 : RelicCardModifierScenario.UnhealedHealthUpgrade ? 145 : OriginalMoonPhaseEnergyRelicScenario.Prepared ? 146 : SpawnStatusRelicScenario.OriginalChicotRelic ? 156 : SpawnStatusRelicScenario.OriginalTunedGearRelic ? 155 : SpawnStatusRelicScenario.Prepared && SpawnStatusRelicScenario.OriginalTeamSpawnStatusRelic ? 144 : RoomCapacityAttachmentScenario.Prepared ? 139 : RandomDiscardScenario.Prepared ? 138 : RelicCardModifierScenario.ExtraToughTombs ? 137 : OriginalUnitUpgradeTriggerRelicScenario.Prepared ? 133 : Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_EPHEMERAL_UPGRADE") == "1" ? 135 : Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_REPLACEMENT_UPGRADE") == "1" ? 129 : OriginalRoomEntryStatusRelicScenario.Prepared ? 132 : OriginalCardPlayMagicPowerScenario.Acquired ? 128 : OriginalEndlessReturnScenario.Prepared ? 126 : HandUpgradeScenario.PermanentConsumeOverTemporaryPrepared ? 125 : GenerationScenario.PermanentConsumePrepared ? 124 : HandUpgradeScenario.PermanentConsumePrepared ? 123 : RelicCardModifierScenario.ExhaustUpgrade ? 122 : SpawnStatusRelicScenario.Prepared && SpawnStatusRelicScenario.MixedCardlessOrder ? 121 : MagebladeScenario.Prepared && MagebladeScenario.OriginalMagnitudeRelic ? 143 : MagebladeScenario.Prepared ? 127 : OriginalFloorCapacityRelicScenario.Prepared ? 119 : PyreResourceRefreshScenario.Prepared ? 118 : OriginalResourceRelicBatchScenario.Prepared ? 117 : OriginalDecayRelicBatchScenario.Prepared ? 116 : OriginalLifecycleRelicBatchScenario.Prepared ? 115 : OriginalMagnitudeRelicBatchScenario.Prepared ? 114 : OriginalStatusRelicBatchScenario.BurnoutStacks ? 142 : OriginalStatusRelicBatchScenario.Prepared ? 136 : TypedDrawProbe.Enabled ? 112 : SpawnStatusRelicScenario.Prepared && SpawnStatusRelicScenario.OriginalStealthWaxersRelic ? 140 : SpawnStatusRelicScenario.Prepared ? SpawnStatusRelicScenario.Clones ? 111 : 110 : TriggerCountScenario.Prepared ? 109 : AbilityIncantScenario.Prepared ? 108 : CardAnimationSettlement.Enabled ? 106 : DeathDissolveSettlement.Enabled ? 105 : EnchantmentBattleScenario.Prepared ? 104 : 103,
                OriginalFunguyDeckScalingPrepared = OriginalFunguyDeckScalingScenario.Prepared,
                OriginalCardDrawOnHealPrepared = OriginalCardDrawOnHealScenario.Prepared,
                OriginalAddSnackOnKillPrepared = OriginalAddSnackOnKillScenario.Prepared,
                OriginalFinalEndingArtifactPrepared = OriginalFinalEndingArtifactScenario.Prepared,
                OriginalBalatroEventYorickPrepared = OriginalBalatroEventYorickScenario.Prepared,
                OriginalBalatroEventYorickCarrierId = OriginalBalatroEventYorickScenario.CarrierInstanceId,
                OriginalBalatroEventCavendishPrepared = OriginalBalatroEventCavendishScenario.Prepared,
                OriginalBalatroEventCavendishMonsterCardId = OriginalBalatroEventCavendishScenario.MonsterCardId,
                OriginalBalatroEventCavendishChampionCardId = OriginalBalatroEventCavendishScenario.ChampionCardId,
                OriginalBalatroEventCavendishSpellCardId = OriginalBalatroEventCavendishScenario.SpellCardId,
                OriginalBalatroEventCavendishEndCombatRestored = OriginalBalatroEventCavendishScenario.EndCombatRestored,
                OriginalBalatroEventCavendishEndCombat = OriginalBalatroEventCavendishScenario.EndCombatChecks,
                OriginalBalatroEventCavendishCardlessCallbacksRestored = OriginalBalatroEventCavendishScenario.CardlessCallbacksRestored,
                OriginalBalatroEventCavendishCardlessCallbacks = OriginalBalatroEventCavendishScenario.CardlessCallbacks,
                OriginalBrokenRailPrepared = OriginalBrokenRailScenario.Prepared,
                OriginalHephsBlueprintsPrepared = OriginalHephsBlueprintsScenario.Prepared,
                OriginalHephsBlueprintsDefinition = OriginalHephsBlueprintsScenario.Definition,
                OriginalRageOnBurnoutStackLossPrepared = OriginalRageOnBurnoutStackLossScenario.Prepared,
                OriginalPyreRevengePrepared = OriginalPyreRevengeScenario.Prepared,
                OriginalExtractForEmberAndEchosPrepared = OriginalExtractForEmberAndEchosScenario.Prepared,
                OriginalExtractForEmberAndEchosCarrierId = OriginalExtractForEmberAndEchosScenario.CarrierInstanceId,
                OriginalExtractForEmberAndEchosCallbacks = OriginalExtractForEmberAndEchosScenario.Callbacks,
                OriginalCopyFirstConsumePlayedPrepared = MixScenario.CopyFirstConsumeMode && MixScenario.Prepared,
                OriginalCopyConsumedCardPrepared = MixScenario.CopyConsumedCardMode && MixScenario.Prepared,
                OriginalGarfieldBoxRandomCardPrepared = OriginalGarfieldBoxRandomCardScenario.Prepared,
                OriginalDamageOnFloorTransitionPrepared = BumpScenario.DamageRelicAcquired,
                OriginalPyregelOnAscendPrepared = BumpScenario.PyregelRelicAcquired,
                OriginalPyreRevengeInstallation = OriginalPyreRevengeScenario.Installation,
                OriginalFunguyDeckScaling = OriginalFunguyDeckScalingScenario.Evidence,
                OriginalCopyFirstSpellPlayedPrepared = MixScenario.CopyFirstSpellMode && MixScenario.Prepared,
                OriginalMonsterSpellTargetingPrepared = MixScenario.MonsterSpellTargetingMode && MixScenario.Prepared,
                OriginalNefariousOverflowPrepared = MixScenario.NefariousOverflowMode && MixScenario.Prepared,
                OriginalUncontrolledGroupPrepared = MixScenario.UncontrolledGroupMode && MixScenario.Prepared,
                DeathDissolveSettlementEnabled = DeathDissolveSettlement.Enabled,
                OuterBossTurnsEnabled = OuterBossPhaseProbe.TurnsEnabled,
                OriginalOuterBossRoute = OriginalOuterBossRoute.Control,
                OriginalOuterBossTrialIntro = OriginalOuterBossRoute.TrialIntroControl,
#if DEVELOPMENT_PROBE
                OriginalTrueFinalBossRoute = TrueFinalBossBirthProbe.Control,
                CovenantEnemyStatsControl = CovenantEnemyStatsScenario.Evidence,
                StartingCovenantControl = StartingCovenantScenario.Evidence,
                ChosenIncantSpellControl = ChosenIncantSpellScenario.Control,
                AngelDuoIncantControl = AngelDuoIncantScenario.Control,
                AngelDuoHarvestTransition = AngelDuoHarvestDeathScenario.Transition,
                AngelDuoDeathControl = AngelDuoDeathScenario.Control,
                AngelDuoDeathTransition = AngelDuoDeathScenario.Transition,
                AngelDuoRelentlessEntry = AngelDuoForcedRelentlessProbe.EntryTransition,
                TrueFinalBossDormantControl = TrueFinalBossDormantScenario.Evidence,
                TrueFinalBossLethalControl = TrueFinalBossLethalScenario.Evidence,
                TrueFinalBossAllyUpgradeControl = TrueFinalBossAllyUpgradeScenario.Evidence,
                TrueFinalBossOrdinaryAllyControl = TrueFinalBossOrdinaryAllyScenario.Evidence,
                TrueFinalBossRelentlessControl = TrueFinalBossRelentlessScenario.Evidence,
                TrueFinalBossRelentlessActions = TrueFinalBossRelentlessScenario.Enabled ? OuterBossPhaseProbe.CapturedActions : null,
#endif
                OriginalCarriageJackPrepared = OriginalCarriageJackScenario.Prepared,
                OriginalTwoChargesOnCardConsumedPrepared = OriginalTwoChargesOnCardConsumedScenario.Prepared,
                OriginalTwoChargesOnCardConsumedCoverage = OriginalTwoChargesOnCardConsumedScenario.Evidence,
                OriginalChanceForSpellWeaknessPrepared = OriginalChanceForSpellWeaknessScenario.Prepared,
                OriginalChanceForSpellWeaknessDefinition = OriginalChanceForSpellWeaknessScenario.Definition,
                OriginalChanceToCleansePrepared = OriginalChanceToCleanseScenario.Prepared,
                OriginalChanceToCleanseDefinition = OriginalChanceToCleanseScenario.Definition,
                OriginalExtraFeedTriggerPrepared = OriginalExtraFeedTriggerScenario.Prepared,
                OriginalExtraFeedTriggerEvidence = OriginalExtraFeedTriggerScenario.Evidence,
                OriginalSummonWithAvaricePrepared = OriginalSummonWithAvariceScenario.Prepared,
                OriginalSummonWithAvariceDefinition = OriginalSummonWithAvariceScenario.Definition,
                RelicAfterSpawnChanceTests = RelicAfterSpawnChanceProbe.Tests,
                RelicAfterSpawnChanceApplications = RelicAfterSpawnChanceProbe.Applications,
                OriginalFirstRoomAttachmentPrepared = OriginalFirstRoomAttachmentScenario.Prepared,
                OriginalStartRoomAttachmentPrepared = OriginalStartRoomAttachmentScenario.Prepared,
                OriginalEarlyDiscardEnergyPrepared = OriginalEarlyDiscardEnergyScenario.Prepared,
                OriginalCheaperSpellCostPrepared = OriginalCheaperSpellCostScenario.Prepared,
                OriginalFirstCardCostPrepared = OriginalFirstCardCostScenario.Prepared,
                OriginalRebalanceValorPrepared = OriginalRebalanceValorScenario.Prepared,
                OriginalRebalanceValorObservations = RelicValorRebalanceProbe.Observations,
                DeathDissolveSettlements = DeathDissolveSettlement.Records,
                DeathDissolveCallbacks = DeathDissolveSettlement.Callbacks,
                DecisionPlayRulesSnapshotsFresh = true,
                GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                NativeWon,
                TerminalCaptureBoundary = "AfterStopCombatLoop",
                TerminalEffectsSettled,
                NativeEffectSettlement = NativeEffectSettlement.Evidence,
                CopyOnPlayTerminal = CopyOnPlayTerminalScenario.Enabled,
                CopyOnPlayTerminalConsume = CopyOnPlayTerminalScenario.Consume,
                CopyOnPlayTerminalPurge = CopyOnPlayTerminalScenario.PurgeCopy,
                CopyOnPlayTerminalThreat = CopyOnPlayTerminalScenario.LethalBoss,
                CopyOnPlayTerminalCardIds = CopyOnPlayTerminalScenario.CardIds,
                Policy = Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY"),
                ModifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS"),
                OriginalChampionStartTurnHealSchema = OriginalChampionStartTurnHealScenario.Prepared ? 208 : 0,
                OriginalChampionStartTurnHeal = OriginalChampionStartTurnHealScenario.Prepared ? OriginalChampionStartTurnHealScenario.Evidence : null,
                OriginalNimsOathSchema = OriginalNimsOathScenario.Prepared ? 210 : 0,
                OriginalNimsOath = OriginalNimsOathScenario.Prepared ? OriginalNimsOathScenario.Evidence : null,
                RelicNimsOathRoomTests = RelicNimsOathProbe.RoomTests,
                RelicNimsOathTimingTests = RelicNimsOathProbe.TimingTests,
                RelicNimsOathApplications = RelicNimsOathProbe.Applications,
                OriginalJudgesScalesSchema = OriginalJudgesScalesScenario.Prepared ? 211 : 0,
                OriginalJudgesScales = OriginalJudgesScalesScenario.Prepared ? OriginalJudgesScalesScenario.Evidence : null,
                OriginalFirstImpBonusSchema = OriginalFirstImpBonusScenario.Prepared ? 213 : 0,
                OriginalFirstImpBonus = OriginalFirstImpBonusScenario.Prepared ? OriginalFirstImpBonusScenario.Evidence : null,
                OriginalMoonPhaseRoomsGainMagicPowerSchema = OriginalMoonPhaseRoomsGainMagicPowerScenario.Prepared ? 214 : 0,
                OriginalMoonPhaseRoomsGainMagicPower = OriginalMoonPhaseRoomsGainMagicPowerScenario.Prepared ? OriginalMoonPhaseRoomsGainMagicPowerScenario.Evidence : null,
                OriginalMothersRemnant3Schema = MixScenario.MothersRemnant3Mode && MixScenario.Prepared ? 216 : 0,
                OriginalMothersRemnant3Prepared = MixScenario.MothersRemnant3Mode && MixScenario.Prepared,
                OriginalTribouletSchema = OriginalTribouletScenario.Prepared ? 217 : 0,
                OriginalTribouletPrepared = OriginalTribouletScenario.Prepared,
                OriginalTriboulet = OriginalTribouletScenario.Prepared ? OriginalTribouletScenario.Evidence : null,
                OriginalCooldownOnDamageSchema = OriginalCooldownOnDamageScenario.Prepared ? 218 : 0,
                OriginalDeckTurnDrawSchema = OriginalDeckTurnDrawScenario.Prepared ? 219 : 0,
                OriginalCardPlayDamageSchema = OriginalCardPlayDamageScenario.Prepared ? 220 : 0,
                OriginalConsumeDamageSchema = OriginalConsumeDamageScenario.Prepared ? 221 : 0,
                OriginalEatenDamageSchema = OriginalEatenDamageScenario.Prepared ? 222 : 0,
                OriginalMoreCapacitySchema = OriginalMoreCapacityScenario.Prepared ? 223 : 0,
                OriginalBurnoutDamageSchema = OriginalBurnoutDamageScenario.Prepared ? 224 : 0,
                OriginalHealDamageSchema = OriginalHealDamageScenario.Prepared ? 225 : 0,
                OriginalKillAttackSchema = OriginalKillAttackScenario.Prepared ? 226 : 0,
                OriginalSpawnSilenceSchema = OriginalSpawnSilenceScenario.Prepared ? 227 : 0,
                OriginalFreezeCostSchema = OriginalFreezeReduceCostScenario.Prepared ? 228 : 0,
                OriginalRandomFreezeSchema = OriginalRandomFreezeScenario.Prepared ? 229 : 0,
                OriginalRandomHandUpgradeSchema = OriginalRandomHandUpgradeScenario.Prepared ? 230 : 0,
                OriginalStartTurnPropagationSchema = OriginalStartTurnPropagationScenario.Prepared ? 231 : 0,
                OriginalMooncycleFaeSchema = OriginalMooncycleFaeScenario.Prepared ? 232 : 0,
                OriginalRandomDrawCostSchema = OriginalRandomDrawCostScenario.Prepared ? 233 : 0,
                OriginalSuppressExhaustSchema = OriginalSuppressExhaustScenario.Prepared ? 234 : 0,
                OriginalExpensiveCardUpgradeSchema = OriginalExpensiveCardUpgradeScenario.Prepared ? 235 : 0,
                OriginalWolosBloodletterPrepared = OriginalWolosBloodletterScenario.Prepared,
                OriginalCardTypeSequencePrepared = OriginalCardTypeSequenceScenario.Prepared,
                OriginalWhelpCooldownPrepared = OriginalWhelpCooldownScenario.Prepared,
                OriginalSpawnRetreatPrepared = OriginalSpawnRetreatScenario.Prepared,
                OriginalOpeningMorselsPrepared = OriginalOpeningMorselsScenario.Prepared,
                OriginalOpeningForsakenTrial = OriginalOpeningMorselsScenario.EnemyTrial && OriginalOpeningMorselsScenario.Prepared,
                CardBackedFeeding = OriginalOpeningMorselsScenario.FoodCardsMode,
                SoldierOfFortunePrepared = SoldierOfFortuneScenario.Prepared,
                SoldierOfFortuneGoldTests = SoldierOfFortuneScenario.GoldTests,
                SoldierOfFortuneEmptyTargetTest = SoldierOfFortuneScenario.EmptyTargetTest,
                SoldierOfFortuneNullRecords = SoldierOfFortuneScenario.NullRecords,
                RestorationDetonationPrepared = RestorationDetonationScenario.Prepared,
                RestorationDetonationCardIds = RestorationDetonationScenario.HealCardIds,
                RestorationDetonationRecords = RestorationDetonationScenario.Records,
                AdaptiveMutationPrepared = AdaptiveMutationScenario.Prepared,
                AdaptiveMutationCardIds = AdaptiveMutationScenario.CardIds,
                AdaptiveMutationRecords = AdaptiveMutationScenario.Records,
                MixalotPrepared = MixalotScenario.Prepared,
                MixalotCardIds = MixalotScenario.CardIds,
                MixalotRecords = MixalotScenario.Records,
                SmallBoxPrepared = SmallBoxScenario.Prepared,
                SmallBoxCardIds = SmallBoxScenario.CardIds,
                SmallBoxRecords = SmallBoxScenario.Records,
                MagneticRelayPrepared = MagneticRelayScenario.Prepared,
                MagneticRelayCardIds = MagneticRelayScenario.CardIds,
                MagneticRelayUnitCardIds = MagneticRelayScenario.UnitCardIds,
                MagneticRelayRecords = MagneticRelayScenario.Records,
                TranscendImpPrepared = TranscendImpScenario.Prepared,
                TranscendImpCardIds = TranscendImpScenario.CardIds,
                TranscendImpSourceCardIds = TranscendImpScenario.SourceCardIds,
                TranscendImpRecallCardId = TranscendImpScenario.RecallCardId,
                TranscendImpRecords = TranscendImpScenario.Records,
                EquipmentMergePrepared = EquipmentMergeScenario.Prepared,
                EquipmentMergeCardIds = EquipmentMergeScenario.CardIds,
                EquipmentMergeSourceIds = EquipmentMergeScenario.SourceIds,
                EquipmentMergeUnitIds = EquipmentMergeScenario.UnitIds,
                EquipmentMergeRecords = EquipmentMergeScenario.Records,
                EquipmentMergeTests = EquipmentMergeScenario.Tests,
                EclipseChamberPrepared = EclipseChamberScenario.Prepared,
                EclipseChamberCallbacks = EclipseChamberScenario.Callbacks,
                EclipseChamberShadeCallbacks = EclipseChamberScenario.ShadeCallbacks,
                EclipseChamberMovement = EclipseChamberScenario.Movement,
                EclipseChamberEnemyMovement = EclipseChamberScenario.EnemyMovement,
                EclipseChamberEnemyCallbacks = EclipseChamberScenario.EnemyCallbacks,
                EclipseChamberEnemyConduit = EclipseChamberScenario.EnemyConduit,
                EclipseChamberMixedConduit = EclipseChamberScenario.MixedConduit,
                EclipseChamberCardIds = EclipseChamberScenario.CardIds,
                EclipseChamberRecords = EclipseChamberScenario.Records,
                OriginalRoomCardsPrepared = OriginalRoomCardsScenario.Prepared,
                OriginalRoomCardsIds = OriginalRoomCardsScenario.CardIds,
                OriginalForgeCardsPrepared = OriginalForgeCardsScenario.Prepared,
                OriginalForgeCardsIds = OriginalForgeCardsScenario.CardIds,
                OriginalStatusCardsPrepared = OriginalStatusCardsScenario.Prepared,
                OriginalStatusCardsIds = OriginalStatusCardsScenario.CardIds,
                OriginalDeckSpellPrepared = OriginalDeckSpellScenario.Prepared,
                OriginalDeckSpellIds = OriginalDeckSpellScenario.CardIds,
                OriginalDeckSpellPileControlIds = OriginalDeckSpellScenario.PileControlIds,
                OriginalDeepOfferingPrepared = OriginalDeepOfferingScenario.Prepared,
                OriginalDeepOfferingCardIds = OriginalDeepOfferingScenario.CardIds,
                OriginalMoldedPrepared = OriginalMoldedScenario.Prepared,
                OriginalMoldedCardIds = OriginalMoldedScenario.CardIds,
                OriginalBrokenMemoriesPrepared = OriginalBrokenMemoriesScenario.Prepared,
                OriginalBrokenMemoriesCardIds = OriginalBrokenMemoriesScenario.CardIds,
                OriginalWickedBlazePrepared = OriginalWickedBlazeScenario.Prepared,
                OriginalWickedBlazeCardIds = OriginalWickedBlazeScenario.CardIds,
                OriginalOfferingTokenPrepared = OriginalOfferingTokenScenario.Prepared,
                OriginalOfferingTokenCardIds = OriginalOfferingTokenScenario.CardIds,
                OriginalMoonRitualPrepared = OriginalMoonRitualScenario.Prepared,
                OriginalMoonRitualCardIds = OriginalMoonRitualScenario.CardIds,
                OriginalSelfPurgeEnergyGainPrepared = OriginalSelfPurgeEnergyGainScenario.Prepared,
                OriginalSelfPurgeEnergyGainCardIds = OriginalSelfPurgeEnergyGainScenario.CardIds,
                OriginalSmeltedTreasurePrepared = OriginalSmeltedTreasureScenario.Prepared,
                OriginalSmeltedTreasureCardIds = OriginalSmeltedTreasureScenario.CardIds,
                OriginalPyreGroPrepared = OriginalPyreGroScenario.Prepared,
                OriginalPyreGroCardIds = OriginalPyreGroScenario.CardIds,
                OriginalDeadlyVariantPrepared = OriginalDeadlyVariantScenario.Prepared,
                OriginalDeadlyVariantCardIds = OriginalDeadlyVariantScenario.CardIds,
                OriginalDrainPrepared = OriginalDrainScenario.Prepared,
                OriginalDrainCardIds = OriginalDrainScenario.CardIds,
                OriginalStarterFrostbiteDiscardPrepared = OriginalStarterFrostbiteDiscardScenario.Prepared,
                OriginalStarterFrostbiteDiscardCardIds = OriginalStarterFrostbiteDiscardScenario.CardIds,
                OriginalFeedingFrenzyPrepared = OriginalFeedingFrenzyScenario.Prepared,
                OriginalFeedingFrenzyCardIds = OriginalFeedingFrenzyScenario.CardIds,
                OriginalTitansGratitudePrepared = OriginalTitansGratitudeScenario.Prepared,
                OriginalTitansGratitudeCardIds = OriginalTitansGratitudeScenario.CardIds,
                OriginalPerfectInsanityPrepared = OriginalPerfectInsanityScenario.Prepared,
                OriginalPerfectInsanityCardIds = OriginalPerfectInsanityScenario.CardIds,
                OriginalRapidRaisePrepared = OriginalRapidRaiseScenario.Prepared,
                OriginalRapidRaiseCardIds = OriginalRapidRaiseScenario.CardIds,
                OriginalStarterReformPrepared = OriginalStarterReformScenario.Prepared,
                OriginalStarterReformCardIds = OriginalStarterReformScenario.CardIds,
                GoldmancePrepared = GoldmanceScenario.Prepared,
                GoldmanceCardIds = GoldmanceScenario.CardIds,
                EngineUpgradePrepared = EngineUpgradeScenario.Prepared,
                EngineUpgradeCardIds = EngineUpgradeScenario.CardIds,
                StewardsQuartersPrepared = StewardsQuartersScenario.Prepared,
                StewardsQuartersCardIds = StewardsQuartersScenario.CardIds,
                ZoneOfSilencePrepared = ZoneOfSilenceScenario.Prepared,
                ZoneOfSilenceCardIds = ZoneOfSilenceScenario.CardIds,
                MemorialPrepared = MemorialScenario.Prepared,
                MemorialCardIds = MemorialScenario.CardIds,
                PlanetariumPrepared = PlanetariumScenario.Prepared,
                PlanetariumCardIds = PlanetariumScenario.CardIds,
                GrowRoomPrepared = GrowRoomScenario.Prepared,
                GrowRoomCardIds = GrowRoomScenario.CardIds,
                GrowRoomStatusFilter = GrowRoomScenario.StatusFilter,
                MushyRoomPrepared = MushyRoomScenario.Prepared,
                MushyRoomCardIds = MushyRoomScenario.CardIds,
                OriginalPiercingCardsPrepared = OriginalPiercingCardsScenario.Prepared,
                OriginalPiercingCardsIds = OriginalPiercingCardsScenario.CardIds,
                OriginalUnpurgableCardsPrepared = OriginalUnpurgableCardsScenario.Prepared,
                OriginalUnpurgableCardsIds = OriginalUnpurgableCardsScenario.CardIds,
                OriginalHandCostsPrepared = OriginalHandCostsScenario.Prepared,
                OriginalHandCostsIds = OriginalHandCostsScenario.CardIds,
                OriginalHandCostsQueryIds = OriginalHandCostsScenario.QueryIds,
                OriginalHandCostsSamples = OriginalHandCostsScenario.Samples,
                OriginalLimitedRangePrepared = OriginalLimitedRangeScenario.Prepared,
                OriginalLimitedRangeIds = OriginalLimitedRangeScenario.CardIds,
                OriginalLimitedRangeQueries = OriginalLimitedRangeScenario.Queries,
                OriginalLimitedRangeQueriesUnchanged = OriginalLimitedRangeScenario.QueriesUnchanged,
                OriginalMonsterHandCostsPrepared = OriginalMonsterHandCostsScenario.Prepared,
                OriginalMonsterHandCostsIds = OriginalMonsterHandCostsScenario.CardIds,
                OriginalMonsterHandCostsSamples = OriginalMonsterHandCostsScenario.Samples,
                OriginalMonsterHandCostsDeploymentQueries = OriginalMonsterHandCostsScenario.DeploymentQueries,
                OriginalMonsterHandCostsQueriesUnchanged = OriginalMonsterHandCostsScenario.QueriesUnchanged,
                OriginalUnplayableCardsPrepared = OriginalUnplayableCardsScenario.Prepared,
                OriginalUnplayableCardsIds = OriginalUnplayableCardsScenario.CardIds,
                OriginalUnplayableCardsNegativeIds = OriginalUnplayableCardsScenario.NegativeIds,
                OriginalUnplayableCardsQueries = OriginalUnplayableCardsScenario.Queries,
                OriginalUnplayableCardsQueriesUnchanged = OriginalUnplayableCardsScenario.QueriesUnchanged,
                OriginalPermafrostCardsPrepared = OriginalPermafrostCardsScenario.Prepared,
                HoldoverCalibrationPrepared = HoldoverCalibration.Prepared,
                HoldoverControlsUnchanged = HoldoverCalibration.ControlsUnchanged,
                HoldoverCallbacks = HoldoverCalibration.Rows,
                OriginalPermafrostCardsIds = OriginalPermafrostCardsScenario.CardIds,
                OriginalPermafrostCardsJunkId = OriginalPermafrostCardsScenario.JunkId,
                OriginalPermafrostCardsInitial = OriginalPermafrostCardsScenario.Initial,
                OriginalPermafrostCardsDraws = OriginalPermafrostCardsScenario.Draws,
                OriginalPermafrostCardsQueriesUnchanged = OriginalPermafrostCardsScenario.QueriesUnchanged,
                OriginalOverflowCardsPrepared = OriginalOverflowCardsScenario.Prepared,
                OriginalOverflowCardsIds = OriginalOverflowCardsScenario.CardIds,
                OriginalCorruptRestrictedPrepared = OriginalCorruptRestrictedScenario.Prepared,
                OriginalCorruptRestrictedIds = OriginalCorruptRestrictedScenario.CardIds,
                OriginalCorruptRestrictedQueries = OriginalCorruptRestrictedScenario.Queries,
                OriginalCorruptRestrictedPrePlays = OriginalCorruptRestrictedScenario.PrePlays,
                OriginalCorruptRestrictedQueriesUnchanged = OriginalCorruptRestrictedScenario.QueriesUnchanged,
                OriginalStatusStackDamagePrepared = OriginalStatusStackDamageScenario.Prepared,
                OriginalStatusStackDamageIds = OriginalStatusStackDamageScenario.CardIds,
                OriginalStatusStackDamageSamples = OriginalStatusStackDamageScenario.Samples,
                OriginalSpecificCardDamagePrepared = OriginalSpecificCardDamageScenario.Prepared,
                OriginalSpecificCardDamageIds = OriginalSpecificCardDamageScenario.CardIds,
                OriginalSpecificCardDamageQueries = OriginalSpecificCardDamageScenario.Queries,
                OriginalSpecificCardDamageQueriesUnchanged = OriginalSpecificCardDamageScenario.QueriesUnchanged,
                OriginalSpecificCardDamageSamples = OriginalSpecificCardDamageScenario.Samples,
                OriginalScaledEnergyPrepared = OriginalScaledEnergyScenario.Prepared,
                OriginalScaledEnergyIds = OriginalScaledEnergyScenario.CardIds,
                OriginalScaledEnergySamples = OriginalScaledEnergyScenario.Samples,
                OriginalStatusRemovalScalingPrepared = OriginalStatusRemovalScalingScenario.Prepared,
                OriginalStatusRemovalScalingIds = OriginalStatusRemovalScalingScenario.CardIds,
                OriginalStatusRemovalScalingSamples = OriginalStatusRemovalScalingScenario.Samples,
                OriginalStatusRemovalScalingQueries = OriginalStatusRemovalScalingScenario.Queries,
                OriginalEffectApplicationsPrepared = OriginalEffectApplicationsScenario.Prepared,
                OriginalEffectApplicationsIds = OriginalEffectApplicationsScenario.CardIds,
                OriginalEffectApplicationsCounts = OriginalEffectApplicationsScenario.Counts,
                OriginalEffectApplicationsSamples = OriginalEffectApplicationsScenario.Applications,
                OriginalPileDrawPrepared = OriginalPileDrawScenario.Prepared,
                OriginalPileDrawIds = OriginalPileDrawScenario.CardIds,
                OriginalPileDrawQueries = OriginalPileDrawScenario.Queries,
                OriginalPileDrawQueriesUnchanged = OriginalPileDrawScenario.QueriesUnchanged,
                OriginalPileDrawAdjustments = OriginalPileDrawScenario.Adjustments,
                OriginalCardDrawPotionPrepared = OriginalCardDrawPotionScenario.Prepared,
                OriginalCardDrawPotionIds = OriginalCardDrawPotionScenario.CardIds,
                OriginalEagerConscriptPrepared = OriginalEagerConscriptScenario.Prepared,
                OriginalCopyMorselTwicePrepared = OriginalCopyMorselTwiceScenario.Prepared,
                OriginalLazarusLeagueSpikePrepared = OriginalLazarusLeagueSpikeScenario.Prepared,
                OriginalLazarusLeagueSpikeIds = OriginalLazarusLeagueSpikeScenario.CardIds,
                OriginalTomeOfContractsPrepared = OriginalTomeOfContractsScenario.Prepared,
                OriginalPileRecursionPrepared = OriginalPileRecursionScenario.Prepared,
                OriginalPileRecursionIds = OriginalPileRecursionScenario.CardIds,
                OriginalPileRecursionQueries = OriginalPileRecursionScenario.Queries,
                OriginalPileRecursionQueriesUnchanged = OriginalPileRecursionScenario.QueriesUnchanged,
                OriginalPileRecursionSamples = OriginalPileRecursionScenario.Samples,
                OriginalScaledBuffPrepared = OriginalScaledBuffScenario.Prepared,
                OriginalPileStatusPrepared = OriginalPileStatusScenario.Prepared,
                OriginalDrawUpgradeScalingPrepared = OriginalDrawUpgradeScalingScenario.Prepared,
                OriginalConsumedUpgradePrepared = OriginalConsumedUpgradeScenario.Prepared,
                OriginalStatusCompositionsPrepared = OriginalStatusCompositionsScenario.Prepared,
                OriginalGraftedEquipmentPrepared = OriginalGraftedEquipmentScenario.Prepared,
                OriginalGraftedEquipmentIds = OriginalGraftedEquipmentScenario.CardIds,
                OriginalGraftedEquipmentResets = OriginalGraftedEquipmentScenario.Resets,
                OriginalGraftedEquipmentControlsUnchanged = OriginalGraftedEquipmentScenario.ControlsUnchanged,
                OriginalSelfAttackPrepared = OriginalSelfAttackScenario.Prepared,
                OriginalSelfAttackIds = OriginalSelfAttackScenario.CardIds,
                OriginalHealingCleansePrepared = OriginalHealingCleanseScenario.Prepared,
                OriginalHealingCleanseIds = OriginalHealingCleanseScenario.CardIds,
                OriginalOnKillPrepared = OriginalOnKillScenario.Prepared,
                OriginalOnKillIds = OriginalOnKillScenario.CardIds,
                OriginalOnKillDeploymentAllowed = OriginalOnKillScenario.DeploymentAllowed,
                OriginalOnKillGenerationPrepared = OriginalOnKillGenerationScenario.Prepared,
                OriginalOnKillGenerationIds = OriginalOnKillGenerationScenario.CardIds,
                OriginalMorselEatenPrepared = OriginalMorselEatenScenario.Prepared,
                OriginalMorselEatenIds = OriginalMorselEatenScenario.CardIds,
                OriginalMorselVoidIgnoredUpgradeId = OriginalMorselEatenScenario.VoidIgnoredUpgradeId,
                OriginalMorselVoidIgnoredCopy = OriginalMorselEatenScenario.VoidIgnoredCopy,
                OriginalMorselVoidIgnoredClassFilter = OriginalMorselEatenScenario.VoidIgnoredClassFilter,
                OriginalUnitBuffsPrepared = OriginalUnitBuffsScenario.Prepared,
                OriginalUnitBuffsIds = OriginalUnitBuffsScenario.CardIds,
                OriginalKillCopyPrepared = OriginalKillCopyScenario.Prepared,
                OriginalKillCopyIds = OriginalKillCopyScenario.CardIds,
                OriginalKillResourcesPrepared = OriginalKillResourcesScenario.Prepared,
                OriginalKillResource = OriginalKillResourcesScenario.Resource,
                OriginalKillResourcesIds = OriginalKillResourcesScenario.CardIds,
                OriginalFriendlyStatusesPrepared = OriginalFriendlyStatusesScenario.Prepared,
                OriginalFriendlyStatusesIds = OriginalFriendlyStatusesScenario.CardIds,
                OriginalBasicDamagePrepared = OriginalBasicDamageScenario.Prepared,
                OriginalBasicDamageIds = OriginalBasicDamageScenario.CardIds,
                OriginalResourceSpellsPrepared = OriginalResourceSpellsScenario.Prepared,
                OriginalResourceSpellsIds = OriginalResourceSpellsScenario.CardIds,
                OriginalHostileStatusesPrepared = OriginalHostileStatusesScenario.Prepared,
                OriginalHostileStatusesIds = OriginalHostileStatusesScenario.CardIds,
                OriginalFuelPrepared = OriginalFuelScenario.Prepared,
                OriginalFuelIds = OriginalFuelScenario.CardIds,
                OriginalSupportUnitsPrepared = OriginalSupportUnitsScenario.Prepared,
                OriginalSupportUnitsIds = OriginalSupportUnitsScenario.CardIds,
                OriginalIsolatedEnergyControlId = OriginalSupportUnitsScenario.IsolatedEnergyControlInstanceId,
                OriginalAttackOverridesPrepared = OriginalAttackOverridesScenario.Prepared,
                OriginalAttackOverridesIds = OriginalAttackOverridesScenario.CardIds,
                OriginalReactiveUnitsPrepared = OriginalReactiveUnitsScenario.Prepared,
                OriginalReactiveUnitsIds = OriginalReactiveUnitsScenario.CardIds,
                OriginalBaseChampionsPrepared = OriginalBaseChampionsScenario.Prepared,
                OriginalBaseChampionsIds = OriginalBaseChampionsScenario.CardIds,
                OriginalStatusUnitsPrepared = OriginalStatusUnitsScenario.Prepared,
                OriginalStatusUnitsIds = OriginalStatusUnitsScenario.CardIds,
                OriginalEquipmentCardsPrepared = OriginalEquipmentCardsScenario.Prepared,
                OriginalEquipmentCardsIds = OriginalEquipmentCardsScenario.CardIds,
                OriginalSupportSpellsPrepared = OriginalSupportSpellsScenario.Prepared,
                OriginalSupportSpellsIds = OriginalSupportSpellsScenario.CardIds,
                OriginalUnitAbilityId = OriginalUnitAbilityScenario.AbilityId,
                OriginalUnitAbilityQueries = OriginalUnitAbilityScenario.Queries,
                OriginalUnitAbilityDrafts = OriginalUnitAbilityScenario.Drafts,
                OriginalRearrangeSpellsPrepared = OriginalRearrangeSpellsScenario.Prepared,
                OriginalRearrangeSpellsIds = OriginalRearrangeSpellsScenario.CardIds,
                OriginalMovementSpellsPrepared = OriginalMovementSpellsScenario.Prepared,
                OriginalMovementSpellsIds = OriginalMovementSpellsScenario.CardIds,
                OriginalScaledDamagePrepared = OriginalScaledDamageScenario.Prepared,
                OriginalScaledDamageIds = OriginalScaledDamageScenario.CardIds,
                OriginalStatusCompositionsIds = OriginalStatusCompositionsScenario.CardIds,
                OriginalConsumedUpgradeIds = OriginalConsumedUpgradeScenario.CardIds,
                OriginalConsumedUpgradeSamples = OriginalConsumedUpgradeScenario.Samples,
                OriginalConsumedUpgradeQueries = OriginalConsumedUpgradeScenario.Queries,
                OriginalDiscardDrawPrepared = OriginalDiscardDrawScenario.Prepared,
                OriginalMoonCardUpgradesPrepared = OriginalMoonCardUpgradesScenario.Prepared,
                OriginalMoonCardUpgradesRoomMask = OriginalMoonCardUpgradesScenario.RoomMask,
                OriginalMoonCardUpgradesLunacy = OriginalMoonCardUpgradesScenario.Lunacy,
                OriginalMoonCardUpgradesCostRelic = OriginalMoonCardUpgradesScenario.CostRelic,
                OriginalMoonCardUpgradesAttackRelic = OriginalMoonCardUpgradesScenario.AttackRelic,
                OriginalMoonCardUpgradesHealingRelic = OriginalMoonCardUpgradesScenario.HealingRelic,
                OriginalMoonCardUpgradesPhaseHealRelic = OriginalMoonCardUpgradesScenario.PhaseHealRelic,
                OriginalMoonCardUpgradesPhaseEnergyRelic = OriginalMoonCardUpgradesScenario.PhaseEnergyRelic,
                OriginalMoonCardUpgradesHandPowerRelic = OriginalMoonCardUpgradesScenario.HandPowerRelic,
                OriginalMoonCardUpgradesStatusRelic = OriginalMoonCardUpgradesScenario.StatusRelic,
                OriginalMoonCardUpgradesLiveRelics = OriginalMoonCardUpgradesScenario.LiveRelics,
                MoonHandPowerCallbacks = MoonHandPowerCompositionProbe.Rows,
                OriginalMoonCardUpgradesIds = OriginalMoonCardUpgradesScenario.CardIds,
                OriginalMoonCardUpgradesHands = OriginalMoonCardUpgradesScenario.Hands,
                OriginalDiscardDrawIds = OriginalDiscardDrawScenario.CardIds,
                OriginalDiscardDrawCounts = OriginalDiscardDrawScenario.Counts,
                OriginalDiscardDrawDiscards = OriginalDiscardDrawScenario.Discards,
                OriginalSpellCompositionsPrepared = OriginalSpellCompositionsScenario.Prepared,
                OriginalSpellCompositionsIds = OriginalSpellCompositionsScenario.CardIds,
                OriginalPositiveStatusPrepared = OriginalPositiveStatusScenario.Prepared,
                OriginalPositiveStatusIds = OriginalPositiveStatusScenario.CardIds,
                OriginalPositiveStatusQueries = OriginalPositiveStatusScenario.Queries,
                OriginalPositiveStatusQueriesUnchanged = OriginalPositiveStatusScenario.QueriesUnchanged,
                OriginalPositiveStatusEffects = OriginalPositiveStatusScenario.Effects,
                OriginalScaledMoonPhasePrepared = OriginalScaledMoonPhaseScenario.Prepared,
                OriginalScaledMoonPhaseIds = OriginalScaledMoonPhaseScenario.CardIds,
                OriginalScaledMoonPhaseCounts = OriginalScaledMoonPhaseScenario.Counts,
                OriginalScaledMoonPhaseQueries = OriginalScaledMoonPhaseScenario.Queries,
                OriginalScaledMoonPhaseStatusSamples = OriginalScaledMoonPhaseScenario.StatusSamples,
                OriginalScaledMoonPhasePhases = OriginalScaledMoonPhaseScenario.Phases,
                OriginalDrawUpgradeScalingIds = OriginalDrawUpgradeScalingScenario.CardIds,
                OriginalDrawUpgradeScalingSamples = OriginalDrawUpgradeScalingScenario.Samples,
                OriginalDrawUpgradeScalingQueries = OriginalDrawUpgradeScalingScenario.Queries,
                OriginalPileStatusIds = OriginalPileStatusScenario.CardIds,
                OriginalPileStatusQueries = OriginalPileStatusScenario.Queries,
                OriginalPileStatusQueriesUnchanged = OriginalPileStatusScenario.QueriesUnchanged,
                OriginalPileStatusSamples = OriginalPileStatusScenario.Samples,
                OriginalScaledBuffIds = OriginalScaledBuffScenario.CardIds,
                OriginalScaledBuffSamples = OriginalScaledBuffScenario.Samples,
                OriginalScaledBuffQueries = OriginalScaledBuffScenario.Queries,
                OriginalRoomStatusDamagePrepared = OriginalRoomStatusDamageScenario.Prepared,
                OriginalRoomStatusDamageIds = OriginalRoomStatusDamageScenario.CardIds,
                OriginalCorruptionDamagePrepared = OriginalCorruptionDamageScenario.Prepared,
                OriginalCorruptionDamageIds = OriginalCorruptionDamageScenario.CardIds,
                OriginalXStatusCardsPrepared = OriginalXStatusCardsScenario.Prepared,
                OriginalXStatusCardsIds = OriginalXStatusCardsScenario.CardIds,
                OriginalXBanishedSpikePrepared = OriginalXBanishedSpikeScenario.Prepared,
                OriginalXBanishedSpikeIds = OriginalXBanishedSpikeScenario.CardIds,
                OriginalXLunaSpikePrepared = OriginalXLunaSpikeScenario.Prepared,
                OriginalXLunaSpikeIds = OriginalXLunaSpikeScenario.CardIds,
                OriginalXStygianSpikePrepared = OriginalXStygianSpikeScenario.Prepared,
                OriginalXStygianSpikeIds = OriginalXStygianSpikeScenario.CardIds,
                OriginalXWurmkinSpikePrepared = OriginalXWurmkinSpikeScenario.Prepared,
                OriginalXWurmkinSpikeIds = OriginalXWurmkinSpikeScenario.CardIds,
                OriginalMineCollapsePrepared = OriginalMineCollapseScenario.Prepared,
                OriginalMineCollapseIds = OriginalMineCollapseScenario.CardIds,
                OriginalImpOlatePrepared = OriginalImpOlateScenario.Prepared,
                OriginalImpOlateIds = OriginalImpOlateScenario.CardIds,
                OriginalImpOlateImpWitnessId = OriginalImpOlateScenario.ImpWitnessId,
                OriginalImpPressivePrepared = OriginalImpPressiveScenario.Prepared,
                OriginalImpPressiveIds = OriginalImpPressiveScenario.CardIds,
                OriginalTomeOfEvergrowthPrepared = OriginalTomeOfEvergrowthScenario.Prepared,
                OriginalTomeOfEvergrowthIds = OriginalTomeOfEvergrowthScenario.CardIds,
                OriginalTomeOfTheLostPathPrepared = OriginalTomeOfTheLostPathScenario.Prepared,
                OriginalTomeOfTheLostPathIds = OriginalTomeOfTheLostPathScenario.CardIds,
                OriginalMakeItRainPrepared = OriginalMakeItRainScenario.Prepared,
                OriginalMakeItRainIds = OriginalMakeItRainScenario.CardIds,
                OriginalDeathroarPrepared = OriginalDeathroarScenario.Prepared,
                OriginalDeathroarIds = OriginalDeathroarScenario.CardIds,
                OriginalSecretIngredientPrepared = MixScenario.SecretIngredientMode && MixScenario.Prepared,
                OriginalGreenRecruitPrepared = OriginalGreenRecruitScenario.Prepared,
                OriginalPrimordiumPrepared = OriginalPrimordiumScenario.Prepared,
                OriginalCheeryDeathcapPrepared = OriginalCheeryDeathcapScenario.Prepared,
                OriginalEnthusiasticFanPrepared = OriginalEnthusiasticFanScenario.Prepared,
                OriginalPyreshotPrepared = OriginalPyreshotScenario.Prepared,
                OriginalPyreshotIds = OriginalPyreshotScenario.CardIds,
                OriginalRetchPrepared = OriginalRetchScenario.Prepared,
                OriginalRetchIds = OriginalRetchScenario.CardIds,
                OriginalRetchEatenWitnessId = OriginalRetchScenario.EatenWitnessId,
                OriginalXDamageCardsPrepared = OriginalXDamageCardsScenario.Prepared,
                OriginalXDamageCardsIds = OriginalXDamageCardsScenario.CardIds,
                OriginalStrongMagicCardsPrepared = OriginalStrongMagicCardsScenario.Prepared,
                OriginalStrongMagicCardsIds = OriginalStrongMagicCardsScenario.CardIds,
                OriginalConsumeCardsPrepared = OriginalConsumeCardsScenario.Prepared,
                OriginalConsumeCardsIds = OriginalConsumeCardsScenario.CardIds,
                InfernoRoomPrepared = InfernoRoomScenario.Prepared,
                InfernoRoomCardIds = InfernoRoomScenario.CardIds,
                RoomPreCombatRecords = RoomPreCombatProbe.Records,
                RoomPostCombatRecords = RoomPostCombatProbe.Records,
                RunCardGenerationPrepared = RunCardGenerationScenario.Prepared,
                ArmamentDrivePrepared = ArmamentDriveScenario.Prepared,
                ArmamentDriveCardIds = ArmamentDriveScenario.CardIds,
                PreservePrepared = PreserveScenario.Prepared,
                CardDuplicatePrepared = CardDuplicateScenario.Prepared,
                CardDuplicateOriginalDataId = CardDuplicateScenario.OriginalId,
                CardDuplicateCardIds = CardDuplicateScenario.CardIds,
                CardDuplicateRecipientCardId = CardDuplicateScenario.RecipientCardId,
                CardDuplicateTests = CardDuplicateScenario.Tests,
                CardDuplicateRecords = CardDuplicateScenario.Records,
                PreserveCardIds = PreserveScenario.CardIds,
                PreserveJunkCardId = PreserveScenario.JunkCardId,
                PreserveSpellCardId = PreserveScenario.SpellCardId,
                PreserveFilters = PreserveScenario.Filters,
                PreserveFilterContext = PreserveScenario.FilterContext,
                PreserveRecords = PreserveScenario.Records,
                ForcedFeeding = ForcedFeedingScenario.Enabled,
                ShroudSpikeFeeding = ForcedFeedingScenario.ScalingMode,
                ForcedFeedingDeploymentRejected = ForcedFeedingScenario.DeploymentRejected,
                ForcedFeedingRecords = ForcedFeedingScenario.Records,
                ForcedFeedingCaptureFailures = ForcedFeedingScenario.Records.Count(row => row.Error != null),
                ForcedFeedingMismatches = ForcedFeedingScenario.Records.Count(row => row.Difference != null),
                ForcedFeedingUnsupported = ForcedFeedingScenario.Records.Count(row => row.UnsupportedReason != null),
                ForcedFeedingPending = ForcedFeedingScenario.Records.Count(row => !row.Completed || row.Actual == null),
                MoonHealCallbacks = OriginalMoonPhaseHealRelicScenario.OnHealMode,
                OriginalOpeningDeckSpawnPrepared = OriginalOpeningDeckSpawnScenario.Prepared,
                RelicOpeningDeckSpawnTests = OriginalOpeningDeckSpawnScenario.Tests,
                RelicOpeningDeckSpawnApplications = OriginalOpeningDeckSpawnScenario.Applications,
                OriginalOpeningDeckSpawnCaptureFailures = OriginalOpeningDeckSpawnScenario.Tests.Count(row => row.Error != null) + OriginalOpeningDeckSpawnScenario.Applications.Count(row => row.Error != null),
                OriginalOpeningDeckSpawnMismatches = OriginalOpeningDeckSpawnScenario.Tests.Count(row => row.Difference != null) + OriginalOpeningDeckSpawnScenario.Applications.Count(row => row.Difference != null),
                OriginalOpeningDeckSpawnUnsupported = OriginalOpeningDeckSpawnScenario.Tests.Count(row => row.UnsupportedReason != null) + OriginalOpeningDeckSpawnScenario.Applications.Count(row => row.UnsupportedReason != null),
                OriginalOpeningDeckSpawnPending = OriginalOpeningDeckSpawnScenario.Tests.Count(row => row.Actual == null) + OriginalOpeningDeckSpawnScenario.Applications.Count(row => !row.Completed || row.Actual == null),
                RelicOpeningMorselTests = OriginalOpeningMorselsScenario.Tests,
                RelicOpeningMorselApplications = OriginalOpeningMorselsScenario.Applications,
                NativeMorselFeeding = OriginalOpeningMorselsScenario.Foods,
                OriginalOpeningMorselsCaptureFailures = OriginalOpeningMorselsScenario.Tests.Count(row => row.Error != null) + OriginalOpeningMorselsScenario.Applications.Count(row => row.Error != null) + OriginalOpeningMorselsScenario.Foods.Count(row => row.Error != null),
                OriginalOpeningMorselsMismatches = OriginalOpeningMorselsScenario.Tests.Count(row => row.Difference != null) + OriginalOpeningMorselsScenario.Applications.Count(row => row.Difference != null) + OriginalOpeningMorselsScenario.Foods.Count(row => row.Difference != null),
                OriginalOpeningMorselsUnsupported = OriginalOpeningMorselsScenario.Applications.Count(row => row.UnsupportedReason != null) + OriginalOpeningMorselsScenario.Foods.Count(row => row.UnsupportedReason != null),
                OriginalOpeningMorselsPending = OriginalOpeningMorselsScenario.Tests.Count(row => row.Actual == null) + OriginalOpeningMorselsScenario.Applications.Count(row => !row.Completed || row.Actual == null) + OriginalOpeningMorselsScenario.Foods.Count(row => !row.Completed || row.Actual == null),
                OriginalDecayDeathPrepared = OriginalDecayDeathScenario.Prepared,
                RelicDecayDeathTests = OriginalDecayDeathScenario.Tests,
                RelicDecayDeathApplications = OriginalDecayDeathScenario.Applications,
                OriginalDecayDeathCaptureFailures = OriginalDecayDeathScenario.Tests.Count(row => row.Error != null) + OriginalDecayDeathScenario.Applications.Count(row => row.Error != null),
                OriginalDecayDeathMismatches = OriginalDecayDeathScenario.Tests.Count(row => row.Difference != null) + OriginalDecayDeathScenario.Applications.Count(row => row.Difference != null),
                OriginalDecayDeathUnsupported = OriginalDecayDeathScenario.Applications.Count(row => row.UnsupportedReason != null),
                OriginalDecayDeathPending = OriginalDecayDeathScenario.Tests.Count(row => row.Actual == null) + OriginalDecayDeathScenario.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalBoneDogPrepared = OriginalBoneDogScenario.Prepared,
                RelicBoneDogRecords = OriginalBoneDogScenario.Records,
                PyreDamageRecords = OriginalBoneDogScenario.DamageRecords,
                PyreResurrectionPreviews = OriginalBoneDogScenario.PreviewRecords,
                OriginalBoneDogCaptureFailures = OriginalBoneDogScenario.Records.Count(row => row.Error != null),
                OriginalBoneDogMismatches = OriginalBoneDogScenario.Records.Count(row => row.Difference != null),
                OriginalBoneDogUnsupported = OriginalBoneDogScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalBoneDogPending = OriginalBoneDogScenario.Records.Count(row => !row.Completed || row.Actual == null) + OriginalBoneDogScenario.DamageRecords.Count(row => !row.Completed) + OriginalBoneDogScenario.PreviewRecords.Count(row => !row.Completed),
                OriginalGrosMichelPrepared = OriginalGrosMichelScenario.Prepared,
                OriginalGrosMichelSeed = OriginalGrosMichelScenario.Seed,
                RelicGrosMichelRecords = OriginalGrosMichelScenario.Records,
                OriginalGrosMichelCaptureFailures = OriginalGrosMichelScenario.Records.Count(row => row.Error != null),
                OriginalGrosMichelMismatches = OriginalGrosMichelScenario.Records.Count(row => row.Difference != null),
                OriginalGrosMichelUnsupported = OriginalGrosMichelScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalGrosMichelPending = OriginalGrosMichelScenario.Records.Count(row => !row.Completed || row.Actual == null),
                RelicSpawnRetreatRecords = OriginalSpawnRetreatScenario.Records,
                OriginalSpawnRetreatCaptureFailures = OriginalSpawnRetreatScenario.Records.Count(row => row.Error != null),
                OriginalSpawnRetreatMismatches = OriginalSpawnRetreatScenario.Records.Count(row => row.Difference != null),
                OriginalSpawnRetreatUnsupported = OriginalSpawnRetreatScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalSpawnRetreatPending = OriginalSpawnRetreatScenario.Records.Count(row => !row.Completed || row.Actual == null),
                OriginalWhelpCooldown = OriginalWhelpCooldownScenario.Prepared ? OriginalWhelpCooldownScenario.Evidence : null,
                RelicWhelpCooldownRecords = OriginalWhelpCooldownScenario.Records,
                OriginalWhelpCooldownCaptureFailures = OriginalWhelpCooldownScenario.Records.Count(row => row.Error != null),
                OriginalWhelpCooldownMismatches = OriginalWhelpCooldownScenario.Records.Count(row => row.Difference != null),
                OriginalWhelpCooldownUnsupported = OriginalWhelpCooldownScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalWhelpCooldownPending = OriginalWhelpCooldownScenario.Records.Count(row => !row.Completed || row.Actual == null),
                OriginalCardTypeSequence = OriginalCardTypeSequenceScenario.Prepared ? OriginalCardTypeSequenceScenario.Evidence : null,
                RelicCardTypeSequenceRecords = OriginalCardTypeSequenceScenario.Records,
                OriginalCardTypeSequenceCaptureFailures = OriginalCardTypeSequenceScenario.Records.Count(row => row.Error != null),
                OriginalCardTypeSequenceMismatches = OriginalCardTypeSequenceScenario.Records.Count(row => row.Difference != null),
                OriginalCardTypeSequenceUnsupported = OriginalCardTypeSequenceScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalCardTypeSequencePending = OriginalCardTypeSequenceScenario.Records.Count(row => !row.Completed || row.Actual == null),
                OriginalWolosBloodletter = OriginalWolosBloodletterScenario.Prepared ? OriginalWolosBloodletterScenario.Evidence : null,
                RelicWolosBloodletterRecords = OriginalWolosBloodletterScenario.Records,
                OriginalWolosBloodletterCaptureFailures = OriginalWolosBloodletterScenario.Records.Count(row => row.Error != null),
                OriginalWolosBloodletterMismatches = OriginalWolosBloodletterScenario.Records.Count(row => row.Difference != null),
                OriginalWolosBloodletterUnsupported = OriginalWolosBloodletterScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalWolosBloodletterPending = OriginalWolosBloodletterScenario.Records.Count(row => !row.Completed || row.Actual == null),
                OriginalExpensiveCardUpgradePrepared = OriginalExpensiveCardUpgradeScenario.Prepared,
                OriginalExpensiveCardUpgrade = OriginalExpensiveCardUpgradeScenario.Prepared ? OriginalExpensiveCardUpgradeScenario.Evidence : null,
                RelicExpensiveCardUpgradeRecords = OriginalExpensiveCardUpgradeScenario.Records,
                OriginalExpensiveCardUpgradeCaptureFailures = OriginalExpensiveCardUpgradeScenario.Records.Count(row => row.Error != null),
                OriginalExpensiveCardUpgradeMismatches = OriginalExpensiveCardUpgradeScenario.Records.Count(row => row.Difference != null),
                OriginalExpensiveCardUpgradeUnsupported = OriginalExpensiveCardUpgradeScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalExpensiveCardUpgradePending = OriginalExpensiveCardUpgradeScenario.Records.Count(row => !row.Completed || row.Actual == null),
                OriginalSuppressExhaustPrepared = OriginalSuppressExhaustScenario.Prepared,
                OriginalSuppressExhaust = OriginalSuppressExhaustScenario.Prepared ? OriginalSuppressExhaustScenario.Evidence : null,
                RelicSuppressExhaustRecords = OriginalSuppressExhaustScenario.Records,
                OriginalSuppressExhaustCaptureFailures = OriginalSuppressExhaustScenario.Records.Count(row => row.Error != null),
                OriginalSuppressExhaustMismatches = OriginalSuppressExhaustScenario.Records.Count(row => row.Difference != null),
                OriginalSuppressExhaustUnsupported = OriginalSuppressExhaustScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalSuppressExhaustPending = OriginalSuppressExhaustScenario.Records.Count(row => !row.Completed || row.Actual == null),
                OriginalRandomDrawCostPrepared = OriginalRandomDrawCostScenario.Prepared,
                OriginalRandomDrawCost = OriginalRandomDrawCostScenario.Prepared ? OriginalRandomDrawCostScenario.Evidence : null,
                RelicRandomDrawCostRecords = OriginalRandomDrawCostScenario.Records,
                OriginalRandomDrawCostCaptureFailures = OriginalRandomDrawCostScenario.Records.Count(row => row.Error != null),
                OriginalRandomDrawCostMismatches = OriginalRandomDrawCostScenario.Records.Count(row => row.Difference != null),
                OriginalRandomDrawCostUnsupported = OriginalRandomDrawCostScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalRandomDrawCostPending = OriginalRandomDrawCostScenario.Records.Count(row => !row.Completed || row.Actual == null),
                OriginalMooncycleFaePrepared = OriginalMooncycleFaeScenario.Prepared,
                OriginalMooncycleFae = OriginalMooncycleFaeScenario.Prepared ? OriginalMooncycleFaeScenario.Evidence : null,
                RelicMooncycleFaeTests = OriginalMooncycleFaeScenario.Tests,
                RelicMooncycleFaeApplications = OriginalMooncycleFaeScenario.Applications,
                OriginalMooncycleFaeCaptureFailures = OriginalMooncycleFaeScenario.Tests.Count(row => row.Error != null) + OriginalMooncycleFaeScenario.Applications.Count(row => row.Error != null),
                OriginalMooncycleFaeMismatches = OriginalMooncycleFaeScenario.Tests.Count(row => row.Difference != null) + OriginalMooncycleFaeScenario.Applications.Count(row => row.Difference != null),
                OriginalMooncycleFaeUnsupported = OriginalMooncycleFaeScenario.Applications.Count(row => row.UnsupportedReason != null),
                OriginalMooncycleFaePending = OriginalMooncycleFaeScenario.Tests.Count(row => row.Actual == null) + OriginalMooncycleFaeScenario.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalStartTurnPropagationPrepared = OriginalStartTurnPropagationScenario.Prepared,
                OriginalStartTurnPropagation = OriginalStartTurnPropagationScenario.Prepared ? OriginalStartTurnPropagationScenario.Evidence : null,
                RelicStartTurnPropagationTests = OriginalStartTurnPropagationScenario.Tests,
                RelicStartTurnPropagationApplications = OriginalStartTurnPropagationScenario.Applications,
                OriginalStartTurnPropagationCaptureFailures = OriginalStartTurnPropagationScenario.Tests.Count(row => row.Error != null) + OriginalStartTurnPropagationScenario.Applications.Count(row => row.Error != null),
                OriginalStartTurnPropagationMismatches = OriginalStartTurnPropagationScenario.Tests.Count(row => row.Difference != null) + OriginalStartTurnPropagationScenario.Applications.Count(row => row.Difference != null),
                OriginalStartTurnPropagationUnsupported = OriginalStartTurnPropagationScenario.Applications.Count(row => row.UnsupportedReason != null),
                OriginalStartTurnPropagationPending = OriginalStartTurnPropagationScenario.Tests.Count(row => row.Actual == null) + OriginalStartTurnPropagationScenario.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalRandomHandUpgradePrepared = OriginalRandomHandUpgradeScenario.CombatPrepared,
                OriginalRandomHandUpgrade = OriginalRandomHandUpgradeScenario.Prepared ? OriginalRandomHandUpgradeScenario.Evidence : null,
                RelicRandomHandUpgradeRecords = OriginalRandomHandUpgradeScenario.Records,
                OriginalRandomHandUpgradeCaptureFailures = OriginalRandomHandUpgradeScenario.Records.Count(row => row.Error != null),
                OriginalRandomHandUpgradeMismatches = OriginalRandomHandUpgradeScenario.Records.Count(row => row.Difference != null),
                OriginalRandomHandUpgradeUnsupported = OriginalRandomHandUpgradeScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalRandomHandUpgradePending = OriginalRandomHandUpgradeScenario.Records.Count(row => !row.Completed || row.Actual == null),
                OriginalRandomFreezePrepared = OriginalRandomFreezeScenario.Prepared,
                OriginalRandomFreeze = OriginalRandomFreezeScenario.Prepared ? OriginalRandomFreezeScenario.Evidence : null,
                RelicRandomFreezeTests = OriginalRandomFreezeScenario.Tests,
                RelicRandomFreezeApplications = OriginalRandomFreezeScenario.Applications,
                OriginalRandomFreezeCaptureFailures = OriginalRandomFreezeScenario.Tests.Count(row => row.Error != null) +
                    OriginalRandomFreezeScenario.Applications.Count(row => row.Error != null),
                OriginalRandomFreezeMismatches = OriginalRandomFreezeScenario.Tests.Count(row => row.Difference != null) +
                    OriginalRandomFreezeScenario.Applications.Count(row => row.Difference != null),
                OriginalRandomFreezeUnsupported = OriginalRandomFreezeScenario.Applications.Count(row => row.UnsupportedReason != null),
                OriginalRandomFreezePending = OriginalRandomFreezeScenario.Tests.Count(row => row.Actual == null) +
                    OriginalRandomFreezeScenario.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalFreezeCostPrepared = OriginalFreezeReduceCostScenario.Prepared,
                OriginalFreezeCost = OriginalFreezeReduceCostScenario.Prepared ? OriginalFreezeReduceCostScenario.Evidence : null,
                RelicFreezeCostTests = OriginalFreezeReduceCostScenario.Tests,
                RelicFreezeCostApplications = OriginalFreezeReduceCostScenario.Applications,
                OriginalFreezeCostCaptureFailures = OriginalFreezeReduceCostScenario.Tests.Count(row => row.Error != null) +
                    OriginalFreezeReduceCostScenario.Applications.Count(row => row.Error != null),
                OriginalFreezeCostMismatches = OriginalFreezeReduceCostScenario.Tests.Count(row => row.Difference != null) +
                    OriginalFreezeReduceCostScenario.Applications.Count(row => row.Difference != null),
                OriginalFreezeCostUnsupported = OriginalFreezeReduceCostScenario.Applications.Count(row => row.UnsupportedReason != null),
                OriginalFreezeCostPending = OriginalFreezeReduceCostScenario.Tests.Count(row => row.Actual == null) +
                    OriginalFreezeReduceCostScenario.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalSpawnSilencePrepared = OriginalSpawnSilenceScenario.Prepared,
                OriginalSpawnSilence = OriginalSpawnSilenceScenario.Prepared ? OriginalSpawnSilenceScenario.Evidence : null,
                RelicSpawnSilenceTests = OriginalSpawnSilenceScenario.Tests,
                RelicSpawnSilenceApplications = OriginalSpawnSilenceScenario.Applications,
                OriginalSpawnSilenceCaptureFailures = OriginalSpawnSilenceScenario.Tests.Count(row => row.Error != null) +
                    OriginalSpawnSilenceScenario.Applications.Count(row => row.Error != null),
                OriginalSpawnSilenceMismatches = OriginalSpawnSilenceScenario.Tests.Count(row => row.Difference != null) +
                    OriginalSpawnSilenceScenario.Applications.Count(row => row.Difference != null),
                OriginalSpawnSilenceUnsupported = OriginalSpawnSilenceScenario.Applications.Count(row => row.UnsupportedReason != null),
                OriginalSpawnSilencePending = OriginalSpawnSilenceScenario.Tests.Count(row => row.Actual == null) +
                    OriginalSpawnSilenceScenario.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalKillAttackPrepared = OriginalKillAttackScenario.Prepared,
                OriginalKillAttack = OriginalKillAttackScenario.Prepared ? OriginalKillAttackScenario.Evidence : null,
                RelicKillAttackTests = OriginalKillAttackScenario.Tests,
                RelicKillAttackApplications = OriginalKillAttackScenario.Applications,
                OriginalKillAttackCaptureFailures = OriginalKillAttackScenario.Tests.Count(row => row.Error != null) +
                    OriginalKillAttackScenario.Applications.Count(row => row.Error != null),
                OriginalKillAttackMismatches = OriginalKillAttackScenario.Tests.Count(row => row.Difference != null) +
                    OriginalKillAttackScenario.Applications.Count(row => row.Difference != null),
                OriginalKillAttackUnsupported = OriginalKillAttackScenario.Applications.Count(row => row.UnsupportedReason != null),
                OriginalKillAttackPending = OriginalKillAttackScenario.Tests.Count(row => row.Actual == null) +
                    OriginalKillAttackScenario.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalHealDamagePrepared = OriginalHealDamageScenario.Prepared && OriginalHealDamageScenario.CombatPrepared,
                OriginalHealDamage = OriginalHealDamageScenario.Prepared ? OriginalHealDamageScenario.Evidence : null,
                RelicHealDamageTests = RelicHealDamageProbe.Tests,
                RelicHealDamageApplications = RelicHealDamageProbe.Applications,
                OriginalHealDamageCaptureFailures = RelicHealDamageProbe.Tests.Count(row => row.Error != null) +
                    RelicHealDamageProbe.Applications.Count(row => row.Error != null),
                OriginalHealDamageMismatches = RelicHealDamageProbe.Tests.Count(row => row.Difference != null) +
                    RelicHealDamageProbe.Applications.Count(row => row.Difference != null),
                OriginalHealDamageUnsupported = RelicHealDamageProbe.Tests.Count(row => row.UnsupportedReason != null) +
                    RelicHealDamageProbe.Applications.Count(row => row.UnsupportedReason != null),
                OriginalHealDamagePending = RelicHealDamageProbe.Tests.Count(row => row.Actual == null) +
                    RelicHealDamageProbe.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalBurnoutDamagePrepared = OriginalBurnoutDamageScenario.Prepared && OriginalBurnoutDamageScenario.CombatPrepared,
                OriginalBurnoutDamage = OriginalBurnoutDamageScenario.Prepared ? OriginalBurnoutDamageScenario.Evidence : null,
                RelicBurnoutDamageTests = RelicEatenDamageProbe.Tests,
                RelicBurnoutDamageApplications = RelicEatenDamageProbe.Applications,
                OriginalBurnoutDamageCaptureFailures = RelicEatenDamageProbe.Tests.Count(row => row.Error != null) +
                    RelicEatenDamageProbe.Applications.Count(row => row.Error != null),
                OriginalBurnoutDamageMismatches = RelicEatenDamageProbe.Tests.Count(row => row.Difference != null) +
                    RelicEatenDamageProbe.Applications.Count(row => row.Difference != null),
                OriginalBurnoutDamageUnsupported = RelicEatenDamageProbe.Tests.Count(row => row.UnsupportedReason != null) +
                    RelicEatenDamageProbe.Applications.Count(row => row.UnsupportedReason != null),
                OriginalBurnoutDamagePending = RelicEatenDamageProbe.Tests.Count(row => row.Actual == null) +
                    RelicEatenDamageProbe.Applications.Count(row => !row.Completed || row.Actual == null),
                OriginalMoreCapacityPrepared = OriginalMoreCapacityScenario.Prepared,
                OriginalMoreCapacity = OriginalMoreCapacityScenario.Prepared ? OriginalMoreCapacityScenario.Evidence : null,
                OriginalMoreCapacityRecords = OriginalMoreCapacityScenario.Records,
                OriginalMoreCapacityCaptureFailures = OriginalMoreCapacityScenario.Records.Count(row => row.Error != null),
                OriginalMoreCapacityMismatches = OriginalMoreCapacityScenario.Records.Count(row => row.Difference != null),
                OriginalMoreCapacityUnsupported = OriginalMoreCapacityScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalMoreCapacityPending = OriginalMoreCapacityScenario.Records.Count(row => !row.Completed || row.After == null),
                OriginalEatenDamagePrepared = OriginalEatenDamageScenario.Prepared && OriginalEatenDamageScenario.CombatPrepared,
                OriginalEatenDamage = OriginalEatenDamageScenario.Prepared ? OriginalEatenDamageScenario.Evidence : null,
                RelicEatenDamageTests = RelicEatenDamageProbe.Tests,
                RelicEatenDamageApplications = RelicEatenDamageProbe.Applications,
                OriginalEatenDamageCaptureFailures = RelicEatenDamageProbe.Tests.Count(record => record.Error != null) +
                    RelicEatenDamageProbe.Applications.Count(record => record.Error != null),
                OriginalEatenDamageMismatches = RelicEatenDamageProbe.Tests.Count(record => record.Difference != null) +
                    RelicEatenDamageProbe.Applications.Count(record => record.Difference != null),
                OriginalEatenDamageUnsupported = RelicEatenDamageProbe.Tests.Count(record => record.UnsupportedReason != null) +
                    RelicEatenDamageProbe.Applications.Count(record => record.UnsupportedReason != null),
                OriginalEatenDamagePending = RelicEatenDamageProbe.Tests.Count(record => record.Actual == null) +
                    RelicEatenDamageProbe.Applications.Count(record => !record.Completed || record.Actual == null),
                OriginalConsumeDamagePrepared = OriginalConsumeDamageScenario.Prepared,
                OriginalConsumeDamage = OriginalConsumeDamageScenario.Prepared ? OriginalConsumeDamageScenario.Evidence : null,
                RelicConsumeDamageTests = RelicConsumeDamageProbe.Tests,
                RelicConsumeDamageApplications = RelicConsumeDamageProbe.Applications,
                OriginalConsumeDamageCaptureFailures = RelicConsumeDamageProbe.Tests.Count(record => record.Error != null) +
                    RelicConsumeDamageProbe.Applications.Count(record => record.Error != null),
                OriginalConsumeDamageMismatches = RelicConsumeDamageProbe.Tests.Count(record => record.Difference != null) +
                    RelicConsumeDamageProbe.Applications.Count(record => record.Difference != null),
                OriginalConsumeDamageUnsupported = RelicConsumeDamageProbe.Tests.Count(record => record.UnsupportedReason != null) +
                    RelicConsumeDamageProbe.Applications.Count(record => record.UnsupportedReason != null),
                OriginalConsumeDamagePending = RelicConsumeDamageProbe.Tests.Count(record => record.Actual == null) +
                    RelicConsumeDamageProbe.Applications.Count(record => !record.Completed || record.Actual == null),
                OriginalCardPlayDamagePrepared = OriginalCardPlayDamageScenario.Prepared,
                OriginalCardPlayDamage = OriginalCardPlayDamageScenario.Prepared ? OriginalCardPlayDamageScenario.Evidence : null,
                RelicCardPlayDamageTests = RelicCardPlayDamageProbe.Tests,
                RelicCardPlayDamageApplications = RelicCardPlayDamageProbe.Applications,
                OriginalCardPlayDamageCaptureFailures = RelicCardPlayDamageProbe.Tests.Count(record => record.Error != null) +
                    RelicCardPlayDamageProbe.Applications.Count(record => record.Error != null),
                OriginalCardPlayDamageMismatches = RelicCardPlayDamageProbe.Tests.Count(record => record.Difference != null) +
                    RelicCardPlayDamageProbe.Applications.Count(record => record.Difference != null),
                OriginalCardPlayDamageUnsupported = RelicCardPlayDamageProbe.Tests.Count(record => record.UnsupportedReason != null) +
                    RelicCardPlayDamageProbe.Applications.Count(record => record.UnsupportedReason != null),
                OriginalCardPlayDamagePending = RelicCardPlayDamageProbe.Tests.Count(record => record.Actual == null) +
                    RelicCardPlayDamageProbe.Applications.Count(record => !record.Completed || record.Actual == null),
                OriginalDeckTurnDrawPrepared = OriginalDeckTurnDrawScenario.Prepared,
                OriginalDeckTurnDraw = OriginalDeckTurnDrawScenario.Prepared ? OriginalDeckTurnDrawScenario.Evidence : null,
                RelicDeckTurnDrawTests = RelicDeckTurnDrawProbe.Tests,
                RelicDeckTurnDrawApplications = RelicDeckTurnDrawProbe.Applications,
                OriginalDeckTurnDrawCaptureFailures = RelicDeckTurnDrawProbe.Applications.Count(record => record.CaptureError != null),
                OriginalDeckTurnDrawMismatches = RelicDeckTurnDrawProbe.Applications.Count(record => record.Difference != null),
                OriginalDeckTurnDrawUnsupported = RelicDeckTurnDrawProbe.Applications.Count(record => record.UnsupportedReason != null),
                OriginalDeckTurnDrawPending = RelicDeckTurnDrawProbe.Applications.Count(record => !record.Completed || record.Actual == null) +
                    RelicDeckTurnDrawProbe.Tests.Count(record => record.After == null),
                OriginalCooldownOnDamagePrepared = OriginalCooldownOnDamageScenario.Prepared,
                OriginalCooldownOnDamage = OriginalCooldownOnDamageScenario.Prepared ? OriginalCooldownOnDamageScenario.Evidence : null,
                RelicCooldownOnDamageTests = OriginalCooldownOnDamageProbe.Tests,
                RelicCooldownOnDamageApplications = OriginalCooldownOnDamageProbe.Applications,
                OriginalCooldownOnDamageCaptureFailures = OriginalCooldownOnDamageProbe.Applications.Count(record => record.CaptureError != null),
                OriginalCooldownOnDamageMismatches = OriginalCooldownOnDamageProbe.Applications.Count(record => record.Difference != null),
                OriginalCooldownOnDamageUnsupported = OriginalCooldownOnDamageProbe.Applications.Count(record => record.UnsupportedReason != null),
                OriginalCooldownOnDamagePending = OriginalCooldownOnDamageProbe.Applications.Count(record => !record.Completed || record.Actual == null),
                OriginalPropagateOnStatusEffectAppliedSchema = OriginalPropagateOnStatusEffectAppliedScenario.Prepared ? 215 : 0,
                OriginalPropagateOnStatusEffectApplied = OriginalPropagateOnStatusEffectAppliedScenario.Prepared ? OriginalPropagateOnStatusEffectAppliedScenario.Evidence : null,
                OriginalNoUpgradeCardCostSchema = OriginalNoUpgradeCardCostScenario.Prepared ? 212 : 0,
                OriginalNoUpgradeCardCost = OriginalNoUpgradeCardCostScenario.Prepared ? OriginalNoUpgradeCardCostScenario.Evidence : null,
                RelicJudgesScalesTests = RelicJudgesScalesProbe.Tests,
                RelicJudgesScalesApplications = RelicJudgesScalesProbe.Applications,
                OriginalPyreCardCostSchema = OriginalPyreCardCostScenario.Prepared ? 209 : 0,
                OriginalPyreCardCost = OriginalPyreCardCostScenario.Prepared ? OriginalPyreCardCostScenario.Evidence : null,
                RelicStartTurnHealResets = RelicStartTurnHealProbe.Resets,
                RelicStartTurnHealTests = RelicStartTurnHealProbe.Tests,
                RelicStartTurnHealOperations = RelicStartTurnHealProbe.Records,
                RelicStartTurnHealCaptureFailures = CaptureFailures,
                RelicStartTurnHealMismatches = RelicStartTurnHealProbe.Records.Count(record => record.Difference != null),
                RelicStartTurnHealUnsupported = RelicStartTurnHealProbe.Records.Count(record => !record.ModelSupported),
                RelicStartTurnHealPending = RelicStartTurnHealProbe.Resets.Count(record => !record.Completed) +
                    RelicStartTurnHealProbe.Records.Count(record => !record.Completed || record.Actual == null),
                EndlessReturnCardId = OriginalEndlessReturnScenario.TargetCardId,
                RequestedGameSpeed = ProbeGameSpeed.Requested?.ToString(),
                LiveGameSpeedOverrides = ProbeGameSpeed.LiveOverrides,
                CaptureFailures = CaptureFailures + RelicMoonPhaseStatusProbe.Records.Count(record => record.After == null) +
                    RelicStartBattleGenerationProbe.Records.Count(record => !record.Completed || record.Actual == null) +
                    RelicPrecachedPlayerClanGenerationProbe.Records.Count(record => !record.Completed || record.Actual == null) +
                    RelicSoloUnitUpgradeProbe.Records.Count(record => !record.Completed || record.Actual == null) +
                    RelicFrontRoomHealthProbe.Records.Count(record => !record.Completed || record.Actual == null || record.ActualRooms == null) +
                    OriginalAddSnackOnKillProbe.Records.Count(record => record.CaptureError != null) +
                    RelicChampionCardAfterDrawProbe.Records.Count(record => record.CaptureError != null) +
                    OriginalBalatroEventYorickScenario.Tests.Count(record => record.CaptureError != null) +
                    OriginalBalatroEventYorickProbe.Records.Count(record => record.CaptureError != null),
                Mismatches = Mismatches + OriginalAddSnackOnKillProbe.Records.Count(record => record.Difference != null) +
                    RelicChampionCardAfterDrawProbe.Records.Count(record => record.Difference != null) +
                    OriginalBalatroEventYorickProbe.Records.Count(record => record.Difference != null),
                Unsupported = Unsupported + OriginalAddSnackOnKillProbe.Records.Count(record => record.UnsupportedReason != null) +
                    RelicChampionCardAfterDrawProbe.Records.Count(record => record.UnsupportedReason != null) +
                    OriginalBalatroEventYorickProbe.Records.Count(record => record.UnsupportedReason != null),
                Pending = Pending + OriginalAddSnackOnKillProbe.Records.Count(record => !record.Completed || record.Actual == null) +
                    RelicMoonPhaseStatusProbe.Records.Count(record => record.After == null) +
                    RelicStartBattleGenerationProbe.Records.Count(record => !record.Completed || record.Actual == null) +
                    RelicPrecachedPlayerClanGenerationProbe.Records.Count(record => !record.Completed || record.Actual == null) +
                    RelicSoloUnitUpgradeProbe.Records.Count(record => !record.Completed || record.Actual == null) +
                    RelicFrontRoomHealthProbe.Records.Count(record => !record.Completed || record.Actual == null || record.ActualRooms == null) +
                    RelicChampionCardAfterDrawProbe.Records.Count(record => !record.Completed || record.Actual == null) +
                    OriginalBalatroEventYorickProbe.Records.Count(record => !record.Completed || record.Actual == null),
                Stages = stages,
                CardCycles = cardCycles.Records,
                TrainPhases = trainCombat.Records,
                Spawns = spawning.Records,
                Turns = turns.Records,
                Actions = actions.Records,
                PlannerRuns = PlannerReplayScenario.Records,
                RollingDecisions = RollingAdviceScenario.Records,
                BattleDistance = AllGameManagers.Instance!.GetSaveManager().GetCurrentDistance(),
#if DEVELOPMENT_PROBE
                NativeRunInput = nativeRunInput,
#endif
                Advisor = BattleAdvisorOverlay.Active?.Evidence,
#if DEVELOPMENT_PROBE
                PlayerAdvisor = PlayerAdvisorProbe.Evidence,
#endif
                RoomCapacityAttachmentCoverage = RoomCapacityAttachmentScenario.Evidence,
                OriginalCarriageJackCoverage = OriginalCarriageJackScenario.Evidence,
                OriginalFirstRoomAttachmentCoverage = OriginalFirstRoomAttachmentScenario.Evidence,
                OriginalStartRoomAttachmentCoverage = OriginalStartRoomAttachmentScenario.Evidence,
                CrossRoomTargets = CrossRoomSpellScenario.Targets,
                NumericRanges = NumericRangeScenario.Samples,
                FilteredTargets = TargetFilterScenario.Targets,
                HandRemovals = HandRemovalScenario.Records,
                RandomDiscards = RandomDiscardScenario.Records,
                TypedDrawEffects = TypedDrawProbe.Records,
                TypedDrawCards = TypedDrawProbe.Draws,
                TypedDrawClears = TypedDrawProbe.Clears,
                RelicShiftQueries = OriginalUnitRelicBatchScenario.ShiftQueries,
                RelicStatusStackQueries = RelicStatusStackProbe.Queries,
                RelicStatusMagnitudeQueries = RelicStatusMagnitudeProbe.Queries,
                RelicStatusDecayQueries = RelicStatusDecayProbe.Queries,
                RelicResourceOperations = RelicResourceProbe.Operations,
                RelicMoonPhaseHealOperations = RelicMoonPhaseHealProbe.Records,
                OriginalMoonPhaseHealScenario = OriginalMoonPhaseHealRelicScenario.Prepared ? new
                {
                    OriginalMoonPhaseHealRelicScenario.AcolyteCardId,
                    OriginalMoonPhaseHealRelicScenario.WoundCardIds,
                    OriginalMoonPhaseHealRelicScenario.FaeCardIds
                } : null,
                RelicMoonPhaseStatusOperations = RelicMoonPhaseStatusProbe.Records,
                OriginalMoonPhaseStatusScenario = OriginalMoonPhaseStatusRelicScenario.Prepared ? new
                {
                    OriginalMoonPhaseStatusRelicScenario.AcolyteCardIds,
                    OriginalMoonPhaseStatusRelicScenario.FaeCardIds
                } : null,
                RelicAfterDrawGeneration = RelicAfterDrawGenerationProbe.Records,
                RelicUnitTriggerCardGeneration = OriginalAddSnackOnKillProbe.Records,
                RelicUnitTriggerCardGenerationTests = OriginalAddSnackOnKillProbe.Tests,
                RelicPrecachedPlayerClanGeneration = RelicPrecachedPlayerClanGenerationProbe.Records,
                RelicChampionCardAfterDraw = RelicChampionCardAfterDrawProbe.Records,
                RelicCardPlayGoldOnExhaustTests = OriginalBalatroEventYorickScenario.Tests,
                RelicCardPlayGoldOnExhaust = OriginalBalatroEventYorickProbe.Records,
                OriginalPerkeoPrepared = OriginalPerkeoScenario.Prepared,
                OriginalCorruptedAngelsPrepared = OriginalCorruptedAngelsScenario.Prepared,
                CorruptedAngelsCostOrder = OriginalCorruptedAngelsScenario.CostOrder,
                OriginalCorruptedAngelsCaptureFailures = OriginalCorruptedAngelsScenario.Records.Count(row => row.Error != null),
                OriginalCorruptedAngelsMismatches = OriginalCorruptedAngelsScenario.Records.Count(row => row.Difference != null),
                OriginalCorruptedAngelsUnsupported = OriginalCorruptedAngelsScenario.Records.Count(row => row.UnsupportedReason != null),
                OriginalCorruptedAngelsPending = OriginalCorruptedAngelsScenario.Records.Count(row => !row.Completed || row.Attempts.Any(call => !call.Completed)),
                RelicCorruptedAngelsRecords = OriginalCorruptedAngelsScenario.Records,
                OriginalPerkeoCaptureFailures = OriginalPerkeoScenario.Conversions.Count(row => row.Error != null) + OriginalPerkeoScenario.Unplayed.Count(row => row.Error != null),
                OriginalPerkeoMismatches = OriginalPerkeoScenario.Conversions.Count(row => row.Difference != null) +
                    RelicStartBattleGenerationProbe.Records.Count(row => row.Difference != null) + OriginalPerkeoScenario.Unplayed.Count(row => row.Difference != null),
                OriginalPerkeoUnsupported = OriginalPerkeoScenario.Conversions.Count(row => row.UnsupportedReason != null) +
                    RelicStartBattleGenerationProbe.Records.Count(row => row.UnsupportedReason != null) + OriginalPerkeoScenario.Unplayed.Count(row => row.UnsupportedReason != null),
                OriginalPerkeoPending = OriginalPerkeoScenario.Conversions.Count(row => !row.Completed || row.Actual == null) +
                    OriginalPerkeoScenario.Unplayed.Count(row => !row.Completed || row.Actual == null),
                RelicPerkeoUnplayed = OriginalPerkeoScenario.Unplayed,
                RelicPerkeoConversions = OriginalPerkeoScenario.Conversions,
                RelicStartBattleGeneration = RelicStartBattleGenerationProbe.Records,
                RelicUnitResources = RelicUnitResourceProbe.Records,
                UncontrolledGroupGrowthCallbacks = UncontrolledGroupGrowthProbe.Records,
                RelicOpeningDraw = RelicOpeningDrawProbe.Records,
                RelicTurnDraw = RelicTurnDrawProbe.Records,
                RelicPileStartTurn = RelicPileStartTurnProbe.Records,
                RelicPhaseUpgrades = RelicPhaseUpgradeProbe.Records,
                RelicPhaseUpgradeTests = RelicPhaseUpgradeProbe.Tests,
                RelicSoloUnitUpgrades = RelicSoloUnitUpgradeProbe.Records,
                RelicSoloUnitUpgradeTests = RelicSoloUnitUpgradeProbe.Tests,
                OriginalSoloUnitUpgrade = OriginalSoloUnitUpgradeScenario.Prepared ? OriginalSoloUnitUpgradeScenario.Evidence : null,
                RelicFrontRoomHealthUpgrades = RelicFrontRoomHealthProbe.Records,
                RelicFrontRoomHealthUpgradeTests = RelicFrontRoomHealthProbe.Tests,
                OriginalFrontRoomHealthUpgrade = OriginalFrontRoomHealthUpgradeScenario.Prepared ? OriginalFrontRoomHealthUpgradeScenario.Evidence : null,
                RelicStartTurnHealCoverage = OriginalChampionStartTurnHealScenario.Prepared ? OriginalChampionStartTurnHealScenario.Evidence : null,
                OriginalNullRelic = OriginalNullRelicScenario.Evidence,
                RelicCountAttackControls = OriginalRelicCountAttackScenario.Controls,
                RelicStatusAttackSchema = OriginalMoreSpikesScenario.Acquired || OriginalStealthSlayScenario.Acquired ? 1 : 0,
                OriginalMoreSpikes = OriginalMoreSpikesScenario.Acquired ? new { OwnerId = OriginalMoreSpikesScenario.Id, Multiplier = 1, Status = "spikes", Team = "Monsters" } : null,
                OriginalStealthSlay = OriginalStealthSlayScenario.Acquired ? new { OwnerId = OriginalStealthSlayScenario.Id, Multiplier = 3, Status = "stealth", Team = "Monsters" } : null,
                EnergyConservationQueries = OriginalConserveEmberScenario.EnergyRemovals,
                ForgeCombatEndOperations = OriginalForgeConservationScenario.Records,
                OpeningCorruptionOperations = OriginalOpeningCorruptionScenario.Records,
                PyreResourceOperations = PyreResourceRefreshProbe.Operations,
                FloorCapacityOriginalDefinitions = OriginalFloorCapacityRelicProbe.OriginalDefinitions,
                FloorCapacityDeploymentCards = OriginalFloorCapacityRelicProbe.DeploymentCards,
                FloorCapacityQueries = OriginalFloorCapacityRelicProbe.Queries,
                FloorCapacityInitializations = OriginalFloorCapacityRelicProbe.Initializations,
                FloorCapacityMatrixRestorations = OriginalFloorCapacityRelicProbe.MatrixRestorations,
                CardGenerations = GenerationScenario.Records,
                DamageScaling = DamageScalingScenario.Samples,
                StatusScaling = StatusScalingScenario.Samples,
                StatusApplications = StatusScalingScenario.Applications,
                UnitUpgradeScaling = UnitUpgradeScalingScenario.Samples,
                DirectUnitUpgrades = DirectUnitUpgradeScenario.Records,
                EquipmentOperations = EquipmentProbe.Records,
                TriggerMutations = TriggerMutationScenario.Records,
                ConditionalTriggers = ConditionalTriggerProbe.Records,
                TriggerRepeatBatches = ConditionalTriggerScenario.RepeatBatches,
                RallyOperations = RallyScenario.Operations,
                DetachedCardClones = UnitBirthProbe.Clones,
                TriggeredSummons = TriggeredSummonProbe.Records,
                TriggeredSummonDamages = TriggeredSummonProbe.Damages,
                PhysicalCompactions = BattleSpawnPointProbe.Compactions,
                SpawnPointOperations = SpawnPointScenario.Records,
                PoolSummonSelections = PoolSummonProbe.Records,
                UnitBirths = UnitBirthProbe.Records,
                SpawnEnchantments = SpawnEnchantScenario.Records,
                SpawnUpgrades = SpawnUpgradeProbe.Records,
                FreshSpawners = FreshSpawnerProbe.Records,
                GlobalStandbyChecks = FreshSpawnerProbe.Checks,
                RallyPhases = RallyProbe.Phases,
                RallyTriggers = RallyProbe.Triggers,
                DyingHordeUpgradeOperations = DyingHordeUpgradeScenario.Operations,
                HordeUpgradeOperations = HordeUpgradeScenario.Operations,
                HordeStatusOperations = HordeStatusScenario.Records,
                HordeMergeOperations = HordeMergeScenario.Records,
                HordeMergeSelections = HordeMergeScenario.Selections,
                BumpOperations = BumpScenario.Records,
                UnitCloneOperations = UnitCloneScenario.Records,
                UnitCopyOperations = UnitCopyScenario.Records,
                HeroCopyOperations = HeroCopyScenario.Records,
                RevivalOperations = RevivalScenario.Operations,
                Revivals = RevivalProbe.Records,
                SummonSourceCaches = TriggeredSummonProbe.Caches,
                DecisionSpawnPoints = BattleSpawnPointProbe.Decisions,
                DecisionRoomCaches = CardModifierProbe.DecisionCaches,
                BranchCardMasksEnabled = CardBranchMaskProbe.Enabled,
                BranchCardMasks = CardBranchMaskProbe.Records,
                HarvestOperations = HarvestScenario.Operations,
                HordeDeathOperations = HordeDeathScenario.Operations,
                HordeRemovalOperations = HordeRemovalScenario.Records,
                HordeRemovalCasts = HordeRemovalScenario.Casts,
                HarvestTriggers = HarvestProbe.Records,
                CompanionBossActions = CompanionBossProbe.Records,
                RelentlessTriggerRemovals = CompanionBossProbe.Removals,
                Sentries = SentryProbe.Records,
                AbilityEffectOperations = AbilityEffectProbe.Records,
                InitialAbilitySpawns = EquipmentAbilityScenario.InitialSpawns,
                AbilityLifecycleOperations = AbilityLifecycleScenario.Records,
                AbilityCooldownEffects = AbilityCooldownProbe.Records,
                AbilityCardOperations = AbilityCardProbe.Records,
                AbilityCardAccesses = AbilityCardProbe.Accesses,
                UnitTurns = UnitTurnBeginProbe.Records,
                PreviewAttackDiagnostic = PreviewAttackDiagnostic.Records,
                TeamTurnBegins = TeamTurnBeginProbe.Records,
                PreHandDiscards = PreHandDiscardProbe.Records,
                PreCombats = PreCombatProbe.Records,
                TriggeredHeals = TriggeredHealingProbe.Records,
                TriggeredDamage = TriggeredDamageProbe.Records,
                TerminalDeaths = TerminalDeathProbe.Records,
                KillCams = TerminalDeathProbe.KillCams,
                HitKills = HitKillProbe.Records,
                DyingUpgrades = DyingUpgradeProbe.Records,
                AttackTriggers = AttackTriggerProbe.Records,
                TriggeredStatuses = TriggeredStatusProbe.Records,
                UnitSelfAttackQueries = UnitSelfAttackProbe.Records,
                UnitAttackFocus = UnitAttackFocusProbe.Records,
                UnitRoomStatusQueries = UnitRoomStatusProbe.Records,
                UnitRoomTriggerCountQueries = UnitRoomTriggerCountProbe.Records,
                TriggeredTrainHeals = TriggeredTrainHealProbe.Records,
                RoomDeathEffects = RoomDeathEffectProbe.Records,
                TriggeredTrainHealTests = TriggeredTrainHealProbe.Tests,
                StatusRegistryCalibration = StatusRegistryCalibration.Samples,
                RelicSpawnStatuses = RelicSpawnStatusProbe.Records,
                OriginalEquipmentGrowthEncounterComplete = OriginalEquipmentGrowthScenario.EncounterCompleteRecords,
                UnitUpgradeTriggerEvents = OriginalUnitUpgradeTriggerRelicScenario.TriggerObservations,
                RelicCardModifiersEnabled = RelicCardModifierProbe.Enabled,
                RelicCardModifiers = RelicCardModifierProbe.Records,
                StatusCallbacks = StatusCallbackProbe.Records,
                StatusCallbackFires = StatusCallbackProbe.Fired,
                PurifyQueueAdmissions = PurifyQueueProbe.Records,
                IncantPhases = IncantProbe.Phases,
                IncantTriggers = IncantProbe.Triggers,
                CharacterCallbackFires = StatusCallbackProbe.OtherFired,
                StatusCallbackActions = Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACK_ACTIONS") == "1",
                EnergyEffects = EnergyEffectProbe.Records,
                BonusDrawEffects = BonusDrawProbe.Records,
                CapacityEffects = RoomCapacityProbe.Records,
                CapacityTests = RoomCapacityProbe.Tests,
                PreviewRngIsolation = PreviewRngIsolation.Records,
                PreviewScheduling = PreviewSchedulingProbe.Records,
                BossPreviewEnchantments = BossPreviewEnchantmentProbe.Records,
                TrueFinalBossPreviews = TrueFinalBossPreviewProbe.Records,
                TrueFinalBossPreviewRooms = TrueFinalBossPreviewProbe.Rooms,
                TrueFinalBossPyreCardDamage = TrueFinalBossTerminalProbe.Records,
                CardAnimationSettlement = new { Enabled = CardAnimationSettlement.Enabled, CardAnimationSettlement.PreviewWaits,
                    CardAnimationSettlement.DecisionWaits, CardAnimationSettlement.ScheduledMovements,
                    CardAnimationSettlement.PendingMovements, EnchantmentCombatProbe.RetainedRemovedSourceCount },
                UnitPostCombats = PostCombatHealingProbe.Records,
                UnitUpgradeScalingCalibrationContextUnchanged = UnitUpgradeScalingScenario.CalibrationContextUnchanged,
                RemovedStatusScaling = UnitUpgradeScalingScenario.RemovalMode,
                TowerCorruptionCallbacks = CorruptionScenario.TowerMode,
                NullAbility = AbilityActivationScenario.NullMode,
                UiRngIsolation = UiRngIsolation.Records,
                Checkpoints = checkpoints
            };
            try
            {
                if (binary)
                {
                    var graphTimer = System.Diagnostics.Stopwatch.StartNew();
                    using (var document = NativeFixtureCapture.Capture(snapshot))
                    {
                        graphTimer.Stop();
                        var archiveTimer = System.Diagnostics.Stopwatch.StartNew();
                        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                            document.Write(stream);
                        log.LogInfo("BATTLE-BINARY-PHASES graphSeconds=" + graphTimer.Elapsed.TotalSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                            " archiveSeconds=" + archiveTimer.Elapsed.TotalSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + " nodes=" + document.UniqueNodeCount);
                    }
                }
                else
                {
                    using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    using (var text = new StreamWriter(stream, new UTF8Encoding(false), 65536))
                    using (var json = new JsonTextWriter(text) { Formatting = Formatting.None })
                        JsonSerializer.CreateDefault().Serialize(json, snapshot);
                }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            timer.Stop();
            log.LogInfo("BATTLE-EXPORT elapsedSeconds=" + timer.Elapsed.TotalSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                " bytes=" + new FileInfo(path).Length + " pending=" + Pending);
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAPTURE_JSON") == "1")
            {
                var jsonTimer = System.Diagnostics.Stopwatch.StartNew();
                string jsonPath = Path.ChangeExtension(path, ".json");
                using (var stream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                using (var text = new StreamWriter(stream, new UTF8Encoding(false), 65536))
                using (var json = new JsonTextWriter(text) { Formatting = Formatting.None })
                    JsonSerializer.CreateDefault().Serialize(json, snapshot);
                log.LogInfo("BATTLE-JSON-COMPARISON elapsedSeconds=" + jsonTimer.Elapsed.TotalSeconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " bytes=" + new FileInfo(jsonPath).Length);
            }
            return path;
        }

        private sealed class StageRecord
        {
            public int Index { get; set; }
            public int Turn { get; set; }
            public string Kind { get; set; } = "";
            [JsonIgnore] public RoomState Room { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public TowerTargetSnapshot[] ExternalTargets { get; set; } = Array.Empty<TowerTargetSnapshot>();
            public CombatUnit? ExternalPyre { get; set; }
            public int ExternalPyreRoomIndex { get; set; } = -1;
            public RoomCombatResult Predicted { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public CombatUnit? ActualExternalPyre { get; set; }
            public TowerTargetSnapshot[]? ActualExternalTargets { get; set; }
            public string? Difference { get; set; }
        }

        private sealed class TowerTargetSnapshot
        {
            public int RoomIndex { get; set; }
            public CombatUnit Unit { get; set; } = null!;
        }

        [HarmonyPatch(typeof(CardManager), "AddCardImpl")]
        private static class CardIdentityPatch
        {
            private static void Postfix(CardState? __result)
            {
                if (__result != null && Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    Active.CardId(__result);
            }
        }

        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.CreateMonsterState))]
        private static class MonsterIdentityPatch
        {
            private static void Prefix(ref Action<CharacterState> onCharacterStateCreated)
            {
                if (Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                Action<CharacterState> callback = onCharacterStateCreated;
                onCharacterStateCreated = unit => { Active?.UnitId(unit); callback?.Invoke(unit); };
            }
        }

        // Wave groups create enemies before arranging their physical front/back
        // order. Allocate identities at creation in both observer assemblies.
        [HarmonyPatch(typeof(HeroManager), "CreateHeroState")]
        private static class HeroIdentityPatch
        {
            private static void Prefix(ref Action<CharacterState, CharacterData> afterCharacterCreated)
            {
                if (Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode && !TriggeredSummonProbe.HasEnemyBirths) return;
                Action<CharacterState, CharacterData> callback = afterCharacterCreated;
                afterCharacterCreated = (unit, data) => { Active?.UnitId(unit); callback?.Invoke(unit, data); };
            }
        }

        [HarmonyPatch(typeof(CombatManager), "DoUnitCombat")]
        private static class ExchangePatch
        {
            private static void Postfix(RoomState room, ref IEnumerator __result)
            { if (Active != null) __result = Active.Wrap(__result, room, "Exchange"); }
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.DoRoomCombat))]
        private static class RoomPatch
        {
            private static void Postfix(RoomState room, ref IEnumerator __result)
            { if (Active != null) __result = Active.Wrap(__result, room, "Room"); }
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.StopCombat))]
        private static class StopPatch
        {
            private static void Postfix(bool combatWon, ref IEnumerator __result)
            { if (Active != null) __result = Stop(__result, combatWon); }

            private static IEnumerator Stop(IEnumerator native, bool won)
            {
                if (Active != null) Active.stoppingOutcome = won;
                while (native.MoveNext()) yield return native.Current;
            }
        }

        [HarmonyPatch(typeof(CombatManager), "StopCombatLoop")]
        private static class StopLoopPatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (Active != null) __result = Complete(__result); }

            private static IEnumerator Complete(IEnumerator native)
            {
                // Native waits for cardEffectResolving and then stops the combat loop.
                // Capture before encounter-complete rewards and out-of-battle cleanup.
                while (native.MoveNext()) yield return native.Current;
                if (Active?.stoppingOutcome is bool won) Active.Stop(won);
            }
        }
    }
}
