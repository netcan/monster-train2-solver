using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class HitKillScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            PreCombatScenario.Prepare(managers, log, lethal: false);
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger("PreCombat", 0, false, true, 0, Effect("CardEffectDamage", TargetMode.Self, Team.Type.Monsters, 1)));
            triggers.Add(Trigger("OnHit", 5, false, false));
            triggers.Add(Trigger("OnHit", 10, true, true));
            triggers.Add(Trigger("OnHit", 15, false, true, 2));
            triggers.Add(Trigger("OnHit", 20, false, true, -1));
            triggers.Add(Trigger("OnKill", 25, false, false));
            triggers.Add(Trigger("OnKill", 30, true, true, 0, Effect("CardEffectHeal", TargetMode.Self, Team.Type.Monsters, 1)));
            triggers.Add(Trigger("OnKill", 35, false, true, 1));
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards)
            {
                card.Setup(data, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup();
                upgrade.AddStatusEffectUpgradeStacks("sweep", 1);
                upgrade.AddStatusEffectUpgradeStacks("armor", 3);
                upgrade.AddStatusEffectUpgradeStacks("damage shield", 1);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var added = enemy.GetTriggers().ToList();
                added.Add(Trigger("OnHit", 5, false, false, 0, Effect("CardEffectHeal", TargetMode.Room, Team.Type.Heroes, 2)));
                added.Add(Trigger("OnHit", 10, true, true));
                added.Add(Trigger("OnKill", 20, false, false, 0, Effect("CardEffectHeal", TargetMode.Self, Team.Type.Heroes, 1)));
                Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            log.LogInfo("HIT-KILL-PREPARED shield/armor/HP thresholds, repeat/once/silence, lethal Revenge and boss suppression, sweep callbacks and healing; natural waves/boss retained.");
        }
        private static CardEffectData Effect(string type, TargetMode target, Team.Type team, int value)
        {
            var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value); return effect;
        }
        private static CharacterTriggerData Trigger(string kind, int gold, bool once, bool ignored, int threshold = 0, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(gold, once, ignored);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
            Set(trigger, "triggerAtThreshold", threshold);
            Set(trigger, "effects", trigger.GetEffects().Concat(effects).ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
