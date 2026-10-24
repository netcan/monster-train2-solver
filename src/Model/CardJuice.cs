using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardJuiceModel
    {
        internal const string Trait = "CardTraitJuice";
        internal const string Upgrade = "0fc9f6e7-df71-4045-9867-67453769adf8";
        internal static bool Present(CardInstanceState card) => card.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade =>
                    upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true));
        internal static string? Validate(CardInstanceState card)
        {
            if (!Present(card)) return null;
            // This native merchant signature is calibrated on the original
            // Lifesteal/Scorch spell. Other callback orders remain explicit gaps.
            if (card.DataId != "3020f56f-71a5-4155-8bed-48ada7e7e0de" ||
                card.StatusScalingTraits?.Count > 0 ||
                CardExhaustModel.RefreshTraits(card).CombinedTraits!.Count(trait => trait.RuntimeType == Trait) != 1)
                return "Juice with this card or ordered scaling traits requires native calibration.";
            return null;
        }
        internal static int Apply(int stacks) => unchecked(stacks + Math.Max(stacks, 0));
    }
}
