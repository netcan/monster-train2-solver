using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RankThreeRoomSlayChecks
{
    internal const string Reason = "Unsupported triggered all-status adjustment definition.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 1 && !rows[0].GetProperty("IsEndTurn").GetBoolean(), "Missing original rank-three deployment.");
        var before = rows[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var champion = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.SpawnerCardId == 9);
        var effect = champion.Triggers.Single(trigger => trigger.Kind == "OnKill").Effects.Single();
        Require(action.CardInstanceId == 9 && action.RoomIndex == 1 && champion is { AssetKey: "MonsterChampionUnderlegion1", Attack: 60, Health: 25 } &&
            effect.Type == "CardEffectAdjustAllStatusEffects" && effect.Action is { Type: "AdjustAllStatusEffects", Target: "Room", Value: 2,
                AllowPlayer: true, AllowEnemy: false, StatusAdjustmentFilter: 1 } && effect.Action.Statuses.Count == 0 &&
            effect.Action.Tests?.StrictTargets == true, "Lost natural rank-three +2 propagatable-status slay payload.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var played = BattleActionModel.PlayCard(before, action);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(actual),
                "Rank-three deployment differs from complete native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Deployment mutated its parent.");
        var floor = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1);
        foreach (string control in new[] { "Amount", "Zero", "Filter", "Target", "Enemy", "Tests" })
        {
            var node = JsonSerializer.SerializeToNode(champion, ModelJson.Options)!;
            var adjustment = node["Triggers"]![0]!["Effects"]![0]!["Action"]!;
            if (control == "Amount") adjustment["Value"] = 3;
            if (control == "Zero") adjustment["Value"] = 0;
            if (control == "Filter") adjustment["StatusAdjustmentFilter"] = 2;
            if (control == "Target") adjustment["Target"] = "Tower";
            if (control == "Enemy") adjustment["AllowEnemy"] = true;
            if (control == "Tests") adjustment["Tests"]!["StrictTargets"] = false;
            var altered = node.Deserialize<CombatUnit>(ModelJson.Options)!;
            var room = new RoomCombatState(floor.RoomIndex, floor.Deployment,
                floor.Units.Select(unit => unit.Id == champion.Id ? altered : unit).ToArray(), floor.ExternalInteractions,
                floor.Context, floor.Preview, floor.ActiveCompanionBossIds);
            string? rejection = RoomCombatModel.Validate(room);
            Require(rejection == Reason, "Unknown room slay definition admitted: " + control + ": " + rejection);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("RANK-THREE-ROOM-SLAY PASS: natural60ATK/25HP champion deployment, +2 room propagatable-status trigger, complete successor/RNG, four immutable branches and six guards; execution requires native combat evidence; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
