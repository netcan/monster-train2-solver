using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFunguyDeckScalingScenario
    {
        internal const string Name = "original-funguy-deck-scaling";
        internal const int TargetDeckCount = 40;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static object? Evidence => Prepared ? new { DeckCount = TargetDeckCount, Threshold = 20, ApplyAllRooms = true } : null;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Funguy deck scaling must be acquired before battle.");
            int initialCount = save.GetVisibleDeckCount();
            if (initialCount >= TargetDeckCount)
                throw new InvalidOperationException("The isolated run must start below the 40-card threshold.");
            CardData steward = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData() != null)!;
            for (int count = initialCount; count < TargetDeckCount; count++)
                save.AddCardToDeck(steward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            if (save.GetVisibleDeckCount() != TargetDeckCount)
                throw new InvalidOperationException("Visible deck count did not reach exactly 40.");

            RelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicPileStartTurnModel.DataId);
            ValidateDefinition(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicPileStartTurnModel.DataId))
                throw new InvalidOperationException("The isolated run already owns the Funguy deck-scaling relic.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-FUNGUY-DECK-ACQUIRED unchanged relic at exactly 40 visible deck cards.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetVisibleDeckCount() != TargetDeckCount ||
                !save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicPileStartTurnModel.DataId))
                throw new InvalidOperationException("Funguy deck scaling requires exact deployment state.");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-FUNGUY-DECK-PREPARED deck=40 threshold=20 expectedApplicationsPerRoom=2.");
        }

        private static void ValidateDefinition(RelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            CardEffectData nested = effect.GetParamCardEffectData().Single();
            StatusEffectStackData status = nested.GetParamStatusEffects().Single();
            if (relic.name != RelicPileStartTurnModel.AssetKey || !relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicPileStartTurnModel.Effect || effect.GetParamInt() != 20 ||
                effect.GetParamInt2() != 0 || effect.GetParamMinInt() != 0 || effect.GetParamMaxInt() != 20 ||
                !effect.GetParamBool() || effect.GetEffectConditions().Count != 0 ||
                nested.GetEffectStateName() != "CardEffectSpawnMonsterOrAddStatusEffect" ||
                nested.GetTargetMode() != TargetMode.Room || nested.GetTargetTeamType() != Team.Type.Monsters ||
                nested.GetParamInt() != 1 || nested.GetParamCharacterData()?.GetID() != "ecf0adcc-be11-4577-8b0b-efe060578cb5" ||
                nested.GetTargetCharacterSubtype().Key != "SubtypesData_Horde" || status.statusId != "horde" || status.count != 1)
                throw new InvalidOperationException("Native Funguy deck-scaling relic definition changed.");
        }
    }
}
