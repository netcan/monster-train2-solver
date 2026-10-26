using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptionScalingPolicyChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var row = fixture.GetProperty("Rows").EnumerateArray().Last();
        var observed = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(observed.Spawn.Turn == 5 && observed.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 3 &&
            actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 4).Attack - observed.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 4).Attack == 24,
            "Missing original positive-corruption native attack counterexample.");
        var node = JsonSerializer.SerializeToNode(observed, ModelJson.Options)!;
        var cards = node["Spawn"]!["Train"]!["Context"]!["Cards"]!;
        // Isolate target selection while retaining the observed units, corruption,
        // traits and native RNG. Other cards remain covered by their original pairs.
        cards["Hand"] = new JsonArray(cards["Hand"]!.AsArray().Single(item => item!["InstanceId"]!.GetValue<int>() == 25)!.DeepClone());
        var source = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        foreach (int preferred in new[] { 0, 1, 2 })
        {
            Check(BattleHeuristicPolicy.Choose(source, out _, preferred));
            Check(BattleTacticalPolicy.Choose(source, out _, preferred));
        }
        void Check(SupportedActionCandidate? choice)
        {
            Require(choice?.Action is { CardInstanceId: 25, RoomIndex: 1, TargetUnitId: 4 } &&
                choice.Result.Supported && choice.Result.State!.Energy == actual.Energy &&
                JsonSerializer.Serialize(choice.Result.State.Spawn.Train.Rooms[1].Units, ModelJson.Options) ==
                    JsonSerializer.Serialize(actual.Spawn.Train.Rooms[1].Units, ModelJson.Options) &&
                JsonSerializer.Serialize(choice.Result.State.Spawn.Train.Context!.BattleRng, ModelJson.Options) ==
                    JsonSerializer.Serialize(actual.Spawn.Train.Context!.BattleRng, ModelJson.Options) &&
                JsonSerializer.Serialize(source, ModelJson.Options) == parent, "A seed wasted the corruption upgrade on its zero-gain champion or changed its parent.");
        }
        Console.WriteLine("CORRUPTION-SCALING-POLICY PASS: both seeds/three deployment preferences choose the native24 attack floor over the zero-gain champion; parent/RNG immutable.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
