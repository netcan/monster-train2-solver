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
                bool fleshweaverTrial = before == FleshweaverSpikesMini && data.GetID() == FleshweaverSpikesMiniId &&
                    after == FleshweaverSpikesTrialMini && effects.Count == 1 &&
                    typeof(CardState).Module.ModuleVersionId == new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") &&
                    effect.SourceRelicData.GetID() == FleshweaverSpikesTrialOwnerId && effect.SourceRelicData.name == FleshweaverSpikesTrialOwner &&
                    managers.GetSaveManager().IsTitanTrialEnabled() && managers.GetSaveManager().GetCurrentScenarioData()?.name == FleshweaverSpikesTrialScenario;
                supported &= seraphTrial || fleshweaverTrial;
                data = replacement;
            }
            return data;
        }
    }
}
