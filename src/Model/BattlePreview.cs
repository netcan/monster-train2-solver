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
            var previewTrain = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
                room.Deployment, room.Units, room.ExternalInteractions, context, preview: true)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, context);
            // Native UI preview runs only the player pre-discard phase, once before all rooms.
            TrainCombatResult preDiscard = TrainCombatModel.EndTurnPreHandDiscard(previewTrain, CombatTeam.Player);
            if (!preDiscard.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported,
                Array.Empty<RoomCombatResult>(), "Battle preview: " + preDiscard.UnsupportedReason);
            CombatContext previewContext = preDiscard.State!.Context!;
            foreach (RoomCombatState room in preDiscard.State.Rooms.Reverse())
            {
                RoomCombatResult preview = RoomCombatModel.Resolve(new RoomCombatState(room.RoomIndex, room.Deployment,
                    room.Units, room.ExternalInteractions, previewContext, preview: true));
                if (!preview.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported,
                    Array.Empty<RoomCombatResult>(), "Battle preview: " + preview.UnsupportedReason);
                previewContext = previewContext.WithStatistics(previewContext.Statistics!.WithLastAttackDamage(preview.State!.Context!.Statistics!.LastAttackDamageDealt));
                if (context.IsolatedBattlePreview == true) previewContext = previewContext.WithBattleRng(preview.State!.Context!.BattleRng);
            }
            context = context.WithStatistics(context.Statistics.WithLastAttackDamage(previewContext.Statistics!.LastAttackDamageDealt));
            var train = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, context)).ToArray(), source.Movement, source.EnemySlotsPerRoom, context);
            return new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        }
    }
}
