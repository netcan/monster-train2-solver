using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicShuffleUnitsModel
    {
        internal const string OwnerId = "afcb1af2-64f5-4887-9f7b-7e509e326604";
        internal const string OwnerKey = "ShuffleUnits";
        internal const string Effect = "RelicEffectShuffleUnitsEndOfTurn";

        internal static bool IsOriginal(CombatRelicState owner) =>
            owner.DataId == OwnerId && owner.AssetKey == OwnerKey && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == true && owner.EffectTypes.Count == 1 && owner.EffectTypes[0] == Effect;

        internal static bool Active(CombatContext? context) => context?.Relics?.Any(IsOriginal) == true;
    }
}
