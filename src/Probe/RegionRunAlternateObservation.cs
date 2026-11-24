#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class RegionRunAlternateObservation
    {
        private static RegionRunAlternateObservation? active;
        private readonly FixtureValue source;
        private readonly string sourceSha256;
        private readonly int actionIndex;
        private readonly int turn;
        private readonly int endTurnCount;
        private bool prefixVerified;
        private int prefixEndTurns;
        private readonly List<(int Stages, int TrainPhases, int Spawns, int CardCycles)> endTurnStarts = new();

        internal static RegionRunAlternateObservation? Active =>
            Environment.GetEnvironmentVariable("MT2_PROBE_REGION_ALTERNATE_SOURCE") is { Length: > 0 } path
                ? active ??= new RegionRunAlternateObservation(path) : null;

        internal PlayCardAction Action { get; }

        private RegionRunAlternateObservation(string path)
        {
            endTurnCount = Environment.GetEnvironmentVariable("MT2_PROBE_REGION_ALTERNATE_END_TURN") switch
            {
                null or "" or "0" => 0,
                "1" => 1,
                "2" => 2,
                _ => throw new InvalidOperationException("Expected zero, one or two RegionRun alternate EndTurns.")
            };
            string[] values = (Environment.GetEnvironmentVariable("MT2_PROBE_REGION_ALTERNATE_ACTION") ?? "").Split(',');
            if (values.Length != 4 || values.Any(value => !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _)))
                throw new InvalidOperationException("Expected the RegionRun alternate decision, card, room and target.");
            int[] ids = values.Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
            actionIndex = ids[0];
            Action = new PlayCardAction(ids[1], ids[2], playerPosition: -1, targetUnitId: ids[3]);
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            sourceSha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            stream.Position = 0;
            using var document = FixtureDocument.Read(stream);
            source = document.RootElement;
            FixtureValue run = source.GetProperty("NativeRunInput");
            if (source.GetProperty("GameVersion").GetString() != Application.version ||
                source.GetProperty("GameModuleMvid").GetString() != typeof(CardState).Module.ModuleVersionId.ToString() ||
                !source.GetProperty("CampaignBattlePolicy").GetBoolean() ||
                !source.GetProperty("TerminalEffectsSettled").GetBoolean() ||
                source.GetProperty("CaptureFailures").GetInt32() != 0 || source.GetProperty("Pending").GetInt32() != 0 ||
                run.GetProperty("RunType").GetString() != "RegionRun" || run.GetProperty("WasDevCheatUsed").GetBoolean())
                throw new InvalidOperationException("Expected a complete original RegionRun policy archive from the running module.");
            var actions = source.GetProperty("Actions");
            if (actionIndex >= actions.GetArrayLength() || ids[1] <= 0 || ids[2] > 2 || ids[3] <= 0)
                throw new InvalidOperationException("The requested alternate is outside the recorded paid-card boundary.");
            FixtureValue boundary = actions[actionIndex];
            if (SameAction(Action, boundary.GetProperty("Action")))
                throw new InvalidOperationException("The requested alternate is the recorded card input.");
            turn = Turn(boundary);
        }

        internal bool IsBoundary(FullBattleTrace trace, int currentTurn)
        {
            if (trace.ObservedActionCount < actionIndex) return false;
            if (trace.ObservedActionCount != actionIndex || currentTurn != turn)
                throw new InvalidOperationException("The native policy missed the requested RegionRun input boundary.");
            if (!prefixVerified)
            {
                VerifyPrefix(trace);
                prefixVerified = true;
            }
            return true;
        }

        internal bool ReachedAlternate(FullBattleTrace trace) =>
            prefixVerified && trace.ObservedActionCount == actionIndex + 1;

        internal bool RequestEndTurn(FullBattleTrace trace)
        {
            if (endTurnStarts.Count == endTurnCount || SettledAction(trace) == null) return false;
            if (trace.NativeWon.HasValue || trace.ObservedTurns.Count != prefixEndTurns + endTurnStarts.Count)
                throw new InvalidOperationException("The paid RegionRun alternate did not leave a native EndTurn available.");
            endTurnStarts.Add(trace.ObservedPhaseCounts);
            return true;
        }

        private BattleActionProbe.Record? SettledAction(FullBattleTrace trace)
        {
            if (!ReachedAlternate(trace) ||
                trace.PendingActionIndex.HasValue || trace.PendingTurnIndex.HasValue)
                return null;
            BattleActionProbe.Record row = trace.ObservedActions[actionIndex];
            if (!JToken.DeepEquals(JToken.FromObject(row.Action), JToken.FromObject(Action)) ||
                row.Actual == null || trace.CaptureFailures != 0 || trace.Pending != 0)
                throw new InvalidOperationException("The native RegionRun alternate did not settle as the requested action.");
            return row;
        }

        internal bool WriteIfSettled(FullBattleTrace trace)
        {
            BattleActionProbe.Record? row = SettledAction(trace);
            if (row == null || endTurnStarts.Count < endTurnCount) return false;
            if (trace.ObservedTurns.Count != prefixEndTurns + endTurnCount)
                throw new InvalidOperationException("The native RegionRun alternate recorded an unexpected EndTurn count.");
            var endTurn = CaptureEndTurn(0);
            var secondEndTurn = CaptureEndTurn(1);
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "NativeRegionRunAlternate", NativeCompleted = true,
                GameVersion = Application.version, GameModuleMvid = typeof(CardState).Module.ModuleVersionId,
                SourceSHA256 = sourceSha256, DecisionIndex = actionIndex, PrefixPlays = actionIndex,
                PrefixEndTurns = prefixEndTurns, NativeRunInput = trace.ObservedRunInput,
                ReplacedAction = PlannerReplayScenario.Read<PlayCardAction>(source.GetProperty("Actions")[actionIndex].GetProperty("Action")),
                row.Action, row.Before, row.Actual, row.ActualOutcome,
                EndTurn = endTurn.Decision, EndTurnPhases = endTurn.Phases,
                SecondEndTurn = secondEndTurn.Decision, SecondEndTurnPhases = secondEndTurn.Phases,
                trace.CaptureFailures, trace.Pending
            });
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "region-run-alternate.mt2f");
            using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            document.Write(output);
            return true;

            (object? Decision, object? Phases) CaptureEndTurn(int index)
            {
                if (index >= endTurnCount) return (null, null);
                var observed = trace.ObservedTurns[prefixEndTurns + index];
                if (observed.Actual == null || observed.Before.Spawn.Turn != turn + index)
                    throw new InvalidOperationException("The native RegionRun alternate EndTurn did not settle at the requested turn.");
                var end = index + 1 < endTurnStarts.Count ? endTurnStarts[index + 1] : trace.ObservedPhaseCounts;
                return (new { observed.Before, observed.Actual, observed.ActualOutcome, observed.RelentlessDecisions },
                    trace.CaptureObservedPhases(endTurnStarts[index], end));
            }
        }

        private void VerifyPrefix(FullBattleTrace trace)
        {
            if (trace.ObservedRunInput == null || !JToken.DeepEquals(JToken.FromObject(trace.ObservedRunInput),
                PlannerReplayScenario.Read<JObject>(source.GetProperty("NativeRunInput"))) ||
                trace.PendingActionIndex.HasValue || trace.PendingTurnIndex.HasValue)
                throw new InvalidOperationException("The native RegionRun lineage or settlement differs from the source.");
            FixtureValue actions = source.GetProperty("Actions");
            for (int index = 0; index < actionIndex; index++)
            {
                BattleActionProbe.Record observed = trace.ObservedActions[index];
                FixtureValue expected = actions[index];
                if (observed.Actual == null || observed.Before.Spawn.Turn != Turn(expected) ||
                    observed.ActualOutcome != (RoomOutcome)expected.GetProperty("ActualOutcome").GetInt32() ||
                    !SameAction(observed.Action, expected.GetProperty("Action")))
                    throw new InvalidOperationException("The original RegionRun card prefix differs at action " + index);
            }
            FixtureValue[] turns = source.GetProperty("Turns").EnumerateArray().Where(row => Turn(row) < turn).ToArray();
            if (turns.Length != trace.ObservedTurns.Count)
                throw new InvalidOperationException("The native RegionRun EndTurn prefix length differs from the source.");
            for (int index = 0; index < turns.Length; index++)
            {
                BattleTurnProbe.Record observed = trace.ObservedTurns[index];
                FixtureValue expected = turns[index];
                if (observed.Actual == null || observed.Before.Spawn.Turn != Turn(expected) ||
                    observed.ActualOutcome != (RoomOutcome)expected.GetProperty("ActualOutcome").GetInt32() ||
                    !PlannerReplayScenario.SameChoices(observed.RelentlessDecisions, PlannerReplayScenario.ReadChoices(expected)))
                    throw new InvalidOperationException("The original RegionRun EndTurn prefix differs at index " + index);
            }
            prefixEndTurns = turns.Length;
        }

        private static bool SameAction(PlayCardAction action, FixtureValue recorded) =>
            JToken.DeepEquals(JToken.FromObject(action), JToken.FromObject(PlannerReplayScenario.Read<PlayCardAction>(recorded)));

        private static int Turn(FixtureValue row) => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32();
    }
}
#endif
