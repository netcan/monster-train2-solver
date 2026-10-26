using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LazarusJuiceSpikeChecks
{
    internal const string Reason = "Native LazarusLeague Spike doubles paid-energy undying after scaling.";
    internal static void Native(FixtureValue refusal)
    {
        const string oldReason = "Juice with this card or ordered scaling traits requires native calibration.";
        var rows = NativeBranchObservationChecks.Replay(refusal, oldReason);
        Require(rows.Length == 1 && rows[0].GetProperty("Expected").ValueKind == FixtureKind.Null,
            "Missing original natural deployment refusal.");
        var root = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var card = root.Spawn.Train.Context!.FindCard(26)!;
        Require(root.Spawn.Turn == 0 && root.Energy == 6 && card.DataId == LazarusJuiceSpikeModel.DataId &&
            CardJuiceModel.Validate(card) == null && LazarusJuiceSpikeModel.Known(card), "Wrong captured Lazarus Juice Spike.");
        foreach (string control in new[] { "Id", "Order", "Multiplier", "Scaling", "Query", "Status", "Upgrade", "Payload", "Temporary", "Callback" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            if (control == "Id") changed["DataId"] = "unknown-spike";
            if (control == "Order")
            {
                var traits = changed["MaskDescriptor"]!["BaseTraits"]!.AsArray();
                var first = traits[0]!.DeepClone(); traits[0] = traits[1]!.DeepClone(); traits[1] = first;
            }
            if (control == "Multiplier") changed["MaskDescriptor"]!["BaseTraits"]![2]!["ParamInt"] = 3;
            if (control == "Scaling") changed["StatusScalingTraits"]![0]!["StacksPerStat"] = 2;
            if (control == "Query") changed["StatusScalingTraits"]![0]!["Query"]!["Duration"] = "ThisBattle";
            if (control == "Status") changed["StatusScalingTraits"]![0]!["StatusIds"]![0] = "valor";
            if (control == "Upgrade") changed["Permanent"]!["Upgrades"]![0]!["DataId"] = "unknown-juice";
            if (control == "Payload") changed["Permanent"]!["Upgrades"]![0]!["Stats"]!["Damage"] = 10;
            if (control == "Temporary") changed["Temporary"]!["Upgrades"]!.AsArray().Add(changed["Permanent"]!["Upgrades"]![0]!.DeepClone());
            if (control == "Callback") changed["ExternalInteractions"]!.AsArray().Add("unknown-callback");
            var unknown = changed.Deserialize<CardInstanceState>(ModelJson.Options)!;
            Require(!LazarusJuiceSpikeModel.Known(unknown) && CardJuiceModel.Validate(unknown) != null,
                "Unknown Lazarus Juice signature admitted: " + control);
        }
        Console.WriteLine("LAZARUS-JUICE-SPIKE DEPLOYMENT PASS: original native refusal/full successor/RNG and ten signature guards; actual cast remains guarded.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
