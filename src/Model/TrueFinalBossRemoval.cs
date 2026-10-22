using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TrueFinalBossRemovalActor
    {
        public int Id { get; }
        public int Room { get; }
        public bool TrueFinalBoss { get; }
        public TrueFinalBossRemovalActor(int id, int room, bool trueFinalBoss) { Id = id; Room = room; TrueFinalBoss = trueFinalBoss; }
    }
    public sealed class TrueFinalBossRemovalStep
    {
        public int Id { get; }
        public int Room { get; }
        public IReadOnlyList<int> ActiveBefore { get; }
        public bool Death { get; }
        public bool Vanish { get; }
        public bool LastDeath { get; }
        public bool IgnoreTriggers { get; }
        public bool DoNotFullyRemove { get; }
        public bool FireRemovedSignal { get; }
        public TrueFinalBossRemovalStep(int id, int room, IReadOnlyList<int> activeBefore, bool death, bool vanish,
            bool lastDeath, bool ignoreTriggers, bool doNotFullyRemove, bool fireRemovedSignal)
        {
            Id = id; Room = room; ActiveBefore = Array.AsReadOnly(activeBefore.ToArray()); Death = death; Vanish = vanish;
            LastDeath = lastDeath; IgnoreTriggers = ignoreTriggers; DoNotFullyRemove = doNotFullyRemove; FireRemovedSignal = fireRemovedSignal;
        }
    }
    public sealed class TrueFinalBossRemovalPlan
    {
        public IReadOnlyList<TrueFinalBossRemovalStep> Steps { get; }
        public IReadOnlyList<int> ActiveAfter { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal TrueFinalBossRemovalPlan(IReadOnlyList<TrueFinalBossRemovalStep> steps, IReadOnlyList<int> activeAfter, string? reason = null)
        { Steps = Array.AsReadOnly(steps.ToArray()); ActiveAfter = Array.AsReadOnly(activeAfter.ToArray()); UnsupportedReason = reason; }
    }
    // Native RemoveTrueFinalBoss group membership, order and forwarded flags.
    // Health/death signals, kill camera and encounter completion are separate.
    public static class TrueFinalBossRemovalModel
    {
        public static TrueFinalBossRemovalPlan Plan(IReadOnlyList<TrueFinalBossRemovalActor> actors, int targetId,
            bool death, bool vanish, bool lastDeath, bool ignoreTriggers = false, bool doNotFullyRemove = false, bool fireRemovedSignal = false)
        {
            if (actors.Any(actor => actor.Id <= 0 || actor.Room < 0 || actor.Room > 3) || actors.Select(actor => actor.Id).Distinct().Count() != actors.Count ||
                actors.Count(actor => actor.TrueFinalBoss) != 5 || !actors.Any(actor => actor.Id == targetId && actor.TrueFinalBoss))
                return new TrueFinalBossRemovalPlan(Array.Empty<TrueFinalBossRemovalStep>(), Array.Empty<int>(),
                    "Native TFB removal requires a complete original five-actor group, distinct identities and a captured root target.");
            // The calibrated original five-actor removal preserves active-list
            // order for peers on the same floor. Larger groups remain guarded.
            TrueFinalBossRemovalActor[] ordered = actors.Where(actor => actor.TrueFinalBoss).OrderByDescending(actor => actor.Room).ToArray();
            var active = actors.Select(actor => actor.Id).ToList();
            var steps = new List<TrueFinalBossRemovalStep>();
            for (int index = 0; index < ordered.Length; index++)
            {
                TrueFinalBossRemovalActor actor = ordered[index];
                steps.Add(new TrueFinalBossRemovalStep(actor.Id, actor.Room, active, death, vanish,
                    lastDeath && index == ordered.Length - 1, ignoreTriggers, doNotFullyRemove, fireRemovedSignal));
                active.Remove(actor.Id);
            }
            return new TrueFinalBossRemovalPlan(steps, active);
        }
    }
}
