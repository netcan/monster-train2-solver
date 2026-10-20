using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitRoomTriggerCountChecks
{
    internal static void Native(FixtureValue root)
    {
        bool positive = false, outside = false, reset = false, doubledUpgrade = false, singleUpgrade = false;
        int checks = 0;
        foreach (FixtureValue record in root.GetProperty("UnitRoomTriggerCountQueries").EnumerateArray())
        {
            RoomCombatState room = record.GetProperty("Room").Deserialize<RoomCombatState>()!;
            foreach (FixtureValue query in record.GetProperty("Queries").EnumerateArray())
            {
                CombatUnit unit = room.Units.Single(unit => unit.Id == query.GetProperty("UnitId").GetInt32());
                var trigger = new CombatTrigger("OnMoonPhaseShift", false, false, false, 1, [], noCountModifiersAllowed: false);
                int expected = TriggerCountModel.Query(room.Context, unit, trigger, out string? error,
                    UnitRoomTriggerCountModel.Modifier(room.Units, unit.Team, trigger.Kind));
                Require(error == null && query.GetProperty("Count").GetInt32() == expected, "Independent native room trigger count query differs.");
                outside |= room.RoomIndex == 1 && unit.AssetKey == "MonsterBattleDancer" && expected == 1;
                checks++;
            }
        }
        void Observe(BattleTurnState state)
        {
            if (state.Spawn.Train.Context!.AllScenarioBossesDead == true) return;
            bool ownerAlive = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Health > 0 && unit.Modifiers?.RoomTriggerCounts?.Count > 0);
            foreach (RoomCombatState room in state.Spawn.Train.Rooms)
            foreach (CombatUnit unit in room.Units.Where(unit => unit.Health > 0 && unit.Team == CombatTeam.Player))
            foreach (CombatTrigger trigger in unit.Triggers.Where(trigger => trigger.Kind == "OnMoonPhaseShift"))
            {
                int modifier = UnitRoomTriggerCountModel.Modifier(room.Units, unit.Team, trigger.Kind);
                Require(trigger.FireCount == 1 + modifier && trigger.NoCountModifiersAllowed == false,
                    "Native trigger fire counts must use the current living owner on the same floor.");
                positive |= unit.AssetKey == "MonsterAstrologer" && trigger.FireCount == 2;
                outside |= ownerAlive && modifier == 0 && trigger.FireCount == 1;
                reset |= !ownerAlive && unit.AssetKey == "MonsterAstrologer" && trigger.FireCount == 1;
                checks++;
            }
        }
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray()) Observe(row.GetProperty("Actual").Deserialize<BattleTurnState>()!);
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Observe(before); Observe(after);
            CombatUnit? prior = before.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.AssetKey == "MonsterAstrologer"),
                next = after.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.AssetKey == "MonsterAstrologer");
            if (prior != null && next != null)
            {
                doubledUpgrade |= next.Attack - prior.Attack == 10;
                singleUpgrade |= reset && next.Attack - prior.Attack == 5;
            }
        }
        Require(positive && outside && reset && doubledUpgrade && singleUpgrade,
            "Mooncaller needs positive same-floor doubling, an outside-floor control and a post-death return to one callback.");
        Console.WriteLine($"ORIGINAL-MOONCALLER PASS: {checks} native trigger counts, doubled +10 Attack, outside-floor control and post-death +5 reset.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
