using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OccupiedPreviewBirthChecks
{
    private const string Reason = "Battle preview floor 1: Invalid retained physical identities or missing shared counter: next=25, retained=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(rows.Length == 28 && before.Spawn.Turn == 5 && action.CardInstanceId == 30 && action.RoomIndex == 2 &&
            before.Spawn.Train.Context!.Enchantments == null && before.Spawn.Train.Context.NextUnitId == 24 &&
            actual.Spawn.Train.Context!.NextUnitId == 25 && actual.Spawn.Train.Context.SpawnPoints!.Units.Any(unit => unit.UnitId == 24),
            "Missing native occupied preview birth boundary.");
        var emptyGroups = Array.Empty<SpawnPointGroupState>();
        var occupied = new BattleSpawnPoints(emptyGroups, [new UnitSpawnPointState(25, null, null, spawnedInPreview: true)], 1);
        var invalid = new BattleSpawnPoints(emptyGroups, [new UnitSpawnPointState(25, null, null)], 1);
        var mixed = new BattleSpawnPoints(emptyGroups, [new UnitSpawnPointState(25, null, null, spawnedInPreview: true),
            new UnitSpawnPointState(26, null, null)], 1);
        Require(occupied.RetainedPreviewNextUnitId(25, 26) == 26 && occupied.RetainedPreviewNextUnitId(25, 25) == 25 &&
            occupied.RetainedPreviewNextUnitId(25, null) == 25 && invalid.RetainedPreviewNextUnitId(25, 26) == 25 &&
            mixed.RetainedPreviewNextUnitId(25, 27) == 25, "Preview counter accepted an unproven or invalid identity.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent occupied preview differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Occupied preview mutated its parent.");
        Console.WriteLine("OCCUPIED-PREVIEW-BIRTH PASS: 28 native inputs, complete successor/RNG, identity guards and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
