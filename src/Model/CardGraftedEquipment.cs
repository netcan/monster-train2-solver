using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardGraftedEquipmentModel
    {
        public const string Trait = "CardTraitGraftedEquipment";
        public const string EventHostId = "3d20e7fd-0c1f-478e-8845-4a11e27baca5";
        public const string EventEquipmentId = "7369cb8e-f8cf-44be-8667-c699c630c063";
        public static string? Validate(CardInstanceState card)
        {
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (descriptor == null) return null;
            if (descriptor.GraftedEquipment != null && (card.DataId != EventHostId ||
                descriptor.GraftedEquipment.Card.DataId != EventEquipmentId || !descriptor.PermanentGraft ||
                card.GraftedEquipmentCardId.GetValueOrDefault() <= 0))
                return "Event graft requires the calibrated Firebrand/MeatShield instance definition and equipment reference.";
            bool added = descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true);
            if (!added && !descriptor.BaseTraits.Any(trait => trait.RuntimeType == Trait)) return null;
            if (card.DataId == EventEquipmentId && descriptor.BaseTraits.Count == 0 && descriptor.TemporaryTraits.Count == 1 &&
                descriptor.TemporaryTraits[0].RuntimeType == Trait && descriptor.PermanentReplacements.Count == 0 &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
            {
                CardTraitValue temporary = descriptor.TemporaryTraits[0];
                return descriptor.Definition.CardType == "Equipment" && descriptor.SourceTraits?.Count == 0 &&
                    temporary.DeclaredName == Trait && temporary.ParamInt == 0 && temporary.DefinitionParamInt == 0 &&
                    temporary.GraftSevered.HasValue && temporary.Removable && temporary.StackMode == 0
                    ? null : "Uncalibrated event-grafted equipment trait.";
            }
            if (added || descriptor.BaseTraits.Count != 1 || descriptor.BaseTraits[0].RuntimeType != Trait ||
                descriptor.BaseTraits[0].DeclaredName != Trait || !descriptor.BaseTraits[0].GraftSevered.HasValue ||
                descriptor.BaseTraits[0].ParamInt != 0 || descriptor.BaseTraits[0].DefinitionParamInt != 0 || descriptor.SourceTraits?.Count != 1 ||
                descriptor.Definition.CardType != "Equipment" || !new[] { "c2689249-1886-423d-98d0-cd3857289b3e", "dc7ff295-83fd-47e4-93c7-a133e7058ac8",
                    "42d238d8-7180-48b0-b0e8-51f2bbb7ffb4", "0a928329-666c-4ce6-a1c3-413977e37e46", "96f46995-23fe-4c0b-8325-54826e95c333" }.Contains(card.DataId))
                return "Grafted equipment requires a captured unchanged original equipment trait and Severed state.";
            CardSourceTrait source = descriptor.SourceTraits[0];
            return source.Type == Trait && source.Int == 0 && source.Int2 == 0 && source.Int3 == 0 && !source.Bool && source.Str == "" && source.Float == 0 &&
                !source.Scaling && source.TrackedValue == "SubtypeInDeck" && source.EntryDuration == "ThisTurn" ? null : "Uncalibrated grafted equipment source parameters.";
        }
        internal static bool Present(CardInstanceState card) => card.MaskDescriptor?.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits)
            .Any(trait => trait.RuntimeType == Trait) == true;
        internal static CardInstanceState AttachEventTrait(CardInstanceState card)
        {
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (card.DataId != EventEquipmentId || descriptor == null || descriptor.BaseTraits.Count != 0 ||
                descriptor.TemporaryTraits.Count != 0 || descriptor.SourceTraits?.Count != 0)
                throw new System.InvalidOperationException("Event graft requires the original trait-free MeatShield equipment.");
            return card.WithMaskDescriptor(descriptor.WithHoldover(descriptor.BaseTraits,
                new[] { new CardTraitValue(Trait, Trait, 0, 0, true, 0, graftSevered: false) }, descriptor.HoldoverUpgradePending));
        }
        public static CardInstanceState SetSevered(CardInstanceState card, bool severed)
        {
            if (!Present(card)) return card;
            string? error = Validate(card);
            if (error != null) throw new System.InvalidOperationException(error);
            CardMaskDescriptor descriptor = card.MaskDescriptor!;
            return card.WithMaskDescriptor(descriptor.WithHoldover(descriptor.BaseTraits.Select(trait =>
                trait.RuntimeType == Trait ? trait.WithGraftSevered(severed) : trait).ToArray(), descriptor.TemporaryTraits.Select(trait =>
                trait.RuntimeType == Trait ? trait.WithGraftSevered(severed) : trait).ToArray(), descriptor.HoldoverUpgradePending));
        }
    }
}
