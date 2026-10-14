using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class SetHealthScenario
    {
        internal const string CardId = "4c0f6e0e-ac05-4e0c-a478-590ac06d646f";
        private static ManualLogSource? logger;
        private static bool candidateLogged;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            CardManager cards = managers.GetCardManager()!;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardId) ??
                throw new InvalidOperationException("SetHealth fixture requires native RiskyOperation.");
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("SetHealth fixture requires one free hand slot.");
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("SetHealth fixture could not place RiskyOperation in hand.");
            log.LogInfo("SET-HEALTH-PREPARED native RiskyOperation card=" + CardId);
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CardToken? card = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(item => item.DataId == CardId);
            if (card != null)
            {
                RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
                CombatUnit? target = room.Units.LastOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 1);
                if (target != null)
                {
                    var action = new PlayCardAction(card.InstanceId, 0, targetUnitId: target.Id);
                    BattleActionResult result = BattleActionModel.PlayCard(source, action);
                    if (!candidateLogged)
                    {
                        logger!.LogInfo("SET-HEALTH-CANDIDATE supported=" + result.Supported + " rejection=" + result.Rejection +
                            " reason=" + result.Reason + " target=" + target.Id + " hand-card=" + card.InstanceId);
                        candidateLogged = true;
                    }
                    if (result.Supported) return action;
                }
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
