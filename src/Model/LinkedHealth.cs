using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Native StateInformation Hp/MaxHp and DeepCopyForPreview only. Damage,
    // healing, death signals and full TFB turn simulation are separate layers.
    public sealed class LinkedHealthActor
    {
        public int Id { get; }
        public int LinkedId { get; }
        public int RawHealth { get; }
        public int RawMaxHealth { get; }
        public bool Dirty { get; }
        public LinkedHealthActor(int id, int linkedId, int rawHealth, int rawMaxHealth, bool dirty)
        { Id = id; LinkedId = linkedId; RawHealth = rawHealth; RawMaxHealth = rawMaxHealth; Dirty = dirty; }
    }
    public sealed class LinkedHealthState
    {
        public IReadOnlyList<LinkedHealthActor> Actors { get; }
        public LinkedHealthState(IReadOnlyList<LinkedHealthActor> actors) { Actors = Array.AsReadOnly(actors.ToArray()); }
    }
    public sealed class LinkedHealthValue
    {
        public int Id { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        public LinkedHealthValue(int id, int health, int maxHealth) { Id = id; Health = health; MaxHealth = maxHealth; }
    }
    public sealed class LinkedHealthSnapshot
    {
        public LinkedHealthState State { get; }
        public IReadOnlyList<LinkedHealthValue> Values { get; }
        public LinkedHealthSnapshot(LinkedHealthState state, IReadOnlyList<LinkedHealthValue> values)
        { State = state; Values = Array.AsReadOnly(values.ToArray()); }
    }
    public sealed class LinkedHealthResult
    {
        public LinkedHealthSnapshot? Snapshot { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Snapshot != null;
        internal LinkedHealthResult(LinkedHealthSnapshot? snapshot, string? reason = null) { Snapshot = snapshot; UnsupportedReason = reason; }
    }
    public static class LinkedHealthModel
    {
        public static string? Validate(LinkedHealthState state)
        {
            if (state.Actors.Any(actor => actor.Id <= 0) || state.Actors.Select(actor => actor.Id).Distinct().Count() != state.Actors.Count)
                return "Linked health requires distinct positive actor identities.";
            foreach (LinkedHealthActor actor in state.Actors.Where(actor => actor.LinkedId != 0))
            {
                LinkedHealthActor? owner = state.Actors.SingleOrDefault(other => other.Id == actor.LinkedId);
                if (owner == null || owner.Id == actor.Id || owner.LinkedId != 0)
                    return "Linked health requires a captured independent owner; chained and cyclic links are not calibrated.";
            }
            return null;
        }
        private static LinkedHealthSnapshot Snapshot(LinkedHealthState state) => new LinkedHealthSnapshot(state,
            state.Actors.Select(actor =>
            {
                LinkedHealthActor owner = actor.LinkedId == 0 ? actor : state.Actors.Single(other => other.Id == actor.LinkedId);
                return new LinkedHealthValue(actor.Id, owner.RawHealth, owner.RawMaxHealth);
            }).ToArray());
        public static LinkedHealthResult Write(LinkedHealthState source, int targetId, string property, int value)
        {
            string? invalid = Validate(source);
            if (invalid != null) return new LinkedHealthResult(null, invalid);
            LinkedHealthActor? target = source.Actors.SingleOrDefault(actor => actor.Id == targetId);
            if (target == null || property is not ("Hp" or "MaxHp"))
                return new LinkedHealthResult(null, "Missing linked-health target or native property.");
            int ownerId = target.LinkedId == 0 ? target.Id : target.LinkedId;
            int stored = Math.Min(value, 99999);
            var next = new LinkedHealthState(source.Actors.Select(actor => new LinkedHealthActor(actor.Id, actor.LinkedId,
                actor.Id == ownerId && property == "Hp" ? stored : actor.RawHealth,
                actor.Id == ownerId && property == "MaxHp" ? stored : actor.RawMaxHealth,
                actor.Dirty || actor.Id == ownerId || actor.Id == targetId)).ToArray());
            return new LinkedHealthResult(Snapshot(next));
        }
        public static LinkedHealthResult CopyForPreview(LinkedHealthState source)
        {
            string? invalid = Validate(source);
            if (invalid != null) return new LinkedHealthResult(null, invalid);
            LinkedHealthSnapshot read = Snapshot(source);
            var next = new LinkedHealthState(source.Actors.Zip(read.Values, (actor, value) =>
                new LinkedHealthActor(actor.Id, 0, value.Health, value.MaxHealth, actor.Dirty)).ToArray());
            return new LinkedHealthResult(Snapshot(next));
        }
    }
}
