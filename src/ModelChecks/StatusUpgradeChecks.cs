using System.Text.Json;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;

internal static class StatusUpgradeChecks
{
    private const string AbilityId = "cad96b9a-a638-44ab-a0f9-488f8120b9ce";
    private const string UpgradeId = "bb6011ba-d63b-47c4-8200-137cfa6e03f7";

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 103 &&
            fixture.GetProperty("ModifierScenario").GetString() == "status-upgrade" &&
            fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Native status-upgrade battle did not finish with a complete match.");
        FixtureValue[] actions = fixture.GetProperty("Actions").EnumerateArray()
            .Where(record => record.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        Require(actions.Length == 1, "Expected one original Solgard ability activation.");
        FixtureValue record = actions[0];
        BattleTurnState before = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = record.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == AbilityId);
        Require(action.ActivatorUnitId > 0 &&
            before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == AbilityId &&
            rule.Effects.Count == 3 && rule.Effects[1].Type == "UnitUpgradePerStatus" &&
            rule.Effects[1].Target == "Self" && rule.Effects[1].Statuses.Single().Id == "stygian_blessing" &&
            rule.Effects[1].Upgrade?.DataId == UpgradeId &&
            rule.Effects[1].Lifetime == "TemporaryUntilEndOfBattle",
            "Marked action is not the original status-scaled Solgard ability.");
        CombatUnit Actor(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == action.ActivatorUnitId);
        int SpawnerUpgrades(BattleTurnState state, CombatUnit unit) =>
            state.Spawn.Train.Context!.FindCard(unit.SpawnerCardId)!.Temporary.Upgrades.Count;
        CombatUnit source = Actor(before);
        CombatUnit result = Actor(actual);
        Require(source.Status("stygian_blessing")?.Stacks == 2 &&
            source.Ability != null && source.BaseAttack == 8 && source.Health == 25 && source.MaxHealth == 25 &&
            source.Modifiers!.Upgrades.Count == 0 && SpawnerUpgrades(before, source) == 0 &&
            result.Status("stygian_blessing")?.Stacks == 2 &&
            result.Ability == null && result.BaseAttack == 24 && result.Health == 41 && result.MaxHealth == 41 &&
            result.Modifiers!.Upgrades.Count == 2 && SpawnerUpgrades(actual, result) == 2,
            "Two native status stacks did not add two unit and spawner upgrades.");
        BattleActionResult predicted = BattleActionModel.PlayCard(before, action);
        Require(predicted.Supported &&
            BattleTurnChecks.Comparable(predicted.State!) == BattleTurnChecks.Comparable(actual),
            "Original Solgard activation differs from the immutable replay: " + predicted.Reason);
        Console.WriteLine("NATIVE-STATUS-UPGRADE-CHECKS PASS: two stacks gave +16 attack, +16 HP and two spawner upgrades.");
    }

    internal static void Run()
    {
        var rng = UnityRng.Seed(481);
        var upgrade = new CardUpgradeModifier("status-upgrade", "status-upgrade",
            new(damage: 2), [], false, false, false, 0, 0, []);
        CardActionEffect Effect(string target = "Self") => new("UnitUpgradePerStatus", target, 0,
            true, true, [new("stygian_blessing", 1)], upgrade, "TemporaryUntilEndOfBattle");
        TrainCombatState Train(int stacks)
        {
            var owner = CardInstanceState.Empty(8, "unit");
            var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 10, 10,
                statistics: BattleStatistics.Empty().TrackCards([8]), cardInstances: [owner]);
            var unit = new CombatUnit(1, "unit", CombatTeam.Player, 5, 20, 20, true, false, false,
                stacks == 0 ? [] : [new("stygian_blessing", stacks, stackable: true)], spawnerCardId: 8,
                modifiers: new(5, 0, 0, 1, 1, true, false, []));
            return new TrainCombatState([new RoomCombatState(0, false, [unit], [], context),
                new RoomCombatState(1, false, [], [], context)], [], 1, context);
        }
        int Attack(TrainCombatState train) => train.Rooms[0].Units.Single().BaseAttack;
        int Copies(TrainCombatState train) => train.Context!.FindCard(8)!.Temporary.Upgrades.Count;

        TrainCombatState root = Train(2);
        string parent = JsonSerializer.Serialize(root);
        SpellCastCheck cast = CardSpellModel.TestPlay(root, 0, [Effect()], 0, selfUnitId: 1);
        Require(cast.Supported && cast.CanPlay,
            "Status-scaled upgrade rejected a valid self target: " + cast.UnsupportedReason);
        TrainSpellResult doubled = CardSpellModel.Apply(root, 0, [Effect()], 0, selfUnitId: 1);
        TrainCombatState? doubledState = doubled.State;
        Require(doubledState != null && Attack(doubledState) == 9 && Copies(doubledState) == 2 &&
            doubledState.Rooms[0].Units.Single().Modifiers!.Upgrades.Count == 2,
            "Two status stacks did not cause two independent unit and spawner upgrades: " + doubled.UnsupportedReason);
        TrainSpellResult empty = CardSpellModel.Apply(Train(0), 0, [Effect()], 0, selfUnitId: 1);
        TrainSpellResult single = CardSpellModel.Apply(Train(1), 0, [Effect()], 0, selfUnitId: 1);
        TrainCombatState? emptyState = empty.State;
        TrainCombatState? singleState = single.State;
        Require(CardSpellModel.TestPlay(Train(0), 0, [Effect()], 0, selfUnitId: 1).CanPlay &&
            emptyState != null && Attack(emptyState) == 5 && Copies(emptyState) == 0 &&
            singleState != null && Attack(singleState) == 7 && Copies(singleState) == 1,
            "Zero or one status stack repeated the upgrade incorrectly.");
        Require(!CardSpellModel.Apply(root, 0, [Effect("Room")], 0, selfUnitId: 1).Supported &&
            JsonSerializer.Serialize(root) == parent,
            "Unverified multi-target upgrade was accepted or the parent train changed.");
        string expected = JsonSerializer.Serialize(doubledState);
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, 0,
            [Effect()], 0, selfUnitId: 1).State) == expected, "Parallel status-upgrade branches diverged."));
        Console.WriteLine("STATUS-UPGRADE-CHECKS PASS: zero, one and two stacks, spawner writes, rejection and parallel branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
