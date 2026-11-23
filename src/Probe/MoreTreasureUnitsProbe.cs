using System;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MoreTreasureUnitsProbe
    {
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");

        internal static bool MatchesOriginal(RelicState owner, SaveManager save) =>
            owner.GetRelicDataID() == RelicMoreTreasureUnitsModel.OwnerId &&
            owner.GetEffects() is [RelicEffectChangeTreasureUnitCount { Value: 1f }] &&
            typeof(SaveManager).Module.ModuleVersionId == Module &&
            save.GetAllGameData().FindMutatorData(RelicMoreTreasureUnitsModel.OwnerId) is MutatorData definition &&
            IsOriginal(definition);

        private static bool IsOriginal(MutatorData definition) =>
            definition.name == RelicMoreTreasureUnitsModel.OwnerKey && !definition.DisallowInDeploymentPhase &&
            definition.GetEffects() is [var effect] && effect.GetEffectClassName() == RelicMoreTreasureUnitsModel.Effect &&
            effect.GetParamInt() == 1 && effect.GetEffectConditions().Count == 0;

#if DEVELOPMENT_PROBE
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_MORE_TREASURE_UNITS") == "1";
        internal static bool Acquired { get; private set; }

        internal static void Acquire(SaveManager save)
        {
            if (UnityEngine.Application.version != "2.3.1" || typeof(SaveManager).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 1 ||
                save.GetCovenants().Count != 0 || save.GetMutators().Count != 0 ||
                save.GetAllGameData().FindMutatorData(RelicMoreTreasureUnitsModel.OwnerId) is not MutatorData definition ||
                !IsOriginal(definition))
                throw new InvalidOperationException("Expected an unchanged original MoreTreasureUnits mutator and Ring 2 checkpoint.");
            save.AddMutator(definition);
            if (save.GetMutators() is not [var owner] || !MatchesOriginal(owner, save))
                throw new InvalidOperationException("Original MoreTreasureUnits balance effect did not initialize.");
            Acquired = true;
        }
#endif
    }
}
