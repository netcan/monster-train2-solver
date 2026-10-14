using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class SoulSiphonScenario
    {
        internal const string CardId = "95f7b6b7-a574-477d-abef-a8bd3388d87b";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static ManualLogSource? logger;
        private static int stewardCardId;
        private static int spellCardId;
        private static bool chosen;
        private static bool marked;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "soul-siphon";
        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!Enabled || marked || action.CardInstanceId != spellCardId) return false;
            marked = true;
            return true;
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            chosen = false;
            marked = false;
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardData definition = data.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Soul Siphon is missing.");
            CardData steward = data.FindCardData(StewardId) ??
                throw new InvalidOperationException("Original Train Steward is missing.");
            if (definition.GetEffects().Count != 2 ||
                definition.GetEffects()[0].GetEffectStateName() != "CardEffectAdjustCorruption" ||
                definition.GetEffects()[0].GetParamInt() != -1 || !definition.GetEffects()[0].GetParamBool() ||
                definition.GetEffects()[1].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                definition.GetEffects()[1].GetParamStatusEffects().Single().statusId != "scorch")
                throw new InvalidOperationException("Original Soul Siphon effects changed.");
            StatusEffectData status = StatusEffectManager.Instance.GetStatusEffectDataById("scorch") ??
                throw new InvalidOperationException("Original Scorch status is missing.");
            log.LogInfo("SOUL-SIPHON-STATUS stage=" + status.GetPrimaryTriggerStage() +
                " removeTriggered=" + status.GetRemoveWhenTriggered() +
                " removeStackEnd=" + status.GetRemoveStackAtEndOfTurn() +
                " removeAllEnd=" + status.GetRemoveAtEndOfTurn());
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count > 8) throw new InvalidOperationException("Soul Siphon requires two free hand slots.");
            CardState unit = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            if (unit == null || spell == null || !cards.GetHand().Contains(unit) || !cards.GetHand().Contains(spell))
                throw new InvalidOperationException("Soul Siphon could not add its original cards.");
            stewardCardId = FullBattleTrace.Active!.CardId(unit);
            spellCardId = FullBattleTrace.Active.CardId(spell);
            RoomState room = managers.GetRoomManager()!.GetRoom(0);
            log.LogInfo("SOUL-SIPHON-PREPARED steward=" + stewardCardId + " spell=" + spellCardId +
                " room=0 current=" + room.GetCurrentCorruption() + " max=" + room.GetMaxCorruption());
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (chosen) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            CombatUnit? steward = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.SpawnerCardId == stewardCardId);
            if (steward == null) return plays.FirstOrDefault(action => action.CardInstanceId == stewardCardId && action.RoomIndex == 0)
                ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction? action = plays.FirstOrDefault(item => item.CardInstanceId == spellCardId && item.RoomIndex == 0);
            if (action == null) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            RoomCapacityState before = source.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
            RoomCapacityState? after = predicted.State?.Spawn.Train.Context?.RoomCapacities?.Single(room => room.RoomIndex == 0);
            CombatUnit? target = predicted.State?.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.Id == steward.Id);
            logger!.LogInfo("SOUL-SIPHON-CANDIDATE supported=" + predicted.Supported +
                " current=" + before.Corruption + "->" + after?.Corruption +
                " scorch=" + (target?.Status("scorch")?.Stacks ?? -1) + " reason=" + predicted.Reason);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            chosen = true;
            return action;
        }
    }
}
