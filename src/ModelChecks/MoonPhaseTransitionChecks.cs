using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonPhaseTransitionChecks
{
    private const string MoonriseId = "fe6dfafa-9712-484c-9bb4-145c0989e667";
    private const string FaeMeddlerId = "d10e2dac-fa58-4509-b0ee-112b3bf78800";
    private const string SeleneAcolyteId = "2b7a187f-da0b-4c34-b8c5-b18755911b43";

    internal static void Run()
    {
        TrainCombatState Train(bool tracking, bool callback = false, string phase = "MonsterTurn")
        {
            UnityRng rng = UnityRng.Seed(641);
            var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 10, 10,
                queryFrame: new StatisticQueryFrame(energy: 3, runningCombat: true, turn: 1,
                    moonPhase: 1, moonPhaseTrackingActive: tracking),
                energyState: new BattleEnergyState(10, 0, 0, phase, true), relics: []);
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
        TrainCombatState outsideAction = Train(true, phase: "HeroTurn");
        MoonPhaseTransitionResult skipped = MoonPhaseTransitionModel.Apply(outsideAction, Effect("AdvanceMoonPhase", 1));
        Require(skipped.Supported && ReferenceEquals(skipped.State, outsideAction),
            "Moon-phase advance changed a non-player-action phase.");
        Require(!MoonPhaseTransitionModel.Apply(Train(true, callback: true), Effect("SetMoonPhase", 2)).Supported &&
            JsonSerializer.Serialize(source) == parent, "Moon callbacks were silently skipped or parent branch changed.");
        string expected = JsonSerializer.Serialize(full.State);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(
            MoonPhaseTransitionModel.Apply(source, Effect("SetMoonPhase", 2)).State) == expected,
            "Parallel moon-phase branches diverged."));
        Console.WriteLine("MOON-PHASE-TRANSITION-CHECKS PASS: set, repeated advance, inactive tracking, action-phase gate, callback guard and branches.");
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

    internal static void NativeAdvance(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "moon-phase-advance" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native Fae Meddler battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 1, "Expected one original Fae Meddler action.");
        BattleTurnState before = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == FaeMeddlerId);
        CombatTrigger[] phaseTriggers = rule.SpawnUnit?.Triggers.Where(trigger => trigger.Kind == "OnSpawn" &&
            trigger.Effects.Any(effect => effect.Type == "CardEffectAdvanceMoonPhase")).ToArray() ?? [];
        BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
        string? difference = predicted.State == null ? predicted.Reason :
            ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
        Require(before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == FaeMeddlerId &&
            before.MoonPhaseTrackingActive == true && before.MoonPhase == "Full" &&
            rule.Effect == "SpawnMonster" && phaseTriggers.Length == 1 &&
            phaseTriggers[0].Effects.Count == 1 &&
            phaseTriggers[0].Effects[0].Action?.Type == "AdvanceMoonPhase" &&
            phaseTriggers[0].Effects[0].Action?.Target == "Self" &&
            phaseTriggers[0].Effects[0].Value == 1 &&
            actual.MoonPhase == "New" && actual.Spawn.Train.Context?.QueryFrame?.MoonPhase == 1 &&
            actual.Spawn.Train.Rooms.Any(room => room.RoomIndex == 1 && room.Units.Any(unit =>
                !unit.IsPyre && unit.SpawnerCardId != action.CardInstanceId)) &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => !unit.IsPyre).All(unit => unit.Status("newmoon")?.Stacks == 1 &&
                    unit.Status("fullmoon") == null) &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.SpawnerCardId == action.CardInstanceId) &&
            predicted.Supported && difference == null,
            "Original Fae Meddler differs from the native replay: " + difference);
        RoomCombatState faeRoom = actual.Spawn.Train.Rooms.Single(room => room.Units.Any(unit =>
            unit.SpawnerCardId == action.CardInstanceId));
        CombatUnit fae = faeRoom.Units.Single(unit => unit.SpawnerCardId == action.CardInstanceId);
        RoomCombatResult roomOnly = RoomCombatModel.ApplyQueuedCharacterTrigger(faeRoom,
            new RoomCombatModel.QueuedCharacterTrigger(faeRoom.RoomIndex, fae, "OnSpawn"), _ => { });
        Require(!roomOnly.Supported && roomOnly.UnsupportedReason?.Contains("complete train callback") == true,
            "Room-only trigger execution silently skipped the whole-train moon-phase transition.");
        Console.WriteLine("NATIVE-MOON-PHASE-ADVANCE-CHECKS PASS: original OnSpawn advance updates the whole train in a complete battle.");
    }

    internal static void NativeCallbacks(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "moon-phase-callbacks" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native Selene Acolyte battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length >= 3 && actions[2].GetProperty("ScenarioAction").GetBoolean(),
            "Original moon-phase callback actions are missing.");
        string[] cardIds = [SeleneAcolyteId, MoonriseId, FaeMeddlerId];
        for (int index = 0; index < cardIds.Length; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            Require(before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == cardIds[index],
                "Original moon-phase callback card order changed at action " + index);
            BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
            string? difference = predicted.State == null ? predicted.Reason :
                ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
            Require(predicted.Supported && difference == null,
                "Original moon-phase callback action " + index + " differs: " + difference);
        }
        BattleTurnState full = actions[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState newMoon = actions[2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardPlayRule acolyteRule = full.PlayRules!.Cards.Single(card => card.DataId == SeleneAcolyteId);
        CombatTrigger[] callbacks = acolyteRule.SpawnUnit!.Triggers.Where(trigger =>
            trigger.Kind is "OnMoonLit" or "OnMoonlitLost").ToArray();
        Require(callbacks.Length == 2 &&
            callbacks.Single(trigger => trigger.Kind == "OnMoonLit").Effects.Single().Type == "CardEffectAddTempCardUpgradeToUnits" &&
            callbacks.Single(trigger => trigger.Kind == "OnMoonlitLost").Effects.Single().Type == "CardEffectRemoveTempUpgradeFromUnit",
            "Original Selene Acolyte moon-phase trigger definitions changed.");
        CombatUnit? fullAcolyte = full.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .SingleOrDefault(unit => unit.SpawnerCardId == actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId);
        CombatUnit? newAcolyte = newMoon.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .SingleOrDefault(unit => unit.Id == fullAcolyte?.Id);
        Require(full.MoonPhase == "Full" && newMoon.MoonPhase == "New" &&
            fullAcolyte?.Modifiers?.Upgrades.Count == 1 && newAcolyte?.Modifiers?.Upgrades.Count == 0 &&
            fullAcolyte.Status("fullmoon")?.Stacks == 1 && newAcolyte.Status("newmoon")?.Stacks == 1,
            "Original Selene Acolyte temporary upgrade did not follow the moon-phase callback lifecycle.");
        Console.WriteLine("NATIVE-MOON-PHASE-CALLBACK-CHECKS PASS: original temporary upgrade add/remove and three exact action replays.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
