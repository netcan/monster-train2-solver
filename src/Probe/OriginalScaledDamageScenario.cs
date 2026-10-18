using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalScaledDamageScenario
    {
        internal const string Name = "original-scaled-damage";
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
            CardData Control(int suffix, string effectName, int value, Team.Type team)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuScaledDamageControl" + suffix; Set(data, "id", "ac110049-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value);
                if (effectName == "CardEffectAdjustCorruption") Set(effect, "paramBool", true);
                Set(data, "effects", new List<CardEffectData> { effect }); Register(data); return data;
            }
            CardData Body(int suffix, int health, int armor)
            {
                CardData data = UnityEngine.Object.Instantiate(steward); data.name = "PojuScaledDamageBody" + suffix;
                Set(data, "id", "ac110049-0000-4000-8000-00000000000" + suffix);
                CharacterData body = UnityEngine.Object.Instantiate(anchor); body.name = data.name + "Unit";
                Set(body, "attackDamage", 0); Set(body, "health", health);
                Set(body, "startingStatusEffects", armor == 0 ? Array.Empty<StatusEffectStackData>() : new[] { new StatusEffectStackData { statusId = "armor", count = armor } });
                var effect = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); effect.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
                Set(data, "effects", new List<CardEffectData> { effect }); Register(data); return data;
            }
            CardData battery = Control(1, "CardEffectGainEnergy", 20, Team.Type.None), health = Control(2, "CardEffectBuffMaxHealth", 500, Team.Type.Heroes),
                support = Body(3, 80, 5), doomed = Body(4, 1, 0), corruption = Control(5, "CardEffectAdjustCorruption", 3, Team.Type.None),
                wound = Control(6, "CardEffectDamage", 1, Team.Type.Monsters);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(support.GetID()), Add(doomed.GetID()), Add(doomed.GetID()),
                Add(battery.GetID()), Add(health.GetID()), Add(corruption.GetID()), Add("97d18bbe-6fa9-45a4-90a9-5a1d0ea24ca3"),
                Add("f6434819-3b62-44e4-a82c-e344dc4c1aa0"), Add("e8e593f4-7e2d-4791-a7d4-8a6155f1a50f"),
                Add("f2bed1d3-6b52-43d5-8b97-bfe5c90dce53"), Add("acc7bdd1-8f5b-4738-8c4a-4542792e7388"), Add(wound.GetID()), Add("97d18bbe-6fa9-45a4-90a9-5a1d0ea24ca3") };
            foreach (CardState card in created.Skip(6)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SCALED-DAMAGE-PREPARED five unchanged originals; armor-inclusive HP, front caster attack, corruption, deck spell type and zero/two allied deaths.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 6 && source.Spawn.Turn < 1) return null;
            int target = index == 10 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[3]).Id : 0;
            var action = new PlayCardAction(ids[index], index is 1 or 2 ? index : 0,
                playerPosition: index < 3 ? 0 : index < 6 ? index - 2 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-SCALED-DAMAGE-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
