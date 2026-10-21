using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Only immutable snapshots cross this process boundary. The .NET worker
    // searches and independently replays; Unity still owns every native action.
    internal sealed class ExternalBattlePlanner
    {
        private readonly string tool;
        private readonly int horizon, milliseconds, floor;
        private readonly bool tactical;
        private FixtureValue originalRoot, externalRoot, externalFinal;
        private BattlePlanResult? verified;
        internal ExternalBattlePlanner(string tool, int horizon, int milliseconds, int floor, bool tactical)
        { this.tool = Path.GetFullPath(tool); this.horizon = horizon; this.milliseconds = milliseconds; this.floor = floor; this.tactical = tactical; }
        internal BattlePlanResult Search(BattleTurnState root, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var elapsed = Stopwatch.StartNew();
            string prefix = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "advisor-" + Guid.NewGuid().ToString("N"));
            string input = prefix + ".input.mt2f", output = prefix + ".output.mt2f";
            using (var request = NativeFixtureCapture.Capture(new { Schema = 1, Root = root, Horizon = horizon,
                Milliseconds = milliseconds, DeploymentRoom = floor, TacticalSeed = tactical }))
            {
                originalRoot = request.RootElement.GetProperty("Root");
                using var stream = new FileStream(input, FileMode.CreateNew, FileAccess.Write);
                request.Write(stream);
            }
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                Arguments = Quote(tool) + " plan-once " + Quote(input) + " " + Quote(output) };
            using (var process = Process.Start(start) ?? throw new InvalidOperationException("External planner did not start."))
            {
                var standardOutput = process.StandardOutput.ReadToEndAsync();
                var standardError = process.StandardError.ReadToEndAsync();
                try
                {
                    while (!process.WaitForExit(100))
                    {
                        token.ThrowIfCancellationRequested();
                        if (elapsed.Elapsed.TotalMilliseconds > milliseconds * 3d + 60000)
                            throw new TimeoutException("External planner process exceeded its search/verification deadline.");
                    }
                    if (process.ExitCode != 0) throw new InvalidOperationException("External planner failed: " + standardError.GetAwaiter().GetResult());
                    standardOutput.GetAwaiter().GetResult();
                }
                catch { if (!process.HasExited) process.Kill(); throw; }
            }
            token.ThrowIfCancellationRequested();
            using var document = FixtureDocument.Read(output);
            FixtureValue result = document.RootElement;
            if (result.GetProperty("Schema").GetInt32() != 1 || !result.GetProperty("Verified").GetBoolean())
                throw new InvalidOperationException("External planner did not verify its complete continuation.");
            externalRoot = result.GetProperty("Root"); externalFinal = result.GetProperty("Plan").GetProperty("State");
            WirePlan plan = PlannerReplayScenario.Read<WirePlan>(result.GetProperty("Plan"));
            elapsed.Stop();
            verified = new BattlePlanResult(plan.State, plan.Outcome,
                plan.Steps.Select(step => new BattleSearchStep(step.Action, step.IsEndTurn)).ToArray(), plan.Score, plan.TurnsCompleted,
                plan.ExpandedNodes, plan.DuplicateStates, plan.BeamPruned, elapsed.Elapsed, plan.TimedOut, plan.Cancelled,
                plan.ReachedNodeLimit, plan.ReachedActionLimit, plan.CompletedHorizon, plan.UnsupportedAttempts, plan.PolicyBaselineScore);
            return verified;
        }
        internal void Verify(BattleTurnState root, BattlePlanResult plan)
        {
            using var source = NativeFixtureCapture.Capture(root);
            using var destination = NativeFixtureCapture.Capture(plan.State);
            if (!originalRoot.ContentEquals(externalRoot) || !source.RootElement.ContentEquals(originalRoot))
                throw new InvalidOperationException("External planner changed the complete request state or RNG.");
            if (!destination.RootElement.ContentEquals(externalFinal))
                throw new InvalidOperationException("External planner result changed during native-runtime decoding.");
            if (!ReferenceEquals(verified, plan) || !plan.CompletedHorizon || plan.Cancelled || plan.Steps.Count == 0 ||
                plan.ExpandedNodes > 256 || plan.TurnsCompleted > horizon ||
                BattlePlanner.Evaluate(plan.State, plan.Outcome) != plan.Score)
                throw new InvalidOperationException("External planner result identity, bounds or score differ.");
        }
        private static string Quote(string path) => "\"" + path + "\"";
        private sealed class WirePlan
        {
            public BattleTurnState State { get; set; } = null!;
            public RoomOutcome Outcome { get; set; }
            public PlannerReplayScenario.Step[] Steps { get; set; } = Array.Empty<PlannerReplayScenario.Step>();
            public double Score { get; set; }
            public int TurnsCompleted { get; set; }
            public int ExpandedNodes { get; set; }
            public int DuplicateStates { get; set; }
            public int BeamPruned { get; set; }
            public bool TimedOut { get; set; }
            public bool Cancelled { get; set; }
            public bool ReachedNodeLimit { get; set; }
            public bool ReachedActionLimit { get; set; }
            public bool CompletedHorizon { get; set; }
            public UnsupportedActionAttempt[] UnsupportedAttempts { get; set; } = Array.Empty<UnsupportedActionAttempt>();
            public double? PolicyBaselineScore { get; set; }
        }
    }
}
