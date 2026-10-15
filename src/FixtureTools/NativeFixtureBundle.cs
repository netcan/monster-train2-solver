using System.Security.Cryptography;
using MonsterTrain2Poju.Fixtures;

internal static class NativeFixtureBundle
{
    internal static void Pack(string baselinePath, string ownedPath, string outputPath)
    {
        if (File.Exists(outputPath)) throw new IOException("Bundle output already exists.");
        using var baseline = FixtureDocument.Read(baselinePath); using var owned = FixtureDocument.Read(ownedPath);
        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children: [builder.Add(FixtureKind.String, "Schema"), builder.Add(FixtureKind.Number, "1"),
            builder.Add(FixtureKind.String, "Baseline"), builder.Import(baseline.RootElement), builder.Add(FixtureKind.String, "Owned"), builder.Import(owned.RootElement)]);
        using var bundle = builder.Build(root, 0, new byte[32]);
        using (var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)) bundle.Write(stream);
        using var verified = FixtureDocument.Read(outputPath);
        if (!verified.RootElement.GetProperty("Baseline").ContentEquals(baseline.RootElement) || !verified.RootElement.GetProperty("Owned").ContentEquals(owned.RootElement))
            throw new InvalidDataException("Bundling changed an input value.");
        Console.WriteLine($"NATIVE-BUNDLE PASS: both complete input graphs preserved; {new FileInfo(outputPath).Length} bytes, {bundle.UniqueNodeCount} nodes, SHA256 {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(outputPath))).ToLowerInvariant()}");
    }
}
