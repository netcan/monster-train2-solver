using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalShuffleUnitsChecks
{
    internal static void Native(FixtureValue witness)
    {
        FixtureValue owner = witness.GetProperty("Owner");
        Require(witness.GetProperty("Schema").GetInt32() == 1 &&
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
            "Expected ordinary top-to-bottom native tests with a positive friendly and enemy pair and empty/Pyre controls.");
        FixtureValue[] pyre = tests[0].GetProperty("Roster").EnumerateArray().ToArray();
        Require(pyre.Length == 1 && !pyre[0].GetProperty("Movable").GetBoolean() &&
            before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 3).Units.Single().IsPyre &&
            !tests[2].GetProperty("Roster").EnumerateArray().Any(), "Expected the immovable Pyre and empty floor controls.");

        var friendlyActors = CheckPair(tests[1], applications[0], "Monsters", draws: 2);
        var enemyActors = CheckPair(tests[3], applications[1], "Heroes", draws: 1);
        Require(PhysicalPair(deployment, 2, CombatTeam.Player).SequenceEqual(friendlyActors) &&
            PhysicalPair(before, 2, CombatTeam.Player).SequenceEqual(friendlyActors) &&
            PhysicalPair(after, 2, CombatTeam.Player).SequenceEqual(friendlyActors.Reverse()) &&
            PhysicalPair(before, 0, CombatTeam.Enemy).SequenceEqual(enemyActors),
            "Original callback actors must agree with the decision snapshots' primary physical slots.");
        Require(Rng(applications[0], "RngBefore").Equals(before.Spawn.Train.Context!.BattleRng) &&
            Rng(applications[0], "RngAfter").Next().Equals(Rng(applications[1], "RngBefore")) &&
            Rng(applications[1], "RngAfter").Equals(after.Spawn.Train.Context!.BattleRng),
            "Expected the captured callback RNG intervals within settled Battle RNG.");

        Replay(deployment, before, "Original ShuffleUnits deployment control");
        Replay(before, after, "Original ShuffleUnits ordinary EndTurn");
        Console.WriteLine("NATIVE-SHUFFLE-UNITS PASS. Deployment and ordinary EndTurn match, both pairs swap, eight native callbacks and exact Battle RNG.");
    }

    private static int[] CheckPair(FixtureValue test, FixtureValue application, string team, int draws)
    {
        int room = Room(application);
        FixtureValue original = application.GetProperty("Before");
        FixtureValue[] entering = original.EnumerateArray().ToArray();
        FixtureValue[] exiting = application.GetProperty("After").EnumerateArray().ToArray();
        Require(test.GetProperty("Roster").ContentEquals(original) && entering.Length == 2 && exiting.Length == 2 &&
            entering.Concat(exiting).All(row => row.GetProperty("Team").GetString() == team &&
                row.GetProperty("Movable").GetBoolean() && row.GetProperty("Alive").GetBoolean()),
            $"Expected an alive movable {team} pair in original floor {room}.");
        int[] actors = entering.Select(Actor).ToArray();
        int[] points = entering.Select(Point).ToArray();
        Require(actors[0] > 0 && actors[1] > 0 && actors[0] != actors[1] && points.SequenceEqual(new[] { 0, 1 }) &&
            exiting.Select(Actor).SequenceEqual(actors.Reverse()) && exiting.Select(Point).SequenceEqual(points),
            $"Expected the original physical pair to exchange slots in floor {room}.");

        var observedCallbacks = application.GetProperty("Callbacks").EnumerateArray()
            .Select(row => (row.GetProperty("Kind").GetString(), Actor(row), Point(row)));
        var expectedCallbacks = new[] { "CharacterFloorRearranged", "ShiftCharacterTrigger" }
            .SelectMany(kind => actors.Select((actor, index) => (kind, actor, points[1 - index])));
        Require(observedCallbacks.SequenceEqual(expectedCallbacks), $"Native callback order or destination differs in floor {room}.");

        UnityRng rng = Rng(application, "RngBefore");
        for (int index = 0; index < draws; index++)
        {
            RngDraw draw = rng.Range(0, actors.Length);
            Require(draw.Value == (index == draws - 1 ? 1 : 0), $"Native Battle RNG retry differs in floor {room}.");
            rng = draw.State;
        }
        Require(rng.Equals(Rng(application, "RngAfter")), $"Native Battle RNG successor differs in floor {room}.");
        return actors;
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
    private static IEnumerable<int> PhysicalPair(BattleTurnState state, int floor, CombatTeam team) =>
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
