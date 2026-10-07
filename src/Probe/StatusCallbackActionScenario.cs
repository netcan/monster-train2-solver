using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class StatusCallbackActionScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade("PojuStatusCallbackUpgrade", "6daaac75-57bc-4c02-9ca5-c011baac0001", 1, 1, 0, 0, "armor", 1);
            var pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = "PojuStatusCallbackClonePool";
            object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) }).Invoke(list, new object[] { data });
            var clone = Action("CardEffectAddBattleCard", TargetMode.Self, Team.Type.Monsters, (int)CardPile.DiscardPile);
            Set(clone, "paramCardPool", pool); Set(clone, "additionalParamInt", 1); Set(clone, "copyModifiersFromSource", true);
            var triggers = unit.GetTriggers().ToList();
            // Reserve the first pyregel addition for a played upgrade. Its callback must
            // copy the source card before the native upgrade is written back to that card.
            CharacterTriggerData cleanse = triggers.Last(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.PreCombat);
            Set(cleanse, "effects", cleanse.GetEffects().Where(effect => effect.GetEffectStateName() != "CardEffectAddStatusEffect").ToList());
            CardUpgradeData zeroArmor = DynamicUpgradeScenario.Upgrade("PojuCallbackZeroArmor", "6daaac75-57bc-4c02-9ca5-c011baac0003", 0, 0, 0, 0, "armor", 0);
            triggers.Add(Trigger("OnPyregelAdded", clone, Upgrade(zeroArmor, TargetMode.Self, Team.Type.Monsters)));
            triggers.Add(Trigger("OnArmorAdded", Action("CardEffectDamage", TargetMode.Self, Team.Type.Monsters, 1),
                Action("CardEffectHeal", TargetMode.Self, Team.Type.Monsters, 1), Status(TargetMode.Self, Team.Type.Monsters, "regen", 1),
                Upgrade(upgrade, TargetMode.Self, Team.Type.Monsters), clone));
            triggers.Add(Trigger("OnNewStatusEffectAdded", Status(TargetMode.Self, Team.Type.Monsters, "armor", 1),
                Action("CardEffectHeal", TargetMode.Room, Team.Type.Monsters, 1)));
            triggers.Add(Trigger("OnSilenceLost", Upgrade(upgrade, TargetMode.Self, Team.Type.Monsters)));
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards) card.Setup(data, save);
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card =>
                card.GetCardType() == CardType.Spell && card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardData spell = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            CardUpgradeData boundary = DynamicUpgradeScenario.Upgrade("PojuCallbackWritebackBoundary", "6daaac75-57bc-4c02-9ca5-c011baac0002", 0, 0, 0, 0, "pyregel", 1);
            spell.GetEffects().Insert(0, Upgrade(boundary, TargetMode.DropTargetCharacter, Team.Type.Monsters));
            foreach (CardState card in spells) card.Setup(spell, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var enemyTriggers = enemy.GetTriggers().ToList();
                CharacterTriggerData sameArmor = HealingScenario.HealGold(1, false, true);
                Set(sameArmor, "trigger", CharacterTriggerData.Trigger.OnStatusEffectChanged); Set(sameArmor, "triggerAtThreshold", 3);
                enemyTriggers.Add(sameArmor);
                enemyTriggers.Add(Trigger("OnArmorAdded", Status(TargetMode.Self, Team.Type.Heroes, "regen", 1),
                    Status(TargetMode.Self, Team.Type.Heroes, "armor", 3), Status(TargetMode.Self, Team.Type.Heroes, "armor", 0),
                    Action("CardEffectHeal", TargetMode.Room, Team.Type.Heroes, 1),
                    Action("CardEffectDamage", TargetMode.Room, Team.Type.Monsters, 1)));
                Set(enemy, "triggers", enemyTriggers);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("STATUS-CALLBACK-ACTIONS-PREPARED nested status/damage/heal/upgrade effects, multi-target effects, source clones and deferred death queues.");
        }
        private static CharacterTriggerData Trigger(string kind, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, true, true);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static CardEffectData Action(string type, TargetMode mode, Team.Type team, int value)
        {
            var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(mode); Set(effect, "paramInt", value); return effect;
        }
        private static CardEffectData Status(TargetMode mode, Team.Type team, string id, int count)
        {
            var effect = Action("CardEffectAddStatusEffect", mode, team, 0);
            Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = id, count = count } }); return effect;
        }
        private static CardEffectData Upgrade(CardUpgradeData data, TargetMode mode, Team.Type team)
        {
            var effect = Action("CardEffectAddCardUpgradeToUnits", mode, team, 0);
            Set(effect, "paramCardUpgradeData", data); Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetime.Permanent); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
