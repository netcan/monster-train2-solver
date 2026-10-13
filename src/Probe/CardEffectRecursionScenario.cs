using System;
using System.Linq;
using BepInEx.Logging;
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

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            recursionCardInstanceId = null;
            CardManager cards = managers.GetCardManager()!;
            var gameData = managers.GetSaveManager().GetAllGameData();
            CardData recursion = gameData.FindCardData(RecursionCardId) ??
                throw new InvalidOperationException("Recursion fixture requires native WurmkinEtchings.");
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
            }
            CardState played = cards.AddNewCard(recursion, CardPile.HandPile, false, false, animate: false);
            if (played == null || !cards.GetHand().Contains(played))
                throw new InvalidOperationException("Recursion fixture could not place WurmkinEtchings in hand.");
            recursionCardInstanceId = FullBattleTrace.Active!.CardId(played);
            log.LogInfo("CARD-RECURSION-PREPARED WurmkinEtchings exhaustedSpellCopies=6");
        }

        internal static bool IsScenarioPrelude(PlayCardAction action) => recursionCardInstanceId == action.CardInstanceId;

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CardToken? card = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(item => item.DataId == RecursionCardId);
            if (card != null)
            {
                var action = new PlayCardAction(card.InstanceId, 0);
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
    }
}
