using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalScaledEnergyChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids.Take(5).ToArray(), id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalScaledEnergyPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original GainXEnergy witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalScaledEnergyIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), samples = root.GetProperty("OriginalScaledEnergySamples").EnumerateArray().ToArray();
        Require(ids.Length == 6 && actions.Length == 5 && samples.Count(sample => sample.GetProperty("WasPlayed").GetBoolean()) == 2 &&
            samples.Any(sample => !sample.GetProperty("WasPlayed").GetBoolean()), "Missing positive/zero played X or unplayed discard control.");
        var paidValues = new List<int>();
        foreach (FixtureValue sample in samples)
        {
            Require(sample.GetProperty("Completed").GetBoolean(), "Incomplete native scaled-energy callback.");
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>()!, after = sample.GetProperty("After").Deserialize<CombatContext>()!;
            int id = sample.GetProperty("CardId").GetInt32(); bool played = sample.GetProperty("WasPlayed").GetBoolean();
            CardInstanceState card = before.FindCard(id)!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            CardScaledEnergyResult actual = CardScaledEnergyModel.Discarded(before, card, played);
            Require(actual.Supported && JsonSerializer.Serialize(actual.Context, ModelJson.Options) == JsonSerializer.Serialize(after, ModelJson.Options),
                "Native scaled-energy callback complete context differs.");
            if (played)
            {
                int paid = before.Statistics!.PlayedCosts.Single(cost => cost.CardId == id).Cost;
                paidValues.Add(paid);
                Require(id == (paid > 0 ? ids[2] : ids[4]) && before.QueryFrame!.Energy == 0 && after.QueryFrame!.Energy == paid * 2 &&
                    before.EnergyState!.NextTurn == after.EnergyState!.NextTurn && before.EnergyState.EveryTurn == after.EnergyState.EveryTurn,
                    "Current-turn gain must use the live paid-cost statistic and leave future income unchanged.");
                CardMaskDescriptor descriptor = card.MaskDescriptor!;
                CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                    descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
                CardSourceTrait[] nextTurn = descriptor.SourceTraits!.Select(source => source.Type != CardScaledEnergyModel.Trait ? source :
                    new CardSourceTrait(source.Type, source.Int, source.Int2, source.Int3, true, source.Str, source.Float, source.Scaling, source.TrackedValue, source.EntryDuration)).ToArray();
                Require(!CardScaledEnergyModel.Discarded(before, WithSource(null), true).Supported &&
                    !CardScaledEnergyModel.Discarded(before, WithSource(nextTurn), true).Supported &&
                    !CardScaledEnergyModel.Discarded(before.WithStatistics(before.Statistics.WithPlayedCost(id, null)), card, true).Supported,
                    "Missing source/paid cost or uncalibrated next-turn route must remain guarded.");
            }
            else Require(id == ids[5] && ReferenceEquals(CardScaledEnergyModel.Discarded(before, card, false).Context, before) &&
                before.QueryFrame!.Energy == after.QueryFrame!.Energy, "Unplayed discard must be a no-op without reading paid cost.");
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Scaled-energy callback or guard mutated its parent.");
        }
        Require(paidValues.SequenceEqual([3, 0]), "Native positive/zero X controls differ.");
        foreach (int index in new[] { 2, 4 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            CardSourceTrait[] traits = card.MaskDescriptor!.SourceTraits!.ToArray();
            Require(card.DataId == "001ff190-4559-42b9-9ca0-73e557656e85" && rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" &&
                rule.Effects.Single().Type == "GainEnergy" && rule.Effects[0].Value == 0 && rule.ExternalInteractions.Count == 0 && rule.HandDiscardInteractions.Count == 0 &&
                traits.Select(trait => trait.Type).SequenceEqual([CardScaledEnergyModel.Trait, "CardTraitExhaustState"]) &&
                traits.All(trait => trait.Int == 2 && trait.Int2 == 0 && trait.Int3 == 0 && !trait.Bool && trait.Str == "" && trait.Float == 2 && trait.Scaling &&
                    trait.TrackedValue == "PlayedCost" && trait.EntryDuration == "ThisBattle") &&
                before.Energy == (index == 2 ? 3 : 0) && after.Energy == (index == 2 ? 6 : 0) &&
                after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                after.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesExhausted", "ThisBattle") == 2,
                "Original source parameters, zero effect gain, paid X, current ember or Consume routing differs.");
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options), "Ember callback changed a unit.");
        }
        Require(actions.All(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() != ids[5]) &&
            root.GetProperty("Turns").EnumerateArray().Any(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Statistics!.Value(ids[5], "TimesDiscarded", "ThisBattle") > 0),
            "Unplayed original must actually discard/redraw without a recorded play.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SCALED-ENERGY PASS: original GainXEnergy, paid X three/zero gives six/zero during played discard before paid-cost clear, unplayed no-op/discard/redraw, Consume, exact source/route guards and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
