using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicDamageTowerRosterChecks
{
    private const string Reason = "Tower trigger targeting requires complete-train unit snapshots.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 22, "Wrong native Consume/Tower prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var boss = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 6);
        var changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 6);
        Require(before.Spawn.Turn == 4 && action.CardInstanceId == 21 && action.RoomIndex == 0 && action.TargetUnitId == 2 &&
            before.Spawn.Train.Context!.FindCard(21)!.DataId == "d35bfe1d-d05e-403a-b6ab-38ce30d3e2ea" &&
            boss.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack" && boss.Health == 1086 && changed.Health == 927 &&
            before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 8).Attack == 15 &&
            after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 8).Attack == 17 &&
            after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 2).Health == 20,
            "Missing native HeroicDive and companion Tower attack mutation.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            var applied = BattleActionModel.PlayCard(before, action);
            Require(applied.Supported && NativeBranchObservationChecks.Key(applied.State!) == NativeBranchObservationChecks.Key(after),
                "Parallel Consume/Tower replay differs from native.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Consume/Tower mutated its parent.");
        if (replay is FixtureValue second)
        {
            NativeBranchObservationChecks.Replay(second, Reason);
            NativeBranchObservationChecks.Pair(source, second);
        }
        Console.WriteLine("RELIC-DAMAGE-TOWER-ROSTER PASS:22 native inputs/full state/forecasts/RNG; Consume relic damage/Tower companion +2attack; eight immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
