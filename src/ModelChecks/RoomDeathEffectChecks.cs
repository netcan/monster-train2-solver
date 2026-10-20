using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomDeathEffectChecks
{
    private const string Owner = "e1a39d88-4a77-4399-98b2-0b81f44fe6a8";
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains(Owner)) return;
        int spawned = 0, stacked = 0, excluded = 0;
        foreach (FixtureValue row in root.GetProperty("RoomDeathEffects").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native room-death callback.");
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!, after = row.GetProperty("After").Deserialize<RoomCombatState>()!;
            Require(before.Context!.RoomCapacities!.Single(room => room.RoomIndex == before.RoomIndex).Attachments!
                .Single().DeathEffect!.Filters.Single().Subtypes.Excluded.SequenceEqual(new[] { "SubtypesData_FunGuy" }),
                "Infectory requires its authored Funguy exclusion filter.");
            if (actor.Subtypes.Contains("SubtypesData_FunGuy") || actor.IsPyre || actor.Health <= 0 && actor.IsBoss == true)
            {
                Require(Json(before) == Json(after), "Excluded Funguy/pyre/dead boss must preserve the complete room/context.");
                excluded++; continue;
            }
            CombatUnit[] old = before.Units.Where(unit => unit.AssetKey == "MonsterFunguy" && unit.Health > 0).ToArray(),
                next = after.Units.Where(unit => unit.AssetKey == "MonsterFunguy" && unit.Health > 0).ToArray();
            if (old.Length == 0)
            {
                Require(next.Length == 1 && next[0].SpawnerCardId == 0 && next[0].Status("cardless")?.Stacks == 1 &&
                    next[0].Status("horde")?.Stacks == 1 && next[0].Id == before.Context.NextUnitId &&
                    after.Context!.NextUnitId == before.Context.NextUnitId + 1,
                    "First eligible room death must create one independent cardless one-troop Funguy.");
                spawned++;
            }
            else
            {
                Require(next.Length == old.Length && next[0].Id == old[0].Id &&
                    next[0].Status("horde")!.Stacks == old[0].Status("horde")!.Stacks + 1 &&
                    after.Context!.NextUnitId == before.Context.NextUnitId,
                    "A later room death must add one troop to the existing first Funguy without a new identity.");
                stacked++;
            }
            Require(before.Context.CardInstances!.Select(card => card.InstanceId).SequenceEqual(after.Context!.CardInstances!.Select(card => card.InstanceId)),
                "Room death must preserve owned card identities.");
        }
        Require(excluded > 0 && spawned > 0 && (!originals.Contains("0c8ff108-478c-4c1c-9c49-f6a297b4e0d8") || stacked > 0),
            "Missing native first-spawn/existing-troop or exclusion witnesses.");
        Console.WriteLine("ORIGINAL-INFECTORY PASS: " + spawned + " first births, " + stacked + " same-identity troop additions and " + excluded + " excluded deaths; full captured preview/battle replay.");
    }
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
