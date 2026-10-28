using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using ShinyShoe;
using ShinyShoe.Loading;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    // Build input saves through the game's own campaign UI. This driver does not
    // claim model equivalence: strict battle probes run separately from checkpoints.
    internal sealed class CampaignProgressScenario
    {
        private static CampaignProgressScenario? active;
        private readonly ManualLogSource log;
        private readonly string auditPath;
        private readonly int stopRing;
        private readonly int seed;
        private float nextAction, lastProgress, heartbeat, quitAt;
        private bool started, setup, menuRequested, checkpointSaving;
        private int? selectingRoom;
        private float roomSelectionRequestedAt;
        private int roomSelectionRetries;
        private string lastScreen = "";
        private int lastTurn = -1, battleDistance = -1;
        private int eventIndex;
        private int quitCode;
        private int savedRing;
        private bool nativeLost;
        private bool? runWon;
        private bool runReturned;
        private readonly bool completeDefeat;
        private readonly HashSet<string> declinedUpgrades = new HashSet<string>();
        private readonly HashSet<string> auditedMerchantOffers = new HashSet<string>();
        private readonly HashSet<string> auditedStoryOffers = new HashSet<string>();
        private readonly HashSet<string> unselectableMerchantOffers = new HashSet<string>();
        private readonly HashSet<string> unselectableStoryChoices = new HashSet<string>();
        private string? pendingMerchantOffer;
        private string? pendingStoryChoice;
        private DeckScreen? enteringDeck;
        private float deckReadyAt;

        internal CampaignProgressScenario(ManualLogSource log, bool afterBattle = false, bool battleWon = true)
        {
            this.log = log; active = this;
            completeDefeat = afterBattle && !battleWon && Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_COMPLETE_DEFEAT") == "1";
            stopRing = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_STOP_RING") ?? "5");
            seed = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_SEED") ?? "424242");
            auditPath = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "campaign-audit.jsonl");
            lastProgress = Time.realtimeSinceStartup;
            Record("start", new { seed, stopRing, resume = Environment.GetEnvironmentVariable("MT2_PROBE_RESUME_RUN") == "1" });
            if (afterBattle)
            {
                started = true;
                Record("battle-replay-complete", new { decisions = RollingAdviceScenario.Records.Count,
                    archive = "full-battle.mt2f", searchNodes = RollingAdviceScenario.Records.Sum(x => x.Nodes) });
                Record("battle-outcome", new { won = battleWon, distance = AllGameManagers.Instance.GetSaveManager().GetCurrentDistance(),
                    pyre = AllGameManagers.Instance.GetPlayerManager().GetTowerHP() });
            }
        }

        private void Record(string kind, object data)
        {
            string json = JsonConvert.SerializeObject(new { index = eventIndex++, kind, data });
            File.AppendAllText(auditPath, json + Environment.NewLine);
            log.LogInfo("CAMPAIGN " + json);
            lastProgress = Time.realtimeSinceStartup;
        }

        internal void Tick()
        {
            if (quitAt > 0) { if (Time.realtimeSinceStartup > quitAt) Environment.Exit(quitCode); return; }
            if (started && Time.realtimeSinceStartup < nextAction) return;
            nextAction = Time.realtimeSinceStartup + .25f;
            try { Step(); }
            catch (Exception ex) { Finish(false, ex.ToString()); }
        }

        private void Step()
        {
            AllGameManagers? m = AllGameManagers.Instance;
            SaveManager? save = m?.GetSaveManager();
            ScreenManager? screens = m?.GetScreenManager();
            if (save == null || screens == null) return;
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_STORY_ONLY") == "1")
            {
                if (save.UserState != SaveManager.SaveManagerUserState.UserLoaded || LoadingScreen.IsWorking()) return;
                ObserveStoryProgression(save, afterEndGame: false);
                Finish(true, "Native story progression observed without starting or resuming a run."); return;
            }
            if (runReturned && runWon.HasValue && !LoadingScreen.IsWorking() &&
                screens.GetTopScreen() is ScreenName.GameOver or ScreenName.Unlock)
            {
                ObserveStoryProgression(save, afterEndGame: true);
                Finish(runWon.Value || completeDefeat && !save.IsVictoriousAfterGameOver() && !SaveManager.WasDevCheatUsed,
                    "Native campaign completed: " + (runWon.Value ? "victory" : "defeat")); return;
            }
            if (nativeLost && !completeDefeat) { Finish(false, "Native campaign battle lost; retained pre-battle checkpoint is unchanged."); return; }
            if (Time.realtimeSinceStartup > heartbeat)
            {
                heartbeat = Time.realtimeSinceStartup + 15f;
                log.LogInfo("CAMPAIGN-HEARTBEAT sequence=" + save.GetGameSequence() + " distance=" + save.GetCurrentDistance() +
                    " screen=" + screens.GetTopScreen() + " loading=" + LoadingScreen.IsWorking() +
                    " started=" + started + " user=" + save.UserState + " preview=" + save.PreviewMode);
                if (save.GetGameSequence() == SaveData.GameSequence.InBattle && m!.GetCombatManager() is CombatManager combat)
                    log.LogInfo("CAMPAIGN-BATTLE-WAIT room=" + m.GetRoomManager().GetSelectedRoom() + " selecting=" + selectingRoom +
                        " endTurn=" + combat.ShouldShowEndTurnButton() + " drawn=" + combat.HaveCardsBeenDrawnForCurrentTurn() +
                        " triggers=" + combat.IsRunningTriggerQueue + " playing=" + m.GetReplayManager().IsCardPlaying() +
                        " animations=" + CardAnimationSettlement.Ready() + " hand=" + (m.GetHandUI()?.AreAnyCardsAnimating() ?? false) +
                        " dirty=" + AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat));
            }
            if (Time.realtimeSinceStartup - lastProgress > 150f) throw new TimeoutException("Campaign made no progress for 150 seconds.");
            if (!started)
            {
                if (save.UserState != SaveManager.SaveManagerUserState.UserLoaded) return;
                if (!screens.GetScreenActive(ScreenName.MainMenu))
                {
                    if (!menuRequested && (screens.GetTopScreen() == ScreenName.IntroStart || screens.GetTopScreen() == ScreenName.Map))
                    { menuRequested = true; screens.ReturnToMainMenu(); }
                    return;
                }
                started = true;
                if (Environment.GetEnvironmentVariable("MT2_PROBE_RESUME_RUN") == "1")
                {
                    if (!save.HasRun(RunType.Class)) throw new InvalidOperationException("Resume requires an existing native run.");
                    save.SetActiveRunType(RunType.Class);
                    save.ContinueRunWithReplay(RunType.Class);
                    Record("resume", new { distance = save.GetCurrentDistance() });
                }
                else
                {
                    CheatManager.SetupConsoleCommands(save, screens);
                    Command("Command_CompleteFTUE").Invoke(null, null);
                    Command("Command_SetSeed", typeof(string)).Invoke(null, new object[] { seed.ToString() });
                    Command("Command_NewRun").Invoke(null, null);
                    Record("new-run", new { seed });
                }
                return;
            }
            if (LoadingScreen.IsWorking()) return;
            string state = screens.GetTopScreen() + ":" + save.GetGameSequence() + ":" + save.GetCurrentDistance();
            if (state != lastScreen)
            {
                lastScreen = state;
                Record("screen", new { state, gold = save.GetGold(), pyre = m!.GetPlayerManager().GetTowerHP(),
                    deck = save.GetDeckState()?.Select(CardDescription).ToList() });
            }
            IScreen screen = screens.GetScreen(screens.GetTopScreen());
            if (screen is not DeckScreen) enteringDeck = null;
            if (screen is MapScreen) pendingStoryChoice = null;
            if (screen is CharacterDialogueScreen characterDialogue)
            {
                // Follow the normal skip-confirmation flow: its coroutine owns
                // dialogue completion and any native story reward.
                characterDialogue.ApplyScreenInput(null!, null!, InputManager.Controls.SkipDialogue);
                Record("skip-character-dialogue", new { input = "SkipDialogue" });
                return;
            }
            if (screen is DialogScreen dialog) { Click(dialog.GetButton1(), "dialog"); return; }
            if (screen is UnlockScreen unlock)
            { Click(unlock.GetDefaultGameUISelectable() as GameUISelectableButton, "collect-unlock"); return; }
            if (screen is RunOpeningScreen opening) { Click(opening.GetConfirmButton(), "opening"); return; }
            if (screen is RunSetupScreen runSetup)
            {
                if (!setup)
                {
                    setup = true;
                    NativeStartingClans.Select(runSetup, save, log);
#if DEVELOPMENT_PROBE
                    StartingCovenantScenario.Select(runSetup, save);
#endif
                    AccessTools.Method(typeof(RunSetupScreen), "StartRun").Invoke(runSetup, null);
                    Record("run-setup", new { });
                }
                return;
            }
            if (screen is ChampionUpgradeScreen champion)
            {
                var offers = Field<List<UpgradeCardChoiceItem>>(champion, "upgradeChoiceItems");
                UpgradeCardChoiceItem? choice = offers.Where(x => x.gameObject.activeInHierarchy && x.upgradeData != null)
                    .OrderByDescending(x => CardValue(x.upgradeData.postCardState)).FirstOrDefault();
                if (choice != null)
                    Click(choice.SelectableUI as GameUISelectableButton, "champion-upgrade", new {
                        offers = offers.Where(x => x.upgradeData != null).Select(x => CardDescription(x.upgradeData.postCardState)).ToList(),
                        selected = CardDescription(choice.upgradeData.postCardState) });
                else Click(champion.GetDefaultGameUISelectable() as GameUISelectableButton, "champion-no-upgrade");
                return;
            }
            if (screen is DraftScreenBase draft)
            {
                var offers = Field<List<IDraftableUI>>(draft, "draftItems");
                var ranked = offers.Where(x => x.rootTransform.gameObject.activeInHierarchy)
                    .OrderByDescending(x => x is CardUI card ? CardValue(card.GetCardState()) : 0).ToList();
                int rank = int.TryParse(Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_DRAFT_CHOICE_RANK"), out int configuredRank)
                    ? Math.Max(0, Math.Min(configuredRank, 2)) : 0;
                IDraftableUI? choice = ranked.Count == 0 ? null : ranked[Math.Min(rank, ranked.Count - 1)];
                if (choice != null) Click(choice.SelectableUI as GameUISelectableButton, "draft", new {
                    offers = offers.Select(DescribeDraft).ToList(), selected = DescribeDraft(choice), rank });
                return;
            }
            if (screen is RewardScreen reward) { Click(reward.GetCollectButton(), "collect-reward"); return; }
            if (screen is MerchantScreen merchant)
            {
                var goods = Field<List<MerchantGoodUIBase>>(merchant, "goodUIs");
                var visible = Enumerable.ToArray(goods.Where(x => x.gameObject.activeInHierarchy && x.GoodState != null));
                string offerKey = save.GetCurrentDistance() + ":" + string.Join(";", visible.Select(x => x.GoodState.RewardData.GetID()));
                bool skipPurchases = Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_SKIP_MERCHANT_PURCHASES") == "1";
                if (auditedMerchantOffers.Add(offerKey)) Record("merchant-offers", new {
                    gold = save.GetGold(), skipPurchases,
                    offers = visible.Select(x => new { reward = x.GoodState.RewardData.GetReplayDataID(),
                        asset = RewardAsset(x.GoodState.RewardData), rewardType = x.GoodState.RewardData.GetType().Name,
                        cost = x.GoodState.GetCost(Field<MerchantData>(x, "merchantData")),
                        x.Affordable, x.Purchasable, x.Claimed, x.GoodState.Unlocked }).ToList() });
                MerchantGoodUIBase? buy = skipPurchases ? null : goods.Where(x => x.gameObject.activeInHierarchy && x.GoodState != null &&
                    x.Affordable && x.Purchasable && !x.Claimed && x.GoodState.Unlocked &&
                    !unselectableMerchantOffers.Contains(save.GetCurrentDistance() + ":" + x.GoodState.RewardData.GetReplayDataID()) &&
                    x is MerchantGoodDetailsUI && AcceptUpgrade(x)).FirstOrDefault();
                if (buy != null)
                {
                    if (Click(buy.SelectableUI as GameUISelectableButton, "buy", new {
                        gold = save.GetGold(), reward = buy.GoodState.RewardData.GetReplayDataID(),
                        rewardType = buy.GoodState.RewardData.GetType().Name }))
                        pendingMerchantOffer = save.GetCurrentDistance() + ":" + buy.GoodState.RewardData.GetReplayDataID();
                }
                else Click(merchant.GetCloseButton(), "leave-merchant");
                return;
            }
            if (screen is DeckScreen deck)
            {
                // Card UIs are initialized by staggered native appearance callbacks.
                // An empty candidate list on the first frame is not an empty reward.
                if (!ReferenceEquals(enteringDeck, deck))
                { enteringDeck = deck; deckReadyAt = Time.realtimeSinceStartup + 1.5f; return; }
                if (Time.realtimeSinceStartup < deckReadyAt || Field<bool>(deck, "waitingForAnimation")) return;
                var mode = Field<DeckScreen.Mode>(deck, "mode");
                string source = Field<GrantableRewardData.Source>(deck, "rewardSource").ToString();
                if (deck.GetSelectedCardIndex() >= 0)
                {
                    if (Click(deck.GetConfirmButton(), "confirm-deck-choice", new { mode = mode.ToString(),
                        source })) { pendingMerchantOffer = null; pendingStoryChoice = null; }
                    return;
                }
                var infos = Field<IEnumerable>(deck, "cardInfos").Cast<object>();
                bool purge = mode == DeckScreen.Mode.Purge;
                var candidates = infos.Select(x => Field<CardUI>(x, "cardUI"))
                    .Where(x => x.gameObject.activeInHierarchy && x.IsCardInteractable())
                    .OrderByDescending(x => (purge ? -1 : 1) * CardValue(x.GetCardState()));
                foreach (CardUI card in candidates)
                    if (Click(card.SelectableUI as GameUISelectableButton, "deck-choice", new {
                        card = CardDescription(card.GetCardState()), mode = mode.ToString(),
                        source, allowChampion = Field<bool>(deck, "allowSelectChampion"),
                        eligible = candidates.Select(x => CardDescription(x.GetCardState())).ToList() })) return;
                if (Click(deck.GetCloseButton(), "close-deck", new { mode = mode.ToString(), source }))
                {
                    if (pendingStoryChoice != null)
                    {
                        unselectableStoryChoices.Add(pendingStoryChoice);
                        Record("decline-unselectable-story-choice", new { choice = pendingStoryChoice, mode = mode.ToString(), source });
                        pendingStoryChoice = null;
                    }
                    if (pendingMerchantOffer != null)
                    {
                        unselectableMerchantOffers.Add(pendingMerchantOffer);
                        Record("decline-unselectable-upgrade", new { offer = pendingMerchantOffer });
                        pendingMerchantOffer = null;
                    }
                }
                return;
            }
            if (screen is StoryEventScreen story)
            {
                if (Click(story.GetContinueButton(), "event-continue")) return;
                var available = Enumerable.Range(0, 8).Select(i => story.FindChoiceItemButton(i)).OfType<StoryChoiceItem>()
                    .Where(x => x.gameObject.activeInHierarchy).ToList();
                string storyKey = save.GetCurrentDistance() + ":" + string.Join(";", available.Select(x => x.choiceIndex + ":" + x.choiceText));
                if (auditedStoryOffers.Add(storyKey)) Record("event-offers", new {
                    choices = available.Select(x => new { index = x.choiceIndex, text = x.choiceText, allowed = x.conditionsPassed,
                        sacrificesChampion = SacrificesChampion(x, save),
                        rewards = x.GetRewardsInfo()?.Select(r => new { type = r.previewType.ToString(), r.dataKey, r.sourceString }).ToList() }).ToList() });
                bool safeAlternative = available.Any(x => x.conditionsPassed && !SacrificesChampion(x, save));
                bool lastChoice = Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_STORY_CHOICE_ORDER") == "Last";
                for (int choice = 0; choice < 8; choice++)
                {
                    int i = lastChoice ? 7 - choice : choice;
                    string choiceKey = save.GetCurrentDistance() + ":" + Field<StoryEventData>(story, "storyContent").GetID() + ":" + i;
                    if (story.FindChoiceItemButton(i) is StoryChoiceItem item && item.conditionsPassed &&
                        !unselectableStoryChoices.Contains(choiceKey) &&
                        (!safeAlternative || !SacrificesChampion(item, save)) &&
                        Click(item.button, "event-choice", new { index = i, text = item.choiceText,
                            order = Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_STORY_CHOICE_ORDER") ?? "First" }))
                    { pendingStoryChoice = choiceKey; return; }
                }
                story.TryToFastForward(); return;
            }
            if (save.GetGameSequence() == SaveData.GameSequence.GameOver)
            {
                if (!runWon.HasValue) ObserveRunOutcome(save, save.IsVictoriousAfterGameOver());
                return;
            }
            if (screen is GameScreen game && save.GetGameSequence() == SaveData.GameSequence.BattlePostCombatRewards)
            { Click(game.GetVictoryUI().GetCollectButton(), "collect-victory"); return; }
            if (screen is MapScreen map)
            {
                if (save.GetGameSequence() == SaveData.GameSequence.LeavingBattle)
                {
                    int distance = save.GetCurrentDistance();
                    var branches = new[] { MapScreen.BranchSelection.LeftBranch, MapScreen.BranchSelection.RightBranch };
                    var branch = branches.OrderByDescending(b => MapSection.GetMapNodeDataForBranch(distance, b, save)
                        .Sum(x => NodeValue(x.data))).First();
                    map.ChooseBranch(branch); Record("branch", new { distance, branch }); return;
                }
                if (save.GetGameSequence() != SaveData.GameSequence.DestinationReached) return;
                var nodes = map.GetComponentsInChildren<MapNodeUI>().Where(x => x.gameObject.activeInHierarchy &&
                    x.GetLocation()?.Distance == save.GetCurrentDistance() && x.GetData() != null &&
                    x.GetData().CanBeTriggered(x.GetLocation(), save) &&
                    !x.GetData().HasBeenVisited(x.GetLocation(), save) &&
                    (x.GetBranch() == MapScreen.BranchSelection.NoBranch || (int)x.GetBranch() == save.GetCurrentBranch()))
                    .OrderByDescending(x => NodeValue(x.GetData()));
                bool hasChampion = save.GetDeckState().Any(card => card.IsChampionCard());
                foreach (MapNodeUI node in nodes)
                {
                    // MapNodeUI.TryTriggerNode refuses this reward without a
                    // champion, leaving the node unvisited after its dialog.
                    // Keep following the map instead of reopening that dialog.
                    if (!hasChampion && node.GetData() is RewardNodeData rewardNode &&
                        rewardNode.GetRewards().Any(item => item is ChampionUpgradeRewardData)) continue;
                    if (Click(node.button, "map-node", new { node = node.GetMapNodeDataName() })) return;
                }
                if (map.TryGetBattleNodeUI(save.GetCurrentDistance() + 1, out MapBattleNodeUI battle))
                    Click(battle.button, "depart-battle");
                return;
            }
            if (screen is BattleIntroScreen intro)
            {
                int ring = save.GetCurrentDistance() + 1;
                if (savedRing != ring)
                {
                    if (!checkpointSaving)
                    { checkpointSaving = true; save.Save(); Record("checkpoint-save", new { ring }); return; }
                    SaveCheckpoint(ring); savedRing = ring; checkpointSaving = false;
                }
                if (ring >= stopRing) { Finish(true, "Native campaign checkpoint ready before ring " + ring); return; }
                if ((bool)AccessTools.Field(typeof(BattleIntroScreen), "isFightButtonAvailable").GetValue(intro))
                    Click(intro.GetFightButton(), "fight");
                return;
            }
            if (save.GetGameSequence() == SaveData.GameSequence.InBattle) PlayBattle(m!, save);
        }

        private void PlayBattle(AllGameManagers m, SaveManager save)
        {
            if (save.PreviewMode || PreviewRngIsolation.Active || !CardAnimationSettlement.Ready()) return;
            CombatManager combat = m.GetCombatManager(); CardManager cards = m.GetCardManager();
            if (!combat.ShouldShowEndTurnButton() || !combat.HaveCardsBeenDrawnForCurrentTurn() || combat.IsRunningTriggerQueue ||
                m.GetReplayManager().IsCardPlaying() || (m.GetHandUI()?.AreAnyCardsAnimating() ?? false) ||
                save.GetBattlePreviewEnabled() && (bool)AccessTools.Field(typeof(CombatManager), "combatStateChanged").GetValue(combat)) return;
            RoomManager rooms = m.GetRoomManager();
            if (selectingRoom.HasValue)
            {
                if (rooms.GetSelectedRoom() != selectingRoom.Value)
                {
                    // A tutorial/dialog can select another room after the
                    // original coroutine completed. Retry the same native UI
                    // request, with a bound rather than waiting indefinitely.
                    if (Time.realtimeSinceStartup - roomSelectionRequestedAt >= 2f)
                        SelectRoom(rooms, save, selectingRoom.Value, retry: true);
                    return;
                }
                selectingRoom = null;
            }
            if (battleDistance != save.GetCurrentDistance() || lastTurn != combat.GetTurnCount())
            {
                battleDistance = save.GetCurrentDistance(); lastTurn = combat.GetTurnCount();
                Record("turn", new { distance = battleDistance, turn = lastTurn, pyre = m.GetPlayerManager().GetTowerHP(),
                    hand = cards.GetHand().Select(CardDescription).ToList(),
                    units = m.GetMonsterManager().GetCharactersReadOnly().Select(UnitDescription).ToList(),
                    enemies = m.GetHeroManager().GetCharactersReadOnly().Select(UnitDescription).ToList() });
            }
            var choices = new List<(CardState card, int room, SpawnPoint? drop, double score)>();
            int preferredRoom = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_DEPLOYMENT_ROOM") ?? "2");
            foreach (CardState card in cards.GetHand())
            for (int r = 0; r < Math.Min(3, rooms.GetNumRooms()); r++)
            {
                RoomState room = rooms.GetRoom(r); if (!room.IsRoomEnabled()) continue;
                var units = new List<CharacterState>(); room.AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
                var drops = new List<SpawnPoint?>();
                TargetHighlightType highlight = card.GetTargetHighlightType();
                TargetValidator? validator = null;
                if (highlight == TargetHighlightType.MonsterSpawnArea)
                {
                    validator = new MonsterAreaTargetValidator(null!);
                    for (int p = 0; p <= room.GetNumLivingCharacters(Team.Type.Monsters); p++)
                    { SpawnPoint? point = room.GetMonsterPoint(p); if (point != null) drops.Add(point); }
                }
                else if (highlight == TargetHighlightType.Character)
                { validator = new CharacterTargetValidator(null!); drops.AddRange(units.Select(x => x.GetSpawnPoint())); }
                else if (highlight == TargetHighlightType.Room && TargetHelper.IsCardDropTargetRoom(card)) drops.Add(null);
                // Teleport has its own two-stage UI. Do not bypass that UI here.
                validator?.SetActiveCard(card);
                foreach (SpawnPoint? drop in drops)
                {
                    if (validator != null && !validator.IsValidTarget(combat, m.GetMonsterManager(), m.GetRelicManager(), room,
                        drop == null ? null : room.GetSpawnPointUIFromSpawnPoint(drop), true)) continue;
                    if (!cards.CanPlayHandCard(card, r, drop, null, null, out _)) continue;
                    if (!combat.TestEffects(card.GetEffectStates(), card.GetTargetless() || card.GetTargetsRoom() ? r : -1,
                        card, null, drop, null, null, out _)) continue;
                    double score = card.GetCardType() == CardType.Monster ?
                        1100 + CardValue(card) - Math.Abs(r - preferredRoom) * 50 : 100;
                    CharacterState? target = drop?.GetCharacterState();
                    if (card.GetCardType() == CardType.Monster)
                    {
                        // Put a healthier body in front of an existing fragile
                        // attacker, using only visible native card/unit stats.
                        bool tank = card.GetHealth() > units.Where(x => x.GetTeamType() == Team.Type.Monsters)
                            .Select(x => (float)x.GetHP()).DefaultIfEmpty(0).Max();
                        score += (tank ? -1 : 1) * (drop?.GetIndexInRoom() ?? 0);
                        if (tank && units.Any(x => x.GetSpawnerCard()?.IsChampionCard() == true))
                            score += 100 + card.GetHealth() * 2;
                    }
                    else if (target != null)
                    {
                        // Native targeting permits several damage and buff
                        // spells on either team. Legality alone is not utility.
                        bool harmful = card.GetEffectStates().Any(effect =>
                            effect.GetSourceCardEffectData().GetEffectStateName() is "CardEffectDamage" or "CardEffectKill" ||
                            effect.GetParamStatusEffectStackData().Any(status =>
                                m.GetStatusEffectManager().GetStatusEffectDataById(status.statusId)?.GetDisplayCategory() ==
                                    StatusEffectData.DisplayCategory.Negative));
                        if (harmful != (target.GetTeamType() == Team.Type.Heroes)) continue;
                        score += target.GetTeamType() == Team.Type.Heroes ? 400 - target.GetHP() :
                            150 + target.GetAttackDamage() + (target.GetSpawnerCard()?.IsChampionCard() == true ? 500 : 0);
                        if (!harmful && target.GetSpawnerCard()?.IsChampionCard() == true && card.IsConsumeRemainingEnergyCostType())
                            score += 800;
                    }
                    else score += units.Count(x => x.GetTeamType() == Team.Type.Heroes) * 25 + r;
                    if (card.GetCardType() == CardType.Spell && card.GetCostWithoutAnyModifications() == 0 &&
                        !card.IsConsumeRemainingEnergyCostType() && card.GetEffectStates().Any(effect =>
                            effect.GetSourceCardEffectData().GetEffectStateName() is "CardEffectDraw" or "CardEffectDrawType"))
                        score += 1600;
                    choices.Add((card, r, drop, score));
                }
            }
            var best = choices.OrderByDescending(x => x.score).FirstOrDefault();
            // Deployment precedes abilities; useful abilities precede ordinary
            // spells so that those spells cannot spend all of their ember first.
            if ((best.card == null || best.card.GetCardType() != CardType.Monster) && PlayAbility(m, save, combat, cards)) return;
            if (best.card != null)
            {
                if (rooms.GetSelectedRoom() != best.room)
                {
                    SelectRoom(rooms, save, best.room); return;
                }
                var error = default(CommonSelectionBehavior.SelectionError);
                int index = cards.GetHand().IndexOf(best.card);
                int? point = best.drop?.GetIndexInRoom();
                CharacterState? target = best.drop?.GetCharacterState();
                string? targetName = target?.GetSourceCharacterData().name, targetTeam = target?.GetTeamType().ToString();
                if (!cards.PlayCard(index, best.drop, ref error)) throw new InvalidOperationException("Native campaign card rejected: " + error);
                Record("play", new { card = best.card.GetAssetName(), room = best.room, point, target = targetName, targetTeam });
                return;
            }
            Command("Command_EndTurn").Invoke(null, null);
            Record("end-turn", new { distance = battleDistance, turn = lastTurn });
        }

        private bool PlayAbility(AllGameManagers m, SaveManager save, CombatManager combat, CardManager cards)
        {
            foreach (CharacterState actor in m.GetMonsterManager().GetCharactersReadOnly())
            {
                if (!actor.CanActivateUnitAbility()) continue;
                CardState ability = actor.GetUnitAbilityCardState();
                RoomState room = actor.GetCurrentRoom();
                if (ability == null || room == null || !room.IsRoomEnabled()) continue;
                var drops = new List<SpawnPoint?>();
                if (ability.GetTargetHighlightType() == TargetHighlightType.Room && TargetHelper.IsCardDropTargetRoom(ability)) drops.Add(null);
                else if (ability.GetTargetHighlightType() == TargetHighlightType.Character)
                {
                    var targets = new List<CharacterState>(); combat.GetTargetCharacters(targets, ability, room, actor);
                    drops.AddRange(targets.OrderByDescending(x => x.GetAttackDamage()).Select(x => x.GetSpawnPoint()));
                }
                foreach (SpawnPoint? drop in drops)
                {
                    if (!combat.TestEffects(ability.GetEffectStates(), room.GetRoomIndex(), ability, actor, drop, actor, null, out _)) continue;
                    RoomManager rooms = m.GetRoomManager();
                    if (rooms.GetSelectedRoom() != room.GetRoomIndex())
                    { SelectRoom(rooms, save, room.GetRoomIndex()); return true; }
                    if (!cards.PlayAnyCard(ability, out _, actor, drop, ignoreCost: false, discardCard: false,
                        fromDirectPlay: true, characterThatActivatedAbility: actor)) throw new InvalidOperationException("Native ability play rejected.");
                    Record("ability", new { unit = actor.GetSourceCharacterData().name, ability = ability.GetAssetName(), room = room.GetRoomIndex() });
                    return true;
                }
            }
            return false;
        }

        private void SelectRoom(RoomManager rooms, SaveManager save, int room, bool retry = false)
        {
            roomSelectionRetries = retry ? roomSelectionRetries + 1 : 0;
            if (roomSelectionRetries > 3)
                throw new InvalidOperationException("Native room selection did not settle: expected=" + room + " actual=" + rooms.GetSelectedRoom());
            if (retry) Record("room-selection-retry", new { from = rooms.GetSelectedRoom(), to = room, attempt = roomSelectionRetries });
            selectingRoom = room; roomSelectionRequestedAt = Time.realtimeSinceStartup;
            save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(room));
        }

        private bool Click(GameUISelectableButton? button, string kind, object? data = null)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.CanBeActivated() || !ReplayManager.TryClickButton(button)) return false;
            Record(kind, data ?? new { }); return true;
        }
        private bool AcceptUpgrade(MerchantGoodUIBase good)
        {
            if (good.GoodState.RewardData is not EnhancerRewardData reward) return true;
            string[] excluded = (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_EXCLUDED_UPGRADE_TRAITS") ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var rejected = new List<string>();
            foreach (RelicEffectData effect in reward.GetRelicData().GetEffects())
            {
                CardUpgradeData? upgrade = effect.GetParamCardUpgradeData();
                if (upgrade is null) continue;
                foreach (CardTraitData trait in upgrade.GetTraitDataUpgrades())
                    if (Array.IndexOf(excluded, trait.GetTraitStateName()) >= 0) rejected.Add(trait.GetTraitStateName());
            }
            string[] traits = Enumerable.ToArray(Enumerable.Distinct(rejected));
            if (traits.Length == 0) return true;
            if (declinedUpgrades.Add(reward.GetReplayDataID()))
                Record("decline-upgrade", new { reward = reward.GetReplayDataID(), traits });
            return false;
        }
        private void SaveCheckpoint(int ring)
        {
            string root = Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!;
            string destination = root + "-before-ring" + ring;
            if (Directory.Exists(destination)) throw new IOException("Checkpoint already exists: " + destination);
            Directory.CreateDirectory(destination);
            Directory.CreateDirectory(Path.Combine(destination, "saves"));
            foreach (string file in Directory.GetFiles(Path.Combine(root, "saves")))
                File.Copy(file, Path.Combine(destination, "saves", Path.GetFileName(file)));
            foreach (string name in new[] { "metagameSave.json", "preferences.json", "preferences-local.json", "campaign-input.json", "campaign-parent-audit.jsonl" })
                if (File.Exists(Path.Combine(root, name))) File.Copy(Path.Combine(root, name), Path.Combine(destination, name));
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_AFTER_REPLAY") == "1")
                File.Copy(Path.Combine(root, "full-battle.mt2f"), Path.Combine(destination, "campaign-battle-replay.mt2f"));
            byte[] bytes = File.ReadAllBytes(Path.Combine(destination, "saves", "save-singlePlayer.json"));
            string hash;
            using (SHA256 sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
            // Read the native pool as well as its stored index: different run
            // definitions need not use the same index for the same final boss.
            SaveManager save = AllGameManagers.Instance.GetSaveManager();
            var finals = new List<ScenarioData>();
            save.GetRunData().GetDistanceData(7).GetAllScenarioDatas(finals);
            int selected = save.GetRunState().GetNodeStateAtDistance(7).scenarioIndex;
            var pool = new List<object>();
            for (int index = 0; index < finals.Count; index++) pool.Add(new { index, scenario = finals[index].name });
            Record("final-boss-route", new { selectedIndex = selected,
                selectedScenario = selected >= 0 && selected < finals.Count ? finals[selected].name : null,
                pool });
            Record("checkpoint", new { ring, profile = destination, sha256 = hash });
            File.Copy(auditPath, Path.Combine(destination, "campaign-audit.jsonl"));
        }
        private static T Field<T>(object instance, string name) => (T)AccessTools.Field(instance.GetType(), name).GetValue(instance);
        private static MethodInfo Command(string name, params Type[] args) => AccessTools.Method(typeof(CheatManager), name, args);
        private static bool SacrificesChampion(StoryChoiceItem item, SaveManager save) =>
            item.GetRewardsInfo()?.Any(info => info.dataKey == "PurgeChampionPurgeReward" &&
                save.GetAllGameData().FindRewardDataByName(info.dataKey) is PurgeRewardData purge &&
                Field<bool>(purge, "allowPurgeChampion")) == true;

        private static double CardValue(CardState card) => card == null ? -10000 :
            card.GetCardType() == CardType.Monster ? (card.IsChampionCard() ? 500 : 0) + card.GetTotalAttackDamage() * 2 + card.GetHealth() +
                (card.GetAssetName().Contains("Steward") ? -100 : 0) : 10 - card.GetCostWithoutAnyModifications();
        private static object CardDescription(CardState card) => new { asset = card.GetAssetName(), id = card.GetID(),
            attack = card.GetTotalAttackDamage(), health = card.GetHealth(), size = card.GetSize(),
            upgrades = card.GetCardStateModifiers().GetCardUpgrades().Select(x => x.GetAssetName()).ToList() };
        private static object UnitDescription(CharacterState unit)
        {
            var statuses = new List<CharacterState.StatusEffectStack>(); unit.GetStatusEffects(ref statuses);
            return new { unit = unit.GetSourceCharacterData().name, room = unit.GetCurrentRoomIndex(),
                point = unit.GetSpawnPoint()?.GetIndexInRoom(), hp = unit.GetHP(), attack = unit.GetAttackDamage(),
                statuses = statuses.Select(x => new { id = x.State.GetStatusId(), count = x.Count }).ToList(),
                ability = unit.GetUnitAbilityCardState()?.GetAssetName(), available = unit.CanActivateUnitAbility() };
        }
        private static object DescribeDraft(IDraftableUI item) => item is CardUI card ? CardDescription(card.GetCardState()) :
            item is RelicInfoUI relic ? (object)new { relic = relic.relicData.name, id = relic.relicData.GetID() } :
            new { ui = item.GetType().Name, name = item.rootTransform.name };
        private static string RewardAsset(GrantableRewardData reward) => reward is RelicRewardData relic ? relic.GetRelicData().name :
            reward is CardRewardData card ? card.GetCardData().name : reward.name;
        private static int NodeValue(MapNodeData node) => node is MerchantData ? 60 :
            node is RewardNodeData reward ? 100 + reward.GetRewards().Sum(x => x is ChampionUpgradeRewardData ? 100 : 0) : 10;
        private void Finish(bool success, string message)
        {
            Record(success ? "complete" : "failure", new { message });
            log.LogInfo((success ? "CAMPAIGN-PASS " : "CAMPAIGN-FAIL ") + message);
            quitCode = success ? 0 : 1;
            quitAt = Time.realtimeSinceStartup + 5f; Application.Quit(quitCode);
        }
        private static void ObserveStoryProgression(SaveManager save, bool afterEndGame)
        {
            var stories = AllGameManagers.Instance.GetAllGameData().GetDialogueCollectionData().GetStoryProgressionDatas(DLC.None);
            var rows = new List<object>();
            for (int index = 0; index < stories.Count; index++)
            {
                StoryProgressionData story = stories[index];
                UnlockCriteria condition = story.GetProgressionCondition();
                bool hasProgress = save.TryGetUnlockCriteriaProgress(condition, out int current, out int required);
                rows.Add(new { index, asset = story.name, reward = story.GetReward().GetRewardType().ToString(),
                    condition = condition.GetUnlockCondition().ToString(), hasProgress, current, required,
                    met = save.IsUnlockCriteriaMet(condition) });
            }
            var hearts = new List<object>();
            foreach (CharacterData character in save.GetBalanceData().GetPyreHeartCharacterDatas())
            {
                UnlockCriteria condition = character.GetPyreHeartData().GetUnlockData();
                bool hasProgress = save.TryGetUnlockCriteriaProgress(condition, out int current, out int required);
                hearts.Add(new { asset = character.name, condition = condition.GetUnlockCondition().ToString(),
                    hasProgress, current, required, met = save.IsUnlockCriteriaMet(condition, noneValue: false),
                    unlocked = save.GetMetagameSave().HasUnlockedPyreHeart(character.GetID()) });
            }
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "native-story-progression.json");
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
                writer.Write(JsonConvert.SerializeObject(new { Schema = 1, EvidenceKind = "NativeMetagameObservation",
                    AfterEndGame = afterEndGame, StoryProgression = save.GetStoryProgression(DLC.None),
                    TrueFinalBossFeatureUnlocked = save.IsTrueFinalBossFeatureUnlocked(), Stories = rows,
                    PyreHearts = hearts }, Formatting.Indented));
        }

        private void ObserveRunOutcome(SaveManager save, bool nativeVictory)
        {
            if (runWon.HasValue) return;
            SaveManager.VictoryType victory = save.GetVictoryTypeAfterGameOver();
            runWon = nativeVictory && save.IsVictoriousAfterGameOver() && !SaveManager.WasDevCheatUsed;
            // FinishRun may rotate both the active slot and its backup. Retain
            // the game's own serialized data while all completed records exist.
            var data = (SaveData)AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            string snapshot = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "completed-save-singlePlayer.json");
            if (File.Exists(snapshot)) throw new IOException("Completed native snapshot already exists.");
            File.WriteAllText(snapshot, JsonUtility.ToJson(data));
            Record("run-outcome", new { won = runWon.Value, nativeVictory, victoryType = victory.ToString(), distance = save.GetCurrentDistance(),
                cheat = SaveManager.WasDevCheatUsed, battlesWon = save.GetNumBattlesWon(),
                battles = save.GetBattleRecords().Select(battle => new { battle.battleIndex, battle.loopIndex,
                    won = battle.WonBattle(), battle.bossHPRemaining, battle.turnsLasted }).ToList() });
        }
        [HarmonyPatch(typeof(GameStateManager), nameof(GameStateManager.EndGame))]
        private static class RunOutcomePatch
        {
            private static void Prefix(GameStateManager.EndGameReason endGameReason)
            {
                // A defeat is added to completedBattles inside EndGame. Retain
                // that record after the native method, before the later
                // GameOver screen's FinishRun resets the active save.
                if (active != null && endGameReason == GameStateManager.EndGameReason.Victory)
                    active.ObserveRunOutcome(AllGameManagers.Instance.GetSaveManager(), true);
            }
            private static void Postfix(GameStateManager.EndGameReason endGameReason)
            {
                if (active == null) return;
                if (endGameReason == GameStateManager.EndGameReason.Defeat)
                    active.ObserveRunOutcome(AllGameManagers.Instance.GetSaveManager(), false);
                active.runReturned = true;
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.StopCombat))]
        private static class BattleOutcomePatch
        {
            private static void Prefix(bool combatWon)
            {
                if (active == null || AllGameManagers.Instance.GetSaveManager().PreviewMode) return;
                if (!combatWon) active.nativeLost = true;
                active.Record("battle-outcome", new { won = combatWon, distance = AllGameManagers.Instance.GetSaveManager().GetCurrentDistance(),
                    pyre = AllGameManagers.Instance.GetPlayerManager().GetTowerHP() });
            }
        }
    }
}
