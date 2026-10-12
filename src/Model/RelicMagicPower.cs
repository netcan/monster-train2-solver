using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicMagicPowerModifier
    {
        public int EffectIndex { get; }
        public int PowerModifier { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public RelicMagicPowerModifier(int effectIndex, int powerModifier,
            IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0)
        {
            EffectIndex = effectIndex;
            PowerModifier = powerModifier;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount;
        }
    }

    internal static class RelicMagicPowerModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.ModifyCardMagicalPower)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.MagicPowerModifiers?.Count > 0 ? "Unexpected relic magic-power definitions." : null;
            if (relic.MagicPowerModifiers == null || !indices.SequenceEqual(relic.MagicPowerModifiers.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic magic-power definitions.";
            foreach (RelicMagicPowerModifier effect in relic.MagicPowerModifiers)
            {
                if (effect.ConditionCount != effect.Conditions.Count)
                    return "Relic magic-power condition definitions are incomplete.";
                if (effect.Conditions.Count != 0)
                    return "Conditional relic magic power requires branch-aware room-power refresh.";
            }
            return null;
        }

        internal static CardInstanceState? ApplyToCard(CardInstanceState source, RelicMagicPowerModifier effect,
            out string? unsupportedReason)
        {
            unsupportedReason = null;
            CardMaskDescriptor? descriptor = source.MaskDescriptor;
            if (descriptor == null || descriptor.Definition.DataId != source.DataId)
            {
                unsupportedReason = "Relic magic-power card modifiers require matching card mask metadata.";
                return null;
            }
            if (!descriptor.Definition.CanScaleWithMagicPower.HasValue)
            {
                unsupportedReason = "Relic magic-power card eligibility was not captured from the native card.";
                return null;
            }
            if (!descriptor.Definition.CanScaleWithMagicPower.Value) return source;

            // No IMultiplyMagicPowerRelicEffect is modeled; such relic types are
            // rejected by RelicModel.Validate, so the native global multiplier is 1.
            int amount = unchecked(effect.PowerModifier * CardMagicPowerModel.Multiplier(source));
            CardStatModifier old = source.Temporary.Offsets;
            var offsets = new CardStatModifier(unchecked(old.Damage + amount), old.Health, old.Cost,
                unchecked(old.Heal + amount), old.Size, old.XCost, old.EquipmentLimit, old.UpgradeSlotCount);
            var temporary = new CardModifiers(offsets, source.Temporary.Upgrades, source.Temporary.PersistentHealth,
                source.Temporary.ExternalInteractions);
            return source.WithModifiers(source.Permanent, temporary, refreshTraits: false);
        }
    }
}
