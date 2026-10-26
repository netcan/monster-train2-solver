using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopRetiredSourceChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, TroopPreviewAdditionChecks.Reason);
        Require(rows.Length == 28 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing twenty-eight-input native EndTurn prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var world = actual.Spawn.Train.Context!.Enchantments!;
        var retired = world.RetainedUnits.Single(actor => actor.Unit.Id == 16);
        Require(before.Spawn.Turn == 4 && actual.Spawn.Turn == 5 && world.EnchanterIds.SequenceEqual([13, 16, 28]) &&
            retired.RoomIndex == -1 && !retired.Preview && retired.Unit.Health == 0 && retired.Unit.DeathState?.IsDestroyed == true &&
            retired.Unit.Triggers.SelectMany(trigger => trigger.Effects).Single(effect => effect.Enchantment != null).Enchantment!.Bound == false &&
            actual.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == 28 && unit.AssetKey == "EnchanterT1_Speed"),
            "Lost retired Haste source, its registry position or the next wave's active owner.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Units").ContentEquals(update.GetProperty("After").GetProperty("Units"))).ToArray();
        Require(writes.Length == 2, "Missing both native uncached troop writes.");
        for (int index = 0; index < writes.Length; index++)
        {
            var left = writes[index].GetProperty("Before"); var right = writes[index].GetProperty("After");
            var initial = left.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 29);
            var final = right.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 29);
            Require(left.GetProperty("Preview").GetBoolean() && right.GetProperty("Preview").GetBoolean() &&
                left.GetProperty("Room").GetInt32() == 1 && left.GetProperty("Cache").GetArrayLength() == 0 &&
                right.GetProperty("Cache").GetArrayLength() == 0 && initial.GetProperty("Attack").GetInt32() == 3 + 2 * index &&
                final.GetProperty("Attack").GetInt32() == 5 + 2 * index && initial.GetProperty("Trample").GetInt32() == index &&
                final.GetProperty("Trample").GetInt32() == index + 1 && final.GetProperty("Horde").GetInt32() == 1,
                "Retired source changed native upgrade/status ordering or preview cache ownership.");
        }
        var birth = world.RetainedUnits.Single(actor => actor.Unit.Id == 29).Unit;
        Require(birth.Attack == 3 && birth.Statuses.Count == 0 && birth.Modifiers!.Upgrades.Count == 0 &&
            birth.DeathState?.IsDestroyed == true && actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 6)
                .Modifiers!.TroopAura!.Targets.Count == 0, "Temporary troop birth leaked into primary combat.");
        Controls(actual, birth);
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Owned native source turnover prefix differs.");
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Native source turnover changed its parent, state or RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TROOP-RETIRED-SOURCE PASS:28 native inputs/continuous state/RNG, destroyed unbound Haste source retained, " +
            "two native preview writes and empty primary cache, retirement controls and four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Controls(BattleTurnState actual, CombatUnit retiredBirth)
    {
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var birthNode = JsonSerializer.SerializeToNode(retiredBirth, ModelJson.Options)!;
        birthNode["DeathState"]!["IsDestroyed"] = false;
        birthNode["Statuses"] = JsonSerializer.SerializeToNode(new[] { context.StatusRules.Single(status => status.Id == "horde").WithStacks(1) }, ModelJson.Options);
        var birth = birthNode.Deserialize<CombatUnit>(ModelJson.Options)!;
        foreach (string control in new[] { "Observed", "StillBound", "Alive", "UnknownRetiredSource", "MissingRetiredSource", "PrimaryWrite", "OverlappingHaste" })
        {
            var retired = frame.RetainedUnits.Where(actor => actor.Unit.Id != birth.Id &&
                (control != "MissingRetiredSource" || actor.Unit.Id != 16)).Select(actor =>
                {
                    if (actor.Unit.Id != 16) return actor;
                    var node = JsonSerializer.SerializeToNode(actor, ModelJson.Options)!;
                    if (control == "StillBound") node["Unit"]!["Triggers"]![0]!["Effects"]![0]!["Enchantment"]!["Bound"] = true;
                    if (control == "Alive") node["Unit"]!["Health"] = 1;
                    if (control == "UnknownRetiredSource") node["Unit"]!["AssetKey"] = "unknown";
                    return node.Deserialize<EnchantmentRetainedUnit>(ModelJson.Options)!;
                }).ToArray();
            var rooms = frame.Train.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Where(unit => control != "OverlappingHaste" || unit.Id != 28)
                    .Concat(room.RoomIndex == 1 ? new[] { birth }.Concat(control == "OverlappingHaste" ?
                        frame.Train.Rooms[0].Units.Where(unit => unit.Id == 28) : []) : []).ToArray(),
                room.ExternalInteractions, frame.Train.Context, preview: control != "PrimaryWrite", room.ActiveCompanionBossIds)).ToArray();
            var changed = new EnchantmentCombatState(new TrainCombatState(rooms, frame.Train.Movement, frame.Train.EnemySlotsPerRoom, frame.Train.Context),
                retired, frame.EnchanterIds, frame.AllowUpdates, false, control != "PrimaryWrite", frame.TestRng);
            var result = TroopAuraModel.UpdateAll(changed, [6]);
            if (control == "Observed") Require(result.Supported && result.State!.Train.Rooms[1].Units.Single(unit => unit.Id == birth.Id).Attack == 5,
                "Observed retired-source preview did not apply exactly one upgrade.");
            else Require(!result.Supported && result.UnsupportedReason == TroopEnchantmentNoOpChecks.Reason,
                "Uncalibrated retired-source write escaped the ordering guard: " + control);
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
