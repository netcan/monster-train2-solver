using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TriggeredCorruptionRelicChecks
{
    private const string Reason = "Battle preview floor 2: CorruptionAdded relic callbacks are not modeled.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 21 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native relic-corruption boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var relic = before.Spawn.Train.Context!.Relics!.Single(item => item.DataId == "f302d4a1-13ba-4907-a26d-f0d8eead6f2e");
        Require(before.Spawn.Turn == 5 && after.Spawn.Turn == 5 && relic.AssetKey == "DamageOnCorruptPlay" &&
            relic.EffectTypes.Count == 2 && relic.CorruptionStatuses is { Count: 2 } rules &&
            rules.Select(rule => rule.EffectIndex).SequenceEqual([0, 1]) && rules.All(rule =>
                rule.OnlyDuringMonsterTurn && !rule.CanApplyInPreview && rule.TargetMode == "RandomInRoom" &&
                rule.TargetHeroes && !rule.TargetMonsters && rule.Statuses.Single().Id == "corrupt_poison"),
            "Missing both ordered original corruption-poison callbacks and their native phase/preview gates.");
        Require(before.Spawn.Train.Context.BattleRng.Equals(after.Spawn.Train.Context!.BattleRng) &&
            before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 2).Corruption == 2 &&
            after.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 2).Corruption == 2,
            "Native spell/preview must retain the live RNG and room corruption.");
        foreach (bool preview in new[] { false, true })
        {
            var floor = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
            var room = new RoomCombatState(2, floor.Deployment, floor.Units, floor.ExternalInteractions,
                floor.Context, preview, floor.ActiveCompanionBossIds);
            var champion = room.Units.Single(unit => unit.AssetKey == "MonsterWurmChampion1");
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var fired = RoomCombatModel.ApplyQueuedCharacterTrigger(room, new(2, champion, "OnAttacking"), callbacks.Add);
            Require(fired.Supported && fired.State!.Context!.RoomCapacities!.Single(item => item.RoomIndex == 2).Corruption == 3 &&
                fired.State.Context.BattleRng.Equals(room.Context!.BattleRng) && callbacks.Any(item => item.Kind == "CorruptionAdded"),
                "Inactive relic statuses must retain queued Inspire, corruption commit and RNG in combat and preview.");
        }
        var json = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        foreach (var item in json["Spawn"]!["Train"]!["Context"]!["Relics"]!.AsArray())
            if (item!["DataId"]!.GetValue<string>() == relic.DataId)
                foreach (var rule in item["CorruptionStatuses"]!.AsArray())
                {
                    rule!["CanApplyInPreview"] = true;
                    rule["OnlyDuringMonsterTurn"] = false;
                }
        var active = json.Deserialize<BattleTurnState>(ModelJson.Options)!;
        var activeFloor = active.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
        // Rooms carry their own context; bind the changed train context explicitly.
        var activeRoom = new RoomCombatState(2, activeFloor.Deployment, activeFloor.Units, activeFloor.ExternalInteractions,
            active.Spawn.Train.Context, true, activeFloor.ActiveCompanionBossIds);
        var rejected = RoomCombatModel.ApplyQueuedCharacterTrigger(activeRoom,
            new(2, activeRoom.Units.Single(unit => unit.AssetKey == "MonsterWurmChampion1"), "OnAttacking"), _ => { });
        Require(!rejected.Supported && rejected.UnsupportedReason!.Contains("CorruptionAdded"),
            "Active relic callbacks bypassed their native calibration guard.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("TRIGGERED-CORRUPTION-RELIC PASS:21 native inputs/full state/RNG; two ordered poison callbacks inactive in Strike combat/preview; active callbacks remain guarded; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
