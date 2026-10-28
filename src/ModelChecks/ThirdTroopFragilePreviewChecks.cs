using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopFragilePreviewChecks
{
    private const string Reason = "Battle preview floor 0: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue source, FixtureValue rngWitness, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 7 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing seven-input native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(source.GetProperty("Request").ContentEquals(rngWitness.GetProperty("Request")), "RNG witness changed the request.");
        var witnessRows = rngWitness.GetProperty("Rows").EnumerateArray().ToArray();
        Require(witnessRows.Length == rows.Length, "RNG witness changed the input count.");
        for (int index = 0; index < rows.Length; index++)
            foreach (string field in new[] { "Before", "Actual" })
                Require(NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(witnessRows[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                    "RNG witness changed the native " + field + " at " + index);
        var random = rngWitness.GetProperty("BattleTestRng").EnumerateArray().ToArray();
        Require(random.Length == 1 && random[0].GetProperty("Preview").GetBoolean() &&
            random[0].GetProperty("Min").GetInt32() == 0 && random[0].GetProperty("Max").GetInt32() == 1 &&
            random[0].GetProperty("Stack").GetString()!.Contains("RelicEffectAddStatusEffectOnSpawn") &&
            random[0].GetProperty("Before").GetProperty("S0").GetUInt32() == 278617022 &&
            random[0].GetProperty("After").GetProperty("S0").GetUInt32() == 1536548855,
            "Missing native preview relic RNG draw.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 11)).ToArray();
        Require(writes.Length >= 3, "Missing repeated native preview troop writes.");
        var first = writes[0].GetProperty("Before");
        var after = writes[0].GetProperty("After");
        var unitBefore = first.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 11);
        var unitAfter = after.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 11);
        Require(first.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
            after.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
            unitBefore.GetProperty("Horde").GetInt32() == 3 && unitBefore.GetProperty("Health").GetInt32() == 12 &&
            unitAfter.GetProperty("Health").GetInt32() == 21 && unitAfter.GetProperty("Upgrades").GetArrayLength() == 3,
            "First native preview write differs.");
        Require(writes[1].GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 11)
            .GetProperty("Health").GetInt32() == 30, "Second native preview write differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Third troop Fragile preview differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        if (replay.HasValue) { Native(replay.Value, replay.Value); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("THIRD-TROOP-FRAGILE-PREVIEW PASS: seven native inputs/full state/RNG, repeated preview HP12->21->30, four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
