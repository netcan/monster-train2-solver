using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InfernoPreCombatPositionsChecks
{
    internal static void Native(FixtureValue fixture)
    {
        foreach (int index in new[] { 42, 43, 79, 80 })
        {
            var stage = fixture.GetProperty("Stages")[index];
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Require(before.RoomIndex == 2 && !before.Preview, "Wrong original room.");
            if (index is 42 or 43) Require(actual.Units.Single(unit => unit.Team == CombatTeam.Player) is
                { Id: 28, AssetKey: "MonsterTruffles", Health: 5 } &&
                actual.Context!.SpawnPoints!.Units.Single(point => point.UnitId == 28).Current!.Index == 2,
                "Missing native survivor and two uncompressed vacancies.");
            else Require(actual.Units.All(unit => unit.Team == CombatTeam.Enemy) &&
                actual.Context!.SpawnPoints!.Units.Single(point => point.UnitId == 40) is
                    { Current: null, LastKnown: { RoomIndex: 2, Team: CombatTeam.Player, Index: 1 } } &&
                actual.Context.SpawnPoints.Units.Single(point => point.UnitId == 41) is { Current: null, LastKnown: null },
                "Lost distinct native old-point lifetimes after the two room-effect deaths.");
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            var targets = stage.GetProperty("ExternalTargets").EnumerateArray().Select(target =>
                (target.GetProperty("RoomIndex").GetInt32(), target.GetProperty("Unit").Deserialize<CombatUnit>()!)).ToArray();
            var pyre = stage.GetProperty("ExternalPyre").Deserialize<CombatUnit>();
            Parallel.For(0, 4, _ =>
            {
                var result = index is 42 or 79 ? RoomCombatModel.Resolve(before, externalPyre: pyre,
                    externalPyreRoomIndex: pyre == null ? -1 : stage.GetProperty("ExternalPyreRoomIndex").GetInt32(),
                    externalTargets: targets) : RoomCombatModel.Exchange(before, externalTargets: targets);
                Require(result.Supported, result.UnsupportedReason ?? "Unsupported pre-combat positions.");
                Require(Key(result.State!) == Key(actual),
                    "Inferno partial-survival stage" + index + ": " + ModelJson.Difference(Key(result.State!), Key(actual)));
            });
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Pre-combat positions changed their parent.");
        }
        Console.WriteLine("INFERNO-PRE-COMBAT-POSITIONS PASS: four full native room/exchange states/RNG; survivor28 remains at point2; pending/previously-removed deaths keep distinct old-point lifetimes; four immutable branches each.");
    }
    private static string Key(RoomCombatState room) => JsonSerializer.Serialize(
        room.WithContext(BattleTurnChecks.ComparableContext(room.Context)), ModelJson.Options);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
