using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;

namespace MonsterTrain2Poju.Fixtures;

// Test/tool metadata only; excluded from the game's netstandard payload.
public sealed class FixtureCatalog
{
    public const string Header = "archive\tsource\tsource_bytes\tsource_sha256\tarchive_bytes\tarchive_sha256\tunique_nodes\tpath";
    private static readonly ConcurrentDictionary<string, FixtureCatalog> Catalogs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> paths = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, string> Paths => paths;

    public FixtureCatalog(string directory)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        string[] lines = File.ReadAllLines(Path.Combine(directory, "manifest.tsv"));
        if (lines.Length == 0 || lines[0] != Header) throw new InvalidDataException("Unknown fixture manifest schema.");
        var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in lines.Skip(1))
        {
            string[] row = line.Split('\t');
            if (row.Length != 8 || Path.GetFileName(row[0]) != row[0] ||
                row[7].Contains('\\') || row[7].Split('/').Any(part => part is "" or "." or "..") ||
                Path.GetFileName(row[7]) != row[0]) throw new InvalidDataException("Invalid fixture manifest row.");
            string path = Path.GetFullPath(Path.Combine(directory, row[7]));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !locations.Add(path) || !paths.TryAdd(row[0], path))
                throw new InvalidDataException("Duplicate or escaped fixture path: " + row[0]);
        }
    }

    public string Resolve(string name) => paths.TryGetValue(name, out string? path) ? path :
        throw new FileNotFoundException("Unknown curated fixture: " + name);

    public static string Related(string source, string name)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(source))!;
        for (DirectoryInfo? current = new(directory); current != null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "manifest.tsv")))
                return Catalogs.GetOrAdd(current.FullName, static root => new FixtureCatalog(root)).Resolve(name);
        // An isolated native run has no curated manifest; its sidecars belong
        // to that run's own directory.
        return Path.Combine(directory, name);
    }
}

public enum Game231FixtureKind { Legacy, Component, UnsettledBattle, SettledBattle }

