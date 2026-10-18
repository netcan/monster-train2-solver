using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MemorialScenario
    {
        internal const string Name = "memorial";
        internal const string DataId = "98f20790-57cc-4bce-943a-2ad5955974d8";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            const string sacrifice = "9322b71d-40c0-4d72-bcf6-c5c896ba8c26";
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(DataId),
                Add(sacrifice), Add(OriginalRoomCardsScenario.FightClubId), Add(sacrifice) }.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("MEMORIAL-PREPARED unchanged original Memorial and Disassemble; Endless death/redraw, outside-floor rebirth, replacement and ordinary second death control.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            int Target() => source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[2]).Id;
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            for (int index = 0; index < 5; index++)
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                    return plays.FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) &&
                        (index != 4 || play.TargetUnitId == Target()));
            if (context.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") < 2)
                return source.Spawn.Turn < 1 ? null : plays.FirstOrDefault(play => play.CardInstanceId == ids[2] && play.RoomIndex == 1);
            if (context.Statistics.Value(ids[5], "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == ids[5] && play.RoomIndex == 0);
            return context.Statistics.Value(ids[6], "TimesPlayed", "ThisBattle") > 0 ? null :
                plays.FirstOrDefault(play => play.CardInstanceId == ids[6] && play.RoomIndex == 1 && play.TargetUnitId == Target());
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
