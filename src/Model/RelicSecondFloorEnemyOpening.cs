using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicSecondFloorEnemyOpeningModel
    {
        internal const string OwnerId = "ea497cb3-cc74-4cb9-84c6-57f1fd92929e";
        internal const string OwnerKey = "Echoes_EnemyUnitsStartAtSecondFloor";
        internal const string Effect = "RelicEffectChangeHeroSpawnRoom";

        internal static bool IsOriginal(CombatRelicState owner) => owner is
        {
            DataId: OwnerId, AssetKey: OwnerKey, IsCovenant: false, DisallowedInPlacementPhase: false, EffectTypes: [Effect]
        };

        internal static bool Calibrated(EnemySpawnState state) =>
            state is { Turn: 1, Phase: 1, Looping: false, OuterBoss: null,
                QueuedOuterBoss: null, QueuedTrueFinalBoss: null } &&
            state.Train.Context is { SpawnPoints: not null, QueryFrame.Turn: 1 } &&
            state.Train.Rooms.Count == 4 && state.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy) &&
            state.Train.Rooms.Skip(1).All(room => room.Units.All(unit => unit.Team != CombatTeam.Enemy)) &&
            state.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit =>
                unit.IsBoss == false && unit.Statuses.All(status => status.Id is "fullmoon" or "newmoon") &&
                unit.Triggers.All(trigger => trigger.Kind is "PostCombat"));
    }
}
