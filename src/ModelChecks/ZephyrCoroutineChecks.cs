using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ZephyrCoroutineChecks
{
    private const string ZephyrId = "f73bc157-ef23-4641-a938-ecd31945ca28";
    private const string DregId = "63a48bad-07da-4687-bfef-2f7c6653af5e";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("Schema").GetInt32() is 103 or 105 &&
            root.GetProperty("ModifierScenario").GetString() == $"original-isolated-spells:{ZephyrId},{DregId}" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop",
            "Expected the original Zephyr and Dreg 2.3.1 terminal witness.");
        foreach (string name in new[] { "CaptureFailures", "Pending", "Unsupported" })
            Require(root.GetProperty(name).GetInt32() == 0, "Incomplete native witness: " + name);
        Require(root.GetProperty("Mismatches").GetInt32() == 3, "Expected the original three terminal diagnostics.");

        FixtureValue[] stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        FixtureValue[] train = root.GetProperty("TrainPhases").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] roomDifferences = stages.Where(HasDifference).ToArray();
        FixtureValue[] trainDifferences = train.Where(HasDifference).ToArray();
        Require(roomDifferences.Select(row => row.GetProperty("Kind").GetString()).SequenceEqual(["Room", "Exchange"]) &&
            trainDifferences.Select(row => row.GetProperty("Kind").GetString()).SequenceEqual(["Combat"]),
            "Historical differences must be one room, exchange and train combat each.");
        BattleTurnState settled = turns[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int terminalTurn = RecordedBattleChecks.DecisionTurn(turns[^1]);
        Require(turns[^1].GetProperty("ActualOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon &&
            roomDifferences.Concat(trainDifferences).All(row => row.GetProperty("Turn").GetInt32() == terminalTurn),
            "The raw diagnostics must occur only while unwinding the final turn.");
        var terminalSamples = root.GetProperty("DecisionSpawnPoints").EnumerateArray()
            .Where(row => row.GetProperty("Terminal").GetBoolean()).ToArray();
        Require(terminalSamples.Length > 1 &&
            terminalSamples.All(row => row.GetProperty("Won").GetBoolean() &&
                row.GetProperty("RetiringEnemyIds").GetArrayLength() == 0), "The original terminal capture must contain both ownership planes.");
        var rawSamples = terminalSamples.Where(row => !row.GetProperty("CanonicalDecision").GetBoolean())
            .Select(row => (Raw: Points(row, "Raw"), Observed: Points(row, "Canonical"))).ToArray();
        var stableSample = terminalSamples.Single(row => row.GetProperty("CanonicalDecision").GetBoolean());

        RoomAttachmentTriggerChecks.Native(root);
        bool positiveMovement = false;
        foreach (FixtureValue row in stages)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            bool exchange = row.GetProperty("Kind").GetString() == "Exchange";
            Require(row.GetProperty("ExternalPyre").ValueKind == FixtureKind.Null &&
                row.GetProperty("ExternalTargets").GetArrayLength() == 0,
                "The isolated Zephyr witness has no borrowed tower targets.");
            RoomCombatResult result = exchange ? RoomCombatModel.Exchange(before,
                pendingPhysicalDeathIds: row.GetProperty("PendingPhysicalDeaths").Deserialize<int[]>()) :
                RoomCombatModel.Resolve(before, relentlessDecisions: RecordedBattleChecks.Decisions(row));
            Require(result.Supported, "Zephyr room replay unsupported: " + result.UnsupportedReason);
            Require(!HasDifference(row) || (exchange ? row.ContentEquals(stages[^1]) : result.Outcome == RoomOutcome.BattleWon),
                "A room diagnostic occurred before the winning room or final exchange.");
            RoomCombatState projected = HasDifference(row) ? actual.WithContext(Project(result.State!.Context!,
                actual.Context!, result.Events, actual.Units)) : actual;
            Equal(result.State, projected, "full room/exchange successor " + row.GetProperty("Index").GetInt32());
            if (exchange && !HasDifference(row)) positiveMovement |= MovedDreg(before, actual);
        }
        Require(positiveMovement, "Missing fully matched original Zephyr front-slot movement.");
        foreach (FixtureValue row in train)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            TrainCombatResult result = row.GetProperty("Kind").GetString() == "Combat"
                ? TrainCombatModel.ResolveCombat(before, RecordedBattleChecks.Decisions(row),
                    row.TryGetProperty("RoomRules", out var rules) ? rules.Deserialize<RoomPlayRule[]>() : null)
                : TrainCombatModel.Ascend(before);
            Require(result.Supported, "Zephyr train replay unsupported: " + result.UnsupportedReason);
            if (HasDifference(row))
            {
                Require(result.Outcome == RoomOutcome.BattleWon, "Train differences must be during the final combat.");
                var deaths = result.RoomResults.SelectMany(room => room.Events).ToArray();
                var survivors = actual.Rooms.SelectMany(room => room.Units).ToArray();
                actual = new TrainCombatState(actual.Rooms.Zip(result.State!.Rooms, (observed, expected) =>
                        observed.WithContext(Project(expected.Context!, observed.Context!, deaths, survivors))).ToArray(),
                    actual.Movement, actual.EnemySlotsPerRoom, Project(result.State.Context!, actual.Context!, deaths, survivors));
            }
            Equal(result.State, actual, "full train successor " + row.GetProperty("Index").GetInt32());
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ZEPHYR-COROUTINE-WITNESS PASS {stages.Length} room/exchange and {train.Length} train stages, " +
            "native rear-to-front movement and complete settled replay. Three raw terminal diagnostics preserved.");

        CombatContext Project(CombatContext predicted, CombatContext actual, IReadOnlyList<CombatEvent> events,
            IReadOnlyList<CombatUnit> survivors)
        {
            BattleSpawnPoints actualPoints = actual.SpawnPoints!;
            BattleSpawnPoints predictedPoints = predicted.SpawnPoints!;
            var deaths = events.Where(item => item.Kind == "Death").Select(item => item.Target).ToHashSet();
            var allowed = actualPoints.Units.Where(point => point.LastKnown != null &&
                Owner(predictedPoints, point.UnitId).LastKnown == null).ToArray();
            Require(allowed.Length > 0, "Missing independently replayed terminal reference difference.");
            foreach (UnitSpawnPointState point in allowed)
            {
                int id = point.UnitId;
                Require(deaths.Contains(id) && point.Current == null && Owner(predictedPoints, id).Current == null &&
                    !point.OuterBoss && !point.SpawnedInPreview && survivors.All(unit => unit.Id != id) &&
                    actual.Enchantments?.RetainedUnits.Any(actor => actor.Unit.Id == id) != true &&
                    actualPoints.Groups.All(group => !group.Occupants.Contains(id)) &&
                    point.LastKnown is { Team: CombatTeam.Enemy, OuterBossAnchor: false, PreviewCopyId: null } reference &&
                    actualPoints.Groups.Single(group => group.RoomIndex == reference.RoomIndex &&
                        group.Team == reference.Team && group.PreviewCopyId == null).Outside[reference.Index] == false,
                    "A raw reference was not the last real point of an unowned enemy killed in this coroutine.");
                Require(terminalSamples.All(sample => !sample.GetProperty("LivingUnitIds").Deserialize<int[]>()!.Contains(id)) &&
                    rawSamples.Length != 0 && rawSamples.All(sample =>
                    SamePoint(Owner(sample.Raw, id), point) && SamePoint(Owner(sample.Observed, id), point)) &&
                    SamePoint(Owner(Points(stableSample, "Raw"), id), point) &&
                    Owner(Points(stableSample, "Canonical"), id) is { Current: null, LastKnown: null } &&
                    Owner(settled.Spawn.Train.Context!.SpawnPoints!, id) is { Current: null, LastKnown: null },
                    "The native terminal capture must corroborate the raw pointer and its later decision projection.");
            }

            // Check a comparison copy only. The native observations and their diagnostics remain as captured.
            var ids = allowed.Select(point => point.UnitId).ToHashSet();
            return actual.WithSpawnPoints(actualPoints.WithState(actualPoints.Groups, actualPoints.Units.Select(point =>
                ids.Contains(point.UnitId) ? new UnitSpawnPointState(point.UnitId, null, null, point.OuterBoss, point.SpawnedInPreview) : point).ToArray()));
        }
    }

    private static bool MovedDreg(RoomCombatState before, RoomCombatState after)
    {
        CombatUnit[] incoming = before.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        CombatUnit[] outgoing = after.Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        if (incoming.Length != 2 || outgoing.Length != 2 || incoming[1].AssetKey != "Dreg" || outgoing[0].Id != incoming[1].Id)
            return false;
        BattleSpawnPoints prior = before.Context!.SpawnPoints!;
        BattleSpawnPoints actual = after.Context!.SpawnPoints!;
        SpawnPointReference? from = Owner(prior, incoming[1].Id).Current;
        SpawnPointReference? to = Owner(actual, incoming[1].Id).Current;
        return from is { Index: 1 } && to is { Index: 0 } && from.RoomIndex == to.RoomIndex &&
            actual.Groups.Where(group => group.RoomIndex != after.RoomIndex).All(group =>
                group.Occupants.SequenceEqual(prior.Groups.Single(other => other.RoomIndex == group.RoomIndex &&
                    other.Team == group.Team && other.PreviewCopyId == group.PreviewCopyId).Occupants));
    }

    private static bool SamePoint(UnitSpawnPointState candidate, UnitSpawnPointState reference) =>
        candidate.Current == null && candidate.LastKnown is SpawnPointReference point && reference.LastKnown is SpawnPointReference last &&
        point.RoomIndex == last.RoomIndex && point.Team == last.Team && point.Index == last.Index &&
        point.OuterBossAnchor == last.OuterBossAnchor && point.PreviewCopyId == last.PreviewCopyId;
    private static UnitSpawnPointState Owner(BattleSpawnPoints points, int id) => points.Units.Single(unit => unit.UnitId == id);
    private static BattleSpawnPoints Points(FixtureValue row, string field) => row.GetProperty(field).Deserialize<BattleSpawnPoints>()!;
    private static bool HasDifference(FixtureValue row) => row.GetProperty("Difference").ValueKind == FixtureKind.String;
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(expected, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidDataException(message);
    }
}
