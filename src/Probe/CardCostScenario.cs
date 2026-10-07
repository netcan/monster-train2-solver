using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardCostScenario
    {
        internal static bool Prepared;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] roomSpells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardState[] targeted = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            Configure(save.GetAllGameData().FindCardData(roomSpells[0].GetCardDataID())!, 0, TargetMode.Room, 2);
            Configure(save.GetAllGameData().FindCardData(targeted[0].GetCardDataID())!, 2, TargetMode.DropTargetCharacter, lethal ? 4096 : 1);
            foreach (CardState card in roomSpells.Concat(targeted))
            {
                card.Setup(save.GetAllGameData().FindCardData(card.GetCardDataID())!, save);
                if (targeted.Contains(card))
                {
                    var permanent = new CardUpgradeState(); permanent.Setup(); permanent.SetCostReduction(99); permanent.SetXCostReduction(3);
                    card.ApplyPermanentUpgrade(permanent, save, ignoreUpgradeAnimation: true);
                    var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetXCostReduction(-1);
                    card.ApplyTemporaryUpgrade(temporary, save);
                }
            }
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678"))
            {
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.SetAdditionalHP(20);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            Prepared = true;
            log.LogInfo("X-COST-PREPARED raw-cost 0/2, independent fixed/X upgrades, zero/positive payments, repeated casts, paid-cost scaling, post-payment gains and original Boss/waves; lethal=" + lethal);
        }
        private static void Configure(CardData card, int rawCost, TargetMode target, int perEnergy)
        {
            Set(card, "costType", CardData.CostType.ConsumeRemainingEnergy); Set(card, "cost", rawCost);
            card.GetTraits().Clear();
            card.GetTraits().Add(DamageScalingScenario.Trait("UnmodifiedPlayedCost", "ThisTurn", perEnergy, 1, false));
            card.GetTraits().Add(DamageScalingScenario.Trait("PlayedCost", "ThisTurn", 1, .5f, true));
            card.GetEffects().Clear();
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes);
            damage.Cheat_SetTargetMode(target); Set(damage, "paramInt", 0); card.GetEffects().Add(damage);
            var gain = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None);
            gain.Cheat_SetTargetMode(TargetMode.Room); Set(gain, "paramInt", target == TargetMode.Room ? 0 : 2); card.GetEffects().Add(gain);
            Set(card, "targetless", false); Set(card, "targetsRoom", true);
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
