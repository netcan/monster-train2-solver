using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class StatusTriggerOrderModel
    {
        public static int TriggerOrder(string id) => id switch
        {
            "regen" => 1, "corruption" => 2, "trample" => 3, "unstable" => 4, _ => 0
        };

        // Native sorts all active dictionary entries before filtering the stage.
        // Equal keys lose insertion order above the runtime's small-list threshold.
        public static IReadOnlyList<CombatStatus> Order(IReadOnlyList<CombatStatus> dictionary)
        {
            var active = dictionary.Where(status => status.Stacks > 0).ToList();
            active.Sort((left, right) => TriggerOrder(left.Id).CompareTo(TriggerOrder(right.Id)));
            return Array.AsReadOnly(active.ToArray());
        }
    }
}
