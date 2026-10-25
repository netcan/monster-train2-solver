using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JuiceTomeChecks
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
            "Preserve the original global deployment refusal and unchanged native root.");
        Require(first.Length == 15 && first.Length == second.Length &&
            source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent UnnamedTome request changed.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent UnnamedTome input differs.");
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                Require(first[index].GetProperty(field).ValueKind == FixtureKind.Object && second[index].GetProperty(field).ValueKind == FixtureKind.Object &&
                    NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent UnnamedTome state/forecast/RNG differs at " + index + ": " + field);
        }
        foreach (var rows in new[] { first, second })
        {
            var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = before.Spawn.Train.Context!;
            var changedContext = after.Spawn.Train.Context!;
            var card = context.FindCard(24)!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            var room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            var changed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            var enemies = room.Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).ToArray();
            Require(before.Spawn.Turn == 2 && action.CardInstanceId == 24 && action.RoomIndex == 0 && action.TargetUnitId == 0 &&
                card.DataId == "561f7a74-93a3-438f-9bfd-fbc84d979bdd" && rule.AssetKey == "UnnamedTome" &&
                CardJuiceModel.Validate(card) == null && rule.CardType == "Spell" && rule.Cost == 0 &&
                card.MaskDescriptor!.TemporaryTraits.Count == 1 &&
                card.MaskDescriptor.TemporaryTraits[0].RuntimeType == "CardTraitFreeze" &&
                rule.Effects.Single().Statuses.Single().Id == "muted" && rule.Effects.Single().Statuses.Single().Stacks == 3 &&
                enemies.Select(unit => unit.Id).SequenceEqual(new[] { 12, 13, 14, 15 }) &&
                enemies.All(unit => changed.Units.Single(item => item.Id == unit.Id).Status("muted")?.Stacks ==
                    (unit.Status("muted")?.Stacks ?? 0) + 6) &&
                enemies.All(unit => changed.Units.Single(item => item.Id == unit.Id).Health == unit.Health) &&
                enemies.Count(unit => unit.Status("spell shield")?.Stacks == 2) == 3 &&
                enemies.All(unit => changed.Units.Single(item => item.Id == unit.Id).Status("spell shield")?.Stacks ==
                    unit.Status("spell shield")?.Stacks) &&
                before.Energy == after.Energy &&
                changedContext.Statistics!.Value(24, "AnyStatusEffectStacksAdded") ==
                    context.Statistics!.Value(24, "AnyStatusEffectStacksAdded") + 6 * enemies.Length &&
                changedContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 24) &&
                !changedContext.Cards.Hand.Any(token => token.InstanceId == 24) &&
                changedContext.FindCard(24)!.ExhaustState!.BaseTraits.Single().IsExhausted &&
                !card.ExhaustState!.BaseTraits.Single().IsExhausted,
                "Missing native UnnamedTome3->6, zero cost, Consume or source attribution.");
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 8, _ =>
            {
                var result = BattleActionModel.PlayCard(before, action);
                Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                    "Parallel UnnamedTome differs from native state/RNG.");
            });
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "UnnamedTome mutated its parent.");
            foreach (string control in new[] { "owner", "scaling", "multiplier", "definition", "stack", "consume", "permafrost", "freeze", "removable", "order", "upgrade", "missing-upgrade" })
            {
                var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
                var traits = json["MaskDescriptor"]!["BaseTraits"]!;
                if (control == "owner") json["DataId"] = "unobserved-juice-owner";
                else if (control == "scaling") json["StatusScalingTraits"] = JsonSerializer.SerializeToNode(new[] {
                    new ScalingStatusTrait(new("Gold"), 1, false, 0, ["muted"]) }, ModelJson.Options);
                else if (control == "multiplier") traits[2]!["ParamInt"] = 3;
                else if (control == "definition") traits[2]!["DefinitionParamInt"] = 3;
                else if (control == "stack") traits[2]!["StackMode"] = 1;
                else if (control == "consume") traits[1]!["ParamInt"] = 1;
                else if (control == "permafrost") traits[0]!["ParamInt"] = 1;
                else if (control == "freeze") json["MaskDescriptor"]!["TemporaryTraits"]![0]!["ParamInt"] = 1;
                else if (control == "removable") traits[2]!["Removable"] = false;
                else if (control == "order")
                {
                    var frost = traits[0]!.DeepClone(); var exhaust = traits[1]!.DeepClone();
                    traits[0] = exhaust; traits[1] = frost;
                }
                else if (control == "upgrade") json["Permanent"]!["Upgrades"]![0]!["DataId"] = "unobserved-juice-upgrade";
                else json["Permanent"]!["Upgrades"] = new JsonArray();
                Require(CardJuiceModel.Validate(json.Deserialize<CardInstanceState>(ModelJson.Options)!) == Reason,
                    "Uncalibrated UnnamedTome signature bypassed its guard: " + control);
            }
        }
        Console.WriteLine($"JUICE-TOME PASS: original refusal;{first.Length} independent paired native inputs/full state/forecasts/RNG; muted3->6; Permafrost Freeze/zero cost/Consume/attribution; eight immutable parallel branches and twelve signature guards.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
