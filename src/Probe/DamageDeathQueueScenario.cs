using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageDeathQueueScenario
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
            triggers.Insert(0, Trigger("PreCombat", false, true, Effect("CardEffectDamage", TargetMode.Room, Team.Type.Heroes, 1),
                Effect("CardEffectHeal", TargetMode.Self, Team.Type.Heroes, 0)));
            CardUpgradeData armor = DynamicUpgradeScenario.Upgrade("PojuOnHealBeforeDeathArmor", "0ba50e12-dd92-4e83-b03b-7f0943600001", 0, 0, 0, 0, "armor", 7);
            var upgrade = Effect("CardEffectAddTempCardUpgradeToUnits", TargetMode.Self, Team.Type.Monsters, 0);
            Set(upgrade, "paramCardUpgradeData", armor); Set(upgrade, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            triggers.Add(Trigger("OnHeal", true, true, upgrade)); Set(unit, "triggers", triggers);
            foreach (CardState card in stewards) card.Setup(data, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                // Keep terminal kill-camera settlement separate from this queue-order fixture.
                if (enemy.IsOuterTrainBoss() || enemy.IsMiniboss()) continue;
                var added = enemy.GetTriggers().ToList();
                added.Add(Trigger("OnDeath", false, false, Effect("CardEffectDamage", TargetMode.Room, Team.Type.Monsters, 3)));
                Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            log.LogInfo("DAMAGE-DEATH-QUEUE-PREPARED room damage kills before zero healing; queued OnHeal grants armor before delayed enemy OnDeath damage; natural waves/boss retained.");
        }
        private static CardEffectData Effect(string type, TargetMode target, Team.Type team, int value)
        {
            var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value); return effect;
        }
        private static CharacterTriggerData Trigger(string kind, bool once, bool ignored, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignored);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
