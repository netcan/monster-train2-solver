using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MagebladeChecks
{
    private const string OriginalMagnitudeOwnerId = "622860d3-b2ca-45b0-991a-b7745e1dcd5c";

    internal static void Native(FixtureValue root, bool required = false)
    {
        string scenario = root.TryGetProperty("ModifierScenario", out var scenarioValue) ? scenarioValue.GetString() ?? "" : "";
        bool originalMagnitude = scenario == "original-mageblade-magnitude";
        bool eligible = scenario == "mageblade" || originalMagnitude;
        Require(eligible || !required, "Fixture lacks the native Mageblade scenario.");
        if (!eligible) return;

        Require(root.GetProperty("Schema").GetInt32() == (originalMagnitude ? 143 : 127) &&
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
        if (originalMagnitude)
        {
            Require(root.GetProperty("Actions").EnumerateArray().Any(action =>
                    action.GetProperty("Actual").GetProperty("Spawn").GetProperty("Train").GetProperty("Rooms")
                        .EnumerateArray().SelectMany(room => room.GetProperty("Units").EnumerateArray())
                        .Any(unit => unit.GetProperty("Statuses").EnumerateArray().Any(status =>
                                status.GetProperty("Id").GetString() == "mageblade" && status.GetProperty("Stacks").GetInt32() > 0) &&
                            unit.GetProperty("Modifiers").GetProperty("MagebladeDamagePerMagicPower").GetInt32() == 4)),
                "Original DoubleMagebladeDamage did not produce a four-point cached coefficient from two starting stacks.");
            CheckOriginalMagnitude(root);
        }
        Console.WriteLine("MAGEBLADE-NATIVE-CHECK PASS: " + witnesses + " captured status/cache/room-power observations; all battle stages matched.");
    }

    private static void CheckOriginalMagnitude(FixtureValue root)
    {
        Query[] rows = root.GetProperty("RelicStatusMagnitudeQueries").EnumerateArray().Select(Read).ToArray();
        Require(rows.Length > 0, "Original Mageblade magnitude queries are missing.");
        foreach (Query row in rows) CheckDefinition(row.Relics);
        string parent = Serialize(rows);
        void CheckRows()
        {
            foreach (Query row in rows)
            {
                CombatContext context = row.Context?.WithRelics(row.Relics) ??
                    new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10, relics: row.Relics);
                StatusScalingResult result = RelicStatusMagnitudeModel.GetModifier(context, row.StatusId, row.NativeTeam);
                Require(result.Supported && result.Stacks == row.Actual,
                    "Original Mageblade magnitude query differs: " + Label(row) + " " + result.UnsupportedReason);
                CombatContext withoutOwner = context.WithRelics(context.Relics!.Where(relic => relic.DataId != OriginalMagnitudeOwnerId).ToArray());
                StatusScalingResult ablated = RelicStatusMagnitudeModel.GetModifier(withoutOwner, row.StatusId, row.NativeTeam);
                Require(ablated.Supported && ablated.Stacks == (row.StatusId == "mageblade" && row.NativeTeam != 1 ? 0 : row.Actual),
                    "Original DoubleMagebladeDamage owner ablation differs: " + Label(row));
            }
        }
        CheckRows(); Parallel.For(0, 32, _ => CheckRows());
        Require(Serialize(rows) == parent, "Parallel Mageblade magnitude checks mutated native query rows.");
        Require(rows.Any(row => !row.Matrix && !row.Preview && row.StatusId == "mageblade" && row.NativeTeam == 2 && row.Actual == 1),
            "Original DoubleMagebladeDamage lacks a real positive Monsters-team magnitude query.");
        foreach (int team in new[] { 0, 1, 2, 255 })
        {
            Query[] matrix = rows.Where(row => row.Matrix && row.StatusId == "mageblade" && row.NativeTeam == team).ToArray();
            int expected = team == 1 ? 0 : 1;
            Require(matrix.Length > 0 && matrix.All(row => row.Actual == expected),
                "Original Mageblade magnitude team gate differs for native team " + team + ".");
        }
        Console.WriteLine("ORIGINAL-DOUBLE-MAGEBLADE-MAGNITUDE PASS: native positive query, exact owner ablation, all four team masks and 32 immutable branches.");
    }

    private static void CheckDefinition(IReadOnlyList<CombatRelicState> relics)
    {
        CombatRelicState[] owners = relics.Where(relic => relic.DataId == OriginalMagnitudeOwnerId).ToArray();
        Require(owners.Length == 1, "Missing or duplicated original DoubleMagebladeDamage owner.");
        CombatRelicState owner = owners[0];
        Require(owner.AssetKey == "DoubleMagebladeDamage" && owner.EffectTypes.SequenceEqual([RelicModel.ModifyStatusMagnitude]) &&
            owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false && owner.StatusMagnitudes?.Count == 1,
            "Original DoubleMagebladeDamage identity or effect type changed.");
        RelicStatusMagnitude rule = owner.StatusMagnitudes![0];
        Require(rule.EffectIndex == 0 && rule.SourceTeam == 2 && rule.StatusId == "mageblade" &&
            rule.Modifier == 1 && rule.Conditions.Count == 0,
            "Original DoubleMagebladeDamage payload changed.");
    }

    private sealed record Query(string StatusId, int NativeTeam, int Actual, bool Matrix, bool Preview,
        CombatRelicState[] Relics, CombatContext? Context);
    private static Query Read(FixtureValue row) => new(row.GetProperty("StatusId").GetString()!, row.GetProperty("NativeTeam").GetInt32(),
        row.GetProperty("Actual").GetInt32(), row.GetProperty("Matrix").GetBoolean(), row.GetProperty("Preview").GetBoolean(),
        row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!,
        row.TryGetProperty("Context", out var context) && context.ValueKind != FixtureKind.Null ? context.Deserialize<CombatContext>() : null);
    private static string Label(Query row) => row.StatusId + "/" + row.NativeTeam + "/matrix=" + row.Matrix + "/preview=" + row.Preview;
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
