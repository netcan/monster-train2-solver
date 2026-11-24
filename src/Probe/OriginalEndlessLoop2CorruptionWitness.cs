#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEndlessLoop2CorruptionWitness
    {
        private const string PlanVariable = "MT2_PROBE_ENDLESS_LOOP2_CORRUPTION_PLAN";
        internal static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(PlanVariable));
        private static readonly List<ReplayStep> Steps = new();
        private static BattleTurnState? root;
        private static int cursor;

        internal static bool TryChoose(FullBattleTrace trace, out PlayCardAction? action, out IReadOnlyList<RelentlessDecision>? choices)
        {
            if (cursor > 0 && Steps[cursor - 1].Input.IsEndTurn) trace.CompleteEndTurn(RoomOutcome.Exchanged);
            BattleTurnState actual = trace.CaptureDecision();
            if (root == null) Prepare(actual);
            if (cursor > 0)
            {
                ReplayStep settled = Steps[cursor - 1];
                settled.Actual = actual;
                JToken observed = BattleTurnProbe.Comparable(actual);
                JToken predicted = BattleTurnProbe.ComparableAtRecordedBoundary(settled.Expected, actual, observed, terminal: false);
                if (!JToken.DeepEquals(observed, predicted))
                {
                    string? difference = UnitPlayModelProbe.FirstDifference(predicted, observed, "$");
                    Write(difference);
                    throw new InvalidOperationException($"Original Endless Loop 2 differs at input {cursor}: {difference}");
                }
            }

            action = null; choices = null;
            if (cursor == Steps.Count)
            {
                ValidateDeath(Steps[cursor - 2].Actual!, actual);
                Write(null);
                return false;
            }
            PlannerReplayScenario.Step next = Steps[cursor++].Input;
            action = next.Action; choices = next.RelentlessDecisions;
            return true;
        }

        private static void Prepare(BattleTurnState actual)
        {
            using var document = FixtureDocument.Read(Environment.GetEnvironmentVariable(PlanVariable)!);
            FixtureValue archive = document.RootElement;
            var frozen = PlannerReplayScenario.Read<BattleTurnState>(archive.GetProperty("Root"));
            FixtureValue plan = archive.GetProperty("Plan");
            var inputs = plan.GetProperty("Steps").EnumerateArray().Select(PlannerReplayScenario.Read<PlannerReplayScenario.Step>).ToList();
            if (archive.GetProperty("EvidenceKind").GetString() != "ModelRootPrediction" || actual.Spawn.Turn != 1 ||
                inputs.Count != 14 || inputs.Count(input => input.IsEndTurn) != 2 ||
                !JToken.DeepEquals(BattleTurnProbe.Comparable(frozen), BattleTurnProbe.Comparable(actual)))
                throw new InvalidOperationException("The frozen Loop 2 plan does not match the original first-wave decision.");
            root = actual;
            foreach (var input in inputs) Add(input);
            var planned = PlannerReplayScenario.Read<BattleTurnState>(plan.GetProperty("State"));
            if (!JToken.DeepEquals(BattleTurnProbe.Comparable(Steps.Last().Expected), BattleTurnProbe.Comparable(planned)))
                throw new InvalidOperationException("The frozen Loop 2 inputs do not reproduce the recorded forecast.");
            Add(new PlannerReplayScenario.Step { Action = new PlayCardAction(3, 1, playerPosition: 0) });
            Add(new PlannerReplayScenario.Step { Action = new PlayCardAction(10, 1, targetUnitId: 8) });
            ValidateDeath(Steps[Steps.Count - 2].Expected, Steps.Last().Expected);

            void Add(PlannerReplayScenario.Step input)
            {
                BattleTurnState before = Steps.LastOrDefault()?.Expected ?? actual;
                if (input.IsEndTurn != (input.Action == null)) throw new InvalidOperationException("Invalid frozen Loop 2 input.");
                BattleTurnState? next;
                if (input.IsEndTurn)
                {
                    BattleTurnResult ended = BattleTurnModel.EndTurn(before, input.RelentlessDecisions);
                    if (ended.Outcome != RoomOutcome.Exchanged) throw new InvalidOperationException(ended.UnsupportedReason ?? "The Loop 2 turn did not settle.");
                    next = ended.State;
                }
                else
                {
                    BattleActionResult played = BattleActionModel.PlayCard(before, input.Action!);
                    if (played.Outcome != RoomOutcome.Exchanged) throw new InvalidOperationException(played.Reason ?? "The Loop 2 card did not settle.");
                    next = played.State;
                }
                Steps.Add(new ReplayStep(input, next ?? throw new InvalidOperationException("Missing Loop 2 successor.")));
            }
        }

        private static void ValidateDeath(BattleTurnState before, BattleTurnState after)
        {
            var floor = before.Spawn.Train.Rooms[1].Units;
            var settled = after.Spawn.Train.Rooms[1].Units;
            if (before.Spawn.Turn != 3 || after.Spawn.Turn != 3 ||
                !floor.Any(unit => unit is { Id: 8, AssetKey: "HordeT2_DeathAddsCorruption", Health: 3 }) ||
                after.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 8) ||
                floor.Single(unit => unit.Id == 18).Status("corruption") != null ||
                settled.Single(unit => unit.Id == 18).Status("corruption")?.Stacks != 15)
                throw new InvalidOperationException("Expected the original Torch kill and its 15-stack Corruption callback.");
        }

        private static void Write(string? difference)
        {
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalEndlessLoop2PaidHordeDeath", GameVersion = Application.version,
                GameModuleMvid = typeof(CharacterState).Module.ModuleVersionId, Root = root, Steps, Difference = difference
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "endless-loop2-paid-horde-death.mt2f"));
            archive.Write(output);
        }

        private sealed class ReplayStep(PlannerReplayScenario.Step input, BattleTurnState expected)
        {
            public PlannerReplayScenario.Step Input { get; } = input;
            public BattleTurnState Expected { get; } = expected;
            public BattleTurnState? Actual { get; set; }
        }
    }
}
#endif
