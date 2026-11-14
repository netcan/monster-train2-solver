using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RelicDamagedFloorEnergyModel
    {
        public const string Effect = "RelicEffectModifyEnergyWhenDamagedUnitOnEachFloor";

        internal static string? ValidateBeforeDrawOrder(CombatContext context)
        {
            if (!context.Relics!.Any(relic => relic.EffectTypes.Contains(Effect))) return null;
            // This older callback runs before RunMonsterTurn irrespective of owner order.
            if (context.Relics.Any(relic => relic.EffectTypes.Contains(RelicSoloUnitUpgradeModel.FrontRoomEffect)))
                return "General start-of-turn max-health effects with damaged-floor energy require ordered native calibration.";
            bool earlierHealthEffect = false;
            foreach (string type in context.Relics.SelectMany(relic => relic.EffectTypes))
            {
                if (type == Effect && earlierHealthEffect)
                    return "Earlier before-draw healing, upgrades or nested room effects with damaged-floor energy require ordered native calibration.";
                if (type == RelicResourceModel.StartTurnHeal || type == RelicResourceModel.StartTurnRoomEffect ||
                    type == RelicResourceModel.PileStartTurn || type == RelicSoloUnitUpgradeModel.Effect)
                    earlierHealthEffect = true;
            }
            return null;
        }

        public static string? Check(TrainCombatState? train, out bool passed)
        {
            passed = false;
            var rooms = train?.Context?.RoomCapacities;
            if (train == null || rooms == null || rooms.Count == 0 ||
                !rooms.Select(room => room.RoomIndex).OrderBy(id => id).SequenceEqual(train.Rooms.Select(room => room.RoomIndex).OrderBy(id => id)) ||
                rooms.Select(room => room.RoomIndex).Distinct().Count() != rooms.Count)
                return "Damaged-floor energy requires the complete native room roster and Pyre-room metadata.";
            // RoomState.AddCharactersToList supplies only living characters by default.
            passed = rooms.Where(room => !room.IsPyre).All(room => train.Rooms.Single(floor => floor.RoomIndex == room.RoomIndex)
                .Units.Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 && unit.Health < unit.MaxHealth &&
                    unit.DeathState?.IsDespawned != true && unit.DeathState?.IsDestroyed != true));
            return null;
        }
    }
}
