using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;

namespace MonsterTrain2Poju.Probe
{
    internal static class GoldRewardCalibration
    {
        internal static void Capture(SaveManager save)
        {
            int originalGold = save.GetGold();
            var samples = new List<object>();
            foreach (int amount in Enumerable.Range(-20, 271).Concat(new[] { 8388607, 8388617, 16777215, 16777217 }))
            foreach (bool reward in new[] { false, true })
            foreach (int increment in new[] { 1, 2, 5, 10 })
            {
                int actual = save.GetAdjustedGoldAmount(amount, reward, roundIncrement: increment);
                if (actual != GoldRewardModel.Adjust(amount, reward, increment))
                    throw new InvalidOperationException("Native gold reward calibration differs.");
                samples.Add(new { Amount = amount, IsReward = reward, RoundIncrement = increment, Actual = actual });
            }
            if (save.GetGold() != originalGold) throw new InvalidOperationException("Gold calibration changed the balance.");
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "gold-reward-calibration.json"),
                JsonConvert.SerializeObject(new { Schema = 1, Samples = samples }, Formatting.Indented));
        }
    }
}
