using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JuiceMagicChecks
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
        Require(first.Length == 14 && second.Length == 14 &&
            source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")), "Independent MagicJuice request changed.");
        for (int index = 0; index < first.Length; index++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                Require(first[index].GetProperty(field).ContentEquals(second[index].GetProperty(field)), "Independent MagicJuice input differs.");
            foreach (string field in new[] { "Before", "Expected", "Actual" })
                Require(first[index].GetProperty(field).ValueKind == FixtureKind.Object && second[index].GetProperty(field).ValueKind == FixtureKind.Object &&
                    NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent MagicJuice state/forecast/RNG differs at " + index + ": " + field);
        }
        foreach (var rows in new[] { first, second })
        {
            var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = before.Spawn.Train.Context!;
            var changedContext = after.Spawn.Train.Context!;
            var card = context.FindCard(10)!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            var room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
            var changed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
            var enemies = room.Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).ToArray();
            Require(before.Spawn.Turn == 2 && action.CardInstanceId == 10 && action.RoomIndex == 0 && action.TargetUnitId == 0 &&
                card.DataId == "9ea70171-8afd-4fab-ad54-7ff5c8b478b8" && rule.AssetKey == "MagicJuice" &&
                CardJuiceModel.Validate(card) == null && rule.CardType == "Spell" && rule.Cost == 0 &&
                rule.Effects.Single().Statuses.Single().Id == "spell weakness" && rule.Effects.Single().Statuses.Single().Stacks == 2 &&
                enemies.Select(unit => unit.Id).SequenceEqual(new[] { 12, 13, 14 }) &&
                enemies.All(unit => changed.Units.Single(item => item.Id == unit.Id).Status("spell weakness")?.Stacks ==
                    (unit.Status("spell weakness")?.Stacks ?? 0) + 4) &&
                enemies.All(unit => changed.Units.Single(item => item.Id == unit.Id).Health == unit.Health) &&
                before.Energy == after.Energy &&
                changedContext.Statistics!.Value(10, "AnyStatusEffectStacksAdded") == context.Statistics!.Value(10, "AnyStatusEffectStacksAdded") + 12 &&
                changedContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == 10) &&
                !changedContext.Cards.Hand.Any(token => token.InstanceId == 10) &&
                changedContext.FindCard(10)!.ExhaustState!.BaseTraits.Single().IsExhausted &&
                !card.ExhaustState!.BaseTraits.Single().IsExhausted,
                "Missing native MagicJuice2->4 on three enemies, zero cost, Consume or source attribution.");
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 8, _ =>
            {
                var result = BattleActionModel.PlayCard(before, action);
                Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                    "Parallel MagicJuice differs from native state/RNG.");
            });
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "MagicJuice mutated its parent.");
            foreach (string control in new[] { "owner", "scaling", "multiplier", "definition", "stack", "consume", "removable", "order", "upgrade", "missing-upgrade" })
            {
                var json = JsonSerializer.SerializeToNode(card, ModelJson.Options)!;
                var traits = json["MaskDescriptor"]!["BaseTraits"]!;
                if (control == "owner") json["DataId"] = "unobserved-juice-owner";
                else if (control == "scaling") json["StatusScalingTraits"] = JsonSerializer.SerializeToNode(new[] {
                    new ScalingStatusTrait(new("Gold"), 1, false, 0, ["spell weakness"]) }, ModelJson.Options);
                else if (control == "multiplier") traits[1]!["ParamInt"] = 3;
                else if (control == "definition") traits[1]!["DefinitionParamInt"] = 3;
                else if (control == "stack") traits[1]!["StackMode"] = 1;
                else if (control == "consume") traits[0]!["ParamInt"] = 1;
                else if (control == "removable") traits[1]!["Removable"] = false;
                else if (control == "order")
                {
                    var exhaust = traits[0]!.DeepClone(); var juice = traits[1]!.DeepClone();
                    traits[0] = juice; traits[1] = exhaust;
                }
                else if (control == "upgrade") json["Permanent"]!["Upgrades"]![0]!["DataId"] = "unobserved-juice-upgrade";
                else json["Permanent"]!["Upgrades"] = new System.Text.Json.Nodes.JsonArray();
                Require(CardJuiceModel.Validate(json.Deserialize<CardInstanceState>(ModelJson.Options)!) == Reason,
                    "Uncalibrated MagicJuice signature bypassed its guard: " + control);
            }
        }
        Console.WriteLine("JUICE-MAGIC PASS: original refusal;14 independent paired native inputs/full state/forecasts/RNG; spell weakness2->4 on three enemies; zero cost/Consume/attribution; eight immutable parallel branches and ten signature guards.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
