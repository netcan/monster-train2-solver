using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MagebladeChecks
{
    internal static void Native(FixtureValue root, bool required = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) && scenario.GetString() == "mageblade";
        Require(eligible || !required, "Fixture lacks the native Mageblade scenario.");
        if (!eligible) return;

        Require(root.GetProperty("Schema").GetInt32() == 127 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Policy").GetString() == "units-and-junk",
            "Native Mageblade battle schema, build, terminal state or policy changed.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Native Mageblade capture has nonzero " + field + ".");

        int witnesses = 0;
        foreach (FixtureValue stage in root.GetProperty("Stages").EnumerateArray())
        {
            FixtureValue actual = stage.GetProperty("Actual");
            if (actual.ValueKind != FixtureKind.Object || !actual.TryGetProperty("Context", out FixtureValue context) ||
                !context.TryGetProperty("MagicPower", out FixtureValue powers)) continue;
            int roomIndex = actual.GetProperty("RoomIndex").GetInt32();
            bool empowered = powers.EnumerateArray().Any(power =>
                power.GetProperty("RoomIndex").GetInt32() == roomIndex &&
                power.GetProperty("Team").GetInt32() == (int)CombatTeam.Player &&
                power.GetProperty("Value").GetInt32() > 0);
            if (!empowered) continue;

            foreach (FixtureValue unit in actual.GetProperty("Units").EnumerateArray())
            {
                bool mageblade = unit.GetProperty("Statuses").EnumerateArray().Any(status =>
                    status.GetProperty("Id").GetString() == "mageblade" && status.GetProperty("Stacks").GetInt32() > 0);
                FixtureValue coefficient = unit.GetProperty("Modifiers").GetProperty("MagebladeDamagePerMagicPower");
                if (mageblade && coefficient.ValueKind == FixtureKind.Number && coefficient.GetInt32() > 0) witnesses++;
            }
        }
        Require(witnesses > 0, "Native Mageblade capture lacks a live cached unit in a positive-magic-power room.");
        Console.WriteLine("MAGEBLADE-NATIVE-CHECK PASS: " + witnesses + " captured status/cache/room-power observations; all battle stages matched.");
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
