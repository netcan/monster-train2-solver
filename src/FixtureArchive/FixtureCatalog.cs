using System.Collections.Concurrent;

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
