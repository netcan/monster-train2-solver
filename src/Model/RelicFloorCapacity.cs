using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static partial class RelicResourceModel
    {
        public static RelicResourceResult GetFloorCapacity(CombatContext source)
        {
            string? error = Ready(source);
            if (error != null) return Fail(error);
            if (!source.Resources!.BaseFloorCapacity.HasValue) return Fail("Missing raw native base floor capacity.");
            int value = source.Resources.BaseFloorCapacity.Value;
            // MutableRules folds initialized floats in their own registration
            // order. Owner conditions, preview and placement never gate this rule.
            foreach (RelicResourceEffect rule in source.Relics!.SelectMany(relic => relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == FloorCapacity && rule.CapacityMutationOrder.HasValue)
                .OrderBy(rule => rule.CapacityMutationOrder))
            {
                if (!TryRound(rule.CapacityValue!.Value, out int rounded))
                    return Fail("Native floor capacity mutation cannot be rounded to Int32.");
                value = unchecked(value + rounded);
            }
            return new RelicResourceResult(source, Math.Max(0, value));
        }

        // Room.Setup snapshots both spawn groups' base capacity. A later rule
        // removal changes the getter, not any already initialized room ledger.
        public static RoomCapacityResult InitializeRoomCapacities(CombatContext source)
        {
            string? error = RoomCapacityModel.Validate(source);
            if (error != null) return new RoomCapacityResult(null, 0, error);
            RelicResourceResult capacity = GetFloorCapacity(source);
            if (!capacity.Supported) return new RoomCapacityResult(null, 0, capacity.UnsupportedReason);
            RoomCapacityState[] rooms = source.RoomCapacities!.Select(room => new RoomCapacityState(
                room.RoomIndex, capacity.Value, capacity.Value, room.IsPyre)).ToArray();
            return new RoomCapacityResult(source.WithRoomCapacities(rooms), capacity.Value);
        }
    }
}
