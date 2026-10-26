using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChallengeRecastChecks
{
    private const string Reason = "Only a single fixed, Monster-targeted DropTarget damage effect without OnCast is modeled.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 28, "Incomplete consumed Challenge prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var prior = before.Spawn.Train.Context!;
        var after = actual.Spawn.Train.Context!;
        Require(before.Spawn.Turn == 4 && action.CardInstanceId == 15 && action.RoomIndex == 0 && action.TargetUnitId == 3 &&
            RelicMonsterSpellTargetingModel.ConsumedCorruptChallenge(prior.FindCard(15)!) &&
            prior.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 0 &&
            after.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 1 &&
            before.Energy == 5 && actual.Energy == 5 && prior.NextCardId == after.NextCardId &&
            after.FindCard(15)!.PlayCount == prior.FindCard(15)!.PlayCount + 1 &&
            after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count(token => token.InstanceId == 15) == 1 &&
            actual.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).Select(unit => unit.Id).SequenceEqual(new[] { 3, 2, 8 }),
            "Missing native zero-cost original Consume/corruption, play identity or unchanged private-copy positioning.");
        foreach (int id in new[] { 3, 2, 8 })
        {
            var old = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            var unit = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            Require((unit.Status("melee weakness")?.Stacks ?? 0) - (old.Status("melee weakness")?.Stacks ?? 0) == 1 &&
                unit.Health == old.Health && unit.Attack == old.Attack &&
                (unit.Status("armor")?.Stacks ?? 0) == (old.Status("armor")?.Stacks ?? 0),
                "Challenge lost a native weakness recipient or invented an extra copy callback.");
        }
        var condition = after.Relics!.Single(relic => relic.DataId == RelicMonsterSpellTargetingModel.RelicId)
            .MonsterSpellTargetingModifiers!.Single().Conditions.Single();
        Require(condition.Triggered && condition.DurationTriggerCount == 1, "Challenge lost its native once-per-turn count.");
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
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Challenge differs or mutates its parent.");
        });
        foreach (string control in new[] { "identity", "cost", "status" })
        {
            var json = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var card = json["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
                .Single(item => item!["InstanceId"]!.GetValue<int>() == 15)!;
            if (control == "identity") card["Permanent"]!["Upgrades"]![0]!["AssetKey"] = "UnknownCorrupt";
            else if (control == "cost") card["Permanent"]!["Upgrades"]![1]!["Stats"]!["Cost"] = -1;
            else json["PlayRules"]!["Cards"]!.AsArray().Single(item => item!["AssetKey"]!.GetValue<string>() == "Challenge")!
                ["Effects"]![1]!["Statuses"]![0]!["Stacks"] = 2;
            var changed = json.Deserialize<BattleTurnState>(ModelJson.Options)!;
            string immutable = JsonSerializer.Serialize(changed, ModelJson.Options);
            Require(!BattleActionModel.PlayCard(changed, action).Supported && JsonSerializer.Serialize(changed, ModelJson.Options) == immutable,
                "Uncalibrated Challenge passed or changed its parent: " + control);
        }
        Console.WriteLine("CHALLENGE-RECAST PASS:28 native inputs/full state/RNG, three weakness recipients, original-only corruption/Consume/play count, private-copy positioning and four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
