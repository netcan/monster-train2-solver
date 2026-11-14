using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardTerminalPurgeModel
    {
        internal static bool Supports(CardInstanceState card)
        {
            if (CardCopyOnPlayModel.SupportsTerminalPurge(card)) return true;
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            // Original TFB Pyre commands have both traits in the base definition.
            // Ephemeral purges on played/discarded, then SelfPurge purges once:
            // the settled terminal pile retains three references to the card.
            return descriptor != null && descriptor.Definition.CardType == "Spell" &&
                descriptor.BaseTraits.Select(trait => trait.RuntimeType).SequenceEqual(new[] { "CardTraitSelfPurge", "CardTraitEphemeral" }) &&
                descriptor.BaseTraits.All(trait => trait.DeclaredName == trait.RuntimeType && trait.ParamInt == 0 && trait.StackMode == 0) &&
                descriptor.TemporaryTraits.Count == 0 && descriptor.PermanentReplacements.Count == 0 &&
                card.Permanent.Upgrades.Count == 0 && card.Temporary.Upgrades.Count == 0 && !card.GraftedEquipmentCardId.HasValue;
        }
    }
}
