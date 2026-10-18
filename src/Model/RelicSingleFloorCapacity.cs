using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicSingleFloorCapacityModel
    {
        internal const string Effect = "RelicEffectChangeSingleFloorCapacity";
        internal const string DataId = "775d24d8-98eb-4f22-ae50-d7069bf05757";
        internal const string AssetKey = "MoreCapacity";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect) =>
            owner.DataId == DataId && owner.AssetKey == AssetKey && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual(new[] { Effect }, StringComparer.Ordinal) && effect.EffectIndex == 0 &&
            effect.ParamInt == 2 && effect.Amount == 2 && effect.ParamFloat == 1f && effect.ParamInt2 == 0 &&
            !effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null && effect.Conditions.Count == 0
                ? null : "Missing or changed original MoreCapacity payload.";

        internal static bool Test(CombatContext source, int turn) => turn == 0 &&
            source.RoomCapacities?.Any(room => room.RoomIndex == 1) == true;

        internal static RoomCapacityResult Apply(CombatContext source, int turn, bool preview = false)
        {
            CombatRelicState[] owners = source.Relics?.Where(owner => owner.EffectTypes.Contains(Effect)).ToArray()
                ?? Array.Empty<CombatRelicState>();
            if (owners.Length == 0) return new(source, 0);
            string? error = RelicModel.Validate(source);
            if (error != null) return new(null, 0, error);
            CombatContext context = source;
            foreach (CombatRelicState owner in owners)
            {
                if (preview || !Test(context, turn)) continue;
                RoomCapacityResult applied = RoomCapacityModel.Apply(context, 1,
                    new CardActionEffect("AdjustCapacity", "Room", 2, false, true, Array.Empty<CombatStatus>()));
                if (!applied.Supported) return applied;
                context = applied.Context!;
            }
            return new(context, ReferenceEquals(context, source) ? 0 : 2 * owners.Length);
        }
    }
}
