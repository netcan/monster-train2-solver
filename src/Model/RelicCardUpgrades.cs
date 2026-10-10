using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // The card side of RelicEffectAddTempUpgrade. The enclosing relic manager
    // owns conditions, ordering, reset scheduling and trigger notifications.
    public sealed class RelicCardUpgradeRule
    {
        public string AssetKey { get; }
        public bool SourceMonsters { get; }
        public bool SourceHeroes { get; }
        public CardLifecycleUpgrade? Upgrade { get; }
        public IReadOnlyList<CardUpgradeMaskRule> Filters { get; }
        public RelicCardUpgradeRule(string assetKey, bool sourceMonsters, CardLifecycleUpgrade? upgrade,
            IReadOnlyList<CardUpgradeMaskRule> filters, bool sourceHeroes = false)
        { AssetKey = assetKey; SourceMonsters = sourceMonsters; SourceHeroes = sourceHeroes; Upgrade = upgrade; Filters = Array.AsReadOnly(filters.ToArray()); }
    }

    public sealed class RelicCardFilterResult
    {
        public string AssetKey { get; }
        public bool Accepted { get; }
        public RelicCardFilterResult(string assetKey, bool accepted) { AssetKey = assetKey; Accepted = accepted; }
    }

    public sealed class RelicCardUpgradeStep
    {
        public CardUpgradeLifecycleState State { get; }
        public bool Returned { get; }
        public bool UpgradeAdded { get; }
        public int NextUpgradeInstanceId { get; }
        public IReadOnlyList<RelicCardFilterResult> Filters { get; }
        public RelicCardUpgradeStep(CardUpgradeLifecycleState state, bool returned, bool upgradeAdded, int nextUpgradeInstanceId,
            IReadOnlyList<RelicCardFilterResult>? filters = null)
        { State = state; Returned = returned; UpgradeAdded = upgradeAdded; NextUpgradeInstanceId = nextUpgradeInstanceId;
            Filters = Array.AsReadOnly((filters ?? Array.Empty<RelicCardFilterResult>()).ToArray()); }
    }

    public static class RelicCardUpgradeModel
    {
        public static RelicCardUpgradeStep Apply(CardUpgradeLifecycleState source, RelicCardUpgradeRule rule,
            int nextUpgradeInstanceId, bool ignoreTemporaryCost = false, bool monstersAreAllSubtypes = false)
        {
            if (rule.Upgrade == null || !rule.SourceMonsters)
                return new RelicCardUpgradeStep(source, false, false, nextUpgradeInstanceId);
            var state = source;
            var observations = new List<RelicCardFilterResult>();
            foreach (var filter in rule.Filters)
            {
                // Native FilterCard reads traits even when another attribute has
                // already failed; only subsequent filters short circuit.
                var query = CardOwnedMaskModel.Resolve(state.Card, ignoreTemporaryCost);
                state = new CardUpgradeLifecycleState(query.State, state.Permanent, state.Temporary,
                    state.AuthoredTriggers, state.UpgradeTriggers, state.StandbyOverride, state.StandbyPile);
                bool accepted = CardUpgradeMaskModel.FilterCard(filter, query.Card, monstersAreAllSubtypes);
                observations.Add(new RelicCardFilterResult(filter.AssetKey, accepted));
                if (!accepted) return new RelicCardUpgradeStep(state, false, false, nextUpgradeInstanceId, observations);
            }
            if (nextUpgradeInstanceId <= 0 || nextUpgradeInstanceId == int.MaxValue)
                throw new InvalidOperationException("Missing fresh temporary upgrade identity.");
            var applied = CardUpgradeLifecycleModel.ApplyTemporary(state, rule.Upgrade.WithInstanceId(nextUpgradeInstanceId));
            // The effect refreshes the body and returns true even if a unique
            // temporary upgrade rejects AddUpgrade. A caller still notifies it.
            state = CardUpgradeLifecycleModel.Ensure(applied.State);
            bool added = applied.Returned == true;
            return new RelicCardUpgradeStep(state, true, added, added ? nextUpgradeInstanceId + 1 : nextUpgradeInstanceId, observations);
        }
    }
}
