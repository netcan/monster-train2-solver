using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CampaignStartingUpgradeFilter
    {
        public CardUpgradeMaskRule Rule { get; }
        public bool NativeAccepted { get; }
        public CampaignStartingUpgradeFilter(CardUpgradeMaskRule rule, bool nativeAccepted)
        { Rule = rule; NativeAccepted = nativeAccepted; }
    }

    public sealed class CampaignStartingUpgradeEffect
    {
        public string CardType { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public IReadOnlyList<CampaignStartingUpgradeFilter> Filters { get; }
        public CampaignStartingUpgradeEffect(string cardType, CardUpgradeModifier? upgrade,
            IReadOnlyList<CampaignStartingUpgradeFilter> filters)
        { CardType = cardType; Upgrade = upgrade; Filters = Array.AsReadOnly(filters.ToArray()); }
    }

    public sealed class CampaignStartingUpgradeEnhancer
    {
        public string DataId { get; }
        public string AssetKey { get; }
        public bool DlcAllowed { get; }
        public IReadOnlyList<CampaignStartingUpgradeEffect> Effects { get; }
        public CampaignStartingUpgradeEnhancer(string dataId, string assetKey, bool dlcAllowed,
            IReadOnlyList<CampaignStartingUpgradeEffect> effects)
        { DataId = dataId; AssetKey = assetKey; DlcAllowed = dlcAllowed; Effects = Array.AsReadOnly(effects.ToArray()); }
    }

    public sealed class CampaignStartingUpgradeResult
    {
        public bool Supported { get; }
        public string? UnsupportedReason { get; }
        public CardInstanceState State { get; }
        public UnityRng Rng { get; }
        public bool Applied { get; }
        public string? EnhancerId { get; }
        public int? PoolIndex { get; }
        public string? UpgradeId { get; }
        public IReadOnlyList<string> EligibleEnhancerIds { get; }
        internal CampaignStartingUpgradeResult(bool supported, string? unsupportedReason, CardInstanceState state,
            UnityRng rng, bool applied, string? enhancerId, int? poolIndex, string? upgradeId, IReadOnlyList<string> eligibleEnhancerIds)
        { Supported = supported; UnsupportedReason = unsupportedReason; State = state; Rng = rng; Applied = applied;
            EnhancerId = enhancerId; PoolIndex = poolIndex; UpgradeId = upgradeId; EligibleEnhancerIds = Array.AsReadOnly(eligibleEnhancerIds.ToArray()); }
    }

    public static class CampaignDraftStartingUpgradeModel
    {
        public static CampaignStartingUpgradeResult Apply(IReadOnlyList<CampaignStartingUpgradeEnhancer> pool,
            CardInstanceState card, CardUpgradeMaskCard cardMask, UnityRng rng, bool ownerActive = true,
            bool previewMode = false, bool canApplyInPreview = false, bool applyToSingleRandomCard = false,
            bool cardIsRandomSelection = true)
        {
            if (!ownerActive || previewMode && !canApplyInPreview || applyToSingleRandomCard && !cardIsRandomSelection)
                return Result(card, rng, Array.Empty<string>());

            bool Accepts(CampaignStartingUpgradeEnhancer enhancer)
            {
                if (!enhancer.DlcAllowed) return false;
                foreach (CampaignStartingUpgradeEffect effect in enhancer.Effects)
                {
                    if (effect.CardType != cardMask.CardType) return false;
                    foreach (CampaignStartingUpgradeFilter filter in effect.Filters)
                        if (!CardUpgradeMaskModel.FilterCard(filter.Rule, cardMask)) return false;
                }
                return true;
            }

            var eligible = pool.Select((item, index) => (item, index)).Where(pair => Accepts(pair.item)).ToArray();
            string[] eligibleIds = eligible.Select(pair => pair.item.DataId).ToArray();
            if (eligible.Length == 0) return Result(card, rng, eligibleIds);

            RngDraw choice = rng.Range(0, eligible.Length);
            int selectedPoolIndex = eligible[choice.Value].index;
            CampaignStartingUpgradeEnhancer selected = eligible[choice.Value].item;
            if (selected.Effects.Count == 0)
                return Unsupported(card, choice.State, selected.DataId, selectedPoolIndex, eligibleIds, "Selected enhancer has no effects.");
            CardUpgradeModifier? upgrade = selected.Effects[0].Upgrade;
            if (upgrade == null)
                return Unsupported(card, choice.State, selected.DataId, selectedPoolIndex, eligibleIds, "Selected enhancer has no first-effect upgrade.");
            if (upgrade.ExternalInteractions.Count != 0)
                return Unsupported(card, choice.State, selected.DataId, selectedPoolIndex, eligibleIds, "Selected upgrade has external interactions.");
            if (upgrade.Lifecycle?.ReplacedAssets.Count > 0)
                return Unsupported(card, choice.State, selected.DataId, selectedPoolIndex, eligibleIds, "Selected upgrade replaces existing upgrades.");

            int multiplier = CardMagicPowerModel.Multiplier(card);
            CardUpgradeModifier scaled = CardMagicPowerModel.Scale(upgrade, multiplier);
            if (scaled.Unique && card.Permanent.Upgrades.Any(item => item.DataId == scaled.DataId))
                return new CampaignStartingUpgradeResult(true, null, card, choice.State, false,
                    selected.DataId, selectedPoolIndex, scaled.DataId, eligibleIds);
            CardInstanceState changed = card.WithPermanentUpgrade(scaled, false);
            return new CampaignStartingUpgradeResult(true, null, changed, choice.State, true,
                selected.DataId, selectedPoolIndex, scaled.DataId, eligibleIds);
        }

        private static CampaignStartingUpgradeResult Result(CardInstanceState card, UnityRng rng, IReadOnlyList<string> eligible) =>
            new CampaignStartingUpgradeResult(true, null, card, rng, false, null, null, null, eligible);
        private static CampaignStartingUpgradeResult Unsupported(CardInstanceState card, UnityRng rng, string enhancer,
            int poolIndex, IReadOnlyList<string> eligible, string reason) =>
            new CampaignStartingUpgradeResult(false, reason, card, rng, false, enhancer, poolIndex, null, eligible);
    }
}
