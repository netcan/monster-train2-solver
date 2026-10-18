using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalConsumedUpgradeChecks
{
    private const string DraffId = "3b74c955-fd58-4b60-81a7-e2d6a0aaa2d4";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        int anchor = Array.FindIndex(ids.Take(2).ToArray(), id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        int cardId, room;
        if (anchor >= 0) { cardId = ids[anchor]; room = anchor; }
        else if (source.Spawn.Turn < 1) return null;
        else if (context.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") == 0) { cardId = ids[2]; room = 0; }
        else if (source.Spawn.Turn == 2 && context.Statistics!.Value(ids[2], "TimesPlayed", "ThisBattle") == 1)
        {
            CardToken? remaining = context.Cards.Hand.FirstOrDefault(card => card.InstanceId != ids[2]);
            cardId = remaining?.InstanceId ?? ids[2]; room = 0;
        }
        else return null;
        var action = new PlayCardAction(cardId, room, playerPosition: context.FindCard(cardId)!.MaskDescriptor!.Definition.CardType == "Monster" ? 0 : -1);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalConsumedUpgradePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original consumed-upgrade witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalConsumedUpgradeIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), samples = root.GetProperty("OriginalConsumedUpgradeSamples").EnumerateArray().ToArray(),
            queries = root.GetProperty("OriginalConsumedUpgradeQueries").EnumerateArray().ToArray();
        Require(ids.Length == 6 && actions.Length == 6 && samples.Length == 2 && queries.Length == 5 &&
            samples.Select(row => row.GetProperty("Actual").Deserialize<CardUpgradeModifier>()!.Stats.Damage).SequenceEqual([30, 0]),
            "Missing same-source three/zero consumed counters and fresh upgrades.");
        foreach (FixtureValue row in samples.Concat(queries))
        {
            CombatContext context = row.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = row.GetProperty("CardId").GetInt32(), sourceId = row.GetProperty("SourceCardId").GetInt32();
            CardUpgradeModifier input = row.GetProperty("Input").Deserialize<CardUpgradeModifier>()!, actual = row.GetProperty("Actual").Deserialize<CardUpgradeModifier>()!;
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            UnitUpgradeScalingResult result = CardConsumedUpgradeModel.Apply(context, id, sourceId, input);
            int count = sourceId == 0 ? 0 : context.FindCard(sourceId)!.EffectCounters!.Single().Value;
            Require(result.Supported && row.GetProperty("Returned").GetBoolean() && input.Stats.Damage == 10 && actual.Stats.Damage == 10 * count,
                "Native consumed upgrade callback, unscaled fresh input or private source counter differs.");
            Equal(result.Upgrade, actual, "native upgrade callback"); Equal(result.Context, context, "query context");
            if (row.TryGetProperty("After", out FixtureValue after)) Equal(context, after.Deserialize<CombatContext>(), "callback state");
            if (row.TryGetProperty("StateUnchanged", out FixtureValue unchanged)) Require(unchanged.GetBoolean(), "Restored native query changed game state or RNG.");
            CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardSourceTrait source = descriptor.SourceTraits!.Single();
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait changed = new(source.Type, source.Int, 1, source.Int3, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue, source.EntryDuration);
            Require(!CardConsumedUpgradeModel.Apply(context.WithCard(WithSource(null)), id, sourceId, input).Supported &&
                !CardConsumedUpgradeModel.Apply(context.WithCard(WithSource([changed])), id, sourceId, input).Supported &&
                !CardConsumedUpgradeModel.Apply(context, id, sourceId, input.WithScaledStats(11, 0)).Supported &&
                !CardConsumedUpgradeModel.Apply(context.WithCard(card.WithCounter(0, "CardEffectDiscardHand", -1)), id, id, input).Supported,
                "Missing source metadata, changed source, altered payload and negative counter must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Consumed upgrade query or guards mutated the parent.");
        }
        FixtureValue[] casts = actions.Where(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[2]).ToArray();
        Require(casts.Length == 2, "Original source was not replayed twice.");
        for (int index = 0; index < casts.Length; index++)
        {
            BattleTurnState before = casts[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = casts[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext old = before.Spawn.Train.Context!, context = after.Spawn.Train.Context!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardConsumedUpgradeModel.CardId);
            int count = index == 0 ? 3 : 0;
            Require(rule.Cost == 0 && rule.CostType == "Default" && rule.ExternalInteractions.Count == 0 && rule.Effects.Select(effect => effect.Type).SequenceEqual(["DiscardHand", "Generate"]) &&
                rule.Effects[0].Value == 1 && rule.Effects[1].Generation!.Pool.Single().DataId == DraffId && rule.Effects[1].Generation!.Count == 0 &&
                !rule.Effects[1].Generation!.UpgradeIsPermanent && before.Energy == after.Energy && old.Cards.Hand.Count == count + 1 &&
                old.FindCard(ids[2])!.EffectCounters!.Single().Value == (index == 0 ? 0 : 3) && context.FindCard(ids[2])!.EffectCounters!.Single().Value == count &&
                context.Cards.Hand.Count == 1 && context.Cards.Discard.Any(card => card.InstanceId == ids[2]),
                "Original cost, ordered consume/generation rules, source counter reset, piles or hand size differs.");
            foreach (CardToken token in context.Cards.Hand)
            {
                CardInstanceState card = context.FindCard(token.InstanceId)!;
                CardUpgradeModifier upgrade = card.Temporary.Upgrades.Single(item => item.DataId == CardConsumedUpgradeModel.UpgradeId);
                Require(token.InstanceId >= old.NextCardId && token.DataId == DraffId && upgrade.Stats.Damage == 10 * count &&
                    card.Permanent.Upgrades.All(item => item.DataId != CardConsumedUpgradeModel.UpgradeId), "A generated card reused/scaled another recipient's upgrade or made it permanent.");
            }
            if (index == 0) Require(ids.Skip(3).All(id => after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id)), "Original fodder was not consumed.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-CONSUMED-UPGRADE PASS: unchanged Sacrificial Ressurection and Draff; same source consumes three then zero, fresh upgrades thirty/zero, native zero/one/three/seven/null-source controls, exact counter reset, guarded payloads and 16 immutable full chains.");
    }
    private static void Equal<T>(T predicted, T actual, string label) => Require(ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)) == null, label + " differs.");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
