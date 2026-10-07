using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class DecisionReferenceChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(71);
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("Eaten", []), new("Purged", []), new("DiscardBuffer", [])];
        var context = new CombatContext(new([new(1, "card")], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: new([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1], [1]), cardInstances: [CardInstanceState.Empty(1, "card")], otherPiles: piles);
        var attacker = new CombatUnit(10, "attacker", CombatTeam.Player, 5, 80, 80, true, false, false, [], lastAttackerId: 11);
        var boss = new CombatUnit(11, "boss", CombatTeam.Enemy, 0, 1, 1, true, false, true, [], isBoss: true, lastAttackerId: 0);
        var pyre = new CombatUnit(12, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [], lastAttackerId: 99);
        var train = new TrainCombatState([new(0, false, [boss, attacker], [], context), new(1, false, [pyre], [], context)], [], 7, context);
        var spawn = new EnemySpawnState(train, [], [], 0, false, rng, 13, [], 0, false, 0, 0, 2, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false)], [new("card", "card", 1, "Null", "Discard", null, [])]);
        BattleTurnState Root(bool canonical) => new(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 71, rng)], piles, [], rules,
            canonicalDecisionReferences: canonical);
        string parent = JsonSerializer.Serialize(Root(true));
        BattleActionResult played = BattleActionModel.PlayCard(Root(true), new(1, 0));
        Require(played.Supported && played.State!.CanonicalDecisionReferences &&
            played.State.Spawn.Train.Rooms[1].Units.Single().LastAttackerId == 0 &&
            played.State.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 10).LastAttackerId == 11,
            "A quiet card decision retained a removed attacker or erased a live attacker.");
        Require(BattleActionModel.PlayCard(Root(false), new(1, 0)).State!.Spawn.Train.Rooms[1].Units.Single().LastAttackerId == 99,
            "Legacy snapshots changed their reference convention.");
        RoomCombatResult room = RoomCombatModel.Resolve(train.Rooms[0]);
        Require(room.Supported && room.Outcome == RoomOutcome.BattleWon && room.State!.Units.Single().LastAttackerId == 11,
            "An in-flight room boundary erased its retained attacker too early.");
        BattleTurnResult terminal = BattleTurnModel.EndTurn(played.State!);
        Require(terminal.Supported && terminal.Outcome == RoomOutcome.BattleWon && terminal.State!.CanonicalDecisionReferences &&
            terminal.State.Spawn.Train.Rooms[0].Units.Single().LastAttackerId == 0,
            "A settled winning decision retained its removed boss reference.");
        string expected = JsonSerializer.Serialize(terminal.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(BattleTurnModel.EndTurn(
            BattleActionModel.PlayCard(Root(true), new(1, 0)).State!).State) == expected, "Parallel canonical decisions diverged."));
        Require(JsonSerializer.Serialize(Root(true)) == parent, "Decision normalization mutated its parent.");
        Console.WriteLine("DECISION-REFERENCE-CHECKS PASS: live/removed attackers, legacy boundaries, in-flight retention, terminal settlement and 32 parallel branches.");
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
