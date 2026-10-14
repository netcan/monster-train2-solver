using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class PermanentCorruptionScenario
    {
        internal const string CardId = "c354dd2c-0087-4eae-82d9-d4ab17b15f19";
        private static ManualLogSource? logger;
        private static int cardId;
        private static bool chosen;
        private static bool marked;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "permanent-corruption";

        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!Enabled || marked || action.CardInstanceId != cardId) return false;
            marked = true;
            return true;
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            chosen = false;
            marked = false;
            CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(CardId) ??
                throw new InvalidOperationException("Original Eternal Kinstone is missing.");
            CardEffectData effect = definition.GetEffects().Single();
            if (effect.GetEffectStateName() != "CardEffectAddPermanentCorruption" ||
                effect.GetTargetMode() != TargetMode.Room || effect.GetParamInt() != 1)
                throw new InvalidOperationException("Original Eternal Kinstone effect changed.");
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count >= 10) throw new InvalidOperationException("Eternal Kinstone requires a free hand slot.");
            CardState card = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (card == null || !cards.GetHand().Contains(card))
                throw new InvalidOperationException("Eternal Kinstone was not added to hand.");
            cardId = FullBattleTrace.Active!.CardId(card);
            RoomState room = managers.GetRoomManager()!.GetRoom(0);
            log.LogInfo("PERMANENT-CORRUPTION-PREPARED card=" + cardId + " room=0 current=" +
                room.GetCurrentCorruption() + " max=" + room.GetMaxCorruption() +
                " permanent=" + room.GetPermanentCorruption());
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (chosen) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(item => item.CardInstanceId == cardId && item.RoomIndex == 0);
            if (action == null) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            RoomCapacityState before = source.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
            RoomCapacityState? after = predicted.State?.Spawn.Train.Context?.RoomCapacities?.Single(room => room.RoomIndex == 0);
            logger!.LogInfo("PERMANENT-CORRUPTION-CANDIDATE supported=" + predicted.Supported +
                " current=" + before.Corruption + "->" + after?.Corruption +
                " permanent=" + before.PermanentCorruption + "->" + after?.PermanentCorruption +
                " reason=" + predicted.Reason);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            chosen = true;
            return action;
        }
    }
}
