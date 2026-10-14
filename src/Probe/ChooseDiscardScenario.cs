using System;
using System.Collections;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class ChooseDiscardScenario
    {
        internal const string Name = "card-effect-choose-discard";
        private const string ScrapId = "bc4cd099-93df-4771-ae72-4984fc50ab9f";
        private const string OneCostId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
        private const string TwoCostId = "25f84315-da10-4915-aa5d-e3f1a38124b4";
        private static int sourceId;
        private static int selectedId;
        private static bool chosenOnce;
        private static ManualLogSource? logger;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CardData scrap = save.GetAllGameData().FindCardData(ScrapId) ??
                throw new InvalidOperationException("Original StarterScrap is missing.");
            CardEffectData effect = scrap.GetEffects().Single();
            if (scrap.name != "StarterScrap" || scrap.GetCost() != 1 ||
                effect.GetEffectStateName() != "CardEffectChooseDiscard" || effect.GetParamInt() != 3 ||
                effect.GetAdditionalParamInt() != 5 || effect.GetParamBool() || !effect.GetParamBool2() ||
                effect.GetParamCardPool() != null)
                throw new InvalidOperationException("Original StarterScrap smelt definition changed.");
            CardData one = save.GetAllGameData().FindCardData(OneCostId) ??
                throw new InvalidOperationException("Original StarterFireStarter is missing.");
            CardData two = save.GetAllGameData().FindCardData(TwoCostId) ??
                throw new InvalidOperationException("Original EngulfedInSmoke is missing.");
            if (one.GetCost() != 1 || two.GetCost() != 2)
                throw new InvalidOperationException("Smelt candidate costs changed.");
            CardState Add(CardData data)
            {
                if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Choose-discard scene needs hand space.");
                CardState added = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
                if (added == null || !cards.GetHand().Contains(added))
                    throw new InvalidOperationException("Choose-discard card did not enter the hand.");
                return added;
            }
            Add(one);
            CardState selected = Add(two);
            CardState source = Add(scrap);
            selectedId = FullBattleTrace.Active!.CardId(selected);
            sourceId = FullBattleTrace.Active.CardId(source);
            chosenOnce = false;
            log.LogInfo("CHOOSE-DISCARD-PREPARED StarterScrap=" + sourceId + " selected=" + selectedId +
                " selectedCost=2 otherCost=1");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (chosenOnce)
                return BattleActionModel.EnumerateSupportedPlays(state)
                    .FirstOrDefault(action => action.CardInstanceId != sourceId);
            CardToken? source = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.InstanceId == sourceId);
            if (source != null)
            {
                var action = new PlayCardAction(sourceId, 0,
                    recursionSelections: [new CardRecursionSelection(0, [selectedId])]);
                BattleActionResult result = BattleActionModel.PlayCard(state, action);
                logger!.LogInfo("CHOOSE-DISCARD-CANDIDATE supported=" + result.Supported + " reason=" + result.Reason);
                if (result.Supported) return action;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
        }

        internal static bool IsScenarioPrelude(PlayCardAction action) => !chosenOnce && action.CardInstanceId == sourceId;

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.SelectCardFromHand))]
        private static class AutoSelectPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(CardManager __instance, HandSelectionUI.Params parameters, ref IEnumerator __result)
            {
                if (!Enabled || chosenOnce || sourceId <= 0 || selectedId <= 0) return true;
                __result = Select(__instance, parameters);
                return false;
            }
        }

        private static IEnumerator Select(CardManager cards, HandSelectionUI.Params parameters)
        {
            CardState selected = cards.GetHand().Single(card => FullBattleTrace.Active!.CardId(card) == selectedId);
            if (parameters.filterCallback(selected))
                throw new InvalidOperationException("Chosen smelt card was rejected by the original native filter.");
            logger!.LogInfo("CHOOSE-DISCARD-AUTOSELECT card=" + selectedId + " cost=" +
                selected.GetCostWithoutAnyModifications());
            chosenOnce = true;
            yield return parameters.cardChosenCallback(selected, cards);
        }
    }
}
