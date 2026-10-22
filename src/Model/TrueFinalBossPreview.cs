using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class TrueFinalBossPreviewModel
    {
        public static TrainCombatResult Refresh(TrainCombatState source, BattlePlayRules playRules)
        {
            if (source.Context?.LifeLinks == null || !source.Rooms.All(TrueFinalBossCombatModel.Supports))
                return new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), "Uncalibrated true-final-boss preview scope.");
            return BattlePreviewModel.Refresh(source, playRules);
        }
    }
}
