using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ForcedFeedingChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ForcedFeeding").GetBoolean() && root.GetProperty("ForcedFeedingDeploymentRejected").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean(), "Missing native forced-feeding witness.");
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "ForcedFeedingCaptureFailures",
            "ForcedFeedingMismatches", "ForcedFeedingUnsupported", "ForcedFeedingPending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Nonzero " + name);
        int positive = 0, controls = 0, survived = 0, final = 0;
        foreach (FixtureValue row in root.GetProperty("ForcedFeedingRecords").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int[] ids = row.GetProperty("Targets").Deserialize<int[]>()!;
            int meals = 0;
            TrainCombatState predicted = before;
            if (ids.Length > 0)
            {
                RoomCombatState room = before.Rooms.Single(room => room.Units.Any(unit => unit.Id == ids[0]));
                string parent = JsonSerializer.Serialize(room, ModelJson.Options);
                RoomCombatResult result = RoomCombatModel.ApplyFeeding(room, CombatTeam.Player, ids);
                Require(result.Supported, result.UnsupportedReason ?? "Forced feeding unsupported.");
                predicted = CardSpellModel.WithContext(new(before.Rooms.Select(item => item.RoomIndex == room.RoomIndex ? result.State! : item).ToArray(),
                    before.Movement, before.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                meals = result.Events.Count(item => item.Kind == "Eaten");
                foreach (CombatEvent meal in result.Events.Where(item => item.Kind == "Eaten"))
                {
                    CombatUnit food = room.Units.Single(unit => unit.Id == meal.Target);
                    CombatUnit? remaining = actual.Rooms.SelectMany(item => item.Units).FirstOrDefault(unit => unit.Id == food.Id);
                    if (remaining != null)
                    {
                        survived++;
                        Require(food.Status("eatmany")!.Stacks - 1 == remaining.Status("eatmany")!.Stacks, "Forced feeding spent the wrong Eatmany count.");
                    }
                    else
                    {
                        final++;
                        Require(food.SpawnerCardId == 0 || actual.Context!.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Any(card => card.InstanceId == food.SpawnerCardId),
                            "Final meal did not move the source card to Eaten.");
                    }
                }
                Parallel.For(0, 16, _ => Equal(RoomCombatModel.ApplyFeeding(room, CombatTeam.Player, ids).State, result.State, "immutable feeding branch"));
                Require(parent == JsonSerializer.Serialize(room, ModelJson.Options), "Forced feeding mutated its parent.");
            }
            Equal(predicted, actual, "complete native forced-feeding state and RNG");
            Require(row.GetProperty("FoodStatAfter").GetInt32() - row.GetProperty("FoodStatBefore").GetInt32() == meals, "Native meal statistic differs.");
            if (meals > 0) positive++; else controls++;
        }
        Require(positive >= 2 && controls >= 1 && survived > 0 && final > 0, "Missing positive, no-feeder, surviving or final-meal controls.");
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Any(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms[0].Deployment &&
            !BattleActionModel.EnumerateSupportedPlays(row.GetProperty("Before").Deserialize<BattleTurnState>()!).Any(action =>
                row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == "236215da-c1dd-40b7-8793-27ce327a7e78")),
            "Model did not enforce the native deployment cast gate.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"FORCED-FEEDING PASS: {positive} positive casts, {controls} controls, {survived} surviving and {final} final meals; full state/RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
