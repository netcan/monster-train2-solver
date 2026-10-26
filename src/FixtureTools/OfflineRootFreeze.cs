using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static partial class OfflineBattleSearch
{
    // Preserve a verified, nonterminal forecast exactly, then finish it with
    // the existing policy. Completion is new model evidence, never acceptance.
    internal static void CompleteRootPlan(string nativeInput, string prediction, string output, int floor, bool strength)
    {
        Require(floor is >= 0 and <= 2, "Expected a deployment room in0..2.");
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        string unsupportedOutput = output + ".unsupported.mt2f";
        if (File.Exists(unsupportedOutput)) throw new IOException("Output already exists: " + unsupportedOutput);
        OfflineRootSearch.Verify(prediction);
        using var native = FixtureDocument.Read(nativeInput);
        RequireNativeInput(native.RootElement);
        using var source = FixtureDocument.Read(prediction);
        var fixture = source.RootElement;
        Require(fixture.GetProperty("SourceSHA256").GetString() ==
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(nativeInput))).ToLowerInvariant() &&
            fixture.GetProperty("SourceDecisionIndex").GetInt32() == 0,
            "Completion requires decision zero from this exact native archive.");
        var plan = fixture.GetProperty("Plan");
        Require(!plan.GetProperty("CompletedHorizon").GetBoolean() && plan.GetProperty("UsedShorterHorizon").GetBoolean(),
            "Completion requires a verified partial root plan.");
        using var keys = new BattleStateKeys();
        BattleTurnState root = DeploymentRoot(native.RootElement);
        string immutable = keys.Key(root);
        Require(root.Spawn.Turn == 0 && immutable == fixture.GetProperty("RootKey").GetString(),
            "The forecast changed the native deployment root.");
        BattleTurnState current = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        var steps = new List<Step>();
        foreach (var value in plan.GetProperty("Steps").EnumerateArray())
        {
            var step = new Step { Action = value.GetProperty("Action").Deserialize<PlayCardAction>(),
                IsEndTurn = value.GetProperty("IsEndTurn").GetBoolean() };
            current = Apply(current, step.Action, step.IsEndTurn, out outcome);
            steps.Add(step);
        }
        Require(!Terminal(outcome) && steps[^1].IsEndTurn &&
            keys.Key(current) == keys.Key(plan.GetProperty("State").Deserialize<BattleTurnState>()!),
            "The independently replayed forecast prefix differs.");
        int prefix = steps.Count, played = 0;
        var unsupported = new HashSet<string>(StringComparer.Ordinal);
        while (!Terminal(outcome) && steps.Count < 256)
        {
            SupportedActionCandidate? choice = null;
            if (played < 18)
            {
                choice = strength ? BattleHeuristicPolicy.Choose(current, out var omitted, floor)
                    : BattleTacticalPolicy.Choose(current, out omitted, floor);
                foreach (var attempt in omitted) unsupported.Add(attempt.Reason);
            }
            if (choice != null)
            {
                current = choice.Result.State!; outcome = choice.Result.Outcome;
                steps.Add(new Step { Action = choice.Action }); played++;
            }
            else
            {
                BattleTurnResult ended = BattleTurnModel.EndTurn(current);
                if (!ended.Supported)
                {
                    Require(keys.Key(root) == immutable, "Unsupported completion mutated its native root.");
                    using var boundary = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
                        SourceArchive = Path.GetFileName(nativeInput),
                        SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(nativeInput))).ToLowerInvariant(),
                        RootPlanArchive = Path.GetFileName(prediction),
                        RootPlanSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(prediction))).ToLowerInvariant(),
                        Root = root, Prefix = steps, FailedRoot = current, Action = (PlayCardAction?)null, IsEndTurn = true,
                        Reason = ended.UnsupportedReason, NativeValidationRequired = true });
                    using (var stream = new FileStream(unsupportedOutput, FileMode.CreateNew, FileAccess.Write)) boundary.Write(stream);
                    throw new InvalidDataException($"Completion blocked at turn{current.Spawn.Turn} after{steps.Count} inputs: " +
                        ended.UnsupportedReason + "; boundary=" + unsupportedOutput);
                }
                current = ended.State!; outcome = ended.Outcome;
                steps.Add(new Step { IsEndTurn = true }); played = 0;
            }
        }
        Require(Terminal(outcome) && keys.Key(root) == immutable,
            "Completion exceeded256 inputs or mutated its native root.");
        Freeze(nativeInput, JsonSerializer.Serialize(steps), output);
        using var exported = FixtureDocument.Read(output);
        Require(keys.Key(exported.RootElement.GetProperty("Final").Deserialize<BattleTurnState>()!) == keys.Key(current),
            "Independent completion export changed its terminal state/RNG.");
        Console.WriteLine($"COMPLETE-ROOT outcome={outcome} prefix={prefix} steps={steps.Count} room={floor} strength={strength} " +
            $"unsupportedReasons={unsupported.Count}; model prediction only, native validation required.");
    }

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
