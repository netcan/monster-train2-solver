using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DoubleJuiceSpikeChecks
{
    internal static void Native(FixtureValue refusal, FixtureValue source, FixtureValue replay)
    {
        const string rejected = "Juice with this card or ordered scaling traits requires native calibration.";
        const string reason = "Native double Juice with paid-energy status scaling.";
        var old = NativeBranchObservationChecks.Replay(refusal, rejected);
        if (old.Length != 1 || old[0].GetProperty("Expected").ValueKind != FixtureKind.Null)
            throw new InvalidOperationException("Original natural deployment refusal missing.");
        var first = NativeBranchObservationChecks.Replay(source, reason);
        var second = NativeBranchObservationChecks.Replay(replay, reason);
        if (first.Length != 11 || second.Length != 11 || !source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")) ||
            NativeBranchObservationChecks.Key(old[0].GetProperty("Before").Deserialize<BattleTurnState>()!) !=
                NativeBranchObservationChecks.Key(first[0].GetProperty("Before").Deserialize<BattleTurnState>()!))
            throw new InvalidOperationException("Independent double Juice deployment/prefix differs.");
        for (int i = 0; i < first.Length; i++)
        {
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                if (NativeBranchObservationChecks.Key(first[i].GetProperty(field).Deserialize<BattleTurnState>()!) !=
                    NativeBranchObservationChecks.Key(second[i].GetProperty(field).Deserialize<BattleTurnState>()!))
                    throw new InvalidOperationException("Independent double Juice state/RNG differs at " + i);
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                if (!first[i].GetProperty(field).ContentEquals(second[i].GetProperty(field)))
                    throw new InvalidOperationException("Independent double Juice input differs.");
        }
        var before = first[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = first[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = first[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(12)!;
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == action.TargetUnitId);
        if (before.Spawn.Turn != 2 || before.Energy != 4 || after.Energy != 0 || action.CardInstanceId != 12 ||
            CardJuiceModel.Validate(card) != null || CardJuiceModel.Validate(before.Spawn.Train.Context.FindCard(25)!) != null ||
            Target(after).Status("valor")!.Stacks != (Target(before).Status("valor")?.Stacks ?? 0) + 32)
            throw new InvalidOperationException("Paid4/scaled16/double32 Valor or concurrent triple owner differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, action);
            if (!played.Supported || NativeBranchObservationChecks.Key(played.State!) != NativeBranchObservationChecks.Key(after))
                throw new InvalidOperationException("Detached double Juice cast differs.");
        });
        if (parent != JsonSerializer.Serialize(before, ModelJson.Options)) throw new InvalidOperationException("Double Juice changed parent.");
        foreach (string control in new[] { "Owner", "Order", "Multiplier", "Scaling", "Upgrade", "Temporary" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            if (control == "Owner") changed["DataId"] = "unknown-spike";
            if (control == "Order")
            {
                var traits = changed["MaskDescriptor"]!["BaseTraits"]!.AsArray();
                var start = traits[0]!.DeepClone(); traits[0] = traits[2]!.DeepClone(); traits[2] = start;
            }
            if (control == "Multiplier") changed["MaskDescriptor"]!["BaseTraits"]![2]!["ParamInt"] = 3;
            if (control == "Scaling") changed["StatusScalingTraits"]![0]!["StacksPerStat"] = 5;
            if (control == "Upgrade") changed["Permanent"]!["Upgrades"]![1]!["AssetKey"] = "OtherJuice";
            if (control == "Temporary") changed["Temporary"]!["Upgrades"]!.AsArray().Add(changed["Permanent"]!["Upgrades"]![1]!.DeepClone());
            if (CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) == null)
                throw new InvalidOperationException("Unknown double Juice signature accepted: " + control);
        }
        Console.WriteLine("DOUBLE-JUICE-SPIKE PASS: original deployment refusal,11 independent native inputs/full state/forecast/RNG, paid4/scaled16/double32 Valor, four immutable branches and six guards.");
    }
}
