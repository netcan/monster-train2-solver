using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;

internal static class AdviceBudgetFailure
{
    internal static void Extract(string source, string output)
    {
        if (File.Exists(output)) throw new IOException("Diagnostic destination already exists: " + output);
        using var battle = FixtureDocument.Read(source);
        FixtureValue player = battle.RootElement.GetProperty("PlayerAdvisor");
        FixtureValue overlay = player.GetProperty("Overlay");
        string error = overlay.GetProperty("Error").GetString()!;
        if (player.GetProperty("Assembly").GetString() != "MonsterTrain2Poju.Advisor" ||
            !System.Text.RegularExpressions.Regex.IsMatch(error, @"\bturns=0 nodes=[1-9]\d*\b") ||
            overlay.GetProperty("UnavailableRoot").ValueKind != FixtureKind.Object ||
            overlay.GetProperty("PurityFailures").GetInt32() != 0 || overlay.GetProperty("Captures").GetInt32() <= 0)
            throw new InvalidOperationException("Missing original zero-turn player budget failure.");
        FixtureValue unavailable = overlay.GetProperty("UnavailableRoot");
        FixtureValue decisions = battle.RootElement.GetProperty("RollingDecisions");
        FixtureValue deployment = decisions.GetArrayLength() > 0 ? decisions[0].GetProperty("Root") : unavailable;
        if (deployment.GetProperty("Spawn").GetProperty("Turn").GetInt32() != 0)
            throw new InvalidOperationException("The trace has no recorded deployment root.");
        using var document = NativeFixtureCapture.Capture(new {
            Schema = 1, EvidenceKind = "NativePlayerBudgetFailure", SourceRunCompleted = false,
            SourceError = error, PureCaptures = overlay.GetProperty("Captures").GetInt32(),
            SourceTraceSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))).ToLowerInvariant(),
            Root = unavailable.ToObjectGraph(),
            DeploymentRoot = deployment.ToObjectGraph()
        });
        using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write)) document.Write(stream);
        Console.WriteLine("ADVICE-BUDGET-DIAGNOSTIC: exact native unavailable root extracted; source battle remains unaccepted.");
    }
}
