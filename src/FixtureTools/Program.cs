using System.Diagnostics;
using System.Security.Cryptography;
using MonsterTrain2Poju.Fixtures;

if (args.Length == 3 && args[0] == "pack")
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
else if (args.Length == 3 && args[0] == "pack-directory")
{
    string sourceDirectory = Path.GetFullPath(args[1]), outputDirectory = Path.GetFullPath(args[2]);
    Directory.CreateDirectory(outputDirectory);
    var sources = Directory.GetFiles(sourceDirectory).Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".json.gz", StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal).ToArray();
    if (sources.Length == 0) throw new InvalidOperationException("No legacy fixture inputs found.");
    var manifest = new List<string> { "archive\tsource\tsource_bytes\tsource_sha256\tarchive_bytes\tarchive_sha256\tunique_nodes" };
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
    Console.Error.WriteLine("Usage: FixtureTools pack <legacy.json[.gz]> <output.mt2f> | verify <legacy> <archive> | pack-directory <source-dir> <output-dir> | compare-speeds <normal.mt2f> <accelerated.mt2f>");
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
    return $"{Path.GetFileName(archive)}\t{Path.GetFileName(source)}\t{imported.SourceLength}\t{imported.SourceSha256}\t{archiveLength}\t{archiveHash}\t{imported.UniqueNodeCount}";
}
