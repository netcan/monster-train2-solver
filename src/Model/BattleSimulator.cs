using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleSimulationAction
    {
        public PlayCardAction Action { get; }
        public BattleActionResult Result { get; }
        internal BattleSimulationAction(PlayCardAction action, BattleActionResult result) { Action = action; Result = result; }
    }
    public sealed class BattleSimulationResult
    {
        public BattleTurnState? State { get; }
        public RoomOutcome Outcome { get; }
        public IReadOnlyList<BattleTurnResult> Turns { get; }
        public IReadOnlyList<BattleSimulationAction> Actions { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal BattleSimulationResult(BattleTurnState? state, RoomOutcome outcome,
            IReadOnlyList<BattleTurnResult> turns, string? reason = null, IReadOnlyList<BattleSimulationAction>? actions = null)
        {
            State = state; Outcome = outcome; Turns = Array.AsReadOnly(turns.ToArray()); UnsupportedReason = reason;
            Actions = Array.AsReadOnly((actions ?? Array.Empty<BattleSimulationAction>()).ToArray());
        }
    }

    public static class BattleSimulator
    {
        // Finish a branch with no further card plays. Every next state is produced by the model.
        // Cancellation is a caller's search budget; it never becomes a predicted win/loss.
        public static BattleSimulationResult ResolveNoMoreCards(BattleTurnState source,
            CancellationToken cancellationToken = default) => Resolve(source, _ => null, cancellationToken);

        // Every card action and EndTurn operates on the same independent state. The callback chooses
        // an action from the model's current decision state, without receiving future native states.
        public static BattleSimulationResult Resolve(BattleTurnState source,
            Func<BattleTurnState, PlayCardAction?> chooseAction, CancellationToken cancellationToken = default)
        {
            BattleTurnState current = source;
            var turns = new List<BattleTurnResult>();
            var actions = new List<BattleSimulationAction>();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!current.Spawn.Looping && current.Spawn.Phase >= current.Spawn.Waves.Count &&
                    !current.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Enemy))
                    return Finish(null, RoomOutcome.Unsupported, "No remaining enemy or modeled terminal death event.");
                PlayCardAction? action = chooseAction(current);
                if (action != null)
                {
                    BattleActionResult played = BattleActionModel.PlayCard(current, action);
                    actions.Add(new BattleSimulationAction(action, played));
                    if (!played.Supported) return Finish(null, RoomOutcome.Unsupported, played.Reason);
                    current = played.State!;
                    if (played.Outcome == RoomOutcome.BattleWon || played.Outcome == RoomOutcome.PlayerDefeated)
                        return Finish(current, played.Outcome);
                    continue;
                }
                BattleTurnResult result = BattleTurnModel.EndTurn(current);
                turns.Add(result);
                if (!result.Supported) return Finish(null, RoomOutcome.Unsupported, result.UnsupportedReason);
                current = result.State!;
                if (result.Outcome == RoomOutcome.BattleWon || result.Outcome == RoomOutcome.PlayerDefeated ||
                    result.Outcome == RoomOutcome.Stalemate)
                    return Finish(current, result.Outcome);
            }
            BattleSimulationResult Finish(BattleTurnState? state, RoomOutcome outcome, string? reason = null) =>
                new BattleSimulationResult(state, outcome, turns, reason, actions);
        }
    }
}
