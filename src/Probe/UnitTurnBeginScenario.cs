using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitTurnBeginScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HealingScenario.Prepare(managers, log, withTriggers: true);
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            data.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Damage", "TurnCount", 1, 0));
            data.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Health", "AnyCharacter", 1, 1));
            data.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Health", "TurnCount", 1, 0));
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            Set(unit, "attackDamage", 0);
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger(false, false, Effect("PojuTurnTemporary", "fde091ee-b111-4db1-b5e1-b69f15a50601", 1, 1, UnitUpgradeLifetime.TemporaryUntilEndOfBattle)));
            triggers.Add(Trigger(true, false, Effect("PojuTurnOnce", "fde091ee-b111-4db1-b5e1-b69f15a50602", 2, 1, UnitUpgradeLifetime.Permanent)));
            triggers.Add(Trigger(true, true, Effect("PojuTurnIgnoredOnce", "fde091ee-b111-4db1-b5e1-b69f15a50603", 1, 1, UnitUpgradeLifetime.Permanent)));
            triggers.Add(Trigger(false, true, Effect("PojuTurnIgnoredRepeat", "fde091ee-b111-4db1-b5e1-b69f15a50604", 1, 1, UnitUpgradeLifetime.TemporaryUntilUnitDeath)));
            CharacterTriggerData gold = HealingScenario.HealGold(1, false, true);
            Set(gold, "trigger", CharacterTriggerData.Trigger.OnTurnBegin); triggers.Add(gold);
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards) card.Setup(data, save);
            var dazed = new CardUpgradeState(); dazed.Setup(); dazed.AddStatusEffectUpgradeStacks("dazed", 1);
            stewards[0].ApplyPermanentUpgrade(dazed, save, ignoreUpgradeAnimation: true);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var added = enemy.GetTriggers().ToList();
                CardEffectData effect = Effect("PojuTurnEnemy", "fde091ee-b111-4db1-b5e1-b69f15a50605", 1, 0, UnitUpgradeLifetime.TemporaryUntilUnitDeath, Team.Type.Heroes);
                added.Add(Trigger(false, false, effect)); Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("UNIT-TURN-BEGIN-PREPARED zero-attack growth, dazed/ignored and silenced/ignored phases, repeat/once source upgrades, enemy turns and natural waves/boss.");
        }
        private static CardEffectData Effect(string name, string id, int damage, int health, UnitUpgradeLifetime lifetime, Team.Type team = Team.Type.Monsters)
        {
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade(name, id, damage, health, 0, 0, "armor", 0);
            upgrade.GetStatusEffectUpgrades().Clear();
            var effect = new CardEffectData(lifetime == UnitUpgradeLifetime.Permanent ? "CardEffectAddCardUpgradeToUnits" : "CardEffectAddTempCardUpgradeToUnits", null!, team);
            effect.Cheat_SetTargetMode(TargetMode.Self); Set(effect, "paramCardUpgradeData", upgrade);
            Set(effect, "additionalParamInt1", (int)lifetime); return effect;
        }
        private static CharacterTriggerData Trigger(bool once, bool ignoreSilence, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignoreSilence);
            Set(trigger, "trigger", CharacterTriggerData.Trigger.OnTurnBegin); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
