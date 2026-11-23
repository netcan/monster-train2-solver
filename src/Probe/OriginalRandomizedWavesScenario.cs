#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRandomizedWavesScenario
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled { get; } = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_RANDOMIZED_WAVES") == "1";
        internal static bool Acquired { get; private set; }
        internal static object? DeploymentWaveOrder { get; private set; }

        internal static void Acquire(SaveManager save)
        {
            var owner = save.GetAllGameData().FindMutatorData(RelicRandomizedWavesModel.OwnerId);
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0 ||
                save.GetCurrentDistance() != 0 || owner == null || owner.name != RelicRandomizedWavesModel.OwnerKey ||
                owner.DisallowInDeploymentPhase || owner.GetEffects().Select(effect => effect.GetEffectClassName()).ToArray()
                    is not [RelicRandomizedWavesModel.Shuffle, RelicRandomizedWavesModel.ForceBonusEnemy] ||
                owner.GetEffects().Any(effect => effect.GetEffectConditions().Count != 0) ||
                owner.GetEffects().Last().GetParamCovenant()?.GetID() != RelicRandomizedWavesModel.BonusCovenantId)
                throw new InvalidOperationException("Expected the original randomized wave mutator in a fresh 2.3.1 battle.");

            save.AddMutator(owner);
            if (!RandomizedWaveProbe.MatchesOriginal(save.GetMutators().Single()))
                throw new InvalidOperationException("The original randomized wave mutator did not initialize.");
            Acquired = true;
        }

        internal static void Observe(int[] phases, IReadOnlyList<CovenantState> spawnCovenants)
        {
            if (!Acquired || DeploymentWaveOrder != null || AllGameManagers.Instance?.GetCombatManager()?.GetTurnCount() != 0)
                return;
            CovenantState bonus = spawnCovenants.Single();
            DeploymentWaveOrder = new
            {
                AuthoredWaveIndices = phases,
                BonusCovenantId = bonus.GetRelicDataID(),
                BonusCovenantAsset = bonus.GetAssetName()
            };
        }
    }
}
#endif
