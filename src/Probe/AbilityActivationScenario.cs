using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityActivationScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool xCost, bool lethal,
            bool sourceAttack = false, bool attackWithUnit = false, bool spawnOrStatus = false, bool teleport = false,
            bool damagePerHp = false)
        {
            AbilityCooldownScenario.Prepare(managers, log, cacheScenario: true);
            SaveManager save = managers.GetSaveManager();
            CardData ability = save.GetAllGameData().FindCardData("c2f6ed7f-18ce-4070-b65f-7dd9f5160063")!;
            CardData steward = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = steward.GetSpawnCharacterData()!;
            if (damagePerHp) Set(ability, "cooldownAtSpawn", 0);
            Set(ability, "cost", 1); Set(ability, "targetsRoom", true); Set(ability, "targetless", false);
            Set(ability, "costType", xCost ? CardData.CostType.ConsumeRemainingEnergy : CardData.CostType.Default);
            if (teleport || damagePerHp) Set(ability, "canAbilityTargetOtherFloors", true);
            var effects = new List<CardEffectData>();
            if (spawnOrStatus)
            {
                CardEffectData nativeEffect = save.GetAllGameData().GetAllCardData().SelectMany(card => card.GetEffects())
                    .FirstOrDefault(effect => effect.GetEffectStateName() == "CardEffectSpawnMonsterOrAddStatusEffect")
                    ?? throw new InvalidOperationException("Spawn-or-status fixture could not find a native effect definition.");
                CharacterData fallback = nativeEffect.GetParamCharacterData()
                    ?? throw new InvalidOperationException("Native spawn-or-status effect has no fallback character.");
                string subtype = fallback.GetSubtypes().Select(item => item.Key)
                    .FirstOrDefault(key => !unit.GetSubtypes().Any(item => item.Key == key))
                    ?? throw new InvalidOperationException("Native fallback has no subtype distinct from the ability actor.");
                CardEffectData StatusOrSpawn(string statusId, int stacks)
                {
                    CardEffectData effect = Effect("SpawnMonsterOrAddStatusEffect", 0, TargetMode.Room, Team.Type.Monsters);
                    Set(effect, "paramCharacterData", fallback);
                    Set(effect, "targetCharacterSubtype", subtype);
                    Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = statusId, count = stacks } });
                    return effect;
                }
                effects.Add(StatusOrSpawn("armor", 2));
                effects.Add(StatusOrSpawn("damage shield", 3));
            }
            else if (teleport)
            {
                effects.Add(Effect("UnitAbilityTeleport", 0, TargetMode.Self, Team.Type.Monsters));
            }
            else if (damagePerHp)
            {
                CardEffectData damage = Effect("DamagePerHp", 1, TargetMode.FrontInRoom, Team.Type.Heroes);
                Set(damage, "paramBool", true);
                effects.Add(damage);
            }
            else
            {
                effects.Add(Effect(sourceAttack ? "DamagePerSourceAttack" : "Damage", lethal ? 999 : 2,
                    TargetMode.FrontInRoom, Team.Type.Heroes));
                effects.Add(Effect("Heal", 1, TargetMode.Self, Team.Type.Heroes));
                effects.Add(Effect("DrawAdditionalNextTurn", 1, TargetMode.Room, Team.Type.None));
            }
            if (attackWithUnit)
            {
                var unitAttack = Effect("AttackWithUnit", 0, TargetMode.Self, Team.Type.Heroes);
                Set(unitAttack, "paramBool", false);
                Set(unitAttack, "paramBool2", false);
                var pyreAttack = Effect("AttackWithUnit", 0, TargetMode.Pyre, Team.Type.Monsters);
                Set(pyreAttack, "paramBool", false);
                Set(pyreAttack, "paramBool2", true);
                effects.Add(unitAttack);
                effects.Add(pyreAttack);
                var seedShield = Effect("AddStatusEffect", 0, TargetMode.Self, Team.Type.Monsters);
                Set(seedShield, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "damage shield", count = 2 } });
                var armorPerShield = Effect("AddStatusEffectPerOtherEffect", 3, TargetMode.Self, Team.Type.Monsters);
                Set(armorPerShield, "paramBool", true);
                Set(armorPerShield, "paramStatusEffects", new[] {
                    new StatusEffectStackData { statusId = "armor", count = 1 },
                    new StatusEffectStackData { statusId = "damage shield", count = 1 }
                });
                effects.Add(seedShield);
                effects.Add(armorPerShield);
            }
            Set(ability, "effects", effects);
            var triggers = unit.GetTriggers().ToList();
            foreach (var phase in spawnOrStatus || damagePerHp ? Array.Empty<CharacterTriggerData.Trigger>() :
                new[] { CharacterTriggerData.Trigger.OnPreOwnAbilityActivated, CharacterTriggerData.Trigger.OnOwnAbilityActivated })
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
            log.LogInfo("ABILITY-ACTIVATION-PREPARED natural cached skill, payment, pre/own callbacks and unchanged waves; x=" + xCost +
                "; lethal=" + lethal + "; sourceAttack=" + sourceAttack + "; attackWithUnit=" + attackWithUnit +
                "; spawnOrStatus=" + spawnOrStatus + "; damagePerHp=" + damagePerHp +
                (damagePerHp ? "; cooldownAtSpawn=0 for live last-attacker coverage" : ""));
        }
        private static CardEffectData Effect(string kind, int value, TargetMode mode, Team.Type team)
        {
            var effect = new CardEffectData("CardEffect" + kind, null!, team);
            effect.Cheat_SetTargetMode(mode); Set(effect, "paramInt", value); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
