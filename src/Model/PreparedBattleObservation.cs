using System;
using System.Diagnostics;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    // Contains immutable model values only. Preparation never reads native or
    // Unity objects; the caller must reject a result invalidated by later input.
    public sealed class PreparedBattleObservation
    {
        public BattleTurnState Root { get; }
        public string RootKey { get; }
        public TimeSpan Elapsed { get; }
        public int ThreadId { get; }
        internal BattleTurnState? Previous { get; }
        internal bool SameBattle { get; }
        internal long Version { get; }
        private readonly long requestedAt;
        internal TimeSpan ResponseElapsed => BattleAdvice.Since(requestedAt);

        internal PreparedBattleObservation(BattleTurnState root, BattleTurnState? previous, string? previousKey, long version, long requestedAt)
        {
            var timer = Stopwatch.StartNew();
            Root = root; Previous = previous; Version = version;
            this.requestedAt = requestedAt;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            using var keys = new BattleStateKeys();
            RootKey = keys.Key(root);
            SameBattle = previous != null && (RootKey == previousKey || SelectionAdviceReuse.SameBattle(previous, root));
            Elapsed = timer.Elapsed;
        }
    }
}
