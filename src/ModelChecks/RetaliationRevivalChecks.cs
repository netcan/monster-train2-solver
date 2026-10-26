using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetaliationRevivalChecks
{
    internal static void Original(FixtureValue fixture)
    {
        RollingAdviceChecks.ObservedSuccessors(fixture);
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 78 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong retaliation/revival boundary.");
        var predicted = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(predicted.Spawn.Train.Rooms.SelectMany(r => r.Units).Single(u => u.Id == 9).Health == 115 &&
            actual.Spawn.Train.Rooms.SelectMany(r => r.Units).Single(u => u.Id == 9).Health == 109,
            "Historical six-damage boss discrepancy was lost.");
        var stages = fixture.GetProperty("Stages").EnumerateArray().Where(s =>
            s.GetProperty("Index").GetInt32() is 78 or 79).ToArray();
        Require(stages.Length == 2, "Missing independent native exchange observations.");
        foreach (var stage in stages)
        {
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var champion = before.Units.Single(u => u.Id == 2);
            Require(champion.Status("undying")?.Stacks > 0 && champion.Status("spikes")?.Stacks == 6 &&
                champion.Status("debuff")?.Stacks > 0, "Missing native Undying/Spikes/debuff interaction.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            var native = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            // Compare native room snapshots directly, including aura maps, card statistics and RNG.
            Parallel.For(0, 4, _ =>
            {
                var result = RoomCombatModel.Exchange(before);
                Require(result.Supported && JsonSerializer.Serialize(result.State, ModelJson.Options) ==
                    JsonSerializer.Serialize(native, ModelJson.Options) &&
                    JsonSerializer.Serialize(before, ModelJson.Options) == parent,
                    "Retaliation/revival exchange differs from native state/RNG or mutates its parent.");
            });
        }
        Console.WriteLine("RETALIATION-REVIVAL PASS: original 78 native successors, six-damage historical counterexample, both native exchanges and four immutable parallel branches each.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
