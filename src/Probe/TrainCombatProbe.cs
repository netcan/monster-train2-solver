using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class TrainCombatProbe
    {
        private readonly ManualLogSource log;
        private readonly FullBattleTrace trace;
        private readonly List<Record> records = new List<Record>();
        private static TrainCombatProbe? active;
        internal IReadOnlyList<Record> Records => records;
        internal int Mismatches => records.Count(record => record.Difference != null);
        internal int Unsupported => records.Count(record => !record.Predicted.Supported);

        internal TrainCombatProbe(ManualLogSource log, FullBattleTrace trace)
        { this.log = log; this.trace = trace; active = this; }

        internal UnitCopyCatalog? CaptureTriggeredCopyCatalog(IEnumerable<RoomCombatState> rooms) =>
            rooms.SelectMany(room => room.Units).Any(unit => unit.AssetKey == TriggeredSummonModel.UnrulyBoss &&
                unit.Triggers.Any(trigger => trigger.Kind == "PostCombat" &&
                    trigger.Effects.Any(effect => effect.Action?.Type == "CopyUnits")))
                ? trace.CaptureDecision(includeOuterBossForBump: true).PlayRules?.UnitCopyCatalog
                : null;

        internal TrainCombatState Capture(bool captureContext = true, bool includeOuterBossForBump = false)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            RoomManager rooms = managers.GetRoomManager()!;
            var states = new List<RoomCombatState>();
            var movement = new List<EnemyMovement>();
            // Decision snapshots have one global context. Discover every floor's
            // units before freezing its card/unit registries and identity counters.
            bool sharedContext = captureContext && trace.CanonicalDecisionCapture;
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                RoomState room = rooms.GetRoom(index);
                RoomCombatState state = trace.Capture(room, captureContext && !sharedContext,
                    includeOuterBossForBump, observe: !sharedContext);
                states.Add(state);
                var enemies = new List<CharacterState>();
                room.AddCharactersToList(enemies, Team.Type.Heroes);
                enemies = enemies.Where(unit => unit.IsAlive && !unit.IsDestroyed).ToList();
                foreach (CharacterState enemy in enemies)
                {
                    movement.Add(new EnemyMovement(trace.UnitId(enemy), 1,
                        enemy.GetAscendsTrainAutomatically(), enemy.GetLoopsBetweenTrainFloors(), enemy.IsCompanionBoss()));
                }
            }
            CombatContext? context = captureContext ? trace.CaptureContext() : null;
            if (sharedContext)
                for (int index = 0; index < states.Count; index++)
                {
                    RoomCombatState room = states[index];
                    states[index] = new RoomCombatState(room.RoomIndex, room.Deployment, room.Units,
                        room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds);
                    trace.ObserveRoom(states[index], includeOuterBossForBump);
                }
            return new TrainCombatState(states, movement,
                managers.GetCombatManager()!.NumSpawnPointsPerFloor(Team.Type.Heroes), context);
        }

        private IEnumerator Wrap(IEnumerator native, string kind)
        {
            // Component comparisons need a stable destruction boundary. Unity
            // can defer OnDestroy from ResolveCombat until a frame yielded by
            // DoTurn, invalidating old weak references between its two captures.
            // Under the explicit isolation protocol, await only objects already
            // scheduled for destruction. Never destroy actors or run removals here.
            CharacterState[] pendingDestruction = kind == "Ascend" && DeathDissolveSettlement.Enabled
                ? trace.KnownUnits.Where(unit => unit != null &&
                    (CharacterState.DestroyedState)AccessTools.Field(typeof(CharacterState), "destroyedState").GetValue(unit) == CharacterState.DestroyedState.Destroyed &&
                    !ReferenceEquals(unit.GetWeakReference().Ref, null)).ToArray()
                : Array.Empty<CharacterState>();
            int settlementFrames = 0;
            while (pendingDestruction.Any(unit => !ReferenceEquals(unit.GetWeakReference().Ref, null)))
            {
                if (++settlementFrames > 2)
                {
                    var error = new InvalidOperationException("Deferred Unity destruction did not finish before Ascend capture.");
                    trace.CaptureFailure(error);
                    throw error;
                }
                yield return null;
            }
            Record? record = null;
            try
            {
                AllGameManagers? managers = AllGameManagers.Instance;
                if (managers != null && !managers.GetSaveManager().PreviewMode && trace.NativeWon == null)
                {
                    TrainCombatState before = Capture();
                    var roomRules = kind == "Combat" && (RelicTurnEndBumpModel.Present(before.Context) ||
                        before.Rooms.SelectMany(room => room.Units).Any(unit =>
                            unit.Triggers.Any(trigger => RoomCombatModel.IsRailforgedAttackerKillBump(unit, trigger))))
                        ? BattleActionProbe.CaptureRoomRules() : null;
                    UnitCopyCatalog? unitCopyCatalog = kind == "Combat" ? CaptureTriggeredCopyCatalog(before.Rooms) : null;
                    record = new Record
                    {
                        Index = records.Count, Turn = managers.GetCombatManager()!.GetTurnCount(),
#if DEVELOPMENT_PROBE
                        TerminalUiStatisticsStart = CardStatisticRefreshTraceProbe.Sequence(trace),
#endif
                        Kind = kind, Before = before,
                        SettledDestructionIds = pendingDestruction.Select(trace.UnitId).OrderBy(id => id).ToArray(),
                        SettlementFrames = settlementFrames,
                        RelentlessDecisions = kind == "Combat" ? RelentlessBattleScenario.CurrentPlan : null,
                        RoomRules = roomRules,
                        Predicted = kind == "Combat"
                            ? TrainCombatModel.ResolveCombat(before, RelentlessBattleScenario.CurrentPlan, roomRules, unitCopyCatalog)
                            : TrainCombatModel.Ascend(before)
                    };
                    records.Add(record);
                }
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) Complete(record);
            }
        }

        internal void CompletePending()
        { foreach (Record record in records.Where(record => record.Actual == null).ToArray()) Complete(record); }

        private void Complete(Record record)
        {
            if (record.Actual != null) return;
            try
            {
                record.Actual = trace.ObserveStoppingStage(() => Capture());
#if DEVELOPMENT_PROBE
                record.TerminalUiStatisticsRefresh = CardStatisticRefreshTraceProbe.Between(trace,
                    record.TerminalUiStatisticsStart, record.Before.Context, record.Actual.Context);
#endif
                if (record.Predicted.Supported)
                {
                    JToken expected = Comparable(record.Predicted.State!, record.Kind);
                    JToken actual = Comparable(record.Actual, record.Kind);
                    record.StoredCardCacheDiffers = !JToken.DeepEquals(
                        JToken.FromObject(record.Predicted.State!.Context?.Statistics?.StoredCards ?? Array.Empty<int>()),
                        JToken.FromObject(record.Actual.Context?.Statistics?.StoredCards ?? Array.Empty<int>()));
                    record.Difference = JToken.DeepEquals(expected, actual) ? null : "Train room unit states differ";
                    log.LogInfo("TRAIN-MODEL-" + (record.Difference == null ? "MATCH" : "MISMATCH") +
                        " index=" + record.Index + " kind=" + record.Kind + " turn=" + record.Turn);
                }
                else log.LogInfo("TRAIN-MODEL-UNSUPPORTED index=" + record.Index + " reason=" +
                    record.Predicted.UnsupportedReason);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }

        private static JToken Comparable(TrainCombatState state, string kind)
        {
            JObject comparable = JObject.FromObject(new
            {
                Rooms = state.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
                Context = BattleTurnProbe.ComparableContext(state.Context)
            });
            // CardUI.LateUpdate queries scaling text between native coroutine
            // yields, refreshing this membership cache without a battle event.
            // Ascend can also finish the next draw before its wrapper returns.
            // Keep the raw snapshots and cache difference; stable decision/turn
            // comparisons still include this field and every numeric counter.
            if (comparable["Context"]?["Statistics"] is JObject statistics)
                statistics.Remove("StoredCards");
            return comparable;
        }

        internal sealed class Record
        {
            public int Index { get; set; }
            public int Turn { get; set; }
            public string Kind { get; set; } = "";
            public int[] SettledDestructionIds { get; set; } = Array.Empty<int>();
            public int SettlementFrames { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public IReadOnlyList<RelentlessDecision>? RelentlessDecisions { get; set; }
            public bool ShouldSerializeRelentlessDecisions() => RelentlessDecisions != null;
            public IReadOnlyList<RoomPlayRule>? RoomRules { get; set; }
            public bool ShouldSerializeRoomRules() => RoomRules != null;
            public TrainCombatResult Predicted { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
#if DEVELOPMENT_PROBE
            internal int TerminalUiStatisticsStart { get; set; }
            public CardStatisticRefreshTraceProbe.Boundary? TerminalUiStatisticsRefresh { get; set; }
            public bool ShouldSerializeTerminalUiStatisticsRefresh() => TerminalUiStatisticsRefresh != null;
#endif
            public string? Difference { get; set; }
            public bool StoredCardCacheDiffers { get; set; }
        }

        [HarmonyPatch(typeof(CombatManager), "ResolveCombat")]
        private static class CombatPatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (active != null) __result = active.Wrap(__result, "Combat"); }
        }

        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.DoTurn))]
        private static class AscendPatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (active != null) __result = active.Wrap(__result, "Ascend"); }
        }
    }
}
