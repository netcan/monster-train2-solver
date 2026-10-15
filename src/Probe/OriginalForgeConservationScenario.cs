using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalForgeConservationScenario
    {
        internal const string Name = "original-forge-conservation-relic";
        internal const string DataId = "b2868935-103c-4d27-ae48-a96d947c05ed";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Started { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool NaturalCompleted { get; private set; }
        internal static string? Error { get; private set; }
        private static string label = "natural";
        private static Record? active;
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public string MoonPhaseAfter { get; set; } = "";
            public int Notifications { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Forge conservation relic must precede battle.");
            var relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId)) throw new InvalidOperationException("SteelStockpile already acquired.");
            save.AddRelic(relic); Acquired = true;
            log.LogInfo("ORIGINAL-FORGE-CONSERVATION-ACQUIRED unchanged SteelStockpile cap 10 at combat end.");
        }
        internal static void Start(AllGameManagers managers, ManualLogSource log)
        {
            Started = true;
            managers.GetSaveManager().StartCoroutine(Protect(Run(managers, log), log));
        }
        private static IEnumerator Protect(IEnumerator native, ManualLogSource log)
        {
            while (true)
            {
                bool next;
                try { next = native.MoveNext(); }
                catch (Exception error) { Error = error.ToString(); log.LogError(Error); break; }
                if (!next) break;
                yield return native.Current;
            }
            (native as IDisposable)?.Dispose(); label = "natural"; Prepared = Error == null;
        }
        private static IEnumerator Run(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0) throw new InvalidOperationException("Forge calibration missed deployment.");
            var relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            foreach (int amount in new[] { 0, 5, 10, 17 })
            {
                label = "matrix-" + amount; save.SetForgePoints(amount);
                yield return managers.GetPlayerManager().OnCombatEnd();
                if (save.GetForgePoints() != Math.Min(amount, 10)) throw new InvalidOperationException("Native forge cap differs.");
            }
            save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == DataId));
            label = "ablation"; save.SetForgePoints(17);
            yield return managers.GetPlayerManager().OnCombatEnd();
            if (save.GetForgePoints() != 0) throw new InvalidOperationException("Native no-owner forge reset differs.");
            save.AddRelic(relic); save.SetForgePoints(17); label = "natural";
            log.LogInfo("ORIGINAL-FORGE-CONSERVATION-PREPARED four real PlayerManager.OnCombatEnd calls and removed-owner control; restored original owner; battle begins with 17 forge points.");
        }
        private static IEnumerator Wrap(IEnumerator native)
        {
            Record record = new Record { Kind = label, Before = FullBattleTrace.Active!.CaptureContext() };
            Records.Add(record); active = record;
            try { while (native.MoveNext()) yield return native.Current; }
            finally { active = null; }
            record.After = FullBattleTrace.Active!.CaptureContext();
            record.MoonPhaseAfter = AllGameManagers.Instance!.GetPlayerManager().CurrentMoonPhase.ToString();
            if (record.Kind == "natural") NaturalCompleted = true;
        }
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.OnCombatEnd))]
        private static class CombatEndPatch
        {
            private static void Postfix(ref IEnumerator __result)
            {
                if (Enabled && Acquired && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Wrap(__result);
            }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            { if (active != null && triggeredEffect is RelicEffectConserveForgePoints) active.Notifications++; }
        }
        private static void CheckOriginal(CollectableRelicData relic)
        {
            var effect = relic.GetEffects().Single();
            if (relic.name != "SteelStockpile" || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectConserveForgePoints" || effect.GetParamInt() != 10 ||
                effect.GetParamInt2() != 0 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original SteelStockpile definition changed.");
        }
    }
}
