using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using ShinyShoe.Loading;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardEffectRecursionScenario
    {
        private const string RecursionCardId = "0e50f7bc-c7c4-46b5-98a4-33d32194d7b8";
        private const string ExhaustedSpellId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
        private static ManualLogSource? logger;
        private static bool candidateLogged;
        private static int? recursionCardInstanceId;
        private static int selectedCardInstanceId;
        private static bool chooseMode;
        private static bool choiceSubmitted;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            recursionCardInstanceId = null;
            selectedCardInstanceId = 0;
            string modifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? string.Empty;
            chooseMode = modifierScenario is "card-effect-recursion-choice" or "card-effect-recursion-choose-to-hand";
            choiceSubmitted = false;
            CardManager cards = managers.GetCardManager()!;
            var gameData = managers.GetSaveManager().GetAllGameData();
            CardData recursion = gameData.FindCardData(RecursionCardId) ??
                throw new InvalidOperationException("Recursion fixture requires native WurmkinEtchings.");
            if (chooseMode)
            {
                CardEffectData recursionEffect = recursion.GetEffects().Single(effect =>
                    effect.GetEffectStateName() == typeof(CardEffectRecursion).Name);
                FieldInfo selectionMode = typeof(CardEffectData).GetField("targetCardSelectionMode",
                    BindingFlags.Instance | BindingFlags.NonPublic) ??
                    throw new MissingFieldException(typeof(CardEffectData).FullName, "targetCardSelectionMode");
                CardEffectData.CardSelectionMode mode = modifierScenario == "card-effect-recursion-choose-to-hand"
                    ? CardEffectData.CardSelectionMode.ChooseToHand : CardEffectData.CardSelectionMode.ChooseToDeck;
                selectionMode.SetValue(recursionEffect, mode);
            }
            CardData exhaustedSpell = gameData.FindCardData(ExhaustedSpellId) ??
                throw new InvalidOperationException("Recursion fixture requires native StarterFireStarter.");
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Recursion fixture requires one free hand slot.");
            for (int index = 0; index < 6; index++)
            {
                CardState added = cards.AddNewCard(exhaustedSpell, CardPile.HandPile, false, false, animate: false);
                if (added == null || !cards.GetHand().Contains(added))
                    throw new InvalidOperationException("Recursion fixture could not create StarterFireStarter in hand.");
                var condition = new RemoveFromStandByCondition(() => CardPile.KeepInStandBy);
                cards.MoveToStandByPile(added, wasPlayed: false, wasExhausted: true, condition: condition);
                if (added == null || !cards.IsCardInExhaustPile(added))
                    throw new InvalidOperationException("Recursion fixture could not place StarterFireStarter in Exhaust.");
                if (index == 2) selectedCardInstanceId = FullBattleTrace.Active!.CardId(added);
            }
            CardState played = cards.AddNewCard(recursion, CardPile.HandPile, false, false, animate: false);
            if (played == null || !cards.GetHand().Contains(played))
                throw new InvalidOperationException("Recursion fixture could not place WurmkinEtchings in hand.");
            recursionCardInstanceId = FullBattleTrace.Active!.CardId(played);
            log.LogInfo("CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=6 selection=" +
                (modifierScenario == "card-effect-recursion-choose-to-hand" ? "ChooseToHand" :
                    chooseMode ? "ChooseToDeck" : "RandomToDeck") + " selectedCard=" + selectedCardInstanceId);
        }

        internal static bool IsScenarioPrelude(PlayCardAction action) => recursionCardInstanceId == action.CardInstanceId;

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CardToken? card = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(item => item.DataId == RecursionCardId);
            if (card != null)
            {
                CardRecursionSelection[] selections = Array.Empty<CardRecursionSelection>();
                if (chooseMode)
                {
                    CardPlayRule? rule = source.PlayRules?.Cards.FirstOrDefault(item => item.DataId == RecursionCardId);
                    (CardActionEffect effect, int index)? recursion = rule?.Effects.Select((effect, index) => (effect, index))
                        .FirstOrDefault(item => item.effect.Type == "Recursion");
                    if (!recursion.HasValue || selectedCardInstanceId <= 0)
                        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
                    IReadOnlyList<int> eligible = CardRecursionModel.EligibleCards(source.Spawn.Train.Context.Cards,
                        source.Spawn.Train.Context.OtherPiles, recursion.Value.effect, source.PlayRules,
                        card.InstanceId, out string? error);
                    if (error != null || !eligible.Contains(selectedCardInstanceId))
                        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
                    selections = [new CardRecursionSelection(recursion.Value.index, [selectedCardInstanceId])];
                }
                var action = new PlayCardAction(card.InstanceId, 0, recursionSelections: selections);
                BattleActionResult result = BattleActionModel.PlayCard(source, action);
                if (!candidateLogged)
                {
                    logger!.LogInfo("CARD-RECURSION-CANDIDATE supported=" + result.Supported + " rejection=" + result.Rejection +
                        " reason=" + result.Reason + " hand-card=" + card.InstanceId);
                    candidateLogged = true;
                }
                if (result.Supported) return action;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }

        private static void AutoSelect(DeckScreen screen)
        {
            if (!chooseMode || choiceSubmitted || selectedCardInstanceId <= 0) return;
            IList? cardInfos = typeof(DeckScreen).GetField("cardInfos", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(screen) as IList;
            if (cardInfos == null) return;
            for (int index = 0; index < cardInfos.Count; index++)
            {
                object? info = cardInfos[index];
                if (info == null) continue;
                CardState? card = info.GetType().GetField("cardState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(info) as CardState;
                if (card == null || FullBattleTrace.Active!.CardId(card) != selectedCardInstanceId) continue;
                MethodInfo addSelected = typeof(DeckScreen).GetMethod("AddSelectedIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;
                MethodInfo confirm = typeof(DeckScreen).GetMethod("ConfirmSelectedCard", BindingFlags.Instance | BindingFlags.NonPublic)!;
                choiceSubmitted = true;
                addSelected.Invoke(screen, new object[] { index });
                confirm.Invoke(screen, null);
                logger!.LogInfo("CARD-RECURSION-CHOOSE-AUTOSELECT card=" + selectedCardInstanceId + " index=" + index);
                return;
            }
        }

        [HarmonyPatch(typeof(DeckScreen), nameof(DeckScreen.AddDeckScreenCardStateChosenDelegate))]
        private static class AutoSelectChosenCardPatch
        {
            [HarmonyPostfix]
            private static void Postfix(DeckScreen __instance) => AutoSelect(__instance);
        }
    }
}
