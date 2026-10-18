using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDrawUpgradeScalingChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalDrawUpgradeScalingPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Prism Retrieval witness.");
        foreach (FixtureValue action in root.GetProperty("Actions").EnumerateArray())
        {
            if (action.GetProperty("Difference").ValueKind == FixtureKind.Null) continue;
            BattleActionResult replay = BattleActionModel.PlayCard(action.GetProperty("Before").Deserialize<BattleTurnState>()!, action.GetProperty("Action").Deserialize<PlayCardAction>()!);
            string? difference = replay.Supported ? ModelJson.Difference(JsonSerializer.Serialize(replay.State, ModelJson.Options),
                JsonSerializer.Serialize(action.GetProperty("Actual").Deserialize<BattleTurnState>(), ModelJson.Options)) : replay.Reason;
            if (difference != null) throw new InvalidOperationException("Native Prism Retrieval mismatch: " + difference);
        }
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalDrawUpgradeScalingIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), samples = root.GetProperty("OriginalDrawUpgradeScalingSamples").EnumerateArray().ToArray(),
            queries = root.GetProperty("OriginalDrawUpgradeScalingQueries").EnumerateArray().ToArray();
        if (actions.Length != 6)
        {
            BattleTurnState decision = root.GetProperty("Turns").EnumerateArray().First().GetProperty("Actual").Deserialize<BattleTurnState>()!;
            BattleActionResult attempted = BattleActionModel.PlayCard(decision, new PlayCardAction(ids[2], 0));
            throw new InvalidOperationException("Missing native Prism Retrieval play: " + attempted.Rejection + ": " + attempted.Reason);
        }
        Require(ids.Length == 6 && samples.Length == 3 && queries.Length == 3 && samples.Select(row => row.GetProperty("Actual").Deserialize<CardUpgradeModifier>()!.Stats.Damage).SequenceEqual([15, 0, 50]),
            "Missing positive/zero/large X and actual upgrade callbacks.");
        foreach (FixtureValue row in samples.Concat(queries))
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(); CardUpgradeModifier input = row.GetProperty("Input").Deserialize<CardUpgradeModifier>()!, actual = row.GetProperty("Actual").Deserialize<CardUpgradeModifier>()!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            UnitUpgradeScalingResult predicted = CardDrawUpgradeScalingModel.Apply(context, id, input);
            int paid = context.Statistics!.PlayedCosts.Single(cost => cost.CardId == id).Cost;
            CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardDrawUpgradeScalingModel.Trait);
            Require(predicted.Supported && ModelJson.Difference(JsonSerializer.Serialize(predicted.Upgrade, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)) == null &&
                row.GetProperty("Returned").GetBoolean() && input.Stats.Damage == 5 && input.Stats.Cost == -1 && actual.Stats.Damage == 5 * paid && actual.Stats.Cost == -paid &&
                source.Int == 1 && source.Int2 == 0 && source.Int3 == 0 && !source.Bool && source.Str == "" && source.Float == 0 && source.Scaling &&
                source.TrackedValue == "PlayedCost" && source.EntryDuration == "ThisTurn" &&
                (!row.TryGetProperty("StateUnchanged", out FixtureValue unchanged) || unchanged.GetBoolean()), "Native upgrade multiplication or exact source/query parameters differ.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait changed = new(source.Type, source.Int, source.Int2, source.Int3, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue, "ThisBattle");
            Require(!CardDrawUpgradeScalingModel.Apply(context.WithCard(WithSource(null)), id, input).Supported &&
                !CardDrawUpgradeScalingModel.Apply(context.WithCard(WithSource(descriptor.SourceTraits.Select(trait => trait.Type == source.Type ? changed : trait).ToArray())), id, input).Supported &&
                !CardDrawUpgradeScalingModel.Apply(context, id, input.WithScaledStats(6, 0)).Supported, "Missing source, changed duration or altered upgrade must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Next-draw scaling query or guard mutated its parent.");
        }
        foreach (int index in new[] { 2, 3, 5 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext old = before.Spawn.Train.Context!, context = after.Spawn.Train.Context!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == "2e77fd27-8322-46ce-a56f-d45844ee577e");
            FixtureValue sample = samples.Single(row => row.GetProperty("CardId").GetInt32() == ids[index]);
            int targetId = sample.GetProperty("TargetCardId").GetInt32(), paid = index == 2 ? 3 : index == 3 ? 0 : 10;
            CardInstanceState recipient = context.FindCard(targetId)!;
            CardUpgradeModifier installed = recipient.Temporary.Upgrades.Single(upgrade => upgrade.AssetKey == "PrismRetrieval");
            Require(rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" && rule.ExternalInteractions.Count == 0 && rule.UpgradeInteractions!.Count == 0 &&
                rule.Effects.Select(effect => effect.Type).SequenceEqual(["ScheduleNextDrawUpgrade", "DrawType"]) && rule.Effects[1].Value == 1 && rule.Effects[1].TypedDraw!.CardType == "Monster" &&
                before.Energy == paid && after.Energy == 0 && context.Cards.Hand.Any(token => token.InstanceId == targetId) && !old.Cards.Hand.Any(token => token.InstanceId == targetId) &&
                recipient.MaskDescriptor!.Definition.CardType == "Monster" && recipient.MaskDescriptor.BaseCost == 4 && installed.Stats.Damage == 5 * paid && installed.Stats.Cost == -paid &&
                CardHandCostModel.Resolve(context, recipient, 4, "Monster") == Math.Max(0, 4 - paid) &&
                context.Cards.NextDrawUpgrades!.Count == 0 && context.Cards.NextUpgradeReferenceId == (index == 2 ? 2 : index == 3 ? 3 : 4) &&
                after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                context.Statistics!.Value(ids[index], "TimesExhausted", "ThisBattle") == 2 &&
                context.Cards.Draw.Any(token => context.FindCard(token.InstanceId)!.DataId == "ac110029-0000-4000-8000-000000000003"),
                "Original identity, paid X, typed draw, recipient upgrade/cost, queue allocation/reset, Consume or untouched Spell control differs.");
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.SelectMany(room => room.Units), ModelJson.Options), "Prism Retrieval changed a live unit.");
        }
        TypedDrawNativeChecks.Native(root, required: true);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-DRAW-UPGRADE-SCALING PASS: original Prism Retrieval X three/zero/ten, damage fifteen/zero/fifty and cost minus three/zero/ten, three unchanged fresh-upgrade queries, Monster draws with Spell control, shared queue mutation/clear, Consume, source/payload guards and 16 immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
