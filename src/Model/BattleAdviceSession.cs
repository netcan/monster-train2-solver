using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MonsterTrain2Poju.Model
{
    public enum BattleAdviceStatus { Idle, Planning, Ready, Unavailable, Disposed }

    public sealed class BattleAdvice
    {
        public BattleTurnState Root { get; }
        public string RootKey { get; }
        public BattlePlanResult Plan { get; }
        public TimeSpan CaptureElapsed { get; }
        public TimeSpan QueueElapsed { get; }
        public TimeSpan VerificationElapsed { get; }
        private readonly long requestedAt;
        public TimeSpan ResponseElapsed => CaptureElapsed + Since(requestedAt);
        internal static TimeSpan Since(long timestamp) => TimeSpan.FromSeconds(
            (Stopwatch.GetTimestamp() - timestamp) / (double)Stopwatch.Frequency);
        internal BattleAdvice(BattleTurnState root, string key, BattlePlanResult plan,
            TimeSpan captureElapsed, TimeSpan queueElapsed, TimeSpan verificationElapsed, long requestedAt)
        {
            Root = root; RootKey = key; Plan = plan; CaptureElapsed = captureElapsed;
            QueueElapsed = queueElapsed; VerificationElapsed = verificationElapsed; this.requestedAt = requestedAt;
        }
    }

    // Owned by the UI thread. Only immutable model snapshots enter the single
    // worker; cancellation never waits on the UI and obsolete work cannot publish.
    public sealed class BattleAdviceSession : IDisposable
    {
        private readonly Func<BattleTurnState, CancellationToken, BattlePlanResult> search;
        private Task<BattleAdvice>? worker;
        private CancellationTokenSource? cancellation;
        private BattleTurnState? pending;
        private string? pendingKey, currentKey;
        private long generation, workerGeneration;
        private long pendingRequestedAt;
        private TimeSpan pendingCaptureElapsed;
        private bool disposed;
        public BattleAdviceStatus Status { get; private set; }
        public BattleAdvice? Current { get; private set; }
        public string? Error { get; private set; }
        public long Generation => generation;

        public BattleAdviceSession(BattlePlannerOptions? options = null)
            : this((root, token) =>
            {
                using var keys = new BattleStateKeys();
                return BattlePlanner.Plan(root, keys.Key, options ?? new BattlePlannerOptions(
                    256, 3, 8, timeLimit: TimeSpan.FromSeconds(3)), cancellationToken: token);
            }) { }

        internal BattleAdviceSession(Func<BattleTurnState, CancellationToken, BattlePlanResult> search)
        { this.search = search ?? throw new ArgumentNullException(nameof(search)); }

        public void Observe(BattleTurnState state, string fullStateKey, TimeSpan captureElapsed = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BattleAdviceSession));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrEmpty(fullStateKey)) throw new ArgumentException("A full-state key is required.", nameof(fullStateKey));
            if (fullStateKey != currentKey)
            {
                generation++; currentKey = fullStateKey;
                Current = null; Error = null;
                pending = state; pendingKey = fullStateKey;
                pendingRequestedAt = Stopwatch.GetTimestamp(); pendingCaptureElapsed = captureElapsed;
                cancellation?.Cancel();
                Status = BattleAdviceStatus.Planning;
            }
            Poll();
        }

        public void Invalidate()
        {
            if (disposed) return;
            if (currentKey != null) generation++;
            currentKey = pendingKey = null; pending = null;
            Current = null; Error = null; Status = BattleAdviceStatus.Idle;
            cancellation?.Cancel();
        }

        public void Poll()
        {
            if (disposed) return;
            if (worker != null && worker.IsCompleted)
            {
                try
                {
                    BattleAdvice advice = worker.GetAwaiter().GetResult();
                    if (workerGeneration == generation && advice.RootKey == currentKey)
                    { Current = advice; Status = BattleAdviceStatus.Ready; }
                }
                catch (Exception error)
                {
                    if (workerGeneration == generation && currentKey != null)
                    { Error = error.Message; Status = BattleAdviceStatus.Unavailable; }
                }
                finally
                { worker = null; cancellation!.Dispose(); cancellation = null; }
            }
            if (worker == null && pending != null)
            {
                BattleTurnState root = pending;
                string key = pendingKey!;
                long requestedAt = pendingRequestedAt;
                TimeSpan captureElapsed = pendingCaptureElapsed;
                pending = null; pendingKey = null;
                workerGeneration = generation;
                cancellation = new CancellationTokenSource();
                CancellationToken token = cancellation.Token;
                worker = Task.Run(() =>
                {
                    TimeSpan queued = BattleAdvice.Since(requestedAt);
                    BattlePlanResult plan = search(root, token);
                    token.ThrowIfCancellationRequested();
                    var verification = Stopwatch.StartNew();
                    Verify(root, plan);
                    verification.Stop();
                    token.ThrowIfCancellationRequested();
                    return new BattleAdvice(root, key, plan, captureElapsed, queued, verification.Elapsed, requestedAt);
                });
            }
        }

        internal static void Verify(BattleTurnState root, BattlePlanResult plan)
        {
            if (plan.Cancelled || !plan.CompletedHorizon || plan.Steps.Count == 0)
                throw new InvalidOperationException("No complete verified continuation within the planning budget.");
            BattleTurnState replayed = root;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            foreach (BattleSearchStep step in plan.Steps)
            {
                if (step.IsEndTurn)
                {
                    BattleTurnResult next = BattleTurnModel.EndTurn(replayed);
                    if (!next.Supported) throw new InvalidOperationException(next.UnsupportedReason);
                    replayed = next.State!; outcome = next.Outcome;
                }
                else
                {
                    if (step.Action == null) throw new InvalidOperationException("Missing planned action.");
                    BattleActionResult next = BattleActionModel.PlayCard(replayed, step.Action);
                    if (!next.Supported) throw new InvalidOperationException(next.Reason);
                    replayed = next.State!; outcome = next.Outcome;
                }
            }
            using var expected = new BattleStateKeys();
            using var actual = new BattleStateKeys();
            if (expected.Key(plan.State) != actual.Key(replayed) || plan.Outcome != outcome)
                throw new InvalidOperationException("Advice does not reproduce the full planned state.");
            if (plan.PolicyBaselineScore.HasValue && plan.Score > plan.PolicyBaselineScore.Value)
                throw new InvalidOperationException("Advice lost its completed policy baseline.");
        }

        public void Dispose()
        {
            if (disposed) return;
            Invalidate(); disposed = true; Status = BattleAdviceStatus.Disposed;
            // Observe exceptions and release the token source after atomic work
            // finishes, without waiting on Unity's thread or publishing a result.
            Task<BattleAdvice>? running = worker;
            CancellationTokenSource? source = cancellation;
            if (running != null) _ = running.ContinueWith(task =>
            { _ = task.Exception; source!.Dispose(); }, TaskScheduler.Default);
            worker = null; cancellation = null;
        }
    }
}
