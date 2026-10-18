using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPileRecursionChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalPileRecursionPrepared").GetBoolean() && root.GetProperty("OriginalPileRecursionQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Unearth Treasure witness or restored queries.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalPileRecursionIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalPileRecursionQueries").EnumerateArray().ToArray(),
            samples = root.GetProperty("OriginalPileRecursionSamples").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 4 && samples.Length == 2 && queries.Select(row => row.GetProperty("ExhaustedCount").GetInt32()).SequenceEqual([0, 4, 5, 10]) &&
            queries.Select(row => row.GetProperty("ActualBonus").GetInt32()).SequenceEqual([0, 0, 1, 1]), "Missing zero/below/exact/above threshold controls or both original casts.");
        foreach (FixtureValue row in queries)
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(); CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            StatisticQueryResult actual = CardPileDrawModel.Adjust(context, id, 0, forRecursion: true);
            Require(actual.Supported && actual.Value == row.GetProperty("ActualBonus").GetInt32() && row.GetProperty("StateUnchanged").GetBoolean() &&
                context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count == row.GetProperty("ExhaustedCount").GetInt32() &&
                descriptor.BaseCost == 1 && descriptor.SourceTraits!.Select(trait => trait.Type).SequenceEqual([CardPileDrawModel.Trait, "CardTraitExhaustState", "CardTraitDistinct"]) &&
                descriptor.SourceTraits.All(source => source.Int == 1 && source.Int2 == 2 && source.Int3 == 5 && !source.Bool && source.Str == "" &&
                    source.Float == 0 && !source.Scaling && source.TrackedValue == "SubtypeInDeck" && source.EntryDuration == "ThisTurn"),
                "Native constant threshold bonus, exhausted-pile count or original source parameters differ.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            Require(!CardPileDrawModel.Adjust(context.WithCard(WithSource(null)), id, 1, forRecursion: true).Supported &&
                !CardPileDrawModel.Adjust(context.WithOtherPiles(null), id, 1, forRecursion: true).Supported &&
                !CardPileDrawModel.Adjust(context, id, 1).Supported, "Missing source/pile or future-draw routing must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Exhausted count query or guard mutated its parent.");
        }
        for (int index = 0; index < 2; index++)
        {
            FixtureValue row = samples[index];
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("NativeBonuses").Deserialize<int[]>()!.SequenceEqual([index == 0 ? 1 : 0]), "Incomplete callback or unexpected native pile bonus.");
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!, after = row.GetProperty("After").Deserialize<CombatContext>()!;
            BattleTurnState actionBefore = actions[index + 2].GetProperty("Before").Deserialize<BattleTurnState>()!, actionAfter = actions[index + 2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int id = ids[index + 2]; CardPlayRule rule = actionBefore.PlayRules!.Cards.Single(card => card.DataId == "1b7663f2-c34d-4b06-a959-f2b4e2f5b567");
            CardActionEffect effect = rule.Effects.Single();
            IReadOnlyList<int> eligible = CardRecursionModel.EligibleCards(before.Cards, before.OtherPiles, effect, actionBefore.PlayRules, id, out string? error, before.CardRegistry);
            StatisticQueryResult count = CardPileDrawModel.Adjust(before, id, effect.Value, forRecursion: true);
            Require(error == null && eligible.Count == (index == 0 ? 3 : 1) && count.Supported && count.Value == (index == 0 ? 2 : 1) &&
                before.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count == (index == 0 ? 5 : 4) &&
                effect.Type == "Recursion" && effect.Target == "Exhaust" && effect.Value == 1 && effect.Recursion!.SelectionMode == "RandomToHand" &&
                effect.Recursion.CardFilter!.DisallowedPools.SelectMany(pool => pool).Contains(rule.DataId) && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions!.Count == 0,
                "Total exhausted count must include units, while random candidate filtering excludes units and the original source family.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            CardRecursionResult restored = CardRecursionModel.Apply(before.Cards, before.OtherPiles, before.BattleRng, effect, actionBefore.PlayRules, id,
                before.MaxHandSize, cardRegistry: before.CardRegistry, drawCount: count.Value);
            Require(restored.Supported && restored.RestoredCardIds.Count == count.Value &&
                restored.RestoredCardIds.All(cardId => actionBefore.PlayRules.Cards.Single(card => card.DataId == before.FindCard(cardId)!.DataId).CardType == "Spell") &&
                JsonSerializer.Serialize(restored.Cards, ModelJson.Options) == JsonSerializer.Serialize(after.Cards, ModelJson.Options) &&
                JsonSerializer.Serialize(restored.OtherPiles, ModelJson.Options) == JsonSerializer.Serialize(after.OtherPiles, ModelJson.Options) &&
                JsonSerializer.Serialize(restored.BattleRng, ModelJson.Options) == JsonSerializer.Serialize(after.BattleRng, ModelJson.Options),
                "Native random return identities/order, physical pile changes or Battle RNG differ.");
            Require(actionAfter.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id) &&
                actionAfter.Spawn.Train.Context!.Statistics!.Value(id, "TimesExhausted", "ThisBattle") == 2 && actionBefore.Energy - actionAfter.Energy == 1 &&
                JsonSerializer.Serialize(actionBefore.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options) ==
                    JsonSerializer.Serialize(actionAfter.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options), "Original payment, Consume or unaffected-unit controls differ.");
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Random recursion changed its parent.");
            if (index == 1) Require(!eligible.Contains(ids[2]) && before.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]),
                "Previously consumed Unearth Treasure must remain excluded from candidates by its original filter.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-PILE-RECURSION PASS: original Unearth Treasure, four restored exhausted-count queries, constant threshold bonus at five/ten, total-versus-eligible controls, two/one random spell returns with exact order/piles/RNG, source-family exclusion, Consume and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
