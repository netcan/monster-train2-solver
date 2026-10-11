using System;

namespace MonsterTrain2Poju.Model
{
    public static class GoldRewardModel
    {
        // Native applies the captured mutable reward rule before floor/round-five.
        public static int Adjust(int amount, bool isReward = true, int roundIncrement = 5, float multiplier = 1f)
        {
            if (roundIncrement <= 0) throw new ArgumentOutOfRangeException(nameof(roundIncrement));
            if (!isReward || amount <= 0) return amount;
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) throw new ArgumentOutOfRangeException(nameof(multiplier));
            int floored = checked((int)Math.Floor((float)amount * multiplier));
            return Math.Max(roundIncrement, checked((int)Math.Round((float)floored / roundIncrement) * roundIncrement));
        }
    }
}
