using MonsterTrain2Poju.Model;

internal static class UnitStatusAbilityChecks
{
    internal static void Native(BattleTurnState before, BattleTurnState after, PlayCardAction action, string id)
    {
        CombatUnit actor = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.ActivatorUnitId);
        CombatUnit changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == actor.Id);
        var oldPlayers = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        var newPlayers = after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        Require(oldPlayers.Length == 2 && oldPlayers[1].Id == actor.Id && newPlayers[0].Id == actor.Id,
            "Original skill must move its activator ahead of an independent living ally.");
        if (id == "be65a126-831e-43c1-b987-698c37edead2")
            Require(actor.MaxHealth - actor.Health == 3 && changed.Health == changed.MaxHealth &&
                changed.Status("armor")?.Stacks == 3 && newPlayers[1].Status("armor") == null,
                "Courage must convert three missing HP to armor before fully healing, without upgrading its ally.");
        else
        {
            Require(changed.Status("valor")?.Stacks == 15 && changed.Status("armor")?.Stacks == 15,
                "Savior must gain fifteen Valor before converting its current Valor count to armor.");
            Require((newPlayers[1].Status("armor")?.Stacks ?? 0) == (id == "2479c736-4463-4346-8464-d517dae23f40" ? 15 : 0),
                "Savior's Self/Room variants must preserve their distinct ally scope.");
        }
        foreach (RoomCombatState room in before.Spawn.Train.Rooms.Where(room => room.RoomIndex != 0))
        foreach (CombatUnit unit in room.Units.Where(unit => unit.Team == CombatTeam.Player))
            Require(after.Spawn.Train.Rooms[room.RoomIndex].Units.Single(candidate => candidate.Id == unit.Id).Status("armor") == unit.Status("armor"),
                "Scaled ability statuses must preserve other-floor controls.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
