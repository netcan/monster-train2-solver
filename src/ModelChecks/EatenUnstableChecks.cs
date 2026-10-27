using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class EatenUnstableChecks
{
    private const string Reason = "Post-combat feeding must preserve the newly applied shield and the latest attacker.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, original: true);
        if (replay.HasValue)
        {
            Check(replay.Value, original: false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("EATEN-UNSTABLE PASS:47 native inputs, original false survival and corrected full terminal state/RNG; independentPair=" + replay.HasValue);
    }
    private static void Check(FixtureValue fixture, bool original)
    {
        // The original model predicted survival. Keep its recorded outcome
        // explicit while verifying every new successor against the native loss.
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason,
            original ? Historical : null, boundaryOutcome: RoomOutcome.PlayerDefeated,
            historicalBoundaryOutcome: original ? RoomOutcome.Exchanged : null);
        Require(rows.Length == 47 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing46 native prefix inputs and the fifth-turn combat.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 5 && before.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.IsPyre).Health == 10 &&
            before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Any(unit =>
                unit.Id == 17 && unit.AssetKey == "Boss_Level6_FeedDamageShield" && unit.Health == 12 &&
                unit.Status("eatmany")?.Stacks == 1 && unit.Status("unstable")?.Stacks == 5),
            "Missing the natural last-meal/self-damage/Unstable boundary.");
        NonfinalMealControl(before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2));
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.PlayerDefeated &&
                NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(actual, true),
                "Parallel eaten-Unstable boundary differs from full native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Eaten-Unstable changed its parent.");
    }
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        Require(predicted.Spawn.Turn == 6 && predicted.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.IsPyre).Health == 4 && actual.Spawn.Turn == 5 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => !unit.IsPyre || unit.Health <= 0),
            "Wrong historical false-survival boundary.");
    }
    internal static void Counterexample(FixtureValue fixture)
    {
        var stage = fixture.GetProperty("Stages").EnumerateArray().Single(row =>
            row.GetProperty("Index").GetInt32() == 49);
        var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        Require(before.RoomIndex == 2 && before.Units.Single(unit => unit.Id == 16).Status("damage shield")?.Stacks == 1 &&
            before.Units.Single(unit => unit.Id == 17).AssetKey == "Boss_Level6_FeedDamageShield" &&
            actual.Units.Single(unit => unit.Id == 16).Health == 38 &&
            actual.Units.Single(unit => unit.Id == 16).Status("damage shield")?.Stacks == 1 &&
            actual.Units.Single(unit => unit.Id == 16).LastAttackerId == 7,
            "Missing the consumed-shield/second-attacker/post-combat-refeed native counterexample.");
        var result = RoomCombatModel.Resolve(before);
        Require(result.Supported, result.UnsupportedReason ?? "Unsupported native refeed room.");
        var heavy = result.State!.Units.Single(unit => unit.Id == 16);
        Console.WriteLine($"REFEED-ROOM hp={heavy.Health} shield={heavy.Status("damage shield")?.Stacks ?? 0} lastAttacker={heavy.LastAttackerId}");
        Require(heavy.Health == 38 && heavy.Status("damage shield")?.Stacks == 1 && heavy.LastAttackerId == 7,
            "Post-combat feeding lost the new shield or the second attacker's identity.");
        Require(JsonSerializer.Serialize(result.State.Units) == JsonSerializer.Serialize(actual.Units) &&
            JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(result.State.Context!)) ==
            JsonSerializer.Serialize(BattleTurnChecks.ComparableContext(actual.Context!)),
            "The complete refeed room state/context differs from native.");
        NonfinalMealControl(before);
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var root = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var final = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 47 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native failure boundary.");
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var ended = BattleTurnModel.EndTurn(root);
            Require(ended.Supported && ended.Outcome == RoomOutcome.PlayerDefeated &&
                NativeBranchObservationChecks.Key(ended.State!, true) == NativeBranchObservationChecks.Key(final, true),
                "The complete terminal EndTurn/state/RNG differs from native.");
        });
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Refeed branches changed their parent.");
        Console.WriteLine("EATEN-UNSTABLE PASS: native shield/attacker, complete room/context and four immutable terminal EndTurns/state/RNG.");
    }
    private static void NonfinalMealControl(RoomCombatState before)
    {
        // A nonfinal meal is not marked as eaten-dead. If its own callback
        // kills it, Unstable must still fire; suppressing all food is incorrect.
        var repeatMeal = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        var food = repeatMeal["Units"]!.AsArray().Single(unit => unit!["Id"]!.GetValue<int>() == 17)!;
        foreach (var key in new[] { "Statuses", "StatusRegistry" })
            food[key]!.AsArray().Single(status => status!["Id"]!.GetValue<string>() == "eatmany")!["Stacks"] = 2;
        var repeated = RoomCombatModel.Resolve(repeatMeal.Deserialize<RoomCombatState>(ModelJson.Options)!);
        Require(repeated.Supported && repeated.State!.Units.Single(unit => unit.Id == 16).Status("damage shield") == null &&
            repeated.Events.Any(item => item.Kind == "Poison" && item.Target == 16),
            "A nonfinal food death incorrectly suppressed its ordinary Unstable explosion.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
