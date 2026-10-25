using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JuiceJustCauseChecks
{
    private const string Reason = "Juice with this card or ordered scaling traits requires native calibration.";
    internal static void Pair(FixtureValue refused, FixtureValue source, FixtureValue replay)
    {
        var original = NativeBranchObservationChecks.Replay(refused, Reason);
        var first = NativeBranchObservationChecks.Replay(source, Reason);
        var second = NativeBranchObservationChecks.Replay(replay, Reason);
        Require(original.Length == 1 && original[0].GetProperty("Expected").ValueKind == FixtureKind.Null &&
            NativeBranchObservationChecks.Key(original[0].GetProperty("Before").Deserialize<BattleTurnState>()!) ==
                NativeBranchObservationChecks.Key(first[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
            "Preserve the original deployment refusal and unchanged native root.");
        Require(first.Length == 11 && second.Length == 11 &&
            source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent JustCause request differs.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent JustCause input differs.");
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                Require(NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent JustCause state/RNG differs at " + index + ": " + field);
        }
        foreach (var rows in new[] { first, second })
        {
            var last = rows[^1];
            var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = before.Spawn.Train.Context!;
            var changedContext = after.Spawn.Train.Context!;
            var card = context.FindCard(6)!;
            var target = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 2);
            var changed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 2);
            Require(before.Spawn.Turn == 2 && action.CardInstanceId == 6 && action.RoomIndex == 1 && action.TargetUnitId == 2 &&
                card.DataId == "c8b39244-83c1-4eb3-b474-8898226d4280" && CardJuiceModel.Validate(card) == null &&
                before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId).Effects.Last().Statuses.Single().Stacks == 2 &&
                target.Team == CombatTeam.Player && target.Status("valor")?.Stacks == 5 && changed.Status("valor")?.Stacks == 9 &&
                changed.Health == target.Health && target.Health == 20 && changed.Attack == target.Attack + 4 &&
                context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 2 &&
                changedContext.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 3 &&
                before.Energy == 3 && after.Energy == 3 &&
                changedContext.Statistics!.Value(6, "AnyStatusEffectStacksAdded") == context.Statistics!.Value(6, "AnyStatusEffectStacksAdded") + 4 &&
                changedContext.Statistics.Value(6, "TimesExhausted") == context.Statistics.Value(6, "TimesExhausted") + 2 &&
                changedContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 6) &&
                changedContext.FindCard(6)!.ExhaustState!.BaseTraits.Single().IsExhausted &&
                !context.FindCard(6)!.ExhaustState!.BaseTraits.Single().IsExhausted,
                "Missing native JustCause Valor2->4/existing5->9/+4attack,Corruption2->3,free cost or Consume attribution.");
            foreach (string control in new[] { "owner", "scaling", "multiplier", "definition", "stack", "corrupt", "consume", "order", "upgrade", "upgrade-order" })
            {
                var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
                var traits = json["MaskDescriptor"]!["BaseTraits"]!;
                var upgrades = json["Permanent"]!["Upgrades"]!;
                if (control == "owner") json["DataId"] = "unobserved-juice-owner";
                else if (control == "scaling") json["StatusScalingTraits"] = JsonSerializer.SerializeToNode(new[] {
                    new ScalingStatusTrait(new("Gold"), 1, false, 0, ["valor"]) }, ModelJson.Options);
                else if (control == "multiplier") traits[2]!["ParamInt"] = 3;
                else if (control == "definition") traits[2]!["DefinitionParamInt"] = 3;
                else if (control == "stack") traits[2]!["StackMode"] = 1;
                else if (control == "corrupt") traits[0]!["ParamInt"] = 1;
                else if (control == "consume") traits[1]!["ParamInt"] = 1;
                else if (control == "order") Swap(traits, 1, 2);
                else if (control == "upgrade") upgrades[1]!["DataId"] = "unobserved-consume-upgrade";
                else Swap(upgrades, 0, 1);
                Require(CardJuiceModel.Validate(json.Deserialize<CardInstanceState>(ModelJson.Options)!) == Reason,
                    "Uncalibrated JustCause signature bypassed its guard: " + control);
            }
        }
        Console.WriteLine("JUICE-JUSTCAUSE PASS: original refusal;11 independent paired native inputs/full state/RNG; Valor2->4/existing5->9/+4attack; Corruption2->3; free cost/Consume/statistics;10 signature controls.");
    }
    private static void Swap(System.Text.Json.Nodes.JsonNode values, int first, int second)
    {
        var left = values[first]!.DeepClone(); var right = values[second]!.DeepClone();
        values[first] = right; values[second] = left;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
