using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicFrontRoomHealthProbe
    {
        internal sealed class Record
        {
            public string Owner { get; set; } = "";
            public int Turn { get; set; }
            public bool Completed { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public RoomCombatState[] BeforeRooms { get; set; } = Array.Empty<RoomCombatState>();
            public RoomCombatState[]? ActualRooms { get; set; }
        }

        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<object> Tests = new List<object>();
        private static bool Active => OriginalFrontRoomHealthUpgradeScenario.Acquired && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch]
        private static class TestPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(RelicEffectModifyFrontInRoomCharacterMaxHealthEachTurn), "TestEffectStartOfTurn");
            }

            private static void Postfix(RelicEffectModifyFrontInRoomCharacterMaxHealthEachTurn __instance,
                MethodBase __originalMethod, bool __result)
            {
                if (!Active) return;
                try
                {
                    Tests.Add(new { Owner = __instance.SourceRelicState!.GetRelicDataID(),
                        Phase = __originalMethod.Name, Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                        Returned = __result, Context = FullBattleTrace.Active!.CaptureContext() });
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyFrontInRoomCharacterMaxHealthEachTurn), "ApplyEffectStartOfTurn")]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectModifyFrontInRoomCharacterMaxHealthEachTurn __instance, ref IEnumerator __result)
            {
                if (Active) __result = Wrap(__result, __instance);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, RelicEffectModifyFrontInRoomCharacterMaxHealthEachTurn effect)
        {
            Record? record = null;
            try
            {
                try
                {
                    record = new Record { Owner = effect.SourceRelicState!.GetRelicDataID(),
                        Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                        Before = FullBattleTrace.Active!.CaptureContext(), BeforeRooms = CaptureRooms() };
                    Records.Add(record);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                while (native.MoveNext()) yield return native.Current;
                if (record != null) record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null)
                {
                    try { record.Actual = FullBattleTrace.Active!.CaptureContext(); }
                    catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                    try { record.ActualRooms = CaptureRooms(); }
                    catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                }
            }
        }

        private static RoomCombatState[] CaptureRooms()
        {
            var managers = AllGameManagers.Instance!;
            var roomManager = managers.GetRoomManager()!;
            return Enumerable.Range(0, roomManager.GetNumRooms())
                .Select(index => FullBattleTrace.Active!.Capture(roomManager.GetRoom(index)!, captureContext: false)).ToArray();
        }
    }
}
