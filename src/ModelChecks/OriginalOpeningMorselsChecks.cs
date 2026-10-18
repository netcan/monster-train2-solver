using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalOpeningMorselsChecks
{
    internal static void Run()
    {
        foreach (int rooms in new[] { 0, 1, 3, 4 })
            foreach (int floor in new[] { -1, 0, 1, 2, 3, 4 })
                Require(RelicOpeningSpawnModel.Test(floor, rooms) == (floor >= 0 && floor < rooms), "Wrong room bounds.");
        CombatUnit Food(params CombatStatus[] statuses) => new(1, "food", CombatTeam.Player, 0, 1, 1, true, false, false,
            statuses, [new("OnEaten", false, false, false, 1, [])], lastFeederId: 0);
        Require(RoomCombatModel.IsFood(Food()) && !RoomCombatModel.IsFood(Food(new CombatStatus("inedible", 1))) &&
            !RoomCombatModel.IsFood(Food(new CombatStatus("silenced", 1))) && !RoomCombatModel.IsFood(Food(new CombatStatus("muted", 1))),
            "Food admission ignored inedible/silenced/muted.");
        Console.WriteLine("OPENING-MORSELS-PURE PASS: room bounds and native food admission gates.");
    }

    internal static void Native(FixtureValue root)
    {
        Run();
        Require(root.GetProperty("Schema").GetInt32() == 243 && root.GetProperty("OriginalOpeningMorselsPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete opening Morsels witness.");
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalOpeningMorselsCaptureFailures",
            "OriginalOpeningMorselsMismatches", "OriginalOpeningMorselsUnsupported", "OriginalOpeningMorselsPending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Nonzero " + name);
        var tests = root.GetProperty("RelicOpeningMorselTests").EnumerateArray().ToArray();
        int positive = 0, negative = 0;
        foreach (FixtureValue row in tests)
        {
            bool expected = RelicOpeningSpawnModel.Test(row.GetProperty("RoomIndex").GetInt32(), row.GetProperty("RoomCount").GetInt32());
            Require(expected == row.GetProperty("Returned").GetBoolean(), "Wrong native Test result.");
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "Test state");
            if (expected) positive++; else { negative++; Require(row.GetProperty("Control").GetBoolean(), "Unexpected original negative Test."); }
        }
        Require(positive == 3 && negative == 2, "Missing original tests or isolated invalid-floor controls.");
        var applications = root.GetProperty("RelicOpeningMorselApplications").EnumerateArray().ToArray();
        Require(applications.Length == 3 && applications.Select(row => row.GetProperty("EffectIndex").GetInt32()).SequenceEqual([0, 1, 2]),
            "Missing native ordered floors 2,1,0.");
        foreach (FixtureValue row in applications)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int index = row.GetProperty("EffectIndex").GetInt32();
            CombatRelicState owner = before.Context!.Relics!.Single(item => item.DataId == RelicOpeningSpawnModel.DataId);
            Require(RelicModel.Validate([owner]) == null && owner.ResourceEffects!.Count == 3, "Changed original owner.");
            foreach (RelicResourceEffect effect in owner.ResourceEffects)
            {
                CombatUnit template = effect.OpeningSpawn!.Units.Single().Unit;
                CombatTrigger trigger = template.Triggers.Single(); CardActionEffect eaten = trigger.Effects.Single().UnitUpgrade!;
                Require(template.BaseAttack == 0 && template.Health == 1 && template.MaxHealth == 1 && template.Size == 1 &&
                    template.CanAttack && template.Subtypes.SequenceEqual(["SubtypesData_Snack"]) && template.Statuses.Count == 0 &&
                    trigger.Kind == "OnEaten" && trigger.SkipDuringDeployment == true && trigger.FireCount == 1 && !trigger.Once &&
                    eaten.Target == "LastFeederCharacter" && eaten.Lifetime == "TemporaryUntilEndOfBattle" &&
                    eaten.Upgrade!.DataId == "2a43f664-585d-4fc1-a7ac-b234ef239780" && eaten.Upgrade.Stats.Damage == 9 && eaten.Upgrade.Stats.Health == 9 &&
                    effect.OpeningSpawn.Units.Single().Fallback == null, "Original Miner body/OnEaten payload was changed.");
            }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                TrainCombatResult result = RelicOpeningSpawnModel.Apply(before, owner.DataId, index);
                Require(result.Supported, result.UnsupportedReason ?? "Spawn unsupported.");
                Equal(result.State, actual, "native opening spawn");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Spawn mutated parent.");
            CombatUnit born = actual.Rooms.Single(room => room.RoomIndex == 2 - index).Units.Single(unit =>
                !before.Rooms.SelectMany(room => room.Units).Any(previous => previous.Id == unit.Id));
            Require(born.AssetKey == "MorselMorselMiner" && born.SpawnerCardId == 0 && born.Modifiers?.IsClone == false &&
                born.Status("cardless")?.Stacks == 1 && born.LastFeederId == 0 && row.GetProperty("Notifications").GetInt32() == 0,
                "Wrong cardless/original/feeder state or unexpected notification.");
            Equal(before.Context.CardRegistry, actual.Context!.CardRegistry, "no fallback/spawner card allocation");
            Require(!RelicOpeningSpawnModel.Apply(before, "absent-owner", index).Supported &&
                !RelicOpeningSpawnModel.Apply(before, owner.DataId, -1).Supported, "Absent owner or effect accepted.");
        }
        int consumed = 0, noFeeder = 0;
        foreach (FixtureValue row in root.GetProperty("NativeMorselFeeding").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int floor = row.GetProperty("RoomIndex").GetInt32(); CombatTeam team = row.GetProperty("Team").Deserialize<CombatTeam>();
            RoomCombatState room = before.Rooms.Single(item => item.RoomIndex == floor);
            RoomCombatResult result = RoomCombatModel.ApplyFeeding(room, team);
            Require(result.Supported, result.UnsupportedReason ?? "Feeding unsupported.");
            TrainCombatState predicted = CardSpellModel.WithContext(new(before.Rooms.Select(item => item.RoomIndex == floor ? result.State! : item).ToArray(),
                before.Movement, before.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
            Equal(predicted, actual, "native feeding complete train");
            int eaten = result.Events.Count(item => item.Kind == "Eaten");
            Require(eaten == row.GetProperty("ExpectedEaten").GetInt32() &&
                row.GetProperty("FoodStatAfter").GetInt32() - row.GetProperty("FoodStatBefore").GetInt32() == (team == CombatTeam.Player ? eaten : 0),
                "Wrong native eating count/run statistic.");
            if (eaten > 0)
            {
                consumed += eaten;
                CombatEvent meal = result.Events.Single(item => item.Kind == "Eaten");
                CombatUnit feederBefore = room.Units.Single(unit => unit.Id == meal.Actor);
                CombatUnit feederAfter = actual.Rooms.Single(item => item.RoomIndex == floor).Units.Single(unit => unit.Id == meal.Actor);
                Require(feederAfter.Attack == feederBefore.Attack + 9 && feederAfter.MaxHealth == feederBefore.MaxHealth + 9 &&
                    !actual.Rooms.SelectMany(item => item.Units).Any(unit => unit.Id == meal.Target), "Missing real +9/+9 or eaten removal.");
            }
            else if (row.GetProperty("FoodIds").EnumerateArray().Any()) noFeeder++;
        }
        Require(consumed >= 1 && noFeeder > 0, "Missing actual food consumer or isolated food/no-feeder controls.");
        Require(root.GetProperty("Actions").EnumerateArray().All(row => !row.GetProperty("ScenarioAction").GetBoolean()), "Expected ordinary model-selected actions.");
        Console.WriteLine($"ORIGINAL-OPENING-MORSELS-NATIVE PASS: {positive} unchanged ordered spawns, {negative} native room controls, exact Miner +9/+9, {consumed} actual meals, {noFeeder} no-feeder controls, full state/RNG/card comparisons and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
