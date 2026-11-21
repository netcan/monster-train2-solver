using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class Game231RecordedChainTriage
{
    internal static void Run(string workspace, string output, string[] archivePaths)
    {
        int workers = 1;
        const string workerOption = "--workers=";
        if (archivePaths.Length > 0 && archivePaths[0].StartsWith(workerOption, StringComparison.Ordinal))
        {
            if (!int.TryParse(archivePaths[0].AsSpan(workerOption.Length), NumberStyles.None, CultureInfo.InvariantCulture, out workers) ||
                workers is < 1 or > 32)
                throw new ArgumentException("Archive workers must be between 1 and 32.");
            archivePaths = archivePaths[1..];
        }
        bool checkCardFrontiers = archivePaths.Length > 0 && archivePaths[0] == "--card-frontiers";
        if (checkCardFrontiers) archivePaths = archivePaths[1..];
        bool checkEndTurnFrontiers = archivePaths.Length > 0 && archivePaths[0] == "--end-turn-frontiers";
        if (checkEndTurnFrontiers) archivePaths = archivePaths[1..];

        string directory = Game231FixtureCatalog.DirectoryFor(workspace);
        var catalog = Game231FixtureCatalog.Load(directory);
        var selected = archivePaths.Select(path => path.Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
        foreach (string path in selected)
            if (!catalog.ContainsKey(path)) throw new ArgumentException("Unknown registered archive: " + path);

        string checkerPath = typeof(Game231RecordedChainTriage).Assembly.Location;
        string modelPath = typeof(BattleTurnState).Assembly.Location;
        string checkerHash = Hash(checkerPath);
        string modelHash = Hash(modelPath);
        output = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        using var writer = new StreamWriter(output) { AutoFlush = true };
        writer.WriteLine("archive\tstatus\tplays\tendTurns\tseconds\tstage\tdetail\trawMismatch\tarchiveSHA256\tcheckerSHA256\tmodelSHA256");
        Console.WriteLine($"RECORDED-CHAIN-TRIAGE-START registered={catalog.Count} selected={(selected.Count == 0 ? catalog.Count : selected.Count)} workers={workers} cardFrontiers={checkCardFrontiers} endTurnFrontiers={checkEndTurnFrontiers} checkerSHA256={checkerHash} modelSHA256={modelHash} report={output}");

        int passed = 0, segmentPassed = 0, failed = 0, skipped = 0;
        IEnumerable<Game231FixtureRegistration> archives = catalog.Values.Where(archive => selected.Count == 0 || selected.Contains(archive.Path));
        ArchiveResult Replay(Game231FixtureRegistration archive) =>
            ReplayArchive(directory, archive, checkCardFrontiers, checkEndTurnFrontiers);
        IEnumerable<ArchiveResult> results = workers == 1
            ? archives.Select(Replay)
            : archives.AsParallel().AsOrdered().WithDegreeOfParallelism(workers)
                .WithMergeOptions(ParallelMergeOptions.NotBuffered).Select(Replay);
        foreach (ArchiveResult result in results)
        {
            switch (result.Status)
            {
                case "SKIP": skipped++; continue;
                case "PASS": passed++; break;
                case "SEGMENT-PASS": segmentPassed++; break;
                default:
                    failed++;
                    Console.WriteLine($"RECORDED-CHAIN-TRIAGE FAIL {result.Archive.Path} {result.Detail}");
                    break;
            }
            Game231FixtureRegistration archive = result.Archive;
            WriteRow(writer, archive.Path, result.Status, result.Plays?.ToString(CultureInfo.InvariantCulture) ?? "",
                result.EndTurns?.ToString(CultureInfo.InvariantCulture) ?? "", result.Seconds.ToString("F3", CultureInfo.InvariantCulture),
                archive.Stage ? "yes" : "no", result.Detail, result.RawMismatch, archive.Sha256, checkerHash, modelHash);
        }

        Console.WriteLine($"RECORDED-CHAIN-TRIAGE-RESULT passed={passed} segmentPassed={segmentPassed} failed={failed} skipped={skipped} report={output}");
        if (Hash(checkerPath) != checkerHash || Hash(modelPath) != modelHash)
            throw new InvalidOperationException("The checker or model changed during archive triage.");
        if (failed > 0) throw new InvalidOperationException($"{failed} registered archive check(s) failed. See {output}");
        if (passed == 0 && segmentPassed == 0) throw new InvalidOperationException("No settled native battles were selected.");
    }

    private sealed record ArchiveResult(Game231FixtureRegistration Archive, string Status, int? Plays,
        int? EndTurns, double Seconds, string Detail, string RawMismatch);

    private static ArchiveResult ReplayArchive(string directory, Game231FixtureRegistration archive,
        bool checkCardFrontiers, bool checkEndTurnFrontiers)
    {
        var timer = Stopwatch.StartNew();
        int? plays = null, endTurns = null;
        string rawMismatch = "";
        ArchiveResult Result(string status, string detail = "") =>
            new(archive, status, plays, endTurns, timer.Elapsed.TotalSeconds, detail, rawMismatch);
        try
        {
            using var document = FixtureDocument.Read(archive.Verify(directory));
            FixtureValue battle = document.RootElement;
            if (Game231FixtureCatalog.Classify(archive, battle) != Game231FixtureKind.SettledBattle)
                return Result("SKIP");

            plays = battle.GetProperty("Actions").GetArrayLength();
            endTurns = battle.GetProperty("Turns").GetArrayLength();
            string status = "PASS", detail = "";
            try
            {
                RecordedBattleChecks.ReplayOnce(battle);
            }
            catch (InvalidOperationException rawFailure)
            {
                rawMismatch = rawFailure.Message;
                if (archive.Stage) throw;
                if (archive.Path == "mechanics/moon-phase-hand-upgrade/battle.mt2f")
                {
                    OriginalMoonPhaseRoomsGainMagicPowerChecks.ReplayPreparedTurns(battle, rawMismatch);
                    detail = "Verified all seven turns and six recorded original-card hand setups.";
                }
                else
                {
                    if (!HasDocumentedTurnOneSetup(archive.Path, battle, rawMismatch)) throw;
                    RecordedBattleChecks.ReplayOnce(battle, lastTurn: 0);
                    RecordedBattleChecks.ReplayOnce(battle, firstTurn: 1);
                    detail = "Verified deployment and turn 1 through terminal on either side of recorded test setup.";
                }
                status = "SEGMENT-PASS";
            }

            if (checkCardFrontiers)
            {
                if (plays > 0)
                    BattleSearchChecks.RecordedFrontiers(battle, requireZeroUnsupported: true, report: false);
                if (detail.Length > 0) detail += " ";
                detail += $"Checked {plays} recorded card frontiers.";
            }
            if (checkEndTurnFrontiers)
            {
                BattleSearchChecks.RecordedEndTurnFrontiers(battle, choiceBudget: 64);
                if (detail.Length > 0) detail += " ";
                detail += $"Checked {endTurns} recorded EndTurn frontiers.";
            }
            return Result(status, detail);
        }
        catch (Exception failure)
        {
            return Result("FAIL", failure.Message);
        }
    }

    // These legacy test scenarios explicitly prepare state outside the recorded action list.
    private static bool HasDocumentedTurnOneSetup(string archive, FixtureValue battle, string mismatch)
    {
        string? friendlySetup = archive switch
        {
            "mechanics/friendly-trigger-damage/burnout.mt2f" => "OriginalBurnoutDamage",
            "mechanics/friendly-trigger-damage/eaten.mt2f" => "OriginalEatenDamage",
            _ => null
        };
        bool earlyEntropy = archive == "mechanics/lifelink-early-sacrifice/entropy-bottom-battle.mt2f";
        bool lifeLinkSetup = earlyEntropy || archive == "mechanics/lifelink-maximum/full-battle.mt2f" ||
            archive.StartsWith("mechanics/lifelink-sacrifice/", StringComparison.Ordinal);
        if (friendlySetup == null && !lifeLinkSetup) return false;

        FixtureValue[] actions = battle.GetProperty("Actions").EnumerateArray().ToArray();
        int deploymentPlays = friendlySetup == null ? 0 : 2;
        if (actions.Length <= deploymentPlays || actions.Take(deploymentPlays).Any(row => RecordedBattleChecks.DecisionTurn(row) != 0) ||
            RecordedBattleChecks.DecisionTurn(actions[deploymentPlays]) != 1 ||
            actions.Skip(deploymentPlays).Any(row => RecordedBattleChecks.DecisionTurn(row) < 1) ||
            !mismatch.StartsWith($"before play {deploymentPlays + 1} in turn 1: ", StringComparison.Ordinal)) return false;
        FixtureValue[] turns = battle.GetProperty("Turns").EnumerateArray().ToArray();
        if (turns.Length < 2 || RecordedBattleChecks.DecisionTurn(turns[0]) != 0 ||
            RecordedBattleChecks.DecisionTurn(turns[1]) != 1 || turns.Skip(1).Any(row => RecordedBattleChecks.DecisionTurn(row) < 1)) return false;

        BattleTurnState deployed = turns[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState firstPlay = actions[deploymentPlays].GetProperty("Before").Deserialize<BattleTurnState>()!;
        if (deployed.Spawn.Turn != 1 || firstPlay.Spawn.Turn != 1) return false;
        if (friendlySetup != null)
        {
            if (!Flag(battle, friendlySetup + "Prepared") ||
                !battle.TryGetProperty(friendlySetup, out FixtureValue setup) || setup.ValueKind != FixtureKind.Object) return false;
            int[] injected = setup.GetProperty("EnemyUnitIds").EnumerateArray().Select(id => id.GetInt32()).ToArray();
            var deployedIds = deployed.Spawn.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            var readyEnemyIds = firstPlay.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).ToHashSet();
            return injected.Length > 0 && injected.All(id => !deployedIds.Contains(id) && readyEnemyIds.Contains(id));
        }

        if (!battle.TryGetProperty("TrueFinalBossMaximumBattle", out FixtureValue control) || control.ValueKind != FixtureKind.Object ||
            !Flag(control, "Prepared") || !Flag(control, "AuthoredCardsAndDrawOrder") ||
            earlyEntropy && !Flag(control, "EarlyEntropy")) return false;
        BattleTurnState before = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RecordedBattleChecks.Compare(deployed, before, false, "before recorded LifeLink hand setup");
        RecordedBattleChecks.Compare(after, firstPlay, false, "after recorded LifeLink hand setup");
        return true;
    }

    private static bool Flag(FixtureValue root, string key) => root.TryGetProperty(key, out FixtureValue value) && value.ValueKind == FixtureKind.True;

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void WriteRow(TextWriter output, params string[] columns) => output.WriteLine(string.Join('\t', columns.Select(column =>
        "\"" + column.Replace("\"", "\"\"", StringComparison.Ordinal).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ') + "\"")));
}
