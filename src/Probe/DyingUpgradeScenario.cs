using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class DyingUpgradeScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HitKillScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            // Keep attack scaling, but prevent drawn-card health scaling from turning
            // the authored negative HP stage positive in this dying-unit calibration.
            data.GetTraits().RemoveAll(trait => trait.GetTraitStateName() == "CardTraitScalingUpgradeUnitHealth");
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            CardUpgradeData slay = Upgrade("PojuDyingSlay", "bbac1270-8424-4877-9015-194eba4b0001", 1, 1, 2);
            CardUpgradeData revenge = Upgrade("PojuDyingRevenge", "bbac1270-8424-4877-9015-194eba4b0002", 1, 2, 2);
            CardUpgradeData failed = Upgrade("PojuDyingFailedHp", "bbac1270-8424-4877-9015-194eba4b0003", 2, -1, 9, 3);
            CardUpgradeData death = Upgrade("PojuDyingDeath", "bbac1270-8424-4877-9015-194eba4b0004", 1, 2, 3);
            CardUpgradeData failedUnhealed = Upgrade("PojuDyingFailedUnhealed", "bbac1270-8424-4877-9015-194eba4b0005", 2, 2, 9, -1);
            CardUpgradeData unitOnly = Upgrade("PojuDyingUnitOnly", "bbac1270-8424-4877-9015-194eba4b0006", 1, 2, 1);
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger("OnKill", Effect(slay, UnitUpgradeLifetime.TemporaryUntilEndOfBattle)));
            triggers.Add(Trigger("OnHit", Effect(revenge, UnitUpgradeLifetime.Permanent), Effect(failed, UnitUpgradeLifetime.Permanent)));
            triggers.Add(Trigger("OnDeath", Effect(death, UnitUpgradeLifetime.Permanent),
                Effect(slay, UnitUpgradeLifetime.TemporaryUntilEndOfBattle, remove: true), Effect(failedUnhealed, UnitUpgradeLifetime.Permanent),
                Effect(unitOnly, UnitUpgradeLifetime.TemporaryUntilUnitDeath)));
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards) card.Setup(data, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                if (enemy.IsMiniboss() || enemy.IsOuterTrainBoss()) continue;
                Set(enemy, "startingStatusEffects", enemy.GetStartingStatusEffects().Where(status => status.statusId != "spikes")
                    .Concat(new[] { new StatusEffectStackData { statusId = "spikes", count = 999 } }).ToArray());
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            log.LogInfo("DYING-UPGRADE-PREPARED dying Slay/Revenge/death self upgrades, failed HP/unhealed stages, source lifetimes/removal and lethal sweep retaliation; natural waves/boss retained.");
        }
        private static CardUpgradeData Upgrade(string name, string id, int damage, int health, int armor, int unhealed = 0) =>
            DynamicUpgradeScenario.Upgrade(name, id, damage, health, 0, unhealed, "armor", armor);
        private static CardEffectData Effect(CardUpgradeData upgrade, UnitUpgradeLifetime lifetime, bool remove = false)
        {
            var effect = new CardEffectData(remove ? "CardEffectRemoveTempUpgradeFromUnit" : "CardEffectAddCardUpgradeToUnits", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Self); Set(effect, "paramCardUpgradeData", upgrade);
            Set(effect, "additionalParamInt1", (int)lifetime); return effect;
        }
        private static CharacterTriggerData Trigger(string kind, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, false, true);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
