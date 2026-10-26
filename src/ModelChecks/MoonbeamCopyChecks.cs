using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonbeamCopyChecks
{
    internal const string DeploymentReason = "Copy-on-play requires no authored traits or one ordinary authored Consume, optionally with one ordinary temporary Freeze.";

    internal static void Deployment(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, DeploymentReason);
        Require(rows.Length == 1, "Expected original deployment rejection.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(26)!;
        Require(card.DataId == CardCopyOnPlayModel.MoonbeamSpellId && CardCopyOnPlayModel.Validate(card) == null &&
            card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey).SequenceEqual(["ChaosScepter", "CreateSpellCopyWithPurge"]),
            "Lost original Moonbeam consume/copy upgrades.");
        foreach (string field in new[] { "Int", "TrackedValue", "StatusEffectIds", "Removable", "DataId" })
        {
            var node = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var authored = node["MaskDescriptor"]!["SourceTraits"]![0]!;
            if (field == "Int") authored[field] = 2;
            if (field == "TrackedValue") authored[field] = "SubtypeInDeck";
            if (field == "StatusEffectIds") authored[field] = JsonSerializer.SerializeToNode(new[] { "poison" });
            if (field == "Removable") node["MaskDescriptor"]!["BaseTraits"]![0]![field] = true;
            if (field == "DataId") node["MaskDescriptor"]!["Definition"]![field] = "uncalibrated";
            Require(CardCopyOnPlayModel.Validate(node.Deserialize<CardInstanceState>(ModelJson.Options)!) == DeploymentReason,
                "Uncalibrated Moonbeam signature accepted: " + field);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, DeploymentReason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("MOONBEAM-COPY-DEPLOYMENT PASS: original rejection/full native state/RNG, five signature rejection controls; independentPair=" +
            replay.HasValue + "; spell/copy execution requires separate native evidence.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
