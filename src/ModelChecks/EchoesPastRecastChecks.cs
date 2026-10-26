using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoesPastRecastChecks
{
    private const string Reason = "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var prior = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        bool positive = action.RoomIndex == 1;
        int[] recipients = positive ? [4, 21, 5] : [3, 2, 8];
        Require(rows.Length == (positive ? 34 : 45) && before.Spawn.Turn == (positive ? 5 : 7) &&
            action.CardInstanceId == 25 && action.TargetUnitId == recipients[0] &&
            EchoesPastRecastModel.Known(prior.FindCard(25)!) &&
            prior.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Corruption == (positive ? 3 : 0) &&
            after.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Corruption == 0 &&
            actual.Energy == before.Energy - 1 && after.NextCardId == prior.NextCardId &&
            after.FindCard(25)!.PlayCount == prior.FindCard(25)!.PlayCount + 1 &&
            after.Cards.Discard.Count(card => card.InstanceId == 25) == 1,
            "Missing observed Echoes snapshot, single cost/discard/play identity or final corruption.");
        foreach (int id in recipients)
        {
            var old = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == id);
            var unit = actual.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == id);
            Require(unit.Attack - old.Attack == (positive ? 24 : 0) && unit.Health == old.Health &&
                JsonSerializer.Serialize(unit.Statuses, ModelJson.Options) == JsonSerializer.Serialize(old.Statuses, ModelJson.Options),
                "A recast lost the original corruption multiplier or fired a spurious corruption callback.");
        }
        var condition = after.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        Require(condition.Triggered && condition.DurationTriggerCount == 1, "Echoes counted its private recasts as new direct plays.");
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
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Echoes changed native state/RNG or its parent.");
        });
        foreach (string control in new[] { "identity", "scaling", "corruption" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var rule = node["PlayRules"]!["Cards"]!.AsArray().Single(item => item!["AssetKey"]!.GetValue<string>() == "EchoesOfThePast")!;
            if (control == "identity") rule["Effects"]![0]!["Upgrade"]!["AssetKey"] = "UncalibratedScalingUpgrade";
            else if (control == "corruption") rule["Effects"]![1]!["CorruptionAdd"] = true;
            else node["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
                .Single(item => item!["InstanceId"]!.GetValue<int>() == 25)!["UnitUpgradeScalingTraits"]![0]!["AmountPerStat"] = 7;
            var changed = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
            string immutable = JsonSerializer.Serialize(changed, ModelJson.Options);
            Require(!BattleActionModel.PlayCard(changed, action).Supported && JsonSerializer.Serialize(changed, ModelJson.Options) == immutable,
                "Uncalibrated Echoes passed or changed its parent: " + control);
        }
        if (positive) CorruptionScalingPolicyChecks.Native(source);
        Console.WriteLine($"ECHOES-PAST-RECAST PASS:{rows.Length} native inputs/full state/RNG, corruption={(positive ? 3 : 0)}, three attack recipients, original-only cost/discard/play count, restored private-copy scaling and four immutable branches; independentPair={replay.HasValue}");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
