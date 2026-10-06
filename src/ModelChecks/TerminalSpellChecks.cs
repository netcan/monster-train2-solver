using MonsterTrain2Poju.Model;

internal static class TerminalSpellChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(1);
        var context = new CombatContext(new CardCycleState([new(8, "spell")], [new(9, "junk")], [], rng, 0, []), rng, 0, 10, 10,
            statistics: BattleStatistics.Empty(deckCards: [8]).TrackCards([8, 9]),
            cardInstances: [CardInstanceState.Empty(8, "spell"), CardInstanceState.Empty(9, "junk")]);
        var boss = new CombatUnit(1, "boss", CombatTeam.Enemy, 7, 1, 125, true, false, true, []);
        var pyre = new CombatUnit(2, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState([new(0, false, [boss], [], context), new(1, false, [pyre], [], context)],
            [new(1, 1, false, false)], 5, context);
        var spawn = new EnemySpawnState(train, [], [], 0, false, rng, 3, [], 0, false, 0, 0, 0, []);
        var root = new BattleTurnState(spawn, 1, 3, 5, 0, 0, "Full", [],
            [new("Standby", []), new("Exhausted", []), new("Purged", []), new("Eaten", [])], [],
            new([new(0, 5, 5, true, false, false), new(1, 5, 5, true, false, true)],
                [new("spell", "spell", 1, "Spell", "Discard", null, [], [new("Damage", "DropTargetCharacter", 5, true, false, [])])]));
        var killed = BattleActionModel.PlayCard(root, new(8, 0, targetUnitId: 1));
        Require(killed.Supported && killed.Outcome == RoomOutcome.BattleWon, "A terminal spell did not finish the battle: " + killed.Reason);
        BattleStatistics statistics = killed.State!.Spawn.Train.Context!.Statistics!;
        Require(statistics.Value(8, "TimesPlayed") == 0 && statistics.CardsPlayedThisTurn.Count == 0 &&
            statistics.PlayedCosts.Single().Cost == 1 && statistics.TrackedCards.SequenceEqual([8]) &&
            statistics.Value(8, "HeroesKilled") == 1, "Terminal spells counted post-cast/discard callbacks or retained temporary deck statistics.");
        Console.WriteLine("TERMINAL-SPELL-CHECKS PASS: stopped callbacks, retained cost and permanent deck statistics.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
