using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalArmoredUnitRecallChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalArmoredUnitRecall" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module,
            "Expected the original 2.3.1 armored-unit recall.");
        var before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = root.GetProperty("After").Deserialize<BattleTurnState>()!;
        var redeployed = root.GetProperty("Redeployed").Deserialize<BattleTurnState>()!;
        var recall = root.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var replay = root.GetProperty("RedeployAction").Deserialize<PlayCardAction>()!;
        int survivor = root.GetProperty("Survivor").GetInt32();
        int floor = recall.RoomIndex;
        var prior = before.Spawn.Train.Context!;
        var next = after.Spawn.Train.Context!;
        var target = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units
            .Single(unit => unit.Id == recall.TargetUnitId);
        var newUnit = redeployed.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units
            .Single(unit => unit.SpawnerCardId == replay.CardInstanceId);
        Require(prior.FindCard(recall.CardInstanceId)?.DataId == "ee166f41-20e0-4bc5-b238-f4082fe72a17" &&
            target.SpawnerCardId == replay.CardInstanceId && target.Statuses is [{ Id: "armor", Stacks: 20 }] &&
            prior.SpawnPoints!.Group(floor, CombatTeam.Player)!.Occupants[0] == target.Id &&
            prior.SpawnPoints.Group(floor, CombatTeam.Player)!.Occupants[1] == survivor &&
            next.SpawnPoints!.Group(floor, CombatTeam.Player)!.Occupants[0] == survivor &&
            next.SpawnPoints.Units.Single(unit => unit.UnitId == target.Id).Current == null &&
            next.Cards.Hand[0].InstanceId == target.SpawnerCardId && before.Energy - after.Energy == 1 &&
            newUnit.Id != target.Id && newUnit.Status("armor") == null,
            "Expected the paid original recall, physical slot compaction and same card redeployed without the old Armor.");

        Replay(before, recall, after, "armored recall");
        Replay(after, replay, redeployed, "redeployment");
        Console.WriteLine("ORIGINAL-ARMORED-UNIT-RECALL PASS: 20 Armor, compacted living slot and same-card redeployment, both direct and candidate full states/RNG.");
    }

    private static void Replay(BattleTurnState root, PlayCardAction action, BattleTurnState actual, string label)
    {
        string parent = Json(root);
        var result = BattleActionModel.PlayCard(root, action);
        Require(result.Supported, $"Original {label} was rejected: {result.Reason}");
        string expected = Json(actual);
        string? difference = ModelJson.Difference(expected, Json(result.State));
        Require(difference == null, $"Original {label} state/RNG differs: {difference}");
        var frontier = BattleActionModel.EnumerateActions(root, action.CardInstanceId, includeAbilities: false);
        var candidate = frontier.SupportedCandidates.Single(item => Json(item.Action) == Json(action));
        Require(Json(candidate.Result.State) == expected && Json(root) == parent, $"Original {label} candidate or parent state differs.");
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
