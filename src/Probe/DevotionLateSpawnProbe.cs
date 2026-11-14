using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionLateSpawnProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        private static readonly List<Record> active = new List<Record>();
        private static bool observing, controlsDone, control;
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public string OwnerId { get; set; } = "";
            public string Trigger { get; set; } = "";
            public bool Control { get; set; }
            public int ActorId { get; set; }
            public bool? Returned { get; set; }
            public bool? ExpectedReturn { get; set; }
            public int Notifications { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
        }
        internal static void Controls()
        {
            if (controlsDone) return; controlsDone = true; control = true;
            try
            {
                var managers = AllGameManagers.Instance!;
                var effect = managers.GetSaveManager().GetCollectedRelics().Single(owner =>
                    owner.GetRelicDataID() == RelicUnitSpawnEffectModel.DevotionDataId).GetEffects().OfType<RelicEffectAddEffectOnUnitSpawn>().Single();
                CharacterState player = FullBattleTrace.Active!.KnownUnits.First(unit => unit.IsAlive && !unit.IsPyreHeart() && unit.GetTeamType() == Team.Type.Monsters);
                foreach (CharacterTriggerData.Trigger kind in new[] { CharacterTriggerData.Trigger.OnSpawn,
                    (CharacterTriggerData.Trigger)Enum.Parse(typeof(CharacterTriggerData.Trigger), "OnSpawnLate"), CharacterTriggerData.Trigger.OnDeath })
                    effect.TestCharacterTriggerEffect(new CharacterTriggerRelicEffectParams(new RelicEffectParams()) {
                        characterState = player, trigger = kind }, managers.GetCoreManagers());
                foreach (CharacterState? actor in FullBattleTrace.Active.KnownUnits.Where(unit => unit.IsAlive &&
                    (unit.GetTeamType() == Team.Type.Heroes || unit.IsPyreHeart())).Cast<CharacterState?>().Concat(new CharacterState?[] { null }))
                    effect.TestCharacterTriggerEffect(new CharacterTriggerRelicEffectParams(new RelicEffectParams()) {
                        characterState = actor!, trigger = (CharacterTriggerData.Trigger)Enum.Parse(typeof(CharacterTriggerData.Trigger), "OnSpawnLate") }, managers.GetCoreManagers());
            }
            finally { control = false; }
        }
        private static string Owner(RelicEffectAddEffectOnUnitSpawn effect) =>
            ((RelicState)AccessTools.Field(effect.GetType(), "_srcRelicState").GetValue(effect)).GetRelicDataID();
        private static Record? Begin(string kind, string trigger, CharacterState? actor, string ownerId = "")
        {
            if (!DevotionLateSpawnBattleScenario.Prepared || observing || AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                !control && trigger is not ("OnSpawn" or "OnSpawnLate")) return null;
            observing = true;
            var row = new Record { Kind = kind, Trigger = trigger, OwnerId = ownerId, Control = control }; Records.Add(row);
            try
            {
                row.ActorId = actor == null ? 0 : FullBattleTrace.Active!.UnitId(actor);
                row.Before = FullBattleTrace.Active!.CaptureTrain();
                if (kind == "Test")
                {
                    row.ExpectedReturn = RelicUnitSpawnEffectModel.Test(actor == null ? null : FullBattleTrace.Active.CaptureUnit(actor), trigger, ownerId);
                    row.Predicted = row.Before;
                }
                else
                {
                    TrainCombatState state = row.Before;
                    string[] owners = kind == "Manager" ? state.Context!.Relics!.Where(owner => owner.EffectTypes.Contains(RelicUnitSpawnEffectModel.Effect))
                        .Select(owner => owner.DataId).ToArray() : new[] { ownerId };
                    foreach (string id in owners)
                    {
                        TrainCombatResult result = RelicUnitSpawnEffectModel.Apply(state, row.ActorId, trigger, kind == "Manager", id);
                        if (!result.Supported) { row.UnsupportedReason = result.UnsupportedReason; break; }
                        state = result.State!;
                    }
                    if (row.UnsupportedReason == null) row.Predicted = state;
                }
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
            active.Add(row); return row;
        }
        private static void Finish(Record? row, bool? result = null)
        {
            if (row == null) return; active.Remove(row); observing = true;
            try
            {
                row.Returned = result; row.Actual = FullBattleTrace.Active!.CaptureTrain(); row.Completed = true;
                row.Difference = result != row.ExpectedReturn ? "Native trigger admission differs." : row.Predicted == null ? null :
                    HalfHealthLifecycleCalibration.Difference(row.Predicted, row.Actual);
            }
            catch (Exception error) { row.Error = error.ToString(); }
            finally { observing = false; }
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnUnitSpawn), nameof(RelicEffectAddEffectOnUnitSpawn.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectAddEffectOnUnitSpawn __instance, CharacterTriggerRelicEffectParams relicEffectParams, out Record? __state) =>
                __state = DevotionLateSpawnBattleScenario.Prepared ? Begin("Test", relicEffectParams.trigger.ToString(), relicEffectParams.characterState, Owner(__instance)) : null;
            private static void Postfix(bool __result, Record? __state) => Finish(__state, __result);
        }
        [HarmonyPatch(typeof(RelicEffectAddEffectOnUnitSpawn), nameof(RelicEffectAddEffectOnUnitSpawn.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddEffectOnUnitSpawn __instance, CharacterTriggerRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (DevotionLateSpawnBattleScenario.Prepared) __result = Observe(__result, "Apply", relicEffectParams.trigger.ToString(), relicEffectParams.characterState, Owner(__instance)); }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyCharacterTriggerRelicEffect))]
        private static class ManagerPatch
        {
            private static void Postfix(CharacterTriggerData.Trigger trigger, CharacterState characterState, ref IEnumerator __result)
            { if (DevotionLateSpawnBattleScenario.Prepared) __result = Observe(__result, "Manager", trigger.ToString(), characterState); }
        }
        private static IEnumerator Observe(IEnumerator native, string kind, string trigger, CharacterState actor, string ownerId = "")
        {
            Record? row = Begin(kind, trigger, actor, ownerId);
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); Finish(row); }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered), new[] { typeof(RelicState), typeof(IRelicEffect) })]
        private static class NotifyPatch
        {
            private static void Prefix(IRelicEffect triggeredEffect)
            { if (triggeredEffect is RelicEffectAddEffectOnUnitSpawn) foreach (Record row in active) row.Notifications++; }
        }
    }
}
