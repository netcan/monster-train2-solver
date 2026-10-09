using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardModifierOverflowChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Native overflow calibration changed state/RNG/frame or game identity.");
        var samples = root.GetProperty("Samples").EnumerateArray().Select(sample => (
            Base: sample.GetProperty("BaseValue").GetInt32(), Stat: sample.GetProperty("Stat").GetString()!,
            Floor: sample.GetProperty("EnforceFloor").GetBoolean(), Offset: sample.GetProperty("FirstOffset").GetBoolean(),
            Modifiers: sample.GetProperty("Modifiers").Deserialize<CardModifiers[]>()!,
            Actual: sample.GetProperty("Actual").Deserialize<int?>(), Exception: sample.GetProperty("NativeException").GetString())).ToArray();
        Require(samples.Length == 2240 && samples.Select(sample => sample.Stat).Distinct().Count() == 8 &&
            samples.Count(sample => sample.Exception == "System.OverflowException") == 896 &&
            samples.Count(sample => sample.Actual.HasValue) == 1344 && samples.Count(sample => sample.Offset) == 1120 &&
            samples.Count(sample => sample.Floor) == 1120, "Native modifier overflow scope differs.");
        string parent = JsonSerializer.Serialize(samples);
        void Check()
        {
            for (int i = 0; i < samples.Length; i++)
            {
                var sample = samples[i]; int? predicted = null; string? exceptionType = null;
                try { predicted = CardModifierModel.UpgradedStat(sample.Base, sample.Stat, sample.Floor, sample.Modifiers); }
                catch (OverflowException exception) { exceptionType = exception.GetType().FullName; }
                Require(predicted == sample.Actual && exceptionType == sample.Exception,
                    $"Native modifier overflow differs: sample{i}/{sample.Stat}, predicted={predicted}/{exceptionType}, actual={sample.Actual}/{sample.Exception}.");
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(samples) == parent, "Modifier overflow replay mutated its parents.");
        Console.WriteLine("NATIVE-MODIFIER-OVERFLOW-CHECKS PASS: 2240 original queries, 1344 numeric results/896 native minimum-integer exceptions, eight statistics, both floors/offset layouts and 32 immutable branches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
