using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityActivationScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool xCost, bool lethal)
        {
            AbilityCooldownScenario.Prepare(managers, log, cacheScenario: true);
            SaveManager save = managers.GetSaveManager();
            CardData ability = save.GetAllGameData().FindCardData("c2f6ed7f-18ce-4070-b65f-7dd9f5160063")!;
            Set(ability, "cost", 1); Set(ability, "targetsRoom", true); Set(ability, "targetless", false);
            Set(ability, "costType", xCost ? CardData.CostType.ConsumeRemainingEnergy : CardData.CostType.Default);
            var damage = Effect("Damage", lethal ? 999 : 2, TargetMode.FrontInRoom, Team.Type.Heroes);
            var heal = Effect("Heal", 1, TargetMode.Self, Team.Type.Heroes);
            var futureDraw = Effect("DrawAdditionalNextTurn", 1, TargetMode.Room, Team.Type.None);
            Set(ability, "effects", new List<CardEffectData> { damage, heal, futureDraw });
            CardData steward = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            var triggers = unit.GetTriggers().ToList();
            foreach (var phase in new[] { CharacterTriggerData.Trigger.OnPreOwnAbilityActivated, CharacterTriggerData.Trigger.OnOwnAbilityActivated })
            {
                CharacterTriggerData trigger = HealingScenario.HealGold(phase == CharacterTriggerData.Trigger.OnPreOwnAbilityActivated ? 3 : 13, false, false);
                Set(trigger, "trigger", phase);
                if (phase == CharacterTriggerData.Trigger.OnPreOwnAbilityActivated)
                    Set(trigger, "effects", trigger.GetEffects().Concat(new[] { Effect("Damage", 3, TargetMode.Self, Team.Type.Heroes) }).ToList());
                triggers.Add(trigger);
            }
            Set(unit, "triggers", triggers);
            foreach (CardState card in managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
            {
                card.Setup(steward, save);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ABILITY-ACTIVATION-PREPARED natural cached skill, payment, pre/own callbacks and unchanged waves; x=" + xCost + "; lethal=" + lethal);
        }
        private static CardEffectData Effect(string kind, int value, TargetMode mode, Team.Type team)
        {
            var effect = new CardEffectData("CardEffect" + kind, null!, team);
            effect.Cheat_SetTargetMode(mode); Set(effect, "paramInt", value); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
