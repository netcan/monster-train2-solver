using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDiscardDrawChecks
{
    private const string CardId = "a6a701f6-cb73-4880-b9b3-ed1abb3b23d4";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Enumerable.Range(0, ids.Length).Where(i => i != 2 && source.Spawn.Train.Context!.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 0).DefaultIfEmpty(-1).First();
        if (index < 0 || index >= 3 && source.Spawn.Turn < (index < 6 ? 1 : index < 9 ? 2 : 3)) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == 0);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalDiscardDrawPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original discard draw witness.");
        if (root.GetProperty("Mismatches").GetInt32() > 0)
        {
            foreach (FixtureValue sample in root.GetProperty("OriginalDiscardDrawDiscards").EnumerateArray())
            {
                CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
                int id = sample.GetProperty("CardId").GetInt32();
                TypedDrawResult result = CardDiscardDrawModel.Discarded(before, before.FindCard(id)!, sample.GetProperty("WasPlayed").GetBoolean(),
                    root.GetProperty("Actions").EnumerateArray().First().GetProperty("Before").Deserialize<BattleTurnState>()!.PlayRules);
                Console.WriteLine("DISCARD-DRAW-CALLBACK-DIFF " + id + ": " + (result.Supported ? ModelJson.Difference(Json(result.Context), Json(sample.GetProperty("After").Deserialize<CombatContext>())) : result.UnsupportedReason));
            }
            foreach (FixtureValue phase in root.GetProperty("TrainPhases").EnumerateArray())
                if (phase.GetProperty("Difference").ValueKind != FixtureKind.Null)
                    Console.WriteLine("DISCARD-DRAW-STORED-PHASE-DIFF " + phase.GetProperty("Index").GetInt32() + ": " + ModelJson.Difference(
                        Json(phase.GetProperty("Predicted").GetProperty("State").Deserialize<TrainCombatState>()), Json(phase.GetProperty("Actual").Deserialize<TrainCombatState>())));
        }
        foreach (FixtureValue action in root.GetProperty("Actions").EnumerateArray())
        {
            if (action.GetProperty("Difference").ValueKind == FixtureKind.Null) continue;
            BattleActionResult replay = BattleActionModel.PlayCard(action.GetProperty("Before").Deserialize<BattleTurnState>()!, action.GetProperty("Action").Deserialize<PlayCardAction>()!);
            string? difference = replay.Supported ? ModelJson.Difference(Json(replay.State), Json(action.GetProperty("Actual").Deserialize<BattleTurnState>())) : replay.Reason;
            Require(difference == null, "Native discard draw action differs: " + difference);
        }
        foreach (FixtureValue phase in root.GetProperty("TrainPhases").EnumerateArray())
            if (phase.GetProperty("Difference").ValueKind != FixtureKind.Null)
                Console.WriteLine("DISCARD-DRAW-PHASE-DIFF " + phase.GetProperty("Index").GetInt32() + ": " + ModelJson.Difference(
                    Json(phase.GetProperty("Predicted").GetProperty("State").Deserialize<TrainCombatState>()), Json(phase.GetProperty("Actual").Deserialize<TrainCombatState>())));
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalDiscardDrawIds").Deserialize<int[]>()!;
        FixtureValue[] counts = root.GetProperty("OriginalDiscardDrawCounts").EnumerateArray().ToArray(), discards = root.GetProperty("OriginalDiscardDrawDiscards").EnumerateArray().ToArray(),
            actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 12 && actions.Length == 11 && counts.Select(row => row.GetProperty("Actual").GetInt32()).SequenceEqual([3, 0, 10]) &&
            discards.Count(row => row.GetProperty("WasPlayed").GetBoolean()) == 3 && discards.Any(row => !row.GetProperty("WasPlayed").GetBoolean()), "Missing X or natural-discard controls.");
        foreach (FixtureValue row in counts)
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!; int id = row.GetProperty("CardId").GetInt32();
            CardInstanceState card = context.FindCard(id)!; string parent = Json(context);
            StatisticQueryResult count = CardDiscardDrawModel.Count(context, card, true);
            Require(count.Supported && count.Value == row.GetProperty("Actual").GetInt32(), "Native paid-cost discard draw count differs.");
            CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardDiscardDrawModel.Trait);
            CardSourceTrait changed = new(source.Type, source.Int, source.Int2, source.Int3, false, source.Str, source.Float, source.Scaling, source.TrackedValue, source.EntryDuration);
            Require(!CardDiscardDrawModel.Count(context, WithSource(null), true).Supported &&
                !CardDiscardDrawModel.Count(context, WithSource(descriptor.SourceTraits.Select(trait => trait.Type == source.Type ? changed : trait).ToArray()), true).Supported &&
                !CardDiscardDrawModel.Count(context.WithCards(context.Cards.WithDrawDisabled(null)), card, true).Supported, "Missing/changed source or unknown draw blocking must remain guarded.");
            Require(parent == Json(context), "Discard-draw count or guard mutated its parent.");
        }
        foreach (FixtureValue row in discards)
        {
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!, after = row.GetProperty("After").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(); bool played = row.GetProperty("WasPlayed").GetBoolean();
            BattlePlayRules rules = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.PlayRules!;
            string parent = Json(before); TypedDrawResult predicted = CardDiscardDrawModel.Discarded(before, before.FindCard(id)!, played, rules);
            string? difference = predicted.Supported ? ModelJson.Difference(Json(predicted.Context), Json(after)) : predicted.UnsupportedReason;
            Require(row.GetProperty("Completed").GetBoolean() && difference == null && parent == Json(before), "Native complete discard callback differs: " + difference);
            if (!played) Require(Json(before) == Json(after), "Unplayed Rail Spike must neither draw nor block draws.");
            else
            {
                int count = before.Statistics!.PlayedCosts.Single(cost => cost.CardId == id).Cost;
                Require(before.Cards.DeckDrawDisabledThisTurn == false && after.Cards.DeckDrawDisabledThisTurn == true && predicted.DrawnCardIds.Count == count,
                    "X discard must draw paid X cards before blocking, including zero X.");
                foreach (int drawnId in predicted.DrawnCardIds)
                {
                    CardInstanceState recipient = after.FindCard(drawnId)!;
                    CardUpgradeModifier upgrade = recipient.Temporary.Upgrades.Single(item => item.DataId == "2571537a-2c44-4731-8c41-97da633bbdbb");
                    Require(upgrade.Stats.Cost == -1 && upgrade.Stats.XCost == 1 &&
                        (recipient.MaskDescriptor!.Definition.XCost || CardHandCostModel.Resolve(after, recipient, recipient.MaskDescriptor.BaseCost,
                            recipient.MaskDescriptor.Definition.CardType) == Math.Max(0, recipient.MaskDescriptor.BaseCost - 1)),
                        "Original next-draw upgrade must add minus-one cost and plus-one X to every drawn recipient.");
                }
                Require(BonusDrawModel.Schedule(after.Cards, "draw-block-copy", 1, null).DeckDrawDisabledThisTurn == true &&
                    NextDrawUpgradeModel.Clear(after.Cards).DeckDrawDisabledThisTurn == true, "Scheduling/clearing lost draw-block state.");
            }
        }
        foreach (int index in new[] { 2, 6, 9 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction play = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            Require(before.Spawn.Train.Context!.FindCard(play.CardInstanceId)!.DataId == CardId && before.Energy - after.Energy == (index == 2 ? 3 : index == 6 ? 0 : 10) &&
                after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == play.CardInstanceId) &&
                after.Spawn.Train.Context!.Cards.NextDrawUpgrades!.Count == 0 && before.Spawn.Train.Context.Cards.DeckDrawDisabledThisTurn == false &&
                after.Spawn.Train.Context.Cards.DeckDrawDisabledThisTurn == true, "Original X cost, Consume, next-turn reset or queue clearing differs.");
            Require(Json(before.Spawn.Train.Rooms.SelectMany(room => room.Units)) == Json(after.Spawn.Train.Rooms.SelectMany(room => room.Units)), "Discard draw changed units.");
        }
        foreach (int index in new[] { 3, 4, 7, 10 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(before.Spawn.Train.Context!.Cards.DeckDrawDisabledThisTurn == true && after.Spawn.Train.Context!.Cards.DeckDrawDisabledThisTurn == true &&
                Json(before.Spawn.Train.Context.Cards.Draw) == Json(after.Spawn.Train.Context.Cards.Draw) &&
                before.Spawn.Train.Context.Cards.Hand.Count - after.Spawn.Train.Context.Cards.Hand.Count == 1, "Blocked ordinary/typed draw moved a card or lost its flag.");
        }
        TypedDrawNativeChecks.Native(root, required: true);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-DISCARD-DRAW PASS: original Awoken's Rail Spike X three/zero/ten, full played/unplayed discard contexts, minus-one cost/plus-one X on drawn recipients, ordinary/typed draw blocking, next-turn reset, Consume, queue clearing, strict source/state guards and 16 immutable chains. Other scaling, caps, shuffle and relic combinations need separate witnesses.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
