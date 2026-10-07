using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class CompanionBossScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            var groups = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).ToArray();
            CharacterData boss = groups.SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save,
                save.GetGeneratedBoss())).Distinct().Single(character => character.IsMiniboss());
            Set(boss, "isCompanionBoss", true); Set(boss, "loopsBetweenTrainFloors", true);
            Set(boss, "startingStatusEffects", boss.GetStartingStatusEffects().Where(status => status.statusId != "relentless").ToArray());
            var triggers = boss.GetTriggers().ToList();
            triggers.Add(Trigger(CharacterTriggerData.Trigger.PreCombat, 11, true));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnTrainRoomLoop, 17, true));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.PostAscension, 23, true));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnShift, 35, false));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnStatusEffectChanged, 45, true, true));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnStatusEffectChanged, 55, false, true));
            Set(boss, "triggers", triggers);
            // Move the same Boss's authored container to the second wave, leaving
            // every ordinary enemy at its original wave. This permits real looping
            // until the wave queue empties and a visible forced return to floor zero.
            object? bossContainer = null;
            foreach (SpawnGroupData group in groups)
            {
                var containers = Containers(group);
                foreach (var container in containers.ToArray())
                    if (container.Character == boss || container.UseBossCharacter) { containers.Remove(container); bossContainer = container; }
            }
            if (bossContainer == null) throw new InvalidOperationException("No ordinary native Boss container was found.");
            if (groups.Length != waves.Count) throw new InvalidOperationException("Companion fixture requires one group per ordinary wave.");
            Containers(groups[1]).Add((SpawnGroupData.CharacterDataContainer)bossContainer);
            Prepared = true;
            log.LogInfo("COMPANION-BOSS-PREPARED original Boss stats, early native spawn container, movement before wave exhaustion and ordered relentless trigger removal.");
        }
        private static CharacterTriggerData Trigger(CharacterTriggerData.Trigger kind, int gold, bool remove, bool requireRelentless = false)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(gold, false, true);
            Set(trigger, "trigger", kind); Set(trigger, "removeOnRelentlessChange", remove);
            if (requireRelentless) Set(trigger, "requiredStatusEffects", new List<StatusEffectStackData> {
                new StatusEffectStackData { statusId = "relentless", count = 1 } });
            return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static SpawnGroupData.CharacterDataContainerList Containers(SpawnGroupData group) =>
            (SpawnGroupData.CharacterDataContainerList)AccessTools.Field(typeof(SpawnGroupData), "characterDataContainerList").GetValue(group);
    }
}
