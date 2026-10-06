using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class RngCalibration
    {
        internal static void Capture()
        {
            UnityEngine.Random.State external = UnityEngine.Random.state;
            var samples = new List<object>();
            try
            {
                foreach (int seed in new[] { 0, 1, -1, 424242, int.MinValue, int.MaxValue })
                {
                    UnityEngine.Random.InitState(seed);
                    UnityEngine.Random.State initial = UnityEngine.Random.state;
                    var draws = new List<object>();
                    foreach ((int min, int max) in new[] { (0, 1), (0, 2), (0, 7), (-10, 11),
                        (0, int.MaxValue), (int.MinValue, int.MaxValue), (5, 5), (10, -10) })
                    {
                        for (int index = 0; index < 16; index++)
                        {
                            uint[] before = Words(UnityEngine.Random.state);
                            int value = UnityEngine.Random.Range(min, max);
                            draws.Add(new { Min = min, Max = max, Before = before, Value = value,
                                After = Words(UnityEngine.Random.state) });
                        }
                    }
                    samples.Add(new { Seed = seed, Initial = Words(initial), Draws = draws });
                }
            }
            finally { UnityEngine.Random.state = external; }
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "rng-calibration.json"), JsonConvert.SerializeObject(new
                {
                    GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                    GlobalStateRestored = Equals(external, UnityEngine.Random.state), Samples = samples
                }, Formatting.Indented));
        }

        internal static uint[] Words(UnityEngine.Random.State state)
        {
            var words = new uint[4];
            for (int index = 0; index < words.Length; index++)
            {
                FieldInfo field = typeof(UnityEngine.Random.State).GetField("s" + index,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException("Unity random state s" + index);
                words[index] = unchecked((uint)Convert.ToInt32(field.GetValue(state)));
            }
            return words;
        }
    }
}
