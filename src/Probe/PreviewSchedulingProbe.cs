using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Observe original requests and preview boundaries without changing scheduling.
    internal static class PreviewSchedulingProbe
    {
        internal sealed class Record
        {
            public string Operation { get; set; } = "";
            public int Frame { get; set; }
            public int Turn { get; set; }
            public bool SavePreview { get; set; }
            public bool RunningQueue { get; set; }
            public int NextUnitId { get; set; }
            public int Room { get; set; }
            public string[] Callers { get; set; } = Array.Empty<string>();
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static void Observe(string operation, bool callers = false)
        {
            if (!PreviewReferenceProbe.Enabled || FullBattleTrace.Active == null || !EnchantmentBattleScenario.Prepared) return;
            AllGameManagers managers = AllGameManagers.Instance!;
            Records.Add(new Record { Operation = operation, Frame = UnityEngine.Time.frameCount,
                Turn = managers.GetCombatManager()!.GetTurnCount(), SavePreview = managers.GetSaveManager().PreviewMode,
                RunningQueue = managers.GetCombatManager()!.IsRunningTriggerQueue,
                NextUnitId = FullBattleTrace.Active.NextUnitId, Room = managers.GetRoomManager()!.GetSelectedRoom(),
                Callers = callers ? new StackTrace().GetFrames().Skip(2).Take(7).Select(frame =>
                    frame.GetMethod()!.DeclaringType?.FullName + "." + frame.GetMethod()!.Name).ToArray() : Array.Empty<string>() });
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ToggleCombatPreviewFromExternalSource))]
        private static class RequestPatch
        { private static void Prefix() => Observe("ExternalRequest", true); }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.OnCardPileChanged))]
        private static class PilePatch
        { private static void Prefix() => Observe("CardPileChanged", true); }
        [HarmonyPatch(typeof(CombatManager), "SetCharacterPreviewState")]
        private static class StatePatch
        { private static void Prefix(CharacterState.CombatPreviewState previewState) => Observe("Battle:" + previewState); }
        [HarmonyPatch(typeof(CombatManager), "SetTemporaryStateEnabled")]
        private static class TemporaryPatch
        { private static void Postfix(bool enable, bool isCardPreview) => Observe((isCardPreview ? "Card:" : "Boss:") + enable); }
    }
}
