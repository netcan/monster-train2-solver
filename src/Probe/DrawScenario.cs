using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class DrawScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            effects.Add(Draw(1)); effects.Add(Draw(0)); effects.Add(Draw(-2));
            CardEffectData ranged = Draw(0);
            Set(ranged, "useIntRange", true); Set(ranged, "paramMinInt", -2); Set(ranged, "paramMaxInt", 4); Set(ranged, "paramMultiplier", .75f);
            effects.Add(ranged); effects.Add(Draw(-1)); effects.Add(Draw(2));
            var buff = new CardEffectData("CardEffectBuffDamage", null!, Team.Type.Monsters);
            buff.Cheat_SetTargetMode(TargetMode.Tower); Set(buff, "paramInt", 2); effects.Add(buff);
            var upgrade = new CardEffectData("CardEffectAddTempCardUpgradeToCardsInHand", null!, Team.Type.None);
            upgrade.Cheat_SetTargetMode(TargetMode.Hand);
            Set(upgrade, "paramCardUpgradeData", DynamicUpgradeScenario.Upgrade("PojuDrawHandUpgrade", "8ae27c21-6e78-4bf6-8009-238180000001", 1, 0, 0, 1, "armor", 0));
            effects.Add(upgrade);
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            CardState[] targeted = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            CardData targetedData = save.GetAllGameData().FindCardData(targeted[0].GetCardDataID())!;
            targetedData.GetEffects().Add(Draw(2));
            foreach (CardState card in targeted) card.Setup(targetedData, save);
            // Establish a native full-hand root, then leave a pending start-of-turn draw modifier.
            if (!cards.GetHand().Contains(spells[0])) cards.DrawSpecificCard(spells[0]);
            cards.DrawCards(cards.GetMaxHandSize());
            // Summons are first in the verification policy; leave enough energy for a deployment-turn draw.
            managers.GetPlayerManager().AddEnergy(1);
            Set(cards, "drawCountModifier", 2);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("DRAWING-PREPARED full hand, pending draw modifier, signed/zero/ranged/max draws, targeted follow-up draws and subsequent hand upgrades; natural waves retained.");
        }
        private static CardEffectData Draw(int amount)
        {
            var effect = new CardEffectData("CardEffectDraw", null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", amount); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
