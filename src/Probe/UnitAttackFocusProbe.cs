using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitAttackFocusProbe
    {
        internal sealed class Record
        {
            public string TargetMode { get; set; } = "";
            public int ParameterRoom { get; set; }
            public int SelectedRoom { get; set; }
            public int[] TargetIds { get; set; } = Array.Empty<int>();
            public int[] TargetRooms { get; set; } = Array.Empty<int>();
            public int AfterSelectedRoom { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        [HarmonyPatch(typeof(CardEffectAttackWithUnit), nameof(CardEffectAttackWithUnit.ApplyEffect))]
        private static class Patch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "ability-activation" &&
                    FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            RoomManager rooms = AllGameManagers.Instance!.GetRoomManager()!;
            var row = new Record { TargetMode = effect.GetTargetMode().ToString(), ParameterRoom = parameters.selectedRoom,
                SelectedRoom = rooms.GetSelectedRoom(), TargetIds = parameters.targets.Select(FullBattleTrace.Active!.UnitId).ToArray(),
                TargetRooms = parameters.targets.Select(unit => unit.GetCurrentRoomIndex()).ToArray() };
            Records.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.AfterSelectedRoom = rooms.GetSelectedRoom(); }
        }
    }
}
