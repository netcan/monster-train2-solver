using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class HandUpgradeScenario
    {
        internal static bool PermanentConsumePrepared { get; private set; }
        internal static bool PermanentConsumeOverTemporaryPrepared { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool targeted, bool permanentConsume = false,
            bool consumeOverTemporary = false)
        {
            PermanentConsumePrepared = permanentConsume;
            PermanentConsumeOverTemporaryPrepared = consumeOverTemporary;
            SaveManager save = managers.GetSaveManager();
            CardManager cardManager = managers.GetCardManager()!;
            CardState[] cards = cardManager.GetAllCards(new List<CardState>()).Where(card =>
                card.GetCardType() == CardType.Spell && card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (cards.Length == 0) throw new InvalidOperationException("Hand upgrade fixture requires the floor-rearranging spell.");
            if (permanentConsume && !cardManager.GetHand().Contains(cards[0]) && !cardManager.DrawSpecificCard(cards[0]))
                throw new InvalidOperationException("Could not draw the permanent Consume hand-upgrade source spell.");
            CardData data = save.GetAllGameData().FindCardData(cards[0].GetCardDataID())!;
            var original = data.GetEffects().ToArray();
            var temporary = DynamicUpgradeScenario.Upgrade("PojuHandTemporary", "740ffbaa-6568-48c5-a22b-f67e346e0001", 2, 3, 0, 0, "armor", 2);
            var unique = DynamicUpgradeScenario.Upgrade("PojuHandUnique", "740ffbaa-6568-48c5-a22b-f67e346e0002", 1, 1, 0, 0, "spikes", 1);
            AccessTools.Field(typeof(CardUpgradeData), "isUnique").SetValue(unique, true);
            AccessTools.Field(typeof(CardUpgradeData), "costReduction").SetValue(unique, 1);
            var permanent = DynamicUpgradeScenario.Upgrade("PojuHandPermanent", "740ffbaa-6568-48c5-a22b-f67e346e0003", 1, 2, 0, 0, "armor", 1);
            CardUpgradeData? permanentConsumeUpgrade = null;
            CardUpgradeData? temporaryConsumeUpgrade = null;
            if (permanentConsume)
            {
                permanentConsumeUpgrade = DynamicUpgradeScenario.Upgrade("PojuHandPermanentConsume", "740ffbaa-6568-48c5-a22b-f67e346e0004", 0, 0, 0, 0, "armor", 0);
                var exhaust = new CardTraitData();
                exhaust.Setup("CardTraitExhaustState");
                AccessTools.Field(typeof(CardUpgradeData), "traitDataUpgrades").SetValue(permanentConsumeUpgrade, new List<CardTraitData> { exhaust });
            }
            if (consumeOverTemporary)
            {
                temporaryConsumeUpgrade = DynamicUpgradeScenario.Upgrade("PojuHandTemporaryConsume", "740ffbaa-6568-48c5-a22b-f67e346e0005", 0, 0, 0, 0, "armor", 0);
                var exhaust = new CardTraitData();
                exhaust.Setup("CardTraitExhaustState");
                AccessTools.Field(typeof(CardUpgradeData), "traitDataUpgrades").SetValue(temporaryConsumeUpgrade, new List<CardTraitData> { exhaust });
            }
            var effects = data.GetEffects(); effects.Clear();
            if (targeted) effects.AddRange(original);
            var upgrades = new List<CardUpgradeData> { temporary, temporary, unique, unique, permanent };
            if (temporaryConsumeUpgrade != null) upgrades.Add(temporaryConsumeUpgrade);
            if (permanentConsumeUpgrade != null) upgrades.Add(permanentConsumeUpgrade);
            foreach (var upgrade in upgrades)
            {
                var effect = new CardEffectData(upgrade == permanent || upgrade == permanentConsumeUpgrade ? "CardEffectAddPermanentCardUpgradeToCardsInHand" :
                    "CardEffectAddTempCardUpgradeToCardsInHand", null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.Hand);
                AccessTools.Field(typeof(CardEffectData), "paramCardUpgradeData").SetValue(effect, upgrade);
                effects.Add(effect);
            }
            if (targeted)
            {
                var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(damage, 1);
                effects.Add(damage);
            }
            else
            {
                AccessTools.Field(typeof(CardData), "targetless").SetValue(data, true);
                AccessTools.Field(typeof(CardData), "targetsRoom").SetValue(data, true);
            }
            if (permanentConsume) AccessTools.Field(typeof(CardData), "cost").SetValue(data, 0);
            foreach (CardState card in cards) card.Setup(data, save);
            NumericUpgradeScenario.Prepare(managers, log);
            log.LogInfo("HAND-UPGRADES-PREPARED targeted=" + targeted + " permanent-consume=" + permanentConsume +
                " consume-over-temporary=" + consumeOverTemporary +
                " source-in-hand=" + cardManager.GetHand().Contains(cards[0]) +
                " repeated temporary, unique, permanent and playing-card exclusion.");
        }
    }
}
