using System;

namespace MonsterTrain2Poju.Model
{
    // This query adapter does not yet model propagation of a friendly room aura.
    internal static class RoomHealthDamageModel
    {
        internal const string Self = "RoomStateSelfDamagePerMissingHp";
        internal const string FriendlyOwn = "RoomStateFriendlyDamagePerOwnMissingHp";
        internal static bool Known(string type) => type == Self || type == FriendlyOwn;
        internal static int Query(string type, int coefficient, int divisor, int maximum, int health,
            bool player, bool hasPoint, bool hasRoom, bool attached, bool requestingStats)
        {
            if (!Known(type)) throw new ArgumentException("Unknown health damage query.", nameof(type));
            if (!requestingStats || !hasPoint || !hasRoom || (type == Self ? !attached : !player)) return 0;
            int missing = unchecked(maximum - health);
            return unchecked((type == Self ? missing : missing / Math.Max(1, divisor)) * coefficient);
        }
    }
}
