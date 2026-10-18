using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPileDrawChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index == 2 && source.Spawn.Turn < 1) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalPileDrawPrepared").GetBoolean() && root.GetProperty("OriginalPileDrawQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Mulch witness or restored queries.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalPileDrawIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalPileDrawQueries").EnumerateArray().ToArray(),
            adjustments = root.GetProperty("OriginalPileDrawAdjustments").EnumerateArray().ToArray();
        if (actions.Length != 3)
        {
            BattleTurnState decision = root.GetProperty("Turns").EnumerateArray().First().GetProperty("Actual").Deserialize<BattleTurnState>()!;
            BattleActionResult attempted = BattleActionModel.PlayCard(decision, new PlayCardAction(ids[2], 0));
            throw new InvalidOperationException("Missing native Mulch play: " + attempted.Rejection + ": " + attempted.Reason);
        }
        Require(ids.Length == 3 && actions.Length == 3 && adjustments.Length == 1 && queries.Select(row => row.GetProperty("VisibleDeckCount").GetInt32()).SequenceEqual([0, 14, 15, 29, 30]) &&
            queries.Select(row => row.GetProperty("ActualBonus").GetInt32()).SequenceEqual([0, 0, 1, 1, 2]), "Missing exact visible save-deck threshold controls.");
        foreach (FixtureValue row in queries)
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(); CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            StatisticQueryResult actual = CardPileDrawModel.Adjust(context, id, 0);
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardPileDrawModel.Trait);
            Require(actual.Supported && actual.Value == row.GetProperty("ActualBonus").GetInt32() && row.GetProperty("StateUnchanged").GetBoolean() &&
                context.Resources!.VisibleDeckCount == row.GetProperty("VisibleDeckCount").GetInt32() && context.Cards.Draw.Count == 8 &&
                source.Int == 1 && source.Int2 == 0 && source.Int3 == 15 && source.Bool && source.Str == "" && source.Float == 0 && !source.Scaling &&
                source.TrackedValue == "SubtypeInDeck" && source.EntryDuration == "ThisTurn", "Native count, runtime-pile control or exact original trait source differs.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait changed = new(source.Type, source.Int, 2, source.Int3, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue, source.EntryDuration);
            CardSourceTrait[] wrongPile = descriptor.SourceTraits.Select(trait => trait.Type == CardPileDrawModel.Trait ? changed : trait).ToArray();
            Require(!CardPileDrawModel.Adjust(context.WithCard(WithSource(null)), id, 1).Supported &&
                !CardPileDrawModel.Adjust(context.WithCard(WithSource(wrongPile)), id, 1).Supported &&
                !CardPileDrawModel.Adjust(context.WithResources(null), id, 1).Supported, "Missing source/resource metadata or another pile must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Pile count query or guard mutated its parent.");
        }
        FixtureValue adjustment = adjustments.Single();
        CombatContext beforeAdjust = adjustment.GetProperty("Before").Deserialize<CombatContext>()!, afterAdjust = adjustment.GetProperty("After").Deserialize<CombatContext>()!;
        StatisticQueryResult adjusted = CardPileDrawModel.Adjust(beforeAdjust, ids[2], 1);
        Require(adjustment.GetProperty("CardId").GetInt32() == ids[2] && adjustment.GetProperty("ActualAmount").GetInt32() == 3 && adjusted.Supported && adjusted.Value == 3 &&
            beforeAdjust.Resources!.VisibleDeckCount == 30 && afterAdjust.Cards.DrawModifier - beforeAdjust.Cards.DrawModifier == 3 &&
            JsonSerializer.Serialize(beforeAdjust.WithCards(BonusDrawModel.Schedule(beforeAdjust.Cards, BonusDrawState.CardKey(ids[2], 0), adjusted.Value, null)), ModelJson.Options) ==
                JsonSerializer.Serialize(afterAdjust, ModelJson.Options), "Native future-draw adjustment complete context differs.");
        BattleTurnState before = actions[2].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == "eb7d86ac-26c0-4099-b101-26e0a787806c");
        Require(rule.Cost == 0 && rule.Effects.Single().Type == "DrawNextTurn" && rule.Effects[0].Value == 1 && rule.ExternalInteractions.Count == 0 &&
            rule.UpgradeInteractions!.Count == 0 && before.Spawn.Train.Context!.Cards.DrawModifier == 0 && after.Spawn.Train.Context!.Cards.DrawModifier == 3 &&
            before.Energy == after.Energy && after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[2]) &&
            after.Spawn.Train.Context.Statistics!.Value(ids[2], "TimesExhausted", "ThisBattle") == 2 &&
            after.Spawn.Train.Context.Resources!.VisibleDeckCount == 30, "Original base draw, zero cost, scaled scheduling, Consume or permanent deck count differs.");
        Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options) ==
            JsonSerializer.Serialize(after.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options), "Draw scheduling changed a unit.");
        Require(root.GetProperty("CardCycles").EnumerateArray().Any(row => row.GetProperty("Kind").GetString() == "Draw" &&
            row.GetProperty("Before").Deserialize<CardCycleState>()!.DrawModifier == 3 &&
            row.GetProperty("Actual").Deserialize<CardCycleState>()!.Hand.Count == 8 && row.GetProperty("Actual").Deserialize<CardCycleState>()!.DrawModifier == 0),
            "Native next turn must draw eight and reset the one-turn modifier.");
        BonusDrawChecks.Native(root);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-PILE-DRAW PASS: original Mulch, five restored visible permanent-deck threshold queries, distinct runtime pile, +three scheduling/full context, eight-card next-turn draw/reset, Consume, source/pile guards and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
