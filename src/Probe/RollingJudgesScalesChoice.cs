#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using ShinyShoe.Loading;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class RollingJudgesScalesChoice
    {
        private bool running;
        private string? error;

        internal bool Tick(AllGameManagers managers, CombatManager combat, CardManager cards, ManualLogSource log)
        {
            if (error != null) throw new InvalidOperationException(error);
            if (running) return true;
            // This owner fires on deployment. The native phase check skips it on the regular opening turn.
            if (!combat.IsPlacementPhase || !combat.AllowExternalInput || !combat.HaveCardsBeenDrawnForCurrentTurn() ||
                !combat.ShouldShowEndTurnButton() || LoadingScreen.IsWorking() || managers.GetSaveManager().PreviewMode ||
                managers.GetScreenManager().GetTopScreen() != ScreenName.Game || !CardAnimationSettlement.Ready()) return false;
            HandUI hand = managers.GetHandUI();
            HandSelectionUI? selection = Selection(hand);
            if (selection == null || !ReplayManager.CanClickButton(selection.GetCancelButton())) return false;

            running = true;
            managers.GetSaveManager().StartCoroutine(Choose(hand, selection, cards, log));
            return true;
        }

        private static HandSelectionUI? Selection(HandUI hand)
        {
            if (!hand.IsHandSelectionActive() || hand.IsHandSelectionHidden()) return null;
            var selection = (HandSelectionUI)AccessTools.Field(typeof(HandUI), "handSelectionUI").GetValue(hand);
            var callback = AccessTools.Field(typeof(HandSelectionUI), "cancelledCallback").GetValue(selection) as Delegate;
            return callback?.Method.Name == "HandleDiscardComplete" &&
                callback.Target is RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw effect &&
                effect.SourceRelicState is RelicState owner && !owner.DisallowedInPlacementPhase &&
                owner.GetRelicDataID() == RelicJudgesScalesModel.DataId ? selection : null;
        }

        private IEnumerator Choose(HandUI hand, HandSelectionUI selection, CardManager cards, ManualLogSource log)
        {
            CardState[] before = cards.GetHand().ToArray();
            string beforeRng = Rng();
            log.LogInfo("ROLLING-JUDGES-SCALES-START turn=0 owner=" + RelicJudgesScalesModel.DataId +
                " choice=done hand=" + Cards(before) + " rng=" + beforeRng);
            if (!ReplayManager.TryClickButton(selection.GetCancelButton()))
            {
                error = "Judge's Scales native Done button was unavailable.";
                yield break;
            }

            yield return new WaitUntil(() => !hand.IsHandSelectionActive());
            CardState[] after = cards.GetHand().ToArray();
            string afterRng = Rng();
            if (!before.SequenceEqual(after) || beforeRng != afterRng)
                error = "Judge's Scales deployment Done changed the native hand or gameplay RNG.";
            log.LogInfo("ROLLING-JUDGES-SCALES-DONE turn=0 discarded=0" +
                " hand=" + Cards(after) + " rng=" + afterRng + " equivalent=" + (error == null));
            running = false;
        }

        private static string Cards(CardState[] hand) => string.Join(",", hand.Select(card => card.GetCardDataID()));
        private static string Rng() => string.Join("/", new[] { RngId.CardDraw, RngId.Battle }.Select(id =>
            id + ":" + string.Join(",", RngCalibration.Words(RandomManager.GetState(id)))));
    }
}
#endif
