using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class UnitAttackOverrideModel
    {
        // Native attack getters return here before any ordinary buff, room or relic modifier.
        internal static int? Resolve(IEnumerable<CombatStatus> statuses, int maxHealth)
        {
            CombatStatus? fixedAttack = statuses.FirstOrDefault(status => status.Id == "fixedattack" && status.Stacks > 0);
            if (fixedAttack != null) return fixedAttack.Stacks;
            if (statuses.Any(status => status.Id == "armorattack" && status.Stacks > 0))
                return statuses.FirstOrDefault(status => status.Id == "armor" && status.Stacks > 0)?.Stacks ?? 0;
            return statuses.Any(status => status.Id == "equalizer" && status.Stacks > 0) ? maxHealth : (int?)null;
        }
    }
}
