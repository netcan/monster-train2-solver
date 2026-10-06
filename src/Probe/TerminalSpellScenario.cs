using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TerminalSpellScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Terminal spell fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effect = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes);
            effect.Cheat_SetTargetMode(TargetMode.Room);
            AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(effect, 999);
            var effects = data.GetEffects(); effects.Clear(); effects.Add(effect);
            AccessTools.Field(typeof(CardData), "targetless").SetValue(data, false);
            AccessTools.Field(typeof(CardData), "targetsRoom").SetValue(data, true);
            foreach (CardState card in spells) card.Setup(data, save);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("TERMINAL-SPELLS-PREPARED single lethal room damage; native bosses and waves retained.");
        }
    }
}
