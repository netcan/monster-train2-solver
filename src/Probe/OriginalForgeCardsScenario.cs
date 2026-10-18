using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalForgeCardsScenario
    {
        internal const string Name = "original-forge-cards";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 60);
            Set(steward.GetSpawnCharacterData(), "health", 200); Set(steward.GetSpawnCharacterData(), "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            // Deployment shuffles discard back into draw. Isolate the active
            // deck so the normal five-card draw includes both original spells.
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState first = Add(steward.GetID()), outside = Add(steward.GetID()), sacrifice = Add(steward.GetID());
            CardState master = Add("29223825-a614-4f34-80e6-75992d04f7b4");
            CardState starter = Add("87398615-091c-465e-8225-5de370f5953e");
            CardState disassemble = Add("9322b71d-40c0-4d72-bcf6-c5c896ba8c26");
            CardState anvil = Add("2ca56db0-bef2-48c6-b575-85bb611a3b0a");
            CardState spike = Add("febac44c-ce77-484d-921f-bf34ae3de377");
            // Keep both original cards intact; the next normal draw supplies them
            // after enemies enter, with two energy for Anvil and one for the X spell.
            cards.MoveCardFromHandToTopOfDeck(spike); cards.MoveCardFromHandToTopOfDeck(anvil);
            CardIds = new[] { first, outside, sacrifice, master, starter, disassemble, anvil, spike }.Select(FullBattleTrace.Active!.CardId).ToArray();
            save.SetForgePoints(0); managers.GetPlayerManager().AddEnergy(10); Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-FORGE-CARDS-PREPARED five unchanged forge spells; deployment accumulation/sacrifice and next-turn enemy damage/reset/X scaling.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
            {
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0) continue;
                int target = index == 5 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[2]).Id : 0;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                    play.RoomIndex == (index == 1 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
            }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
