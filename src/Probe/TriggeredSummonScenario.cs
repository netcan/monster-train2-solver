using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredSummonScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardData hostCard = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData host = hostCard.GetSpawnCharacterData() ?? throw new InvalidOperationException("Missing triggered summon host.");
            CharacterData child = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Select(card => card.GetSpawnCharacterData()).First(unit => unit != null && unit != host &&
                    unit.name.StartsWith("TrainSteward", StringComparison.Ordinal)) ?? throw new InvalidOperationException("Missing triggered summon child.");
            Set(host, "size", 1); Set(child, "size", 1);
            bool equipment = TriggeredSummonEquipmentScenario.Enabled;
            if (equipment)
            {
                Set(host, "equipmentLimit", 3); Set(host, "health", 35); Set(child, "equipmentLimit", 1);
                TriggeredSummonEquipmentScenario.Prepare(managers);
            }
            bool fresh = (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").EndsWith("fresh", StringComparison.Ordinal);
            var extra = DynamicUpgradeScenario.Upgrade("PojuTriggeredSpawnExtra", "c2f6ed7f-18ce-4070-b65f-7dd9f5190041",
                1, 2, 0, 0, "armor", 1);
            CardEffectData summon = Summon(child, fresh, extra);
            CardEffectData deathSummon = Summon(child, fresh, extra);
            Set(host, "triggers", host.GetTriggers().Concat(new[] {
                Trigger(CharacterTriggerData.Trigger.OnTurnBegin, summon),
                Trigger(CharacterTriggerData.Trigger.OnDeath, deathSummon),
                Trigger(CharacterTriggerData.Trigger.CardMonsterPlayed, Gold(3)),
                Trigger(CharacterTriggerData.Trigger.OnSpawn, Gold(1)) }).ToList());
            Set(child, "triggers", child.GetTriggers().Concat(new[] {
                Trigger(CharacterTriggerData.Trigger.OnSpawn, Gold(2)),
                Trigger(CharacterTriggerData.Trigger.OnUnscaledSpawn, Gold(4)),
                Trigger(CharacterTriggerData.Trigger.OnHeal, Gold(6)) }).Concat(equipment ? new[] {
                Trigger(CharacterTriggerData.Trigger.OnEquipmentAdded, Gold(8)),
                Trigger(CharacterTriggerData.Trigger.OnEquipmentAddedToAny, Gold(9)),
                Trigger(CharacterTriggerData.Trigger.OnEquipmentRemoved, Gold(10)) } : Array.Empty<CharacterTriggerData>()).Concat(new[] {
                Trigger(CharacterTriggerData.Trigger.CardMonsterPlayed, Gold(5)) }).ToList());
            foreach (CardState card in managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == hostCard.GetID()))
                card.Setup(hostCard, save);
            if ((Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").Contains("death"))
            {
                CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
                if (spells.Length == 0) throw new InvalidOperationException("Death summon fixture requires its real rearrangement spell.");
                CardData spell = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
                var lethal = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                lethal.Cheat_SetTargetMode(TargetMode.Tower); Set(lethal, "paramInt", 9999);
                spell.GetEffects().Clear(); spell.GetEffects().Add(lethal);
                Set(spell, "targetless", false); Set(spell, "targetsRoom", true);
                foreach (CardState card in spells) card.Setup(spell, save);
            }
            // Seed the finite catalog before any context boundary is observed.
            TriggeredSummonProbe.Definition(summon); TriggeredSummonProbe.Definition(deathSummon);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("TRIGGERED-SUMMON-PREPARED live turn-begin and death births, copied/fresh sources=" + fresh +
                ", extra upgrades, cardless Rally and deferred spawn rewards; original Boss/waves retained.");
        }
        private static CardEffectData Summon(CharacterData child, bool fresh, CardUpgradeData extra)
        {
            var effect = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramCharacterData", child);
            Set(effect, "paramInt", TriggeredSummonEquipmentScenario.Enabled ? 2 : 1); Set(effect, "paramBool", fresh); Set(effect, "paramCardUpgradeData", extra);
            return effect;
        }
        private static CardEffectData Gold(int amount)
        { var effect = new CardEffectData("CardEffectRewardGold", null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", amount); return effect; }
        private static CharacterTriggerData Trigger(CharacterTriggerData.Trigger kind, params CardEffectData[] effects)
        { var trigger = HealingScenario.HealGold(0, false, true); Set(trigger, "trigger", kind); Set(trigger, "effects", effects.ToList()); return trigger; }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
