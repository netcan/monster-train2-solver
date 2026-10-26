using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InherentJuiceSpikeChecks
{
    private const string Reason = "Native inherent double Juice with bonus X cost.";
    internal static void Native(FixtureValue refusal, FixtureValue source, FixtureValue replay)
    {
        var rejected = refusal.GetProperty("Rows").EnumerateArray().Single();
        Require(rejected.GetProperty("Expected").ValueKind == FixtureKind.Null &&
            rejected.GetProperty("UnsupportedReason").GetString() == "Juice with this card or ordered scaling traits requires native calibration.",
            "Original inherent Spike deployment refusal was lost.");
        var oldRoot = rejected.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var deployed = BattleActionModel.PlayCard(oldRoot, rejected.GetProperty("Action").Deserialize<PlayCardAction>()!);
        Require(deployed.Supported && NativeBranchObservationChecks.Key(deployed.State!) ==
            NativeBranchObservationChecks.Key(rejected.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Original native deployment differs.");
        var first = NativeBranchObservationChecks.Replay(source, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        Require(first.Length == 8 && second.Length == 8 && source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")) &&
            NativeBranchObservationChecks.Key(oldRoot) == NativeBranchObservationChecks.Key(first[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
            "Missing independent8-input inherent Spike cast/root.");
        for (int i = 0; i < first.Length; i++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[i].GetProperty(field).ContentEquals(second[i].GetProperty(field)), "Independent inherent input differs.");
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                Require(NativeBranchObservationChecks.Key(first[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[i].GetProperty(field).Deserialize<BattleTurnState>()!), "Independent inherent state/forecast/RNG differs.");
        }
        var before = first[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = first[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = first[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(24)!;
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        Require(before.Spawn.Turn == 1 && before.Energy == 5 && after.Energy == 0 && action.CardInstanceId == 24 &&
            CardJuiceModel.Validate(card) == null && card.Permanent.Upgrades.Select(upgrade => upgrade.AssetKey)
                .SequenceEqual(new[] { "TraitJuice", "TraitInherentSpellReduceCost" }) &&
            card.Permanent.Upgrades[1].Stats.Cost == -1 &&
            CardModifierModel.UpgradedStat(0, "Cost", true, card.Permanent, card.Temporary) == 0 &&
            CardModifierModel.UpgradedStat(0, "XCost", true, card.Permanent, card.Temporary) == 1 &&
            Target(before).Status("valor")!.Stacks == 8 && Target(after).Status("valor")!.Stacks == 56 &&
            CardJuiceModel.Validate(before.Spawn.Train.Context.FindCard(17)!) == null &&
            CardJuiceModel.Validate(before.Spawn.Train.Context.FindCard(26)!) == null,
            "Native5 energy +1 X ->24 scaled ->48 doubled Valor or concurrent original Spikes differ.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, action);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(after),
                "Detached inherent Spike cast differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Inherent Spike changed parent.");
        foreach (string control in new[] { "Id", "Asset", "Cost", "XCost", "Damage", "Order", "Multiplier", "Temporary" })
        {
            var changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            var upgrade = changed["Permanent"]!["Upgrades"]![1]!;
            if (control == "Id") upgrade["DataId"] = "unknown-inherent";
            if (control == "Asset") upgrade["AssetKey"] = "OtherInherent";
            if (control == "Cost") upgrade["Stats"]!["Cost"] = 0;
            if (control == "XCost") upgrade["Stats"]!["XCost"] = 2;
            if (control == "Damage") upgrade["Stats"]!["Damage"] = 1;
            if (control == "Order")
            {
                var traits = changed["MaskDescriptor"]!["BaseTraits"]!.AsArray();
                var prior = traits[2]!.DeepClone(); traits[2] = traits[3]!.DeepClone(); traits[3] = prior;
            }
            if (control == "Multiplier") changed["MaskDescriptor"]!["BaseTraits"]![2]!["ParamInt"] = 3;
            if (control == "Temporary") changed["Temporary"]!["Upgrades"]!.AsArray().Add(upgrade.DeepClone());
            Require(CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown inherent Spike signature accepted: " + control);
        }
        Console.WriteLine("INHERENT-JUICE-SPIKE PASS:original refusal/eight independent native inputs/full state/forecast/RNG;5 energy +1 X ->24 scaled ->48 doubled Valor; three distinct Spike owners/four immutable branches/eight guards.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
