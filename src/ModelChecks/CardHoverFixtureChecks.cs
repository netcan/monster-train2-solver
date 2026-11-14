using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardHoverFixtureChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Same(TrainCombatState expected, TrainCombatState actual, string stage)
    {
        string? difference = ModelJson.Difference(Json(expected), Json(actual));
        Require(difference == null, stage + ": " + difference);
    }
    internal static bool GameplayRng(FixtureValue before, FixtureValue after)
    {
        var left = before.GetProperty("Streams").EnumerateArray().ToArray();
        var right = after.GetProperty("Streams").EnumerateArray().ToArray();
        return left.Length == 16 && right.Length == 16 && before.GetProperty("Unity").GetArrayLength() == 4 &&
            before.GetProperty("Unity").ContentEquals(after.GetProperty("Unity")) && left.Zip(right).All(pair =>
                pair.First.GetProperty("Id").GetString() == pair.Second.GetProperty("Id").GetString() &&
                (pair.First.GetProperty("Id").GetString() is "NonDeterministic" or "Chatter" || pair.First.ContentEquals(pair.Second)));
    }
    internal static void Match(FixtureValue row)
    {
        string label = row.GetProperty("Label").GetString()!;
        int targetId = row.GetProperty("TargetId").GetInt32();
        var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var native = row.GetProperty("CopiedActual").Deserialize<TrainCombatState>()!;
        var restored = row.GetProperty("After").Deserialize<TrainCombatState>()!;
        var rule = row.GetProperty("Rule").Deserialize<CardPlayRule>()!;
        int dropId = row.TryGetProperty("DropTargetId", out var dropTarget) ? dropTarget.GetInt32() : targetId;
        var action = new PlayCardAction(row.GetProperty("CardId").GetInt32(), row.GetProperty("RoomIndex").GetInt32(), targetUnitId: dropId);
        var observed = row.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
        Require(GameplayRng(row.GetProperty("RngBefore"), row.GetProperty("RngAfter")), "Card hover changed gameplay RNG: " + label);
        string frozen = Json(before);
        var result = CardHoverPreviewModel.Apply(before, rule, action);
        Require(result.Supported, "Card hover unsupported: " + label + ": " + result.UnsupportedReason);
        Same(result.CopiedBefore!, row.GetProperty("CopiedBefore").Deserialize<TrainCombatState>()!, label + " copied before");
        Same(result.AfterEffects!, native, label + " after effects");
        var predictedActors = result.AfterEffects!.Rooms.SelectMany(room => room.Units).Concat(result.TemporaryRemovedUnits).ToArray();
        CombatUnit Predicted(int id)
        {
            Require(predictedActors.Count(unit => unit.Id == id) == 1,
                label + " missing/duplicate temporary actor " + id + "; captured ids=" + string.Join(",", predictedActors.Select(unit => unit.Id)));
            return predictedActors.Single(unit => unit.Id == id);
        }
        var predictedTarget = Predicted(targetId);
        string? difference = ModelJson.Difference(Json(predictedTarget), Json(observed));
        Require(difference == null, label + " temporary target: " + difference);
        if (row.TryGetProperty("ActualTargets", out var actualTargets))
        {
            var actors = actualTargets.Deserialize<CombatUnit[]>()!;
            Require(actors.Select(unit => unit.Id).SequenceEqual(result.CopiedIds), "Incomplete native temporary actor roster: " + label);
            foreach (var actor in actors)
            {
                var predicted = Predicted(actor.Id);
                difference = ModelJson.Difference(Json(predicted), Json(actor));
                Require(difference == null, label + " temporary actor " + actor.Id + ": " + difference);
            }
        }
        Same(result.Restored!, restored, label + " restored primary");
        Same(result.Restored!, row.GetProperty("Cleared").Deserialize<TrainCombatState>()!, label + " cleared visuals");
        Require(result.CopiedIds.SequenceEqual(row.GetProperty("CopiedIds").EnumerateArray().Select(id => id.GetInt32())) &&
            Json(before) == frozen, "Card hover changed its parent or copied different actors: " + label);
        string expected = Json(result.AfterEffects!), expectedRemoved = Json(result.TemporaryRemovedUnits);
        Parallel.For(0, 16, branch =>
        {
            var fork = CardHoverPreviewModel.Apply(before, rule, action);
            Require(fork.Supported && Json(fork.AfterEffects!) == expected && Json(fork.Restored!) == Json(restored) &&
                Json(fork.TemporaryRemovedUnits) == expectedRemoved && Json(before) == frozen,
                "Card hover immutable branch differs: " + label + "/" + branch);
        });
    }
}
