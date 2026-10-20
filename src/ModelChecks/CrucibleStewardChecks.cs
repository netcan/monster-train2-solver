using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CrucibleStewardChecks
{
    internal static void Native(FixtureValue root)
    {
        int positive = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            if (card.DataId != "2ed78ce2-13c3-4e82-963f-99c9ea12d0f0") continue;
            RoomCombatState room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex),
                changed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            CombatUnit observer = room.Units.Single(unit => unit.AssetKey == "MonsterCrucibleSteward");
            CombatUnit front = room.Units.First(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
            CombatUnit? survivor = changed.Units.SingleOrDefault(unit => unit.Id == front.Id);
            Require(CardExhaustModel.HasConsume(card) && observer.Attack == 15 &&
                (survivor?.Health ?? 0) + (survivor?.Status("armor")?.Stacks ?? 0) == Math.Max(0, front.Health + (front.Status("armor")?.Stacks ?? 0) - 15) &&
                changed.Units.Single(unit => unit.Id == observer.Id).Triggers.Single(trigger => trigger.Kind == "CardExhausted").HasTriggered &&
                room.Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Id != front.Id).All(unit =>
                    changed.Units.Single(next => next.Id == unit.Id).Health == unit.Health),
                "Crucible Steward must deal its fifteen source attack to only the front enemy when Rage Serum is consumed.");
            positive++;
        }
        Require(positive == 1, "Crucible Steward needs a positive natural Consume callback.");
        Console.WriteLine("ORIGINAL-CRUCIBLE-STEWARD PASS: native Consume callback deals fifteen source-attack damage to the front enemy and preserves other enemies.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
