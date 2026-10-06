using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class HandUpgradeScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool targeted)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] cards = managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card =>
                card.GetCardType() == CardType.Spell && card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (cards.Length == 0) throw new InvalidOperationException("Hand upgrade fixture requires the floor-rearranging spell.");
            CardData data = save.GetAllGameData().FindCardData(cards[0].GetCardDataID())!;
            var original = data.GetEffects().ToArray();
            var temporary = DynamicUpgradeScenario.Upgrade("PojuHandTemporary", "740ffbaa-6568-48c5-a22b-f67e346e0001", 2, 3, 0, 0, "armor", 2);
            var unique = DynamicUpgradeScenario.Upgrade("PojuHandUnique", "740ffbaa-6568-48c5-a22b-f67e346e0002", 1, 1, 0, 0, "spikes", 1);
            AccessTools.Field(typeof(CardUpgradeData), "isUnique").SetValue(unique, true);
            AccessTools.Field(typeof(CardUpgradeData), "costReduction").SetValue(unique, 1);
            var permanent = DynamicUpgradeScenario.Upgrade("PojuHandPermanent", "740ffbaa-6568-48c5-a22b-f67e346e0003", 1, 2, 0, 0, "armor", 1);
            var effects = data.GetEffects(); effects.Clear();
            if (targeted) effects.AddRange(original);
            foreach (var upgrade in new[] { temporary, temporary, unique, unique, permanent })
            {
                var effect = new CardEffectData(upgrade == permanent ? "CardEffectAddPermanentCardUpgradeToCardsInHand" :
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
            foreach (CardState card in cards) card.Setup(data, save);
            NumericUpgradeScenario.Prepare(managers, log);
            log.LogInfo("HAND-UPGRADES-PREPARED targeted=" + targeted + " repeated temporary, unique, permanent and playing-card exclusion.");
        }
    }
}
