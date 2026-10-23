using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;

namespace MonsterTrain2Poju.Probe
{
    // Read-only stat projection shared by the actual player assembly and the
    // controlled native observations. This never activates a Covenant.
    internal static class CovenantBirthStatProbe
    {
        internal static bool Calibrated(SaveManager save)
        {
            string[] ids = save.GetCovenants().Select(covenant => covenant.GetRelicDataID()).ToArray();
            string[] known = { "3cc0272d-c0d7-46dc-9c7a-be9633572f70", "d3b53481-f26b-4344-ae8e-ec80b28d6004",
                "80cfbaca-da9a-4aa8-ac0d-a6563f26fcb8", "ad5c826a-e716-43cc-9c13-df0dd138de71" };
            return ids.Length > 0 && ids.Distinct().Count() == ids.Length && ids.All(known.Contains);
        }

        internal static bool WavesCalibrated(SaveManager save)
        {
            if (Calibrated(save) && PlayerTrueFinalBossProbe.Enabled) return true;
            var owners = save.GetCovenants();
            return owners.Any(owner => owner.GetRelicDataID() == "d3b53481-f26b-4344-ae8e-ec80b28d6004") &&
                owners.Select(owner => owner.GetRelicDataID()).Distinct().Count() == owners.Count &&
                owners.All(owner => owner.GetRelicDataID() is "3cc0272d-c0d7-46dc-9c7a-be9633572f70" or "d3b53481-f26b-4344-ae8e-ec80b28d6004" ||
                    owner.GetRelicDataID() == "92c18b99-e4c0-4953-ba2c-4815f570a66e" &&
                    owner.GetSourceRelicData().name == "Covenant02_BattlesNewEnemies" && owner.GetEffects().Count == 0 ||
                    owner.GetRelicDataID() == "dcb31cd7-4293-4b07-b396-5456a0ce0b6e" &&
                    owner.GetSourceRelicData().name == "Covenant03_MerchantCostsIncrease" &&
                    owner.GetEffects().Select(effect => effect.GetType().Name).SequenceEqual(new[] {
                        "RelicEffectModifyMerchantRerollCosts", "RelicEffectModifyMerchantPurgeCosts" }));
        }

        internal static EnemyBirthStatAdjustment? Capture(CharacterData data)
        {
            SaveManager save = AllGameManagers.Instance.GetSaveManager();
            if (!WavesCalibrated(save)) return null;
            EnemyBirthStatAdjustment result = Query(data, out object evidence);
#if DEVELOPMENT_PROBE
            CovenantEnemyStatsScenario.Observe(data, evidence);
            StartingCovenantScenario.ObserveStats(data, evidence);
#endif
            return result;
        }

        internal static bool OuterBossCalibrated(SaveManager save, CharacterData data)
        {
            if (save.GetCovenantsForSpawnPattern().Count == 0) return true;
            if (data.name is not ("Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter")) return false;
            return save.GetCovenantsForSpawnPattern().Select(owner => owner.GetRelicDataID()).SequenceEqual(
                save.GetCovenants().Select(owner => owner.GetRelicDataID())) &&
                WavesCalibrated(save) && save.GetCovenants().All(owner =>
                owner.GetRelicDataID() is "3cc0272d-c0d7-46dc-9c7a-be9633572f70" or "d3b53481-f26b-4344-ae8e-ec80b28d6004");
        }

        internal static EnemyBirthStatAdjustment Query(CharacterData data, out object evidence)
        {
            int frame = UnityEngine.Time.frameCount;
            object before = Rng();
            CharacterStatAdjustment adjustment = AllGameManagers.Instance.GetRelicManager().GetEnemyStatAdjustmentsFromAllRelics(data);
            object after = Rng();
            if (frame != UnityEngine.Time.frameCount || JsonConvert.SerializeObject(before) != JsonConvert.SerializeObject(after))
                throw new InvalidOperationException("Covenant stat preview changed native RNG or frame.");
            evidence = new { AssetKey = data.name, RawAttack = data.GetAttackDamage(), RawHealth = data.GetHealth(),
                Attack = adjustment.attackAdjustment, Health = adjustment.healthAdjustment,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = before, RngAfter = after };
            return new EnemyBirthStatAdjustment(adjustment.attackAdjustment, adjustment.healthAdjustment,
                AllGameManagers.Instance.GetSaveManager().GetCovenants().Select(covenant => covenant.GetRelicDataID()).ToArray());
        }

        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
            new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
    }
}
