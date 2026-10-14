using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonPhaseTransitionChecks
{
    private const string MoonriseId = "fe6dfafa-9712-484c-9bb4-145c0989e667";

    internal static void Run()
    {
        TrainCombatState Train(bool tracking, bool callback = false)
        {
            UnityRng rng = UnityRng.Seed(641);
            var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 10, 10,
                queryFrame: new StatisticQueryFrame(energy: 3, runningCombat: true, turn: 1,
                    moonPhase: 1, moonPhaseTrackingActive: tracking),
                energyState: new BattleEnergyState(10, 0, 0, "MonsterTurn", true), relics: []);
            var trigger = new CombatTrigger("OnMoonPhaseShift", false, false, false, 1, []);
            var unit = new CombatUnit(1, "steward", CombatTeam.Player, 8, 25, 25, true, false, false,
                [], callback ? [trigger] : [], modifiers: new UnitModifiers(8, 0, 0, 2, 1, true, false, []));
            if (tracking) unit = MoonPhaseStatusModel.WithPhase(unit, 1);
            return new TrainCombatState([new RoomCombatState(0, false, [unit], [], context)], [], 1, context);
        }
        CardActionEffect Effect(string type, int value) => new(type, "Room", value, false, false, []);
        TrainCombatState source = Train(true);
        string parent = JsonSerializer.Serialize(source);
        MoonPhaseTransitionResult full = MoonPhaseTransitionModel.Apply(source, Effect("SetMoonPhase", 2));
        Require(full.Supported && full.State!.Context!.QueryFrame!.MoonPhase == 2 &&
            full.State.Rooms[0].Units[0].Status("fullmoon")?.Stacks == 1 &&
            full.State.Rooms[0].Units[0].Status("newmoon") == null,
            "Setting Full Moon did not update the active unit and shared phase.");
        MoonPhaseTransitionResult twice = MoonPhaseTransitionModel.Apply(source, Effect("AdvanceMoonPhase", 2));
        Require(twice.Supported && twice.State!.Context!.QueryFrame!.MoonPhase == 1 &&
            twice.State.Rooms[0].Units[0].Status("newmoon")?.Stacks == 1,
            "Two native phase advances did not return to New Moon.");
        MoonPhaseTransitionResult inactive = MoonPhaseTransitionModel.Apply(Train(false), Effect("SetMoonPhase", 2));
        Require(inactive.Supported && inactive.State!.Context!.QueryFrame!.MoonPhase == 2 &&
            inactive.State.Rooms[0].Units[0].Status("fullmoon") == null,
            "Inactive tracking incorrectly applied moon statuses.");
        Require(!MoonPhaseTransitionModel.Apply(Train(true, callback: true), Effect("SetMoonPhase", 2)).Supported &&
            JsonSerializer.Serialize(source) == parent, "Moon callbacks were silently skipped or parent branch changed.");
        string expected = JsonSerializer.Serialize(full.State);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(
            MoonPhaseTransitionModel.Apply(source, Effect("SetMoonPhase", 2)).State) == expected,
            "Parallel moon-phase branches diverged."));
        Console.WriteLine("MOON-PHASE-TRANSITION-CHECKS PASS: set, repeated advance, inactive tracking, callback guard and branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "moon-phase-card" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native Moonrise battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 1, "Expected one original Moonrise action.");
        BattleTurnState before = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == MoonriseId);
        BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
        string? difference = predicted.State == null ? predicted.Reason :
            ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
        Require(before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == MoonriseId &&
            before.MoonPhaseTrackingActive == true && before.MoonPhase == "New" &&
            rule.Effects.Count == 2 && rule.Effects[0].Type == "SetMoonPhase" && rule.Effects[0].Value == 2 &&
            rule.Effects[1].Type == "GainEnergy" && rule.Effects[1].Value == 1 &&
            actual.MoonPhase == "Full" && actual.Energy == before.Energy + 1 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => !unit.IsPyre && unit.Status("fullmoon")?.Stacks == 1) &&
            predicted.Supported && difference == null,
            "Original Moonrise differs from the native replay: " + difference);
        Console.WriteLine("NATIVE-MOON-PHASE-TRANSITION-CHECKS PASS: original Moonrise phase, status and energy in a complete battle.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
