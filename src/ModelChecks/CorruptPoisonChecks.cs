using System;
using System.Linq;
using MonsterTrain2Poju.Model;

internal static class CorruptPoisonChecks
{
    internal static void Run()
    {
        CombatRelicState adder = new("corrupt-poison-adder", "IncreaseDecaySlimeStacks",
            [RelicModel.AddStatusOnApplied], statusStackAdditions: [new(0, "None", "corrupt_poison", 1, 0)]);
        RoomCombatState turnOne = State(turn: 1, corruption: 2, adder: adder);
        RoomCombatResult applied = StatusApplicationModel.Apply(turnOne, 1, new("corrupt_poison", 1, 5));
        Require(applied.Supported && applied.State!.Units.Single(unit => unit.Id == 1).Status("corrupt_poison")?.Stacks == 2,
            applied.UnsupportedReason ?? "IncreaseDecaySlimeStacks did not add one Corrupt Poison stack.");
        RoomCombatResult resolved = RoomCombatModel.Resolve(applied.State!);
        Require(resolved.Supported && resolved.State!.Units.Single(unit => unit.Id == 1).Health == 80 &&
            resolved.Events.Any(item => item.Kind == "CorruptPoison" && item.Target == 1 && item.Amount == 20),
            resolved.UnsupportedReason ?? "Corrupt Poison did not deal stacks x base damage x room corruption.");

        RoomCombatResult withPyregel = RoomCombatModel.Resolve(State(turn: 1, corruption: 2,
            statusStacks: 2, pyregelStacks: 3));
        Require(withPyregel.Supported && withPyregel.Events.Any(item => item.Kind == "CorruptPoison" &&
            item.Target == 1 && item.Amount == 23),
            withPyregel.UnsupportedReason ?? "Pyregel did not modify Corrupt Poison damage.");

        RoomCombatResult relentless = RoomCombatModel.Resolve(State(turn: 1, corruption: 2,
            statusStacks: 2, relentless: true));
        Require(relentless.Supported && relentless.Rounds == 3 &&
            relentless.Events.Count(item => item.Kind == "CorruptPoison" && item.Target == 1) == 1,
            relentless.UnsupportedReason ?? "Corrupt Poison did not resolve once after the final relentless exchange.");

        RoomCombatResult firstTurn = RoomCombatModel.Resolve(State(turn: 0, corruption: 2, adder: adder,
            statusStacks: 2));
        Require(firstTurn.Supported && firstTurn.State!.Units.Single(unit => unit.Id == 1).Health == 100 &&
            firstTurn.Events.All(item => item.Kind != "CorruptPoison"),
            firstTurn.UnsupportedReason ?? "Corrupt Poison triggered on battle turn zero.");

        RoomCombatResult missingTurn = RoomCombatModel.Resolve(State(turn: null, corruption: 2, statusStacks: 1));
        RoomCombatResult missingCorruption = RoomCombatModel.Resolve(State(turn: 1, corruption: null, statusStacks: 1));
        Require(!missingTurn.Supported && missingTurn.UnsupportedReason!.Contains("battle turn", StringComparison.Ordinal) &&
            !missingCorruption.Supported && missingCorruption.UnsupportedReason!.Contains("room corruption", StringComparison.Ordinal),
            "Corrupt Poison guessed a missing battle-turn or room-corruption input.");
        Console.WriteLine("CORRUPT-POISON-CHECKS PASS: original status adder, turn-zero gate, corruption-scaled damage and missing-input rejection.");
    }

    private static RoomCombatState State(int? turn, int? corruption, CombatRelicState? adder = null,
        int statusStacks = 0, int pyregelStacks = 0, bool relentless = false)
    {
        UnityRng rng = UnityRng.Seed(41);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10,
            queryFrame: turn.HasValue ? new StatisticQueryFrame(turn: turn) : null,
            roomCapacities: [new RoomCapacityState(0, 5, 5, false, corruption)],
            relics: adder == null ? [] : [adder]);
        var enemyStatuses = new List<CombatStatus>();
        if (statusStacks > 0) enemyStatuses.Add(new CombatStatus("corrupt_poison", statusStacks, 5));
        if (pyregelStacks > 0) enemyStatuses.Add(new CombatStatus("pyregel", pyregelStacks, 1));
        if (relentless) enemyStatuses.Add(new CombatStatus("relentless", 1, 1));
        return new RoomCombatState(0, false,
            [new CombatUnit(1, "enemy", CombatTeam.Enemy, relentless ? 10 : 0, 100, 100, true, false, false, enemyStatuses),
             new CombatUnit(2, "player", CombatTeam.Player, 0, relentless ? 25 : 100, relentless ? 25 : 100,
                 !relentless, false, false, [])],
            [], context);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
