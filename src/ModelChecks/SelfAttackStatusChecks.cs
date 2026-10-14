using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SelfAttackStatusChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.Spawn.Turn == 0)
        {
            if (source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Player)) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                action.RoomIndex == 0 && source.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId ==
                "d14a50f3-728d-43e1-87f0-ef1b013f6678");
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(817);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10);
        CombatEffect effect = new("CardEffectAddStatusEffectPerSelfAttack", 2, 0, "", 0, [], false,
            action: new("AddStatus", "FrontInRoom", 2, false, true,
                [new CombatStatus("armor", 0, stackable: true)], tests: new(true, false, false, false)));
        CombatTrigger trigger = new("PreCombat", false, false, false, 1, [effect], false);
        CombatUnit actor = new(2, "weldling", CombatTeam.Enemy, 6, 3, 3,
            true, false, false, [], [trigger], isBoss: false);
        CombatUnit front = new(1, "front", CombatTeam.Player, 1, 10, 10,
            true, false, false, [], [], isBoss: false);
        RoomCombatState room = new(0, false, [front, actor], [], context);
        string parent = JsonSerializer.Serialize(room);
        RoomCombatResult result = RoomCombatModel.ApplyPreCombat(room, actor.Id);
        Require(result.Supported && result.State!.Units.Single(unit => unit.Id == front.Id).Status("armor")?.Stacks == 3 &&
            result.State.Units.Single(unit => unit.Id == actor.Id).Status("armor") == null,
            "Self-attack status did not use the live actor attack and selected front target.");

        CombatUnit buffed = new(2, "weldling", CombatTeam.Enemy, 6, 3, 3,
            true, false, false, [new CombatStatus("buff", 4, paramInt: 1)], [trigger], isBoss: false);
        RoomCombatResult buffResult = RoomCombatModel.ApplyPreCombat(new RoomCombatState(0, false,
            [front, buffed], [], context), actor.Id);
        Require(buffResult.Supported && buffResult.State!.Units.Single(unit => unit.Id == front.Id).Status("armor")?.Stacks == 5,
            "Self-attack status ignored current attack buffs.");

        CombatEffect zeroDivisor = new("CardEffectAddStatusEffectPerSelfAttack", 0, 0, "", 0, [], false,
            action: new("AddStatus", "FrontInRoom", 0, false, true,
                [new CombatStatus("armor", 0, stackable: true)], tests: new(true, false, false, false)));
        RoomCombatResult zeroResult = RoomCombatModel.ApplyPreCombat(new RoomCombatState(0, false,
            [front, actor.WithTriggers([new CombatTrigger("PreCombat", false, false, false, 1, [zeroDivisor], false)])],
            [], context), actor.Id);
        Require(zeroResult.Supported && zeroResult.State!.Units.Single(unit => unit.Id == front.Id).Status("armor")?.Stacks == 6,
            "Zero divisor did not use the native divisor-one fallback.");
        Require(JsonSerializer.Serialize(room) == parent, "Self-attack status changed the parent branch.");
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(RoomCombatModel.ApplyPreCombat(room, actor.Id).State) ==
            JsonSerializer.Serialize(result.State), "Parallel self-attack status branches diverged."));
        Console.WriteLine("SELF-ATTACK-STATUS-CHECKS PASS: front target, live attack buff, zero divisor and immutable parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "self-attack-status" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Original Weldling battle did not finish with a complete match.");
        FixtureValue[] turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 7, "Original Weldling battle did not record seven EndTurns.");
        BattleTurnState before = turns[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        EnemyDefinition[] originals = before.Spawn.Waves[0].Candidates.SelectMany(group => group.Units)
            .Where(unit => unit.Unit.AssetKey == "MonsterWeldling").ToArray();
        Require(originals.Length > 0 && originals.All(unit =>
        {
            CombatEffect effect = unit.Unit.Triggers.Single(trigger => trigger.Kind == "PostCombat").Effects.Single();
            return effect.Type == "CardEffectAddStatusEffectPerSelfAttack" && effect.Action?.Type == "AddStatus" &&
                effect.Action.Target == "FrontInRoom" && effect.Action.AllowPlayer && !effect.Action.AllowEnemy &&
                effect.Action.Value == 2 && effect.Action.Statuses.Single().Id == "armor" &&
                effect.Action.Statuses.Single().Stacks == 0;
        }), "The native wave did not retain the original Weldling status effect.");
        CombatUnit steward(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.AssetKey == "TrainStewardBig");
        Require(before.Spawn.Train.Rooms[0].Units.Any(unit => unit.AssetKey == "MonsterWeldling") &&
            steward(before).Status("armor") == null && steward(actual).Status("armor")?.Stacks == 3 &&
            actual.Spawn.Train.Rooms[1].Units.Any(unit => unit.AssetKey == "MonsterWeldling"),
            "Original Weldling did not add three Armor to the player's front unit before ascending.");
        BattleTurnResult predicted = BattleTurnModel.EndTurn(before);
        string? difference = predicted.State == null ? predicted.UnsupportedReason :
            ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
        Require(predicted.Supported && difference == null, "Original Weldling EndTurn differs: " + difference);
        Console.WriteLine("NATIVE-SELF-ATTACK-STATUS-CHECKS PASS: unchanged Weldling, 6/2 Armor on player front, exact EndTurn.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
