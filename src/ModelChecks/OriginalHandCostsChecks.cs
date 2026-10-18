using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalHandCostsChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0) return null;
        int room = index is 1 or 13 or 14 ? 1 : 0;
        int targetCard = index == 3 ? ids[0] : index == 10 ? ids[8] : 0;
        int target = targetCard == 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(value => value.Units).Single(value => value.SpawnerCardId == targetCard).Id;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room && (target == 0 || play.TargetUnitId == target));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalHandCostsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original hand cost witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalHandCostsIds").Deserialize<int[]>()!;
        int[] queryIds = root.GetProperty("OriginalHandCostsQueryIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), samples = root.GetProperty("OriginalHandCostsSamples").EnumerateArray().ToArray();
        Require(ids.Length == 15 && actions.Length == 15 && queryIds.Length == 17 && samples.Length == 16, "Missing hand cost controls or queries.");
        int[] payments = [2, 2, 1, 2, 2, 1, 0, 1, 1, 0, 1, 1, 0, 0, 0];
        for (int index = 0; index < actions.Length; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(actions[index].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[index] &&
                before.Energy - after.Energy == payments[index] && after.Spawn.Train.Context!.FindCard(ids[index])!.LastPlayedCost == payments[index],
                "Native play order, payment or played-cost statistic differs at " + index);
        }
        foreach (int index in new[] { 7, 11 })
        {
            var context = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            CardInstanceState card = context.FindCard(ids[index])!;
            Require(card.DataId == "13981930-b732-4c42-9bd3-1950d7a553b5" && card.MaskDescriptor!.SourceTraits!.Count == 5, "Titan Bane definition changed.");
            var traits = card.MaskDescriptor.SourceTraits.Take(4).ToArray();
            Require(traits.Select(trait => trait.HandCostTargetType).SequenceEqual(["Spell", "Monster", "TrainRoomAttachment", "Equipment"]) &&
                traits.All(trait => trait.Type == CardHandCostModel.Trait && trait.Int == 1 && trait.Int2 == 0 && trait.Int3 == 0 && !trait.Bool &&
                    trait.Str == "" && trait.Float == 0 && !trait.Scaling && trait.TrackedValue == "SubtypeInDeck" && trait.EntryDuration == "ThisTurn") &&
                card.MaskDescriptor.BaseTraits.Take(4).Select(trait => trait.HandCostTargetType).SequenceEqual(traits.Select(trait => trait.HandCostTargetType)),
                "Missing exact original hand cost source or runtime type masks.");
            Require(actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Cards.Discard.Any(value => value.InstanceId == ids[index]),
                "Played Titan Bane must leave the hand and enter discard.");
        }
        int count = 0;
        foreach (FixtureValue sample in samples)
        {
            CombatContext context = sample.GetProperty("Context").Deserialize<CombatContext>()!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            foreach (FixtureValue row in sample.GetProperty("Costs").EnumerateArray())
            {
                int id = row.GetProperty("CardId").GetInt32(); CardInstanceState card = context.FindCard(id)!;
                Require(CardHandCostModel.Resolve(context, card, card.MaskDescriptor!.BaseCost, card.MaskDescriptor.Definition.CardType) == row.GetProperty("Cost").GetInt32(),
                    "Live hand cost query differs: card " + id);
                count++;
            }
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Cost query mutated its parent.");
        }
        int Cost(int sample, int card) => samples[sample].GetProperty("Costs").EnumerateArray().Single(row => row.GetProperty("CardId").GetInt32() == queryIds[card]).GetProperty("Cost").GetInt32();
        Require(count == 272 && Cost(0, 2) == 1 && Cost(8, 9) == 0 && Cost(12, 12) == 0 &&
            Cost(0, 15) == 1 && Cost(8, 15) == 1 && Cost(12, 15) == 0 && Enumerable.Range(0, 16).All(index => Cost(index, 16) == 0),
            "Native upgrade-order, upper-clamp, source-removal or outside-hand control differs.");
        foreach (string? filter in new string?[] { null, "Unknown" })
        {
            bool rejected = false;
            try { CardHandCostModel.Apply(0, "Spell", [new CardTraitValue(CardHandCostModel.Trait, CardHandCostModel.Trait, 1, 1, true, 0, handCostTargetType: filter)]); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "Missing/unknown hand cost type filter must remain unsupported.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-HAND-COSTS PASS: two original Titan Banes, four type filters, stacking, Blight/Junk controls, ordered reductions/upper clamp, outside-hand control, live removal, 272 native cost queries and 16 immutable battle chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
