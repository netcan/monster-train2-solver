using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class TerminalSpellChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(1);
        foreach (bool generated in new[] { false, true })
        {
            var context = new CombatContext(new CardCycleState([new(8, "spell")], [new(9, "junk")], [], rng, 0, []), rng, 0, 10, 10,
                statistics: BattleStatistics.Empty(deckCards: generated ? [] : [8]).TrackCards([8, 9]),
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
            Require(statistics.Value(8, "TimesPlayed") == (generated ? 0 : 1) && statistics.Value(8, "TimesDiscarded") == 1 &&
                statistics.CardsPlayedThisTurn.SequenceEqual([8]) && statistics.PlayedCosts.Count == 0 &&
                statistics.TrackedCards.SequenceEqual([8]) && statistics.Value(8, "HeroesKilled") == (generated ? 0 : 1) &&
                killed.State.Spawn.Train.Context!.Cards.Discard.Single().InstanceId == 8 &&
                killed.State.Spawn.Train.Context.CardInstances!.Single().PlayCount == 1,
                "Settled terminal spells lost played/discard callbacks, retained costs, or did not restore the resolving card.");
        }
        Console.WriteLine("TERMINAL-SPELL-CHECKS PASS: settled callbacks, restored card instance, cleared cost and permanent/generated deck statistic membership.");
    }
    internal static void Native(JsonElement entry)
    {
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext old = before.Spawn.Train.Context!, settled = actual.Spawn.Train.Context!;
        CardInstanceState instance = old.CardInstances!.Single(card => card.InstanceId == action.CardInstanceId);
        CardPlayRule rule = CardModifierModel.Resolve(before.PlayRules!.Cards.Single(card => card.DataId == instance.DataId), instance);
        bool permanent = old.Statistics!.DeckCards!.Contains(instance.InstanceId);
        Require(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.EndsBattleOnDeath) &&
            settled.Cards.Discard.Single().InstanceId == instance.InstanceId && settled.CardInstances!.Single().PlayCount == instance.PlayCount + 1 &&
            settled.Statistics!.Value(instance.InstanceId, "TimesPlayed") == (permanent ? old.Statistics.Value(instance.InstanceId, "TimesPlayed") + 1 : 0) &&
            settled.Statistics.Value(instance.InstanceId, "TimesDiscarded") == (permanent ? old.Statistics.Value(instance.InstanceId, "TimesDiscarded") + 1 : 1) &&
            settled.Statistics.PlayedCosts.Count == 0 && settled.Statistics.CardsPlayedThisTurn.Contains(instance.InstanceId) &&
            actual.Energy == before.Energy - rule.Cost,
            "The settled terminal oracle lacks cast/discard callbacks, restored card state or paid energy.");
        Console.WriteLine("NATIVE-TERMINAL-SPELL-COVERAGE PASS: native boss kill, completed callbacks, paid energy, cleared cost and restored resolving card.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
