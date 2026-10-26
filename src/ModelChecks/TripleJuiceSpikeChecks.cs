using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleJuiceSpikeChecks
{
    private const string Reason = "Native triple Juice with paid-energy status scaling.";
    internal static void Native(FixtureValue refused, FixtureValue source, FixtureValue replay)
    {
        var refusal = refused.GetProperty("Rows").EnumerateArray().Single();
        Require(refusal.GetProperty("Expected").ValueKind == FixtureKind.Null &&
            refusal.GetProperty("UnsupportedReason").GetString() == "Upgrade traits", "Original deployment guard was lost.");
        var oldRoot = refusal.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var oldAction = refusal.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var released = BattleActionModel.PlayCard(oldRoot, oldAction);
        Require(released.Supported && NativeBranchObservationChecks.Key(released.State!) ==
            NativeBranchObservationChecks.Key(refusal.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Original native deployment changed.");
        var first = NativeBranchObservationChecks.Replay(source, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        Require(first.Length == 23 && second.Length == 23 && source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")),
            "Independent triple Juice prefix changed.");
        Require(NativeBranchObservationChecks.Key(oldRoot) == NativeBranchObservationChecks.Key(first[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
            "Natural deployment refusal and cast roots differ.");
        for (int i = 0; i < first.Length; i++)
        {
            foreach (string name in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[i].GetProperty(name).ContentEquals(second[i].GetProperty(name)), "Native triple Juice input differs.");
            foreach (string name in new[] { "Before", "Expected", "Actual" })
                Require(NativeBranchObservationChecks.Key(first[i].GetProperty(name).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[i].GetProperty(name).Deserialize<BattleTurnState>()!), "Native triple Juice state/RNG differs.");
        }
        foreach (var fixture in new[] { source, replay })
        {
            var samples = fixture.GetProperty("TripleJuiceCalibration").EnumerateArray().ToArray();
            Require(samples.Length == 6 && samples.Select(row => row.GetProperty("Input").GetInt32()).SequenceEqual(
                new[] { int.MinValue, -3, 0, 1, 4, int.MaxValue }), "Missing native triple callback boundaries.");
            foreach (var sample in samples)
                Require(sample.GetProperty("UpgradeId").GetString() == CardJuiceModel.TripleUpgrade &&
                    sample.GetProperty("ParamInt").GetInt32() == 3 && sample.GetProperty("Multiplier").GetInt32() == 3 &&
                    sample.GetProperty("Bonus").GetInt32() == unchecked(Math.Max(0, sample.GetProperty("Input").GetInt32()) * 2),
                    "Native triple Juice callback differs.");
        }
        var last = first[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(18)!;
        foreach (var sample in source.GetProperty("TripleJuiceCalibration").EnumerateArray())
        {
            int input = sample.GetProperty("Input").GetInt32(), bonus = sample.GetProperty("Bonus").GetInt32();
            Require(CardJuiceModel.Apply(card, input) == unchecked(input + bonus), "Signed/wrapped triple composition differs.");
        }
        CombatUnit Target(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
        Require(before.Spawn.Turn == 4 && before.Energy == 3 && after.Energy == 0 && action.CardInstanceId == 18 &&
            CardJuiceModel.Validate(card) == null && Target(after).Status("valor")!.Stacks == (Target(before).Status("valor")?.Stacks ?? 0) + 36,
            "Paid energy3 -> scaled12 -> triple36 Valor differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after), "Detached triple Juice cast differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Triple Juice mutated its parent.");
        foreach (string control in new[] { "Owner", "TraitOrder", "Multiplier", "Scaling", "Upgrade", "MissingMask" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
            if (control == "Owner") changed["DataId"] = "unknown-spike";
            if (control == "TraitOrder")
            {
                var traits = changed["MaskDescriptor"]!["BaseTraits"]!.AsArray();
                var firstTrait = traits[0]!.DeepClone(); traits[0] = traits[2]!.DeepClone(); traits[2] = firstTrait;
            }
            if (control == "Multiplier") changed["MaskDescriptor"]!["BaseTraits"]![2]!["ParamInt"] = 4;
            if (control == "Scaling") changed["StatusScalingTraits"]![0]!["StacksPerStat"] = 5;
            if (control == "Upgrade") changed["Permanent"]!["Upgrades"]![1]!["AssetKey"] = "OtherJuice3";
            if (control == "MissingMask") changed["MaskDescriptor"] = null;
            Require(CardJuiceModel.Validate(changed.Deserialize<CardInstanceState>(ModelJson.Options)!) != null,
                "Unknown triple signature accepted: " + control);
        }
        Console.WriteLine("TRIPLE-JUICE-SPIKE PASS: original deployment refusal;23 paired native inputs/full state/forecast/RNG; native callback limits;3 energy ->36 Valor; four immutable branches and six signature guards.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
