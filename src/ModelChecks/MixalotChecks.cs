using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MixalotChecks
{
    private const string DataId = "a68aa4ef-ff56-4d3f-b760-ddb7b2bbe207";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] cards)
    {
        CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
        var steward = plays.FirstOrDefault(play => play.RoomIndex == 0 && context.FindCard(play.CardInstanceId)!.DataId == StewardId);
        if (steward != null) return steward;
        for (int index = 0; index < 2; index++)
            if (context.Statistics!.Value(cards[index], "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == cards[index] && play.RoomIndex == 0);
        bool hasElixir = context.Cards.Hand.Any(card => source.PlayRules!.Elixirs!.Recipes.Any(recipe => recipe.Creation.DataId == card.DataId));
        if (context.Statistics!.Value(cards[2], "TimesPlayed", "ThisBattle") == 0)
        {
            if (hasElixir) return plays.FirstOrDefault(play => play.RoomIndex == 0 && source.PlayRules!.Elixirs!.Recipes.Any(recipe =>
                recipe.Creation.DataId == context.FindCard(play.CardInstanceId)!.DataId));
            return plays.FirstOrDefault(play => play.CardInstanceId == cards[2] && play.RoomIndex == 0);
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.DataId != DataId);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("MixalotPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed random-mix witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int refused = 0, fresh = 0, upgraded = 0, excludedPrevious = 0;
        foreach (FixtureValue row in root.GetProperty("MixalotRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("TestReturned").GetBoolean(), "Incomplete native random-mix effect/Test.");
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = row.GetProperty("After").Deserialize<CombatContext>()!;
            var catalog = row.GetProperty("Catalog").Deserialize<ElixirCatalog>()!;
            int source = row.GetProperty("CardId").GetInt32();
            Require(before.FindCard(source)!.DataId == DataId, "Original Mixalot identity changed.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = ElixirRandomMixModel.Apply(before, catalog, 5);
                Require(result.Supported, result.UnsupportedReason ?? "Random mixing unsupported.");
                Equal(result.Context, after, "complete native random-mix state/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Random mixing mutated its parent.");
            if (row.GetProperty("IsolatedFullHand").GetBoolean())
            {
                Require(before.Cards.Hand.Count == before.MaxHandSize, "Missing full-hand control.");
                Equal(before, after, "Full-hand random mix must preserve card identities, selection history and RNG"); refused++; continue;
            }
            CardToken? current = before.Cards.Hand.FirstOrDefault(card => catalog.Recipes.Any(recipe => recipe.Creation.DataId == card.DataId));
            Require(after.NextCardId == before.NextCardId + 1, "Random mix allocated intermediate registered cards.");
            CardInstanceState resultCard = after.FindCard(before.NextCardId)!;
            Require(after.Cards.Hand.Any(card => card.InstanceId == resultCard.InstanceId) &&
                !after.BattleRng.Equals(before.BattleRng), "Random mix did not place the completed elixir or advance Battle RNG.");
            if (current == null)
            {
                Require(resultCard.Permanent.Upgrades.Count == 5 && after.MixState!.LastCardDataId == resultCard.DataId,
                    "Fresh elixir must have its base upgrade plus four random upgrades.");
                if (before.MixState!.LastCardDataId != null)
                {
                    Require(resultCard.DataId != before.MixState.LastCardDataId, "Fresh random mix repeated the previous recipe."); excludedPrevious++;
                }
                fresh++;
            }
            else
            {
                Require(resultCard.DataId == current.DataId && resultCard.Permanent.Upgrades.Count == before.FindCard(current.InstanceId)!.Permanent.Upgrades.Count + 5 &&
                    after.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == current.InstanceId),
                    "Existing elixir did not gain five upgrades while retaining its prior standby card."); upgraded++;
            }
        }
        Require(refused == 1 && fresh == 2 && upgraded == 1 && excludedPrevious == 1, "Missing full-hand, existing or repeated-fresh random-mix controls.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("MIXALOT PASS: three original fresh/existing/fresh casts, full-hand Test/no-op, five upgrades, previous-recipe exclusion, exact RNG and 16 immutable full-context branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
