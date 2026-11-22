#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOpeningRelicsScenario
    {
        private const string Prefix = "original-opening-relics:";
        private static readonly string Modifier = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "";
        internal static bool Enabled => Modifier.StartsWith(Prefix, StringComparison.Ordinal);
        internal static bool Acquired { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            var save = managers.GetSaveManager();
            if (typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.InTestScenario || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original opening relics require a fresh 2.3.1 Ring 1 before combat.");

            string[] ids = Modifier.Substring(Prefix.Length).Split(',');
            if (ids.Any(id => !Guid.TryParseExact(id, "D", out Guid parsed) || parsed.ToString() != id) ||
                ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
                throw new InvalidOperationException("Expected distinct original collectible relic IDs.");

            var data = save.GetAllGameData().GetAllCollectableRelicData();
            var owned = save.GetCollectedRelics().Select(relic => relic.GetRelicDataID()).ToArray();
            var definitions = ids.Select(id =>
            {
                var relic = data.Single(item => item.GetID() == id);
                var effect = relic.GetEffects().Single();
                var status = effect.GetParamStatusEffects().Single();
                if (owned.Contains(id) || relic.DisallowInDeploymentPhase ||
                    effect.GetEffectClassName() != nameof(RelicEffectAddStatusEffectOnSpawn) ||
                    effect.GetParamSourceTeam() is not (Team.Type.Monsters or Team.Type.Heroes) ||
                    effect.GetParamCharacterSubtype()?.IsNone != true || effect.GetParamExcludeCharacterSubtypes().Length != 0 ||
                    effect.GetParamCharacters().Count != 0 || effect.GetEffectConditions().Count != 0 ||
                    effect.GetParamBool() || effect.GetParamBool2() || effect.GetParamInt2() != 0 ||
                    effect.GetParamString() != string.Empty || string.IsNullOrEmpty(status.statusId) || status.count <= 0)
                    throw new InvalidOperationException("Expected an original unconditional spawn-status relic: " + relic.name);
                return (Relic: relic, StatusId: status.statusId);
            }).ToArray();
            if (definitions.Select(item => item.StatusId).Distinct(StringComparer.Ordinal).Count() != definitions.Length)
                throw new InvalidOperationException("Each opening relic needs a distinct status for its native callback witness.");

            foreach (var item in definitions) save.AddRelic(item.Relic);
            if (!save.GetCollectedRelics().Select(relic => relic.GetRelicDataID()).Where(ids.Contains).SequenceEqual(ids))
                throw new InvalidOperationException("Collected original relics differ from acquisition order.");
            Acquired = true;
            log.LogInfo("ORIGINAL-OPENING-RELICS acquired unchanged original definitions " + string.Join(",", ids));
        }
    }
}
#endif
