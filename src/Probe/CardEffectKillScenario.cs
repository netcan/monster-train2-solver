using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;
using ShinyShoe.Loading;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardEffectKillScenario
    {
        private const string CardId = "75a82656-de22-4b86-b581-79ffa34f3afe";
        private static ManualLogSource? logger;
        private static bool candidateLogged;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            CardManager cards = managers.GetCardManager()!;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardId) ??
                throw new InvalidOperationException("CardEffectKill fixture requires native KillRandomUnit.");
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("CardEffectKill fixture requires one free hand slot.");
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("CardEffectKill fixture could not place KillRandomUnit in hand.");
            log.LogInfo("CARD-EFFECT-KILL-PREPARED native KillRandomUnit card=" + CardId);
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CardToken? kill = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == CardId);
            if (kill != null)
            {
                RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
                bool hasEnemy = room.Units.Any(unit => unit.Team == CombatTeam.Enemy && !unit.IsPyre);
                bool hasPlayer = room.Units.Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
                if (hasEnemy && hasPlayer)
                {
                    var action = new PlayCardAction(kill.InstanceId, room.RoomIndex);
                    BattleActionResult result = BattleActionModel.PlayCard(source, action);
                    if (!candidateLogged)
                    {
                        logger!.LogInfo("CARD-EFFECT-KILL-CANDIDATE supported=" + result.Supported + " rejection=" + result.Rejection +
                            " reason=" + result.Reason + " hand-card=" + kill.InstanceId);
                        candidateLogged = true;
                    }
                    if (result.Supported) return action;
                }
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
