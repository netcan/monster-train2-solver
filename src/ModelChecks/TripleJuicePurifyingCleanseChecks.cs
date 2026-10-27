using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleJuicePurifyingCleanseChecks
{
    internal const string Reason = "Triple Juice with this card or ordered scaling traits requires native calibration.";
    internal const string CastReason = "Native triple-Juice Purifying Cleanse healing and Burnout application.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 1 && rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing original deployment/first-EndTurn rejection.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 0 && after.Spawn.Turn == 1, "Wrong native triple-Juice boundary.");
        var card = before.Spawn.Train.Context!.FindCard(8)!;
        Require(card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" && CardJuiceModel.Validate(card) == null &&
            CardJuiceModel.Apply(card, 3) == 9, "Missing natural Purifying Cleanse triple upgrade.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Parallel triple-Juice deployment branch differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Triple-Juice check changed its parent.");
        foreach (string control in new[] { "Owner", "Multiplier", "DefinitionMultiplier", "Upgrade", "Damage", "Scaling", "MissingMask", "Temporary" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            if (control == "Owner") changed["DataId"] = "unknown-cleanse";
            if (control == "Multiplier") changed["MaskDescriptor"]!["BaseTraits"]![0]!["ParamInt"] = 4;
            if (control == "DefinitionMultiplier") changed["MaskDescriptor"]!["BaseTraits"]![0]!["DefinitionParamInt"] = 4;
            if (control == "Upgrade") changed["Permanent"]!["Upgrades"]![0]!["AssetKey"] = "OtherJuice";
            if (control == "Damage") changed["Permanent"]!["Upgrades"]![0]!["Stats"]!["Damage"] = 1;
            if (control == "Scaling") changed["StatusScalingTraits"] = JsonSerializer.SerializeToNode(new[] {
                new ScalingStatusTrait(new CardStatisticQuery("PlayedCost", "ThisBattle"), 1, false, 0, ["ephemeral"]) }, ModelJson.Options);
            if (control == "MissingMask") changed["MaskDescriptor"] = null;
            if (control == "Temporary") changed["Temporary"]!["Upgrades"]!.AsArray().Add(changed["Permanent"]!["Upgrades"]![0]!.DeepClone());
            Require(CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown Purifying Cleanse triple signature accepted: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TRIPLE-JUICE-PURIFYING PASS: original first-EndTurn guard, complete native successor/forecast/RNG, four immutable branches and eight signature controls; independentPair=" + replay.HasValue);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
