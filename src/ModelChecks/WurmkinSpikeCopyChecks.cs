using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class WurmkinSpikeCopyChecks
{
    private const string Reason = "Copy-on-play requires no authored traits or one ordinary authored Consume, optionally with one ordinary temporary Freeze.";
    private const string CopyReason = "Observed copied spike intrinsic mask differs after native discard.";
    private static string[] Traits(CardInstanceState card) => card.MaskDescriptor!.BaseTraits.Select(trait => trait.RuntimeType).ToArray();

    internal static void Copy(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, CopyReason, (old, actual) =>
        {
            var missing = old.Spawn.Train.Context!.FindCard(35)!;
            var inherited = actual.Spawn.Train.Context!.FindCard(35)!;
            Require(Traits(missing).SequenceEqual(new[] { "CardTraitExhaustState", "CardTraitScalingAddStatusEffect" }) &&
                Traits(inherited).SequenceEqual(new[] { "CardTraitExhaustState", "CardTraitScalingAddStatusEffect", "CardTraitIntrinsicState" }),
                "Lost historical copied intrinsic discrepancy.");
        });
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, CopyReason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Require(rows.Length == 8, "Wrong copied-spike native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        var original = context.FindCard(22)!;
        var copy = after.FindCard(context.NextCardId)!;
        Require(action.CardInstanceId == 22 && before.Spawn.Turn == 1 && context.NextCardId == 35 &&
            original.DataId == CardCopyOnPlayModel.WurmkinSpikeSpellId && CardCopyOnPlayModel.SupportedIntrinsicSpike(original) &&
            after.NextCardId == 36 && copy.DataId == original.DataId &&
            CardCopyOnPlayModel.SupportedIntrinsicSpike(copy) && !CardCopyOnPlayModel.Present(copy) &&
            after.Cards.Hand.Any(token => token.InstanceId == copy.InstanceId) &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == original.InstanceId) &&
            copy.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(new[] { "TraitInherentSpellReduceCost" }) &&
            copy.Temporary.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(new[] { "CopyOnPlayTraitUpgrade" }),
            "Copied spike lost identity, intrinsic X bonus, replacement or native pile routing.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == native && JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                "Copied-spike play changed its native successor/RNG or immutable parent.");
        });
        Console.WriteLine("WURMKIN-SPIKE-COPY PASS: eight native inputs/full state/RNG, original lost intrinsic counterexample, authored scaling/X bonus, copy replacement and four immutable branches; independentPair=" + replay.HasValue);
    }

    internal static void Deployment(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Require(rows.Length == 1, "Expected the original zero-prefix deployment refusal.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(22)!;
        Require(card.DataId == CardCopyOnPlayModel.WurmkinSpikeSpellId && CardCopyOnPlayModel.Present(card) &&
            CardCopyOnPlayModel.Validate(card) == null && CardCopyOnPlayModel.SupportedIntrinsicSpike(card),
            "Missing original copied WurmkinSpike and intrinsic X-cost upgrade signature.");
        foreach (string field in new[] { "Int", "Float", "Scaling", "StatusEffectIds" })
        {
            var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var trait = node["MaskDescriptor"]!["SourceTraits"]![1]!;
            if (field == "Scaling") trait[field] = false;
            else if (field == "StatusEffectIds") trait[field] = JsonSerializer.SerializeToNode(new[] { "poison" });
            else trait[field] = 7;
            Require(CardCopyOnPlayModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) == Reason,
                "Uncalibrated WurmkinSpike source signature was accepted: " + field);
        }
        var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
        changed["Permanent"]!["Upgrades"]![0]!["Stats"]!["XCost"] = 2;
        Require(CardCopyOnPlayModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
            "Changed intrinsic upgrade was accepted.");
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Copied-spike deployment differs or mutated its parent.");
        });
        Console.WriteLine("WURMKIN-SPIKE-COPY-DEPLOYMENT PASS: original refusal and exact authored/intrinsic signature, five rejection controls, full native successor/RNG and four immutable branches; independentPair=" + replay.HasValue + "; actual spell execution still requires separate native evidence.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
