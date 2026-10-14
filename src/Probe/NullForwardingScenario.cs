using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class NullForwardingScenario
    {
        internal const string Name = "null-forwarding";
        private const string CleaveId = "e8e593f4-7e2d-4791-a7d4-8a6155f1a50f";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static bool played;
        private static int scenarioActionId;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CleaveId) ??
                throw new InvalidOperationException("Original Cleave definition is missing.");
            CardEffectData[] effects = definition.GetEffects().ToArray();
            if (effects.Length != 2 || effects[0].GetEffectStateName() != "CardEffectNULL" ||
                effects[0].GetTargetMode() != TargetMode.FrontInRoom || effects[0].GetAdditionalParamInt() != 1 ||
                effects[0].GetParamBool() || effects[0].GetParamBool2() ||
                effects[1].GetEffectStateName() != "CardEffectDamagePerSourceAttack" ||
                effects[1].GetTargetMode() != TargetMode.Room)
                throw new InvalidOperationException("Original Cleave forwarding definition changed.");
            CardManager cards = managers.GetCardManager()!;
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Cleave requires a free hand slot.");
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Cleave did not enter the initial hand.");
            played = false;
            scenarioActionId = 0;
            log.LogInfo("NULL-FORWARDING-PREPARED original=Cleave card=" + CleaveId);
        }

        internal static PlayCardAction? Choose(BattleTurnState state, ManualLogSource log)
        {
            if (!played)
            {
                foreach (RoomCombatState room in state.Spawn.Train.Rooms)
                {
                    if (!room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0) ||
                        !room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Health > 0)) continue;
                    CardToken? cleave = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == CleaveId);
                    if (cleave == null) break;
                    var action = new PlayCardAction(cleave.InstanceId, room.RoomIndex);
                    BattleActionResult result = BattleActionModel.PlayCard(state, action);
                    if (!result.Supported) throw new InvalidOperationException("Cleave model rejected native scene: " + result.Reason);
                    played = true;
                    scenarioActionId = action.CardInstanceId;
                    log.LogInfo("NULL-FORWARDING-PLAY room=" + room.RoomIndex + " card=" + cleave.InstanceId);
                    return action;
                }
                PlayCardAction? steward = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(action =>
                    action.RoomIndex == 0 && state.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == StewardId);
                if (steward != null)
                {
                    scenarioActionId = steward.CardInstanceId;
                    return steward;
                }
            }
            scenarioActionId = 0;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
        }

        internal static bool ConsumeScenarioActionMark(PlayCardAction action)
        {
            bool matches = scenarioActionId > 0 && action.CardInstanceId == scenarioActionId;
            scenarioActionId = 0;
            return matches;
        }
    }
}
