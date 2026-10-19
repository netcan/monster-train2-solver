using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMetallurgyScenario
    {
        internal const string Name = "original-metallurgy";
        internal const string CardId = "bdfec381-a336-4b7d-b08a-c10c7ce231c0";
        private const string OneCostId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
        private const string TwoCostId = "25f84315-da10-4915-aa5d-e3f1a38124b4";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static int sourceId;
        private static int selectedId;
        private static bool chosenOnce;
        private static ManualLogSource? logger;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData metallurgy = all.FindCardData(CardId) ?? throw new InvalidOperationException("Original Metallurgy CardData lookup failed.");
            CardEffectData[] effects = metallurgy.GetEffects().ToArray();
            if (effects.Length != 2) throw new InvalidOperationException("Original Metallurgy effect count changed.");
            StatusEffectStackData[] armor = effects[0].GetParamStatusEffects();
            if (metallurgy.name != "Metallurgy" || metallurgy.GetCardType() != CardType.Spell || metallurgy.GetCost() != 1 ||
                metallurgy.GetTraits().Count != 0 || metallurgy.GetCardTriggers().Count != 0 ||
                effects[0].GetEffectStateName() != "CardEffectAddStatusEffect" || effects[0].GetTargetMode() != TargetMode.Room ||
                effects[0].GetTargetTeamType() != Team.Type.Monsters || armor.Length != 1 || armor[0].statusId != "armor" || armor[0].count != 4 ||
                effects[1].GetEffectStateName() != "CardEffectChooseDiscard" || effects[1].GetTargetMode() != TargetMode.Hand ||
                effects[1].GetParamInt() != 3 || effects[1].GetAdditionalParamInt() != 5 || effects[1].GetParamBool() ||
                !effects[1].GetParamBool2() || effects[1].GetParamCardPool() != null)
                throw new InvalidOperationException("Original Metallurgy definition changed.");

            CardData oneCost = all.FindCardData(OneCostId) ?? throw new InvalidOperationException("Original one-cost choice card lookup failed.");
            CardData twoCost = all.FindCardData(TwoCostId) ?? throw new InvalidOperationException("Original two-cost choice card lookup failed.");
            if (oneCost.GetCost() != 1 || twoCost.GetCost() != 2 || oneCost.GetCardType() != CardType.Spell || twoCost.GetCardType() != CardType.Spell)
                throw new InvalidOperationException("Metallurgy choice control card costs changed.");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 0);
            Set(unit, "health", 500);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState host = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(metallurgy, CardPile.HandPile, false, false, animate: false);
            CardState retained = cards.AddNewCard(oneCost, CardPile.HandPile, false, false, animate: false);
            CardState consumed = cards.AddNewCard(twoCost, CardPile.HandPile, false, false, animate: false);
            if (host == null || spell == null || retained == null || consumed == null ||
                new[] { host, spell, retained, consumed }.Any(card => !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original Metallurgy scenario could not add its controlled cards.");

            CardIds = new[] { host, spell, retained, consumed }.Select(FullBattleTrace.Active!.CardId).ToArray();
            sourceId = CardIds[1];
            selectedId = CardIds[3];
            chosenOnce = false;
            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-METALLURGY-PREPARED unchanged armor-4 spell; native ConsumeAndForge choice consumes cost 2 and retains cost 1.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[1]))
            {
                var cast = new PlayCardAction(ids[1], 0,
                    recursionSelections: [new CardRecursionSelection(1, [ids[3]])]);
                BattleActionResult result = BattleActionModel.PlayCard(source, cast);
                logger?.LogInfo("ORIGINAL-METALLURGY-CANDIDATE supported=" + result.Supported + " reason=" + result.Reason);
                if (result.Supported) return cast;
            }
            return null;
        }

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
                throw new InvalidOperationException("The original Metallurgy selection policy was rejected by its native filter.");
            chosenOnce = true;
            logger!.LogInfo("ORIGINAL-METALLURGY-AUTOSELECT kept=" + selectedId + " cost=" + selected.GetCostWithoutAnyModifications());
            yield return parameters.cardChosenCallback(selected, cards);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
