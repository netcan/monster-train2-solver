using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Exercise several unchanged catalog artifacts through the same native battle.
    // Only the fixture's cards are authored; no relic or relic payload is replaced.
    internal static class OriginalRelicBatchScenario
    {
        internal const string Name = "original-relic-batch";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name || OriginalStingRelicBatchScenario.Enabled || OriginalUnitRelicBatchScenario.Enabled || OriginalTriggerRelicBatchScenario.Enabled || OriginalStatusRelicBatchScenario.Enabled || OriginalMagnitudeRelicBatchScenario.Enabled || OriginalLifecycleRelicBatchScenario.Enabled || OriginalDecayRelicBatchScenario.Enabled;
        private static bool prepared, captureReady;
        internal static bool Prepared => prepared || OriginalStingRelicBatchScenario.Prepared || OriginalUnitRelicBatchScenario.Prepared || OriginalTriggerRelicBatchScenario.Prepared || OriginalStatusRelicBatchScenario.Prepared || OriginalMagnitudeRelicBatchScenario.Prepared || OriginalLifecycleRelicBatchScenario.Prepared || OriginalDecayRelicBatchScenario.Prepared;
        internal static bool CaptureReady => captureReady || OriginalStingRelicBatchScenario.CaptureReady || OriginalUnitRelicBatchScenario.CaptureReady || OriginalTriggerRelicBatchScenario.CaptureReady || OriginalStatusRelicBatchScenario.CaptureReady || OriginalMagnitudeRelicBatchScenario.CaptureReady || OriginalLifecycleRelicBatchScenario.CaptureReady || OriginalDecayRelicBatchScenario.CaptureReady;
        internal static readonly string[] ArtifactKeys =
        {
            "MoltenManifold", "SpawnWithRage", "UnitsGainLifesteal", "ContingencyPlans"
        };

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!Enabled || Prepared) return;
            if (OriginalStingRelicBatchScenario.Enabled) { OriginalStingRelicBatchScenario.Prepare(managers, log); return; }
            if (OriginalDecayRelicBatchScenario.Enabled) { OriginalDecayRelicBatchScenario.Prepare(managers, log); return; }
            if (OriginalLifecycleRelicBatchScenario.Enabled) { OriginalLifecycleRelicBatchScenario.Prepare(managers, log); return; }
            if (OriginalMagnitudeRelicBatchScenario.Enabled) { OriginalMagnitudeRelicBatchScenario.Prepare(managers, log); return; }
            if (OriginalStatusRelicBatchScenario.Enabled) { OriginalStatusRelicBatchScenario.Prepare(managers, log); return; }
            if (OriginalTriggerRelicBatchScenario.Enabled) { OriginalTriggerRelicBatchScenario.Prepare(managers, log); return; }
            if (OriginalUnitRelicBatchScenario.Enabled) { OriginalUnitRelicBatchScenario.Prepare(managers, log); return; }
            CardCostScenario.Prepare(managers, log, lethal: false);
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CharacterData[] stewards = owned.Select(card => card.GetSpawnCharacterData())
                .Where(unit => unit != null && unit.name.StartsWith("TrainSteward", StringComparison.Ordinal)).Distinct().ToArray()!;
            if (stewards.Length < 2) throw new InvalidOperationException("Original relic batch needs matching and nonmatching unit births.");
            // This fixture authors one carrier subtype, while every acquired relic
            // and its payload retain their original catalog definition.
            var subtypes = (List<string>)AccessTools.Field(typeof(CharacterData), "subtypeKeys").GetValue(stewards[0]);
            if (!subtypes.Contains("SubtypesData_Monstrosity")) subtypes.Add("SubtypesData_Monstrosity");
            var definitions = save.GetAllGameData().GetAllCollectableRelicData();
            Acquire("f141d334-b503-48ff-ac21-5f2ad27105dd", "MoltenManifold", null, 0);
            Acquire("55ca34e9-047b-4b93-b390-d8d228a43261", "SpawnWithRage", "buff", 3);
            Acquire("a6b0b657-8893-4cc8-bc62-ec09544c4306", "UnitsGainLifesteal", "lifesteal", 2);
            Acquire("0278309e-d15c-4041-a8dc-b76f360f306b", "ContingencyPlans", "spikes", 7);

            // Capture once the complete batch is present. Repeated application and
            // reset exercise native non-unique stacking and rejected non-X filters.
            captureReady = true;
            CardUpgradeData upgrade = definitions.Single(relic => relic.name == "MoltenManifold")
                .GetEffects().Single().GetParamCardUpgradeData();
            if (!owned.Any(card => upgrade.GetFilters().All(filter => filter.FilterCard(card, managers.GetRelicManager()))))
                throw new InvalidOperationException("Original relic batch lacks an eligible native X-cost card.");
            foreach (CardState card in owned)
            {
                managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
            }
            prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-RELIC-BATCH-PREPARED unchanged " + string.Join(", ", ArtifactKeys) +
                "; native X-cost cards and paid unit births; original Boss/waves retained.");

            void Acquire(string id, string key, string? statusId, int stacks)
            {
                CollectableRelicData relic = definitions.Single(item => item.GetID() == id);
                RelicEffectData effect = relic.GetEffects().Single();
                if (relic.name != key || effect.GetEffectConditions().Count != 0 ||
                    effect.GetParamSourceTeam() != Team.Type.Monsters)
                    throw new InvalidOperationException("Original relic batch identity/team/conditions changed: " + key);
                if (statusId == null)
                {
                    CardUpgradeData payload = effect.GetParamCardUpgradeData();
                    if (effect.GetEffectClassName() != "RelicEffectAddTempUpgrade" || payload == null ||
                        payload.name != "TinkerersToolbelt_Upgrade" || payload.IsUnique() || payload.GetXCostReduction() != 2 ||
                        payload.GetFilters().Count != 1 || payload.GetFilters()[0].name != "OnlyXCost")
                        throw new InvalidOperationException("Original MoltenManifold X-cost payload changed.");
                }
                else
                {
                    StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
                    if (effect.GetEffectClassName() != "RelicEffectAddStatusEffectOnSpawn" || statuses.Length != 1 ||
                        statuses[0].statusId != statusId || statuses[0].count != stacks)
                        throw new InvalidOperationException("Original spawn-status payload changed: " + key);
                }
                save.AddRelic(relic);
            }
        }
    }
}
