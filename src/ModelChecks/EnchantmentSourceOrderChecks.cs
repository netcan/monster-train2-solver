using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnchantmentSourceOrderChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameModuleMvid").GetString() ==
            "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" && root.GetProperty("Boundary").GetString() == "NativeManagerListCollection" &&
            !root.GetProperty("GameplaySuppressed").GetBoolean(), "Native source-order provenance gate failed.");
        Require(samples.Count(sample => sample.GetProperty("Label").GetString()!.StartsWith("sort-stress-", StringComparison.Ordinal)) == 32 &&
            samples.Any(sample => sample.GetProperty("Initial").GetArrayLength() == 64), "Native sort threshold/equal-slot matrix missing.");
        Require(samples.Any(sample => sample.GetProperty("Positions").Deserialize<EnchantmentSourcePosition[]>()!
            .Select(position => position.Team).Distinct().Count() == 2), "Native both-team source order missing.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-ENCHANTMENT-SOURCE-ORDER-CHECKS PASS: {samples.Length} native manager lists, 32 equal-index/size-threshold cases, " +
            "both teams and cross-floor actors, repeated empty-room sorting and 32 independent branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        var positions = sample.GetProperty("Positions").Deserialize<EnchantmentSourcePosition[]>()!;
        var initial = sample.GetProperty("Initial").Deserialize<EnchantmentSourcePosition[]>()!;
        string parent = JsonSerializer.Serialize(new { positions, initial });
        int[] actual = EnchantmentSourceOrderModel.Collect(sample.GetProperty("RoomCount").GetInt32(), positions, initial);
        Require(actual.SequenceEqual(sample.GetProperty("Actual").Deserialize<int[]>()!),
            sample.GetProperty("Label").GetString() + ": native manager source order differs: " + string.Join(",", actual) + " != " +
                string.Join(",", sample.GetProperty("Actual").Deserialize<int[]>()!));
        Require(JsonSerializer.Serialize(new { positions, initial }) == parent, "Source-order parent mutated.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
}
