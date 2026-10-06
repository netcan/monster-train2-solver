using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StandbyPileProbe
    {
        internal static CardPileState Capture(Dictionary<CardState, RemoveFromStandByCondition> native, UnitPlayModelProbe projection)
        {
            // Read Mono's physical entries and free-list without changing dictionary state.
            Type type = native.GetType();
            int count = (int)AccessTools.Field(type, "_count").GetValue(native);
            var entries = (Array?)AccessTools.Field(type, "_entries").GetValue(native);
            int free = (int)AccessTools.Field(type, "_freeList").GetValue(native);
            int freeCount = (int)AccessTools.Field(type, "_freeCount").GetValue(native);
            if (freeCount == 0) free = -1; // A never-initialized dictionary has a default head of zero.
            var slots = new int[count];
            var holes = new List<int>();
            for (int index = 0; index < count; index++)
            {
                object entry = entries!.GetValue(index)!;
                if ((int)AccessTools.Field(entry.GetType(), "hashCode").GetValue(entry) < 0) continue;
                var card = (CardState)AccessTools.Field(entry.GetType(), "key").GetValue(entry);
                slots[index] = projection.CaptureCards(new List<CardState> { card })[0].InstanceId;
            }
            while (free >= 0)
            {
                if (free >= count || holes.Contains(free)) throw new InvalidOperationException("Invalid native standby free list.");
                holes.Add(free);
                object entry = entries!.GetValue(free)!;
                free = (int)AccessTools.Field(entry.GetType(), "next").GetValue(entry);
            }
            if (holes.Count != freeCount) throw new InvalidOperationException("Native standby free count differs from its chain.");
            var result = new CardPileState("Standby", projection.CaptureCards(native.Keys.ToList()), slots, holes);
            string? error = CardPileModel.Validate(result);
            if (error != null) throw new InvalidOperationException(error);
            return result;
        }
    }
}
