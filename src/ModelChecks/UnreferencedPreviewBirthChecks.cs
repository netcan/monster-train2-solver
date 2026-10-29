using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnreferencedPreviewBirthChecks
{
    private const string Reason = "Battle preview floor 0: Invalid retained physical identities or missing shared counter: next=28, retained=1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28.";

    internal static void Native(FixtureValue first, FixtureValue second, FixtureValue repaired, FixtureValue repaired2)
    {
        FixtureValue[] a = NativeBranchObservationChecks.Replay(first, Reason);
        FixtureValue[] b = NativeBranchObservationChecks.Replay(second, Reason);
        NativeBranchObservationChecks.Replay(repaired, Reason);
        NativeBranchObservationChecks.Replay(repaired2, Reason);
        NativeBranchObservationChecks.Pair(first, repaired);
        NativeBranchObservationChecks.Pair(second, repaired2);
        Require(a.Length == 26 && b.Length == a.Length && a[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing recorded boundary.");
        for (int i = 0; i < a.Length; i++)
            foreach (string field in new[] { "Before", "Actual" })
                Require(NativeBranchObservationChecks.Key(a[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(b[i].GetProperty(field).Deserialize<BattleTurnState>()!), "Independent native capture differs at " + i);
        BattleTurnState before = a[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = a[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Train.Context!.Enchantments == null && before.Spawn.Train.Context.NextUnitId == 28 &&
            actual.Spawn.Train.Context!.NextUnitId == 28 && actual.Spawn.Train.Context.SpawnPoints!.Units.All(unit => unit.UnitId < 28),
            "Native temporary birth escaped into the durable actor set.");
        var point = new SpawnPointReference(0, CombatTeam.Player, 0);
        var points = new BattleSpawnPoints([new SpawnPointGroupState(0, CombatTeam.Player, 1, 1, [32], [false])],
        [
            new UnitSpawnPointState(27, null, null, spawnedInPreview: true),
            new UnitSpawnPointState(28, null, null, spawnedInPreview: true),
            new UnitSpawnPointState(29, null, null),
            new UnitSpawnPointState(30, point, null, spawnedInPreview: true),
            new UnitSpawnPointState(31, null, point, spawnedInPreview: true),
            new UnitSpawnPointState(32, null, null, spawnedInPreview: true)
        ], 1).BeginPreview().WithBirthPrimary(28);
        BattleSpawnPoints cleaned = points.ForgetUnreferencedPreviewBirths(28);
        Require(cleaned.Units.Select(unit => unit.UnitId).SequenceEqual([27, 29, 30, 31, 32]) &&
            cleaned.BirthPrimaries.Count == 0 && cleaned.ActiveCopies.SequenceEqual(points.ActiveCopies) &&
            points.Units.Count == 6 && points.BirthPrimaries.Count == 1,
            "Preview cleanup changed a durable, referenced or occupied object, or mutated its parent.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent preview differs from native.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Preview mutated its parent.");
        Console.WriteLine("UNREFERENCED-PREVIEW-BIRTH PASS: 26 native inputs, independent original captures, full state/RNG, reference guards and four concurrent replays.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
