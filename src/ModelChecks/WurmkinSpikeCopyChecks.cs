using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class WurmkinSpikeCopyChecks
{
    private const string Reason = "Copy-on-play requires no authored traits or one ordinary authored Consume, optionally with one ordinary temporary Freeze.";
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
