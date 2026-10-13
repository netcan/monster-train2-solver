using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DragonHoardScenario
    {
        private const string CardId = "f884b713-6268-4ac2-8360-6a6c6b3c155e";
        private static ManualLogSource? logger;
        private static int cardInstanceId;
        private static bool candidateLogged;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "dragon-hoard";
        internal static bool Prepared { get; private set; }
        internal static bool IsScenarioAction(PlayCardAction action) => Enabled && action.CardInstanceId == cardInstanceId;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            Prepared = false;
            SaveManager save = managers.GetSaveManager();
            if (save.GetDragonsHoardCap() < 4)
                throw new InvalidOperationException("Dragon's Hoard fixture requires a cap of at least four.");
            save.SetDragonsHoardAmount(2);
            if (save.GetDragonsHoardAmount() != 2)
                throw new InvalidOperationException("Dragon's Hoard fixture could not set the captured starting amount.");

            CardManager cards = managers.GetCardManager()!;
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Dragon's Hoard fixture requires one free hand slot.");
            CardData definition = save.GetAllGameData().FindCardData(CardId) ??
                throw new InvalidOperationException("Dragon's Hoard fixture requires native Alchemy.");
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Dragon's Hoard fixture could not place Alchemy in hand.");
            if (!added.GetEffectStates().Any(effect => effect.GetCardEffect() is CardEffectAdjustDragonsHoard))
                throw new InvalidOperationException("Native Alchemy did not initialize its Dragon's Hoard effect.");
            AccessTools.Field(typeof(CardState), "cost").SetValue(added, 0);
            cardInstanceId = FullBattleTrace.Active!.CardId(added);
            Prepared = true;
            log.LogInfo("DRAGON-HOARD-PREPARED native Alchemy start=2 cap=" + save.GetDragonsHoardCap() +
                " card=" + cardInstanceId + " cost=0");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared) return null;
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(item => item.CardInstanceId == cardInstanceId);
            if (action == null) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            if (!candidateLogged)
            {
                logger!.LogInfo("DRAGON-HOARD-CANDIDATE supported=" + predicted.Supported + " before=" + source.DragonsHoard +
                    " after=" + (predicted.State?.DragonsHoard.ToString() ?? "<none>") + " reason=" + predicted.Reason);
                candidateLogged = true;
            }
            return predicted.Supported ? action : BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
