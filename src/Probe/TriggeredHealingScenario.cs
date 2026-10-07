using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredHealingScenario
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
            triggers.Add(Trigger(false, true, Heal(TargetMode.Self, Team.Type.Heroes, 3),
                Heal(TargetMode.RoomHealTargets, Team.Type.Monsters, 0), Ranged(TargetMode.RandomInRoom, Team.Type.Monsters, -2, 5, .5f),
                Heal(TargetMode.Room, Team.Type.Monsters, 4), Ranged(TargetMode.Room, Team.Type.None, -2, 5, .5f),
                Ranged(TargetMode.Self, Team.Type.Monsters, 2, 2, -.5f),
                Heal(TargetMode.Self, Team.Type.Monsters, -1)));
            triggers.Add(Trigger(true, false, Heal(TargetMode.Self, Team.Type.Monsters, 1)));
            CardUpgradeData deferred = DynamicUpgradeScenario.Upgrade("PojuOnHealDeferredMaxHealth", "3de091ee-b111-4db1-b5e1-b69f15a50806", 0, 0, 0, 5, "armor", 0);
            deferred.GetStatusEffectUpgrades().Clear();
            var upgradeEffect = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters);
            upgradeEffect.Cheat_SetTargetMode(TargetMode.Self); Set(upgradeEffect, "paramCardUpgradeData", deferred);
            Set(upgradeEffect, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            CharacterTriggerData healed = HealingScenario.HealGold(0, true, true); Set(healed, "effects", new List<CardEffectData> { upgradeEffect });
            triggers.Add(healed);
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards)
            {
                card.Setup(data, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAdditionalHeal(9);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var added = enemy.GetTriggers().ToList();
                added.Add(Trigger(false, false, Heal(TargetMode.Room, Team.Type.Heroes, 1)));
                Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            log.LogInfo("TRIGGERED-HEALING-PREPARED self/team bypass, area/healable/random targets, group ranges, empty-room ranges, negative/zero heals, nested OnHeal, once/silence and source-card heal modifiers; natural waves/boss retained.");
        }
        private static CardEffectData Heal(TargetMode target, Team.Type team, int amount)
        {
            var effect = new CardEffectData("CardEffectHeal", null!, team); effect.Cheat_SetTargetMode(target);
            Set(effect, "paramInt", amount); return effect;
        }
        private static CardEffectData Ranged(TargetMode target, Team.Type team, int min, int max, float multiplier)
        {
            CardEffectData effect = Heal(target, team, 0); Set(effect, "useIntRange", true);
            Set(effect, "paramMinInt", min); Set(effect, "paramMaxInt", max); Set(effect, "paramMultiplier", multiplier); return effect;
        }
        private static CharacterTriggerData Trigger(bool once, bool ignored, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignored);
            Set(trigger, "trigger", CharacterTriggerData.Trigger.PreCombat); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
