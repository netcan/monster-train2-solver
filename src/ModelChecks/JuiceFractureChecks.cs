using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JuiceFractureChecks
{
    private const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Pair(FixtureValue refused, FixtureValue source, FixtureValue replay)
    {
        var old = NativeBranchObservationChecks.Replay(refused, Reason);
        var first = NativeBranchObservationChecks.Replay(source, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        Require(old.Length == 1 && old[0].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            NativeBranchObservationChecks.Key(old[0].GetProperty("Before").Deserialize<BattleTurnState>()!) ==
                NativeBranchObservationChecks.Key(first[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
            "Original deployment refusal or native root was replaced.");
        Require(first.Length == 15 && second.Length == 15 &&
            source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent Fracture request changed.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent Fracture input differs.");
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                Require(NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent Fracture state/RNG differs at " + index + ": " + field);
        }
        foreach (var rows in new[] { first, second })
        {
            var last = rows[^1];
            var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = before.Spawn.Train.Context!;
            var card = context.FindCard(12)!;
            var target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 9);
            var changed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 9);
            Require(before.Spawn.Turn == 3 && action.CardInstanceId == 12 && action.RoomIndex == 1 && action.TargetUnitId == 9 &&
                target.AssetKey == "HeavyT2_Zealot" && target.Team == CombatTeam.Enemy &&
                card.DataId == "b197fdec-a77d-49f5-bc05-6233b7ce01c7" && CardJuiceModel.Validate(card) == null &&
                before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId).Effects.Single().Statuses.Single().Stacks == 2 &&
                target.Status("corrupt_poison")?.Stacks == 2 && changed.Status("corrupt_poison")?.Stacks == 6 &&
                changed.Health == target.Health && target.Health == 7 &&
                context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 1 &&
                after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 2 &&
                after.Spawn.Train.Context.Statistics!.Value(12, "AnyStatusEffectStacksAdded") ==
                    context.Statistics!.Value(12, "AnyStatusEffectStacksAdded") + 4,
                "Missing native Fracture Reap2->4,existing2->6,Corruption1->2 or source attribution.");
            foreach (string control in new[] { "owner", "ordered", "multiplier", "definition", "stack", "corrupt", "order", "upgrade" })
            {
                var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
                var traits = json["MaskDescriptor"]!["BaseTraits"]!;
                if (control == "owner") json["DataId"] = "uncalibrated-juice-owner";
                else if (control == "ordered") json["StatusScalingTraits"] = JsonSerializer.SerializeToNode(new[] {
                    new ScalingStatusTrait(new("Gold"), 1, false, 0, ["corrupt_poison"]) }, ModelJson.Options);
                else if (control == "multiplier") traits[1]!["ParamInt"] = 3;
                else if (control == "definition") traits[1]!["DefinitionParamInt"] = 3;
                else if (control == "stack") traits[1]!["StackMode"] = 1;
                else if (control == "corrupt") traits[0]!["ParamInt"] = 1;
                else if (control == "order")
                {
                    var corrupt = traits[0]!.DeepClone(); var juice = traits[1]!.DeepClone();
                    traits[0] = juice; traits[1] = corrupt;
                }
                else json["Permanent"]!["Upgrades"]![1]!["DataId"] = "unobserved-juice-upgrade";
                Require(CardJuiceModel.Validate(json.Deserialize<CardInstanceState>(ModelJson.Options)!) == Reason,
                    "Uncalibrated Juice/Corrupt signature bypassed its guard: " + control);
            }
        }
        Console.WriteLine("JUICE-FRACTURE PASS: original deployment refusal;15 independent paired native inputs/full state/RNG; Reap2->4 and existing2->6; Corruption1->2; attribution;8 signature guards.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
