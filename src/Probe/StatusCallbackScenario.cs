using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class StatusCallbackScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Gold("OnStatusEffectChanged", 1, false, true, 3));
            triggers.Add(Gold("OnArmorAdded", 2, true, true, 0));
            triggers.Add(Gold("OnPyregelAdded", 3, false, true, 0));
            triggers.Add(Gold("OnValiant", 4, false, true, 2));
            triggers.Add(Gold("OnSilence", 5, false, true, 0));
            triggers.Add(Gold("OnSilenceLost", 6, false, false, 0));
            triggers.Add(Gold("OnNewStatusEffectAdded", 7, false, true, 3));
            triggers.Add(Gold("OnStatusEffectChanged", 2, true, false, 1));
            // Saturation followed by removal also clears the authored silence on this unit.
            var temporarySilence = DynamicUpgradeScenario.Upgrade("PojuCallbackSilence", "9257a7de-a153-4a2b-8821-c001baac0001", 0, 0, 0, 0, "silenced", 9999);
            CharacterTriggerData cleanse = Gold("PreCombat", 0, true, true, 0);
            AccessTools.Field(typeof(CharacterTriggerData), "effects").SetValue(cleanse, new List<CardEffectData>
            {
                Upgrade(temporarySilence, false), Upgrade(temporarySilence, true),
                Status("pyregel", 1)
            });
            triggers.Add(cleanse);
            AccessTools.Field(typeof(CharacterData), "triggers").SetValue(unit, triggers);
            foreach (CardState card in stewards) card.Setup(data, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var enemyTriggers = enemy.GetTriggers().ToList();
                enemyTriggers.Add(Gold("OnStatusEffectChanged", 1, true, true, 3));
                enemyTriggers.Add(Gold("OnArmorAdded", 2, true, true, 0));
                enemyTriggers.Add(Gold("OnNewStatusEffectAdded", 1, true, true, 1));
                AccessTools.Field(typeof(CharacterData), "triggers").SetValue(enemy, enemyTriggers);
            }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            if (Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACK_ACTIONS") == "1") StatusCallbackActionScenario.Prepare(managers, log);
            log.LogInfo("STATUS-CALLBACKS-PREPARED exact native queue payloads, thresholds, once flags and status dictionary presence.");
        }
        private static CardEffectData Upgrade(CardUpgradeData data, bool remove)
        {
            var effect = new CardEffectData(remove ? "CardEffectRemoveTempUpgradeFromUnit" : "CardEffectAddCardUpgradeToUnits", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Self);
            AccessTools.Field(typeof(CardEffectData), "paramCardUpgradeData").SetValue(effect, data);
            AccessTools.Field(typeof(CardEffectData), "additionalParamInt1").SetValue(effect, (int)UnitUpgradeLifetime.TemporaryUntilEndOfBattle);
            return effect;
        }
        private static CardEffectData Status(string id, int count)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Self);
            AccessTools.Field(typeof(CardEffectData), "paramStatusEffects").SetValue(effect, new[] { new StatusEffectStackData { statusId = id, count = count } });
            return effect;
        }
        private static CharacterTriggerData Gold(string kind, int value, bool once, bool ignoreSilence, int threshold)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(value, once, ignoreSilence);
            AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(trigger, Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
            AccessTools.Field(typeof(CharacterTriggerData), "triggerAtThreshold").SetValue(trigger, threshold);
            return trigger;
        }
    }
}
