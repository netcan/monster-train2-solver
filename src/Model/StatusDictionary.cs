using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Unity's Mono dictionary enumerates allocated slots and reuses removed slots in LIFO order.
    public sealed class StatusDictionaryState
    {
        public IReadOnlyList<string?> Slots { get; }
        public IReadOnlyList<int> FreeSlots { get; }
        public StatusDictionaryState(IReadOnlyList<string?> slots, IReadOnlyList<int> freeSlots)
        { Slots = Array.AsReadOnly(slots.ToArray()); FreeSlots = Array.AsReadOnly(freeSlots.ToArray()); }
        internal StatusDictionaryState Sync(IReadOnlyList<CombatStatus> registry)
        {
            var slots = Slots.ToList(); var free = FreeSlots.ToList();
            foreach (CombatStatus status in registry)
            {
                if (slots.Contains(status.Id)) continue;
                if (free.Count == 0) slots.Add(status.Id);
                else { int index = free[0]; free.RemoveAt(0); slots[index] = status.Id; }
            }
            return new StatusDictionaryState(slots, free);
        }
        internal StatusDictionaryState Remove(string id)
        {
            int index = Slots.ToList().IndexOf(id);
            if (index < 0) return this;
            var slots = Slots.ToArray(); slots[index] = null;
            return new StatusDictionaryState(slots, new[] { index }.Concat(FreeSlots).ToArray());
        }
        internal CombatStatus[] Order(IReadOnlyList<CombatStatus> registry) => Slots.Where(id => id != null)
            .Select(id => registry.Single(status => status.Id == id)).ToArray();
        internal string? Validate(IReadOnlyList<CombatStatus>? registry) => registry == null ||
            Slots.Where(id => id != null).Distinct().Count() != Slots.Count(id => id != null) ||
            !Slots.Where(id => id != null).OrderBy(id => id).SequenceEqual(registry.Select(status => status.Id).OrderBy(id => id)) ||
            FreeSlots.Distinct().Count() != FreeSlots.Count || FreeSlots.Any(index => index < 0 || index >= Slots.Count || Slots[index] != null) ||
            FreeSlots.Count != Slots.Count(id => id == null) ? "Invalid captured status dictionary slots." : null;
    }
}
