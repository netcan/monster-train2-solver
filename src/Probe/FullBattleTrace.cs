using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private readonly List<StageRecord> stages = new List<StageRecord>();
        private readonly List<object> checkpoints = new List<object>();
        private int nextId = 1;
        private int phaseSequence;
        internal int NextPhaseSequence() => ++phaseSequence;
        internal int NextUnitId => nextId;
        internal int UnitId(CharacterState unit)
        {
            if (!identities.TryGetValue(unit, out int id)) identities.Add(unit, id = nextId++);
            return id;
        }
        private bool calibrated;
        internal static FullBattleTrace? Active { get; private set; }
        internal bool? NativeWon { get; private set; }
        private bool? stoppingOutcome;
        internal bool? TerminalEffectsSettled { get; private set; }
        private int captureFailures;
        internal int CaptureFailures => captureFailures + DamageScalingScenario.Samples.Count(sample => sample.CaptureError != null) +
            StatusScalingScenario.Samples.Count(sample => sample.CaptureError != null) + StatusScalingScenario.Applications.Count(sample => sample.CaptureError != null) +
            UnitUpgradeScalingScenario.Samples.Count(sample => sample.CaptureError != null);
        internal int Mismatches => stages.Count(stage => stage.Difference != null) + cardCycles.Mismatches + trainCombat.Mismatches + spawning.Mismatches + turns.Mismatches + actions.Mismatches +
            HandRemovalScenario.Records.Count(record => record.Difference != null) + GenerationScenario.Records.Count(record => record.Difference != null) +
            DamageScalingScenario.Samples.Count(sample => sample.Difference != null) + StatusScalingScenario.Samples.Count(sample => sample.Difference != null) +
            StatusScalingScenario.Applications.Count(sample => sample.Difference != null) + UnitUpgradeScalingScenario.Samples.Count(sample => sample.Difference != null) +
            UnitTurnBeginProbe.Records.Count(record => record.Difference != null) + TeamTurnBeginProbe.Records.Count(record => record.Difference != null);
        internal int Unsupported => stages.Count(stage => !stage.Predicted.Supported) + cardCycles.Unsupported + trainCombat.Unsupported + spawning.Unsupported + turns.Unsupported + actions.Unsupported +
            HandRemovalScenario.Records.Count(record => !record.Predicted.Supported) + GenerationScenario.Records.Count(record => !record.Predicted.Supported) +
            UnitTurnBeginProbe.Records.Count(record => !record.Predicted.Supported) + TeamTurnBeginProbe.Records.Count(record => !record.Predicted.Supported);
        internal int Pending => stages.Count(stage => stage.Actual == null) + cardCycles.Records.Count(record => record.Actual == null) +
            trainCombat.Records.Count(record => record.Actual == null) + spawning.Records.Count(record => record.Actual == null) +
            turns.Records.Count(record => record.Actual == null) + actions.Records.Count(record => record.Actual == null) +
            HandRemovalScenario.Records.Count(record => record.Actual == null) + GenerationScenario.Records.Count(record => record.Actual == null) +
            DamageScalingScenario.Samples.Count(sample => sample.After == null) + StatusScalingScenario.Samples.Count(sample => sample.After == null) +
            StatusScalingScenario.Applications.Count(sample => sample.After == null) + UnitUpgradeScalingScenario.Samples.Count(sample => sample.After == null) +
            UnitTurnBeginProbe.Records.Count(record => record.Actual == null) + TeamTurnBeginProbe.Records.Count(record => record.Actual == null);

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
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "unit-upgrade-scaling") UnitUpgradeScalingScenario.Calibrate(this);
                calibrated = true;
            }
            checkpoints.Add(new { Label = label, State = projection.Capture(managers, save, combat, cards) });
        }

        internal void BeginEndTurn() => turns.Begin();
        internal BattleTurnState CaptureDecision() => turns.Capture();
        internal BattlePlayRules CapturePlayRules(EnemySpawnState spawn) => actions.CaptureRules(spawn);
        internal int CardId(CardState card) => projection.CaptureCards(new List<CardState> { card })[0].InstanceId;
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
                int id = UnitId(character);
                CombatTrigger[] triggers = CaptureTriggers(character, interactions);
                if (character.IsPurified()) interactions.Add("Purified unit trigger restrictions");
                if (character.GetRoomStateModifiers().Count > 0)
                    interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " room modifiers");
                if (character.GetEquipment().Count > 0)
                    interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " equipment");
                CardState? card = character.GetSpawnerCard();
                if (card != null && (card.GetTraitStates().Any(trait => !DamageScalingProbe.Known(trait.GetType().Name)) || card.GetTriggers().Count > 0))
                    interactions.Add(character.GetSourceCharacterData().GetAssetKey() + " card traits/triggers");
                var nativeStatuses = new List<CharacterState.StatusEffectStack>();
                character.GetStatusEffects(ref nativeStatuses);
                var statuses = new List<CombatStatus>();
                foreach (CharacterState.StatusEffectStack status in nativeStatuses)
                {
                    StatusEffectState rule = status.State;
                    if (status.Count <= 0) continue;
                    if (rule.GetRemoveWhenTriggeredAfterCardPlayed() || rule.GetRemoveAtEndOfTurnIfTriggered())
                        interactions.Add(rule.GetStatusId() + " delayed status removal");
                    statuses.Add(new CombatStatus(rule.GetStatusId(), status.Count, rule.GetParamInt(),
                        rule.GetRemoveWhenTriggered(), rule.GetRemoveStackAtEndOfTurn(), rule.GetRemoveAtEndOfTurn(),
                        rule.GetRemoveAtEndOfTurnAfterPostCombat(), rule.PreventRemovalDuringRelentlessPhase,
                        rule.GetSkipTriggerDuringDeployment(), rule.GetRemoveDuringDeployment(),
                        BattleActionProbe.TriggeredVfx(rule.GetSourceStatusEffectData(), -1f),
                        BattleActionProbe.TriggeredVfx(rule.GetSourceStatusEffectData(), 1f), rule.IsStackable()));
                }
                CombatTeam team = character.GetTeamType() == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player;
                bool endsBattle = team == CombatTeam.Enemy &&
                    (character.IsMiniboss() || character.IsOuterTrainBoss()) &&
                    heroes.FindPairedCompanionBoss(character) == null;
                units.Add(new CombatUnit(id, character.GetSourceCharacterData()?.GetAssetKey() ?? "",
                    team, character.GetAttackDamageWithoutStatusEffectBuffs(), character.GetHP(), character.GetMaxHP(),
                    character.GetCanAttack(), character.IsPyreHeart(), endsBattle, statuses, triggers,
                    card == null ? 0 : projection.CaptureCards(new List<CardState> { card })[0].InstanceId, character.GetSize(),
                    ((List<string>)AccessTools.Field(typeof(CharacterState), "statusEffectImmunities").GetValue(character)).ToArray(),
                    character.GetSubtypes().Select(subtype => subtype.Key).ToArray(), UnitModifierProbe.Capture(character), character.IsAnyBoss()));
            }
            return new RoomCombatState(room.GetRoomIndex(), combat.IsPlacementPhase, units,
                interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(), CaptureContext());
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
            CardInstanceState[] registry = CardModifierProbe.Capture(projection.KnownCards, CardId);
            return new CombatContext(new CardCycleState(state.Hand, state.Draw, state.Discard,
                new UnityRng(draw[0], draw[1], draw[2], draw[3]), state.DrawModifier, Array.Empty<string>()),
                new UnityRng(battle[0], battle[1], battle[2], battle[3]), state.Gold, projection.NextCardId,
                cards.GetMaxHandSize(), new[] { "armor", "valor", "pyregel" }.Select(id => BattleActionProbe.Status(id, 1)).ToArray(),
                statistics, instances, registry, managers.GetCombatManager()!.AllScenarioBossesDead,
                ((IEnumerable<CardUpgradeState>)AccessTools.Field(typeof(CardManager), "nextAddedTempCardUpgrades").GetValue(cards))
                    .Select(CardModifierProbe.Upgrade).ToArray(), CaptureOtherPiles(), CaptureQueryFrame(managers));
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
                if (data.GetTriggerAtThreshold() != 0 || data.GetOnlyTriggerIfEquipped() || data.GetRemoveOnRelentlessChange() ||
                    data.GetRequiredStatusEffects().Count > 0 || data.GetRequiredStatusEffectsForDyingCharacter().Count > 0)
                    interactions.Add("Conditional trigger " + trigger.GetTrigger());
                var effects = trigger.GetEffectStates().Select(effect =>
                {
                    string type = effect.GetCardEffect().GetType().Name;
                    int value = effect.GetParamInt(), counter = 0;
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
                    return new CombatEffect(type, value, counter, ((CardPile)effect.GetParamInt()).ToString(),
                        effect.GetAdditionalParamInt(), pool.Select(card => card.GetID()).ToArray(), effect.GetParamBool2(),
                        type == "CardEffectAddBattleCard" ? CardGenerationProbe.Definition(effect) : null,
                        UnitTriggerUpgradeProbe.Capture(effect, interactions));
                }).ToArray();
                return new CombatTrigger(trigger.GetTrigger().ToString(), data.GetTriggerOnce(),
                    trigger.GetHasTriggeredOnce(false), trigger.GetHideVisualAndIgnoreSilence(),
                    unit.GetTriggerFireCount(trigger.GetTrigger(), trigger), effects,
                    AllGameManagers.Instance!.GetSaveManager().GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers().Contains(trigger.GetTrigger()));
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
            Write();
        }

        internal string Write()
        {
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "full-battle.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(new
            {
                Schema = 29,
                GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                NativeWon,
                TerminalCaptureBoundary = "AfterStopCombatLoop",
                TerminalEffectsSettled,
                Policy = Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY"),
                ModifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS"),
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
                UnitTurns = UnitTurnBeginProbe.Records,
                TeamTurnBegins = TeamTurnBeginProbe.Records,
                UnitUpgradeScalingCalibrationContextUnchanged = UnitUpgradeScalingScenario.CalibrationContextUnchanged,
                UiRngIsolation = UiRngIsolation.Records,
                Checkpoints = checkpoints
            }, Formatting.Indented));
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
