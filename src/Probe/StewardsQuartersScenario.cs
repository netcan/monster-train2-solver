using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StewardsQuartersScenario
    {
        internal const string Name = "stewards-quarters";
        internal const string DataId = "a3366bba-51f4-4a1d-803a-dc172ac4a76d";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 200); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "multistrike", count = 1 } });
            CardData ordinary = UnityEngine.Object.Instantiate(steward); ordinary.name = "PojuQuartersOrdinary";
            Set(ordinary, "id", "ac110012-0000-4000-8000-000000000001");
            CharacterData ordinaryUnit = UnityEngine.Object.Instantiate(unit); ordinaryUnit.name = "PojuQuartersOrdinaryUnit";
            Set(ordinaryUnit, "id", "ac110012-0000-4000-8000-000000000002"); Set(ordinaryUnit, "subtypeKeys", new List<string>());
            Set(ordinaryUnit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters); Set(spawn, "paramCharacterData", ordinaryUnit);
            Set(ordinary, "effects", new List<CardEffectData> { spawn });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(ordinary);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Add(ordinaryUnit);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(ordinary.GetID()), Add(DataId),
                Add(steward.GetID()), Add(RoomCapacityAttachmentScenario.CardDataId), Add(DataId) }.Select(FullBattleTrace.Active!.CardId).ToArray();
            managers.GetPlayerManager().AddEnergy(10); Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("STEWARDS-QUARTERS-PREPARED two original rooms; existing/later Steward, ordinary/outside controls, pre-existing Multistrike and replacement/restoration.");
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
