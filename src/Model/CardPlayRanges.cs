using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardPlayRangeModel
    {
        public const string Trait = "CardTraitLimitedRange";
        public static bool HasLimitedRange(CardInstanceState card) =>
            card.MaskDescriptor != null && CardExhaustModel.RefreshTraits(card).CombinedTraits!.Any(trait => trait.RuntimeType == Trait);
        public static bool Allows(int selectedRoomIndex, int pyreRoomIndex) =>
            selectedRoomIndex == pyreRoomIndex || selectedRoomIndex == pyreRoomIndex - 1;
    }
}
