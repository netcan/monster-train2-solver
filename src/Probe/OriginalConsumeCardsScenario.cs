using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalConsumeCardsScenario
    {
        internal const string Name = "original-consume-cards";
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
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add("2ed78ce2-13c3-4e82-963f-99c9ea12d0f0"),
                Add("a6dfc0d2-9bfe-499a-b521-0a2c4cefc1a2"), Add("0ce88ae8-27f6-4894-8b1c-9c1aeed39f4d"),
                Add("ec0980cf-9f88-4d38-9704-b89042f2f625"), Add("45ace521-7551-4473-a4d7-2cf7ecc9fa44"),
                Add("2ed78ce2-13c3-4e82-963f-99c9ea12d0f0"), Add("9ea70171-8afd-4fab-ad54-7ff5c8b478b8"),
                Add("768a53f0-2743-4298-82c3-8541cbecf935") }.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-CONSUME-CARDS-PREPARED seven unchanged definitions; played Consume, unplayed discard/redraw, enemy statuses and outside-floor controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    if (index >= 7 && source.Spawn.Turn < 1) return null;
                    int target = index is 2 or 3 or 4 or 5 or 7 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                        play.RoomIndex == (index == 1 ? 1 : 0) && (target == 0 || play.TargetUnitId == target));
                }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
