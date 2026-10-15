using System;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    public sealed class CampaignPurgeGoldOwner
    {
        public string Id { get; }
        public int Amount { get; }
        public string CardType { get; }
        public bool DisallowedInPlacement { get; }
        public bool CanApplyInPreview { get; }
        public CampaignPurgeGoldOwner(string id, int amount, string cardType, bool disallowedInPlacement, bool canApplyInPreview)
        { Id = id; Amount = amount; CardType = cardType; DisallowedInPlacement = disallowedInPlacement; CanApplyInPreview = canApplyInPreview; }
    }
    public sealed class CampaignPurgeGoldResult
    {
        public int Gold { get; }
        public IReadOnlyList<string> TriggeredOwners { get; }
        internal CampaignPurgeGoldResult(int gold, List<string> triggered) { Gold = gold; TriggeredOwners = triggered.AsReadOnly(); }
    }
    public static class CampaignPurgeGoldModel
    {
        // Models the actual purge callback, not deck removal or merchant payment.
        public static CampaignPurgeGoldResult Apply(int gold, IReadOnlyList<CampaignPurgeGoldOwner> owners,
            string cardType, float rewardMultiplier = 1f, bool inCombat = false, bool placement = false, bool preview = false)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            var triggered = new List<string>(); var seen = new HashSet<string>();
            foreach (var owner in owners)
            {
                if (string.IsNullOrEmpty(owner.Id) || !seen.Add(owner.Id)) throw new ArgumentException("Expected unique original purge owners.");
                if (inCombat || placement && owner.DisallowedInPlacement || preview && !owner.CanApplyInPreview ||
                    owner.CardType != "Invalid" && owner.CardType != cardType) continue;
                int amount = GoldRewardModel.Adjust(owner.Amount, roundIncrement: 1, multiplier: rewardMultiplier);
                gold = Math.Max(0, unchecked(gold + amount)); triggered.Add(owner.Id);
            }
            return new CampaignPurgeGoldResult(gold, triggered);
        }
    }
}
