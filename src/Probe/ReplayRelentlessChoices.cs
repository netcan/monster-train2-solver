#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;
using GameUISelectableButton = ShinyShoe.GameUISelectableButton;

namespace MonsterTrain2Poju.Probe
{
    internal static class ReplayRelentlessChoices
    {
        private static IReadOnlyList<RelentlessDecision>? expected;
        private static readonly List<RelentlessDecision> actual = new List<RelentlessDecision>();
        private static int turn;
        private static Dialog? clicked;
        private static Dialog.Data? clickedData;
        private static GameUISelectableButton? requestedButton;

        internal static bool Enabled => RollingAdviceScenario.Enabled || PlannerReplayScenario.Enabled;
        internal static bool Armed => expected != null;
        internal static IReadOnlyList<RelentlessDecision>? Planned => expected?.Count > 0 ? expected : null;

        internal static void Arm(int nativeTurn, IReadOnlyList<RelentlessDecision>? choices)
        {
            if (Armed) throw new InvalidOperationException("The previous Relentless EndTurn is still pending.");
            turn = nativeTurn;
            expected = choices?.ToArray() ?? Array.Empty<RelentlessDecision>();
            actual.Clear();
            clicked = null;
            clickedData = null;
            requestedButton = null;
        }

        internal static IReadOnlyList<RelentlessDecision>? Complete()
        {
            if (expected == null || actual.Count != expected.Count || requestedButton != null)
                throw new InvalidOperationException("Native EndTurn did not consume every planned Relentless choice.");
            var result = actual.Count > 0 ? actual.ToArray() : null;
            expected = null;
            clicked = null;
            clickedData = null;
            return result;
        }

        internal static bool TryHandle(DialogScreen screen, ManualLogSource log)
        {
            if (expected == null) return false;
            var stack = (List<Dialog>)AccessTools.Field(typeof(DialogScreen), "activeDialogStack").GetValue(screen);
            Dialog? dialog = stack.LastOrDefault();
            if (dialog == null) return false;
            var data = (Dialog.Data?)AccessTools.Field(typeof(Dialog), "data").GetValue(dialog);
            if (data?.content != "Combat_Relentless_Description".Localize()) return false;
            if (ReferenceEquals(clicked, dialog) && ReferenceEquals(clickedData, data)) return true;

            CombatManager combat = AllGameManagers.Instance!.GetCombatManager();
            var (room, rounds) = RelentlessBattleScenario.CurrentExchange();
            if (combat.GetTurnCount() != turn || actual.Count >= expected.Count ||
                data.button1Text != "Combat_Relentless_Abandon".Localize() ||
                data.button2Text != "Combat_Relentless_Continue".Localize() || data.callback1 == null || data.callback2 == null)
                throw new InvalidOperationException("Unexpected native Relentless dialog during frozen EndTurn.");
            RelentlessDecision choice = expected[actual.Count];
            if (choice.RoomIndex != room || choice.CompletedRounds != rounds)
                throw new InvalidOperationException($"Native Relentless dialog at room {room}, round {rounds} differs from planned room {choice.RoomIndex}, round {choice.CompletedRounds}.");
            GameUISelectableButton button = choice.Continue
                ? (GameUISelectableButton)AccessTools.Field(typeof(Dialog), "button2").GetValue(dialog) : dialog.GetButton1();
            if (!ReplayManager.CanClickButton(button) || !ShinyShoe.CoreInput.CanAcceptClickAtEventTime(Time.time)) return true;
            clicked = dialog;
            clickedData = data;
            requestedButton = button;
            if (!ReplayManager.TryClickButton(button) || requestedButton != null)
                throw new InvalidOperationException("Native Relentless button did not activate through its normal input path.");
            log.LogInfo($"RELENTLESS-REPLAY-CHOICE turn={turn} room={room} rounds={rounds} continue={choice.Continue}");
            return true;
        }

        [HarmonyPatch(typeof(Dialog), "ActivateButton")]
        private static class ActivationPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(Dialog __instance, GameUISelectableButton button, bool __result)
            {
                if (!__result || requestedButton == null || !ReferenceEquals(__instance, clicked) ||
                    !ReferenceEquals(button, requestedButton)) return;
                RelentlessDecision choice = expected![actual.Count];
                actual.Add(choice);
                RelentlessBattleScenario.RecordDialog(turn, choice);
                requestedButton = null;
            }
        }
    }
}
#endif
