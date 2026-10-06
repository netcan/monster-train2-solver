using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class BattlePreviewModel
    {
        // The installed game's preview does not guard SetAttackDamageDealt with PreviewMode.
        // Each room is simulated on copies, and only that observed statistic crosses back.
        internal static TrainCombatResult Refresh(TrainCombatState source)
        {
            CombatContext? context = source.Context;
            if (context?.Statistics == null) return new TrainCombatResult(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
            foreach (RoomCombatState room in source.Rooms.Reverse())
            {
                RoomCombatResult preview = RoomCombatModel.Resolve(new RoomCombatState(room.RoomIndex, room.Deployment,
                    room.Units, room.ExternalInteractions, context, preview: true));
                if (!preview.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported,
                    Array.Empty<RoomCombatResult>(), "Battle preview: " + preview.UnsupportedReason);
                context = context.WithStatistics(context.Statistics!.WithLastAttackDamage(preview.State!.Context!.Statistics!.LastAttackDamageDealt));
            }
            var train = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, context)).ToArray(), source.Movement, source.EnemySlotsPerRoom, context);
            return new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        }
    }
}
