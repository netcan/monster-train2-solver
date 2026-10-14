using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class BrainTransferScenario
    {
        internal const string Name = "brain-transfer";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private const string ImpId = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";
        private const string PerilsId = "4ca4ed47-9867-4004-acf0-171c3282751f";
        private const string TransferId = "560b9623-f130-4848-9f9f-56cc6e8bf19e";
        private static bool transferChosen;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            SaveManager save = managers.GetSaveManager();
            transferChosen = false;
            foreach ((string id, CardPile pile) in new[] {
                (StewardId, CardPile.HandPile), (ImpId, CardPile.HandPile),
                (PerilsId, CardPile.HandPile), (TransferId, CardPile.HandPile) })
            {
                CardData definition = save.GetAllGameData().FindCardData(id) ??
                    throw new InvalidOperationException("BrainTransfer fixture is missing an original card: " + id);
                if (pile == CardPile.HandPile && !cards.GetBelowHandSize())
                    throw new InvalidOperationException("BrainTransfer fixture needs four free hand slots.");
                CardState added = cards.AddNewCard(definition, pile, false, false, animate: false);
                if (added == null || !(pile == CardPile.HandPile ? cards.GetHand() : cards.GetDrawPile()).Contains(added))
                    throw new InvalidOperationException("BrainTransfer fixture could not place original card: " + id);
            }
            log.LogInfo("BRAIN-TRANSFER-PREPARED original Steward, Imp, PerilsOfProduction, BrainTransfer");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var hand = source.Spawn.Train.Context!.Cards.Hand;
            RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
            CardToken? steward = hand.FirstOrDefault(card => card.DataId == StewardId);
            CardToken? imp = hand.FirstOrDefault(card => card.DataId == ImpId);
            CardToken? perils = hand.FirstOrDefault(card => card.DataId == PerilsId);
            CardToken? transfer = hand.FirstOrDefault(card => card.DataId == TransferId);
            if (steward != null && room.Units.All(unit => unit.Team != CombatTeam.Player))
            {
                var summon = new PlayCardAction(steward.InstanceId, 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (imp != null && room.Units.Any(unit => unit.Team == CombatTeam.Player) &&
                room.Units.All(unit => unit.AssetKey != "MonsterQueensImp"))
            {
                var summon = new PlayCardAction(imp.InstanceId, 0, playerPosition: 1);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            CombatUnit? target = room.Units.FirstOrDefault(unit => unit.AssetKey == "MonsterQueensImp");
            if (perils != null && target != null && target.Status("buff") == null)
            {
                var cast = new PlayCardAction(perils.InstanceId, 0, targetUnitId: target.Id);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            if (!transferChosen && transfer != null && target?.Status("buff")?.Stacks > 0 &&
                room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Id != target.Id))
            {
                transferChosen = true;
                return new PlayCardAction(transfer.InstanceId, 0, targetUnitId: target.Id);
            }
            if (!transferChosen) return null;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
