using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomSpellScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Room spell fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            effects.Add(Effect("CardEffectDamage", TargetMode.Room, Team.Type.Heroes, 2));
            var health = DynamicUpgradeScenario.Upgrade("PojuRoomMaxHealth", "0d803b19-e51a-48bb-b8c8-79550caf0001", 0, 0, 0, 4, "armor", 0);
            var upgrade = Effect("CardEffectAddTempCardUpgradeToUnits", TargetMode.Room, Team.Type.Monsters);
            AccessTools.Field(typeof(CardEffectData), "paramCardUpgradeData").SetValue(upgrade, health);
            AccessTools.Field(typeof(CardEffectData), "additionalParamInt1").SetValue(upgrade, (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            effects.Add(upgrade);
            var status = Effect("CardEffectAddStatusEffect", TargetMode.LastTargetedCharacters, Team.Type.Heroes | Team.Type.Monsters);
            AccessTools.Field(typeof(CardEffectData), "paramStatusEffects").SetValue(status,
                new[] { new StatusEffectStackData { statusId = "pyregel", count = 1 } });
            effects.Add(status);
            // Strongest-last uses the first collection and ignores this conflicting team filter.
            effects.Add(Effect("CardEffectDamage", TargetMode.StrongestLastTargetedCharacters, Team.Type.Monsters));
            effects.Add(Effect("CardEffectDamage", TargetMode.BackInRoom, Team.Type.Heroes, 1));
            effects.Add(Effect("CardEffectDamage", TargetMode.LastTargetedCharacters, Team.Type.Heroes, 1));
            effects.Add(Effect("CardEffectDamage", TargetMode.Room, Team.Type.Monsters, 2));
            effects.Add(Effect("CardEffectHeal", TargetMode.FrontInRoom, Team.Type.Monsters));
            effects.Add(Effect("CardEffectHeal", TargetMode.Weakest, Team.Type.Monsters, 1));
            effects.Add(Effect("CardEffectHeal", TargetMode.RoomHealTargets, Team.Type.Monsters, 999));
            AccessTools.Field(typeof(CardData), "targetless").SetValue(data, false);
            AccessTools.Field(typeof(CardData), "targetsRoom").SetValue(data, true);
            foreach (CardState card in spells) card.Setup(data, save);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ROOM-SPELLS-PREPARED area damage/upgrade/heal, front/back/weakest and persistent last-target collections.");
        }

        private static CardEffectData Effect(string type, TargetMode mode, Team.Type team, int value = 0)
        {
            var effect = new CardEffectData(type, null!, team);
            effect.Cheat_SetTargetMode(mode);
            AccessTools.Field(typeof(CardEffectData), "paramInt").SetValue(effect, value);
            return effect;
        }
    }
}
