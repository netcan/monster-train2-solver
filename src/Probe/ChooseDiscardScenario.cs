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
        internal const string PersistentName = "card-effect-choose-discard-persistent";
        internal const string OriginalSmeltRelicName = "original-smelt-ember-relic";
        internal const string OriginalSmeltRelicZeroName = "original-smelt-ember-relic-zero";
        internal const string OriginalSmeltRelicBulkName = "original-smelt-ember-relic-bulk";
        private const string SmeltRelicId = "a034e00f-3a12-4c19-b626-4b8fd17c412a";
        private const string StingId = "356a00a4-010b-462b-b683-2ef296d076a8";
        private const string ScrapId = "bc4cd099-93df-4771-ae72-4984fc50ab9f";
        private const string FoundryLineId = "26651f53-95d0-4802-b710-51d23bf89bbd";
        private const string ScrapMetalId = "d4f0ff05-1fe1-483c-adda-928a8b05808c";
        private const string OneCostId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
        private const string TwoCostId = "25f84315-da10-4915-aa5d-e3f1a38124b4";
        private static int sourceId;
        private static int selectedId;
        private static bool chosenOnce;
        private static ManualLogSource? logger;
        internal static bool RelicAcquired { get; private set; }
        internal static bool OriginalSmeltRelicMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is
            OriginalSmeltRelicName or OriginalSmeltRelicZeroName or OriginalSmeltRelicBulkName;
        internal static bool OriginalSmeltRelicZeroMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == OriginalSmeltRelicZeroName;
        internal static bool OthersPolicyMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == OthersPolicyName;
        internal static bool RefineMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == RefineName;
        internal static bool PersistentMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == PersistentName;
        internal static bool OthersMode => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is OthersName or OthersPolicyName or RefineName or PersistentName or OriginalSmeltRelicBulkName;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is Name or OthersName or OthersPolicyName or RefineName or PersistentName or OriginalSmeltRelicName or OriginalSmeltRelicZeroName or OriginalSmeltRelicBulkName;

        internal static void AcquireRelicBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (RelicAcquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Original smelt relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == SmeltRelicId);
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != "EmberFoundryCrucible" || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAdditionalSmeltEmber" || effect.GetParamInt() != 1 ||
                effect.GetEffectConditions().Count != 0 || save.GetCollectedRelics().Any(item => item.GetRelicDataID() == SmeltRelicId))
                throw new InvalidOperationException("Original EmberFoundryCrucible definition or acquisition changed.");
            save.AddRelic(relic);
            RelicAcquired = true;
            log.LogInfo("ORIGINAL-SMELT-RELIC-ACQUIRED unchanged EmberFoundryCrucible additionalEmber=1");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (OriginalSmeltRelicMode && (!RelicAcquired ||
                managers.GetSaveManager().GetCollectedRelics().Count(item => item.GetRelicDataID() == SmeltRelicId) != 1))
                throw new InvalidOperationException("Original smelt relic is missing or duplicated at battle setup.");
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
            if (OriginalSmeltRelicZeroMode)
            {
                CardData sting = save.GetAllGameData().FindCardData(StingId) ??
                    throw new InvalidOperationException("Original zero-cost Sting is missing.");
                if (sting.name != "Sting" || sting.GetCost() != 0)
                    throw new InvalidOperationException("Original zero-cost Sting definition changed.");
                selected = Add(sting);
            }
            CardState? persistent = null;
            if (RefineMode || PersistentMode)
            {
                CardData scrapMetal = save.GetAllGameData().FindCardData(ScrapMetalId) ??
                    throw new InvalidOperationException("Original ScrapMetal is missing.");
                if (scrapMetal.name != "ScrapMetal" || scrapMetal.GetCost() != 0 ||
                    scrapMetal.GetTraits().Single(trait => trait.GetTraitStateName() == "CardTraitRefine").GetParamInt() != 3)
                    throw new InvalidOperationException("Original ScrapMetal Refine definition changed.");
                persistent = Add(scrapMetal);
                log.LogInfo("CHOOSE-DISCARD-REFINE card=" + FullBattleTrace.Active!.CardId(persistent) + " bonus=3");
            }
            CardState source = Add(sourceData);
            selectedId = FullBattleTrace.Active!.CardId(PersistentMode ? persistent! : selected);
            sourceId = FullBattleTrace.Active.CardId(source);
            chosenOnce = false;
            log.LogInfo("CHOOSE-DISCARD-PREPARED " + sourceName + "=" + sourceId + " selected=" + selectedId +
                " selectedCost=" + (PersistentMode || OriginalSmeltRelicZeroMode ? 0 : 2) + " otherCost=1");
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
