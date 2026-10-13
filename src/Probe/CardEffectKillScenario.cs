using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using ShinyShoe.Loading;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardEffectKillScenario
    {
        private const string CardId = "75a82656-de22-4b86-b581-79ffa34f3afe";
        private static ManualLogSource? logger;
        private static bool candidateLogged;
        private static bool subtypeChecked;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            subtypeChecked = false;
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
            if (!subtypeChecked)
            {
                RoomCombatState? candidateRoom = source.Spawn.Train.Rooms.FirstOrDefault(item => item.RoomIndex == 0);
                CombatUnit[] candidates = candidateRoom?.Units.Where(unit => !unit.IsPyre).ToArray() ?? Array.Empty<CombatUnit>();
                bool hasDistinctSubtypePair = candidates.Any(matching => matching.Subtypes.Any(subtype =>
                    candidates.Any(other => other.Id != matching.Id && !other.Subtypes.Contains(subtype))));
                if (hasDistinctSubtypePair)
                {
                    CheckNativeSubtypeValidation(AllGameManagers.Instance!, candidateRoom!.RoomIndex, logger!);
                    subtypeChecked = true;
                }
            }
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

        private static void CheckNativeSubtypeValidation(AllGameManagers managers, int roomIndex, ManualLogSource log)
        {
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardId) ??
                throw new InvalidOperationException("CardEffectKill subtype check could not find KillRandomUnit.");
            CardEffectData effectData = definition.GetEffects().First(effect => effect.GetEffectStateName() == "CardEffectKill");
            var effectState = new CardEffectState();
            effectState.Setup(effectData, null, managers.GetSaveManager());
            if (effectState.GetCardEffect() is not CardEffectKill effect)
                throw new InvalidOperationException("CardEffectKill subtype check resolved the wrong native effect.");
            var characters = new List<CharacterState>();
            managers.GetRoomManager()!.GetRoom(roomIndex).AddCharactersToList(characters, Team.Type.Heroes | Team.Type.Monsters);
            var subtypeField = AccessTools.Field(typeof(CardEffectState), "paramSubtype") ??
                throw new InvalidOperationException("CardEffectKill subtype check could not find the native parameter field.");
            foreach (CharacterState matching in characters)
            foreach (SubtypeData subtype in matching.GetSubtypes().Where(item => !item.IsNone))
            foreach (CharacterState mismatching in characters)
            {
                if (ReferenceEquals(matching, mismatching)) continue;
                subtypeField.SetValue(effectState, subtype);
                bool Test(CharacterState target) => effect.TestEffect(effectState,
                    new CardEffectParams { targets = new List<CharacterState> { target } }, managers.GetCoreManagers());
                if (!Test(matching) || Test(mismatching)) continue;
                log.LogInfo("CARD-EFFECT-KILL-SUBTYPE-NATIVE subtype=" + subtype.Key +
                    " matching=" + matching.GetSourceCharacterData().GetAssetKey() +
                    " mismatching=" + mismatching.GetSourceCharacterData().GetAssetKey() + " result=true/false");
                return;
            }
            throw new InvalidOperationException("CardEffectKill subtype check found no live matching/mismatching character pair.");
        }
    }
}
