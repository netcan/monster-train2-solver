using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DeadCasterStatusChecks
{
    private const string Reason = "Spell damage source left the captured train before damage resolution.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        FixtureValue last = rows[^1];
        BattleTurnState before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(rows.Length == 30 && before.Spawn.Turn == 4 && action.CardInstanceId == 27 &&
            action.RoomIndex == 2 && action.TargetUnitId == 2 && action.ActivatorUnitId == 2,
            "Missing the original self-targeted champion ability boundary.");
        var effects = before.PlayRules!.Cards.Single(card => card.DataId == "c12a58a4-0bae-4120-9921-f93d91b20bcb").Effects;
        Require(effects.Count == 2 && effects[0].Type == "DamagePerSourceAttack" && effects[0].Target == "DropTargetCharacter" &&
            effects[1].Type == "AddStatus" && effects[1].Target == "Room" && effects[1].Statuses.Single().Id == "pyregel" &&
            effects[1].Statuses.Single().Stacks == 4, "Unexpected ability effect order.");
        CombatUnit caster = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 2);
        Require(caster.Health == 25 && caster.Attack == 31 &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != caster.Id),
            "The self-targeted attack must kill its caster before applying the room status.");
        foreach (int id in new[] { 10, 11 })
        {
            CombatUnit old = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            CombatUnit next = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            Require(next.Health == old.Health && next.Status("pyregel")!.Stacks == (old.Status("pyregel")?.Stacks ?? 0) + 4,
                "The surviving enemies must gain four Pyregel without direct damage.");
        }
        Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health ==
            before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health &&
            BattleActionModel.EnumerateActions(before).SupportedActions.Any(candidate =>
                JsonSerializer.Serialize(candidate) == JsonSerializer.Serialize(action)),
            "The legal self-targeted ability is still rejected or changed Pyre health.");
        CombatContext oldContext = before.Spawn.Train.Context!, nextContext = after.Spawn.Train.Context!;
        Require(nextContext.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == 12) &&
            nextContext.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.TakeLast(2).Select(card => card.InstanceId).SequenceEqual([7, 20]) &&
            nextContext.Statistics!.Value(12, "TimesExhausted") == oldContext.Statistics!.Value(12, "TimesExhausted"),
            "Ability completion must return current equipment and preserve the detached historical standby entry.");
        Console.WriteLine("DEAD-CASTER-STATUS PASS:30 native inputs/full state/RNG; self-targeted lethal damage then room Pyregel, immutable parents; component observation only.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay); NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("DEAD-CASTER-STATUS-PAIR PASS: original rejection preserved and repaired prediction reproduced in an independent native process.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
