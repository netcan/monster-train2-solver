using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class IntrinsicInfusionChecks
{
    private const string Reason = "Corrupt State's additional eligibility and pre-play ordering require native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 9, "Incomplete intrinsic consumed Infusion prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var prior = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        bool zeroCorruption = action.RoomIndex == 1;
        int[] ids = zeroCorruption ? [4, 5] : [3, 2, 8];
        Require(action.CardInstanceId == 23 && action.RoomIndex == (zeroCorruption ? 1 : 0) && action.TargetUnitId == ids[0] &&
            CardCorruptRestrictedModel.IntrinsicConsumedInfusion(prior.FindCard(23)!) &&
            prior.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Corruption == (zeroCorruption ? 0 : 2) &&
            after.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Corruption == (zeroCorruption ? 0 : 2) &&
            after.FindCard(23)!.PlayCount == prior.FindCard(23)!.PlayCount + 1 && after.NextCardId == prior.NextCardId &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count(token => token.InstanceId == 23) == 1,
            "Missing native intrinsic/Consume/corrupt signature, net-zero corruption or original-only play/identity.");
        foreach (int id in ids)
        {
            var old = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == id);
            var unit = actual.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == id);
            Require(unit.Health == old.Health + 15 && unit.MaxHealth == old.MaxHealth + 15,
                "Original target or teammate lost its native15 health upgrade.");
            if (zeroCorruption) Require((unit.Status("armor")?.Stacks ?? 0) - (old.Status("armor")?.Stacks ?? 0) == 3,
                "Zero-room eligibility lost the single pre-play Corrupt armor callback.");
        }
        var condition = after.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        Require(condition.Triggered && condition.DurationTriggerCount == 1, "Infusion lost its native once-per-turn count.");
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
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Infusion changed full native state/RNG or parent.");
        });
        foreach (string field in new[] { "AssetKey", "Cost", "Health" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            if (field == "Health") node["PlayRules"]!["Cards"]!.AsArray().Single(item => item!["AssetKey"]!.GetValue<string>() == "EchoInfusion")!
                ["Effects"]![0]!["Upgrade"]!["Stats"]![field] = 14;
            else
            {
                var upgrade = node["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
                    .Single(item => item!["InstanceId"]!.GetValue<int>() == 23)!["Permanent"]!["Upgrades"]![2]!;
                if (field == "AssetKey") upgrade[field] = "ChangedIntrinsicUpgrade";
                else upgrade["Stats"]![field] = 0;
            }
            var control = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
            string unchanged = JsonSerializer.Serialize(control, ModelJson.Options);
            Require(!BattleActionModel.PlayCard(control, action).Supported && JsonSerializer.Serialize(control, ModelJson.Options) == unchanged,
                "Uncalibrated Infusion was accepted or mutated its parent.");
        }
        Console.WriteLine("INTRINSIC-INFUSION PASS:9 native inputs/full state/RNG, corruption=" + (zeroCorruption ? 0 : 2) +
            ", original and teammate15 health, one pre-play corruption/Consume/relic count and four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
