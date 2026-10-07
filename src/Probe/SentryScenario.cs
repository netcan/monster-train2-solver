using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class SentryScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] cards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            if (cards.Length < 2) throw new InvalidOperationException("Sentry fixture requires multiple Stewards.");
            CardData data = save.GetAllGameData().FindCardData(cards[0].GetCardDataID())!;
            CharacterData steward = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = steward.GetTriggers().ToList();
            triggers.Add(Trigger("OnSentry", 3, 4, false, false));
            triggers.Add(Trigger("OnSentry", 5, 2, true, false));
            triggers.Add(Trigger("OnSentry", 7, lethal ? 999 : 3, false, true));
            CharacterTriggerData threshold = Trigger("OnSentry", 99, 0, false, true);
            Set(threshold, "triggerAtThreshold", 1); triggers.Add(threshold);
            Set(steward, "triggers", triggers);
            foreach (CardState card in cards)
            {
                card.Setup(data, save);
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAdditionalHP(50); upgrade.SetAdditionalSize(-1);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            var silence = new CardUpgradeState(); silence.Setup(); silence.AddStatusEffectUpgradeStacks("silenced", 1);
            cards[0].ApplyPermanentUpgrade(silence, save, ignoreUpgradeAnimation: true);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save,
                    save.GetGeneratedBoss())).Distinct())
            {
                var added = enemy.GetTriggers().ToList();
                added.Add(Trigger("PostAscension", 17, 0, false, true));
                added.Add(Trigger("OnShift", 19, 0, false, true));
                added.Add(Trigger("OnHit", 11, 0, false, true));
                added.Add(Trigger("OnDeath", 13, 0, false, true));
                Set(enemy, "triggers", added);
                if (enemy.IsMiniboss())
                {
                    Set(enemy, "loopsBetweenTrainFloors", true);
                    Set(enemy, "startingStatusEffects", enemy.GetStartingStatusEffects().Where(status => status.statusId != "relentless").ToArray());
                }
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            log.LogInfo("SENTRY-PREPARED repeated/once/ignored-silence/threshold triggers, explicit moved targets, hit/death queues, ordinary waves and looping Boss; lethal=" + lethal);
        }
        private static CharacterTriggerData Trigger(string kind, int gold, int damage, bool once, bool ignored)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(gold, once, ignored);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
            if (damage > 0)
            {
                // The native explicit target bypasses the team's normal target filter.
                var effect = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.LastAttackedCharacter); Set(effect, "paramInt", damage);
                Set(trigger, "effects", trigger.GetEffects().Concat(new[] { effect }).ToList());
            }
            return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
