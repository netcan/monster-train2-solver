#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDisabledRoomPreview
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_DISABLED_ROOM_PREVIEW") == "1";
        internal static bool Acquired { get; private set; }

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(SaveManager).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || !save.GetBattlePreviewEnabled() || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the original 2.3.1 run with combat preview initially available.");
            MutatorData definition = save.GetAllGameData().FindMutatorData(RelicModel.DisableRoomPreviewOwnerId)
                ?? throw new InvalidOperationException("The original NoCombatPreviews mutator is missing.");
            RelicEffectData effect = definition.GetEffects().Single();
            if (definition.name != RelicModel.DisableRoomPreviewOwnerKey || definition.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicModel.DisableRoomPreview || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("The original NoCombatPreviews definition changed.");
            save.AddMutator(definition);
            if (save.GetBattlePreviewEnabled() || save.GetMutators().Single().GetEffects() is not [RelicEffectDisableRoomPreview])
                throw new InvalidOperationException("Original NoCombatPreviews did not disable the native room preview.");
            Acquired = true;
        }
    }
}
#endif
