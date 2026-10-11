using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;
using ShinyShoe.Loading;

namespace MonsterTrain2Poju.Probe
{
    internal static class SacrificeScenario
    {
        private const string CardId = "17f98264-cbc5-46c4-8fd8-8f86c413e228";
        private static ManualLogSource? logger;
        private static bool candidateLogged;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            CardManager cards = managers.GetCardManager()!;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardId) ??
                throw new InvalidOperationException("Sacrifice fixture requires the native ImpProvisation card definition.");
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Sacrifice fixture requires one free hand slot.");
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Sacrifice fixture could not place ImpProvisation in hand.");
            log.LogInfo("SACRIFICE-EFFECTS-PREPARED native ImpProvisation card=" + CardId);
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CardToken? sacrifice = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == CardId);
            if (sacrifice != null)
            {
                RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
                CombatUnit? target = room.Units.LastOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
                if (target != null)
                {
                    var action = new PlayCardAction(sacrifice.InstanceId, 0, targetUnitId: target.Id);
                    BattleActionResult result = BattleActionModel.PlayCard(source, action);
                    if (!candidateLogged)
                    {
                        logger!.LogInfo("SACRIFICE-CANDIDATE supported=" + result.Supported + " rejection=" + result.Rejection +
                            " reason=" + result.Reason + " target=" + target.Id + " hand-card=" + sacrifice.InstanceId);
                        candidateLogged = true;
                    }
                    if (result.Supported) return action;
                }
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
