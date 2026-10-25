using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FriendlyRoomDamagePolicyChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var row = rows[9];
        var root = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var original = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(rows.Length == 43 && !fixture.GetProperty("NativeWon").GetBoolean() &&
            root.Spawn.Turn == 1 && original.CardInstanceId == 19 && original.RoomIndex == 0 &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 2 && unit.Modifiers?.IsChampion == true) &&
            !actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 2 && unit.Health > 0),
            "Original native friendly-fire counterexample changed.");
        Check(root, preserveChampion: true);
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        var destructive = BattleActionModel.PlayCard(root, original);
        Require(destructive.Supported && keys.Key(destructive.State!) == keys.Key(actual) &&
            BattleActionModel.EnumerateActions(root, 19, includeAbilities: false).SupportedActions.Any(action => action.RoomIndex == 0) &&
            keys.Key(root) == parent, "Friendly-fire correction pruned a legal action or changed native state/RNG.");

        // Enemy-only damage must retain its useful occupied-floor preference.
        var node = JsonSerializer.SerializeToNode(root)!;
        string cardId = root.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == 19).DataId;
        var rule = node["PlayRules"]!["Cards"]!.AsArray().Single(item => item!["DataId"]!.GetValue<string>() == cardId)!;
        rule["Effects"]![0]!["AllowPlayer"] = false;
        Check(node.Deserialize<BattleTurnState>()!, preserveChampion: false);
        Console.WriteLine("FRIENDLY-ROOM-DAMAGE PASS: native43-input defeat retained; both seeds preserve champion, enemy-only control, legal destructive action and full-state/RNG replay.");
    }

    private static void Check(BattleTurnState root, bool preserveChampion)
    {
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        foreach (bool tactical in new[] { false, true })
        {
            var choice = tactical ? BattleTacticalPolicy.Choose(root, out _) : BattleHeuristicPolicy.Choose(root, out _);
            Require(choice?.Action.CardInstanceId == 19 &&
                (preserveChampion ? choice.Action.RoomIndex != 0 : choice.Action.RoomIndex == 0) &&
                choice.Result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 2 && unit.Health > 0),
                $"Room damage seed selected harmful friendly fire or skipped enemy-only damage: tactical={tactical}, preserve={preserveChampion}, room={choice?.Action.RoomIndex}.");
            var replay = BattleActionModel.PlayCard(root, choice!.Action);
            Require(replay.Supported && keys.Key(replay.State!) == keys.Key(choice.Result.State!) && keys.Key(root) == parent,
                "Room damage seed failed independent state/RNG replay or mutated its root.");
        }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
