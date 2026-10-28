using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopFragileMixedPreviewChecks
{
    private const string Reason = "Battle preview floor 0: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 27 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        Require(frame.EnchanterIds.SequenceEqual([11, 16, 24]) && frame.RetainedUnits.Any(actor =>
            actor.Unit.Id == 16 && actor.RoomIndex == -1 && actor.Unit.DeathState?.IsDestroyed == true), "Missing mixed native sources.");
        EnchantmentCombatState With(IReadOnlyList<int> ids, bool preview) => new(frame.Train, frame.RetainedUnits,
            ids, frame.AllowUpdates, frame.Updating, preview, frame.TestRng, frame.TemporaryPreviewUnitIds);
        Require(TroopAuraModel.OnlyFragileBuffSource(With([11, 16, 24], true)) &&
            !TroopAuraModel.OnlyFragileBuffSource(With([11, 16, 24], false), allowLiveCombat: true) &&
            !TroopAuraModel.OnlyFragileBuffSource(With([11, 16, 24, 28], true)) &&
            !TroopAuraModel.OnlyFragileBuffSource(With([11, 16, 16], true)), "Mixed source guard failed.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 31)).ToArray();
        Require(writes.Length >= 2 && writes[0].GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 7, 13]) &&
            writes[0].GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 31).GetProperty("Health").GetInt32() == 21 &&
            writes[1].GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 31).GetProperty("Health").GetInt32() == 30,
            "Native mixed preview writes differ.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Mixed preview differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-FRAGILE-MIXED-PREVIEW PASS: 27 native inputs/full state/RNG, repeated writes, four immutable branches and source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
