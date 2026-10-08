using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SpawnPointChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("SpawnPointOperations", out var records) || records.GetArrayLength() == 0) return;
        var samples = records.EnumerateArray().ToArray();
        Require(samples.Length == 21 && samples.All(sample => sample.GetProperty("Completed").GetBoolean() &&
            sample.GetProperty("Difference").ValueKind == FixtureKind.Null), "Physical position recording was incomplete.");
        var byLabel = samples.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
        int first = byLabel["remember-initial"].GetProperty("UnitId").GetInt32();
        int displaced = byLabel["remember-displaced"].GetProperty("UnitId").GetInt32();
        int restored = byLabel["live-ignores-old-point"].GetProperty("UnitId").GetInt32();
        var overwritten = After("overwrite-retains-displaced-reference");
        Require(Equal(overwritten.Point(first), overwritten.Point(displaced)) &&
            overwritten.Groups.Single(group => group.RoomIndex == 1 && group.Team == CombatTeam.Player).Occupants[1] == first,
            "Overwriting a point did not retain the displaced unit's independent reference.");
        var repaired = After("repair-displaced-reference");
        Require(repaired.Point(first)?.Index == 1 && repaired.Point(displaced)?.Index == 0,
            "Repairing a displaced reference cleared a different occupant.");
        var zero = Unit(After("remove-zero-hp"), restored);
        Require(zero.Health == 0 && zero.Undying == 0 && zero.Current == null && zero.LastKnown != null,
            "Compaction did not remember and detach a zero-HP occupant.");
        var undying = Unit(After("undying-retains-zero-hp"), restored);
        Require(undying.Health == 0 && undying.Undying > 0 && undying.Current != null && undying.LastKnown != null,
            "Undying zero-HP occupancy was not exercised.");
        var live = Unit(After("live-ignores-old-point"), restored);
        Require(live.Health > 0 && live.Current != null && live.LastKnown != null && !Equal(live.Current, live.LastKnown),
            "Live current-position precedence over an old reference was not exercised.");
        var removed = After("remove-keeps-last");
        Require(Unit(removed, first).Current == null && Equal(Unit(removed, first).LastKnown,
            Unit(Before("remove-keeps-last"), first).LastKnown), "Removal unexpectedly remembered a new point.");
        Require(Unit(Before("remove-preview-born"), displaced).SpawnedInPreview &&
            Unit(After("remove-preview-born"), displaced).Current == null, "Preview-born removal was not exercised.");
        Require(Unit(Before("destroyed-detached-cannot-reenter"), displaced).Destroyed &&
            Equal(Before("destroyed-detached-cannot-reenter"), After("destroyed-detached-cannot-reenter")),
            "A destroyed detached actor reentered the layout.");
        Require(After("move-across-room").Point(restored)?.RoomIndex == 2 &&
            After("return-across-room").Point(restored)?.RoomIndex == 1, "Cross-room references were not exercised.");
        Require(byLabel["compact-pivot"].GetProperty("PivotMoves").GetInt32() > 0 &&
            Equal(Before("invalid-rearrange-no-op"), After("invalid-rearrange-no-op")),
            "Pivot movement or invalid-index no-op was not exercised.");
        var final = After("restore-dense-layout");
        foreach (int id in new[] { first, displaced, restored })
        {
            var point = final.Point(id);
            Require(point != null && final.Groups.Single(group => group.RoomIndex == point.RoomIndex &&
                group.Team == point.Team).Occupants[point.Index] == id, "Setup did not restore actor-owned positions.");
        }
        Require(samples.Any(sample => sample.GetProperty("Before").Deserialize<SpawnPointWorld>()!.Groups.Any(group =>
            group.Occupants.Take(group.GroupCount).SkipWhile(id => id != 0).Skip(1).Any(id => id != 0))),
            "No occupied layout with a physical hole was observed.");
        Verify();
        Parallel.For(0, 32, _ => Verify());
        VerifyRejections(Before("remember-initial"));
        Console.WriteLine($"NATIVE-SPAWN-POINT-CHECKS PASS: {samples.Length} exact physical operations, " +
            $"{samples.Sum(sample => sample.GetProperty("Queries").GetArrayLength())} current/last-known queries, " +
            "stale references, holes, zero HP, undying, preview-born removal, cross-room moves and 32 branches.");

        SpawnPointWorld Before(string label) => byLabel[label].GetProperty("Before").Deserialize<SpawnPointWorld>()!;
        SpawnPointWorld After(string label) => byLabel[label].GetProperty("After").Deserialize<SpawnPointWorld>()!;
        void Verify()
        {
            foreach (var sample in samples)
            {
                var before = sample.GetProperty("Before").Deserialize<SpawnPointWorld>()!;
                var expected = sample.GetProperty("After").Deserialize<SpawnPointWorld>()!;
                string parent = JsonSerializer.Serialize(before);
                var actual = SpawnPointModel.Apply(before, sample.GetProperty("Operation").GetString()!,
                    sample.GetProperty("RoomIndex").GetInt32(), sample.GetProperty("Team").Deserialize<CombatTeam>(),
                    sample.GetProperty("UnitId").GetInt32(), sample.GetProperty("Index").GetInt32(),
                    sample.GetProperty("TargetIndex").GetInt32(), sample.GetProperty("Target").Deserialize<SpawnPointReference>());
                Require(actual.Supported && Equal(actual.State, expected) &&
                    actual.PivotMoves == sample.GetProperty("PivotMoves").GetInt32(),
                    "Physical state differs at " + sample.GetProperty("Label").GetString() + ": " + actual.UnsupportedReason);
                Require(actual.State!.Remaining(sample.GetProperty("RoomIndex").GetInt32(),
                    sample.GetProperty("Team").Deserialize<CombatTeam>()) == sample.GetProperty("Remaining").GetInt32(),
                    "Remaining point count changed its outer-boss/dead-occupant rules.");
                Require(sample.GetProperty("Queries").GetArrayLength() == 3, "Detached native actors lost their point queries.");
                foreach (var query in sample.GetProperty("Queries").EnumerateArray())
                {
                    int id = query.GetProperty("UnitId").GetInt32();
                    Require(Equal(actual.State.Point(id), query.GetProperty("Current").Deserialize<SpawnPointReference>()) &&
                        Equal(actual.State.Point(id, true), query.GetProperty("LastKnown").Deserialize<SpawnPointReference>()),
                        "Current/last-known native point lookup differs.");
                }
                Require(parent == JsonSerializer.Serialize(before), "A position branch mutated its parent.");
            }
        }
    }
    private static void VerifyRejections(SpawnPointWorld source)
    {
        string parent = JsonSerializer.Serialize(source);
        var duplicate = new SpawnPointWorld(source.Preview, source.Groups, source.Units.Append(source.Units[0]).ToArray());
        var actor = source.Units[0];
        var badActor = new SpawnPointOccupant(actor.UnitId, actor.Health, actor.Alive, actor.Dead, actor.Destroyed,
            actor.Undying, actor.SpawnedInPreview, actor.OuterBoss, new SpawnPointReference(999, CombatTeam.Player, 0), actor.LastKnown);
        var badReference = new SpawnPointWorld(source.Preview, source.Groups,
            source.Units.Select(unit => unit.UnitId == actor.UnitId ? badActor : unit).ToArray());
        foreach (var result in new[] {
            SpawnPointModel.Apply(duplicate, "Compact", 1, CombatTeam.Player),
            SpawnPointModel.Apply(badReference, "Compact", 1, CombatTeam.Player),
            SpawnPointModel.Apply(source, "Set", 1, CombatTeam.Player, actor.UnitId,
                target: new SpawnPointReference(999, CombatTeam.Player, 0)),
            SpawnPointModel.Apply(source, "Unknown", 1, CombatTeam.Player) })
            Require(!result.Supported && result.State == null && result.UnsupportedReason != null,
                "Malformed physical state produced a supported or partial child.");
        Require(parent == JsonSerializer.Serialize(source), "Rejected physical branches mutated the parent.");
    }
    private static SpawnPointOccupant Unit(SpawnPointWorld state, int id) => state.Units.Single(unit => unit.UnitId == id);
    private static bool Equal<T>(T first, T second) => JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
