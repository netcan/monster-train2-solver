using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TeamTurnBeginScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            UnitTurnBeginScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger(false, false, Effect("PojuTeamRoom", "1de091ee-b111-4db1-b5e1-b69f15a50701", 1, 1, TargetMode.Room, Team.Type.Monsters)));
            triggers.Add(Trigger(true, true, Effect("PojuTeamIgnoredOnce", "1de091ee-b111-4db1-b5e1-b69f15a50702", 1, 1, TargetMode.Self, Team.Type.Monsters)));
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards) card.Setup(data, save);
            var ambush = new CardUpgradeState(); ambush.Setup(); ambush.AddStatusEffectUpgradeStacks("ambush", 1);
            stewards[0].ApplyPermanentUpgrade(ambush, save, ignoreUpgradeAnimation: true);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var added = enemy.GetTriggers().ToList();
                added.Add(Trigger(false, false, Effect("PojuTeamEnemyRoom", "1de091ee-b111-4db1-b5e1-b69f15a50703", 0, 1, TargetMode.Room, Team.Type.Heroes)));
                Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("TEAM-TURN-BEGIN-PREPARED whole-team room buffs, ignored-silence once, source scaling/lifetimes, ambush before enemy phase, dazed/silenced units and natural waves/boss.");
        }
        private static CardEffectData Effect(string name, string id, int damage, int health, TargetMode target, Team.Type team)
        {
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade(name, id, damage, health, 0, 0, "armor", 0);
            upgrade.GetStatusEffectUpgrades().Clear();
            var effect = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, team);
            effect.Cheat_SetTargetMode(target); Set(effect, "paramCardUpgradeData", upgrade);
            Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath); return effect;
        }
        private static CharacterTriggerData Trigger(bool once, bool ignored, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignored);
            Set(trigger, "trigger", CharacterTriggerData.Trigger.OnTeamTurnBegin); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
