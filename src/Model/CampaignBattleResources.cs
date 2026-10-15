using System;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    public sealed class CampaignBattleResourceOwner
    {
        public string Id { get; }
        public string Resource { get; }
        public int Amount { get; }
        public bool RequireNoPyreDamage { get; }
        public bool PyreWasDamaged { get; }
        public CampaignBattleResourceOwner(string id, string resource, int amount, bool requireNoPyreDamage, bool pyreWasDamaged = false)
        { Id = id; Resource = resource; Amount = amount; RequireNoPyreDamage = requireNoPyreDamage; PyreWasDamaged = pyreWasDamaged; }
        internal CampaignBattleResourceOwner WithDamage(bool damaged) => new CampaignBattleResourceOwner(Id, Resource, Amount, RequireNoPyreDamage, damaged);
    }
    public sealed class CampaignBattleResources
    {
        public int Gold { get; }
        public int Hoard { get; }
        public int HoardCap { get; }
        public CampaignBattleResources(int gold, int hoard, int hoardCap) { Gold = gold; Hoard = hoard; HoardCap = hoardCap; }
    }

    public static class CampaignBattleResourceModel
    {
        public static CampaignBattleResourceOwner StartBattle(CampaignBattleResourceOwner owner) =>
            owner.Amount == 0 ? owner : owner.WithDamage(false);

        // The native Test callback records damage even though it always returns false.
        public static CampaignBattleResourceOwner Observe(CampaignBattleResourceOwner owner,
            string trigger, bool isPyre, int damage) => owner.RequireNoPyreDamage &&
            trigger == "OnHit" && isPyre && damage > 0 ? owner.WithDamage(true) : owner;

        public static CampaignBattleResources EndBattle(CampaignBattleResources state,
            IReadOnlyList<CampaignBattleResourceOwner> owners, float goldRewardMultiplier = 1f)
        {
            if (state.Gold < 0 || state.HoardCap < 0 || state.Hoard < 0 || state.Hoard > state.HoardCap)
                throw new ArgumentOutOfRangeException(nameof(state));
            var seen = new HashSet<string>();
            foreach (var owner in owners)
            {
                if (string.IsNullOrEmpty(owner.Id) || !seen.Add(owner.Id) ||
                    (owner.Resource != "Gold" && owner.Resource != "Hoard"))
                    throw new ArgumentException("Expected unique, supported campaign resource owners.");
                if (owner.Amount == 0 || owner.RequireNoPyreDamage && owner.PyreWasDamaged) continue;
                state = owner.Resource == "Gold"
                    ? new CampaignBattleResources(Math.Max(0, unchecked(state.Gold + GoldRewardModel.Adjust(owner.Amount, multiplier: goldRewardMultiplier))), state.Hoard, state.HoardCap)
                    : new CampaignBattleResources(state.Gold, Math.Min(state.HoardCap, Math.Max(0, unchecked(state.Hoard + owner.Amount))), state.HoardCap);
            }
            return state;
        }
    }
}
