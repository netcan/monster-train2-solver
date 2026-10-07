using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class PostCombatHealingScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            TriggeredHealingScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = unit.GetTriggers().ToList();
            CardUpgradeData healUpgrade = DynamicUpgradeScenario.Upgrade("PojuHealingBeforeHeal", "3de091ee-b111-4db1-b5e1-b69f15a50809", 2, 0, 0, 0, "armor", 0);
            healUpgrade.GetStatusEffectUpgrades().Clear();
            var beforeHeal = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters); beforeHeal.Cheat_SetTargetMode(TargetMode.Self);
            Set(beforeHeal, "paramCardUpgradeData", healUpgrade); Set(beforeHeal, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            triggers.Add(Trigger(CharacterTriggerData.Trigger.PostCombatHealing, false, false, beforeHeal, Heal(TargetMode.Self, Team.Type.Monsters, 3)));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.PostCombatHealing, true, true, Heal(TargetMode.Self, Team.Type.Monsters, 0)));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.PostCombatHealing, false, true,
                Heal(TargetMode.Room, Team.Type.Heroes | Team.Type.Monsters, 4)));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.PostCombat, false, false, Upgrade("PojuOrdinaryPostHealth", "3de091ee-b111-4db1-b5e1-b69f15a50807", Team.Type.Monsters)));
            CharacterTriggerData gold = HealingScenario.HealGold(1, false, true); Set(gold, "trigger", CharacterTriggerData.Trigger.PostCombat); triggers.Add(gold);
            Set(unit, "triggers", triggers); foreach (CardState card in stewards) card.Setup(data, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var added = enemy.GetTriggers().ToList();
                added.Add(Trigger(CharacterTriggerData.Trigger.PostCombatHealing, false, false, Heal(TargetMode.Self, Team.Type.Heroes, 1)));
                added.Add(Trigger(CharacterTriggerData.Trigger.PostCombat, false, false, Upgrade("PojuEnemyPostHealth", "3de091ee-b111-4db1-b5e1-b69f15a50808", Team.Type.Heroes)));
                Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            log.LogInfo("POST-COMBAT-HEALING-PREPARED per-actor healing before ordinary triggers, enemy/player order, unhealed max HP, daze prevention surviving status clearing, silence/ignored exception, once/repeated and natural relentless boss.");
        }
        private static CardEffectData Heal(TargetMode target, Team.Type team, int amount)
        { var effect = new CardEffectData("CardEffectHeal", null!, team); effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", amount); return effect; }
        private static CardEffectData Upgrade(string name, string id, Team.Type team)
        {
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade(name, id, 0, 0, 0, 3, "armor", 0); upgrade.GetStatusEffectUpgrades().Clear();
            var effect = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, team); effect.Cheat_SetTargetMode(TargetMode.Self);
            Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath); return effect;
        }
        private static CharacterTriggerData Trigger(CharacterTriggerData.Trigger kind, bool once, bool ignored, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignored); Set(trigger, "trigger", kind);
            Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
