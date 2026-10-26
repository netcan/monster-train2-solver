using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LazarusJuiceSpikeChecks
{
    internal const string Reason = "Native LazarusLeague Spike doubles paid-energy undying after scaling.";
    internal static void Native(FixtureValue refusal, FixtureValue? cast = null, FixtureValue? replay = null)
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
        if (cast.HasValue)
        {
            var first = NativeBranchObservationChecks.Replay(cast.Value, Reason);
            Require(first.Length == 7 && NativeBranchObservationChecks.Key(root) ==
                NativeBranchObservationChecks.Key(first[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
                "Missing original deployment and six native prefix inputs.");
            var before = first[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = first[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = first[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
            var played = after.Spawn.Train.Context!.FindCard(26)!;
            Require(before.Spawn.Turn == 1 && before.Energy == 4 && after.Energy == 0 &&
                action.CardInstanceId == 26 && action.RoomIndex == 0 && action.TargetUnitId == 5 &&
                Target(after).Status("undying")!.Stacks == (Target(before).Status("undying")?.Stacks ?? 0) + 8 &&
                played.LastPlayedCost == 4 && played.PlayCount == 1 && played.ExhaustState!.BaseTraits.Single().IsExhausted &&
                !after.Spawn.Train.Context.Cards.Hand.Any(value => value.InstanceId == 26) &&
                after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(value => value.InstanceId == 26),
                "Native paid4/scaled4/double8 Undying, accounting or Consume differs.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = BattleActionModel.PlayCard(before, action);
                Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                    "Detached Lazarus Juice cast differs.");
            });
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Lazarus Juice mutated its parent.");
            if (replay.HasValue)
            {
                NativeBranchObservationChecks.Replay(replay.Value, Reason);
                NativeBranchObservationChecks.Pair(cast.Value, replay.Value);
            }
        }
        Console.WriteLine("LAZARUS-JUICE-SPIKE PASS: original native refusal/full state/RNG, ten signature guards; nativeCast=" + cast.HasValue + "; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
