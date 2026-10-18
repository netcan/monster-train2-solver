using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MushyRoomScenario
    {
        internal const string Name = "mushy-room";
        internal const string DataId = "83e40135-9e3a-4290-ab9f-d4c8c7e1ac0b";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 1000); Set(unit, "size", 1);
            CharacterData mage = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Single(data => data.name == "MageT1_Junker");
            Set(mage, "health", 1000); Set(mage, "attackDamage", 0);
            // Keep an enemy on the restored floor for a positive second pulse;
            // surviving ordinary Shield Guards provide outside-floor controls.
            Set(mage, "ascendsTrainAutomatically", false);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Single(data => data.name == "ShieldGuardT1_BasicNonAttack");
            Set(guard, "health", 1000);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(DataId), Add(OriginalRoomCardsScenario.FightClubId), Add(DataId) }
                .Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("MUSHY-ROOM-PREPARED original room, surviving original Mage triggers, enemy/friendly/floor controls and replacement/restoration.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => GrowRoomScenario.Choose(source, ids);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
