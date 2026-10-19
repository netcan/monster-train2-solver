using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCardDrawPotionScenario
    {
        internal const string Name = "original-card-draw-potion";
        internal const string CardId = "84633260-7209-4515-9582-82605ee2a36a";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardData blank = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            blank.name = "PojuCardDrawPotionBlank";
            Set(blank, "id", "ac110060-0000-4000-8000-000000000001");
            Set(blank, "cost", 0); Set(blank, "costType", CardData.CostType.Default);
            Set(blank, "targetless", true); Set(blank, "targetsRoom", true);
            Set(blank, "traits", new List<CardTraitData>()); Set(blank, "startingUpgrades", new List<CardUpgradeData>());
            Set(blank, "triggers", new List<CardTriggerEffectData>()); Set(blank, "effectTriggers", new List<CharacterTriggerData>());
            var empty = new CardEffectData("CardEffectNULL", null!, Team.Type.None);
            empty.Cheat_SetTargetMode(TargetMode.Room); Set(empty, "paramBool2", true);
            Set(blank, "effects", new List<CardEffectData> { empty });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(blank);

            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId) };
            var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze");
            created[2].AddTemporaryTrait(freeze, managers.GetCoreManagers());
            for (int index = 0; index < 8; index++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-CARD-DRAW-POTION-PREPARED unchanged 1-cost Card Draw Potion, +3 next-turn draw and eight-card native draw control.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0) return null;
            int roomIndex = index == 1 ? 1 : 0;
            return BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == roomIndex);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
