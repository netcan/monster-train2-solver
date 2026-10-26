using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonSilenceResetChecks
{
    private const string Reason = "Moon status silence loss resets phase understanding.";
    internal static void Native(FixtureValue battle, FixtureValue? source = null, FixtureValue? replay = null)
    {
        var rows = battle.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 19, "Missing native nineteen-input counterexample.");
        var row = rows[^1];
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var old = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit Unit(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2);
        Require(action.CardInstanceId == 33 && action.TargetUnitId == 2 && before.Spawn.Turn == 3 &&
            Unit(before).MoonPhaseMask == 6 && Unit(before).Status("silenced")?.Stacks == 1 &&
            Unit(actual).MoonPhaseMask == 2 && Unit(actual).Status("silenced")?.Stacks == 1 &&
            Unit(old).MoonPhaseMask == 6, "Wrong native equipment/Silence replacement boundary.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && BattleTurnChecks.Comparable(result.State!, false) == BattleTurnChecks.Comparable(actual, false),
                "Silence reset differs from complete native state/RNG: " + (result.Reason ?? ModelJson.Difference(
                    JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options))));
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Silence reset mutated parent.");
        foreach (string control in new[] { "Callback", "Tracking", "Mask" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            var target = changed["Spawn"]!["Train"]!["Rooms"]![0]!["Units"]!.AsArray().Single(unit => unit!["Id"]!.GetValue<int>() == 2)!;
            if (control == "Callback") target["Triggers"]![0]!["Kind"] = "OnMoonLit";
            if (control == "Tracking") changed["Spawn"]!["Train"]!["Context"]!["QueryFrame"]!["MoonPhaseTrackingActive"] = false;
            if (control == "Mask") target["MoonPhaseMask"] = null;
            Require(!BattleActionModel.PlayCard(changed.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Uncalibrated moon reset accepted: " + control);
        }
        if (source.HasValue || replay.HasValue)
        {
            Require(source.HasValue && replay.HasValue, "Independent observation pair missing.");
            NativeBranchObservationChecks.Replay(source!.Value, Reason, (historical, native) =>
                Require(Unit(historical).MoonPhaseMask == 6 && Unit(native).MoonPhaseMask == 2, "Old mask mismatch changed."));
            NativeBranchObservationChecks.Replay(replay!.Value, Reason);
            NativeBranchObservationChecks.Pair(source.Value, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("MOON-SILENCE-RESET PASS: exact StoicPlatemail replacement, full native state/RNG, four immutable branches and three strict guards; independentPair=" + source.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
