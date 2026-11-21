using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicOpeningCapacityModel
    {
        internal const string Effect = "RelicEffectModifyRandomRoomCapacity";
        internal const string Carryover = "RelicEffectModifyRandomRoomCapacityAfterBattle";
        internal const string MutatorId = "5d9aa853-182a-4cc3-96eb-8189ce7c7d74";
        internal const string BogwurmId = "7a60e645-e4f0-403c-9b4a-e794675f3a60";
        internal const string LifeId = "96cca2fd-d096-4340-9054-d498826a0781";

        internal static bool IsEffect(string type) => type is Effect or Carryover;
        internal static bool IsPyreOwner(string id) => id is BogwurmId or LifeId;

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule)
        {
            if (owner.DisallowedInPlacementPhase != false || rule.Amount != rule.ParamInt || rule.ParamInt2 != 0 ||
                rule.Conditions.Count != 0 || rule.RequiredMutatorId != null || rule.CanApplyInPreviewMode)
                return "Incomplete native opening room-capacity definition.";
            bool known = rule.Type == Effect
                ? owner.DataId == MutatorId && owner.EffectTypes.Count == 1 && rule.EffectIndex == 0 && rule.ParamInt == -3
                : rule.ParamInt == 1 && (owner.DataId == BogwurmId && owner.EffectTypes.Count == 1 && rule.EffectIndex == 0 ||
                    owner.DataId == LifeId && owner.EffectTypes.SequenceEqual(new[] { RelicResourceModel.FloorCapacity, Carryover }) && rule.EffectIndex == 1);
            return known ? null : "Unverified opening room-capacity owner definition.";
        }

        internal static string? Validate(CombatContext context)
        {
            var relics = context.Relics!;
            int mutators = relics.Count(relic => relic.EffectTypes.Contains(Effect));
            int pyreOwners = relics.Count(relic => relic.EffectTypes.Contains(Carryover));
            bool mutatorActive = context.Resources!.MutatorIds.Contains(MutatorId);
            if (mutators == 0 && pyreOwners == 0 && !mutatorActive && !context.Resources.OpeningCapacitySettled.HasValue) return null;
            if (mutators != (mutatorActive ? 1 : 0) || pyreOwners > 1 || mutators + pyreOwners == 0 ||
                context.Resources.OpeningCapacitySettled != true)
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
