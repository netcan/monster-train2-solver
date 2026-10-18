using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RunCardGenerationChecks
{
    private const string FirstId = "66189a7b-2593-4b08-aafd-f1bdae7e3c2a";
    private const string SecondId = "0130a4a0-2269-4003-97e1-58d1f5ce3bd8";
    private const string ThirdId = "c778fab6-585a-40ea-9f26-c460b8b82eae";
    private const string ControlId = "f15d0001-0000-4000-8000-000000000001";
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        CombatContext context = source.Spawn.Train.Context!;
        var plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (string dataId in new[] { ControlId, FirstId, "f15d0001-0000-4000-8000-000000000003", SecondId, ThirdId })
        {
            var choice = plays.FirstOrDefault(play => play.RoomIndex == 2 && context.FindCard(play.CardInstanceId)!.DataId == dataId);
            if (choice != null) return choice;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("RunCardGenerationPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed run-card witness.");
        RecordedBattleChecks.Native(root);
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int count = 0, copies = 0, skipped = 0, empty = 0, overflow = 0, evolved = 0;
        var destinations = new HashSet<string>();
        foreach (FixtureValue row in root.GetProperty("CardGenerations").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            var after = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            var rule = row.GetProperty("Rule").Deserialize<CardGenerationRule>()!;
            int source = row.GetProperty("SourceCardId").GetInt32();
            Require(rule.Permanent == true && rule.Count == 1 && !rule.RequireHandSpace && !rule.IgnoreTemporaryModifiers,
                "AddRunCard inherited ignored battle-generation parameters.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                var result = CardGenerationModel.Apply(before, rule, source);
                Require(result.Supported, result.UnsupportedReason ?? "Permanent generation unsupported.");
                Equal(result.Context, after, "native permanent generation full context/RNG");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Permanent generation mutated its parent.");
            int created = after.NextCardId - before.NextCardId;
            bool ephemeral = before.FindCard(source) is { } card && CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitEphemeral");
            if (rule.IgnoreIfEphemeral == true && ephemeral)
            {
                Require(created == 0, "Ephemeral source generated a permanent card.");
                Equal(before, after, "Ephemeral skip must preserve RNG, pending upgrades and identities"); skipped++;
            }
            else if (rule.Pool.Count == 0)
            {
                Equal(before, after, "Empty pool must preserve complete state"); empty++;
            }
            else
            {
                Require(created == 1, "Permanent generation did not create exactly one card.");
                Require(after.Resources!.VisibleDeckCount == before.Resources!.VisibleDeckCount + 1 &&
                    after.Statistics!.DeckCards!.Contains(before.NextCardId), "Generated card was not permanently owned.");
                var generated = after.FindCard(before.NextCardId)!;
                destinations.Add(rule.Destination);
                if (rule.CopyModifiers && before.FindCard(source)?.DataId == ControlId)
                {
                    Require(generated.Permanent.Upgrades.Any(upgrade => upgrade.Stats.Damage == 3 && upgrade.Stats.Health == 4) &&
                        generated.Temporary.Upgrades.Any(upgrade => upgrade.Stats.Damage == 2), "Source permanent/temporary upgrades were not copied.");
                    copies++;
                }
                if (rule.Destination == "HandPile" && before.Cards.Hand.Count == before.MaxHandSize)
                {
                    Require(after.OtherPiles!.Single(pile => pile.Name == "Standby").ExhaustConditions!
                        .Any(condition => condition.CardId == before.NextCardId && condition.KeepInStandBy && condition.ExhaustedCardId == null) &&
                        after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == before.NextCardId) &&
                        after.Statistics.Value(before.NextCardId, "TimesExhausted", "ThisBattle") == 1,
                        "Full-hand permanent generation lost standby/exhaust routing or its statistic."); overflow++;
                }
                if (before.FindCard(source)?.DataId is FirstId or SecondId) evolved++;
            }
            count++;
        }
        Require(count >= 8 && copies == 5 && skipped == 1 && empty == 1 && overflow == 1 && evolved >= 2 && destinations.Count == 5,
            $"Incomplete permanent-generation controls: {count} effects/{copies} copies/{skipped} skips/{empty} empty/{overflow} overflow/{evolved} evolutions/{destinations.Count} piles.");
        bool playedPurge = false, discardedPurge = false;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            int id = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            if (before.Spawn.Train.Context!.FindCard(id)?.DataId != "f15d0001-0000-4000-8000-000000000003") continue;
            CombatContext after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            Require(before.Spawn.Train.Context.Statistics!.DeckCards!.Contains(id) && !after.Statistics!.DeckCards!.Contains(id) &&
                after.Resources!.VisibleDeckCount == before.Spawn.Train.Context.Resources!.VisibleDeckCount - 1,
                "Played permanent Ephemeral purge did not remove one owned/visible card.");
            playedPurge = true;
        }
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            var card = before.Cards.Hand.FirstOrDefault(card => card.DataId == "f15d0001-0000-4000-8000-000000000004");
            if (card == null) continue;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            Require(before.Statistics!.DeckCards!.Contains(card.InstanceId) && !after.Statistics!.DeckCards!.Contains(card.InstanceId) &&
                after.Resources!.VisibleDeckCount == before.Resources!.VisibleDeckCount - 1,
                "Unplayed permanent Ephemeral discard did not remove one owned/visible card.");
            discardedPurge = true;
        }
        Require(playedPurge && discardedPurge, "Missing permanent Ephemeral play/discard purge witnesses.");
        Console.WriteLine($"RUN-CARD-GENERATION PASS: {count} native effects, five permanent destinations, full-hand standby/exhaust overflow, empty pool, Ephemeral skip, both source modifier lifetimes and original evolution; full state/RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
