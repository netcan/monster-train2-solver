using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using Newtonsoft.Json;

internal static class NativeCaptureBenchmark
{
    internal static void Run(string input, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        string binaryPath = Path.Combine(outputDirectory, "capture.mt2f"), jsonPath = Path.Combine(outputDirectory, "capture.json");
        if (File.Exists(binaryPath) || File.Exists(jsonPath)) throw new IOException("Benchmark output already exists.");
        var timer = Stopwatch.StartNew();
        using var fixture = FixtureDocument.Read(input);
        object snapshot = fixture.RootElement.ToObjectGraph()!;
        double inspection = timer.Elapsed.TotalMilliseconds;

        timer.Restart();
        using var direct = NativeFixtureCapture.Capture(snapshot);
        using (var file = File.Create(binaryPath)) direct.Write(file);
        double binaryExport = timer.Elapsed.TotalMilliseconds;
        using var decoded = FixtureDocument.Read(binaryPath);
        if (!fixture.RootElement.ContentEquals(decoded.RootElement))
            throw new InvalidDataException("Inspection reconstruction changed captured values; this fixture cannot be used for the export benchmark.");

        timer.Restart();
        using (var file = File.Create(jsonPath))
        using (var text = new StreamWriter(file, new UTF8Encoding(false), 65536))
        using (var json = new JsonTextWriter(text) { Formatting = Formatting.None })
            JsonSerializer.CreateDefault().Serialize(json, snapshot);
        double jsonExport = timer.Elapsed.TotalMilliseconds;

        timer.Restart();
        using var legacy = LegacyFixtureImport.Read(jsonPath);
        double jsonImport = timer.Elapsed.TotalMilliseconds;
        if (!legacy.RootElement.ContentEquals(decoded.RootElement)) throw new InvalidDataException("Binary and diagnostic JSON capture values differ.");
        Console.WriteLine("OFFLINE-CAPTURE-BENCHMARK PASS: every captured value/order/number lexeme verified; " + RuntimeInformation.FrameworkDescription + "; not a Unity battle timing.");
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
            "inspectionMs={0:F3} binaryExportMs={1:F3} jsonExportMs={2:F3} jsonImportMs={3:F3} binaryBytes={4} jsonBytes={5} uniqueNodes={6}",
            inspection, binaryExport, jsonExport, jsonImport, new FileInfo(binaryPath).Length, new FileInfo(jsonPath).Length, direct.UniqueNodeCount));
    }
}
