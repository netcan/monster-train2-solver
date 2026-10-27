using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InfernoRemovalQueueChecks
{
    internal static void Native(FixtureValue fixture)
    {
        int matched = 0;
        foreach (var boundary in new[] { (Turn: 3, Unit: 5, Card: 6), (Turn: 5, Unit: 27, Card: 9), (Turn: 6, Unit: 34, Card: 14) })
        foreach (string kind in new[] { "Room", "Exchange" })
        {
            var stage = fixture.GetProperty("Stages").EnumerateArray().Single(row =>
                row.GetProperty("Turn").GetInt32() == boundary.Turn && row.GetProperty("Kind").GetString() == kind &&
                row.GetProperty("Before").GetProperty("RoomIndex").GetInt32() == 1);
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            if (kind == "Exchange")
            {
                var pending = before.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == boundary.Unit);
                Require(pending is { Preview: false, RoomIndex: 1, Unit.Health: <= 0,
                    Unit.DeathState: { HasFinishedDying: true, HasStatisticsListener: false, IsDestroyed: false } } &&
                    pending.Unit.SpawnerCardId == boundary.Card && before.Units.All(unit => unit.Id != boundary.Unit) &&
                    before.Context.SpawnPoints!.Units.Single(point => point.UnitId == boundary.Unit).Current != null,
                    "Missing native pending physical removal and its already-completed death callbacks.");
            }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = kind == "Exchange" ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
                Require(result.Supported, result.UnsupportedReason ?? "Unsupported removal boundary.");
                string? difference = ModelJson.Difference(Key(result.State!), Key(actual));
                Require(difference == null, $"Native Inferno removal turn{boundary.Turn}/{kind}: {difference}");
            });
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Removal mutated its immutable parent.");
            var point = actual.Context!.SpawnPoints!.Units.Single(item => item.UnitId == boundary.Unit);
            Require(point.Current == null && point.LastKnown == null, "Native destruction retained a physical placement.");
            matched++;
        }
        Console.WriteLine($"INFERNO-REMOVAL-QUEUE PASS:{matched} full native room states/RNG; Endless return and two ordinary exhausts; four immutable branches each.");
    }

    private static string Key(RoomCombatState room) => JsonSerializer.Serialize(
        room.WithContext(BattleTurnChecks.ComparableContext(room.Context)), ModelJson.Options);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
