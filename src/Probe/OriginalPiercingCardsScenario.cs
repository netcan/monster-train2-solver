using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPiercingCardsScenario
    {
        internal const string Name = "original-piercing-cards";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] {
                new StatusEffectStackData { statusId = "armor", count = 40 }, new StatusEffectStackData { statusId = "damage shield", count = 2 },
                new StatusEffectStackData { statusId = "spell shield", count = 2 }, new StatusEffectStackData { statusId = "titanskin", count = 2 } });
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData control = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell)); control.name = "PojuOrdinaryPiercingControl";
            Set(control, "id", "ac110014-0000-4000-8000-000000000001"); Set(control, "cost", 0); Set(control, "costType", CardData.CostType.Default);
            Set(control, "traits", new List<CardTraitData>()); Set(control, "startingUpgrades", new List<CardUpgradeData>());
            Set(control, "triggers", new List<CardTriggerEffectData>()); Set(control, "effectTriggers", new List<CharacterTriggerData>());
            Set(control, "targetless", false); Set(control, "targetsRoom", true);
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters); damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(damage, "paramInt", 8);
            Set(control, "effects", new List<CardEffectData> { damage });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(control);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(control.GetID()), Add("5a88d0e1-1c31-40f0-9e8d-8068dad832e9"),
                Add("f381b99b-518d-4818-b182-44d8f04110be"), Add("887b4b22-7f26-443f-b942-8ad620741617") }.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PIERCING-CARDS-PREPARED unchanged Horn Break, Branding Rite and Inferno; ordinary damage control and armor/shield/Titan Skin retention.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    if (index == 5 && source.Spawn.Turn < 1) return null;
                    int target = index is 2 or 3 or 4 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) &&
                        (target == 0 || play.TargetUnitId == target));
                }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
