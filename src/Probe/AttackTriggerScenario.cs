using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class AttackTriggerScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            DyingUpgradeScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            CardUpgradeData before = Upgrade("PojuAttackBefore", "5aba1420-c01a-4b0d-8bc2-92364db50001", 1, 0, 1);
            CardUpgradeData once = Upgrade("PojuAttackBeforeOnce", "5aba1420-c01a-4b0d-8bc2-92364db50002", 1, 0, 1);
            CardUpgradeData after = Upgrade("PojuAttackAfterVictim", "5aba1420-c01a-4b0d-8bc2-92364db50003", 0, 1, 1);
            var added = unit.GetTriggers().ToList();
            added.Add(Trigger("OnAttackingBeforeDamage", 10, true, true, 0, Up(once, TargetMode.Self, Team.Type.Monsters)));
            added.Add(Trigger("OnAttackingBeforeDamage", 5, false, true, 0, Up(before, TargetMode.Self, Team.Type.Monsters)));
            added.Add(Trigger("OnAttacking", 5, false, false, 0, Action("CardEffectDamage", Team.Type.Heroes, 1)));
            // The native override targets the enemy despite the authored friendly team.
            added.Add(Trigger("OnAttacking", 10, false, true, 0, Up(after, TargetMode.LastAttackedCharacter, Team.Type.Monsters)));
            added.Add(Trigger("OnAttacking", 99, false, true, 1));
            Set(unit, "triggers", added); foreach (CardState card in stewards) card.Setup(data, save);
            CardUpgradeData enemyBefore = Upgrade("PojuEnemyAttackBefore", "5aba1420-c01a-4b0d-8bc2-92364db50004", 0, 0, 2);
            CardUpgradeData enemyAfter = Upgrade("PojuEnemyAttackAfter", "5aba1420-c01a-4b0d-8bc2-92364db50005", 0, 1, 1);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var triggers = enemy.GetTriggers().ToList();
                triggers.Add(Trigger("OnAttackingBeforeDamage", 5, true, true, 0));
                triggers.Add(Trigger("OnAttackingBeforeDamage", 5, false, true, 0,
                    Up(enemyBefore, TargetMode.LastAttackedCharacter, Team.Type.Monsters), Action("CardEffectHeal", Team.Type.Monsters, 2)));
                triggers.Add(Trigger("OnAttacking", 10, false, true, 0,
                    Action("CardEffectDamage", Team.Type.Monsters, 1), Up(enemyAfter, TargetMode.LastAttackedCharacter, Team.Type.Monsters)));
                Set(enemy, "triggers", triggers);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            log.LogInfo("ATTACK-TRIGGER-PREPARED staged damage/HP, pre/post upgrades/healing, victim overrides/history, sweep queues, dying callbacks, once/silence/zero thresholds and nested Default damage; natural waves/boss retained.");
        }
        private static CardUpgradeData Upgrade(string name, string id, int damage, int health, int armor) =>
            DynamicUpgradeScenario.Upgrade(name, id, damage, health, 0, 0, "armor", armor);
        private static CardEffectData Up(CardUpgradeData upgrade, TargetMode mode, Team.Type team)
        {
            var effect = new CardEffectData("CardEffectAddCardUpgradeToUnits", null!, team); effect.Cheat_SetTargetMode(mode);
            Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath); return effect;
        }
        private static CardEffectData Action(string type, Team.Type team, int amount)
        {
            var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(TargetMode.LastAttackedCharacter); Set(effect, "paramInt", amount); return effect;
        }
        private static CharacterTriggerData Trigger(string kind, int gold, bool once, bool ignored, int threshold, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(gold, once, ignored);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); Set(trigger, "triggerAtThreshold", threshold);
            Set(trigger, "effects", trigger.GetEffects().Concat(effects).ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
