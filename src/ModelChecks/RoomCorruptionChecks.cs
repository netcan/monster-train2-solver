using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomCorruptionChecks
{
    private const string UnearthedId = "17fb5276-2311-44c9-b662-b73581d9af56";
    private const string EchoesId = "78996030-e8d1-4731-897e-ee4b809fa26c";

    internal static void Run()
    {
        TrainCombatState Train(int current, int maximum, int permanent = 0, CombatRelicState? relic = null)
        {
            UnityRng rng = UnityRng.Seed(227);
            var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 10, 10,
                roomCapacities: [new RoomCapacityState(0, 5, 5, false, current, [], maximum, permanent),
                    new RoomCapacityState(1, 5, 5, true, 0, [], 1, 0)],
                relics: relic == null ? [] : [relic]);
            return new TrainCombatState([new RoomCombatState(0, false, [], [], context),
                new RoomCombatState(1, false, [], [], context)], [], 1, context);
        }
        CardActionEffect Effect(string kind, int value, bool? add = null) =>
            new(kind, "Room", value, false, false, [], corruptionAdd: add);
        RoomCapacityState Room(CombatContext context) => context.RoomCapacities!.Single(room => room.RoomIndex == 0);
        TrainCombatState source = Train(2, 4, 1);
        string parent = JsonSerializer.Serialize(source);
        TrainSpellResult paired = CardSpellModel.Apply(source, 0,
            [Effect("AdjustMaxCorruption", 1), Effect("AdjustCorruption", 1, true)], 0);
        Require(paired.Supported && Room(paired.State!.Context!).Corruption == 3 &&
            Room(paired.State.Context!).MaxCorruption == 5 && Room(paired.State.Context!).PermanentCorruption == 1,
            "Maximum and current corruption did not settle in native effect order: " + paired.UnsupportedReason);
        RoomCorruptionResult filled = RoomCorruptionModel.Apply(source, 0, Effect("AdjustCorruption", -1, true));
        Require(filled.Supported && Room(filled.Context!).Corruption == 4,
            "Negative add amount did not fill to the current maximum.");
        RoomCorruptionResult overMaximum = RoomCorruptionModel.Apply(Train(6, 4), 0, Effect("AdjustCorruption", -1, true));
        Require(overMaximum.Supported && Room(overMaximum.Context!).Corruption == 6,
            "Fill-to-max incorrectly reduced corruption already above maximum.");
        RoomCorruptionResult removed = RoomCorruptionModel.Apply(source, 0, Effect("AdjustCorruption", -1, false));
        Require(removed.Supported && Room(removed.Context!).Corruption == 1 &&
            RoomCorruptionModel.Test(source.Rooms[0], Effect("AdjustCorruption", -1, false)) &&
            !RoomCorruptionModel.Test(source.Rooms[0], Effect("AdjustCorruption", 3, false)),
            "Remove-all ignored the permanent floor or native cast test.");
        RoomCorruptionResult beyondMax = RoomCorruptionModel.Apply(source, 0, Effect("AdjustCorruption", 8, true));
        Require(beyondMax.Supported && Room(beyondMax.Context!).Corruption == 10 && Room(beyondMax.Context!).MaxCorruption == 4,
            "Fixed corruption addition was incorrectly capped by the maximum.");
        var callback = new CombatRelicState("callback", "callback", ["RelicEffectDrawOnCorruptionAdded"]);
        RoomCorruptionResult rejected = RoomCorruptionModel.Apply(Train(0, 4, relic: callback), 0,
            Effect("AdjustCorruption", 1, true));
        Require(!rejected.Supported && rejected.UnsupportedReason!.Contains("relic callbacks", StringComparison.Ordinal) &&
            JsonSerializer.Serialize(source) == parent,
            "Unmodeled corruption callback was accepted or a parent branch changed.");
        string expected = JsonSerializer.Serialize(paired.State);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(source, 0,
            [Effect("AdjustMaxCorruption", 1), Effect("AdjustCorruption", 1, true)], 0).State) == expected,
            "Parallel corruption branches diverged."));
        Console.WriteLine("ROOM-CORRUPTION-CHECKS PASS: max/current order, fill, remove, permanent floor, callback guard and branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "corruption" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native corruption battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 2, "Expected original increase and removal actions.");
        for (int index = 0; index < actions.Length; index++)
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            string expectedId = index == 0 ? UnearthedId : EchoesId;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == expectedId);
            RoomCapacityState oldRoom = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
            RoomCapacityState newRoom = actual.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
            CardActionEffect corruption = rule.Effects[1];
            Require(before.Spawn.Train.Context.FindCard(action.CardInstanceId)?.DataId == expectedId &&
                rule.Effects.Count == 2 && action.RoomIndex == 0 && corruption.Type == "AdjustCorruption" &&
                corruption.Value == (index == 0 ? 1 : -1) && corruption.CorruptionAdd == (index == 0) &&
                oldRoom.Corruption == index && oldRoom.MaxCorruption == 4 + index &&
                newRoom.Corruption == 1 - index && newRoom.MaxCorruption == 5 &&
                oldRoom.PermanentCorruption == 0 && newRoom.PermanentCorruption == 0 &&
                (index != 0 || rule.Effects[0].Type == "AdjustMaxCorruption" && rule.Effects[0].Value == 1) &&
                (index != 1 || rule.Effects[0].Type == "UnitUpgrade" && action.TargetUnitId > 0),
                "Marked action does not match the original ordered corruption effects.");
            if (index == 1)
            {
                CombatUnit oldTarget = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Id == action.TargetUnitId);
                CombatUnit newTarget = actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Id == action.TargetUnitId);
                CardUpgradeModifier applied = newTarget.Modifiers!.Upgrades.Single();
                Require(oldTarget.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal) &&
                    newTarget.BaseAttack == oldTarget.BaseAttack + 8 && applied.Stats.Damage == 8 &&
                    applied.Lifecycle?.Values.Stats.Damage == 8,
                    "Echoes did not scale its stored upgrade from pre-removal corruption.");
            }
            BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
            string? difference = predicted.State == null ? predicted.Reason :
                ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State), BattleTurnChecks.Comparable(actual));
            Require(predicted.Supported && difference == null,
                "Original corruption action differs from the native replay: " + difference);
        }
        Console.WriteLine("NATIVE-ROOM-CORRUPTION-CHECKS PASS: ordered increase and corruption-scaled removal in a complete battle.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
