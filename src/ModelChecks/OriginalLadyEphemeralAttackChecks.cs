using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalLadyEphemeralAttackChecks
{
    private const string Modifier = "RoomStateSelfDamagePerStatusEffectStack";

    internal static void Native(FixtureValue source)
    {
        int matched = 0;
        var actors = new HashSet<int>();
        var stackCounts = new HashSet<int>();
        foreach (FixtureValue record in source.GetProperty("UnitSelfAttackQueries").EnumerateArray())
        {
            var room = record.GetProperty("Room").Deserialize<RoomCombatState>()!;
            foreach (FixtureValue query in record.GetProperty("Attacks").EnumerateArray())
            {
                var unit = room.Units.Single(actor => actor.Id == query.GetProperty("UnitId").GetInt32());
                if (unit.AssetKey != "LadyOfTheHouse") continue;
                int stacks = unit.Status("ephemeral")?.Stacks ?? 0;
                Require(unit.Modifiers?.SelfAttackRules is [{ Type: Modifier, Coefficient: 5, Divisor: 1, StatusId: "ephemeral" }],
                    "The original Lady of the House lost its authored Ephemeral attack modifier.");
                Require(UnitSelfAttackModel.Additional(unit, room.Context) == 5 * stacks,
                    $"Lady of the House modifier differs at {stacks} Ephemeral stacks.");
                int actual = query.GetProperty("Attack").GetInt32();
                int expected = RelicResourceModel.AttackDamage(unit, room.Context, room);
                Require(expected == actual,
                    $"Native Lady of the House attack differs: unit={unit.Id}, room={room.RoomIndex}, stacks={stacks}, expected={expected}, actual={actual}.");
                matched++;
                actors.Add(unit.Id);
                stackCounts.Add(stacks);
            }
        }
        Require(matched == 994 && actors.Count == 3 && stackCounts.SetEquals([1, 2, 3, 4, 5, 9, 10, 11]),
            "Missing the complete original Lady of the House query witness.");
        Console.WriteLine($"ORIGINAL-LADY-EPHEMERAL-ATTACK PASS: {matched} exact native attack getters across {actors.Count} actors and {stackCounts.Count} Ephemeral stack counts.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
