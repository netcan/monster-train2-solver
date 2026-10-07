using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredStatusScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            AttackTriggerScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData data = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CardTraitData trait = DamageScalingScenario.Trait("TimesDrawn", "ThisBattle", 1, 1, false);
            Set(trait, "traitStateName", "CardTraitScalingAddStatusEffect");
            Set(trait, "paramTrackedValue", CardStatistics.TrackedValueType.TimesDrawn);
            Set(trait, "paramBool", true); Set(trait, "paramInt2", 0);
            Set(trait, "paramStatusEffects", new[] { Stack("armor", 1) }); data.GetTraits().Add(trait);
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            CardEffectData scaled = Status(TargetMode.Room, Team.Type.Heroes, 0, Stack("armor", 1));
            Set(scaled, "useStatusEffectStackMultiplier", true); Set(scaled, "statusEffectStackMultiplier", "regen");
            Set(scaled, "useHealthMissingStackMultiplier", true); Set(scaled, "useMagicPowerMultiplier", true);
            CardEffectData strict = Status(TargetMode.Self, Team.Type.Monsters, 0, Stack("piercing", 1)); Set(strict, "paramBool", true);
            CardEffectData filtered = Status(TargetMode.Room, Team.Type.Heroes | Team.Type.Monsters, 0, Stack("melee weakness", 1));
            Set(filtered, "targetModeStatusEffectsExcludedFilter", new[] { "immune" });
            var added = unit.GetTriggers().ToList();
            added.Insert(0, Trigger("OnSpawn", false, true, Status(TargetMode.Room, Team.Type.Monsters, 0, Stack("armor", 1), Stack("regen", 1)), strict, strict));
            added.Add(Trigger("PreCombat", true, true,
                Status(TargetMode.RandomInRoom, Team.Type.Monsters, 50, Stack("armor", 1), Stack("regen", 1)),
                Status(TargetMode.Room, Team.Type.Monsters, -10, Stack("armor", 1)),
                Status(TargetMode.Room, Team.Type.Monsters, 100, Stack("armor", 1)), scaled, filtered,
                Range(Status(TargetMode.Room, Team.Type.None, 0, Stack("armor", 1), Stack("regen", 1)), 0, 101),
                Range(Status(TargetMode.Self, Team.Type.Monsters, 0, Stack("armor", 0)), 0, 0)));
            added.Add(Trigger("OnAttacking", false, true, Status(TargetMode.LastAttackedCharacter, Team.Type.Heroes, 50, Stack("debuff", 1)),
                Status(TargetMode.LastAttackedCharacter, Team.Type.Monsters, 0, Stack("armor", -40000))));
            added.Add(Trigger("OnHit", false, true, Status(TargetMode.Self, Team.Type.Monsters, 0, Stack("armor", 1))));
            added.Add(Trigger("OnDeath", false, true, Status(TargetMode.Self, Team.Type.Monsters, 0, Stack("regen", 2)),
                Status(TargetMode.Room, Team.Type.Monsters, 0, Stack("armor", 0))));
            Set(unit, "triggers", added); foreach (CardState card in stewards) card.Setup(data, save);
            var immune = new CardUpgradeState(); immune.Setup(); immune.AddStatusEffectUpgradeStacks("immune", 1);
            stewards[0].ApplyPermanentUpgrade(immune, save, ignoreUpgradeAnimation: true);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var triggers = enemy.GetTriggers().ToList();
                triggers.Add(Trigger("OnTurnBegin", false, true, Status(TargetMode.Self, Team.Type.Heroes, 50, Stack("armor", 1)),
                    Status(TargetMode.Room, Team.Type.Monsters, 0, Stack("debuff", 1))));
                triggers.Add(Trigger("OnAttacking", false, true,
                    Status(TargetMode.LastAttackedCharacter, Team.Type.Monsters, 50, Stack("debuff", 1)),
                    Status(TargetMode.LastAttackedCharacter, Team.Type.Monsters, 0, Stack("armor", 1))));
                Set(enemy, "triggers", triggers);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true); Prepared = true;
            if (Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACKS") == "1") StatusCallbackScenario.Prepare(managers, log);
            log.LogInfo("TRIGGERED-STATUS-PREPARED status pools, reverse chance/immune order, empty/ranged/strict legality, actor+first-target scaling, room magic power, source traits/statistics and retained dying targets; natural waves/boss retained.");
        }
        private static CardEffectData Status(TargetMode mode, Team.Type team, int chance, params StatusEffectStackData[] statuses)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, team); effect.Cheat_SetTargetMode(mode);
            Set(effect, "paramInt", chance); Set(effect, "paramStatusEffects", statuses); return effect;
        }
        private static CardEffectData Range(CardEffectData effect, int min, int max)
        { Set(effect, "useIntRange", true); Set(effect, "paramMinInt", min); Set(effect, "paramMaxInt", max); return effect; }
        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static CharacterTriggerData Trigger(string kind, bool once, bool ignored, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignored);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
