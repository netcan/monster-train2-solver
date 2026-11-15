using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicTurnEnergyRule
    {
        public int InitialEnergy { get; }
        public int ReductionPerTurn { get; }
        public bool AllowDuringDeployment { get; }
        public int LastCheckedTurn { get; }
        public int OccupancyBonus { get; }
        public int CapacityThreshold { get; }
        public int ThresholdBonus { get; }
        public RelicTurnEnergyRule(int initialEnergy, int reductionPerTurn, bool allowDuringDeployment,
            int lastCheckedTurn, int occupancyBonus, int capacityThreshold, int thresholdBonus)
        {
            InitialEnergy = initialEnergy; ReductionPerTurn = reductionPerTurn; AllowDuringDeployment = allowDuringDeployment;
            LastCheckedTurn = lastCheckedTurn; OccupancyBonus = occupancyBonus; CapacityThreshold = capacityThreshold; ThresholdBonus = thresholdBonus;
        }
        internal RelicTurnEnergyRule WithCheckedTurn(int turn) => new(InitialEnergy, ReductionPerTurn, AllowDuringDeployment,
            turn, OccupancyBonus, CapacityThreshold, ThresholdBonus);
        internal int ReducingAmount(int turn) => turn == 0 && !AllowDuringDeployment ? 0 :
            Math.Max(0, unchecked(InitialEnergy - ReductionPerTurn * (AllowDuringDeployment ? turn : unchecked(turn - 1))));
    }

    internal static class RelicTurnEnergyModel
    {
        internal const string Reducing = "RelicEffectModifyEnergyReducingEveryTurn";
        internal const string Occupied = "RelicEffectModifyEnergyWhenFloorsOccupied";
        internal static bool IsEffect(string type) => type is Reducing or Occupied;
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            var rule = effect.TurnEnergy;
            if (rule == null || !owner.IsCovenant.HasValue || !owner.DisallowedInPlacementPhase.HasValue ||
                effect.CanApplyInPreviewMode || effect.RequiredMutatorId != null || effect.Amount != 0)
                return "Missing initialized dynamic turn energy rule.";
            if (effect.Type == Reducing)
                return rule.InitialEnergy == effect.ParamInt && rule.ReductionPerTurn == effect.ParamInt2 &&
                    rule.OccupancyBonus == 0 && rule.CapacityThreshold == 0 && rule.ThresholdBonus == 0 ? null : "Inconsistent reducing energy parameters.";
            return effect.Type == Occupied && rule.OccupancyBonus == effect.ParamMinInt && rule.ThresholdBonus == effect.ParamInt &&
                rule.InitialEnergy == 0 && rule.ReductionPerTurn == 0 && !rule.AllowDuringDeployment && rule.LastCheckedTurn == 0 ? null : "Inconsistent occupied-floor energy parameters.";
        }
        internal static string? Occupancy(TrainCombatState? train, out bool occupied, out int size)
        {
            occupied = false; size = 0;
            var context = train?.Context; var rooms = context?.RoomCapacities;
            if (train == null || rooms == null || context!.SpawnPoints == null || rooms.Count == 0 ||
                !rooms.Select(room => room.RoomIndex).OrderBy(id => id).SequenceEqual(train.Rooms.Select(room => room.RoomIndex).OrderBy(id => id)))
                return "Occupied-floor energy requires complete rooms, physical membership and Pyre metadata.";
            foreach (var room in rooms.OrderBy(room => room.RoomIndex))
            {
                if (room.IsPyre) continue;
                var group = context.SpawnPoints.SelectedGroup(room.RoomIndex, CombatTeam.Player);
                if (group == null) return "Missing occupied-floor player spawn points.";
                int[] ids = group.Occupants.Where(id => id > 0).Distinct().ToArray();
                // GetNumCharacters counts registered dead actors too; capacity
                // counts only IsAlive actors and excludes the outer train anchor.
                if (ids.Length == 0) return null;
                var units = train.Rooms.Single(floor => floor.RoomIndex == room.RoomIndex).Units;
                foreach (int id in ids)
                {
                    var unit = units.SingleOrDefault(actor => actor.Id == id);
                    if (unit == null) return "Missing physically occupied energy actor.";
                    if (unit.Health > 0 && unit.DeathState?.IsDespawned != true) size = unchecked(size + Math.Max(1, Math.Min(6, unit.Size)));
                }
            }
            occupied = true; return null;
        }
        // Native issues two separate energy mutations. Preserve both clamps and
        // the zero-valued threshold RemoveEnergy call before its one notification.
        internal static int[] Requests(RelicResourceEffect effect, int turn, bool occupied, int size)
        {
            var rule = effect.TurnEnergy!;
            if (effect.Type == Reducing)
            {
                int amount = rule.ReducingAmount(turn);
                return amount > 0 ? new[] { amount } : Array.Empty<int>();
            }
            if (!occupied || rule.OccupancyBonus == 0) return Array.Empty<int>();
            return size >= rule.CapacityThreshold ? new[] { rule.OccupancyBonus, rule.ThresholdBonus } : new[] { rule.OccupancyBonus };
        }
    }
}
