using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class SpurnedChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeOriginalSpurnedAttack" &&
            root.GetProperty("Scenario").GetString() == "SoulSavior_R4_BossBattle_EstrangedChild_2" &&
            root.GetProperty("BossKey").GetString() == "Boss_SoulSavior_R4_Spurned_ArmorPerDebuff" &&
            root.GetProperty("TestScenario").GetBoolean(), "Expected a controlled native Spurned witness.");

        CombatUnit before = Boss(root, "BeforeMalaise");
        CombatUnit after = Boss(root, "AfterMalaise");
        CombatUnit removed = Boss(root, "AfterRemoval");
        CombatStatus malaise = after.RegisteredStatus(StatusAttackDamageModel.Malaise)!;
        Require(malaise.Stacks == 2 && removed.RegisteredStatus(malaise.Id)?.Stacks == 0 &&
            before.MaxHealth - after.MaxHealth == 2 && after.Health == after.MaxHealth &&
            removed.MaxHealth == before.MaxHealth && removed.Health == after.Health,
            "The native Malaise witness must clip the boss and restore its upper limit without healing.");

        var room = new RoomCombatState(0, false, [before], []);
        RoomCombatResult added = StatusApplicationModel.ApplyRetained(room, before.Id, malaise, 0);
        Require(added.Supported, added.UnsupportedReason ?? "Malaise application failed.");
        CompareMalaise(added.State!.Units.Single(), after);
        RoomCombatResult clear = StatusRemovalModel.Remove(added.State, before.Id, malaise.Id, -1);
        Require(clear.Supported, clear.UnsupportedReason ?? "Malaise removal failed.");
        CompareMalaise(clear.State!.Units.Single(), removed);
        Console.WriteLine("NATIVE-SPURNED-MALAISE PASS: exact native stack, HP, maximum HP, and attack accumulator changes.");
    }

    private static CombatUnit Boss(FixtureValue root, string stage) =>
        root.GetProperty(stage).GetProperty("Boss").Deserialize<CombatUnit>()!;

    private static void CompareMalaise(CombatUnit modeled, CombatUnit native)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(modeled), JsonSerializer.Serialize(native));
        Require(difference == null, "Modeled Malaise actor differs from the native boss at " + difference);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
