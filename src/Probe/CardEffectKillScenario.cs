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
        private static bool subtypeScenario;
        private static bool subtypeActionLogged;
        private static bool subtypePreludeComplete;
        private static int subtypeActionCardInstanceId;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool withSubtype = false)
        {
            logger = log;
            candidateLogged = false;
            subtypeChecked = false;
            subtypeScenario = withSubtype;
            subtypeActionLogged = false;
            subtypePreludeComplete = false;
            subtypeActionCardInstanceId = 0;
            CardManager cards = managers.GetCardManager()!;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardId) ??
                throw new InvalidOperationException("CardEffectKill fixture requires native KillRandomUnit.");
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("CardEffectKill fixture requires one free hand slot.");
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("CardEffectKill fixture could not place KillRandomUnit in hand.");
            log.LogInfo("CARD-EFFECT-KILL-PREPARED native KillRandomUnit card=" + CardId + " subtypeScenario=" + subtypeScenario);
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
                if (subtypeScenario && !subtypeActionLogged &&
                    TryChooseSubtypePlay(source, kill.InstanceId, room.RoomIndex, out PlayCardAction? subtypeAction))
                {
                    subtypeActionLogged = true;
                    return subtypeAction;
                }
                if (hasEnemy && hasPlayer && !subtypeScenario)
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

        internal static bool IsScenarioPrelude(PlayCardAction action)
        {
            if (!subtypeScenario || subtypePreludeComplete) return false;
            if (subtypeActionCardInstanceId > 0 && action.CardInstanceId == subtypeActionCardInstanceId)
                subtypePreludeComplete = true;
            return true;
        }

        private static bool TryChooseSubtypePlay(BattleTurnState source, int cardInstanceId, int roomIndex,
            out PlayCardAction? action)
        {
            action = null;
            AllGameManagers managers = AllGameManagers.Instance!;
            CardState card = managers.GetCardManager()!.GetHand()
                .FirstOrDefault(item => item.GetCardDataID() == CardId) ??
                throw new InvalidOperationException("CardEffectKill subtype scenario lost its native card.");
            CardEffectState[] killStates = card.GetEffectStates()
                .Where(effect => effect.GetCardEffect() is CardEffectKill).ToArray();
            if (killStates.Length != 2)
                throw new InvalidOperationException("CardEffectKill subtype scenario expected two native Kill effects.");

            foreach (CardEffectState state in killStates) SetSubtype(state, SubtypeData.None);
            BattleTurnState unfiltered = FullBattleTrace.Active!.CaptureDecision();
            var candidate = new PlayCardAction(cardInstanceId, roomIndex);
            BattleActionResult baseline = BattleActionModel.PlayCard(unfiltered, candidate);
            if (!baseline.Supported)
            {
                logger!.LogInfo("CARD-EFFECT-KILL-SUBTYPE-SKIP stage=unfiltered reason=" + baseline.Reason);
                return false;
            }

            RoomCombatState beforeRoom = unfiltered.Spawn.Train.Rooms.Single(item => item.RoomIndex == roomIndex);
            RoomCombatState afterRoom = baseline.State!.Spawn.Train.Rooms.Single(item => item.RoomIndex == roomIndex);
            HashSet<int> afterIds = afterRoom.Units.Select(unit => unit.Id).ToHashSet();
            CombatUnit[] removed = beforeRoom.Units.Where(unit => !unit.IsPyre && !afterIds.Contains(unit.Id)).ToArray();
            var usedTargets = new HashSet<int>();
            var assignments = new List<(int UnitId, string Subtype)>();
            foreach (CardEffectState state in killStates)
            {
                Team.Type targetTeams = state.GetSourceCardEffectData().GetTargetTeamType();
                CombatTeam? targetTeam = targetTeams.HasFlag(Team.Type.Heroes) ? CombatTeam.Enemy :
                    targetTeams.HasFlag(Team.Type.Monsters) ? CombatTeam.Player : null;
                if (!targetTeam.HasValue) continue;
                CombatUnit? target = removed.FirstOrDefault(unit => unit.Team == targetTeam.Value &&
                    !usedTargets.Contains(unit.Id) && unit.Subtypes.Count > 0);
                if (target == null) continue;
                string subtypeKey = target.Subtypes[0];
                SubtypeData subtype = SubtypeManager.GetSubtypeData(subtypeKey);
                SetSubtype(state, subtype);
                usedTargets.Add(target.Id);
                assignments.Add((target.Id, subtypeKey));
            }
            if (assignments.Count == 0)
            {
                logger!.LogInfo("CARD-EFFECT-KILL-SUBTYPE-SKIP stage=target-selection removed=" +
                    string.Join(",", removed.Select(unit => unit.Id + ":" + unit.Team + ":" + string.Join("+", unit.Subtypes))));
                return false;
            }

            BattleTurnState filtered = FullBattleTrace.Active.CaptureDecision();
            BattleActionResult checkedResult = BattleActionModel.PlayCard(filtered, candidate);
            if (!checkedResult.Supported)
            {
                logger!.LogInfo("CARD-EFFECT-KILL-SUBTYPE-SKIP stage=filtered reason=" + checkedResult.Reason +
                    " targets=" + string.Join(",", assignments.Select(item => item.UnitId + ":" + item.Subtype)));
                foreach (CardEffectState state in killStates) SetSubtype(state, SubtypeData.None);
                FullBattleTrace.Active.CaptureDecision();
                return false;
            }

            logger!.LogInfo("CARD-EFFECT-KILL-SUBTYPE-PLAY targets=" + string.Join(",",
                assignments.Select(item => item.UnitId + ":" + item.Subtype)) + " result=supported");
            subtypeActionCardInstanceId = cardInstanceId;
            action = candidate;
            return true;
        }

        private static void SetSubtype(CardEffectState state, SubtypeData subtype)
        {
            var dataField = AccessTools.Field(typeof(CardEffectData), "paramSubtype") ??
                throw new InvalidOperationException("CardEffectKill subtype scenario could not find CardEffectData.paramSubtype.");
            var stateField = AccessTools.Field(typeof(CardEffectState), "paramSubtype") ??
                throw new InvalidOperationException("CardEffectKill subtype scenario could not find CardEffectState.paramSubtype.");
            dataField.SetValue(state.GetSourceCardEffectData(), subtype.IsNone ? string.Empty : subtype.Key);
            stateField.SetValue(state, subtype);
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
