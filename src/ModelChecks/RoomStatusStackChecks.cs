using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomStatusStackChecks
{
    internal static void Native(FixtureValue root)
    {
        int positive = 0, replaced = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string id = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId;
            if (id is not ("28875721-10df-4427-b9ef-4d237e89608e" or "479ceb1b-fc8c-420f-8fa0-17683cee4511")) continue;
            bool originalHell = root.GetProperty("Actions").EnumerateArray().Any(other =>
                other.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.FindCard(
                    other.GetProperty("Action").GetProperty("CardInstanceId").GetInt32())?.DataId == "5d34a8d1-be5c-44e0-bd7e-b6cd576df0bb");
            if (!originalHell) continue;
            RoomCombatState room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            bool active = before.Spawn.Train.Context.RoomCapacities!.Single(capacity => capacity.RoomIndex == action.RoomIndex)
                .Attachments!.Any(attachment => attachment.StatusStackBonuses?.SingleOrDefault()?.Amount == 3);
            CombatUnit[] targets = room.Units.Where(unit => id == "28875721-10df-4427-b9ef-4d237e89608e"
                ? unit.Id == action.TargetUnitId : unit.Team == CombatTeam.Enemy).ToArray();
            Require(targets.Length > 0, "Room stack witness requires live targets.");
            foreach (CombatUnit unit in targets)
            {
                CombatUnit next = after.Spawn.Train.Rooms.SelectMany(floor => floor.Units).Single(next => next.Id == unit.Id);
                int expected = (id == "28875721-10df-4427-b9ef-4d237e89608e" ? 10 : 4) + (active ? 3 : 0);
                Require((next.Status("pyregel")?.Stacks ?? 0) - (unit.Status("pyregel")?.Stacks ?? 0) == expected,
                    "Hellfurnace adds three Pyregel stacks to friendly and enemy targets only while attached.");
            }
            foreach (CombatUnit unit in before.Spawn.Train.Rooms.Where(floor => floor.RoomIndex != action.RoomIndex).SelectMany(floor => floor.Units))
                Require(after.Spawn.Train.Rooms.SelectMany(floor => floor.Units).Single(next => next.Id == unit.Id).Status("pyregel")?.Stacks == unit.Status("pyregel")?.Stacks,
                    "Room stack modifiers must preserve other floors.");
            if (active) positive++; else replaced++;
        }
        if (positive + replaced > 0) Console.WriteLine($"ORIGINAL-ROOM-STACKS PASS: {positive} positive casts, {replaced} replacement controls, independent native target deltas.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
