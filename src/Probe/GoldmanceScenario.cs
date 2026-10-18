using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class GoldmanceScenario
    {
        internal const string Name = "goldmance";
        internal const string AbilityId = "ac258de8-8765-4856-87c2-03868ebbf27e";
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
            CardData hoard = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            hoard.name = "PojuGoldmanceHoard"; Set(hoard, "id", "ac110011-0000-4000-8000-000000000001"); Set(hoard, "cost", 0);
            Set(hoard, "costType", CardData.CostType.Default); Set(hoard, "traits", new List<CardTraitData>());
            Set(hoard, "startingUpgrades", new List<CardUpgradeData>()); Set(hoard, "triggers", new List<CardTriggerEffectData>());
            Set(hoard, "effectTriggers", new List<CharacterTriggerData>()); Set(hoard, "targetless", false); Set(hoard, "targetsRoom", true);
            var effect = new CardEffectData("CardEffectAdjustDragonsHoard", null!, Team.Type.None); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", 3);
            Set(hoard, "effects", new List<CardEffectData> { effect });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(hoard);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add("5fcb0af2-7bab-4998-94c9-e74a828fff01"),
                Add("5fcb0af2-7bab-4998-94c9-e74a828fff01"), Add(hoard.GetID()) }.Select(FullBattleTrace.Active!.CardId).ToArray();
            save.SetDragonsHoardAmount(0); Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("GOLDMANCE-PREPARED two original Goldy Whelps and original shared ability; zero/three Hoard rewards with same-floor protectors.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            for (int index = 0; index < 4; index++)
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                    return plays.FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index % 2) && play.PlayerPosition == (index < 2 ? 0 : 1));
            CombatUnit[] actors = ids.Skip(2).Take(2).Select(id => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.SpawnerCardId == id)).ToArray();
            int used = actors.Count(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnOwnAbilityActivated" && trigger.HasTriggered));
            if (used == 1 && context.Statistics!.Value(ids[4], "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == ids[4] && play.RoomIndex == 0);
            if (used >= 2) return null;
            CombatUnit actor = actors[used];
            return plays.FirstOrDefault(play => play.ActivatorUnitId == actor.Id);
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
