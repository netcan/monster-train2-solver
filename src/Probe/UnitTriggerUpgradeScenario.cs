using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitTriggerUpgradeScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            HealingScenario.Prepare(managers, log, withTriggers: true);
            SaveManager save = managers.GetSaveManager();
            CardState[] stewards = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData cardData = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            cardData.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Damage", "TimesPlayed", 1, 0, "ThisBattle"));
            cardData.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Health", "AnyCharacter", 2, 1));
            cardData.GetTraits().Add(UnitUpgradeScalingScenario.Trait("Health", "TurnCount", 1, 0));
            CharacterData unit = cardData.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            CardUpgradeData temporary = Upgrade("PojuTriggerTemporary", "bde091ee-b111-4db1-b5e1-b69f15a50401", 1, 1);
            CardUpgradeData permanent = Upgrade("PojuTriggerPermanent", "bde091ee-b111-4db1-b5e1-b69f15a50402", 1, 2);
            CardUpgradeData deathOnly = Upgrade("PojuTriggerUntilDeath", "bde091ee-b111-4db1-b5e1-b69f15a50403", 0, 1);
            CardUpgradeData magic = Upgrade("PojuTriggerMagic", "bde091ee-b111-4db1-b5e1-b69f15a50404", 0, 1);
            Set(magic, "magicPowerTraitScalingOnly", true);
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnHeal, false, false,
                Effect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle),
                Effect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle),
                Effect("CardEffectRemoveTempUpgradeFromUnit", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle)));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.OnHeal, true, true,
                Effect("CardEffectAddCardUpgradeToUnits", permanent, UnitUpgradeLifetime.Permanent)));
            triggers.Add(Trigger(CharacterTriggerData.Trigger.PostCombat, false, false,
                Effect("CardEffectAddTempCardUpgradeToUnits", deathOnly, UnitUpgradeLifetime.TemporaryUntilUnitDeath),
                Effect("CardEffectAddTempCardUpgradeToUnits", magic, UnitUpgradeLifetime.TemporaryUntilUnitDeath)));
            Set(unit, "triggers", triggers);
            // Setup refreshes traits while retaining the prepared healing/status upgrades.
            foreach (CardState card in stewards) card.Setup(cardData, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("UNIT-TRIGGER-UPGRADES-PREPARED OnHeal/PostCombat, repeat/once/silence, source-card scaling, three lifetimes, repeated base-valued removal and magic-only gates; natural waves/boss retained.");
        }
        private static CardUpgradeData Upgrade(string name, string id, int damage, int health)
        {
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade(name, id, damage, health, 0, 0, "armor", 0);
            upgrade.GetStatusEffectUpgrades().Clear(); return upgrade;
        }
        private static CardEffectData Effect(string type, CardUpgradeData upgrade, UnitUpgradeLifetime lifetime)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Self);
            Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)lifetime); return effect;
        }
        private static CharacterTriggerData Trigger(CharacterTriggerData.Trigger kind, bool once, bool ignoreSilence,
            params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, once, ignoreSilence);
            Set(trigger, "trigger", kind); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
