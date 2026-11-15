using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalArbitraryVerdictScenario
    {
        internal const string Name = "original-arbitrary-verdict";
        internal const string OriginalId = "5f14ee85-d078-439c-b64e-2fa7ce18e13f";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static bool calibrated;
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData Control(string suffix, string effectType, int amount, Team.Type team, bool targetless)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalId)); data.name = "PojuArbitraryVerdict" + suffix;
                Set(data, "id", "fc231b72-0000-4000-8000-" + suffix); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(effectType, null!, team); effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", amount); Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control("000000000001", "CardEffectGainEnergy", 10, Team.Type.None, true);
            var damage = Control("000000000002", "CardEffectDamage", 400, Team.Type.Monsters, false);
            var blank = Control("000000000003", "CardEffectNULL", 0, Team.Type.None, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            var created = new[] { Add(steward), Add(steward), Add(battery), Add(damage), Add(all.FindCardData(OriginalId)!),
                Add(all.FindCardData(OriginalId)!), Add(all.FindCardData(OriginalId)!) };
            foreach (var card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ARBITRARY-VERDICT-PREPARED unchanged 50-percent room spell: reduce, repeat and restore wounded units; component scheduled for the first normal turn.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.Spawn.Turn >= 1 && !calibrated)
            {
                HealthPercentCalibration.Capture(AllGameManagers.Instance!);
                calibrated = true;
            }
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int floor = index == 1 || index == 3 || index == 6 ? 1 : 0;
            int target = index == 3 ? source.Spawn.Train.Rooms[floor].Units.Single(unit => unit.SpawnerCardId == CardIds[1]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
        }
    }
}
