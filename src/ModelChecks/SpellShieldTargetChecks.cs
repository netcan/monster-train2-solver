using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SpellShieldTargetChecks
{
    internal static void Native(FixtureValue source, FixtureValue replay)
    {
        const string reason = "Observed spell shield seed target";
        FixtureValue[] first = NativeBranchObservationChecks.Replay(source, reason);
        FixtureValue[] second = NativeBranchObservationChecks.Replay(replay, reason);
        Require(first.Length == 8 && source.GetProperty("Request").ContentEquals(replay.GetProperty("Request")),
            "Changed native target investigation prefix.");
        for (int index = 0; index < first.Length; index++)
            foreach (string field in new[] { "Before", "Actual" })
                Require(NativeBranchObservationChecks.Key(first[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "Independent target observation differs.");
        BattleTurnState before = first[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = first[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = first[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 13 && action.TargetUnitId == 10 && action.RoomIndex == 0 &&
            before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 10).Health == 15 &&
            actual.Spawn.Train.Rooms.All(room => room.Units.All(unit => unit.Id != 10 || unit.Health == 0)),
            "Missing native unshielded fifteen-health kill.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        var blocked = BattleActionModel.PlayCard(before, new PlayCardAction(13, 0, targetUnitId: 8));
        Require(blocked.Supported && blocked.State!.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 8) is
            { Health: 1 } shielded && shielded.Status("spell shield") == null,
            "The old target must consume both shields while leaving its one health intact.");
        foreach (int floor in Enumerable.Range(0, 3))
        {
            var chosen = BattleTacticalPolicy.Choose(before, out var omitted, floor);
            Require(chosen?.Action.CardInstanceId == 13 && chosen.Action.TargetUnitId == 10 && omitted.Count == 0 &&
                keys.Key(chosen.Result.State!) == keys.Key(actual),
                "Tactical seed must prefer the native kill over an entirely shielded damage-only cast.");
        }
        Require(keys.Key(before) == parent && BattleActionModel.EnumerateActions(before, 13, includeAbilities: false)
            .SupportedActions.Any(candidate => candidate.TargetUnitId == 8),
            "Target ordering must preserve its parent and the legal shield-breaking branch.");
        Controls(before);
        Console.WriteLine("SPELL-SHIELD-TARGET PASS: eight native successors/RNG in two processes, fifteen-health kill preferred over two fully blocked hits; three deployment preferences, all-shielded fallback, mixed-effect control and legal branches preserved.");
    }

    private static void Controls(BattleTurnState root)
    {
        foreach (bool mixed in new[] { false, true })
        {
            JsonNode json = JsonSerializer.SerializeToNode(root, ModelJson.Options)!;
            JsonArray hand = json["Spawn"]!["Train"]!["Context"]!["Cards"]!["Hand"]!.AsArray();
            for (int index = hand.Count - 1; index >= 0; index--)
                if (hand[index]!["InstanceId"]!.GetValue<int>() != 13) hand.RemoveAt(index);
            string dataId = root.Spawn.Train.Context!.FindCard(13)!.DataId;
            JsonNode rule = json["PlayRules"]!["Cards"]!.AsArray().Single(card => card!["DataId"]!.GetValue<string>() == dataId)!;
            JsonArray effects = rule["Effects"]!.AsArray();
            if (mixed)
                effects.Add(JsonSerializer.SerializeToNode(root.PlayRules!.Cards.Single(card => card.AssetKey == "StarterFracture")
                    .Effects.First(effect => effect.Statuses.Count > 0), ModelJson.Options));
            else
                foreach (JsonNode? effect in effects)
                {
                    effect!["Filters"]!["Subtype"] = root.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 8).Subtypes.Single();
                    effect["Tests"]!["MustHaveValidTargets"] = true;
                    effect["Tests"]!["StrictTargets"] = true;
                }
            var state = json.Deserialize<BattleTurnState>(ModelJson.Options)!;
            using var keys = new BattleStateKeys();
            string parent = keys.Key(state);
            var chosen = BattleTacticalPolicy.Choose(state, out var omitted);
            Require(chosen?.Action.CardInstanceId == 13 && chosen.Action.TargetUnitId == 8 && omitted.Count == 0 &&
                keys.Key(state) == parent,
                (mixed ? "A useful mixed-effect cast must retain its target ranking." :
                    "If every legal target is shielded, retain a legal shield-breaking cast.") +
                    $" chosen={JsonSerializer.Serialize(chosen?.Action)} omitted={string.Join(";", omitted.Select(item => item.Reason))}");
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
