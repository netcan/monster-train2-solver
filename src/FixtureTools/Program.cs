using System.Diagnostics;
using System.Security.Cryptography;
using MonsterTrain2Poju.Fixtures;

if (args.Length == 3 && args[0] == "extract-rolling-action-mismatch")
{
    RollingActionMismatchRequest.Extract(args[1], args[2]);
}
else if (args.Length == 3 && args[0] == "extract-rolling-turn-mismatch")
{
    RollingActionMismatchRequest.ExtractTurn(args[1], args[2]);
}
else if (args.Length == 4 && args[0] == "refresh-native-prefix")
{
    PreviewIdMismatchRequest.RefreshNativePrefix(args[1], args[2], args[3]);
}
else if (args.Length == 3 && args[0] == "observe-partial-mismatch")
{
    PreviewIdMismatchRequest.ObservePartial(args[1], args[2]);
}
else if (args.Length == 4 && args[0] == "refresh-preview-id-prefix")
{
    PreviewIdMismatchRequest.Refresh(args[1], args[2], args[3]);
}
else if (args.Length == 4 && args[0] == "refresh-existing-horde-preview-id-prefix")
{
    PreviewIdMismatchRequest.RefreshExistingHorde(args[1], args[2], args[3]);
}
else if (args.Length == 3 && args[0] == "extract-existing-horde-preview-id-mismatch")
{
    PreviewIdMismatchRequest.ExtractExistingHorde(args[1], args[2]);
}
else if (args.Length == 3 && args[0] == "extract-preview-id-mismatch")
{
    PreviewIdMismatchRequest.Extract(args[1], args[2]);
}
else if (args.Length == 3 && args[0] == "extract-advice-budget-failure")
{
    AdviceBudgetFailure.Extract(args[1], args[2]);
}
else if (args.Length == 3 && args[0] == "extract-inactive-fragile-inferno-exit")
{
    InactiveFragileInfernoExitRequest.Extract(args[1], args[2]);
}
else if (args.Length == 3 && args[0] == "extract-inactive-heavy-inferno-exit")
{
    InactiveFragileInfernoExitRequest.Extract(args[1], args[2], heavy: true);
}
else if (args.Length == 3 && args[0] == "extract-inactive-fullmoon-inferno-exit")
{
    InactiveFragileInfernoExitRequest.Extract(args[1], args[2], fullMoon: true);
}
else if (args.Length == 3 && args[0] == "extract-inactive-fragile-inferno-new-moon-entry")
{
    InactiveFragileInfernoExitRequest.Extract(args[1], args[2], newMoonEntry: true);
}
else if (args.Length == 3 && args[0] == "extract-inactive-fragile-inferno-entry")
{
    InactiveFragileInfernoExitRequest.Extract(args[1], args[2], entry: true);
}
else if (args.Length is 7 or 8 && args[0] == "plan-belief")
{
    BattleBeliefCommands.Run(args[1], args[2], int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]), int.Parse(args[6]),
        args.Length == 8 ? int.Parse(args[7]) : null);
}
else if (args.Length == 2 && args[0] == "verify-belief")
{
    BattleBeliefCommands.Verify(args[1]);
}
else if (args.Length is 5 or 6 && args[0] == "solve-exact")
{
    ExactBattleCommands.Run(args[1], args[2], int.Parse(args[3]), int.Parse(args[4]), args.Length == 6 ? int.Parse(args[5]) : null);
}
else if (args.Length == 3 && args[0] == "plan-once")
{
    OfflineBattleSearch.Single(args[1], args[2]);
}
else if (args.Length is >= 4 and <= 7 && args[0] == "plan-root")
{
    OfflineRootSearch.Run(args[1], args[2], int.Parse(args[3]),
        args.Length >= 5 ? int.Parse(args[4]) : 0, args.Length >= 6 ? int.Parse(args[5]) : 2,
        args.Length == 7 ? int.Parse(args[6]) : 256);
}
else if (args.Length == 2 && args[0] == "verify-root-plan")
{
    OfflineRootSearch.Verify(args[1]);
}
else if (args.Length == 3 && args[0] == "freeze-root-plan")
{
    OfflineBattleSearch.FreezeRootPlan(args[1], args[2]);
}
else if (args.Length is 5 or 6 && args[0] == "complete-root-plan" && (args.Length == 5 || args[5] == "--strength"))
{
    OfflineBattleSearch.CompleteRootPlan(args[1], args[2], args[3], int.Parse(args[4]), args.Length == 6);
}
else if (args.Length is >= 6 and <= 8 && args[0] == "plan-battle" &&
    args.Skip(6).All(flag => flag is "--tactical" or "--player-continuation") &&
    args.Skip(6).Distinct().Count() == args.Length - 6)
{
    OfflineBattleSearch.Run(args[1], args[2], int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]),
        args.Contains("--tactical"), args.Contains("--player-continuation"));
}
else if (args.Length == 4 && args[0] == "freeze-actions")
{
    OfflineBattleSearch.Freeze(args[1], args[2] == "-" ? Console.In.ReadToEnd() : File.ReadAllText(args[2]), args[3]);
}
else if (args.Length == 3 && args[0] == "extract-simple-policy-unsupported")
{
    OfflineBattleSearch.ExtractSimplePolicyUnsupported(args[1], args[2]);
}
else if (args.Length == 4 && args[0] == "extract-unsupported-branch")
{
    OfflineBattleSearch.ExtractUnsupported(args[1], args[2], args[3]);
}
else if (args.Length == 5 && args[0] == "extract-unsupported-from")
{
    OfflineBattleSearch.ExtractUnsupported(args[1], args[2], args[3], fromDecision: int.Parse(args[4]));
}
else if (args.Length == 6 && args[0] == "explore-unsupported-from")
{
    OfflineBattleSearch.ExtractUnsupported(args[1], args[2], args[3], fromDecision: int.Parse(args[4]), exploreDepth: int.Parse(args[5]));
}
else if (args.Length is >= 4 and <= 6 && args[0] == "extract-unsupported-endturn" &&
    (args.Length < 6 || args[5] == "--strength"))
{
    int room = args.Length >= 5 ? int.Parse(args[4]) : 2;
    if (room is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(room), "Expected floor0..2.");
    OfflineBattleSearch.ExtractUnsupported(args[1], args[2], args[3], endTurnOnly: true, preferredRoom: room,
        strength: args.Length == 6);
}
else if (args.Length is 4 or 5 && args[0] == "policy-battle" && (args.Length == 4 || args[4] == "--strength"))
{
    OfflineBattleSearch.Policy(args[1], args[2], int.Parse(args[3]), args.Length == 5);
}
else if (args.Length is >= 4 and <= 6 && args[0] == "improve-policy")
{
    OfflineBattleSearch.Improve(args[1], args[2], int.Parse(args[3]), args.Length >= 5 ? int.Parse(args[4]) : 4,
        startDecision: args.Length == 6 ? int.Parse(args[5]) : 0);
}
else if (args.Length is >= 5 and <= 7 && args[0] == "improve-prediction")
{
    OfflineBattleSearch.Improve(args[1], args[3], int.Parse(args[4]), args.Length >= 6 ? int.Parse(args[5]) : 4, args[2],
        args.Length == 7 ? int.Parse(args[6]) : 0);
}
else if (args.Length == 7 && args[0] == "improve-order")
{
    OfflineBattleSearch.Improve(args[1], args[3], int.Parse(args[4]), int.Parse(args[5]), args[2],
        int.Parse(args[6]), exploreCardOrder: true);
}
else if (args.Length is 7 or 8 && args[0] == "improve-suffix")
{
    OfflineBattleSearch.Improve(args[1], args[3], int.Parse(args[4]), int.Parse(args[5]), args[2],
        int.Parse(args[6]), exploreCardOrder: true, retainSuffix: true,
        unsupportedReasonFilter: args.Length == 8 ? args[7] : null);
}
else if (args.Length == 8 && args[0] == "improve-pairs")
{
    OfflineBattleSearch.Improve(args[1], args[3], int.Parse(args[4]), int.Parse(args[5]), args[2],
        int.Parse(args[6]), exploreCardOrder: true, retainSuffix: true, secondActionWidth: int.Parse(args[7]));
}
else if (args.Length is 8 or 9 && args[0] == "improve-win")
{
    OfflineBattleSearch.Improve(args[1], args[3], int.Parse(args[4]), int.Parse(args[5]), args[2],
        int.Parse(args[6]), exploreCardOrder: true, retainSuffix: true, secondActionWidth: int.Parse(args[7]),
        improveWinningRoute: true, perDecisionCandidateLimit: args.Length == 9 ? int.Parse(args[8]) : 0);
}
else if (args.Length == 10 && args[0] == "improve-triples")
{
    int limit = int.Parse(args[9]);
    int second = int.Parse(args[7]), third = int.Parse(args[8]);
    if (limit is < 1 or > 256 || second is < 1 or > 32 || third is < 1 or > 16)
        throw new ArgumentOutOfRangeException(nameof(limit), "Expected1..256 candidates,1..32 second inputs and1..16 third inputs.");
    OfflineBattleSearch.Improve(args[1], args[3], int.Parse(args[4]), int.Parse(args[5]), args[2],
        int.Parse(args[6]), exploreCardOrder: true, retainSuffix: true, secondActionWidth: second,
        perDecisionCandidateLimit: limit, thirdActionWidth: third);
}
else if (args.Length == 9 && args[0] == "improve-sweep")
{
    int limit = int.Parse(args[8]);
    if (limit is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(limit), "Expected1..256 candidates per decision.");
    OfflineBattleSearch.Improve(args[1], args[3], int.Parse(args[4]), int.Parse(args[5]), args[2],
        int.Parse(args[6]), exploreCardOrder: true, retainSuffix: true, secondActionWidth: int.Parse(args[7]),
        perDecisionCandidateLimit: limit);
}
else if (args.Length == 11 && args[0] is "improve-diverse" or "improve-survival" or "improve-survival-pairs")
{
    int limit = int.Parse(args[7]), count = int.Parse(args[8]), width = int.Parse(args[9]), second = int.Parse(args[10]);
    if (limit is < 1 or > 256 || count is < 1 or > 256 || width is < 1 or > 16 || second is < 0 or > 32)
        throw new ArgumentOutOfRangeException(nameof(limit), "Expected1..256 candidates,1..256 decisions,1..16 routes and0..32 second inputs.");
    OfflineBattleSearch.Improve(args[1], args[3], int.Parse(args[4]), int.Parse(args[5]), args[2],
        int.Parse(args[6]), exploreCardOrder: true, retainSuffix: true, secondActionWidth: second,
        perDecisionCandidateLimit: limit, diversificationWidth: width, maxDecisionCount: count,
        preferLaterDefeat: args[0] != "improve-diverse", balancePairs: args[0] == "improve-survival-pairs");
}
else if (args.Length is 6 or 7 && args[0] == "deployment-grid")
{
    OfflineBattleSearch.DeploymentGrid(args[1], args[2], args[3], int.Parse(args[4]), int.Parse(args[5]),
        args.Length == 7 ? int.Parse(args[6]) : null);
}
else if (args.Length is 6 or 7 && args[0] == "deployment-win")
{
    OfflineBattleSearch.DeploymentGrid(args[1], args[2], args[3], int.Parse(args[4]), int.Parse(args[5]),
        args.Length == 7 ? int.Parse(args[6]) : null, improveWinningRoute: true);
}
else if (args.Length is >= 6 and <= 9 && args[0] is "deployment-spread" or "deployment-order-spread" or "deployment-order-strength" or "deployment-order-mixed-strength")
{
    OfflineBattleSearch.DeploymentGrid(args[1], args[2], args[3], int.Parse(args[4]), int.Parse(args[5]),
        args.Length >= 7 && args[6] != "any" ? int.Parse(args[6]) : null, spread: true,
        startSample: args.Length >= 8 ? int.Parse(args[7]) : 0,
        unsupportedReasonFilter: args.Length == 9 ? args[8] : null, spreadOrder: args[0] != "deployment-spread",
        strengthRollout: args[0] is "deployment-order-strength" or "deployment-order-mixed-strength",
        mixedPlacements: args[0] == "deployment-order-mixed-strength");
}
else if (args.Length is >= 4 and <= 6 && args[0] == "refine-policy")
{
    OfflineBattleSearch.Refine(args[1], args[2], int.Parse(args[3]), args.Length >= 5 ? int.Parse(args[4]) : 0,
        args.Length == 6 ? int.Parse(args[5]) : null);
}
else if (args.Length is >= 5 and <= 7 && args[0] == "refine-prediction")
{
    OfflineBattleSearch.Refine(args[1], args[3], int.Parse(args[4]), args.Length >= 6 ? int.Parse(args[5]) : 0,
        args.Length == 7 ? int.Parse(args[6]) : null, args[2]);
}
else if (args.Length == 2 && args[0] == "verify-offline-plan")
{
    OfflineBattleSearch.Verify(args[1]);
}
else if (args.Length == 3 && args[0] == "verify-offline-native")
{
    OfflineBattleSearch.VerifyNative(args[1], args[2]);
}
else if (args.Length is 2 or 3 && args[0] == "boss-inventory" && (args.Length == 2 || args[2] == "--check"))
{
    BossCoverageInventory.Run(args[1], args.Length == 3);
}
else if (args.Length is 2 or 3 && args[0] == "card-inventory" && (args.Length == 2 || args[2] == "--check"))
{
    CardCoverageInventory.Run(args[1], args.Length == 3);
}
else if (args.Length == 4 && args[0] == "bundle-native")
{
    NativeFixtureBundle.Pack(args[1], args[2], args[3]);
}
else if (args.Length == 3 && args[0] == "pack")
{
    Console.WriteLine("MANIFEST: " + Pack(args[1], args[2]));
}
else if (args.Length == 3 && args[0] == "verify")
{
    using var binary = FixtureDocument.Read(args[2]);
    LegacyFixtureImport.Verify(args[1], binary);
    Console.WriteLine("FIXTURE-VERIFY PASS: all captured values, property order and numeric lexemes preserved.");
}
else if (args.Length == 3 && args[0] == "compare-speeds")
{
    NativeSpeedComparison.Run(args[1], args[2]);
}
else if (args.Length == 3 && args[0] == "compare-values")
{
    using var legacy = LegacyFixtureImport.Read(args[1]);
    using var direct = FixtureDocument.Read(args[2]);
    if (!legacy.RootElement.ContentEquals(direct.RootElement))
        throw new InvalidDataException("Native binary capture differs from diagnostic JSON values.");
    Console.WriteLine("NATIVE-BINARY-VALUES PASS: every captured value, property order and numeric lexeme matches the diagnostic JSON export.");
}
else if (args.Length == 3 && args[0] == "benchmark-capture")
{
    NativeCaptureBenchmark.Run(args[1], args[2]);
}
else if (args.Length == 3 && args[0] == "pack-directory")
{
    string sourceDirectory = Path.GetFullPath(args[1]), outputDirectory = Path.GetFullPath(args[2]);
    Directory.CreateDirectory(outputDirectory);
    var sources = Directory.GetFiles(sourceDirectory).Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".json.gz", StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal).ToArray();
    if (sources.Length == 0) throw new InvalidOperationException("No legacy fixture inputs found.");
    var manifest = new List<string> { FixtureCatalog.Header };
    foreach (string source in sources)
    {
        string stem = Path.GetFileName(source).Replace(".json.gz", "", StringComparison.OrdinalIgnoreCase).Replace(".json", "", StringComparison.OrdinalIgnoreCase);
        string archive = Path.Combine(outputDirectory, stem + ".mt2f");
        manifest.Add(Pack(source, archive));
        GC.Collect(); // Drop large import DOMs before the next capture.
    }
    File.WriteAllLines(Path.Combine(outputDirectory, "manifest.tsv"), manifest);
    Console.WriteLine($"FIXTURE-MIGRATION PASS: {sources.Length} complete captures verified.");
}
else
{
    Console.Error.WriteLine("Diagnostics: extract-simple-policy-unsupported <native.mt2f> <new-output.mt2f>");
    Console.Error.WriteLine("Diagnostics: extract-unsupported-branch <recorded.mt2f> <new-output.mt2f> <exact-rejection-reason>");
    Console.Error.WriteLine("Diagnostics: extract-unsupported-from <recorded.mt2f> <new-output.mt2f> <exact-rejection-reason> <first-decision-index>; retain the complete original prefix.");
    Console.Error.WriteLine("Diagnostics: explore-unsupported-from <recorded.mt2f> <new-output.mt2f> <exact-rejection-reason> <first-decision-index> <depth:1..8>; explore up to 2000 model states after the recorded prefix.");
    Console.Error.WriteLine("Diagnostics: observe-partial-mismatch <partial-native.mt2f> <new-request.mt2f>; validate the matched prefix and independently observe the mismatching input.");
    Console.Error.WriteLine("Diagnostics: refresh-native-prefix <old-request.mt2f> <native-observation.mt2f> <new-request.mt2f>; verify the native prefix before rebuilding the protected target.");
    Console.Error.WriteLine("Captured mismatch: extract-inactive-fragile-inferno-exit <native.mt2f> <new-request.mt2f>; replay and validate the original 18-step prefix and repaired end turn before independent native observation.");
    Console.Error.WriteLine("Captured mismatch: extract-inactive-heavy-inferno-exit <native.mt2f> <new-request.mt2f>; validate the original 25-step prefix and repaired end turn before independent native observation.");
    Console.Error.WriteLine("Captured mismatch: extract-inactive-fullmoon-inferno-exit <native.mt2f> <new-request.mt2f>; validate the original 33-step prefix and repaired end turn before independent native observation.");
    Console.Error.WriteLine("Captured mismatch: extract-inactive-fragile-inferno-entry <native.mt2f> <new-request.mt2f>; validate the original 34-step prefix and repaired end turn before independent native observation.");
    Console.Error.WriteLine("Captured mismatch: extract-inactive-fragile-inferno-new-moon-entry <native.mt2f> <new-request.mt2f>; validate the original 41-step prefix and repaired end turn before independent native observation.");
    Console.Error.WriteLine("EndTurn diagnostics: extract-unsupported-endturn <native-or-deployment.mt2f> <new-output.mt2f> <exact-rejection-reason> [floor:0..2] [--strength]; match policy-battle's stronger heuristic when reproducing a deployment branch.");
    Console.Error.WriteLine("Player root: plan-root <capture.mt2f> <new-output.mt2f> <milliseconds> [decision-index] [workers] [shared-nodes] | verify-root-plan <prediction.mt2f> | freeze-root-plan <prediction.mt2f> <new-output.mt2f>; nodes default256, other node budgets are offline experiments.");
    Console.Error.WriteLine("Partial forecast: complete-root-plan <native.mt2f> <verified-root-prediction.mt2f> <new-output.mt2f> <deployment-room> [--strength]; preserves the entire verified prefix before policy completion; model evidence only.");
    Console.Error.WriteLine("Counterexample: refine-policy <native.mt2f> <new-winning-output.mt2f> <milliseconds> [start-decision] [last-decision]; bounded single-decision alternatives followed by legal tactical choices, model prediction only.");
    Console.Error.WriteLine("Iterated counterexample: improve-policy <native.mt2f> <new-output.mt2f> <milliseconds> [passes] [start-decision]; retain complete improvements between sweeps; exported outcome may still be a defeat and needs native validation.");
    Console.Error.WriteLine("Resume improvement: improve-prediction <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> [passes] [start-decision]; independently verifies the full baseline and binds its source hash and complete native deployment root.");
    Console.Error.WriteLine("Card order investigation: improve-order <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <passes> <start-decision>; also tries other cards and unit abilities at each decision; model prediction only.");
    Console.Error.WriteLine("Retained route investigation: improve-suffix <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <passes> <start-decision>; reapplies legal recorded suffix inputs by turn before tactical fallback; model prediction only.");
    Console.Error.WriteLine("Retained route investigation: improve-suffix <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <passes> <start-decision> [exact-unsupported-reason]; save the first matching rejected branch; model prediction only.");
    Console.Error.WriteLine("Joint route investigation: improve-pairs <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <passes> <start-decision> <second-action-width:0..32>; bounded two-input alternatives, independent final replay; model prediction only.");
    Console.Error.WriteLine("Winning route investigation: improve-win <native.mt2f> <verified-winning-prediction.mt2f> <new-output.mt2f> <milliseconds> <passes> <start-decision> <second-action-width:0..32> [candidates-per-decision:0..256]; retain complete winning improvements for the next campaign battle; model prediction only.");
    Console.Error.WriteLine("Bounded route sweep: improve-sweep <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <passes> <start-decision> <second-action-width:0..32> <candidates-per-decision:1..256>; share the budget across later decisions; model prediction only.");
    Console.Error.WriteLine("Diversified route investigation: improve-diverse <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <passes> <start-decision> <candidates-per-decision:1..256> <decision-count:1..256> <alternative-routes:1..16> <second-action-width:0..32>; explore bounded complete losing alternatives, export only a strictly better independently replayed route; model prediction only.");
    Console.Error.WriteLine("Survival route investigation: improve-survival has the same arguments as improve-diverse; wins rank first, then later complete losses, then the original loss score. Model prediction only; does not change player search.");
    Console.Error.WriteLine("Balanced survival investigation: improve-survival-pairs uses the same arguments, reserving part of each decision budget for joint inputs; requires at least two candidates and a positive second-action width.");
    Console.Error.WriteLine("Deployment investigation: deployment-grid <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <max-candidates:1..4096> [first-unit-room:0..2]; keep the initial unit order and explore joint legal room/position assignments, preserve the first independently replayed unsupported boundary, export only a complete independently replayed improvement; model prediction only.");
    Console.Error.WriteLine("Spread deployment investigation: deployment-spread <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <max-candidates:1..4096> [first-unit-room:any|0..2] [start-sample:0..1000000] [unsupported-reason-substring]; sample legal joint placements across early and late units, resume at nextSample, with the same exact-root/complete-replay guards as deployment-grid; model prediction only.");
    Console.Error.WriteLine("Order spread investigation: deployment-order-spread uses the same arguments and guards, also sampling unit play order; first-unit-room applies to the first unit in each sampled order. Fixed opening spells remain unchanged; model prediction only.");
    Console.Error.WriteLine("Strength rollout investigation: deployment-order-strength uses order-spread arguments and guards with the existing strength policy for later decisions; model prediction only.");
    Console.Error.WriteLine("Mixed placement investigation: deployment-order-mixed-strength uses independently mixed deterministic placement choices and strength rollout; resume with the same mode and nextSample; model prediction only.");
    Console.Error.WriteLine("Bounded three-input investigation: improve-triples <native.mt2f> <verified-prediction.mt2f> <new-output.mt2f> <milliseconds> <passes> <start-decision> <second-action-width:1..32> <third-action-width:1..16> <candidates-per-decision:1..256>; model prediction only.");
    Console.Error.WriteLine("Predicted counterexample: refine-prediction <native.mt2f> <verified-prediction.mt2f> <new-winning-output.mt2f> <milliseconds> [start-decision] [last-decision]; validates native input hash/root and independently replays the baseline before exploring.");
    Console.Error.WriteLine("Search: solve-exact <input.mt2f> <new-output.mt2f> <max-nodes> <milliseconds> [EndTurn-horizon] | plan-belief <input.mt2f> <new-output.mt2f> <max-worlds> <decisions> <max-nodes> <milliseconds> [native-Turns-index] | verify-belief <policy.mt2f>");
    Console.Error.WriteLine("Usage: FixtureTools plan-once <request.mt2f> <output.mt2f> | plan-battle <native.mt2f> <output.mt2f> <horizon> <milliseconds-per-decision> <deployment-room> [--tactical] [--player-continuation] | policy-battle <native.mt2f> <output.mt2f> <deployment-room> [--strength] | freeze-actions <native.mt2f> <actions.json or -> <output.mt2f> | verify-offline-plan <prediction.mt2f> | verify-offline-native <prediction.mt2f> <fresh-native.mt2f> | card-inventory <workspace> [--check] | pack <legacy.json[.gz]> <output.mt2f> | bundle-native <baseline.mt2f> <owned.mt2f> <output.mt2f> | verify <legacy> <archive> | compare-values <diagnostic.json> <native.mt2f> | benchmark-capture <fixture.mt2f> <output-directory> | pack-directory <source-dir> <output-dir> | compare-speeds <normal.mt2f> <accelerated.mt2f>");
    return 2;
}
return 0;

