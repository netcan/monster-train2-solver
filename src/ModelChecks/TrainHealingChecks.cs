using System.Text.Json;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;

internal static class TrainHealingChecks
{
    private const string PayOffId = "996a0953-168a-44b9-bb90-5e46cbbb3e09";

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "train-heal" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native train-healing battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 1, "Expected one native PayOff action.");
        FixtureValue record = actions[0];
        BattleTurnState before = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState? card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId);
        CardPlayRule rule = before.PlayRules!.Cards.Single(value => value.DataId == PayOffId);
        Require(card?.DataId == PayOffId && before.DragonsHoard == 2 &&
            card.TrainHealScalingTraits?.Count == 1 &&
            card.TrainHealScalingTraits[0].Query.Type == "DragonsHoardAmount" &&
            card.TrainHealScalingTraits[0].HealPerStat == 2 &&
            rule.Effects.Count == 1 && rule.Effects[0].Type == "HealTrain" &&
            rule.Effects[0].Target == "Pyre" && rule.Effects[0].UseCardTraits &&
            before.Spawn.Train.Context.Resources?.TowerHealingDisabled == false,
            "Marked action is not original PayOff with its native train-healing rule.");
        int Health(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.IsPyre).Health;
        BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
        Require(predicted.Supported && Health(before) == 70 && Health(actual) == 74 &&
            Health(predicted.State!) == 74 &&
            BattleTurnChecks.Comparable(predicted.State!) == BattleTurnChecks.Comparable(actual),
            "Original PayOff did not heal the Pyre by four or differed from native replay.");
        Console.WriteLine("NATIVE-TRAIN-HEAL-CHECKS PASS: original PayOff healed 70/80 to 74/80 at two Dragon's Hoard.");
    }

    internal static void Run()
    {
        const int cardId = 7;
        var rng = UnityRng.Seed(37);
        var query = new CardStatisticQuery("DragonsHoardAmount");
        var card = new CardInstanceState(cardId, "PayOff", CardModifiers.Empty(), CardModifiers.Empty(),
            0, 0, 0, [], trainHealScalingTraits: [new(query, 2, 1f)]);
        var raw = new BattleResourceState(3, 1, 5, 10, 10, "pyre", 20, 80, 0, 0, [], [], [],
            towerHealingDisabled: false);
        CombatContext Context(BattleResourceState resources) => new(new([new(cardId, "PayOff")], [], [], rng, 0, []),
            rng, 0, 8, 10, statistics: BattleStatistics.Empty(deckCards: [cardId]), cardInstances: [card],
            otherPiles: [], queryFrame: new(dragonsHoard: 5, runningCombat: true, turn: 1),
            energyState: new(10, 0, 0, "MonsterTurn", true), relics: [], resources: resources);
        RoomCombatState Room(BattleResourceState resources, int hp = 65) => new(3, false,
            [new CombatUnit(1, "pyre", CombatTeam.Player, 20, hp, 80, true, true, false, [],
                [new CombatTrigger("OnHeal", true, false, false, 1, [], false)])], [], Context(resources));
        CardActionEffect Effect(bool scale = true, int amount = 0) => new("HealTrain", "Pyre", amount,
            false, false, [], useCardTraits: scale);

        RoomCombatState root = Room(raw);
        string parent = JsonSerializer.Serialize(root);
        RoomCombatResult scaled = CardSpellModel.Apply(root, [Effect()], 0, cardId);
        Require(scaled.Supported, scaled.UnsupportedReason ?? "Train healing failed.");
        Require(scaled.State!.Units.Single().Health == 75 &&
            !scaled.State.Units.Single().Triggers.Single().HasTriggered,
            "Train healing did not use the current Dragon's Hoard or fired unit OnHeal.");
        Require(CardSpellModel.Apply(Room(raw, 78), [Effect()], 0, cardId).State!.Units.Single().Health == 80 &&
            CardSpellModel.Apply(root, [Effect(false, 4)], 0, cardId).State!.Units.Single().Health == 69,
            "Train healing ignored its cap, fixed amount or trait toggle.");
        var disabled = new BattleResourceState(3, 1, 5, 10, 10, "pyre", 20, 80, 0, 0, [], [], [],
            towerHealingDisabled: true);
        Require(CardSpellModel.Apply(Room(disabled), [Effect()], 0, cardId).State!.Units.Single().Health == 65 &&
            !CardSpellModel.Apply(Room(new BattleResourceState(3, 1, 5, 10, 10, "pyre", 20, 80, 0, 0,
                [], [], [])), [Effect()], 0, cardId).Supported,
            "Disabled or uncaptured tower-healing rules were ignored.");
        string expected = JsonSerializer.Serialize(scaled.State);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, [Effect()], 0, cardId).State) == expected,
            "Parallel train-healing branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Train healing changed its parent state.");
        Console.WriteLine("TRAIN-HEAL-CHECKS PASS: scaling, cap, fixed amount, disabled rule, no OnHeal, 16 branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
