using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptionStatusChecks
{
    internal static void Native(string path)
    {
        using var fixture = ModelJson.ReadFixture(path);
        FixtureValue root = fixture.RootElement;
        FixtureValue rule = root.GetProperty("NativeCorruptionStatusRule");
        Require(rule.GetProperty("Type").GetString() == "StatusEffectCorruptionState" &&
            rule.GetProperty("Stage").GetString() == "OnPostCombatPoison" && rule.GetProperty("Value").GetInt32() == 1 &&
            !rule.GetProperty("RemoveWhenTriggered").GetBoolean() && !rule.GetProperty("RemoveStackAtEnd").GetBoolean() &&
            !rule.GetProperty("RemoveAllAtEnd").GetBoolean(), "Native persistent corruption status definition changed.");
        FixtureValue stages = root.GetProperty("Stages");
        Require(stages.GetArrayLength() == 6, "Missing independent native corruption exchanges.");
        foreach (int offset in new[] { 0, 3 })
        {
            var before = stages[offset].GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit enemy = before.Units.Single(unit => unit.Status("corruption")?.Stacks == 20);
            var first = stages[offset + 1].GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var second = stages[offset + 2].GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit damaged = first.Units.Single(unit => unit.Id == enemy.Id);
            CombatUnit persisted = second.Units.Single(unit => unit.Id == enemy.Id);
            Require(damaged.Health - persisted.Health == 20 && persisted.Status("corruption")?.Stacks == 20,
                "Native corruption must deal 20 post-combat damage and retain its stacks between exchanges.");
            string parent = JsonSerializer.Serialize(before);
            RoomCombatResult resolved = RoomCombatModel.Resolve(before);
            Require(resolved.Supported && resolved.Events.Any(item => item.Kind == "Corruption" && item.Amount == 20) &&
                JsonSerializer.Serialize(before) == parent, "Corruption damage missing or source changed.");
        }
        RoomCombatChecks.NativeRoomOnly(path);
        BattleTurnChecks.Native(root);
        Console.WriteLine("CORRUPTION-STATUS PASS: native definition, two persistent damage witnesses, six room/exchange states and two EndTurns including full context/RNG.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
