using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardStatusCompositionState
    {
        public IReadOnlyList<UpgradeMaskStatus> StartingStatuses { get; }
        public IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> PermanentUpgradeStatuses { get; }
        public IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> TemporaryUpgradeStatuses { get; }
        public bool Purified { get; }
        public bool SupportsSpawnStatuses { get; }
        public CardStatusCompositionState(IReadOnlyList<UpgradeMaskStatus> startingStatuses,
            IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> permanentUpgradeStatuses,
            IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> temporaryUpgradeStatuses,
            bool purified, bool supportsSpawnStatuses)
        {
            StartingStatuses = Array.AsReadOnly(startingStatuses.ToArray());
            PermanentUpgradeStatuses = CardUpgradeMaskRule.CopyLists(permanentUpgradeStatuses);
            TemporaryUpgradeStatuses = CardUpgradeMaskRule.CopyLists(temporaryUpgradeStatuses);
            Purified = purified; SupportsSpawnStatuses = supportsSpawnStatuses;
        }
    }
    // CardState.TryGetStatusEffects with applyDuality=false. Card queries retain
    // source flags and use no unit status cap; each merge discards zero groups.
    public static class CardStatusCompositionModel
    {
        public static IReadOnlyList<UpgradeMaskStatus> Resolve(CardStatusCompositionState source)
        {
            if (!source.SupportsSpawnStatuses) return Array.Empty<UpgradeMaskStatus>();
            var permanent = Merge(source.Purified ? Array.Empty<UpgradeMaskStatus>() : source.StartingStatuses, source.PermanentUpgradeStatuses);
            return Merge(permanent, source.TemporaryUpgradeStatuses);
        }
        private static IReadOnlyList<UpgradeMaskStatus> Merge(IReadOnlyList<UpgradeMaskStatus> starting,
            IReadOnlyList<IReadOnlyList<UpgradeMaskStatus>> upgrades)
        {
            var result = new List<UpgradeMaskStatus>();
            foreach (var status in starting.Concat(upgrades.SelectMany(list => list)))
            {
                int index = result.FindIndex(item => item.Id == status.Id && item.FromPermanentUpgrade == status.FromPermanentUpgrade);
                int count = Math.Max(0, index < 0 ? status.Count : unchecked(result[index].Count + status.Count));
                var merged = new UpgradeMaskStatus(status.Id, count, status.FromPermanentUpgrade);
                if (index < 0) result.Add(merged); else result[index] = merged;
            }
            return Array.AsReadOnly(result.Where(status => status.Count > 0).ToArray());
        }
    }
}
