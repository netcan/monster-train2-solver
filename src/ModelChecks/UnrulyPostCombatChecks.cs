using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnrulyPostCombatChecks
{
    private const string Scenario = "SoulSavior_R3_BossBattle_DuplicateEnemy";
    private const string Boss = "Boss_SoulSavior_R3_Unruly_DuplicateEnemy";
    private const string Decoy = "R3_Horde_T1_Decoy";
    private const string Copy = "CardEffectCopyUnits";
    private const string Fallback = "CardEffectSpawnHeroIfNoOtherHeroUnits";

    internal static void Native(FixtureValue root, bool replay)
    {
        Require(Int(root, "Schema") == 1 && Text(root, "Boundary") == "OriginalUnrulyPostCombat" &&
            Text(root, "GameVersion") == Game231FixtureCatalog.Version && Text(root, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(root, "Scenario") == Scenario && Text(root, "BossKey") == Boss && Text(root, "DecoyKey") == Decoy &&
            root.GetProperty("RailforgedInstalled").GetBoolean() && root.GetProperty("ControlledTestScenario").GetBoolean(),
            "Expected the pinned Railforged Unruly test of the original scenario.");

        var roster = root.GetProperty("SourceEffects").EnumerateArray().Select(effect =>
            (Text(effect, "Type"), Text(effect, "Target"), Text(effect, "Team"), effect.GetProperty("IgnoreBosses").GetBoolean()));
        Require(roster.SequenceEqual([(Copy, "FrontInRoom", "Heroes", true), (Fallback, "FrontInRoom", "Heroes", false)]),
            "Original ordered Unruly effects, targets or filters changed.");

        FixtureValue[] rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(rows.Length == 2 && Text(rows[0], "Control") == "BossOnly" && Text(rows[1], "Control") == "DecoyAlreadyPresent" &&
            rows[0].GetProperty("After").ContentEquals(rows[1].GetProperty("Before")),
            "Expected two continuous native callbacks from the same boss and room.");
        UnitCopyCatalog? unitCopyCatalog = null;
        if (replay)
        {
            Require(root.TryGetProperty("UnitCopyCatalog", out FixtureValue catalog) && catalog.ValueKind == FixtureKind.Object,
                "Strict replay requires the captured native unit-copy catalog.");
            unitCopyCatalog = catalog.Deserialize<UnitCopyCatalog>()!;
        }

        for (int pass = 0; pass < rows.Length; pass++)
        {
            FixtureValue row = rows[pass], rawBefore = row.GetProperty("Before"), rawAfter = row.GetProperty("After");
            string control = Text(row, "Control")!;
            TrainCombatState before = rawBefore.GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = rawAfter.GetProperty("Train").Deserialize<TrainCombatState>()!;
            int roomIndex = Int(rawBefore, "RoomIndex");
            CombatUnit priorBoss = rawBefore.GetProperty("Boss").Deserialize<CombatUnit>()!;
            CombatUnit actualBoss = rawAfter.GetProperty("Boss").Deserialize<CombatUnit>()!;
            RoomCombatState input = before.Rooms.Single(room => room.RoomIndex == roomIndex);
            RoomCombatState output = actual.Rooms.Single(room => room.RoomIndex == roomIndex);
            CombatTrigger postCombat = priorBoss.Triggers.Single(trigger => trigger.Kind == "PostCombat");
            CombatUnit firedBoss = priorBoss.WithTriggers(priorBoss.Triggers.Select(trigger =>
                ReferenceEquals(trigger, postCombat) ? trigger.Fired(trigger.Effects) : trigger).ToArray());
            Require(priorBoss.AssetKey == Boss && priorBoss.Team == CombatTeam.Enemy && priorBoss.IsBoss == true &&
                priorBoss.Health > 0 && postCombat.HasTriggered == (pass > 0) &&
                Json(firedBoss) == Json(actualBoss) && Int(rawAfter, "RoomIndex") == roomIndex &&
                input.Units.Any(unit => unit.Id == priorBoss.Id) && output.Units.Any(unit => unit.Id == priorBoss.Id) &&
                postCombat.Effects.Select(effect => effect.Type).SequenceEqual([Copy, Fallback]),
                control + " lost the original living boss or its ordered PostCombat callback.");

            CombatUnit[] priorHeroes = input.Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            CombatUnit[] heroes = output.Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            CombatUnit[] births = heroes.Where(unit => priorHeroes.All(prior => prior.Id != unit.Id)).ToArray();
            Require(priorHeroes.Length == pass + 1 && heroes.Length == pass + 2 && births is [CombatUnit birth] &&
                birth.AssetKey == Decoy && birth.Health > 0 && birth.Id == before.Context!.NextUnitId &&
                actual.Context!.NextUnitId == birth.Id + 1 &&
                Ids(rawBefore.GetProperty("Heroes")).SequenceEqual(priorHeroes.Select(unit => unit.Id)) &&
                Ids(rawAfter.GetProperty("Heroes")).SequenceEqual(heroes.Select(unit => unit.Id)),
                control + " did not settle exactly one native Decoy at the next unit identity.");
            CombatUnit spawned = births[0];
            UnitSpawnPointState point = actual.Context!.SpawnPoints!.Units.Single(state => state.UnitId == spawned.Id);
            SpawnPointGroupState group = actual.Context.SpawnPoints.Groups.Single(state =>
                state.RoomIndex == roomIndex && state.Team == CombatTeam.Enemy && state.PreviewCopyId == null);
            Require(!point.OuterBoss && point.Current is { OuterBossAnchor: false, PreviewCopyId: null } current &&
                current.RoomIndex == roomIndex && current.Index >= 0 && current.Index < group.Occupants.Count &&
                group.Occupants[current.Index] == spawned.Id &&
                rawBefore.GetProperty("Battle").ContentEquals(rawAfter.GetProperty("Battle")) &&
                rawBefore.GetProperty("BattleTest").ContentEquals(rawAfter.GetProperty("BattleTest")),
                control + " changed the tracked RNG streams or lost the Decoy's physical slot.");

            int target = pass == 0 ? priorBoss.Id : priorHeroes.First(unit => unit.AssetKey == Decoy).Id;
            FixtureValue[] events = row.GetProperty("Events").EnumerateArray().ToArray();
            var observed = events.Select(item => (Int(item, "EffectIndex"), Text(item, "Stage"),
                item.GetProperty("Passed").ValueKind == FixtureKind.Null ? (bool?)null : item.GetProperty("Passed").GetBoolean())).ToArray();
            (int, string?, bool?)[] expected = pass == 0
                ? [(0, "Test", false), (1, "Test", true), (0, "Test", false), (1, "Test", true),
                    (1, "Apply.Enter", null), (1, "Apply.Exit", null)]
                : [(0, "Test", true), (1, "Test", false), (0, "Test", true), (0, "Apply.Enter", null),
                    (0, "Apply.Exit", null), (1, "Test", false)];
            Require(observed.SequenceEqual(expected) && events.All(item =>
                Ids(item.GetProperty("Targets")).SequenceEqual(Int(item, "EffectIndex") == 0 && pass == 0 ? [] : [target])) &&
                Ids(events.Single(item => Text(item, "Stage") == "Apply.Enter").GetProperty("State").GetProperty("Heroes"))
                    .SequenceEqual(priorHeroes.Select(unit => unit.Id)) &&
                Ids(events.Single(item => Text(item, "Stage") == "Apply.Exit").GetProperty("State").GetProperty("Heroes"))
                    .SequenceEqual(heroes.Select(unit => unit.Id)),
                control + " changed the native effect test/apply order or attributed the birth to a different effect.");

            if (replay)
            {
                string parent = Json(before);
                RoomCombatResult result = RoomCombatModel.ApplyUnitPostCombat(input, [], [], unitCopyCatalog: unitCopyCatalog);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged,
                    control + " model PostCombat rejected the native snapshot. " + result.UnsupportedReason);
                TrainCombatState predicted = CardSpellModel.ReplaceRoom(before, result.State!);
                string? difference = ModelJson.Difference(Comparable(predicted), Comparable(actual));
                Require(difference == null && Json(before) == parent, control + " model PostCombat differs. " + difference);
            }
        }
        Console.WriteLine(replay
            ? "NATIVE-UNRULY-POST-COMBAT PASS: original fallback, CopyUnits control, full PostCombat successors and RNG."
            : "NATIVE-UNRULY-OBSERVATION PASS: original fallback, CopyUnits control, physical slots and RNG.");
    }

    private static string Comparable(TrainCombatState state) => Json(new
    {
        Rooms = state.Rooms.Select(room => room.WithContext(null)),
        state.Movement,
        state.EnemySlotsPerRoom,
        Context = BattleTurnChecks.ComparableContext(state.Context)
    });

    private static int[] Ids(FixtureValue value) => value.EnumerateArray().Select(item => item.GetInt32()).ToArray();
    private static int Int(FixtureValue value, string name) => value.GetProperty(name).GetInt32();
    private static string? Text(FixtureValue value, string name) => value.GetProperty(name).GetString();
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool valid, string message) { if (!valid) throw new InvalidDataException(message); }
}
