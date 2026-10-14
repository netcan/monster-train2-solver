using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrampleScenario
    {
        internal const string Name = "original-trample";
        private const string BasherId = "c493d7e0-6731-4d8a-9ae1-56e44febb891";
        private static bool played;
        private static int scenarioActionId;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardData card = managers.GetSaveManager().GetAllGameData().FindCardData(BasherId) ??
                throw new InvalidOperationException("Original Basher is missing.");
            CharacterData? unit = card.GetSpawnCharacterData();
            var trample = unit?.GetStartingStatusEffects().SingleOrDefault(status => status.statusId == "trample");
            if (card.name != "Basher" || unit?.GetAssetKey() != "MonsterBasher" || trample?.count != 1)
                throw new InvalidOperationException("Original Basher trample definition changed.");
            CardManager cards = managers.GetCardManager()!;
            if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Basher requires a free hand slot.");
            CardState added = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Original Basher did not enter the hand.");
            played = false; scenarioActionId = 0;
            log.LogInfo("TRAMPLE-PREPARED original=Basher unit=MonsterBasher stacks=1");
        }

        internal static PlayCardAction? Choose(BattleTurnState state, ManualLogSource log)
        {
            if (!played)
            {
                CardToken? card = state.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(item => item.DataId == BasherId);
                if (card != null)
                {
                    PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(item =>
                        item.CardInstanceId == card.InstanceId && item.RoomIndex == 0);
                    if (action == null)
                    {
                        BattleActionResult reason = BattleActionModel.PlayCard(state, new PlayCardAction(card.InstanceId, 0));
                        throw new InvalidOperationException("Original Basher cannot be played: " + reason.Reason);
                    }
                    played = true; scenarioActionId = action.CardInstanceId;
                    log.LogInfo("TRAMPLE-PLAY original=Basher room=0 card=" + card.InstanceId);
                    return action;
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
