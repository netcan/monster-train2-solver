using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AngelDuoForcedRelentlessChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("Control").GetString() == "TemporaryNativeTurnSpawnPhaseAndScoringCounter",
            "Forced Relentless calibration must identify its temporary native controls.");
        BattleTurnState original = fixture.GetProperty("Original").Deserialize<BattleTurnState>()!;
        BattleTurnState restored = fixture.GetProperty("Restored").Deserialize<BattleTurnState>()!;
        Require(ModelJson.Difference(Json(original), Json(restored)) == null,
            "Native controls must restore complete original battle state and RNG.");
        Require(original.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Any(unit => OuterBossDefinitions.IsAngelDuo(unit.AssetKey)),
            "Forced Relentless calibration requires the original Angel Duo battle.");
        FixtureValue samples = fixture.GetProperty("Samples");
        Require(samples.GetArrayLength() == 5, "Expected early, penultimate, final and post-final native turn controls.");
        int early = 0, unchanged = 0;
        var turns = new HashSet<int>();
        foreach (FixtureValue row in samples.EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<ForcedRelentlessProgress>()!;
            var actual = row.GetProperty("Actual").Deserialize<ForcedRelentlessProgress>()!;
            var battle = row.GetProperty("BeforeBattle").Deserialize<BattleTurnState>()!;
            var actualBattle = row.GetProperty("ActualBattle").Deserialize<BattleTurnState>()!;
            string input = Json(before), root = Json(battle), expected = Json(actualBattle);
            Require(turns.Add(before.Turn) && before.RemainingTurns == 9,
                "Native controls must include distinct turns and the original scoring sentinel.");
            ForcedRelentlessProgress predicted = ForcedRelentlessModel.Apply(before);
            Require(Json(predicted) == Json(actual), "Forced Relentless counters differ from native: turn=" + before.Turn);
            string? difference = ModelJson.Difference(Json(ForcedRelentlessModel.ApplyBattle(battle, before)), expected);
            Require(difference == null, "Forced Relentless complete native state differs at turn " + before.Turn + ": " + difference);
            if (before.Turn < before.WaveCount - 1) early++;
            else
            {
                unchanged++;
                Require(ReferenceEquals(before, predicted) && actual.RemainingTurns == 9 &&
                    ReferenceEquals(battle, ForcedRelentlessModel.ApplyBattle(battle, before)),
                    "Final/post-final native calls must retain counters and model root identity.");
            }
            Parallel.For(0, 16, _ => {
                Require(Json(ForcedRelentlessModel.Apply(before)) == Json(actual) &&
                    Json(ForcedRelentlessModel.ApplyBattle(battle, before)) == expected &&
                    Json(before) == input && Json(battle) == root,
                    "Forced Relentless independent branches changed native state/RNG or their shared input.");
            });
        }
        Require(early == 3 && unchanged == 2 && turns.Contains(0) && turns.Contains(1),
            "Calibration must cover three early turns and two native no-op boundaries.");
        Console.WriteLine("ANGEL-DUO-FORCED-RELENTLESS PASS: five native counter/full-state controls, three early transitions, two no-op boundaries, eighty immutable branches and full native restoration; this check covers the global primitive.");
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
