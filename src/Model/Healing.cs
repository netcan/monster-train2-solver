using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class HealingModel
    {
        // The native multiplier uses the status parameter, independently of its stack count.
        public static int ModifiedAmount(int amount, IReadOnlyList<CombatStatus> statuses, bool fromMaxHealthChange = false)
        {
            CombatStatus? multiplier = statuses.FirstOrDefault(status => status.Id == "heal multiplier" && status.Stacks > 0);
            if (multiplier != null) amount = checked(amount * multiplier.ParamInt);
            if (!fromMaxHealthChange && statuses.Any(status => status.Id == "heal immunity" && status.Stacks > 0)) return 0;
            return amount;
        }
        public static int HealedHealth(int health, int maxHealth, int amount, bool canBeHealed,
            IReadOnlyList<CombatStatus> statuses, bool fromMaxHealthChange = false)
        {
            amount = ModifiedAmount(amount, statuses, fromMaxHealthChange);
            return health <= 0 || !canBeHealed || amount < 0 ? health : health + System.Math.Min(amount, maxHealth - health);
        }
    }
}
