using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnstableExplosionChecks
{
    internal static void Native(FixtureValue root, int damagePerStack = 1)
    {
        var stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        foreach (FixtureValue roomStage in stages.Where(stage => stage.GetProperty("Kind").GetString() == "Room"))
        {
            RoomCombatState before = roomStage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit? carrier = before.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player &&
                unit.Status("unstable")?.Stacks > 0);
            CombatUnit? guard = before.Units.FirstOrDefault(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
            CombatUnit? mage = before.Units.FirstOrDefault(unit => unit.AssetKey == "MageT1_Junker");
            if (carrier == null || guard == null || mage == null) continue;
            int turn = roomStage.GetProperty("Turn").GetInt32();
            FixtureValue exchange = stages.Single(stage => stage.GetProperty("Turn").GetInt32() == turn &&
                stage.GetProperty("Kind").GetString() == "Exchange" &&
                stage.GetProperty("Before").GetProperty("RoomIndex").GetInt32() == before.RoomIndex);
            RoomCombatState afterExchange = exchange.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            RoomCombatState afterRoom = roomStage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit? guardAfterExchange = afterExchange.Units.FirstOrDefault(unit => unit.Id == guard.Id);
            CombatUnit? guardAfterRoom = afterRoom.Units.FirstOrDefault(unit => unit.Id == guard.Id);
            CombatUnit? carrierAfterExchange = afterExchange.Units.FirstOrDefault(unit => unit.Id == carrier.Id);
            int stacks = carrier.Status("unstable")!.Stacks;
            if (carrierAfterExchange?.Health > 0 && afterExchange.Units.Any(unit => unit.Id == mage.Id) &&
                afterRoom.Units.All(unit => unit.Id != carrier.Id && unit.Id != mage.Id) &&
                guardAfterExchange?.Status("armor")?.Stacks - guardAfterRoom?.Status("armor")?.Stacks == stacks * damagePerStack &&
                guardAfterRoom?.Health == guardAfterExchange.Health)
            {
                Console.WriteLine($"NATIVE-UNSTABLE-EXPLOSION PASS: turn {turn}, room {before.RoomIndex}, " +
                    $"carrier {carrier.Id} sacrificed, mage {mage.Id} killed, guard armor " +
                    $"{guardAfterExchange.Status("armor")!.Stacks}->{guardAfterRoom.Status("armor")!.Stacks}, damagePerStack={damagePerStack}.");
                return;
            }
        }
        throw new InvalidDataException("Native Unstable fixture lacks a same-room sacrifice and five-damage explosion witness.");
    }
}
