using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TrueFinalBossHealthState
    {
        public LinkedHealthState Health { get; }
        public LifeLinkContext LifeLink { get; }
        public TrueFinalBossHealthState(LinkedHealthState health, LifeLinkContext lifeLink) { Health = health; LifeLink = lifeLink; }
    }
    public sealed class HealthChangedEvent
    {
        public int Id { get; }
        public int Health { get; }
        public HealthChangedEvent(int id, int health) { Id = id; Health = health; }
    }
    public sealed class TrueFinalBossHealthResult
    {
        public TrueFinalBossHealthState? State { get; }
        public IReadOnlyList<HealthChangedEvent> Events { get; }
        public LifeLinkDamageResult? DamageStage { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public TrueFinalBossHealthResult(TrueFinalBossHealthState? state, IReadOnlyList<HealthChangedEvent> events,
            LifeLinkDamageResult? damageStage, string? unsupportedReason = null)
        { State = state; Events = Array.AsReadOnly(events.ToArray()); DamageStage = damageStage; UnsupportedReason = unsupportedReason; }
    }
    // Synchronous native health updates on non-preview actors. The life-link
    // stage can precede DebuffHP; the full damage/heal coroutine is not run here.
    public static class TrueFinalBossHealthModel
    {
        public static TrueFinalBossHealthResult Mutate(TrueFinalBossHealthState source, int targetId, string operation, int value)
        {
            TrueFinalBossHealthResult Reject(string reason) => new TrueFinalBossHealthResult(null, Array.Empty<HealthChangedEvent>(), null, reason);
            LinkedHealthResult read = LinkedHealthModel.Read(source.Health);
            string? invalid = LifeLinkModel.Validate(source.LifeLink);
            if (!read.Supported || invalid != null) return Reject(read.UnsupportedReason ?? invalid!);
            if (!source.Health.Actors.Select(actor => actor.Id).SequenceEqual(source.LifeLink.Actors.Select(actor => actor.Id)) ||
                source.LifeLink.Actors.Any(actor => !actor.Initialized || !actor.TrueFinalBoss || actor.Health <= 0 ||
                    actor.Health != read.Snapshot!.Values.Single(item => item.Id == actor.Id).Health ||
                    actor.LinkedId != source.Health.Actors.Single(item => item.Id == actor.Id).LinkedId))
                return Reject("Health updates require matching captured, living, non-preview true-final-boss actors.");
            LifeLinkActor? target = source.LifeLink.Actors.SingleOrDefault(actor => actor.Id == targetId);
            if (target == null || operation is not ("UpdateHp" or "DebuffHP" or "LifeLinkAndDebuffHP"))
                return Reject("Missing target or uncalibrated native health operation.");
            LifeLinkDamageResult? damage = null;
            int amount = value;
            if (operation == "LifeLinkAndDebuffHP")
            {
                if (!target.HasLifeLink) return Reject("Life-link stage requires the original status on its target.");
                damage = LifeLinkModel.Test(source.LifeLink, targetId, amount);
                amount = damage.Damage;
            }
            int next = operation == "UpdateHp" ? amount : amount > 0 ? Math.Max(0, unchecked(target.Health - amount)) : target.Health;
            if (next <= 0) return Reject("Terminal true-final-boss health updates require native death/removal calibration.");
            if (next == target.Health) return new TrueFinalBossHealthResult(source, Array.Empty<HealthChangedEvent>(), damage);
            int change = source.LifeLink.SharedPreviewHpChange;
            if (target.OuterBoss) change = unchecked(change + target.Health - next);
            LinkedHealthSnapshot updated = LinkedHealthModel.Write(source.Health, targetId, "Hp", next).Snapshot!;
            var context = new LifeLinkContext(source.LifeLink.Actors.Select(actor => new LifeLinkActor(actor.Id, actor.LinkedId,
                updated.Values.Single(item => item.Id == actor.Id).Health, actor.PreviewHealth, actor.Initialized,
                actor.HasLifeLink, actor.TrueFinalBoss, actor.OuterBoss)).ToArray(), source.LifeLink.CalculatingPreview, change);
            return new TrueFinalBossHealthResult(new TrueFinalBossHealthState(updated.State, context),
                new[] { new HealthChangedEvent(targetId, updated.Values.Single(item => item.Id == targetId).Health) }, damage);
        }
    }
}
