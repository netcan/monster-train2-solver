using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SecondFloorEnemyOpeningChecks
{
    private const string NativeSha = "41EF1F7C778A4E388B4B8E30B505DCE3297639E2BE246741DC0D8A9A3A7F76EB";
    private const string PriorUnsupported = "Unmodeled relic effect RelicEffectChangeHeroSpawnRoom on Echoes_EnemyUnitsStartAtSecondFloor.";

    internal static void Native(string path)
    {
        bool historical = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == NativeSha;
        using var archive = ModelJson.ReadFixture(path);
        FixtureValue root = archive.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalSecondFloorEnemyOpening" &&
            root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == RelicSecondFloorEnemyOpeningModel.OwnerId &&
            root.GetProperty("StarterRoomIndex").GetInt32() == 1, "Wrong original second-floor opening witness.");
        var phases = root.GetProperty("Phases").EnumerateArray().ToArray();
        Require(phases.Select(row => row.GetProperty("Name").GetString()).SequenceEqual(
            ["InitialSpawn", "HeroTurn", "HeroEndOfTurn", "StartOfBattleRelics"]) &&
            phases.Select(row => row.GetProperty("Before").GetProperty("TurnModifier").GetInt32()).SequenceEqual([0, 0, 0, 1]) &&
            phases.Select(row => row.GetProperty("Actual").GetProperty("TurnModifier").GetInt32()).SequenceEqual([0, 0, 0, 1]) &&
            root.GetProperty("FirstDecisionTurnModifier").GetInt32() == 1, "Wrong original opening order or displayed turn offset.");

        BattleTurnState source = Read(root.GetProperty("Deployment"));
        BattleTurnState actual = Read(root.GetProperty("FirstDecision"));
        Require(source.Spawn is { Turn: 0, Phase: 0, TreasuresRemaining: 1 } &&
            source.Spawn.Train.Context?.Relics?.Count(RelicSecondFloorEnemyOpeningModel.IsOriginal) == 1 &&
            actual.Spawn is { Turn: 1, Phase: 2, TreasuresRemaining: 0 }, "Wrong original deployment and first decision.");
        string original = JsonSerializer.Serialize(source, ModelJson.Options);
        BattleTurnResult result = BattleTurnModel.EndTurn(source);
        Require(result.Supported && result.Outcome == RoomOutcome.Exchanged,
            result.UnsupportedReason ?? "Second-floor opening did not finish the first native decision.");
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual));
        Require(difference == null, "First native decision differs " + difference);
        Require(JsonSerializer.Serialize(source, ModelJson.Options) == original, "Opening replay mutated deployment.");
        Console.WriteLine("NATIVE-SECOND-FLOOR-ENEMY-OPENING PASS: first decision, two waves, collector and gameplay RNG.");

        BattleTurnState Read(FixtureValue value)
        {
            BattleTurnState state = value.Deserialize<BattleTurnState>()!;
            if (!historical) return state;
            // This immutable first receipt predates admission of the exact mutator.
            // Leave every captured logic field intact while removing its prior diagnostic.
            JsonNode captured = JsonSerializer.SerializeToNode(state, ModelJson.Options)!;
            RemovePriorMarkers(captured);
            return captured.Deserialize<BattleTurnState>(ModelJson.Options)!;
        }
    }

    private static void RemovePriorMarkers(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            if (obj["ExternalInteractions"] is JsonArray interactions)
                for (int index = interactions.Count - 1; index >= 0; index--)
                    if (interactions[index]?.GetValue<string>() is PriorUnsupported or "Mutator effects" or "Spawn mutators" or "Mutators")
                        interactions.RemoveAt(index);
            foreach (var child in obj) if (child.Value != null) RemovePriorMarkers(child.Value);
        }
        else if (node is JsonArray array)
            foreach (JsonNode? child in array) if (child != null) RemovePriorMarkers(child);
    }

    private static void Require(bool okay, string message)
    {
        if (!okay) throw new InvalidOperationException(message);
    }
}
