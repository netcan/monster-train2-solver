using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleHordeTargetDeathChecks
{
    private const string Reason = "Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 22 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 25 && action.RoomIndex == 2 && action.TargetUnitId == 15 && actual.Spawn.Train.Context!.NextUnitId == 27,
            "Missing native targeted kill or horde birth.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 26 &&
                unit.GetProperty("Health").GetInt32() == 12) &&
            update.GetProperty("After").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 26 &&
                unit.GetProperty("Health").GetInt32() == 21)).ToArray();
        Require(writes.Length == 1 && writes[0].GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
            writes[0].GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 26]), "Missing native triple aura write.");
        var context = before.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var dying = frame.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 15)
            .WithHealth(0).WithDeathState(new UnitDeathState(true, true, false, isDespawned: false, isDestroyed: false));
        var rooms = frame.Train.Rooms.Select(room => room.WithUnits(room.Units.Where(unit => unit.Id != 15).ToArray())).ToArray();
        var train = new TrainCombatState(rooms, frame.Train.Movement, frame.Train.EnemySlotsPerRoom, context);
        EnchantmentCombatState Pending(int room, bool preview, CombatUnit unit) => new(train,
            frame.RetainedUnits.Append(new EnchantmentRetainedUnit(unit, room, preview)).ToArray(), frame.EnchanterIds,
            frame.AllowUpdates, frame.Updating, false, frame.TestRng, frame.TemporaryPreviewUnitIds);
        Require(!TroopAuraModel.OnlyFragileBuffSource(Pending(2, false, dying)) &&
            TroopAuraModel.OnlyFragileBuffSource(Pending(2, false, dying), pendingDyingRoom: 2) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(1, false, dying), pendingDyingRoom: 2) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(2, true, dying), pendingDyingRoom: 2) &&
            !TroopAuraModel.OnlyFragileBuffSource(Pending(2, false, dying.WithHealth(1)), pendingDyingRoom: 2),
            "Pending death escaped its native room, preview or health guard.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Action mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterSycophant" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == control)
                        obj["AssetKey"] = "unknown";
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(altered);
            var result = BattleActionModel.PlayCard(altered.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual),
                "Unknown source incorrectly received native result: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("TRIPLE-HORDE-TARGET-DEATH PASS: 22 native inputs/full state/RNG, one write, four immutable branches and source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
