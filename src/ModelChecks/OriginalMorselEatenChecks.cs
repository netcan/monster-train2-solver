using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMorselEatenChecks
{
    private static readonly string[] Originals = { "0347419a-2e3a-492d-a054-f6e340be3db2", "e16040b1-eac0-4769-bc89-90bc2eb31944",
        "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6", "b24a8e4f-294f-4d24-9bc2-9c14474d83d1", "536e6de0-7ccf-412a-9a4e-ec55ab3988e5" };
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        var action = new PlayCardAction(ids[index], index < 3 ? index : (index - 3) / 2, playerPosition: index < 3 ? 0 : (index - 3) % 2 + 1);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalMorselEatenPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Morsel witness.");
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalOpeningMorselsCaptureFailures",
            "OriginalOpeningMorselsMismatches", "OriginalOpeningMorselsUnsupported", "OriginalOpeningMorselsPending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Nonzero " + name);
        Require(root.GetProperty("OriginalMorselVoidIgnoredUpgradeId").GetString()!.Length > 0 ||
            root.GetProperty("OriginalMorselVoidIgnoredCopy").GetBoolean() || root.GetProperty("OriginalMorselVoidIgnoredClassFilter").GetBoolean(), "Unused original status fields were removed.");
        int[] ids = root.GetProperty("OriginalMorselEatenIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 8 && actions.Length == 8, "Missing three anchors or five original summons.");
        for (int i = 3; i < 8; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, actual = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 3]);
            Require(card.DataId == Originals[i - 3] && rule.Cost == 0 && rule.ExternalInteractions.Count == 0 &&
                rule.SpawnUnit!.Triggers.Any(trigger => trigger.Kind == "OnEaten"), "Original Morsel identity, cost or feeding projection differs.");
            Require(before.Energy == actual.Energy && actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.SpawnerCardId == ids[i]) == 1, "Original summon cost or spawned body differs.");
        }
        var eaten = new HashSet<int>(); int controls = 0, foodDelta = 0; bool voidPositive = false;
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
                Equal(predicted, actual, "complete native feeding state"); return result;
            }
            RoomCombatResult result = Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Feeding mutated its parent.");
            var meals = result.Events.Where(item => item.Kind == "Eaten").ToArray();
            int delta = row.GetProperty("FoodStatAfter").GetInt32() - row.GetProperty("FoodStatBefore").GetInt32(); foodDelta += delta;
            Require(delta == (team == CombatTeam.Player ? meals.Length : 0), "Wrong native food run-stat delta.");
            if (meals.Length == 0) controls++;
            foreach (CombatEvent meal in meals)
            {
                CombatUnit food = before.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.Id == meal.Target);
                Require(ids.Skip(3).Contains(food.SpawnerCardId) && eaten.Add(food.SpawnerCardId), "Unknown or repeated original meal.");
                Require(!actual.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == food.Id) &&
                    actual.Context!.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Any(card => card.InstanceId == food.SpawnerCardId), "Original source card did not enter Eaten.");
                Require(!result.Events.Any(item => item.Kind == "Death" && item.Target == food.Id), "Eating incorrectly caused a damage death.");
                if (food.SpawnerCardId == ids[6])
                {
                    CombatUnit feeder = actual.Rooms[floor].Units.Single(unit => unit.SpawnerCardId == ids[floor]);
                    CombatUnit old = before.Rooms[floor].Units.Single(unit => unit.Id == feeder.Id);
                    Require(feeder.Status("lifesteal")?.Stacks == 1 && old.Status("lifesteal") == null && feeder.Attack == old.Attack + 6,
                        "Void original attack upgrade or lifesteal was lost.");
                    Require(feeder.MaxHealth == old.MaxHealth + 6 && feeder.Status("damage shield")?.Stacks == 1,
                        "Jeweler original health upgrade or team-independent feeder shield was lost.");
                    voidPositive = true;
                }
            }
        }
        Require(eaten.Count == 5 && foodDelta == 5 && controls > 0 && voidPositive, "Missing original meals, controls or Void positive effect.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ORIGINAL-MORSEL-EATEN PASS: five unchanged original summons and meals, Void upgrade/lifesteal with unused source fields, Eaten routing, {controls} controls, exact full state/RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
