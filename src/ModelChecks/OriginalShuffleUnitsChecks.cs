using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalShuffleUnitsChecks
{
    internal static void Native(FixtureValue witness)
    {
        FixtureValue owner = witness.GetProperty("Owner");
        int schema = witness.GetProperty("Schema").GetInt32();
        bool trio = schema == 2;
        Require(schema is 1 or 2 &&
            witness.GetProperty("Boundary").GetString() == "OriginalShuffleUnits" &&
            witness.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            witness.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            owner.GetProperty("Id").GetString() == RelicShuffleUnitsModel.OwnerId &&
            owner.GetProperty("AssetKey").GetString() == RelicShuffleUnitsModel.OwnerKey &&
            owner.GetProperty("EffectType").GetString() == RelicShuffleUnitsModel.Effect &&
            owner.GetProperty("DisallowedInPlacementPhase").GetBoolean() &&
            !owner.GetProperty("CanApplyInPreviewMode").GetBoolean(), "Original ShuffleUnits owner and phase flags differ.");

        BattleTurnState deployment = witness.GetProperty("DeploymentBefore").Deserialize<BattleTurnState>()!;
        BattleTurnState before = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = witness.GetProperty("After").Deserialize<BattleTurnState>()!;
        Require(deployment.Spawn.Turn == 0 && before.Spawn.Turn == 1 && after.Spawn.Turn == 2 &&
            new[] { deployment, before, after }.All(state =>
                state.Spawn.Train.Context!.Relics!.Count(RelicShuffleUnitsModel.IsOriginal) == 1),
            "Expected the original owner across deployment and the first ordinary EndTurn.");

        FixtureValue[] tests = witness.GetProperty("Tests").EnumerateArray().ToArray();
        FixtureValue[] applications = witness.GetProperty("Applications").EnumerateArray().ToArray();
        Require(tests.Select(Room).SequenceEqual(new[] { 3, 2, 1, 0 }) &&
            tests.Select(row => row.GetProperty("Result").GetBoolean()).SequenceEqual(new[] { false, true, false, true }) &&
            applications.Select(Room).SequenceEqual(new[] { 2, 0 }) &&
            tests.Concat(applications).All(row => row.GetProperty("Turn").GetInt32() == 1 &&
                !row.GetProperty("Preview").GetBoolean()),
            "Expected ordinary top-to-bottom native tests with friendly and enemy shuffles and empty/Pyre controls.");
        FixtureValue[] pyre = tests[0].GetProperty("Roster").EnumerateArray().ToArray();
        Require(pyre.Length == 1 && !pyre[0].GetProperty("Movable").GetBoolean() &&
            before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 3).Units.Single().IsPyre &&
            !tests[2].GetProperty("Roster").EnumerateArray().Any(), "Expected the immovable Pyre and empty floor controls.");

        var friendly = CheckShuffle(tests[1], applications[0], "Monsters",
            destinationPoints: trio ? [1, 2, 0] : [1, 0], draws: trio ? [1, 1] : [0, 1]);
        var enemy = CheckShuffle(tests[3], applications[1], "Heroes", destinationPoints: [1, 0], draws: [1]);
        Require(PhysicalRoster(deployment, 2, CombatTeam.Player).SequenceEqual(friendly.Before) &&
            PhysicalRoster(before, 2, CombatTeam.Player).SequenceEqual(friendly.Before) &&
            PhysicalRoster(after, 2, CombatTeam.Player).SequenceEqual(friendly.After) &&
            PhysicalRoster(before, 0, CombatTeam.Enemy).SequenceEqual(enemy.Before),
            "Original callback actors must agree with the decision snapshots' primary physical slots.");
        Require(Rng(applications[0], "RngBefore").Equals(before.Spawn.Train.Context!.BattleRng) &&
            Rng(applications[0], "RngAfter").Next().Equals(Rng(applications[1], "RngBefore")) &&
            Rng(applications[1], "RngAfter").Equals(after.Spawn.Train.Context!.BattleRng),
            "Expected the captured callback RNG intervals within settled Battle RNG.");

        Replay(deployment, before, "Original ShuffleUnits deployment control");
        Replay(before, after, "Original ShuffleUnits ordinary EndTurn");
        Console.WriteLine($"NATIVE-SHUFFLE-UNITS PASS. Deployment and ordinary EndTurn match with {friendly.Before.Length} friendly units, two enemy units, {2 * (friendly.Before.Length + enemy.Before.Length)} native callbacks and exact Battle RNG.");
    }

    private static (int[] Before, int[] After) CheckShuffle(FixtureValue test, FixtureValue application,
        string team, int[] destinationPoints, int[] draws)
    {
        int room = Room(application);
        FixtureValue original = application.GetProperty("Before");
        FixtureValue[] entering = original.EnumerateArray().ToArray();
        FixtureValue[] exiting = application.GetProperty("After").EnumerateArray().ToArray();
        Require(test.GetProperty("Roster").ContentEquals(original) &&
            entering.Length == destinationPoints.Length && exiting.Length == entering.Length &&
            entering.Concat(exiting).All(row => row.GetProperty("Team").GetString() == team &&
                row.GetProperty("Movable").GetBoolean() && row.GetProperty("Alive").GetBoolean()),
            $"Expected the original alive movable {team} roster in floor {room}.");
        int[] actors = entering.Select(Actor).ToArray();
        int[] points = entering.Select(Point).ToArray();
        int[] exitActors = exiting.Select(Actor).ToArray();
        int[] expectedExit = actors.Select((actor, index) => (Actor: actor, Point: destinationPoints[index]))
            .OrderBy(unit => unit.Point).Select(unit => unit.Actor).ToArray();
        Require(actors.All(actor => actor > 0) && actors.Distinct().Count() == actors.Length &&
            points.SequenceEqual(Enumerable.Range(0, actors.Length)) &&
            exitActors.SequenceEqual(expectedExit) && exiting.Select(Point).SequenceEqual(points),
            $"Expected the original physical permutation in floor {room}.");

        var observedCallbacks = application.GetProperty("Callbacks").EnumerateArray()
            .Select(row => (row.GetProperty("Kind").GetString(), Actor(row), Point(row)));
        var expectedCallbacks = new[] { "CharacterFloorRearranged", "ShiftCharacterTrigger" }
            .SelectMany(kind => actors.Select((actor, index) => (kind, actor, destinationPoints[index])));
        Require(observedCallbacks.SequenceEqual(expectedCallbacks), $"Native callback order or destination differs in floor {room}.");

        UnityRng rng = Rng(application, "RngBefore");
        foreach (int expected in draws)
        {
            RngDraw draw = rng.Range(0, actors.Length);
            Require(draw.Value == expected, $"Native Battle RNG target draw differs in floor {room}.");
            rng = draw.State;
        }
        Require(rng.Equals(Rng(application, "RngAfter")), $"Native Battle RNG successor differs in floor {room}.");
        return (actors, exitActors);
    }

    private static void Replay(BattleTurnState before, BattleTurnState actual, string label)
    {
        string parent = JsonSerializer.Serialize(before);
        BattleTurnResult result = BattleTurnModel.EndTurn(before);
        Require(result.Supported && result.Outcome == RoomOutcome.Exchanged && result.State != null,
            $"{label}: {result.UnsupportedReason ?? result.Outcome.ToString()}.");
        RecordedBattleChecks.Compare(result.State!, actual, terminal: false, label);
        Require(JsonSerializer.Serialize(before) == parent, $"{label}: replay mutated its parent.");
    }

    private static int Room(FixtureValue row) => row.GetProperty("Room").GetInt32();
    private static int Actor(FixtureValue row) => row.GetProperty("Actor").GetInt32();
    private static int Point(FixtureValue row) => row.GetProperty("Point").GetInt32();
    private static IEnumerable<int> PhysicalRoster(BattleTurnState state, int floor, CombatTeam team) =>
        state.Spawn.Train.Context!.SpawnPoints!.SelectedGroup(floor, team)!.Occupants.Where(id => id > 0);

    private static UnityRng Rng(FixtureValue row, string key)
    {
        uint[] words = row.GetProperty(key).EnumerateArray().Select(value => value.GetUInt32()).ToArray();
        Require(words.Length == 4, "Expected four captured Battle RNG words.");
        return new UnityRng(words[0], words[1], words[2], words[3]);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
