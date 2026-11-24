#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalLonelyRoomsBattle
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_LONELY_ROOMS_BATTLE") == "1";
        internal static bool Acquired { get; private set; }

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(RelicState).Module.ModuleVersionId !=
                new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") || save.PreviewMode ||
                save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetCurrentDistance() != 0 ||
                save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the pinned original Ring 1 run before combat.");

            MutatorData definition = save.GetAllGameData().FindMutatorData(RelicModel.LonelyRoomsOwnerId)
                ?? throw new InvalidOperationException("Original LonelyRooms mutator was missing.");
            RelicEffectData effect = definition.GetEffects().Single();
            if (definition.name != RelicModel.LonelyRoomsOwnerKey || definition.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicModel.LonelyRoomsEffect || effect.GetEffectConditions().Count != 0 ||
                effect.GetParamInt() != RelicModel.LonelyRoomsPlayerSlots)
                throw new InvalidOperationException("Original LonelyRooms definition changed.");

            save.AddMutator(definition);
            if (!RelicPercentageStatProbe.SupportsLonelyRooms(save.GetMutators().Single(), save))
                throw new InvalidOperationException("Original LonelyRooms did not establish one player spawn point.");
            Acquired = true;
        }
    }
}
#endif
