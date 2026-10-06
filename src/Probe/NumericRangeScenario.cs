using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class NumericRangeScenario
    {
        internal static readonly List<SampleRecord> Samples = new List<SampleRecord>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool lethal)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card =>
                card.GetCardType() == CardType.Spell && card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Range fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            effects.Add(Range("CardEffectDamage", TargetMode.Tower, Team.Type.Heroes, 0, 7, .75f));
            effects.Add(Range("CardEffectBuffDamage", TargetMode.Tower, Team.Type.Monsters, 1, 4, 1.5f));
            effects.Add(Range("CardEffectDebuffDamage", TargetMode.Tower, Team.Type.Monsters, 0, 2, .5f));
            effects.Add(Range("CardEffectBuffMaxHealth", TargetMode.Tower, Team.Type.Monsters, -3, 2, .5f));
            effects.Add(Range("CardEffectBuffMaxHealth", TargetMode.Tower, Team.Type.Monsters, 1, 4, .75f));
            effects.Add(Range("CardEffectDebuffMaxHealth", TargetMode.Tower, Team.Type.Heroes, 0, 2, .5f));
            effects.Add(Range("CardEffectHeal", TargetMode.Tower, Team.Type.Monsters, 0, 7, .75f));
            effects.Add(Range("CardEffectHeal", TargetMode.Tower, Team.Type.Monsters, 2, 2, -.5f));
            effects.Add(Status(TargetMode.Room, Team.Type.Heroes | Team.Type.Monsters, 5, 105));
            effects.Add(Status(TargetMode.Tower, Team.Type.Heroes, 0, 0));
            effects.Add(Range("CardEffectDamage", TargetMode.RandomFromAnyRoom, Team.Type.Heroes, 0, 7, .75f));
            effects.Add(Range("CardEffectDamage", TargetMode.Tower, Team.Type.Heroes, 0, 0, .75f));
            effects.Add(Range("CardEffectDamage", TargetMode.Tower, Team.Type.Heroes, 0, 7, -.5f));
            // Every ranged damage test runs before the post-boss gate; rejected effects still draw.
            if (lethal) effects.Add(Range("CardEffectDamage", TargetMode.Tower, Team.Type.Heroes, 10000, 10001));
            effects.Add(Status(TargetMode.Tower, Team.Type.Heroes, -5, 5));
            effects.Add(Range("CardEffectBuffMaxHealth", TargetMode.Tower, Team.Type.Monsters, -3, 2, .5f));
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells)
            {
                card.Setup(data, save);
                var permanent = new CardUpgradeState(); permanent.Setup(); permanent.SetAttackDamage(-3); permanent.SetAdditionalHeal(-5);
                card.ApplyPermanentUpgrade(permanent, save, ignoreUpgradeAnimation: true);
                var temporary = new CardUpgradeState(); temporary.Setup(); temporary.SetAttackDamage(2); temporary.SetAdditionalHeal(8);
                card.ApplyTemporaryUpgrade(temporary, save);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("NUMERIC-RANGES-PREPARED shared quantities, casting/runtime tests, endpoint upgrades, fractional/signed/equal ranges, random targets, status pool/chances and empty targets; lethal=" + lethal + ".");
        }

        private static CardEffectData Range(string type, TargetMode target, Team.Type team, int min, int max, float multiplier = 1f)
        {
            var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(target);
            Set(effect, "useIntRange", true); Set(effect, "paramMinInt", min); Set(effect, "paramMaxInt", max);
            Set(effect, "paramMultiplier", multiplier); return effect;
        }
        private static CardEffectData Status(TargetMode target, Team.Type team, int min, int max)
        {
            CardEffectData effect = Range("CardEffectAddStatusEffect", target, team, min, max);
            Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "armor", count = 1 },
                new StatusEffectStackData { statusId = "regen", count = 1 } });
            return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static UnityRng Rng()
        {
            uint[] words = RngCalibration.Words(RandomManager.GetState(RngId.Battle));
            return new UnityRng(words[0], words[1], words[2], words[3]);
        }
        internal sealed class SampleRecord
        {
            public int ActionIndex { get; set; }
            public int TurnIndex { get; set; }
            public int CardId { get; set; }
            public int EffectIndex { get; set; }
            public string Phase { get; set; } = "";
            public string[] Callers { get; set; } = Array.Empty<string>();
            public int Min { get; set; }
            public int Max { get; set; }
            public float Multiplier { get; set; }
            public int Value { get; set; }
            public UnityRng Before { get; set; }
            public UnityRng After { get; set; }
        }
        [HarmonyPatch(typeof(CardEffectState), nameof(CardEffectState.GetIntInRange))]
        private static class SamplePatch
        {
            private static void Prefix(CardEffectState __instance, out SampleRecord? __state)
            {
                __state = null;
                string? scenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS");
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (scenario != "numeric-ranges" && scenario != "numeric-ranges-lethal" && scenario != "drawing" || !__instance.GetUseIntRange() ||
                    trace == null || !trace.PendingActionIndex.HasValue && !trace.PendingTurnIndex.HasValue || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                CardState? parent = __instance.GetParentCardState();
                if (parent == null) return;
                var callers = new StackTrace().GetFrames().Select(frame => frame.GetMethod()).ToArray();
                string phase = callers.Any(method => method?.DeclaringType == typeof(CardUI)) ? "Highlight" :
                    callers.Any(method => method?.Name == "TestEffects" && method.DeclaringType == typeof(CombatManager)) ? "Cast" :
                    callers.Any(method => method?.Name == "TestEffect" && (method.DeclaringType == typeof(CardEffectDamage) ||
                        method.DeclaringType == typeof(CardEffectDraw))) ? "Test" : "Apply";
                __state = new SampleRecord { ActionIndex = trace.PendingActionIndex ?? -1, TurnIndex = trace.PendingTurnIndex ?? -1,
                    CardId = trace.CardId(parent), EffectIndex = parent.GetEffectStates().IndexOf(__instance),
                    Phase = phase, Min = __instance.GetParamMinInt(), Max = __instance.GetParamMaxInt(), Multiplier = __instance.GetParamMultiplier(), Before = Rng(),
                    Callers = callers.Select(method => method?.DeclaringType?.FullName + "." + method?.Name).ToArray() };
                if (phase == "Cast" && __state.EffectIndex == 0)
                    UnityEngine.Debug.Log("NUMERIC-RANGE-CAST action=" + __state.ActionIndex + " callers=" + string.Join(" > ", __state.Callers));
            }
            private static void Postfix(int __result, SampleRecord? __state)
            {
                if (__state == null) return;
                __state.Value = __result; __state.After = Rng(); Samples.Add(__state);
            }
        }
    }
}
