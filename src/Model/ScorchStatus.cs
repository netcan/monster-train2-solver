using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class ScorchStatusResult
    {
        public TrainCombatState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal ScorchStatusResult(TrainCombatState? state, string? error = null)
        { State = state; UnsupportedReason = error; }
    }

    public static class ScorchStatusModel
    {
        public static ScorchStatusResult StartTurn(TrainCombatState source)
        {
            TrainCombatState state = source;
            foreach (RoomCombatState room in source.Rooms.OrderByDescending(item => item.RoomIndex))
            {
                foreach (int unitId in room.Units.Where(unit => unit.Team == CombatTeam.Player)
                    .Concat(room.Units.Where(unit => unit.Team == CombatTeam.Enemy)).Select(unit => unit.Id))
                {
                    RoomCombatState currentRoom = state.Rooms.Single(item => item.RoomIndex == room.RoomIndex);
                    CombatUnit? unit = currentRoom.Units.FirstOrDefault(item => item.Id == unitId);
                    CombatStatus? status = unit?.Status("scorch");
                    if (status == null || status.Stacks <= 0) continue;
                    if (!status.RemoveWhenTriggered || status.RemoveWhenTriggeredAfterCardPlayed ||
                        status.RemoveAtEndOfTurnIfTriggered || status.SkipDuringDeployment ||
                        status.RemoveStackAtEnd || status.RemoveAllAtEnd)
                        return new ScorchStatusResult(null, "Unmodeled Scorch trigger or removal rule.");
                    string? energyError = EnergyModel.Validate(state.Context);
                    if (energyError != null) return new ScorchStatusResult(null, energyError);
                    RelicStatusDecayResult removal = RelicStatusDecayModel.GetShouldRemove(state.Context, true,
                        "scorch", unit!.Team == CombatTeam.Player ? 2 : 1, currentRoom.Preview);
                    if (!removal.Supported) return new ScorchStatusResult(null, removal.UnsupportedReason);
                    if (!removal.ShouldRemove)
                        return new ScorchStatusResult(null, "Scorch suppression requires native callback calibration.");
                    RoomCombatResult changed = StatusRemovalModel.Remove(currentRoom, unit.Id, "scorch", 1);
                    if (!changed.Supported) return new ScorchStatusResult(null, changed.UnsupportedReason);
                    changed = StatusRemovalModel.Drain(changed);
                    if (!changed.Supported || changed.Outcome != RoomOutcome.Exchanged)
                        return new ScorchStatusResult(null, changed.UnsupportedReason ?? "Scorch removal ended battle.");
                    int remaining = changed.State!.Units.First(item => item.Id == unit.Id).Status("scorch")?.Stacks ?? 0;
                    CombatContext context = changed.State.Context!;
                    if (context.QueryFrame?.SelectedRoomIndex != null)
                        context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: room.RoomIndex));
                    context = EnergyModel.Apply(context, "AdjustEnergy", -unchecked(remaining + 1));
                    state = new TrainCombatState(state.Rooms.Select(item => item.RoomIndex == room.RoomIndex
                        ? changed.State : item).ToArray(), state.Movement, state.EnemySlotsPerRoom, context);
                    state = CardSpellModel.WithContext(state, context);
                }
            }
            return new ScorchStatusResult(state);
        }
    }
}
