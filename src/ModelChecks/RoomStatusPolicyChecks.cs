using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomStatusPolicyChecks
{
    internal static void Native(FixtureValue source)
    {
        var rows = NativeBranchObservationChecks.Replay(source,
            "Juice with this card or ordered scaling traits requires native calibration.");
        Require(rows.Length == 14, "Changed native room-status counterexample.");
        var native = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 10 && action.RoomIndex == 0 &&
            native.Spawn.Train.Rooms.Take(3).Select(room => room.Units.Count(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0))
                .SequenceEqual(new[] { 3, 2, 1 }), "Missing native three/two/one enemy room choice.");
        var json = JsonSerializer.SerializeToNode(native, ModelJson.Options)!;
        JsonArray hand = json["Spawn"]!["Train"]!["Context"]!["Cards"]!["Hand"]!.AsArray();
        for (int index = hand.Count - 1; index >= 0; index--)
            if (hand[index]!["InstanceId"]!.GetValue<int>() != 10) hand.RemoveAt(index);
        var state = json.Deserialize<BattleTurnState>(ModelJson.Options)!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(state);
        foreach (int preferred in Enumerable.Range(0, 3))
        {
            foreach (bool tactical in new[] { false, true })
            {
                var chosen = tactical ? BattleTacticalPolicy.Choose(state, out var omitted, preferred) :
                    BattleHeuristicPolicy.Choose(state, out omitted, preferred);
                Require(chosen?.Action.CardInstanceId == 10 && chosen.Action.RoomIndex == 0 && omitted.Count == 0,
                    "Pure room debuff must reach three enemies instead of following deployment preference.");
            }
        }
        var legalRooms = BattleActionModel.EnumerateActions(state, 10, includeAbilities: false)
            .SupportedActions.Select(candidate => candidate.RoomIndex).Distinct().ToArray();
        Require(keys.Key(state) == parent && Enumerable.Range(0, 3).All(legalRooms.Contains),
            "Room-status ranking mutated its parent or removed legal alternatives.");
        string dataId = native.Spawn.Train.Context!.FindCard(10)!.DataId;
        foreach (string control in new[] { "immune", "subtype", "front", "mixed", "positive", "targeted" })
        {
            JsonNode changed = json.DeepClone();
            JsonNode ruleNode = changed["PlayRules"]!["Cards"]!.AsArray().Single(rule => rule!["DataId"]!.GetValue<string>() == dataId)!;
            JsonNode effect = ruleNode["Effects"]![0]!;
            if (control == "immune")
                foreach (var unit in changed["Spawn"]!["Train"]!["Rooms"]![0]!["Units"]!.AsArray()
                    .Where(unit => unit!["Team"]!.GetValue<int>() == (int)CombatTeam.Enemy))
                    unit!["StatusImmunities"]!.AsArray().Add("spell weakness");
            else if (control == "subtype") effect["Filters"]!["Subtype"] = "unmatched-room-policy-control";
            else if (control == "front") effect["Target"] = "FrontInRoom";
            else if (control == "mixed") effect["AllowPlayer"] = true;
            else if (control == "positive") effect["Statuses"]![0]!["DisplayCategory"] = "Positive";
            else effect["Target"] = "DropTargetCharacter";
            var branch = changed.Deserialize<BattleTurnState>(ModelJson.Options)!;
            var rule = branch.PlayRules!.Cards.Single(rule => rule.DataId == dataId);
            string immutable = keys.Key(branch);
            double? value = BattleHeuristicPolicy.RoomNegativeStatusValue(branch, new PlayCardAction(10, 0), rule, 2);
            double? preferredValue = BattleHeuristicPolicy.RoomNegativeStatusValue(branch, new PlayCardAction(10, 2), rule, 2);
            if (control == "immune") Require(value < preferredValue, "Immune recipients must not inflate room-status rank.");
            else if (control is "subtype" or "front") Require(value < preferredValue, "Filtered/one-front ties must retain deployment preference.");
            else Require(value == null && preferredValue == null, "Mixed, positive or selected-target effects need their existing ranking.");
            Require(keys.Key(branch) == immutable, "Room-status ranking mutated its control.");
        }
        Console.WriteLine("ROOM-STATUS-POLICY PASS: native14-input/full-state/RNG counterexample; three recipients preferred over one for all deployment preferences; six immunity/filter/front/mixed/positive/targeted controls; parent and legal branches preserved.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
