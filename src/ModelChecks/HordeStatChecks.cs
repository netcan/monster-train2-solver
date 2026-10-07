using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HordeStatChecks
{
    internal static void Run()
    {
        var definition = new HordeBaseStats(3, 7);
        var root = new HordeStats(17, 15, 21);
        string parent = JsonSerializer.Serialize(root);
        Require(Equals(HordeStatModel.Change(root, definition, 3, 3), new(9, 21, 21)), "First Horde addition retained existing attack or HP.");
        Require(Equals(HordeStatModel.Change(root, definition, 2, 5), new(23, 29, 35)), "Later Horde addition replaced existing stats.");
        Require(Equals(HordeStatModel.Change(root, definition, -2, 3), new(11, 15, 21)), "Horde removal double-subtracted already damaged HP.");
        Require(Equals(HordeStatModel.Change(root, definition, -2, 2), new(11, 1, 7)), "Explicit Horde removal did not subtract unmatched HP/max HP buckets.");
        Require(Equals(HordeStatModel.Change(new(3, 99998, 99999), definition, 1, 2), new(6, 99999, 99999)), "Native HP caps changed.");
        Require(Equals(HordeStatModel.Change(root, new(int.MaxValue, int.MaxValue), 2, 2), new(-2, -2, -2)), "Horde multiplication stopped wrapping or lower-clamped growth.");
        var edge = HordeStatModel.Casualties(new(9, 14, 21), definition, 3);
        Require(edge.ShouldRemove && edge.RemovalCount == 1, "Exact troop HP boundary missed its casualty.");
        var above = HordeStatModel.Casualties(new(9, 15, 21), definition, 3);
        var dead = HordeStatModel.Casualties(new(9, 0, 21), definition, 3);
        Require(!above.ShouldRemove && above.RemovalCount == 0 && dead.ShouldRemove && dead.RemovalCount == 2,
            "Horde casualty rounding or final physical unit retention differs.");
        Parallel.For(0, 32, _ => Require(Equals(HordeStatModel.Change(root, definition, -2, 3), new(11, 15, 21)), "Horde branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Horde numerical step mutated its parent.");
        Console.WriteLine("HORDE-STAT-CHECKS PASS: first/later additions, separate HP buckets, signed overflow/caps, casualty thresholds and 32 branches.");
    }

    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var fixture = document.RootElement;
        Require(fixture.GetProperty("LiveContextUnchanged").GetBoolean() && fixture.GetProperty("Mismatches").GetInt32() == 0,
            "Native Horde calibration differs or changed live state.");
        var samples = fixture.GetProperty("Samples").EnumerateArray().ToArray();
        var casualties = fixture.GetProperty("Casualties").EnumerateArray().ToArray();
        Require(samples.Length == 560 && casualties.Length == 175, "Incomplete native Horde numerical matrix.");
        foreach (var sample in samples) VerifyChange(sample);
        foreach (var sample in casualties) VerifyCasualties(sample);
        Require(samples.Any(sample => sample.GetProperty("After").Deserialize<HordeStats>()!.Health < 0) &&
            samples.Any(sample => sample.GetProperty("After").Deserialize<HordeStats>()!.Health == 99999) &&
            samples.Any(sample => sample.GetProperty("After").Deserialize<HordeStats>() is { } stats && stats.Health > stats.MaxHealth) &&
            samples.Any(sample => sample.GetProperty("After").Deserialize<HordeStats>()!.Attack < 0), "Native caps and signed overflow were not observed.");
        Require(casualties.Any(sample => sample.GetProperty("RequestedStacks").GetInt32() == int.MaxValue &&
            sample.GetProperty("Stacks").GetInt32() == 9999) &&
            casualties.Any(sample => sample.GetProperty("Before").Deserialize<HordeStats>()!.Health == 0 &&
                sample.GetProperty("After").Deserialize<HordeCasualties>()!.RemovalCount > 0) &&
            casualties.Any(sample => sample.GetProperty("After").Deserialize<HordeCasualties>() is { ShouldRemove: false, RemovalCount: > 0 }),
            "Native stack cap, lethal casualty boundary or separate threshold gate missing.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) VerifyChange(sample); foreach (var sample in casualties) VerifyCasualties(sample); });
        Console.WriteLine($"NATIVE-HORDE-STAT-CHECKS PASS: {samples.Length} exact numerical transitions, {casualties.Length} casualty boundaries, live restoration and 32 branches.");
    }

    private static void VerifyChange(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<HordeStats>()!;
        string parent = JsonSerializer.Serialize(before);
        var result = HordeStatModel.Change(before, sample.GetProperty("Definition").Deserialize<HordeBaseStats>()!,
            sample.GetProperty("Delta").GetInt32(), sample.GetProperty("Total").GetInt32());
        Require(Equals(result, sample.GetProperty("After").Deserialize<HordeStats>()!), "Native Horde stat transition differs.");
        Require(JsonSerializer.Serialize(before) == parent, "Native Horde numerical branch mutated its root.");
    }
    private static void VerifyCasualties(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<HordeStats>()!;
        var result = HordeStatModel.Casualties(before, sample.GetProperty("Definition").Deserialize<HordeBaseStats>()!, sample.GetProperty("Stacks").GetInt32());
        var actual = sample.GetProperty("After").Deserialize<HordeCasualties>()!;
        Require(result.ShouldRemove == actual.ShouldRemove && result.RemovalCount == actual.RemovalCount, "Native Horde casualty threshold/count differs.");
    }
    private static bool Equals(HordeStats left, HordeStats right) => left.Attack == right.Attack && left.Health == right.Health && left.MaxHealth == right.MaxHealth;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
