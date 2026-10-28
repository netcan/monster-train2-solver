using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopRetiredFourHordePreviewChecks
{
    private const string Reason = "Battle preview floor 1: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 17 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 31 && actual.Spawn.Train.Context!.NextUnitId == 20 &&
            PreviewBirthModel.IsOriginalGreenRecruit(before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 19), before.Spawn.Train.Context!),
            "Missing recruit source or native identity cleanup.");
        var context = before.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var titan = frame.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 15)
            .WithHealth(0).WithDeathState(new UnitDeathState(true, true, false, isDestroyed: true));
        var rooms = frame.Train.Rooms.Select(room => room.WithUnits(room.Units.Where(unit => unit.Id != 15).ToArray())).ToArray();
        var train = new TrainCombatState(rooms, frame.Train.Movement, frame.Train.EnemySlotsPerRoom, context);
        EnchantmentCombatState Retired(int room, bool preview, IReadOnlyList<int> ids) => new(train,
            frame.RetainedUnits.Append(new EnchantmentRetainedUnit(titan, room, true)).ToArray(), ids,
            frame.AllowUpdates, frame.Updating, preview, frame.TestRng, frame.TemporaryPreviewUnitIds);
        var retired = Retired(-1, true, [11, 15]);
        Require(!TroopAuraModel.OnlyFragileBuffSource(retired) &&
            TroopAuraModel.OnlyFragileBuffSource(retired, allowSingleRetiredPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Retired(-1, false, [11, 15]), allowSingleRetiredPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Retired(1, true, [11, 15]), allowSingleRetiredPreview: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(Retired(-1, true, [11, 15, 15]), allowSingleRetiredPreview: true),
            "Single retired Titanskin escaped its explicit preview guard.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 20) &&
            update.GetProperty("After").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 20)).ToArray();
        Require(writes.Length == 6, "Expected six preview writes.");
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 20);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 20);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 19]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 19]) &&
                bu.GetProperty("Health").GetInt32() == 16 + 12 * i && au.GetProperty("Health").GetInt32() == 28 + 12 * i &&
                bu.GetProperty("Upgrades").GetArrayLength() == 4 * i && au.GetProperty("Upgrades").GetArrayLength() == 4 * (i + 1),
                "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Preview differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "PlayCard mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterGenerousHost" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int changedCount = 0;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == control)
                    { obj["AssetKey"] = "unknown"; changedCount++; }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(altered);
            var result = BattleActionModel.PlayCard(altered.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(changedCount > 0 && (!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual)),
                "Unknown source matched calibrated successor: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-RETIRED-FOUR-HORDE-PREVIEW PASS: 17 native inputs/full state/RNG, six writes, identity, four immutable branches and four source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
