using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public bool ReusedWinningContinuation { get; }
        public bool ReusedSelection { get; }
        internal WinningAdviceContinuation? WinningContinuation { get; }
        private readonly long requestedAt;
        public TimeSpan ResponseElapsed => CaptureElapsed + Since(requestedAt);
        internal static TimeSpan Since(long timestamp) => TimeSpan.FromSeconds(
            (Stopwatch.GetTimestamp() - timestamp) / (double)Stopwatch.Frequency);
        internal BattleAdvice(BattleTurnState root, string key, BattlePlanResult plan,
            TimeSpan captureElapsed, TimeSpan queueElapsed, TimeSpan verificationElapsed, long requestedAt,
            WinningAdviceContinuation? winningContinuation = null, bool reusedWinningContinuation = false, bool reusedSelection = false)
        {
            Root = root; RootKey = key; Plan = plan; CaptureElapsed = captureElapsed;
            QueueElapsed = queueElapsed; VerificationElapsed = verificationElapsed; this.requestedAt = requestedAt;
            WinningContinuation = winningContinuation; ReusedWinningContinuation = reusedWinningContinuation;
            ReusedSelection = reusedSelection;
        }
    }

    // Owned by the UI thread. Only immutable model snapshots enter the single
    // worker; cancellation never waits on the UI and obsolete work cannot publish.
    public sealed class BattleAdviceSession : IDisposable
    {
        private readonly Func<BattleTurnState, CancellationToken, BattlePlanResult> search;
        private readonly Action<BattleTurnState, BattlePlanResult> verify;
        private readonly bool retainWinningPlans;
        private WinningAdviceContinuation? winningContinuation;
        private Task<BattleAdvice>? worker;
        private CancellationTokenSource? cancellation;
        private BattleTurnState? pending;
        private BattleTurnState? observed;
        private BattleAdvice? reusable;
        private string? pendingKey, currentKey;
        private long generation, workerGeneration;
        private long searchCount;
        private long pendingRequestedAt;
        private TimeSpan pendingCaptureElapsed;
        private bool disposed;
        public BattleAdviceStatus Status { get; private set; }
        public BattleAdvice? Current { get; private set; }
        public string? Error { get; private set; }
        public long Generation => generation;
        public long SearchCount => Interlocked.Read(ref searchCount);

        public BattleAdviceSession(BattlePlannerOptions? options = null)
            : this((root, token) =>
            {
                using var keys = new BattleStateKeys();
                return BattlePlanner.Plan(root, keys.Key, options ?? new BattlePlannerOptions(
                    256, 3, 8, timeLimit: TimeSpan.FromSeconds(3)), cancellationToken: token);
            }) { }

        internal BattleAdviceSession(Func<BattleTurnState, CancellationToken, BattlePlanResult> search)
            : this(search, Verify) { retainWinningPlans = true; }

        internal BattleAdviceSession(Func<BattleTurnState, CancellationToken, BattlePlanResult> search,
            Action<BattleTurnState, BattlePlanResult> verify)
        {
            this.search = search ?? throw new ArgumentNullException(nameof(search));
            this.verify = verify ?? throw new ArgumentNullException(nameof(verify));
        }

        public void Observe(BattleTurnState state, string fullStateKey, TimeSpan captureElapsed = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BattleAdviceSession));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrEmpty(fullStateKey)) throw new ArgumentException("A full-state key is required.", nameof(fullStateKey));
            if (fullStateKey != currentKey)
            {
                bool browsing = observed != null && SelectionAdviceReuse.SameBattle(observed, state);
                if (browsing) reusable = Current ?? reusable;
                else { generation++; reusable = null; cancellation?.Cancel(); }
                currentKey = fullStateKey; observed = state;
                Current = null; Error = null;
                // A floor change must not cancel an in-flight search. Rebase
                // that verified result onto the latest settled root afterward.
                if (!browsing || worker == null || pending != null)
                { pending = state; pendingKey = fullStateKey; }
                pendingRequestedAt = Stopwatch.GetTimestamp(); pendingCaptureElapsed = captureElapsed;
                Status = BattleAdviceStatus.Planning;
            }
            Poll();
        }

        public void Invalidate() => Invalidate(false);

        // Input/animation boundaries hide the old publication immediately, but
        // may retain an immutable proof until the next settled state is known.
        public void InvalidateForInput() => Invalidate(true);

        private void Invalidate(bool preserveWinningContinuation)
        {
            if (disposed) return;
            if (!preserveWinningContinuation) winningContinuation = null;
            if (currentKey != null) generation++;
            currentKey = pendingKey = null; pending = null;
            observed = null; reusable = null;
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
                    if (workerGeneration == generation && currentKey != null)
                    {
                        if (advice.RootKey == currentKey)
                        { Current = advice; winningContinuation = advice.WinningContinuation; Status = BattleAdviceStatus.Ready; }
                        else
                        { reusable = advice; pending = observed; pendingKey = currentKey; }
                    }
                }
                catch (Exception error)
                {
                    if (workerGeneration == generation && currentKey != null)
                    { winningContinuation = null; Error = error.Message; Status = BattleAdviceStatus.Unavailable; }
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
                WinningAdviceContinuation? retained = winningContinuation;
                BattleAdvice? candidate = reusable;
                reusable = null;
                pending = null; pendingKey = null;
                workerGeneration = generation;
                cancellation = new CancellationTokenSource();
                CancellationToken token = cancellation.Token;
                worker = Task.Run(() =>
                {
                    TimeSpan queued = BattleAdvice.Since(requestedAt);
                    token.ThrowIfCancellationRequested();
                    BattlePlanResult plan = null!;
                    WinningAdviceContinuation? proof = null;
                    var verification = new Stopwatch();
                    verification.Start();
                    using var fullKeys = new BattleStateKeys();
                    if (fullKeys.Key(root) != key)
                        throw new InvalidOperationException(retained == null ? "Captured state differs from its full-state key." :
                            "Captured state differs from the retained winning continuation.");
                    bool reusedSelection = candidate != null && SelectionAdviceReuse.TryRebase(candidate, root,
                        retainWinningPlans, token, out plan, out proof);
                    WinningAdviceContinuation? matched = reusedSelection ? null : retained?.Match(root);
                    verification.Stop();
                    if (reusedSelection)
                    {
                        verification.Start();
                        if (!retainWinningPlans) verify(root, plan!);
                    }
                    else if (matched != null)
                    {
                        verification.Start();
                        plan = matched.Remaining(); proof = matched;
                    }
                    else
                    {
                        Interlocked.Increment(ref searchCount);
                        plan = search(root, token);
                        token.ThrowIfCancellationRequested();
                        verification.Start();
                        if (retainWinningPlans) proof = VerifyAndRetain(root, plan, true, token);
                        else { verify(root, plan); proof = null; }
                    }
                    verification.Stop();
                    token.ThrowIfCancellationRequested();
                    return new BattleAdvice(root, key, plan, captureElapsed, queued, verification.Elapsed, requestedAt,
                        proof, reusedWinningContinuation: matched != null, reusedSelection: reusedSelection);
                });
            }
        }

        internal static void Verify(BattleTurnState root, BattlePlanResult plan) => VerifyAndRetain(root, plan, false);

        private static WinningAdviceContinuation? VerifyAndRetain(BattleTurnState root, BattlePlanResult plan,
            bool retain, CancellationToken token = default)
        {
            if (plan.Cancelled || (!plan.CompletedHorizon && !plan.UsedShorterHorizon) || plan.Steps.Count == 0)
                throw new InvalidOperationException("No complete verified continuation within the planning budget. turns=" +
                    plan.TurnsCompleted + " nodes=" + plan.ExpandedNodes + " elapsedMs=" + plan.Elapsed.TotalMilliseconds +
                    " reasons=" + string.Join("; ", plan.UnsupportedAttempts.Select(attempt => attempt.Reason)));
            if (plan.UsedShorterHorizon && (plan.CompletedHorizon || plan.TurnsCompleted < 1 ||
                plan.TurnsCompleted >= plan.RequestedTurns || (!plan.TimedOut && !plan.ReachedNodeLimit) ||
                plan.PolicyBaselineScore.HasValue || !plan.Steps[plan.Steps.Count - 1].IsEndTurn ||
                plan.Steps.Count(step => step.IsEndTurn) != plan.TurnsCompleted ||
                plan.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate))
                throw new InvalidOperationException("Invalid shorter planning window.");
            BattleTurnState replayed = root;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            using var keys = new BattleStateKeys();
            using var continuationKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            var roots = retain && plan.Outcome == RoomOutcome.BattleWon ? new List<string>() : null;
            foreach (BattleSearchStep step in plan.Steps)
            {
                token.ThrowIfCancellationRequested();
                roots?.Add(continuationKeys.Key(replayed));
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
            if (keys.Key(plan.State) != keys.Key(replayed) || plan.Outcome != outcome)
                throw new InvalidOperationException("Advice does not reproduce the full planned state.");
            if (plan.PolicyBaselineScore.HasValue && plan.Score > plan.PolicyBaselineScore.Value)
                throw new InvalidOperationException("Advice lost its completed policy baseline.");
            return roots == null ? null : new WinningAdviceContinuation(plan, roots.ToArray());
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
