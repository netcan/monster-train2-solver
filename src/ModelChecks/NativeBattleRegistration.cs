using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MonsterTrain2Poju.Fixtures;

internal static class NativeBattleRegistration
{
    private const string Header = "path\tbytes\tsha256";

    internal static void Run(string workspace, string profile, string relativeArchive, string[] options)
    {
        if (options.Any(option => option is not ("--card-frontiers" or "--end-turn-frontiers")))
            throw new ArgumentException("Expected optional card or EndTurn frontier checks.");

        workspace = Path.TrimEndingDirectorySeparator(Path.GetFullPath(workspace));
        string probeRuns = Path.Combine(workspace, ".probe-runs");
        profile = Path.TrimEndingDirectorySeparator(Path.GetFullPath(profile));
        string profileName = Path.GetRelativePath(probeRuns, profile);
        string[] segments = relativeArchive.Split('/');
        if (!SafeSegment(profileName) || segments.Length < 2 || segments.Any(segment => !SafeSegment(segment)) ||
            !relativeArchive.EndsWith(".mt2f", StringComparison.Ordinal))
            throw new ArgumentException("Use a direct .probe-runs profile and a mechanics-relative group/file.mt2f path.");

        string directory = Game231FixtureCatalog.DirectoryFor(workspace);
        string archiveKey = "mechanics/" + relativeArchive;
        string destination = Path.Combine(directory, archiveKey);
        string destinationDirectory = Path.GetDirectoryName(destination)!;
        string source = Path.Combine(profile, "full-battle.mt2f");
        string summaryPath = Path.Combine(profile, "native-accepted-summary.json");
        string manifest = Path.Combine(destinationDirectory, "manifest.tsv");
        string lockPath = Path.Combine(probeRuns, "native-battle-registration.lock");
        CheckPath(workspace, source);
        CheckPath(workspace, summaryPath);
        CheckPath(workspace, destination);
        if (!Directory.Exists(destinationDirectory)) throw new DirectoryNotFoundException(destinationDirectory);
        if (File.Exists(destination) || Directory.Exists(destination))
            throw new IOException("Fixture already exists: " + destination);

        using var summaryDocument = JsonDocument.Parse(File.ReadAllBytes(summaryPath));
        JsonElement summary = summaryDocument.RootElement;
        string? sha256 = summary.GetProperty("TraceSha256").GetString();
        if (summary.GetProperty("Schema").GetInt32() != 1 || summary.GetProperty("ExitCode").GetInt32() != 0 ||
            !summary.GetProperty("NativePassed").GetBoolean() || !summary.GetProperty("OriginalFilesUnchanged").GetBoolean() ||
            !string.Equals(Path.GetFileName(summary.GetProperty("Profile").GetString()), profileName, StringComparison.OrdinalIgnoreCase) ||
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.Any(key => summary.GetProperty(key).GetInt32() != 0) ||
            sha256 == null || !Regex.IsMatch(sha256, @"^[a-fA-F0-9]{64}\z"))
            throw new InvalidDataException("Expected the accepted native run summary with four zero counters and SHA-256.");

        using var sourceStream = File.Open(source, FileMode.Open, FileAccess.Read, FileShare.Read);
        var registration = new Game231FixtureRegistration("full-battle.mt2f", sourceStream.Length, sha256.ToLowerInvariant(), Stage: false);
        bool cardFrontiers = options.Contains("--card-frontiers");
        bool endTurnFrontiers = options.Contains("--end-turn-frontiers");
        int plays, endTurns;
        using (var fixture = FixtureDocument.Read(registration.Verify(profile)))
        {
            FixtureValue battle = fixture.RootElement;
            if (Game231FixtureCatalog.Classify(registration, battle) != Game231FixtureKind.SettledBattle ||
                battle.GetProperty("GameVersion").GetString() != Game231FixtureCatalog.Version ||
                battle.GetProperty("NativeWon").GetBoolean() != summary.GetProperty("NativeWon").GetBoolean())
                throw new InvalidDataException("Expected a settled pinned native battle matching the accepted outcome.");

            plays = battle.GetProperty("Actions").GetArrayLength();
            endTurns = battle.GetProperty("Turns").GetArrayLength();
            RecordedBattleChecks.ReplayOnce(battle);
            if (cardFrontiers && plays > 0)
                BattleSearchChecks.RecordedFrontiers(battle, requireZeroUnsupported: true, report: false);
            if (endTurnFrontiers)
                BattleSearchChecks.RecordedEndTurnFrontiers(battle, choiceBudget: 64);
        }

        // Replay without the shared lock. Serialize the short two-file publish.
        CheckPath(workspace, lockPath);
        using var registrationLock = File.Open(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        CheckPath(workspace, manifest);
        if (Game231FixtureCatalog.Load(directory).Keys.Contains(archiveKey, StringComparer.OrdinalIgnoreCase))
            throw new IOException("Fixture is already registered: " + archiveKey);
        string[] rows = File.Exists(manifest) ? File.ReadAllLines(manifest) : [Header];
        string row = $"{segments[^1]}\t{registration.Bytes.ToString(CultureInfo.InvariantCulture)}\t{registration.Sha256}";
        string suffix = Guid.NewGuid().ToString("N");
        string temporaryArchive = Path.Combine(destinationDirectory, $".{suffix}.mt2f");
        string temporaryManifest = Path.Combine(destinationDirectory, $".{suffix}.tmp");
        try
        {
            File.Copy(source, temporaryArchive);
            (registration with { Path = Path.GetFileName(temporaryArchive) }).Verify(destinationDirectory);
            File.WriteAllLines(temporaryManifest, rows.Append(row));
            File.Move(temporaryArchive, destination);
            try { File.Move(temporaryManifest, manifest, overwrite: true); }
            catch
            {
                File.Delete(destination);
                throw;
            }
        }
        finally
        {
            File.Delete(temporaryArchive);
            File.Delete(temporaryManifest);
        }
        Console.WriteLine($"NATIVE-BATTLE-REGISTRATION PASS archive={archiveKey} bytes={registration.Bytes} sha256={registration.Sha256} " +
            $"plays={plays} endTurns={endTurns} cardFrontiers={cardFrontiers} endTurnFrontiers={endTurnFrontiers}");
    }

    private static bool SafeSegment(string segment) => Regex.IsMatch(segment, @"^[a-zA-Z0-9][a-zA-Z0-9._-]*\z") &&
        !segment.EndsWith('.');

    private static void CheckPath(string workspace, string path)
    {
        for (string? current = path; current != null && current != workspace; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Registration path contains a link: " + current);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }
}
