using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionStatusGateCalibration
    {
        internal static object Run(AllGameManagers managers)
        {
            var trace = FullBattleTrace.Active!; var save = managers.GetSaveManager();
            TrainCombatState original = trace.CaptureTrain(); object rng = HalfHealthLifecycleCalibration.Rng();
            DevotionState resource = original.Context!.QueryFrame!.Devotion!;
            object data = AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            object stats = AccessTools.Field(data.GetType(), "runStats").GetValue(data);
            object shadow = AccessTools.Field(data.GetType(), "runStats").GetValue(((SaveData)data).CreatePreviewCopy());
            var samples = new List<object>();
            try
            {
                AccessTools.Field(data.GetType(), "runStats").SetValue(data, shadow);
                foreach (int level in new[] { 0, 1, 2, 3 })
                foreach (int divisor in new[] { -2, 0, 1, 2, 4 })
                {
                    SetLevel(level, 0);
                    var definition = new CardEffectData("CardEffectAddStatusEffectPerDevotionLevel", null!, Team.Type.Monsters);
                    definition.Cheat_SetTargetMode(TargetMode.Room);
                    AccessTools.Field(typeof(CardEffectData), "additionalParamInt").SetValue(definition, divisor);
                    AccessTools.Field(typeof(CardEffectData), "paramStatusEffects").SetValue(definition,
                        new[] { new StatusEffectStackData { statusId = "zeal", count = 2 } });
                    var interactions = new List<string>();
                    CardActionEffect action = BattleActionProbe.DescribeTriggerEffect(definition, interactions);
                    if (interactions.Count != 0) throw new InvalidOperationException(string.Join(";", interactions));
                    var effect = new CardEffectState(); effect.Setup(definition, Team.Type.Monsters, save);
                    TrainCombatState before = trace.CaptureTrain(); object rngBefore = HalfHealthLifecycleCalibration.Rng();
                    bool native = GameEffectHelper.TestEffect(effect, new CardEffectParams { selectedRoom = 2 }, managers.GetCoreManagers(), false);
                    SpellCastCheck model = CardSpellModel.TestPlay(before, 2, new[] { action }, 0);
                    TrainCombatState after = trace.CaptureTrain(); object rngAfter = HalfHealthLifecycleCalibration.Rng();
                    string? difference = !model.Supported ? model.UnsupportedReason : model.CanPlay != native ? "Admission differs." :
                        HalfHealthLifecycleCalibration.Difference(before, after) ?? HalfHealthLifecycleCalibration.Difference(rngBefore, rngAfter);
                    samples.Add(new { Level = level, Divisor = divisor, Action = action, Before = before, Actual = after,
                        NativeTest = native, RngBefore = rngBefore, RngAfter = rngAfter, Difference = difference, Completed = true });
                    if (difference != null) throw new InvalidOperationException("Devotion status gate: " + difference);
                }
            }
            finally
            {
                SetLevel(resource.Level, resource.Points);
                AccessTools.Field(data.GetType(), "runStats").SetValue(data, stats);
            }
            string? restoration = HalfHealthLifecycleCalibration.Difference(original, trace.CaptureTrain()) ??
                HalfHealthLifecycleCalibration.Difference(rng, HalfHealthLifecycleCalibration.Rng());
            if (restoration != null) throw new InvalidOperationException("Devotion status gate restoration: " + restoration);
            return new { Completed = true, LiveContextUnchanged = true, RestorationDifference = restoration, Samples = samples };
            void SetLevel(int level, int points)
            {
                AccessTools.Method(typeof(SaveManager), "SetDevotionLevel").Invoke(save, new object[] { level });
                AccessTools.Method(typeof(SaveManager), "SetDevotionPoints").Invoke(save, new object[] { points,
                    DevotionModel.Threshold(new DevotionState(level, points, resource.FirstThreshold, resource.ThresholdIncrease, resource.PassiveStacksPerLevel)) });
            }
        }
    }
}
