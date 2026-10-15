using System;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    public static class GoldRewardModel
    {
        // Native applies the captured mutable reward rule before floor/round-five.
        public static int Adjust(int amount, bool isReward = true, int roundIncrement = 5, float multiplier = 1f,
            bool applyTrialDataModifiers = false, IReadOnlyList<int>? trialGoldPercents = null)
        {
            if (roundIncrement <= 0) throw new ArgumentOutOfRangeException(nameof(roundIncrement));
            if (!isReward || amount <= 0) return amount;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) throw new ArgumentOutOfRangeException(nameof(multiplier));
            int floored = checked((int)Math.Floor((float)amount * multiplier));
            if (applyTrialDataModifiers)
                foreach (int percent in trialGoldPercents ?? Array.Empty<int>())
                    floored = unchecked(floored + (int)((float)unchecked(floored * percent) / 100f));
            return Math.Max(roundIncrement, checked((int)Math.Round((float)floored / roundIncrement) * roundIncrement));
        }
    }
}
