using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class DevotionStatusModel
    {
        internal const string Effect = "AddStatusPerDevotion";
        internal static int Grants(CombatContext? context, CardActionEffect effect) =>
            Math.Max(0, context?.QueryFrame?.Devotion?.Level ?? 0) / Math.Max(1, effect.DevotionLevelsPerGrant ?? 0);

        // TestEffect tests the authored status; only ApplyEffect scales its stacks.
        internal static CardActionEffect AsStatus(CardActionEffect effect, int grants = 1) =>
            new CardActionEffect("AddStatus", effect.Target, effect.Value, effect.AllowEnemy, effect.AllowPlayer,
                effect.Statuses.Select(status => status.WithStacks(unchecked(status.Stacks * grants))).ToArray(),
                tests: effect.Tests, range: effect.Range, filters: effect.Filters, ignoreDualism: effect.IgnoreDualism);
    }
}
