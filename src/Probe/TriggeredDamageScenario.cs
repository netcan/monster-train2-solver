using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredDamageScenario
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
            CardEffectData stacked = Damage(TargetMode.Self, Team.Type.Heroes, 1);
            Set(stacked, "useStatusEffectStackMultiplier", true); Set(stacked, "statusEffectStackMultiplier", "regen");
            triggers.Add(Trigger("PreCombat", false, true, Damage(TargetMode.Self, Team.Type.Heroes, 1), stacked,
                Damage(TargetMode.Room, Team.Type.Monsters | Team.Type.Heroes, 1),
                Ranged(TargetMode.RandomInRoom, Team.Type.Monsters, -2, 5, .5f),
                Ranged(TargetMode.Room, Team.Type.None, -2, 5, .5f)));
            triggers.Add(Trigger("PreCombat", true, true, Ranged(TargetMode.Self, Team.Type.Monsters, 2, 2, -.5f)));
            triggers.Add(Trigger("PreCombat", true, true, Ranged(TargetMode.Room, Team.Type.None, 0, 0, 1)));
            triggers.Add(Trigger("OnDeath", false, true, Damage(TargetMode.Room, Team.Type.Heroes, 1), Damage(TargetMode.Self, Team.Type.Monsters, 999)));
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards)
            {
                card.Setup(data, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAttackDamage(9);
                upgrade.AddStatusEffectUpgradeStacks("armor", 2); upgrade.AddStatusEffectUpgradeStacks("damage shield", 1);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var added = enemy.GetTriggers().ToList();
                added.Add(Trigger("PreCombat", false, false, Damage(TargetMode.Room, Team.Type.Heroes, 0)));
                added.Add(Trigger("OnDeath", false, false, Damage(TargetMode.Room, Team.Type.Monsters, 1)));
                Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            log.LogInfo("TRIGGERED-DAMAGE-PREPARED three quantity samples, empty/random/area/self targets, status multiplier, armor/shield, death FIFO, source-card numeric offsets; natural waves/boss retained.");
        }
        private static CardEffectData Damage(TargetMode target, Team.Type team, int amount)
        {
            var effect = new CardEffectData("CardEffectDamage", null!, team); effect.Cheat_SetTargetMode(target);
            Set(effect, "paramInt", amount); return effect;
        }
        private static CardEffectData Ranged(TargetMode target, Team.Type team, int min, int max, float multiplier)
        {
            CardEffectData effect = Damage(target, team, 0); Set(effect, "useIntRange", true);
            Set(effect, "paramMinInt", min); Set(effect, "paramMaxInt", max); Set(effect, "paramMultiplier", multiplier); return effect;
        }
        private static CharacterTriggerData Trigger(string kind, bool once, bool ignored, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignored);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
