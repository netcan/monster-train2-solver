using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopTwoRetiredMixedPreviewChecks
{
    private const string Reason = "Battle preview floor 0: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 18 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        Require(context.NextUnitId == 23 && frame.EnchanterIds.SequenceEqual([11, 15, 22]) &&
            frame.RetainedUnits.Any(actor => actor.Unit.Id == 15 && !actor.Preview && actor.Unit.DeathState?.IsDestroyed == true),
            "Missing native identity or retired source.");
        var titan = frame.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 22)
            .WithHealth(0).WithDeathState(new UnitDeathState(true, true, false, isDestroyed: true));
        var rooms = frame.Train.Rooms.Select(room => room.WithUnits(room.Units.Where(unit => unit.Id != 22).ToArray())).ToArray();
        var train = new TrainCombatState(rooms, frame.Train.Movement, frame.Train.EnemySlotsPerRoom, context);
        EnchantmentCombatState Retired(int room, bool preview, IReadOnlyList<int> ids) => new(train,
            frame.RetainedUnits.Append(new EnchantmentRetainedUnit(titan, room, true)).ToArray(), ids,
            frame.AllowUpdates, frame.Updating, preview, frame.TestRng, frame.TemporaryPreviewUnitIds);
        Require(!TroopAuraModel.OnlyFragileBuffSource(Retired(-1, true, [11, 15, 22])) &&
            TroopAuraModel.OnlyFragileBuffSource(Retired(-1, true, [11, 15, 22]), allowTwoRetiredMixedPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Retired(-1, false, [11, 15, 22]), allowTwoRetiredMixedPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Retired(1, true, [11, 15, 22]), allowTwoRetiredMixedPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Retired(-1, true, [11, 15, 15]), allowTwoRetiredMixedPreview: true),
            "Mixed retired sources escaped their explicit preview guard.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 23) &&
            update.GetProperty("After").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 23)).ToArray();
        Require(writes.Length == 3, "Expected three preview writes.");
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 23);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 23);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
                bu.GetProperty("Health").GetInt32() == 12 + 9 * i && au.GetProperty("Health").GetInt32() == 21 + 9 * i &&
                bu.GetProperty("Upgrades").GetArrayLength() == 3 * i && au.GetProperty("Upgrades").GetArrayLength() == 3 * (i + 1),
                "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Preview differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
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
            var result = BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(!result.Supported || result.State!.Spawn.Train.Context!.NextUnitId != 23,
                "Unknown source incorrectly received identity cleanup: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-TWO-RETIRED-MIXED-PREVIEW PASS: 18 native inputs/full state/RNG, three writes, identity, four immutable branches and source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
