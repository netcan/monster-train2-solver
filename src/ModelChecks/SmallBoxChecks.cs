using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SmallBoxChecks
{
    private const string DataId = "f0ff6411-5c58-4b69-b025-0f6e4a52fca7";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] cards)
    {
        CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (int id in cards)
            if (context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == 0);
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.DataId != DataId);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("SmallBoxPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed unboxing witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int empty = 0, full = 0, limited = 0;
        foreach (FixtureValue row in root.GetProperty("SmallBoxRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("TestReturned").GetBoolean(), "Incomplete unboxing/Test.");
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = row.GetProperty("After").Deserialize<CombatContext>()!;
            int source = row.GetProperty("CardId").GetInt32();
            Require(before.FindCard(source)!.DataId == DataId, "Original Small Box identity changed.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = SmallBoxModel.Apply(before, source);
                Require(result.Supported, result.UnsupportedReason ?? "Unboxing unsupported.");
                Equal(result.Context, after, "complete native unboxing state/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Unboxing mutated its parent.");
            var templates = before.FindCard(source)!.Permanent.BoxedCards!;
            Equal(templates, after.FindCard(source)!.Permanent.BoxedCards, "Unboxing must retain saved templates");
            if (templates.Count == 0) { Equal(before, after, "Empty box no-op"); empty++; continue; }
            Require(!before.BattleRng.Equals(after.BattleRng), "Filled box did not shuffle its entire contents.");
            if (row.GetProperty("IsolatedFullHand").GetBoolean())
            {
                Require(before.Cards.Hand.Count == before.MaxHandSize && before.NextCardId == after.NextCardId,
                    "Full-hand box allocated a card."); full++; continue;
            }
            Require(templates.Count == 3 && before.MaxHandSize - before.Cards.Hand.Count == 2 &&
                after.NextCardId == before.NextCardId + 2 && after.Cards.Hand.Count == after.MaxHandSize,
                "Missing capacity-limited two-of-three unboxing.");
            foreach (CardInstanceState created in after.CardInstances!.Where(card => card.InstanceId >= before.NextCardId))
            {
                Require(created.Permanent.Upgrades.Count == 1 && created.Temporary.Upgrades.Count == 1 &&
                    created.Permanent.Upgrades[0].ExcludeFromClones && created.Temporary.Upgrades[0].Stats.Damage == 2,
                    "Boxed starting upgrade duplicated/lost, temporary upgrade lost or clone-exclusion wrongly applied.");
            }
            limited++;
        }
        Require(empty == 1 && full == 1 && limited == 1, "Missing empty/full/capacity controls.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("SMALL-BOX PASS: original empty/filled casts, full-hand shuffle, two-of-three capacity, starting/temporary upgrade inheritance, clone-exclusion bypass, exact RNG and immutable parallel replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
