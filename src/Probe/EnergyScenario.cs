using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class EnergyScenario
    {
        internal static bool Prepared;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData steward = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            CharacterData unit = steward.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            var triggers = unit.GetTriggers().ToList();
            triggers.Add(Trigger("OnSpawn", Energy("GainEnergy", 2, true)));
            triggers.Add(Trigger("PreCombat", Energy("GainEnergy", 3), Energy("AdjustEnergy", -1), Energy("GainEnergyNextTurn", 2),
                Energy("GainEnergyEveryTurn", 1)));
            triggers.Add(Trigger("OnTurnBegin", Ranged("AdjustEnergy", -3, 4),
                Ranged("GainEnergy", 1, 100, true, true), Energy("GainEnergyEveryTurn", 99)));
            triggers.Add(Trigger("EndTurnPreHandDiscard", Energy("GainEnergy", 2), Energy("AdjustEnergy", -2),
                Energy("GainEnergyNextTurn", 1), Energy("GainEnergyEveryTurn", 0)));
            Set(unit, "triggers", triggers);
            foreach (CardState card in stewards) card.Setup(steward, save);
            CardState[] roomSpells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardData roomSpell = save.GetAllGameData().FindCardData(roomSpells[0].GetCardDataID())!;
            roomSpell.GetEffects().InsertRange(0, new[] { Energy("GainEnergy", 10000), Energy("AdjustEnergy", -6),
                Energy("GainEnergy", 0), Energy("GainEnergy", -2), Energy("GainEnergyNextTurn", 0), Energy("GainEnergyNextTurn", -3),
                Energy("GainEnergyEveryTurn", 0), Energy("GainEnergyEveryTurn", -1), Energy("GainEnergy", 2, true), Energy("AdjustEnergy", 2),
                Ranged("GainEnergy", -2, 4), Energy("GainEnergyNextTurn", 2), Energy("GainEnergyEveryTurn", 1) });
            foreach (CardState card in roomSpells) card.Setup(roomSpell, save);
            CardState[] damageSpells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            CardData damageSpell = save.GetAllGameData().FindCardData(damageSpells[0].GetCardDataID())!;
            if (lethal) Set(damageSpell.GetEffects()[0], "paramInt", 9999);
            damageSpell.GetEffects().AddRange(new[] { Energy("GainEnergy", 1), Ranged("AdjustEnergy", -2, 3),
                Energy("GainEnergyNextTurn", 3), Energy("GainEnergyEveryTurn", 1) });
            foreach (CardState card in damageSpells) card.Setup(damageSpell, save);
            managers.GetCombatManager()!.ModifyEnergyForNextTurn(-4);
            managers.GetCombatManager()!.ModifyEnergyForEveryTurn(2);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            Prepared = true;
            log.LogInfo("ENERGY-PREPARED caps, signed/ranged/current/next/every-turn effects, phase gates, post-boss gates and original waves; lethal=" + lethal);
        }
        private static CardEffectData Energy(string kind, int amount, bool onlyMonsterTurn = false)
        {
            var effect = new CardEffectData("CardEffect" + kind, null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", amount); Set(effect, "paramBool", onlyMonsterTurn); return effect;
        }
        private static CardEffectData Ranged(string kind, int min, int max, bool onlyMonsterTurn = false, bool cancel = false)
        {
            CardEffectData effect = Energy(kind, 0, onlyMonsterTurn);
            Set(effect, "useIntRange", true); Set(effect, "paramMinInt", min); Set(effect, "paramMaxInt", max); Set(effect, "paramMultiplier", .75f);
            Set(effect, "shouldCancelSubsequentEffectsIfTestFails", cancel); return effect;
        }
        private static CharacterTriggerData Trigger(string kind, params CardEffectData[] effects)
        {
            CharacterTriggerData trigger = HealingScenario.HealGold(0, false, true);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