public sealed record Game231FixtureRegistration(string Path, long Bytes, string Sha256, bool Stage, bool Revalidated = false)
{
    public string Verify(string directory)
    {
        string fullPath = System.IO.Path.Combine(directory, Path);
        using var stream = File.OpenRead(fullPath);
        if (stream.Length != Bytes || !Convert.ToHexString(SHA256.HashData(stream)).Equals(Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Archive integrity failed: " + Path);
        return fullPath;
    }
}

public static class Game231FixtureCatalog
{
    public const string Version = "2.3.1";
    public const string Module = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
    public const string RevalidatedManifest = "revalidated.tsv";
    private const string LegacyModule = "8fb07b96-f4db-4d2b-884d-c00536d6ccf4";
    private static readonly string[] BattleCounters = ["CaptureFailures", "Mismatches", "Unsupported", "Pending"];

    public static string DirectoryFor(string workspace) => Path.Combine(Path.GetFullPath(workspace), "tests", "compatibility", "game-" + Version);

    public static IReadOnlyDictionary<string, Game231FixtureRegistration> Load(string directory)
    {
        var archives = new SortedDictionary<string, Game231FixtureRegistration>(StringComparer.Ordinal);
        string stagePath = Path.Combine(directory, "solver-stage.tsv");
        var manifests = Directory.EnumerateFiles(directory, "manifest.tsv", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(directory, RevalidatedManifest, SearchOption.AllDirectories)).Append(stagePath);
        foreach (string manifest in manifests)
        {
            bool stage = manifest == stagePath;
            bool revalidated = Path.GetFileName(manifest) == RevalidatedManifest;
            string[] lines = File.ReadAllLines(manifest);
            string header = stage ? "case\tarchive\tbytes\tsha256" : "path\tbytes\tsha256";
            if (lines.Length < 2 || lines[0].Replace("\"", "", StringComparison.Ordinal) != header)
                throw new InvalidDataException("Unexpected archive manifest header: " + manifest);
            int offset = stage ? 1 : 0;
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in lines.Skip(1))
            {
                string[] cells = line.Split('\t').Select(cell => cell.Trim('"')).ToArray();
                if (cells.Length != 3 + offset) throw new InvalidDataException("Invalid archive manifest row: " + manifest);
                string relative = cells[offset];
                string sha256 = cells[offset + 2].ToLowerInvariant();
                long bytes = long.Parse(cells[offset + 1], NumberStyles.None, CultureInfo.InvariantCulture);
                if (bytes <= 0 || sha256.Length != 64 || !sha256.All(Uri.IsHexDigit) ||
                    !relative.EndsWith(".mt2f", StringComparison.Ordinal) || Path.IsPathRooted(relative) ||
                    relative.Contains('\\') || relative.Contains(':') ||
                    relative.Split('/').Any(part => part is "" or "." or "..") || !listed.Add(relative))
                    throw new InvalidDataException("Invalid or duplicate archive path: " + relative);
                string path = Path.Combine(Path.GetDirectoryName(manifest)!, relative);
                string key = Path.GetRelativePath(directory, path).Replace('\\', '/');
                if (archives.TryGetValue(key, out Game231FixtureRegistration? prior))
                {
                    if (prior.Bytes != bytes || prior.Sha256 != sha256 || prior.Revalidated || revalidated)
                        throw new InvalidDataException("Conflicting archive registrations: " + key);
                    archives[key] = prior with { Stage = stage || prior.Stage };
                    continue;
                }
                archives.Add(key, new Game231FixtureRegistration(key, bytes, sha256, stage, revalidated));
            }
        }
        return archives;
    }

    public static Game231FixtureKind Classify(Game231FixtureRegistration archive, FixtureValue root)
    {
        bool legacy = archive.Path.StartsWith("2.2.1/", StringComparison.Ordinal);
        if ((legacy && (archive.Stage || archive.Revalidated)) || !root.TryGetProperty("GameModuleMvid", out FixtureValue module) ||
            module.ValueKind != FixtureKind.String || module.GetString() != (legacy ? LegacyModule : Module))
            throw new InvalidDataException("Unexpected fixture module: " + archive.Path);
        if (legacy) return Game231FixtureKind.Legacy;
        bool hasActions = root.TryGetProperty("Actions", out FixtureValue actions) && actions.ValueKind == FixtureKind.Array;
        bool settled = hasActions && root.TryGetProperty("Turns", out FixtureValue turns) && turns.ValueKind == FixtureKind.Array &&
            root.TryGetProperty("TerminalEffectsSettled", out FixtureValue terminal) && terminal.ValueKind == FixtureKind.True &&
            BattleCounters.All(key => root.TryGetProperty(key, out FixtureValue counter) && counter.ValueKind == FixtureKind.Number && counter.GetInt32() == 0);
        if (archive.Revalidated)
        {
            if (!HasCompleteRevalidatedCapture(root))
                throw new InvalidDataException("Incomplete original capture for offline revalidation: " + archive.Path);
            return Game231FixtureKind.SettledBattle;
        }
        if (archive.Stage && (!settled || !root.TryGetProperty("NativeWon", out FixtureValue won) || won.ValueKind != FixtureKind.True))
            throw new InvalidDataException("Stage archive failed its native battle gate: " + archive.Path);
        if (settled) return Game231FixtureKind.SettledBattle;
        return hasActions ? Game231FixtureKind.UnsettledBattle : Game231FixtureKind.Component;
    }

    private static bool HasCompleteRevalidatedCapture(FixtureValue root) =>
        root.TryGetProperty("GameVersion", out var version) && version.ValueKind == FixtureKind.String && version.GetString() == Version &&
        root.TryGetProperty("TerminalEffectsSettled", out var terminal) && terminal.ValueKind == FixtureKind.True &&
        root.TryGetProperty("TerminalCaptureBoundary", out var boundary) && boundary.ValueKind == FixtureKind.String &&
        boundary.GetString() == "AfterStopCombatLoop" &&
        root.TryGetProperty("NativeWon", out var outcome) && outcome.ValueKind is FixtureKind.True or FixtureKind.False &&
        root.TryGetProperty("Actions", out var actions) && actions.ValueKind == FixtureKind.Array &&
        root.TryGetProperty("Turns", out var turns) && turns.ValueKind == FixtureKind.Array &&
        actions.GetArrayLength() + turns.GetArrayLength() > 0 &&
        root.TryGetProperty("CaptureFailures", out var captureFailures) && captureFailures.ValueKind == FixtureKind.Number && captureFailures.GetInt32() == 0 &&
        root.TryGetProperty("Pending", out var pending) && pending.ValueKind == FixtureKind.Number && pending.GetInt32() == 0;
}
