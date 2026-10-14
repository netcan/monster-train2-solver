using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OverhealChecks
{
    private const string TidalTurningId = "3c96e9b7-bdfb-4784-88f7-4b01bd58b4b7";
    private const string FaesBlessingId = "f7839c43-c139-4671-9f53-9347673e3b0b";

    internal static void Run()
    {
        CardActionEffect effect = new("HealOverheal", "Room", 5, false, true,
            [new CombatStatus("armor", 1)], overheal: new OverhealRule(2, 3));
        TrainCombatState Train(int health, bool immune = false)
        {
            var rng = UnityRng.Seed(819);
            var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 10, 10);
            var unit = new CombatUnit(1, "steward", CombatTeam.Player, 5, health, 10, true, false, false,
                immune ? [new CombatStatus("heal immunity", 1)] : [],
                modifiers: new UnitModifiers(5, 0, 0, 1, 1, true, false, []));
            return new TrainCombatState([new RoomCombatState(0, false, [unit], [], context),
                new RoomCombatState(1, false, [], [], context)], [], 1, context);
        }
        CombatUnit Result(TrainCombatState state) => state.Rooms[0].Units.Single();
        TrainCombatState parent = Train(8);
        string unchanged = JsonSerializer.Serialize(parent);
        TrainSpellResult excess = CardSpellModel.Apply(parent, 0, [effect], 0);
        Require(excess.Supported && excess.State != null, "Overheal rejected a valid target: " + excess.UnsupportedReason);
        CombatUnit unit = Result(excess.State!);
        Require(unit.Health == 19 && unit.MaxHealth == 19 && unit.BaseAttack == 11 && unit.Status("armor")?.Stacks == 3,
            "Overheal did not apply pre-heal excess to attack, max health and armor.");
        CardActionEffect statsOnly = new("HealOverheal", "Room", 5, false, true, [],
            overheal: new OverhealRule(2, 3));
        TrainSpellResult withoutStatuses = CardSpellModel.Apply(parent, 0, [statsOnly], 0);
        Require(withoutStatuses.Supported && Result(withoutStatuses.State!).Health == 19 &&
            Result(withoutStatuses.State!).BaseAttack == 11 && Result(withoutStatuses.State!).Status("armor") == null,
            "Stat-only overheal incorrectly required a status definition.");
        TrainSpellResult exact = CardSpellModel.Apply(Train(5), 0, [effect], 0);
        Require(exact.Supported && Result(exact.State!).Health == 10 && Result(exact.State!).MaxHealth == 10 &&
            Result(exact.State!).BaseAttack == 5 && Result(exact.State!).Status("armor") == null,
            "Exact healing incorrectly generated overheal rewards.");
        TrainSpellResult blocked = CardSpellModel.Apply(Train(8, immune: true), 0, [effect], 0);
        Require(blocked.Supported && Result(blocked.State!).Health == 8 && Result(blocked.State!).MaxHealth == 10 &&
            Result(blocked.State!).Status("armor") == null, "Heal immunity failed to skip overheal.");
        Require(JsonSerializer.Serialize(parent) == unchanged, "Overheal mutated its parent branch.");
        string expected = JsonSerializer.Serialize(excess.State);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(parent, 0, [effect], 0).State) == expected,
            "Parallel overheal branches diverged."));
        Console.WriteLine("OVERHEAL-CHECKS PASS: excess, stat-only reward, exact heal, immunity and parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "overheal" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native overheal battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 2, "Expected both original overheal actions.");
        for (int index = 0; index < 2; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            string expectedId = index == 0 ? TidalTurningId : FaesBlessingId;
            int expectedArmor = index == 0 ? 10 : 5;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == expectedId);
            CombatUnit source = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.SpawnerCardId > 0 && unit.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal));
            CombatUnit target = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == source.Id);
            Require(before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == expectedId &&
                rule.Effects.Count == 1 && rule.Effects[0].Type == "HealOverheal" &&
                rule.Effects[0].Value == expectedArmor && rule.Effects[0].Statuses.Single().Id == "armor" &&
                rule.Effects[0].Statuses.Single().Stacks == 1 && rule.Effects[0].Overheal?.AttackPerPoint == 0 &&
                rule.Effects[0].Overheal?.MaxHealthPerPoint == 0 &&
                (index == 0 ? rule.Effects[0].Target == "Room" :
                    rule.Effects[0].Target == "DropTargetCharacter" && action.TargetUnitId == source.Id),
                "Marked action is not the expected original overheal effect.");
            BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
            string? difference = predicted.State == null ? predicted.Reason :
                ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
            Require(predicted.Supported && source.Health == source.MaxHealth &&
                target.Status("armor")?.Stacks == (source.Status("armor")?.Stacks ?? 0) + expectedArmor &&
                difference == null,
                "Original overheal action differs from the native replay: " + difference);
        }
        Console.WriteLine("NATIVE-OVERHEAL-CHECKS PASS: room and targeted original cards gave ten and five armor.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
