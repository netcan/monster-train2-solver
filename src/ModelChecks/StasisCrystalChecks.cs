using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StasisCrystalChecks
{
    internal static void Native(FixtureValue root, int[] ids)
    {
        const string growth = "b16feaf0-bc18-438e-b7c4-859aca76c420";
        int positive = 0, empty = 0;
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit a = Host(before), b = Host(after);
            if (a.EquipmentCards?.Contains(ids[4]) != true) continue;
            int count = a.Status("untouchable")?.Stacks ?? 0;
            Require(b.BaseAttack - a.BaseAttack == 20 * count && b.MaxHealth - a.MaxHealth == 20 * count &&
                b.Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == growth) - a.Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == growth) == count,
                "Stasis must apply one native +20 attack/+20 health upgrade per current Untouchable stack, and none after removing that status.");
            CardInstanceState oldSpawner = before.Spawn.Train.Context!.FindCard(ids[0])!, newSpawner = after.Spawn.Train.Context!.FindCard(ids[0])!;
            Require(newSpawner.Temporary.Upgrades.Count(upgrade => upgrade.DataId == growth) - oldSpawner.Temporary.Upgrades.Count(upgrade => upgrade.DataId == growth) == count,
                "Stasis growth must persist on its spawner for the rest of the battle.");
            Require(b.Triggers.Single(trigger => trigger.Kind == "PostCombat" && trigger.Origin?.EquipmentCardId == ids[4]).HasTriggered,
                "Both positive and zero-stack Stasis controls must execute the native PostCombat trigger.");
            foreach (int room in new[] { 1, 2 })
                Require(after.Spawn.Train.Rooms.Single(floor => floor.RoomIndex == room).Units.Single(unit => unit.SpawnerCardId == ids[room]).BaseAttack == 1000,
                    "Stasis must leave other floor hosts' attack unchanged.");
            if (count > 0) positive++; else empty++;
        }
        FixtureValue activation = root.GetProperty("Actions").EnumerateArray().Single(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.ActivatorUnitId != 0);
        BattleTurnState prior = activation.GetProperty("Before").Deserialize<BattleTurnState>()!, next = activation.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = activation.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.ActivatorUnitId == Host(prior).Id && prior.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == "91fc348d-5b7a-4114-ad4b-a234b3dfd8d1" &&
            Host(prior).Status("untouchable")?.Stacks == 1 && Host(next).Status("untouchable") == null && Host(next).Ability?.HasAbility != true &&
            Host(next).BaseAttack == Host(prior).BaseAttack && Host(next).MaxHealth == Host(prior).MaxHealth,
            "The original Stasis ability must remove Untouchable and itself while preserving accumulated growth.");
        Require(positive > 0 && empty > 0, "Stasis requires positive and zero-stack native callbacks around its real ability activation.");
        Console.WriteLine($"ORIGINAL-STASIS-CRYSTAL PASS: {positive} growth callbacks, {empty} zero-stack callbacks, original ability removal and spawner/floor controls.");
        CombatUnit Host(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.SpawnerCardId == ids[0]);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
