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
        internal const string OthersName = "card-effect-choose-discard-others";
        internal const string OthersPolicyName = "card-effect-choose-discard-others-policy";
        internal const string RefineName = "card-effect-choose-discard-refine";
        private const string ScrapId = "bc4cd099-93df-4771-ae72-4984fc50ab9f";
        private const string FoundryLineId = "26651f53-95d0-4802-b710-51d23bf89bbd";
        private const string ScrapMetalId = "d4f0ff05-1fe1-483c-adda-928a8b05808c";
        private const string OneCostId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
        private const string TwoCostId = "25f84315-da10-4915-aa5d-e3f1a38124b4";
        private static int sourceId;
        private static int selectedId;
        private static bool chosenOnce;
        private static ManualLogSource? logger;
        internal static bool OthersPolicyMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == OthersPolicyName;
        internal static bool RefineMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == RefineName;
        internal static bool OthersMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is OthersName or OthersPolicyName or RefineName;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is Name or OthersName or OthersPolicyName or RefineName;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            string sourceDataId = OthersMode ? FoundryLineId : ScrapId;
            string sourceName = OthersMode ? "FoundryLine" : "StarterScrap";
            CardData sourceData = save.GetAllGameData().FindCardData(sourceDataId) ??
                throw new InvalidOperationException("Original " + sourceName + " is missing.");
            CardEffectData effect = sourceData.GetEffects().Single();
            if (sourceData.name != sourceName || sourceData.GetCost() != 1 ||
                effect.GetEffectStateName() != "CardEffectChooseDiscard" || effect.GetParamInt() != (OthersMode ? 2 : 3) ||
                effect.GetAdditionalParamInt() != 5 || effect.GetParamBool() || !effect.GetParamBool2() ||
                effect.GetParamCardPool() != null)
                throw new InvalidOperationException("Original " + sourceName + " smelt definition changed.");
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
            if (RefineMode)
            {
                CardData scrapMetal = save.GetAllGameData().FindCardData(ScrapMetalId) ??
                    throw new InvalidOperationException("Original ScrapMetal is missing.");
                if (scrapMetal.name != "ScrapMetal" || scrapMetal.GetCost() != 0 ||
                    scrapMetal.GetTraits().Single(trait => trait.GetTraitStateName() == "CardTraitRefine").GetParamInt() != 3)
                    throw new InvalidOperationException("Original ScrapMetal Refine definition changed.");
                CardState refined = Add(scrapMetal);
                log.LogInfo("CHOOSE-DISCARD-REFINE card=" + FullBattleTrace.Active!.CardId(refined) + " bonus=3");
            }
            CardState source = Add(sourceData);
            selectedId = FullBattleTrace.Active!.CardId(selected);
            sourceId = FullBattleTrace.Active.CardId(source);
            chosenOnce = false;
            log.LogInfo("CHOOSE-DISCARD-PREPARED " + sourceName + "=" + sourceId + " selected=" + selectedId +
                " selectedCost=2 otherCost=1");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (chosenOnce)
                return OthersMode && !OthersPolicyMode ? null : BattleActionModel.EnumerateSupportedPlays(state)
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
