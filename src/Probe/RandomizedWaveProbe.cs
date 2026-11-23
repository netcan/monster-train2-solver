using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RandomizedWaveProbe
    {
        internal static bool MatchesOriginal(RelicState owner) =>
            owner.GetRelicDataID() == RelicRandomizedWavesModel.OwnerId &&
            owner.GetAssetName() == RelicRandomizedWavesModel.OwnerKey && !owner.DisallowedInPlacementPhase &&
            owner.GetEffects() is [RelicEffectRandomizeWaveOrder shuffle, RelicEffectForceBonusEnemyCovenant bonus] &&
            bonus.GetCovenantData()?.GetID() == RelicRandomizedWavesModel.BonusCovenantId &&
            Unconditional(shuffle) && Unconditional(bonus);

        private static bool Unconditional(RelicEffectBase effect) =>
            ((RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect))
                .GetEffectConditions().Count == 0;

        internal static int[] CaptureIndices(AllGameManagers managers, SpawnPatternData pattern, int count,
            IReadOnlyList<CovenantState> spawnCovenants, List<string> interactions)
        {
            int[] indices = Enumerable.Range(0, count).ToArray();
            SaveManager save = managers.GetSaveManager();
            RelicState[] owners = save.GetMutators().Where(owner =>
                owner.GetRelicDataID() == RelicRandomizedWavesModel.OwnerId).ToArray();
            if (owners.Length == 0) return indices;

            if (owners is not [RelicState owner] || !MatchesOriginal(owner) || save.GetCovenants().Count != 0 ||
                save.GetScenarioData(save.GetCurrentDistance())?.GetSpawnPattern() != pattern)
            {
                interactions.Add("Uncalibrated original randomized wave owner or scenario.");
                return indices;
            }

            var effects = owner.GetEffects();
            if (spawnCovenants is not [CovenantState nativeBonus] ||
                nativeBonus.GetRelicDataID() != RelicRandomizedWavesModel.BonusCovenantId)
            {
                interactions.Add("Missing or mixed native bonus enemy Covenant.");
                return indices;
            }

            if (pattern.GetIsLoopingScenario()) return indices;
            if (!managers.GetRelicManager().TryGetRandomizedWaveOrderRelic(out var active) ||
                !ReferenceEquals(active, effects[0]))
            {
                interactions.Add("Missing or overridden original randomized wave order.");
                return indices;
            }

            // Native consumes the shuffle RNG before deployment. Project its settled
            // permutation and preserve the last two authored wave indices.
            IReadOnlyList<int> order = active.RandomWaveOrder;
            int shuffledCount = Math.Max(0, count - 2);
            if (order.Count != shuffledCount || !order.OrderBy(index => index).SequenceEqual(indices.Take(shuffledCount)))
            {
                interactions.Add("Missing or invalid settled native randomized wave order.");
                return indices;
            }

            for (int phase = 0; phase < order.Count; phase++) indices[phase] = order[phase];
            return indices;
        }
    }
}
