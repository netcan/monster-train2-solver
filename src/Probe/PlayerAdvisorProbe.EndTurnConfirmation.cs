#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;
using GameUISelectableButton = ShinyShoe.GameUISelectableButton;

namespace MonsterTrain2Poju.Probe
{
    internal static partial class PlayerAdvisorProbe
    {
        internal const string EndTurnConfirmationPass = "Player Advisor preserved advice after canceled end turn and observed accepted end turn.";
        internal static bool ObserveEndTurnConfirmation =>
            Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_END_TURN_CONFIRMATION") == "1";

        private sealed class EndTurnBoundary
        {
            public BattleTurnState Root { get; }
            public long Generation { get; } = GetGeneration();
            public long Searches { get; } = SearchCount;
            public int AcceptedEndTurns { get; } = observedEndTurnInputs;
            public long CardInputs { get; } = Get<long>("DiagnosticCardInputs");
            internal EndTurnBoundary(BattleTurnState root) => Root = root;
        }

        private static EndTurnBoundary? endTurnBefore, endTurnAfterCancel;
        private static object? endTurnAccepted;
        private static BattleAdvice? endTurnOriginalAdvice;
        private static int observedEndTurnInputs;
        private static bool endTurnCanceled;

        private static void InstallEndTurnConfirmationObserver()
        {
            var harmony = new Harmony("netcan.poju.end-turn-confirmation-acceptance");
            harmony.Patch(AccessTools.Method(type!, "RecordDiagnosticEndTurn"),
                postfix: new HarmonyMethod(typeof(PlayerAdvisorProbe), nameof(ObserveAcceptedEndTurn)));
        }

        private static void ObserveAcceptedEndTurn() => observedEndTurnInputs++;

        internal static bool TryCancelEndTurnConfirmation(AllGameManagers managers, DialogScreen screen)
        {
            if (endTurnBefore == null || endTurnCanceled) return false;
            var dialogs = (List<Dialog>)AccessTools.Field(typeof(DialogScreen), "activeDialogStack").GetValue(screen);
            Dialog? dialog = dialogs.LastOrDefault();
            if (dialog == null) return true;
            var data = (Dialog.Data?)AccessTools.Field(typeof(Dialog), "data").GetValue(dialog);
            Require(data?.callback1 != null && data.callback2 != null && observedEndTurnInputs == 0 &&
                managers.GetCombatManager().GetTurnCount() == 0 &&
                managers.GetSaveManager().HasSeenOneTimeMessage(OneTimeMessage.EndTurnWithoutPlayingChampion),
                "expected the original unplayed-champion confirmation before any accepted EndTurn");
            var cancel = (GameUISelectableButton)AccessTools.Field(typeof(Dialog), "button2").GetValue(dialog);
            if (!ReplayManager.CanClickButton(cancel) || !ShinyShoe.CoreInput.CanAcceptClickAtEventTime(Time.time)) return true;
            Require(ReplayManager.TryClickButton(cancel), "native end-turn cancel button did not accept the click");
            endTurnCanceled = true;
            return true;
        }

        internal static bool TryCaptureEndTurnConfirmation(AllGameManagers managers, FullBattleTrace trace)
        {
            Resolve();
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            if (endTurnAccepted != null)
            {
                if (combat.GetTurnCount() == 0 || !TryReadRestartPublication(out var after, out _, expectedTurn: 1)) return false;
                trace.CompleteEndTurn(RoomOutcome.Exchanged);
                var turn = trace.ObservedTurns.Single();
                Require(turn.Actual != null && turn.Predicted.Supported && turn.Difference == null &&
                    turn.ActualOutcome == RoomOutcome.Exchanged && observedEndTurnInputs == 1 && Get<long>("DiagnosticCardInputs") == 0,
                    "the accepted native EndTurn did not settle exactly with one accepted diagnostic");
                using var document = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, EvidenceKind = "NativeAdvisorEndTurnConfirmation", GameVersion = Application.version,
                    GameModuleMvid = typeof(CardState).Module.ModuleVersionId, Assembly = type!.Assembly.GetName().Name,
                    Before = endTurnBefore, AfterCancel = endTurnAfterCancel, Accepted = endTurnAccepted,
                    After = new EndTurnBoundary(after!), EndTurn = turn
                });
                using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                    "player-advisor-end-turn-confirmation.mt2f"));
                document.Write(output);
                return true;
            }
            if (endTurnBefore != null && !endTurnCanceled) return false;
            Require(save.GetRunType() == RunType.Class && save.GetCurrentDistance() == 0 && !save.IsEndlessRun &&
                save.GetCovenants().Count == 0 && combat.GetTurnCount() == 0 && combat.IsPlacementPhase &&
                managers.GetCardManager().HasChampionInHand() && trace.ObservedActionCount == 0 && trace.ObservedTurns.Count == 0,
                "end-turn confirmation requires the unplayed original Covenant 0 Ring 1 champion");
            if (!TryReadRestartPublication(out var current, out var advice)) return false;
            var button = UnityEngine.Object.FindObjectOfType<BattleHud>()?.GetEndTurnButton();
            if (button == null || !ReplayManager.CanClickButton(button) || !ShinyShoe.CoreInput.CanAcceptClickAtEventTime(Time.time)) return false;
            if (endTurnBefore == null)
            {
                Require(!save.HasSeenOneTimeMessage(OneTimeMessage.EndTurnWithoutPlayingChampion),
                    "the stock donor already consumed the original unplayed-champion confirmation");
                endTurnBefore = new EndTurnBoundary(current!);
                endTurnOriginalAdvice = advice;
                Require(ReplayManager.TryClickButton(button), "native end-turn request button did not accept the click");
                return false;
            }

            endTurnAfterCancel = new EndTurnBoundary(current!);
            using var keys = new BattleStateKeys();
            Require(keys.Key(endTurnBefore.Root) == keys.Key(current!) && ReferenceEquals(endTurnOriginalAdvice, advice) &&
                endTurnAfterCancel.Generation == endTurnBefore.Generation && endTurnAfterCancel.Searches == endTurnBefore.Searches &&
                endTurnAfterCancel.AcceptedEndTurns == 0 && endTurnAfterCancel.CardInputs == 0,
                "cancel changed the published advice, search generation, native state/RNG or accepted input count");
            trace.BeginEndTurn();
            Require(ReplayManager.TryClickButton(button), "native second end-turn request did not accept the click");
            bool retired = CurrentIsNull() && GetGeneration() > endTurnAfterCancel.Generation;
            Require(retired && observedEndTurnInputs == 1, "the accepted end turn must invalidate prior advice and record one input");
            endTurnAccepted = new { Generation = GetGeneration(), Searches = SearchCount, AcceptedEndTurns = observedEndTurnInputs,
                CardInputs = Get<long>("DiagnosticCardInputs"), PriorAdviceRetired = retired };
            return false;
        }
    }
}
#endif
