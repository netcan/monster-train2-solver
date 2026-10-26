using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class XRoomStatusPolicyChecks
{
    private const string Reason = "Observed X-cost room-status seed preferred empty room.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 9, "Missing native X room-status prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(29)!;
        Require(before.Energy == 5 && action.CardInstanceId == 29 && action.RoomIndex == 0 &&
            card.DataId == CardCopyOnPlayModel.WurmkinSpikeSpellId &&
            CardModifierModel.UpgradedStat(0, "XCost", true, card.Permanent, card.Temporary) == 1 &&
            before.Spawn.Train.Rooms[0].Units.Count(unit => unit.Team == CombatTeam.Enemy) == 3 &&
            !before.Spawn.Train.Rooms[2].Units.Any(unit => unit.Team == CombatTeam.Enemy) &&
            actual.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)
                .All(unit => unit.Status("corrupt_poison")?.Stacks == 36),
            "Missing native X upgrade, occupied/empty floors or prospective36 poison.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        string native = NativeBranchObservationChecks.Key(actual);
        foreach (int preferred in Enumerable.Range(0, 3))
        {
            var chosen = BattleTacticalPolicy.Choose(before, out _, preferred);
            Require(chosen?.Action.CardInstanceId == 29 && chosen.Action.RoomIndex == 0 &&
                NativeBranchObservationChecks.Key(chosen.Result.State!) == native,
                "Tactical X status still follows an empty deployment floor or changes the native successor.");
        }
        var json = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        var hand = json["Spawn"]!["Train"]!["Context"]!["Cards"]!["Hand"]!.AsArray();
        for (int i = hand.Count - 1; i >= 0; i--)
            if (hand[i]!["InstanceId"]!.GetValue<int>() != 29) hand.RemoveAt(i);
        var isolated = json.Deserialize<BattleTurnState>(ModelJson.Options)!;
        foreach (int preferred in Enumerable.Range(0, 3))
            Require(BattleHeuristicPolicy.Choose(isolated, out _, preferred)?.Action.RoomIndex == 0,
                "Strength seed lost the occupied floor for the isolated X card.");
        json["Energy"] = 0;
        json["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["Energy"] = 0;
        var zero = json.Deserialize<BattleTurnState>(ModelJson.Options)!;
        var rule = zero.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
        double? zeroRank = BattleHeuristicPolicy.RoomNegativeStatusValue(zero, new PlayCardAction(29, 0), rule, 2);
        var zeroChoice = BattleTacticalPolicy.Choose(zero, out var omitted, 2);
        Require(zeroRank > 17.99 && zeroChoice?.Action.RoomIndex == 0,
            "Positive intrinsic X bonus at zero energy must still reach its enemies: rank=" + zeroRank +
            ", chosen=" + zeroChoice?.Action.CardInstanceId + "/" + zeroChoice?.Action.RoomIndex +
            ", omitted=" + string.Join(";", omitted.Select(attempt => attempt.Reason).Distinct()));
        var noScaling = json.DeepClone();
        noScaling["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
            .Single(value => value!["InstanceId"]!.GetValue<int>() == 29)!["StatusScalingTraits"] = new JsonArray();
        var unscaled = noScaling.Deserialize<BattleTurnState>(ModelJson.Options)!;
        Require(BattleHeuristicPolicy.RoomNegativeStatusValue(unscaled, new PlayCardAction(29, 0), rule, 2) == null,
            "A static zero-status effect must not acquire invented stacks.");
        var legalRooms = BattleActionModel.EnumerateActions(before, 29, includeAbilities: false).SupportedActions
            .Select(input => input.RoomIndex).Distinct().ToArray();
        Require(Enumerable.Range(0, 3).All(legalRooms.Contains), "Ranking removed legal X spell alternatives.");
        Parallel.For(0, 4, index =>
        {
            var chosen = BattleTacticalPolicy.Choose(before, out _, 2);
            Require(chosen?.Action.RoomIndex == 0 && NativeBranchObservationChecks.Key(chosen.Result.State!) == native &&
                JsonSerializer.Serialize(before, ModelJson.Options) == immutable, "X room status changed its full state/RNG or parent.");
        });
        if (replay.HasValue)
        {
            var second = NativeBranchObservationChecks.Replay(replay.Value, Reason);
            Require(source.GetProperty("Request").ContentEquals(replay.Value.GetProperty("Request")) && second.Length == rows.Length,
                "Independent X room-status process changed its input request.");
            for (int index = 0; index < rows.Length; index++)
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(second[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent X room-status full state/RNG differs at " + index);
        }
        Console.WriteLine("X-ROOM-STATUS-POLICY PASS:9 native inputs/full state/RNG, prospective X payment/upgrade, occupied floor for both seeds, zero-energy/no-scaling controls, legal rooms and four immutable choices; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
