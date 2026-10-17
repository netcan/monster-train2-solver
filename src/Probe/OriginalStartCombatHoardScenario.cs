using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStartCombatHoardScenario
    {
        internal const string Name = "original-start-combat-hoard-relic";
        internal const string DataId = "3b6780ab-a1e8-443d-9ddb-5caedbd87775";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Start-combat Hoard relic must be acquired before battle.");
            var relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Start-combat Hoard relic already acquired.");
            if (save.GetDragonsHoardCap() <= 0)
                throw new InvalidOperationException("Start-combat Hoard probe requires positive native Hoard capacity.");
            if (save.GetDragonsHoardAmount() >= save.GetDragonsHoardCap())
                save.SetDragonsHoardAmount(save.GetDragonsHoardCap() - 1);
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-START-COMBAT-HOARD-ACQUIRED unchanged ModifyDragonsHoardBattleStart, native amount +1.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Start-combat Hoard relic missed deployment.");
            CheckOriginal(managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId));
            Prepared = true;
            log.LogInfo("ORIGINAL-START-COMBAT-HOARD-PREPARED native +1 callback, resource cap and deployment refreshes captured.");
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            var effect = relic.GetEffects().Single();
            if (relic.name != "ModifyDragonsHoardBattleStart" || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectModifyDragonsHoardStartOfCombat" || effect.GetParamInt() != 1 ||
                effect.GetParamInt2() != 0 || effect.GetParamFloat() != 0 || effect.GetParamBool() ||
                effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original ModifyDragonsHoardBattleStart definition changed.");
        }
    }
}
