using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InfernoPlacementPolicyChecks
{
    internal static void Native(FixtureValue source)
    {
        var rows = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var row = rows.First(row => !row.GetProperty("IsEndTurn").GetBoolean() &&
            row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 21);
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var rule = before.PlayRules!.Cards.Single(card => card.DataId == before.Spawn.Train.Context!.FindCard(21)!.DataId);
        var championRoom = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Modifiers?.IsChampion == true));
        Require(rows.Length == 58 && rule.AssetKey == "InfernoRoom" && action.RoomIndex == championRoom.RoomIndex,
            "Missing original persistent friendly-fire counterexample.");
        using var keys = new BattleStateKeys();
        string immutable = keys.Key(before);
        var original = BattleActionModel.PlayCard(before, action);
        Require(original.Supported && keys.Key(original.State!) == keys.Key(actual), "Original attachment full native state/RNG changed.");
        var enumeration = BattleActionModel.EnumerateActions(before, 21, includeAbilities: false);
        var choices = enumeration.SupportedCandidates;
        Require(choices.Any(choice => choice.Action.RoomIndex == championRoom.RoomIndex) &&
            choices.Select(choice => choice.Action.RoomIndex).Distinct().Count() == 2 &&
            choices.Select(choice => choice.Action.RoomIndex).Concat(enumeration.UnsupportedAttempts.Select(attempt => attempt.Action.RoomIndex))
                .Distinct().Order().SequenceEqual([0, 1, 2]),
            "Inferno floors=" + string.Join(",", choices.Select(choice => choice.Action.RoomIndex)) +
            "; unsupported=" + string.Join("/", enumeration.UnsupportedAttempts.Select(attempt => attempt.Action.RoomIndex + ":" + attempt.Reason)));
        foreach (bool tactical in new[] { false, true })
        {
            BattleTurnState current = before;
            SupportedActionCandidate? choice = null;
            for (int step = 0; step < 18; step++)
            {
                choice = tactical ? BattleTacticalPolicy.Choose(current, out _) : BattleHeuristicPolicy.Choose(current, out _);
                if (choice == null || choice.Action.CardInstanceId == 21) break;
                var replayed = BattleActionModel.PlayCard(current, choice.Action);
                Require(replayed.Supported && keys.Key(replayed.State!) == keys.Key(choice.Result.State!),
                    "Preceding policy action failed successor/RNG replay.");
                current = replayed.State!;
            }
            Require(choice?.Action.CardInstanceId == 21 && choice.Action.RoomIndex != championRoom.RoomIndex,
                $"Persistent friendly fire still follows the main army: tactical={tactical}, card={choice?.Action.CardInstanceId}, room={choice?.Action.RoomIndex}.");
            var replay = BattleActionModel.PlayCard(current, choice!.Action);
            Require(replay.Supported && keys.Key(replay.State!) == keys.Key(choice.Result.State!) && keys.Key(before) == immutable,
                "Inferno seed changed its parent or failed complete successor/RNG replay.");
            var room = replay.State!.Spawn.Train.Rooms.Single(item => item.RoomIndex == championRoom.RoomIndex);
            var pre = RoomPreCombatModel.Apply(room, null);
            Require(pre.Supported && room.Units.Where(unit => unit.Modifiers?.IsChampion == true)
                .All(unit => pre.State!.Units.Single(after => after.Id == unit.Id).Health == unit.Health),
                "The selected Inferno floor still damages the champion before combat.");
        }
        Console.WriteLine("INFERNO-PLACEMENT-POLICY PASS: original58-input native defeat retained; both seeds move Inferno away from the champion; two supported floors and original guarded floor0 retained; complete native/alternative successor/RNG and pre-combat survival checked.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
