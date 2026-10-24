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
        public TimeSpan PreparationElapsed { get; }
        public TimeSpan QueueElapsed { get; }
        public TimeSpan VerificationElapsed { get; }
        public bool ReusedWinningContinuation { get; }
        public bool ReusedContinuation { get; }
        public bool ReusedSelection { get; }
        internal VerifiedAdviceContinuation? Continuation { get; }
        internal Dictionary<string, SelectionAdviceReuse.Result> SelectionCache { get; }
        private readonly long requestedAt;
        public TimeSpan ResponseElapsed => CaptureElapsed + PreparationElapsed + Since(requestedAt);
        internal static TimeSpan Since(long timestamp) => TimeSpan.FromSeconds(
            (Stopwatch.GetTimestamp() - timestamp) / (double)Stopwatch.Frequency);
        internal BattleAdvice(BattleTurnState root, string key, BattlePlanResult plan,
            TimeSpan captureElapsed, TimeSpan queueElapsed, TimeSpan verificationElapsed, long requestedAt,
            VerifiedAdviceContinuation? continuation = null, bool reusedWinningContinuation = false, bool reusedSelection = false,
            bool reusedContinuation = false, TimeSpan preparationElapsed = default,
            Dictionary<string, SelectionAdviceReuse.Result>? selectionCache = null)
        {
            Root = root; RootKey = key; Plan = plan; CaptureElapsed = captureElapsed;
            PreparationElapsed = preparationElapsed;
            QueueElapsed = queueElapsed; VerificationElapsed = verificationElapsed; this.requestedAt = requestedAt;
            Continuation = continuation; ReusedWinningContinuation = reusedWinningContinuation;
            ReusedContinuation = reusedContinuation;
            ReusedSelection = reusedSelection;
            SelectionCache = selectionCache ?? new Dictionary<string, SelectionAdviceReuse.Result>(StringComparer.Ordinal);
        }
    }

    // Owned by the UI thread. Only immutable model snapshots enter the single
    // worker; cancellation never waits on the UI and obsolete work cannot publish.
    public sealed class BattleAdviceSession : IDisposable
    {
        private readonly Func<BattleTurnState, CancellationToken, BattlePlanResult> search;
        private readonly Action<BattleTurnState, BattlePlanResult> verify;
        private readonly bool retainPlans;
        private VerifiedAdviceContinuation? continuation;
        private Task<BattleAdvice>? worker;
        private CancellationTokenSource? cancellation;
        private BattleTurnState? pending;
        private BattleTurnState? observed;
        private BattleAdvice? reusable;
        private readonly List<SelectionAdviceReuse.Observation> reuseObservations = new List<SelectionAdviceReuse.Observation>();
        private int workerObservationCount;
        private string? pendingKey, currentKey;
        private long generation, workerGeneration;
        private long observationVersion;
        private long searchCount;
        private long pendingRequestedAt;
        private TimeSpan pendingCaptureElapsed;
        private TimeSpan pendingPreparationElapsed;
        private bool disposed;
        public BattleAdviceStatus Status { get; private set; }
        public BattleAdvice? Current { get; private set; }
        public string? Error { get; private set; }
        public long Generation => generation;
        public long SearchCount => Interlocked.Read(ref searchCount);

        // A view change records only its order. No hashing, model traversal,
        // worker cancellation or publication happens until a gameplay capture.
        public void BrowseRoom(int roomIndex)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BattleAdviceSession));
            if (roomIndex < 0) throw new ArgumentOutOfRangeException(nameof(roomIndex));
            var previous = reuseObservations.Count > 0 ? reuseObservations[reuseObservations.Count - 1] : null;
            int previousRoom = previous?.Root?.SelectedRoom ?? previous?.Room ?? observed?.SelectedRoom ?? -1;
            if (previousRoom != roomIndex) reuseObservations.Add(new SelectionAdviceReuse.Observation(roomIndex));
        }

        // Used only by isolated native acceptance, which supplies a fresh full
        // capture. The player does not call this while browsing or idling.
        internal bool MatchesCurrentRoot(BattleTurnState root)
        {
            if (Current == null) return false;
            if (reuseObservations.Count == 0)
            { using var keys = new BattleStateKeys(); return keys.Key(root) == Current.RootKey; }
            BattleTurnState projected = Current.Root;
            foreach (var observation in reuseObservations)
            {
                if (observation.Root != null) return false;
                BattleTurnResult selected = BattleTurnModel.SelectRoom(projected, observation.Room);
                if (!selected.Supported) return false;
                projected = selected.State!;
            }
            using var selectionKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            return selectionKeys.Key(projected) == selectionKeys.Key(root);
        }

        public BattleAdviceSession(BattlePlannerOptions? options = null, int maxSearchWorkers = 1)
            : this((root, token) =>
            {
                return ParallelBattlePlanner.Plan(root, options ?? new BattlePlannerOptions(
                    256, 3, 8, timeLimit: TimeSpan.FromSeconds(3)), maxSearchWorkers, token);
            }) { }

        internal BattleAdviceSession(Func<BattleTurnState, CancellationToken, BattlePlanResult> search)
            : this(search, Verify) { retainPlans = true; }

        internal BattleAdviceSession(Func<BattleTurnState, CancellationToken, BattlePlanResult> search,
            Action<BattleTurnState, BattlePlanResult> verify)
        {
            this.search = search ?? throw new ArgumentNullException(nameof(search));
            this.verify = verify ?? throw new ArgumentNullException(nameof(verify));
        }

        public void Observe(BattleTurnState state, string fullStateKey, TimeSpan captureElapsed = default)
            => Observe(state, fullStateKey, captureElapsed, null);

        // Start from the UI thread, hashing only the detached immutable snapshot
        // on a worker. The prior root is captured here, never read by that worker.
        public Task<PreparedBattleObservation> PrepareObservationAsync(BattleTurnState state)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BattleAdviceSession));
            if (state == null) throw new ArgumentNullException(nameof(state));
            BattleTurnState? previous = observed;
            string? previousKey = currentKey;
            long version = observationVersion;
            long requestedAt = Stopwatch.GetTimestamp();
            return Task.Run(() => new PreparedBattleObservation(state, previous, previousKey, version, requestedAt));
        }

        public void Observe(PreparedBattleObservation observation, TimeSpan captureElapsed = default)
        {
            if (observation == null) throw new ArgumentNullException(nameof(observation));
            if (observationVersion != observation.Version || !ReferenceEquals(observed, observation.Previous))
                throw new InvalidOperationException("Prepared observation belongs to an obsolete root.");
            Observe(observation.Root, observation.RootKey, captureElapsed, observation.SameBattle, observation.ResponseElapsed);
        }

        private void Observe(BattleTurnState state, string fullStateKey, TimeSpan captureElapsed, bool? sameBattle, TimeSpan preparationElapsed = default)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BattleAdviceSession));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrEmpty(fullStateKey)) throw new ArgumentException("A full-state key is required.", nameof(fullStateKey));
            if (fullStateKey != currentKey)
            {
                observationVersion++;
                bool browsing = sameBattle ?? (observed != null && SelectionAdviceReuse.SameBattle(observed, state));
                reusable = Current ?? reusable;
                if (!browsing) { generation++; cancellation?.Cancel(); }
                if (reusable != null) reuseObservations.Add(new SelectionAdviceReuse.Observation(state));
                currentKey = fullStateKey; observed = state;
                Current = null; Error = null;
                // A floor change must not cancel an in-flight search. Rebase
                // that verified result onto the latest settled root afterward.
                if (!browsing || worker == null || pending != null)
                { pending = state; pendingKey = fullStateKey; }
                pendingRequestedAt = Stopwatch.GetTimestamp(); pendingCaptureElapsed = captureElapsed;
                pendingPreparationElapsed = preparationElapsed;
                Status = BattleAdviceStatus.Planning;
            }
            Poll();
        }

        public void Invalidate() => Invalidate(false);

        // Input/animation boundaries hide the old publication immediately, but
        // may retain an immutable proof until the next settled state is known.
        public void InvalidateForInput() => Invalidate(true);

        private void Invalidate(bool preserveContinuation)
        {
            if (disposed) return;
            observationVersion++;
            if (!preserveContinuation) { continuation = null; reusable = null; reuseObservations.Clear(); }
            else reusable = Current ?? reusable;
            if (reusable == null && continuation == null) reuseObservations.Clear();
            if (currentKey != null) generation++;
            currentKey = pendingKey = null; pending = null;
            observed = null;
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
                        {
                            Current = reusable = advice; continuation = advice.Continuation;
                            // Starting or cancelling verification must not consume
                            // the anchor/history. Commit only this accepted prefix;
                            // later view callbacks still belong to the new root.
                            reuseObservations.RemoveRange(0, workerObservationCount);
                            Status = BattleAdviceStatus.Ready;
                        }
                        else
                        {
                            reusable = advice;
                            reuseObservations.RemoveRange(0, workerObservationCount);
                            pending = observed; pendingKey = currentKey;
                        }
                    }
                }
                catch (Exception error)
                {
                    if (workerGeneration == generation && currentKey != null)
                    { continuation = null; Error = error.Message; Status = BattleAdviceStatus.Unavailable; }
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
                TimeSpan preparationElapsed = pendingPreparationElapsed;
                VerifiedAdviceContinuation? retained = continuation;
                BattleAdvice? candidate = reusable;
                var observations = candidate == null ? Array.Empty<SelectionAdviceReuse.Observation>() : reuseObservations.ToArray();
                workerObservationCount = observations.Length;
                pending = null; pendingKey = null;
                workerGeneration = generation;
                cancellation = new CancellationTokenSource();
                CancellationToken token = cancellation.Token;
                worker = Task.Run(() =>
                {
                    TimeSpan queued = BattleAdvice.Since(requestedAt);
                    token.ThrowIfCancellationRequested();
                    BattlePlanResult plan = null!;
                    VerifiedAdviceContinuation? proof = null;
                    var verification = new Stopwatch();
                    verification.Start();
                    using var fullKeys = new BattleStateKeys();
                    if (fullKeys.Key(root) != key)
                        throw new InvalidOperationException(retained == null ? "Captured state differs from its full-state key." :
                            "Captured state differs from the retained continuation.");
                    VerifiedAdviceContinuation? matched = candidate != null && observations.Any(item => item.Root == null)
                        ? SelectionAdviceReuse.MatchAfterObservations(candidate, observations, root, token) : retained?.Match(root);
                    bool reusedSelection = matched == null && candidate != null && SelectionAdviceReuse.TryRebase(candidate, root,
                        retainPlans, token, out plan, out proof);
                    verification.Stop();
                    if (reusedSelection)
                    {
                        verification.Start();
                        if (!retainPlans) verify(root, plan!);
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
                        if (retainPlans) proof = VerifyAndRetain(root, plan, true, token);
                        else { verify(root, plan); proof = null; }
                    }
                    verification.Stop();
                    token.ThrowIfCancellationRequested();
                    return new BattleAdvice(root, key, plan, captureElapsed, queued, verification.Elapsed, requestedAt,
                        proof, reusedWinningContinuation: matched?.IsWinning == true, reusedSelection: reusedSelection,
                        reusedContinuation: matched != null, preparationElapsed: preparationElapsed,
                        selectionCache: reusedSelection || matched != null && retained != null && matched.SamePosition(retained)
                            ? candidate?.SelectionCache : null);
                });
            }
        }

        internal static void Verify(BattleTurnState root, BattlePlanResult plan) => VerifyAndRetain(root, plan, false);

        private static VerifiedAdviceContinuation? VerifyAndRetain(BattleTurnState root, BattlePlanResult plan,
            bool retain, CancellationToken token = default)
        {
            if (plan.Cancelled || (!plan.CompletedHorizon && !plan.UsedShorterHorizon) || plan.Steps.Count == 0)
                throw new InvalidOperationException("No complete verified continuation within the planning budget. turns=" +
                    plan.TurnsCompleted + " nodes=" + plan.ExpandedNodes + " elapsedMs=" + plan.Elapsed.TotalMilliseconds +
                    " reasons=" + string.Join("; ", plan.UnsupportedAttempts.Select(attempt => attempt.Reason)));
            if (plan.UsedShorterHorizon && (plan.CompletedHorizon || plan.TurnsCompleted < 1 ||
                plan.TurnsCompleted >= plan.RequestedTurns || (!plan.TimedOut && !plan.ReachedNodeLimit && !plan.ReachedStepLimit) ||
                plan.PolicyBaselineScore.HasValue || !plan.Steps[plan.Steps.Count - 1].IsEndTurn ||
                plan.Steps.Count(step => step.IsEndTurn) != plan.TurnsCompleted ||
                plan.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate))
                throw new InvalidOperationException("Invalid shorter planning window.");
            if (plan.Steps.Count > plan.MaxSteps || plan.SearchedToBattleEnd && plan.CompletedHorizon &&
                plan.Outcome is not (RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate))
                throw new InvalidOperationException("Whole-battle advice must reach a terminal outcome within its step limit.");
            BattleTurnState replayed = root;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            using var keys = new BattleStateKeys();
            using var continuationKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            var roots = retain && VerifiedAdviceContinuation.CanRetain(plan) ? new List<string>() : null;
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
            return roots == null ? null : new VerifiedAdviceContinuation(plan, roots.ToArray());
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
