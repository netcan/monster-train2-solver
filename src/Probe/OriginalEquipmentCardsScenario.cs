using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEquipmentCardsScenario
    {
        internal const string Name = "original-equipment-cards";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 30);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            void Register(CardData data) => ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data);
            CardData Host(int suffix, int attack, int size)
            {
                CardData data = UnityEngine.Object.Instantiate(steward); data.name = "PojuEquipmentHost" + suffix;
                Set(data, "id", "ac110053-0000-4000-8000-00000000000" + suffix);
                CharacterData body = UnityEngine.Object.Instantiate(anchor); body.name = data.name + "Unit";
                Set(body, "attackDamage", attack); Set(body, "size", size); Set(body, "equipmentLimit", 4);
                var spawn = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
                Set(data, "effects", new List<CardEffectData> { spawn }); Register(data); return data;
            }
            CardData Control(int suffix, string effectName, int value, Team.Type team)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuEquipmentControl" + suffix; Set(data, "id", "ac110053-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value);
                Set(data, "effects", new List<CardEffectData> { effect }); Register(data); return data;
            }
            CardData primary = Host(1, 15, 1), secondary = Host(2, 5, 3), third = Host(3, 15, 1),
                battery = Control(4, "CardEffectGainEnergy", 20, Team.Type.None), wound = Control(5, "CardEffectDamage", 30, Team.Type.Monsters),
                health = Control(6, "CardEffectBuffMaxHealth", 50, Team.Type.Heroes);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(primary.GetID()), Add(secondary.GetID()), Add(third.GetID()), Add(steward.GetID()), Add(battery.GetID()), Add(wound.GetID()), Add(health.GetID()),
                Add("217d467c-77c2-481d-b78f-57aaf3dcd4a6"), Add("567f37b4-9f93-4e55-9fcb-315b870627c3"), Add("7725a001-cc6d-4aef-89cf-19480b16c015"),
                Add("99fed8d9-e235-4109-860f-04a0293ba821"), Add("e625aaa1-911c-43f3-8a0d-963e6a9a07bc"), Add("f1c2a098-e20d-4587-b8de-5bef315de42d"),
                Add("1dd23b59-fa5e-44bc-83ca-de119ede4f21"), Add("66cbbd14-f5f3-44a1-bd1c-ce98f3c13584"), Add("87c155c4-55e4-4434-a99d-1dda85d5784c"),
                Add("e4ba411f-09b2-4a87-91f6-5f00cd742ba6"), Add("7369cb8e-f8cf-44be-8667-c699c630c063"), Add("1f352534-0895-488e-9345-481bc447a089"),
                Add("75cfe90b-47b9-406c-b891-33564aac6677") };
            foreach (CardState card in created.Skip(4)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-EQUIPMENT-CARDS-PREPARED thirteen unchanged originals; three four-slot hosts plus the separate-floor Pyre Steward for Ambient Charge, wounded units and surviving attack targets.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 4 && source.Spawn.Turn < 1) return null;
            int host = index is >= 7 and <= 10 ? 1 : index is 11 or 12 or 17 or 18 ? 2 : index == 19 ? 3 : 0;
            int target = index >= 7 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[host]).Id : 0;
            int room = index == 3 || index == 19 ? 1 : 0;
            int position = index == 3 ? 0 : index < 4 ? index : -1;
            var action = new PlayCardAction(ids[index], room, playerPosition: position, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-EQUIPMENT-CARDS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
