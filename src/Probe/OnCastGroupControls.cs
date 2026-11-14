using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OnCastGroupControls
    {
        internal static CardData Create(AllGameData all, int mode, int amount, int rangeCase, Team.Type team, int cost = 2)
        {
            var data = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId));
            void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
            data.name = "PojuOnCastGroups" + mode + "_" + amount;
            Set(data, "id", "fc231b70-0000-4000-8000-" + (rangeCase * 100000000 + mode * 1000000 + amount).ToString("D12"));
            Set(data, "cost", cost);
            CardEffectData Effect(int value, TargetMode target = TargetMode.DropTargetCharacter, bool heal = false, bool fail = false, bool both = false)
            {
                var effect = new CardEffectData(heal ? "CardEffectHeal" : "CardEffectDamage", null!,
                    target == TargetMode.Room || both ? Team.Type.Heroes | Team.Type.Monsters : team);
                effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value);
                if (rangeCase != 0 || fail)
                {
                    Set(effect, "useIntRange", true); Set(effect, "paramMinInt", 0); Set(effect, "paramMaxInt", fail ? 0 : value + 7);
                    Set(effect, "paramMultiplier", fail ? 1f : .75f);
                }
                if (fail) Set(effect, "shouldCancelSubsequentEffectsIfTestFails", true);
                return effect;
            }
            var main = new List<CardEffectData> { Effect(1, mode is 28 or 31 ? TargetMode.Room : TargetMode.DropTargetCharacter, fail: mode == 27) };
            if (mode == 27) main.Add(Effect(amount, TargetMode.Room));
            var groups = new List<CardEffectData[]>();
            switch (mode)
            {
                case 23: groups.Add(new[] { Effect(1), Effect(amount, heal: true) }); break;
                case 24: groups.Add(new[] { Effect(1, TargetMode.Room) }); groups.Add(new[] { Effect(amount, heal: true) }); break;
                case 25: groups.Add(new[] { Effect(1, fail: true), Effect(amount, TargetMode.Room) }); groups.Add(new[] { Effect(amount, heal: true) }); break;
                case 26: groups.Add(new[] { Effect(1, fail: true) }); groups.Add(new[] { Effect(1, TargetMode.Room), Effect(amount, heal: true) }); break;
                case 27: groups.Add(new[] { Effect(amount, heal: true) }); groups.Add(new[] { Effect(1, TargetMode.Room) }); break;
                case 28: groups.Add(new[] { Effect(1, heal: true) }); groups.Add(new[] { Effect(amount, TargetMode.Room) }); break;
                case 29: groups.Add(new[] { Effect(1, TargetMode.Room) }); groups.Add(new[] { Effect(1, fail: true), Effect(amount, TargetMode.Room) }); groups.Add(new[] { Effect(amount, heal: true) }); break;
                case 30: groups.Add(new[] { Effect(1, TargetMode.Room, heal: true) }); groups.Add(new[] { Effect(amount, TargetMode.LastTargetedCharacters) }); break;
                case 31: groups.Add(new[] { Effect(1, heal: true) }); groups.Add(new[] { Effect(amount, TargetMode.LastTargetedCharacters, both: true) }); break;
                default: throw new System.ArgumentOutOfRangeException(nameof(mode));
            }
            Set(data, "effects", main);
            Set(data, "triggers", groups.Select(effects =>
            {
                var trigger = new CardTriggerEffectData(); Set(trigger, "trigger", CardTriggerType.OnCast);
                Set(trigger, "cardEffects", effects.ToList()); return trigger;
            }).ToList());
            Set(data, "traits", new List<CardTraitData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
            Set(data, "startingUpgrades", new List<CardUpgradeData>()); Set(data, "targetless", false); Set(data, "targetsRoom", true);
            return data;
        }
    }
}
