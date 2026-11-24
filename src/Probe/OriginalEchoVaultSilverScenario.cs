#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEchoVaultSilverScenario
    {
        internal const string RelicId = "1a69fbff-9f1d-4c98-92ff-a63e0ac520c5";
        internal const string RelicKey = "SilverIncreasedEmber";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ECHO_VAULT_SILVER") == "1";
        internal static bool Acquired { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (!Enabled || Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (typeof(RelicState).Assembly.ManifestModule.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                OriginalSupportSpellsScenario.IsolatedIds?.SequenceEqual(new[]
                {
                    RoomEchoProbe.CardId, "64beabe3-0260-44e2-a47a-b07192b52f25",
                    "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b", "f9f20326-dccb-44af-894b-467534afab4a"
                }) != true)
                throw new InvalidOperationException("Echo Vault/Silver witness requires the pinned original isolated spell setup before battle.");

            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            if (relic.name != RelicKey || relic.DisallowInDeploymentPhase ||
                !save.IsDlcAvailableWhenStartingRun(relic.GetRequiredDLC()) ||
                effects.Length != 2 ||
                effects[0].GetEffectClassName() != "RelicEffectModifyEnergy" ||
                effects[1].GetEffectClassName() != "RelicEffectModifyEnergyDuringDeployment" ||
                effects.Any(effect => effect.GetParamInt() != 1 || effect.GetParamInt2() != 0 ||
                    effect.GetParamSourceTeam() != Team.Type.None || effect.GetParamBool() || effect.GetParamBool2() ||
                    effect.GetEffectConditions().Count != 0 || effect.GetParamMutatorData() != null ||
                    effect.GetParamCardUpgradeData() != null || effect.GetParamCharacters().Count != 0) ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("Pinned original Silver Increased Ember relic definition or ownership changed.");

            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-ECHO-VAULT-SILVER-ACQUIRED unchanged relic=" + RelicKey +
                " id=" + RelicId + " requiredDlc=" + relic.GetRequiredDLC() + ".");
        }
    }
}
#endif
