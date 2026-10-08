using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PoolSummonChecks
{
    internal static void Native(FixtureValue fixture)
    {
        string scenario = fixture.GetProperty("ModifierScenario").GetString()!;
        bool fresh = scenario.EndsWith("fresh", StringComparison.Ordinal);
        var samples = fixture.GetProperty("PoolSummonSelections").EnumerateArray().ToArray();
        var births = fixture.GetProperty("UnitBirths").EnumerateArray().ToArray();
        var clones = fixture.GetProperty("DetachedCardClones").EnumerateArray().ToArray();
        var upgrades = fixture.GetProperty("SpawnUpgrades").EnumerateArray().ToArray();
        var phases = fixture.GetProperty("RallyPhases").EnumerateArray().ToArray();
        var triggers = fixture.GetProperty("RallyTriggers").EnumerateArray().ToArray();
        bool overridden = false, duplicates = false, noPrimary = false;
        int total = 0;
        foreach (var sample in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = sample.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = sample.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            var rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            if (rule.Summon?.Pool.Count is not > 0) continue;
            var after = sample.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var oldIds = before.Spawn.Train.Rooms[action.RoomIndex].Units.Select(unit => unit.Id).ToHashSet();
            var born = after.Spawn.Train.Rooms[action.RoomIndex].Units.Where(unit => !oldIds.Contains(unit.Id)).OrderBy(unit => unit.Id).ToArray();
            int available = before.PlayRules.Rooms[action.RoomIndex].PlayerSlots -
                before.Spawn.Train.Rooms[action.RoomIndex].Units.Count(unit => unit.Team == CombatTeam.Player);
            int capped = Math.Min(Math.Max(1, rule.Summon.Count) * (rule.Summon.Additional == null ? 1 : 2), available);
            var rng = before.Spawn.Train.Context.BattleRng;
            for (int index = 0; index < born.Length; index++)
            {
                var draw = rng.Range(0, rule.Summon.Pool.Count); rng = draw.State;
                var selected = rule.Summon.Pool[draw.Value];
                if (rule.Summon.Additional != null && index >= capped / 2)
                {
                    overridden |= selected.Unit.AssetKey != rule.Summon.Additional.Unit.AssetKey;
                    selected = rule.Summon.Additional;
                }
                Require(born[index].AssetKey == selected.Unit.AssetKey &&
                    born[index].Modifiers!.SpawnerMatchesDefinition == selected.Unit.Modifiers!.SpawnerMatchesDefinition,
                    "Pool choice, additional override or source matching differs.");
                Require(after.Spawn.Train.Context!.FindCard(born[index].SpawnerCardId)!.DataId ==
                    (fresh ? selected.FallbackCreation!.DataId : card.DataId), "Selected unit source definition differs.");
            }
            Require(rng.Equals(after.Spawn.Train.Context!.BattleRng), "Birth callbacks or override skipped/added a pool draw.");
            total += born.Length;
            duplicates |= rule.Summon.Pool.Count > rule.Summon.Pool.Select(choice => choice.Unit.AssetKey).Distinct().Count();
            noPrimary |= rule.SpawnUnit == null && rule.Summon.NativeBaseSize == 0 && !rule.Summon.TriggersPaidRally;
        }
        Require(total >= 7 && total == samples.Length && upgrades.Length == total, "Pool selections were not recorded once per birth.");
        if (scenario.Contains("additional")) Require(overridden, "No sampled result was actually overridden by the additional character.");
        if (scenario.Contains("singleton")) Require(samples.All(sample => sample.GetProperty("Pool").GetArrayLength() == 1), "Singleton case used a larger pool.");
        else Require(duplicates, "Ordered duplicate pool entries were not exercised.");
        if (scenario.EndsWith("no-primary", StringComparison.Ordinal)) Require(noPrimary, "A no-primary pool was replaced by a placeholder template.");
        if (fresh) FreshSummonChecks.Native(fixture);
        Verify();
        Parallel.For(0, 32, _ => Verify());
        void Verify()
        {
            foreach (var sample in samples)
            {
                Require(sample.GetProperty("Completed").GetBoolean(), "Incomplete pool sampling.");
                var before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
                var after = sample.GetProperty("After").Deserialize<CombatContext>()!;
                var pool = sample.GetProperty("Pool").EnumerateArray().Select(item => item.GetString()!).ToArray();
                string parent = Serialize(before);
                var draw = before.BattleRng.Range(0, pool.Length);
                Require(pool[draw.Value] == sample.GetProperty("Selected").GetString() &&
                    Serialize(before.WithBattleRng(draw.State)) == Serialize(after), "Independent pool sampling differs from native complete context.");
                Require(!before.BattleRng.Equals(after.BattleRng) && Serialize(before) == parent, "Sampling failed to advance RNG or mutated its parent.");
            }
            foreach (var sample in births) UnitSummonChecks.VerifyBirth(sample);
            foreach (var sample in clones) UnitSummonChecks.VerifyClone(sample);
            foreach (var sample in upgrades) UnitSummonChecks.VerifyExtra(sample);
            foreach (var sample in phases) RallyChecks.VerifyPhase(sample);
            foreach (var sample in triggers) RallyChecks.VerifyTrigger(sample);
        }
        Console.WriteLine($"NATIVE-POOL-SUMMON-CHECKS PASS: {samples.Length} native selections, ordered duplicates={duplicates}, overridden={overridden}, no primary={noPrimary}, exact sources/RNG and 32 branches.");
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
