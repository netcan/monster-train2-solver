using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossMaximumCardPreviewChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Same(TrainCombatState expected, TrainCombatState actual, string stage)
    {
        string? difference = ModelJson.Difference(Json(expected), Json(actual));
        Require(difference == null, stage + ": " + difference);
    }
    private static PlayCardAction Action(FixtureValue row) => new(row.GetProperty("CardId").GetInt32(),
        row.GetProperty("RoomIndex").GetInt32(), targetUnitId: row.GetProperty("TargetId").GetInt32());
    private static CardHoverPreviewResult Apply(TrainCombatState train, FixtureValue row) =>
        CardHoverPreviewModel.Apply(train, row.GetProperty("Rule").Deserialize<CardPlayRule>()!, Action(row));
    // Probe room selection is an explicit input under combat-preview suppression.
    // Derive its sole observed state change instead of importing the next native root.
    private static TrainCombatState Focus(TrainCombatState source, int room)
    {
        var context = source.Context!.WithQueryFrame(source.Context.QueryFrame!.With(selectedRoomIndex: room));
        return EnchantmentWorldModel.Rebase(new TrainCombatState(source.Rooms.Select(floor => new RoomCombatState(
            floor.RoomIndex, floor.Deployment, floor.Units, floor.ExternalInteractions, context, floor.Preview,
            floor.ActiveCompanionBossIds)).ToArray(), source.Movement, source.EnemySlotsPerRoom, context));
    }
    private static bool SameGameplayRng(FixtureValue before, FixtureValue after)
    {
        var left = before.GetProperty("Streams").EnumerateArray().ToArray();
        var right = after.GetProperty("Streams").EnumerateArray().ToArray();
        return left.Length == 16 && right.Length == 16 && before.GetProperty("Unity").GetArrayLength() == 4 &&
            before.GetProperty("Unity").ContentEquals(after.GetProperty("Unity")) && left.Zip(right).All(pair =>
                pair.First.GetProperty("Id").GetString() == pair.Second.GetProperty("Id").GetString() &&
                (pair.First.GetProperty("Id").GetString() is "NonDeterministic" or "Chatter" || pair.First.ContentEquals(pair.Second)));
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeLifeLinkMaximumCardHover" &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("AuthoredCards").GetBoolean() &&
            root.GetProperty("OriginalBosses").GetBoolean() &&
            root.GetProperty("OriginalCounter").GetInt32() == root.GetProperty("FinalCounter").GetInt32() &&
            root.GetProperty("OriginalSuppression").GetInt32() == root.GetProperty("FinalSuppression").GetInt32(),
            "Incomplete native card-hover evidence.");
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(rows.Length == 51 && rows.Count(row => row.GetProperty("MismatchedFocus").GetBoolean()) == 1 &&
            rows[^1].GetProperty("MismatchedFocus").GetBoolean(), "Missing fifty safe hovers and final mismatched-focus control.");
        var positive = rows[..50];
        Require(positive.GroupBy(row => row.GetProperty("Label").GetString()).Count() == 10 &&
            positive.GroupBy(row => row.GetProperty("Label").GetString()).All(group => group.Count() == 5 &&
                group.Select(row => row.GetProperty("TargetId").GetInt32()).Distinct().Count() == 5),
            "Missing five-body main-effect/OnCast hover matrix.");
        var initial = positive[0].GetProperty("Before").Deserialize<TrainCombatState>()!;
        var chained = initial;
        string frozen = Json(initial);
        var first = Apply(initial, positive[0]);
        Require(first.Supported && !RoomCombatModel.Resolve(first.CopiedBefore!.Rooms[3]).Supported,
            "Card-hover evidence incorrectly admitted arbitrary mixed-copy room combat.");
        using (var keys = new BattleStateKeys())
            Require(keys.SnapshotKey(initial) != keys.SnapshotKey(first.CopiedBefore), "Search keys merged primary and card-hover actor planes.");
        int onCast = 0, roomSweep = 0, counterChanges = 0;
        foreach (var row in rows)
        {
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            Require(row.GetProperty("NativeReturned").GetBoolean() && row.GetProperty("QueueEmpty").GetBoolean() &&
                row.GetProperty("TemporaryDisabled").GetBoolean() && row.GetProperty("EnableCount").GetInt32() == 1 &&
                row.GetProperty("DisableCount").GetInt32() == 1 &&
                before.Context!.QueryFrame!.SelectedRoomIndex == row.GetProperty("SelectedRoomIndex").GetInt32() &&
                SameGameplayRng(row.GetProperty("RngBefore"), row.GetProperty("RngAfter")),
                "Incomplete native hover copy/cancel or changed gameplay RNG.");
            var result = Apply(before, row);
            if (row.GetProperty("MismatchedFocus").GetBoolean())
            {
                Require(!result.Supported && result.UnsupportedReason!.Contains("matching selected-room focus") &&
                    row.GetProperty("Events").GetArrayLength() == 2 && before.Context.LifeLinks!.Actors.Zip(after.Context!.LifeLinks!.Actors)
                        .Count(pair => pair.First.Health != pair.Second.Health) == 4,
                    "Mismatched native focus must demonstrate primary HP leakage and remain rejected.");
                continue;
            }
            string label = row.GetProperty("Label").GetString()!;
            Require(result.Supported, "Card hover unsupported: " + label + "/" + row.GetProperty("TargetId").GetInt32() + ": " + result.UnsupportedReason);
            Same(result.CopiedBefore!, row.GetProperty("CopiedBefore").Deserialize<TrainCombatState>()!, label + " copied before");
            Same(result.AfterEffects!, row.GetProperty("CopiedActual").Deserialize<TrainCombatState>()!, label + " after effects");
            Same(result.Restored!, after, label + " restored");
            Same(result.Restored!, row.GetProperty("Cleared").Deserialize<TrainCombatState>()!, label + " cleared visuals");
            Require(result.CopiedIds.SequenceEqual(row.GetProperty("CopiedIds").EnumerateArray().Select(id => id.GetInt32())) &&
                row.GetProperty("Events").GetArrayLength() == 0, "Copied native target roster or live HP notifications differ.");
            chained = Focus(chained, row.GetProperty("RoomIndex").GetInt32());
            Same(chained, before, label + " frozen-root room selection");
            var step = Apply(chained, row);
            Require(step.Supported, "Frozen-root card hover unsupported: " + step.UnsupportedReason);
            chained = step.Restored!;
            Same(chained, after, label + " frozen-root hover cancellation");
            if (row.GetProperty("Rule").GetProperty("OnCastEffects").GetArrayLength() > 0) onCast++;
            if (label == "room-sweep") roomSweep++;
            if (before.Context.LifeLinks!.SharedPreviewHpChange != after.Context!.LifeLinks!.SharedPreviewHpChange) counterChanges++;
        }
        string expected = Json(chained);
        Parallel.For(0, 16, branch =>
        {
            var train = initial;
            foreach (var row in positive)
            {
                train = Focus(train, row.GetProperty("RoomIndex").GetInt32());
                var result = Apply(train, row);
                Require(result.Supported, "Parallel native card-hover chain unsupported: " + result.UnsupportedReason);
                train = result.Restored!;
            }
            Require(Json(train) == expected && Json(initial) == frozen, "Native card-hover branch diverged or mutated its parent: " + branch);
        });
        Require(onCast == 15 && roomSweep == 5 && counterChanges > 0, "Missing OnCast, room sweep or retained native TFB accumulator.");
        Console.WriteLine($"TRUE-FINAL-BOSS-MAXIMUM-CARD-PREVIEW PASS: 50 native hovers, {onCast} OnCast, {roomSweep} room sweeps, " +
            $"{counterChanges} retained-counter changes; complete copied/effect/cancel successors, gameplay RNG, one unsafe-focus rejection and 16 frozen-root chains.");
    }
}
