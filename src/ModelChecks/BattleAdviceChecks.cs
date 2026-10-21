using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleAdviceChecks
{
    internal static void Run(FixtureValue fixture)
    {
        FixtureValue record = fixture.GetProperty("PlannerRuns").EnumerateArray().First();
        BattleTurnState root = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState[] following = record.GetProperty("Expected").EnumerateArray().Take(2)
            .Select(value => value.Deserialize<BattleTurnState>()!).ToArray();
        BattleTurnState second = following[0], latest = following[1];
        string Key(BattleTurnState state) { using var key = new BattleStateKeys(); return key.Key(state); }
        BattlePlanResult Plan(BattleTurnState state)
        {
            using var keys = new BattleStateKeys();
            return BattlePlanner.Plan(state, keys.Key, new BattlePlannerOptions(2, 2, 1, timeLimit: TimeSpan.FromSeconds(10)));
        }
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Pump(BattleAdviceSession session, BattleAdviceStatus status)
        {
            Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status == status || session.Status == BattleAdviceStatus.Unavailable; }, 10000), "Advice worker did not finish.");
            Require(session.Status == status, "Advice worker failed: " + session.Error);
        }

        string unchanged = JsonSerializer.Serialize(root);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int calls = 0, active = 0, peak = 0;
        using (var session = new BattleAdviceSession((state, token) =>
        {
            int running = Interlocked.Increment(ref active); peak = Math.Max(peak, running);
            try
            {
                if (Interlocked.Increment(ref calls) == 1)
                { entered.Set(); if (!release.Wait(10000)) throw new TimeoutException(); }
                // Deliberately ignore cancellation: publication must still reject it.
                return Plan(state);
            }
            finally { Interlocked.Decrement(ref active); }
        }))
        {
            try
            {
                session.Observe(root, Key(root));
                Require(entered.Wait(10000), "Advice worker did not start.");
                session.Observe(second, Key(second)); session.Observe(latest, Key(latest));
                Require(session.Current == null, "Obsolete advice remained visible.");
                release.Set(); Pump(session, BattleAdviceStatus.Ready);
                Require(ReferenceEquals(session.Current!.Root, latest) && session.Current.RootKey == Key(latest), "Stale worker result was published.");
                Require(calls == 2 && peak == 1, "Advice work overlapped or queued intermediate snapshots.");
                Require(session.Current.Plan.CompletedHorizon && session.Current.Plan.ReachedNodeLimit, "Budget fallback did not complete its horizon.");
                session.Observe(latest, Key(latest));
                Require(calls == 2 && session.Current != null, "Identical observation restarted search.");
                session.Invalidate();
                Require(session.Current == null && session.Status == BattleAdviceStatus.Idle, "Player action did not invalidate advice.");
            }
            finally { release.Set(); }
        }
        using (var failing = new BattleAdviceSession((_, _) => throw new InvalidOperationException("unsupported test state")))
        {
            failing.Observe(root, Key(root)); Pump(failing, BattleAdviceStatus.Unavailable);
            Require(failing.Current == null && failing.Error == "unsupported test state", "Failure exposed an executable recommendation.");
            failing.Invalidate();
            Require(failing.Error == null, "Failure survived invalidation.");
        }
        using var disposingEntered = new ManualResetEventSlim();
        using var disposingRelease = new ManualResetEventSlim();
        var disposing = new BattleAdviceSession((state, _) =>
        { disposingEntered.Set(); disposingRelease.Wait(10000); return Plan(state); });
        try
        {
            disposing.Observe(root, Key(root)); Require(disposingEntered.Wait(10000), "Dispose worker did not start.");
            disposing.Dispose();
            Require(disposing.Status == BattleAdviceStatus.Disposed && disposing.Current == null, "Disposed advice remained visible.");
        }
        finally { disposingRelease.Set(); }
        Require(JsonSerializer.Serialize(root) == unchanged, "Advice modified its root snapshot.");
        Console.WriteLine("BATTLE-ADVICE PASS: stale result rejection, latest snapshot coalescing, one worker, cancellation, verified budget fallback, failure and nonblocking disposal.");
    }
}
