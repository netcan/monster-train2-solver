using MonsterTrain2Poju.Model;

internal static class RelicRoomEntryStatusChecks
{
    internal static void Run()
    {
        CombatStatus armor = new("armor", 0);
        CombatStatus shield = new("damage shield", 2);
        CombatUnit player = Unit(CombatTeam.Player);
        CombatUnit enemy = Unit(CombatTeam.Enemy);
        CombatRelicState relic = Relic([new(0, 2, true, false, false, [armor, shield])]);
        RoomCombatState room = Room(2, player, relic);
        RngDraw expected = room.Context!.BattleRng.Range(0, 2);
        var generated = new List<RoomCombatModel.QueuedCharacterTrigger>();
        RoomCombatResult entered = RoomCombatModel.ApplyQueuedCharacterTrigger(room,
            new(2, player, "PostAscension"), generated.Add);
        Require(entered.Supported, entered.UnsupportedReason ?? "Post-ascension relic status failed.");
        CombatStatus expectedStatus = expected.Value == 0 ? armor : shield;
        CombatUnit enteredPlayer = entered.State!.Units.Single(unit => unit.Id == player.Id);
        Require(entered.State.Context!.BattleRng.Equals(expected.State) &&
            enteredPlayer.Status(expectedStatus.Id)?.Stacks == Math.Max(1, expectedStatus.Stacks) &&
            enteredPlayer.Status(expected.Value == 0 ? shield.Id : armor.Id) == null,
            "Post-ascension entry did not select one native status or advance Battle RNG.");

        CombatRelicState heroesRelic = Relic([new(0, 2, false, true, false, [armor])]);
        RoomCombatResult enteredHeroes = RoomCombatModel.ApplyQueuedCharacterTrigger(Room(2, enemy, heroesRelic),
            new(2, enemy, "PostAscension"), _ => { });
        Require(enteredHeroes.Supported && enteredHeroes.State!.Units.Single().Status("armor")?.Stacks == 1,
            "A native Heroes target was not mapped to model enemy-team units.");

        CombatRelicState single = Relic([new(0, 2, true, false, false, [armor])]);
        RoomCombatResult zeroStacks = RoomCombatModel.ApplyQueuedCharacterTrigger(Room(2, player, single),
            new(2, player, "AfterSpawnEnchant"), _ => { });
        Require(zeroStacks.Supported && zeroStacks.State!.Units.Single().Status("armor")?.Stacks == 1,
            "A nonpositive native room-entry status count was not normalized to one.");

        foreach (var skipped in new[]
        {
            (Room(1, player, single), new RoomCombatModel.QueuedCharacterTrigger(1, player, "PostAscension")),
            (Room(2, enemy, single), new RoomCombatModel.QueuedCharacterTrigger(2, enemy, "PostAscension")),
            (Room(2, player, single), new RoomCombatModel.QueuedCharacterTrigger(2, player, "OnShift")),
            (new RoomCombatState(2, false, [player], [], Room(2, player, single).Context, preview: true),
                new RoomCombatModel.QueuedCharacterTrigger(2, player, "PostAscension")),
            (Room(2, Unit(CombatTeam.Player, new CombatStatus("untouchable", 1)), single),
                new RoomCombatModel.QueuedCharacterTrigger(2,
                    Unit(CombatTeam.Player, new CombatStatus("untouchable", 1)), "PostAscension"))
        })
        {
            RoomCombatResult ignored = RoomCombatModel.ApplyQueuedCharacterTrigger(skipped.Item1, skipped.Item2, _ => { });
            Require(ignored.Supported && ignored.State!.Context!.BattleRng.Equals(skipped.Item1.Context!.BattleRng) &&
                ignored.State.Units.All(unit => unit.Status("armor") == null),
                "A room/team/event/preview/untouchable gate applied status or consumed Battle RNG.");
        }

        CombatRelicState ordered = new("ordered", "ordered",
            [RelicModel.AddStatusOnRoomEntry, RelicModel.AddStatusOnRoomEntry],
            roomEntryStatuses: [new(0, 2, true, false, false, [new("armor", 1)]),
                new(1, 2, true, false, false, [new("armor", 2)])]);
        RoomCombatState orderedRoom = Room(2, player, ordered);
        UnityRng twice = orderedRoom.Context!.BattleRng.Next().Next();
        RoomCombatResult stacked = RoomCombatModel.ApplyQueuedCharacterTrigger(orderedRoom,
            new(2, player, "PostAscension"), _ => { });
        Require(stacked.Supported && stacked.State!.Context!.BattleRng.Equals(twice) &&
            stacked.State.Units.Single().Status("armor")?.Stacks == 3,
            "Effect order, stacking, or one RNG draw per matching effect differed.");

        CombatRelicState firstWaveOnly = Relic([new(0, 2, true, false, true, [armor])]);
        Require(!RoomCombatModel.ApplyQueuedCharacterTrigger(Room(2, player, firstWaveOnly),
            new(2, player, "PostAscension"), _ => { }).Supported,
            "An uncaptured first-wave phase was silently modeled.");
        CombatRelicState missing = new("missing", "missing", [RelicModel.AddStatusOnRoomEntry]);
        Require(!RoomCombatModel.ApplyQueuedCharacterTrigger(Room(2, player, missing),
            new(2, player, "PostAscension"), _ => { }).Supported,
            "Missing ordered native room-entry parameters were guessed.");

        Console.WriteLine("RELIC-ROOM-ENTRY-STATUS-CHECKS PASS: PostAscension/AfterSpawnEnchant, Heroes/Monsters team mapping, status choice and Battle RNG, floor/event/preview/untouchable gates, stack normalization, effect order, fail-closed first-wave metadata.");
    }

    private static CombatUnit Unit(CombatTeam team, params CombatStatus[] statuses) =>
        new(1, team == CombatTeam.Player ? "player" : "enemy", team, 2, 20, 20, true, false, false,
            statuses, statusRegistry: statuses, statusDictionary: new([], []));

    private static CombatRelicState Relic(IReadOnlyList<RelicRoomEntryStatus> rules) =>
        new("entry", "entry", [RelicModel.AddStatusOnRoomEntry], roomEntryStatuses: rules);

    private static RoomCombatState Room(int roomIndex, CombatUnit actor, CombatRelicState relic) =>
        new(roomIndex, false, [actor], [], new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13),
            0, 1, 10, relics: [relic]));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
