using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardGraftedEquipmentModel
    {
        public const string Trait = "CardTraitGraftedEquipment";
        public static string? Validate(CardInstanceState card)
        {
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (descriptor == null) return null;
            bool added = descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true);
            if (!added && !descriptor.BaseTraits.Any(trait => trait.RuntimeType == Trait)) return null;
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
        internal static bool Present(CardInstanceState card) => card.MaskDescriptor?.BaseTraits.Any(trait => trait.RuntimeType == Trait) == true;
        public static CardInstanceState SetSevered(CardInstanceState card, bool severed)
        {
            if (!Present(card)) return card;
            string? error = Validate(card);
            if (error != null) throw new System.InvalidOperationException(error);
            return card.WithMaskDescriptor(card.MaskDescriptor!.WithBaseTraits(card.MaskDescriptor.BaseTraits.Select(trait =>
                trait.RuntimeType == Trait ? trait.WithGraftSevered(severed) : trait).ToArray()));
        }
    }
}
