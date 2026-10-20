using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomAbilityChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        string[] roomCards = ["8d65e688-d93d-4bfa-975d-8c5f046fe2db", "8fbd9c3b-f027-4fdb-9a14-1ddae9c24d60", "9d18f2c2-1141-494d-943c-6ea6e13ff509", "e3f252bc-ed72-4b8d-8e7a-bb9e07d449ed"];
        if (!originals.Any(roomCards.Contains)) return;
        var witnessed = new HashSet<string>();
        int casts = 0, resets = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (!action.ActivatorRoomIndex.HasValue) continue;
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext old = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
            RoomCapacityAttachmentState attachment = old.RoomCapacities!.Single(room => room.RoomIndex == action.ActivatorRoomIndex).Attachments!.Single();
            RoomAbilityState ability = attachment.Ability!, actual = next.RoomCapacities!.Single(room => room.RoomIndex == action.ActivatorRoomIndex).Attachments!.Single().Ability!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == ability.DataId);
            CardInstanceState card = next.FindCard(action.CardInstanceId)!;
            Require(ability.Cooldown == 0 && actual.Cooldown == ability.CooldownAfterActivated && actual.Cooldown > 0 &&
                card.PlayCount == 0 && card.LastPlayedCost == rule.Cost && before.Energy - after.Energy == rule.Cost &&
                next.CardInstances!.All(owned => owned.InstanceId != card.InstanceId) &&
                next.AbilityCardCache!.Any(entry => entry.InstanceId == card.InstanceId) && next.LastAbilityActivatorUnitId == 0,
                "Room skill cooldown/payment/cache ownership or room/unit activator identity differs.");
            Require(BattleActionModel.PlayCard(after, action).Rejection == ActionRejection.Illegal,
                "A cooling room skill remained usable.");
            if (attachment.DataId == roomCards[2])
            {
                CombatUnit clone = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id >= old.NextUnitId);
                Require(clone.Modifiers?.IsClone == true && after.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == clone.Id)).RoomIndex == action.RoomIndex - 1,
                    "Assembly Room must positively clone its front unit and descend that new copy one floor.");
            }
            if (attachment.DataId == roomCards[3])
                Require(next.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Corruption ==
                    old.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Corruption + 4,
                    "Echo Bloom must positively add its original four Corruption.");
            if (attachment.DataId == roomCards[0])
                Require(before.Spawn.Train.Rooms[action.RoomIndex].Units.Where(unit => unit.Team == CombatTeam.Enemy).Any(unit =>
                    after.Spawn.Train.Rooms.Any(room => room.RoomIndex == action.RoomIndex - 2 && room.Units.Any(moved => moved.Id == unit.Id && moved.Status("dazed")?.Stacks == 2))),
                    "Trap Chute needs a living enemy positively dazed and descended two floors.");
            if (attachment.DataId == roomCards[1])
                Require(action.RoomIndex != action.ActivatorRoomIndex && after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.ActivatorRoomIndex)
                    .Units.Any(unit => unit.Id == action.TargetUnitId), "Steel Pulley Claw must pull its remote target to the activating room.");
            witnessed.Add(attachment.DataId); casts++;
        }
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            if (after.Spawn.Train.Context!.AllScenarioBossesDead == true) continue;
            foreach (RoomCapacityState room in before.Spawn.Train.Context!.RoomCapacities!)
            foreach (RoomCapacityAttachmentState attachment in room.Attachments ?? [])
            {
                if (attachment.Ability?.Cooldown is not > 0) continue;
                RoomAbilityState next = after.Spawn.Train.Context.RoomCapacities!.Single(current => current.RoomIndex == room.RoomIndex).Attachments!.Single().Ability!;
                Require(next.Cooldown == attachment.Ability.Cooldown - 1, "Room ability cooldown must decrease exactly once per ended turn.");
                resets++;
            }
        }
        Require(originals.Where(roomCards.Contains).All(witnessed.Contains) && resets > 0, "Missing positive original room skill or cooldown-reset witnesses.");
        Console.WriteLine($"ORIGINAL-ROOM-ABILITIES PASS: {witnessed.Count} original room skills, {casts} activations, {resets} native cooldown transitions; positive effects, payment, non-owned cache identities and cooling rejection.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
