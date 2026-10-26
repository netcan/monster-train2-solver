using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FractureRecastChecks
{
    private const string Reason = "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 9, "Incomplete original Fracture recast chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var prior = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        var card = prior.FindCard(18)!;
        Require(action.CardInstanceId == 18 && action.RoomIndex == 0 && action.TargetUnitId == 6 &&
            card.DataId == "b197fdec-a77d-49f5-bc05-6233b7ce01c7" &&
            card.Permanent.Upgrades.Count == 1 && card.Permanent.Upgrades[0].AssetKey == "TraitCorrupt" &&
            after.NextCardId == prior.NextCardId && after.FindCard(18)!.PlayCount == card.PlayCount + 1 &&
            after.Cards.Discard.Count(token => token.InstanceId == 18) == 1 &&
            after.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption -
            prior.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1,
            "Missing native private copy, single original play/corruption or discard.");
        foreach (int id in new[] { 6, 7 })
        {
            var old = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            var unit = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            Require((unit.Status("corrupt_poison")?.Stacks ?? 0) - (old.Status("corrupt_poison")?.Stacks ?? 0) == 2 &&
                (unit.Status("armor")?.Stacks ?? 0) - (old.Status("armor")?.Stacks ?? 0) == 3,
                "Original target or teammate lost native poison/armor.");
        }
        var condition = prior.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        var recorded = after.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        Require(!condition.Triggered && condition.DurationTriggerCount == 0 && recorded.Triggered && recorded.DurationTriggerCount == 1,
            "Fracture lost its native once-per-turn relic count.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Fracture recast changed full state/RNG or parent.");
        });
        void Reject(Action<JsonNode> mutate)
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            mutate(node);
            var control = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
            string immutable = JsonSerializer.Serialize(control, ModelJson.Options);
            Require(!BattleActionModel.PlayCard(control, action).Supported && JsonSerializer.Serialize(control, ModelJson.Options) == immutable,
                "An uncalibrated Fracture copy was accepted or mutated its parent.");
        }
        JsonNode Card(JsonNode node) => node["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
            .Single(item => item!["InstanceId"]!.GetValue<int>() == 18)!;
        Reject(node => node["PlayRules"]!["Cards"]!.AsArray().Single(item => item!["AssetKey"]!.GetValue<string>() == "StarterFracture")!
            ["Effects"]![0]!["Statuses"]![0]!["Stacks"] = 3);
        Reject(node => Card(node)["Permanent"]!["Upgrades"]![0]!["AssetKey"] = "ChangedCorruptUpgrade");
        Reject(node => Card(node)["Permanent"]!["Upgrades"]![0]!["Stats"]!["Damage"] = 1);
        Console.WriteLine("FRACTURE-RECAST PASS:9 native inputs/full state/RNG, original target and teammate poison/armor, one corruption/discard/relic count and four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
