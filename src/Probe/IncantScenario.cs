using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class IncantScenario
    {
        internal static bool Prepared { get; private set; }
        internal static bool ThresholdCoverage { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool spawnAndDeath = false)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] cards = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] stewards = cards.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            if (stewards.Length == 0) throw new InvalidOperationException("Incant fixture requires owned Stewards.");
            CardData steward = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            ThresholdCoverage = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "incant-thresholds";
            Set(unit, "startingStatusEffects", unit.GetStartingStatusEffects().Concat(new[] { Stack("silenced", 1) }).ToArray());
            var guarded = Gold(19, false, true);
            Set(guarded, "requiredStatusEffects", new List<StatusEffectStackData> { Stack("silenced", 999) });
            var chain = Gold(0, false, true);
            Set(chain, "effects", new List<CardEffectData> { Add("armor", 1), Add("armor", 0), Remove("armor", 1) });
            var child = Gold(1, false, true); Set(child, "trigger", CharacterTriggerData.Trigger.OnArmorAdded);
            var silence = Gold(0, false, true); Set(silence, "trigger", CharacterTriggerData.Trigger.OnTurnBegin);
            Set(silence, "effects", new List<CardEffectData> { Add("silenced", 1) });
            Set(unit, "triggers", unit.GetTriggers().Concat(new[] { Gold(5, false, true), Gold(11, true, true),
                Gold(17, false, false), guarded, chain, child, silence }).ToList());
            if (spawnAndDeath)
                Set(unit, "triggers", unit.GetTriggers().Concat(new[] { OtherGold(CharacterTriggerData.Trigger.OnSpawn, 5, false),
                    OtherGold(CharacterTriggerData.Trigger.OnSpawn, 11, true), OtherGold(CharacterTriggerData.Trigger.OnDeath, 23, false),
                    OtherGold(CharacterTriggerData.Trigger.OnDeath, 31, true) }).ToList());
            if (ThresholdCoverage)
            {
                var positive = Gold(23, true, true); Set(positive, "triggerAtThreshold", 1);
                var zero = Gold(31, false, true); Set(zero, "triggerAtThreshold", 0);
                var negative = Gold(13, true, true); Set(negative, "triggerAtThreshold", -3);
                Set(unit, "triggers", unit.GetTriggers().Concat(new[] { positive, zero, negative }).ToList());
            }
            foreach (CardState card in stewards) card.Setup(steward, save);

            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData[] ordinary = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss()))
                .Where(character => !character.IsMiniboss()).GroupBy(character => character.GetID()).Select(group => group.First()).ToArray();
            if (ordinary.Length == 0) throw new InvalidOperationException("Incant fixture requires a natural ordinary enemy.");
            foreach (CharacterData enemy in ordinary)
            {
                Set(enemy, "triggers", enemy.GetTriggers().Concat(new[] { Gold(7, false, true) }).ToList());
                if (spawnAndDeath)
                    Set(enemy, "triggers", enemy.GetTriggers().Concat(new[] { OtherGold(CharacterTriggerData.Trigger.OnSpawn, 7, false),
                        OtherGold(CharacterTriggerData.Trigger.OnDeath, 13, false) }).ToList());
                Set(enemy, "startingStatusEffects", enemy.GetStartingStatusEffects().Concat(new[] { Stack("purify", 1) }).ToArray());
            }

            CardState[] spells = cards.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Incant fixture requires the owned rearrangement spell.");
            CardData spell = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = spell.GetEffects(); effects.Clear();
            effects.Add(Add("armor", 1, TargetMode.Room));
            effects.Add(Remove("silenced", 1, TargetMode.Room));
            effects.Add(Remove("purify", 1, TargetMode.Room));
            Set(spell, "targetless", false); Set(spell, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(spell, save);
            CardState[] emptySpells = cards.Where(card => card.GetCardType() == CardType.Spell && !card.IsAnyAbility() &&
                card.GetCardDataID() != spell.GetID()).ToArray();
            if (emptySpells.Length == 0) throw new InvalidOperationException("Incant fixture requires a second owned ordinary spell.");
            CardData emptySpell = save.GetAllGameData().FindCardData(emptySpells[0].GetCardDataID())!;
            emptySpell.GetEffects().Clear();
            Set(emptySpell, "targetless", true); Set(emptySpell, "targetsRoom", false);
            foreach (CardState card in emptySpells.Where(card => card.GetCardDataID() == emptySpell.GetID())) card.Setup(emptySpell, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            Prepared = true;
            log.LogInfo("INCANT-PREPARED both natural teams, repeat/once/visible/ignored silence, status conditions/children, Purify rejection/removal and paid spells; original Boss and wave pattern retained.");
        }
        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static CharacterTriggerData Gold(int value, bool once, bool ignoreSilence)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(value, once, ignoreSilence);
            Set(trigger, "trigger", CharacterTriggerData.Trigger.CardSpellPlayed); return trigger;
        }
        private static CharacterTriggerData OtherGold(CharacterTriggerData.Trigger kind, int amount, bool once)
        { var trigger = Gold(amount, once, true); Set(trigger, "trigger", kind); return trigger; }
        private static CardEffectData Add(string id, int count, TargetMode mode = TargetMode.Self) => Effect("CardEffectAddStatusEffect", id, count, mode);
        private static CardEffectData Remove(string id, int count, TargetMode mode = TargetMode.Self) => Effect("CardEffectRemoveStatusEffect", id, count, mode);
        private static CardEffectData Effect(string type, string id, int count, TargetMode mode)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters | Team.Type.Heroes);
            effect.Cheat_SetTargetMode(mode); Set(effect, "paramStatusEffects", new[] { Stack(id, count) }); return effect;
        }
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
    }
}
