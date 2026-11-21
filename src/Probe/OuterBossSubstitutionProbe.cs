using System;
using System.Collections.Generic;

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
                bool fleshweaverTrial = effects.Count == 1 &&
                    typeof(CardState).Module.ModuleVersionId == new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") &&
                    managers.GetSaveManager().IsTitanTrialEnabled() && MatchesFleshweaverTrial(data, replacement,
                        effect.SourceRelicData, managers.GetSaveManager().GetCurrentScenarioData()?.name);
                supported &= seraphTrial || fleshweaverTrial;
                data = replacement;
            }
            return data;
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
