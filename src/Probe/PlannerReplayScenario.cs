using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Executes a pure-model recommendation through the existing native card UI
    // path. A second fresh process can load the first binary trace and replay
    // its frozen sequence without searching again.
    internal static class PlannerReplayScenario
    {
        internal sealed class Step
        {
            public PlayCardAction? Action { get; set; }
            public bool IsEndTurn { get; set; }
        }
        internal sealed class Record
        {
            public string Mode { get; set; } = "search";
            public BattleTurnState Root { get; set; } = null!;
            public BattleTurnState Planned { get; set; } = null!;
            public List<Step> Steps { get; set; } = new List<Step>();
            public List<BattleTurnState> Expected { get; set; } = new List<BattleTurnState>();
            public List<RoomOutcome> Outcomes { get; set; } = new List<RoomOutcome>();
            public List<BattleTurnState> Actual { get; set; } = new List<BattleTurnState>();
            public double Score { get; set; }
            public double BaselineScore { get; set; }
            public double? PolicyBaselineScore { get; set; }
            public int Horizon { get; set; }
            public int Nodes { get; set; }
            public int Duplicates { get; set; }
            public int BeamPruned { get; set; }
            public double ElapsedMs { get; set; }
            public bool TimedOut { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
        }
        internal static List<Record> Records { get; } = new List<Record>();
        private static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER") == "1";
        private static int cursor;
        private static bool waiting;
        private static ManualLogSource? logger;
        internal static bool WaitingForWorker => Enabled &&
#if DEVELOPMENT_PROBE
            !PlayerAdvisorProbe.Enabled &&
#endif
            BattleAdvisorOverlay.Active?.IsPlanning == true &&
            (Records.Count == 0 || Records.Count == 1 && cursor == 1 && waiting && !Records[0].Completed);

        internal static bool AwaitingAdvice(BattleTurnState state)
        {
            BattleAdvisorOverlay? advisor = BattleAdvisorOverlay.Active;
            bool verifyRefresh = Records.Count == 1 && cursor == 1 && waiting && !Records[0].Completed;
#if DEVELOPMENT_PROBE
            if (Enabled && PlayerAdvisorProbe.Enabled && (Records.Count == 0 || verifyRefresh) &&
                state.Spawn.Turn >= int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TURN") ?? "2", CultureInfo.InvariantCulture))
                return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_INPUT")) && !PlayerAdvisorProbe.ReadyFor();
#endif
            return Enabled && (Records.Count == 0 || verifyRefresh) && advisor != null &&
                state.Spawn.Turn >= int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TURN") ?? "2", CultureInfo.InvariantCulture) &&
                !advisor.ReadyFor(state);
        }

        internal static bool TryChoose(BattleTurnState state, ManualLogSource log, out PlayCardAction? action)
        {
            action = null;
            if (!Enabled) return false;
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled) state = PlayerAdvisorProbe.Capture();
#endif
            logger = log;
            int start = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TURN") ?? "2", CultureInfo.InvariantCulture);
            if (Records.Count == 0)
            {
                if (state.Spawn.Turn < start) return false;
                if (state.Spawn.Turn != start) throw new InvalidOperationException("Missed planner root turn.");
                Prepare(state, log);
            }
            Record record = Records.Single();
            if (record.Completed) return false;
            if (waiting) Observe(state, false);
            if (record.Completed) return false;
            Step step = record.Steps[cursor++];
            waiting = true;
            action = step.Action;
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled) action = PlayerAdvisorProbe.NativeAction(action);
#endif
            return true; // A null action deliberately advances the native turn.
        }

        internal static void ObserveTerminal()
        {
            if (!Enabled || Records.Count == 0 || Records[0].Completed) return;
            if (!waiting || cursor != Records[0].Steps.Count)
                throw new InvalidOperationException("Native battle ended before the planned prefix completed.");
            Observe(FullBattleTrace.Active!.CaptureDecision(), true);
        }

        private static void Observe(BattleTurnState actual, bool terminal)
        {
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled) actual = terminal ? PlayerAdvisorProbe.Terminal : PlayerAdvisorProbe.Capture();
#endif
            Record record = Records.Single();
            BattleTurnState expected = record.Expected[cursor - 1];
            record.Actual.Add(actual);
            waiting = false;
            if (!JToken.DeepEquals(BattleTurnProbe.Comparable(expected, terminal), BattleTurnProbe.Comparable(actual, terminal)))
            {
                record.Difference = "Planner native state differs after step " + cursor;
                throw new InvalidOperationException(record.Difference);
            }
            if (cursor == record.Steps.Count)
            {
                record.Completed = true;
                logger!.LogInfo("PLANNER-REPLAY-PASS mode=" + record.Mode + " steps=" + cursor +
                    " horizon=" + record.Horizon + " score=" + record.Score + " baseline=" + record.BaselineScore +
                    " elapsedMs=" + record.ElapsedMs.ToString("F1", CultureInfo.InvariantCulture));
            }
        }

        private static void Prepare(BattleTurnState root, ManualLogSource log)
        {
            int horizon = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_HORIZON") ?? "3", CultureInfo.InvariantCulture);
            var record = new Record { Root = root, Horizon = horizon };
            string? input = Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_INPUT");
            if (!string.IsNullOrEmpty(input))
            {
                using var document = FixtureDocument.Read(input);
                FixtureValue original = document.RootElement.GetProperty("PlannerRuns").EnumerateArray().Single();
                BattleTurnState originalRoot = Read<BattleTurnState>(original.GetProperty("Root"));
                if (!JToken.DeepEquals(BattleTurnProbe.Comparable(originalRoot), BattleTurnProbe.Comparable(root)))
                    throw new InvalidOperationException("Fresh native planner replay root differs from the searched capture.");
                record.Mode = "replay";
                record.Horizon = original.GetProperty("Horizon").GetInt32();
                foreach (FixtureValue step in original.GetProperty("Steps").EnumerateArray())
                    record.Steps.Add(new Step { IsEndTurn = step.GetProperty("IsEndTurn").GetBoolean(),
                        Action = step.GetProperty("Action").ValueKind == FixtureKind.Null ? null : Read<PlayCardAction>(step.GetProperty("Action")) });
                record.Planned = Read<BattleTurnState>(original.GetProperty("Planned"));
                record.Score = original.GetProperty("Score").GetDouble();
                if (original.TryGetProperty("PolicyBaselineScore", out FixtureValue policy) && policy.ValueKind == FixtureKind.Number)
                    record.PolicyBaselineScore = policy.GetDouble();
            }
            else
            {
                using var keys = new BattleStateKeys();
                BattlePlanResult plan =
#if DEVELOPMENT_PROBE
                    PlayerAdvisorProbe.Enabled ? PlayerAdvisorProbe.Advice.Plan :
#endif
                    BattleAdvisorOverlay.Active != null ? BattleAdvisorOverlay.Active.CurrentPlan :
                    BattlePlanner.Plan(root, keys.Key,
                        new BattlePlannerOptions(256, horizon, 8,
                            timeLimit: TimeSpan.FromMilliseconds(int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TIME_LIMIT_MS") ?? "3000", CultureInfo.InvariantCulture)),
                            preferredDeploymentRoom: int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_DEPLOYMENT_ROOM") ?? "2", CultureInfo.InvariantCulture),
                            useTacticalSeed: Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TACTICAL_SEED") == "1"));
                if (!plan.CompletedHorizon || plan.UnsupportedAttempts.Count != 0 || plan.Steps.Count == 0)
                    throw new InvalidOperationException("Native planner root has no complete supported recommendation: " +
                        string.Join("; ", plan.UnsupportedAttempts.Select(attempt => attempt.Reason)));
                record.Steps = plan.Steps.Select(step => new Step { Action = step.Action, IsEndTurn = step.IsEndTurn }).ToList();
                record.Planned = plan.State; record.Score = plan.Score; record.Nodes = plan.ExpandedNodes;
                record.Duplicates = plan.DuplicateStates; record.BeamPruned = plan.BeamPruned;
                record.ElapsedMs = plan.Elapsed.TotalMilliseconds; record.TimedOut = plan.TimedOut;
                record.PolicyBaselineScore = plan.PolicyBaselineScore;
            }
            BattleTurnState current = root;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            foreach (Step step in record.Steps)
            {
                if (step.IsEndTurn)
                {
                    BattleTurnResult ended = BattleTurnModel.EndTurn(current);
                    if (!ended.Supported) throw new InvalidOperationException(ended.UnsupportedReason);
                    current = ended.State!; outcome = ended.Outcome;
                }
                else
                {
                    BattleActionResult played = BattleActionModel.PlayCard(current, step.Action!);
                    if (!played.Supported) throw new InvalidOperationException(played.Reason);
                    current = played.State!; outcome = played.Outcome;
                }
                record.Expected.Add(current); record.Outcomes.Add(outcome);
            }
            if (!JToken.DeepEquals(BattleTurnProbe.Comparable(current), BattleTurnProbe.Comparable(record.Planned)))
                throw new InvalidOperationException("Frozen planner sequence does not reproduce its planned state.");
            current = root;
            for (int turn = 0; turn < record.Horizon; turn++)
            {
                BattleTurnResult ended = BattleTurnModel.EndTurn(current);
                if (!ended.Supported) throw new InvalidOperationException(ended.UnsupportedReason);
                current = ended.State!; outcome = ended.Outcome;
                if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate) break;
            }
            record.BaselineScore = BattlePlanner.Evaluate(current, outcome);
            if (record.Score > record.BaselineScore) throw new InvalidOperationException("Planner recommendation lost its baseline.");
            if (record.PolicyBaselineScore.HasValue && record.Score > record.PolicyBaselineScore.Value)
                throw new InvalidOperationException("Planner recommendation lost its completed fixed-policy seed.");
            Records.Add(record);
            log.LogInfo("PLANNER-READY mode=" + record.Mode + " turn=" + root.Spawn.Turn + " steps=" + record.Steps.Count +
                " nodes=" + record.Nodes + " duplicates=" + record.Duplicates);
        }
        internal static T Read<T>(FixtureValue value) => Token(value).ToObject<T>()!;
        internal static T ReadWithoutNativeObservation<T>(FixtureValue value) =>
            new JObject(value.EnumerateObject().Where(property => property.Name is not "Actual" and not "Timing")
                .Select(property => new JProperty(property.Name, Token(property.Value)))).ToObject<T>()!;
        private static JToken Token(FixtureValue value)
        {
            if (value.ValueKind == FixtureKind.Object)
                return new JObject(value.EnumerateObject().Select(property => new JProperty(property.Name, Token(property.Value))));
            if (value.ValueKind == FixtureKind.Array) return new JArray(value.EnumerateArray().Select(Token));
            if (value.ValueKind == FixtureKind.Number) return JToken.Parse(value.GetNumberText());
            if (value.ValueKind == FixtureKind.String) return new JValue(value.GetString());
            if (value.ValueKind is FixtureKind.True or FixtureKind.False) return new JValue(value.GetBoolean());
            return JValue.CreateNull();
        }
    }
}
