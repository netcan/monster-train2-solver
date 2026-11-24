using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class GoldRewardModel
    {
        internal const string MultiplierEffect = "RelicEffectChangeGoldReward";

        internal static float? OriginalMultiplier(string dataId, string assetKey) => (dataId, assetKey) switch
        {
            ("fee18e5b-e123-46c7-8fd3-0ea8f2cd47f1", "DoubleGoldEarned") => 2f,
            ("717261f5-95d2-4524-9449-cfc6f8e98a74", "HalfGoldEarned") => 0.5f,
            _ => null
        };

        internal static bool IsOriginalMultiplier(CombatRelicState relic) =>
            relic is { IsCovenant: false, DisallowedInPlacementPhase: false, EffectTypes: [MultiplierEffect] } &&
            OriginalMultiplier(relic.DataId, relic.AssetKey).HasValue;

        internal static string? Validate(CombatContext? context)
        {
            if (context?.Relics == null) return null;
            const string Missing = "Missing or inconsistent native gold reward multiplier.";
            BattleResourceState? raw = context.Resources;
            float expected = 1f;
            bool hasOwner = false;
            foreach (CombatRelicState relic in context.Relics)
            {
                if (!IsOriginalMultiplier(relic)) continue;
                if (raw == null || !raw.MutatorIds.Contains(relic.DataId)) return Missing;
                expected *= OriginalMultiplier(relic.DataId, relic.AssetKey)!.Value;
                hasOwner = true;
            }
            return hasOwner && raw!.GoldRewardMultiplier != expected ? Missing : null;
        }

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
