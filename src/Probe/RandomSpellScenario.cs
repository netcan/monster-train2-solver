using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class RandomSpellScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Random spell fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            effects.Add(Effect("CardEffectDamage", TargetMode.RandomInRoom, Team.Type.Heroes, 2));
            var last = Effect("CardEffectAddStatusEffect", TargetMode.LastTargetedCharacters, Team.Type.Heroes);
            Set(last, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "pyregel", count = 1 } });
            effects.Add(last);
            effects.Add(Effect("CardEffectDamage", TargetMode.RandomInRoom, Team.Type.Heroes | Team.Type.Monsters, 1));
            effects.Add(Effect("CardEffectHeal", TargetMode.RandomInRoom, Team.Type.Monsters));
            var randomStatus = Effect("CardEffectAddStatusEffect", TargetMode.RandomInRoom, Team.Type.Monsters);
            Set(randomStatus, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "armor", count = 1 } });
            effects.Add(randomStatus);
            effects.Add(Effect("CardEffectDamage", TargetMode.LastTargetedCharacters, Team.Type.Heroes));
            effects.Add(Effect("CardEffectHeal", TargetMode.Room, Team.Type.Monsters, 2));
            effects.Add(Effect("CardEffectDamage", TargetMode.Room, Team.Type.Heroes, 999));
            effects.Add(Effect("CardEffectDamage", TargetMode.RandomInRoom, Team.Type.Heroes));
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("RANDOM-SPELLS-PREPARED enemy/both-team/friendly random targets, zero heals, status addition and sticky last targets; natural boss and waves retained.");
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
