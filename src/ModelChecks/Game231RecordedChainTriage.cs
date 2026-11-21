using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class Game231RecordedChainTriage
{
    internal static void Run(string workspace, string output, string[] archivePaths)
    {
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
        Console.WriteLine($"RECORDED-CHAIN-TRIAGE-START registered={catalog.Count} selected={(selected.Count == 0 ? catalog.Count : selected.Count)} checkerSHA256={checkerHash} modelSHA256={modelHash} report={output}");

        int passed = 0, segmentPassed = 0, failed = 0, skipped = 0;
        foreach (Game231FixtureRegistration archive in catalog.Values)
        {
            if (selected.Count > 0 && !selected.Contains(archive.Path)) continue;
            var timer = Stopwatch.StartNew();
            int? plays = null, endTurns = null;
            string detail = "";
            string rawMismatch = "";
            string status = "FAIL";
            try
            {
                using var document = FixtureDocument.Read(archive.Verify(directory));
                FixtureValue battle = document.RootElement;
                Game231FixtureKind kind = Game231FixtureCatalog.Classify(archive, battle);
                if (kind != Game231FixtureKind.SettledBattle)
                {
                    skipped++;
                    continue;
                }
                plays = battle.GetProperty("Actions").GetArrayLength();
                endTurns = battle.GetProperty("Turns").GetArrayLength();
                try
                {
                    RecordedBattleChecks.ReplayOnce(battle);
                    passed++;
                    status = "PASS";
                }
                catch (InvalidOperationException rawFailure)
                {
                    rawMismatch = rawFailure.Message;
                    if (archive.Stage || !HasDocumentedTurnOneSetup(archive.Path, battle, rawMismatch)) throw;
                    RecordedBattleChecks.ReplayOnce(battle, lastTurn: 0);
                    RecordedBattleChecks.ReplayOnce(battle, firstTurn: 1);
                    detail = "Verified deployment and turn 1 through terminal on either side of recorded test setup.";
                    segmentPassed++;
                    status = "SEGMENT-PASS";
                }
            }
            catch (Exception failure)
            {
                failed++;
                detail = failure.Message;
                Console.WriteLine($"RECORDED-CHAIN-TRIAGE FAIL {archive.Path} {detail}");
            }
            string seconds = timer.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
            WriteRow(writer, archive.Path, status, plays?.ToString(CultureInfo.InvariantCulture) ?? "",
                endTurns?.ToString(CultureInfo.InvariantCulture) ?? "", seconds, archive.Stage ? "yes" : "no", detail, rawMismatch,
                archive.Sha256, checkerHash, modelHash);
        }

        Console.WriteLine($"RECORDED-CHAIN-TRIAGE-RESULT passed={passed} segmentPassed={segmentPassed} failed={failed} skipped={skipped} report={output}");
        if (Hash(checkerPath) != checkerHash || Hash(modelPath) != modelHash)
            throw new InvalidOperationException("The checker or model changed during archive triage.");
        if (failed > 0) throw new InvalidOperationException($"{failed} registered archive check(s) failed. See {output}");
        if (passed == 0 && segmentPassed == 0) throw new InvalidOperationException("No settled native battles were selected.");
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
        bool lifeLinkSetup = archive == "mechanics/lifelink-maximum/full-battle.mt2f" ||
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
            !Flag(control, "Prepared") || !Flag(control, "AuthoredCardsAndDrawOrder")) return false;
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
