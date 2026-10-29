using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MushyHeavyHealthChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 41 && before.Spawn.Turn == 9 && actual.Spawn.Turn == 10, "Missing native heavy boundary.");
        Require(before.Spawn.Train.Rooms[2].Units.Count(unit => unit.Id is 33 or 34 && unit.AssetKey == "HeavyT2_Basic" && unit.Health == 120) == 2,
            "Missing native heavy pair.");
        Require(!actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id is 33 or 34), "Native heavy pair did not leave.");
        RoomCapacityAttachmentState attachment = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single();
        foreach (int id in new[] { 8, 33 })
        {
            CombatUnit source = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            JsonNode unit = JsonSerializer.SerializeToNode(source, ModelJson.Options)!;
            bool Accept()
            {
                CombatUnit candidate = unit.Deserialize<CombatUnit>(ModelJson.Options)!;
                bool exit = RoomAttachmentUnitModel.OriginalMushyExit(attachment, candidate);
                if (id == 8) Require(exit == RoomAttachmentUnitModel.OriginalMushyEntry(attachment, candidate), "Boss entry/exit signatures differ.");
                return exit;
            }
            foreach (int health in new[] { 1, source.Health, source.MaxHealth })
            {
                unit["Health"] = health;
                Require(Accept(), "Valid native health rejected for " + id);
            }
            foreach (int health in new[] { 0, source.MaxHealth + 1 })
            {
                unit["Health"] = health;
                Require(!Accept(), "Invalid health accepted for " + id);
            }
            unit["Health"] = source.Health;
            unit["SpawnerCardId"] = 1;
            Require(!Accept(), "Unknown spawning card accepted for " + id);
            unit["SpawnerCardId"] = 0;
            unit["BaseAttack"] = source.BaseAttack + 1;
            Require(!Accept(), "Unknown attack signature accepted for " + id);
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent heavy movement differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Heavy movement mutated its parent.");
        Console.WriteLine("MUSHY-HEAVY-HEALTH PASS: 41 native inputs, complete successor/RNG, signature controls and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
