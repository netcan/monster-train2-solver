using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicOpeningCapacityModel
    {
        internal const string Effect = "RelicEffectModifyRandomRoomCapacity";
        internal const string MutatorId = "5d9aa853-182a-4cc3-96eb-8189ce7c7d74";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule) =>
            owner.DataId == MutatorId && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.Count == 1 && rule.EffectIndex == 0 && rule.ParamInt == -3 &&
            rule.Amount == rule.ParamInt && rule.ParamInt2 == 0 && rule.Conditions.Count == 0 &&
            rule.RequiredMutatorId == null && !rule.CanApplyInPreviewMode
                ? null : "Unverified opening room-capacity mutator definition.";

        internal static string? Validate(CombatContext context)
        {
            int owners = context.Relics!.Count(relic => relic.EffectTypes.Contains(Effect));
            bool active = context.Resources!.MutatorIds.Contains(MutatorId);
            if (!active && owners == 0) return null;
            if (!active || owners != 1 || context.Resources.OpeningCapacitySettled != true)
                return "Opening room-capacity mutation requires the settled native deployment snapshot.";
            var rooms = context.RoomCapacities;
            if (rooms == null || rooms.Count != 4 || rooms.Count(room => room.IsPyre) != 1 ||
                rooms.Select(room => room.RoomIndex).Distinct().Count() != rooms.Count)
                return "Opening room-capacity mutation requires all native room capacities.";

            // Native already applied the turn-zero callback before the first
            // decision. Carry its captured floor capacities and Battle RNG forward.
            return null;
        }
    }
}
