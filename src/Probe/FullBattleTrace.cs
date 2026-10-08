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
        private readonly List<StageRecord> stages = new List<StageRecord>();
        private readonly List<object> checkpoints = new List<object>();
        private int nextId = 1;
        private int phaseSequence;
        private bool normalizeDestroyedAttacker;
        internal T CaptureDecision<T>(Func<T> capture)
        {
            bool previous = normalizeDestroyedAttacker;
            normalizeDestroyedAttacker = true;
            try { return capture(); }
            finally { normalizeDestroyedAttacker = previous; }
        }
        internal int NextPhaseSequence() => ++phaseSequence;
        internal TrainCombatState CaptureTrain() => trainCombat.Capture();
        internal EnemySpawnState CaptureSpawnPhase() => spawning.CapturePhase();
        internal EnemySpawnState CaptureCanonicalSpawn() => spawning.Capture();
        internal TrainCombatState CaptureCanonicalTrain() => CaptureDecision(trainCombat.Capture);
        internal int[] PendingDestroyedUnitIds() => identities.Where(pair => pair.Key != null && pair.Key.IsDestroyed)
            .Select(pair => pair.Value).OrderBy(id => id).ToArray();
        internal int NextUnitId => nextId;
        internal IReadOnlyList<CardState> KnownCards => projection.KnownCards.ToArray();
        internal IReadOnlyList<CharacterState> KnownUnits => identities.Keys.ToArray();
        internal int UnitId(CharacterState unit)
        {
            if (!identities.TryGetValue(unit, out int id)) identities.Add(unit, id = nextId++);
            return id;
        }
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
        internal static FullBattleTrace? Active { get; private set; }
        internal bool? NativeWon { get; private set; }
        private bool? stoppingOutcome;
        internal bool? TerminalEffectsSettled { get; private set; }
        private int captureFailures;
        internal int CaptureFailures => TriggeredSummonProbe.Damages.Count(record => record.Error != null) + TriggeredSummonProbe.Records.Count(record => record.Error != null) + BattleSpawnPointProbe.Compactions.Count(record => record.Error != null) + SpawnPointScenario.Records.Count(record => !record.Completed) + (SpawnPointScenario.Error == null ? 0 : 1) + PoolSummonProbe.Records.Count(record => !record.Completed) + FreshSpawnerProbe.Records.Count(record => !record.Completed) + FreshSpawnerProbe.Checks.Count(record => !record.Completed) + SpawnUpgradeProbe.Records.Count(record => !record.Completed) + (MultiSummonScenario.Error == null ? 0 : 1) + UnitBirthProbe.Clones.Count(record => !record.Completed) + UnitBirthProbe.Records.Count(record => !record.Completed) + (RallyScenario.Error == null ? 0 : 1) + (DyingHordeUpgradeScenario.Error == null ? 0 : 1) + (HordeUpgradeScenario.Error == null ? 0 : 1) + (HordeDeathScenario.Error == null ? 0 : 1) + (HordeRemovalScenario.Error == null ? 0 : 1) + (HarvestScenario.Error == null ? 0 : 1) + (EquipmentScenario.Error == null ? 0 : 1) + (ConditionalTriggerScenario.Error == null ? 0 : 1) + captureFailures + (TriggerMutationScenario.Error == null ? 0 : 1) + (DirectUnitUpgradeScenario.Error == null ? 0 : 1) + DamageScalingScenario.Samples.Count(sample => sample.CaptureError != null) +
            StatusScalingScenario.Samples.Count(sample => sample.CaptureError != null) + StatusScalingScenario.Applications.Count(sample => sample.CaptureError != null) +
            UnitUpgradeScalingScenario.Samples.Count(sample => sample.CaptureError != null);
        internal int Mismatches => SpawnPointScenario.Records.Count(record => record.Difference != null) + PoolSummonProbe.Records.Count(record => record.Difference != null) + FreshSpawnerProbe.Records.Count(record => record.Difference != null) + FreshSpawnerProbe.Checks.Count(record => record.Difference != null) + SpawnUpgradeProbe.Records.Count(record => record.Difference != null) + UnitBirthProbe.Clones.Count(record => record.Difference != null) + UnitBirthProbe.Records.Count(record => record.Difference != null) + RallyProbe.Phases.Count(record => record.Difference != null) + RallyProbe.Triggers.Count(record => record.Difference != null) + HarvestProbe.Records.Count(record => record.Difference != null) + EquipmentAbilityScenario.InitialSpawns.Count(record => record.Difference != null) + AbilityLifecycleScenario.Records.Count(record => record.Difference != null) + AbilityCardProbe.Records.Count(record => record.Difference != null) + SentryProbe.Mismatches + CompanionBossProbe.Mismatches + ConditionalTriggerProbe.Records.Count(record => record.Difference != null) + TriggerMutationScenario.Records.Count(record => record.Difference != null) + EquipmentProbe.Records.Count(record => record.Difference != null) + DirectUnitUpgradeScenario.Records.Count(record => record.Difference != null) + stages.Count(stage => stage.Difference != null) + cardCycles.Mismatches + trainCombat.Mismatches + spawning.Mismatches + turns.Mismatches + actions.Mismatches +
            HandRemovalScenario.Records.Count(record => record.Difference != null) + GenerationScenario.Records.Count(record => record.Difference != null) +
            DamageScalingScenario.Samples.Count(sample => sample.Difference != null) + StatusScalingScenario.Samples.Count(sample => sample.Difference != null) +
            StatusScalingScenario.Applications.Count(sample => sample.Difference != null) + UnitUpgradeScalingScenario.Samples.Count(sample => sample.Difference != null) +
            UnitTurnBeginProbe.Records.Count(record => record.Difference != null) + TeamTurnBeginProbe.Records.Count(record => record.Difference != null) + PreHandDiscardProbe.Records.Count(record => record.Difference != null) + PreCombatProbe.Records.Count(record => record.Difference != null) + TriggeredHealingProbe.Records.Count(record => record.Difference != null) + PostCombatHealingProbe.Records.Count(record => record.Difference != null) + TriggeredDamageProbe.Records.Count(record => record.Difference != null);
        internal int Unsupported => HarvestProbe.Records.Count(record => !record.Predicted.Supported) + EquipmentAbilityScenario.InitialSpawns.Count(record => !record.Predicted.Supported) + AbilityLifecycleScenario.Records.Count(record => record.UnsupportedReason != null) + AbilityCardProbe.Records.Count(record => record.UnsupportedReason != null) + SentryProbe.Unsupported + CompanionBossProbe.Unsupported + ConditionalTriggerProbe.Records.Count(record => !record.Predicted.Supported) + TriggerMutationScenario.Records.Count(record => record.UnsupportedReason != null) + EquipmentProbe.Records.Count(record => record.UnsupportedReason != null) + DirectUnitUpgradeScenario.Records.Count(record => record.UnsupportedReason != null) + stages.Count(stage => !stage.Predicted.Supported) + cardCycles.Unsupported + trainCombat.Unsupported + spawning.Unsupported + turns.Unsupported + actions.Unsupported +
            HandRemovalScenario.Records.Count(record => !record.Predicted.Supported) + GenerationScenario.Records.Count(record => !record.Predicted.Supported) +
            UnitTurnBeginProbe.Records.Count(record => !record.Predicted.Supported) + TeamTurnBeginProbe.Records.Count(record => !record.Predicted.Supported) + PreHandDiscardProbe.Records.Count(record => !record.Predicted.Supported) + PreCombatProbe.Records.Count(record => !record.Predicted.Supported) + PostCombatHealingProbe.Records.Count(record => !record.Predicted.Supported);
        internal int Pending => TriggeredSummonProbe.Damages.Count(record => !record.Completed || record.After == null || record.TargetAfter == null) + TriggeredSummonProbe.Records.Count(record => !record.Completed || record.After == null || record.ActorAfter == null || record.RuleAfter == null) + BattleSpawnPointProbe.Compactions.Count(record => record.After == null) + SpawnPointScenario.Records.Count(record => !record.Completed || record.After == null) + (SpawnPointScenario.Started && !SpawnPointScenario.Completed ? 1 : 0) + PoolSummonProbe.Records.Count(record => !record.Completed) + FreshSpawnerProbe.Records.Count(record => !record.Completed) + FreshSpawnerProbe.Checks.Count(record => !record.Completed) + SpawnUpgradeProbe.Records.Count(record => !record.Completed) + (MultiSummonScenario.ZeroStarted && !MultiSummonScenario.ZeroCompleted ? 1 : 0) + (RallyScenario.Started && !RallyScenario.Completed ? 1 : 0) + RallyScenario.Operations.Count(record => record.After == null) + RallyProbe.Phases.Count(record => !record.Completed || record.After == null) + RallyProbe.Triggers.Count(record => !record.Completed || record.After == null || record.AfterActor == null) + (DyingHordeUpgradeScenario.Started && !DyingHordeUpgradeScenario.Completed ? 1 : 0) + DyingHordeUpgradeScenario.Operations.Count(record => record.After == null || record.AfterActor == null) + (HordeUpgradeScenario.Started && !HordeUpgradeScenario.Completed ? 1 : 0) + HordeUpgradeScenario.Operations.Count(record => record.After == null || record.AfterActor == null) + (HordeDeathScenario.Started && !HordeDeathScenario.Completed ? 1 : 0) + (HordeRemovalScenario.Started && !HordeRemovalScenario.Completed ? 1 : 0) + (HarvestScenario.Started && !HarvestScenario.Completed ? 1 : 0) + HarvestProbe.Records.Count(record => !record.Completed || record.After == null || record.AfterActor == null) + EquipmentAbilityScenario.InitialSpawns.Count(record => record.Actual == null) + (EquipmentScenario.Started && !EquipmentScenario.Completed ? 1 : 0) + StatusCallbackProbe.OtherFired.Count(record => !record.Completed || record.Actual == null || record.ActualUnit == null) + AbilityEffectProbe.Records.Count(record => !record.Completed || record.After == null) + AbilityLifecycleScenario.Records.Count(record => record.After == null) + (AbilityLifecycleScenario.Started && !AbilityLifecycleScenario.Completed ? 1 : 0) + AbilityCardProbe.Records.Count(record => record.After == null) + AbilityCooldownProbe.Records.Count(record => !record.Completed || record.After == null) + SentryProbe.Pending + CompanionBossProbe.Pending + ConditionalTriggerProbe.Records.Count(record => !record.Completed || record.After == null || record.AfterActor == null) + (ConditionalTriggerScenario.Started && !ConditionalTriggerScenario.Completed ? 1 : 0) + TriggerMutationScenario.Records.Count(record => record.After == null) + (TriggerMutationScenario.Started && !TriggerMutationScenario.Completed ? 1 : 0) + EquipmentProbe.Records.Count(record => !record.Completed || record.After == null) + DirectUnitUpgradeScenario.Records.Count(record => record.After == null) + (DirectUnitUpgradeScenario.Started && !DirectUnitUpgradeScenario.Completed ? 1 : 0) + RoomCapacityProbe.Records.Count(record => !record.Completed || record.After == null) + BonusDrawProbe.Records.Count(record => !record.Completed || !record.Sampled || record.After == null) + StatusCallbackProbe.Fired.Count(record => !record.Completed || record.Actual == null || record.ActualUnit == null) + PreviewRngIsolation.Records.Count(record => !record.Completed) + TriggeredStatusProbe.Records.Count(record => !record.Completed || record.Actual == null || record.ActualUnits == null) + AttackTriggerProbe.Records.Count(record => !record.Completed || !record.GoldAfter.HasValue || record.AfterTriggered == null) + DyingUpgradeProbe.Records.Count(record => !record.Completed || record.Actual == null || record.ActualUnits == null) + HitKillProbe.Records.Count(record => !record.Completed || !record.GoldAfter.HasValue || record.AfterTriggered == null) + stages.Count(stage => stage.Actual == null) + cardCycles.Records.Count(record => record.Actual == null) +
            trainCombat.Records.Count(record => record.Actual == null) + spawning.Records.Count(record => record.Actual == null) +
            turns.Records.Count(record => record.Actual == null) + actions.Records.Count(record => record.Actual == null) +
            HandRemovalScenario.Records.Count(record => record.Actual == null) + GenerationScenario.Records.Count(record => record.Actual == null) +
            DamageScalingScenario.Samples.Count(sample => sample.After == null) + StatusScalingScenario.Samples.Count(sample => sample.After == null) +
            StatusScalingScenario.Applications.Count(sample => sample.After == null) + UnitUpgradeScalingScenario.Samples.Count(sample => sample.After == null) +
            UnitTurnBeginProbe.Records.Count(record => record.Actual == null) + TeamTurnBeginProbe.Records.Count(record => record.Actual == null) + PreHandDiscardProbe.Records.Count(record => record.Actual == null) + PreCombatProbe.Records.Count(record => record.Actual == null) + TriggeredHealingProbe.Records.Count(record => !record.Completed || !record.Sampled || record.Requests.Any(request => !request.AfterHealth.HasValue)) + PostCombatHealingProbe.Records.Count(record => record.Actual == null) + TriggeredDamageProbe.Records.Count(record => !record.Completed || !record.Sampled || record.Requests.Any(request => !request.AfterHealth.HasValue));

        internal FullBattleTrace(ManualLogSource log)
        {
            this.log = log;
            projection = new UnitPlayModelProbe(log);
            cardCycles = new CardCycleProbe(log, projection);
            trainCombat = new TrainCombatProbe(log, this);
            spawning = new EnemySpawningProbe(log, this, trainCombat);
            turns = new BattleTurnProbe(this, spawning, cardCycles, projection, log);
            actions = new BattleActionProbe(this, projection, log);
            Active = this;
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
            if (!calibrated)
            {
                RngCalibration.Capture();
                GoldRewardCalibration.Capture(save);
                RuleCatalogProbe.Capture(managers);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_STATISTIC_QUERIES") == "1") StatisticQueryCalibration.Capture(this);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_STATISTIC_OVERFLOW") == "1") StatisticOverflowCalibration.Capture(this);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_HORDE_STATS") == "1") HordeStatCalibration.Capture(this);
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "unit-upgrade-scaling") UnitUpgradeScalingScenario.Calibrate(this);
                calibrated = true;
            }
            checkpoints.Add(new { Label = label, State = projection.Capture(managers, save, combat, cards) });
        }

        internal void BeginEndTurn() => turns.Begin();
        internal BattleTurnState CaptureDecision() => turns.Capture();
        internal BattlePlayRules CapturePlayRules(EnemySpawnState spawn) => actions.CaptureRules(spawn);
        internal int CardId(CardState card) => projection.CaptureCards(new List<CardState> { card })[0].InstanceId;
        internal int EquippedUnitId(CardState card)
        {
            CharacterState? unit = card.EquippedCharacter;
            return unit == null || normalizeDestroyedAttacker && (unit.IsDestroyed || !unit.IsAlive) ? 0 : UnitId(unit);
        }
        internal string UpgradeKey(string nativeKey)
        {
            // CardState.GetID() is the definition ID, shared by its copies.
            return nativeKey;
        }
        internal CardUpgradeModifier CaptureAppliedUpgrade(CardUpgradeState upgrade)
        {
            CardUpgradeModifier modifier = CardModifierProbe.Upgrade(upgrade);
            foreach (CardState card in projection.KnownCards.Where(item => item.GetCardType() == CardType.Equipment))
            {
                int index = card.GetCardStateModifiers().GetCardUpgrades().FindIndex(item => ReferenceEquals(item, upgrade));
                if (index >= 0) return modifier.WithEquipmentSource(CardId(card), index);
            }
            return modifier;
        }
        internal int? PendingActionIndex => actions.Records.LastOrDefault(record => record.Actual == null)?.Index;
        internal int? PendingTurnIndex => turns.Records.LastOrDefault(record => record.Actual == null)?.Index;
        internal void BeginCardPlay(PlayCardAction action) => actions.Begin(action);
        internal void CompleteCardPlay() => actions.Complete();

        internal RoomCombatState Capture(RoomState room)
        {
            AllGameManagers managers = AllGameManagers.Instance ?? throw new InvalidOperationException("No game managers.");
            var interactions = new List<string>();
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager() ?? throw new InvalidOperationException("No combat manager.");
            HeroManager heroes = managers.GetHeroManager() ?? throw new InvalidOperationException("No enemy manager.");
            if (!room.GetCombatAllowed()) interactions.Add("Disabled room combat");
            if (save.GetCollectedRelics().Count != 0) interactions.Add("Collected relic effects");
            if (save.GetMutators().Count != 0) interactions.Add("Mutator effects");
            if (room.Attachments.Count > 0) interactions.Add("Room attachments");
            if (room.GetCurrentCorruption() > 0) interactions.Add("Room corruption");
            CharacterState? outerBoss = heroes.GetOuterTrainBossCharacter();
            if (outerBoss != null && outerBoss.IsAlive && outerBoss.GetCurrentRoomIndex() == room.GetRoomIndex())
                interactions.Add("Outer train boss");
            if (heroes.HasNextBoss()) interactions.Add("Queued final bosses");
            var characters = new List<CharacterState>();
            room.AddCharactersToList(characters, Team.Type.Heroes);
            room.AddCharactersToList(characters, Team.Type.Monsters);
            var units = new List<CombatUnit>();
            foreach (CharacterState character in characters)
            {
                if (!character.IsAlive || character.IsDestroyed) continue;
                units.Add(CaptureUnit(character, interactions));
            }
            return new RoomCombatState(room.GetRoomIndex(), combat.IsPlacementPhase, units,
                interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), CaptureContext());
        }

        internal CombatUnit CaptureUnit(CharacterState character, List<string>? interactions = null)
        {
            interactions ??= new List<string>();
            HeroManager heroes = AllGameManagers.Instance!.GetHeroManager()!;
            int id = UnitId(character);
            CharacterState? lastAttacker = character.GetLastAttackerCharacter();
            CombatTrigger[] triggers = CaptureTriggers(character, interactions);
            if (character.IsPurified()) interactions.Add("Purified unit trigger restrictions");
            if (character.GetRoomStateModifiers().Count > 0)
                interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " room modifiers");
            CardState? card = character.GetSpawnerCard();
            if (card != null && (card.GetTraitStates().Any(trait => !DamageScalingProbe.Known(trait.GetType().Name)) || card.GetTriggers().Count > 0))
                interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " card traits/triggers");
            var nativeStatuses = new List<CharacterState.StatusEffectStack>();
            character.GetStatusEffects(ref nativeStatuses, includeZeroStacks: true);
            var statuses = new List<CombatStatus>();
            foreach (CharacterState.StatusEffectStack status in nativeStatuses)
            {
                StatusEffectState rule = status.State;
                if (status.Count > 0 && (rule.GetRemoveWhenTriggeredAfterCardPlayed() || rule.GetRemoveAtEndOfTurnIfTriggered()))
                    interactions.Add(rule.GetStatusId() + " delayed status removal");
                statuses.Add(new CombatStatus(rule.GetStatusId(), status.Count, rule.GetParamInt(),
                    rule.GetRemoveWhenTriggered(), rule.GetRemoveStackAtEndOfTurn(), rule.GetRemoveAtEndOfTurn(),
                    rule.GetRemoveAtEndOfTurnAfterPostCombat(), rule.PreventRemovalDuringRelentlessPhase,
                    rule.GetSkipTriggerDuringDeployment(), rule.GetRemoveDuringDeployment(),
                    BattleActionProbe.TriggeredVfx(rule.GetSourceStatusEffectData(), -1f),
                    BattleActionProbe.TriggeredVfx(rule.GetSourceStatusEffectData(), 1f), rule.IsStackable(), rule.IsHidden(), rule.GetDisplayCategory().ToString()));
            }
            CombatTeam team = character.GetTeamType() == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player;
            bool endsBattle = team == CombatTeam.Enemy &&
                (character.IsMiniboss() || character.IsOuterTrainBoss()) &&
                heroes.FindPairedCompanionBoss(character) == null;
            StatusRegistryCalibration.Capture(character, id, statuses);
            return new CombatUnit(id, character.GetSourceCharacterData()?.GetAssetKey() ?? "",
                team, character.GetAttackDamageWithoutStatusEffectBuffs(), character.GetHP(), character.GetMaxHP(),
                character.GetCanAttack(), character.IsPyreHeart(), endsBattle, statuses, triggers,
                card == null ? 0 : projection.CaptureCards(new List<CardState> { card })[0].InstanceId, character.GetSize(),
                ((List<string>)AccessTools.Field(typeof(CharacterState), "statusEffectImmunities").GetValue(character)).ToArray(),
                character.GetSubtypes().Select(subtype => subtype.Key).ToArray(), UnitModifierProbe.Capture(character), character.IsAnyBoss(),
                lastAttacker == null || normalizeDestroyedAttacker && (lastAttacker.IsDestroyed || !lastAttacker.IsAlive) ? 0 : UnitId(lastAttacker), statuses,
                character.GetEquipment().Select(CardId).ToArray(), TriggerStates(character).Count, AbilityCooldownProbe.Capture(character), AbilityCooldownProbe.CaptureDictionary(character), AbilityLifecycleProbe.Rules(character.GetSourceCharacterData()),
                new HordeBaseStats(character.GetSourceCharacterData().GetAttackDamage(), character.GetSourceCharacterData().GetHealth()), character.IsSpawning, character.IsSacrifice ? character.SacrificeCard == null ? 0 : CardId(character.SacrificeCard) : (int?)null, DeathSignalProbe.Capture(character));
        }

        internal CombatContext CaptureContext()
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            CardManager cards = managers.GetCardManager()!;
            CombatProjection state = projection.Capture(managers, managers.GetSaveManager(),
                managers.GetCombatManager()!, cards);
            uint[] draw = RngCalibration.Words(RandomManager.GetState(RngId.CardDraw));
            uint[] battle = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
            CardInstanceState[] instances = CardModifierProbe.Capture(cards, CardId);
            BattleStatistics statistics = BattleStatisticsProbe.Capture(managers.GetCardStatistics(), CardId);
            AbilityCardCacheEntry[] abilityCache = AbilityCardProbe.Capture(this);
            CardInstanceState[] registry = CardModifierProbe.Capture(projection.KnownCards, CardId);
            CharacterState lastSpawned = managers.GetMonsterManager()!.GetLastSpawnedCharacterThisTurn();
            BattleSpawnPoints? spawnPoints = BattleSpawnPointProbe.Enabled ? BattleSpawnPointProbe.Capture(this) : null;
            return new CombatContext(new CardCycleState(state.Hand, state.Draw, state.Discard,
                new UnityRng(draw[0], draw[1], draw[2], draw[3]), state.DrawModifier, Array.Empty<string>(), BonusDrawProbe.Capture(cards, this)),
                new UnityRng(battle[0], battle[1], battle[2], battle[3]), state.Gold, projection.NextCardId,
                cards.GetMaxHandSize(), new[] { "armor", "valor", "pyregel", "relentless", "cooldown", "unit_ability", "unit_ability_available", "horde" }.Concat(TriggeredSummonProbe.Enabled ? new[] { "cardless" } : Array.Empty<string>()).Select(id => BattleActionProbe.Status(id, 1)).ToArray(),
                statistics, instances, registry, managers.GetCombatManager()!.AllScenarioBossesDead,
                ((IEnumerable<CardUpgradeState>)AccessTools.Field(typeof(CardManager), "nextAddedTempCardUpgrades").GetValue(cards))
                    .Select(CardModifierProbe.Upgrade).ToArray(), CaptureOtherPiles(), CaptureQueryFrame(managers),
                (bool)AccessTools.Field(typeof(CombatManager), "isKillCamActivated").GetValue(managers.GetCombatManager()),
                Enumerable.Range(0, managers.GetRoomManager()!.GetNumRooms()).SelectMany(index => new[] { Team.Type.Heroes, Team.Type.Monsters }
                    .Select(team => new RoomMagicPower(index, team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player,
                        managers.GetRoomManager()!.GetRoom(index).GetRoomStateModifiedMagicPower(team, managers.GetCoreManagers()) +
                        managers.GetRelicManager().GetMagicPowerModification()))).ToArray(), PreviewRngIsolation.Enabled ? true : (bool?)null,
                new BattleEnergyState(managers.GetSaveManager().GetBalanceData().GetMaxEnergy(),
                    (int)AccessTools.Field(typeof(CombatManager), "modifiedEnergyNextTurn").GetValue(managers.GetCombatManager()),
                    (int)AccessTools.Field(typeof(CombatManager), "modifiedEnergyEveryTurn").GetValue(managers.GetCombatManager()),
                    managers.GetCombatManager()!.GetCombatPhase().ToString(), managers.GetPlayerManager().GetTowerHP() > 0),
                RoomCapacityProbe.Capture(managers.GetRoomManager()!), abilityCache,
                AccessTools.Field(typeof(CombatManager), "lastAbilityActivatorCharacter").GetValue(managers.GetCombatManager()) is CharacterState activator
                    ? UnitId(activator) : 0, AbilityLifecycleProbe.Disabled(managers.GetSaveManager()),
                lastSpawned == null || normalizeDestroyedAttacker && (lastSpawned.IsDestroyed || !lastSpawned.IsAlive) ? 0 : UnitId(lastSpawned), NextUnitId, spawnPoints,
                TriggeredSummonProbe.Catalog());
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
                save.GetGameSequence() == SaveData.GameSequence.InBattle);
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
            return unit.GetTriggers().Select(trigger =>
            {
                CharacterTriggerData data = trigger.GetTriggerData();
                if (unit.IsSacrifice && unit.SacrificeCard != null && unit.SacrificeCard.GetTraitStates().Count > 0 &&
                    trigger.GetEffectStates().Any(effect => effect.GetCardEffect() is CardEffectHeal))
                    interactions.Add("Sacrifice healing damage traits");
                var effects = trigger.GetEffectStates().Select(effect =>
                {
                    string type = effect.GetCardEffect().GetType().Name;
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
                    if (type == "CardEffectAddBattleCard")
                    {
                        effect.GetFilteredCardListFromPool(AllGameManagers.Instance!.GetRelicManager(), ref pool);
                    }
                    return new CombatEffect(type, value, counter, ((CardPile)(AbilityLifecycleProbe.Known(type) ? 0 : effect.GetParamInt())).ToString(),
                        effect.GetAdditionalParamInt(), pool.Select(card => card.GetID()).ToArray(), effect.GetParamBool2(),
                        type == "CardEffectAddBattleCard" ? CardGenerationProbe.Definition(effect) : null,
                        UnitTriggerUpgradeProbe.Capture(effect, interactions), UnitTriggerActionProbe.Capture(effect),
                        type == "CardEffectDamage" && effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null, UnitTriggerActionProbe.Scaling(effect),
                        TriggeredSummonProbe.Capture(effect));
                }).ToArray();
                int[] boundEquipment = trigger.GetEffectStates().Select(effect => effect.GetParentEquipment() == null ? 0 : Active!.CardId(effect.GetParentEquipment()!)).Distinct().ToArray();
                int equipmentId = boundEquipment.Length == 1 ? boundEquipment[0] : 0;
                if (boundEquipment.Length > 1 || trigger.IsFromEquipment != (equipmentId > 0)) interactions.Add("Inconsistent equipment trigger bindings");
                return new CombatTrigger(trigger.GetTrigger().ToString(), data.GetTriggerOnce(),
                    trigger.GetHasTriggeredOnce(false), trigger.GetHideVisualAndIgnoreSilence(),
                    unit.GetTriggerFireCount(trigger.GetTrigger(), trigger), effects,
                    AllGameManagers.Instance!.GetSaveManager().GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers().Contains(trigger.GetTrigger()),
                    data.GetTriggerAtThreshold(), new CombatTriggerOrigin(Active!.UpgradeKey(trigger.triggerId), equipmentId,
                        trigger.IsFromEquipment, data.GetOnlyTriggerIfEquipped()), Active!.TriggerStateId(unit, trigger), EnemySpawningProbe.TriggerConditions(data), data.GetRemoveOnRelentlessChange());
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
                    record = new StageRecord
                    {
                        Index = stages.Count,
                        Turn = managers.GetCombatManager()!.GetTurnCount(),
                        Kind = kind,
                        Room = room,
                        Before = before,
                        Predicted = kind == "Exchange" ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before)
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
                if (stage.Predicted.Supported)
                {
                    JToken expected = JToken.FromObject(stage.Predicted.State!.Units);
                    JToken actual = JToken.FromObject(stage.Actual.Units);
                    stage.Difference = JToken.DeepEquals(expected, actual) ? null : "Unit states differ";
                    if (!JToken.DeepEquals(JToken.FromObject(stage.Predicted.State.Context!),
                        JToken.FromObject(stage.Actual.Context!))) stage.Difference = "Card piles, RNG or gold differ";
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
            log.LogInfo("BATTLE-TERMINAL won=" + won + " pyre=" + AllGameManagers.Instance!.GetSaveManager().GetTowerHP());
            // Tick exports once, after this native coroutine and its pending
            // observation wrappers have returned. Exporting here would duplicate
            // the entire capture and could still include an unfinished wrapper.
        }

        internal string Write()
        {
            bool binary = Environment.GetEnvironmentVariable("MT2_PROBE_BINARY_CAPTURE") == "1";
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, binary ? "full-battle.mt2f" : "full-battle.json");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            string temporary = path + ".tmp";
            var snapshot = new
            {
                Schema = 86,
                GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                NativeWon,
                TerminalCaptureBoundary = "AfterStopCombatLoop",
                TerminalEffectsSettled,
                Policy = Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY"),
                ModifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS"),
                RequestedGameSpeed = ProbeGameSpeed.Requested?.ToString(),
                LiveGameSpeedOverrides = ProbeGameSpeed.LiveOverrides,
                CaptureFailures,
                Mismatches,
                Unsupported,
                Pending,
                Stages = stages,
                CardCycles = cardCycles.Records,
                TrainPhases = trainCombat.Records,
                Spawns = spawning.Records,
                Turns = turns.Records,
                Actions = actions.Records,
                CrossRoomTargets = CrossRoomSpellScenario.Targets,
                NumericRanges = NumericRangeScenario.Samples,
                FilteredTargets = TargetFilterScenario.Targets,
                HandRemovals = HandRemovalScenario.Records,
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
                SpawnUpgrades = SpawnUpgradeProbe.Records,
                FreshSpawners = FreshSpawnerProbe.Records,
                GlobalStandbyChecks = FreshSpawnerProbe.Checks,
                RallyPhases = RallyProbe.Phases,
                RallyTriggers = RallyProbe.Triggers,
                DyingHordeUpgradeOperations = DyingHordeUpgradeScenario.Operations,
                HordeUpgradeOperations = HordeUpgradeScenario.Operations,
                HordeStatusOperations = HordeStatusScenario.Records,
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
                StatusRegistryCalibration = StatusRegistryCalibration.Samples,
                StatusCallbacks = StatusCallbackProbe.Records,
                StatusCallbackFires = StatusCallbackProbe.Fired,
                CharacterCallbackFires = StatusCallbackProbe.OtherFired,
                StatusCallbackActions = Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACK_ACTIONS") == "1",
                EnergyEffects = EnergyEffectProbe.Records,
                BonusDrawEffects = BonusDrawProbe.Records,
                CapacityEffects = RoomCapacityProbe.Records,
                CapacityTests = RoomCapacityProbe.Tests,
                PreviewRngIsolation = PreviewRngIsolation.Records,
                UnitPostCombats = PostCombatHealingProbe.Records,
                UnitUpgradeScalingCalibrationContextUnchanged = UnitUpgradeScalingScenario.CalibrationContextUnchanged,
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
            public RoomCombatResult Predicted { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public string? Difference { get; set; }
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
