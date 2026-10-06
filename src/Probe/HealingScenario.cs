using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class HealingScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            RegisterHealingStatus("heal multiplier", "StatusEffectHealMultiplierState", 2);
            RegisterHealingStatus("heal immunity", "StatusEffectHealImmunityState", 0);
            SaveManager save = managers.GetSaveManager();
            var owned = managers.GetCardManager()!.GetAllCards(new List<CardState>());
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Healing fixture requires the floor-rearranging spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects();
            var unhealed = DynamicUpgradeScenario.Upgrade("PojuHealMaxHealth", "34331f75-5c8c-4139-8b91-b2f03fda0001", 0, 0, 0, 12, "regen", 1);
            var add = new CardEffectData("CardEffectAddTempCardUpgradeToUnits", null!, Team.Type.Monsters);
            add.Cheat_SetTargetMode(TargetMode.LastTargetedCharacters);
            AccessTools.Field(typeof(CardEffectData), "paramCardUpgradeData").SetValue(add, unhealed);
            AccessTools.Field(typeof(CardEffectData), "additionalParamInt1").SetValue(add, (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            effects.Add(add);
            foreach (var entry in new[] { ("CardEffectDamage", 2), ("CardEffectHeal", 3), ("CardEffectHeal", 0), ("CardEffectHeal", 999) })
            {
                var effect = new CardEffectData(entry.Item1, null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.LastTargetedCharacters);
                AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(effect, entry.Item2);
                effects.Add(effect);
            }
            foreach (CardState card in spells)
            {
                card.Setup(data, save);
                var permanent = new CardUpgradeState(); permanent.Setup(); permanent.SetAdditionalHeal(-5);
                card.ApplyPermanentUpgrade(permanent, save, ignoreUpgradeAnimation: true);
                var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAdditionalHeal(8); temporary.SetRemoveOnDiscard(true);
                card.ApplyTemporaryUpgrade(temporary, save);
            }
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").Take(2).ToArray();
            if (stewards.Length != 2) throw new InvalidOperationException("Healing fixture requires two Steward copies.");
            for (int index = 0; index < stewards.Length; index++)
            {
                var upgrade = new CardUpgradeState(); upgrade.Setup();
                upgrade.AddStatusEffectUpgradeStacks(index == 0 ? "heal multiplier" : "heal immunity", 1);
                upgrade.AddStatusEffectUpgradeStacks("regen", 2);
                upgrade.AddStatusEffectUpgradeStacks("lifesteal", 2);
                stewards[index].ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("HEALING-PREPARED heal group clamps, max health, immunity, multiplier, regen and lifesteal.");
        }

        // These native state classes exist in this build but their definitions may be absent.
        // Register hidden definitions only in the isolated fixture's in-memory database.
        private static void RegisterHealingStatus(string id, string state, int parameter)
        {
            StatusEffectManager manager = StatusEffectManager.Instance;
            if (manager.GetStatusEffectDataById(id, expectToFind: false) != null) return;
            var database = (StatusEffectsData)AccessTools.Field(typeof(StatusEffectManager), "statusEffectsData").GetValue(manager);
            var rule = (StatusEffectData)AccessTools.Method(typeof(object), "MemberwiseClone")
                .Invoke(manager.GetStatusEffectDataById("regen"), null);
            foreach (var entry in new[] { ("statusId", (object)id), ("statusEffectStateName", state),
                ("paramInt", parameter), ("hidden", true), ("removeStackAtEndOfTurn", false),
                ("triggerStage", StatusEffectData.TriggerStage.None) })
                AccessTools.Field(typeof(StatusEffectData), entry.Item1).SetValue(rule, entry.Item2);
            database.GetStatusEffectData().Add(rule);
        }
    }
}
