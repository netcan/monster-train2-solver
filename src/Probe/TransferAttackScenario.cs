using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TransferAttackScenario
    {
        internal const string Name = "transfer-attack";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private const string ImpId = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";
        private const string SpellId = "1450e56b-ca09-453a-937a-97bd44bcb3de";
        private static int stewardId;
        private static int impId;
        private static int spellId;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CardData spell = save.GetAllGameData().FindCardData(SpellId) ??
                throw new InvalidOperationException("Transfer fixture requires original VineGrasp card data.");
            if (spell.name != "VineGrasp" || spell.GetEffects().All(effect => effect.GetEffectStateName() != "CardEffectFloorRearrange"))
                throw new InvalidOperationException("Transfer fixture's source spell definition changed.");
            var transfer = new CardEffectData("CardEffectTransferAttackFromUnit", null!, Team.Type.Monsters);
            transfer.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(transfer, 0);
            AccessTools.Field(typeof(CardEffectData), "additionalParamInt").SetValue(transfer, 2);
            var effects = spell.GetEffects();
            effects.Clear(); effects.Add(transfer);
            AccessTools.Field(typeof(CardData), "targetless").SetValue(spell, false);
            AccessTools.Field(typeof(CardData), "targetsRoom").SetValue(spell, true);
            foreach (CardState card in cards.GetAllCards(new System.Collections.Generic.List<CardState>())
                .Where(card => card.GetCardDataID() == SpellId)) card.Setup(spell, save);
            CardState Add(string id)
            {
                CardData data = save.GetAllGameData().FindCardData(id) ??
                    throw new InvalidOperationException("Transfer fixture is missing an original card: " + id);
                if (!cards.GetBelowHandSize()) throw new InvalidOperationException("Transfer fixture needs three hand slots.");
                CardState added = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
                if (added == null || !cards.GetHand().Contains(added))
                    throw new InvalidOperationException("Transfer fixture could not add the original card: " + id);
                return added;
            }
            stewardId = FullBattleTrace.Active!.CardId(Add(StewardId));
            impId = FullBattleTrace.Active.CardId(Add(ImpId));
            spellId = FullBattleTrace.Active.CardId(Add(SpellId));
            AccessTools.Field(managers.GetCombatManager()!.GetType(), "combatStateChanged")
                .SetValue(managers.GetCombatManager(), true);
            log.LogInfo("TRANSFER-ATTACK-PREPARED source=" + impId + " recipient=" + stewardId + " spell=" + spellId);
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var hand = source.Spawn.Train.Context!.Cards.Hand;
            RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
            if (hand.Any(card => card.InstanceId == stewardId))
            {
                var action = new PlayCardAction(stewardId, 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
            if (hand.Any(card => card.InstanceId == impId) &&
                room.Units.Any(unit => unit.SpawnerCardId == stewardId))
            {
                var action = new PlayCardAction(impId, 0, playerPosition: 1);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
            CombatUnit? imp = room.Units.FirstOrDefault(unit => unit.SpawnerCardId == impId);
            if (hand.Any(card => card.InstanceId == spellId) && imp != null &&
                room.Units.Any(unit => unit.SpawnerCardId == stewardId))
            {
                var action = new PlayCardAction(spellId, 0, targetUnitId: imp.Id);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
