using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FoodSpawnChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains("13876e8d-b111-429d-acdb-2e095fda897e")) return;
        int meals = 0, controls = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string played = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId;
            if (played == "536e6de0-7ccf-412a-9a4e-ec55ab3988e5")
            {
                RoomCombatState room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
                CombatUnit made = room.Units.Single(unit => unit.AssetKey == "MonsterMorselMade");
                Require(room.Units.First(unit => unit.Team == CombatTeam.Player).Id != made.Id, "Self-feeder witness must differ from the ordinary front feeder.");
                CombatUnit fed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == made.Id);
                Require(fed.Attack == made.Attack + 13 && fed.MaxHealth == made.MaxHealth + 13 && fed.Health == made.Health + 13,
                    "Morsel-Made must receive the original Miner's +9/+9 and its own +4/+4 OnFeed before the play completes.");
                Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.SpawnerCardId != action.CardInstanceId) &&
                    after.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Any(card => card.InstanceId == action.CardInstanceId),
                    "The immediately eaten birth must leave live rooms and route its source to Eaten.");
                foreach (CombatUnit other in room.Units.Where(unit => unit.Id != made.Id))
                {
                    CombatUnit next = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == other.Id);
                    Require(next.Attack == other.Attack && next.MaxHealth == other.MaxHealth, "Self feeding must leave the ordinary front feeder and enemies unchanged.");
                }
                meals++;
            }
            if (played == "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6")
            {
                Require(after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.Any(unit => unit.SpawnerCardId == action.CardInstanceId),
                    "An original Morsel on another floor must remain alive after its birth.");
                controls++;
            }
        }
        Require(meals > 0 && controls > 0, "Missing positive self feeding or other-floor birth control.");
        Console.WriteLine($"ORIGINAL-FOOD-SPAWN PASS: {meals} immediate original meals behind another feeder, {controls} other-floor controls, native +9/+9 and +4/+4 growth and Eaten source routing.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
