using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ConditionalNoPlayerUnitChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.Spawn.Turn == 0)
        {
            if (source.Spawn.Train.Rooms[1].Units.Any(unit => unit.Team == CombatTeam.Player)) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                action.RoomIndex == 1 && source.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId ==
                "d14a50f3-728d-43e1-87f0-ef1b013f6678");
        }
        if (source.Spawn.Turn == 1) return null;
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(809);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10);
        CombatEffect status = new("CardEffectAddStatusEffectIfNoPlayerUnits", 0, 0, "", 0, [], false,
            action: new("AddStatus", "Self", 0, true, true, [new CombatStatus("armor", 3, stackable: true)],
                tests: new(true, false, false, false)));
        CombatTrigger trigger = new("PreCombat", false, false, false, 1, [status], false);
        CombatUnit enemy = new(1, "original-conditional-enemy", CombatTeam.Enemy, 0, 20, 20,
            false, false, false, [], [trigger], isBoss: false);
        CombatUnit player = new(2, "blocker", CombatTeam.Player, 0, 10, 10,
            false, false, false, [], [], isBoss: false);
        RoomCombatState blocked = new(0, false, [enemy, player], [], context);
        RoomCombatState open = new(0, false, [enemy], [], context);
        string parent = JsonSerializer.Serialize(blocked);
        RoomCombatResult blockedResult = RoomCombatModel.ApplyPreCombat(blocked, enemy.Id);
        RoomCombatResult openResult = RoomCombatModel.ApplyPreCombat(open, enemy.Id);
        Require(blockedResult.Supported && blockedResult.State!.Units.Single(unit => unit.Id == enemy.Id).Status("armor") == null &&
            openResult.Supported && openResult.State!.Units.Single().Status("armor")?.Stacks == 3,
            "No-player-unit status condition ignored live room occupancy.");

        CombatEffect kill = new("CardEffectDamage", 20, 0, "", 0, [], false,
            action: new("Damage", "Room", 20, false, true, []));
        CombatUnit chainedEnemy = enemy.WithTriggers([new CombatTrigger("PreCombat", false, false, false, 1,
            [kill, status], false)]);
        RoomCombatResult chained = RoomCombatModel.ApplyPreCombat(new RoomCombatState(0, false,
            [chainedEnemy, player], [], context), enemy.Id);
        Require(chained.Supported && chained.State!.Units.Single(unit => unit.Id == enemy.Id).Status("armor")?.Stacks == 3 &&
            chained.State.Units.All(unit => unit.Id != player.Id),
            "No-player-unit status condition used the pre-trigger room snapshot.");
        Require(JsonSerializer.Serialize(blocked) == parent, "Conditional status changed the parent branch.");
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(RoomCombatModel.ApplyPreCombat(open, enemy.Id).State) ==
            JsonSerializer.Serialize(openResult.State), "Parallel conditional status branches diverged."));
        Console.WriteLine("NO-PLAYER-UNITS-STATUS-CHECKS PASS: blocked, open, live prior kill and immutable parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "conditional-no-player-status" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Original conditional Armor battle did not finish with a complete match.");
        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 7, "Original conditional Armor battle did not record seven EndTurns.");
        BattleTurnState first = turns[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState open = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState blocked = turns[2].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState afterBlocked = turns[2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        EnemyDefinition[] originals = first.Spawn.Waves[0].Candidates.SelectMany(group => group.Units)
            .Where(unit => unit.Unit.AssetKey == "R4_Heavy_ArmorIfUnblocked_Ver2").ToArray();
        Require(originals.Length > 0 && originals.All(unit => unit.Unit.Triggers.Single(trigger =>
                trigger.Kind == "PostCombat").Effects.Single().Type == "CardEffectAddStatusEffectIfNoPlayerUnits"),
            "The native wave did not retain the original conditional Armor effect.");
        CombatUnit Unit(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.AssetKey == "R4_Heavy_ArmorIfUnblocked_Ver2");
        int Room(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room =>
            room.Units.Any(unit => unit.AssetKey == "R4_Heavy_ArmorIfUnblocked_Ver2")).RoomIndex;
        Require(Room(first) == 0 && first.Spawn.Train.Rooms[0].Units.All(unit => unit.Team != CombatTeam.Player) &&
            Unit(first).Status("armor") == null && Room(open) == 1 && Unit(open).Status("armor")?.Stacks == 50 &&
            Room(blocked) == 1 && blocked.Spawn.Train.Rooms[1].Units.Any(unit => unit.Team == CombatTeam.Player) &&
            Unit(blocked).Status("armor")?.Stacks == 50 && Room(afterBlocked) == 2 &&
            Unit(afterBlocked).Status("armor")?.Stacks == 50,
            "Original conditional Armor did not distinguish the open and blocked floors.");
        foreach (int index in new[] { 1, 2 })
        {
            BattleTurnState before = turns[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            BattleTurnResult predicted = BattleTurnModel.EndTurn(before);
            string? difference = predicted.State == null ? predicted.UnsupportedReason :
                ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
            Require(predicted.Supported && difference == null,
                "Original conditional Armor EndTurn " + index + " differs: " + difference);
        }
        Console.WriteLine("NATIVE-NO-PLAYER-UNITS-STATUS-CHECKS PASS: original enemy, open and blocked PostCombat, two exact EndTurns.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
