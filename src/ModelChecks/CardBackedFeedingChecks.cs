using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardBackedFeedingChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("CardBackedFeeding").GetBoolean() && root.GetProperty("NativeWon").GetBoolean(), "Missing food-card native win.");
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalOpeningMorselsCaptureFailures",
            "OriginalOpeningMorselsMismatches", "OriginalOpeningMorselsUnsupported", "OriginalOpeningMorselsPending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Nonzero " + name);
        int sourceCards = 0, equipped = 0, survived = 0, repeatedFinal = 0, controls = 0;
        foreach (FixtureValue row in root.GetProperty("NativeMorselFeeding").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int floor = row.GetProperty("RoomIndex").GetInt32(); CombatTeam team = row.GetProperty("Team").Deserialize<CombatTeam>();
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            RoomCombatResult Replay()
            {
                RoomCombatResult result = RoomCombatModel.ApplyFeeding(before.Rooms.Single(room => room.RoomIndex == floor), team);
                Require(result.Supported, result.UnsupportedReason ?? "Feeding unsupported.");
                TrainCombatState predicted = CardSpellModel.WithContext(new(before.Rooms.Select(room => room.RoomIndex == floor ? result.State! : room).ToArray(),
                    before.Movement, before.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                Equal(predicted, actual, "complete native feeding state");
                return result;
            }
            RoomCombatResult result = Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Feeding mutated its parent.");
            Require(row.GetProperty("FoodStatAfter").GetInt32() - row.GetProperty("FoodStatBefore").GetInt32() ==
                (team == CombatTeam.Player ? result.Events.Count(item => item.Kind == "Eaten") : 0), "Wrong native food run-stat delta.");
            if (!result.Events.Any(item => item.Kind == "Eaten")) controls++;
            foreach (CombatEvent meal in result.Events.Where(item => item.Kind == "Eaten"))
            {
                CombatUnit food = before.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.Id == meal.Target);
                if (food.SpawnerCardId <= 0) continue;
                sourceCards++;
                bool remains = actual.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == food.Id);
                if (remains) { survived++; Require(food.Status("eatmany")?.Stacks > 1, "Ordinary food survived."); }
                else
                {
                    Require(actual.Context!.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Any(card => card.InstanceId == food.SpawnerCardId), "Source card did not enter Eaten.");
                    if (food.Status("eatmany")?.Stacks == 1) repeatedFinal++;
                    if (food.EquipmentCards?.Count > 0)
                    {
                        equipped++;
                        foreach (int id in food.EquipmentCards)
                            Require(actual.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id), "Food equipment did not return to Exhausted.");
                    }
                }
                Require(!result.Events.Any(item => item.Kind == "Death" && item.Target == food.Id), "Eating incorrectly caused a damage death.");
            }
        }
        Require(sourceCards >= 4 && equipped > 0 && survived > 0 && repeatedFinal > 0 && controls > 0,
            $"Missing feeding cases: cards={sourceCards}, equipment={equipped}, survival={survived}, final={repeatedFinal}, controls={controls}.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"CARD-BACKED-FEEDING PASS: {sourceCards} card-backed meals, {equipped} equipment returns, {survived} surviving meals, {repeatedFinal} final repeated meals, {controls} controls; complete state/RNG, all plays/turns and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
