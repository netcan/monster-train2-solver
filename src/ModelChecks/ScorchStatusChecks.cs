using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ScorchStatusChecks
{
    private const string SoulSiphonId = "95f7b6b7-a574-477d-abef-a8bd3388d87b";

    internal static void Run()
    {
        TrainCombatState Train(int stacks, int energy)
        {
            UnityRng rng = UnityRng.Seed(523);
            var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 10, 10,
                queryFrame: new StatisticQueryFrame(energy: energy, runningCombat: true, turn: 1,
                    selectedRoomIndex: 1), energyState: new BattleEnergyState(10, 0, 0, "MonsterTurn", true));
            var status = new CombatStatus("scorch", stacks, 1, removeWhenTriggered: true,
                stackable: true, displayCategory: "Negative");
            var unit = new CombatUnit(1, "steward", CombatTeam.Player, 8, 25, 25, true, false, false,
                [status], modifiers: new UnitModifiers(8, 0, 0, 2, 1, true, false, []), statusRegistry: [status]);
            return new TrainCombatState([new RoomCombatState(0, false, [unit], [], context),
                new RoomCombatState(1, false, [], [], context)], [], 1, context);
        }
        TrainCombatState one = Train(1, 3);
        string parent = JsonSerializer.Serialize(one);
        ScorchStatusResult triggered = ScorchStatusModel.StartTurn(one);
        Require(triggered.Supported && triggered.State!.Context!.QueryFrame!.Energy == 2 &&
            triggered.State.Context.QueryFrame.SelectedRoomIndex == 0 &&
            triggered.State.Rooms[0].Units[0].Status("scorch") == null &&
            triggered.State.Rooms[0].Units[0].RegisteredStatus("scorch")?.Stacks == 0,
            "One Scorch stack did not remove before deducting one energy.");
        ScorchStatusResult multiple = ScorchStatusModel.StartTurn(Train(3, 2));
        Require(multiple.Supported && multiple.State!.Context!.QueryFrame!.Energy == 0 &&
            multiple.State.Rooms[0].Units[0].Status("scorch")?.Stacks == 2,
            "Scorch did not use its post-removal stacks or clamp energy at zero.");
        Require(JsonSerializer.Serialize(one) == parent, "Scorch changed its parent branch.");
        string expected = JsonSerializer.Serialize(triggered.State);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(ScorchStatusModel.StartTurn(one).State) == expected,
            "Parallel Scorch branches diverged."));
        Console.WriteLine("SCORCH-CHECKS PASS: pre-trigger removal, post-removal energy, zero clamp and parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "soul-siphon" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0,
            "Native Soul Siphon battle did not finish with complete captures.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 1, "Expected one original Soul Siphon action.");
        BattleTurnState before = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == SoulSiphonId);
        RoomCapacityState oldRoom = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
        RoomCapacityState newRoom = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
        CombatUnit target = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal));
        BattleActionResult played = BattleActionModel.PlayCard(before, action);
        Require(before.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId == SoulSiphonId &&
            rule.Effects.Count == 2 && rule.Effects[0].Type == "AdjustCorruption" &&
            rule.Effects[0].Value == -1 && rule.Effects[0].CorruptionAdd == true &&
            rule.Effects[1].Type == "AddStatus" && rule.Effects[1].Statuses.Single().Id == "scorch" &&
            oldRoom.Corruption == 0 && newRoom.Corruption == oldRoom.MaxCorruption &&
            target.Status("scorch")?.Stacks == 1 && played.Supported &&
            ModelJson.Difference(BattleTurnChecks.Comparable(played.State!), BattleTurnChecks.Comparable(actual)) == null,
            "Original Soul Siphon fill or status application differs from native.");
        FixtureValue firstTurn = fixture.GetProperty("Turns")[0];
        BattleTurnState turnBefore = firstTurn.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState turnActual = firstTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnResult predicted = BattleTurnModel.EndTurn(turnBefore);
        string? difference = predicted.State == null ? predicted.UnsupportedReason :
            ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(turnActual));
        Require(predicted.Supported && turnBefore.Spawn.Train.Rooms[0].Units
            .Any(unit => unit.Status("scorch")?.Stacks == 1) && turnActual.Energy == 2 &&
            turnActual.Spawn.Train.Rooms[0].Units.All(unit => unit.Status("scorch") == null) && difference == null,
            "Native Scorch turn result differs: " + difference);
        Console.WriteLine("NATIVE-SCORCH-CHECKS PASS: original corruption fill and first-turn Scorch energy/removal.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
