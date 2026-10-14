using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class FrostChannelingScenario
    {
        internal const string Name = "frost-channeling";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private const string PerilsId = "4ca4ed47-9867-4004-acf0-171c3282751f";
        private const string FlashId = "a6c8ce76-3c82-47ca-a6e0-8a20e84558dd";
        private const string FrostId = "a82b7b1b-31f1-4634-9cf3-197d17d61ba3";
        private static bool frostChosen;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            SaveManager save = managers.GetSaveManager();
            frostChosen = false;
            foreach ((string id, CardPile pile) in new[] {
                (StewardId, CardPile.HandPile), (PerilsId, CardPile.DeckPileTop),
                (FlashId, CardPile.DeckPileTop), (FlashId, CardPile.DeckPileTop),
                (FrostId, CardPile.DeckPileTop) })
            {
                CardData definition = save.GetAllGameData().FindCardData(id) ??
                    throw new InvalidOperationException("FrostChanneling fixture is missing an original card: " + id);
                if (pile == CardPile.HandPile && !cards.GetBelowHandSize())
                    throw new InvalidOperationException("FrostChanneling fixture needs a free hand slot.");
                CardState added = cards.AddNewCard(definition, pile, false, false, animate: false);
                if (added == null || !(pile == CardPile.HandPile ? cards.GetHand() : cards.GetDrawPile()).Contains(added))
                    throw new InvalidOperationException("FrostChanneling fixture could not place original card: " + id);
            }
            var draw = cards.GetDrawPile();
            foreach (CardState card in draw.Where(card => card.GetCardDataID() != PerilsId &&
                card.GetCardDataID() != FlashId && card.GetCardDataID() != FrostId).ToArray())
            {
                draw.Remove(card);
                cards.GetExhaustedPile().Add(card);
            }
            log.LogInfo("FROST-CHANNELING-PREPARED original Steward, PerilsOfProduction, two FlashFreeze, FrostChanneling");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var hand = source.Spawn.Train.Context!.Cards.Hand;
            RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
            if (source.Spawn.Turn == 0)
            {
                CardToken? steward = hand.FirstOrDefault(card => card.DataId == StewardId);
                if (steward != null && room.Units.All(unit => unit.Team != CombatTeam.Player))
                {
                    var summon = new PlayCardAction(steward.InstanceId, 0, playerPosition: 0);
                    if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
                }
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            }
            if (!frostChosen)
            {
                CardToken? perils = hand.FirstOrDefault(card => card.DataId == PerilsId);
                CombatUnit? friendly = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player);
                if (perils != null && friendly != null && friendly.Status("buff") == null)
                {
                    var cast = new PlayCardAction(perils.InstanceId, 0, targetUnitId: friendly.Id);
                    if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
                }
                CombatUnit? enemy = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Enemy);
                CardToken? flash = hand.FirstOrDefault(card => card.DataId == FlashId);
                if (flash != null && enemy != null && enemy.Status("poison") == null)
                {
                    var cast = new PlayCardAction(flash.InstanceId, 0, targetUnitId: enemy.Id);
                    if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
                }
                if (flash != null && friendly != null && enemy?.Status("poison")?.Stacks > 0 &&
                    friendly.Status("poison") == null)
                {
                    var cast = new PlayCardAction(flash.InstanceId, 0, targetUnitId: friendly.Id);
                    if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
                }
                CardToken? frost = hand.FirstOrDefault(card => card.DataId == FrostId);
                if (frost != null && enemy?.Status("poison")?.Stacks > 0 &&
                    friendly?.Status("poison")?.Stacks > 0)
                {
                    frostChosen = true;
                    return new PlayCardAction(frost.InstanceId, 0, targetUnitId: enemy.Id);
                }
                return null;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
