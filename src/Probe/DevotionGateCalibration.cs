using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionGateCalibration
    {
        internal static object Run(AllGameManagers managers, CardEffectData sermon)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            SaveManager save = managers.GetSaveManager();
            TrainCombatState original = trace.CaptureTrain();
            object originalRng = HalfHealthLifecycleCalibration.Rng();
            DevotionState resource = original.Context!.QueryFrame!.Devotion!;
            object data = AccessTools.Property(typeof(SaveManager), "ActiveSaveData").GetValue(save);
            object stats = AccessTools.Field(data.GetType(), "runStats").GetValue(data);
            object shadow = AccessTools.Field(data.GetType(), "runStats").GetValue(((SaveData)data).CreatePreviewCopy());
            var streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().ToDictionary(id => id,
                id => new HadesRNG((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id })));
            UnityEngine.Random.State unity = UnityEngine.Random.state;
            var samples = new List<object>();
            try
            {
                AccessTools.Field(data.GetType(), "runStats").SetValue(data, shadow);
                foreach (int level in new[] { 0, 1, 2, 3 })
                foreach (var bounds in new[] { (0, 0), (1, 0), (0, 2), (1, 2), (2, 2), (3, 1) })
                {
                    var effect = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters);
                    effect.Cheat_SetTargetMode(TargetMode.Room);
                    Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "zeal", count = 3 } });
                    Set(effect, "devotionRequirement", bounds.Item1); Set(effect, "devotionRequirementMax", bounds.Item2);
                    Record(level, effect, false);
                }
                var negative = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters);
                negative.Cheat_SetTargetMode(TargetMode.Room);
                Set(negative, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "zeal", count = 3 } });
                Set(negative, "devotionRequirement", -1); Set(negative, "devotionRequirementMax", -2);
                Record(0, negative, false);
                Record(0, sermon, true); Record(1, sermon, true);
            }
            finally
            {
                SetLevel(resource.Level, resource.Points);
                AccessTools.Field(data.GetType(), "runStats").SetValue(data, stats);
                foreach (var pair in streams)
                    ((HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { pair.Key })).Init(pair.Value);
                UnityEngine.Random.state = unity;
            }
            string? restoration = HalfHealthLifecycleCalibration.Difference(original, trace.CaptureTrain()) ??
                HalfHealthLifecycleCalibration.Difference(originalRng, HalfHealthLifecycleCalibration.Rng());
            if (restoration != null) throw new InvalidOperationException("Devotion gate restoration: " + restoration);
            return new { Completed = true, LiveContextUnchanged = true, RestorationDifference = restoration,
                RngBefore = originalRng, RngAfter = HalfHealthLifecycleCalibration.Rng(), Samples = samples };

            void Record(int level, CardEffectData effectData, bool originalSermon)
            {
                SetLevel(level, 0);
                TrainCombatState before = trace.CaptureTrain(); object rngBefore = HalfHealthLifecycleCalibration.Rng();
                var interactions = new List<string>();
                CardActionEffect action = BattleActionProbe.DescribeTriggerEffect(effectData, interactions)!;
                if (interactions.Count != 0) throw new InvalidOperationException(string.Join(";", interactions));
                var effect = new CardEffectState(); effect.Setup(effectData, Team.Type.Monsters, save);
                var parameters = new CardEffectParams { selectedRoom = 2 };
                bool native = GameEffectHelper.TestEffect(effect, parameters, managers.GetCoreManagers(), false);
                SpellCastCheck model = CardSpellModel.TestPlay(before, 2, new[] { action }, 0);
                TrainCombatState after = trace.CaptureTrain(); object rngAfter = HalfHealthLifecycleCalibration.Rng();
                string? difference = !model.Supported ? model.UnsupportedReason : model.CanPlay != native ? "Admission differs." :
                    HalfHealthLifecycleCalibration.Difference(before, after) ??
                    HalfHealthLifecycleCalibration.Difference(rngBefore, rngAfter);
                samples.Add(new { Level = level, Minimum = DevotionProbe.Requirement(effectData), Maximum = DevotionProbe.Requirement(effectData, true),
                    OriginalSermon = originalSermon, Action = action, Before = before, Actual = after, NativeTest = native,
                    RngBefore = rngBefore, RngAfter = rngAfter, Difference = difference, Completed = true });
                if (difference != null) throw new InvalidOperationException("Devotion gate " + level + ": " + difference);
            }
            void SetLevel(int level, int points)
            {
                AccessTools.Method(typeof(SaveManager), "SetDevotionLevel").Invoke(save, new object[] { level });
                AccessTools.Method(typeof(SaveManager), "SetDevotionPoints").Invoke(save,
                    new object[] { points, DevotionModel.Threshold(new DevotionState(level, points, resource.FirstThreshold,
                        resource.ThresholdIncrease, resource.PassiveStacksPerLevel)) });
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
