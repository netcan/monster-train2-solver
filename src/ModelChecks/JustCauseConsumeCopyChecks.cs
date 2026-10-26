using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class JustCauseConsumeCopyChecks
{
    private const string Reason = "Monster spell replication requires an owned, unupgraded card with captured mask metadata.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 18, "Incomplete consumed JustCause native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var prior = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        var card = after.FindCard(17)!;
        Require(action.CardInstanceId == 17 && action.RoomIndex == 0 && action.TargetUnitId == 2 &&
            prior.FindCard(17)!.Permanent.Upgrades.Count == 3 && CardExhaustModel.HasConsume(card) &&
            (card.MaskDescriptor!.HoldoverUpgradePending == true ||
                card.MaskDescriptor.BaseTraits.Any(trait => trait.RuntimeType == "CardTraitRetain" && trait.HoldoverPending == true)) &&
            card.PlayCount == prior.FindCard(17)!.PlayCount + 1 && after.NextCardId == prior.NextCardId &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count(token => token.InstanceId == 17) == 1,
            "Lost original triple upgrade, single Consume/play, Holdover or private-copy identity.");
        Require(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).Select(unit => unit.Id).SequenceEqual(new[] { 3, 2, 8 }) &&
            actual.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).Select(unit => unit.Id).SequenceEqual(new[] { 2, 3, 8 }),
            "The copied JustCause rearranged teammates after moving its original target.");
        foreach (int id in new[] { 2, 3, 8 })
            Require((actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id).Status("valor")?.Stacks ?? 0) -
                (before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id).Status("valor")?.Stacks ?? 0) == 2,
                "A teammate lost its native Valor2.");
        var condition = after.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        Require(condition.Triggered && condition.DurationTriggerCount == 1, "Copied JustCause lost its native once-per-turn count.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == immutable, "Consumed JustCause copy changed native state/RNG or parent.");
        });
        foreach (string field in new[] { "AssetKey", "Cost", "XCost" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var upgrade = node["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
                .Single(item => item!["InstanceId"]!.GetValue<int>() == 17)!["Permanent"]!["Upgrades"]!.AsArray()
                .Single(item => item!["AssetKey"]!.GetValue<string>() == "ReduceCostAddConsume")!;
            if (field == "AssetKey") upgrade[field] = "ChangedConsumeUpgrade";
            else upgrade["Stats"]![field] = field == "Cost" ? -1 : 1;
            var control = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
            string parent = JsonSerializer.Serialize(control, ModelJson.Options);
            Require(!BattleActionModel.PlayCard(control, action).Supported && JsonSerializer.Serialize(control, ModelJson.Options) == parent,
                "Uncalibrated consumed JustCause upgrade was accepted or mutated its parent.");
        }
        Console.WriteLine("JUSTCAUSE-CONSUME-COPY PASS:18 native inputs/full state/RNG, original-only movement/Consume/Holdover, three Valor targets, one relic count and four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
