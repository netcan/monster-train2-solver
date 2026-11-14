using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardTriggerGroupModel
    {
        internal static string? Validate(IReadOnlyList<int>? sizes, int count) => sizes != null &&
            (sizes.Count == 0 && count > 0 || sizes.Any(size => size <= 0) || sizes.Sum(size => (long)size) != count)
                ? "OnCast group sizes must partition the captured effects." : null;

        internal static int[] Ends(IReadOnlyList<int> sizes, int offset = 0)
        {
            int cursor = offset;
            return sizes.Select(size => cursor += size).ToArray();
        }
    }
}
