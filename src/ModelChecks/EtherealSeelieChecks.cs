using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EtherealSeelieChecks
{
    private const string Owner = "02258ff7-457e-4de6-88cf-faff348801d7";
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains(Owner)) return;
        int births = 0, positive = 0, outside = 0, otherStatus = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext prior = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
            string played = prior.FindCard(action.CardInstanceId)!.DataId;
            if (played == Owner)
            {
                CombatUnit born = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == action.CardInstanceId);
                Require(action.RoomIndex == 0 && born.Status("conduit")?.Stacks == 5 &&
                    born.Modifiers!.RoomStatuses!.Single().Type == UnitRoomStatusModel.QueuedStatus &&
                    next.MagicPower!.Single(power => power.RoomIndex == 0 && power.Team == CombatTeam.Player).Value == 5,
                    "Ethereal Seelie must preserve five authored spawn stacks without queuing an extra stack.");
                births++;
            }
            if (played is "297c2618-1a10-4367-8b7c-d068a6c5b3f4" or "5656f5dd-e400-4e19-96d3-c9415820a4bc" or "2ed78ce2-13c3-4e82-963f-99c9ea12d0f0")
            {
                CombatUnit old = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId),
                    current = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == old.Id);
                int delta = (current.Status("conduit")?.Stacks ?? 0) - (old.Status("conduit")?.Stacks ?? 0);
                if (played == "297c2618-1a10-4367-8b7c-d068a6c5b3f4")
                { Require(action.RoomIndex == 0 && delta == 5, "One original Draught application must add four base stacks plus one queued room stack, with no recursive bonus."); positive++; }
                else if (played == "5656f5dd-e400-4e19-96d3-c9415820a4bc")
                { Require(action.RoomIndex == 1 && before.Energy > 0 && delta == 3 * before.Energy, "Other-floor Spike must add only its native three stacks per ember."); outside++; }
                else
                { Require(delta == 0 && (current.Status("buff")?.Stacks ?? 0) - (old.Status("buff")?.Stacks ?? 0) == 5, "A different status must not trigger the room Conduit bonus."); otherStatus++; }
                foreach (CombatUnit unit in before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Id != old.Id))
                    Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(next => next.Id == unit.Id).Status("conduit")?.Stacks == unit.Status("conduit")?.Stacks,
                        "Queued room status must preserve every other unit's Conduit stacks.");
            }
        }
        Require(births == 1 && positive == 1 && outside == 1 && otherStatus == 1,
            "Ethereal Seelie requires original birth, positive queue, other-floor and other-status native witnesses.");
        Console.WriteLine("ORIGINAL-ETHEREAL-SEELIE PASS: five spawn stacks, one nonrecursive queued bonus, off-floor and other-status controls, complete native Conduit tracker replay.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
