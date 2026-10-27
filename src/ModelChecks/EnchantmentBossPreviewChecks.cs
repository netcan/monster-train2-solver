using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnchantmentBossPreviewChecks
{
    private const string Reason = "Native ordinary boss aura action outside the temporary preview roster.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (replay != null)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("ENCHANTMENT-BOSS-PREVIEW PASS: seven complete native EndTurns/forecasts/RNG; preserved live boss pending action; independentNative=" + (replay != null));
    }
    private static void Check(FixtureValue fixture, bool historical)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historical ? Historical : null);
        Require(rows.Length == 7 && rows.All(row => row.GetProperty("IsEndTurn").GetBoolean()), "Wrong seven-EndTurn boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 6 && actual.Spawn.Turn == 7 &&
            Actor(before, 15).AssetKey == "Boss_Level5_Reanimate" && Actor(actual, 15).Health == 500 &&
            actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Select(unit => unit.Id).SequenceEqual(new[] { 15, 16 }) &&
            Actor(actual, 1).Health == 13 && State(actual).PrimaryTargets.Single() is { UnitId: 15, IsEnchanted: true, NextAction: 0 } &&
            State(actual).PreviewTargets.Single() is { UnitId: 15, IsEnchanted: true, NextAction: 1 } && State(actual).PreviewRequiresSync,
            "Missing live reanimation boss/aura preview witness.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Shared aura preview state differs under an independent immutable branch.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Aura preview mutated its parent.");
    }
    private static void Historical(BattleTurnState old, BattleTurnState actual)
    {
        Require(State(old).PreviewTargets.Single() is { UnitId: 15, IsEnchanted: true, NextAction: 0 }, "Historical action was replaced.");
        var node = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
        // Normalize only the witnessed historical map field; all other captured state remains strict.
        foreach (var context in node["Spawn"]!["Train"]!["Rooms"]!.AsArray().Select(room => room!["Context"])
            .Append(node["Spawn"]!["Train"]!["Context"]))
        {
            var actor = context!["Enchantments"]!["Rooms"]!.AsArray().SelectMany(room => room!["Units"]!.AsArray())
                .Single(unit => unit!["Id"]!.GetValue<int>() == 16);
            actor!["Triggers"]![0]!["Effects"]![0]!["Enchantment"]!["State"]!["PreviewTargets"]![0]!["NextAction"] = 1;
        }
        node["Spawn"]!["Train"]!["Rooms"]!.AsArray().Single(room => room!["RoomIndex"]!.GetValue<int>() == 1)!["Units"]![1]!
            ["Triggers"]![0]!["Effects"]![0]!["Enchantment"]!["State"]!["PreviewTargets"]![0]!["NextAction"] = 1;
        Require(NativeBranchObservationChecks.Key(node.Deserialize<BattleTurnState>(ModelJson.Options)!) == NativeBranchObservationChecks.Key(actual),
            "Historical difference extends beyond the retained boss pending action.");
    }
    private static CombatUnit Actor(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static EnchantmentState State(BattleTurnState state) => Actor(state, 16).Triggers[0].Effects[0].Enchantment!.State;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
