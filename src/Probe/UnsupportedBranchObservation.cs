#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Development-only driver: observe a rejected candidate without inventing
    // its expected successor or requiring a full terminal battle.
    internal static class UnsupportedBranchObservation
    {
        internal static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_UNSUPPORTED_BRANCH"));
        internal static bool Completed { get; private set; }
        private static FixtureValue request;
        private static FixtureValue[]? prefix;
        private static BattleTurnState expected = null!;
        private static readonly List<Row> rows = new List<Row>();
        private static bool waiting;
        private static string Comparable(BattleTurnState state) => BattleTurnProbe.Comparable(state).ToString(Newtonsoft.Json.Formatting.None);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private static void Initialize()
        {
            if (prefix != null) return;
            using var document = FixtureDocument.Read(Environment.GetEnvironmentVariable("MT2_PROBE_UNSUPPORTED_BRANCH")!);
            request = document.RootElement;
            Require(request.GetProperty("Schema").GetInt32() == 1 &&
                request.GetProperty("EvidenceKind").GetString() == "ModelUnsupportedBranch", "Expected a rejected model branch.");
            prefix = request.GetProperty("Prefix").EnumerateArray().ToArray();
            Require(prefix.Length <= 256, "Unsupported branch prefix exceeds the input bound.");
            expected = Read<BattleTurnState>(request.GetProperty("Root"));
            Require(expected.Spawn.Turn == 0, "Branch observation must start at native deployment.");
        }

        internal static void Observe(BattleTurnState actual)
        {
            Initialize();
            if (!waiting) return;
            Row row = rows.Last();
            row.Actual = actual; waiting = false;
            if (!row.Boundary)
            {
                Require(Comparable(expected) == Comparable(actual), "Native branch prefix successor differs at " + rows.Count);
                return;
            }
            using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "NativeUnsupportedBranchObservation",
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId.ToString(),
                Request = Read<JObject>(request), NativeCompleted = true, Rows = rows,
                PlayerAdvisor = PlayerAdvisorProbe.Evidence });
            using var stream = new FileStream(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "unsupported-branch-native.mt2f"), FileMode.CreateNew, FileAccess.Write);
            document.Write(stream);
            Completed = true;
        }

        internal static PlayCardAction? Choose(BattleTurnState root, ManualLogSource log)
        {
            Initialize();
            Require(!waiting && !Completed, "Branch observation consumed an unsettled input.");
            bool boundary = rows.Count == prefix!.Length;
            Require(rows.Count <= prefix.Length, "Branch input overflow.");
            Require(Comparable(root) == Comparable(expected), "Native branch prefix root differs at " + rows.Count);
            if (boundary) Require(Comparable(root) == Comparable(Read<BattleTurnState>(request.GetProperty("FailedRoot"))),
                "Native branch did not reach the recorded failure root.");
            FixtureValue value = boundary ? request : prefix[rows.Count];
            PlayCardAction? action = Read<PlayCardAction?>(value.GetProperty("Action"));
            bool end = !boundary && value.GetProperty("IsEndTurn").GetBoolean();
            Require(end == (action == null), "Invalid branch input.");
            BattleTurnState? predicted;
            string? reason;
            RoomOutcome outcome;
            if (end)
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(root);
                predicted = result.State; reason = result.UnsupportedReason; outcome = result.Outcome;
            }
            else
            {
                BattleActionResult result = BattleActionModel.PlayCard(root, action!);
                predicted = result.State; reason = result.Reason; outcome = result.Outcome;
                Require(result.Rejection != ActionRejection.Illegal, "Native diagnostic action is illegal: " + reason);
            }
            Require(boundary || predicted != null && outcome == RoomOutcome.Exchanged,
                "Branch prefix is unsupported or terminal: " + reason);
            rows.Add(new Row { Before = root, Action = action, IsEndTurn = end, Boundary = boundary,
                Expected = predicted, UnsupportedReason = reason, Outcome = outcome });
            if (predicted != null) expected = predicted;
            waiting = true;
            log.LogInfo("UNSUPPORTED-BRANCH-INPUT index=" + rows.Count + " boundary=" + boundary + " reason=" + reason);
            return PlayerAdvisorProbe.NativeAction(action);
        }

        private static T Read<T>(FixtureValue value) => PlannerReplayScenario.Read<T>(value);
        private sealed class Row
        {
            public BattleTurnState Before { get; set; } = null!;
            public PlayCardAction? Action { get; set; }
            public bool IsEndTurn { get; set; }
            public bool Boundary { get; set; }
            public BattleTurnState? Expected { get; set; }
            public string? UnsupportedReason { get; set; }
            public RoomOutcome Outcome { get; set; }
            public BattleTurnState? Actual { get; set; }
        }
    }
}
#endif
