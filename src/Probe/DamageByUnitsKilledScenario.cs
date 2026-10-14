using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageByUnitsKilledScenario
    {
        internal const string ImpCardId = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";
        internal const string SpellCardId = "d9c0c510-ccb5-4a89-9f3b-99799e765540";
        private const string ImpSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";
        private static ManualLogSource? logger;
        private static bool candidateLogged;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            CardManager cards = managers.GetCardManager()!;
            foreach ((string id, CardPile pile) in new[] { (ImpCardId, CardPile.HandPile), (SpellCardId, CardPile.DeckPileTop) })
            {
                CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(id) ??
                    throw new InvalidOperationException("Damage-by-killed-units fixture requires original Imp and Shardtail cards.");
                if (pile == CardPile.HandPile && !cards.GetBelowHandSize())
                    throw new InvalidOperationException("Damage-by-killed-units fixture requires one free hand slot.");
                CardState added = cards.AddNewCard(definition, pile, false, false, animate: false);
                if (added == null || !(pile == CardPile.HandPile ? cards.GetHand() : cards.GetDrawPile()).Contains(added))
                    throw new InvalidOperationException("Damage-by-killed-units fixture could not place original card in its pile: " + id);
            }
            List<CardState> draw = cards.GetDrawPile();
            foreach (CardState card in draw.Where(card => card.GetCardDataID() != SpellCardId).ToArray())
            {
                draw.Remove(card);
                cards.GetExhaustedPile().Add(card);
            }
            log.LogInfo("DAMAGE-BY-UNITS-KILLED-PREPARED native StarterImp and ShardtailSacrificeImps1");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CardToken? imp = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == ImpCardId);
            if (imp != null)
            {
                RoomCombatState floor = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
                int friendlyCount = floor.Units.Count(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
                if (friendlyCount == 0)
                {
                    PlayCardAction? defender = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                        action.RoomIndex == 0 && source.Spawn.Train.Context.Cards.Hand.Any(card =>
                            card.InstanceId == action.CardInstanceId && card.InstanceId != imp.InstanceId &&
                            source.PlayRules?.Cards.Any(rule => rule.DataId == card.DataId && rule.CardType == "Monster") == true));
                    if (defender != null) return defender;
                }
                else
                {
                    var summon = new PlayCardAction(imp.InstanceId, 0, playerPosition: friendlyCount);
                    BattleActionResult result = BattleActionModel.PlayCard(source, summon);
                    if (result.Supported) return summon;
                    LogCandidate(result, "summon");
                }
            }
            RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
            bool hasImp = room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(ImpSubtype));
            bool hasEnemy = room.Units.Any(unit => unit.Team == CombatTeam.Enemy);
            CardToken? spell = source.Spawn.Train.Context.Cards.Hand.FirstOrDefault(card => card.DataId == SpellCardId);
            if (spell != null && hasImp && hasEnemy)
            {
                var action = new PlayCardAction(spell.InstanceId, 0);
                BattleActionResult result = BattleActionModel.PlayCard(source, action);
                if (result.Supported) return action;
                LogCandidate(result, "spell");
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }

        private static void LogCandidate(BattleActionResult result, string phase)
        {
            if (candidateLogged) return;
            logger!.LogInfo("DAMAGE-BY-UNITS-KILLED-CANDIDATE phase=" + phase + " supported=" + result.Supported +
                " rejection=" + result.Rejection + " reason=" + result.Reason);
            candidateLogged = true;
        }
    }
}
