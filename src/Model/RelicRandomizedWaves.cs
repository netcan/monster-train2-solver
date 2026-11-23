namespace MonsterTrain2Poju.Model
{
    internal static class RelicRandomizedWavesModel
    {
        internal const string OwnerId = "f6ab3e2c-2e61-4a53-ac4a-501ecda375ac";
        internal const string OwnerKey = "RandomizeWaveOrder";
        internal const string BonusCovenantId = "92c18b99-e4c0-4953-ba2c-4815f570a66e";
        internal const string Shuffle = "RelicEffectRandomizeWaveOrder";
        internal const string ForceBonusEnemy = "RelicEffectForceBonusEnemyCovenant";

        internal static bool IsOriginal(CombatRelicState relic) => relic.DataId == OwnerId && relic.AssetKey == OwnerKey &&
            relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
            relic.EffectTypes is [Shuffle, ForceBonusEnemy];
    }
}
