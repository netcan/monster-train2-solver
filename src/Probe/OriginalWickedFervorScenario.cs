using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalWickedFervorScenario
    {
        internal const string Name = "original-wicked-fervor";
        internal const string OriginalId = "2107d396-9427-4bfd-9889-eef36bd5c7b0";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static int[] DiscardIds { get; private set; } = Array.Empty<int>();
        internal static int[] ProtectedIds { get; private set; } = Array.Empty<int>();
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
            CardData Blank(string suffix, string? protectedTrait)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalId)); data.name = "PojuWickedFervor" + suffix;
                Set(data, "id", "fc231b73-0000-4000-8000-" + suffix); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                var traits = new List<CardTraitData>();
                if (protectedTrait != null) { var trait = new CardTraitData(); trait.Setup(protectedTrait); traits.Add(trait); }
                Set(data, "traits", traits); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", true); Set(data, "targetsRoom", true);
                var effect = new CardEffectData("CardEffectNULL", null!, Team.Type.None); effect.Cheat_SetTargetMode(TargetMode.Room);
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var ordinary = Blank("000000000001", null); var persistent = Blank("000000000002", "CardTraitPersistent");
            var infusion = Blank("000000000003", "CardTraitInfusion");
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            var created = new[] { Add(steward), Add(steward), Add(all.FindCardData(OriginalId)!), Add(all.FindCardData(OriginalId)!),
                Add(ordinary), Add(ordinary), Add(ordinary), Add(persistent), Add(infusion) };
            var keep = new CardTraitData(); keep.Setup("CardTraitPersistent"); created[3].AddTemporaryTrait(keep, managers.GetCoreManagers());
            foreach (var card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(persistent, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Take(4).Select(FullBattleTrace.Active!.CardId).ToArray();
            DiscardIds = created.Skip(4).Take(3).Select(FullBattleTrace.Active!.CardId).ToArray();
            ProtectedIds = created.Skip(7).Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-WICKED-FERVOR-PREPARED unchanged spell; three frozen ordinary discards, protected Persistent/Infusion, then zero eligible discards.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int floor = index == 1 ? 1 : 0;
            int target = index >= 2 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == CardIds[0]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
        }
    }
}
