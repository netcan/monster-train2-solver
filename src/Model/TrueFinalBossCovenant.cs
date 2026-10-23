using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class TrueFinalBossCovenantModel
    {
        internal static bool KnownOwner(CombatRelicState owner) =>
            owner.IsCovenant == true && (owner.AssetKey == "Covenant00_FTUE" && owner.DataId == "3cc0272d-c0d7-46dc-9c7a-be9633572f70" ||
                owner.AssetKey == "Covenant01_StrongerEnemiesMoreCards" && owner.DataId == "d3b53481-f26b-4344-ae8e-ec80b28d6004" ||
                owner.AssetKey == "Covenant04_StrongerBosses" && owner.DataId == "80cfbaca-da9a-4aa8-ac0d-a6563f26fcb8" ||
                owner.AssetKey == "Covenant08_StrongerEnemies" && owner.DataId == "ad5c826a-e716-43cc-9c13-df0dd138de71");

        internal static bool MatchesOrigins(CombatContext? context, EnemyBirthStatAdjustment stats)
        {
            CombatRelicState[] owners = context?.Relics?.Where(relic => relic.IsCovenant == true).ToArray()
                ?? System.Array.Empty<CombatRelicState>();
            return (CalibratedOwners(owners) || CalibratedOrdinaryWaveOwners(owners)) &&
                stats.CovenantIds.SequenceEqual(owners.Select(owner => owner.DataId));
        }

        internal static bool CalibratedOrdinaryWaveOwners(CombatRelicState[] owners) => owners.Length > 0 &&
            owners.Select(owner => owner.DataId).Distinct().Count() == owners.Length &&
            owners.Any(owner => owner.AssetKey == "Covenant01_StrongerEnemiesMoreCards" && KnownOwner(owner)) &&
            owners.All(owner => KnownOwner(owner) && owner.AssetKey is "Covenant00_FTUE" or "Covenant01_StrongerEnemiesMoreCards" ||
                owner.IsCovenant == true && owner.AssetKey == "Covenant02_BattlesNewEnemies" &&
                owner.DataId == "92c18b99-e4c0-4953-ba2c-4815f570a66e" && owner.EffectTypes.Count == 0 ||
                OrdinaryWaveExtensionOwner(owner));

        internal static bool OrdinaryWaveExtensionOwner(CombatRelicState owner) => MerchantOnlyOwner(owner) ||
            StrongerBossOwner(owner) || StartupCardsOwner(owner) || DraftOnlyOwner(owner) || WaveMessageOwner(owner) ||
            StrongerEnemyOwner(owner) || StartingPyreOwner(owner) || FlyingBossSelectionOwner(owner);

        internal static bool DraftOnlyOwner(CombatRelicState owner) => owner.IsCovenant == true &&
            owner.AssetKey == "Covenant06_RandomDudUpgradeOnDraft" && owner.DataId == "2cd816ea-fc0d-4129-ae64-24aa99ef0594" &&
            owner.EffectTypes.SequenceEqual(new[] { "RelicEffectAddBonusCardToCardDrafts" });

        internal static bool WaveMessageOwner(CombatRelicState owner) => owner.IsCovenant == true &&
            owner.AssetKey == "Covenant07_NoRespite" && owner.DataId == "06101ac8-9afb-401c-aaa7-7ef2745ce6a9" &&
            owner.EffectTypes.SequenceEqual(new[] { "RelicEffectNoRespiteWaves" });

        internal static bool StartingPyreOwner(CombatRelicState owner) => owner.IsCovenant == true &&
            owner.AssetKey == "Covenant09_PyreDamage" && owner.DataId == "222e1727-2480-475c-85e8-d833a1db6e2d" &&
            owner.EffectTypes.SequenceEqual(new[] { "RelicEffectDamagePyreStartOfRun" });

        internal static bool FlyingBossSelectionOwner(CombatRelicState owner) => owner.IsCovenant == true &&
            owner.AssetKey == "Covenant10_ChosenEnemiesDuringFlyingBosses" && owner.DataId == "871bec39-7863-4fdd-9919-929d05c92697" &&
            owner.EffectTypes.Count == 0;

        internal static bool StrongerEnemyOwner(CombatRelicState owner) => KnownOwner(owner) &&
            owner.AssetKey == "Covenant08_StrongerEnemies" && owner.EffectTypes.SequenceEqual(
                Enumerable.Repeat("RelicEffectModifyCharacterMaxHealth", 6)
                    .Concat(Enumerable.Repeat("RelicEffectModifyCharacterAttackDamage", 3))
                    .Concat(Enumerable.Repeat("RelicEffectModifyCharacterMaxHealth", 2))
                    .Concat(new[] { "RelicEffectModifyCharacterAttackDamage" })
                    .Concat(Enumerable.Repeat("RelicEffectModifyCharacterMaxHealth", 2)));

        internal static bool StartupCardsOwner(CombatRelicState owner) => owner.IsCovenant == true &&
            owner.AssetKey == "Covenant05_VengefulShards" && owner.DataId == "0310c0f2-f970-4656-9cc6-6d3c457303dd" &&
            owner.EffectTypes.SequenceEqual(Enumerable.Repeat("RelicEffectAddCardsStartOfRun", 2));

        internal static bool StrongerBossOwner(CombatRelicState owner) => KnownOwner(owner) &&
            owner.AssetKey == "Covenant04_StrongerBosses" && owner.EffectTypes.SequenceEqual(
                new[] { "RelicEffectAddTrigger" }
                    .Concat(Enumerable.Repeat("RelicEffectModifyCharacterMaxHealth", 9))
                    .Concat(Enumerable.Repeat("RelicEffectModifyCharacterAttackDamage", 2))
                    .Concat(new[] { "RelicEffectAddStatusEffectOnSpawn" })
                    .Concat(Enumerable.Repeat("RelicEffectAddTrigger", 4))
                    .Concat(new[] { "RelicEffectAddStatusEffectOnSpawn" })
                    .Concat(Enumerable.Repeat("RelicEffectAddTrigger", 6))
                    .Concat(new[] { "RelicEffectModifyCharacterMaxHealth" }));

        internal static bool MerchantOnlyOwner(CombatRelicState owner) => owner.IsCovenant == true &&
            owner.AssetKey == "Covenant03_MerchantCostsIncrease" && owner.DataId == "dcb31cd7-4293-4b07-b396-5456a0ce0b6e" &&
            owner.EffectTypes.SequenceEqual(new[] { "RelicEffectModifyMerchantRerollCosts", "RelicEffectModifyMerchantPurgeCosts" });

        internal static bool CalibratedOwners(CombatRelicState[] owners) => owners.Length > 0 &&
            owners.All(KnownOwner) && owners.Select(owner => owner.DataId).Distinct().Count() == owners.Length;

        internal static bool CalibratedOuterBossOwners(CombatContext? context, string asset)
        {
            CombatRelicState[] owners = context?.Relics?.Where(owner => owner.IsCovenant == true).ToArray()
                ?? System.Array.Empty<CombatRelicState>();
            return owners.Length == 0 || asset is "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" &&
                CalibratedOrdinaryWaveOwners(owners);
        }

        // Original additive Covenant controls. Authored stats remain separate
        // from the native birth adjustments; other definitions stay guarded.
        internal static bool Adjustment(CombatContext? context, string asset, out int attack, out int health)
        {
            attack = 0; health = 0;
            CombatRelicState[] owners = context?.Relics?.Where(relic => relic.IsCovenant == true).ToArray()
                ?? System.Array.Empty<CombatRelicState>();
            if (owners.Length == 0) return true;
            if (!CalibratedOwners(owners) && !CalibratedOrdinaryWaveOwners(owners)) return false;
            if (!owners.Any(owner => owner.AssetKey == "Covenant04_StrongerBosses")) return true;
            if (asset == "Boss_Level9_Dominion_V2") { health = 500; return true; }
            if (asset == "Boss_Level9_Savagery_V2") { attack = 5; health = 500; return true; }
            if (asset is "Boss_Level9_Entropy_Bot_V2" or "Boss_Level9_Entropy_Mid_V2" or "Boss_Level9_Entropy_Top_V2")
            { attack = 5; health = 1000; return true; }
            return false;
        }
    }
}
