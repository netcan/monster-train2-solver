using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpawnStatusRelicScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HealingScenario.Prepare(managers, log, withTriggers: true);
            SaveManager save = managers.GetSaveManager();
            CardState[] cards = managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card =>
                card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(cards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var armor = HealingScenario.HealGold(11, false, true);
            AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(armor, CharacterTriggerData.Trigger.OnArmorAdded);
            AccessTools.Field(typeof(CharacterData), "triggers").SetValue(unit, unit.GetTriggers().Concat(new[] { armor }).ToList());
            foreach (CardState card in cards) card.Setup(data, save);
            Acquire("68ef2523-5c2e-4660-b96d-00b1c0485f54", "SpawnWithArmor");
            Acquire("60a2a8a3-5f7a-4a9d-b427-5f261145fa1f", "FirstUnitGainDamageShield");
            Acquire("270356af-16bd-4433-a0b2-3e5bb94ef890", "FrostbiteOnEnemies");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("SPAWN-STATUS-RELICS-PREPARED original armor, first-unit shield and enemy frostbite acquired without definition changes; native Boss/waves retained.");

            void Acquire(string id, string key)
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (relic.name != key || relic.GetEffects().Count != 1 || relic.GetEffects()[0].GetEffectClassName() != "RelicEffectAddStatusEffectOnSpawn")
                    throw new InvalidOperationException("Original spawn status relic changed: " + key);
                save.AddRelic(relic);
            }
        }
    }
}
