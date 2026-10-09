using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class PurifyScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] cards = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] stewards = cards.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            if (stewards.Length == 0) throw new InvalidOperationException("Purify fixture requires owned Stewards.");
            CardData steward = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            Set(unit, "startingStatusEffects", new[] { Stack("armor", 3), Stack("silenced", 1), Stack("regen", 2), Stack("purify", 1), Stack("buff", 2) });
            var triggers = unit.GetTriggers().ToList();
            foreach (string kind in new[] { "OnStatusEffectChanged", "OnSilenceLost", "OnNewStatusEffectAdded" })
                triggers.Add(Gold(kind, 1));
            // Status notifications are allowed while purified; ordinary spawn,
            // attack and combat triggers remain subject to the real BalanceData.
            triggers.Add(Trigger("OnSilenceLost", Remove("purify", 1), Add("armor", 3), Add("buff", 0),
                Add("purify", 0), Add("armor", 2), Add("purify", 1), Add("armor", 1), Add("armor", 0), Add("regen", -3), Add("purify", -1)));
            triggers.Add(Trigger("PreCombat", Add("armor", 2), Remove("purify", 1), Add("regen", 2),
                Add("purify", 1), Add("buff", -100)));
            triggers.Add(Trigger("OnAttacking", Add("armor", 1), Add("purify", -100)));
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards) card.Setup(steward, save);

            CardState[] spells = cards.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Purify fixture requires the owned rearrangement spell.");
            CardData spell = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = spell.GetEffects(); effects.Clear();
            effects.Add(Add("armor", 2, TargetMode.Room));
            effects.Add(Add("purify", 1, TargetMode.Room));
            effects.Add(Add("regen", 3, TargetMode.Room));
            effects.Add(Remove("purify", 1, TargetMode.Room));
            effects.Add(Add("armor", 1, TargetMode.Room));
            effects.Add(Add("purify", 0, TargetMode.Room));
            effects.Add(Add("armor", 2, TargetMode.Room));
            effects.Add(Add("purify", 1, TargetMode.Room));
            effects.Add(Add("buff", -100, TargetMode.Room));
            Set(spell, "targetless", false); Set(spell, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(spell, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            Prepared = true;
            log.LogInfo("PURIFY-PREPARED starting status order, signed/zero blocked additions, zero-stack clears, removal/reapplication, callback payloads and real paid room spells; original Boss/waves retained.");
        }
        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static CardEffectData Add(string id, int count, TargetMode target = TargetMode.Self)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(target); Set(effect, "paramStatusEffects", new[] { Stack(id, count) }); return effect;
        }
        private static CardEffectData Remove(string id, int count, TargetMode target = TargetMode.Self)
        {
            var effect = new CardEffectData("CardEffectRemoveStatusEffect", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(target); Set(effect, "paramStatusEffects", new[] { Stack(id, count) }); return effect;
        }
        private static CharacterTriggerData Gold(string kind, int value)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(value, false, true);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); return trigger;
        }
        private static CharacterTriggerData Trigger(string kind, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = Gold(kind, 0); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
