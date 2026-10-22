using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class LifeLinkActor
    {
        public int Id { get; }
        public int LinkedId { get; }
        public int Health { get; }
        public int PreviewHealth { get; }
        public bool Initialized { get; }
        public bool HasLifeLink { get; }
        public bool TrueFinalBoss { get; }
        public bool OuterBoss { get; }
        public LifeLinkActor(int id, int linkedId, int health, int previewHealth, bool initialized,
            bool hasLifeLink, bool trueFinalBoss, bool outerBoss)
        {
            Id = id; LinkedId = linkedId; Health = health; PreviewHealth = previewHealth;
            Initialized = initialized; HasLifeLink = hasLifeLink; TrueFinalBoss = trueFinalBoss; OuterBoss = outerBoss;
        }
    }
    public sealed class LifeLinkContext
    {
        public IReadOnlyList<LifeLinkActor> Actors { get; }
        public bool CalculatingPreview { get; }
        public int SharedPreviewHpChange { get; }
        public LifeLinkContext(IReadOnlyList<LifeLinkActor> actors, bool calculatingPreview, int sharedPreviewHpChange)
        { Actors = Array.AsReadOnly(actors.ToArray()); CalculatingPreview = calculatingPreview; SharedPreviewHpChange = sharedPreviewHpChange; }
    }
    public sealed class LifeLinkDamageResult
    {
        public bool Triggered { get; }
        public int Damage { get; }
        public int Prevented { get; }
        public LifeLinkDamageResult(bool triggered, int damage, int prevented) { Triggered = triggered; Damage = damage; Prevented = prevented; }
    }
    // Native life-link protection query and OnPreAttackedLifeLink stage only.
    // This does not run damage callbacks, death, or TFB room combat.
    public static class LifeLinkModel
    {
        public static string? Validate(LifeLinkContext context)
        {
            if (context.Actors.Any(actor => actor.Id <= 0) || context.Actors.Select(actor => actor.Id).Distinct().Count() != context.Actors.Count)
                return "Life-link queries require distinct positive actor identities.";
            foreach (LifeLinkActor actor in context.Actors.Where(actor => actor.LinkedId != 0))
            {
                LifeLinkActor? owner = context.Actors.SingleOrDefault(other => other.Id == actor.LinkedId);
                if (owner == null || owner.Id == actor.Id || owner.LinkedId != 0 || owner.Health != actor.Health)
                    return "Life-link queries require a captured independent owner and matching shared health.";
            }
            return null;
        }
        public static bool Protected(LifeLinkContext context, int targetId, bool forPrediction = false)
        {
            string? invalid = Validate(context);
            if (invalid != null) throw new ArgumentException(invalid, nameof(context));
            LifeLinkActor? target = context.Actors.SingleOrDefault(actor => actor.Id == targetId);
            if (target == null || !target.HasLifeLink) return false;
            int owner = target.LinkedId == 0 ? target.Id : target.LinkedId;
            foreach (LifeLinkActor actor in context.Actors)
            {
                int otherOwner = actor.LinkedId == 0 ? actor.Id : actor.LinkedId;
                if (!actor.Initialized || !actor.HasLifeLink || owner == otherOwner) continue;
                int remaining = actor.TrueFinalBoss && actor.OuterBoss && context.CalculatingPreview
                    ? unchecked(actor.Health - context.SharedPreviewHpChange) : forPrediction ? actor.PreviewHealth : actor.Health;
                if (remaining > 1) return true;
            }
            return false;
        }
        public static LifeLinkDamageResult Test(LifeLinkContext context, int targetId, int damage, int initialDamage = 0, int initialPrevented = 0)
        {
            string? invalid = Validate(context);
            if (invalid != null) throw new ArgumentException(invalid, nameof(context));
            LifeLinkActor? target = context.Actors.SingleOrDefault(actor => actor.Id == targetId);
            if (target == null || !target.Initialized || target.Health <= 0) return new LifeLinkDamageResult(false, initialDamage, initialPrevented);
            int result = damage;
            if (Protected(context, targetId) && unchecked(target.Health - damage) <= 0) result = unchecked(target.Health - 1);
            return new LifeLinkDamageResult(result != damage, result, Math.Max(0, unchecked(damage - result)));
        }
    }
}
