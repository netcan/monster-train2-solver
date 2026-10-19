using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ForcedFeedingChecks
{
    internal static void Native(FixtureValue root, bool requireOriginalSource = false)
    {
        Require(root.GetProperty("ForcedFeeding").GetBoolean() && root.GetProperty("ForcedFeedingDeploymentRejected").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean(), "Missing native forced-feeding witness.");
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "ForcedFeedingCaptureFailures",
            "ForcedFeedingMismatches", "ForcedFeedingUnsupported", "ForcedFeedingPending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Nonzero " + name);
        int positive = 0, controls = 0, survived = 0, final = 0;
        bool scalingMode = root.TryGetProperty("ShroudSpikeFeeding", out var mode) && mode.GetBoolean();
        Require(!requireOriginalSource || scalingMode, "Original Shroud Spike source checks require the scaling scenario.");
        int scaled = 0, zeroCost = 0;
        foreach (FixtureValue row in root.GetProperty("ForcedFeedingRecords").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int[] ids = row.GetProperty("Targets").Deserialize<int[]>()!;
            int meals = 0;
            int repeats = 1;
            if (row.TryGetProperty("CardId", out var sourceId))
            {
                StatisticQueryResult count = ForcedFeedingModel.TriggerCount(before.Context!, before.Context!.FindCard(sourceId.GetInt32()));
                Require(count.Supported, count.UnsupportedReason ?? "Feeding count unsupported.");
                repeats = count.Value;
                Require(repeats == row.GetProperty("TriggerCount").GetInt32(), "Modeled repeat count differs from the native trait.");
            }
            TrainCombatState predicted = before;
            if (ids.Length > 0)
            {
                RoomCombatState room = before.Rooms.Single(room => room.Units.Any(unit => unit.Id == ids[0]));
                string parent = JsonSerializer.Serialize(room, ModelJson.Options);
                RoomCombatResult result = RoomCombatModel.ApplyFeeding(room, CombatTeam.Player, ids, repeats);
                Require(result.Supported, result.UnsupportedReason ?? "Forced feeding unsupported.");
                predicted = CardSpellModel.WithContext(new(before.Rooms.Select(item => item.RoomIndex == room.RoomIndex ? result.State! : item).ToArray(),
                    before.Movement, before.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                meals = result.Events.Count(item => item.Kind == "Eaten");
                if (scalingMode && meals > 0)
                {
                    CombatUnit feeder = room.Units.First(unit => unit.Team == CombatTeam.Player && !RoomCombatModel.IsFood(unit));
                    CombatUnit fed = actual.Rooms.SelectMany(item => item.Units).Single(unit => unit.Id == feeder.Id);
                    Require(actual.Context!.Gold - before.Context!.Gold == 5 * repeats, "OnFeed repeat rewards differ.");
                    Require(fed.Attack - feeder.Attack == 9 * repeats && fed.MaxHealth - feeder.MaxHealth == 9 * repeats,
                        "OnEaten scaling differs from the original Miner rewards.");
                    if (repeats == 0) zeroCost++;
                    if (repeats == 6) scaled++;
                }
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
                Parallel.For(0, 16, _ => Equal(RoomCombatModel.ApplyFeeding(room, CombatTeam.Player, ids, repeats).State, result.State, "immutable feeding branch"));
                Require(parent == JsonSerializer.Serialize(room, ModelJson.Options), "Forced feeding mutated its parent.");
            }
            Equal(predicted, actual, "complete native forced-feeding state and RNG");
            Require(row.GetProperty("FoodStatAfter").GetInt32() - row.GetProperty("FoodStatBefore").GetInt32() == unchecked(meals * repeats), "Native meal statistic differs.");
            if (meals > 0) positive++; else controls++;
        }
        Require(positive >= 2 && controls >= 1 && survived > 0 && final > 0, "Missing positive, no-feeder, surviving or final-meal controls.");
        if (scalingMode) Require(scaled > 0 && zeroCost > 0, "Missing six-repeat paid cast or zero-cost consumed meal.");
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        if (scalingMode)
        {
            var paid = new List<int>();
            foreach (FixtureValue action in actions)
            {
                BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
                int id = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                if (before.Spawn.Train.Context!.FindCard(id)?.DataId != "fecc00fd-70ef-473e-bfa1-35c221540d43") continue;
                BattleTurnState actual = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CardInstanceState card = before.Spawn.Train.Context.FindCard(id)!;
                CardMaskDescriptor mask = card.MaskDescriptor!;
                if (requireOriginalSource || mask.SourceTraits != null)
                {
                    CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == card.DataId);
                    CardSourceTrait[] traits = mask.SourceTraits!.ToArray();
                    CardSourceTrait scalingTrait = traits.Single(trait => trait.Type == "CardTraitScalingFeederRules");
                    CardSourceTrait consumeTrait = traits.Single(trait => trait.Type == "CardTraitExhaustState");
                    CardTraitValue scalingBase = mask.BaseTraits.Single(trait => trait.RuntimeType == "CardTraitScalingFeederRules");
                    CardStatisticQuery query = scalingBase.FeedingQuery!;
                    Require(card.DataId == "fecc00fd-70ef-473e-bfa1-35c221540d43" && rule.AssetKey == "ShroudSpike" &&
                        rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.Effects.Count == 1 &&
                        rule.Effects[0].Type == "FeederRules" && rule.Effects[0].Target == "DropTargetCharacter" &&
                        !rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer && rule.Effects[0].Filters?.Subtype == "SubtypesData_Snack" &&
                        mask.Definition.XCost && mask.BaseCost == 0 && traits.Length == 2 &&
                        scalingTrait.Int == 2 && scalingTrait.Int2 == 0 && scalingTrait.Int3 == 0 && !scalingTrait.Bool && scalingTrait.Str == "" &&
                        scalingTrait.Float == 1 && scalingTrait.Scaling && scalingTrait.TrackedValue == "PlayedCost" && scalingTrait.EntryDuration == "ThisBattle" &&
                        consumeTrait.Int == 0 && consumeTrait.Int2 == 0 && consumeTrait.Int3 == 0 && !consumeTrait.Bool && consumeTrait.Str == "" &&
                        consumeTrait.Float == 0 && !consumeTrait.Scaling && consumeTrait.TrackedValue == "PlayedCost" && consumeTrait.EntryDuration == "ThisBattle" &&
                        scalingBase.DefinitionParamInt == 2 && scalingBase.ParamInt == 2 &&
                        query.Type == "PlayedCost" && query.Duration == "ThisBattle" && query.TypeMatchDataIds == null &&
                        query.SubtypeMatchDataIds is { Count: 0 } && query.SubtypeIsNone && query.Subtype == "SubtypesData_None" &&
                        query.SpecificCardDataId == null && query.SourceRawCost == null && !query.VariableCost && query.TargetTeam == "None" &&
                        !query.CurrentCostCardModifiersOnly && !query.CurrentCostSourceIsSpell && query.StatusIds == null,
                        "Original Shroud Spike source trait, feeder target rule or paid-cost query differs.");
                    Require(JsonSerializer.Serialize(mask.SourceTraits, ModelJson.Options) ==
                        JsonSerializer.Serialize(actual.Spawn.Train.Context!.FindCard(id)!.MaskDescriptor!.SourceTraits, ModelJson.Options),
                        "Shroud Spike play changed its immutable original source traits.");
                }
                Require(actual.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id),
                    "Shroud Spike did not Consume after its meal.");
                int lastPlayedCost = actual.Spawn.Train.Context.FindCard(id)!.LastPlayedCost;
                if (requireOriginalSource) Require(card.MaskDescriptor!.SourceTraits != null, "Original Shroud Spike capture lacks raw source traits.");
                paid.Add(lastPlayedCost);
            }
            Require(paid.OrderBy(value => value).SequenceEqual(new[] { 0, 3 }), "Missing native X-cost payments 0 and 3.");
            if (requireOriginalSource) Require(paid.Count == 2, "Expected exactly two original Shroud Spike source plays.");
        }
        Require(actions.Any(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms[0].Deployment &&
            !BattleActionModel.EnumerateSupportedPlays(row.GetProperty("Before").Deserialize<BattleTurnState>()!).Any(action =>
                row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == "236215da-c1dd-40b7-8793-27ce327a7e78")),
            "Model did not enforce the native deployment cast gate.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"FORCED-FEEDING PASS: {positive} positive casts, {controls} controls, {survived} surviving and {final} final meals, {scaled} six-repeat and {zeroCost} zero-cost meals; full state/RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
