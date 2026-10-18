using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDeckSpellScenario
    {
        internal const string Name = "original-deck-spell";
        internal const string MasterworkId = "29223825-a614-4f34-80e6-75992d04f7b4";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] PileControlIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState Add(string id, CardPile pile = CardPile.HandPile) => cards.AddNewCard(all.FindCardData(id)!, pile, false, false, animate: false);
            CardState first = Add(steward.GetID()), outside = Add(steward.GetID());
            CardState decay = Add("61ed6915-b344-4571-8dcb-966fa4ebbaf0"), paid = Add(steward.GetID()); paid.GetCardStateModifiers().IncrementAdditionalCost(2);
            CardState master = Add(MasterworkId), spike = Add("febac44c-ce77-484d-921f-bf34ae3de377");
            CardState draw = Add(MasterworkId, CardPile.DeckPile), discard = Add(MasterworkId, CardPile.DiscardPile);
            CardIds = new[] { first, outside, decay, paid, master, spike }.Select(FullBattleTrace.Active!.CardId).ToArray();
            PileControlIds = new[] { draw, discard }.Select(FullBattleTrace.Active!.CardId).ToArray();
            save.SetForgePoints(0); managers.GetPlayerManager().AddEnergy(4); Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DECK-SPELL-PREPARED unchanged Great Decay, paid Masterwork and Railforged Spike; hand/draw/discard upgrades, unit control, granted Consume and X bonus.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
