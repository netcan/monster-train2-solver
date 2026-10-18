using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMovementSpellsScenario
    {
        internal const string Name = "original-movement-spells";
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
            CardData strong = UnityEngine.Object.Instantiate(steward); strong.name = "PojuMovementStrongAnchor";
            Set(strong, "id", "ac110050-0000-4000-8000-000000000001");
            CharacterData body = UnityEngine.Object.Instantiate(anchor); body.name = strong.name + "Unit"; Set(body, "attackDamage", 80); Set(body, "health", 600);
            var spawn = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
            Set(strong, "effects", new List<CardEffectData> { spawn }); Register(strong);
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuMovementEnergyControl"; Set(battery, "id", "ac110050-0000-4000-8000-000000000002");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 20);
            Set(battery, "effects", new List<CardEffectData> { energy }); Register(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(strong.GetID()), Add(steward.GetID()), Add(battery.GetID()),
                Add("77770369-492e-4e9e-8ce3-ba044862bb1b"), Add("7cb76384-d8e7-4b4e-a3eb-f9c5b844c941"),
                Add("b67ca7bf-bd65-4d98-ae53-26397de1e355"), Add("9170969d-ab4b-4de2-8d24-674a65aa2894"),
                Add("01f1fe24-f72b-4749-833b-46796463cdd7"), Add("d67c52be-d7bb-4291-a716-31408e4ced60"),
                Add("f21a04ea-2d1d-4f40-a36b-da4e9239b808"), Add("8b1c7ea7-a7f4-4e21-9a2a-01a255e15d67"), Add("07fd0035-5dde-40f1-bfed-7d9f1f89f5c8") };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MOVEMENT-SPELLS-PREPARED nine unchanged originals; one tracked enemy, three floors, unequal friendly attack and horizontal/vertical movement.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            var room = source.Spawn.Train.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack"));
            int target = index is >= 5 and <= 10 ? room!.Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id : 0;
            var action = new PlayCardAction(ids[index], index < 3 ? index : index is >= 5 and <= 11 ? room!.RoomIndex : 0,
                playerPosition: index < 3 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-MOVEMENT-SPELLS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
