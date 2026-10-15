using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CampaignFixedReward
    {
        public string Id { get; }
        public string Key { get; }
        public string Type { get; }
        public CampaignFixedReward(string id, string key, string type) { Id = id; Key = key; Type = type; }
    }
    public sealed class CampaignPostBattleOwner
    {
        public string OwnerId { get; }
        public CampaignFixedReward Reward { get; }
        public CampaignPostBattleOwner(string ownerId, CampaignFixedReward reward) { OwnerId = ownerId; Reward = reward; }
    }
    // The actual post-battle reward-collection consumer. This covers direct
    // GrantableRewardData assets; pool generation and accepting/granting a reward
    // are separate consumers. Placement and preview gates are absent in native.
    public static class CampaignPostBattleRewardModel
    {
        public static IReadOnlyList<CampaignFixedReward> Collect(IReadOnlyList<CampaignFixedReward> existing,
            IReadOnlyList<CampaignPostBattleOwner> owners)
        {
            if (owners.Select(owner => owner.OwnerId).Distinct().Count() != owners.Count ||
                owners.Any(owner => string.IsNullOrEmpty(owner.OwnerId)) ||
                existing.Concat(owners.Select(owner => owner.Reward)).Any(reward =>
                    string.IsNullOrEmpty(reward.Id) || string.IsNullOrEmpty(reward.Key) ||
                    reward.Type is not ("PurgeRewardData" or "EnhancerRewardData")))
                throw new ArgumentException("Unsupported fixed post-battle reward collection.");
            // Native AddRange preserves existing duplicates and relic-manager order;
            // the asset's unrelated Int/Int2 parameters do not change the rewards.
            return Array.AsReadOnly(existing.Concat(owners.Select(owner => owner.Reward)).ToArray());
        }
    }
}
