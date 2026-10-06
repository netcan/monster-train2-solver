using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class PostKillSpellScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Post-kill fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            // The natural boss is behind the guard: kill it, then damage the remaining targets.
            effects.Add(Effect("CardEffectDamage", TargetMode.BackInRoom, Team.Type.Heroes, 999));
            effects.Add(Effect("CardEffectDamage", TargetMode.Room, Team.Type.Heroes | Team.Type.Monsters, 2));
            effects.Add(Effect("CardEffectDamage", TargetMode.Room, Team.Type.Heroes, 999));
            var status = Effect("CardEffectAddStatusEffect", TargetMode.Room, Team.Type.Monsters);
            Set(status, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "armor", count = 1 } });
            effects.Add(status);
            CardUpgradeData permanent = Upgrade("Permanent", 1, 4);
            CardUpgradeData temporary = Upgrade("Temporary", 2, 3);
            CardUpgradeData skipped = Upgrade("SkippedHand", 0, 0);
            CardUpgradeData canceled = Upgrade("CanceledTail", 0, 0);
            effects.Add(UpgradeEffect("CardEffectAddCardUpgradeToUnits", permanent, UnitUpgradeLifetime.Permanent));
            effects.Add(UpgradeEffect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            effects.Add(UpgradeEffect("CardEffectRemoveTempUpgradeFromUnit", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            effects.Add(Effect("CardEffectHeal", TargetMode.RoomHealTargets, Team.Type.Monsters, 999));
            effects.Add(Hand(skipped, false));
            effects.Add(Effect("CardEffectDamage", TargetMode.Room, Team.Type.Monsters, 1));
            effects.Add(Hand(skipped, true));
            effects.Add(UpgradeEffect("CardEffectAddTempCardUpgradeToUnits", canceled, UnitUpgradeLifetime.TemporaryUntilUnitDeath));
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("POST-KILL-SPELLS-PREPARED boss damage followed by room damage/status/upgrades/removal/healing and hand-effect skip/cancel; natural bosses and waves retained.");
        }

        private static CardUpgradeData Upgrade(string name, int damage, int health)
        {
            string suffix = name == "Permanent" ? "1" : name == "Temporary" ? "2" : name == "SkippedHand" ? "3" : "4";
            return DynamicUpgradeScenario.Upgrade("PojuPostKill" + name,
                "8a430eea-6eea-4d89-9404-4b334db0000" + suffix, damage, 0, 0, health, "armor", 0);
        }
        private static CardEffectData UpgradeEffect(string type, CardUpgradeData upgrade, UnitUpgradeLifetime lifetime)
        {
            CardEffectData effect = Effect(type, TargetMode.Room, Team.Type.Monsters);
            Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)lifetime);
            return effect;
        }
        private static CardEffectData Hand(CardUpgradeData upgrade, bool cancel)
        {
            CardEffectData effect = Effect("CardEffectAddTempCardUpgradeToCardsInHand", TargetMode.Hand, Team.Type.Monsters);
            Set(effect, "paramCardUpgradeData", upgrade);
            Set(effect, "shouldCancelSubsequentEffectsIfTestFails", cancel);
            return effect;
        }
        private static CardEffectData Effect(string type, TargetMode target, Team.Type team, int value = 0)
        {
            var effect = new CardEffectData(type, null!, team);
            effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value);
            return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
