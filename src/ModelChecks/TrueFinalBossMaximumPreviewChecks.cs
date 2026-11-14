using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossMaximumPreviewChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static bool SameGameplayRng(FixtureValue before, FixtureValue after)
    {
        var left = before.GetProperty("Streams").EnumerateArray().ToArray();
        var right = after.GetProperty("Streams").EnumerateArray().ToArray();
        return left.Length == 16 && right.Length == 16 && before.GetProperty("Unity").GetArrayLength() == 4 &&
            before.GetProperty("Unity").ContentEquals(after.GetProperty("Unity")) && left.Zip(right).All(pair =>
                pair.First.GetProperty("Id").GetString() == pair.Second.GetProperty("Id").GetString() &&
                (pair.First.GetProperty("Id").GetString() is "NonDeterministic" or "Chatter" || pair.First.ContentEquals(pair.Second)));
    }
    private static string Primary(TrainCombatState train)
    {
        var node = JsonNode.Parse(Json(train))!;
        void Visit(JsonNode? current)
        {
            if (current is JsonObject obj)
                foreach (var pair in obj.ToArray())
                {
                    if (pair.Key == "NextPreviewCopyId" && pair.Value != null) obj[pair.Key] = 0;
                    else Visit(pair.Value);
                }
            else if (current is JsonArray array) foreach (var child in array) Visit(child);
        }
        Visit(node); return node.ToJsonString();
    }
    private static TrainCombatState Train(RoomCombatState room)
    {
        var world = room.Context!.Enchantments!;
        return new TrainCombatState(world.Rooms.Select(floor => new RoomCombatState(floor.RoomIndex, floor.Deployment,
            floor.Units, floor.ExternalInteractions, room.Context, floor.Preview, floor.ActiveCompanionBossIds)).ToArray(),
            world.Movement, world.EnemySlotsPerRoom, room.Context);
    }
    private static RoomCombatResult Apply(RoomCombatState before, FixtureValue row) => UnitHealthModel.ApplyNative(before,
        row.GetProperty("TargetId").GetInt32(), row.GetProperty("Amount").GetInt32(), row.GetProperty("Debuff").GetBoolean(),
        heal: row.GetProperty("Heal").GetBoolean(), floor: row.GetProperty("Floor").GetInt32(),
        decreaseHp: row.GetProperty("DecreaseHp").GetBoolean());
    private static TrainCombatState Step(TrainCombatState train, FixtureValue row)
    {
        int id = row.GetProperty("TargetId").GetInt32();
        var result = Apply(train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)), row);
        Require(result.Supported && result.PendingCallbacks.Count == 0 && result.RetainedUnits.Count == 0,
            "Temporary maximum-health chain is unsupported: " + result.UnsupportedReason);
        return Train(result.State!);
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeLifeLinkMaximumTemporaryCopies" &&
            root.GetProperty("OriginalActorsAndStartingStats").GetBoolean() && root.GetProperty("NativeCompleted").GetBoolean() &&
            root.GetProperty("ControlsRestored").GetBoolean() &&
            root.GetProperty("OriginalMode").GetString() == "On" && root.GetProperty("FinalMode").GetString() == "On" &&
            root.GetProperty("OriginalCounter").GetInt32() == root.GetProperty("FinalCounter").GetInt32() &&
            root.GetProperty("OriginalSuppressionCount").GetInt32() == root.GetProperty("FinalSuppressionCount").GetInt32() &&
            root.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null, "Incomplete native temporary-copy maximum-health evidence.");
        var sessions = root.GetProperty("Sessions").EnumerateArray().ToArray();
        Require(sessions.Length == 30 && sessions.GroupBy(session => session.GetProperty("Mode").GetString())
            .OrderBy(group => group.Key).Select(group => (group.Key, group.Count())).SequenceEqual(new[] { ("Calculating", 10), ("Off", 10), ("On", 10) }),
            "Missing native combat modes or partial/all-body temporary copies.");
        int count = 0, protectedRows = 0, unprotectedRows = 0, borrowedChanges = 0, countersRetained = 0;
        foreach (var session in sessions)
        {
            string mode = session.GetProperty("Mode").GetString()!;
            bool all = session.GetProperty("AllBodiesCopied").GetBoolean();
            var beforePrimary = session.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var afterPrimary = session.GetProperty("After").Deserialize<TrainCombatState>()!;
            Require(session.GetProperty("NativeCancelRestoredPrimary").GetBoolean() &&
                session.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(Primary(beforePrimary), Primary(afterPrimary)) == null &&
                SameGameplayRng(session.GetProperty("RngBefore"), session.GetProperty("RngAfter")), "Native cancellation changed primary gameplay or gameplay RNG.");
            int counter = session.GetProperty("CounterAtCancel").GetInt32();
            Require(counter == session.GetProperty("CounterAfterCancel").GetInt32(), "Native cancellation unexpectedly reset the preview accumulator.");
            if (counter != 0) countersRetained++;
            var rows = session.GetProperty("Rows").EnumerateArray().ToArray();
            Require(rows.Length == (all ? 19 : 12), "Missing native maximum-health preview controls.");
            var initial = Train(rows[0].GetProperty("Before").Deserialize<RoomCombatState>()!);
            var chained = initial; string parent = Json(initial);
            Require(initial.Context!.LifeLinks!.Actors.Count == 5 &&
                initial.Context.LifeLinks.TemporaryPreviewIds?.Count == (all ? 5 : 1) &&
                initial.Context.LifeLinks.Actors.All(actor => actor.HasPreviewHealthCopy == true), "Incorrect temporary or existing battle-preview copied-actor roster.");
            var bosses = initial.Context.Enchantments!.Rooms.SelectMany(room => room.Units)
                .Where(unit => initial.Context.LifeLinks.Actors.Any(actor => actor.Id == unit.Id)).OrderBy(unit => unit.Id).ToArray();
            Require(bosses.Select(unit => unit.AssetKey).SequenceEqual(TrueFinalBossChecks.Assets) &&
                bosses.Select(unit => unit.Health).SequenceEqual(new[] { 1555, 2750, 3500, 3500, 3500 }) && bosses.All(unit => unit.Health == unit.MaxHealth),
                "Temporary controls did not start from original primary boss statistics.");
            Require(!RoomCombatModel.Resolve(initial.Rooms[3]).Supported,
                "Maximum-health API evidence incorrectly admitted arbitrary mixed-copy combat.");
            using (var keys = new BattleStateKeys())
            {
                var links = initial.Context.LifeLinks;
                var different = new LifeLinkContext(links.Actors, links.CalculatingPreview, links.SharedPreviewHpChange,
                    all ? new[] { links.Actors[0].Id } : links.Actors.Select(actor => actor.Id).ToArray());
                Require(keys.SnapshotKey(links) != keys.SnapshotKey(different), "Search keys merged different temporary shared-health ownership.");
            }
            foreach (var row in rows)
            {
                string label = row.GetProperty("Label").GetString()!; int target = row.GetProperty("TargetId").GetInt32();
                var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
                var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
                Require(before.Preview && before.Context!.LifeLinks!.CalculatingPreview == (mode == "Calculating") &&
                    before.Context.LifeLinks.TemporaryPreviewIds!.Contains(target) &&
                    before.Context.LifeLinks.TemporaryPreviewIds.SequenceEqual(before.Context.Enchantments!.TemporaryPreviewUnitIds!),
                    "Missing native temporary actor or preview-mode identity.");
                bool protection = row.GetProperty("Protected").GetBoolean();
                Require(LifeLinkModel.Protected(before.Context.LifeLinks, target) == protection &&
                    LifeLinkModel.Protected(before.Context.LifeLinks, target, forPrediction: true) == row.GetProperty("ProtectedForPrediction").GetBoolean(),
                    "Temporary native live/prediction life-link protection differs.");
                if (protection) protectedRows++; else unprotectedRows++;
                var result = Apply(before, row);
                string? difference = result.Supported ? ModelJson.Difference(Json(result.State!), Json(actual)) : result.UnsupportedReason;
                Require(result.Supported && difference == null && result.PendingCallbacks.Count == 0,
                    $"Temporary maximum-health successor differs: {mode}/{all}/{target}/{label}: {difference}");
                chained = Step(chained, row);
                difference = ModelJson.Difference(Json(chained), Json(Train(actual)));
                Require(difference == null, "Frozen-root temporary chain differs: " + difference);
                Require(row.GetProperty("Events").GetArrayLength() == 0 && SameGameplayRng(row.GetProperty("RngBefore"), row.GetProperty("RngAfter")),
                    "Native temporary maximum-health API emitted live HP events or changed RNG.");
                if (before.Context.LifeLinks.Actors.Where(actor => actor.Id != target).Any(actor =>
                    actual.Context!.LifeLinks!.Actors.Single(other => other.Id == actor.Id).Health != actor.Health)) borrowedChanges++;
                count++;
            }
            string expected = Json(chained);
            Parallel.For(0, 16, branch =>
            {
                var state = initial; foreach (var row in rows) state = Step(state, row);
                Require(Json(state) == expected && Json(initial) == parent, "Temporary maximum-health concurrent branch diverged: " + branch);
            });
        }
        Require(count == 465 && protectedRows > 0 && unprotectedRows > 0 && borrowedChanges > 0 && countersRetained > 0,
            "Missing protected/unprotected, borrowed-owner or retained-accumulator evidence.");
        Console.WriteLine($"TRUE-FINAL-BOSS-MAXIMUM-PREVIEW PASS: {count} real API calls across 30 native temporary-copy sessions; Off/On/Calculating modes, partial/all-body copies, independent HP/max, borrowed primary links, live/prediction protection, native cancellation and complete gameplay state/RNG; control mode/counter/suppression restored, 16 immutable concurrent chains each. Full card-hover/UI preview remains separate.");
    }
}
