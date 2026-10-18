using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStrongMagicCardsScenario
    {
        internal const string Name = "original-strong-magic-cards";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add("bae01a8c-25dd-4a08-b08a-5f17a5f3f52a"),
                Add("5b2ba535-54b5-4f42-86e4-9b2ff2e07563"), Add(PlanetariumScenario.DataId),
                Add("bae01a8c-25dd-4a08-b08a-5f17a5f3f52a"), Add("5b2ba535-54b5-4f42-86e4-9b2ff2e07563"),
                Add("9113371f-f1dc-4971-b8d4-ed2930e456bc") }.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            managers.GetPlayerManager().AddEnergy(10);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-STRONG-MAGIC-CARDS-PREPARED unchanged Cosmic Bolt, Duality and RIP; native Planetarium plus zero-power floor control, damage/healing and raw trait parameter variants.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    int room = index is 1 or 2 or 3 ? 1 : 0;
                    int target = index is 2 or 3 or 5 or 6 or 7 ? source.Spawn.Train.Rooms.SelectMany(item => item.Units)
                        .Single(unit => unit.SpawnerCardId == ids[room]).Id : 0;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                        play.RoomIndex == room && (target == 0 || play.TargetUnitId == target));
                }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
