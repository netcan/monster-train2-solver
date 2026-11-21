using System;
using System.Collections.Generic;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OuterBossSubstitutionProbe
    {
        internal const string FleshweaverSpikesTrialOwnerId = "c4abfdc2-a89c-43cf-9e35-59f6ea3b39f7";
        internal const string FleshweaverSpikesTrialOwner = "Enemy_Boss_Level3_ImmortalMass_Spike_TitanTrial";
        internal const string FleshweaverSpikesTrialScenario = "Level3ImmortalMass_Spikes";
        internal const string FleshweaverSpikesMini = "Boss_Level3_MiniFleshweaver_Spike";
        internal const string FleshweaverSpikesMiniId = "eb00da55-1597-4768-85bc-2fa241af11cb";
        internal const string FleshweaverSpikesTrialMini = "Boss_Level3_MiniFleshweaver_Spike_TitanTrial";

        // Mirror the native ordered CreateHeroState lookup without creating a
        // unit, changing assets or RNG. Only observed mappings are calibrated.
        internal static CharacterData Resolve(CharacterData data, out bool supported)
        {
            supported = true;
            AllGameManagers managers = AllGameManagers.Instance;
            var effects = managers.GetRelicManager().GetRelicEffects(new List<RelicEffectOnSpawnHeroSubstitution>());
            foreach (RelicEffectOnSpawnHeroSubstitution effect in effects)
            {
                if (!effect.TryGetSubstitution(data, out CharacterData? replacement)) continue;
                string before = data.GetAssetKey();
                if (replacement == null)
                {
                    supported = false;
                    continue;
                }
                string after = replacement.GetAssetKey();
                bool seraphTrial = before is "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" &&
                    after == before + "_TitanTrial";
                SaveManager save = managers.GetSaveManager();
                string? scenario = save.GetCurrentScenarioData()?.name;
                bool calibratedTrial = effects.Count == 1 && save.IsTitanTrialEnabled() &&
                    typeof(CardState).Module.ModuleVersionId == new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") &&
                    (MatchesFleshweaverTrial(data, replacement, effect.SourceRelicData, scenario) ||
                     save.GetAscensionLevel() == 0 && MatchesCaelTrial(data, replacement, effect.SourceRelicData, scenario));
                supported &= seraphTrial || calibratedTrial;
                data = replacement;
            }
            return data;
        }

        private static bool MatchesCaelTrial(CharacterData source, CharacterData replacement, RelicData owner, string? scenario)
        {
            (string? Variant, string OwnerId, string OuterSource, string OuterReplacement, string FeedSource, string FeedReplacement) trial = scenario switch
            {
                "Level6BattleEaterDamageShield" => ("DamageShield", OuterBossDefinitions.CaelShieldTrialOwnerId,
                    "168b33b6-361b-4691-bc64-aa58c66eb5db", "3794c878-797d-4e81-8f0b-644869661970",
                    "43bbea4c-c1e8-4744-a4b1-e5b5f135c059", "b29a1359-c39b-48d6-9649-1740667ed97e"),
                "Level6BattleEaterAtkHP" => ("AtkHP", OuterBossDefinitions.CaelAtkHpTrialOwnerId,
                    "e88fbe2c-a90d-457d-9d87-3246547a72ac", "7545c2d9-53ba-49d7-b937-a5018ef32e27",
                    "104e4d49-1c74-4fc1-92ec-73fd69842a28", "85380a63-dbe3-4db4-98ec-a266a68a60e5"),
                "Level6BattleEaterLifesteal" => ("Lifesteal", OuterBossDefinitions.CaelLifestealTrialOwnerId,
                    "dee70287-3dd8-4b9b-8d4d-0a20344b92ea", "a7f04f71-f121-4471-85d7-2c7860e1ca50",
                    "e795417c-eb95-470d-b8ff-46ff8bcb166a", "0a0b9e16-8f55-4f11-898b-9dbeb26b6fe3"),
                _ => default
            };
            if (trial.Variant == null) return false;
            var ownerEffects = owner.GetEffects();
            if (owner.GetID() != trial.OwnerId || owner.name != "Enemy_SpawnHeroEaterFeed" + trial.Variant + "_TitanTrial" ||
                ownerEffects.Count != 2 || ownerEffects[0].GetEffectClassName() != "RelicEffectOnSpawnHeroSubstitution" ||
                ownerEffects[1].GetEffectClassName() != "RelicEffectNull") return false;

            string before = source.GetAssetKey();
            if (replacement.GetAssetKey() != before + "_TitanTrial") return false;
            return before == "Boss_Level6_Cael_" + trial.Variant && source.GetID() == trial.OuterSource &&
                replacement.GetID() == trial.OuterReplacement ||
                before == "Boss_Level6_Feed" + trial.Variant && source.GetID() == trial.FeedSource &&
                replacement.GetID() == trial.FeedReplacement;
        }

        private static bool MatchesFleshweaverTrial(CharacterData source, CharacterData replacement, RelicData owner, string? scenario)
        {
            string replacementKey = replacement.GetAssetKey();
            return source.GetAssetKey() switch
            {
                FleshweaverSpikesMini => source.GetID() == FleshweaverSpikesMiniId && replacementKey == FleshweaverSpikesTrialMini &&
                    owner.GetID() == FleshweaverSpikesTrialOwnerId && owner.name == FleshweaverSpikesTrialOwner &&
                    scenario == FleshweaverSpikesTrialScenario,
                "Boss_Level3_MiniFleshweaver_Corruption" => source.GetID() == "ebe69a47-be18-4515-b19c-8cf4da8624ed" &&
                    replacementKey == "Boss_Level3_MiniFleshweaver_Corruption_TitanTrial" && replacement.GetID() == "c05429e3-c6de-49fc-870c-6124fcc87076" &&
                    owner.GetID() == "52dc060c-bd51-4b7b-865a-6d70f9806e53" && owner.name == "Enemy_Boss_Level3_ImmortalMass_Corruption_TitanTrial" &&
                    scenario == "Level3ImmortalMass_Corruption",
                "Boss_Level3_MiniFleshweaver_MoreMass" => source.GetID() == "c32f2387-4812-46c3-9749-14205bb22025" &&
                    replacementKey == "Boss_Level3_MiniFleshweaver_MoreMass_TitanTrial" && replacement.GetID() == "4159e8ac-faec-4dae-a55f-dde1320b7c5b" &&
                    owner.GetID() == "28c0e907-d882-4052-93f4-0a07d88496af" && owner.name == "Enemy_Boss_Level3_ImmortalMass_MoreMass_TitanTrial" &&
                    scenario == "Level3ImmortalMass_MoreMass",
                _ => false
            };
        }
    }
}
