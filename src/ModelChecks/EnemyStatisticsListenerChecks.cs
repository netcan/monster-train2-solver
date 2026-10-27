using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnemyStatisticsListenerChecks
{
    // Preserve the diagnostic request's original label. Its complete native
    // witness identifies ordinary enemies Heavy8/Titanskin10, not a treasure.
    private const string Reason = "Native departed treasure listener cleanup differs after input21.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (replay.HasValue) { Check(replay.Value, false); NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true); }
        Console.WriteLine("ENEMY-STATISTICS-LISTENERS PASS:21 native inputs/full state/forecast/RNG; Heavy8/Titanskin10 OnDestroy listeners; four immutable branches and partial-frame controls; independentPair=" + replay.HasValue);
    }
    private static void Check(FixtureValue fixture, bool historical)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historical ? Historical : null);
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing complete ordinary enemy destruction prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 3 && actual.Spawn.Turn == 4 &&
            Retained(actual, 8) is { AssetKey: "HeavyT2_Basic", Health: 0,
                DeathState: { IsDestroyed: true, IsDespawned: false, HasFinishedDying: true, HasStatisticsListener: false } } &&
            Retained(actual, 10) is { AssetKey: "EnchanterT2_Titanskin", Health: 0,
                DeathState: { IsDestroyed: true, IsDespawned: false, HasFinishedDying: true, HasStatisticsListener: false } },
            "Lost ordinary lethal deaths and native completed listener cleanup.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(actual),
                "Completed enemy frame changed native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Enemy destruction mutated its parent.");
        if (historical)
        {
            var old = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
            var pending = TrainCombatModel.CompleteFrameRemovals(old.Spawn.Train, []);
            var partial = TrainCombatModel.CompleteFrameRemovals(old.Spawn.Train, [8]);
            bool Listener(TrainCombatState state, int id) => state.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit.DeathState!.HasStatisticsListener;
            Require(Listener(pending, 8) && Listener(pending, 10) && !Listener(partial, 8) && Listener(partial, 10),
                "Uncompleted destruction was cleared together with the completed frame.");
        }
    }
    private static void Historical(BattleTurnState old, BattleTurnState actual)
    {
        var projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        foreach (int id in new[] { 8, 10 })
        {
            Require(Retained(old, id).DeathState!.HasStatisticsListener && !Retained(actual, id).DeathState!.HasStatisticsListener,
                "Preserve original ordinary enemy statistics listeners.");
            projected["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray()
                .Single(actor => actor!["Unit"]!["Id"]!.GetValue<int>() == id)!["Unit"]!["DeathState"]!["HasStatisticsListener"] = false;
        }
        Require(JsonNode.DeepEquals(projected, JsonNode.Parse(NativeBranchObservationChecks.Key(actual))),
            "Historical mismatch extends beyond two ordinary enemy statistics listeners.");
    }
    private static CombatUnit Retained(BattleTurnState state, int id) => state.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == id).Unit;
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
