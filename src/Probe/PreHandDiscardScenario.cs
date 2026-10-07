using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class PreHandDiscardScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            HealingScenario.Prepare(managers, log, withTriggers: true);
            SaveManager save = managers.GetSaveManager();
            // Exercise this phase during deployment with a deliberately distinct incoming
            // statistic. Both are native in-memory fixture inputs, captured in the root.
            ((List<CharacterTriggerData.Trigger>)save.GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers())
                .Remove(CharacterTriggerData.Trigger.EndTurnPreHandDiscard);
            managers.GetCardStatistics().SetEnergyRemainingEndOfTurn(7);
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            // The incoming statistic must remain unchanged until the later energy snapshot.
            data.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Damage", "EnergyRemainingEndOfTurn", 1, 0));
            data.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Health", "AnyDiscarded", 1, 0));
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger(false, false, Effect("PojuPreDiscardRepeat", "2de091ee-b111-4db1-b5e1-b69f15a50801", 1, 1)));
            triggers.Add(Trigger(true, true, Effect("PojuPreDiscardOnce", "2de091ee-b111-4db1-b5e1-b69f15a50802", 1, 1, permanent: true)));
            CardData junk = save.GetAllGameData().FindCardData("62cc5532-5fa0-4e66-9fec-85be573af359")!;
            CardPool pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = "PojuPreDiscardJunkPool";
            object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) }).Invoke(list, new object[] { junk });
            var generated = new CardEffectData("CardEffectAddBattleCard", null!, Team.Type.None);
            generated.Cheat_SetTargetMode(TargetMode.Room); Set(generated, "paramCardPool", pool);
            Set(generated, "paramInt", (int)CardPile.HandPile); Set(generated, "additionalParamInt", 1);
            CharacterTriggerData extra = HealingScenario.HealGold(1, false, true);
            Set(extra, "trigger", CharacterTriggerData.Trigger.EndTurnPreHandDiscard);
            var effects = extra.GetEffects().ToList(); effects.Add(generated);
            if (lethal) effects.Add(Effect("PojuPreDiscardFatal", "2de091ee-b111-4db1-b5e1-b69f15a50803", 0, -999));
            Set(extra, "effects", effects); triggers.Add(extra);
            CharacterTriggerData death = HealingScenario.HealGold(1, false, true);
            if (lethal)
            {
                var deathCard = new CardEffectData("CardEffectAddBattleCard", null!, Team.Type.None);
                deathCard.Cheat_SetTargetMode(TargetMode.Room); Set(deathCard, "paramCardPool", pool);
                Set(deathCard, "paramInt", (int)CardPile.HandPile); Set(deathCard, "additionalParamInt", 1);
                // Distinct native upgrades expose death generation order in the final state.
                CardUpgradeData marker = DynamicUpgradeScenario.Upgrade("PojuPreDiscardDeathCard", "2de091ee-b111-4db1-b5e1-b69f15a50805", 0, 1, 0, 0, "armor", 0);
                marker.GetStatusEffectUpgrades().Clear(); Set(deathCard, "paramCardUpgradeData", marker);
                Set(death, "effects", death.GetEffects().Concat(new[] { deathCard }).ToList());
            }
            Set(death, "trigger", CharacterTriggerData.Trigger.OnDeath); triggers.Add(death);
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
                added.Add(Trigger(false, false, Effect("PojuPreDiscardEnemy", "2de091ee-b111-4db1-b5e1-b69f15a50804", 0, 1, team: Team.Type.Heroes)));
                Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("PRE-HAND-DISCARD-PREPARED creation/team order, once/repeat, prior energy/discard statistics, hand generation and immediate discard, daze/silence, preview and deaths=" + lethal + "; natural waves/boss retained.");
        }
        private static CardEffectData Effect(string name, string id, int damage, int health, bool permanent = false, Team.Type team = Team.Type.Monsters)
        {
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade(name, id, damage, health, 0, 0, "armor", 0);
            upgrade.GetStatusEffectUpgrades().Clear();
            var effect = new CardEffectData(permanent ? "CardEffectAddCardUpgradeToUnits" : "CardEffectAddTempCardUpgradeToUnits", null!, team);
            effect.Cheat_SetTargetMode(TargetMode.Self); Set(effect, "paramCardUpgradeData", upgrade);
            Set(effect, "additionalParamInt1", (int)(permanent ? UnitUpgradeLifetime.Permanent : UnitUpgradeLifetime.TemporaryUntilUnitDeath)); return effect;
        }
        private static CharacterTriggerData Trigger(bool once, bool ignored, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignored);
            Set(trigger, "trigger", CharacterTriggerData.Trigger.EndTurnPreHandDiscard); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
