using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class DynamicUpgradeScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool sacrifice = false)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            var owned = cards.GetAllCards(new List<CardState>());
            CardState[] rallies = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (rallies.Length == 0) throw new InvalidOperationException("Dynamic upgrade fixture requires Rally copies.");
            CardData rally = save.GetAllGameData().FindCardData(rallies[0].GetCardDataID())!;
            var original = rally.GetEffects().ToArray();
            CardUpgradeData permanent = Upgrade("PojuProbePermanent", "cdd091ee-b111-4db1-b5e1-b69f15a50001", 2, 4, 1, 0, "spikes", 1);
            CardUpgradeData temporary = Upgrade("PojuProbeTemporary", "cdd091ee-b111-4db1-b5e1-b69f15a50002", 3, 7, 1, 0, "armor", 3);
            CardUpgradeData unique = Upgrade("PojuProbeUnique", "cdd091ee-b111-4db1-b5e1-b69f15a50003", 1, 2, 0, 0, "valor", 1);
            AccessTools.Field(typeof(CardUpgradeData), "isUnique").SetValue(unique, true);
            CardUpgradeData deathOnly = Upgrade("PojuProbeUntilDeath", "cdd091ee-b111-4db1-b5e1-b69f15a50004", 1, 0, -1, 6, "regen", 1);
            CardUpgradeData restricted = Upgrade("PojuProbeRestricted", "cdd091ee-b111-4db1-b5e1-b69f15a50005", 1, 1, 1, 0, "armor", 1);
            AccessTools.Field(typeof(CardUpgradeData), "restrictSizeToRoomCapacity").SetValue(restricted, true);
            CardUpgradeData oversized = Upgrade("PojuProbeOversized", "cdd091ee-b111-4db1-b5e1-b69f15a50006", 1, 1, 99, 0, "armor", 1);
            AccessTools.Field(typeof(CardUpgradeData), "restrictSizeToRoomCapacity").SetValue(oversized, true);
            var effects = rally.GetEffects(); effects.Clear();
            effects.Add(Effect("CardEffectAddTempCardUpgradeToUnits", oversized, UnitUpgradeLifetime.TemporaryUntilEndOfBattle, true));
            effects.Add(Effect("CardEffectAddTempCardUpgradeToUnits", restricted, UnitUpgradeLifetime.TemporaryUntilUnitDeath));
            effects.Add(Effect("CardEffectRemoveTempUpgradeFromUnit", restricted, UnitUpgradeLifetime.TemporaryUntilUnitDeath));
            effects.Add(Effect("CardEffectAddCardUpgradeToUnits", permanent, UnitUpgradeLifetime.Permanent));
            effects.Add(Effect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            effects.Add(Effect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            effects.Add(Effect("CardEffectRemoveTempUpgradeFromUnit", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            effects.Add(Effect("CardEffectAddTempCardUpgradeToUnits", unique, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            effects.Add(Effect("CardEffectAddTempCardUpgradeToUnits", unique, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            effects.Add(Effect("CardEffectAddTempCardUpgradeToUnits", deathOnly, UnitUpgradeLifetime.TemporaryUntilUnitDeath));
            if (sacrifice)
            {
                CardUpgradeData lethal = Upgrade("PojuProbeLethal", "cdd091ee-b111-4db1-b5e1-b69f15a50007", 0, -100, 0, 0, "armor", 0);
                effects.Add(Effect("CardEffectAddTempCardUpgradeToUnits", lethal, UnitUpgradeLifetime.TemporaryUntilUnitDeath));
            }
            effects.AddRange(original);
            foreach (CardState card in rallies) card.Setup(rally, save);
            NumericUpgradeScenario.Prepare(managers, log);
            log.LogInfo("DYNAMIC-UPGRADES-PREPARED lifetimes, repeated removal, uniqueness, capacity and sacrifice=" + sacrifice);
        }
        private static CardUpgradeData Upgrade(string name, string id, int damage, int hp, int size, int unhealedHp, string status, int count)
        {
            var upgrade = ScriptableObject.CreateInstance<CardUpgradeData>(); upgrade.name = name;
            foreach (var field in typeof(CardUpgradeData).GetFields(System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
                if (field.GetValue(upgrade) == null)
                {
                    if (field.FieldType == typeof(string)) field.SetValue(upgrade, "");
                    else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                        field.SetValue(upgrade, Activator.CreateInstance(field.FieldType));
                }
            AccessTools.Field(typeof(GameData), "id").SetValue(upgrade, id);
            Set(upgrade, "bonusDamage", damage); Set(upgrade, "bonusHP", hp); Set(upgrade, "bonusSize", size);
            Set(upgrade, "unhealedBonusHP", unhealedHp);
            Set(upgrade, "statusEffectUpgrades", new List<StatusEffectStackData> { new StatusEffectStackData { statusId = status, count = count } });
            return upgrade;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static CardEffectData Effect(string type, CardUpgradeData upgrade, UnitUpgradeLifetime lifetime, bool first = false)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(first ? TargetMode.DropTargetCharacter : TargetMode.LastTargetedCharacters);
            Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)lifetime);
            return effect;
        }
    }
}
