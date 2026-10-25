using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static partial class OfflineBattleSearch
{
    // Export a verified root plan as ordinary frozen native inputs. This does
    // not upgrade model evidence to native acceptance or default-budget advice.
    internal static void FreezeRootPlan(string input, string output)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        OfflineRootSearch.Verify(input);
        using var source = FixtureDocument.Read(input);
        var fixture = source.RootElement;
        var plan = fixture.GetProperty("Plan");
        Require(plan.GetProperty("CompletedHorizon").GetBoolean() && !plan.GetProperty("UsedShorterHorizon").GetBoolean(),
            "Native whole-battle export requires a complete terminal root plan.");
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState current = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        var decisions = new List<Decision>();
        foreach (var value in plan.GetProperty("Steps").EnumerateArray())
        {
            var step = new Step { Action = value.GetProperty("Action").Deserialize<PlayCardAction>(),
                IsEndTurn = value.GetProperty("IsEndTurn").GetBoolean() };
            Require(!Terminal(outcome) && step.IsEndTurn == (step.Action == null), "Invalid terminal root plan input.");
            BattleTurnState next = Apply(current, step.Action, step.IsEndTurn, out outcome);
            decisions.Add(new Decision { Root = current, Action = step.Action, IsEndTurn = step.IsEndTurn,
                Expected = next, Outcome = outcome, PlanSteps = new[] { step }, Planned = next, PlannedOutcome = outcome,
                Score = BattlePlanner.Evaluate(next, outcome), Nodes = 0, Generation = decisions.Count + 1 });
            current = next;
        }
        Require(Terminal(outcome), "Root plan did not reach its terminal outcome.");
        using (var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelPrediction",
            NativeValidationRequired = true, SourceArchive = fixture.GetProperty("SourceArchive").GetString(),
            SourceSHA256 = fixture.GetProperty("SourceSHA256").GetString(), RootPlanArchive = Path.GetFileName(input),
            RootPlanSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Strategy = "verified-root-plan", Root = root, Final = current, Outcome = outcome, RollingDecisions = decisions }))
        using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write)) document.Write(stream);
        Verify(output);
    }
}
