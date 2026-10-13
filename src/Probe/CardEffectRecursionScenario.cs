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
        private static bool filterMode;
        private static bool upgradeMode;
        private static bool choiceSubmitted;
        internal static bool FilterMode => filterMode;
        internal static bool UpgradeMode => upgradeMode;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            recursionCardInstanceId = null;
            selectedCardInstanceId = 0;
            string modifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? string.Empty;
            filterMode = modifierScenario == "card-effect-recursion-filter-choice";
            upgradeMode = modifierScenario == "card-effect-recursion-upgrade";
            chooseMode = (modifierScenario is "card-effect-recursion-choice" or "card-effect-recursion-choose-to-hand") || filterMode;
            choiceSubmitted = false;
            CardManager cards = managers.GetCardManager()!;
            SaveManager save = managers.GetSaveManager();
            var gameData = save.GetAllGameData();
            CardData recursion = gameData.FindCardData(RecursionCardId) ??
                throw new InvalidOperationException("Recursion fixture requires native WurmkinEtchings.");
            CardEffectData recursionEffect = recursion.GetEffects().Single(effect =>
                effect.GetEffectStateName() == typeof(CardEffectRecursion).Name);
            if (upgradeMode)
            {
                CardUpgradeData temporaryUpgrade = DynamicUpgradeScenario.Upgrade("PojuRecursionDamage", "f2f8c03b-327d-4f3d-8cc1-f9b2a20e4ff6",
                    3, 0, 0, 0, "armor", 0);
                (AccessTools.Field(typeof(CardUpgradeData), "statusEffectUpgrades") ??
                    throw new MissingFieldException(typeof(CardUpgradeData).FullName, "statusEffectUpgrades"))
                    .SetValue(temporaryUpgrade, new List<StatusEffectStackData>());
                (AccessTools.Field(typeof(CardEffectData), "paramCardUpgradeData") ??
                    throw new MissingFieldException(typeof(CardEffectData).FullName, "paramCardUpgradeData"))
                    .SetValue(recursionEffect, temporaryUpgrade);
                (AccessTools.Field(typeof(CardEffectData), "paramInt") ??
                    throw new MissingFieldException(typeof(CardEffectData).FullName, "paramInt"))
                    .SetValue(recursionEffect, 1);
            }
            if (chooseMode)
            {
                FieldInfo selectionMode = typeof(CardEffectData).GetField("targetCardSelectionMode",
                    BindingFlags.Instance | BindingFlags.NonPublic) ??
                    throw new MissingFieldException(typeof(CardEffectData).FullName, "targetCardSelectionMode");
                CardEffectData.CardSelectionMode mode = modifierScenario == "card-effect-recursion-choose-to-hand"
                    ? CardEffectData.CardSelectionMode.ChooseToHand : CardEffectData.CardSelectionMode.ChooseToDeck;
                selectionMode.SetValue(recursionEffect, mode);
                if (filterMode)
                {
                    FieldInfo targetCardType = typeof(CardEffectData).GetField("targetCardType",
                        BindingFlags.Instance | BindingFlags.NonPublic) ??
                        throw new MissingFieldException(typeof(CardEffectData).FullName, "targetCardType");
                    FieldInfo cardFilter = typeof(CardEffectData).GetField("paramCardFilter",
                        BindingFlags.Instance | BindingFlags.NonPublic) ??
                        throw new MissingFieldException(typeof(CardEffectData).FullName, "paramCardFilter");
                    FieldInfo maskCardType = AccessTools.Field(typeof(CardUpgradeMaskData), "cardType") ??
                        throw new MissingFieldException(typeof(CardUpgradeMaskData).FullName, "cardType");
                    CardUpgradeMaskData filter = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>();
                    filter.name = "recursion-spell-filter";
                    maskCardType.SetValue(filter, CardType.Spell);
                    targetCardType.SetValue(recursionEffect, CardType.Invalid);
                    cardFilter.SetValue(recursionEffect, filter);
                }
            }
            CardData exhaustedSpell = gameData.FindCardData(ExhaustedSpellId) ??
                throw new InvalidOperationException("Recursion fixture requires native StarterFireStarter.");
            CardData? excludedCandidate = filterMode ? save.GetDeckState()
                .Select(card => gameData.FindCardData(card.GetCardDataID()))
                .FirstOrDefault(card => card != null && card.GetCardType() == CardType.Monster) : null;
            if (filterMode && excludedCandidate == null)
                throw new InvalidOperationException("Card-filter recursion requires a Monster card candidate in the starting deck.");
            if (filterMode && exhaustedSpell.GetCardType() != CardType.Spell)
                throw new InvalidOperationException("Card-filter recursion requires StarterFireStarter to be a Spell.");
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Recursion fixture requires one free hand slot.");
            for (int index = 0; index < 6; index++)
            {
                CardData candidate = filterMode && index == 5 ? excludedCandidate! : exhaustedSpell;
                CardState added = cards.AddNewCard(candidate, CardPile.HandPile, false, false, animate: false);
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
            log.LogInfo("CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=" + (filterMode ? 5 : 6) +
                (filterMode ? " excludedCandidates=1 totalCandidates=6" : string.Empty) + " selection=" +
                (filterMode ? "ChooseToDeck+SpellFilter" : modifierScenario == "card-effect-recursion-choose-to-hand" ? "ChooseToHand" :
                    chooseMode ? "ChooseToDeck" : "RandomToDeck") + (upgradeMode ? " temporaryUpgrade=+3Damage" : string.Empty) +
                " selectedCard=" + selectedCardInstanceId);
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
                        card.InstanceId, out string? error, source.Spawn.Train.Context.CardRegistry);
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
