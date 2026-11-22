using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SourceBackedFeedingChecks
{
    internal static void Native(FixtureValue root)
    {
        if (root.GetProperty("Boundary").GetString() == "NativeOriginalExtraFeedTriggerMeal")
        {
            ExtraFeedTriggerMeal(root);
            return;
        }
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeOriginalSourceBackedFeeding" &&
            root.GetProperty("Primordium").GetString() == "2e1b29f2-fbe8-47b9-8ef8-1bac4c1f1f65" &&
            root.GetProperty("Miner").GetString() == "536e6de0-7ccf-412a-9a4e-ec55ab3988e5" &&
            root.GetProperty("Feast").GetString() == "236215da-c1dd-40b7-8793-27ce327a7e78",
            "Missing pinned original source-backed meal evidence.");
        int floor = root.GetProperty("Floor").GetInt32();
        int[] deployment = root.GetProperty("DeploymentCards").Deserialize<int[]>()!;
        int[] forced = root.GetProperty("ForcedCards").Deserialize<int[]>()!;
        Require(deployment.Length == 3 && forced.Length == 2, "Missing recorded original food and Feast instances.");
        Check("Ordinary", deployment[1], 3, selected: false);
        Check("Forced", forced[0], 2, selected: true);
        Console.WriteLine("SOURCE-BACKED-FEEDING PASS ordinary slots preserve the gap, Feast compacts, and both full native successors match.");

        void Check(string phase, int minerCard, int oldStacks, bool selected)
        {
            FixtureValue row = root.GetProperty(phase);
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Error").ValueKind == FixtureKind.Null &&
                row.GetProperty("FoodStatAfter").GetInt32() - row.GetProperty("FoodStatBefore").GetInt32() == 2,
                phase + " native callback did not record two meals and complete.");
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            var food = before.Rooms.Single(room => room.RoomIndex == floor);
            CombatUnit feeder = food.Units.Single(unit => unit.SpawnerCardId == deployment[0]);
            CombatUnit miner = food.Units.Single(unit => unit.SpawnerCardId == minerCard && unit.AssetKey == "MorselMorselMiner");
            CombatUnit repeat = food.Units.Single(unit => unit.SpawnerCardId == deployment[2] && unit.AssetKey == "MonsterChampionUmbraPrimordium");
            CombatUnit survivor = actual.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.Id == repeat.Id);
            int Position(TrainCombatState state, int unitId) => state.Context!.SpawnPoints!.Units.Single(point => point.UnitId == unitId).Current!.Index;
            Require(Position(before, miner.Id) == 1 && Position(before, repeat.Id) == 2 && Position(actual, repeat.Id) == (selected ? 1 : 2) &&
                !actual.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == miner.Id) &&
                actual.Context!.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Any(card => card.InstanceId == minerCard) &&
                repeat.Status("eatmany")?.Stacks == oldStacks && survivor.Status("eatmany")?.Stacks == oldStacks - 1 && survivor.LastFeederId == feeder.Id,
                phase + " must witness a removed Miner and a surviving Primordium with the native slot and feeder reference.");
            int[]? targets = selected ? row.GetProperty("Targets").Deserialize<int[]>() : null;
            int repeats = selected ? row.GetProperty("TriggerCount").GetInt32() : 1;
            Require(!selected || row.GetProperty("CardId").GetInt32() == forced[1] && repeats == 1 &&
                targets!.Contains(miner.Id) && targets.Contains(repeat.Id), "Feast must select both native food units.");
            string input = JsonSerializer.Serialize(before, ModelJson.Options);
            string expected = JsonSerializer.Serialize(actual, ModelJson.Options);
            Parallel.For(0, 16, _ =>
            {
                RoomCombatResult result = RoomCombatModel.ApplyFeeding(food, CombatTeam.Player, targets, repeats);
                Require(result.Supported, result.UnsupportedReason ?? phase + " forecast was unsupported.");
                RoomCombatState room = result.State!;
                var predicted = CardSpellModel.WithContext(new TrainCombatState(before.Rooms.Select(old => old.RoomIndex == floor ? room : old).ToArray(),
                    before.Movement, before.EnemySlotsPerRoom, room.Context), room.Context!);
                string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), expected);
                Require(difference == null, phase + " meal differs from the complete native successor. " + difference);
            });
            Require(input == JsonSerializer.Serialize(before, ModelJson.Options), phase + " forecast changed its input.");
        }
    }

    private static void ExtraFeedTriggerMeal(FixtureValue root)
    {
        const string ownerId = "b6613fe4-13c6-4462-8a0b-9af42850a851";
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Construct").GetString() == "3db9cb2c-3495-47a7-983e-d717a206460c" &&
            root.GetProperty("Miner").GetString() == "536e6de0-7ccf-412a-9a4e-ec55ab3988e5" &&
            root.GetProperty("Definition").GetProperty("OwnerId").GetString() == ownerId,
            "Missing pinned original ExtraFeedTrigger meal evidence.");
        int floor = root.GetProperty("Floor").GetInt32();
        int[] cards = root.GetProperty("DeploymentCards").Deserialize<int[]>()!;
        Require(cards.Length == 2 && cards[0] != cards[1], "Missing distinct original feeder and Miner cards.");
        FixtureValue meal = root.GetProperty("Ordinary");
        Require(meal.GetProperty("Completed").GetBoolean() && meal.GetProperty("Error").ValueKind == FixtureKind.Null &&
            meal.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
            meal.GetProperty("Difference").ValueKind == FixtureKind.Null &&
            meal.GetProperty("RoomIndex").GetInt32() == floor && meal.GetProperty("Team").GetInt32() == (int)CombatTeam.Player &&
            meal.GetProperty("FoodStatAfter").GetInt32() - meal.GetProperty("FoodStatBefore").GetInt32() == 1,
            "The native Monsters meal must complete and count one consumed Miner.");
        var before = meal.GetProperty("Before").Deserialize<TrainCombatState>()!;
        var actual = meal.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        RoomCombatState room = before.Rooms.Single(state => state.RoomIndex == floor);
        RoomCombatState settled = actual.Rooms.Single(state => state.RoomIndex == floor);
        CombatContext context = before.Context!;
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == ownerId);
        Require(owner.AssetKey == "ExtraFeedTrigger" && owner.EffectTypes.SequenceEqual(["RelicEffectModifyTriggerCount"]) &&
            context.TriggerCounts!.Modifiers.Count == 1 && context.TriggerCounts.Modifiers[0].Kind == "OnFeed" &&
            context.TriggerCounts.Modifiers[0].Value == 1 && context.TriggerCounts.EnemyAllowedKinds.Count == 0,
            "The original OnFeed count modifier was not active on the native feeder.");
        CombatUnit feeder = room.Units.Single(unit => unit.AssetKey == "AlloyedConstruct" && unit.SpawnerCardId == cards[0]);
        CombatUnit miner = room.Units.Single(unit => unit.AssetKey == "MorselMorselMiner" && unit.SpawnerCardId == cards[1]);
        CombatUnit survivor = settled.Units.Single(unit => unit.Id == feeder.Id);
        CombatTrigger trigger = feeder.Triggers.Single(item => item.Kind == "OnFeed");
        CombatEffect effect = trigger.Effects.Single();
        int fuelBefore = feeder.Status("spark")?.Stacks ?? 0, fuelAfter = survivor.Status("spark")?.Stacks ?? 0;
        Require(feeder.Id == root.GetProperty("FeederId").GetInt32() && miner.Id == root.GetProperty("FoodId").GetInt32() &&
            trigger.FireCount == 2 && root.GetProperty("NativeFireCount").GetInt32() == trigger.FireCount &&
            effect.Type == "CardEffectAddStatusEffect" && effect.Action is { Type: "AddStatus", Target: "Self" } &&
            effect.Action.Statuses is [{ Id: "spark", Stacks: 1 }] &&
            root.GetProperty("FuelBefore").GetInt32() == fuelBefore && root.GetProperty("FuelAfter").GetInt32() == fuelAfter &&
            fuelAfter - fuelBefore == 2 && meal.GetProperty("FoodIds").Deserialize<int[]>()!.SequenceEqual([miner.Id]) &&
            settled.Units.All(unit => unit.Id != miner.Id) &&
            actual.Context!.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Any(card => card.InstanceId == cards[1]),
            "One original Miner must drive two native one-fuel OnFeed applications.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = JsonSerializer.Serialize(actual, ModelJson.Options);
        Parallel.For(0, 16, _ =>
        {
            RoomCombatResult result = RoomCombatModel.ApplyFeeding(room, CombatTeam.Player);
            Require(result.Supported, result.UnsupportedReason ?? "Original ExtraFeedTrigger meal was unsupported.");
            RoomCombatState changed = result.State!;
            TrainCombatState predicted = CardSpellModel.WithContext(new TrainCombatState(before.Rooms.Select(old =>
                old.RoomIndex == floor ? changed : old).ToArray(), before.Movement, before.EnemySlotsPerRoom, changed.Context), changed.Context!);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), expected);
            Require(difference == null, "Original ExtraFeedTrigger native successor differs: " + difference);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "ExtraFeedTrigger meal changed its input.");
        Console.WriteLine($"EXTRA-FEED-TRIGGER-MEAL PASS: one native Miner, two original OnFeed fuel gains {fuelBefore}->{fuelAfter}, full state and RNG across 16 branches.");
    }

    private static void Require(bool supported, string description)
    {
        if (!supported) throw new InvalidOperationException(description);
    }
}
