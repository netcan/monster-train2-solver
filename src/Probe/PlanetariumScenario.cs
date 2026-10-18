using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class PlanetariumScenario
    {
        internal const string Name = "planetarium";
        internal const string DataId = "b895b6ef-6d0c-4474-a12c-1229fb7749c3";
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
            CardData spell = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell)); spell.name = "PojuPlanetariumDamage";
            Set(spell, "id", "ac110013-0000-4000-8000-000000000001"); Set(spell, "cost", 0); Set(spell, "costType", CardData.CostType.Default);
            Set(spell, "traits", new List<CardTraitData>()); Set(spell, "startingUpgrades", new List<CardUpgradeData>());
            Set(spell, "triggers", new List<CardTriggerEffectData>()); Set(spell, "effectTriggers", new List<CharacterTriggerData>());
            Set(spell, "targetless", false); Set(spell, "targetsRoom", true);
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters); damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(damage, "paramInt", 5);
            Set(spell, "effects", new List<CardEffectData> { damage });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(spell);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(DataId), Add(spell.GetID()), Add(spell.GetID()),
                Add(steward.GetID()), Add(OriginalRoomCardsScenario.FightClubId), Add(spell.GetID()), Add(DataId), Add(spell.GetID()) }.Select(FullBattleTrace.Active!.CardId).ToArray();
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("PLANETARIUM-PREPARED two original rooms; existing/later Conduit contributors, inside/outside damage, replacement and restored spell tracking.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                if (source.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") == 0)
                {
                    int room = index is 1 or 4 ? 1 : 0;
                    int target = index is 3 or 4 or 7 or 9 ? source.Spawn.Train.Rooms.SelectMany(item => item.Units).Single(unit => unit.SpawnerCardId == ids[room]).Id : 0;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room &&
                        (target == 0 || play.TargetUnitId == target));
                }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
