using System;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal sealed partial class BattleAdvisorOverlay
    {
        private readonly AdviceDiagnosticJournal? journal;
        private long publishedGeneration = -1;
        private bool diagnosticErrorReported;
        private string SearchScopeLabel => "已知牌序 · " + AdvisorForecastPresentation.SearchScope(wholeBattle, horizon) +
            " · " + toggleKey + " 显示/隐藏";
        internal long DiagnosticCardInputs => journal?.CardInputs ?? 0;
        private object? DiagnosticHistory => journal == null ? null : new
        {
            Directory = journal.DirectoryPath, journal.Written, journal.Dropped,
            journal.Completed, journal.Error, journal.WriterThreadId
        };

        private bool CanRecordNativeInput => journal != null && !capturing &&
            AllGameManagers.Instance?.GetSaveManager()?.PreviewMode == false && !PreviewRngIsolation.Active;

        private static string ClipDiagnosticText(string value, int limit = 512) =>
            value.Length > limit ? value.Substring(0, limit) : value;

        private AdviceDiagnosticEntry DiagnosticEntry(string kind)
        {
            AllGameManagers managers = AllGameManagers.Instance;
            return new AdviceDiagnosticEntry
            {
                Kind = kind, Battle = manualBattleKey,
                Turn = managers.GetCombatManager().GetTurnCount(), Generation = session.Generation,
                PublishedGeneration = publishedGeneration, Searches = session.SearchCount,
                CurrentPyreHp = managers.GetSaveManager().GetTowerHP(),
                AdviceVisible = panelVisible && displayed != null,
                AdvicePending = captureNeeded || selectionPending || preparation != null,
                FullPlan = showFullPlan
            };
        }

        private void RecordDiagnosticView(string kind)
        {
            if (journal == null || displayed == null) return;
            try
            {
                BattleAdvice advice = displayed;
                FullBattleTrace trace = FullBattleTrace.Active!;
                var cards = trace.KnownCards.Where(card => trace.TryGetCardId(card, out _))
                    .ToDictionary(card => { trace.TryGetCardId(card, out int id); return id; });
                var units = trace.KnownUnits.Where(unit => unit != null && trace.TryGetUnitId(unit, out _))
                    .ToDictionary(unit => { trace.TryGetUnitId(unit, out int id); return id; });
                string? Unit(int id) => units.TryGetValue(id, out CharacterState unit) ? ClipDiagnosticText(unit.GetName()) : null;
                int turn = advice.Root.Spawn.Turn;
                var steps = advice.Plan.Steps.Take(256).Select(step =>
                {
                    PlayCardAction? action = step.Action;
                    var snapshot = new AdviceDiagnosticStep
                    {
                        Turn = turn, EndTurn = step.IsEndTurn, Action = action,
                        Card = action != null && cards.TryGetValue(action.CardInstanceId, out CardState card) ?
                            ClipDiagnosticText(card.GetTitle()) : null,
                        SourceUnit = action == null ? null : Unit(action.ActivatorUnitId),
                        TargetUnit = action == null ? null : Unit(action.TargetUnitId)
                    };
                    if (step.IsEndTurn) turn++;
                    return snapshot;
                }).ToArray();
                int hp = advice.Plan.State.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Where(unit => unit.IsPyre).Select(unit => unit.Health).FirstOrDefault();
                AdviceDiagnosticEntry entry = DiagnosticEntry(kind);
                entry.DisplayText = ClipDiagnosticText(body.text, 32768);
                entry.DisplayTextTruncated = body.text.Length > entry.DisplayText.Length;
                entry.Forecast = new AdviceDiagnosticForecast
                {
                    Outcome = advice.Plan.Outcome.ToString(), Terminal = AdvisorForecastPresentation.IsTerminal(advice.Plan.Outcome),
                    ForecastPyreHp = hp, FutureLoss = Math.Max(0, entry.CurrentPyreHp - hp),
                    TurnsCompleted = advice.Plan.TurnsCompleted, TimedOut = advice.Plan.TimedOut,
                    Source = advice.ReusedSelection ? "floor-browse" : advice.ReusedWinningContinuation ? "verified-continuation" :
                        advice.ReusedContinuation ? "verified-plan" : "search",
                    PlanStepCount = advice.Plan.Steps.Count, Steps = steps
                };
                journal.Record(entry);
            }
            catch (Exception error) { ReportDiagnosticError(error.Message); }
        }

        private void RecordDiagnosticTentative()
        {
            if (journal == null) return;
            try
            {
                AdviceDiagnosticEntry entry = DiagnosticEntry("tentative");
                entry.AdvicePending = true;
                entry.DisplayText = ClipDiagnosticText(body.text, 32768);
                entry.DisplayTextTruncated = body.text.Length > entry.DisplayText.Length;
                journal.Record(entry);
            }
            catch (Exception error) { ReportDiagnosticError(error.Message); }
        }

        private AdviceDiagnosticEntry? DiagnosticCardInput(CardState card, CharacterState selfTarget, SpawnPoint drop,
            bool direct, CharacterState actor, RoomState roomActor, int roomOverride)
        {
            if (!CanRecordNativeInput) return null;
            try
            {
                AllGameManagers managers = AllGameManagers.Instance;
                FullBattleTrace? trace = FullBattleTrace.Active;
                AdviceDiagnosticEntry entry = DiagnosticEntry("card-accepted");
                int handIndex = managers.GetCardManager().GetHand().IndexOf(card);
                int targetRoom = managers.GetRoomManager().IsValidRoomIndex(roomOverride) ? roomOverride :
                    drop?.GetRoomOwner()?.GetRoomIndex() ?? selfTarget?.GetCurrentRoomIndex() ??
                    managers.GetRoomManager().GetSelectedRoom();
                entry.Input = new AdviceDiagnosticInput
                {
                    CardInstanceId = trace != null && trace.TryGetCardId(card, out int cardId) ? cardId : (int?)null,
                    CardDataId = card.GetCardDataID(), Card = ClipDiagnosticText(card.GetTitle()),
                    CardIdentity = ClipDiagnosticText(AdvisorCardPresentation.Describe(card, handIndex, null), 2048),
                    HandIndex = handIndex, TargetRoom = targetRoom, DropSpawnPoint = drop?.GetIndexInRoom() ?? -1,
                    TargetUnitId = trace != null && (drop?.GetCharacterState() ?? selfTarget) is CharacterState target &&
                        trace.TryGetUnitId(target, out int targetId) ? targetId : (int?)null,
                    SourceUnitId = trace != null && actor != null && trace.TryGetUnitId(actor, out int actorId) ? actorId : (int?)null,
                    SourceUnit = actor == null ? null : ClipDiagnosticText(actor.GetName()),
                    SourceRoom = actor != null ? actor.GetCurrentRoomIndex() : roomActor?.GetRoomIndex() ?? -1,
                    FromDirectPlay = direct
                };
                return entry;
            }
            catch (Exception error) { ReportDiagnosticError(error.Message); return null; }
        }

        private void RecordDiagnosticEndTurn()
        {
            if (!CanRecordNativeInput) return;
            try { journal!.Record(DiagnosticEntry("end-turn-accepted")); }
            catch (Exception error) { ReportDiagnosticError(error.Message); }
        }

        private void RecordDiagnosticReset(string reason)
        {
            if (!CanRecordNativeInput) return;
            try
            {
                AdviceDiagnosticEntry entry = DiagnosticEntry("reset"); entry.Reason = reason;
                journal!.Record(entry);
            }
            catch (Exception error) { ReportDiagnosticError(error.Message); }
        }

        private void ReportDiagnosticError(string? failure = null)
        {
            failure ??= journal?.Error;
            if (failure == null || diagnosticErrorReported) return;
            diagnosticErrorReported = true;
            log.LogWarning("POJU-HISTORY unavailable: " + failure);
        }
    }
}
