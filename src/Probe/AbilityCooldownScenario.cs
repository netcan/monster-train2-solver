using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityCooldownScenario
    {
        internal static bool Prepared;
        internal static bool CacheScenario;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool cacheScenario = false)
        {
            CacheScenario = cacheScenario;
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            CardData ability = UnityEngine.Object.Instantiate(save.GetAllGameData().FindCardData(
                owned.First(card => card.GetCardType() == CardType.Spell).GetCardDataID())!);
            Set(ability, "isUnitAbility", true); Set(ability, "cooldownAtSpawn", 2); Set(ability, "cooldownAfterActivated", 3);
            ability.name = "PojuNativeAbilityCard";
            AccessTools.Field(typeof(GameData), "id").SetValue(ability, "c2f6ed7f-18ce-4070-b65f-7dd9f5160063");
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(save.GetAllGameData())).Add(ability);
            if (cacheScenario) Set(ability, "startingUpgrades", new List<CardUpgradeData> {
                DynamicUpgradeScenario.Upgrade("PojuAbilityStartingUpgrade", "c2f6ed7f-18ce-4070-b65f-7dd9f5160064", 2, 0, 0, 0, "armor", 0) });
            Set(unit, "unitAbility", ability);
            var triggers = unit.GetTriggers().ToList();
            foreach (string kind in new[] { "OnUnitAbilityAvailable", "OnUnitAbilityUnavailable" })
            {
                CharacterTriggerData trigger = HealingScenario.HealGold(kind.EndsWith("Unavailable") ? 7 : 11, false, true);
                Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); triggers.Add(trigger);
            }
            if (cacheScenario)
            {
                CardPool pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = "PojuAbilitySpawnPool";
                object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
                AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) }).Invoke(list,
                    new object[] { save.GetAllGameData().FindCardData("62cc5532-5fa0-4e66-9fec-85be573af359")! });
                var generated = new CardEffectData("CardEffectAddBattleCard", null!, Team.Type.None);
                generated.Cheat_SetTargetMode(TargetMode.Room); Set(generated, "paramCardPool", pool);
                Set(generated, "paramInt", (int)CardPile.HandPile); Set(generated, "additionalParamInt", 1);
                CharacterTriggerData spawn = HealingScenario.HealGold(0, false, true);
                Set(spawn, "trigger", CharacterTriggerData.Trigger.OnSpawn); Set(spawn, "effects", new List<CardEffectData> { generated });
                triggers.Add(spawn);
                CardData enemyAbility = UnityEngine.Object.Instantiate(ability); enemyAbility.name = "PojuNativeEnemyAbilityCard";
                AccessTools.Field(typeof(GameData), "id").SetValue(enemyAbility, "c2f6ed7f-18ce-4070-b65f-7dd9f5160065");
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(save.GetAllGameData())).Add(enemyAbility);
                var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
                var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
                CharacterData boss = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                    .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(),
                        save, save.GetGeneratedBoss())).Distinct().Single(character => character.IsMiniboss());
                Set(boss, "unitAbility", enemyAbility);
            }
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards)
            {
                card.Setup(data, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAdditionalHP(50); upgrade.SetAdditionalSize(-1);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            if (cacheScenario)
            {
                log.LogInfo("ABILITY-CACHE-PREPARED natural shared skill card and OnSpawn generation allocation; original enemy waves.");
            }
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell && card.GetEffects()
                .Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            CardData spell = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            spell.GetEffects().AddRange(new[] {
                Cooldown("AdjustAbilityCooldown", -3, false), Cooldown("ResetCooldown", 0, false),
                Cooldown("AdjustAbilityCooldown", 5, true), Cooldown("ResetCooldown", 0, false),
                Cooldown("ResetCooldown", 0, true), Cooldown("AdjustAbilityCooldown", -10, false),
                Cooldown("ResetCooldown", 0, true), Cooldown("AdjustAbilityCooldown", 0, true),
                Cooldown("ResetCooldown", 0, false), Cooldown("ResetCooldown", 0, true),
                Cooldown("ResetCooldown", 0, false) });
            foreach (CardState card in spells) card.Setup(spell, save);
            log.LogInfo("ABILITY-COMMON " + JsonConvert.SerializeObject(managers.GetCombatManager()!.GetUnitAbilityCommonData().GetCommonTriggers()
                .Select(trigger => new { Kind = trigger.GetTrigger().ToString(), Effects = trigger.GetEffects().Select(effect => new {
                    Type = effect.GetEffectStateName(), Target = effect.GetTargetMode().ToString(), Bool = effect.GetParamBool(),
                    Value = effect.GetParamInt(), Statuses = effect.GetParamStatusEffects() }) })));
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ABILITY-COOLDOWN-PREPARED native ability at spawn, cooldown decay and availability callbacks; original enemy waves.");
        }
        private static CardEffectData Cooldown(string kind, int value, bool parameter)
        {
            var effect = new CardEffectData("CardEffect" + kind, null!, Team.Type.Heroes | Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value); Set(effect, "paramBool", parameter);
            return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
