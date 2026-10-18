using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;
using ShinyShoe;
using ShinyShoe.Loading;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class NativeReplayScenario
    {
        private enum Stage
        {
            Menu,
            NewRun,
            NaturalMap,
            NaturalIntro,
            Battle,
            SelectingRoom,
            PlayingCard,
            WaitingTurn,
            Restarting,
            Replaying,
            Quitting
        }

        private enum Pass { Source, Branched, Direct }

        private const int Seed = 424242;
        private const string StewardCardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private readonly ManualLogSource log;
        private readonly int depth;
        private readonly int targetTurn;
        private readonly HashSet<int> sourcePlayTurns;
        private readonly bool directMode;
        private readonly bool fastReplay;
        private readonly bool noTimeoutReplay;
        private readonly bool branchAnyUnit;
        private readonly bool fullBattle;
        private readonly MethodInfo setSeed;
        private readonly MethodInfo newRun;
        private readonly MethodInfo completeFtue;
        private readonly MethodInfo startRunFromSetup;
        private readonly MethodInfo endTurn;
        private readonly FieldInfo fightAvailable;
        private readonly List<string> sourceSignatures = new List<string>();
        private readonly List<int> sourceReplayCounts = new List<int>();
        private Stage stage;
        private Pass pass;
        private float deadline;
        private float quitStarted;
        private float lastHeartbeat;
        private float lastMapAttempt;
        private float restartStarted;
        private float replayStarted;
        private int quitCode;
        private int initialTurn;
        private int recordedTurn = -1;
        private int expectedTurn;
        private int sourceBattleOffset;
        private bool mainMenuRequested;
        private bool runSetupRequested;
        private bool roomSelectionRequested;
        private bool playbackCallbackReceived;
        private bool playbackSucceeded;
        private bool numericModifiersPrepared;
        private string playbackDescription = string.Empty;
        private List<string>? sourceEntries;
        private PlayCardAction? pendingPlay;
        private bool damagePerHpAbilityInputPrepared;

        internal NativeReplayScenario(ManualLogSource log)
        {
            this.log = log;
            depth = ParseTurn("MT2_PROBE_DEPTH", 4);
            targetTurn = ParseTurn("MT2_PROBE_TARGET_TURN", 1);
            if (depth < 1 || targetTurn < 0 || targetTurn >= depth)
            {
                throw new ArgumentException("Expected 0 <= MT2_PROBE_TARGET_TURN < MT2_PROBE_DEPTH.");
            }
            sourcePlayTurns = ParseTurnSet(Environment.GetEnvironmentVariable("MT2_PROBE_SOURCE_PLAY_TURNS"));
            foreach (int turn in sourcePlayTurns)
            {
                if (turn < 0 || turn >= depth || turn == targetTurn)
                {
                    throw new ArgumentException("Source play turns must be in range and exclude the branch turn.");
                }
            }
            directMode = Environment.GetEnvironmentVariable("MT2_PROBE_DIRECT_BRANCH") == "1";
            fastReplay = Environment.GetEnvironmentVariable("MT2_PROBE_FAST_REPLAY") == "1";
            noTimeoutReplay = Environment.GetEnvironmentVariable("MT2_PROBE_NO_TIMEOUT") != "0";
            branchAnyUnit = Environment.GetEnvironmentVariable("MT2_PROBE_BRANCH_ANY_UNIT") == "1";
            fullBattle = Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE") == "1";
            if (fullBattle && !directMode)
                throw new ArgumentException("Full-battle capture requires direct mode.");
            pass = directMode ? Pass.Direct : Pass.Source;
            setSeed = Command("Command_SetSeed", typeof(string));
            newRun = Command("Command_NewRun");
            completeFtue = Command("Command_CompleteFTUE");
            endTurn = Command("Command_EndTurn");
            startRunFromSetup = typeof(RunSetupScreen).GetMethod("StartRun", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(RunSetupScreen).FullName, "StartRun");
            fightAvailable = typeof(BattleIntroScreen).GetField("isFightButtonAvailable", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(BattleIntroScreen).FullName, "isFightButtonAvailable");
            Enter(Stage.Menu, 120f);
            log.LogInfo("DEPTH-CONFIG depth=" + depth + " target=" + targetTurn +
                " sourcePlays=" + string.Join(",", sourcePlayTurns) + " direct=" + directMode +
                " fastReplay=" + fastReplay + " noTimeout=" + noTimeoutReplay +
                " branchAnyUnit=" + branchAnyUnit);
        }

        internal void Tick()
        {
            if (Time.realtimeSinceStartup - lastHeartbeat >= 10f)
            {
                lastHeartbeat = Time.realtimeSinceStartup;
                AllGameManagers? current = AllGameManagers.Instance;
                log.LogInfo("DEPTH-WAIT stage=" + stage + " pass=" + pass +
                    " turn=" + (current?.GetCombatManager()?.GetTurnCount().ToString() ?? "none") +
                    " sequence=" + (current?.GetSaveManager()?.GetGameSequence().ToString() ?? "none") +
                    " loading=" + LoadingScreen.IsWorking());
            }
            if (stage == Stage.Quitting)
            {
                if (Time.realtimeSinceStartup - quitStarted > 5f)
                {
                    Environment.Exit(quitCode);
                }
                return;
            }
            if (fullBattle && FullBattleTrace.Active?.NativeWon != null)
            {
                FullBattleTrace trace = FullBattleTrace.Active;
                try
                {
                    if (OriginalForgeConservationScenario.Enabled && !OriginalForgeConservationScenario.NaturalCompleted)
                    {
                        if (Time.realtimeSinceStartup > deadline) Finish(false, "Natural forge-conservation combat-end callback did not complete.");
                        return;
                    }
                    if (OriginalEquipmentGrowthScenario.Prepared && !OriginalEquipmentGrowthScenario.EncounterCompleteSettled)
                    {
                        if (Time.realtimeSinceStartup > deadline) Finish(false, "Natural equipment-growth encounter-complete callback did not complete.");
                        return;
                    }
                    // StopCombatLoop captures the battle boundary. Later cleanup
                    // resets resources/phase; keep that snapshot while allowing
                    // native card animation callbacks to finish before export/exit.
                    if (!CardAnimationSettlement.Ready())
                    {
                        if (Time.realtimeSinceStartup > deadline) Finish(false, "Terminal card animations did not settle before capture.");
                        return;
                    }
                    trace.Write();
                    Finish(trace.CaptureFailures == 0 && trace.Pending == 0 && trace.Mismatches == 0 && trace.Unsupported == 0,
                        "Full native battle finished; won=" + trace.NativeWon + "; unsupported stages=" + trace.Unsupported);
                }
                catch (Exception error) { Finish(false, "Could not export settled full battle: " + error); }
                return;
            }
            if (Time.realtimeSinceStartup > deadline)
            {
                Finish(false, "Timed out in " + stage + ", pass=" + pass);
                return;
            }
            try
            {
                Step();
            }
            catch (Exception ex)
            {
                Finish(false, "Error in " + stage + ": " + (ex is TargetInvocationException ? ex.InnerException ?? ex : ex));
            }
        }

        private void Step()
        {
            AllGameManagers? managers = AllGameManagers.Instance;
            SaveManager? save = managers?.GetSaveManager();
            ScreenManager? screen = managers?.GetScreenManager();
            if (save == null || screen == null)
            {
                return;
            }
            if (stage == Stage.Menu)
            {
                if (save.UserState != SaveManager.SaveManagerUserState.UserLoaded)
                {
                    return;
                }
                if (!screen.GetScreenActive(ScreenName.MainMenu))
                {
                    if (!mainMenuRequested && screen.GetTopScreen() == ScreenName.IntroStart && !LoadingScreen.IsWorking())
                    {
                        mainMenuRequested = true;
                        screen.ReturnToMainMenu();
                    }
                    return;
                }
                CheatManager.SetupConsoleCommands(save, screen);
                completeFtue.Invoke(null, null);
                setSeed.Invoke(null, new object[] { Seed.ToString(CultureInfo.InvariantCulture) });
                newRun.Invoke(null, null);
                Enter(Stage.NewRun, 240f);
                return;
            }
            if (stage == Stage.NewRun)
            {
                if (!LoadingScreen.IsWorking() && screen.GetScreenActive(ScreenName.RunOpening) &&
                    screen.GetScreen(ScreenName.RunOpening) is RunOpeningScreen opening &&
                    ReplayManager.TryClickButton(opening.GetConfirmButton()))
                {
                    return;
                }
                if (!runSetupRequested && !LoadingScreen.IsWorking() &&
                    screen.GetScreenActive(ScreenName.RunSetup) &&
                    screen.GetScreen(ScreenName.RunSetup) is RunSetupScreen setup)
                {
                    runSetupRequested = true;
                    startRunFromSetup.Invoke(setup, null);
                    return;
                }
                if (!LoadingScreen.IsWorking() && save.GetRunType() == RunType.Class &&
                    save.GetGameSequence() == SaveData.GameSequence.DestinationReached)
                {
                    Enter(Stage.NaturalMap, 120f);
                }
                return;
            }
            if (stage == Stage.NaturalMap)
            {
                if (save.GetGameSequence() == SaveData.GameSequence.BattleIntro)
                {
                    Enter(Stage.NaturalIntro, 120f);
                    return;
                }
                if (screen.GetScreenActive(ScreenName.Dialog))
                {
                    DialogScreen? dialog = screen.GetScreen(ScreenName.Dialog) as DialogScreen;
                    if (dialog?.GetButton1() != null)
                    {
                        ReplayManager.TryClickButton(dialog.GetButton1());
                    }
                    return;
                }
                if (LoadingScreen.IsWorking() || save.GetGameSequence() != SaveData.GameSequence.DestinationReached ||
                    !screen.GetScreenActive(ScreenName.Map) || Time.realtimeSinceStartup - lastMapAttempt < 1f)
                {
                    return;
                }
                lastMapAttempt = Time.realtimeSinceStartup;
                MapScreen? map = screen.GetScreen(ScreenName.Map) as MapScreen;
                int section = save.GetCurrentDistance() + 1;
                if (map != null && map.TryGetBattleNodeUI(section, out MapBattleNodeUI node))
                {
                    if (fullBattle && CovenantCharacterFilterScenario.Enabled && !CovenantCharacterFilterScenario.Prepared)
                        CovenantCharacterFilterScenario.ConfigureBeforeBattle(managers!, log);
                    if (fullBattle && TitanSkinScenario.Enabled && !TitanSkinScenario.Prepared)
                        TitanSkinScenario.ConfigureBeforeBattle(managers!, log);
                    if (fullBattle && MagebladeScenario.Enabled && !MagebladeScenario.Prepared)
                        MagebladeScenario.ConfigureBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCardPlayMagicPowerScenario.Enabled && !OriginalCardPlayMagicPowerScenario.Acquired)
                        OriginalCardPlayMagicPowerScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalTwoChargesOnCardConsumedScenario.Enabled && !OriginalTwoChargesOnCardConsumedScenario.Acquired)
                        OriginalTwoChargesOnCardConsumedScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalChanceForSpellWeaknessScenario.Enabled && !OriginalChanceForSpellWeaknessScenario.Acquired)
                        OriginalChanceForSpellWeaknessScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalChanceToCleanseScenario.Enabled && !OriginalChanceToCleanseScenario.Acquired)
                        OriginalChanceToCleanseScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalExtraFeedTriggerScenario.Enabled && !OriginalExtraFeedTriggerScenario.Acquired)
                        OriginalExtraFeedTriggerScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalSummonWithAvariceScenario.Enabled && !OriginalSummonWithAvariceScenario.Acquired)
                        OriginalSummonWithAvariceScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalExtractForEmberAndEchosScenario.Enabled && !OriginalExtractForEmberAndEchosScenario.Acquired)
                        OriginalExtractForEmberAndEchosScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCheaperSpellCostScenario.Enabled && !OriginalCheaperSpellCostScenario.Acquired)
                        OriginalCheaperSpellCostScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalFirstCardCostScenario.Enabled && !OriginalFirstCardCostScenario.Acquired)
                        OriginalFirstCardCostScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalRebalanceValorScenario.Enabled && !OriginalRebalanceValorScenario.Acquired)
                        OriginalRebalanceValorScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCardDrawOnCardPlayScenario.Enabled && !OriginalCardDrawOnCardPlayScenario.Acquired)
                        OriginalCardDrawOnCardPlayScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCarriageJackScenario.Enabled && !OriginalCarriageJackScenario.Acquired)
                        OriginalCarriageJackScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalFirstRoomAttachmentScenario.Enabled && !OriginalFirstRoomAttachmentScenario.Acquired)
                        OriginalFirstRoomAttachmentScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalStartRoomAttachmentScenario.Enabled && !OriginalStartRoomAttachmentScenario.Acquired)
                        OriginalStartRoomAttachmentScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCardAttackRelicScenario.Enabled && !OriginalCardAttackRelicScenario.Acquired)
                        OriginalCardAttackRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalGarfieldBoxRandomCardScenario.Enabled && !OriginalGarfieldBoxRandomCardScenario.Acquired)
                        OriginalGarfieldBoxRandomCardScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalAddSnackOnKillScenario.Enabled && !OriginalAddSnackOnKillScenario.Acquired)
                        OriginalAddSnackOnKillScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalBalatroEventYorickScenario.Enabled && !OriginalBalatroEventYorickScenario.Acquired)
                        OriginalBalatroEventYorickScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalBalatroEventCavendishScenario.Enabled && !OriginalBalatroEventCavendishScenario.Acquired)
                        OriginalBalatroEventCavendishScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalTribouletScenario.Enabled && !OriginalTribouletScenario.Acquired)
                        OriginalTribouletScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCooldownOnDamageScenario.Enabled && !OriginalCooldownOnDamageScenario.Acquired)
                        OriginalCooldownOnDamageScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalDeckTurnDrawScenario.Enabled && !OriginalDeckTurnDrawScenario.Acquired)
                        OriginalDeckTurnDrawScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCardPlayDamageScenario.Enabled && !OriginalCardPlayDamageScenario.Acquired)
                        OriginalCardPlayDamageScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalConsumeDamageScenario.Enabled && !OriginalConsumeDamageScenario.Acquired)
                        OriginalConsumeDamageScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalEatenDamageScenario.Enabled && !OriginalEatenDamageScenario.Acquired)
                        OriginalEatenDamageScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalMoreCapacityScenario.Enabled && !OriginalMoreCapacityScenario.Acquired)
                        OriginalMoreCapacityScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalBurnoutDamageScenario.Enabled && !OriginalBurnoutDamageScenario.Acquired)
                        OriginalBurnoutDamageScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalHealDamageScenario.Enabled && !OriginalHealDamageScenario.Acquired)
                        OriginalHealDamageScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalKillAttackScenario.Enabled && !OriginalKillAttackScenario.Acquired)
                        OriginalKillAttackScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalSpawnSilenceScenario.Enabled && !OriginalSpawnSilenceScenario.Acquired)
                        OriginalSpawnSilenceScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalFreezeReduceCostScenario.Enabled && !OriginalFreezeReduceCostScenario.Acquired)
                        OriginalFreezeReduceCostScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalRandomFreezeScenario.Enabled && !OriginalRandomFreezeScenario.Acquired)
                        OriginalRandomFreezeScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalRandomHandUpgradeScenario.Enabled && !OriginalRandomHandUpgradeScenario.Acquired)
                        OriginalRandomHandUpgradeScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalStartTurnPropagationScenario.Enabled && !OriginalStartTurnPropagationScenario.Acquired)
                        OriginalStartTurnPropagationScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalMooncycleFaeScenario.Enabled && !OriginalMooncycleFaeScenario.Acquired)
                        OriginalMooncycleFaeScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalRandomDrawCostScenario.Enabled && !OriginalRandomDrawCostScenario.Acquired)
                        OriginalRandomDrawCostScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalSuppressExhaustScenario.Enabled && !OriginalSuppressExhaustScenario.Acquired)
                        OriginalSuppressExhaustScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalExpensiveCardUpgradeScenario.Enabled && !OriginalExpensiveCardUpgradeScenario.Acquired)
                        OriginalExpensiveCardUpgradeScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalWolosBloodletterScenario.Enabled && !OriginalWolosBloodletterScenario.Acquired)
                        OriginalWolosBloodletterScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCardTypeSequenceScenario.Enabled && !OriginalCardTypeSequenceScenario.Acquired)
                        OriginalCardTypeSequenceScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalWhelpCooldownScenario.Enabled && !OriginalWhelpCooldownScenario.Acquired)
                        OriginalWhelpCooldownScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalSpawnRetreatScenario.Enabled && !OriginalSpawnRetreatScenario.Acquired)
                        OriginalSpawnRetreatScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalOpeningMorselsScenario.Enabled && !OriginalOpeningMorselsScenario.Acquired)
                        OriginalOpeningMorselsScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalOpeningDeckSpawnScenario.Enabled && !OriginalOpeningDeckSpawnScenario.Acquired)
                        OriginalOpeningDeckSpawnScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalDecayDeathScenario.Enabled && !OriginalDecayDeathScenario.Acquired)
                        OriginalDecayDeathScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalBoneDogScenario.Enabled && !OriginalBoneDogScenario.Acquired)
                        OriginalBoneDogScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalGrosMichelScenario.Enabled && !OriginalGrosMichelScenario.Acquired)
                        OriginalGrosMichelScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCardTypeCostScenario.Enabled && !OriginalCardTypeCostScenario.Acquired)
                        OriginalCardTypeCostScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalPurgeToExhaustScenario.Enabled && !OriginalPurgeToExhaustScenario.Acquired)
                        OriginalPurgeToExhaustScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalUpgradeCountRelicsScenario.Enabled && !OriginalUpgradeCountRelicsScenario.Acquired)
                        OriginalUpgradeCountRelicsScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalHealingPowerScenario.Enabled && !OriginalHealingPowerScenario.Acquired)
                        OriginalHealingPowerScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalStartCombatEnergyScenario.Enabled && !OriginalStartCombatEnergyScenario.Acquired)
                        OriginalStartCombatEnergyScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalStartCombatHoardScenario.Enabled && !OriginalStartCombatHoardScenario.Acquired)
                        OriginalStartCombatHoardScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalForgeConservationScenario.Enabled && !OriginalForgeConservationScenario.Acquired)
                        OriginalForgeConservationScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalOpeningCorruptionScenario.Enabled && !OriginalOpeningCorruptionScenario.Acquired)
                        OriginalOpeningCorruptionScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalOpeningDrawScenario.Enabled && !OriginalOpeningDrawScenario.Acquired)
                        OriginalOpeningDrawScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalPerkeoScenario.Enabled && !OriginalPerkeoScenario.Acquired)
                        OriginalPerkeoScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCorruptedAngelsScenario.Enabled && !OriginalCorruptedAngelsScenario.Acquired)
                        OriginalCorruptedAngelsScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalPartFactoryScenario.Enabled && !OriginalPartFactoryScenario.Acquired)
                        OriginalPartFactoryScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalBigDamageScenario.Enabled && !OriginalBigDamageScenario.Acquired)
                        OriginalBigDamageScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalEarlyDiscardEnergyScenario.Enabled && !OriginalEarlyDiscardEnergyScenario.Acquired)
                        OriginalEarlyDiscardEnergyScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalTurnDrawScenario.Enabled && !OriginalTurnDrawScenario.Acquired)
                        OriginalTurnDrawScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalSoloUnitUpgradeScenario.Enabled && !OriginalSoloUnitUpgradeScenario.Acquired)
                        OriginalSoloUnitUpgradeScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalFrontRoomHealthUpgradeScenario.Enabled && !OriginalFrontRoomHealthUpgradeScenario.Acquired)
                        OriginalFrontRoomHealthUpgradeScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalFunguyDeckScalingScenario.Enabled && !OriginalFunguyDeckScalingScenario.Acquired)
                        OriginalFunguyDeckScalingScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalCardDrawOnHealScenario.Enabled && !OriginalCardDrawOnHealScenario.Acquired)
                        OriginalCardDrawOnHealScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalBrokenRailScenario.Enabled && !OriginalBrokenRailScenario.Acquired)
                        OriginalBrokenRailScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalHephsBlueprintsScenario.Enabled && !OriginalHephsBlueprintsScenario.Acquired)
                        OriginalHephsBlueprintsScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalRageOnBurnoutStackLossScenario.Enabled && !OriginalRageOnBurnoutStackLossScenario.Acquired)
                        OriginalRageOnBurnoutStackLossScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalPyreRevengeScenario.Enabled && !OriginalPyreRevengeScenario.Acquired)
                        OriginalPyreRevengeScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalNullRelicScenario.Enabled && !OriginalNullRelicScenario.Acquired)
                        OriginalNullRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalRelicCountAttackScenario.Enabled && !OriginalRelicCountAttackScenario.Acquired)
                        OriginalRelicCountAttackScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalMoreSpikesScenario.Enabled && !OriginalMoreSpikesScenario.Acquired)
                        OriginalMoreSpikesScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalStealthSlayScenario.Enabled && !OriginalStealthSlayScenario.Acquired)
                        OriginalStealthSlayScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalSpellPlayThresholdScenario.Enabled && !OriginalSpellPlayThresholdScenario.Acquired)
                        OriginalSpellPlayThresholdScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalConserveEmberScenario.Enabled && !OriginalConserveEmberScenario.Acquired)
                        OriginalConserveEmberScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && ChooseDiscardScenario.OriginalSmeltRelicMode && !ChooseDiscardScenario.RelicAcquired)
                        ChooseDiscardScenario.AcquireRelicBeforeBattle(managers!, log);
                    if (fullBattle && (MixScenario.TinkermasterMode || MixScenario.ProgressMode || MixScenario.NefariousOverflowMode || MixScenario.UncontrolledGroupMode || MixScenario.CopyFirstSpellMode || MixScenario.CopyFirstConsumeMode || MixScenario.CopyConsumedCardMode || MixScenario.MothersRemnant3Mode || MixScenario.MonsterSpellTargetingMode) && !MixScenario.Acquired)
                        MixScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalRoomEntryStatusRelicScenario.Enabled && !OriginalRoomEntryStatusRelicScenario.Acquired)
                        OriginalRoomEntryStatusRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && CorruptionScenario.RelicTrigger && !CorruptionScenario.RelicAcquired)
                        CorruptionScenario.AcquireRelicBeforeBattle(managers!, log);
                    if (fullBattle && OriginalMoonPhaseHealRelicScenario.Enabled && !OriginalMoonPhaseHealRelicScenario.Acquired)
                        OriginalMoonPhaseHealRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalChampionStartTurnHealScenario.Enabled && !OriginalChampionStartTurnHealScenario.Acquired)
                        OriginalChampionStartTurnHealScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalNimsOathScenario.Enabled && !OriginalNimsOathScenario.Acquired)
                        OriginalNimsOathScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalJudgesScalesScenario.Enabled && !OriginalJudgesScalesScenario.Acquired)
                        OriginalJudgesScalesScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalFirstImpBonusScenario.Enabled && !OriginalFirstImpBonusScenario.Acquired)
                        OriginalFirstImpBonusScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalMoonPhaseRoomsGainMagicPowerScenario.Enabled && !OriginalMoonPhaseRoomsGainMagicPowerScenario.Acquired)
                        OriginalMoonPhaseRoomsGainMagicPowerScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalPropagateOnStatusEffectAppliedScenario.Enabled && !OriginalPropagateOnStatusEffectAppliedScenario.Acquired)
                        OriginalPropagateOnStatusEffectAppliedScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalNoUpgradeCardCostScenario.Enabled && !OriginalNoUpgradeCardCostScenario.Acquired)
                        OriginalNoUpgradeCardCostScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalPyreCardCostScenario.Enabled && !OriginalPyreCardCostScenario.Acquired)
                        OriginalPyreCardCostScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalMoonPhaseStatusRelicScenario.Enabled && !OriginalMoonPhaseStatusRelicScenario.Acquired)
                        OriginalMoonPhaseStatusRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalMoonPhaseEnergyRelicScenario.Enabled && !OriginalMoonPhaseEnergyRelicScenario.Acquired)
                        OriginalMoonPhaseEnergyRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalUnitUpgradeTriggerRelicScenario.Enabled && !OriginalUnitUpgradeTriggerRelicScenario.Acquired)
                        OriginalUnitUpgradeTriggerRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && (Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_FLOOR_TRANSITION_DAMAGE") == "1" ||
                        Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_PYREGEL_ON_ASCEND") == "1") && !BumpScenario.RelicAcquired)
                        BumpScenario.AcquireRelicBeforeBattle(managers!, log);
                    if (fullBattle && OriginalResourceRelicBatchScenario.Enabled && !OriginalResourceRelicBatchScenario.Prepared)
                        OriginalResourceRelicBatchScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && PyreResourceRefreshScenario.Enabled && !PyreResourceRefreshScenario.Prepared)
                        PyreResourceRefreshScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && SpawnStatusRelicScenario.OriginalCombatRelics && !SpawnStatusRelicScenario.OriginalCombatRelicsAcquired)
                        SpawnStatusRelicScenario.AcquireCombatBeforeBattle(managers!, log);
                    if (fullBattle && OriginalFloorCapacityRelicScenario.Enabled && !OriginalFloorCapacityRelicScenario.Prepared)
                        OriginalFloorCapacityRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && OriginalDeployableRelicScenario.Enabled && !OriginalDeployableRelicScenario.Acquired)
                        OriginalDeployableRelicScenario.AcquireBeforeBattle(managers!, log);
                    if (fullBattle && RelicCardModifierScenario.ExtraToughTombs && !RelicCardModifierScenario.TombCarrierPrepared)
                        RelicCardModifierScenario.AcquireTombCarrierBeforeBattle(managers!, log);
                    ReplayManager.TryClickButton(node);
                }
                return;
            }
            if (stage == Stage.NaturalIntro)
            {
                if (save.GetGameSequence() == SaveData.GameSequence.InBattle)
                {
                    Enter(Stage.Battle, 120f);
                    return;
                }
                if (!LoadingScreen.IsWorking() && screen.GetScreenActive(ScreenName.BattleIntro) &&
                    screen.GetScreen(ScreenName.BattleIntro) is BattleIntroScreen intro &&
                    (bool)fightAvailable.GetValue(intro)! &&
                    ReplayManager.TryClickButton(intro.GetFightButton()))
                {
                    Enter(Stage.Battle, 120f);
                }
                return;
            }

            if (stage != Stage.Replaying && stage != Stage.Restarting && screen.GetTopScreen() == ScreenName.Dialog)
            {
                DialogScreen? dialog = screen.GetScreen(ScreenName.Dialog) as DialogScreen;
                if (dialog?.GetButton1() != null)
                {
                    ReplayManager.TryClickButton(dialog.GetButton1());
                }
                return;
            }
            CombatManager? combat = managers!.GetCombatManager();
            CardManager? cards = managers.GetCardManager();
            ReplayManager replay = managers.GetReplayManager();
            if (stage == Stage.Restarting)
            {
                if (save.IsInRestartBattleMode || !Ready(managers, save, combat, cards) ||
                    combat!.GetTurnCount() != initialTurn)
                {
                    return;
                }
                log.LogInfo("DEPTH-RESTART-MS " + (int)((Time.realtimeSinceStartup - restartStarted) * 1000f));
                string restored = Capture("restarted-0", managers, save, combat, cards!);
                if (restored != sourceSignatures[0])
                {
                    Finish(false, "Restart did not restore the battle-start state and seeded RNG.");
                    return;
                }
                if (targetTurn == 0)
                {
                    BeginBranchedPass();
                    return;
                }
                if (sourceEntries == null || sourceReplayCounts.Count <= targetTurn)
                {
                    Finish(false, "Missing captured replay prefix.");
                    return;
                }
                int targetCount = sourceReplayCounts[targetTurn];
                if (targetCount <= sourceBattleOffset || targetCount > sourceEntries.Count)
                {
                    Finish(false, "Invalid replay prefix boundary: " + targetCount);
                    return;
                }
                playbackCallbackReceived = false;
                playbackSucceeded = false;
                playbackDescription = string.Empty;
                replayStarted = Time.realtimeSinceStartup;
                replay.StartPlayback(sourceEntries, new ReplayManager.ReplayStartParams
                {
                    verificationMode = noTimeoutReplay ? ReplayManager.VerificationMode.None :
                        ReplayManager.VerificationMode.ContinueRun,
                    numEntriesToSkip = sourceBattleOffset,
                    numEntriesToTrim = sourceEntries.Count - targetCount,
                    allowPlaybackFast = fastReplay,
                    playbackFast = fastReplay,
                    verificationCallback = (success, description) =>
                    {
                        playbackSucceeded = success;
                        playbackDescription = description;
                        playbackCallbackReceived = true;
                    }
                });
                log.LogInfo("DEPTH-REPLAY-START skip=" + sourceBattleOffset + " targetCount=" + targetCount +
                    " fullCount=" + sourceEntries.Count);
                Enter(Stage.Replaying, 180f);
                return;
            }
            if (stage == Stage.Replaying)
            {
                if (noTimeoutReplay ? replay.IsPlayingBackAReplay() : !playbackCallbackReceived)
                {
                    return;
                }
                if (!noTimeoutReplay && !playbackSucceeded)
                {
                    Finish(false, "Native replay verification failed: " + playbackDescription);
                    return;
                }
                if (replay.IsPlayingBackAReplay() || !Ready(managers, save, combat, cards))
                {
                    return;
                }
                log.LogInfo("DEPTH-REPLAY-MS " + (int)((Time.realtimeSinceStartup - replayStarted) * 1000f));
                if (combat!.GetTurnCount() != initialTurn + targetTurn ||
                    save.GetReplayData().GetReplayEntries().Count != sourceReplayCounts[targetTurn])
                {
                    Finish(false, "Native replay stopped at the wrong turn or entry count.");
                    return;
                }
                string restored = Capture("replayed-" + targetTurn, managers, save, combat, cards!);
                if (restored != sourceSignatures[targetTurn])
                {
                    Finish(false, "Native replay state differed at turn " + targetTurn + ".");
                    return;
                }
                log.LogInfo("DEPTH-PREFIX-MATCH turn=" + targetTurn + " entries=" + sourceReplayCounts[targetTurn]);
                BeginBranchedPass();
                return;
            }
            if (stage == Stage.WaitingTurn)
            {
                if (save.GetGameSequence() != SaveData.GameSequence.InBattle)
                {
                    Finish(false, "Battle ended before depth " + depth + ".");
                    return;
                }
                if (!Ready(managers, save, combat, cards))
                {
                    return;
                }
                if (combat!.GetTurnCount() != initialTurn + expectedTurn)
                {
                    Finish(false, "Expected turn " + expectedTurn + ", found " + combat.GetTurnCount());
                    return;
                }
                Enter(Stage.Battle, 60f);
                return;
            }
            if (stage == Stage.SelectingRoom)
            {
                if (Ready(managers, save, combat, cards) && managers.GetRoomManager()?.GetSelectedRoom() == (pendingPlay?.RoomIndex ?? 0))
                {
                    PreviewSchedulingProbe.Observe("RoomSelectionReady");
                    if (pendingPlay != null) PlayPendingPolicyCard(managers, cards!);
                    else PlayConfiguredCard(cards!);
                }
                return;
            }
            if (stage == Stage.PlayingCard)
            {
                if (Ready(managers, save, combat, cards))
                {
                    PreviewSchedulingProbe.Observe("PlayReady");
                    if (fullBattle) FullBattleTrace.Active?.CompleteCardPlay();
                    RoomCapacityAttachmentScenario.CompleteCardPlay(managers);
                    OriginalCarriageJackScenario.CompleteCardPlay(managers);
                    OriginalTwoChargesOnCardConsumedScenario.CompleteCardPlay(managers);
                    OriginalFirstRoomAttachmentScenario.CompleteCardPlay(managers);
                    log.LogInfo("DEPTH-PLAYED pass=" + pass + " turn=" + (combat!.GetTurnCount() - initialTurn));
                    if (IsUnitAndJunkPolicy()) Enter(Stage.Battle, 60f);
                    else AdvanceTurn(combat);
                }
                return;
            }
            if (stage != Stage.Battle || !Ready(managers, save, combat, cards))
            {
                return;
            }
            string? modifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS");
            if (fullBattle && OriginalPropagateOnStatusEffectAppliedScenario.Acquired && !OriginalPropagateOnStatusEffectAppliedScenario.Prepared)
            {
                OriginalPropagateOnStatusEffectAppliedScenario.PrepareCarrier(managers, log);
                return;
            }
            if (fullBattle && !numericModifiersPrepared && RoomCapacityAttachmentScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Capacity attachment fixture must start on deployment turn.");
                RoomCapacityAttachmentScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalCarriageJackScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("CarriageJack fixture must start on deployment turn.");
                OriginalCarriageJackScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalTwoChargesOnCardConsumedScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("2ChargesOnCardConsumed fixture must start on deployment turn.");
                OriginalTwoChargesOnCardConsumedScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalChanceForSpellWeaknessScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("ChanceForSpellWeakness fixture must start on deployment turn.");
                OriginalChanceForSpellWeaknessScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalChanceToCleanseScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("ChanceToCleanse fixture must start on deployment turn.");
                OriginalChanceToCleanseScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalExtraFeedTriggerScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("ExtraFeedTrigger fixture must start on deployment turn.");
                OriginalExtraFeedTriggerScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalSummonWithAvariceScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("SummonWithAvarice fixture must start on deployment turn.");
                OriginalSummonWithAvariceScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalFirstRoomAttachmentScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("FirstRoomCardOnAllFloors fixture must start on deployment turn.");
                OriginalFirstRoomAttachmentScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalStartRoomAttachmentScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Floor-one start attachment fixture must start on deployment turn.");
                OriginalStartRoomAttachmentScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalFloorCapacityRelicScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Floor capacity originals must start on deployment turn.");
                OriginalFloorCapacityRelicScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && PyreResourceRefreshScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Pyre resource refresh must start on deployment turn.");
                PyreResourceRefreshScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalMoonPhaseEnergyRelicScenario.Enabled)
            {
                OriginalMoonPhaseEnergyRelicScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalCardAttackRelicScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Card-attack relic must start on deployment turn.");
                OriginalCardAttackRelicScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalCardTypeCostScenario.Enabled)
            {
                OriginalCardTypeCostScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalPurgeToExhaustScenario.Enabled)
            {
                OriginalPurgeToExhaustScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalUpgradeCountRelicsScenario.Enabled)
            {
                OriginalUpgradeCountRelicsScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalHealingPowerScenario.Enabled)
            {
                OriginalHealingPowerScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalStartCombatEnergyScenario.Enabled)
            {
                OriginalStartCombatEnergyScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalStartCombatHoardScenario.Enabled)
            {
                OriginalStartCombatHoardScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalOpeningCorruptionScenario.Enabled)
            {
                OriginalOpeningCorruptionScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalSpellPlayThresholdScenario.Enabled)
            {
                OriginalSpellPlayThresholdScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalEquipmentGrowthScenario.Enabled)
            {
                OriginalEquipmentGrowthScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalHealthChoiceScenario.Enabled)
            {
                OriginalHealthChoiceScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalAfterDrawGenerationScenario.Enabled)
            {
                OriginalAfterDrawGenerationScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalAddSnackOnKillScenario.Enabled)
            {
                OriginalAddSnackOnKillScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalFinalEndingArtifactScenario.Enabled)
            {
                OriginalFinalEndingArtifactScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalBalatroEventYorickScenario.Enabled)
            {
                OriginalBalatroEventYorickScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalBalatroEventCavendishScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Cavendish multiplier fixture must start on deployment turn.");
                OriginalBalatroEventCavendishScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalGarfieldBoxRandomCardScenario.Enabled)
            {
                OriginalGarfieldBoxRandomCardScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalRelicCountAttackScenario.Enabled)
            {
                OriginalRelicCountAttackScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalBigDamageScenario.Enabled)
            {
                OriginalBigDamageScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalEarlyDiscardEnergyScenario.Enabled)
            {
                OriginalEarlyDiscardEnergyScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalMoreSpikesScenario.Enabled)
            {
                OriginalMoreSpikesScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalStealthSlayScenario.Enabled)
            {
                OriginalStealthSlayScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalUnitPhaseUpgradeScenario.Enabled)
            {
                OriginalUnitPhaseUpgradeScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalSoloUnitUpgradeScenario.Enabled)
            {
                OriginalSoloUnitUpgradeScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalFrontRoomHealthUpgradeScenario.Enabled)
            {
                OriginalFrontRoomHealthUpgradeScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalChampionStartTurnHealScenario.Enabled)
            {
                OriginalChampionStartTurnHealScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalPyreCardCostScenario.Enabled && combat!.GetTurnCount() >= 1)
            {
                OriginalPyreCardCostScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalUnitResourceScenario.Enabled)
            {
                OriginalUnitResourceScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalCardDrawOnHealScenario.Enabled)
            {
                OriginalCardDrawOnHealScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalBrokenRailScenario.Enabled)
            {
                OriginalBrokenRailScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalHephsBlueprintsScenario.Enabled)
            {
                OriginalHephsBlueprintsScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalRageOnBurnoutStackLossScenario.Enabled)
            {
                OriginalRageOnBurnoutStackLossScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalPyreRevengeScenario.Enabled)
            {
                OriginalPyreRevengeScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalTurnDrawScenario.Enabled)
            {
                OriginalTurnDrawScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalDeckTurnDrawScenario.Enabled)
            {
                OriginalDeckTurnDrawScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalFunguyDeckScalingScenario.Enabled)
            {
                OriginalFunguyDeckScalingScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalOpeningDrawScenario.Enabled)
            {
                OriginalOpeningDrawScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalConduitScenario.Enabled)
            {
                OriginalConduitScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalCombatTriggerBatchScenario.Enabled)
            {
                OriginalCombatTriggerBatchScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalForgeConservationScenario.Enabled)
            {
                if (OriginalForgeConservationScenario.Error != null) throw new InvalidOperationException(OriginalForgeConservationScenario.Error);
                if (!OriginalForgeConservationScenario.Prepared)
                {
                    if (!OriginalForgeConservationScenario.Started) OriginalForgeConservationScenario.Start(managers, log);
                    return;
                }
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalConserveEmberScenario.Enabled)
            {
                OriginalConserveEmberScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalResourceRelicBatchScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Resource relic batch must start on deployment turn.");
                OriginalResourceRelicBatchScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalUnitUpgradeTriggerRelicScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Unit-upgrade relic must start on deployment turn.");
                OriginalUnitUpgradeTriggerRelicScenario.Prepare(managers, log);
                numericModifiersPrepared = true;
                return;
            }
            if (fullBattle && !numericModifiersPrepared && TypedDrawProbe.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Typed draw fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                TypedDrawScenario.Prepare(managers, log);
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalRelicBatchScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original relic batch must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalRelicBatchScenario.Prepare(managers, log);
                return;
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "sacrifice-effects")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Sacrifice fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SacrificeScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "set-health")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("SetHealth fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SetHealthScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && SoldierOfFortuneScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Soldier fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SoldierOfFortuneScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && RestorationDetonationScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Restoration fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                RestorationDetonationScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && RunCardGenerationScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Run-card fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                RunCardGenerationScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && ArmamentDriveScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Armament fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                ArmamentDriveScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && PreserveScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Preserve fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                PreserveScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && MixalotScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Mixalot fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                MixalotScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && SmallBoxScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Small Box fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SmallBoxScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && MagneticRelayScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Magnetic Relay fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                MagneticRelayScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && TranscendImpScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Transcend Imp fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                TranscendImpScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && EquipmentMergeScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Equipment merge fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                EquipmentMergeScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && EclipseChamberScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Eclipse fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                EclipseChamberScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalRoomCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original room card fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalRoomCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalForgeCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original forge fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalForgeCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalStatusCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original status fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalStatusCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalDeckSpellScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original deck-spell fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalDeckSpellScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalScaledBuffScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original scaled buff fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalScaledBuffScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalPileRecursionScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original pile recursion fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalPileRecursionScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalPileDrawScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original pile draw fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalPileDrawScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalEffectApplicationsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original effect applications fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalEffectApplicationsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalStatusRemovalScalingScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original status-removal scaling fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalStatusRemovalScalingScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalScaledEnergyScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original scaled energy fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalScaledEnergyScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalSpecificCardDamageScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original specific-card damage fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalSpecificCardDamageScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalStatusStackDamageScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original status-stack damage fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalStatusStackDamageScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalCorruptRestrictedScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original Corrupt Restricted fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalCorruptRestrictedScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalOverflowCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original overflow fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalOverflowCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalPermafrostCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original Permafrost fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalPermafrostCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalUnplayableCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original Unplayable fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalUnplayableCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalMonsterHandCostsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original Monster hand cost fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalMonsterHandCostsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalLimitedRangeScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original Limited Range fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalLimitedRangeScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalHandCostsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original hand cost fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalHandCostsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalUnpurgableCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original Unpurgable fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalUnpurgableCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalRoomStatusDamageScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original room-status damage fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalRoomStatusDamageScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalCorruptionDamageScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original corruption-damage fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalCorruptionDamageScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalXStatusCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original X-status fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalXStatusCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalXDamageCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original X-damage fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalXDamageCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalStrongMagicCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original strong-magic fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalStrongMagicCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalConsumeCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original Consume fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalConsumeCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && InfernoRoomScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Inferno Room fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                InfernoRoomScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalPiercingCardsScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original piercing cards fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalPiercingCardsScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && MushyRoomScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Mushy Room fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                MushyRoomScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && GrowRoomScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Grow Room fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                GrowRoomScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && PlanetariumScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Planetarium fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                PlanetariumScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && MemorialScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Memorial fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                MemorialScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && ZoneOfSilenceScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Zone of Silence fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                ZoneOfSilenceScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && StewardsQuartersScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Stewards Quarters fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                StewardsQuartersScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && EngineUpgradeScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Engine Upgrade fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                EngineUpgradeScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && GoldmanceScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Goldmance fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                GoldmanceScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && AdaptiveMutationScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Adaptive Mutation fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                AdaptiveMutationScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && CardDuplicateScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Duplication fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                CardDuplicateScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "damage-by-units-killed")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Damage-by-units-killed fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                DamageByUnitsKilledScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == TransferAttackScenario.Name)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Transfer-attack fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                TransferAttackScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == BrainTransferScenario.Name)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("BrainTransfer fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                BrainTransferScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == FrostChannelingScenario.Name)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("FrostChanneling fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                FrostChannelingScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario is "card-effect-kill" or "card-effect-kill-subtype")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("CardEffectKill fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                CardEffectKillScenario.Prepare(managers, log, modifierScenario == "card-effect-kill-subtype");
            }
            if (fullBattle && !numericModifiersPrepared && NullForwardingScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Cleave fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                NullForwardingScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && NullTriggerScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Null-trigger fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                NullTriggerScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && NullTargetlessScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Null-targetless fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                NullTargetlessScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && TrampleScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Trample fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                TrampleScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && SpellShieldScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Spell Shield fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SpellShieldScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "remove-all-status-effects")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Remove-all status fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                CardEffectRemoveAllStatusScenario.Prepare(log);
            }
            if (fullBattle && !numericModifiersPrepared && DragonHoardScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Dragon's Hoard fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                DragonHoardScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && TrainHealScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Train heal fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                TrainHealScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OverhealScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Overheal fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OverhealScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && CorruptionScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Corruption fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                CorruptionScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && PermanentCorruptionScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Permanent corruption fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                PermanentCorruptionScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && MoonPhaseCardScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Moonrise fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                MoonPhaseCardScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && MoonPhaseAdvanceScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Moon-phase advance fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                MoonPhaseAdvanceScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && MoonPhaseCallbackScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Moon-phase callbacks fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                MoonPhaseCallbackScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalMoonPhaseHealRelicScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Moon-phase heal relic fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalMoonPhaseHealRelicScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalMoonPhaseStatusRelicScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Moon-phase status relic fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalMoonPhaseStatusRelicScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && ConditionalNoPlayerUnitScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Conditional status fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                ConditionalNoPlayerUnitScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && SelfAttackStatusScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Self-attack status fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SelfAttackStatusScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && ConditionalPyreDamageScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Conditional Pyre fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                ConditionalPyreDamageScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && SoulSiphonScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Soul Siphon fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SoulSiphonScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && StatusUpgradeScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Status upgrade fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                StatusUpgradeScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario is "card-effect-recursion" or "card-effect-recursion-choice" or "card-effect-recursion-choose-to-hand" or "card-effect-recursion-filter-choice" or "card-effect-recursion-discard-choice" or "card-effect-recursion-upgrade" or "card-effect-recursion-upgrade-hand" or "card-effect-recursion-upgrade-choice" or "card-effect-recursion-upgrade-filter-choice" or "card-effect-recursion-upgrade-replacement-choice")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Card recursion fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                CardEffectRecursionScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && MixScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Mix fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                MixScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalTribouletScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Triboulet fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalTribouletScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && OriginalCooldownOnDamageScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("CooldownOnDamage fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                OriginalCooldownOnDamageScenario.Prepare(managers, log);
            }
            if (fullBattle && OriginalCardPlayDamageScenario.Enabled && !OriginalCardPlayDamageScenario.Prepared)
            {
                if (OriginalCardPlayDamageScenario.Error != null) throw new InvalidOperationException(OriginalCardPlayDamageScenario.Error);
                if (!OriginalCardPlayDamageScenario.Started)
                {
                    numericModifiersPrepared = true;
                    OriginalCardPlayDamageScenario.Start(managers, log);
                }
                return;
            }
            if (fullBattle && OriginalConsumeDamageScenario.Enabled && !OriginalConsumeDamageScenario.Prepared)
            {
                if (OriginalConsumeDamageScenario.Error != null) throw new InvalidOperationException(OriginalConsumeDamageScenario.Error);
                if (!OriginalConsumeDamageScenario.Started)
                {
                    numericModifiersPrepared = true;
                    OriginalConsumeDamageScenario.Start(managers, log);
                }
                return;
            }
            if (fullBattle && OriginalKillAttackScenario.Enabled && !OriginalKillAttackScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalKillAttackScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalSpawnSilenceScenario.Enabled && !OriginalSpawnSilenceScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalSpawnSilenceScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalFreezeReduceCostScenario.Enabled && !OriginalFreezeReduceCostScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalFreezeReduceCostScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalRandomFreezeScenario.Enabled && !OriginalRandomFreezeScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalRandomFreezeScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalRandomHandUpgradeScenario.Enabled && !OriginalRandomHandUpgradeScenario.CombatPrepared)
            {
                numericModifiersPrepared = true;
                OriginalRandomHandUpgradeScenario.Prepare(managers);
            }
            if (fullBattle && OriginalStartTurnPropagationScenario.Enabled && !OriginalStartTurnPropagationScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalStartTurnPropagationScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalMooncycleFaeScenario.Enabled && !OriginalMooncycleFaeScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalMooncycleFaeScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalRandomDrawCostScenario.Enabled && !OriginalRandomDrawCostScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalRandomDrawCostScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalSuppressExhaustScenario.Enabled && !OriginalSuppressExhaustScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalSuppressExhaustScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalWolosBloodletterScenario.Enabled && !OriginalWolosBloodletterScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalWolosBloodletterScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalCardTypeSequenceScenario.Enabled && !OriginalCardTypeSequenceScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalCardTypeSequenceScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalWhelpCooldownScenario.Enabled && !OriginalWhelpCooldownScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalWhelpCooldownScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalSpawnRetreatScenario.Enabled && !OriginalSpawnRetreatScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalSpawnRetreatScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalOpeningMorselsScenario.Enabled && !OriginalOpeningMorselsScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalOpeningMorselsScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalPerkeoScenario.Enabled && !OriginalPerkeoScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalPerkeoScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalCorruptedAngelsScenario.Enabled && !OriginalCorruptedAngelsScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalCorruptedAngelsScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalOpeningDeckSpawnScenario.Enabled && !OriginalOpeningDeckSpawnScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalOpeningDeckSpawnScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalDecayDeathScenario.Enabled && !OriginalDecayDeathScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalDecayDeathScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalBoneDogScenario.Enabled && !OriginalBoneDogScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalBoneDogScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalGrosMichelScenario.Enabled && !OriginalGrosMichelScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalGrosMichelScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalExpensiveCardUpgradeScenario.Enabled && !OriginalExpensiveCardUpgradeScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalExpensiveCardUpgradeScenario.PrepareDeployment(managers);
            }
            if (fullBattle && OriginalHealDamageScenario.Enabled && (!OriginalHealDamageScenario.Prepared ||
                combat!.GetTurnCount() > 0 && !OriginalHealDamageScenario.CombatPrepared))
            {
                if (OriginalHealDamageScenario.Error != null) throw new InvalidOperationException(OriginalHealDamageScenario.Error);
                numericModifiersPrepared = true;
                if (!OriginalHealDamageScenario.Prepared) OriginalHealDamageScenario.PrepareDeployment(managers);
                else if (!OriginalHealDamageScenario.Started)
                {
                    FullBattleTrace.Active!.CompleteEndTurn(RoomOutcome.Exchanged);
                    OriginalHealDamageScenario.Start(managers, log);
                }
                return;
            }
            if (fullBattle && OriginalBurnoutDamageScenario.Enabled && (!OriginalBurnoutDamageScenario.Prepared ||
                combat!.GetTurnCount() > 0 && !OriginalBurnoutDamageScenario.CombatPrepared))
            {
                if (OriginalBurnoutDamageScenario.Error != null) throw new InvalidOperationException(OriginalBurnoutDamageScenario.Error);
                numericModifiersPrepared = true;
                if (!OriginalBurnoutDamageScenario.Prepared) OriginalBurnoutDamageScenario.PrepareDeployment(managers);
                else if (!OriginalBurnoutDamageScenario.Started)
                {
                    FullBattleTrace.Active!.CompleteEndTurn(RoomOutcome.Exchanged);
                    OriginalBurnoutDamageScenario.Start(managers, log);
                }
                return;
            }
            if (fullBattle && OriginalMoreCapacityScenario.Enabled && !OriginalMoreCapacityScenario.Prepared)
            {
                numericModifiersPrepared = true;
                OriginalMoreCapacityScenario.Prepare(managers, log);
                return;
            }
            if (fullBattle && OriginalEatenDamageScenario.Enabled && (!OriginalEatenDamageScenario.Prepared ||
                combat!.GetTurnCount() > 0 && !OriginalEatenDamageScenario.CombatPrepared))
            {
                if (OriginalEatenDamageScenario.Error != null) throw new InvalidOperationException(OriginalEatenDamageScenario.Error);
                if (!OriginalEatenDamageScenario.Prepared)
                {
                    numericModifiersPrepared = true;
                    OriginalEatenDamageScenario.PrepareDeployment(managers);
                    return;
                }
                if (!OriginalEatenDamageScenario.Started)
                {
                    numericModifiersPrepared = true;
                    // Freeze the completed native EndTurn before authoring the
                    // next decision's enemy and trigger-card carriers.
                    FullBattleTrace.Active!.CompleteEndTurn(RoomOutcome.Exchanged);
                    OriginalEatenDamageScenario.Start(managers, log);
                }
                return;
            }
            if (fullBattle && !numericModifiersPrepared && ChooseDiscardScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Choose-discard fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                ChooseDiscardScenario.Prepare(managers, log);
            }
            if (fullBattle && Environment.GetEnvironmentVariable("MT2_PROBE_ENCHANTMENT_COMBAT") == "1")
            {
                if (EnchantmentCombatCalibration.Error != null) throw new InvalidOperationException(EnchantmentCombatCalibration.Error);
                if (!EnchantmentCombatCalibration.Completed && (EnchantmentCombatCalibration.Started || EnchantmentCombatCalibration.Eligible))
                {
                    if (!EnchantmentCombatCalibration.Started) EnchantmentCombatCalibration.Start(log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario.StartsWith("triggered-summon", StringComparison.Ordinal) &&
                TriggeredSummonEquipmentScenario.Enabled)
            {
                if (TriggeredSummonEquipmentScenario.Error != null) throw new InvalidOperationException(TriggeredSummonEquipmentScenario.Error);
                if (!TriggeredSummonEquipmentScenario.Completed)
                {
                    if (!TriggeredSummonEquipmentScenario.Started) TriggeredSummonEquipmentScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && modifierScenario == "conditional-triggers")
            {
                if (ConditionalTriggerScenario.Error != null) throw new InvalidOperationException(ConditionalTriggerScenario.Error);
                if (!ConditionalTriggerScenario.Completed)
                {
                    if (!ConditionalTriggerScenario.Started) ConditionalTriggerScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && (modifierScenario == "trigger-mutation" || modifierScenario == "detached-bonus-draw"))
            {
                if (TriggerMutationScenario.Error != null) throw new InvalidOperationException(TriggerMutationScenario.Error);
                if (!TriggerMutationScenario.Completed)
                {
                    if (!TriggerMutationScenario.Started) TriggerMutationScenario.Start(managers, log, modifierScenario == "detached-bonus-draw");
                    return;
                }
            }
            if (fullBattle && (modifierScenario == "equipment-abilities" || modifierScenario == "equipment" || modifierScenario == "equipment-exhausted" || modifierScenario == "equipment-overflow" || modifierScenario == "equipment-triggers" || modifierScenario == "equipment-buff"))
            {
                if (EquipmentScenario.Error != null) throw new InvalidOperationException(EquipmentScenario.Error);
                if (!EquipmentScenario.Completed)
                {
                    if (!EquipmentScenario.Started) EquipmentScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && modifierScenario == "direct-unit-upgrades")
            {
                if (DirectUnitUpgradeScenario.Error != null) throw new InvalidOperationException(DirectUnitUpgradeScenario.Error);
                if (!DirectUnitUpgradeScenario.Completed)
                {
                    if (!DirectUnitUpgradeScenario.Started) DirectUnitUpgradeScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "spawn-points")
            {
                if (SpawnPointScenario.Error != null) throw new InvalidOperationException(SpawnPointScenario.Error);
                if (!SpawnPointScenario.Completed)
                {
                    if (!SpawnPointScenario.Started) SpawnPointScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "multi-summon-zero")
            {
                if (MultiSummonScenario.Error != null) throw new InvalidOperationException(MultiSummonScenario.Error);
                if (!MultiSummonScenario.ZeroCompleted)
                {
                    if (!MultiSummonScenario.ZeroStarted) MultiSummonScenario.StartZero(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "rally-triggers")
            {
                if (RallyScenario.Error != null) throw new InvalidOperationException(RallyScenario.Error);
                if (!RallyScenario.Completed)
                {
                    if (!RallyScenario.Started) RallyScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "dying-horde-upgrades")
            {
                if (DyingHordeUpgradeScenario.Error != null) throw new InvalidOperationException(DyingHordeUpgradeScenario.Error);
                if (!DyingHordeUpgradeScenario.Completed)
                {
                    if (!DyingHordeUpgradeScenario.Started) DyingHordeUpgradeScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && UnitCloneScenario.Enabled)
            {
                if (UnitCloneScenario.Error != null) throw new InvalidOperationException(UnitCloneScenario.Error);
                if (!UnitCloneScenario.Completed)
                {
                    if (!UnitCloneScenario.Started) UnitCloneScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "hero-copy")
            {
                if (HeroCopyScenario.Error != null) throw new InvalidOperationException(HeroCopyScenario.Error);
                if (!HeroCopyScenario.Completed)
                {
                    if (!HeroCopyScenario.Started) HeroCopyScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "unit-copy")
            {
                if (UnitCopyScenario.Error != null) throw new InvalidOperationException(UnitCopyScenario.Error);
                if (!UnitCopyScenario.Completed)
                {
                    if (!UnitCopyScenario.Started) UnitCopyScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "bump")
            {
                if (BumpScenario.Error != null) throw new InvalidOperationException(BumpScenario.Error);
                if (!BumpScenario.Completed)
                {
                    if (!BumpScenario.Started) BumpScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "horde-merge")
            {
                if (HordeMergeScenario.Error != null) throw new InvalidOperationException(HordeMergeScenario.Error);
                if (!HordeMergeScenario.Completed)
                {
                    if (!HordeMergeScenario.Started) HordeMergeScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "horde-upgrades")
            {
                if (HordeUpgradeScenario.Error != null) throw new InvalidOperationException(HordeUpgradeScenario.Error);
                if (!HordeUpgradeScenario.Completed)
                {
                    if (!HordeUpgradeScenario.Started) HordeUpgradeScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "horde-death")
            {
                if (HordeDeathScenario.Error != null) throw new InvalidOperationException(HordeDeathScenario.Error);
                if (!HordeDeathScenario.Completed)
                {
                    if (!HordeDeathScenario.Started) HordeDeathScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "horde-removal")
            {
                if (HordeRemovalScenario.Error != null) throw new InvalidOperationException(HordeRemovalScenario.Error);
                if (!HordeRemovalScenario.Completed)
                {
                    if (!HordeRemovalScenario.Started) HordeRemovalScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "revival")
            {
                if (RevivalScenario.Error != null) throw new InvalidOperationException(RevivalScenario.Error);
                if (!RevivalScenario.Completed)
                {
                    if (!RevivalScenario.Started) RevivalScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "harvest-triggers")
            {
                if (HarvestScenario.Error != null) throw new InvalidOperationException(HarvestScenario.Error);
                if (!HarvestScenario.Completed)
                {
                    if (!HarvestScenario.Started) HarvestScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "horde-statuses")
            {
                if (HordeStatusScenario.Error != null) throw new InvalidOperationException(HordeStatusScenario.Error);
                if (!HordeStatusScenario.Completed)
                {
                    if (!HordeStatusScenario.Started) HordeStatusScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && numericModifiersPrepared && modifierScenario == "ability-lifecycle")
            {
                if (AbilityLifecycleScenario.Error != null) throw new InvalidOperationException(AbilityLifecycleScenario.Error);
                if (!AbilityLifecycleScenario.Completed)
                {
                    if (!AbilityLifecycleScenario.Started) AbilityLifecycleScenario.Start(managers, log);
                    return;
                }
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "spawn-or-status")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Spawn-or-status fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                AbilityActivationScenario.Prepare(managers, log, false, false, spawnOrStatus: true);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "ability-teleport")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Unit teleport fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                AbilityTeleportScenario.Prepare(managers, log);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "damage-per-hp-ability")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("DamagePerHp ability fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                AbilityActivationScenario.Prepare(managers, log, false, false, damagePerHp: true);
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "transfer-attack-ability")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Transfer ability fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                AbilityActivationScenario.Prepare(managers, log, false, false, transferAttack: true);
            }
            if (fullBattle && !numericModifiersPrepared &&
                (modifierScenario == DamagePerHpScenario.Name || modifierScenario == DamagePerHpScenario.ArmorName ||
                 modifierScenario == DamagePerHpScenario.LastAttackerName))
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("DamagePerHp fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                DamagePerHpScenario.Prepare(managers, log, modifierScenario);
                return;
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == DamageOverflowScenario.Name)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Damage overflow fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                DamageOverflowScenario.Prepare(managers, log);
                return;
            }
            if (fullBattle && !numericModifiersPrepared && (modifierScenario == "spawn-status-relics-tuned-gear" ||
                modifierScenario == "spawn-status-relics-combat" || modifierScenario == "spawn-status-relics-egg-hatch" ||
                modifierScenario == "spawn-status-relics-egg-transformation"))
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Spawn-status relic fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SpawnStatusRelicScenario.Prepare(managers, log);
                return;
            }
            if (fullBattle && !numericModifiersPrepared && OriginalEggCardScenario.Enabled)
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Original Egg card must be prepared on the deployment turn.");
                numericModifiersPrepared = true;
                OriginalEggCardScenario.Prepare(managers, log);
                return;
            }
            if (fullBattle && !numericModifiersPrepared && modifierScenario == "spawn-status-relics-boss-mute")
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Spawn-status relic fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                SpawnStatusRelicScenario.Prepare(managers, log);
                return;
            }
            if (fullBattle && !numericModifiersPrepared && ((modifierScenario == "spawn-status-relics" || modifierScenario == "spawn-status-relics-clones" || modifierScenario == "spawn-status-relics-mixed-cardless-order" || modifierScenario == "spawn-status-relics-stealth-waxers" || modifierScenario == "spawn-status-relics-team-wide" || modifierScenario == "incant" || modifierScenario == "incant-thresholds" || (modifierScenario == "incant-relic" || modifierScenario == "incant-relic-combined") || modifierScenario == "ability-incant") || modifierScenario == "purify" || modifierScenario == "purify-queues" || EnchantmentBattleScenario.IsScenario(modifierScenario) || modifierScenario == "spawn-enchant" || modifierScenario == "hero-copy" || modifierScenario == "unit-copy" || modifierScenario == "unit-clone" || modifierScenario == "bump" || modifierScenario == "horde-merge" || modifierScenario == "revival" || modifierScenario.StartsWith("triggered-summon", StringComparison.Ordinal) || modifierScenario == "spawn-points" || (modifierScenario.StartsWith("multi-summon", StringComparison.Ordinal)) || modifierScenario == "rally-lethal" || modifierScenario == "rally-triggers" || modifierScenario == "dying-horde-upgrades" || modifierScenario == "horde-upgrades" || modifierScenario == "horde-death" || modifierScenario == "horde-removal" || modifierScenario == "harvest-triggers" || modifierScenario == "horde-statuses" || modifierScenario == "ability-effects" || modifierScenario == "ability-lifecycle" || modifierScenario == "ability-activation" || modifierScenario == "ability-activation-x" || modifierScenario == "ability-activation-lethal" || modifierScenario == "ability-cooldown" || modifierScenario == "ability-cache" || modifierScenario == "sentry" || modifierScenario == "sentry-lethal" || modifierScenario == "companion-boss" || modifierScenario == "numeric-upgrades" || modifierScenario == "dynamic-upgrades" ||
                modifierScenario == "sacrifice-upgrades" || modifierScenario == "hand-upgrades" || modifierScenario == "targeted-hand-upgrades" || modifierScenario == "permanent-consume-hand-upgrades" || modifierScenario == "permanent-consume-over-temporary" ||
                modifierScenario == "healing" || modifierScenario == "healing-triggers" || modifierScenario == "room-spells" ||
                modifierScenario == "terminal-spells" || modifierScenario == "post-kill-spells" || modifierScenario == "random-spells" ||
                modifierScenario == "random-status" || modifierScenario == "cross-room-spells" || modifierScenario == "cross-room-targets" || modifierScenario == "attack-buffs" ||
                 modifierScenario == "room-capacity" || modifierScenario == "room-capacity-lethal" || modifierScenario == "energy-effects" || modifierScenario == "energy-effects-lethal" || modifierScenario == "forge-points" || modifierScenario == "bonus-draw" || modifierScenario == "bonus-draw-lethal" || modifierScenario == "x-cost" || modifierScenario == "x-cost-lethal" || modifierScenario == "max-health-spells" || modifierScenario == "max-health-lethal" ||
                modifierScenario == "numeric-ranges" || modifierScenario == "numeric-ranges-lethal" || modifierScenario == "target-filters" || modifierScenario == "drawing" || modifierScenario == "damage-scaling" || modifierScenario == "dynamic-statistics" ||
                modifierScenario == "hand-removal" || modifierScenario == "hand-removal-lethal" || modifierScenario == "random-discard" || modifierScenario == "generation" || modifierScenario == "generation-lethal" || modifierScenario == "generation-permanent-consume" ||
                (modifierScenario == "conditional-relic-card-upgrades" && Environment.GetEnvironmentVariable("MT2_PROBE_GENERATION") == "1") ||
                (modifierScenario == "relic-card-status-upgrades" && Environment.GetEnvironmentVariable("MT2_PROBE_GENERATION") == "1") ||
                modifierScenario == "status-scaling" || modifierScenario == "unit-upgrade-scaling" || modifierScenario == "unit-trigger-upgrades" ||
                modifierScenario == "spawn-triggers" || modifierScenario == "spawn-triggers-lethal" || modifierScenario == "unit-turn-begin" || modifierScenario == "team-turn-begin" || modifierScenario == "pre-hand-discard" || modifierScenario == "pre-hand-discard-lethal" || modifierScenario == "clone-upgrade-refresh" || modifierScenario == "pre-combat" || modifierScenario == "pre-combat-lethal" || modifierScenario == "triggered-healing" || modifierScenario == "post-combat-healing" || modifierScenario == "triggered-damage" || modifierScenario == "damage-death-queue" || modifierScenario == "terminal-death-damage" || modifierScenario == "hit-kill" || modifierScenario == "dying-upgrades" || modifierScenario == "attack-triggers" || modifierScenario == "triggered-status"))
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Numeric fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                if (modifierScenario == "generation" || modifierScenario == "generation-lethal" || modifierScenario == "generation-permanent-consume" ||
                    ((modifierScenario == "conditional-relic-card-upgrades" || modifierScenario == "relic-card-status-upgrades") &&
                        Environment.GetEnvironmentVariable("MT2_PROBE_GENERATION") == "1"))
                    GenerationScenario.Prepare(managers, log, modifierScenario == "generation-lethal",
                        modifierScenario == "generation-permanent-consume");
                else if (modifierScenario == "hand-removal" || modifierScenario == "hand-removal-lethal")
                    HandRemovalScenario.Prepare(managers, log, modifierScenario == "hand-removal-lethal");
                else if (modifierScenario == "random-discard")
                    RandomDiscardScenario.Prepare(managers, log);
                else if (modifierScenario == "energy-effects" || modifierScenario == "energy-effects-lethal") EnergyScenario.Prepare(managers, log, modifierScenario == "energy-effects-lethal");
                else if (modifierScenario == "forge-points") ForgePointsScenario.Prepare(managers, log);
                else if (modifierScenario == "room-capacity" || modifierScenario == "room-capacity-lethal") RoomCapacityScenario.Prepare(managers, log, modifierScenario == "room-capacity-lethal");
                else if (modifierScenario == "x-cost" || modifierScenario == "x-cost-lethal") CardCostScenario.Prepare(managers, log, modifierScenario == "x-cost-lethal");
                else if (modifierScenario == "bonus-draw" || modifierScenario == "bonus-draw-lethal") BonusDrawScenario.Prepare(managers, log, modifierScenario == "bonus-draw-lethal");
                else if (modifierScenario == "ability-activation" || modifierScenario == "ability-activation-x" || modifierScenario == "ability-activation-lethal")
                    AbilityActivationScenario.Prepare(managers, log, modifierScenario == "ability-activation-x",
                        modifierScenario == "ability-activation-lethal", sourceAttack: modifierScenario == "ability-activation",
                        attackWithUnit: modifierScenario == "ability-activation");
                else if (modifierScenario.StartsWith("triggered-summon", StringComparison.Ordinal)) TriggeredSummonScenario.Prepare(managers, log);
                else if (modifierScenario == "spawn-points") SpawnPointScenario.Prepare(managers, log);
                else if (modifierScenario == "rally-triggers") RallyScenario.Prepare(managers, log);
                else if (modifierScenario == "rally-lethal") LethalRallyScenario.Prepare(managers, log);
                else if (modifierScenario.StartsWith("multi-summon", StringComparison.Ordinal)) MultiSummonScenario.Prepare(managers, log);
                else if (modifierScenario == "dying-horde-upgrades") DyingHordeUpgradeScenario.Prepare(managers, log);
                else if (modifierScenario == "unit-clone") UnitCloneScenario.Prepare(managers, log);
                else if (modifierScenario == "unit-copy") UnitCopyScenario.Prepare(managers, log);
                else if (modifierScenario == "hero-copy") HeroCopyScenario.Prepare(managers, log);
                else if (modifierScenario == "spawn-enchant") SpawnEnchantScenario.Prepare(managers, log);
                else if (modifierScenario == "spawn-status-relics" || modifierScenario == "spawn-status-relics-clones" || modifierScenario == "spawn-status-relics-mixed-cardless-order" || modifierScenario == "spawn-status-relics-stealth-waxers" || modifierScenario == "spawn-status-relics-team-wide") SpawnStatusRelicScenario.Prepare(managers, log);
                else if (modifierScenario == "incant-relic" || modifierScenario == "incant-relic-combined") TriggerCountScenario.Prepare(managers, log);
                else if (modifierScenario == "incant" || modifierScenario == "incant-thresholds") IncantScenario.Prepare(managers, log);
                else if (modifierScenario == "ability-incant") AbilityIncantScenario.Prepare(managers, log);
                else if (modifierScenario is "purify" or "purify-queues") PurifyScenario.Prepare(managers, log);
                else if (EnchantmentBattleScenario.IsScenario(modifierScenario)) EnchantmentBattleScenario.Prepare(managers, log);
                else if (modifierScenario == "bump") BumpScenario.Prepare(managers, log);
                else if (modifierScenario == "horde-merge") HordeMergeScenario.Prepare(managers, log);
                else if (modifierScenario == "horde-upgrades") HordeUpgradeScenario.Prepare(managers, log);
                else if (modifierScenario == "horde-death") HordeDeathScenario.Prepare(managers, log);
                else if (modifierScenario == "horde-removal") HordeRemovalScenario.Prepare(managers, log);
                else if (modifierScenario == "revival") RevivalScenario.Prepare(managers, log);
                else if (modifierScenario == "harvest-triggers") HarvestScenario.Prepare(managers, log);
                else if (modifierScenario == "horde-statuses") HordeStatusScenario.Prepare(managers, log);
                else if (modifierScenario == "ability-effects") AbilityEffectsScenario.Prepare(managers, log);
                else if (modifierScenario == "ability-lifecycle") AbilityLifecycleScenario.Prepare(managers, log);
                else if (modifierScenario == "ability-cooldown" || modifierScenario == "ability-cache") AbilityCooldownScenario.Prepare(managers, log, modifierScenario == "ability-cache");
                else if (modifierScenario == "companion-boss") CompanionBossScenario.Prepare(managers, log);
                else if (modifierScenario == "sentry" || modifierScenario == "sentry-lethal") SentryScenario.Prepare(managers, log, modifierScenario == "sentry-lethal");
                else if (modifierScenario == "damage-scaling") DamageScalingScenario.Prepare(managers, log);
                else if (modifierScenario == "dynamic-statistics") DamageScalingScenario.Prepare(managers, log, true);
                else if (modifierScenario == "status-scaling") StatusScalingScenario.Prepare(managers, log);
                else if (modifierScenario == "unit-upgrade-scaling") UnitUpgradeScalingScenario.Prepare(managers, log);
                else if (modifierScenario == "unit-trigger-upgrades") UnitTriggerUpgradeScenario.Prepare(managers, log);
                else if (modifierScenario == "unit-turn-begin") UnitTurnBeginScenario.Prepare(managers, log);
                else if (modifierScenario == "team-turn-begin") TeamTurnBeginScenario.Prepare(managers, log);
                else if (modifierScenario == "triggered-status") TriggeredStatusScenario.Prepare(managers, log);
                else if (modifierScenario == "attack-triggers") AttackTriggerScenario.Prepare(managers, log);
                else if (modifierScenario == "dying-upgrades") DyingUpgradeScenario.Prepare(managers, log);
                else if (modifierScenario == "hit-kill") HitKillScenario.Prepare(managers, log);
                else if (modifierScenario == "terminal-death-damage") DamageDeathQueueScenario.Prepare(managers, log, true);
                else if (modifierScenario == "damage-death-queue") DamageDeathQueueScenario.Prepare(managers, log);
                else if (modifierScenario == "triggered-damage") TriggeredDamageScenario.Prepare(managers, log);
                else if (modifierScenario == "post-combat-healing") PostCombatHealingScenario.Prepare(managers, log);
                else if (modifierScenario == "triggered-healing") TriggeredHealingScenario.Prepare(managers, log);
                else if (modifierScenario is "pre-combat" or "pre-combat-lethal")
                    PreCombatScenario.Prepare(managers, log, modifierScenario == "pre-combat-lethal");
                else if (modifierScenario == "clone-upgrade-refresh") PreHandDiscardScenario.Prepare(managers, log, true, true);
                else if (modifierScenario == "pre-hand-discard" || modifierScenario == "pre-hand-discard-lethal")
                    PreHandDiscardScenario.Prepare(managers, log, modifierScenario == "pre-hand-discard-lethal");
                else if (modifierScenario == "spawn-triggers" || modifierScenario == "spawn-triggers-lethal")
                    SpawnTriggerScenario.Prepare(managers, log, modifierScenario == "spawn-triggers-lethal");
                else if (modifierScenario == "drawing") DrawScenario.Prepare(managers, log);
                else if (modifierScenario == "target-filters") TargetFilterScenario.Prepare(managers, log);
                else if (modifierScenario == "numeric-ranges" || modifierScenario == "numeric-ranges-lethal")
                    NumericRangeScenario.Prepare(managers, log, modifierScenario == "numeric-ranges-lethal");
                else if (modifierScenario == "max-health-spells" || modifierScenario == "max-health-lethal")
                    MaxHealthScenario.Prepare(managers, log, modifierScenario == "max-health-lethal");
                else if (modifierScenario == "attack-buffs") AttackBuffScenario.Prepare(managers, log);
                else if (modifierScenario == "cross-room-spells" || modifierScenario == "cross-room-targets")
                    CrossRoomSpellScenario.Prepare(managers, log, modifierScenario == "cross-room-spells");
                else if (modifierScenario == "random-status") RandomStatusScenario.Prepare(managers, log);
                else if (modifierScenario == "random-spells") RandomSpellScenario.Prepare(managers, log);
                else if (modifierScenario == "post-kill-spells") PostKillSpellScenario.Prepare(managers, log);
                else if (modifierScenario == "terminal-spells") TerminalSpellScenario.Prepare(managers, log);
                else if (modifierScenario == "room-spells") RoomSpellScenario.Prepare(managers, log);
                else if (modifierScenario == "healing" || modifierScenario == "healing-triggers")
                    HealingScenario.Prepare(managers, log, modifierScenario == "healing-triggers");
                else if (modifierScenario == "hand-upgrades" || modifierScenario == "targeted-hand-upgrades" || modifierScenario == "permanent-consume-hand-upgrades" || modifierScenario == "permanent-consume-over-temporary")
                    HandUpgradeScenario.Prepare(managers, log, modifierScenario == "targeted-hand-upgrades",
                        modifierScenario == "permanent-consume-hand-upgrades" || modifierScenario == "permanent-consume-over-temporary",
                        modifierScenario == "permanent-consume-over-temporary");
                else if (modifierScenario == "dynamic-upgrades" || modifierScenario == "sacrifice-upgrades")
                    DynamicUpgradeScenario.Prepare(managers, log, modifierScenario == "sacrifice-upgrades");
                else NumericUpgradeScenario.Prepare(managers, log);
                return;
            }
            if (fullBattle && RelicCardModifierProbe.Enabled && !OriginalCorruptedAngelsScenario.Enabled && !OriginalPerkeoScenario.Enabled && !OriginalOpeningDeckSpawnScenario.Enabled && !OriginalOpeningMorselsScenario.Enabled && !OriginalGrosMichelScenario.Enabled && !OriginalRelicBatchScenario.Enabled && !OriginalHealingPowerScenario.Enabled &&
                !OriginalCardAttackRelicScenario.Enabled && !OriginalCardTypeCostScenario.Enabled && !OriginalUpgradeCountRelicsScenario.Enabled &&
                !OriginalPurgeToExhaustScenario.Enabled && !OriginalExpensiveCardUpgradeScenario.Enabled && !OriginalWolosBloodletterScenario.Enabled &&
                !RelicCardModifierScenario.Prepared)
            {
                RelicCardModifierScenario.Prepare(managers, log);
                return;
            }
            if (pass == Pass.Source && sourceSignatures.Count == 0)
            {
                initialTurn = combat!.GetTurnCount();
                sourceBattleOffset = save.GetReplayData().GetNumEntriesUpUntilBattle();
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                save.MarkOneTimeMessageComplete(OneTimeMessage.Capacity);
                save.MarkOneTimeMessageComplete(OneTimeMessage.FirstMonsterPlayed);
                log.LogInfo("DEPTH-BATTLE scenario=" + save.GetCurrentScenarioData()?.name +
                    " testScenario=" + save.InTestScenario + " offset=" + sourceBattleOffset);
            }
            else if (pass == Pass.Direct && recordedTurn < 0)
            {
                initialTurn = combat!.GetTurnCount();
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                save.MarkOneTimeMessageComplete(OneTimeMessage.Capacity);
                save.MarkOneTimeMessageComplete(OneTimeMessage.FirstMonsterPlayed);
                log.LogInfo("DEPTH-BATTLE scenario=" + save.GetCurrentScenarioData()?.name +
                    " testScenario=" + save.InTestScenario);
            }
            int relativeTurn = combat!.GetTurnCount() - initialTurn;
            if (relativeTurn < 0 || relativeTurn > depth)
            {
                Finish(false, "Unexpected relative turn " + relativeTurn);
                return;
            }
            if (relativeTurn != recordedTurn)
            {
                string signature = Capture(pass.ToString().ToLowerInvariant() + "-" + relativeTurn,
                    managers, save, combat, cards!);
                recordedTurn = relativeTurn;
                if (fullBattle) FullBattleTrace.Active?.Checkpoint("decision-" + relativeTurn);
                if (pass == Pass.Source)
                {
                    sourceSignatures.Add(signature);
                    sourceReplayCounts.Add(save.GetReplayData().GetReplayEntries().Count);
                    log.LogInfo("DEPTH-CHECKPOINT turn=" + relativeTurn +
                        " replayCount=" + sourceReplayCounts[relativeTurn]);
                }
            }
            if (relativeTurn == depth && !fullBattle)
            {
                if (pass == Pass.Source)
                {
                    sourceEntries = new List<string>(save.GetReplayData().GetReplayEntries());
                    restartStarted = Time.realtimeSinceStartup;
                    save.RestartBattle();
                    log.LogInfo("DEPTH-RESTART-START entries=" + sourceEntries.Count);
                    Enter(Stage.Restarting, 180f);
                }
                else
                {
                    log.LogInfo("DEPTH-FINAL pyre=" + save.GetTowerHP() + '/' + save.GetMaxTowerHP() +
                        " turn=" + relativeTurn + " pass=" + pass);
                    Finish(true, "Depth " + depth + " branch at turn " + targetTurn + " completed.");
                }
                return;
            }
            if (IsUnitAndJunkPolicy())
            {
                if (fullBattle && MagebladeScenario.Enabled && !MagebladeScenario.CaptureReady)
                {
                    if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Mageblade fixture must start on deployment turn.");
                    MagebladeScenario.Prepare(managers, log);
                }
                if (fullBattle && OriginalCardPlayMagicPowerScenario.Enabled && !OriginalCardPlayMagicPowerScenario.Prepared)
                    OriginalCardPlayMagicPowerScenario.Prepare(managers, log);
                if (fullBattle && OriginalChanceForSpellWeaknessScenario.Enabled && !OriginalChanceForSpellWeaknessScenario.Prepared)
                    OriginalChanceForSpellWeaknessScenario.Prepare(managers, log);
                if (fullBattle && OriginalChanceToCleanseScenario.Enabled && !OriginalChanceToCleanseScenario.Prepared)
                    OriginalChanceToCleanseScenario.Prepare(managers, log);
                if (fullBattle && OriginalExtraFeedTriggerScenario.Enabled && !OriginalExtraFeedTriggerScenario.Prepared)
                    OriginalExtraFeedTriggerScenario.Prepare(managers, log);
                if (fullBattle && OriginalSummonWithAvariceScenario.Enabled && !OriginalSummonWithAvariceScenario.Prepared)
                    OriginalSummonWithAvariceScenario.Prepare(managers, log);
                if (fullBattle && OriginalExtractForEmberAndEchosScenario.Enabled && !OriginalExtractForEmberAndEchosScenario.Prepared)
                    OriginalExtractForEmberAndEchosScenario.Prepare(managers, log);
                if (fullBattle && OriginalCheaperSpellCostScenario.Enabled && !OriginalCheaperSpellCostScenario.Prepared)
                    OriginalCheaperSpellCostScenario.Prepare(managers, log);
                if (fullBattle && OriginalFirstCardCostScenario.Enabled && !OriginalFirstCardCostScenario.Prepared)
                    OriginalFirstCardCostScenario.Prepare(managers, log);
                if (fullBattle && OriginalNoUpgradeCardCostScenario.Enabled && !OriginalNoUpgradeCardCostScenario.Prepared)
                    OriginalNoUpgradeCardCostScenario.Prepare(managers, log);
                if (fullBattle && OriginalFirstImpBonusScenario.Enabled && !OriginalFirstImpBonusScenario.Prepared && combat!.GetTurnCount() > 0)
                    OriginalFirstImpBonusScenario.Prepare(managers, log);
                if (fullBattle && OriginalMoonPhaseRoomsGainMagicPowerScenario.Enabled && combat!.GetTurnCount() > 0)
                    OriginalMoonPhaseRoomsGainMagicPowerScenario.PrepareTurn(managers, log);
                if (fullBattle && OriginalRebalanceValorScenario.Enabled && !OriginalRebalanceValorScenario.Prepared)
                    OriginalRebalanceValorScenario.Prepare(managers, log);
                if (fullBattle && OriginalCardDrawOnCardPlayScenario.Enabled && !OriginalCardDrawOnCardPlayScenario.Prepared)
                    OriginalCardDrawOnCardPlayScenario.Prepare(managers, log);
                if (fullBattle && UnplayedCardTriggerScenario.Enabled && !UnplayedCardTriggerScenario.Prepared)
                    UnplayedCardTriggerScenario.Prepare(managers, log);
                if (fullBattle && UnplayedCardEffectsScenario.Enabled && !UnplayedCardEffectsScenario.Prepared)
                    UnplayedCardEffectsScenario.Prepare(managers, log);
                if (OriginalUnitRelicBatchScenario.Enabled) OriginalUnitRelicBatchScenario.PrepareDecision(managers);
                if (CorruptionScenario.RelicTrigger) CorruptionScenario.PrepareRelicDecision(managers, log);
                if (modifierScenario == "damage-per-hp-ability" && !damagePerHpAbilityInputPrepared)
                    damagePerHpAbilityInputPrepared = DamagePerHpScenario.SeedAbilityLastAttackerInput(managers, log);
                BattleTurnState decision = FullBattleTrace.Active!.CaptureDecision();
                if (fullBattle && RelicCardModifierScenario.CastTriggerUpgrade)
                    decision = RelicCardModifierScenario.PrepareCastTriggerCardInHand(decision, managers, cards!, log);
                RelicStatusStackProbe.Matrix();
                RelicStatusMagnitudeProbe.Matrix();
                RelicStatusDecayProbe.Matrix();
                if (OriginalBalatroEventYorickScenario.Enabled)
                {
                    pendingPlay = OriginalBalatroEventYorickScenario.Choose(decision);
                }
                else if (OriginalAddSnackOnKillScenario.Enabled)
                {
                    pendingPlay = OriginalAddSnackOnKillScenario.PolicyActionPlayed ? null :
                        Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY") == "units-and-junk"
                            ? BattleActionModel.ChooseUnitAndJunkPlay(decision,
                                card => !OriginalAddSnackOnKillProbe.IsGeneratedCard(card.InstanceId))
                            : BattleActionModel.ChooseUnitSpellAndJunkPlay(decision,
                                card => !OriginalAddSnackOnKillProbe.IsGeneratedCard(card.InstanceId));
                }
                else if (OriginalFinalEndingArtifactScenario.Enabled)
                {
                    pendingPlay = OriginalFinalEndingArtifactScenario.PolicyActionPlayed ? null :
                        Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY") == "units-and-junk"
                            ? BattleActionModel.ChooseUnitAndJunkPlay(decision,
                                card => !OriginalFinalEndingArtifactScenario.IsGeneratedCard(card.InstanceId))
                            : BattleActionModel.ChooseUnitSpellAndJunkPlay(decision,
                                card => !OriginalFinalEndingArtifactScenario.IsGeneratedCard(card.InstanceId));
                }
                else
                {
                    pendingPlay = modifierScenario == OriginalStingRelicBatchScenario.Name
                        ? OriginalStingRelicBatchScenario.Choose(decision)
                        : Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY") == "units-spells-and-junk"
                            ? BattleActionModel.ChooseUnitSpellAndJunkPlay(decision) : BattleActionModel.ChooseUnitAndJunkPlay(decision);
                }
                if (OriginalBigDamageScenario.Enabled)
                    pendingPlay = OriginalBigDamageScenario.Choose(decision, pendingPlay, log);
                if (OriginalEggCardScenario.Enabled)
                    pendingPlay = OriginalEggCardScenario.Choose(decision) ?? pendingPlay;
                if (OriginalCombatTriggerBatchScenario.Enabled)
                    pendingPlay = OriginalCombatTriggerBatchScenario.Choose(decision);
                if (OriginalStealthSlayScenario.Enabled)
                    pendingPlay = OriginalStealthSlayScenario.Choose(decision);
                if (OriginalEquipmentGrowthScenario.Enabled)
                    pendingPlay = OriginalEquipmentGrowthScenario.Choose(decision);
                if (OriginalHealthChoiceScenario.Enabled)
                    pendingPlay = OriginalHealthChoiceScenario.Choose(decision);
                if (OriginalUnitResourceScenario.Enabled)
                    pendingPlay = OriginalUnitResourceScenario.Choose(decision);
                if (fullBattle && OriginalCardDrawOnHealScenario.Enabled)
                    pendingPlay = OriginalCardDrawOnHealScenario.Choose(decision);
                if (fullBattle && OriginalBrokenRailScenario.Enabled)
                    pendingPlay = OriginalBrokenRailScenario.Choose(decision) ?? pendingPlay;
                if (fullBattle && OriginalHephsBlueprintsScenario.Enabled)
                    pendingPlay = OriginalHephsBlueprintsScenario.Choose(decision) ?? pendingPlay;
                if (fullBattle && OriginalRageOnBurnoutStackLossScenario.Enabled)
                    pendingPlay = OriginalRageOnBurnoutStackLossScenario.Choose(decision) ?? pendingPlay;
                if (fullBattle && OriginalCardDrawOnCardPlayScenario.Enabled)
                    pendingPlay = OriginalCardDrawOnCardPlayScenario.ChooseSnack(decision) ?? pendingPlay;
                if (fullBattle && OriginalCarriageJackScenario.Enabled)
                    pendingPlay = OriginalCarriageJackScenario.Choose(decision, pendingPlay);
                if (fullBattle && OriginalFirstRoomAttachmentScenario.Enabled)
                    pendingPlay = OriginalFirstRoomAttachmentScenario.Choose(decision) ?? pendingPlay;
                if (fullBattle && UnplayedCardTriggerScenario.Enabled)
                    pendingPlay = UnplayedCardTriggerScenario.AvoidPlaying(decision, pendingPlay);
                if (fullBattle && UnplayedCardEffectsScenario.Enabled)
                    pendingPlay = UnplayedCardEffectsScenario.AvoidPlaying(decision, pendingPlay);
                if (RoomCapacityAttachmentScenario.Enabled)
                    pendingPlay = RoomCapacityAttachmentScenario.Evidence.Completed
                        ? null : RoomCapacityAttachmentScenario.Choose(decision) ?? pendingPlay;
                if (RelicCardModifierScenario.ExtraToughTombs)
                    pendingPlay = RelicCardModifierScenario.ChooseTombCarrier(decision) ?? pendingPlay;
                if (RelicCardModifierScenario.CastTriggerUpgrade)
                {
                    PlayCardAction? castTriggerPlay = RelicCardModifierScenario.ChooseCastTriggerCard(decision);
                    if (castTriggerPlay != null) pendingPlay = castTriggerPlay;
                    else if (RelicCardModifierScenario.CastTriggerUnitUpgrade &&
                        pendingPlay?.CardInstanceId == RelicCardModifierScenario.CastTriggerUpgradeInstanceId)
                        pendingPlay = BattleActionModel.EnumerateSupportedPlays(decision)
                            .FirstOrDefault(action => action.CardInstanceId != RelicCardModifierScenario.CastTriggerUpgradeInstanceId);
                }
                if (OriginalDeployableRelicScenario.Enabled) pendingPlay = OriginalDeployableRelicScenario.Choose(decision) ?? pendingPlay;
                if (modifierScenario == "sacrifice-effects") pendingPlay = SacrificeScenario.Choose(decision);
                if (modifierScenario == "set-health") pendingPlay = SetHealthScenario.Choose(decision);
                if (SoldierOfFortuneScenario.Enabled) pendingPlay = SoldierOfFortuneScenario.Choose(decision);
                if (RestorationDetonationScenario.Enabled) pendingPlay = RestorationDetonationScenario.Choose(decision);
                if (RunCardGenerationScenario.Enabled) pendingPlay = RunCardGenerationScenario.Choose(decision);
                if (ArmamentDriveScenario.Enabled) pendingPlay = ArmamentDriveScenario.Choose(decision);
                if (PreserveScenario.Enabled) pendingPlay = PreserveScenario.Choose(decision);
                if (AdaptiveMutationScenario.Enabled) pendingPlay = AdaptiveMutationScenario.Choose(decision, AdaptiveMutationScenario.CardIds);
                if (MixalotScenario.Enabled) pendingPlay = MixalotScenario.Choose(decision, MixalotScenario.CardIds);
                if (SmallBoxScenario.Enabled) pendingPlay = SmallBoxScenario.Choose(decision, SmallBoxScenario.CardIds);
                if (MagneticRelayScenario.Enabled) pendingPlay = MagneticRelayScenario.Choose(decision, MagneticRelayScenario.CardIds, MagneticRelayScenario.UnitCardIds);
                if (TranscendImpScenario.Enabled) pendingPlay = TranscendImpScenario.Choose(decision, TranscendImpScenario.CardIds, TranscendImpScenario.SourceCardIds, TranscendImpScenario.RecallCardId);
                if (EquipmentMergeScenario.Enabled) pendingPlay = EquipmentMergeScenario.Choose(decision, EquipmentMergeScenario.CardIds, EquipmentMergeScenario.SourceIds, EquipmentMergeScenario.UnitIds);
                if (EclipseChamberScenario.Enabled) pendingPlay = EclipseChamberScenario.Choose(decision, EclipseChamberScenario.CardIds);
                if (OriginalRoomCardsScenario.Enabled) pendingPlay = OriginalRoomCardsScenario.Choose(decision, OriginalRoomCardsScenario.CardIds);
                if (OriginalForgeCardsScenario.Enabled) pendingPlay = OriginalForgeCardsScenario.Choose(decision, OriginalForgeCardsScenario.CardIds);
                if (OriginalStatusCardsScenario.Enabled) pendingPlay = OriginalStatusCardsScenario.Choose(decision, OriginalStatusCardsScenario.CardIds);
                if (OriginalDeckSpellScenario.Enabled) pendingPlay = OriginalDeckSpellScenario.Choose(decision, OriginalDeckSpellScenario.CardIds);
                if (GoldmanceScenario.Enabled) pendingPlay = GoldmanceScenario.Choose(decision, GoldmanceScenario.CardIds);
                if (EngineUpgradeScenario.Enabled) pendingPlay = EngineUpgradeScenario.Choose(decision, EngineUpgradeScenario.CardIds);
                if (StewardsQuartersScenario.Enabled) pendingPlay = StewardsQuartersScenario.Choose(decision, StewardsQuartersScenario.CardIds);
                if (ZoneOfSilenceScenario.Enabled) pendingPlay = ZoneOfSilenceScenario.Choose(decision, ZoneOfSilenceScenario.CardIds);
                if (MemorialScenario.Enabled) pendingPlay = MemorialScenario.Choose(decision, MemorialScenario.CardIds);
                if (PlanetariumScenario.Enabled) pendingPlay = PlanetariumScenario.Choose(decision, PlanetariumScenario.CardIds);
                if (GrowRoomScenario.Enabled) pendingPlay = GrowRoomScenario.Choose(decision, GrowRoomScenario.CardIds);
                if (MushyRoomScenario.Enabled) pendingPlay = MushyRoomScenario.Choose(decision, MushyRoomScenario.CardIds);
                if (OriginalPiercingCardsScenario.Enabled) pendingPlay = OriginalPiercingCardsScenario.Choose(decision, OriginalPiercingCardsScenario.CardIds);
                if (InfernoRoomScenario.Enabled) pendingPlay = InfernoRoomScenario.Choose(decision, InfernoRoomScenario.CardIds);
                if (OriginalConsumeCardsScenario.Enabled) pendingPlay = OriginalConsumeCardsScenario.Choose(decision, OriginalConsumeCardsScenario.CardIds);
                if (OriginalStrongMagicCardsScenario.Enabled) pendingPlay = OriginalStrongMagicCardsScenario.Choose(decision, OriginalStrongMagicCardsScenario.CardIds);
                if (OriginalXDamageCardsScenario.Enabled) pendingPlay = OriginalXDamageCardsScenario.Choose(decision, OriginalXDamageCardsScenario.CardIds);
                if (OriginalUnpurgableCardsScenario.Enabled) pendingPlay = OriginalUnpurgableCardsScenario.Choose(decision, OriginalUnpurgableCardsScenario.CardIds);
                if (OriginalHandCostsScenario.Enabled) pendingPlay = OriginalHandCostsScenario.Choose(decision, OriginalHandCostsScenario.CardIds);
                if (OriginalLimitedRangeScenario.Enabled) pendingPlay = OriginalLimitedRangeScenario.Choose(decision, OriginalLimitedRangeScenario.CardIds);
                if (OriginalMonsterHandCostsScenario.Enabled) pendingPlay = OriginalMonsterHandCostsScenario.Choose(decision, OriginalMonsterHandCostsScenario.CardIds);
                if (OriginalUnplayableCardsScenario.Enabled) pendingPlay = OriginalUnplayableCardsScenario.Choose(decision, OriginalUnplayableCardsScenario.CardIds, OriginalUnplayableCardsScenario.NegativeIds[0]);
                if (OriginalPermafrostCardsScenario.Enabled) pendingPlay = OriginalPermafrostCardsScenario.Choose(decision, OriginalPermafrostCardsScenario.CardIds, OriginalPermafrostCardsScenario.JunkId);
                if (OriginalOverflowCardsScenario.Enabled) pendingPlay = OriginalOverflowCardsScenario.Choose(decision, OriginalOverflowCardsScenario.CardIds);
                if (OriginalCorruptRestrictedScenario.Enabled) pendingPlay = OriginalCorruptRestrictedScenario.Choose(decision, OriginalCorruptRestrictedScenario.CardIds);
                if (OriginalStatusStackDamageScenario.Enabled) pendingPlay = OriginalStatusStackDamageScenario.Choose(decision, OriginalStatusStackDamageScenario.CardIds);
                if (OriginalSpecificCardDamageScenario.Enabled) pendingPlay = OriginalSpecificCardDamageScenario.Choose(decision, OriginalSpecificCardDamageScenario.CardIds);
                if (OriginalScaledEnergyScenario.Enabled) pendingPlay = OriginalScaledEnergyScenario.Choose(decision, OriginalScaledEnergyScenario.CardIds);
                if (OriginalStatusRemovalScalingScenario.Enabled) pendingPlay = OriginalStatusRemovalScalingScenario.Choose(decision, OriginalStatusRemovalScalingScenario.CardIds);
                if (OriginalEffectApplicationsScenario.Enabled) pendingPlay = OriginalEffectApplicationsScenario.Choose(decision, OriginalEffectApplicationsScenario.CardIds);
                if (OriginalPileDrawScenario.Enabled) pendingPlay = OriginalPileDrawScenario.Choose(decision, OriginalPileDrawScenario.CardIds);
                if (OriginalPileRecursionScenario.Enabled) pendingPlay = OriginalPileRecursionScenario.Choose(decision, OriginalPileRecursionScenario.CardIds);
                if (OriginalScaledBuffScenario.Enabled) pendingPlay = OriginalScaledBuffScenario.Choose(decision, OriginalScaledBuffScenario.CardIds);
                if (OriginalRoomStatusDamageScenario.Enabled) pendingPlay = OriginalRoomStatusDamageScenario.Choose(decision, OriginalRoomStatusDamageScenario.CardIds);
                if (OriginalCorruptionDamageScenario.Enabled) pendingPlay = OriginalCorruptionDamageScenario.Choose(decision, OriginalCorruptionDamageScenario.CardIds);
                if (OriginalXStatusCardsScenario.Enabled) pendingPlay = OriginalXStatusCardsScenario.Choose(decision, OriginalXStatusCardsScenario.CardIds);
                if (CardDuplicateScenario.Enabled) pendingPlay = CardDuplicateScenario.Choose(decision, CardDuplicateScenario.CardIds, CardDuplicateScenario.RecipientCardId);
                if (modifierScenario == "damage-by-units-killed") pendingPlay = DamageByUnitsKilledScenario.Choose(decision);
                if (modifierScenario == TransferAttackScenario.Name) pendingPlay = TransferAttackScenario.Choose(decision);
                if (modifierScenario == BrainTransferScenario.Name) pendingPlay = BrainTransferScenario.Choose(decision);
                if (modifierScenario == FrostChannelingScenario.Name) pendingPlay = FrostChannelingScenario.Choose(decision);
                if (modifierScenario is "card-effect-kill" or "card-effect-kill-subtype")
                    pendingPlay = CardEffectKillScenario.Choose(decision);
                if (NullForwardingScenario.Enabled) pendingPlay = NullForwardingScenario.Choose(decision, log);
                if (NullTriggerScenario.Enabled) pendingPlay = NullTriggerScenario.Choose(decision, log);
                if (NullTargetlessScenario.Enabled) pendingPlay = NullTargetlessScenario.Choose(decision, log);
                if (TrampleScenario.Enabled) pendingPlay = TrampleScenario.Choose(decision, log);
                if (SpellShieldScenario.Enabled) pendingPlay = SpellShieldScenario.Choose(decision, log);
                if (modifierScenario == "remove-all-status-effects")
                    pendingPlay = CardEffectRemoveAllStatusScenario.Choose(decision) ?? pendingPlay;
                if (DragonHoardScenario.Enabled) pendingPlay = DragonHoardScenario.Choose(decision);
                if (TrainHealScenario.Enabled) pendingPlay = TrainHealScenario.Choose(decision);
                if (OverhealScenario.Enabled) pendingPlay = OverhealScenario.Choose(decision);
                if (CorruptionScenario.Enabled) pendingPlay = CorruptionScenario.Choose(decision);
                if (PermanentCorruptionScenario.Enabled) pendingPlay = PermanentCorruptionScenario.Choose(decision);
                if (MoonPhaseCardScenario.Enabled) pendingPlay = MoonPhaseCardScenario.Choose(decision);
                if (MoonPhaseAdvanceScenario.Enabled) pendingPlay = MoonPhaseAdvanceScenario.Choose(decision);
                if (MoonPhaseCallbackScenario.Enabled) pendingPlay = MoonPhaseCallbackScenario.Choose(decision);
                if (OriginalMoonPhaseHealRelicScenario.Enabled) pendingPlay = OriginalMoonPhaseHealRelicScenario.Choose(decision);
                if (OriginalOpeningMorselsScenario.FoodCardsMode) pendingPlay = OriginalOpeningMorselsScenario.Choose(decision);
                if (OriginalMoonPhaseStatusRelicScenario.Enabled) pendingPlay = OriginalMoonPhaseStatusRelicScenario.Choose(decision);
                if (ConditionalNoPlayerUnitScenario.Enabled) pendingPlay = ConditionalNoPlayerUnitScenario.Choose(decision);
                if (SelfAttackStatusScenario.Enabled) pendingPlay = SelfAttackStatusScenario.Choose(decision);
                if (ConditionalPyreDamageScenario.Enabled) pendingPlay = ConditionalPyreDamageScenario.Choose(decision);
                if (SoulSiphonScenario.Enabled) pendingPlay = SoulSiphonScenario.Choose(decision);
                if (StatusUpgradeScenario.Enabled) pendingPlay = StatusUpgradeScenario.Choose(decision);
                if (modifierScenario is "card-effect-recursion" or "card-effect-recursion-choice" or "card-effect-recursion-choose-to-hand" or "card-effect-recursion-filter-choice" or "card-effect-recursion-discard-choice" or "card-effect-recursion-upgrade" or "card-effect-recursion-upgrade-hand" or "card-effect-recursion-upgrade-choice" or "card-effect-recursion-upgrade-filter-choice" or "card-effect-recursion-upgrade-replacement-choice")
                    pendingPlay = CardEffectRecursionScenario.Choose(decision);
                if (MixScenario.Enabled) pendingPlay = MixScenario.Choose(decision);
                if (OriginalTribouletScenario.Enabled)
                    pendingPlay = OriginalTribouletScenario.Choose(decision) ??
                        OriginalTribouletScenario.AvoidRepeatedWitness(decision, pendingPlay);
                if (OriginalCooldownOnDamageScenario.Enabled)
                    pendingPlay = OriginalCooldownOnDamageScenario.Choose(decision) ??
                        OriginalCooldownOnDamageScenario.AvoidUnsequencedWitness(decision, pendingPlay);
                if (OriginalCardPlayDamageScenario.Enabled)
                    pendingPlay = OriginalCardPlayDamageScenario.Choose(decision, pendingPlay);
                if (OriginalConsumeDamageScenario.Enabled)
                    pendingPlay = OriginalConsumeDamageScenario.Choose(decision, pendingPlay);
                if (OriginalEatenDamageScenario.Enabled)
                    pendingPlay = OriginalEatenDamageScenario.Choose(decision, pendingPlay);
                if (OriginalMoreCapacityScenario.Enabled)
                    pendingPlay = OriginalMoreCapacityScenario.Choose(decision, pendingPlay);
                if (OriginalBurnoutDamageScenario.Enabled)
                    pendingPlay = OriginalBurnoutDamageScenario.Choose(decision, pendingPlay);
                if (OriginalHealDamageScenario.Enabled)
                    pendingPlay = OriginalHealDamageScenario.Choose(decision, pendingPlay);
                if (OriginalKillAttackScenario.Enabled)
                    pendingPlay = OriginalKillAttackScenario.Choose(decision, pendingPlay);
                if (OriginalSpawnSilenceScenario.Enabled)
                    pendingPlay = OriginalSpawnSilenceScenario.Choose(decision, pendingPlay);
                if (OriginalFreezeReduceCostScenario.Enabled)
                    pendingPlay = OriginalFreezeReduceCostScenario.Choose(decision);
                if (OriginalRandomFreezeScenario.Enabled)
                    pendingPlay = OriginalRandomFreezeScenario.Choose(decision);
                if (OriginalRandomHandUpgradeScenario.Enabled)
                    pendingPlay = OriginalRandomHandUpgradeScenario.Choose(decision);
                if (OriginalStartTurnPropagationScenario.Enabled)
                    pendingPlay = OriginalStartTurnPropagationScenario.Choose(decision);
                if (OriginalMooncycleFaeScenario.Enabled)
                    pendingPlay = OriginalMooncycleFaeScenario.Choose(decision);
                if (OriginalRandomDrawCostScenario.Enabled)
                    pendingPlay = OriginalRandomDrawCostScenario.Choose(decision);
                if (OriginalSuppressExhaustScenario.Enabled)
                    pendingPlay = OriginalSuppressExhaustScenario.Choose(decision);
                if (OriginalExpensiveCardUpgradeScenario.Enabled)
                    pendingPlay = OriginalExpensiveCardUpgradeScenario.Choose(decision);
                if (OriginalWolosBloodletterScenario.Enabled)
                    pendingPlay = OriginalWolosBloodletterScenario.Choose(decision);
                if (OriginalCardTypeSequenceScenario.Enabled)
                    pendingPlay = OriginalCardTypeSequenceScenario.Choose(decision);
                if (OriginalWhelpCooldownScenario.Enabled)
                    pendingPlay = OriginalWhelpCooldownScenario.Choose(decision);
                if (OriginalSpawnRetreatScenario.Enabled)
                    pendingPlay = OriginalSpawnRetreatScenario.Choose(decision);
                if (OriginalGrosMichelScenario.Enabled)
                    pendingPlay = RelicEndCombatModel.ChooseGrosMichelPlay(decision);
                if (OriginalDecayDeathScenario.Enabled)
                    pendingPlay = RelicDecayDeathModel.ChoosePlay(decision);
                if (OriginalBoneDogScenario.Enabled)
                    pendingPlay = OriginalBoneDogScenario.Choose(decision);
                if (ChooseDiscardScenario.Enabled) pendingPlay = ChooseDiscardScenario.Choose(decision);
                if (modifierScenario == "typed-draw") pendingPlay = TypedDrawScenario.Choose(decision);
                if (modifierScenario == DamagePerHpScenario.Name || modifierScenario == DamagePerHpScenario.ArmorName ||
                    modifierScenario == DamagePerHpScenario.LastAttackerName)
                    pendingPlay = DamagePerHpScenario.Choose(decision);
                if (modifierScenario == DamageOverflowScenario.Name)
                    pendingPlay = DamageOverflowScenario.Choose(decision) ?? pendingPlay;
                if (OriginalUnitRelicBatchScenario.Enabled) pendingPlay = OriginalUnitRelicBatchScenario.Choose(decision);
                if (OriginalEndlessReturnScenario.Enabled) pendingPlay = OriginalEndlessReturnScenario.Choose(decision);
                if (OriginalTriggerRelicBatchScenario.Enabled) pendingPlay = OriginalTriggerRelicBatchScenario.Choose(decision);
                if (OriginalLifecycleRelicBatchScenario.Enabled) pendingPlay = OriginalLifecycleRelicBatchScenario.Choose(decision);
                if (OriginalDecayRelicBatchScenario.Enabled) pendingPlay = OriginalDecayRelicBatchScenario.Choose(decision);
                if (OriginalResourceRelicBatchScenario.Enabled) pendingPlay = OriginalResourceRelicBatchScenario.Choose(decision);
                if (PyreResourceRefreshScenario.Enabled) pendingPlay = PyreResourceRefreshScenario.Choose(decision);
                if (OriginalFloorCapacityRelicScenario.Enabled) pendingPlay = OriginalFloorCapacityRelicScenario.Choose(decision);
                if (MagebladeScenario.Enabled) pendingPlay = MagebladeScenario.Choose(decision) ?? pendingPlay;
                if (OriginalCardPlayMagicPowerScenario.Enabled) pendingPlay = OriginalCardPlayMagicPowerScenario.Choose(decision) ?? pendingPlay;
                if (OriginalTwoChargesOnCardConsumedScenario.Enabled)
                    pendingPlay = OriginalTwoChargesOnCardConsumedScenario.Choose(decision) ?? pendingPlay;
                if (OriginalExtractForEmberAndEchosScenario.Enabled)
                    pendingPlay = OriginalExtractForEmberAndEchosScenario.Choose(decision) ?? pendingPlay;
                if (OriginalCheaperSpellCostScenario.Enabled) pendingPlay = OriginalCheaperSpellCostScenario.Choose(decision);
                if (OriginalFirstCardCostScenario.Enabled) pendingPlay = OriginalFirstCardCostScenario.Choose(decision);
                if (OriginalRebalanceValorScenario.Enabled) pendingPlay = OriginalRebalanceValorScenario.Choose(decision) ?? pendingPlay;
                if (OriginalCardAttackRelicScenario.Enabled) pendingPlay = OriginalCardAttackRelicScenario.Choose(decision) ?? pendingPlay;
                if (OriginalCardTypeCostScenario.Enabled) pendingPlay = OriginalCardTypeCostScenario.Choose(decision) ?? pendingPlay;
                if (OriginalPurgeToExhaustScenario.Enabled) pendingPlay = OriginalPurgeToExhaustScenario.Choose(decision) ?? pendingPlay;
                if (OriginalUpgradeCountRelicsScenario.Enabled) pendingPlay = OriginalUpgradeCountRelicsScenario.Choose(decision) ?? pendingPlay;
                if (OriginalHealingPowerScenario.Enabled) pendingPlay = OriginalHealingPowerScenario.Choose(decision) ?? pendingPlay;
                if (OriginalConserveEmberScenario.Enabled) pendingPlay = OriginalConserveEmberScenario.Choose(decision);
                if (OriginalUnitUpgradeTriggerRelicScenario.Enabled) pendingPlay = OriginalUnitUpgradeTriggerRelicScenario.Choose(decision) ?? pendingPlay;
                if (OriginalUnitPhaseUpgradeScenario.Enabled) pendingPlay = OriginalUnitPhaseUpgradeScenario.Choose(decision);
                if (OriginalSoloUnitUpgradeScenario.Enabled) pendingPlay = OriginalSoloUnitUpgradeScenario.Choose(decision);
                if (OriginalFrontRoomHealthUpgradeScenario.Enabled) pendingPlay = OriginalFrontRoomHealthUpgradeScenario.Choose(decision);
                if (OriginalChampionStartTurnHealScenario.Enabled) pendingPlay = OriginalChampionStartTurnHealScenario.Choose(decision);
                if (OriginalPyreCardCostScenario.Enabled) pendingPlay = OriginalPyreCardCostScenario.Choose(decision) ?? pendingPlay;
                if (modifierScenario == "rally-lethal") pendingPlay = LethalRallyScenario.ChooseNativeBossSummon(managers) ?? pendingPlay;
                if (modifierScenario == "incant" || modifierScenario == "incant-thresholds" || (modifierScenario == "incant-relic" || modifierScenario == "incant-relic-combined")) pendingPlay = BattleActionModel.ChooseEmptySpellThenCards(decision);
                if (modifierScenario.StartsWith("multi-summon", StringComparison.Ordinal)) pendingPlay = MultiSummonScenario.Choose(managers) ?? pendingPlay;
                if (modifierScenario == "ability-teleport")
                    pendingPlay = AbilityTeleportScenario.Choose(decision);
                else if (modifierScenario == "transfer-attack-ability")
                    pendingPlay = UnitAbilityModel.ChooseAbilityThenCards(decision);
                else if (modifierScenario == "damage-per-hp-ability")
                    pendingPlay = DamagePerHpScenario.ChooseAbility(decision);
                else if (modifierScenario == "ability-incant" || modifierScenario == "equipment-abilities" || modifierScenario == "ability-effects" || modifierScenario == "spawn-or-status" || modifierScenario == "ability-activation" || modifierScenario == "ability-activation-x" || modifierScenario == "ability-activation-lethal")
                    pendingPlay = UnitAbilityModel.ChooseAbilityThenCards(decision);
                if (modifierScenario == "equipment-buff")
                    pendingPlay = EquipmentScenario.ChooseBuffTarget(decision, pendingPlay) ?? pendingPlay;
                if (fullBattle && OriginalFirstImpBonusScenario.Enabled)
                    pendingPlay = OriginalFirstImpBonusScenario.Choose(decision) ?? pendingPlay;
                if (pendingPlay != null)
                {
                    RoomManager rooms = managers.GetRoomManager()!;
                    if (rooms.GetSelectedRoom() != pendingPlay.RoomIndex)
                    {
                        PreviewSchedulingProbe.Observe("RoomSelectionRequest:" + pendingPlay.RoomIndex);
                        save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(pendingPlay.RoomIndex));
                        Enter(Stage.SelectingRoom, 20f);
                    }
                    else PlayPendingPolicyCard(managers, cards!);
                    return;
                }
            }
            else if (!(fullBattle && Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY") == "no-cards") &&
                (pass == Pass.Source && sourcePlayTurns.Contains(relativeTurn) ||
                pass != Pass.Source && (relativeTurn == targetTurn ||
                    relativeTurn < targetTurn && sourcePlayTurns.Contains(relativeTurn))))
            {
                SelectRoomAndPlay(managers, save, cards!);
                return;
            }
            AdvanceTurn(combat);
        }

        private void BeginBranchedPass()
        {
            pass = Pass.Branched;
            recordedTurn = targetTurn - 1;
            Enter(Stage.Battle, 60f);
        }

        private void SelectRoomAndPlay(AllGameManagers managers, SaveManager save, CardManager cards)
        {
            RoomManager? rooms = managers.GetRoomManager();
            if (rooms == null)
            {
                Finish(false, "Room manager unavailable for card play.");
                return;
            }
            if (rooms.GetSelectedRoom() != 0)
            {
                if (!roomSelectionRequested)
                {
                    roomSelectionRequested = true;
                    save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                }
                Enter(Stage.SelectingRoom, 20f);
                return;
            }
            PlayConfiguredCard(cards);
        }

        private void PlayConfiguredCard(CardManager cards)
        {
            roomSelectionRequested = false;
            List<CardState> hand = cards.GetHand();
            for (int index = 0; index < hand.Count; index++)
            {
                if (hand[index].GetCardDataID() != StewardCardId ||
                    !cards.CanPlayHandCard(index, 0, out CommonSelectionBehavior.SelectionError error))
                {
                    continue;
                }
                if (fullBattle) FullBattleTrace.Active?.BeginCardPlay(new PlayCardAction(FullBattleTrace.Active.CardId(hand[index]), 0));
                if (!cards.PlayCard(index, null, ref error))
                {
                    Finish(false, "Direct Steward play failed: " + error);
                    return;
                }
                log.LogInfo("DEPTH-PLAY pass=" + pass + " handIndex=" + index + " room=0");
                Enter(Stage.PlayingCard, 30f);
                return;
            }
            if (pass != Pass.Source && branchAnyUnit)
            {
                for (int index = 0; index < hand.Count; index++)
                {
                    if (hand[index].GetCardType() != CardType.Monster ||
                        !cards.CanPlayHandCard(index, 0, out CommonSelectionBehavior.SelectionError error))
                    {
                        continue;
                    }
                    string cardId = hand[index].GetCardDataID();
                    if (!cards.PlayCard(index, null, ref error))
                    {
                        Finish(false, "Unit play failed: " + error);
                        return;
                    }
                    log.LogInfo("DEPTH-PLAY pass=" + pass + " card=" + cardId +
                        " handIndex=" + index + " room=0");
                    Enter(Stage.PlayingCard, 30f);
                    return;
                }
            }
            foreach (CardState card in hand)
            {
                log.LogInfo("DEPTH-HAND card=" + card.GetCardDataID() + " type=" + card.GetCardType() +
                    " target=" + card.GetCardTargetMode());
            }
            Finish(false, "No legal branch card at turn " + recordedTurn + ", pass=" + pass);
        }

        private bool IsUnitAndJunkPolicy() => fullBattle &&
            (Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY") == "units-and-junk" ||
             Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY") == "units-spells-and-junk");

        private void PlayPendingPolicyCard(AllGameManagers managers, CardManager cards)
        {
            PlayCardAction action = pendingPlay ?? throw new InvalidOperationException("Missing policy card action.");
            string modifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? string.Empty;
            bool scenarioPrelude = (OriginalUnitRelicBatchScenario.Enabled && !OriginalUnitRelicBatchScenario.PolicyCarriersInjected) ||
                (OriginalBalatroEventYorickScenario.Enabled && OriginalBalatroEventYorickScenario.IsScenarioAction(action)) ||
                (modifierScenario == "damage-per-hp-ability" && !damagePerHpAbilityInputPrepared) ||
                (modifierScenario == "dragon-hoard" && DragonHoardScenario.IsScenarioAction(action)) ||
                OriginalOpeningMorselsScenario.FoodCardsMode ||
                (OriginalCarriageJackScenario.Enabled && OriginalCarriageJackScenario.IsScenarioAction(action)) ||
                (OriginalTwoChargesOnCardConsumedScenario.Enabled && OriginalTwoChargesOnCardConsumedScenario.IsScenarioAction(action)) ||
                (OriginalFirstRoomAttachmentScenario.Enabled && OriginalFirstRoomAttachmentScenario.IsScenarioAction(action)) ||
                (modifierScenario == "train-heal" && TrainHealScenario.IsScenarioAction(action)) ||
                (modifierScenario == "overheal" && OverhealScenario.IsScenarioAction(action)) ||
                (modifierScenario is "corruption" or "corruption-unit-trigger" or "corruption-carver-trigger" or "corruption-keeper-trigger" or "corruption-relic-trigger" &&
                    CorruptionScenario.IsScenarioAction(action)) ||
                (modifierScenario == "permanent-corruption" && PermanentCorruptionScenario.IsScenarioAction(action)) ||
                (modifierScenario == "moon-phase-card" && MoonPhaseCardScenario.IsScenarioAction(action)) ||
                (modifierScenario == "moon-phase-advance" && MoonPhaseAdvanceScenario.IsScenarioAction(action)) ||
                (modifierScenario == "moon-phase-callbacks" && MoonPhaseCallbackScenario.IsScenarioAction(action)) ||
                (modifierScenario == OriginalMoonPhaseHealRelicScenario.Name && OriginalMoonPhaseHealRelicScenario.IsScenarioAction(action)) ||
                (modifierScenario == OriginalChampionStartTurnHealScenario.Name && OriginalChampionStartTurnHealScenario.IsScenarioAction(action)) ||
                (OriginalPyreCardCostScenario.Enabled && OriginalPyreCardCostScenario.IsScenarioAction(action)) ||
                (modifierScenario == OriginalMoonPhaseStatusRelicScenario.Name && OriginalMoonPhaseStatusRelicScenario.IsScenarioAction(action)) ||
                (OriginalCardDrawOnHealScenario.IsScenarioAction(action)) ||
                (OriginalBrokenRailScenario.IsScenarioAction(action)) ||
                (OriginalHephsBlueprintsScenario.IsScenarioAction(action)) ||
                (OriginalRageOnBurnoutStackLossScenario.IsScenarioAction(action)) ||
                (OriginalExtractForEmberAndEchosScenario.IsScenarioAction(action)) ||
                (modifierScenario == "soul-siphon" && SoulSiphonScenario.IsScenarioAction(action)) ||
                (modifierScenario == "status-upgrade" && StatusUpgradeScenario.IsScenarioAction(action)) ||
                (modifierScenario == "card-effect-kill-subtype" && CardEffectKillScenario.IsScenarioPrelude(action)) ||
                (OriginalCheaperSpellCostScenario.Enabled && OriginalCheaperSpellCostScenario.IsScenarioAction(action)) ||
                (OriginalFirstCardCostScenario.Enabled && OriginalFirstCardCostScenario.IsScenarioAction(action)) ||
                (OriginalRebalanceValorScenario.Enabled && OriginalRebalanceValorScenario.IsScenarioAction(action)) ||
                (NullForwardingScenario.Enabled && NullForwardingScenario.ConsumeScenarioActionMark(action)) ||
                (NullTriggerScenario.Enabled && NullTriggerScenario.ConsumeScenarioActionMark(action)) ||
                (NullTargetlessScenario.Enabled && NullTargetlessScenario.ConsumeScenarioActionMark(action)) ||
                (TrampleScenario.Enabled && TrampleScenario.ConsumeScenarioActionMark(action)) ||
                (SpellShieldScenario.Enabled && SpellShieldScenario.ConsumeScenarioActionMark(action)) ||
                (modifierScenario == "remove-all-status-effects" && CardEffectRemoveAllStatusScenario.ConsumeScenarioActionMark(action)) ||
                (modifierScenario is "card-effect-recursion" or "card-effect-recursion-choice" or "card-effect-recursion-choose-to-hand" or "card-effect-recursion-filter-choice" or "card-effect-recursion-discard-choice" or "card-effect-recursion-upgrade" or "card-effect-recursion-upgrade-hand" or "card-effect-recursion-upgrade-choice" or "card-effect-recursion-upgrade-filter-choice" or "card-effect-recursion-upgrade-replacement-choice" &&
                    CardEffectRecursionScenario.IsScenarioPrelude(action)) ||
                (MixScenario.Enabled && MixScenario.IsScenarioPrelude(action)) ||
                (OriginalTribouletScenario.Enabled && OriginalTribouletScenario.IsScenarioAction(action)) ||
                (OriginalCooldownOnDamageScenario.Enabled && OriginalCooldownOnDamageScenario.IsScenarioAction(action)) ||
                (OriginalCardPlayDamageScenario.Enabled && OriginalCardPlayDamageScenario.IsScenarioAction(action)) ||
                (OriginalConsumeDamageScenario.Enabled && OriginalConsumeDamageScenario.IsScenarioAction(action)) ||
                (OriginalEatenDamageScenario.Enabled && OriginalEatenDamageScenario.IsScenarioAction(action)) ||
                (OriginalMoreCapacityScenario.Enabled && OriginalMoreCapacityScenario.IsScenarioAction(action)) ||
                (OriginalBurnoutDamageScenario.Enabled && OriginalBurnoutDamageScenario.IsScenarioAction(action)) ||
                (OriginalHealDamageScenario.Enabled && OriginalHealDamageScenario.IsScenarioAction(action)) ||
                (OriginalKillAttackScenario.Enabled && OriginalKillAttackScenario.IsScenarioAction(action)) ||
                (OriginalSpawnSilenceScenario.Enabled && OriginalSpawnSilenceScenario.IsScenarioAction(action)) ||
                (OriginalFreezeReduceCostScenario.Enabled && OriginalFreezeReduceCostScenario.IsScenarioAction(action)) ||
                (OriginalRandomFreezeScenario.Enabled && OriginalRandomFreezeScenario.IsScenarioAction(action)) ||
                (OriginalRandomHandUpgradeScenario.Enabled && OriginalRandomHandUpgradeScenario.IsScenarioAction(action)) ||
                (OriginalStartTurnPropagationScenario.Enabled && OriginalStartTurnPropagationScenario.IsScenarioAction(action)) ||
                (OriginalMooncycleFaeScenario.Enabled && OriginalMooncycleFaeScenario.IsScenarioAction(action)) ||
                (OriginalRandomDrawCostScenario.Enabled && OriginalRandomDrawCostScenario.IsScenarioAction(action)) ||
                (OriginalSuppressExhaustScenario.Enabled && OriginalSuppressExhaustScenario.IsScenarioAction(action)) ||
                (OriginalExpensiveCardUpgradeScenario.Enabled && OriginalExpensiveCardUpgradeScenario.IsScenarioAction(action)) ||
                (OriginalWolosBloodletterScenario.Enabled && OriginalWolosBloodletterScenario.IsScenarioAction(action)) ||
                (OriginalCardTypeSequenceScenario.Enabled && OriginalCardTypeSequenceScenario.IsScenarioAction(action)) ||
                (OriginalWhelpCooldownScenario.Enabled && OriginalWhelpCooldownScenario.IsScenarioAction(action)) ||
                (OriginalSpawnRetreatScenario.Enabled && OriginalSpawnRetreatScenario.IsScenarioAction(action)) ||
                (OriginalCardTypeCostScenario.Enabled && OriginalCardTypeCostScenario.IsScenarioPrelude(action)) ||
                (OriginalPurgeToExhaustScenario.Enabled && OriginalPurgeToExhaustScenario.IsScenarioPrelude(action)) ||
                (OriginalUpgradeCountRelicsScenario.Enabled && OriginalUpgradeCountRelicsScenario.IsScenarioPrelude(action)) ||
                (OriginalHealingPowerScenario.Enabled && OriginalHealingPowerScenario.IsScenarioPrelude(action)) ||
                (ChooseDiscardScenario.Enabled && ChooseDiscardScenario.IsScenarioPrelude(action));
            PreviewSchedulingProbe.Observe("Play:" + action.CardInstanceId);
            if (MixScenario.Enabled) MixScenario.Expect(action);
            if (action.ActivatorUnitId > 0)
            {
                CharacterState actor = FullBattleTrace.Active!.KnownUnits.Single(unit => FullBattleTrace.Active.UnitId(unit) == action.ActivatorUnitId);
                FullBattleTrace.Active.BeginCardPlay(action, scenarioAction: scenarioPrelude);
                if (action.TargetSpawnPointIndex < 0)
                {
                    if (!actor.ActivateUnitAbility(cards)) throw new InvalidOperationException("Native unit ability activation failed.");
                }
                else
                {
                    RoomState targetRoom = managers.GetRoomManager()!.GetRoom(action.RoomIndex);
                    SpawnPoint abilityDrop = targetRoom.GetMonsterPoint(action.TargetSpawnPointIndex);
                    bool played = cards.PlayAnyCard(actor.GetUnitAbilityCardState(), out CommonSelectionBehavior.SelectionError abilityError,
                        actor, abilityDrop, ignoreCost: false, discardCard: false, fromDirectPlay: true,
                        characterThatActivatedAbility: actor);
                    if (!played) throw new InvalidOperationException("Native targeted unit ability failed: " + abilityError);
                }
                log.LogInfo("DEPTH-POLICY-ABILITY card=" + action.CardInstanceId + " unit=" + action.ActivatorUnitId +
                    " room=" + action.RoomIndex + " targetPoint=" + action.TargetSpawnPointIndex);
                if (OriginalAddSnackOnKillScenario.Enabled) OriginalAddSnackOnKillScenario.MarkPolicyActionPlayed();
                if (OriginalFinalEndingArtifactScenario.Enabled) OriginalFinalEndingArtifactScenario.MarkPolicyActionPlayed();
                pendingPlay = null; Enter(Stage.PlayingCard, 30f); return;
            }
            int index = cards.GetHand().FindIndex(card => FullBattleTrace.Active!.CardId(card) == action.CardInstanceId);
            if (index < 0) throw new InvalidOperationException("Policy card disappeared from hand.");
            RoomState room = managers.GetRoomManager()!.GetRoom(action.RoomIndex);
            // CardManager's direct-play API omits the UI's disabled-room gate.
            // Keep probe policies within CommonSelectionBehavior.CanSelectCard rules.
            if (!room.IsRoomEnabled())
                throw new InvalidOperationException("The native UI rejects a card play in a disabled room.");
            SpawnPoint? drop = action.PlayerPosition < 0 ? null : room.GetMonsterPoint(action.PlayerPosition);
            if (action.TargetUnitId > 0)
            {
                var units = new List<CharacterState>(); room.AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
                CharacterState target = units.Single(unit => FullBattleTrace.Active!.UnitId(unit) == action.TargetUnitId);
                drop = target.GetSpawnPoint();
            }
            if (!cards.CanPlayHandCard(cards.GetHand()[index], action.RoomIndex, drop, null, null, out var error))
                throw new InvalidOperationException("The native game rejected a modeled legal play: " + error);
            FullBattleTrace.Active!.BeginCardPlay(action, scenarioAction: scenarioPrelude);
            if (!cards.PlayCard(index, drop, ref error)) throw new InvalidOperationException("Policy card play failed: " + error);
            if (OriginalAddSnackOnKillScenario.Enabled) OriginalAddSnackOnKillScenario.MarkPolicyActionPlayed();
            if (OriginalFinalEndingArtifactScenario.Enabled) OriginalFinalEndingArtifactScenario.MarkPolicyActionPlayed();
            if (OriginalBalatroEventYorickScenario.Enabled) OriginalBalatroEventYorickScenario.MarkPolicyActionPlayed();
            log.LogInfo("DEPTH-POLICY-PLAY card=" + action.CardInstanceId + " room=" + action.RoomIndex + " position=" + action.PlayerPosition + " target=" + action.TargetUnitId);
            pendingPlay = null;
            string summonScenario = modifierScenario;
            // Generation plus relic setup can exceed 30 seconds under concurrent
            // replay load; keep a bounded wait without changing settlement checks.
            Enter(Stage.PlayingCard, summonScenario.StartsWith("multi-summon-additional", StringComparison.Ordinal) ||
                summonScenario.StartsWith("multi-summon-pool", StringComparison.Ordinal) ||
                summonScenario == "generation-relic-card-status-upgrades" ? 180f : 30f);
        }

        private void AdvanceTurn(CombatManager combat)
        {
            if (fullBattle) FullBattleTrace.Active?.BeginEndTurn();
            expectedTurn = combat.GetTurnCount() - initialTurn + 1;
            endTurn.Invoke(null, null);
            Enter(Stage.WaitingTurn, 90f);
        }

        private string Capture(string label, AllGameManagers managers, SaveManager save,
            CombatManager combat, CardManager cards)
        {
            return TwoTurnScenario.CaptureSnapshot(label, managers, save, combat, cards, log);
        }

        private static bool Ready(AllGameManagers managers, SaveManager save, CombatManager? combat, CardManager? cards)
        {
            return !LoadingScreen.IsWorking() && !save.PreviewMode && !PreviewRngIsolation.Active && CardAnimationSettlement.Ready() && save.GetGameSequence() == SaveData.GameSequence.InBattle &&
                combat != null && cards != null && combat.ShouldShowEndTurnButton() &&
                (!save.GetBattlePreviewEnabled() || !(bool)typeof(CombatManager).GetField("combatStateChanged",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(combat)) &&
                combat.HaveCardsBeenDrawnForCurrentTurn() && !combat.IsRunningTriggerQueue &&
                !managers.GetReplayManager().IsCardPlaying() &&
                !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false);
        }

        private static int ParseTurn(string name, int fallback)
        {
            string? raw = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }
            if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            {
                throw new ArgumentException(name + " must be a non-negative integer.");
            }
            return value;
        }

        private static HashSet<int> ParseTurnSet(string? raw)
        {
            var turns = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return turns;
            }
            foreach (string item in raw.Split(','))
            {
                if (!int.TryParse(item.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int turn) ||
                    !turns.Add(turn))
                {
                    throw new ArgumentException("MT2_PROBE_SOURCE_PLAY_TURNS must contain distinct non-negative turns.");
                }
            }
            return turns;
        }

        private static MethodInfo Command(string name, params Type[] parameters)
        {
            return typeof(CheatManager).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic,
                null, parameters, null) ?? throw new MissingMethodException(typeof(CheatManager).FullName, name);
        }

        private void Enter(Stage next, float seconds)
        {
            stage = next;
            deadline = Time.realtimeSinceStartup + seconds;
        }

        private void Finish(bool success, string message)
        {
            if (success)
            {
                log.LogInfo("DEPTH-PASS " + message);
            }
            else
            {
                log.LogError("DEPTH-FAIL " + message);
            }
            quitCode = success ? 0 : 1;
            stage = Stage.Quitting;
            quitStarted = Time.realtimeSinceStartup;
            Application.Quit(quitCode);
        }
    }
}
