using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopDeadCacheBirthChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, TroopPreviewAdditionChecks.Reason);
        Require(rows.Length == 29 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing twenty-nine-input native EndTurn prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var room = actual.Spawn.Train.Rooms[1];
        Require(before.Spawn.Turn == 4 && actual.Spawn.Turn == 5 &&
            room.Units.Single(unit => unit.Id == 6).Modifiers!.TroopAura!.Targets.SequenceEqual([30]) &&
            room.Units.Single(unit => unit.Id == 30) is { AssetKey: "MonsterFunguy", Health: 12, Attack: 15 } &&
            room.Units.Single(unit => unit.Id == 30).Status("horde")?.Stacks == 3,
            "Preview overwrote the primary living troop or its cache.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Units").ContentEquals(update.GetProperty("After").GetProperty("Units"))).ToArray();
        Require(writes.Length == 2, "Missing two uncached native preview writes after the cached troop died.");
        for (int index = 0; index < writes.Length; index++)
        {
            var left = writes[index].GetProperty("Before"); var right = writes[index].GetProperty("After");
            var first = left.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 33);
            var last = right.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 33);
            Require(left.GetProperty("Preview").GetBoolean() && right.GetProperty("Preview").GetBoolean() &&
                left.GetProperty("Room").GetInt32() == 1 &&
                left.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([30]) &&
                right.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([30]) &&
                !left.GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 30) &&
                first.GetProperty("Attack").GetInt32() == 3 + 2 * index && last.GetProperty("Attack").GetInt32() == 5 + 2 * index &&
                first.GetProperty("Trample").GetInt32() == index && last.GetProperty("Trample").GetInt32() == index + 1,
                "Dead primary cache changed preview upgrade/status ordering or received a removal write.");
        }
        var birth = actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 33).Unit;
        Require(birth is { Attack: 3, Health: 4, DeathState.IsDestroyed: true } && birth.Statuses.Count == 0 &&
            birth.Modifiers!.Upgrades.Count == 0, "Temporary troop was not restored after preview.");
        Controls(actual, birth);
        var current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Owned dead-cache birth prefix differs.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Dead-cache birth changed its parent, state or RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TROOP-DEAD-CACHE-BIRTH PASS:29 native inputs/continuous state/RNG, unchanged primary cache, native3/5/7 " +
            "preview writes after cached troop death, restored birth, missing/live cache guards and four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Controls(BattleTurnState actual, CombatUnit retiredBirth)
    {
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var node = JsonSerializer.SerializeToNode(retiredBirth, ModelJson.Options)!;
        node["DeathState"]!["IsDestroyed"] = false;
        node["Statuses"] = JsonSerializer.SerializeToNode(new[] { context.StatusRules.Single(status => status.Id == "horde").WithStacks(1) }, ModelJson.Options);
        var birth = node.Deserialize<CombatUnit>(ModelJson.Options)!;
        var deadNode = JsonSerializer.SerializeToNode(frame.Train.Rooms[1].Units.Single(unit => unit.Id == 30), ModelJson.Options)!;
        deadNode["Health"] = 0; deadNode["DeathState"]!["IsDestroyed"] = true;
        var dead = deadNode.Deserialize<CombatUnit>(ModelJson.Options)!;
        foreach (string control in new[] { "Observed", "MissingCachedActor", "LivingCachedActor", "UndestroyedCachedActor" })
        {
            var retained = frame.RetainedUnits.Where(actor => actor.Unit.Id != birth.Id && actor.Unit.Id != 30).ToList();
            if (control != "MissingCachedActor")
            {
                var changed = JsonSerializer.SerializeToNode(dead, ModelJson.Options)!;
                if (control == "LivingCachedActor") changed["Health"] = 1;
                if (control == "UndestroyedCachedActor") changed["DeathState"]!["IsDestroyed"] = false;
                retained.Add(new EnchantmentRetainedUnit(changed.Deserialize<CombatUnit>(ModelJson.Options)!, -1, true));
            }
            var rooms = frame.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Where(unit => unit.Id != 30).Concat(room.RoomIndex == 1 ? new[] { birth } : []).ToArray(),
                room.ExternalInteractions, frame.Train.Context, preview: true, room.ActiveCompanionBossIds)).ToArray();
            var result = TroopAuraModel.UpdateAll(new EnchantmentCombatState(new TrainCombatState(rooms, frame.Train.Movement,
                frame.Train.EnemySlotsPerRoom, frame.Train.Context), retained, frame.EnchanterIds, frame.AllowUpdates, false, true, frame.TestRng), [6]);
            if (control == "Observed") Require(result.Supported && result.State!.Train.Rooms[1].Units.Single(unit => unit.Id == birth.Id).Attack == 5,
                "Observed dead-cache birth did not apply exactly one upgrade.");
            else Require(!result.Supported && result.UnsupportedReason == TroopEnchantmentNoOpChecks.Reason,
                "Unknown or living cached departure escaped its ordering guard: " + control);
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
