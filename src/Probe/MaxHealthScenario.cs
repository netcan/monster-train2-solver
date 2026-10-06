using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class MaxHealthScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            HealingScenario.RegisterHealingStatus("heal multiplier", "StatusEffectHealMultiplierState", 2);
            HealingScenario.RegisterHealingStatus("heal immunity", "StatusEffectHealImmunityState", 0);
            SaveManager save = managers.GetSaveManager();
            var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>());
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Maximum-health fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            var gap = Effect("CardEffectAddTempCardUpgradeToUnits", Team.Type.Monsters, 0);
            Set(gap, "paramCardUpgradeData", DynamicUpgradeScenario.Upgrade("PojuMaxHealthGap", "30a7b6d3-fab4-410e-8a12-e88561000001", 0, 0, 0, 5, "armor", 0));
            Set(gap, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath); effects.Add(gap);
            effects.Add(Buff(4, UnitUpgradeLifetimeTempOnly.TemporaryUntilEndOfBattle));
            effects.Add(Buff(-2, UnitUpgradeLifetimeTempOnly.TemporaryUntilEndOfBattle));
            effects.Add(Buff(0, UnitUpgradeLifetimeTempOnly.TemporaryUntilEndOfBattle));
            effects.Add(Buff(3, UnitUpgradeLifetimeTempOnly.TemporaryUntilUnitDeath));
            effects.Add(Effect("CardEffectDebuffMaxHealth", Team.Type.Monsters, 1));
            effects.Add(Effect("CardEffectDebuffMaxHealth", Team.Type.Monsters, -7));
            effects.Add(Effect("CardEffectDebuffMaxHealth", Team.Type.Heroes, lethal ? 9999 : 1));
            // Following effects must still update surviving units and retained spawners after a boss sacrifice.
            if (lethal) effects.Add(Buff(2, UnitUpgradeLifetimeTempOnly.TemporaryUntilEndOfBattle));
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").Take(2).ToArray();
            if (stewards.Length != 2) throw new InvalidOperationException("Maximum-health fixture requires two Steward copies.");
            CharacterData unit = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!
                .GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = unit.GetTriggers().ToList(); triggers.Add(HealingScenario.HealGold(3, once: true, ignoreSilence: false));
            Set(unit, "triggers", triggers);
            for (int index = 0; index < stewards.Length; index++)
            {
                var upgrade = new CardUpgradeState(); upgrade.Setup();
                upgrade.AddStatusEffectUpgradeStacks(index == 0 ? "heal multiplier" : "heal immunity", 1);
                stewards[index].ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("MAX-HEALTH-PREPARED signed spawner offsets, both lifetimes, multiplier/immunity without OnHeal, global debuffs and sacrifice; lethal=" + lethal + "; natural boss and waves retained.");
        }
        private static CardEffectData Buff(int amount, UnitUpgradeLifetimeTempOnly lifetime)
        {
            CardEffectData effect = Effect("CardEffectBuffMaxHealth", Team.Type.Monsters, amount);
            Set(effect, "additionalParamInt1", (int)lifetime); return effect;
        }
        private static CardEffectData Effect(string type, Team.Type team, int value)
        {
            var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(TargetMode.Tower);
            Set(effect, "paramInt", value); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