static string Pack(string source, string archive)
{
    var timer = Stopwatch.StartNew();
    if (File.Exists(archive)) throw new IOException("Output already exists: " + archive);
    using var imported = LegacyFixtureImport.Read(source);
    string temporary = archive + ".tmp";
    try
    {
        using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write)) imported.Write(output);
        using var binary = FixtureDocument.Read(temporary);
        LegacyFixtureImport.Verify(source, binary);
        File.Move(temporary, archive);
    }
    finally { if (File.Exists(temporary)) File.Delete(temporary); }
    long archiveLength = new FileInfo(archive).Length;
    using var archiveStream = File.OpenRead(archive);
    string archiveHash = Convert.ToHexString(SHA256.HashData(archiveStream)).ToLowerInvariant();
    Console.WriteLine($"FIXTURE-PACK PASS: {Path.GetFileName(archive)}; {new FileInfo(source).Length:N0} -> {archiveLength:N0} bytes; {imported.UniqueNodeCount:N0} unique nodes; {timer.Elapsed.TotalSeconds:F1}s.");
    string archivePath = Path.GetFileName(archive);
    for (DirectoryInfo? directory = new FileInfo(Path.GetFullPath(archive)).Directory; directory != null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "manifest.tsv")))
        { archivePath = Path.GetRelativePath(directory.FullName, Path.GetFullPath(archive)).Replace('\\', '/'); break; }
    return $"{Path.GetFileName(archive)}\t{Path.GetFileName(source)}\t{imported.SourceLength}\t{imported.SourceSha256}\t{archiveLength}\t{archiveHash}\t{imported.UniqueNodeCount}\t{archivePath}";
}
