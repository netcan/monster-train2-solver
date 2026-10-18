using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardStatusDamageModel
    {
        public const string Trait = "CardTraitAddDamagePerStatusEffect";

        public static string? Apply(CardInstanceState card, int damage, IEnumerable<CombatStatus>? targetStatuses, out int result)
        {
            result = damage;
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true)) return null;
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>())
                .Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return null;
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return "Status-stack damage requires one unchanged base trait and captured source parameters.";
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 3 || active[0].DefinitionParamInt != 3 ||
                source.Type != Trait || source.Int != 3 || source.Int2 != 2 || source.Int3 != 0 || source.Bool || source.Str != "" ||
                source.Float != 2 || source.Scaling || source.TrackedValue != "MagicPowerInTargetRoom" || source.EntryDuration != "ThisTurn")
                return "Only captured Wracking Pain target-debuff damage parameters are calibrated.";
            if (card.DamageScalingTraits?.Count > 0)
                return "Mixed status-stack and statistic damage trait ordering requires native calibration.";
            if (targetStatuses == null) return "Status-stack damage requires live target status metadata.";
            int stacks = 0;
            foreach (CombatStatus status in targetStatuses)
            {
                if (status.Id == "horde" || status.Stacks == 0) continue;
                if (!status.Hidden.HasValue || status.DisplayCategory == null)
                    return "Status-stack damage requires captured visibility and display category.";
                if (status.Hidden == false && status.DisplayCategory == "Negative") stacks = unchecked(stacks + status.Stacks);
            }
            result = unchecked(damage + stacks * active[0].ParamInt);
            return null;
        }
    }
}
