using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopFragileUpperCombatChecks
{
    private const string Reason = "Train combat floor 1: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 10 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        Require(frame.EnchanterIds.SequenceEqual([10, 13]) && !frame.Preview, "Missing native active combat sources.");
        EnchantmentCombatState With(IReadOnlyList<int> ids) => new(frame.Train, frame.RetainedUnits,
            ids, frame.AllowUpdates, frame.Updating, false, frame.TestRng, frame.TemporaryPreviewUnitIds);
        Require(!TroopAuraModel.OnlyFragileBuffSource(With([10, 13])) &&
            TroopAuraModel.OnlyFragileBuffSource(With([10, 13]), allowLiveCombat: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(With([10, 13, 7]), allowLiveCombat: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(With([10, 13, 13]), allowLiveCombat: true), "Combat source guard failed.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 15)).ToArray();
        Require(writes.Length >= 2 && writes[0].GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
            writes[0].GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 15]) &&
            writes[0].GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 15).GetProperty("Health").GetInt32() == 21 &&
            writes[1].GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 15).GetProperty("Health").GetInt32() == 21,
            "Native combat troop cache or writes differ.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Upper combat differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-FRAGILE-UPPER-COMBAT PASS: ten native inputs/full state/RNG, combat cache and writes, four immutable branches and source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
