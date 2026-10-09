using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class PreviewEffectsModel
    {
        // Trigger/effect objects are shared between the native primary and preview
        // state information. Numeric unit state and primary once flags are restored.
        public static CombatUnit Restore(CombatUnit primary, CombatUnit preview,
            IReadOnlyCollection<int> removedPreviewUnitIds, bool prepareEnchantments = false)
        {
            if (primary.Id != preview.Id) throw new ArgumentException("Preview restoration requires the same actor identity.");
            return primary.WithTriggers(primary.Triggers.Select((trigger, index) =>
            {
                CombatTrigger? tested = trigger.StateId.HasValue ? preview.Triggers.FirstOrDefault(item => item.StateId == trigger.StateId) :
                    index < preview.Triggers.Count ? preview.Triggers[index] : null;
                if (tested == null) return trigger;
                return trigger.WithEffects(trigger.Effects.Select((effect, effectIndex) =>
                {
                    CombatEffect? observed = effectIndex < tested.Effects.Count ? tested.Effects[effectIndex] : null;
                    if (observed == null || effect.Type != observed.Type) return effect;
                    if (effect.Enchantment != null && observed.Enchantment != null)
                        effect = effect.WithEnchantment(effect.Enchantment.WithState(prepareEnchantments
                            ? EnchantmentLifecycleModel.PrepareForPreview(observed.Enchantment.State) : observed.Enchantment.State));
                    if (effect.Summon != null && observed.Summon != null)
                        effect = effect.WithSummon(effect.Summon.WithFirstSpawned(
                            removedPreviewUnitIds.Contains(observed.Summon.FirstSpawnedUnitId) ? 0 : observed.Summon.FirstSpawnedUnitId));
                    return effect;
                }).ToArray());
            }).ToArray());
        }
    }
}
