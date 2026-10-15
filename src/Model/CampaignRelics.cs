using System;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    // Campaign consumers are separate from battle-turn resolution. These APIs
    // model the unchanged unrestricted slot/all-cost owners, not subtype/reward
    // filters belonging to other, unvalidated effect classes.
    public static class CampaignRelicModel
    {
        public static int UpgradeSlots(string cardType, int baseSlots, int maximum, int cardModifier,
            IReadOnlyList<string> bonusCardTypes)
        {
            if (cardType != "Monster" && cardType != "Spell" && cardType != "Equipment") return 0;
            if (maximum < 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            int value = unchecked(baseSlots + cardModifier);
            foreach (string type in bonusCardTypes) if (type == cardType) value = unchecked(value + 1);
            return Math.Min(maximum, Math.Max(0, value));
        }

        public static int MerchantCost(IReadOnlyList<int> costs, int purchases, IReadOnlyList<float> additionalMultipliers,
            float merchantDiscount = 0, bool discountAllowed = true)
        {
            if (costs.Count == 0) return 0;
            if (purchases < 0) throw new ArgumentOutOfRangeException(nameof(purchases));
            float multiplier = 1f;
            foreach (float additional in additionalMultipliers) multiplier += additional;
            int value = Math.Max(0, checked((int)Math.Ceiling((float)costs[Math.Min(purchases, costs.Count - 1)] * multiplier)));
            if (merchantDiscount > float.Epsilon && discountAllowed)
                value = checked((int)Math.Ceiling((float)value * (1f - merchantDiscount)));
            return checked((int)Math.Round((float)value / 5f) * 5);
        }

        public static int AcquireHoardCap(int current, int change, int maximum) =>
            Math.Min(maximum, Math.Max(0, unchecked(current + change)));

        public static (int Cap, int Amount) RemoveHoardCap(int currentCap, int currentAmount, int change, int maximum)
        {
            int cap = AcquireHoardCap(currentCap, unchecked(-change), maximum);
            return (cap, Math.Min(currentAmount, cap));
        }
    }
}
