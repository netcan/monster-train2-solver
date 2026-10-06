using System;

namespace MonsterTrain2Poju.Model
{
    public static class GoldRewardModel
    {
        // Relic/mutator reward multipliers are rejected by the native state capture.
        public static int Adjust(int amount, bool isReward = true, int roundIncrement = 5)
        {
            if (roundIncrement <= 0) throw new ArgumentOutOfRangeException(nameof(roundIncrement));
            if (!isReward || amount <= 0) return amount;
            int floored = checked((int)Math.Floor((float)amount));
            return Math.Max(roundIncrement, checked((int)Math.Round((float)floored / roundIncrement) * roundIncrement));
        }
    }
}
