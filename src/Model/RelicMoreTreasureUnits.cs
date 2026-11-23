namespace MonsterTrain2Poju.Model
{
    internal static class RelicMoreTreasureUnitsModel
    {
        internal const string OwnerId = "cbacfccc-fc3b-43b9-b1d2-27a4c30ed628";
        internal const string OwnerKey = "MoreTreasureUnits";
        internal const string Effect = "RelicEffectChangeTreasureUnitCount";

        // Scenario initialization has already applied the original balance rule
        // to the captured remaining treasure count.
        internal static bool IsOriginal(CombatRelicState owner) => owner is
        {
            DataId: OwnerId, AssetKey: OwnerKey, IsCovenant: false, DisallowedInPlacementPhase: false,
            EffectTypes: [Effect]
        };
    }
}
