using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardDuplicateChecks
{
    private const string DataId = "98d0f24b-0249-463e-a4b5-5c3e3a178f49";
    private const string RecipientId = "f15d0004-0000-4000-8000-000000000001";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] cards, int recipient)
    {
        CombatContext context = source.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (int id in cards)
            if (context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0)
                return plays.FirstOrDefault(play => play.CardInstanceId == id && play.RoomIndex == 0 &&
                    play.RecursionSelections.Any(choice => choice.EffectIndex == 0 && choice.CardInstanceIds.SequenceEqual(new[] { recipient })));
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("CardDuplicatePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed hand-copy witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        var tests = new List<bool>();
        foreach (FixtureValue row in root.GetProperty("CardDuplicateTests").EnumerateArray())
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            var effect = row.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            int source = row.GetProperty("SourceId").GetInt32();
            var eligible = CardDuplicateModel.EligibleCards(context, effect, source, out string? error);
            bool native = row.GetProperty("Returned").GetBoolean();
            Require(error == null && eligible.Count > 0 == native, "Original duplication TestEffect differs: " + error);
            tests.Add(native);
        }
        Require(tests.SequenceEqual([false, true]), "Missing self-only negative and eligible-card positive native Tests.");
        CardPlayRule originalRule = root.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!
            .PlayRules!.Cards.Single(rule => rule.DataId == DataId);
        int copies = 0;
        foreach (FixtureValue row in root.GetProperty("CardDuplicateRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native copy effect.");
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = row.GetProperty("After").Deserialize<CombatContext>()!;
            var effect = row.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            int source = row.GetProperty("SourceId").GetInt32(), selected = row.GetProperty("SelectedId").GetInt32();
            Require(before.FindCard(source)!.DataId == DataId && before.FindCard(selected)!.DataId == RecipientId &&
                effect.Value == 3 && effect.CardFilter != null && effect.Upgrade != null, "Original Mass Production parameters changed.");
            CardActionEffect resolved = CardModifierModel.Resolve(originalRule, before.FindCard(source)!).Effects.Single(item => item.Type == "DuplicateCard");
            Equal(resolved.CardFilter, effect.CardFilter, "Resolved play rule lost the native copy filter");
            Equal(resolved.UpgradeFilters, effect.UpgradeFilters, "Resolved play rule lost native optional-upgrade filters");
            Require(before.Cards.Hand.Count == before.MaxHandSize - 1 && after.Cards.Hand.Count == after.MaxHandSize,
                "Full-hand source vacancy was not filled by one copy.");
            Require(after.NextCardId == before.NextCardId + 1 && after.BattleRng.Equals(before.BattleRng),
                "Hand copy allocated more than one identity or consumed pool RNG.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = CardDuplicateModel.Apply(before, effect, source, selected);
                Require(result.Supported, result.UnsupportedReason ?? "Hand copy unsupported.");
                Equal(result.Context, after, "native hand-copy complete context/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Hand copy mutated its parent.");
            foreach (int? invalid in new int?[] { null, source, int.MaxValue })
                Require(!CardDuplicateModel.Apply(before, effect, source, invalid).Supported, "Hand copy accepted an absent/self/missing choice.");
            var overflow = CardDuplicateModel.Apply(after, effect, source, selected);
            Require(overflow.Supported && overflow.AddedCards.Count == 0, "An already full hand allocated a temporary copy.");
            Equal(overflow.Context, after, "Full-hand copy refusal must preserve identities, upgrades and RNG");
            CardInstanceState copied = after.FindCard(before.NextCardId)!;
            Require(copied.Permanent.Upgrades.Any(upgrade => upgrade.Stats.Damage == 3 && upgrade.Stats.Health == 4) &&
                copied.Temporary.Upgrades.Any(upgrade => upgrade.Stats.Damage == 2) &&
                copied.Permanent.Upgrades.Any(upgrade => upgrade.DataId == effect.Upgrade!.DataId), "Permanent, temporary or optional copy upgrade was lost.");
            var eligible = CardDuplicateModel.EligibleCards(before, effect, source, out string? error);
            Require(error == null && before.Cards.Hand.Where(token => token.DataId == "f15d0004-0000-4000-8000-000000000002")
                .All(token => !eligible.Contains(token.InstanceId)), "Uncopyable Infusion filler became selectable.");
            copies++;
        }
        Require(copies == 2, "Missing two unchanged Mass Production casts.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("CARD-DUPLICATE PASS: two original full-hand casts, one copy despite ParamInt=3, native mask/optional upgrade, permanent+temporary inheritance, self-only negative Test and 16 immutable full-context branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
