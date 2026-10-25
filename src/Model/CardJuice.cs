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
            // Original Lifesteal/Scorch,Ritual of Battle,and Corrupt/Juice Fracture merchant owners.
            // Ordered scaling callbacks and other owners remain explicit gaps.
            var traits = CardExhaustModel.RefreshTraits(card).CombinedTraits!;
            bool fracture = card.DataId == "b197fdec-a77d-49f5-bc05-6233b7ce01c7" && traits.Count == 2 &&
                traits[0].RuntimeType == "CardTraitCorruptState" && Ordinary(traits[0], 0) &&
                traits[1].RuntimeType == Trait && Ordinary(traits[1], 2) &&
                card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == "83a6c9ec-0e40-48e6-8a4c-695a68b7e990") &&
                card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == Upgrade);
            if ((!fracture && card.DataId is not ("3020f56f-71a5-4155-8bed-48ada7e7e0de" or "f9f20326-dccb-44af-894b-467534afab4a")) ||
                card.StatusScalingTraits?.Count > 0 ||
                traits.Count(trait => trait.RuntimeType == Trait) != 1)
                return "Juice with this card or ordered scaling traits requires native calibration.";
            return null;
        }
        private static bool Ordinary(CardTraitValue trait, int parameter) =>
            trait.DefinitionParamInt == parameter && trait.ParamInt == parameter && trait.Removable &&
            trait.DefinitionRemovable == true && trait.StackMode == 0 && !trait.TemporaryReplacement;
        internal static int Apply(int stacks) => unchecked(stacks + Math.Max(stacks, 0));
    }
}
