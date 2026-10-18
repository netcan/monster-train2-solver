using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class GrowRoomScenario
    {
        internal const string Name = "grow-room";
        internal const string DataId = "7768326f-bc62-4143-93ba-63a5a17b9306";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int StatusFilter { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] {
                new StatusEffectStackData { statusId = "buff", count = 3 }, new StatusEffectStackData { statusId = "regen", count = 2 },
                new StatusEffectStackData { statusId = "armor", count = 4 } });
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            StatusFilter = all.FindCardData(DataId)!.GetEffects().Single().GetParamCardUpgradeData()!
                .GetRoomModifierUpgrades().Single().GetParamCardEffectData().Single().GetAdditionalParamInt();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(DataId), Add(OriginalRoomCardsScenario.FightClubId), Add(DataId) }
                .Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("GROW-ROOM-PREPARED original post-combat room; status/floor controls, turn-two replacement and turn-three restoration.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    if (index >= 3 && source.Spawn.Turn < index - 1) return null;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
                }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
