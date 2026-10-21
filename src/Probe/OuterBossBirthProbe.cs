using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OuterBossBirthProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_BIRTH") == "1";
        private sealed class Record
        {
            public OuterBossCreationState? Before { get; set; }
            public OuterBossPhaseState? Actual { get; set; }
            public bool NativeReturned { get; set; }
            internal CharacterState? Created;
            internal CombatUnit? Definition;
        }
        private static readonly List<Record> Records = new List<Record>();
        private static readonly List<string> Errors = new List<string>();
        private static Record? pending;
        internal static CombatUnit? CreatedDefinition => pending?.Definition;
        private static bool Capturing => Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance.GetSaveManager().PreviewMode;
        private static IEnumerator ObserveBirth(IEnumerator native)
        {
            Record? row = null;
            if (Capturing)
            {
                row = new Record(); Records.Add(row); pending = row;
            }
            bool completed = false;
            try { while (native.MoveNext()) yield return native.Current; completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (row != null)
                {
                    try
                    {
                        row.NativeReturned = completed;
                        row.Actual = new OuterBossPhaseState(FullBattleTrace.Active!.CaptureDecision(true, true), OuterBossPhaseProbe.CaptureProgress());
                    }
                    catch (Exception error) { Errors.Add(error.ToString()); }
                    pending = null;
                }
            }
        }
        private static IEnumerator ObserveCreation(IEnumerator native, Record row)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    if (row.Created == null) throw new InvalidOperationException("Native outer-boss creation callback was missing.");
                    var interactions = new List<string>();
                    if (AllGameManagers.Instance.GetSaveManager().GetCovenantsForSpawnPattern().Count > 0)
                        interactions.Add("Outer-boss OnSpawn Covenant effects require separate calibration.");
                    CombatUnit unit = FullBattleTrace.Active!.CaptureUnit(row.Created, interactions);
                    row.Definition = unit;
                    row.Before = new OuterBossCreationState(FullBattleTrace.Active.CaptureDecision(true, true), unit,
                        OuterBossPhaseProbe.CaptureProgress(row.Created), interactions,
                        new EnemyMovement(unit.Id, 1, row.Created.GetAscendsTrainAutomatically(), row.Created.GetLoopsBetweenTrainFloors(), row.Created.IsCompanionBoss()));
                }
                catch (Exception error) { Errors.Add(error.ToString()); }
            }
        }
        internal static void Write(ManualLogSource log)
        {
            if (!Enabled) return;
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "AfterCreateHeroStateBeforeInitialBossAction", Births = Records, Errors }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "outer-boss-birth-calibration.mt2f"))) document.Write(output);
            if (Records.Count != 1 || Records.Any(row => row.Before == null || row.Actual == null || !row.NativeReturned) || Errors.Count != 0)
                throw new InvalidOperationException("Native outer-boss birth capture failed.");
            log.LogInfo("OUTER-BOSS-BIRTH-CAPTURE births=" + Records.Count + " errors=" + Errors.Count);
        }
        [HarmonyPatch(typeof(HeroManager), "DoOuterTrainBossSpawning")]
        private static class BirthPatch
        { private static void Postfix(ref IEnumerator __result) { if (Enabled) __result = ObserveBirth(__result); } }
        [HarmonyPatch(typeof(HeroManager), "CreateHeroState")]
        private static class CreationPatch
        {
            private static void Prefix(CharacterData heroData, ref Action<CharacterState, CharacterData> afterCharacterCreated, out Record? __state)
            {
                __state = null;
                if (!Capturing || pending == null || !heroData.IsOuterTrainBoss()) return;
                Record row = pending; __state = row;
                Action<CharacterState, CharacterData> callback = afterCharacterCreated;
                afterCharacterCreated = (unit, data) => { row.Created = unit; callback?.Invoke(unit, data); };
            }
            private static void Postfix(ref IEnumerator __result, Record? __state)
            { if (__state != null) __result = ObserveCreation(__result, __state); }
        }
    }
}
