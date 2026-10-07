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
                Enter(Stage.NewRun, 90f);
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
                    if (pendingPlay != null) PlayPendingPolicyCard(managers, cards!);
                    else PlayConfiguredCard(cards!);
                }
                return;
            }
            if (stage == Stage.PlayingCard)
            {
                if (Ready(managers, save, combat, cards))
                {
                    if (fullBattle) FullBattleTrace.Active?.CompleteCardPlay();
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
            if (fullBattle && (modifierScenario == "equipment-abilities" || modifierScenario == "equipment" || modifierScenario == "equipment-exhausted" || modifierScenario == "equipment-overflow" || modifierScenario == "equipment-triggers"))
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
            if (fullBattle && !numericModifiersPrepared && (modifierScenario == "rally-triggers" || modifierScenario == "dying-horde-upgrades" || modifierScenario == "horde-upgrades" || modifierScenario == "horde-death" || modifierScenario == "horde-removal" || modifierScenario == "harvest-triggers" || modifierScenario == "horde-statuses" || modifierScenario == "ability-effects" || modifierScenario == "ability-lifecycle" || modifierScenario == "ability-activation" || modifierScenario == "ability-activation-x" || modifierScenario == "ability-activation-lethal" || modifierScenario == "ability-cooldown" || modifierScenario == "ability-cache" || modifierScenario == "sentry" || modifierScenario == "sentry-lethal" || modifierScenario == "companion-boss" || modifierScenario == "numeric-upgrades" || modifierScenario == "dynamic-upgrades" ||
                modifierScenario == "sacrifice-upgrades" || modifierScenario == "hand-upgrades" || modifierScenario == "targeted-hand-upgrades" ||
                modifierScenario == "healing" || modifierScenario == "healing-triggers" || modifierScenario == "room-spells" ||
                modifierScenario == "terminal-spells" || modifierScenario == "post-kill-spells" || modifierScenario == "random-spells" ||
                modifierScenario == "random-status" || modifierScenario == "cross-room-spells" || modifierScenario == "cross-room-targets" || modifierScenario == "attack-buffs" ||
                modifierScenario == "room-capacity" || modifierScenario == "room-capacity-lethal" || modifierScenario == "energy-effects" || modifierScenario == "energy-effects-lethal" || modifierScenario == "bonus-draw" || modifierScenario == "bonus-draw-lethal" || modifierScenario == "x-cost" || modifierScenario == "x-cost-lethal" || modifierScenario == "max-health-spells" || modifierScenario == "max-health-lethal" ||
                modifierScenario == "numeric-ranges" || modifierScenario == "numeric-ranges-lethal" || modifierScenario == "target-filters" || modifierScenario == "drawing" || modifierScenario == "damage-scaling" || modifierScenario == "dynamic-statistics" ||
                modifierScenario == "hand-removal" || modifierScenario == "hand-removal-lethal" || modifierScenario == "generation" || modifierScenario == "generation-lethal" || modifierScenario == "status-scaling" || modifierScenario == "unit-upgrade-scaling" || modifierScenario == "unit-trigger-upgrades" ||
                modifierScenario == "spawn-triggers" || modifierScenario == "spawn-triggers-lethal" || modifierScenario == "unit-turn-begin" || modifierScenario == "team-turn-begin" || modifierScenario == "pre-hand-discard" || modifierScenario == "pre-hand-discard-lethal" || modifierScenario == "clone-upgrade-refresh" || modifierScenario == "pre-combat" || modifierScenario == "pre-combat-lethal" || modifierScenario == "triggered-healing" || modifierScenario == "post-combat-healing" || modifierScenario == "triggered-damage" || modifierScenario == "damage-death-queue" || modifierScenario == "terminal-death-damage" || modifierScenario == "hit-kill" || modifierScenario == "dying-upgrades" || modifierScenario == "attack-triggers" || modifierScenario == "triggered-status"))
            {
                if (combat!.GetTurnCount() != 0) throw new InvalidOperationException("Numeric fixture must start on deployment turn.");
                numericModifiersPrepared = true;
                if (modifierScenario == "generation" || modifierScenario == "generation-lethal")
                    GenerationScenario.Prepare(managers, log, modifierScenario == "generation-lethal");
                else if (modifierScenario == "hand-removal" || modifierScenario == "hand-removal-lethal")
                    HandRemovalScenario.Prepare(managers, log, modifierScenario == "hand-removal-lethal");
                else if (modifierScenario == "energy-effects" || modifierScenario == "energy-effects-lethal") EnergyScenario.Prepare(managers, log, modifierScenario == "energy-effects-lethal");
                else if (modifierScenario == "room-capacity" || modifierScenario == "room-capacity-lethal") RoomCapacityScenario.Prepare(managers, log, modifierScenario == "room-capacity-lethal");
                else if (modifierScenario == "x-cost" || modifierScenario == "x-cost-lethal") CardCostScenario.Prepare(managers, log, modifierScenario == "x-cost-lethal");
                else if (modifierScenario == "bonus-draw" || modifierScenario == "bonus-draw-lethal") BonusDrawScenario.Prepare(managers, log, modifierScenario == "bonus-draw-lethal");
                else if (modifierScenario == "ability-activation" || modifierScenario == "ability-activation-x" || modifierScenario == "ability-activation-lethal") AbilityActivationScenario.Prepare(managers, log, modifierScenario == "ability-activation-x", modifierScenario == "ability-activation-lethal");
                else if (modifierScenario == "rally-triggers") RallyScenario.Prepare(managers, log);
                else if (modifierScenario == "dying-horde-upgrades") DyingHordeUpgradeScenario.Prepare(managers, log);
                else if (modifierScenario == "horde-upgrades") HordeUpgradeScenario.Prepare(managers, log);
                else if (modifierScenario == "horde-death") HordeDeathScenario.Prepare(managers, log);
                else if (modifierScenario == "horde-removal") HordeRemovalScenario.Prepare(managers, log);
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
                else if (modifierScenario == "hand-upgrades" || modifierScenario == "targeted-hand-upgrades")
                    HandUpgradeScenario.Prepare(managers, log, modifierScenario == "targeted-hand-upgrades");
                else if (modifierScenario == "dynamic-upgrades" || modifierScenario == "sacrifice-upgrades")
                    DynamicUpgradeScenario.Prepare(managers, log, modifierScenario == "sacrifice-upgrades");
                else NumericUpgradeScenario.Prepare(managers, log);
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
                BattleTurnState decision = FullBattleTrace.Active!.CaptureDecision();
                pendingPlay = Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE_POLICY") == "units-spells-and-junk"
                    ? BattleActionModel.ChooseUnitSpellAndJunkPlay(decision) : BattleActionModel.ChooseUnitAndJunkPlay(decision);
                if (modifierScenario == "equipment-abilities" || modifierScenario == "ability-effects" || modifierScenario == "ability-activation" || modifierScenario == "ability-activation-x" || modifierScenario == "ability-activation-lethal")
                    pendingPlay = UnitAbilityModel.ChooseAbilityThenCards(decision);
                if (pendingPlay != null)
                {
                    RoomManager rooms = managers.GetRoomManager()!;
                    if (rooms.GetSelectedRoom() != pendingPlay.RoomIndex)
                    {
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
            if (action.ActivatorUnitId > 0)
            {
                CharacterState actor = FullBattleTrace.Active!.KnownUnits.Single(unit => FullBattleTrace.Active.UnitId(unit) == action.ActivatorUnitId);
                FullBattleTrace.Active.BeginCardPlay(action);
                if (!actor.ActivateUnitAbility(cards)) throw new InvalidOperationException("Native unit ability activation failed.");
                log.LogInfo("DEPTH-POLICY-ABILITY card=" + action.CardInstanceId + " unit=" + action.ActivatorUnitId + " room=" + action.RoomIndex);
                pendingPlay = null; Enter(Stage.PlayingCard, 30f); return;
            }
            int index = cards.GetHand().FindIndex(card => FullBattleTrace.Active!.CardId(card) == action.CardInstanceId);
            if (index < 0) throw new InvalidOperationException("Policy card disappeared from hand.");
            RoomState room = managers.GetRoomManager()!.GetRoom(action.RoomIndex);
            SpawnPoint? drop = action.PlayerPosition < 0 ? null : room.GetMonsterPoint(action.PlayerPosition);
            if (action.TargetUnitId > 0)
            {
                var units = new List<CharacterState>(); room.AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
                CharacterState target = units.Single(unit => FullBattleTrace.Active!.UnitId(unit) == action.TargetUnitId);
                drop = target.GetSpawnPoint();
            }
            if (!cards.CanPlayHandCard(cards.GetHand()[index], action.RoomIndex, drop, null, null, out var error))
                throw new InvalidOperationException("The native game rejected a modeled legal play: " + error);
            FullBattleTrace.Active!.BeginCardPlay(action);
            if (!cards.PlayCard(index, drop, ref error)) throw new InvalidOperationException("Policy card play failed: " + error);
            log.LogInfo("DEPTH-POLICY-PLAY card=" + action.CardInstanceId + " room=" + action.RoomIndex + " position=" + action.PlayerPosition + " target=" + action.TargetUnitId);
            pendingPlay = null;
            Enter(Stage.PlayingCard, 30f);
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
            return !LoadingScreen.IsWorking() && !save.PreviewMode && save.GetGameSequence() == SaveData.GameSequence.InBattle &&
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
