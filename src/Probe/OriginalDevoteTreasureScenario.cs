using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDevoteTreasureScenario
    {
        internal const string Name = "original-devote-treasure";
        internal const string UnitId = OriginalDevotionUpgradeScenario.OriginalId;
        internal const string SpellId = "690ba1bf-378e-4db3-a2ab-c37dc36df04b";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] SetupIds { get; private set; } = Array.Empty<int>();
        internal static int[] TreasureIds { get; private set; } = Array.Empty<int>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                DevotionProbe.Read(save, "GetDevotionLevel") != 0 || DevotionProbe.Read(save, "GetDevotionPoints") != 0)
                throw new InvalidOperationException("Treasure Devote requires isolated zero-Devotion deployment.");
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData Control(int suffix, string type, int amount, bool persistent = false)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755")); data.name = "PojuDevoteTreasure" + suffix;
                Set(data, "id", "fc231b76-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default);
                var traits = new List<CardTraitData>();
                if (persistent) { var trait = new CardTraitData(); trait.Setup("CardTraitPersistent"); traits.Add(trait); }
                Set(data, "traits", traits); Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                Set(data, "startingUpgrades", new List<CardUpgradeData>()); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, Team.Type.None); effect.Cheat_SetTargetMode(TargetMode.Room);
                Set(effect, "paramInt", amount); Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(1, "CardEffectGainEnergy", 10);
            var almostLevel = Control(2, "CardEffectGainDevotionPoints", DevotionProbe.Read(all.GetBalanceData(), "GetMonolithDevotionFirstThreshold") - 1);
            var blank = Control(3, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            var setup = new[] { Add(steward), Add(battery), Add(almostLevel) };
            var treasures = new[] { Add(all.FindCardData(UnitId)!), Add(all.FindCardData(UnitId)!), Add(all.FindCardData(UnitId)!),
                Add(all.FindCardData(UnitId)!), Add(all.FindCardData(SpellId)!) };
            foreach (var card in setup.Skip(1).Concat(treasures)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            SetupIds = setup.Select(FullBattleTrace.Active!.CardId).ToArray(); TreasureIds = treasures.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DEVOTE-TREASURE-PREPARED unchanged DarkInitiate/GuardianAmulet; level-up precedes free casts, capacity overflow and Pyre refusal.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var context = source.Spawn.Train.Context!;
            int index = Array.FindIndex(SetupIds, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index >= 0)
            {
                if (index > 0 && source.Spawn.Turn < 1) return null;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == SetupIds[index] && play.RoomIndex == 0);
            }
            var eligible = DevoteModel.EligibleCards(context, out var error);
            if (eligible == null) throw new InvalidOperationException(error);
            if (eligible.Length == 0) return null;
            int selected = eligible[context.BattleRng.Range(0, eligible.Length).Value];
            if (!TreasureIds.Contains(selected)) throw new InvalidOperationException("An ordinary card entered the controlled Devote pool.");
            int floor = 0;
            if (context.FindCard(selected)!.DataId == UnitId && source.Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit => TreasureIds.Contains(unit.SpawnerCardId)) >= 3) floor = 3;
            return new PlayCardAction(0, floor, operation: DevoteModel.Operation);
        }
    }
}
