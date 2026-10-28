using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopCacheOnlyChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, TroopEnchantmentNoOpChecks.Reason);
        Require(rows.Length == 29 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native deployment chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var owner = frame.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2);
        var target = frame.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 34);
        var rule = owner.Modifiers!.TroopAura!;
        Require(action.CardInstanceId == 8 && action.RoomIndex == 2 && target.AssetKey == "MonsterSycophant" &&
            target.Status("horde") == null && rule.Statuses.Count == 0 && rule.Targets.SequenceEqual([2, 34]),
            "Missing original cache-only addition.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().ToArray();
        var first = writes[0];
        Require(!first.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            first.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2]) &&
            first.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 34]) &&
            first.GetProperty("Before").GetProperty("Units").ContentEquals(first.GetProperty("After").GetProperty("Units")),
            "Native cache addition changed the observed units.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Cache-only deployment differs from native: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Deployment changed its parent.");

        foreach (string control in new[] { "CacheOnly", "Horde", "Status", "Departure" })
        {
            var changedRule = new TroopAuraState(rule.Subtype, rule.Upgrade,
                control == "Status" ? [new CombatStatus("armor", 1)] : rule.Statuses,
                control == "Departure" ? [2, 34] : [2]);
            var changedOwner = owner.WithModifiers(owner.Modifiers.WithTroopAura(changedRule));
            var changedTarget = target;
            if (control == "Horde")
            {
                JsonNode node = JsonSerializer.SerializeToNode(target, ModelJson.Options)!;
                node["Statuses"]!.AsArray().Add(JsonSerializer.SerializeToNode(new CombatStatus("horde", 1), ModelJson.Options));
                changedTarget = node.Deserialize<CombatUnit>(ModelJson.Options)!;
            }
            var train = new TrainCombatState(frame.Train.Rooms.Select(room => room.WithUnits(room.Units
                .Where(unit => control != "Departure" || unit.Id != target.Id)
                .Select(unit => unit.Id == owner.Id ? changedOwner : unit.Id == target.Id ? changedTarget : unit)
                .Concat(control == "Departure" && room.RoomIndex == 0 ? [changedTarget] : Array.Empty<CombatUnit>()).ToArray())).ToArray(),
                frame.Train.Movement, frame.Train.EnemySlotsPerRoom, frame.Train.Context);
            var changed = new EnchantmentCombatState(train, frame.RetainedUnits, frame.EnchanterIds, frame.AllowUpdates,
                frame.Updating, frame.Preview, frame.TestRng, frame.TemporaryPreviewUnitIds);
            var result = TroopAuraModel.UpdateAll(changed, [owner.Id]);
            if (control == "CacheOnly")
                Require(result.Supported && result.State!.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Id == owner.Id).Modifiers!.TroopAura!.Targets.SequenceEqual([2, 34]),
                    "A no-write cache addition was rejected.");
            else Require(!result.Supported && result.UnsupportedReason == TroopEnchantmentNoOpChecks.Reason,
                "A real write or live departure escaped the ordering guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("TROOP-CACHE-ONLY PASS: 29 native inputs/full state/RNG, no-write cache addition, four immutable branches and status/horde/departure guards; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
