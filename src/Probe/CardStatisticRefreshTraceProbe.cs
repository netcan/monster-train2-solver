#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Opt-in, read-only diagnostic for lazy zero-valued native statistic entries.
    [HarmonyPatch(typeof(CardStatistics), "UpdateDeckStats")]
    internal static class CardStatisticRefreshTraceProbe
    {
        private static readonly bool Enabled = Environment.GetEnvironmentVariable("MT2_PROBE_TRACE_CARD_STATS") == "1";
        private static readonly ConditionalWeakTable<FullBattleTrace, List<Refresh>> Observed = new ConditionalWeakTable<FullBattleTrace, List<Refresh>>();
        private static readonly List<EquipmentObservation> EquipmentObservations = new List<EquipmentObservation>();
        private static CardState? updatingText;

        internal sealed class Refresh : CardStatisticRefreshSample
        {
            internal EquipmentObservation? Equipment { get; set; }
        }

        internal sealed class EquipmentObservation : IDisposable
        {
            internal readonly FullBattleTrace Trace;
            internal readonly CardState Card;
            internal readonly List<Refresh> Events = new List<Refresh>();

            internal EquipmentObservation(FullBattleTrace trace, CardState card)
            { Trace = trace; Card = card; }

            public void Dispose() => EquipmentObservations.Remove(this);
        }

        internal static EquipmentObservation? ObserveEquipment(FullBattleTrace trace, RoomCombatState before, int hostId, int cardId, CardState card)
        {
            if (!EquipmentStatisticRefreshComparison.CanObserve(before, hostId, cardId)) return null;
            var observation = new EquipmentObservation(trace, card);
            EquipmentObservations.Add(observation);
            return observation;
        }

        internal sealed class Boundary
        {
            public int StartSequence { get; }
            public int EndSequence { get; }
            public Refresh Event { get; }

            internal Boundary(int startSequence, int endSequence, Refresh refresh)
            { StartSequence = startSequence; EndSequence = endSequence; Event = refresh; }
        }

        internal static int Sequence(FullBattleTrace trace) =>
            Observed.TryGetValue(trace, out var records) ? records.Count : 0;

        internal static Boundary? Between(FullBattleTrace trace, int start, CombatContext? before, CombatContext? actual)
        {
            if (before?.KillCamActivated != false || actual?.KillCamActivated != true || actual.CardInstances?.Count != 0 ||
                !Observed.TryGetValue(trace, out var records) || records.Count != start + 1)
                return null;
            return new Boundary(start, records.Count, records[start]);
        }

        [HarmonyPatch(typeof(HandUI), "UpdateCardUIAtEndOfFrame")]
        private static class HandTextPatch
        {
            private static void Prefix(CardState cardState, bool updateText, out CardState? __state)
            {
                __state = updatingText;
                if (Enabled) updatingText = updateText ? cardState : null;
            }

            private static void Finalizer(CardState? __state) => updatingText = __state;
        }

        private static void Prefix(CardStatistics __instance, out Refresh? __state)
        {
            __state = null;
            if (!Enabled && EquipmentObservations.Count == 0) return;
            FullBattleTrace? trace = FullBattleTrace.Active;
            bool terminalText = Enabled && trace != null && trace.NativeWon == null && updatingText != null;
            EquipmentObservation? equipment = EquipmentObservations.LastOrDefault(item => ReferenceEquals(item.Trace, trace));
            if (!terminalText && !(Enabled && UnsupportedBranchObservation.Enabled) && equipment == null) return;
            AllGameManagers? managers = AllGameManagers.Instance;
            var stored = (Dictionary<CardState, CardStatsEntry>)AccessTools.Field(typeof(CardStatistics), "deckStats").GetValue(__instance);
            var owned = managers?.GetCardManager()?.GetAllCards(new List<CardState>());
            var missing = owned?.Where(card => !stored.ContainsKey(card)).ToArray();
            var current = owned?.Count > 0 ? owned : managers?.GetSaveManager()?.GetDeckState();
            var removed = current == null ? Array.Empty<CardState>() : stored.Keys.Except(current).ToArray();
            if (missing == null || missing.Length == 0 && removed.Length == 0) return;
            string[] caller = new StackTrace(1, false).GetFrames().Take(12)
                .Select(frame => frame.GetMethod()?.DeclaringType?.FullName + "." + frame.GetMethod()?.Name)
                .Select(name => name == "HandUI.DMD<HandUI::UpdateCardUIAtEndOfFrame>" ? "HandUI.UpdateCardUIAtEndOfFrame" : name).ToArray();
            UnityEngine.Debug.Log("CARD-STAT-REFRESH missing=" + string.Join(",", missing.Select(card => card.GetCardDataID())) +
                " removed=" + string.Join(",", removed.Select(card => card.GetCardDataID())) + " caller=" + string.Join(">", caller));
            if (equipment != null && current != null)
            {
                __state = Capture(equipment.Trace, equipment.Card, stored.Keys, current, caller, equipment.Events.Count + 1);
                __state.Equipment = equipment;
                return;
            }
            if (!terminalText || owned?.Count != 0 || removed.Length == 0 || current == null)
                return;
            CombatManager combat = managers!.GetCombatManager()!;
            if (!(bool)AccessTools.Field(typeof(CombatManager), "isKillCamActivated").GetValue(combat) ||
                !caller.Contains("CardTraitScalingAddDamage.GetCurrentEffectText") ||
                !caller.Contains("HandUI.UpdateCardUIAtEndOfFrame") || !caller.Contains("CardUI.LateUpdate"))
                return;
            __state = Capture(trace!, updatingText!, stored.Keys, current, caller, Sequence(trace!) + 1);
        }

        private static Refresh Capture(FullBattleTrace trace, CardState card, IEnumerable<CardState> stored,
            IEnumerable<CardState> owned, string[] caller, int sequence)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            CombatManager combat = managers.GetCombatManager()!;
            return new Refresh
            {
                Sequence = sequence,
                Turn = combat.GetTurnCount(),
                Frame = UnityEngine.Time.frameCount,
                Phase = combat.GetCombatPhase().ToString(),
                NativePreviewMode = managers.GetSaveManager().PreviewMode,
                CardId = trace.CardId(card),
                CardDataId = card.GetCardDataID(),
                BeforeStoredCards = Ids(trace, stored),
                DeckCards = Ids(trace, owned),
                Caller = caller
            };
        }

        private static void Postfix(CardStatistics __instance, Refresh? __state)
        {
            if (__state == null || FullBattleTrace.Active is not FullBattleTrace trace) return;
            var stored = (Dictionary<CardState, CardStatsEntry>)AccessTools.Field(typeof(CardStatistics), "deckStats").GetValue(__instance);
            __state.AfterStoredCards = Ids(trace, stored.Keys);
            if (__state.Equipment is EquipmentObservation equipment)
            {
                equipment.Events.Add(__state);
                return;
            }
            if (!__state.AfterStoredCards.SequenceEqual(__state.DeckCards))
            {
                trace.CaptureFailure(new InvalidOperationException("Terminal card text did not refresh statistics to the permanent deck."));
                return;
            }
            Observed.GetOrCreateValue(trace).Add(__state);
        }

        private static int[] Ids(FullBattleTrace trace, IEnumerable<CardState> cards) =>
            cards.Select(trace.CardId).Distinct().OrderBy(id => id).ToArray();
    }
}
#endif
