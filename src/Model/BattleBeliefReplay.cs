using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleBeliefReplayResult
    {
        public double ExpectedScore { get; }
        public IReadOnlyList<BattleBeliefWorld> FinalWorlds { get; }
        internal BattleBeliefReplayResult(double score, IReadOnlyList<BattleBeliefWorld> worlds)
        { ExpectedScore = score; FinalWorlds = Array.AsReadOnly(worlds.ToArray()); }
    }
    public static class BattleBeliefReplay
    {
        // Re-evaluate each policy input from the original hypotheses. This does
        // not reuse planner successor caches, scores or posterior probabilities.
        public static BattleBeliefReplayResult Verify(BattleBelief belief, BattleBeliefPolicy policy,
            Func<BattleTurnState, RoomOutcome, double>? evaluate = null)
        {
            evaluate ??= BattlePlanner.Evaluate;
            using var observations = new BattleObservationKeys(); using var stateKeys = new BattleStateKeys();
            var totals = new Dictionary<BattleBeliefPolicy, (CompensatedSum Mass, CompensatedSum Value)>();
            var masses = new Dictionary<BattleBeliefPolicy, Dictionary<string, double>>();
            var final = new List<BattleBeliefWorld>(); var expected = new CompensatedSum();
            // Capture every distinct parent before replaying any world, then
            // rehash independently after all branches. Shared immutable worlds
            // need one check, and a later branch cannot hide parent mutation.
            var parents = belief.Worlds.Select(world => world.State).Distinct()
                .ToDictionary(state => state, state => stateKeys.Key(state));
            foreach (var world in belief.Worlds)
            {
                var state = world.State; var outcome = world.Outcome;
                var node = policy; var path = new List<BattleBeliefPolicy>();
                while (true)
                {
                    path.Add(node);
                    if (node.IsLeaf) break;
                    if (node.IsEndTurn)
                    {
                        var next = BattleTurnModel.EndTurn(state);
                        if (!next.Supported) throw new InvalidOperationException(next.UnsupportedReason);
                        state = next.State!; outcome = next.Outcome;
                    }
                    else
                    {
                        if (node.Action == null) throw new InvalidOperationException("Policy has no executable action.");
                        var next = BattleActionModel.PlayCard(state, node.Action);
                        if (!next.Supported) throw new InvalidOperationException(next.Reason);
                        state = next.State!; outcome = next.Outcome;
                    }
                    string visible = observations.Key(state, outcome);
                    var branch = node.Branches.SingleOrDefault(child => child.Observation == visible)
                        ?? throw new InvalidOperationException("Policy omits a revealed observation.");
                    if (!masses.TryGetValue(node, out var counts)) masses[node] = counts = new Dictionary<string, double>(StringComparer.Ordinal);
                    counts[visible] = (counts.TryGetValue(visible, out double mass) ? mass : 0) + world.Weight;
                    node = branch.Policy;
                }
                double score = evaluate(state, outcome);
                if (double.IsNaN(score) || double.IsInfinity(score)) throw new InvalidOperationException("Nonfinite policy replay score.");
                foreach (var visited in path)
                {
                    totals.TryGetValue(visited, out var value);
                    value.Mass.Add(world.Weight); value.Value.Add(world.Weight * score);
                    totals[visited] = value;
                }
                expected.Add(world.Weight * score);
                final.Add(new BattleBeliefWorld(state, world.Weight, outcome));
            }
            using (var independent = new BattleStateKeys())
                foreach (var parent in parents)
                    if (independent.Key(parent.Key) != parent.Value) throw new InvalidOperationException("Policy replay mutated an input hypothesis.");
            foreach (var item in totals)
            {
                if (!Close(item.Value.Value.Value / item.Value.Mass.Value, item.Key.ExpectedScore))
                    throw new InvalidOperationException("Policy node expected score differs from independent weighted replay.");
                if (item.Key.IsLeaf) continue;
                if (item.Key.Branches.Select(branch => branch.Observation).Distinct().Count() != item.Key.Branches.Count ||
                    !Close(item.Key.Branches.Sum(branch => branch.Probability), 1))
                    throw new InvalidOperationException("Policy has duplicate observations or invalid total probability.");
                foreach (var branch in item.Key.Branches)
                    if (!masses[item.Key].TryGetValue(branch.Observation, out double mass) ||
                        !Close(mass / item.Value.Mass.Value, branch.Probability))
                        throw new InvalidOperationException("Policy posterior probability differs from independent replay.");
            }
            return new BattleBeliefReplayResult(expected.Value, final);
        }
        private static bool Close(double left, double right) => Math.Abs(left - right) <=
            1e-9 + 8e-15 * Math.Max(Math.Abs(left), Math.Abs(right));
    }
}
