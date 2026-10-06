using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using ShinyShoe;
using ShinyShoe.Loading;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class TwoTurnScenario
    {
        private enum Stage
        {
            Menu,
            NewRun,
            NaturalMap,
            NaturalIntro,
            Battle,
            SelectingRoom,
            PlayingFirstCard,
            FirstTurn,
            SecondTurn,
            Undoing,
            Restarting,
            Quitting
        }

        private const int Seed = 424242;
        private const string StewardCardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private readonly ManualLogSource log;
        private readonly string battleId;
        private readonly bool playFirstCard;
        private readonly bool directPlay;
        private readonly bool undoAfterPlay;
        private readonly bool restartAfterSecondTurn;
        private readonly bool branchOnTurnOne;
        private readonly bool playOnTurnOne;
        private readonly bool skipFtue;
        private readonly UnitPlayModelProbe? modelProbe;
        private readonly MethodInfo setSeed;
        private readonly MethodInfo newRun;
        private readonly MethodInfo completeFtue;
        private readonly MethodInfo startRunFromSetup;
        private readonly MethodInfo startBattle;
        private readonly MethodInfo endTurn;
        private readonly FieldInfo fightAvailable;
        private readonly FieldInfo activeDialogStack;
        private readonly FieldInfo dialogData;
        private Stage stage;
        private float deadline;
        private float quitStarted;
        private float lastHeartbeat;
        private int quitCode;
        private int initialTurn;
        private bool mainMenuRequested;
        private bool firstCardPlayed;
        private float lastMapAttempt;
        private float lastDialogAttempt;
        private float undoStarted;
        private float restartStarted;
        private string? beforeSignature;
        private string? afterFirstSignature;
        private bool runSetupRequested;
        private bool restarted;
        private bool restartRoomSelectionRequested;
        private string? lastDialogContent;
        private Dialog.Data? lastClickedDialogData;

        internal TwoTurnScenario(ManualLogSource log)
        {
            this.log = log;
            battleId = Environment.GetEnvironmentVariable("MT2_PROBE_BATTLE_ID") ?? string.Empty;
            playFirstCard = Environment.GetEnvironmentVariable("MT2_PROBE_PLAY_CARD") == "1";
            directPlay = Environment.GetEnvironmentVariable("MT2_PROBE_DIRECT_PLAY") == "1";
            undoAfterPlay = Environment.GetEnvironmentVariable("MT2_PROBE_UNDO") == "1";
            restartAfterSecondTurn = Environment.GetEnvironmentVariable("MT2_PROBE_RESTART") == "1";
            branchOnTurnOne = Environment.GetEnvironmentVariable("MT2_PROBE_BRANCH_TURN1") == "1";
            playOnTurnOne = Environment.GetEnvironmentVariable("MT2_PROBE_PLAY_TURN1") == "1";
            skipFtue = Environment.GetEnvironmentVariable("MT2_PROBE_SKIP_FTUE") == "1";
            if (Environment.GetEnvironmentVariable("MT2_PROBE_MODEL_UNIT_PLAY") == "1")
            {
                if (!playFirstCard || !directPlay || undoAfterPlay || restartAfterSecondTurn || playOnTurnOne)
                {
                    throw new ArgumentException("The unit-play model requires a single direct turn-zero card play.");
                }
                modelProbe = new UnitPlayModelProbe(log);
            }
            setSeed = Command("Command_SetSeed", typeof(string));
            newRun = Command("Command_NewRun");
            completeFtue = Command("Command_CompleteFTUE");
            startRunFromSetup = typeof(RunSetupScreen).GetMethod("StartRun", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(RunSetupScreen).FullName, "StartRun");
            startBattle = Command("Command_StartBattle", typeof(string));
            endTurn = Command("Command_EndTurn");
            fightAvailable = typeof(BattleIntroScreen).GetField("isFightButtonAvailable", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(BattleIntroScreen).FullName, "isFightButtonAvailable");
            activeDialogStack = typeof(DialogScreen).GetField("activeDialogStack", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(DialogScreen).FullName, "activeDialogStack");
            dialogData = typeof(Dialog).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(Dialog).FullName, "data");
            Enter(Stage.Menu, 120f);
        }

        internal void Tick()
        {
            if (Time.realtimeSinceStartup - lastHeartbeat >= 10f)
            {
                lastHeartbeat = Time.realtimeSinceStartup;
                AllGameManagers? current = AllGameManagers.Instance;
                log.LogInfo("SMOKE-WAIT stage=" + stage + " managers=" + (current != null) +
                    " user=" + (current?.GetSaveManager()?.UserState.ToString() ?? "none") +
                    " menu=" + (current?.GetScreenManager()?.GetScreenActive(ScreenName.MainMenu).ToString() ?? "none") +
                    " top=" + (current?.GetScreenManager()?.GetTopScreen().ToString() ?? "none") +
                    " loading=" + LoadingScreen.IsWorking() +
                    " sequence=" + (current?.GetSaveManager()?.GetGameSequence().ToString() ?? "none") +
                    " busy=" + CheatManager.IsBusy);
            }
            if (stage == Stage.Quitting)
            {
                if (Time.realtimeSinceStartup - quitStarted > 5f)
                {
                    Environment.Exit(quitCode);
                }
                return;
            }

            if (Time.realtimeSinceStartup > deadline)
            {
                Finish(false, "Timed out in stage " + stage);
                return;
            }

            try
            {
                Step();
            }
            catch (Exception ex)
            {
                Finish(false, "Scenario error in " + stage + ": " + (ex is TargetInvocationException ? ex.InnerException ?? ex : ex));
            }
        }

        private void Step()
        {
            AllGameManagers? managers = AllGameManagers.Instance;
            if (managers == null)
            {
                return;
            }

            SaveManager save = managers.GetSaveManager();
            ScreenManager screen = managers.GetScreenManager();
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
                        log.LogInfo("Smoke: moved isolated profile from IntroStart to main menu.");
                    }
                    return;
                }
                CheatManager.SetupConsoleCommands(save, screen);
                if (skipFtue)
                {
                    Invoke(completeFtue);
                    log.LogInfo("Smoke: FTUE completion set in isolated profile.");
                }
                Invoke(setSeed, Seed.ToString());
                Invoke(newRun);
                log.LogInfo("Smoke: fixed seed " + Seed + "; new run requested.");
                Enter(Stage.NewRun, 90f);
                return;
            }

            if (stage == Stage.NewRun)
            {
                if (skipFtue && !LoadingScreen.IsWorking() && screen.GetScreenActive(ScreenName.RunOpening) &&
                    screen.GetScreen(ScreenName.RunOpening) is RunOpeningScreen opening &&
                    ReplayManager.TryClickButton(opening.GetConfirmButton()))
                {
                    log.LogInfo("Smoke: confirmed run opening.");
                    return;
                }
                if (skipFtue && !runSetupRequested && !LoadingScreen.IsWorking() &&
                    screen.GetScreenActive(ScreenName.RunSetup) &&
                    screen.GetScreen(ScreenName.RunSetup) is RunSetupScreen runSetup)
                {
                    runSetupRequested = true;
                    startRunFromSetup.Invoke(runSetup, null);
                    log.LogInfo("Smoke: confirmed isolated run setup.");
                    return;
                }
                if (LoadingScreen.IsWorking() || save.GetRunType() != RunType.Class || save.GetGameSequence() == SaveData.GameSequence.Initial)
                {
                    return;
                }
                if (save.GetGameSequence() == SaveData.GameSequence.InBattle)
                {
                    log.LogInfo("Smoke: tutorial battle started by the game.");
                    Enter(Stage.Battle, 90f);
                    return;
                }
                if (battleId == "natural")
                {
                    log.LogInfo("Smoke: entering battle through the map UI.");
                    Enter(Stage.NaturalMap, 120f);
                    return;
                }
                string selectedBattle = battleId == "first-nonempty" ? FindNonemptyBattle(save) : battleId;
                Invoke(startBattle, selectedBattle);
                log.LogInfo("Smoke: battle requested: " + (selectedBattle.Length == 0 ? "blank" : selectedBattle));
                Enter(Stage.Battle, 90f);
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
                    if (dialog?.GetButton1() != null && ReplayManager.TryClickButton(dialog.GetButton1()))
                    {
                        log.LogInfo("Smoke: confirmed map battle warning.");
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
                if (map != null && map.TryGetBattleNodeUI(section, out MapBattleNodeUI node) &&
                    ReplayManager.TryClickButton(node))
                {
                    log.LogInfo("Smoke: clicked map battle node in section " + section + ".");
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
                    log.LogInfo("Smoke: started battle through the intro UI.");
                    Enter(Stage.Battle, 120f);
                }
                return;
            }

            if (stage != Stage.Undoing && screen.GetTopScreen() == ScreenName.Dialog)
            {
                if (Time.realtimeSinceStartup - lastDialogAttempt >= 0.5f)
                {
                    lastDialogAttempt = Time.realtimeSinceStartup;
                    DialogScreen? dialog = screen.GetScreen(ScreenName.Dialog) as DialogScreen;
                    List<Dialog>? stack = dialog == null ? null : activeDialogStack.GetValue(dialog) as List<Dialog>;
                    Dialog.Data? data = stack?.Count > 0 ? dialogData.GetValue(stack[stack.Count - 1]) as Dialog.Data : null;
                    if (data != null && data.content != lastDialogContent)
                    {
                        lastDialogContent = data.content;
                        log.LogInfo("SMOKE-DIALOG content=" + data.content.Substring(0, Math.Min(data.content.Length, 240)) +
                            " button=" + data.button1Text + " autoClose=" + data.closeAutomatically +
                            " callback=" + (data.callback1 != null));
                    }
                    if (data != null && !ReferenceEquals(data, lastClickedDialogData) &&
                        dialog?.GetButton1() != null && ReplayManager.TryClickButton(dialog.GetButton1()))
                    {
                        lastClickedDialogData = data;
                        log.LogInfo("Smoke: confirmed battle dialog.");
                    }
                }
                return;
            }

            CombatManager? combat = managers.GetCombatManager();
            CardManager? cards = managers.GetCardManager();
            if (stage == Stage.Undoing)
            {
                if (!save.IsInUndoMode && !LoadingScreen.IsWorking() &&
                    save.GetGameSequence() == SaveData.GameSequence.InBattle &&
                    combat != null && cards != null && combat.GetTurnCount() == initialTurn &&
                    combat.ShouldShowEndTurnButton() && combat.HaveCardsBeenDrawnForCurrentTurn() &&
                    !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false))
                {
                    string restored = Snapshot("after-undo", managers, save, combat, cards);
                    log.LogInfo("SMOKE-UNDO-ELAPSED " + (int)((Time.realtimeSinceStartup - undoStarted) * 1000f) + "ms");
                    if (restored != beforeSignature)
                    {
                        Finish(false, "Undo state differed from the state before card play.");
                        return;
                    }
                    log.LogInfo("SMOKE-RESTORE-MATCH Current-turn state and seeded RNG restored.");
                    Invoke(endTurn);
                    Enter(Stage.FirstTurn, 60f);
                }
                return;
            }
            if (stage == Stage.Restarting)
            {
                if (!save.IsInRestartBattleMode && !LoadingScreen.IsWorking() &&
                    save.GetGameSequence() == SaveData.GameSequence.InBattle &&
                    combat != null && cards != null && combat.GetTurnCount() == initialTurn &&
                    combat.ShouldShowEndTurnButton() && combat.HaveCardsBeenDrawnForCurrentTurn() &&
                    !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false))
                {
                    string restored = Snapshot("after-restart", managers, save, combat, cards);
                    log.LogInfo("SMOKE-RESTART-ELAPSED " + (int)((Time.realtimeSinceStartup - restartStarted) * 1000f) + "ms");
                    if (restored != beforeSignature)
                    {
                        Finish(false, "Restart state differed from the original battle start.");
                        return;
                    }
                    log.LogInfo("SMOKE-RESTART-MATCH Battle-start state and seeded RNG restored.");
                    if (branchOnTurnOne)
                    {
                        Invoke(endTurn);
                        Enter(Stage.FirstTurn, 60f);
                        return;
                    }
                    RoomManager? rooms = managers.GetRoomManager();
                    if (rooms == null)
                    {
                        Finish(false, "Room manager unavailable after restart.");
                        return;
                    }
                    if (rooms.GetSelectedRoom() != 0)
                    {
                        if (!restartRoomSelectionRequested)
                        {
                            restartRoomSelectionRequested = true;
                            save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                        }
                        return;
                    }
                    PlayFirstCardDirect(managers, save, combat!, cards);
                }
                return;
            }
            if (stage == Stage.Battle && save.GetGameSequence() != SaveData.GameSequence.InBattle)
            {
                return;
            }
            if (save.GetGameSequence() != SaveData.GameSequence.InBattle && stage != Stage.Battle)
            {
                Finish(false, "Battle ended before two turns completed.");
                return;
            }
            if (LoadingScreen.IsWorking() || combat == null || cards == null || !combat.ShouldShowEndTurnButton() ||
                !combat.HaveCardsBeenDrawnForCurrentTurn() || (managers.GetHandUI()?.AreAnyCardsAnimating() ?? false))
            {
                return;
            }

            if (stage == Stage.Battle)
            {
                initialTurn = combat.GetTurnCount();
                beforeSignature = Snapshot("before", managers, save, combat, cards);
                log.LogInfo("Smoke: scenario=" + save.GetCurrentScenarioData()?.name +
                    " testScenario=" + save.InTestScenario +
                    " replayEntries=" + save.GetReplayData().GetReplayEntries().Count +
                    " battleOffset=" + save.GetReplayData().GetNumEntriesUpUntilBattle());
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                if (playFirstCard)
                {
                    if (directPlay)
                    {
                        RoomManager? rooms = managers.GetRoomManager();
                        if (rooms == null)
                        {
                            Finish(false, "Room manager unavailable for direct play.");
                            return;
                        }
                        if (rooms.GetSelectedRoom() != 0)
                        {
                            save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                            Enter(Stage.SelectingRoom, 20f);
                            return;
                        }
                        PlayFirstCardDirect(managers, save, combat, cards);
                        return;
                    }
                    int cardIndex = -1;
                    List<CardState> hand = cards.GetHand();
                    for (int index = 0; index < hand.Count; index++)
                    {
                        if (hand[index].GetCardDataID() == StewardCardId &&
                            cards.CanPlayHandCard(index, 0, out _))
                        {
                            cardIndex = index;
                            break;
                        }
                    }
                    HandUI? handUI = managers.GetHandUI();
                    if (cardIndex < 0 || handUI == null)
                    {
                        Finish(false, "No legal TrainStewardShield card in the first hand.");
                        return;
                    }
                    log.LogInfo("Smoke: playing TrainStewardShield from hand index " + cardIndex + " in room 0.");
                    save.StartCoroutine(handUI.PlayCard(cardIndex, 0, null, () => firstCardPlayed = true));
                    Enter(Stage.PlayingFirstCard, 30f);
                    return;
                }
                Invoke(endTurn);
                Enter(Stage.FirstTurn, 60f);
            }
            else if (stage == Stage.SelectingRoom && managers.GetRoomManager()?.GetSelectedRoom() == 0)
            {
                PlayFirstCardDirect(managers, save, combat, cards);
            }
            else if (stage == Stage.PlayingFirstCard && firstCardPlayed &&
                !combat.IsRunningTriggerQueue && !managers.GetReplayManager().IsCardPlaying())
            {
                Snapshot("after-play", managers, save, combat, cards);
                if (modelProbe != null && !modelProbe.Complete(managers, save, combat, cards,
                    out string modelError))
                {
                    Finish(false, modelError);
                    return;
                }
                if (undoAfterPlay)
                {
                    log.LogInfo("Smoke: undoing card play within the current turn.");
                    undoStarted = Time.realtimeSinceStartup;
                    save.UndoTurn();
                    Enter(Stage.Undoing, 180f);
                }
                else
                {
                    Invoke(endTurn);
                    Enter((restarted && branchOnTurnOne) || playOnTurnOne ? Stage.SecondTurn : Stage.FirstTurn, 60f);
                }
            }
            else if (stage == Stage.FirstTurn && combat.GetTurnCount() >= initialTurn + 1)
            {
                string reached = Snapshot(restarted && branchOnTurnOne ? "second-branch-after-first" : "after-first",
                    managers, save, combat, cards);
                if (!restarted)
                {
                    afterFirstSignature = reached;
                }
                if ((restarted && branchOnTurnOne) || (!restarted && playOnTurnOne))
                {
                    if (restarted && reached != afterFirstSignature)
                    {
                        Finish(false, "Turn-one state differed after replaying the first turn.");
                        return;
                    }
                    if (restarted)
                    {
                        log.LogInfo("SMOKE-TURN1-MATCH Intermediate state and seeded RNG restored.");
                    }
                    RoomManager? rooms = managers.GetRoomManager();
                    if (rooms == null)
                    {
                        Finish(false, "Room manager unavailable on turn one.");
                        return;
                    }
                    if (rooms.GetSelectedRoom() != 0)
                    {
                        save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                        Enter(Stage.SelectingRoom, 20f);
                    }
                    else
                    {
                        PlayFirstCardDirect(managers, save, combat, cards);
                    }
                    return;
                }
                Invoke(endTurn);
                Enter(Stage.SecondTurn, 60f);
            }
            else if (stage == Stage.SecondTurn && combat.GetTurnCount() >= initialTurn + 2)
            {
                Snapshot(restarted ? "second-branch-after-second" : "after-second", managers, save, combat, cards);
                if (restartAfterSecondTurn && !restarted)
                {
                    restarted = true;
                    restartStarted = Time.realtimeSinceStartup;
                    log.LogInfo("Smoke: restarting the battle after two turns.");
                    save.RestartBattle();
                    Enter(Stage.Restarting, 180f);
                }
                else
                {
                    Finish(true, "Two turns completed from turn " + initialTurn + " to " + combat.GetTurnCount() +
                        (restarted ? " after restart." : "."));
                }
            }
        }

        private void PlayFirstCardDirect(AllGameManagers managers, SaveManager save,
            CombatManager combat, CardManager cards)
        {
            List<CardState> hand = cards.GetHand();
            for (int index = 0; index < hand.Count; index++)
            {
                if (hand[index].GetCardDataID() != StewardCardId ||
                    !cards.CanPlayHandCard(index, 0, out CommonSelectionBehavior.SelectionError error))
                {
                    continue;
                }
                if (modelProbe != null && !modelProbe.Begin(managers, save, combat, cards, index,
                    out string modelError))
                {
                    Finish(false, modelError);
                    return;
                }
                if (!cards.PlayCard(index, null, ref error))
                {
                    Finish(false, "Direct card play failed: " + error);
                    return;
                }
                firstCardPlayed = true;
                log.LogInfo("Smoke: directly played TrainStewardShield from hand index " + index + " in room 0.");
                Enter(Stage.PlayingFirstCard, 30f);
                return;
            }
            Finish(false, "No legal TrainStewardShield card in the first hand.");
        }

        private string FindNonemptyBattle(SaveManager save)
        {
            ScenarioData? fallback = null;
            foreach (ScenarioData candidate in save.GetAllGameData().GetAllScenarioDatas())
            {
                if (candidate == null || candidate.IsArchived || candidate.GetSpawnPattern() == null ||
                    candidate.GetSpawnPattern().GetNumGroups(save, true) == 0)
                {
                    continue;
                }
                if (candidate.name.StartsWith("Level1", StringComparison.OrdinalIgnoreCase))
                {
                    log.LogInfo("Smoke: selected nonempty scenario " + candidate.name + " groups=" +
                        candidate.GetSpawnPattern().GetNumGroups(save, true));
                    return candidate.name;
                }
                fallback ??= candidate;
            }
            if (fallback == null)
            {
                throw new InvalidOperationException("No active nonempty battle scenario was found.");
            }
            log.LogInfo("Smoke: selected fallback nonempty scenario " + fallback.name + " groups=" +
                fallback.GetSpawnPattern().GetNumGroups(save, true));
            return fallback.name;
        }

        private string Snapshot(string label, AllGameManagers managers, SaveManager save, CombatManager combat, CardManager cards)
        {
            return CaptureSnapshot(label, managers, save, combat, cards, log);
        }

        internal static string CaptureSnapshot(string label, AllGameManagers managers, SaveManager save,
            CombatManager combat, CardManager cards, ManualLogSource log)
        {
            var state = new StringBuilder(1024);
            state.Append("SMOKE-SNAPSHOT ").Append(label)
                .Append(" turn=").Append(combat.GetTurnCount())
                .Append(" phase=").Append(combat.GetCombatPhase())
                .Append(" scenario=").Append(save.GetCurrentScenarioData()?.name)
                .Append(" pyre=").Append(save.GetTowerHP()).Append('/').Append(save.GetMaxTowerHP())
                .Append(" energy=").Append(managers.GetPlayerManager()?.GetEnergy() ?? -1)
                .Append(" gold=").Append(save.GetGold())
                .Append(" forge=").Append(save.GetForgePoints())
                .Append(" hoard=").Append(save.GetDragonsHoardAmount())
                .Append(" drawMod=").Append(cards.GetDrawCountModifier());
            HeroManager? heroManager = managers.GetHeroManager();
            FieldInfo? spawnPhase = typeof(HeroManager).GetField("spawnPatternPhase",
                BindingFlags.Instance | BindingFlags.NonPublic);
            state.Append(" spawnPhase=").Append(heroManager == null ? -1 : spawnPhase?.GetValue(heroManager) ?? -1);
            AppendCards(state, "hand", cards.GetHand());
            AppendCards(state, "draw", cards.GetDrawPile());
            AppendCards(state, "discard", cards.GetDiscardPile());
            AppendCards(state, "discardBuffer", cards.GetDiscardBufferPile());
            AppendCards(state, "exhausted", cards.GetExhaustedPile());
            AppendCards(state, "eaten", cards.GetEatenPile());
            AppendCards(state, "purged", cards.GetPurgedPile());
            AppendUnits(state, "heroes", heroManager);
            AppendUnits(state, "monsters", managers.GetMonsterManager());
            AppendRooms(state, managers.GetRoomManager());
            string stateText = state.ToString();
            log.LogInfo(stateText);

            var rng = new StringBuilder(512).Append("SMOKE-RNG ").Append(label);
            FieldInfo[] fields = typeof(UnityEngine.Random.State).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
            {
                object value = RandomManager.GetState(id);
                rng.Append(' ').Append(id).Append('=');
                if (fields.Length == 0)
                {
                    rng.Append(value.GetHashCode());
                    continue;
                }
                rng.Append('{');
                foreach (FieldInfo field in fields)
                {
                    rng.Append(field.Name).Append(':').Append(field.GetValue(value)).Append(',');
                }
                rng.Append('}');
            }
            string rngText = rng.ToString();
            log.LogInfo(rngText);
            return stateText.Substring(("SMOKE-SNAPSHOT " + label).Length) + "\n" +
                Regex.Replace(rngText.Substring(("SMOKE-RNG " + label).Length), @"NonDeterministic=\{[^}]*\} ?", "");
        }

        private static void AppendCards(StringBuilder text, string name, List<CardState> cards)
        {
            text.Append(' ').Append(name).Append("=[");
            foreach (CardState card in cards)
            {
                AppendCard(text, card);
                text.Append(',');
            }
            text.Append(']');
        }

        private static void AppendCard(StringBuilder text, CardState card)
        {
            text.Append(card.GetCardDataID()).Append(':').Append(card.GetCurrentScenarioPlayCount())
                .Append(':').Append(card.GetLastPlayedCost()).Append(':').Append(card.GetLastForgedAmount());
            AppendUpgrades(text, card.GetCardStateModifiers().GetCardUpgrades());
            AppendUpgrades(text, card.GetTemporaryCardStateModifiers().GetCardUpgrades());
        }

        private static void AppendUpgrades(StringBuilder text, List<CardUpgradeState> upgrades)
        {
            text.Append('{');
            foreach (CardUpgradeState upgrade in upgrades)
            {
                text.Append(upgrade.GetCardUpgradeDataId()).Append(':')
                    .Append(upgrade.GetAttackDamage()).Append(':')
                    .Append(upgrade.GetAdditionalHP()).Append(':')
                    .Append(upgrade.GetCostReduction()).Append(',');
            }
            text.Append('}');
        }

        private static void AppendUnits(StringBuilder text, string name, ICharacterManager? characters)
        {
            text.Append(' ').Append(name).Append("=[");
            if (characters != null)
            {
                for (int index = 0; index < characters.GetNumCharacters(); index++)
                {
                    CharacterState character = characters.GetCharacter(index);
                    SpawnPoint? point = character.GetSpawnPoint();
                    text.Append(character.GetSourceCharacterData()?.GetAssetKey())
                        .Append('@').Append(point?.GetRoomOwner()?.GetRoomIndex() ?? -1)
                        .Append(':').Append(point?.GetIndexInRoom() ?? -1)
                        .Append(':').Append(character.GetAttackDamage())
                        .Append(':').Append(character.GetHP()).Append('/').Append(character.GetMaxHP())
                        .Append(':').Append(character.GetUnitAbilityCooldown());
                    var statuses = new List<CharacterState.StatusEffectStack>();
                    character.GetStatusEffects(ref statuses);
                    var statusValues = new List<string>(statuses.Count);
                    foreach (CharacterState.StatusEffectStack status in statuses)
                    {
                        StatusEffectState effect = status.State;
                        statusValues.Add(effect.GetStatusId() + ':' + status.Count + ':' + effect.GetParamInt() + ':' +
                            effect.GetParamSecondaryInt() + ':' + effect.GetParamStr() + ':' +
                            effect.GetParamFloat().ToString("R", CultureInfo.InvariantCulture));
                    }
                    statusValues.Sort(StringComparer.Ordinal);
                    text.Append("{statuses:").Append(string.Join(";", statusValues)).Append('}');
                    text.Append("{equipment:");
                    foreach (CardState equipment in character.GetEquipment())
                    {
                        AppendCard(text, equipment);
                        text.Append(';');
                    }
                    text.Append('}');
                    text.Append("{upgrades:");
                    foreach (CardUpgradeState upgrade in character.GetAppliedCardUpgrades())
                    {
                        text.Append(upgrade.GetCardUpgradeDataId()).Append(';');
                    }
                    text.Append("},");
                }
            }
            text.Append(']');
        }

        private static void AppendRooms(StringBuilder text, RoomManager? rooms)
        {
            text.Append(" rooms=[");
            if (rooms != null)
            {
                for (int index = 0; index < rooms.GetNumRooms(); index++)
                {
                    RoomState room = rooms.GetRoom(index);
                    CapacityInfo heroes = room.GetCapacityInfo(Team.Type.Heroes);
                    CapacityInfo monsters = room.GetCapacityInfo(Team.Type.Monsters);
                    text.Append(index).Append(':').Append(heroes.count).Append('/')
                        .Append(heroes.max).Append(':').Append(heroes.nextSpawn)
                        .Append(':').Append(monsters.count).Append('/')
                        .Append(monsters.max).Append(':').Append(monsters.nextSpawn)
                        .Append(':').Append(room.GetCurrentCorruption()).Append('/')
                        .Append(room.GetMaxCorruption()).Append('{');
                    foreach (TrainRoomAttachmentState attachment in room.Attachments)
                    {
                        text.Append(attachment.AttachType).Append(':').Append(attachment.IsActive)
                            .Append(':').Append(attachment.CurrentAbilityCooldown).Append(':')
                            .Append(attachment.CardState?.GetCardDataID()).Append(',');
                    }
                    text.Append("},");
                }
            }
            text.Append(']');
        }

        private static MethodInfo Command(string name, params Type[] arguments)
        {
            return typeof(CheatManager).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic, null, arguments, null)
                ?? throw new MissingMethodException(typeof(CheatManager).FullName, name);
        }

        private static void Invoke(MethodInfo command, params object[] arguments)
        {
            command.Invoke(null, arguments);
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
                log.LogInfo("SMOKE-PASS " + message);
            }
            else
            {
                log.LogError("SMOKE-FAIL " + message);
            }
            quitCode = success ? 0 : 1;
            stage = Stage.Quitting;
            quitStarted = Time.realtimeSinceStartup;
            Application.Quit(quitCode);
        }
    }
}
