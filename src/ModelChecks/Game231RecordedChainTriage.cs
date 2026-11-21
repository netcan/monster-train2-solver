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
        writer.WriteLine("archive\tstatus\tplays\tendTurns\tseconds\tstage\tdetail\tarchiveSHA256\tcheckerSHA256\tmodelSHA256");
        Console.WriteLine($"RECORDED-CHAIN-TRIAGE-START registered={catalog.Count} selected={(selected.Count == 0 ? catalog.Count : selected.Count)} checkerSHA256={checkerHash} modelSHA256={modelHash} report={output}");

        int passed = 0, failed = 0, skipped = 0;
        foreach (Game231FixtureRegistration archive in catalog.Values)
        {
            if (selected.Count > 0 && !selected.Contains(archive.Path)) continue;
            var timer = Stopwatch.StartNew();
            int? plays = null, endTurns = null;
            string detail = "";
            bool pass = false;
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
                RecordedBattleChecks.ReplayOnce(battle);
                passed++;
                pass = true;
            }
            catch (Exception failure)
            {
                failed++;
                detail = failure.Message;
                Console.WriteLine($"RECORDED-CHAIN-TRIAGE FAIL {archive.Path} {detail}");
            }
            string seconds = timer.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
            WriteRow(writer, archive.Path, pass ? "PASS" : "FAIL", plays?.ToString(CultureInfo.InvariantCulture) ?? "",
                endTurns?.ToString(CultureInfo.InvariantCulture) ?? "", seconds, archive.Stage ? "yes" : "no", detail,
                archive.Sha256, checkerHash, modelHash);
        }

        Console.WriteLine($"RECORDED-CHAIN-TRIAGE-RESULT passed={passed} failed={failed} skipped={skipped} report={output}");
        if (Hash(checkerPath) != checkerHash || Hash(modelPath) != modelHash)
            throw new InvalidOperationException("The checker or model changed during archive triage.");
        if (failed > 0) throw new InvalidOperationException($"{failed} registered archive check(s) failed. See {output}");
        if (passed == 0) throw new InvalidOperationException("No settled native battles were selected.");
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void WriteRow(TextWriter output, params string[] columns) => output.WriteLine(string.Join('\t', columns.Select(column =>
        "\"" + column.Replace("\"", "\"\"", StringComparison.Ordinal).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ') + "\"")));
}
