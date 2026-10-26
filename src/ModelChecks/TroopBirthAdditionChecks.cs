using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopBirthAdditionChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, TroopEnchantmentNoOpChecks.Reason);
        Require(rows.Length == 18 && !rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            rows[^1].GetProperty("Action").Deserialize<PlayCardAction>() is { CardInstanceId: 21, RoomIndex: 1 },
            "Missing eighteen-input Eager Conscript birth prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var birth = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 17);
        Require(before.Spawn.Turn == 3 && actual.Spawn.Turn == 3 && birth.AssetKey == "MonsterFunguy" && birth.Attack == 5 &&
            birth.Status("horde")?.Stacks == 1 && birth.Status("trample")?.Stacks == 1 && birth.DeathState?.IsDestroyed == false &&
            birth.Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == "troop-aura:6") == 1 &&
            actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 6).Modifiers!.TroopAura!.Targets.SequenceEqual([17]) &&
            context.SpawnPoints!.Units.Any(point => point.UnitId == 17 && !point.SpawnedInPreview && point.Current?.RoomIndex == 1),
            "Real birth lost one upgrade/status, primary physical point or committed target cache.");
        var updates = rows[^1].GetProperty("TroopUpdates").EnumerateArray().ToArray();
        Require(updates.Length >= 3, "Missing primary and preview native callback witnesses.");
        foreach (var update in updates)
        {
            var left = update.GetProperty("Before"); var right = update.GetProperty("After");
            var first = left.GetProperty("Units").EnumerateArray().SingleOrDefault(unit => unit.GetProperty("Id").GetInt32() == 17);
            var last = right.GetProperty("Units").EnumerateArray().SingleOrDefault(unit => unit.GetProperty("Id").GetInt32() == 17);
            bool addition = left.GetProperty("Cache").GetArrayLength() == 0;
            Require(right.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([17]) &&
                (!addition || !left.GetProperty("Preview").GetBoolean() && first.GetProperty("Attack").GetInt32() == 3 &&
                    last.GetProperty("Attack").GetInt32() == 5 && last.GetProperty("Trample").GetInt32() == 1) &&
                (addition || left.GetProperty("Units").ContentEquals(right.GetProperty("Units"))),
                "Native primary cache or subsequent preview upgrade count differs.");
        }
        var frame = context.Enchantments!.Frame(context);
        var repeated = TroopAuraModel.UpdateAll(frame, [6]);
        Require(repeated.Supported && repeated.State!.Train.Rooms[1].Units.Single(unit => unit.Id == 17).Attack == 5 &&
            repeated.State.Train.Rooms[1].Units.Single(unit => unit.Id == 17).Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == "troop-aura:6") == 1,
            "Stable primary cache reapplied its upgrade.");
        BattleTurnState current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Owned real-birth prefix differs.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                parent == JsonSerializer.Serialize(before, ModelJson.Options), "Real troop birth changed its parent, state or RNG.");
        });
        if (replay.HasValue)
        {
            Native(replay.Value);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TROOP-BIRTH-ADDITION PASS:18 native inputs/continuous state/RNG, real3-to5 attack and committed target cache, " +
            "subsequent previews do not duplicate modifiers, four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
