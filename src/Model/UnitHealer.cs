using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class UnitHealerModel
    {
        // Native GetHealerEffect selects only the first heal in PostCombatHealing.
        // Upgrades change its scalar, including on ranged effects; endpoints stay unchanged.
        internal static IReadOnlyList<CombatTrigger> ApplyDamageUpgrade(IReadOnlyList<CombatTrigger> triggers, int delta, bool preview)
        {
            if (preview) return triggers;
            for (int index = 0; index < triggers.Count; index++)
            {
                CombatTrigger trigger = triggers[index]; if (trigger.Kind != "PostCombatHealing") continue;
                for (int effectIndex = 0; effectIndex < trigger.Effects.Count; effectIndex++)
                {
                    CombatEffect effect = trigger.Effects[effectIndex]; if (effect.Type != "CardEffectHeal") continue;
                    var effects = trigger.Effects.ToArray(); effects[effectIndex] = effect.WithActionValue(Math.Max(0, unchecked(effect.Value + delta)));
                    var changed = triggers.ToArray(); changed[index] = new CombatTrigger(trigger.Kind, trigger.Once, trigger.HasTriggered,
                        trigger.IgnoreSilence, trigger.FireCount, effects, trigger.SkipDuringDeployment); return changed;
                }
            }
            return triggers;
        }
    }
}
