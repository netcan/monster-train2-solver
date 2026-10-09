using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using TypeNameCache = ShinyShoe.TypeNameCache;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityIncantScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            AbilityActivationScenario.Prepare(managers, log, xCost: false, lethal: false);
            SaveManager save = managers.GetSaveManager();
            // Prefer a native asset. This build retains the effect class without a
            // collectable definition, so an isolated authored asset exercises it.
            CollectableRelicData[] candidates = save.GetAllGameData().GetAllCollectableRelicData().Where(data =>
                data.GetEffects().Any(effect => TypeNameCache.GetType(effect.GetEffectClassName()) == typeof(RelicEffectIncantTriggeredByUnitAbilities))).ToArray();
            log.LogInfo("ABILITY-INCANT-RELIC-CATALOG " + string.Join("; ", candidates.Select(data => data.name + ":" +
                string.Join(",", data.GetEffects().Select(effect => effect.GetEffectClassName())))));
            CollectableRelicData? relic = candidates.FirstOrDefault(data => data.GetEffects().Count == 1);
            bool authored = relic == null;
            if (relic == null)
            {
                relic = UnityEngine.Object.Instantiate(save.GetAllGameData().GetAllCollectableRelicData().First());
                relic.name = "PojuNativeAbilityIncantRelic";
                AccessTools.Field(typeof(GameData), "id").SetValue(relic, "c2f6ed7f-18ce-4070-b65f-7dd9f5160068");
                var effect = new RelicEffectData();
                Set(effect, "relicEffectClassName", typeof(RelicEffectIncantTriggeredByUnitAbilities).AssemblyQualifiedName!);
                Set(effect, "effectConditions", new List<RelicEffectCondition>());
                Set(relic, "effects", new List<RelicEffectData> { effect });
                ((IList)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(save.GetAllGameData())).Add(relic);
            }
            save.AddRelic(relic);
            CardData steward = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            var child = Gold(5, false); Set(child, "trigger", CharacterTriggerData.Trigger.OnStatusEffectChanged);
            Set(unit, "triggers", unit.GetTriggers().Concat(new[] { Gold(5, false), Gold(11, true), Gold(17, false, false), child }).ToList());
            foreach (CardState card in managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                card.Setup(steward, save);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData[] ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss()))
                .Where(character => !character.IsMiniboss()).GroupBy(character => character.GetID()).Select(group => group.First()).ToArray();
            if (ordinary.Length == 0) throw new InvalidOperationException("Ability Incant fixture requires natural ordinary enemies.");
            foreach (CharacterData enemy in ordinary) Set(enemy, "triggers", enemy.GetTriggers().Concat(new[] { Gold(7, false) }).ToList());
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            Prepared = true;
            log.LogInfo("ABILITY-INCANT-PREPARED authored asset=" + authored + "; relic=" + relic.name + "; id=" + relic.GetID() +
                "; current effects=" + string.Join(",", RelicProbe.Capture(managers).SelectMany(state => state.EffectTypes)) +
                "; actual shared unit skill, both natural teams, cached room and interleaved Own/Incant queues.");
        }
        private static CharacterTriggerData Gold(int value, bool once, bool ignoreSilence = true)
        {
            var trigger = HealingScenario.HealGold(value, once, ignoreSilence);
            Set(trigger, "trigger", CharacterTriggerData.Trigger.CardSpellPlayed); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
