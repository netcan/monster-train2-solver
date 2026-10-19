using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOfferingTokenScenario
    {
        internal const string Name = "original-offering-token";
        internal const string CardId = "e2d5dbd1-3930-4a41-9631-bef04cbc3fa4";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static int selectedId;
        private static bool chosenOnce;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData offering = all.FindCardData(CardId);
            if (offering == null) throw new InvalidOperationException("Original Offering Token CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = offering.GetEffects();
            if (offering.name != "OfferingToken" || offering.GetCost() != 0 || effects == null || effects.Count != 2 ||
                effects[0].GetEffectStateName() != "CardEffectDraw" || effects[0].GetParamInt() != 1 ||
                effects[1].GetEffectStateName() != "CardEffectChooseDiscard" || effects[1].GetTargetMode().ToString() != "Hand" ||
                effects[1].GetParamInt() != 0 || effects[1].GetAdditionalParamInt() != 0 ||
                !effects[1].GetParamBool() || effects[1].GetParamBool2() || effects[1].GetParamBool3() ||
                offering.GetTraits().Count != 0 || offering.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Offering Token definition changed.");

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId);
            if (steward == null) throw new InvalidOperationException("Train Steward CardData lookup failed: " + MagneticRelayScenario.StewardId);
            CharacterData unit = steward.GetSpawnCharacterData();
            if (unit == null) throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState Add(string id, CardPile pile = CardPile.HandPile) => cards.AddNewCard(all.FindCardData(id)!, pile, false, false, animate: false);
            CardState anchor = Add(steward.GetID());
            CardState spell = Add(CardId);
            CardState discard = Add(steward.GetID());
            CardState drawn = Add(steward.GetID(), CardPile.DeckPile);
            CardIds = new[] { anchor, spell, discard, drawn }.Select(FullBattleTrace.Active!.CardId).ToArray();
            selectedId = CardIds[2];
            chosenOnce = false;

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-OFFERING-TOKEN-PREPARED unchanged zero-cost card, one controlled draw, one selected hand discard and a Train Steward anchor.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
                context.Cards.Hand.Any(card => card.InstanceId == ids[2]) &&
                context.Cards.Draw.Any(card => card.InstanceId == ids[3]))
            {
                var cast = new PlayCardAction(ids[1], 0,
                    recursionSelections: [new CardRecursionSelection(1, [ids[2]])]);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return null;
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.SelectCardFromHand))]
        private static class AutoSelectPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(CardManager __instance, HandSelectionUI.Params parameters, ref IEnumerator __result)
            {
                if (!Enabled || !Prepared || chosenOnce || selectedId <= 0) return true;
                __result = Select(__instance, parameters);
                return false;
            }
        }

        private static IEnumerator Select(CardManager cards, HandSelectionUI.Params parameters)
        {
            CardState selected = cards.GetHand().Single(card => FullBattleTrace.Active!.CardId(card) == selectedId);
            if (parameters.filterCallback(selected))
                throw new InvalidOperationException("Original Offering Token selected card failed the native hand filter.");
            chosenOnce = true;
            yield return parameters.cardChosenCallback(selected, cards);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
