using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleSpawnPointChecks
{
    internal static void DecisionReferences(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("DecisionSpawnPoints", out var decisions) || decisions.GetArrayLength() == 0) return;
        int removed = 0, planeMappings = 0;
        foreach (var sample in decisions.EnumerateArray())
        {
            var raw = sample.GetProperty("Raw").Deserialize<BattleSpawnPoints>()!;
            var canonical = sample.GetProperty("Canonical").Deserialize<BattleSpawnPoints>()!;
            var living = sample.GetProperty("LivingUnitIds").Deserialize<int[]>()!.ToHashSet();
            var expected = new BattleSpawnPoints(raw.Groups.Where(group => group.PreviewCopyId == null).ToArray(), raw.Units.Select(unit => living.Contains(unit.UnitId) ? unit :
                new UnitSpawnPointState(unit.UnitId, null, null, unit.OuterBoss, unit.SpawnedInPreview)).ToArray());
            Require(JsonSerializer.Serialize(canonical) == JsonSerializer.Serialize(expected),
                "Decision normalization changed live positions, group order or retained identity metadata.");
            if (sample.TryGetProperty("Planes", out var planes))
            {
                var groups = planes.GetProperty("Groups").Deserialize<SpawnPointGroupState[]>()!;
                Require(groups.Select(group => (group.RoomIndex, group.Team, group.PreviewCopyId)).Distinct().Count() == groups.Length,
                    "A copied list was aliased to another physical group.");
                int[] current = planes.GetProperty("CurrentCopyIds").Deserialize<int[]>()!;
                Require(current.Distinct().Count() == current.Length && current.All(id => id > 0 && groups.Any(group => group.PreviewCopyId == id)),
                    "Current copied groups are missing from raw physical state.");
                foreach (var unit in planes.GetProperty("Units").EnumerateArray())
                {
                    int id = unit.GetProperty("UnitId").GetInt32();
                    var primary = unit.GetProperty("Primary").Deserialize<UnitSpawnPointState>()!;
                    Require(JsonSerializer.Serialize(primary) == JsonSerializer.Serialize(raw.Units.Single(actor => actor.UnitId == id)),
                        "Raw primary pointers differ from their complete plane mapping.");
                    foreach (string key in new[] { "Primary", "Preview", "Temporary" })
                    {
                        var state = unit.GetProperty(key).Deserialize<UnitSpawnPointState>();
                        if (state == null) continue;
                        Require(state.UnitId == id, "A native state plane changed its actor identity.");
                        foreach (var point in new[] { state.Current, state.LastKnown }.Where(point => point != null))
                            Require(groups.Any(group => group.RoomIndex == point!.RoomIndex && group.Team == point.Team &&
                                group.PreviewCopyId == point.PreviewCopyId && point.Index >= 0 && point.Index < group.Occupants.Count),
                                "A raw state-plane pointer is outside its observed physical list.");
                        planeMappings++;
                    }
                }
            }
            removed += raw.Units.Count(unit => !living.Contains(unit.UnitId) && (unit.Current != null || unit.LastKnown != null));
        }
        Console.WriteLine($"NATIVE-DECISION-SPAWN-POINT-CHECKS PASS: {decisions.GetArrayLength()} complete raw/canonical mappings, " +
            $"{removed} removed object references, {planeMappings} primary/preview/temporary plane mappings, preserved live actors and unchanged physical groups.");
    }
    internal static void Native(FixtureValue fixture)
    {
        var samples = fixture.GetProperty("Stages").EnumerateArray().ToArray();
        var first = samples[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        if (first.Context?.SpawnPoints == null) return;
        DecisionReferences(fixture);
        int contexts = 0, removals = 0;
        foreach (var sample in samples)
        {
            var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Validate(before); Validate(after); contexts += 2;
            foreach (var actor in before.Units.Where(unit => !unit.IsPyre && !after.Units.Any(next => next.Id == unit.Id)))
                if (after.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == actor.Id).Current == null) removals++;
        }
        int moves = 0, ascents = 0, emptyAscents = 0;
        foreach (var sample in fixture.GetProperty("TrainPhases").EnumerateArray())
        {
            var before = sample.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = sample.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(before.Context?.SpawnPoints != null && after.Context?.SpawnPoints != null, "A train phase lost physical state.");
            if (sample.GetProperty("Kind").GetString() == "Ascend")
            {
                ascents++;
                if (!before.Rooms.SelectMany(room => room.Units).Any(unit => unit.Team == CombatTeam.Enemy)) emptyAscents++;
            }
            foreach (var unit in before.Context!.SpawnPoints!.Units.Where(unit => unit.Current != null))
            {
                var next = after.Context!.SpawnPoints!.Units.Single(item => item.UnitId == unit.UnitId);
                if (next.Current != null && next.Current.RoomIndex != unit.Current!.RoomIndex) moves++;
            }
        }
        // If combat kills every enemy before every ascent, no cross-room move
        // should occur. The complete captured ascent inputs prove this case.
        bool firstTurnDefeat = fixture.GetProperty("NativeWon").ValueKind == FixtureKind.False &&
            fixture.GetProperty("Turns").GetArrayLength() == 1 &&
            fixture.GetProperty("Turns")[0].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.PlayerDefeated;
        bool noUnitDied = samples.All(sample =>
        {
            var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            return before.Units.Where(unit => !unit.IsPyre).All(unit => after.Units.Any(next => next.Id == unit.Id));
        });
        // A first-turn defeat can occur before any ordinary actor is killed.
        // Its complete room observations must prove the absence of removals.
        Require((contexts > 50 || firstTurnDefeat && contexts > 0) && (removals > 0 || firstTurnDefeat && noUnitDied) &&
            (moves > 0 || ascents > 0 && emptyAscents == ascents),
            "Complete battle position transitions were not exercised.");
        foreach (var sample in fixture.GetProperty("PhysicalCompactions").EnumerateArray())
            Require(sample.GetProperty("Error").ValueKind == FixtureKind.Null && sample.GetProperty("After").ValueKind != FixtureKind.Null,
                "Native compaction observations were incomplete.");
        Verify();
        Parallel.For(0, 32, _ => Verify());
        RejectMalformed(first);
        Console.WriteLine($"NATIVE-BATTLE-SPAWN-POINT-CHECKS PASS: {contexts} complete room contexts, {removals} physical removals, " +
            $"{moves} cross-room moves, {emptyAscents} empty ascents, retained identities/references and 32 independent branches.");

        void Verify()
        {
            foreach (var sample in samples)
            {
                var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
                var expected = sample.GetProperty("Actual").Deserialize<RoomCombatState>()!;
                string parent = JsonSerializer.Serialize(before);
                var result = sample.GetProperty("Kind").GetString() == "Exchange"
                    ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
                Require(result.Supported && JsonSerializer.Serialize(result.State!.Context!.SpawnPoints) ==
                    JsonSerializer.Serialize(expected.Context!.SpawnPoints), "Full room resolution changed physical references incorrectly.");
                Require(parent == JsonSerializer.Serialize(before), "A full battle physical branch mutated its parent.");
            }
        }
    }
    private static void Validate(RoomCombatState state)
    {
        var points = state.Context!.SpawnPoints;
        Require(points != null && BattleSpawnPointModel.Validate(points, state.Context.NextUnitId) == null,
            "Missing or invalid complete battle point layout.");
        Require(points!.Units.Select(unit => unit.UnitId).SequenceEqual(Enumerable.Range(1, state.Context.NextUnitId!.Value - 1)),
            "Retained position identities do not cover all observed births.");
        foreach (var actor in state.Units.Where(unit => !unit.IsPyre))
        {
            var unit = points.Units.Single(unit => unit.UnitId == actor.Id);
            Require(unit.Current != null && unit.Current.RoomIndex == state.RoomIndex && unit.Current.Team == actor.Team &&
                points.Group(unit.Current.RoomIndex, unit.Current.Team)!.Occupants[unit.Current.Index] == actor.Id,
                "A living room actor lost its owned point.");
        }
    }
    private static void RejectMalformed(RoomCombatState source)
    {
        var points = source.Context!.SpawnPoints!;
        string parent = JsonSerializer.Serialize(source);
        var duplicate = new BattleSpawnPoints(points.Groups, points.Units.Append(points.Units[0]).ToArray());
        var old = points.Units[0];
        var invalid = new UnitSpawnPointState(old.UnitId, old.Current, new SpawnPointReference(999, CombatTeam.Player, 0),
            old.OuterBoss, old.SpawnedInPreview);
        var missing = new BattleSpawnPoints(points.Groups, points.Units.Select(unit => unit.UnitId == old.UnitId ? invalid : unit).ToArray());
        foreach (var child in new[] { duplicate, missing })
        {
            var state = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units, source.ExternalInteractions,
                source.Context.WithSpawnPoints(child), source.Preview);
            var result = RoomCombatModel.Resolve(state);
            Require(!result.Supported && result.State == null, "Malformed physical references produced a supported child.");
        }
        Require(parent == JsonSerializer.Serialize(source), "Rejected physical children mutated their parent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
