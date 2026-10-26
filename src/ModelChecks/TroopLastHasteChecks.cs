using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopLastHasteChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, TroopPreviewAdditionChecks.Reason);
        Require(rows.Length == 22 && !rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            rows[^1].GetProperty("Action").Deserialize<PlayCardAction>() is { CardInstanceId: 15, RoomIndex: 0, TargetUnitId: 16 },
            "Missing twenty-two-input last Haste death prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var world = actual.Spawn.Train.Context!.Enchantments!;
        var retired = world.RetainedUnits.Single(actor => actor.Unit.Id == 16);
        Require(before.Spawn.Turn == 3 && actual.Spawn.Turn == 3 && world.EnchanterIds.SequenceEqual([13, 16]) &&
            retired.RoomIndex == -1 && !retired.Preview && retired.Unit.Health == 0 && retired.Unit.DeathState?.IsDestroyed == true &&
            retired.Unit.Triggers.SelectMany(trigger => trigger.Effects).Single(effect => effect.Enchantment != null).Enchantment!.Bound &&
            !actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.AssetKey == "EnchanterT1_Speed") &&
            actual.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit => unit.Status("haste") == null),
            "Lost destroyed but still-bound last Haste source, registry identity or its real status removal.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Units").ContentEquals(update.GetProperty("After").GetProperty("Units"))).ToArray();
        Require(writes.Length == 2, "Missing both native preview troop writes after the last Haste death.");
        for (int index = 0; index < writes.Length; index++)
        {
            var left = writes[index].GetProperty("Before"); var right = writes[index].GetProperty("After");
            var first = left.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 21);
            var last = right.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 21);
            Require(left.GetProperty("Preview").GetBoolean() && right.GetProperty("Preview").GetBoolean() &&
                left.GetProperty("Room").GetInt32() == 1 && left.GetProperty("Cache").GetArrayLength() == 0 &&
                right.GetProperty("Cache").GetArrayLength() == 0 && first.GetProperty("Attack").GetInt32() == 3 + index * 2 &&
                last.GetProperty("Attack").GetInt32() == 5 + index * 2 && first.GetProperty("Trample").GetInt32() == index &&
                last.GetProperty("Trample").GetInt32() == index + 1, "Last Haste death changed native preview writes or cache ownership.");
        }
        var birth = world.RetainedUnits.Single(actor => actor.Unit.Id == 21).Unit;
        Require(birth.Attack == 3 && birth.Statuses.Count == 0 && birth.Modifiers!.Upgrades.Count == 0 && birth.DeathState?.IsDestroyed == true &&
            actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 6).Modifiers!.TroopAura!.Targets.Count == 0,
            "Last Haste death leaked a temporary troop or its cache into primary combat.");
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Owned last Haste death prefix differs.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Last Haste death changed its parent, state or RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TROOP-LAST-HASTE PASS:22 native inputs/continuous state/RNG, destroyed still-bound Haste retained, " +
            "real enemy statuses removed, two troop preview writes, restored birth/cache and four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
