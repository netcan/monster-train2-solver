using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpawnTriggerScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            HealingScenario.Prepare(managers, log, withTriggers: true);
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData steward = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            steward.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Damage", "AnyMonsterSpawnedTopFloor", 1, 1, "ThisBattle"));
            steward.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Health", "AnyMonsterSpawned", 2, 1, "ThisBattle"));
            steward.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Health", "PlayedCost", 2, 1));
            steward.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Damage", "TurnCount", 1, 0));
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            CardUpgradeData temporary = Upgrade("PojuSpawnTemporary", "ade091ee-b111-4db1-b5e1-b69f15a50501", 1, 1);
            CardUpgradeData permanent = Upgrade("PojuSpawnPermanent", "ade091ee-b111-4db1-b5e1-b69f15a50502", 1, 2);
            CardUpgradeData unscaled = Upgrade("PojuSpawnUnscaled", "ade091ee-b111-4db1-b5e1-b69f15a50503", 1, 1);
            CardUpgradeData notFromCard = Upgrade("PojuSpawnNotFromCard", "ade091ee-b111-4db1-b5e1-b69f15a50504", 1, 1);
            CardUpgradeData fatal = Upgrade("PojuSpawnFatal", "ade091ee-b111-4db1-b5e1-b69f15a50505", 0, -999);
            // Scourge cards enter later in this natural encounter; they are absent from its starting deck.
            CardData junk = save.GetAllGameData().FindCardData("62cc5532-5fa0-4e66-9fec-85be573af359")
                ?? throw new InvalidOperationException("Spawn fixture requires the native ScourgeT1 definition.");
            CardPool pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = "PojuSpawnJunkPool";
            object list = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            AccessTools.Method(list.GetType(), "Add", new[] { typeof(CardData) }).Invoke(list, new object[] { junk });
            var generated = new CardEffectData("CardEffectAddBattleCard", null!, Team.Type.None);
            generated.Cheat_SetTargetMode(TargetMode.Room); Set(generated, "paramCardPool", pool);
            Set(generated, "paramInt", (int)CardPile.HandPile); Set(generated, "additionalParamInt", 1);
            var spawnEffects = new List<CardEffectData> {
                Effect(temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle),
                Effect(permanent, UnitUpgradeLifetime.Permanent), generated };
            if (lethal) spawnEffects.Add(Effect(fatal, UnitUpgradeLifetime.TemporaryUntilUnitDeath));
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnSpawn, spawnEffects.ToArray()));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnUnscaledSpawn, Effect(unscaled, UnitUpgradeLifetime.TemporaryUntilUnitDeath)));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnSpawnNotFromCard, Effect(notFromCard, UnitUpgradeLifetime.TemporaryUntilUnitDeath)));
            triggers.Add(HealingScenario.HealGold(1, false, true));
            Set(triggers[triggers.Count - 1], "trigger", CharacterTriggerData.Trigger.OnDeath);
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards) card.Setup(steward, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData[] enemies = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct().ToArray();
            CardUpgradeData waveBuff = Upgrade("PojuSpawnWaveRoom", "ade091ee-b111-4db1-b5e1-b69f15a50506", 1, 1);
            foreach (CharacterData enemy in enemies)
            {
                var added = enemy.GetTriggers().ToList();
                added.Add(Trigger(CharacterTriggerData.Trigger.OnSpawn,
                    Effect(waveBuff, UnitUpgradeLifetime.TemporaryUntilUnitDeath, Team.Type.Heroes, TargetMode.Room)));
                added.Add(Trigger(CharacterTriggerData.Trigger.OnUnscaledSpawn,
                    Effect(unscaled, UnitUpgradeLifetime.TemporaryUntilUnitDeath, Team.Type.Heroes)));
                added.Add(Trigger(CharacterTriggerData.Trigger.OnSpawnNotFromCard,
                    Effect(notFromCard, UnitUpgradeLifetime.TemporaryUntilUnitDeath, Team.Type.Heroes)));
                Set(enemy, "triggers", added);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("SPAWN-TRIGGERS-PREPARED paid cost/spawn statistics, source upgrades/generation, ordered scaled/unscaled/cardless phases, enemy batch room buffs and transient summon death=" + lethal + "; natural waves/boss retained.");
        }
        private static CardUpgradeData Upgrade(string name, string id, int damage, int health)
        {
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade(name, id, damage, health, 0, 0, "armor", 0);
            upgrade.GetStatusEffectUpgrades().Clear(); return upgrade;
        }
        private static CardEffectData Effect(CardUpgradeData upgrade, UnitUpgradeLifetime lifetime,
            Team.Type team = Team.Type.Monsters, TargetMode mode = TargetMode.Self)
        {
            var effect = new CardEffectData(lifetime == UnitUpgradeLifetime.Permanent ? "CardEffectAddCardUpgradeToUnits" : "CardEffectAddTempCardUpgradeToUnits", null!, team);
            effect.Cheat_SetTargetMode(mode); Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)lifetime); return effect;
        }
        private static CharacterTriggerData Trigger(CharacterTriggerData.Trigger kind, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, false, true);
            Set(trigger, "trigger", kind); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);

        [HarmonyPatch(typeof(CharacterState), "OnSpawn")]
        private static class SpawnIdentityPatch
        {
            private static void Prefix(CharacterState __instance)
            {
                string? scenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS");
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (scenario != "spawn-triggers" && scenario != "spawn-triggers-lethal" || trace == null ||
                    trace.NativeWon.HasValue || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                // A unit may die during this coroutine before any decision snapshot can see it.
                // Allocate its projection identity at birth, preserving the native event history.
                trace.UnitId(__instance);
            }
        }
    }
}
