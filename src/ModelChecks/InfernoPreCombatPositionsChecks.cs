using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InfernoPreCombatPositionsChecks
{
    internal static void Native(FixtureValue fixture)
    {
        foreach (int index in new[] { 42, 43 })
        {
            var stage = fixture.GetProperty("Stages")[index];
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Require(before.RoomIndex == 2 && !before.Preview && actual.Units.Single(unit => unit.Team == CombatTeam.Player) is
                { Id: 28, AssetKey: "MonsterTruffles", Health: 5 } &&
                actual.Context!.SpawnPoints!.Units.Single(point => point.UnitId == 28).Current!.Index == 2,
                "Missing native survivor and two uncompressed vacancies.");
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = index == 42 ? RoomCombatModel.Resolve(before) : RoomCombatModel.Exchange(before);
                Require(result.Supported, result.UnsupportedReason ?? "Unsupported pre-combat positions.");
                Require(Key(result.State!) == Key(actual),
                    "Inferno partial-survival stage" + index + ": " + ModelJson.Difference(Key(result.State!), Key(actual)));
            });
            Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Pre-combat positions changed their parent.");
        }
        Console.WriteLine("INFERNO-PRE-COMBAT-POSITIONS PASS: two full native room/exchange states/RNG; survivor28 remains at point2; four immutable branches each.");
    }
    private static string Key(RoomCombatState room) => JsonSerializer.Serialize(
        room.WithContext(BattleTurnChecks.ComparableContext(room.Context)), ModelJson.Options);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
