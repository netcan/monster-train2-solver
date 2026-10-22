using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class EmberTrainHealScenario
    {
        internal static bool XCost => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "train-heal-per-ember-x";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "train-heal-per-ember" || XCost;
        private static readonly List<int> Cards = new List<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (XCost) { PrepareXCost(managers, log); return; }
            var save = managers.GetSaveManager();
            var all = save.GetAllGameData();
            var cards = managers.GetCardManager()!;
            var definition = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            var upgradeData = all.GetAllCardUpgradeData().Single(item => item.name == "TraitHealPyrePerEmber");
            var effects = upgradeData.GetCardTriggerUpgrades().SelectMany(trigger => trigger.GetCardEffects()).ToArray();
            if (definition.GetCost() != 1 || effects.Length != 1 ||
                effects[0].GetEffectStateName() != "CardEffectHealPyrePerEmberCost" || effects[0].GetParamInt() != 1)
                throw new InvalidOperationException("Original per-ember healing upgrade or steward definition changed.");
            if (cards.GetHand().Count > 6 || save.GetMaxTowerHP() <= 5)
                throw new InvalidOperationException("Per-ember healing controls require four hand slots and Pyre HP.");
            save.Cheat_SetTowerHP(save.GetMaxTowerHP() - 5);
            Cards.Clear();
            for (int index = 0; index < 4; index++)
            {
                CardData carrier = index == 3 ? all.FindCardData(CardGraftedEquipmentModel.EventEquipmentId)! : definition;
                if (carrier.GetCost() != 1) throw new InvalidOperationException("Original healing carrier cost changed.");
                CardState card = cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
                var upgrade = new CardUpgradeState(); upgrade.Setup(upgradeData);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
                if (index != 0)
                {
                    var discount = new CardUpgradeState(); discount.Setup(); discount.SetCostReduction(1);
                    card.ApplyPermanentUpgrade(discount, save, ignoreUpgradeAnimation: true);
                }
                if (index == 2) AccessTools.Field(typeof(CardState), "cost").SetValue(card, 3);
                Cards.Add(FullBattleTrace.Active!.CardId(card));
            }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("EMBER-TRAIN-HEAL-PREPARED original upgrade; four explicit carrier controls: raw/paid costs 1/1, 1/0, 3/2, 1/0; Pyre missing 5 HP.");
        }

        private static void PrepareXCost(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardUpgradeData healing = all.GetAllCardUpgradeData().Single(item => item.name == "TraitHealPyrePerEmber");
            CardEffectData effect = healing.GetCardTriggerUpgrades().SelectMany(trigger => trigger.GetCardEffects()).Single();
            if (effect.GetEffectStateName() != "CardEffectHealPyrePerEmberCost" || effect.GetParamInt() != 1 ||
                cards.GetHand().Count > 6 || save.GetMaxTowerHP() <= 12)
                throw new InvalidOperationException("Original X-cost healing controls require unchanged healing and four hand slots.");
            save.Cheat_SetTowerHP(save.GetMaxTowerHP() - 12);
            Cards.Clear();
            for (int index = 0; index < 4; index++)
            {
                CardData carrier = all.FindCardData(index < 2 ? "d14a50f3-728d-43e1-87f0-ef1b013f6678" : OriginalXDamageCardsScenario.VentId)!;
                CardState card = cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
                if (index < 2 ? carrier.GetCost() != 1 : !card.IsConsumeRemainingEnergyCostType())
                    throw new InvalidOperationException("Original healing carrier cost type changed.");
                var upgrade = new CardUpgradeState(); upgrade.Setup(healing);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
                if (index == 1)
                {
                    var discount = new CardUpgradeState(); discount.Setup(); discount.SetCostReduction(1);
                    card.ApplyPermanentUpgrade(discount, save, ignoreUpgradeAnimation: true);
                }
                if (index >= 2)
                {
                    var bonus = new CardUpgradeState(); bonus.Setup(); bonus.SetXCostReduction(2);
                    card.ApplyPermanentUpgrade(bonus, save, ignoreUpgradeAnimation: true);
                    var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze");
                    card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                }
                Cards.Add(FullBattleTrace.Active!.CardId(card));
            }
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("EMBER-TRAIN-HEAL-X-PREPARED original upgrade; two original Stewards, two original Vent cards with +2 X; Pyre missing 12 HP.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            foreach (int id in Cards)
                if (source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                {
                    if (XCost && Cards.IndexOf(id) >= 2 && source.Spawn.Turn < 1) return null;
                    return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action => action.CardInstanceId == id);
                }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }
    }
}
