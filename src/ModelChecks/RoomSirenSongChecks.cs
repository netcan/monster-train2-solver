using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomSirenSongChecks
{
    private const string Owner = "8ccceee7-0eb9-4aa0-9268-ff83b6f6c54e";
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains(Owner)) return;
        int moved = 0, bosses = 0, controls = 0;
        foreach (FixtureValue row in root.GetProperty("RoomPreCombatRecords").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete Siren Song room callback.");
            int index = row.GetProperty("RoomIndex").GetInt32();
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!,
                after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            Require(RoomPreCombatModel.IsSirenSong(before.Context!.RoomCapacities!.Single(room => room.RoomIndex == index)
                .Attachments!.Single().PreCombat!), "Missing unchanged Siren Song status/movement composition.");
            foreach (RoomCombatState room in before.Rooms)
            foreach (CombatUnit unit in room.Units)
            {
                RoomCombatState destination = after.Rooms.Single(item => item.Units.Any(actor => actor.Id == unit.Id));
                CombatUnit next = destination.Units.Single(actor => actor.Id == unit.Id);
                if (room.RoomIndex == index && unit.Team == CombatTeam.Enemy && unit.IsBoss != true && !unit.IsPyre)
                {
                    Require(destination.RoomIndex == index + 1 && next.Status("dazed")?.Stacks == (unit.Status("dazed")?.Stacks ?? 0) + 3 &&
                        next.Health == unit.Health, "Siren Song must daze ordinary enemies three before moving them up one room.");
                    moved++;
                }
                else
                {
                    Require(destination.RoomIndex == room.RoomIndex && Json(unit) == Json(next), "Siren Song changed an unrelated unit or boss.");
                    controls++;
                    if (unit.Team == CombatTeam.Enemy && unit.IsBoss == true) bosses++;
                }
            }
        }
        Require(moved > 0 && bosses > 0 && controls > 0, "Missing Siren Song movement, boss or unrelated-unit controls.");
        Console.WriteLine("ORIGINAL-SIREN-SONG PASS: " + moved + " three-stack dazes and one-floor moves, " + bosses + " untouched boss controls and " + controls + " unchanged controls; complete preview/primary replay.");
    }
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
