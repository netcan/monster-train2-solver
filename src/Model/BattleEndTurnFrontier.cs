using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleEndTurnCandidate
    {
        public IReadOnlyList<RelentlessDecision> Decisions { get; }
        public BattleTurnResult Result { get; }
        internal BattleEndTurnCandidate(IReadOnlyList<RelentlessDecision> decisions, BattleTurnResult result)
        { Decisions = Array.AsReadOnly(decisions.ToArray()); Result = result; }
        public BattleSearchStep Step => new BattleSearchStep(null, true, Decisions);
    }

    public sealed class BattleEndTurnFrontier
    {
        public IReadOnlyList<BattleEndTurnCandidate> Candidates { get; }
        public IReadOnlyList<UnsupportedActionAttempt> UnsupportedAttempts { get; }
        public int ChoiceNodes { get; }
        public bool ReachedNodeLimit { get; }
        public bool Interrupted { get; }
        internal BattleEndTurnFrontier(List<BattleEndTurnCandidate> candidates, List<UnsupportedActionAttempt> unsupported,
            int choiceNodes, bool limited, bool interrupted)
        { Candidates = Array.AsReadOnly(candidates.ToArray()); UnsupportedAttempts = Array.AsReadOnly(unsupported.ToArray());
            ChoiceNodes = choiceNodes; ReachedNodeLimit = limited; Interrupted = interrupted; }
    }

    public static class BattleEndTurnModel
    {
        // EndTurn is deterministic once all intervening dialogs have explicit
        // inputs. Retry from the immutable decision, branching only on a typed
        // pending request. A never-ending Continue chain consumes the same node
        // budget as other search work; a truncated frontier is always reported.
        public static BattleEndTurnFrontier Enumerate(BattleTurnState source, int maxChoiceNodes,
            Func<bool>? shouldStop = null) => Enumerate(source, maxChoiceNodes, shouldStop,
                choices => BattleTurnModel.EndTurn(source, choices));

        internal static BattleEndTurnFrontier Enumerate(BattleTurnState source, int maxChoiceNodes,
            Func<bool>? shouldStop, Func<IReadOnlyList<RelentlessDecision>, BattleTurnResult> simulate,
            Func<bool>? shouldStopBeforeFirstAttempt = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxChoiceNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxChoiceNodes));
            var candidates = new List<BattleEndTurnCandidate>();
            var unsupported = new List<UnsupportedActionAttempt>();
            var pending = new Stack<RelentlessDecision[]>();
            int nodes = 0; bool interrupted = false;
            if ((shouldStopBeforeFirstAttempt ?? shouldStop)?.Invoke() == true)
                return new BattleEndTurnFrontier(candidates, unsupported, nodes, false, true);
            Observe(Array.Empty<RelentlessDecision>(), simulate(Array.Empty<RelentlessDecision>()));
            while (pending.Count > 0 && nodes < maxChoiceNodes)
            {
                if (shouldStop?.Invoke() == true) { interrupted = true; break; }
                RelentlessDecision[] choices = pending.Pop(); nodes++;
                Observe(choices, simulate(choices));
            }
            return new BattleEndTurnFrontier(candidates, unsupported, nodes,
                pending.Count > 0 && nodes >= maxChoiceNodes, interrupted);

            void Observe(RelentlessDecision[] choices, BattleTurnResult result)
            {
                if (result.Supported) { candidates.Add(new BattleEndTurnCandidate(choices, result)); return; }
                if (result.RelentlessChoice is not RelentlessChoiceRequest request)
                { unsupported.Add(new UnsupportedActionAttempt(null, result.UnsupportedReason ?? "Unsupported EndTurn.")); return; }
                if (choices.Any(choice => choice.RoomIndex == request.RoomIndex && choice.CompletedRounds == request.CompletedRounds))
                { unsupported.Add(new UnsupportedActionAttempt(null, "EndTurn repeated a consumed Relentless request.")); return; }
                // Explore Abandon first to obtain a complete successor before
                // spending a bounded budget on additional Continue callbacks.
                pending.Push(choices.Concat(new[] { request.Choose(true) }).ToArray());
                pending.Push(choices.Concat(new[] { request.Choose(false) }).ToArray());
            }
        }
    }
}
