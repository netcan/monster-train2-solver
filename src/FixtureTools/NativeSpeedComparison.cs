using MonsterTrain2Poju.Fixtures;

internal static class NativeSpeedComparison
{
    internal static void Run(string normalPath, string acceleratedPath)
    {
        using var normal = FixtureDocument.Read(normalPath);
        using var accelerated = FixtureDocument.Read(acceleratedPath);
        var left = normal.RootElement; var right = accelerated.RootElement;
        if (left.TryGetProperty("RequestedGameSpeed", out var normalSpeed) && normalSpeed.GetString() != "Normal")
            throw new InvalidDataException("Reference capture must use Normal game speed.");
        if (!right.TryGetProperty("RequestedGameSpeed", out var fastSpeed) ||
            !new[] { "Fast", "Ultra", "SuperUltra", "Instant" }.Contains(fastSpeed.GetString()) ||
            right.GetProperty("LiveGameSpeedOverrides").GetInt32() <= 0)
            throw new InvalidDataException("Accelerated capture did not apply a native game speed override.");
        // UI queries/preview scopes may occur a different number of times per frame.
        // Every captured gameplay section, checkpoint and RNG stream remains compared.
        var observations = new HashSet<string>(StringComparer.Ordinal)
        { "Schema", "RequestedGameSpeed", "LiveGameSpeedOverrides", "UiRngIsolation", "PreviewRngIsolation" };
        var original = left.EnumerateObject().Where(p => !observations.Contains(p.Name)).ToArray();
        var fast = right.EnumerateObject().Where(p => !observations.Contains(p.Name)).ToArray();
        if (!original.Select(p => p.Name).SequenceEqual(fast.Select(p => p.Name)))
            throw new InvalidDataException("Gameplay capture fields differ.");
        foreach (var property in original)
        {
            var value = right.GetProperty(property.Name);
            string? difference = Difference(property.Value, value, "$." + property.Name);
            if (difference != null) throw new InvalidDataException(difference);
        }
        foreach (var input in new[] { left, right })
        {
            if (input.GetProperty("CaptureFailures").GetInt32() != 0 || input.GetProperty("Mismatches").GetInt32() != 0 ||
                input.GetProperty("Unsupported").GetInt32() != 0 || input.GetProperty("Pending").GetInt32() != 0 ||
                !input.GetProperty("TerminalEffectsSettled").GetBoolean()) throw new InvalidDataException("Native capture did not settle cleanly.");
        }
        Console.WriteLine($"NATIVE-SPEED-EQUIVALENCE PASS: {original.Length} complete gameplay sections including all states, RNG streams, effects, callback order and checkpoints; {left.GetProperty("Actions").GetArrayLength()} plays, {left.GetProperty("Turns").GetArrayLength()} turns. Frame-dependent UI observations are checked separately by model regression.");
    }
    private static string? Difference(FixtureValue left, FixtureValue right, string path)
    {
        if (left.ContentEquals(right)) return null;
        if (left.ValueKind != right.ValueKind) return path + ": kind differs.";
        if (left.ValueKind == FixtureKind.Array)
        {
            if (left.GetArrayLength() != right.GetArrayLength()) return path + ": array length differs.";
            for (int i = 0; i < left.GetArrayLength(); i++)
            {
                string? error = Difference(left[i], right[i], path + "[" + i + "]");
                if (error != null) return error;
            }
        }
        else if (left.ValueKind == FixtureKind.Object)
        {
            var original = left.EnumerateObject().ToArray(); var fast = right.EnumerateObject().ToArray();
            if (!original.Select(p => p.Name).SequenceEqual(fast.Select(p => p.Name))) return path + ": object properties differ.";
            for (int i = 0; i < original.Length; i++)
            {
                string? error = Difference(original[i].Value, fast[i].Value, path + "." + original[i].Name);
                if (error != null) return error;
            }
        }
        return path + ": scalar differs.";
    }
}
