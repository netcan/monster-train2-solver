using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UntouchableFrontRelicChecks
{
    private const string Reason = "Native revenge upgrade target differs after Untouchable.";
    private const string UpgradeId = "45bdb6aa-dd9a-42b7-930d-f586a8c7979a";
    private static CombatUnit Unit(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    internal static void Native(FixtureValue battle, FixtureValue? source = null, FixtureValue? replay = null)
    {
        var rows = battle.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 39, "Missing39-input natural counterexample.");
        var row = rows[^1];
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var old = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
        Require(row.GetProperty("IsEndTurn").GetBoolean() && before.Spawn.Turn == 6 &&
            Unit(before, 2).Status("untouchable")?.Stacks == 1 && Unit(before, 2).Health == 170 &&
            Unit(actual, 2).Health == 190 && Unit(old, 2).Health == 205 &&
            Unit(actual, 8).Health == 124 && Unit(old, 8).Health == 109 &&
            Count(Unit(actual, 2)) == Count(Unit(before, 2)) && Count(Unit(actual, 8)) == Count(Unit(before, 8)) + 1,
            "Wrong front Untouchable/+15HP relic transfer.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && BattleTurnChecks.Comparable(result.State!, false) == BattleTurnChecks.Comparable(actual, false),
                "Front relic target differs from full native state/RNG: " + (result.UnsupportedReason ?? ModelJson.Difference(
                    JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options))));
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Front relic mutated parent.");
        var direct = RelicSoloUnitUpgradeModel.Apply(before.Spawn.Train);
        Require(direct.Supported &&
            direct.State!.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2).Health == 170 &&
            direct.State.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 8).Health == Unit(before, 8).Health + 15,
            "Front-room relic did not skip exactly the Untouchable character.");
        if (source.HasValue || replay.HasValue)
        {
            Require(source.HasValue && replay.HasValue, "Independent pair missing.");
            NativeBranchObservationChecks.Replay(source!.Value, Reason, (historical, native) =>
                Require(Unit(historical, 2).Health == Unit(native, 2).Health + 15 &&
                    Unit(historical, 8).Health == Unit(native, 8).Health - 15, "Original +15HP target counterexample changed."));
            NativeBranchObservationChecks.Replay(replay!.Value, Reason);
            NativeBranchObservationChecks.Pair(source.Value, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("UNTOUCHABLE-FRONT-RELIC PASS: natural39-input counterexample; exact +15HP target/card upgrade transfer; full state/forecast/RNG and four immutable branches; independentPair=" + source.HasValue);
    }
    private static int Count(CombatUnit unit) => unit.Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == UpgradeId);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
