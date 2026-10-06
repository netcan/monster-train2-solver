using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class RandomStatusScenario
    {
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Random status fixture requires the owned rearrangement spell.");
            CardState steward = owned.First(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678");
            var immune = new CardUpgradeState(); immune.Setup(); immune.AddStatusEffectUpgradeStacks("immune", 1);
            steward.ApplyPermanentUpgrade(immune, save, ignoreUpgradeAnimation: true);
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            // A zero-damage enemy opener makes the verification policy choose an occupied enemy room.
            var opener = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes);
            opener.Cheat_SetTargetMode(TargetMode.Room); effects.Add(opener);
            effects.Add(Status(Team.Type.Monsters, 0, Stack("armor", 2), Stack("regen", 1)));
            effects.Add(Status(Team.Type.Monsters, 50, Stack("armor", 1)));
            effects.Add(Status(Team.Type.Monsters, 100, Stack("regen", 1)));
            effects.Add(Status(Team.Type.Heroes, 50, Stack("armor", 2), Stack("pyregel", 1)));
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes);
            damage.Cheat_SetTargetMode(TargetMode.Room); Set(damage, "paramInt", 999); effects.Add(damage);
            // Empty targets still select the pool entry, but have no per-target chance draws.
            effects.Add(Status(Team.Type.Heroes, 50, Stack("armor", 2), Stack("pyregel", 1)));
            effects.Add(Status(Team.Type.Monsters, 50, Stack("armor", 1)));
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("RANDOM-STATUS-PREPARED effect-wide pools, zero/half/full probabilities, reverse target order, immune spawner and empty/post-kill collections; natural boss and waves retained.");
        }

        private static CardEffectData Status(Team.Type team, int chance, params StatusEffectStackData[] statuses)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, team);
            effect.Cheat_SetTargetMode(TargetMode.Room);
            Set(effect, "paramInt", chance); Set(effect, "paramStatusEffects", statuses);
            return effect;
        }
        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
