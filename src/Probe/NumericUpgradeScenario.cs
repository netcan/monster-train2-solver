using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;

namespace MonsterTrain2Poju.Probe
{
    internal static class NumericUpgradeScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardManager cards = managers.GetCardManager()!;
            SaveManager save = managers.GetSaveManager();
            var owned = cards.GetAllCards(new List<CardState>());
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").Take(2).ToArray();
            if (stewards.Length != 2) throw new InvalidOperationException("Numeric upgrade fixture requires two Steward copies.");
            var permanent = Upgrade(5, 12, 1, 1, false);
            permanent.AddStatusEffectUpgradeStacks("armor", 5);
            stewards[0].ApplyPermanentUpgrade(permanent, save, ignoreUpgradeAnimation: true);
            // Offset and upgrade lists remain separate in the input and apply in native order.
            stewards[0].GetCardStateModifiers().IncrementAdditionalDamage(2);
            stewards[0].GetCardStateModifiers().IncrementAdditionalHP(3);
            var temporary = Upgrade(3, 7, 1, -1, true);
            temporary.AddStatusEffectUpgradeStacks("spikes", 2);
            stewards[1].ApplyTemporaryUpgrade(temporary, save);
            CardState spell = owned.First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage"));
            spell.ApplyPermanentUpgrade(Upgrade(4, 0, 0, 0, false), save, ignoreUpgradeAnimation: true);
            spell.ApplyTemporaryUpgrade(Upgrade(2, 0, 1, 0, true), save);
            spell.GetTemporaryCardStateModifiers().IncrementAdditionalDamage(1);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            CaptureCalibration();
            log.LogInfo("NUMERIC-UPGRADES-PREPARED two independent Steward copies and one damage spell.");
        }

        private static CardUpgradeState Upgrade(int damage, int health, int costReduction, int size, bool removeOnDiscard)
        {
            var upgrade = new CardUpgradeState(); upgrade.Setup();
            upgrade.SetAttackDamage(damage); upgrade.SetAdditionalHP(health); upgrade.SetCostReduction(costReduction);
            upgrade.SetAdditionalSize(size); upgrade.SetRemoveOnDiscard(removeOnDiscard);
            return upgrade;
        }

        private static void CaptureCalibration()
        {
            var samples = new List<object>();
            foreach (CardStateModifiers.StatType stat in Enum.GetValues(typeof(CardStateModifiers.StatType)))
            foreach (int basis in new[] { -5, 0, 3, 100 })
            foreach (bool enforceFloor in new[] { false, true })
            foreach (int[] additions in new[] { new[] { -5, 8 }, new[] { -100, 10 }, new[] { 100, -10 }, new[] { -99, 99, 2 } })
            {
                var groups = new[] { new CardStateModifiers(), new CardStateModifiers() };
                groups[0].GetCardUpgrades().Add(StatUpgrade(stat, additions[0]));
                groups[1].GetCardUpgrades().Add(StatUpgrade(stat, additions[1]));
                if (additions.Length > 2) groups[1].GetCardUpgrades().Add(StatUpgrade(stat, additions[2]));
                string name = stat == CardStateModifiers.StatType.HP ? "Health" : stat.ToString();
                var model = groups.Select(group => new CardModifiers(new CardStatModifier(), group.GetCardUpgrades().Select(upgrade =>
                    new CardUpgradeModifier("", "", new CardStatModifier(upgrade.GetAttackDamage(), upgrade.GetAdditionalHP(),
                        -upgrade.GetCostReduction(), upgrade.GetAdditionalHeal(), upgrade.GetAdditionalSize(), upgrade.GetXCostReduction(),
                        upgrade.GetAdditionalEquipmentLimit(), upgrade.GetAdditionalUpgradeSlotCount()), Array.Empty<CombatStatus>(),
                        false, false, false, 0, 0, Array.Empty<string>())).ToArray(), 0, Array.Empty<string>())).ToArray();
                int native = CardStateModifiers.GetUpgradedStatValue(basis, stat, enforceFloor, groups);
                if (CardModifierModel.UpgradedStat(basis, name, enforceFloor, model) != native)
                    throw new InvalidOperationException("Native modifier calibration differs for " + name);
                samples.Add(new { BaseValue = basis, Stat = name, EnforceFloor = enforceFloor, Modifiers = model, Actual = native });
            }
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "card-modifier-calibration.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(new { Schema = 1, Samples = samples }, Formatting.Indented));
        }

        private static CardUpgradeState StatUpgrade(CardStateModifiers.StatType stat, int value)
        {
            var upgrade = new CardUpgradeState(); upgrade.Setup();
            if (stat == CardStateModifiers.StatType.Damage) upgrade.SetAttackDamage(value);
            else if (stat == CardStateModifiers.StatType.HP) upgrade.SetAdditionalHP(value);
            else if (stat == CardStateModifiers.StatType.Cost) upgrade.SetCostReduction(-value);
            else if (stat == CardStateModifiers.StatType.Heal) upgrade.SetAdditionalHeal(value);
            else if (stat == CardStateModifiers.StatType.Size) upgrade.SetAdditionalSize(value);
            else if (stat == CardStateModifiers.StatType.XCost) upgrade.SetXCostReduction(value);
            else if (stat == CardStateModifiers.StatType.EquipmentLimit) upgrade.SetAdditionalEquipmentLimit(value);
            else upgrade.SetAdditionalUpgradeSlotCount(value);
            return upgrade;
        }
    }
}
