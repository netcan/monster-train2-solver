using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class AttackBuffScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardType() == CardType.Spell && card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Attack fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            effects.Add(Effect("CardEffectBuffDamage", TargetMode.Tower, Team.Type.Heroes, 2));
            effects.Add(Effect("CardEffectDebuffDamage", TargetMode.LastTargetedCharacters, Team.Type.Heroes, 99));
            effects.Add(Effect("CardEffectBuffDamage", TargetMode.LastTargetedCharacters, Team.Type.Heroes, 100));
            effects.Add(Effect("CardEffectDebuffDamage", TargetMode.Tower, Team.Type.Monsters, 99));
            effects.Add(Effect("CardEffectBuffDamage", TargetMode.Tower, Team.Type.Monsters, 102));
            effects.Add(Effect("CardEffectBuffDamage", TargetMode.RandomFromAnyRoom, Team.Type.Monsters, 1));
            effects.Add(Effect("CardEffectBuffDamage", TargetMode.Room, Team.Type.Heroes | Team.Type.Monsters, 0));
            effects.Add(Effect("CardEffectDebuffDamage", TargetMode.Room, Team.Type.Heroes | Team.Type.Monsters, -7));
            var upgrade = Effect("CardEffectAddTempCardUpgradeToUnits", TargetMode.Tower, Team.Type.Monsters);
            Set(upgrade, "paramCardUpgradeData", DynamicUpgradeScenario.Upgrade("PojuAttackBuffUpgrade",
                "854ecdd2-5f57-4c85-9e3b-64c6b1900001", 1, 0, 0, 0, "armor", 0));
            Set(upgrade, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            effects.Add(upgrade);
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ATTACK-BUFFS-PREPARED incapable enemies, raw negative balances/recovery, remote/random friendly buffs, zero/negative values and later unit upgrades; natural boss and waves retained.");
        }
        private static CardEffectData Effect(string type, TargetMode target, Team.Type team, int value)
        {
            var effect = new CardEffectData(type, null!, team);
            effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value);
            return effect;
        }
        private static CardEffectData Effect(string type, TargetMode target, Team.Type team) => Effect(type, target, team, 0);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
